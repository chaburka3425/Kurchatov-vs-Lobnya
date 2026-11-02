using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Работа предметов: активки (клавиши 1–6 или клик), аура «Мангала», телепорт в баню.
[RequireComponent(typeof(Inventory))]
public class ItemEffects : MonoBehaviour
{
    Inventory inv;
    Unit unit;
    StatusEffects status;
    NavMeshAgent agent;
    ItemStats cached;
    float burnTick;

    public bool IsTeleporting { get; private set; }
    public float TeleportProgress { get; private set; }
    public string TeleportLabel { get; private set; } = "Телепорт...";

    // Билет на электричку (T): выбрать, куда ехать — к своей башне, трону или в баню
    public const float TicketChannel = 3f, TicketCooldown = 40f;
    public bool TpTargeting { get; private set; }
    public bool CanTp => inv != null && inv.TpCharges > 0 && inv.TpCooldownLeft <= 0f && !IsTeleporting && unit.IsAlive;
    float tpPressedAt = -10f;

    static readonly Key[] Keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };

    void Awake()
    {
        inv = GetComponent<Inventory>();
        unit = GetComponent<Unit>();
        status = GetComponent<StatusEffects>();
        agent = GetComponent<NavMeshAgent>();
        inv.OnChanged += () => cached = inv.TotalStats();
        var h = GetComponent<Health>();
        if (h != null) h.OnDamaged += info => { if (info.source != null && info.source.kind == UnitKind.Hero && unit.IsEnemyOf(info.source)) lastHeroHit = Time.time; };
    }

    void Update()
    {
        inv.TickCooldowns(Time.deltaTime);
        if (!unit.IsAlive) return;

        var gm = GameManager.Instance;
        bool blocked = gm != null && (gm.IsPaused || gm.IsGameOver);

        var hc = GetComponent<HeroController>();
        if (hc == null || !hc.playerControlled || MatchSetup.InLobby) blocked = true;
        var kb = Keyboard.current;
        if (!blocked && kb != null)
        {
            for (int i = 0; i < Keys.Length; i++)
                if (kb[Keys[i]].wasPressedThisFrame) Use(i);
            if (kb.tKey.wasPressedThisFrame) PressTicket();
        }

        // выбор, куда ехать: ЛКМ по земле, ПКМ — отмена
        var mouse = Mouse.current;
        if (TpTargeting && !blocked && mouse != null && !TouchInput.UsingTouch)
        {
            Vector2 mp = mouse.position.ReadValue();
            if (mouse.rightButton.wasPressedThisFrame) TpTargeting = false;
            else if (mouse.leftButton.wasPressedThisFrame && !HudArea.IsOver(mp) && !MinimapGUI.IsOverMap(mp)) TicketAtScreen(mp);
        }

        // Жар «Мангала»
        if (cached.burnDps > 0f)
        {
            burnTick += Time.deltaTime;
            if (burnTick >= 0.5f)
            {
                burnTick -= 0.5f;
                foreach (var u in Unit.All)
                {
                    if (!u.IsAlive || !unit.IsEnemyOf(u) || u.kind == UnitKind.Structure) continue;
                    if (u.DistanceFrom(transform.position) > cached.burnRadius) continue;
                    u.Health.TakeDamage(new DamageInfo { amount = cached.burnDps * 0.5f, type = DamageType.Magical, source = unit });
                }
            }
        }
    }

    // ---------- Билет на электричку ----------
    public void PressTicket()
    {
        if (!unit.IsAlive) return;
        if (inv.TpCharges <= 0) { Say("Нет билета — купи в магазине (100)"); return; }
        if (inv.TpCooldownLeft > 0f) { Say($"Билет: ещё {Mathf.CeilToInt(inv.TpCooldownLeft)} сек."); return; }
        if (status != null && status.IsStunned) { Say("Оглушён!"); return; }
        if (IsTeleporting) return;
        // двойное нажатие — сразу в баню
        if (TpTargeting && Time.unscaledTime - tpPressedAt < 0.6f) { TpTargeting = false; TicketTo(BasePoint(), "Еду в баню..."); return; }
        TpTargeting = true;
        tpPressedAt = Time.unscaledTime;
        Say("Куда едем? Кликни у своей башни или на мини-карте", new Color(0.5f, 0.8f, 1f));
    }

    public void CancelTicket() => TpTargeting = false;

    public void TicketAtScreen(Vector2 screenPos)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var ray = cam.ScreenPointToRay(screenPos);
        if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d)) TicketAtWorld(ray.GetPoint(d));
    }

    // Ближайшая своя постройка к выбранной точке: башня, трон или баня
    public void TicketAtWorld(Vector3 point)
    {
        TpTargeting = false;
        if (!CanTp) { PressTicket(); TpTargeting = false; return; }
        Vector3 best = BasePoint(); string where = "в баню";
        float bd = Flat(point, best);
        foreach (var u in Unit.All)
        {
            if (u.kind != UnitKind.Structure || u.team != unit.team || !u.IsAlive) continue;
            float d = Flat(point, u.transform.position);
            if (d < bd) { bd = d; best = u.transform.position; where = u.GetComponent<Throne>() != null ? "к трону" : "к башне"; }
        }
        // встаём рядом с постройкой, со стороны клика (не дальше 4.5 м)
        Vector3 off = point - best; off.y = 0;
        if (off.sqrMagnitude < 9f) off = (BasePoint() - best).normalized * 3f;
        Vector3 dest = best + Vector3.ClampMagnitude(off, 4.5f);
        if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 5f, NavMesh.AllAreas)) dest = hit.position;
        TicketTo(dest, $"Еду {where}...");
    }

    public void TicketTo(Vector3 dest, string label)
    {
        if (!CanTp) return;
        inv.TpCharges--;
        inv.TpCooldownLeft = TicketCooldown;
        StartCoroutine(Teleport(TicketChannel, dest, label, true));
    }

    Vector3 BasePoint()
    {
        var bath = FindOwnBath();
        return bath != null ? bath.transform.position : transform.position;
    }

    static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

    // ---------- Самокат-блинк ----------
    public const float BlinkLockAfterHit = 3f;
    float lastHeroHit = -10f;
    Vector3? blinkTarget;
    public bool BlinkReady
    {
        get
        {
            int s = SlotOf(ItemActive.Blink);
            return s >= 0 && inv.Get(s).cooldownLeft <= 0f && Time.time - lastHeroHit >= BlinkLockAfterHit;
        }
    }

    public int SlotOf(ItemActive kind)
    {
        for (int i = 0; i < Inventory.MainSlots; i++) { var it = inv.Get(i); if (it != null && it.def.active == kind) return i; }
        return -1;
    }

    // Для бота: блинк в точку
    public bool BotBlink(Vector3 to)
    {
        int s = SlotOf(ItemActive.Blink);
        if (s < 0 || !BlinkReady) return false;
        blinkTarget = to;
        Use(s);
        return true;
    }

    // Для бота: использовать предмет такого типа, если он готов
    public bool BotUse(ItemActive kind)
    {
        int s = SlotOf(kind);
        if (s < 0 || inv.Get(s).cooldownLeft > 0f) return false;
        if (kind == ItemActive.Bottle && inv.Get(s).charges <= 0) return false;
        Use(s);
        return true;
    }

    Vector3? MouseWorld()
    {
        var mouse = Mouse.current; var cam = Camera.main;
        if (mouse == null || cam == null || TouchInput.UsingTouch) return null;
        var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d) ? ray.GetPoint(d) : (Vector3?)null;
    }

    bool DoBlink(Vector3 to, float range)
    {
        if (Time.time - lastHeroHit < BlinkLockAfterHit) { Say($"Самокат не заводится: тебя бьют ({Mathf.CeilToInt(BlinkLockAfterHit - (Time.time - lastHeroHit))} сек.)"); return false; }
        Vector3 from = transform.position;
        Vector3 dir = to - from; dir.y = 0f;
        if (dir.magnitude > range) to = from + dir.normalized * range;
        if (!NavMesh.SamplePosition(to, out NavMeshHit hit, 3f, NavMesh.AllAreas)) { Say("Туда не проехать"); return false; }
        var c = new Color(0.45f, 0.8f, 1f);
        Fx.Flash(from + Vector3.up * 1.3f, c, 2f, 0.25f);
        Fx.Glints(from, c, 12, 0.8f, 1.5f, 0.9f);
        var lr = MaxFx.Line("BlinkTrail", new Color(0.5f, 0.85f, 1f, 0.8f), 0.25f, false);
        lr.positionCount = 2; lr.SetPosition(0, from + Vector3.up * 0.6f); lr.SetPosition(1, hit.position + Vector3.up * 0.6f);
        lr.gameObject.AddComponent<FadeLine>().life = 0.35f;
        if (agent != null) { agent.Warp(hit.position); if (agent.isOnNavMesh) agent.ResetPath(); } else transform.position = hit.position;
        var att = GetComponent<AutoAttack>();
        if (att != null) att.OrderMove();
        Fx.Shockwave(hit.position, c, 2.5f, 0.4f);
        Fx.Flash(hit.position + Vector3.up * 1.3f, c, 1.6f, 0.25f);
        var rig = FindFirstObjectByType<RTSCamera>();
        if (rig != null && GetComponent<HeroController>() == HeroController.Player && Flat(hit.position, from) > 6f) rig.JumpTo(hit.position);
        return true;
    }

    public void Use(int slot)
    {
        var it = inv.Get(slot);
        if (it == null || slot >= Inventory.MainSlots) return;
        var d = it.def;
        if (d.active == ItemActive.None) { Say("У предмета нет активки"); return; }
        if (it.cooldownLeft > 0f) { Say($"Перезарядка: {Mathf.CeilToInt(it.cooldownLeft)} сек."); return; }
        if (status != null && status.IsStunned) { Say("Оглушён!"); return; }

        switch (d.active)
        {
            case ItemActive.HealOverTime:
                status.AddHealOverTime(d.activeValue, 0f, d.activeDuration, d.name, d.id);
                Say("Ням!", new Color(0.4f, 1f, 0.4f));
                break;
            case ItemActive.ManaOverTime:
                status.AddHealOverTime(0f, d.activeValue, d.activeDuration, d.name, d.id);
                Say("Хрусть!", new Color(0.5f, 0.7f, 1f));
                break;
            case ItemActive.SpeedBurst:
                status.AddMoveSpeed(d.activeValue, d.activeDuration, d.name, d.id);
                Say("Вжух!", new Color(1f, 0.8f, 0.3f));
                break;
            case ItemActive.MagicImmunity:
                status.AddMagicImmunity(d.activeDuration, d.name, d.id);
                Say("ЧЁРНЫЙ ПОЯС!", new Color(1f, 0.85f, 0.2f));
                break;
            case ItemActive.Blink:
            {
                // к курсору (у бота — в точку BlinkTarget), не дальше activeValue
                Vector3 to = blinkTarget ?? MouseWorld() ?? transform.position + transform.forward * d.activeValue;
                blinkTarget = null;
                if (!DoBlink(to, d.activeValue)) return;
                break;
            }
            case ItemActive.Bottle:
                if (it.charges <= 0) { Say("Бутылка пустая — наполни в бане"); return; }
                it.charges--;
                status.AddHealOverTime(d.activeValue / d.activeDuration, 60f / d.activeDuration, d.activeDuration, d.name, d.id);
                Say($"Глоток кваса! ({it.charges}/{d.maxCharges})", new Color(0.9f, 0.7f, 0.35f));
                inv.NotifyChanged();
                break;
            case ItemActive.RevealInvis:
            {
                int found = 0;
                foreach (var u in Unit.All)
                {
                    if (!u.IsAlive || !unit.IsEnemyOf(u) || u.kind == UnitKind.Structure) continue;
                    if (u.DistanceFrom(transform.position) > d.activeValue) continue;
                    var st = u.GetComponent<StatusEffects>();
                    if (st == null) continue;
                    if (st.IsInvisible || u.kind == UnitKind.Hero) found += st.IsInvisible ? 1 : 0;
                    st.Reveal(d.activeDuration, d.name, d.id);
                    st.ApplySlow(20f, d.activeDuration, d.name, d.id);
                }
                Fx.Shockwave(transform.position, new Color(0.9f, 0.35f, 0.15f), d.activeValue, 0.7f);
                Fx.Glints(transform.position, new Color(1f, 0.5f, 0.2f), 20, d.activeValue * 0.6f, 1.2f, 1.6f);
                Say(found > 0 ? $"Апчхи! Нашёл невидимых: {found}" : "Апчхи!", new Color(1f, 0.6f, 0.3f));
                break;
            }
            case ItemActive.TeleportBase:
                if (IsTeleporting) return;
                if (d.id == Inventory.TicketId)
                {
                    // старый билет из инвентаря — переносим в слот T
                    inv.Remove(slot);
                    inv.TpCharges = Mathf.Min(Inventory.MaxTickets, inv.TpCharges + 1);
                    PressTicket();
                    return;
                }
                StartCoroutine(Teleport(d.activeDuration, BasePoint(), "Еду в баню...", false));
                break;
        }

        it.cooldownLeft = d.activeCooldown;
        if (d.consumable) inv.Remove(slot);
        ParticleBurst.Spawn(transform.position + Vector3.up * 2f, d.color, 14);
    }

    IEnumerator Teleport(float channel, Vector3 dest, string label, bool ticket)
    {
        IsTeleporting = true;
        TeleportLabel = label;
        Say(label, new Color(0.5f, 0.8f, 1f));
        var attack = GetComponent<AutoAttack>();
        if (attack != null) attack.ClearTarget();
        if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }

        // голубой круг там, куда едем (его видят все)
        var ring = new GameObject("TicketRing").AddComponent<LineRenderer>();
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.startColor = ring.endColor = new Color(0.45f, 0.8f, 1f, 0.9f);
        ring.startWidth = ring.endWidth = 0.15f;
        ring.loop = true; ring.positionCount = 32;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = 0; i < 32; i++) { float a = i * Mathf.PI * 2f / 32f; ring.SetPosition(i, dest + new Vector3(Mathf.Cos(a) * 1.6f, 0.15f, Mathf.Sin(a) * 1.6f)); }
        float burst = 0f;

        Vector3 start = transform.position;
        float t = 0f;
        while (t < channel)
        {
            t += Time.deltaTime;
            TeleportProgress = t / channel;
            burst += Time.deltaTime;
            if (burst > 0.25f) { burst = 0f; ParticleBurst.Spawn(dest + Vector3.up * 0.5f, new Color(0.5f, 0.85f, 1f), 4, 3f); ParticleBurst.Spawn(transform.position + Vector3.up, new Color(0.5f, 0.85f, 1f), 3, 2f); }
            // Сдвинулся, оглушили или умер — телепорт сорван.
            if (!unit.IsAlive || (status != null && status.IsStunned) || (transform.position - start).sqrMagnitude > 0.5f)
            {
                Say("Телепорт сорван", new Color(1f, 0.5f, 0.5f));
                IsTeleporting = false;
                Destroy(ring.gameObject);
                yield break;
            }
            yield return null;
        }

        Destroy(ring.gameObject);
        Vector2 r = Random.insideUnitCircle * (ticket ? 0.8f : 2f);
        dest += new Vector3(r.x, 0f, r.y);
        if (agent != null) agent.Warp(dest); else transform.position = dest;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        ParticleBurst.Spawn(dest + Vector3.up, new Color(0.5f, 0.8f, 1f), 24);
        IsTeleporting = false;

        var rig = FindFirstObjectByType<RTSCamera>();
        if (rig != null && GetComponent<HeroController>() == HeroController.Player) rig.JumpTo(dest);
    }

    public BeerBath FindOwnBath()
    {
        foreach (var b in FindObjectsByType<BeerBath>(FindObjectsSortMode.None))
            if (b.team == unit.team) return b;
        return null;
    }

    void Say(string text) => Say(text, new Color(1f, 0.6f, 0.6f));
    void Say(string text, Color c) => FloatingText.Show(transform.position + Vector3.up * 3.8f, text, c, 22);
}
