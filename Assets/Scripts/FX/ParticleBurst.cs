using System.Collections.Generic;
using UnityEngine;

// Простые частицы без настроек: разлетающиеся кубики при попаданиях, смерти, покупке и т.п.
public class ParticleBurst : MonoBehaviour
{
    static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
    static Shader shader;

    Vector3 velocity;
    float life, maxLife;
    Vector3 startScale;

    public static void Spawn(Vector3 pos, Color color, int count, float speed = 4f)
    {
        var mat = GetMaterial(color);
        for (int i = 0; i < count; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Particle";
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.transform.position = pos;
            go.transform.rotation = Random.rotation;
            float sz = Random.Range(0.08f, 0.2f);
            go.transform.localScale = Vector3.one * sz;

            var p = go.AddComponent<ParticleBurst>();
            p.velocity = Random.onUnitSphere * Random.Range(speed * 0.4f, speed) + Vector3.up * speed * 0.6f;
            p.maxLife = p.life = Random.Range(0.35f, 0.7f);
            p.startScale = go.transform.localScale;
        }
    }

    static Material GetMaterial(Color c)
    {
        if (materials.TryGetValue(c, out var m) && m != null) return m;
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        materials[c] = m;
        return m;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }
        velocity += Vector3.down * 12f * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.localScale = startScale * (life / maxLife);
    }
}
