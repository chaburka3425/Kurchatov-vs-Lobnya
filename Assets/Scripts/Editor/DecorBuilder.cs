using System.Collections.Generic;
using UnityEngine;

// Деревья, кусты, камни, трава и цветы по карте. Формы — гладкие (ProcMesh), меши общие на все копии.
// Плотный лес по краям, рощи между линиями. Ничего не ставим на дороги, в реку, у тронов, бань,
// башен, мест появления крипов и лесных лагерей.
public static class DecorBuilder
{
    static readonly List<Vector2> Keep = BuildKeep();

    static List<Vector2> BuildKeep()
    {
        var k = new List<Vector2>();
        foreach (var p in new[] { LaneData.Throne(Team.Kurchatov), LaneData.Throne(Team.Lobnya), LaneData.W(-36, -36), LaneData.W(36, 36),
                                  LaneData.W(-31, -41), LaneData.W(41, 31), LaneData.W(-38, -38), LaneData.W(38, 38) })
            k.Add(new Vector2(p.x, p.z));
        foreach (var team in new[] { Team.Kurchatov, Team.Lobnya })
        {
            foreach (var t in LaneData.Towers(team)) k.Add(new Vector2(t.pos.x, t.pos.z));
            foreach (var l in LaneData.All) { var p = LaneData.Spawn(team, l); k.Add(new Vector2(p.x, p.z)); }
        }
        foreach (var c in LaneData.Camps) k.Add(new Vector2(c.pos.x, c.pos.z));
        return k;
    }

    // общие меши (по несколько вариантов)
    static Mesh pineTop, pineTrunk, trunk, birchTrunk;
    static Mesh[] crowns, bushes, rocks;
    static Mesh tuft, flower;

