using System.Collections.Generic;
using UnityEngine;

// Материалы, которые создаются прямо в игре (для эффектов и моделей, собранных на лету).
public static class RuntimeMats
{
    static readonly Dictionary<Color, Material> lit = new Dictionary<Color, Material>();
    static readonly Dictionary<Color, Material> glow = new Dictionary<Color, Material>();

    public static Material Lit(Color c)
    {
        c.a = 1f;
        if (lit.TryGetValue(c, out var m) && m != null) return m;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        lit[c] = m;
        return m;
    }

    // Светящийся (без освещения) — огонь, глаза
    public static Material Glow(Color c)
    {
        c.a = 1f;
        if (glow.TryGetValue(c, out var m) && m != null) return m;
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        glow[c] = m;
        return m;
    }

    public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.Destroy(go.GetComponent<Collider>());
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // Сосиска: капсула с концами потемнее
    public static readonly Color SausageColor = new Color(0.66f, 0.27f, 0.17f);
    public static GameObject Sausage(Transform parent, Vector3 localPos, float size)
    {
        var root = new GameObject("Sausage").transform;
        if (parent != null) root.SetParent(parent, false);
        root.localPosition = localPos;
        var body = Prim(PrimitiveType.Capsule, root, Vector3.zero, new Vector3(0.26f, 0.42f, 0.26f) * size, Lit(SausageColor));
        body.transform.localRotation = Quaternion.Euler(90, 0, 0);
        return root.gameObject;
    }
}
