using System.Collections.Generic;
using UnityEngine;

// Красивые эффекты без готовых ассетов: расходящиеся кольца, столбы света, вспышки,
// мерцающие искры, монеты, летящие к герою, и метка клика. Всё полупрозрачное и само исчезает.
public static class Fx
{
    static Material soft;   // полупрозрачный материал (цвет — через MaterialPropertyBlock)
    public static Material Soft => soft != null ? soft : (soft = new Material(Shader.Find("Sprites/Default")));

    static GameObject Make(string name, PrimitiveType type, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = Soft;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    // Кольцо по земле: расходится от r0 до r1 и гаснет
    public static void Ring(Vector3 pos, Color c, float r0, float r1, float life, float width = 0.14f)
    {
        var lr = MaxFx.Line("FxRing", c, width, true);
        var a = lr.gameObject.AddComponent<FxAnim>();
        a.kind = FxAnim.Kind.Ring; a.pos = pos + Vector3.up * 0.08f; a.r0 = r0; a.r1 = r1; a.life = life; a.color = c; a.width = width; a.lr = lr;
        a.Tick(0f);
    }

    // Ударная волна: кольцо + светлый диск на земле
    public static void Shockwave(Vector3 pos, Color c, float radius, float life = 0.5f)
    {
        Ring(pos, c, radius * 0.2f, radius, life, 0.22f);
        Ring(pos, Color.Lerp(c, Color.white, 0.5f), radius * 0.1f, radius * 0.7f, life * 0.8f, 0.1f);
        var disc = Make("FxDisc", PrimitiveType.Cylinder, pos + Vector3.up * 0.05f, new Vector3(0.1f, 0.005f, 0.1f));
        var a = disc.AddComponent<FxAnim>();
        a.kind = FxAnim.Kind.Disc; a.r0 = radius * 0.3f; a.r1 = radius * 1.6f; a.life = life * 0.7f; a.color = new Color(c.r, c.g, c.b, 0.45f);
        a.Tick(0f);
    }

    // Столб света (уровень, воскрешение): поднимается и тает
    public static void Pillar(Vector3 pos, Color c, float height = 6f, float radius = 0.9f, float life = 1.2f)
    {
        foreach (var (k, a0) in new[] { (1f, 0.35f), (0.45f, 0.7f) })
        {
            var go = Make("FxPillar", PrimitiveType.Cylinder, pos + Vector3.up * height * 0.5f, new Vector3(radius * 2f * k, height * 0.5f, radius * 2f * k));
            var a = go.AddComponent<FxAnim>();
            a.kind = FxAnim.Kind.Pillar; a.pos = pos; a.r0 = radius * k; a.r1 = height; a.life = life; a.color = new Color(c.r, c.g, c.b, a0);
            a.Tick(0f);
        }
    }

    // Вспышка: светящийся шар быстро раздувается и гаснет
    public static void Flash(Vector3 pos, Color c, float size = 1.2f, float life = 0.25f)
    {
        var go = Make("FxFlash", PrimitiveType.Sphere, pos, Vector3.one * size * 0.3f);
        var a = go.AddComponent<FxAnim>();
        a.kind = FxAnim.Kind.Flash; a.r0 = size * 0.3f; a.r1 = size; a.life = life; a.color = new Color(c.r, c.g, c.b, 0.75f);
        a.Tick(0f);
    }

    // Мерцающие искры: медленно поднимаются, крутятся и гаснут
    public static void Glints(Vector3 pos, Color c, int n, float radius = 0.8f, float rise = 1.8f, float life = 1.1f)
    {
        for (int i = 0; i < n; i++)
        {
            Vector2 p = Random.insideUnitCircle * radius;
            var go = Make("FxGlint", PrimitiveType.Quad, pos + new Vector3(p.x, Random.Range(0f, 0.6f), p.y), Vector3.one * Random.Range(0.12f, 0.24f));
            var a = go.AddComponent<FxAnim>();
            a.kind = FxAnim.Kind.Glint; a.vel = new Vector3(Random.Range(-0.2f, 0.2f), rise * Random.Range(0.6f, 1.2f), Random.Range(-0.2f, 0.2f));
            a.life = life * Random.Range(0.7f, 1.2f); a.color = c; a.r0 = go.transform.localScale.x; a.spin = Random.Range(-360f, 360f);
            a.Tick(0f);
        }
    }

    // Монеты: вылетают из точки и по дуге летят к герою
    public static void Coins(Vector3 from, Transform to, int n)
    {
        if (to == null) return;
        for (int i = 0; i < n; i++)
        {
            var go = Make("FxCoin", PrimitiveType.Cylinder, from + Vector3.up * 1.2f, new Vector3(0.28f, 0.03f, 0.28f));
            go.GetComponent<Renderer>().sharedMaterial = RuntimeMats.Glow(new Color(1f, 0.82f, 0.25f));
            var a = go.AddComponent<FxAnim>();
            a.kind = FxAnim.Kind.Coin; a.pos = go.transform.position; a.target = to;
            a.vel = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(3f, 5f), Random.Range(-2.5f, 2.5f));
            a.life = 0.75f + i * 0.06f; a.spin = Random.Range(500f, 900f);
        }
    }

