using System.Collections.Generic;
using UnityEngine;

// Гладкие формы из кода вместо кубиков и капсул: тела вращения (бутылка, бочонок, торс),
// изогнутые сужающиеся трубки (ноги, лапы, клыки) и бублики (обручи, крышки).
// Работает и в редакторе (билдеры сохраняют меши в Assets/Meshes), и в игре.
public static class ProcMesh
{
    // Тело вращения вокруг оси Y. profile — точки (радиус, высота) снизу вверх.
    // Если первая/последняя точка с радиусом 0 — получается закрытый конец.
    public static Mesh Lathe(Vector2[] profile, int sides = 28)
    {
        int n = profile.Length;
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        // длина профиля — для UV по вертикали
        var len = new float[n];
        for (int i = 1; i < n; i++) len[i] = len[i - 1] + Vector2.Distance(profile[i], profile[i - 1]);
        float total = Mathf.Max(0.0001f, len[n - 1]);

        for (int i = 0; i < n; i++)
        {
            // нормаль профиля: перпендикуляр к касательной (центральная разность)
            Vector2 a = profile[Mathf.Max(0, i - 1)], b = profile[Mathf.Min(n - 1, i + 1)];
            Vector2 t = (b - a).normalized;
            Vector2 pn = new Vector2(t.y, -t.x);
            if (profile[i].x <= 0.0001f) pn = new Vector2(0f, i == 0 ? -1f : 1f);
            for (int s = 0; s <= sides; s++)
            {
                float ang = s * Mathf.PI * 2f / sides;
                float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                verts.Add(new Vector3(profile[i].x * c, profile[i].y, profile[i].x * sn));
                norms.Add(new Vector3(pn.x * c, pn.y, pn.x * sn).normalized);
                uvs.Add(new Vector2((float)s / sides, len[i] / total));
            }
        }
        int row = sides + 1;
        for (int i = 0; i < n - 1; i++)
            for (int s = 0; s < sides; s++)
            {
                int v0 = i * row + s, v1 = v0 + 1, v2 = v0 + row, v3 = v2 + 1;
                tris.Add(v0); tris.Add(v2); tris.Add(v1);
                tris.Add(v1); tris.Add(v2); tris.Add(v3);
            }
        return Build("Lathe", verts, norms, uvs, tris);
    }

    // Трубка вдоль пути path, радиус по точкам radii (можно сужать до 0 — острый кончик).
    public static Mesh Tube(Vector3[] path, float[] radii, int sides = 12, bool capStart = true)
    {
        int n = path.Length;
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        // рамки без перекручивания (parallel transport)
        var tang = new Vector3[n];
        for (int i = 0; i < n; i++) tang[i] = (path[Mathf.Min(n - 1, i + 1)] - path[Mathf.Max(0, i - 1)]).normalized;
        Vector3 nrm = Vector3.Cross(tang[0], Mathf.Abs(tang[0].y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        float acc = 0f;
        for (int i = 0; i < n; i++)
        {
            if (i > 0)
            {
                nrm = Vector3.ProjectOnPlane(nrm, tang[i]).normalized;
                acc += Vector3.Distance(path[i], path[i - 1]);
            }
            Vector3 bin = Vector3.Cross(tang[i], nrm);
            for (int s = 0; s <= sides; s++)
            {
                float ang = s * Mathf.PI * 2f / sides;
                Vector3 dir = nrm * Mathf.Cos(ang) + bin * Mathf.Sin(ang);
                verts.Add(path[i] + dir * radii[i]);
                norms.Add(dir);
                uvs.Add(new Vector2((float)s / sides, acc));
            }
        }
        int row = sides + 1;
        for (int i = 0; i < n - 1; i++)
            for (int s = 0; s < sides; s++)
            {
                int v0 = i * row + s, v1 = v0 + 1, v2 = v0 + row, v3 = v2 + 1;
                tris.Add(v0); tris.Add(v1); tris.Add(v2);
                tris.Add(v1); tris.Add(v3); tris.Add(v2);
            }
        // круглая заглушка в начале
        if (capStart && radii[0] > 0f)
        {
            int c = verts.Count;
            verts.Add(path[0]); norms.Add(-tang[0]); uvs.Add(Vector2.zero);
            for (int s = 0; s < sides; s++) { tris.Add(c); tris.Add(s + 1); tris.Add(s); }
        }
        return Build("Tube", verts, norms, uvs, tris);
    }

    // Бублик в плоскости XZ (ось — Y): обручи бочонка, кольца на ногах, крышки
    public static Mesh Torus(float R, float r, int segs = 32, int sides = 10)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int i = 0; i <= segs; i++)
        {
            float a = i * Mathf.PI * 2f / segs;
            Vector3 c = new Vector3(Mathf.Cos(a) * R, 0f, Mathf.Sin(a) * R);
            Vector3 outDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (int s = 0; s <= sides; s++)
            {
                float b = s * Mathf.PI * 2f / sides;
                Vector3 d = outDir * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                verts.Add(c + d * r); norms.Add(d); uvs.Add(new Vector2((float)i / segs, (float)s / sides));
            }
        }
        int row = sides + 1;
        for (int i = 0; i < segs; i++)
            for (int s = 0; s < sides; s++)
            {
                int v0 = i * row + s, v1 = v0 + 1, v2 = v0 + row, v3 = v2 + 1;
                tris.Add(v0); tris.Add(v1); tris.Add(v2);
                tris.Add(v1); tris.Add(v3); tris.Add(v2);
            }
        return Build("Torus", verts, norms, uvs, tris);
    }

