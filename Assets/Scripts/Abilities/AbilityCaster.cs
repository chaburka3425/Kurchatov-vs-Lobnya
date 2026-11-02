using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Способности героя на Q/W/E/R.
// Нажал клавишу → выбери цель левой кнопкой мыши (ПКМ — отмена).
// Если цель далеко — герой сам подойдёт и применит.
[RequireComponent(typeof(Unit))]
public class AbilityCaster : MonoBehaviour
{
    public string heroId = "ogr_mog";

    public Ability[] Abilities { get; private set; }
    public int TargetingIndex { get; private set; } = -1;
    public bool IsTargeting => TargetingIndex >= 0;

    static readonly Key[] Keys = { Key.Q, Key.W, Key.E, Key.R };

    Unit self;
    Mana mana;
    StatusEffects status;
    NavMeshAgent agent;
    AutoAttack attack;
    Camera cam;

    int pendingIndex = -1;
    Unit pendingTarget;
    bool pendingIsPoint;
    Vector3 pendingPoint;

    void Awake()
    {
        self = GetComponent<Unit>();
        mana = GetComponent<Mana>();
        status = GetComponent<StatusEffects>();
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<AutoAttack>();

        BuildAbilities();
    }

    void BuildAbilities()
    {
        var def = HeroCatalog.Get(heroId);
        var list = new List<Ability>();
        foreach (var info in def.abilities)
        {
            var a = AbilityFactory.Create(info);
            if (a != null) list.Add(a);
        }
        Abilities = list.ToArray();
    }

    // Сменить героя (выбор в лобби)
    public void SetHero(string id)
    {
        heroId = id;
        BuildAbilities();
        TargetingIndex = -1;
        CancelPending();
    }

    bool PlayerInput
    {
        get
        {
            var hc = GetComponent<HeroController>();
            return hc != null && hc.playerControlled && !MatchSetup.InLobby && !hc.IsBusyChanneling;
        }
    }

    // Для бота: применить способность сразу (без мыши). true — получилось.
    public bool BotCast(int i, Unit target)
    {
        if (Abilities == null || i < 0 || i >= Abilities.Length || !self.IsAlive) return false;
        var a = Abilities[i];
        if (!a.IsLearned || a.IsPassive || a.CooldownLeft > 0f) return false;
        if (status != null && status.IsStunned) return false;
        var hc = GetComponent<HeroController>();
        if (hc != null && hc.IsBusyChanneling) return false;
        if (mana != null && mana.Current < a.ManaCost) return false;
        if (target == null) target = self;
        if (a.Target != AbilityTarget.NoTarget && !a.IsValidTarget(self, target)) return false;
        if (a.Target != AbilityTarget.NoTarget && target.DistanceFrom(transform.position) > a.CastRange) return false;
        DoCast(i, target);
        return true;
    }

    // Для бота: способность в точку
    public bool BotCastPoint(int i, Vector3 point)
    {
        if (Abilities == null || i < 0 || i >= Abilities.Length || !self.IsAlive) return false;
        var a = Abilities[i];
        if (a.Target != AbilityTarget.Point || !CanCast(i)) return false;
        if (status != null && status.IsStunned) return false;
        var hc = GetComponent<HeroController>();
        if (hc != null && hc.IsBusyChanneling) return false;
        if (Flat(point, transform.position) > a.CastRange) return false;
        DoCastPoint(i, point);
        return true;
    }

    public bool CanCast(int i)
    {
        if (Abilities == null || i < 0 || i >= Abilities.Length) return false;
        var a = Abilities[i];
        return a.IsLearned && !a.IsPassive && a.CooldownLeft <= 0f && (a.Info.charges == 0 || a.Charges > 0) && (mana == null || mana.Current >= a.ManaCost);
    }

    static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

