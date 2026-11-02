using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Аватарки героев из их 3D-моделей: Tools → MOBA → «Сделать аватарки из моделей».
// Герой собирается во временном месте, фотографируется в 3/4 с тёплым основным светом и контровым,
// потом картинка собирается: фон цвета атрибута с лучами, свечение вокруг силуэта, виньетка.
// Старые фото-аватарки один раз копируются в Assets/Textures/PortraitPhotos.
public static class PortraitRenderer
{
    const int Big = 512, Size = 256, Layer = 31;
    const string Folder = "Assets/Resources/Portraits";

    // как кадрировать: какую часть высоты героя показать и куда смотреть
    struct Shot { public System.Func<GameObject> build; public string id; public float span, focus, yaw; }

    [MenuItem("Tools/MOBA/Сделать аватарки из моделей")]
    public static void RenderAll()
    {
        BackupPhotos();
        var far = new Vector3(0, 1000, 0);
        var shots = new[]
        {
            // span — сколько метров влезает в кадр, focus — насколько ниже головы центр кадра (карточки широкие и режут верх/низ)
            new Shot { id = "ogr_mog", build = () => HeroBuilder.BuildOgrMog(Team.Kurchatov, far, null), span = 2.5f, focus = 0.2f, yaw = 10f },
            new Shot { id = "sanya_fiend", build = () => HeroBuilder.BuildSanya(Team.Kurchatov, far, null), span = 1.6f, focus = 0.12f, yaw = 20f },
            new Shot { id = "max_gold", build = () => HeroBuilder.BuildMaxGold(Team.Kurchatov, far, null), span = 1.55f, focus = 0.12f, yaw = 22f },
            new Shot { id = "pivmaver", build = () => HeroBuilder.BuildPivmaver(Team.Kurchatov, far, null), span = 1.9f, focus = 0.15f, yaw = 16f },
            new Shot { id = "shashlik", build = () => HeroBuilder.BuildShashlik(Team.Kurchatov, far, null), span = 1.5f, focus = 0.12f, yaw = 20f },
        };

        // свет и окружение только на время съёмки
        var oldLights = new List<Light>();
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.enabled) { l.enabled = false; oldLights.Add(l); }
        var oldAmbientMode = RenderSettings.ambientMode;
        var oldAmbient = RenderSettings.ambientLight;
        var oldFog = RenderSettings.fog;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.48f);
        RenderSettings.fog = false;

        var temp = new List<GameObject>();
        try
        {
            foreach (var shot in shots)
            {
                var hero = shot.build();
                temp.Add(hero);
                var def = HeroCatalog.Get(shot.id);
                var png = Shoot(hero, def, shot, temp);
                File.WriteAllBytes($"{Folder}/{shot.id}.png", png);
                Object.DestroyImmediate(hero);
                temp.Remove(hero);
            }
        }
        finally
        {
            foreach (var t in temp) if (t != null) Object.DestroyImmediate(t);
            foreach (var l in oldLights) if (l != null) l.enabled = true;
            RenderSettings.ambientMode = oldAmbientMode;
            RenderSettings.ambientLight = oldAmbient;
            RenderSettings.fog = oldFog;
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Аватарки готовы",
            "Аватарки героев сделаны из моделей (Assets/Resources/Portraits).\nТеперь пересоздай карту: Tools → MOBA → «Создать карту Курчатов vs Москва».", "Ок");
    }

    static byte[] Shoot(GameObject hero, HeroDefinition def, Shot shot, List<GameObject> temp)
    {
        // всё героя — на отдельный слой, кольцо команды не снимаем
        foreach (var t in hero.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
        var ring = hero.transform.Find("Model/TeamRing");
        if (ring != null) ring.gameObject.SetActive(false);

        // центр кадра — голова (у Огра — между двумя головами)
        Vector3 basePos = hero.transform.position;
        Vector3 headSum = Vector3.zero; int heads = 0;
        foreach (var t in hero.GetComponentsInChildren<Transform>(true))
            if (t.name == "Head" || t.name == "HeadLeft" || t.name == "HeadRight") { headSum += t.position; heads++; }
        Vector3 head = heads > 0 ? headSum / heads : basePos + Vector3.up * 2.5f;
        float top = head.y - basePos.y + 0.5f;

        // камера спереди в 3/4, чуть сверху. «Перед» героя — +Z корня.
        Vector3 look = head + Vector3.down * shot.focus;
        Vector3 dir = Quaternion.Euler(-7f, shot.yaw, 0f) * Vector3.forward;
        const float fov = 24f;
        float dist = shot.span * 0.5f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * 1.08f;

        var camGo = new GameObject("PortraitCam"); temp.Add(camGo);
        var cam = camGo.AddComponent<Camera>();
        cam.transform.position = look + dir * dist;
        cam.transform.LookAt(look);
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;
        cam.cullingMask = 1 << Layer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.allowHDR = false;
        cam.allowMSAA = true;

        // лица-картинки разворачиваем к камере (Billboard в редакторе не работает)
        foreach (var bb in hero.GetComponentsInChildren<Billboard>(true))
        {
            if (bb.anchor != null) bb.transform.position = bb.anchor.position - cam.transform.forward * bb.towardCamera;
            bb.transform.rotation = cam.transform.rotation;
        }

        // основной тёплый свет сбоку-сверху и холодный контровой сзади
        var key = MakeLight(temp, LightType.Directional, new Color(1f, 0.95f, 0.86f), 1.35f, Quaternion.LookRotation(-(Quaternion.Euler(-35f, shot.yaw - 40f, 0) * Vector3.forward)));
        var rim = MakeLight(temp, LightType.Spot, Color.Lerp(AttributeUtil.GetColor(def.attribute), Color.white, 0.45f), 40f, Quaternion.identity);
        rim.transform.position = basePos + new Vector3(-1.8f, top + 1.2f, -2.6f);
        rim.transform.LookAt(look);
        rim.range = 9f; rim.spotAngle = 70f;
        var fill = MakeLight(temp, LightType.Spot, new Color(0.75f, 0.82f, 1f), 14f, Quaternion.identity);
        fill.transform.position = look + Quaternion.Euler(0, shot.yaw + 50f, 0) * Vector3.forward * 4f;
        fill.transform.LookAt(look);
        fill.range = 10f; fill.spotAngle = 80f;

        // два кадра — на чёрном и на белом: так получаем прозрачность без альфа-канала URP
        var black = Render(cam, Color.black);
        var white = Render(cam, Color.white);
        Object.DestroyImmediate(camGo);

        return Compose(black, white, AttributeUtil.GetColor(def.attribute));
    }

    static Light MakeLight(List<GameObject> temp, LightType type, Color c, float intensity, Quaternion rot)
    {
        var go = new GameObject("PortraitLight"); temp.Add(go);
        var l = go.AddComponent<Light>();
        l.type = type; l.color = c; l.intensity = intensity; l.shadows = LightShadows.None;
        go.transform.rotation = rot;
        return l;
    }

    static Color[] Render(Camera cam, Color bg)
    {
        cam.backgroundColor = bg;
        var rt = new RenderTexture(Big, Big, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(Big, Big, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, Big, Big), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        var px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        return px;
    }

    // Сборка: фон с лучами → свечение силуэта → герой → затемнение снизу и по краям → уменьшение 512 → 256
    static byte[] Compose(Color[] black, Color[] white, Color attr)
    {
        int n = Big * Big;
        var alpha = new float[n];
        var hero = new Color[n];
        for (int i = 0; i < n; i++)
        {
            Color b = black[i], w = white[i];
            float a = 1f - Mathf.Clamp01(((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f);
            alpha[i] = a;
            hero[i] = a > 0.004f ? new Color(Mathf.Clamp01(b.r / a), Mathf.Clamp01(b.g / a), Mathf.Clamp01(b.b / a)) : Color.clear;
        }
        var glow = Blur(alpha, 10);
        glow = Blur(glow, 10);

        Color dark = Color.Lerp(attr, Color.black, 0.86f), mid = Color.Lerp(attr, Color.black, 0.45f), light = Color.Lerp(attr, Color.white, 0.25f);
        var outPx = new Color[n];
        for (int y = 0; y < Big; y++)
            for (int x = 0; x < Big; x++)
            {
                int i = y * Big + x;
                float u = x / (float)Big, v = y / (float)Big;   // v = 0 внизу
                // радиальный градиент: светлее за головой
                float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.62f));
                Color c = Color.Lerp(mid, dark, Mathf.SmoothStep(0f, 1f, d * 1.5f));
                // мягкие лучи из-за спины
                float ang = Mathf.Atan2(v - 0.62f, u - 0.5f);
                float rays = Mathf.Pow(Mathf.Abs(Mathf.Sin(ang * 7f + 0.6f)), 6f) * Mathf.Clamp01(1.1f - d * 1.6f) * 0.22f;
                c = Color.Lerp(c, light, rays);
                // свечение вокруг силуэта
                c = Color.Lerp(c, light, Mathf.Clamp01(glow[i] * 1.6f) * 0.55f * (1f - alpha[i]));
                // герой
                c = Color.Lerp(c, hero[i], alpha[i]);
                // низ уходит в темноту, края — виньетка
                float bottom = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.22f - v) / 0.22f)) * 0.55f;
                float vig = Mathf.Clamp01((Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.55f)) - 0.42f) * 1.6f) * 0.6f;
                c = Color.Lerp(c, Color.black, Mathf.Max(bottom, vig));
                c.a = 1f;
                outPx[i] = c;
            }

        // 512 → 256 (сглаживание)
        var small = new Texture2D(Size, Size, TextureFormat.RGB24, false);
        var sp = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int i = y * 2 * Big + x * 2;
                sp[y * Size + x] = (outPx[i] + outPx[i + 1] + outPx[i + Big] + outPx[i + Big + 1]) * 0.25f;
            }
        small.SetPixels(sp);
        small.Apply();
        var bytes = small.EncodeToPNG();
        Object.DestroyImmediate(small);
        return bytes;
    }

    // Быстрое размытие (два прохода «скользящим окном»)
    static float[] Blur(float[] src, int r)
    {
        var tmp = new float[src.Length];
        var dst = new float[src.Length];
        for (int y = 0; y < Big; y++)
        {
            float sum = 0f; int cnt = 0;
            for (int x = -r; x < Big + r; x++)
            {
                if (x + r < Big) { sum += src[y * Big + x + r]; cnt++; }
                if (x - r - 1 >= 0) { sum -= src[y * Big + x - r - 1]; cnt--; }
                if (x >= 0 && x < Big) tmp[y * Big + x] = sum / Mathf.Max(1, cnt);
            }
        }
        for (int x = 0; x < Big; x++)
        {
            float sum = 0f; int cnt = 0;
            for (int y = -r; y < Big + r; y++)
            {
                if (y + r < Big) { sum += tmp[(y + r) * Big + x]; cnt++; }
                if (y - r - 1 >= 0) { sum -= tmp[(y - r - 1) * Big + x]; cnt--; }
                if (y >= 0 && y < Big) dst[y * Big + x] = sum / Mathf.Max(1, cnt);
            }
        }
        return dst;
    }

    // Фото-аватарки сохраняем один раз, чтобы их не потерять
    static void BackupPhotos()
    {
        const string backup = "Assets/Textures/PortraitPhotos";
        if (!AssetDatabase.IsValidFolder(backup)) AssetDatabase.CreateFolder("Assets/Textures", "PortraitPhotos");
        foreach (var id in new[] { "ogr_mog_l", "ogr_mog_r", "sanya_fiend", "max_gold", "pivmaver" })
        {
            string src = $"{Folder}/{id}.png", dst = $"{backup}/{id}.png";
            if (File.Exists(src) && !File.Exists(dst)) AssetDatabase.CopyAsset(src, dst);
        }
    }
}
