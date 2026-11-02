using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Шашлык Ассасин (по мотивам Templar Assassin): Маринад-щит, Засада (невидимость + удар из дыма),
// Длинный шампур (удар насквозь), Охотничьи капканы и голос (Resources/Voice/shashlik, смерть — line6).
[RequireComponent(typeof(Unit))]
public class ShashlikKit : MonoBehaviour
{
    public const string HeroId = "shashlik";
    public const int DeathLineIndex = 5;
    public static readonly Color Violet = new Color(0.75f, 0.45f, 1f);
    public static readonly Color Pink = new Color(1f, 0.55f, 0.9f);
    public static readonly Color Ember = new Color(1f, 0.55f, 0.15f);

    Unit unit;
    Health health;
    AbilityCaster caster;
    HeroStats stats;
    StatusEffects status;
    bool wasAlive = true;

    Ability Ab(int i) => caster != null && caster.Abilities != null && i < caster.Abilities.Length ? caster.Abilities[i] : null;

    void Awake()
    {
        unit = GetComponent<Unit>();
        health = GetComponent<Health>();
        caster = GetComponent<AbilityCaster>();
        stats = GetComponent<HeroStats>();
        status = GetComponent<StatusEffects>();
        health.BlockHook = Block;
        Health.AnyDamaged += OnAnyDamaged;
        var hc = GetComponent<HeroController>();
        if (hc != null) hc.OnHeroDied += OnDied;
    }

    void OnDestroy()
    {
        Health.AnyDamaged -= OnAnyDamaged;
        foreach (var t in traps) if (t.go != null) Destroy(t.go);
    }

    public void Say(float chance = 1f, bool force = false) => VoiceLines.Speak(HeroId, transform.position, chance, force, -1, DeathLineIndex);

    // Длинный шампур: дальность атаки
    public float RangeBonus { get { var e = Ab(2); return e != null && e.IsLearned ? e.Info.Value(0, e.Level) : 0f; } }

    // ================= Q — Маринад =================
    int blockCharges, hitCharges;
    float marinadeUntil, marinadeBonus;
    Transform shieldFx;
    readonly List<GameObject> orbit = new List<GameObject>();

    public void StartMarinade(int charges, float bonus, float duration)
    {
        EndMarinadeBonus();
        blockCharges = hitCharges = charges;
        marinadeUntil = Time.time + duration;
        marinadeBonus = bonus;
        if (stats != null) { stats.BonusDamage += bonus; stats.Recalculate(false); }
        status.ShowShield(duration, "Маринад", "shash_marinade");
        BuildShieldFx(charges);
        ParticleBurst.Spawn(transform.position + Vector3.up * 1.6f, Pink, 22, 5f);
        Say(0.5f);
    }

    bool Block(DamageInfo info)
    {
        if (blockCharges <= 0 || Time.time > marinadeUntil || info.amount <= 5f || info.source == unit) return false;
        blockCharges--;
        FloatingText.Show(transform.position + Vector3.up * 3.6f, "Маринад!", Pink, 20);
        ParticleBurst.Spawn(transform.position + Vector3.up * 1.6f, Pink, 10, 4f);
        if (orbit.Count > 0) { Destroy(orbit[orbit.Count - 1]); orbit.RemoveAt(orbit.Count - 1); }
        if (blockCharges <= 0) { status.HideShield(); if (shieldFx != null) shieldFx.gameObject.SetActive(false); }
        return true;
    }

    void EndMarinadeBonus()
    {
        if (marinadeBonus > 0f && stats != null) { stats.BonusDamage -= marinadeBonus; stats.Recalculate(false); }
        marinadeBonus = 0f;
        hitCharges = 0;
    }