    static void MakeMeshes()
    {
        pineTrunk = BuildParts.Save("Decor_PineTrunk", ProcMesh.Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.3f, 0f), new Vector2(0.22f, 0.3f), new Vector2(0.15f, 1.6f), new Vector2(0f, 1.7f) }, 10));
        // три яруса «ёлочкой» одним телом вращения, края зубчатые
        pineTop = BuildParts.Save("Decor_PineTop", ProcMesh.Lathe(new[] { new Vector2(0f, 1.0f), new Vector2(1.45f, 1.05f), new Vector2(1.2f, 1.25f), new Vector2(0.45f, 2.3f),
            new Vector2(1.15f, 2.05f), new Vector2(0.95f, 2.25f), new Vector2(0.3f, 3.3f), new Vector2(0.8f, 3.1f), new Vector2(0.62f, 3.3f), new Vector2(0f, 4.4f) }, 9));
        trunk = BuildParts.Save("Decor_Trunk", ProcMesh.Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.42f, 0f), new Vector2(0.28f, 0.35f), new Vector2(0.22f, 1.8f), new Vector2(0.16f, 2.3f), new Vector2(0f, 2.4f) }, 10));
        birchTrunk = BuildParts.Save("Decor_BirchTrunk", ProcMesh.Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.22f, 0f), new Vector2(0.16f, 0.3f), new Vector2(0.12f, 2.8f), new Vector2(0f, 2.9f) }, 8));
        crowns = new Mesh[3]; bushes = new Mesh[3]; rocks = new Mesh[3];
        for (int i = 0; i < 3; i++)
        {
            int s = i;
            crowns[i] = BuildParts.Save("Decor_Crown" + i, ProcMesh.Blob(d => d * (1.3f + Lumps(d, 5 + s * 2, s) * 0.28f) + Vector3.up * 0.1f * d.y, 24, 18));
            bushes[i] = BuildParts.Save("Decor_Bush" + i, ProcMesh.Blob(d => Vector3.Scale(d * (0.7f + Lumps(d, 6 + s, s + 7) * 0.18f), new Vector3(1.2f, 0.7f, 1.1f)), 18, 12));
            rocks[i] = BuildParts.Save("Decor_Rock" + i, ProcMesh.Blob(d => Vector3.Scale(d * (1f + Lumps(d, 3 + s, s + 13) * 0.22f), new Vector3(1.3f, 0.65f, 1f)), 16, 10));
        }
        // пучок травы: 7 сужающихся травинок одним мешем
        var parts = new List<CombineInstance>();
        var r = new System.Random(4);
        for (int i = 0; i < 7; i++)
        {
            float a = i * 0.9f, lean = 0.15f + (float)r.NextDouble() * 0.25f, h = 0.35f + (float)r.NextDouble() * 0.3f;
            Vector3 b = new Vector3(Mathf.Cos(a) * 0.06f, 0, Mathf.Sin(a) * 0.06f);
            Vector3 dir = new Vector3(Mathf.Cos(a) * lean, 1f, Mathf.Sin(a) * lean).normalized;
            var m = ProcMesh.Tube(ProcMesh.Bezier(b, b + dir * h * 0.4f, b + dir * h * 0.8f + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.08f, b + dir * h, 5), ProcMesh.Taper(5, 0.025f, 0f), 4, false);
            parts.Add(new CombineInstance { mesh = m, transform = Matrix4x4.identity });
        }
        var t = new Mesh(); t.CombineMeshes(parts.ToArray(), true, true); t.RecalculateBounds();
        tuft = BuildParts.Save("Decor_Tuft", t);
        flower = BuildParts.Save("Decor_Flower", ProcMesh.Lathe(new[] { new Vector2(0f, 0.32f), new Vector2(0.09f, 0.34f), new Vector2(0.05f, 0.38f), new Vector2(0f, 0.37f) }, 6));
    }

    // Комочки на поверхности (крона, камень)
    static float Lumps(Vector3 d, int f, int seed) =>
        Mathf.Sin(d.x * f + seed) * Mathf.Sin(d.y * f * 0.8f + seed * 1.7f) * Mathf.Sin(d.z * f * 1.1f + seed * 0.6f) + Mathf.Sin((d.x + d.z) * f * 0.5f + seed) * 0.4f;

    public static void Build()
    {
        MakeMeshes();
        var parent = new GameObject("Decor").transform;
        // трава и цветы не мешают ходить: в NavMesh не попадают
        var soft = new GameObject("DecorSoft").transform;
        soft.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;
        var rnd = new System.Random(2026);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        float edge = LaneData.Half - 3f;

        // Плотный лес по краям карты
        for (float t = -edge; t <= edge; t += 3.3f)
        {
            TryTree(parent, new Vector2(t + R(-1, 1), -edge + R(-1, 1.5f)), rnd);
            TryTree(parent, new Vector2(t + R(-1, 1), edge + R(-1.5f, 1)), rnd);
            TryTree(parent, new Vector2(-edge + R(-1, 1.5f), t + R(-1, 1)), rnd);
            TryTree(parent, new Vector2(edge + R(-1.5f, 1), t + R(-1, 1)), rnd);
        }
        // Рощи внутри карты (лес между линиями)
        float inner = LaneData.Half - 7f;
        int A(int n) => Mathf.RoundToInt(n * LaneData.Scale * LaneData.Scale / 1.96f);   // больше карта — больше деревьев
        for (int i = 0; i < A(620); i++) TryTree(parent, new Vector2(R(-inner, inner), R(-inner, inner)), rnd);
        // Кусты, камни, трава и цветы
        for (int i = 0; i < A(220); i++) { var p = new Vector2(R(-inner, inner), R(-inner, inner)); if (Free(p, 6.5f, 7f)) Bush(parent, p, rnd); }
        for (int i = 0; i < A(90); i++) { var p = new Vector2(R(-inner, inner), R(-inner, inner)); if (Free(p, 7f, 8f)) Rock(parent, p, rnd); }
        var grassMats = new[] { ("Decor_Grass0", new Color(0.3f, 0.55f, 0.22f)), ("Decor_Grass1", new Color(0.38f, 0.62f, 0.26f)), ("Decor_Grass2", new Color(0.26f, 0.48f, 0.2f)) };
        for (int i = 0; i < A(1100); i++)
        {
            var p = new Vector2(R(-edge, edge), R(-edge, edge));
            if (LaneData.OnAnyLane(p.x, p.y, LaneData.LaneHalfWidth + 0.6f) || InRiver(p, 0.5f) || NearKeep(p, 5f)) continue;
            var (m, c) = grassMats[i % 3];
            BuildParts.Mesh(soft, "Grass", tuft, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, R(0, 360), 0), Vector3.one * R(0.8f, 1.6f), m, c, 0f, 0.3f);
        }
        var flowerMats = new[] { ("Decor_FlowerW", new Color(0.97f, 0.97f, 0.92f)), ("Decor_FlowerY", new Color(1f, 0.85f, 0.25f)), ("Decor_FlowerP", new Color(0.85f, 0.45f, 0.85f)) };
        for (int i = 0; i < A(260); i++)
        {
            var p = new Vector2(R(-edge, edge), R(-edge, edge));
            if (LaneData.OnAnyLane(p.x, p.y, LaneData.LaneHalfWidth + 0.8f) || InRiver(p, 0.8f) || NearKeep(p, 5f)) continue;
            var (m, c) = flowerMats[i % 3];
            BuildParts.Mesh(soft, "Flower", tuft, new Vector3(p.x, 0, p.y), Quaternion.Euler(0, R(0, 360), 0), Vector3.one * 0.7f, "Decor_Grass0", new Color(0.3f, 0.55f, 0.22f), 0f, 0.3f);
            BuildParts.Mesh(soft, "Bloom", flower, new Vector3(p.x, 0, p.y), Quaternion.identity, Vector3.one * R(0.9f, 1.3f), m, c, 0f, 0.5f);
        }
    }

    static bool InRiver(Vector2 p, float extra) => Mathf.Abs(p.x + p.y) / 1.4142f < MapLookBuilder.RiverHalfWidth + 2.5f + extra;
    static bool NearKeep(Vector2 p, float r) { foreach (var k in Keep) if (Vector2.Distance(p, k) < r) return true; return false; }

    static bool Free(Vector2 p, float laneHalfWidth, float keep = 9f)
    {
        if (LaneData.OnAnyLane(p.x, p.y, laneHalfWidth)) return false;   // дороги
        if (InRiver(p, 1f)) return false;                                // река
        return !NearKeep(p, keep);
    }

    static void TryTree(Transform parent, Vector2 p, System.Random rnd)
    {
        if (!Free(p, 9.5f)) return;
        double r = rnd.NextDouble();
        if (r < 0.45) Pine(parent, p, rnd); else if (r < 0.85) Round(parent, p, rnd); else Birch(parent, p, rnd);
    }

    static Transform Root(Transform parent, string name, Vector2 p, System.Random rnd, float scale)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.position = new Vector3(p.x, 0, p.y);
        root.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
        root.localScale = Vector3.one * scale;
        return root;
    }

    static void Pine(Transform parent, Vector2 p, System.Random rnd)
    {
        var root = Root(parent, "Pine", p, rnd, 0.85f + (float)rnd.NextDouble() * 0.55f);
        BuildParts.Mesh(root, "Trunk", pineTrunk, Vector3.zero, Quaternion.identity, Vector3.one, "Decor_Trunk", new Color(0.38f, 0.24f, 0.13f), 0f, 0.2f);
        int v = rnd.Next(3);
        BuildParts.Mesh(root, "Needles", pineTop, Vector3.zero, Quaternion.identity, Vector3.one, "Decor_Pine" + v,
            v == 0 ? new Color(0.1f, 0.32f, 0.16f) : v == 1 ? new Color(0.13f, 0.38f, 0.18f) : new Color(0.09f, 0.28f, 0.17f), 0f, 0.25f);
    }

    static void Round(Transform parent, Vector2 p, System.Random rnd)
    {
        var root = Root(parent, "Tree", p, rnd, 0.8f + (float)rnd.NextDouble() * 0.5f);
        BuildParts.Mesh(root, "Trunk", trunk, Vector3.zero, Quaternion.identity, Vector3.one, "Decor_Trunk", new Color(0.38f, 0.24f, 0.13f), 0f, 0.2f);
        int v = rnd.Next(3);
        var leaf = v == 0 ? new Color(0.22f, 0.5f, 0.2f) : v == 1 ? new Color(0.28f, 0.56f, 0.22f) : new Color(0.35f, 0.55f, 0.18f);
        BuildParts.Mesh(root, "Crown", crowns[v], new Vector3(0, 3.0f, 0), Quaternion.identity, Vector3.one, "Decor_Leaves" + v, leaf, 0f, 0.3f);
        BuildParts.Mesh(root, "Crown2", crowns[(v + 1) % 3], new Vector3(0.7f, 3.5f, 0.3f), Quaternion.identity, Vector3.one * 0.65f, "Decor_Leaves" + v, leaf, 0f, 0.3f);
    }

    static void Birch(Transform parent, Vector2 p, System.Random rnd)
    {
        var root = Root(parent, "Birch", p, rnd, 0.85f + (float)rnd.NextDouble() * 0.4f);
        BuildParts.Mesh(root, "Trunk", birchTrunk, Vector3.zero, Quaternion.identity, Vector3.one, "Decor_Birch", new Color(0.92f, 0.9f, 0.86f), 0f, 0.3f);
        for (int i = 0; i < 4; i++)   // чёрные чёрточки на коре
            BuildParts.Prim(root, "Mark", PrimitiveType.Cube, new Vector3(0, 0.5f + i * 0.55f, -0.13f), new Vector3(0.12f, 0.04f, 0.04f), "Decor_BirchMark", new Color(0.1f, 0.1f, 0.1f), 0f, 0.2f,
                Quaternion.Euler(0, i * 70f, 0));
        BuildParts.Mesh(root, "Crown", crowns[rnd.Next(3)], new Vector3(0, 3.3f, 0), Quaternion.identity, new Vector3(0.75f, 1.05f, 0.75f), "Decor_BirchLeaves", new Color(0.45f, 0.66f, 0.25f), 0f, 0.3f);
    }

    static void Bush(Transform parent, Vector2 p, System.Random rnd)
    {
        var root = Root(parent, "Bush", p, rnd, 0.7f + (float)rnd.NextDouble() * 0.7f);
        int v = rnd.Next(3);
        BuildParts.Mesh(root, "Bush", bushes[v], new Vector3(0, 0.35f, 0), Quaternion.identity, Vector3.one, "Decor_Bush" + v,
            v == 0 ? new Color(0.2f, 0.45f, 0.18f) : v == 1 ? new Color(0.25f, 0.5f, 0.2f) : new Color(0.18f, 0.4f, 0.2f), 0f, 0.3f);
        if (rnd.NextDouble() < 0.4)   // ягоды
            for (int i = 0; i < 4; i++)
                BuildParts.Prim(root, "Berry", PrimitiveType.Sphere, new Vector3((float)rnd.NextDouble() - 0.5f, 0.5f + (float)rnd.NextDouble() * 0.25f, (float)rnd.NextDouble() - 0.5f) * 0.9f,
                    Vector3.one * 0.12f, "Decor_Berry", new Color(0.85f, 0.12f, 0.15f), 0f, 0.7f);
    }

    static void Rock(Transform parent, Vector2 p, System.Random rnd)
    {
        var root = Root(parent, "Rock", p, rnd, 0.7f + (float)rnd.NextDouble() * 1.3f);
        int v = rnd.Next(3);
        BuildParts.Mesh(root, "Stone", rocks[v], new Vector3(0, 0.15f, 0), Quaternion.Euler((float)rnd.NextDouble() * 12f, 0, (float)rnd.NextDouble() * 12f), Vector3.one,
            "Decor_Rock" + v, v == 0 ? new Color(0.46f, 0.45f, 0.43f) : v == 1 ? new Color(0.52f, 0.5f, 0.47f) : new Color(0.4f, 0.4f, 0.42f), 0f, 0.25f);
        if (rnd.NextDouble() < 0.5)   // мох сверху
            BuildParts.Mesh(root, "Moss", bushes[v], new Vector3(0.1f, 0.62f, 0), Quaternion.identity, new Vector3(0.7f, 0.25f, 0.6f), "Decor_Moss", new Color(0.3f, 0.48f, 0.22f), 0f, 0.2f);
    }
}
