using System.Collections.Generic;
using UnityEngine;

// Туман войны: каждая команда видит только то, что рядом с её героями, крипами, башнями и троном.
// Враги в тумане не рисуются, по ним нельзя кликнуть, боты их тоже «не видят».
// Строения видны всегда (как в Доте). Тёмный слой лежит на земле, на мини-карте — тоже туман.
public class FogOfWar : MonoBehaviour
{
    public const float HeroVision = 12f, CreepVision = 8f, TowerVision = 11f, ThroneVision = 14f;
    const int N = 200;
    const float WorldMin = -LaneData.Half, WorldSize = LaneData.Half * 2f;

    static readonly HashSet<Unit>[] visible = { new HashSet<Unit>(), new HashSet<Unit>(), new HashSet<Unit>() };
    static readonly Dictionary<Unit, bool> shown = new Dictionary<Unit, bool>();
    public static Texture2D Texture { get; private set; }
    public static bool Active { get; private set; }

    Color32[] pixels;
    float next;
    GameObject plane;

    struct Src { public Vector3 p; public float r; }

    // Временное зрение (Золотой дождь) и подсветка цели (Хантерас пик)
    struct Temp { public Team team; public Vector3 p; public float r, until; public Unit reveal; }
    static readonly List<Temp> temps = new List<Temp>();
    public static void AddVision(Team team, Vector3 p, float radius, float duration) =>
        temps.Add(new Temp { team = team, p = p, r = radius, until = Time.time + duration });
    public static void Reveal(Team team, Unit u, float duration) =>
        temps.Add(new Temp { team = team, reveal = u, until = Time.time + duration });

    void Awake()
    {
        Active = true;
        for (int i = 0; i < 3; i++) visible[i].Clear();
        shown.Clear();
        temps.Clear();
        Texture = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        pixels = new Color32[N * N];

        // Тёмный слой над землёй
        plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = "FogPlane";
        Destroy(plane.GetComponent<Collider>());
        plane.transform.position = new Vector3(0, 0.12f, 0);
        plane.transform.rotation = Quaternion.Euler(90, 0, 0);
        plane.transform.localScale = new Vector3(WorldSize, WorldSize, 1);
        var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));
        mat.mainTexture = Texture;
        mat.renderQueue = 3000;
        plane.GetComponent<Renderer>().sharedMaterial = mat;
        plane.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void OnDestroy() => Active = false;

    // Видит ли команда этот юнит
    public static bool CanSee(Team team, Unit u)
    {
        if (u == null || !Active) return true;
        if (u.team == team || u.kind == UnitKind.Structure || team == Team.Neutral) return true;
        if (u.IsInvisibleNow) return false;   // в засаде (невидимость)
        return visible[(int)team].Contains(u);
    }

    public static bool VisibleToPlayer(Unit u) => CanSee(MatchSetup.PlayerTeam, u);

    // ночью обзор героев и крипов меньше (DayNight)
    static float VisionOf(Unit u) => u.kind switch
    {
        UnitKind.Hero => HeroVision * DayNight.VisionMult,
        UnitKind.Creep => CreepVision * DayNight.VisionMult,
        _ => u.GetComponent<Throne>() != null ? ThroneVision : TowerVision
    };

    void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 0.12f;

        var srcK = new List<Src>(); var srcM = new List<Src>();
        foreach (var u in Unit.All)
        {
            if (!u.IsAlive || u.team == Team.Neutral) continue;
            var s = new Src { p = u.transform.position, r = VisionOf(u) };
            (u.team == Team.Kurchatov ? srcK : srcM).Add(s);
        }
        temps.RemoveAll(x => Time.time > x.until);
        foreach (var t in temps)
            if (t.reveal == null) (t.team == Team.Kurchatov ? srcK : srcM).Add(new Src { p = t.p, r = t.r });
        Compute(Team.Kurchatov, srcK);
        Compute(Team.Lobnya, srcM);
        foreach (var t in temps)
            if (t.reveal != null && t.reveal.IsAlive && t.team != Team.Neutral) visible[(int)t.team].Add(t.reveal);

        var mine = MatchSetup.PlayerTeam == Team.Kurchatov ? srcK : srcM;
        ApplyToPlayerView();
        Paint(mine);
    }

    static void Compute(Team team, List<Src> src)
    {
        var set = visible[(int)team];
        set.Clear();
        foreach (var u in Unit.All)
        {
            if (!u.IsAlive || u.team == team) continue;
            Vector3 p = u.transform.position;
            foreach (var s in src)
            {
                float dx = p.x - s.p.x, dz = p.z - s.p.z;
                if (dx * dx + dz * dz <= s.r * s.r) { set.Add(u); break; }
            }
        }
    }

    // Прячем/показываем врагов (рендеры и коллайдеры, чтобы нельзя было кликнуть в тумане)
    static void ApplyToPlayerView()
    {
        foreach (var u in Unit.All)
        {
            if (u.team == MatchSetup.PlayerTeam || u.kind == UnitKind.Structure) continue;
            if (!u.IsAlive) { shown.Remove(u); continue; }
            bool see = VisibleToPlayer(u);
            if (shown.TryGetValue(u, out bool was) && was == see) continue;
            shown[u] = see;
            foreach (var r in u.GetComponentsInChildren<Renderer>()) r.enabled = see;
            foreach (var c in u.GetComponentsInChildren<Collider>()) c.enabled = see;
        }
    }

    void Paint(List<Src> src)
    {
        // после перекомпиляции во время игры статическая текстура теряется — создаём заново
        if (Texture == null || pixels == null || pixels.Length != N * N)
        {
            Texture = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            pixels = new Color32[N * N];
            if (plane != null) plane.GetComponent<Renderer>().sharedMaterial.mainTexture = Texture;
            Active = true;
        }
        var dark = new Color32(6, 10, 18, 150);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = dark;
        float cell = WorldSize / N;
        foreach (var s in src)
        {
            int cx = Mathf.FloorToInt((s.p.x - WorldMin) / cell), cz = Mathf.FloorToInt((s.p.z - WorldMin) / cell);
            int rc = Mathf.CeilToInt(s.r / cell) + 1;
            for (int z = Mathf.Max(0, cz - rc); z <= Mathf.Min(N - 1, cz + rc); z++)
                for (int x = Mathf.Max(0, cx - rc); x <= Mathf.Min(N - 1, cx + rc); x++)
                {
                    float wx = WorldMin + (x + 0.5f) * cell, wz = WorldMin + (z + 0.5f) * cell;
                    float d = Mathf.Sqrt((wx - s.p.x) * (wx - s.p.x) + (wz - s.p.z) * (wz - s.p.z));
                    if (d > s.r) continue;
                    float k = Mathf.Clamp01((s.r - d) / (s.r * 0.3f));   // мягкий край
                    int i = z * N + x;
                    byte a = (byte)Mathf.RoundToInt(150 * (1f - k));
                    if (a < pixels[i].a) pixels[i].a = a;
                }
        }
        Texture.SetPixels32(pixels);
        Texture.Apply(false);
    }
}