    // Метка клика: сжимающееся кольцо и четыре стрелки к центру
    public static void ClickMarker(Vector3 pos, Color c)
    {
        Ring(pos, c, 0.9f, 0.15f, 0.35f, 0.1f);
        for (int i = 0; i < 4; i++)
        {
            float ang = i * 90f + 45f;
            var go = Make("FxArrow", PrimitiveType.Quad, pos + Vector3.up * 0.1f, new Vector3(0.18f, 0.35f, 1f));
            go.transform.rotation = Quaternion.Euler(90f, ang, 0f);
            var a = go.AddComponent<FxAnim>();
            a.kind = FxAnim.Kind.Arrow; a.pos = pos + Vector3.up * 0.1f; a.r0 = 0.9f; a.r1 = 0.2f; a.life = 0.35f; a.color = c; a.spin = ang;
            a.Tick(0f);
        }
    }

    // Всё сразу для «большого» события
    public static void Burst(Vector3 pos, Color c, float radius)
    {
        Shockwave(pos, c, radius);
        Flash(pos + Vector3.up * 1.2f, c, radius * 0.9f, 0.3f);
        Glints(pos, c, 10, radius * 0.6f);
    }
}

// Одна анимация эффекта (кольцо, диск, столб, вспышка, искра, монета, стрелка)
public class FxAnim : MonoBehaviour
{
    public enum Kind { Ring, Disc, Pillar, Flash, Glint, Coin, Arrow }
    public Kind kind;
    public Vector3 pos, vel;
    public float r0, r1, life, width, spin;
    public Color color;
    public LineRenderer lr;
    public Transform target;

    float t;
    Renderer rend;
    MaterialPropertyBlock mpb;

    void SetAlpha(float a)
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        mpb.SetColor("_Color", new Color(color.r, color.g, color.b, color.a * a));
        rend.SetPropertyBlock(mpb);
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t >= life) { Destroy(gameObject); return; }
        Tick(t / life);
    }

    public void Tick(float k)
    {
        float fade = 1f - k;
        switch (kind)
        {
            case Kind.Ring:
            {
                float r = Mathf.Lerp(r0, r1, 1f - (1f - k) * (1f - k));
                MaxFx.Circle(lr, pos, Mathf.Max(0.01f, r), 40);
                lr.startColor = lr.endColor = new Color(color.r, color.g, color.b, color.a * fade);
                lr.widthMultiplier = 0.5f + fade;
                break;
            }
            case Kind.Disc:
            {
                float r = Mathf.Lerp(r0, r1, k);
                transform.localScale = new Vector3(r * 2f, 0.005f, r * 2f);
                SetAlpha(fade * fade);
                break;
            }
            case Kind.Pillar:
            {
                float h = r1 * Mathf.Lerp(0.3f, 1f, Mathf.Min(1f, k * 3f));
                float w = r0 * 2f * (k < 0.7f ? 1f : fade / 0.3f);
                transform.localScale = new Vector3(w, h * 0.5f, w);
                transform.position = pos + Vector3.up * h * 0.5f;
                SetAlpha(k < 0.2f ? k / 0.2f : fade);
                break;
            }
            case Kind.Flash:
                transform.localScale = Vector3.one * Mathf.Lerp(r0, r1, Mathf.Sqrt(k));
                SetAlpha(fade * fade);
                break;
            case Kind.Glint:
            {
                transform.position += vel * Time.deltaTime;
                var cam = Camera.main;
                if (cam != null) transform.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, spin * k);
                float tw = 0.6f + 0.4f * Mathf.Sin(k * 30f);
                transform.localScale = Vector3.one * r0 * (k < 0.15f ? k / 0.15f : fade);
                SetAlpha(tw);
                break;
            }
            case Kind.Coin:
            {
                // сначала подлетают вверх, потом притягиваются к герою
                if (target == null) { Destroy(gameObject); return; }
                vel += Vector3.down * 9f * Time.deltaTime;
                pos += vel * Time.deltaTime;
                Vector3 goal = target.position + Vector3.up * 1.6f;
                float pull = Mathf.Clamp01((k - 0.3f) / 0.7f);
                transform.position = Vector3.Lerp(pos, goal, pull * pull);
                transform.Rotate(spin * Time.deltaTime, 0f, 0f, Space.Self);
                if (pull >= 0.98f) { ParticleBurst.Spawn(goal, new Color(1f, 0.85f, 0.3f), 2, 2f); Destroy(gameObject); }
                break;
            }
            case Kind.Arrow:
            {
                float r = Mathf.Lerp(r0, r1, k);
                Vector3 dir = Quaternion.Euler(0, spin, 0) * Vector3.forward;
                transform.position = pos - dir * r;
                SetAlpha(fade);
                break;
            }
        }
    }
}
