using System.Collections.Generic;
using UnityEngine;

// Всё «постоянное» у Пивмавера: метка Q (кто умрёт — из того вылезут бутылочки), пивная пена,
// пассивка Похмельный укус, ульта Пивной голод и голос (Resources/Voice/pivmaver).
[RequireComponent(typeof(Unit))]
public class PivmaverKit : MonoBehaviour
{
    public const string HeroId = "pivmaver";
    public const int MaxFoams = 2;
    public const int DeathLineIndex = 5;      // line6 — последнее голосовое
    public static readonly Color Amber = new Color(0.95f, 0.62f, 0.12f);
    public static readonly Color Foam = new Color(1f, 0.96f, 0.82f);

    Unit unit;
    Health health;
    AbilityCaster caster;
    HeroStats stats;
    StatusEffects status;
    bool wasAlive = true;

    // Q: метки «умрёт — родит бутылочки»
    class Mark { public float until; public int count; public float hp, dmg; }
    readonly Dictionary<Unit, Mark> marks = new Dictionary<Unit, Mark>();

    // W: пивная пена
    class FoamPool { public Vector3 c; public float r, until, speed, regen; public GameObject fx; }
    readonly List<FoamPool> foams = new List<FoamPool>();
    float foamTick;

    // R: Пивной голод
    float hungerUntil, hungerBonus, lifesteal;
    float hungerFx;

    public readonly List<BeerBottle> Bottles = new List<BeerBottle>();
    public bool Hungry => Time.time < hungerUntil;

    void Awake()
    {
        unit = GetComponent<Unit>();
        health = GetComponent<Health>();
        caster = GetComponent<AbilityCaster>();
        stats = GetComponent<HeroStats>();
        status = GetComponent<StatusEffects>();
        var hc = GetComponent<HeroController>();
        if (hc != null) hc.OnHeroDied += OnDied;
        Health.AnyDeath += OnAnyDeath;
        Health.AnyDamaged += OnAnyDamaged;
    }

    void OnDestroy()
    {
        Health.AnyDeath -= OnAnyDeath;
        Health.AnyDamaged -= OnAnyDamaged;
        foreach (var f in foams) if (f.fx != null) Destroy(f.fx);
    }

    Ability Ab(int i) => caster != null && caster.Abilities != null && i < caster.Abilities.Length ? caster.Abilities[i] : null;

    public void Say(float chance = 1f, bool force = false) => VoiceLines.Speak(HeroId, transform.position, chance, force, -1, DeathLineIndex);

    // ---------- Q ----------
    public void MarkTarget(Unit target, float duration, int count, float hp, float dmg)
    {
        if (target == null) return;
        marks[target] = new Mark { until = Time.time + duration, count = count, hp = hp, dmg = dmg };
    }

    void OnAnyDeath(Health h, Unit killer)
    {
        var u = h.GetComponent<Unit>();
        if (u == null || !marks.TryGetValue(u, out var m)) return;
        marks.Remove(u);
        if (Time.time > m.until) return;
        Vector3 at = u.transform.position;
        for (int i = 0; i < m.count; i++)
        {
            Vector2 off = Random.insideUnitCircle * 1.2f;
            BeerBottle.Spawn(this, at + new Vector3(off.x, 0, off.y), m.hp, m.dmg);
        }
        ParticleBurst.Spawn(at + Vector3.up * 1f, Amber, 18, 5f);
        if (FogOfWar.VisibleToPlayer(u))
            FloatingText.Show(at + Vector3.up * 3f, $"+{m.count} бутылочки!", Amber, 22);
    }

    // ---------- W ----------
    public void AddFoam(Vector3 c, float r, float speed, float regen, float duration)
    {
        c.y = 0f;
        if (foams.Count >= MaxFoams) { if (foams[0].fx != null) Destroy(foams[0].fx); foams.RemoveAt(0); }
        foams.Add(new FoamPool { c = c, r = r, until = Time.time + duration, speed = speed, regen = regen, fx = BuildFoam(c, r) });
        FogOfWar.AddVision(unit.team, c, r, duration);
    }

    FoamPool FoamAt(Vector3 p)
    {
        foreach (var f in foams)
        {
            float dx = p.x - f.c.x, dz = p.z - f.c.z;
            if (dx * dx + dz * dz <= f.r * f.r) return f;
        }
        return null;
    }
    public bool InFoam(Vector3 p, out float speed, out float regen)
    {
        var f = FoamAt(p);
        speed = f != null ? f.speed : 0f; regen = f != null ? f.regen : 0f;
        return f != null;
    }