    // Розовый пузырь и кружащие шампуры (по одному на заряд)
    void BuildShieldFx(int charges)
    {
        if (shieldFx == null)
        {
            shieldFx = new GameObject("MarinadeFx").transform;
            shieldFx.SetParent(transform, false);
            var bubbleMat = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 0.55f, 0.9f, 0.16f) };
            var bubble = RuntimeMats.Prim(PrimitiveType.Sphere, shieldFx, new Vector3(0, 1.6f, 0), new Vector3(2.4f, 3.2f, 2.4f), bubbleMat);
            bubble.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var spin = new GameObject("Orbit").transform;
            spin.SetParent(shieldFx, false);
            spin.localPosition = new Vector3(0, 1.5f, 0);
            var fx = spin.gameObject.AddComponent<SpinFx>(); fx.speed = 160f; fx.bob = 0.15f;
        }
        shieldFx.gameObject.SetActive(true);
        foreach (var o in orbit) if (o != null) Destroy(o);
        orbit.Clear();
        var orbitT = shieldFx.Find("Orbit");
        for (int i = 0; i < charges; i++)
        {
            float a = i * Mathf.PI * 2f / charges;
            var s = BuildSkewer(orbitT, 0.7f);
            s.transform.localPosition = new Vector3(Mathf.Cos(a) * 1.3f, (i % 2) * 0.35f, Mathf.Sin(a) * 1.3f);
            s.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 75f);
            orbit.Add(s);
        }
    }

    // Маленький шампур с мясом и луком (для эффектов)
    public static GameObject BuildSkewer(Transform parent, float size)
    {
        var root = new GameObject("Skewer");
        root.transform.SetParent(parent, false);
        root.transform.localScale = Vector3.one * size;
        RuntimeMats.Prim(PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.04f, 0.55f, 0.04f), RuntimeMats.Glow(new Color(0.9f, 0.9f, 1f)));
        for (int i = 0; i < 4; i++)
        {
            bool meat = i % 2 == 0;
            var m = RuntimeMats.Prim(PrimitiveType.Sphere, root.transform, new Vector3(0, -0.3f + i * 0.2f, 0), meat ? new Vector3(0.2f, 0.16f, 0.2f) : new Vector3(0.18f, 0.08f, 0.18f),
                RuntimeMats.Lit(meat ? new Color(0.55f, 0.22f, 0.1f) : new Color(0.95f, 0.92f, 0.8f)));
            m.transform.localRotation = Quaternion.Euler(0, i * 40f, 0);
        }
        return root;
    }

    // ================= W — Засада =================
    bool melded;
    Vector3 meldPos;
    float meldDamage, meldArmor, smokeAt;

    public void StartAmbush(float bonus, float armor)
    {
        melded = true;
        meldPos = transform.position;
        meldDamage = bonus; meldArmor = armor;
        status.SetInvisible(10f, "Засада", "shash_ambush");
        var ag = GetComponent<NavMeshAgent>();
        if (ag != null && ag.isOnNavMesh) ag.ResetPath();
        Smoke(transform.position, 26);
        Say(0.4f);
    }

    void EndAmbush()
    {
        melded = false;
        status.BreakInvisible();
    }

    void Smoke(Vector3 p, int n)
    {
        ParticleBurst.Spawn(p + Vector3.up * 0.6f, new Color(0.55f, 0.35f, 0.75f), n, 3f);
        ParticleBurst.Spawn(p + Vector3.up * 1.2f, new Color(0.35f, 0.32f, 0.38f), n / 2, 2f);
    }

    // ================= Удары: маринад, засада, шампур насквозь =================
    void OnAnyDamaged(Health h, DamageInfo info, float dmg)
    {
        if (info.source != unit || info.type != DamageType.Physical || h == health) return;
        var target = h.GetComponent<Unit>();
        if (target == null) return;

        // удар маринованным шампуром
        if (hitCharges > 0 && Time.time <= marinadeUntil && --hitCharges <= 0) EndMarinadeBonus();

        // удар из засады
        if (melded)
        {
            EndAmbush();
            h.TakeDamage(new DamageInfo { amount = meldDamage, type = DamageType.Pure, source = unit });
            var st = target.GetComponent<StatusEffects>();
            if (st != null && target.kind != UnitKind.Structure) st.AddArmorReduction(meldArmor, 6f, "Засада", "shash_ambush");
            FloatingText.Show(target.transform.position + Vector3.up * 3.4f, "ИЗ ЗАСАДЫ!", Violet, 26);
            ParticleBurst.Spawn(target.transform.position + Vector3.up * 1.4f, Violet, 22, 6f);
            ParticleBurst.Spawn(target.transform.position + Vector3.up * 1.4f, Ember, 10, 4f);
        }

        // Длинный шампур: нанизывает врагов за целью
        var e = Ab(2);
        if (e != null && e.IsLearned)
        {
            float len = e.Info.Value(1, e.Level);
            Vector3 from = transform.position, dir = target.transform.position - from; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            dir.Normalize();
            float start = Vector3.Dot(target.transform.position - from, dir);
            bool any = false;
            foreach (var u in new List<Unit>(Unit.All))
            {
                if (u == target || !u.IsAlive || !unit.IsEnemyOf(u) || u.kind == UnitKind.Structure) continue;
                Vector3 d = u.transform.position - from; d.y = 0f;
                float along = Vector3.Dot(d, dir);
                if (along < start || along > start + len) continue;
                if ((d - dir * along).magnitude > 0.9f) continue;
                u.Health.TakeDamage(new DamageInfo { amount = info.amount, type = DamageType.Pure, source = unit });
                ParticleBurst.Spawn(u.transform.position + Vector3.up * 1.2f, Violet, 6, 3f);
                any = true;
            }
            if (any || FogOfWar.VisibleToPlayer(unit)) SkewerLine(from + Vector3.up * 1.4f, from + Vector3.up * 1.4f + dir * (start + (any ? len : 0.5f)));
        }
    }

    // Светящаяся линия удара шампуром насквозь (гаснет за 0.25 сек.)
    static void SkewerLine(Vector3 a, Vector3 b)
    {
        var lr = MaxFx.Line("SkewerLine", new Color(0.85f, 0.6f, 1f, 0.9f), 0.12f, false);
        lr.positionCount = 2;
        lr.SetPosition(0, a); lr.SetPosition(1, b);
        lr.endColor = new Color(1f, 0.6f, 0.2f, 0.9f);
        lr.gameObject.AddComponent<FadeLine>().life = 0.25f;
    }

    // ================= R — Охотничий капкан =================
    class Trap { public GameObject go; public Vector3 pos; public float dmg, visionAt; public Transform jawA, jawB; public bool sprung; public float springT; }
    readonly List<Trap> traps = new List<Trap>();

    public void PlaceTrap(Vector3 p, int max, float dmg)
    {
        p.y = 0f;
        if (traps.Count >= max) { var old = traps[0]; if (old.go != null) Destroy(old.go); traps.RemoveAt(0); }
        var t = new Trap { pos = p, dmg = dmg };
        t.go = BuildTrap(p, out t.jawA, out t.jawB);
        traps.Add(t);
        ParticleBurst.Spawn(p + Vector3.up * 0.3f, Violet, 12, 3f);
        Say(0.3f);
    }

    GameObject BuildTrap(Vector3 p, out Transform jawA, out Transform jawB)
    {
        var root = new GameObject("HunterTrap");
        root.transform.position = p;
        var steel = RuntimeMats.Lit(new Color(0.55f, 0.55f, 0.6f));
        var tooth = RuntimeMats.Lit(new Color(0.85f, 0.85f, 0.9f));
        // фиолетовый круг охотницы
        var ring = MaxFx.Line("TrapRing", new Color(0.75f, 0.45f, 1f, 0.75f), 0.07f, true);
        ring.transform.SetParent(root.transform, true);
        MaxFx.Circle(ring, p + Vector3.up * 0.06f, 1.6f, 32);
        var glowMat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.7f, 0.4f, 1f, 0.18f) };
        RuntimeMats.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.03f, 0), new Vector3(3.2f, 0.005f, 3.2f), glowMat);
        // основа, пружины и кусочек шашлыка-приманки
        RuntimeMats.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.06f, 0), new Vector3(0.9f, 0.03f, 0.9f), steel);
        RuntimeMats.Prim(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.12f, 0), new Vector3(0.26f, 0.18f, 0.22f), RuntimeMats.Lit(new Color(0.55f, 0.22f, 0.1f)));
        // две челюсти с зубьями — открыты (лежат), при срабатывании захлопываются
        jawA = Jaw(root.transform, 1f, steel, tooth);
        jawB = Jaw(root.transform, -1f, steel, tooth);
        // враги капкан не видят
        if (unit.team != MatchSetup.PlayerTeam) foreach (var r in root.GetComponentsInChildren<Renderer>()) r.enabled = false;
        return root;
    }

    static Transform Jaw(Transform parent, float side, Material steel, Material tooth)
    {
        var hinge = new GameObject("Jaw").transform;
        hinge.SetParent(parent, false);
        hinge.localPosition = new Vector3(0, 0.08f, 0);
        hinge.localRotation = Quaternion.Euler(0, 0, side > 0 ? -85f : 85f);   // лежит раскрытой
        var arc = RuntimeMats.Prim(PrimitiveType.Cylinder, hinge, new Vector3(0, 0.38f, 0), new Vector3(0.75f, 0.02f, 0.75f), steel);
        arc.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        for (int i = 0; i < 6; i++)
        {
            float z = Mathf.Lerp(-0.3f, 0.3f, i / 5f);
            var t = RuntimeMats.Prim(PrimitiveType.Cube, hinge, new Vector3(0, 0.62f, z), new Vector3(0.04f, 0.12f, 0.04f), tooth);
            t.transform.localRotation = Quaternion.Euler(45, 0, 0);
        }
        return hinge;
    }

    void UpdateTraps()
    {
        for (int i = traps.Count - 1; i >= 0; i--)
        {
            var t = traps[i];
            if (t.go == null) { traps.RemoveAt(i); continue; }
            if (t.sprung)
            {
                // челюсти захлопываются, потом капкан исчезает
                t.springT += Time.deltaTime;
                float k = Mathf.Clamp01(t.springT / 0.12f);
                t.jawA.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-85f, -8f, k));
                t.jawB.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(85f, 8f, k));
                if (t.springT > 1.2f) { Destroy(t.go); traps.RemoveAt(i); }
                continue;
            }
            if (Time.time >= t.visionAt) { t.visionAt = Time.time + 1f; FogOfWar.AddVision(unit.team, t.pos, 6f, 1.2f); }
            foreach (var u in Unit.All)
            {
                if (!u.IsAlive || u.kind != UnitKind.Hero || !unit.IsEnemyOf(u) || u.team == Team.Neutral) continue;
                Vector3 d = u.transform.position - t.pos; d.y = 0f;
                if (d.magnitude > 1.6f) continue;
                Spring(t, u);
                break;
            }
        }
    }

    void Spring(Trap t, Unit victim)
    {
        t.sprung = true;
        foreach (var r in t.go.GetComponentsInChildren<Renderer>()) r.enabled = true;   // теперь видно всем
        victim.Health.TakeDamage(new DamageInfo { amount = t.dmg, type = DamageType.Magical, source = unit });
        var st = victim.GetComponent<StatusEffects>();
        if (st != null) st.ApplySlow(50f, 3f, "Охотничий капкан", "shash_trap");
        FogOfWar.Reveal(unit.team, victim, 3f);
        FloatingText.Show(victim.transform.position + Vector3.up * 3.6f, "КАПКАН!", Violet, 28);
        ParticleBurst.Spawn(t.pos + Vector3.up * 0.5f, Violet, 26, 6f);
        ParticleBurst.Spawn(t.pos + Vector3.up * 0.5f, Ember, 12, 4f);
    }

    // ================= Общее =================
    void OnDied(Unit killer)
    {
        EndMarinadeBonus();
        blockCharges = 0;
        if (shieldFx != null) shieldFx.gameObject.SetActive(false);
        EndAmbush();
        VoiceLines.Speak(HeroId, transform.position, 1f, true, DeathLineIndex);
        var def = HeroCatalog.Get(HeroId);
        FloatingText.Show(transform.position + Vector3.up * 4.4f, def.deathLine, Pink, 24);
    }

    float appliedRange = -1f;

    void Update()
    {
        if (unit.IsAlive && !wasAlive) Say();
        wasAlive = unit.IsAlive;

        // выучила Длинный шампур — пересчитать дальность атаки
        float rb = RangeBonus;
        if (!Mathf.Approximately(rb, appliedRange) && stats != null)
        {
            appliedRange = rb;
            stats.Recalculate(false);
            var at = GetComponent<AutoAttack>();
            if (at != null) { at.acquireRange = Mathf.Max(5f, at.range + 0.8f); at.loseRange = Mathf.Max(12f, at.range + 5f); }
        }

        if (marinadeUntil > 0f && Time.time > marinadeUntil)
        {
            marinadeUntil = 0f;
            EndMarinadeBonus();
            blockCharges = 0;
            status.HideShield();
            if (shieldFx != null) shieldFx.gameObject.SetActive(false);
        }

        if (melded)
        {
            Vector3 d = transform.position - meldPos; d.y = 0f;
            if (!status.IsInvisible || d.magnitude > 0.35f || !unit.IsAlive) { EndAmbush(); Smoke(transform.position, 10); }
            else if (Time.time >= smokeAt && FogOfWar.VisibleToPlayer(unit))
            {
                smokeAt = Time.time + 0.35f;   // свои видят дымок вокруг охотницы
                ParticleBurst.Spawn(transform.position + Vector3.up * Random.Range(0.4f, 2.4f), new Color(0.6f, 0.4f, 0.85f), 3, 1.2f);
            }
        }

        UpdateTraps();
    }
}

// Линия, которая плавно гаснет и удаляется
public class FadeLine : MonoBehaviour
{
    public float life = 0.3f;
    float t;
    LineRenderer lr;
    Color a, b;
    void Start() { lr = GetComponent<LineRenderer>(); a = lr.startColor; b = lr.endColor; }
    void Update()
    {
        t += Time.deltaTime;
        float k = 1f - t / life;
        if (k <= 0f) { Destroy(gameObject); return; }
        lr.startColor = new Color(a.r, a.g, a.b, a.a * k);
        lr.endColor = new Color(b.r, b.g, b.b, b.a * k);
        lr.widthMultiplier = 0.4f + 0.6f * k;
    }
}
