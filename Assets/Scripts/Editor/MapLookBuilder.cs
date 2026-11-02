using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Красивая карта: текстуры травы, дорог и камня (рисуются кодом и сохраняются ассетами), река с мостом,
// мощёные площади у тронов, солнце, мягкий свет неба и пост-обработка (свечение, цвет, виньетка).
public static class MapLookBuilder
{
    const string TexFolder = "Assets/Textures/Map";
    public const float RiverHalfWidth = 5f;

    public static void Apply(GameObject ground)
    {
        if (!AssetDatabase.IsValidFolder(TexFolder)) AssetDatabase.CreateFolder("Assets/Textures", "Map");

        // ---------- Трава ----------
        var grass = Tex("Grass", 512, (u, v, rnd) =>
        {
            float n = Wave(u, v, 3, 2, 0.5f) * 0.5f + Wave(u, v, 7, 5, 1.3f) * 0.3f + Wave(u, v, 17, 13, 2.1f) * 0.2f;
            var c = Color.Lerp(new Color(0.24f, 0.45f, 0.19f), new Color(0.36f, 0.58f, 0.24f), n * 0.5f + 0.5f);
            if (rnd.NextDouble() < 0.07) c = Color.Lerp(c, new Color(0.5f, 0.7f, 0.3f), 0.5f);    // светлые травинки
            if (rnd.NextDouble() < 0.04) c *= 0.8f;                                                // тень у корней
            return c;
        });
        SetTex(ground.GetComponent<Renderer>().sharedMaterial, grass, new Vector2(30f * LaneData.Scale, 30f * LaneData.Scale), 0.12f);

        // ---------- Дороги: утоптанная земля с камешками ----------
        var dirt = Tex("Dirt", 256, (u, v, rnd) =>
        {
            float n = Wave(u, v, 4, 3, 0.2f) * 0.5f + Wave(u, v, 11, 9, 0.9f) * 0.5f;
            var c = Color.Lerp(new Color(0.5f, 0.4f, 0.27f), new Color(0.64f, 0.53f, 0.36f), n * 0.5f + 0.5f);
            if (rnd.NextDouble() < 0.02) c = new Color(0.7f, 0.68f, 0.62f);   // камешки
            if (rnd.NextDouble() < 0.03) c *= 0.82f;
            return c;
        });
        var mid = GameObject.Find("MidLane");
        if (mid != null) LaneMat(mid, "Lane_Mid", dirt);
        for (int i = 0; i < 4; i++)
        {
            var s = GameObject.Find("SideLane_" + i);
            if (s != null) LaneMat(s, "Lane_Side" + i, dirt);
        }

        // ---------- Река поперёк карты (из левого верхнего угла в правый нижний) ----------
        var sand = Tex("Sand", 256, (u, v, rnd) =>
        {
            float n = Wave(u, v, 5, 4, 0.7f);
            var c = Color.Lerp(new Color(0.72f, 0.64f, 0.45f), new Color(0.82f, 0.75f, 0.55f), n * 0.5f + 0.5f);
            if (rnd.NextDouble() < 0.03) c *= 0.85f;
            return c;
        });
        var water = Tex("Water", 256, (u, v, rnd) =>
        {
            float w = Mathf.Abs(Wave(u, v, 6, 3, 0.4f) + Wave(u, v, 3, 7, 1.7f) * 0.7f);
            var c = Color.Lerp(new Color(0.12f, 0.35f, 0.55f), new Color(0.22f, 0.55f, 0.75f), Mathf.Clamp01(w * 0.6f));
            if (w < 0.08f) c = Color.Lerp(c, new Color(0.75f, 0.9f, 1f), 0.6f);   // блики-каустика
            return c;
        });
        float len = LaneData.Half * 2.6f;
        var river = new GameObject("River").transform;
        var bank = Strip(river, "RiverBank", RiverHalfWidth * 2f + 5f, len, 0.004f, "River_Sand", sand, new Vector2(3f, len / 6f), 0.1f);
        var wat = Strip(river, "RiverWater", RiverHalfWidth * 2f, len, 0.008f, "River_Water", water, new Vector2(2f, len / 8f), 0.92f);
        wat.AddComponent<WaterFx>();
        for (int i = -3; i <= 3; i++)
        {
            Vector3 p = new Vector3(i * 14f, 0.1f, -i * 14f);
            BuildParts.Ambient(river, "WaterGlint", p, AmbientFx.Kind.Sparkle, new Color(0.75f, 0.92f, 1f), 0.6f, 3f, 1);
        }
        Bridge(river);

        // ---------- Мощёные площади у тронов ----------
        var stone = Tex("Stone", 256, (u, v, rnd) =>
        {
            float gu = Mathf.Repeat(u * 4f, 1f), gv = Mathf.Repeat(v * 4f + Mathf.Floor(u * 4f) * 0.5f, 1f);
            bool seam = gu < 0.04f || gv < 0.04f;
            float n = Wave(u, v, 8, 8, 0.3f);
            var c = Color.Lerp(new Color(0.55f, 0.54f, 0.52f), new Color(0.68f, 0.67f, 0.64f), n * 0.5f + 0.5f);
            return seam ? c * 0.6f : c;
        });
        foreach (var t in new[] { Team.Kurchatov, Team.Lobnya })
        {
            float pr = 14.2f * LaneData.Scale * 0.75f;
            var pm = ProcMesh.Lathe(new[] { new Vector2(0f, 0f), new Vector2(pr, 0f), new Vector2(pr - 0.2f, 0.014f), new Vector2(0f, 0.014f) }, 64);
            // плитка ровной сеткой сверху (а не «спицами» по кругу)
            var pv = pm.vertices; var puv = new Vector2[pv.Length];
            for (int i = 0; i < pv.Length; i++) puv[i] = new Vector2(pv[i].x, pv[i].z) / (pr * 2f) + new Vector2(0.5f, 0.5f);
            pm.uv = puv;
            var plaza = BuildParts.Mesh(null, "Plaza", BuildParts.Save("Map_Plaza", pm), LaneData.Throne(t), Quaternion.identity, Vector3.one,
                "Map_Plaza_" + t, Color.Lerp(Color.white, TeamUtil.GetColor(t), 0.12f), 0f, 0.3f);
            SetTex(plaza.GetComponent<Renderer>().sharedMaterial, stone, new Vector2(7f, 7f), 0.3f);
            BuildParts.GlowMesh(plaza.transform.parent, "PlazaRing", BuildParts.Save("Map_PlazaRing", ProcMesh.Torus(14.2f * LaneData.Scale * 0.75f, 0.1f, 96, 6)),
                LaneData.Throne(t) + Vector3.up * 0.02f, Quaternion.identity, Vector3.one, "Throne_Ring_" + (t == Team.Kurchatov ? "Kur" : "Mos"), TeamUtil.GetColor(t));
        }

        Lighting();
        PostFx();
    }