    GameObject BuildFoam(Vector3 c, float r)
    {
        var root = new GameObject("BeerFoam");
        root.transform.position = c;
        var mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 0.93f, 0.7f, 0.33f) };
        var disc = RuntimeMats.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.05f, 0), new Vector3(r * 2f, 0.01f, r * 2f), mat);
        disc.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var ring = MaxFx.Line("FoamRing", new Color(1f, 0.85f, 0.45f, 0.8f), 0.1f, true);
        ring.transform.SetParent(root.transform, true);
        MaxFx.Circle(ring, c + Vector3.up * 0.1f, r);
        // нити пены, как паутина
        for (int i = 0; i < 8; i++)
        {
            var line = MaxFx.Line("FoamThread", new Color(1f, 0.97f, 0.85f, 0.45f), 0.05f, false);
            line.transform.SetParent(root.transform, true);
            line.positionCount = 2;
            float a = i * Mathf.PI / 4f;
            line.SetPosition(0, c + Vector3.up * 0.1f);
            line.SetPosition(1, c + new Vector3(Mathf.Cos(a) * r, 0.1f, Mathf.Sin(a) * r));
        }
        var bubble = RuntimeMats.Lit(Foam);
        for (int i = 0; i < 14; i++)
        {
            Vector2 p = Random.insideUnitCircle * r * 0.9f;
            float s = Random.Range(0.25f, 0.6f);
            RuntimeMats.Prim(PrimitiveType.Sphere, root.transform, new Vector3(p.x, 0.08f, p.y), new Vector3(s, s * 0.5f, s), bubble);
        }
        return root;
    }

    // ---------- R ----------
    public void StartHunger(float bonusDamage, float lifestealPct, float duration)
    {
        EndHunger();
        hungerBonus = bonusDamage;
        lifesteal = lifestealPct;
        hungerUntil = Time.time + duration;
        if (stats != null) { stats.BonusDamage += hungerBonus; stats.Recalculate(false); }
        if (status != null) status.AddAttackSpeed(0f, duration, "Пивной голод", "pivo_hunger");
        FloatingText.Show(transform.position + Vector3.up * 4.6f, "ПИВНОЙ ГОЛОД!", new Color(1f, 0.35f, 0.2f), 30);
        ParticleBurst.Spawn(transform.position + Vector3.up * 2f, new Color(1f, 0.3f, 0.15f), 24, 6f);
    }

    void EndHunger()
    {
        if (hungerBonus > 0f && stats != null) { stats.BonusDamage -= hungerBonus; stats.Recalculate(false); }
        hungerBonus = 0f;
        hungerUntil = 0f;
    }

    // ---------- E и вампиризм R: всё по ударам героя ----------
    void OnAnyDamaged(Health h, DamageInfo info, float dmg)
    {
        if (info.source != unit || info.type != DamageType.Physical || h == health) return;
        var target = h.GetComponent<Unit>();
        if (target == null) return;

        if (Hungry && dmg > 0f && lifesteal > 0f)
        {
            health.Heal(dmg * lifesteal);
            if (Random.value < 0.35f) ParticleBurst.Spawn(transform.position + Vector3.up * 2f, new Color(1f, 0.25f, 0.2f), 4, 2f);
        }

        var e = Ab(2);
        if (e != null && e.IsLearned && target.kind != UnitKind.Structure && target.IsAlive)
        {
            var st = target.GetComponent<StatusEffects>();
            if (st != null) st.ApplySlow(e.Info.Value(0, e.Level), 2f, "Похмельный укус", "pivo_bite");
            // доп. урон — магией, чтобы не попасть сюда же второй раз
            h.TakeDamage(new DamageInfo { amount = e.Info.Value(1, e.Level), type = DamageType.Magical, source = unit });
        }
    }

    void OnDied(Unit killer)
    {
        EndHunger();
        VoiceLines.Speak(HeroId, transform.position, 1f, true, DeathLineIndex);
        var def = HeroCatalog.Get(HeroId);
        FloatingText.Show(transform.position + Vector3.up * 4.4f, def.deathLine, new Color(1f, 0.75f, 0.45f), 24);
    }

    void Update()
    {
        if (unit.IsAlive && !wasAlive) Say();          // фраза после воскрешения
        wasAlive = unit.IsAlive;

        if (hungerUntil > 0f && !Hungry) EndHunger();
        if (Hungry && Time.time >= hungerFx)
        {
            hungerFx = Time.time + 0.4f;
            if (FogOfWar.VisibleToPlayer(unit)) ParticleBurst.Spawn(transform.position + Vector3.up * 2.4f, new Color(1f, 0.2f, 0.12f), 3, 1.5f);
        }

        // старые метки и пена
        if (marks.Count > 0)
        {
            var dead = new List<Unit>();
            foreach (var kv in marks) if (kv.Key == null || Time.time > kv.Value.until) dead.Add(kv.Key);
            foreach (var k in dead) marks.Remove(k);
        }
        for (int i = foams.Count - 1; i >= 0; i--)
            if (Time.time > foams[i].until) { if (foams[i].fx != null) Destroy(foams[i].fx); foams.RemoveAt(i); }
        Bottles.RemoveAll(b => b == null);

        // в пене: быстрее и лечится
        if (!unit.IsAlive || status == null) return;
        if (InFoam(transform.position, out float sp, out float regen))
        {
            health.Heal(regen * Time.deltaTime);
            if (Time.time >= foamTick) { foamTick = Time.time + 0.25f; status.AddMoveSpeed(sp, 0.25f, "Пивная пена", "pivo_foam"); }
        }
    }
}
