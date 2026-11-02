using UnityEngine;

// Общие гладкие детали для моделей героев: трубки, тела вращения, бублики, руки и ноги на шарнирах.
// Шарниры: ArmRPivot / ArmLPivot (плечи — ими бьёт и колдует), HipPivot_L / HipPivot_R (ноги — шагают).
public static partial class HeroBuilder
{
    static Transform Pivot(Transform parent, string name, Vector3 pos, Quaternion? rot = null)
    {
        var p = new GameObject(name).transform;
        p.SetParent(parent, false);
        p.localPosition = pos;
        p.localRotation = rot ?? Quaternion.identity;
        return p;
    }

    static GameObject TubePart(Transform parent, string name, string key, Vector3[] path, float[] radii, string mat, Color c,
                               float metal = 0f, float smooth = 0.4f, int sides = 12, bool cap = true) =>
        MeshPart(parent, name, SaveMesh(key, ProcMesh.Tube(path, radii, sides, cap)), Vector3.zero, Quaternion.identity, Vector3.one, mat, c, metal, smooth);

    static GameObject LathePart(Transform parent, string name, string key, Vector2[] profile, Vector3 pos, Quaternion rot, Vector3 scale,
                                string mat, Color c, float metal = 0f, float smooth = 0.4f, int sides = 28) =>
        MeshPart(parent, name, SaveMesh(key, ProcMesh.Lathe(profile, sides)), pos, rot, scale, mat, c, metal, smooth);

    static GameObject TorusPart(Transform parent, string name, string key, float R, float r, Vector3 pos, Quaternion rot, Vector3 scale,
                                string mat, Color c, float metal = 0f, float smooth = 0.4f) =>
        MeshPart(parent, name, SaveMesh(key, ProcMesh.Torus(R, r, 32, 10)), pos, rot, scale, mat, c, metal, smooth);

    static GameObject Ball(Transform parent, string name, Vector3 pos, Vector3 scale, string mat, Color c, float metal = 0f, float smooth = 0.4f) =>
        Shiny(parent.gameObject, name, PrimitiveType.Sphere, pos, scale, mat, c, metal, smooth);

    // Плавная кривая через среднюю точку (плечо → локоть → кисть, бедро → колено → щиколотка)
    static Vector3[] Through(Vector3 a, Vector3 b, Vector3 c, int n) =>
        ProcMesh.Bezier(a, Vector3.Lerp(a, b, 0.5f) + (b - Vector3.Lerp(a, c, 0.5f)) * 0.9f, Vector3.Lerp(c, b, 0.5f) + (b - Vector3.Lerp(a, c, 0.5f)) * 0.9f, c, n);

    // Тот же путь, сдвинутый вбок (лампасы, швы)
    static Vector3[] Offset(Vector3[] path, Vector3 by)
    {
        var r = new Vector3[path.Length];
        for (int i = 0; i < path.Length; i++) r[i] = path[i] + by;
        return r;
    }

    // Рука на шарнире в плече. Точки — в координатах модели. Возвращает шарнир; hand — кисть в координатах шарнира.
    static Transform Arm(Transform m, string prefix, string side, Vector3 shoulder, Vector3 elbow, Vector3 wrist,
                         float r0, float r1, string mat, Color c, float metal, float smooth, Color skin, float handSize, out Vector3 hand)
    {
        var p = Pivot(m, "Arm" + side + "Pivot", shoulder);
        var path = Through(Vector3.zero, elbow - shoulder, wrist - shoulder, 12);
        TubePart(p, "Arm" + side, $"{prefix}_Arm{side}", path, ProcMesh.Taper(12, r0, r1), mat, c, metal, smooth);
        Ball(p, "Shoulder" + side, Vector3.zero, Vector3.one * r0 * 2.1f, mat, c, metal, smooth);
        hand = wrist - shoulder + (wrist - elbow).normalized * handSize * 0.35f;
        // свой материал кожи у каждого героя (общий Hero_Skin перекрашивался последним собранным)
        Ball(p, "Hand" + side, hand, new Vector3(handSize, handSize * 1.1f, handSize), prefix + "_Hand", skin, 0f, 0.35f);
        return p;
    }

    // Нога на шарнире в бедре + кроссовок с подошвой
    static Transform Leg(Transform m, string prefix, string side, Vector3 hip, Vector3 knee, Vector3 ankle, float r0, float r1,
                         string mat, Color c, float metal, float smooth, string shoeMat, Color shoe, Color sole, float shoeScale = 1f)
    {
        var p = Pivot(m, "HipPivot_" + side, hip);
        var path = Through(Vector3.zero, knee - hip, ankle - hip, 12);
        TubePart(p, "Leg" + side, $"{prefix}_Leg{side}", path, ProcMesh.Taper(12, r0, r1), mat, c, metal, smooth);
        Vector3 a = ankle - hip;
        float s = shoeScale;
        Ball(p, "Sneaker", a + new Vector3(0, -0.06f * s, -0.12f * s), new Vector3(0.36f, 0.24f, 0.64f) * s, shoeMat, shoe, 0.1f, 0.55f);
        Ball(p, "SneakerToe", a + new Vector3(0, -0.1f * s, -0.36f * s), new Vector3(0.3f, 0.17f, 0.28f) * s, shoeMat, shoe, 0.1f, 0.55f);
        Ball(p, "Sole", a + new Vector3(0, -0.17f * s, -0.14f * s), new Vector3(0.38f, 0.07f, 0.7f) * s, "Hero_Sole", sole, 0f, 0.2f);
        return p;
    }

    // Мягкий «стёганый» торс: радиус волнами (пуховик) или гладкий
    static Vector2[] TorsoProfile(float hBottom, float hTop, float rBottom, float rMid, float rShoulder, int quilts, float quiltDepth)
    {
        const int N = 40;
        var pts = new Vector2[N + 2];
        pts[0] = new Vector2(0f, hBottom);
        for (int i = 0; i <= N; i++)
        {
            float t = i / (float)N, y = Mathf.Lerp(hBottom, hTop, t);
            float r = t < 0.5f ? Mathf.Lerp(rBottom, rMid, Mathf.SmoothStep(0, 1, t * 2f)) : Mathf.Lerp(rMid, rShoulder, Mathf.SmoothStep(0, 1, (t - 0.5f) * 2f));
            if (t > 0.88f) r *= Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((t - 0.88f) / 0.12f, 2f))) * 0.7f + 0.3f;   // плечи скругляются к шее
            if (quilts > 0) r += quiltDepth * Mathf.Abs(Mathf.Sin(t * quilts * Mathf.PI));
            pts[i + 1] = new Vector2(Mathf.Max(0.001f, r), y);
        }
        pts[N + 1] = new Vector2(0f, hTop);
        return pts;
    }

    // Шипы/пряди: конус-трубка от точки в направлении
    static void Spike(Transform parent, string name, string key, Vector3 from, Vector3 dir, float len, float r, string mat, Color c, float bend = 0.15f)
    {
        Vector3 side = Vector3.Cross(dir, Vector3.up).sqrMagnitude > 0.01f ? Vector3.Cross(dir, Vector3.up).normalized : Vector3.right;
        var path = ProcMesh.Bezier(from, from + dir * len * 0.35f, from + dir * len * 0.7f + side * bend * len - Vector3.up * bend * len * 0.3f, from + dir * len, 6);
        TubePart(parent, name, key, path, ProcMesh.Taper(6, r, 0f, 1.2f), mat, c, 0f, 0.35f, 7);
    }
}
