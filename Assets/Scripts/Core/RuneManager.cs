using System.Collections.Generic;
using UnityEngine;

// Руны на реке (как в Доте): с 2:00 и каждые 2 минуты в одной из двух точек реки появляется руна.
// Подобрать — просто подойти. Двойной урон, Скорость, Невидимость, Регенерация.
public class RuneManager : MonoBehaviour
{
    public enum Kind { DoubleDamage, Haste, Invisibility, Regeneration }
    public const float Interval = 120f, PickRadius = 1.8f;

    public class Rune { public Kind kind; public Vector3 pos; public GameObject go; }
    public static readonly List<Rune> Active = new List<Rune>();
    public static RuneManager Instance { get; private set; }

    // точки на реке: выше и ниже мида
    static Vector3[] Spots => new[] { LaneData.W(-18, 18), LaneData.W(18, -18) };

    float nextAt = Interval;

    void Awake() { Instance = this; Active.Clear(); }
    void OnDestroy() { Active.Clear(); }

    public static string Name(Kind k) => k switch
    {
        Kind.DoubleDamage => "Руна двойного урона",
        Kind.Haste => "Руна скорости",
        Kind.Invisibility => "Руна невидимости",
        _ => "Руна регенерации"
    };
    public static string Icon(Kind k) => k switch
    {
        Kind.DoubleDamage => "rune_dd", Kind.Haste => "rune_haste", Kind.Invisibility => "rune_invis", _ => "rune_regen"
    };
    public static Color Col(Kind k) => k switch
    {
        Kind.DoubleDamage => new Color(0.35f, 0.55f, 1f),
        Kind.Haste => new Color(1f, 0.25f, 0.2f),
        Kind.Invisibility => new Color(0.75f, 0.55f, 1f),
        _ => new Color(0.35f, 1f, 0.4f)
    };

