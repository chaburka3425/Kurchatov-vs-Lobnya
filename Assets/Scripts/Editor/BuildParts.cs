using UnityEditor;
using UnityEngine;

// Гладкие детали для построек (башни, троны, баня): тела вращения, трубки, бублики, светящиеся части.
// Меши сохраняются в Assets/Meshes/Structures, материалы — через MapBuilder.Paint.
public static class BuildParts
{
    const string MeshFolder = "Assets/Meshes/Structures";
    const string MatFolder = "Assets/Materials";

    public static Mesh Save(string key, Mesh mesh)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Meshes")) AssetDatabase.CreateFolder("Assets", "Meshes");
        if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/Meshes", "Structures");
        string path = $"{MeshFolder}/{key}.asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old != null)
        {
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

    public static GameObject Mesh(Transform parent, string name, Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, string mat, Color c, float metal = 0f, float smooth = 0.4f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        MapBuilder.Paint(go, mat, c);
        Finish(go, metal, smooth);
        return go;
    }

    public static GameObject Lathe(Transform p, string name, string key, Vector2[] profile, Vector3 pos, Vector3 scale, string mat, Color c, float metal = 0f, float smooth = 0.4f, int sides = 32, Quaternion? rot = null) =>
        Mesh(p, name, Save(key, ProcMesh.Lathe(profile, sides)), pos, rot ?? Quaternion.identity, scale, mat, c, metal, smooth);

    public static GameObject Torus(Transform p, string name, string key, float R, float r, Vector3 pos, Quaternion rot, Vector3 scale, string mat, Color c, float metal = 0f, float smooth = 0.4f) =>
        Mesh(p, name, Save(key, ProcMesh.Torus(R, r, 36, 10)), pos, rot, scale, mat, c, metal, smooth);

    public static GameObject Tube(Transform p, string name, string key, Vector3[] path, float[] radii, string mat, Color c, float metal = 0f, float smooth = 0.4f, int sides = 10) =>
        Mesh(p, name, Save(key, ProcMesh.Tube(path, radii, sides)), Vector3.zero, Quaternion.identity, Vector3.one, mat, c, metal, smooth);

    public static GameObject Prim(Transform p, string name, PrimitiveType type, Vector3 pos, Vector3 scale, string mat, Color c, float metal = 0f, float smooth = 0.4f, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos; go.transform.localScale = scale; go.transform.localRotation = rot ?? Quaternion.identity;
        MapBuilder.Paint(go, mat, c);
        Finish(go, metal, smooth);
        return go;
    }

    // Светится сам (окна, неон, кристаллы, огонь) — материал без освещения
    public static GameObject Glow(Transform p, string name, PrimitiveType type, Vector3 pos, Vector3 scale, string mat, Color c, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos; go.transform.localScale = scale; go.transform.localRotation = rot ?? Quaternion.identity;
        go.GetComponent<Renderer>().sharedMaterial = GlowMat(mat, c);
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    public static GameObject GlowMesh(Transform p, string name, Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, string mat, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = GlowMat(mat, c);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    public static Material GlowMat(string mat, Color c)
    {
        string path = $"{MatFolder}/{mat}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Finish(GameObject go, float metal, float smooth)
    {
        var mat = go.GetComponent<Renderer>().sharedMaterial;
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metal);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(mat);
    }

    public static void Ambient(Transform p, string name, Vector3 pos, AmbientFx.Kind kind, Color c, float interval, float radius, int count)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        var a = go.AddComponent<AmbientFx>();
        a.kind = kind; a.color = c; a.interval = interval; a.radius = radius; a.count = count;
    }

    // Надпись (вывески)
    public static GameObject Text(Transform p, string text, Vector3 pos, Color color, float charSize, Quaternion? rot = null)
    {
        var go = new GameObject("SignText");
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text; tm.fontSize = 60; tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.fontStyle = FontStyle.Bold; tm.color = color;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) { tm.font = font; go.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
        return go;
    }

    // Фонарь на столбе со светящимся плафоном
    public static void Lamp(Transform p, string key, Vector3 pos, Color glow, float h = 2.6f)
    {
        var dark = new Color(0.12f, 0.12f, 0.14f);
        Lathe(p, "LampPost", "LampPost", new[] { new Vector2(0f, 0f), new Vector2(0.14f, 0f), new Vector2(0.1f, 0.08f), new Vector2(0.05f, 0.2f), new Vector2(0.045f, 1f), new Vector2(0f, 1f) },
            pos, new Vector3(1f, h, 1f), "Lamp_Iron", dark, 0.6f, 0.6f, 12);
        Glow(p, "LampGlow", PrimitiveType.Sphere, pos + Vector3.up * (h + 0.12f), Vector3.one * 0.32f, "Lamp_" + key, glow);
        Lathe(p, "LampCap", "LampCap", new[] { new Vector2(0.24f, 0f), new Vector2(0.12f, 0.12f), new Vector2(0f, 0.16f) }, pos + Vector3.up * (h + 0.25f), Vector3.one, "Lamp_Iron", dark, 0.6f, 0.6f, 12);
    }
}
