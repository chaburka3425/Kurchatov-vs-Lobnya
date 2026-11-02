using UnityEngine;

// Пивная баня у трона: лечит здоровье и ману всем своим, кто стоит в бассейне.
public class BeerBath : MonoBehaviour
{
    public Team team = Team.Kurchatov;
    public float radius = 4.5f;
    [Tooltip("Сколько % от максимума восстанавливается в секунду")]
    public float healthPercentPerSecond = 6f;
    public float manaPercentPerSecond = 6f;

    [Header("Пузырьки")]
    public float bubbleInterval = 0.12f;
    float nextBubble;

    float healFxAt;

    void Update()
    {
        bool fx = Time.time >= healFxAt;
        if (fx) healFxAt = Time.time + 0.35f;
        foreach (var u in Unit.All)
        {
            if (u.team != team || !u.IsAlive || u.kind == UnitKind.Structure) continue;
            if (!IsInside(u.transform.position)) continue;
            // зелёные искорки над теми, кто лечится
            if (fx && u.kind == UnitKind.Hero && u.Health != null && u.Health.Current < u.Health.maxHealth - 1f && FogOfWar.VisibleToPlayer(u))
                Fx.Glints(u.transform.position, new Color(0.45f, 1f, 0.5f), 3, 0.7f, 2.2f, 0.9f);

            if (u.Health != null)
                u.Health.Heal(u.Health.maxHealth * healthPercentPerSecond / 100f * Time.deltaTime);
            if (fx && u.kind == UnitKind.Hero) { var inv = u.GetComponent<Inventory>(); if (inv != null) inv.RefillCharges(); }   // бутылка снова полная
            var mana = u.GetComponent<Mana>();
            if (mana != null)
                mana.Restore(mana.maxMana * manaPercentPerSecond / 100f * Time.deltaTime);
        }

        if (Time.time >= nextBubble)
        {
            nextBubble = Time.time + bubbleInterval;
            SpawnBubble();
        }
    }

    public bool IsInside(Vector3 p)
    {
        Vector3 d = p - transform.position;
        d.y = 0f;
        return d.magnitude <= radius;
    }

    void SpawnBubble()
    {
        Vector2 r = Random.insideUnitCircle * (radius - 0.6f);
        var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        b.name = "Bubble";
        Destroy(b.GetComponent<Collider>());
        b.transform.position = transform.position + new Vector3(r.x, 0.3f, r.y);
        b.transform.localScale = Vector3.one * Random.Range(0.12f, 0.3f);
        var mat = b.GetComponent<Renderer>().material;
        var c = new Color(1f, 0.97f, 0.85f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        b.AddComponent<Bubble>();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.7f, 0.1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

// Пузырёк: всплывает и лопается.
public class Bubble : MonoBehaviour
{
    float life = 1.4f;
    float speed;

    void Start() => speed = Random.Range(0.6f, 1.3f);

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }
}