    // ---------- Солнце, небо, пост-обработка ----------
    static void Lighting()
    {
        Light sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) sun = l;
        if (sun != null)
        {
            sun.color = new Color(1f, 0.95f, 0.84f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var dn = new GameObject("DayNight").AddComponent<DayNight>();
            dn.sun = sun;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.7f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.55f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.26f, 0.2f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.7f, 0.76f, 0.82f);
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 140f;
    }

    static void PostFx()
    {
        const string path = "Assets/Scenes/PostFX.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.85f); bloom.threshold.Override(0.95f); bloom.scatter.Override(0.65f);
        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.12f); ca.contrast.Override(14f); ca.saturation.Override(16f);
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.22f); vig.smoothness.Override(0.45f);
        var tm = profile.Add<Tonemapping>(true);
        tm.mode.Override(TonemappingMode.Neutral);
        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var go = new GameObject("PostFX");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
        var cam = Camera.main;
        if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    // ---------- Мост на миде через реку ----------
    static void Bridge(Transform parent)
    {
        var b = new GameObject("MidBridge").transform;
        b.SetParent(parent, false);
        b.rotation = Quaternion.Euler(0, 45f, 0);   // вдоль мида
        // перила и фонари не перегораживают реку: в навигацию мост не попадает
        b.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;
        var wood = new Color(0.55f, 0.35f, 0.18f);
        var dark = new Color(0.36f, 0.21f, 0.1f);
        float span = RiverHalfWidth * 2f + 4f, w = LaneData.LaneHalfWidth * 2f + 0.6f;
        for (int i = 0; i < 14; i++)
        {
            float z = Mathf.Lerp(-span / 2f, span / 2f, i / 13f);
            BuildParts.Prim(b, "Plank", PrimitiveType.Cube, new Vector3(0, 0.03f, z), new Vector3(w, 0.05f, span / 14f - 0.06f), i % 2 == 0 ? "Bath_Wood" : "Bath_WoodDark", i % 2 == 0 ? wood : dark, 0f, 0.35f);
        }
        foreach (float x in new[] { -w / 2f, w / 2f })
        {
            BuildParts.Prim(b, "Rail", PrimitiveType.Cube, new Vector3(x, 0.75f, 0), new Vector3(0.14f, 0.12f, span), "Bath_WoodDark", dark, 0f, 0.35f);
            for (int i = 0; i < 6; i++)
                BuildParts.Prim(b, "Post", PrimitiveType.Cube, new Vector3(x, 0.4f, Mathf.Lerp(-span / 2f, span / 2f, i / 5f)), new Vector3(0.18f, 0.8f, 0.18f), "Bath_WoodDark", dark, 0f, 0.35f);
            BuildParts.Lamp(b, "Warm", new Vector3(x, 0f, -span / 2f - 0.5f), new Color(1f, 0.82f, 0.45f), 2.4f);
            BuildParts.Lamp(b, "Warm", new Vector3(x, 0f, span / 2f + 0.5f), new Color(1f, 0.82f, 0.45f), 2.4f);
        }
    }

    // Полоса вдоль реки (из угла в угол)
    static GameObject Strip(Transform parent, string name, float width, float len, float y, string mat, Texture2D tex, Vector2 tiling, float smooth)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(0, y, 0);
        go.transform.rotation = Quaternion.Euler(0, -45f, 0);
        go.transform.localScale = new Vector3(width, 0.01f, len);
        MapBuilder.Paint(go, mat, Color.white);
        SetTex(go.GetComponent<Renderer>().sharedMaterial, tex, tiling, smooth);
        return go;
    }

    static void LaneMat(GameObject lane, string mat, Texture2D tex)
    {
        MapBuilder.Paint(lane, mat, Color.white);
        var s = lane.transform.localScale;
        SetTex(lane.GetComponent<Renderer>().sharedMaterial, tex, new Vector2(Mathf.Max(1f, s.x / 6f), Mathf.Max(1f, s.z / 6f)), 0.15f);
    }

    static void SetTex(Material m, Texture2D t, Vector2 tiling, float smooth)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", t); m.SetTextureScale("_BaseMap", tiling); }
        m.mainTexture = t; m.mainTextureScale = tiling;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(m);
    }

    // Бесшовная волна: целые частоты → края текстуры совпадают
    static float Wave(float u, float v, int fx, int fy, float ph) =>
        Mathf.Sin(2f * Mathf.PI * (fx * u + fy * v) + ph) * 0.6f + Mathf.Sin(2f * Mathf.PI * (fy * u - fx * v) + ph * 1.7f) * 0.4f;

    static Texture2D Tex(string name, int n, System.Func<float, float, System.Random, Color> f)
    {
        string path = $"{TexFolder}/{name}.asset";
        AssetDatabase.DeleteAsset(path);
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        var rnd = new System.Random(name.GetHashCode());
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var c = f(x / (float)n, y / (float)n, rnd);
                c.a = 1f;
                px[y * n + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply(true);
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }
}