    // «Комок»: сфера, каждую точку которой можно сдвинуть (голова, причёска, череп).
    // shape(dir) → позиция для направления dir с единичной сферы. Нормали сглажены и без шва.
    public static Mesh Blob(System.Func<Vector3, Vector3> shape, int lon = 40, int lat = 30)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int j = 0; j <= lat; j++)
        {
            float th = Mathf.PI * j / lat;               // 0 — верх, PI — низ
            for (int i = 0; i <= lon; i++)
            {
                float ph = 2f * Mathf.PI * i / lon;
                var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                verts.Add(shape(d));
                uvs.Add(new Vector2((float)i / lon, 1f - (float)j / lat));
            }
        }
        int row = lon + 1;
        for (int j = 0; j < lat; j++)
            for (int i = 0; i < lon; i++)
            {
                int v0 = j * row + i, v1 = v0 + 1, v2 = v0 + row, v3 = v2 + 1;
                tris.Add(v0); tris.Add(v1); tris.Add(v2);
                tris.Add(v1); tris.Add(v3); tris.Add(v2);
            }
        var m = new Mesh { name = "Blob" };
        m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        // сглаживаем шов и полюса: у совпадающих точек — общая нормаль
        var n = m.normals;
        var groups = new Dictionary<Vector3Int, Vector3>();
        System.Func<Vector3, Vector3Int> keyOf = p => new Vector3Int(Mathf.RoundToInt(p.x * 10000f), Mathf.RoundToInt(p.y * 10000f), Mathf.RoundToInt(p.z * 10000f));
        for (int i = 0; i < verts.Count; i++) { var k = keyOf(verts[i]); groups[k] = (groups.TryGetValue(k, out var s) ? s : Vector3.zero) + n[i]; }
        for (int i = 0; i < verts.Count; i++) n[i] = groups[keyOf(verts[i])].normalized;
        m.normals = n;
        m.RecalculateBounds();
        m.RecalculateTangents();
        return m;
    }

    // Кривая Безье из 4 точек → n точек пути
    public static Vector3[] Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int n)
    {
        var pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1), u = 1f - t;
            pts[i] = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }
        return pts;
    }

    // Радиусы, плавно меняющиеся от r0 к r1 (pow — как быстро сужается)
    public static float[] Taper(int n, float r0, float r1, float pow = 1f)
    {
        var r = new float[n];
        for (int i = 0; i < n; i++) r[i] = Mathf.Lerp(r0, r1, Mathf.Pow(i / (float)(n - 1), pow));
        return r;
    }

    // ---------- Готовые профили ----------
    // Пивная бутылка высотой 1 (донышко — y = 0)
    public static readonly Vector2[] BottleProfile =
    {
        new(0f, 0f), new(0.14f, 0f), new(0.16f, 0.02f), new(0.165f, 0.06f), new(0.165f, 0.5f), new(0.158f, 0.57f),
        new(0.13f, 0.64f), new(0.09f, 0.7f), new(0.062f, 0.76f), new(0.056f, 0.8f), new(0.056f, 0.93f),
        new(0.068f, 0.945f), new(0.068f, 0.975f), new(0.05f, 0.99f), new(0f, 1f)
    };

    // Бочонок с пузом: длина 1.5 вдоль Y, центр в 0
    public static readonly Vector2[] KegProfile =
    {
        new(0f, -0.76f), new(0.5f, -0.76f), new(0.56f, -0.72f), new(0.62f, -0.55f), new(0.68f, -0.3f),
        new(0.7f, 0f), new(0.68f, 0.3f), new(0.62f, 0.55f), new(0.56f, 0.72f), new(0.5f, 0.76f), new(0f, 0.76f)
    };

    static Mesh bottle;
    public static Mesh Bottle => bottle != null ? bottle : (bottle = Lathe(BottleProfile, 20));

    static Mesh Build(string name, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
    {
        var m = new Mesh { name = name };
        if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        m.RecalculateTangents();
        return m;
    }
}