    void Update()
    {
        if (MatchSetup.InLobby) return;
        float t = MatchSetup.MatchTime;
        if (t >= nextAt)
        {
            nextAt += Interval;
            // старая руна пропадает, новая — в случайной из двух точек
            foreach (var r in Active) if (r.go != null) Destroy(r.go);
            Active.Clear();
            var spot = Spots[Random.Range(0, 2)];
            Spawn((Kind)Random.Range(0, 4), spot);
        }

        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var r = Active[i];
            foreach (var u in Unit.All)
            {
                if (!u.IsAlive || u.kind != UnitKind.Hero || u.team == Team.Neutral) continue;
                Vector3 d = u.transform.position - r.pos; d.y = 0f;
                if (d.magnitude > PickRadius) continue;
                Pick(u, r.kind);
                if (r.go != null) Destroy(r.go);
                Active.RemoveAt(i);
                break;
            }
        }
    }

    void Spawn(Kind k, Vector3 p)
    {
        var root = new GameObject("Rune_" + k);
        root.transform.position = p;
        var c = Col(k);
        var spin = new GameObject("Spin").transform;
        spin.SetParent(root.transform, false);
        spin.localPosition = Vector3.up * 1.1f;
        var s = spin.gameObject.AddComponent<SpinFx>(); s.speed = 120f; s.bob = 0.18f; s.bobSpeed = 2f;
        // светящийся кристалл-«ромб» и кольцо вокруг
        var gem = RuntimeMats.Prim(PrimitiveType.Cube, spin, Vector3.zero, new Vector3(0.55f, 0.55f, 0.55f), RuntimeMats.Glow(c));
        gem.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        RuntimeMats.Prim(PrimitiveType.Sphere, spin, Vector3.zero, Vector3.one * 0.35f, RuntimeMats.Glow(Color.Lerp(c, Color.white, 0.6f)));
        var halo = RuntimeMats.Prim(PrimitiveType.Sphere, spin, Vector3.zero, Vector3.one * 1.3f, Fx.Soft);
        var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Color", new Color(c.r, c.g, c.b, 0.25f));
        halo.GetComponent<Renderer>().SetPropertyBlock(mpb);
        var ring = MaxFx.Line("RuneRing", new Color(c.r, c.g, c.b, 0.8f), 0.08f, true);
        ring.transform.SetParent(root.transform, true);
        MaxFx.Circle(ring, p + Vector3.up * 0.1f, 1.2f, 32);
        var fx = root.AddComponent<AmbientFx>();
        fx.kind = AmbientFx.Kind.Sparkle; fx.color = c; fx.interval = 0.3f; fx.radius = 0.6f; fx.count = 1;
        Fx.Pillar(p, c, 5f, 0.6f, 1f);
        Active.Add(new Rune { kind = k, pos = p, go = root });
    }

    static void Pick(Unit u, Kind k)
    {
        var st = u.GetComponent<StatusEffects>();
        var c = Col(k);
        string icon = Icon(k), name = Name(k);
        switch (k)
        {
            case Kind.DoubleDamage:
                RuneBuff.For(u.gameObject).DoubleDamage(45f);
                if (st != null) st.AddAttackSpeed(0f, 45f, name, icon);
                break;
            case Kind.Haste:
                if (st != null) st.AddMoveSpeed(100f, 22f, name, icon);
                break;
            case Kind.Invisibility:
                if (st != null) st.SetInvisible(45f, name, icon);
                RuneBuff.For(u.gameObject).WatchInvis(45f);
                break;
            case Kind.Regeneration:
                var h = u.Health; var m = u.GetComponent<Mana>();
                if (st != null) st.AddHealOverTime(h.maxHealth * 0.06f, m != null ? m.maxMana * 0.06f : 0f, 30f, name, icon);
                break;
        }
        Fx.Burst(u.transform.position, c, 3f);
        if (FogOfWar.VisibleToPlayer(u) || u.team == MatchSetup.PlayerTeam)
            FloatingText.Show(u.transform.position + Vector3.up * 4.2f, name.ToUpper() + "!", c, 26);
    }

    // Ближайшая руна (для ботов)
    public static Rune Nearest(Vector3 p, float max)
    {
        Rune best = null; float bd = max;
        foreach (var r in Active)
        {
            Vector3 d = r.pos - p; d.y = 0f;
            if (d.magnitude < bd) { bd = d.magnitude; best = r; }
        }
        return best;
    }
}

// Действие рун, которым нужно время: двойной урон и «невидимость до первого удара»
public class RuneBuff : MonoBehaviour
{
    float ddUntil, ddBonus, invisUntil;
    HeroStats stats;
    Unit unit;

    public static RuneBuff For(GameObject go) { var b = go.GetComponent<RuneBuff>(); return b != null ? b : go.AddComponent<RuneBuff>(); }

    void Awake()
    {
        stats = GetComponent<HeroStats>();
        unit = GetComponent<Unit>();
        Health.AnyDamaged += OnAnyDamaged;
    }
    void OnDestroy() => Health.AnyDamaged -= OnAnyDamaged;

    public void DoubleDamage(float dur)
    {
        EndDD();
        var at = GetComponent<AutoAttack>();
        ddBonus = at != null ? at.damage : 0f;
        ddUntil = Time.time + dur;
        if (stats != null) { stats.BonusDamage += ddBonus; stats.Recalculate(false); }
    }

    void EndDD()
    {
        if (ddBonus > 0f && stats != null) { stats.BonusDamage -= ddBonus; stats.Recalculate(false); }
        ddBonus = 0f;
    }

    public void WatchInvis(float dur) => invisUntil = Time.time + dur;

    // ударил кого-то — руна невидимости спадает
    void OnAnyDamaged(Health h, DamageInfo info, float dmg)
    {
        if (info.source != unit || Time.time > invisUntil) return;
        invisUntil = 0f;
        var st = GetComponent<StatusEffects>();
        if (st != null) st.BreakInvisible();
    }

    void Update()
    {
        if (ddBonus > 0f && (Time.time > ddUntil || (unit != null && !unit.IsAlive))) EndDD();
    }
}
