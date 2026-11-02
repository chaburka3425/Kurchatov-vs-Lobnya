using UnityEditor;
using UnityEngine;

// Саня Финд — как Shadow Fiend из Доты: тёмный демон-скелет, парит на дымном хвосте,
// грудь в светящихся трещинах, шипастый воротник, грива из осколков и рога, длинные руки с когтями.
// Вместо душ вокруг летают горящие сосиски, в поднятой лапе — горящая сосиска.
public static partial class HeroBuilder
{
    public static GameObject BuildSanya(Team team, Vector3 pos, Transform respawn)
    {
        var def = HeroCatalog.SanyaFiend;
        var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Portraits/sanya_fiend.png");
        var root = BaseHero(def, team, pos, respawn, 3.0f, 0.65f, portrait, null, 3.7f);
        root.AddComponent<SausageStash>();
        var model = ModelRoot(root);
        var m = model.transform;

        var body = new Color(0.09f, 0.08f, 0.1f);
        var bone = new Color(0.2f, 0.18f, 0.2f);
        var smoke = new Color(0.04f, 0.035f, 0.05f);
        var ember = new Color(1f, 0.36f, 0.05f);
        var emberHot = new Color(1f, 0.7f, 0.25f);
        var sausage = new Color(0.66f, 0.27f, 0.17f);

        Part(model, "TeamRing", PrimitiveType.Cylinder, new Vector3(0, 0.02f, 0), new Vector3(1.9f, 0.02f, 1.9f), "Hero_Ring_" + team, TeamUtil.GetColor(team));

        // ---------- Дымный хвост вместо ног ----------
        LathePart(m, "Tail", "San_Tail", new[] { new Vector2(0f, 0.3f), new Vector2(0.05f, 0.35f), new Vector2(0.12f, 0.55f), new Vector2(0.2f, 0.8f), new Vector2(0.27f, 1.05f),
            new Vector2(0.25f, 1.2f), new Vector2(0f, 1.25f) }, Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.8f), "San_Smoke", smoke, 0f, 0.15f, 24);
        var rnd = new System.Random(2);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 0.7f, y = 0.45f + (float)rnd.NextDouble() * 0.6f;
            Vector3 from = new Vector3(Mathf.Cos(a) * 0.15f, y, Mathf.Sin(a) * 0.12f);
            Spike(m, "Wisp", "San_Wisp" + i, from, new Vector3(Mathf.Cos(a) * 0.6f, -0.7f, Mathf.Sin(a) * 0.6f + 0.3f).normalized, 0.4f + (float)rnd.NextDouble() * 0.25f, 0.07f,
                "San_Smoke", smoke, 0.35f);
        }
        Ball(m, "SmokeA", new Vector3(0.1f, 0.5f, 0.15f), new Vector3(0.35f, 0.28f, 0.35f), "San_Smoke", smoke, 0f, 0.15f);
        Ball(m, "SmokeB", new Vector3(-0.12f, 0.75f, 0.05f), new Vector3(0.4f, 0.3f, 0.38f), "San_Smoke", smoke, 0f, 0.15f);

        // ---------- Торс: узкая талия, широкая грудь, рёбра и тлеющие трещины ----------
        var torso = new[] { new Vector2(0f, 1.1f), new Vector2(0.2f, 1.12f), new Vector2(0.22f, 1.3f), new Vector2(0.3f, 1.5f), new Vector2(0.44f, 1.72f),
                            new Vector2(0.5f, 1.88f), new Vector2(0.46f, 2.02f), new Vector2(0.3f, 2.12f), new Vector2(0f, 2.16f) };
        LathePart(m, "Torso", "San_Torso", torso, Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.7f), "San_Body", body, 0.3f, 0.75f, 32);
        for (int i = 0; i < 4; i++)
        {
            float y = 1.55f + i * 0.12f, w = 0.24f + i * 0.04f;
            foreach (float sd in new[] { -1f, 1f })
                TubePart(m, "Rib", $"San_Rib{i}{(sd < 0 ? "L" : "R")}", ProcMesh.Bezier(new Vector3(0.03f * sd, y + 0.03f, -0.36f + i * 0.0f), new Vector3(w * 0.5f * sd, y + 0.01f, -0.38f),
                    new Vector3(w * sd, y - 0.04f, -0.3f), new Vector3((w + 0.08f) * sd, y - 0.1f, -0.15f), 8), ProcMesh.Taper(8, 0.035f, 0.02f), "San_Bone", bone, 0.2f, 0.6f, 7);
        }
        // трещины с огнём на груди
        var crackA = new[] { new Vector3(0, 1.95f, -0.36f), new Vector3(0.04f, 1.8f, -0.39f), new Vector3(-0.03f, 1.66f, -0.39f), new Vector3(0.05f, 1.5f, -0.33f), new Vector3(0.0f, 1.36f, -0.24f) };
        GlowMesh(m, "Crack", SaveMesh("San_Crack", ProcMesh.Tube(crackA, new[] { 0.012f, 0.025f, 0.03f, 0.022f, 0.01f }, 6)), Vector3.zero, Quaternion.identity, Vector3.one, "San_Ember", ember);
        Glow(model, "Core", PrimitiveType.Sphere, new Vector3(0, 1.72f, -0.33f), new Vector3(0.12f, 0.16f, 0.06f), "San_EmberHot", emberHot);

        // ---------- Шипастый воротник и наплечные рога ----------
        for (int i = 0; i < 14; i++)
        {
            float a = Mathf.Lerp(-200f, 20f, i / 13f) * Mathf.Deg2Rad;      // дугой вокруг шеи (спереди открыто)
            Vector3 from = new Vector3(Mathf.Cos(a) * 0.3f, 2.05f, Mathf.Sin(a) * 0.24f + 0.05f);
            Vector3 dir = (new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.6f + Vector3.up * 0.9f + Vector3.forward * 0.35f).normalized;
            float len = 0.3f + 0.25f * Mathf.Sin(i / 13f * Mathf.PI);
            Spike(m, "Collar", "San_Collar" + i, from, dir, len, 0.06f, "San_Body", body, 0.2f);
        }
        foreach (float sd in new[] { -1f, 1f })
        {
            Ball(m, "Pauldron", new Vector3(0.5f * sd, 2.0f, 0.02f), new Vector3(0.36f, 0.26f, 0.34f), "San_Bone", bone, 0.2f, 0.6f);
            for (int k = 0; k < 3; k++)
                Spike(m, "ShoulderHorn", $"San_SHorn{(sd < 0 ? "L" : "R")}{k}", new Vector3(0.52f * sd, 2.08f, -0.06f + k * 0.12f),
                    new Vector3(0.45f * sd, 1f, 0.35f + k * 0.15f).normalized, 0.38f - k * 0.08f, 0.07f, "San_Body", body, 0.25f);
        }

        // ---------- Голова демона ----------
        var head = Pivot(m, "Head", new Vector3(0, 2.42f, -0.12f));
        head.localScale = Vector3.one * 0.55f;
        MeshPart(head, "Skull", SaveMesh("San_Skull", ProcMesh.Blob(d =>
        {
            float x = d.x * 0.42f, y = d.y * 0.5f, z = d.z * 0.55f;
            if (d.z < 0f) { z *= 1.15f; x *= 1f - 0.35f * (-d.z) * Mathf.Max(0f, -d.y + 0.2f); }   // вытянутая морда
            if (d.y > 0.2f && d.z < 0f) y += 0.06f * (-d.z);                                     // надбровная дуга
            if (d.y < -0.3f) x *= 0.8f;                                                           // узкая челюсть
            return new Vector3(x, y, z);
        })), Vector3.zero, Quaternion.identity, Vector3.one, "San_Body", body, 0.3f, 0.75f);
        // глаза: светящиеся щели, наклонены злобно
        foreach (float sd in new[] { -1f, 1f })
        {
            var eye = Glow(head.gameObject, "Eye", PrimitiveType.Sphere, new Vector3(0.15f * sd, 0.1f, -0.585f), new Vector3(0.2f, 0.07f, 0.06f), "Sanya_Eyes", new Color(1f, 0.42f, 0.06f));
            eye.transform.localRotation = Quaternion.Euler(0, 0, -18f * sd);
            var core = Glow(head.gameObject, "EyeCore", PrimitiveType.Sphere, new Vector3(0.15f * sd, 0.1f, -0.615f), new Vector3(0.1f, 0.035f, 0.04f), "San_EmberHot", emberHot);
            core.transform.localRotation = Quaternion.Euler(0, 0, -18f * sd);
            // рог: изогнутый вверх и назад
            TubePart(head, "Horn", $"San_Horn{(sd < 0 ? "L" : "R")}", ProcMesh.Bezier(new Vector3(0.22f * sd, 0.32f, -0.15f), new Vector3(0.4f * sd, 0.6f, -0.1f),
                new Vector3(0.45f * sd, 0.85f, 0.2f), new Vector3(0.3f * sd, 1.05f, 0.45f), 12), ProcMesh.Taper(12, 0.09f, 0f, 1.2f), "San_Bone", bone, 0.2f, 0.6f, 9);
        }
        // пасть с клыками
        Ball(head, "Maw", new Vector3(0, -0.24f, -0.5f), new Vector3(0.36f, 0.12f, 0.2f), "San_Maw", new Color(0.35f, 0.06f, 0.03f), 0f, 0.5f);
        Glow(head.gameObject, "MawGlow", PrimitiveType.Sphere, new Vector3(0, -0.24f, -0.53f), new Vector3(0.24f, 0.05f, 0.1f), "San_Ember", ember);
        for (int i = 0; i < 6; i++)
        {
            float x = Mathf.Lerp(-0.13f, 0.13f, i / 5f);
            bool fang = i == 0 || i == 5;
            Spike(head, "Tooth", "San_Tooth" + i, new Vector3(x, -0.18f, -0.56f + Mathf.Abs(x) * 0.4f), Vector3.down, fang ? 0.16f : 0.08f, fang ? 0.03f : 0.02f,
                "San_Teeth", new Color(0.9f, 0.87f, 0.78f), 0f);
        }
        // грива из осколков: назад и вверх, кончики тлеют
        for (int i = 0; i < 11; i++)
        {
            float t = i / 10f, side = Mathf.Lerp(-1f, 1f, t);
            Vector3 from = new Vector3(side * 0.3f, 0.35f - Mathf.Abs(side) * 0.15f, 0.05f + Mathf.Abs(side) * 0.1f);
            Vector3 dir = new Vector3(side * 0.55f, 0.7f, 0.9f).normalized;
            float len = 0.9f + 0.4f * (1f - Mathf.Abs(side));
            Spike(head, "Mane", "San_Mane" + i, from, dir, len, 0.09f, "San_Body", body, 0.3f);
            Glow(head.gameObject, "ManeTip", PrimitiveType.Sphere, from + dir * len * 0.92f, Vector3.one * 0.06f, "San_Ember", ember);
        }

        // ---------- Руки: длинные, костлявые, с когтями ----------
        var armR = DemonArm(m, "R", new Vector3(0.55f, 2.0f, 0.02f), new Vector3(0.82f, 1.45f, 0.12f), new Vector3(0.86f, 0.98f, -0.12f), body, bone, ember, out var handR, down: true);
        var armL = DemonArm(m, "L", new Vector3(-0.55f, 2.0f, 0.02f), new Vector3(-0.82f, 2.35f, 0.05f), new Vector3(-0.84f, 2.72f, -0.06f), body, bone, ember, out var handL, down: false);
        // горящая сосиска в поднятой лапе
        SausageMesh(armL, "HandSausage", handL + new Vector3(0, 0.22f, 0), Quaternion.identity, 1f, sausage);
        Glow(armL.gameObject, "Flame", PrimitiveType.Sphere, handL + new Vector3(0, 0.52f, 0), new Vector3(0.22f, 0.4f, 0.22f), "Sanya_Fire", new Color(1f, 0.42f, 0.04f));
        Glow(armL.gameObject, "FlameMid", PrimitiveType.Sphere, handL + new Vector3(0, 0.48f, -0.02f), new Vector3(0.15f, 0.28f, 0.15f), "Sanya_FireMid", new Color(1f, 0.68f, 0.12f));
        Glow(armL.gameObject, "FlameCore", PrimitiveType.Sphere, handL + new Vector3(0, 0.44f, -0.03f), new Vector3(0.08f, 0.16f, 0.08f), "Sanya_FireCore", new Color(1f, 0.93f, 0.4f));
        Spike(armL, "FlameTip", "San_FlameTip", handL + new Vector3(0, 0.66f, 0), Vector3.up, 0.28f, 0.08f, "Sanya_Fire", new Color(1f, 0.42f, 0.04f), 0.25f);

        // ---------- Души-сосиски кружат вокруг ----------
        var souls = Pivot(m, "Souls", new Vector3(0, 1.3f, 0));
        var spin = souls.gameObject.AddComponent<SpinFx>();
        spin.speed = 70f; spin.bob = 0.12f;
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 0.95f, i * 0.25f, Mathf.Sin(a) * 0.95f);
            SausageMesh(souls, "Soul", p, Quaternion.Euler(0, -a * Mathf.Rad2Deg, 90), 0.8f, sausage);
            Glow(souls.gameObject, "SoulGlow", PrimitiveType.Sphere, p, new Vector3(0.36f, 0.22f, 0.22f), "San_SoulGlow", new Color(1f, 0.45f, 0.1f));
        }
        return root;
    }

    // Костлявая рука демона: плечо → локоть (шип) → кисть с тремя когтями
    static Transform DemonArm(Transform m, string side, Vector3 shoulder, Vector3 elbow, Vector3 wrist, Color body, Color bone, Color ember, out Vector3 hand, bool down)
    {
        var p = Pivot(m, "Arm" + side + "Pivot", shoulder);
        var path = Through(Vector3.zero, elbow - shoulder, wrist - shoulder, 12);
        TubePart(p, "Arm" + side, "San_Arm" + side, path, ProcMesh.Taper(12, 0.11f, 0.06f), "San_Body", body, 0.3f, 0.75f);
        Vector3 el = elbow - shoulder;
        Spike(p, "ElbowSpike", "San_Elbow" + side, el, (el - (wrist - shoulder) * 0.5f + Vector3.forward * 0.3f).normalized, 0.25f, 0.05f, "San_Bone", bone, 0.1f);
        hand = wrist - shoulder;
        Ball(p, "Hand" + side, hand, new Vector3(0.16f, 0.14f, 0.14f), "San_Bone", bone, 0.2f, 0.6f);
        Vector3 along = (wrist - elbow).normalized;
        for (int i = 0; i < 3; i++)
        {
            Vector3 spread = new Vector3((i - 1) * 0.35f, 0f, (i - 1) * 0.15f - 0.25f);
            Vector3 dir = (along + spread).normalized;
            Spike(p, "Claw", $"San_Claw{side}{i}", hand + dir * 0.06f, dir, 0.3f, 0.035f, "San_Body", body, down ? 0.35f : -0.2f);
            Glow(p.gameObject, "ClawTip", PrimitiveType.Sphere, hand + dir * 0.33f, Vector3.one * 0.03f, "San_Ember", ember);
        }
        return p;
    }

    // Меш со светящимся (неосвещаемым) материалом — огонь, трещины
    static GameObject GlowMesh(Transform parent, string name, Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, string matName, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        string path = $"{MatFolder}/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); else mat.color = color;
        EditorUtility.SetDirty(mat);
        r.sharedMaterial = mat;
        return go;
    }

    // Сосиска: тело вращения с закруглёнными концами
    static void SausageMesh(Transform parent, string name, Vector3 pos, Quaternion rot, float size, Color color)
    {
        var prof = new[] { new Vector2(0f, -0.22f), new Vector2(0.06f, -0.21f), new Vector2(0.1f, -0.16f), new Vector2(0.115f, -0.08f), new Vector2(0.12f, 0f),
                           new Vector2(0.115f, 0.08f), new Vector2(0.1f, 0.16f), new Vector2(0.06f, 0.21f), new Vector2(0f, 0.22f) };
        LathePart(parent, name, "San_Sausage", prof, pos, rot, Vector3.one * size, "Sanya_Sausage", color, 0f, 0.7f, 16);
    }
}
