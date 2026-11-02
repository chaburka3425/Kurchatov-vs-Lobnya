using UnityEngine;

// Постоянный эффект у постройки: дым из трубы, пар над баней, огонь в чаше, искры у кристалла.
// Работает, только когда камера рядом (чтобы не нагружать игру).
public class AmbientFx : MonoBehaviour
{
    public enum Kind { Smoke, Steam, Fire, Sparkle }
    public Kind kind = Kind.Smoke;
    public Color color = Color.white;
    public float interval = 0.3f;
    public float radius = 0.4f;
    public int count = 2;

    float next;

    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + interval * Random.Range(0.8f, 1.2f);
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 d = cam.transform.position - transform.position; d.y = 0f;
        if (d.magnitude > 55f) return;
        switch (kind)
        {
            case Kind.Smoke: Fx.Glints(transform.position, color, count, radius, 1.4f, 2.2f); break;
            case Kind.Steam: Fx.Glints(transform.position, color, count, radius, 1.1f, 1.8f); break;
            case Kind.Fire: Fx.Glints(transform.position, color, count, radius, 2.4f, 0.7f); break;
            case Kind.Sparkle: Fx.Glints(transform.position, color, count, radius, 0.8f, 1.4f); break;
        }
    }
}