    void Update()
    {
        foreach (var a in Abilities)
        {
            if (a.CooldownLeft > 0f) a.CooldownLeft = Mathf.Max(0f, a.CooldownLeft - Time.deltaTime);
            // заряды копятся по одному
            if (a.Info.charges > 0 && a.IsLearned && a.Charges < a.Info.charges)
            {
                a.ChargeTimer -= Time.deltaTime;
                if (a.ChargeTimer <= 0f) { a.Charges++; a.ChargeTimer = a.Charges < a.Info.charges ? a.Info.chargeCooldown : 0f; }
            }
        }

        if (!self.IsAlive) { TargetingIndex = -1; pendingIndex = -1; return; }
        var gm = GameManager.Instance;
        if (gm != null && (gm.IsGameOver || gm.IsPaused)) return;

        if (!PlayerInput) { UpdatePending(); return; }

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        if (kb != null)
            for (int i = 0; i < Keys.Length && i < Abilities.Length; i++)
                if (kb[Keys[i]].wasPressedThisFrame)
                {
                    // Shift + клавиша — показывать радиус способности постоянно (ещё раз — убрать)
                    if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                        RangeIndicator.For(gameObject).TogglePin(i);
                    // Ctrl + клавиша — изучить способность
                    else if (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed)
                    {
                        var progress = GetComponent<HeroProgress>();
                        if (progress != null && !progress.Learn(i)) Say("Нельзя изучить");
                    }
                    else OnKey(i);
                }

        if (TargetingIndex >= 0 && mouse != null && !TouchInput.UsingTouch)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (HudArea.IsOver(mouse.position.ReadValue())) TargetingIndex = -1; // клик по интерфейсу — отмена
                else TryClickTarget(mouse.position.ReadValue());
            }
            else if (mouse.rightButton.wasPressedThisFrame) TargetingIndex = -1;
        }

        UpdatePending();
    }

    // Нажатие на способность (клавиша или кнопка на панели).
    public void PressAbility(int i)
    {
        if (Abilities == null || i < 0 || i >= Abilities.Length) return;
        var gm = GameManager.Instance;
        if (gm != null && (gm.IsGameOver || gm.IsPaused)) return;
        if (!self.IsAlive || !PlayerInput) return;
        OnKey(i);
    }

    // Выбор цели тапом/кликом в точке экрана.
    public void TargetAt(Vector2 screenPos)
    {
        if (TargetingIndex < 0) return;
        TryClickTarget(screenPos);
    }

    void OnKey(int i)
    {
        var a = Abilities[i];
        if (!a.IsLearned) { Say("Способность не изучена"); return; }
        if (a.IsPassive) { Say("Это пассивная способность"); return; }
        if (status != null && status.IsStunned) { Say("Оглушён!"); return; }
        if (a.CooldownLeft > 0f) { Say($"Перезарядка: {Mathf.CeilToInt(a.CooldownLeft)} сек."); return; }
        if (mana != null && mana.Current < a.ManaCost) { Say("Не хватает маны"); return; }

        switch (a.Target)
        {
            case AbilityTarget.NoTarget:
                DoCast(i, self);
                break;
            case AbilityTarget.AllyUnit when TargetingIndex == i:
                TargetingIndex = -1;          // двойное нажатие — на себя
                BeginCast(i, self);
                break;
            default:
                TargetingIndex = i;
                break;
        }
    }

    void TryClickTarget(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        int i = TargetingIndex;
        TargetingIndex = -1;
        if (cam == null) return;

        if (Abilities[i].Target == AbilityTarget.Point)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray = cam.ScreenPointToRay(screenPos);
            if (plane.Raycast(ray, out float dist)) { BeginCastPoint(i, ray.GetPoint(dist)); return; }
            Say("Неверная точка");
            return;
        }

        if (Physics.Raycast(cam.ScreenPointToRay(screenPos), out RaycastHit hit, 500f))
        {
            var u = hit.collider.GetComponentInParent<Unit>();
            if (Abilities[i].IsValidTarget(self, u)) { BeginCast(i, u); return; }
        }
        Say("Неверная цель");
    }

    void BeginCast(int i, Unit target)
    {
        if (target.DistanceFrom(transform.position) <= Abilities[i].CastRange)
        {
            DoCast(i, target);
            return;
        }
        // Далеко — подходим.
        pendingIndex = i;
        pendingTarget = target;
        pendingIsPoint = false;
        if (attack != null) attack.ClearTarget();
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }

    void BeginCastPoint(int i, Vector3 point)
    {
        if (Flat(point, transform.position) <= Abilities[i].CastRange) { DoCastPoint(i, point); return; }
        pendingIndex = i;
        pendingTarget = null;
        pendingIsPoint = true;
        pendingPoint = point;
        if (attack != null) attack.ClearTarget();
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }

    void UpdatePending()
    {
        if (pendingIndex < 0) return;
        if (status != null && status.IsStunned) return;

        if (pendingIsPoint)
        {
            if (Flat(pendingPoint, transform.position) <= Abilities[pendingIndex].CastRange)
            {
                int k = pendingIndex;
                pendingIndex = -1;
                if (agent != null && agent.isOnNavMesh) agent.ResetPath();
                DoCastPoint(k, pendingPoint);
            }
            else if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(pendingPoint);
            }
            return;
        }

        if (pendingTarget == null || !Abilities[pendingIndex].IsValidTarget(self, pendingTarget))
        {
            CancelPending();
            return;
        }

        if (pendingTarget.DistanceFrom(transform.position) <= Abilities[pendingIndex].CastRange)
        {
            int i = pendingIndex;
            var t = pendingTarget;
            pendingIndex = -1;
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
            DoCast(i, t);
        }
        else if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(pendingTarget.transform.position);
        }
    }

    public void CancelPending()
    {
        pendingIndex = -1;
        pendingTarget = null;
        pendingIsPoint = false;
    }

    void DoCastPoint(int i, Vector3 point)
    {
        var a = Abilities[i];
        if (!a.IsLearned || a.CooldownLeft > 0f) return;
        if (a.Info.charges > 0 && a.Charges <= 0) return;
        if (mana != null && !mana.TrySpend(a.ManaCost)) { Say("Не хватает маны"); return; }
        if (a.Info.charges > 0)
        {
            a.Charges--;
            if (a.ChargeTimer <= 0f) a.ChargeTimer = a.Info.chargeCooldown;
            a.CooldownLeft = a.Charges > 0 ? 0.4f : a.ChargeTimer;
        }
        else a.CooldownLeft = a.Cooldown;
        FaceTowards(point);
        if (attack != null) attack.CancelSwing();
        UnitAnimator.For(gameObject).Cast(i);
        CastFx(a, point);
        a.CastAtPoint(self, point, this);
    }

    void DoCast(int i, Unit target)
    {
        var a = Abilities[i];
        if (!a.IsLearned || a.CooldownLeft > 0f) return;
        if (mana != null && !mana.TrySpend(a.ManaCost)) { Say("Не хватает маны"); return; }

        a.CooldownLeft = a.Cooldown;
        int times = RollMulticast();
        FaceTowards(target.transform.position);
        if (attack != null) attack.CancelSwing();   // каст сбивает замах автоатаки
        UnitAnimator.For(gameObject).Cast(i);
        CastFx(a, target != self ? target.transform.position : (Vector3?)null);
        a.Cast(self, target, times, this);

        if (times > 1)
            FloatingText.Show(transform.position + Vector3.up * 4f, $"x{times}!", new Color(1f, 0.8f, 0.1f), 30 + times * 8);
    }

    // Каст: кольцо и искры цвета способности под героем (ульта — со вспышкой), метка в точке/на цели
    void CastFx(Ability a, Vector3? at)
    {
        if (!FogOfWar.VisibleToPlayer(self)) return;
        Color c = a.Info.color; c.a = 0.9f;
        Vector3 p = transform.position;
        if (a.Info.isUltimate)
        {
            Fx.Shockwave(p, c, 3.5f, 0.6f);
            Fx.Flash(p + Vector3.up * 1.6f, c, 2.4f, 0.35f);
            Fx.Glints(p, c, 18, 1.2f, 3f, 1.4f);
        }
        else
        {
            Fx.Ring(p, c, 0.5f, 2.2f, 0.45f, 0.16f);
            Fx.Glints(p, c, 8, 0.8f, 2f, 1f);
        }
        if (at.HasValue) Fx.Ring(at.Value, c, 1.4f, 0.3f, 0.4f, 0.12f);
    }

    int RollMulticast()
    {
        foreach (var a in Abilities)
            if (a is DoublePortionAbility dp && dp.IsLearned) return dp.Roll();
        return 1;
    }

    void FaceTowards(Vector3 p)
    {
        Vector3 d = p - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
    }

    void Say(string text) =>
        FloatingText.Show(transform.position + Vector3.up * 3.6f, text, new Color(1f, 0.6f, 0.6f), 20);

    // Найти до count ближайших юнитов в радиусе, подходящих под условие.
    public static List<Unit> FindNearby(Vector3 center, float radius, int count, Func<Unit, bool> filter)
    {
        var result = new List<Unit>();
        if (count <= 0) return result;
        var candidates = new List<Unit>();
        foreach (var u in Unit.All)
            if (filter(u) && u.DistanceFrom(center) <= radius) candidates.Add(u);
        candidates.Sort((a, b) => a.DistanceFrom(center).CompareTo(b.DistanceFrom(center)));
        for (int i = 0; i < candidates.Count && result.Count < count; i++) result.Add(candidates[i]);
        return result;
    }
}
