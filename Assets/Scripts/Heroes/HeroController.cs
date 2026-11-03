using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Управление героем: ПКМ по земле — идти, ПКМ по врагу — атаковать, S — стоп.
[RequireComponent(typeof(NavMeshAgent), typeof(Unit), typeof(AutoAttack))]
public class HeroController : MonoBehaviour
{
    public Transform respawnPoint;
    public float respawnTime = 5f;
    public float RespawnAt { get; private set; }
    [Tooltip("Управляет игрок (мышь/тач). У бота — false")]
    public bool playerControlled = true;

    // Герой, которым управляет игрок
    public static HeroController Player { get; private set; }
    public event System.Action<Unit> OnHeroDied;
    public bool IsBusyChanneling;   // ульта Сани: стоит на месте

    NavMeshAgent agent;
    Unit unit;
    Health health;
    AutoAttack attack;
    StatusEffects status;
    AbilityCaster caster;
    Mana mana;
    Camera cam;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.avoidancePriority = 20;   // крипы (50) уступают дорогу герою
        unit = GetComponent<Unit>();
        health = GetComponent<Health>();
        attack = GetComponent<AutoAttack>();
        status = GetComponent<StatusEffects>();
        caster = GetComponent<AbilityCaster>();
        mana = GetComponent<Mana>();
        if (health != null) health.OnDeath += OnDied;
        if (playerControlled) Player = this;
    }

    void OnDestroy() { if (Player == this) Player = null; }

    void Update()
    {
        if (!unit.IsAlive) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)) return;
        if (status != null && status.IsStunned) return;
        if (!playerControlled || MatchSetup.InLobby || IsBusyChanneling) return;

        var mouse = Mouse.current;
        if (!TouchInput.UsingTouch && mouse != null && mouse.rightButton.wasPressedThisFrame && !IsPointerOverUI() && !HudArea.IsOver(mouse.position.ReadValue()))
            HandleRightClick(mouse.position.ReadValue());

        var kb = Keyboard.current;
        if (kb != null && kb.sKey.wasPressedThisFrame) Stop();
    }

    // Приказ по точке экрана (ПКМ мышью или тап пальцем): враг — атаковать, земля — идти.
    public void CommandAt(Vector2 screenPos)
    {
        if (!unit.IsAlive || (status != null && status.IsStunned)) return;
        HandleRightClick(screenPos);
    }

    void HandleRightClick(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f)) return;
        if (caster != null) caster.CancelPending(); // новый приказ отменяет подход для способности

        // Кликнули по врагу — атакуем.
        var target = hit.collider.GetComponentInParent<Unit>();
        if (target != null && target != unit && target.IsAlive && unit.IsEnemyOf(target))
        {
            if (Net.IsClient) NetCmd.Attack(target); else attack.SetTarget(target);
            Fx.ClickMarker(target.transform.position, new Color(1f, 0.3f, 0.25f, 0.95f));   // красная метка — атака
            return;
        }

        // Свой крип: добить можно, когда у него меньше половины здоровья (денай)
        // (если здоровья больше половины — просто идём туда, как в Доте)
        if (target != null && target != unit && target.IsAlive && target.kind == UnitKind.Creep && target.team == unit.team && target.GetComponent<Courier>() == null
            && target.Health != null && target.Health.Current < target.Health.maxHealth * 0.5f)
        {
            if (Net.IsClient) NetCmd.Attack(target); else attack.SetTarget(target);
            return;
        }

        // Иначе — идём в точку.
        if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
        {
            if (Net.IsClient) { NetCmd.Move(navHit.position); Fx.ClickMarker(navHit.position, new Color(0.45f, 1f, 0.5f, 0.95f)); return; }
            agent.isStopped = false;
            agent.SetDestination(navHit.position);
            attack.OrderMove();
            Fx.ClickMarker(navHit.position, new Color(0.45f, 1f, 0.5f, 0.95f));   // зелёная метка — идти
        }
    }

    // Приказ идти в точку (например, ПКМ по мини-карте).
    public void MoveTo(Vector3 world)
    {
        if (!unit.IsAlive || (status != null && status.IsStunned)) return;
        if (!NavMesh.SamplePosition(world, out NavMeshHit navHit, 6f, NavMesh.AllAreas)) return;
        if (Net.IsClient) { NetCmd.Move(navHit.position); return; }
        if (caster != null) caster.CancelPending();
        agent.isStopped = false;
        agent.SetDestination(navHit.position);
        attack.OrderMove();
    }

    public void NetSetRespawn(float at) => RespawnAt = at;

    public void Stop()
    {
        if (Net.IsClient) { NetCmd.Stop(); return; }
        attack.ClearTarget();
        agent.isStopped = true;
        agent.ResetPath();
    }

    void OnDied(Unit killer)
    {
        OnHeroDied?.Invoke(killer);
        // билет на электричку кончился — покупается сам
        var inv = GetComponent<Inventory>();
        if (inv != null) inv.AutoBuyTicket(GetComponent<HeroProgress>());
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        Stop();
        if (caster != null) caster.CancelPending();
        IsBusyChanneling = false;
        yield return new WaitForSeconds(0.25f);
        SetVisible(false);
        // Время воскрешения растёт с уровнем
        var stats = GetComponent<HeroStats>();
        float wait = respawnTime + (stats != null ? stats.level * 0.6f : 0f);
        RespawnAt = Time.time + wait + 0.25f;
        yield return new WaitForSeconds(wait);

        Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
        agent.Warp(pos);
        health.Revive();
        if (mana != null) mana.Refill();
        if (status != null) status.ClearAll();
        SetVisible(true);
        agent.isStopped = false;
        if (FogOfWar.VisibleToPlayer(unit))
        {
            // воскрешение: столб света цвета команды
            var tc = Color.Lerp(TeamUtil.GetColor(unit.team), Color.white, 0.3f);
            Fx.Pillar(pos, tc, 8f, 1f, 1.4f);
            Fx.Shockwave(pos, tc, 3f, 0.6f);
            Fx.Glints(pos, tc, 14, 1f, 2.6f, 1.5f);
        }
    }

    void SetVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = visible;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = visible;
    }

    static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
