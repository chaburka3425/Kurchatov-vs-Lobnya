using UnityEngine;

public enum ProjectileShape { Ball, Disc, Bullet }

// Самонаводящийся снаряд: летит к цели и наносит урон при попадании.
public class Projectile : MonoBehaviour
{
    Unit target;
    Unit source;
    float damage;
    float speed;
    DamageType damageType;
    System.Action<Unit> onHit;
    bool crit;
    Color color;
    bool bullet;

    public static Projectile Spawn(Vector3 from, Unit target, Unit source, float damage, float speed,
        ProjectileShape shape, Color color, float size,
        DamageType damageType = DamageType.Physical, System.Action<Unit> onHit = null, bool crit = false)
    {
        NetWorld.ProjEvent(from, target, speed, shape, color, size);
        bool bullet = shape == ProjectileShape.Bullet;
        var go = bullet ? new GameObject("Projectile") : GameObject.CreatePrimitive(shape == ProjectileShape.Disc ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
        go.name = "Projectile";
        if (bullet)
        {
            // Золотая пуля: вытянутая капсула со светящимся следом
            var body = RuntimeMats.Prim(PrimitiveType.Capsule, go.transform, Vector3.zero, new Vector3(size * 0.45f, size * 0.9f, size * 0.45f), RuntimeMats.Glow(color));
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.12f; tr.startWidth = size * 0.35f; tr.endWidth = 0f;
            tr.material = new Material(Shader.Find("Sprites/Default"));
            tr.startColor = new Color(1f, 0.9f, 0.55f, 0.9f); tr.endColor = new Color(1f, 0.75f, 0.2f, 0f);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        else Destroy(go.GetComponent<Collider>());
        go.transform.position = from;
        if (!bullet)
            go.transform.localScale = shape == ProjectileShape.Disc
                ? new Vector3(size, size * 0.08f, size)
                : Vector3.one * size;

        if (!bullet)
        {
            var mat = go.GetComponent<Renderer>().material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            // светящийся шлейф за снарядом
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.22f; tr.startWidth = size * 0.8f; tr.endWidth = 0f; tr.minVertexDistance = 0.05f;
            tr.material = Fx.Soft;
            tr.startColor = new Color(color.r, color.g, color.b, 0.8f);
            tr.endColor = new Color(color.r, color.g, color.b, 0f);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // ореол вокруг снаряда
            var halo = RuntimeMats.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, shape == ProjectileShape.Disc ? new Vector3(1.6f, 12f, 1.6f) : Vector3.one * 1.7f, Fx.Soft);
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", new Color(color.r, color.g, color.b, 0.28f));
            halo.GetComponent<Renderer>().SetPropertyBlock(mpb);
        }

        var p = go.AddComponent<Projectile>();
        p.target = target;
        p.source = source;
        p.damage = damage;
        p.speed = speed;
        p.damageType = damageType;
        p.onHit = onHit;
        p.crit = crit;
        p.color = color;
        p.bullet = bullet;
        return p;
    }

    void Update()
    {
        if (target == null || !target.IsAlive) { Destroy(gameObject); return; }

        Vector3 aim = target.transform.position + Vector3.up * 0.5f;
        transform.position = Vector3.MoveTowards(transform.position, aim, speed * Time.deltaTime);
        if (bullet) { Vector3 d = aim - transform.position; if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d); }
        else transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World); // крутится в полёте

        if ((transform.position - aim).sqrMagnitude < 0.09f)
        {
            if (target.Health != null && damage > 0f)
                target.Health.TakeDamage(new DamageInfo { amount = damage, type = damageType, source = source, crit = crit });
            onHit?.Invoke(target);
            ParticleBurst.Spawn(transform.position, color, crit ? 14 : 8);
            if (FogOfWar.VisibleToPlayer(target)) Fx.Flash(transform.position, color, crit ? 1.8f : 1f, 0.2f);
            Destroy(gameObject);
        }
    }
}
