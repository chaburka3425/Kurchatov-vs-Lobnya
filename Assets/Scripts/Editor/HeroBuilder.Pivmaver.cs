using UnityEditor;
using UnityEngine;

// Пивмавер: пивная «паучиха» из гладких форм (ProcMesh) — бочонок-брюшко с обручами и пеной,
// торс в чёрной зипке, 8 изогнутых глянцевых ног с янтарными кольцами, лапы с бутылками.
// Меши сохраняются в Assets/Meshes/Heroes, ноги и лапы — на шарнирах (их двигает UnitAnimator).
public static partial class HeroBuilder
{
    const string MeshFolder = "Assets/Meshes/Heroes";

    public static GameObject BuildPivmaver(Team team, Vector3 pos, Transform respawn)
    {
        var def = HeroCatalog.Pivmaver;
        var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Portraits/pivmaver.png");
        var root = BaseHero(def, team, pos, respawn, 3.0f, 0.9f, portrait, null, 3.6f);
        root.AddComponent<PivmaverKit>();
        var model = ModelRoot(root);
        var m = model.transform;

        var skin = new Color(0.93f, 0.78f, 0.66f);
        var hoodie = new Color(0.07f, 0.07f, 0.08f);
        var hair = new Color(0.13f, 0.09f, 0.07f);
        var chitin = new Color(0.13f, 0.14f, 0.3f);
        var amber = new Color(0.98f, 0.6f, 0.1f);
        var iron = new Color(0.42f, 0.43f, 0.48f);
        var glass = new Color(0.34f, 0.16f, 0.04f);
        var foam = new Color(1f, 0.97f, 0.88f);
        var label = new Color(0.96f, 0.84f, 0.5f);

        Part(model, "TeamRing", PrimitiveType.Cylinder, new Vector3(0, 0.02f, 0), new Vector3(2.8f, 0.02f, 2.8f), "Hero_Ring_" + team, TeamUtil.GetColor(team));

        // ---------- Бочонок-брюшко (сзади, +Z), задний край приподнят ----------
        var keg = new GameObject("Keg").transform;
        keg.SetParent(m, false);
        keg.localPosition = new Vector3(0, 1.6f, 0.95f);
        keg.localRotation = Quaternion.Euler(72, 0, 0);    // ось бочонка → назад и чуть вверх; «верх» бочонка — локальный -Z
        var wood = MeshPart(keg, "KegBody", SaveMesh("Piv_Keg", ProcMesh.Lathe(ProcMesh.KegProfile, 36)), Vector3.zero, Quaternion.identity, Vector3.one,
            "Piv_KegWood", Color.white, 0f, 0.35f);
        wood.GetComponent<Renderer>().sharedMaterial.SetTexture("_BaseMap", KegWoodTexture());
        foreach (var (y, R) in new[] { (-0.7f, 0.575f), (-0.5f, 0.64f), (0.5f, 0.64f), (0.7f, 0.575f) })
            MeshPart(keg, "Hoop", SaveMesh($"Piv_Hoop_{R:0.000}", ProcMesh.Torus(R, 0.035f, 40, 8)), new Vector3(0, y, 0), Quaternion.identity, Vector3.one,
                "Piv_Iron", iron, 0.85f, 0.7f);
        // кран сзади, снизу
        MeshPart(keg, "Tap", SaveMesh("Piv_Tap", ProcMesh.Tube(new[] { new Vector3(0, 0.7f, 0.3f), new Vector3(0, 0.95f, 0.3f), new Vector3(0, 1.0f, 0.42f), new Vector3(0, 0.98f, 0.5f) },
            new[] { 0.06f, 0.05f, 0.045f, 0.045f }, 10)), Vector3.zero, Quaternion.identity, Vector3.one, "Piv_Iron", iron, 0.85f, 0.7f);
        // пена шапкой из пробки сверху и потёки по бокам
        var rnd = new System.Random(5);
        for (int i = 0; i < 16; i++)
        {
            float ry = ((float)rnd.NextDouble() - 0.5f) * 0.9f, rx = ((float)rnd.NextDouble() - 0.5f) * 0.45f, s = 0.2f + (float)rnd.NextDouble() * 0.18f;
            Shiny(keg.gameObject, "Foam", PrimitiveType.Sphere, Vector3.zero, Vector3.one * s, "Piv_Foam", foam, 0f, 0.6f)
                .transform.localPosition = new Vector3(rx, ry * 0.55f, -0.68f - (float)rnd.NextDouble() * 0.12f);
        }
        foreach (float sd in new[] { -1f, 1f })
            for (int k = 0; k < 3; k++)
            {
                var drip = Shiny(keg.gameObject, "FoamDrip", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.11f, 0.22f - k * 0.05f, 0.08f), "Piv_Foam", foam, 0f, 0.6f);
                float a = Mathf.Deg2Rad * (-90f + sd * (35f + k * 18f));   // по окружности бочонка вниз от «верха»
                drip.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.71f, -0.1f + k * 0.12f * sd, Mathf.Sin(a) * 0.71f);
            }
        // красные пятна, как у паучихи
        foreach (var p in new[] { new Vector3(0.28f, 0.25f, -0.66f), new Vector3(-0.28f, 0.25f, -0.66f), new Vector3(0.18f, 0.5f, -0.6f), new Vector3(-0.18f, 0.5f, -0.6f) })
            Glow(keg.gameObject, "Spot", PrimitiveType.Sphere, p, new Vector3(0.16f, 0.2f, 0.05f), "Piv_Spot", new Color(0.95f, 0.13f, 0.08f));

        // ---------- Торс в чёрной зипке ----------
        var torso = new[] { new Vector2(0f, 0f), new Vector2(0.3f, 0.02f), new Vector2(0.46f, 0.15f), new Vector2(0.53f, 0.4f), new Vector2(0.52f, 0.62f),
                            new Vector2(0.44f, 0.82f), new Vector2(0.28f, 0.95f), new Vector2(0f, 1.0f) };
        MeshPart(m, "Body", SaveMesh("Piv_Torso", ProcMesh.Lathe(torso, 32)), new Vector3(0, 1.1f, -0.25f), Quaternion.identity, new Vector3(1f, 1f, 0.8f),
            "Piv_Hoodie", hoodie, 0f, 0.15f);
        MeshPart(m, "Zip", SaveMesh("Piv_Zip", ProcMesh.Tube(new[] { new Vector3(0, 1.15f, -0.6f), new Vector3(0, 1.5f, -0.675f), new Vector3(0, 1.75f, -0.665f), new Vector3(0, 1.95f, -0.58f) },
            new[] { 0.014f, 0.014f, 0.014f, 0.014f }, 6)), Vector3.zero, Quaternion.identity, Vector3.one, "Hero_Zip", new Color(0.65f, 0.65f, 0.7f));
        var logo = Part(model, "Logo", PrimitiveType.Cylinder, new Vector3(0.19f, 1.72f, -0.655f), new Vector3(0.26f, 0.01f, 0.2f), "Piv_Logo", new Color(0.97f, 0.78f, 0.22f));
        logo.transform.localRotation = Quaternion.Euler(80, 0, 0);
        var logoIn = Part(model, "LogoIn", PrimitiveType.Cylinder, new Vector3(0.19f, 1.72f, -0.665f), new Vector3(0.19f, 0.01f, 0.13f), "Piv_LogoRed", new Color(0.85f, 0.2f, 0.12f));
        logoIn.transform.localRotation = Quaternion.Euler(80, 0, 0);
        // капюшон: валик вокруг шеи и складка сзади
        MeshPart(m, "Hood", SaveMesh("Piv_Hood", ProcMesh.Torus(0.3f, 0.11f, 28, 10)), new Vector3(0, 2.05f, -0.22f), Quaternion.Euler(-15, 0, 0), new Vector3(1f, 1f, 0.9f),
            "Piv_Hoodie", hoodie, 0f, 0.15f);
        Part(model, "HoodBack", PrimitiveType.Sphere, new Vector3(0, 2.02f, 0.06f), new Vector3(0.55f, 0.32f, 0.4f), "Piv_Hoodie", hoodie);
        foreach (float sd in new[] { -1f, 1f })
        {
            MeshPart(m, "String", SaveMesh("Piv_String" + (sd < 0 ? "L" : "R"), ProcMesh.Tube(ProcMesh.Bezier(new Vector3(0.09f * sd, 2.02f, -0.5f), new Vector3(0.1f * sd, 1.92f, -0.6f),
                new Vector3(0.11f * sd, 1.8f, -0.66f), new Vector3(0.12f * sd, 1.7f, -0.67f), 8), ProcMesh.Taper(8, 0.018f, 0.016f), 6)), Vector3.zero, Quaternion.identity, Vector3.one,
                "Piv_String", new Color(0.92f, 0.92f, 0.92f));
            Part(model, "Aglet", PrimitiveType.Capsule, new Vector3(0.12f * sd, 1.66f, -0.67f), new Vector3(0.035f, 0.04f, 0.035f), "Hero_Zip", new Color(0.65f, 0.65f, 0.7f));
        }

        // ---------- Голова по фотке: узкое лицо, густые тёмные лохматые волосы с чёлкой, хмурый ----------
        Part(model, "Neck", PrimitiveType.Cylinder, new Vector3(0, 2.15f, -0.3f), new Vector3(0.2f, 0.1f, 0.2f), "Hero_Skin", skin);
        BuildFace(m, "Head", new Vector3(0, 2.55f, -0.36f), 0.62f, new FaceStyle
        {
            key = "Piv", skin = new Color(0.9f, 0.75f, 0.63f), hair = hair, brow = new Color(0.1f, 0.07f, 0.05f),
            iris = new Color(0.3f, 0.18f, 0.1f), lip = new Color(0.76f, 0.5f, 0.46f),
            width = 0.95f, height = 1.14f, chub = 0.2f, jaw = 0.45f, hairStyle = HairStyle.Messy, hairFront = 0.3f, hairBack = -0.55f, hairVolume = 0.09f, hairNoise = 0.05f,
            eyeSize = 0.95f, eyeOpen = 0.55f, browThick = 0.03f, browAngle = 12f, noseSize = 1.1f, mouth = MouthStyle.Frown, mouthWidth = 0.9f, seed = 6
        });

        // клыки-хелицеры под лицом
        foreach (float sd in new[] { -1f, 1f })
            MeshPart(m, "Fang", SaveMesh("Piv_Fang" + (sd < 0 ? "L" : "R"), ProcMesh.Tube(ProcMesh.Bezier(new Vector3(0.13f * sd, 2.14f, -0.6f), new Vector3(0.16f * sd, 2.02f, -0.72f),
                new Vector3(0.1f * sd, 1.88f, -0.8f), new Vector3(0.06f * sd, 1.8f, -0.74f), 10), ProcMesh.Taper(10, 0.06f, 0f, 1.4f), 8)), Vector3.zero, Quaternion.identity, Vector3.one,
                "Piv_Amber", amber, 0.3f, 0.92f);

        // ---------- 8 ног на шарнирах (LegPivot_L0..R3) ----------
        float[] zs = { -0.5f, -0.22f, 0.06f, 0.34f };
        float[] az = { -38f, -13f, 12f, 36f };     // передние смотрят вперёд, задние назад
        foreach (float sd in new[] { -1f, 1f })
            for (int i = 0; i < 4; i++)
            {
                string key = (sd < 0 ? "L" : "R") + i;
                Vector3 hip = new Vector3(0.42f * sd, 1.55f, zs[i]);
                float a = az[i] * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(sd * Mathf.Cos(a), 0f, Mathf.Sin(a));
                float reach = 1f + (i == 0 || i == 3 ? 0.12f : 0f);
                Vector3 knee = dir * 1.1f * reach + Vector3.up * 1.0f;          // относительно бедра
                Vector3 foot = dir * 2.05f * reach + Vector3.down * 1.5f;

                var pivot = new GameObject("LegPivot_" + key).transform;
                pivot.SetParent(m, false);
                pivot.localPosition = hip;

                var thigh = ProcMesh.Bezier(Vector3.zero, dir * 0.25f + Vector3.up * 0.55f, knee - dir * 0.25f + Vector3.up * 0.15f, knee, 10);
                MeshPart(pivot, "Thigh", SaveMesh("Piv_Thigh_" + key, ProcMesh.Tube(thigh, ProcMesh.Taper(10, 0.14f, 0.11f), 12)), Vector3.zero, Quaternion.identity, Vector3.one,
                    "Piv_Chitin", chitin, 0.35f, 0.85f);
                var shin = ProcMesh.Bezier(knee, knee + dir * 0.3f + Vector3.up * 0.12f, foot + dir * 0.05f + Vector3.up * 0.75f, foot, 16);
                var shinTop = new Vector3[13]; System.Array.Copy(shin, shinTop, 13);
                var shinTip = new Vector3[4]; System.Array.Copy(shin, 12, shinTip, 0, 4);
                MeshPart(pivot, "Shin", SaveMesh("Piv_Shin_" + key, ProcMesh.Tube(shinTop, ProcMesh.Taper(13, 0.11f, 0.055f), 12)), Vector3.zero, Quaternion.identity, Vector3.one,
                    "Piv_Chitin", chitin, 0.35f, 0.85f);
                MeshPart(pivot, "Tip", SaveMesh("Piv_Tip_" + key, ProcMesh.Tube(shinTip, ProcMesh.Taper(4, 0.055f, 0f), 10, false)), Vector3.zero, Quaternion.identity, Vector3.one,
                    "Piv_Amber", amber, 0.3f, 0.92f);
                Shiny(pivot.gameObject, "Knee", PrimitiveType.Sphere, knee, Vector3.one * 0.26f, "Piv_Chitin", chitin, 0.35f, 0.85f);
                // янтарные кольца, как полоски у паучихи
                foreach (var (arr, k, r) in new[] { (thigh, 6, 0.125f), (shin, 4, 0.1f), (shin, 7, 0.088f) })
                {
                    Vector3 tg = (arr[k + 1] - arr[k - 1]).normalized;
                    MeshPart(pivot, "Band", SaveMesh($"Piv_Band_{r:0.000}", ProcMesh.Torus(r, 0.035f, 24, 8)), arr[k], Quaternion.FromToRotation(Vector3.up, tg), Vector3.one,
                        "Piv_Amber", amber, 0.3f, 0.92f);
                }
            }

        // ---------- Передние лапы с бутылками (ArmRPivot / ArmLPivot — ими бьёт) ----------
        var bottleMesh = SaveMesh("Piv_Bottle", ProcMesh.Lathe(ProcMesh.BottleProfile, 24));
        var labelMesh = SaveMesh("Piv_BottleLabel", ProcMesh.Lathe(new[] { new Vector2(0.168f, 0.2f), new Vector2(0.168f, 0.44f) }, 24));
        var capMesh = SaveMesh("Piv_BottleCap", ProcMesh.Torus(0.062f, 0.018f, 20, 6));
        foreach (float sd in new[] { -1f, 1f })
        {
            string s = sd > 0 ? "R" : "L";
            var pivot = new GameObject("Arm" + s + "Pivot").transform;
            pivot.SetParent(m, false);
            pivot.localPosition = new Vector3(0.4f * sd, 1.95f, -0.55f);
            Vector3 hand = new Vector3(0.2f * sd, -0.66f, -0.42f);
            MeshPart(pivot, "Arm" + s, SaveMesh("Piv_Arm" + s, ProcMesh.Tube(ProcMesh.Bezier(Vector3.zero, new Vector3(0.15f * sd, -0.1f, -0.12f),
                new Vector3(0.22f * sd, -0.4f, -0.3f), hand, 10), ProcMesh.Taper(10, 0.1f, 0.07f), 10)), Vector3.zero, Quaternion.identity, Vector3.one, "Piv_Chitin", chitin, 0.35f, 0.85f);
            Shiny(pivot.gameObject, "Hand" + s, PrimitiveType.Sphere, hand, Vector3.one * 0.16f, "Piv_Amber", amber, 0.3f, 0.92f);
            var bottle = new GameObject("Bottle" + s).transform;
            bottle.SetParent(pivot, false);
            bottle.localPosition = hand + new Vector3(0, -0.22f, -0.04f);
            bottle.localRotation = Quaternion.Euler(-12f, 0, 8f * sd);
            bottle.localScale = Vector3.one * 0.78f;
            MeshPart(bottle, "Glass", bottleMesh, Vector3.zero, Quaternion.identity, Vector3.one, "Piv_Glass", glass, 0.1f, 0.95f);
            MeshPart(bottle, "Label", labelMesh, Vector3.zero, Quaternion.identity, Vector3.one, "Piv_Label", label, 0f, 0.3f);
            MeshPart(bottle, "Cap", capMesh, new Vector3(0, 0.975f, 0), Quaternion.identity, Vector3.one, "Piv_Cap", new Color(0.9f, 0.7f, 0.2f), 0.9f, 0.7f);
        }
        return root;
    }

    // Меш из кода → ассет (иначе в сцене он не сохранится)
    static Mesh SaveMesh(string key, Mesh mesh)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Meshes")) AssetDatabase.CreateFolder("Assets", "Meshes");
        if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/Meshes", "Heroes");
        string path = $"{MeshFolder}/{key}.asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old != null)
        {
            // обновляем на месте, чтобы ссылки на меш не сломались
            old.Clear();
            old.SetVertices(mesh.vertices); old.SetNormals(mesh.normals); old.SetUVs(0, mesh.uv); old.SetTangents(mesh.tangents);
            old.SetTriangles(mesh.triangles, 0);
            old.RecalculateBounds();
            EditorUtility.SetDirty(old);
            return old;
        }
        mesh.name = key;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static GameObject MeshPart(Transform parent, string name, Mesh mesh, Vector3 localPos, Quaternion rot, Vector3 scale,
                               string matName, Color color, float metallic = 0f, float smooth = 0.5f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        MapBuilder.Paint(go, matName, color);
        var mat = go.GetComponent<Renderer>().sharedMaterial;
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);
        EditorUtility.SetDirty(mat);
        return go;
    }

    // Доски бочонка: тёплые полосы с тёмными щелями и волокнами
    static Texture2D KegWoodTexture()
    {
        string path = $"{MeshFolder}/Piv_KegWood.asset";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null) return tex;
        const int W = 256, H = 128, Staves = 14;
        tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "Piv_KegWood", wrapMode = TextureWrapMode.Repeat };
        var rnd = new System.Random(9);
        var tone = new float[Staves];
        for (int i = 0; i < Staves; i++) tone[i] = 0.85f + (float)rnd.NextDouble() * 0.3f;
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)W * Staves;
                int st = Mathf.FloorToInt(u);
                float edge = Mathf.Min(u - st, 1f - (u - st));
                float grain = 0.9f + 0.1f * Mathf.Sin(y * 0.35f + st * 7f + Mathf.Sin(x * 0.2f) * 2f);
                float k = tone[st % Staves] * grain * (edge < 0.04f ? 0.45f : 1f);
                px[y * W + x] = new Color(0.55f * k, 0.32f * k, 0.13f * k, 1f);
            }
        tex.SetPixels(px);
        tex.Apply(true);
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }
}
