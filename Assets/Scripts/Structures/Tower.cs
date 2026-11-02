using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Башня: стоит на месте и стреляет снарядами.
// Правила как в MOBA: сначала бьёт крипов, героев — если больше некого,
// и сразу переключается на вражеского героя, который атакует союзного героя рядом.
[RequireComponent(typeof(Unit), typeof(Health))]
public class Tower : MonoBehaviour
{
    [Header("Линия")]
    public LaneData.Lane lane = LaneData.Lane.Mid;
    public int tier = 1;

    [Header("Атака")]
    public float damage = 40f;
    public float range = 9f;
    public float attackInterval = 1.5f;

    [Header("Снаряд")]
    public float projectileSpeed = 16f;
    public ProjectileShape projectileShape = ProjectileShape.Ball;
    public Color projectileColor = Color.yellow;
    public float projectileSize = 0.7f;
    public float firePointHeight = 4f;

    Unit self;
    Unit target;
    float cooldown;
    float nextScan;

    void Awake()
    {
        self = GetComponent<Unit>();
        var h = GetComponent<Health>();
        if (h != null) h.OnDeath += _ => StartCoroutine(Collapse());
    }

    // Башня рушится: уходит под землю и пропадает, крипы и герои ходят насквозь
    IEnumerator Collapse()
    {
        target = null;
        var obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle != null) obstacle.enabled = false;
        var box = GetComponent<Collider>();
        if (box != null) box.enabled = false;
        ParticleBurst.Spawn(transform.position + Vector3.up * 2f, new Color(0.55f, 0.5f, 0.45f), 40, 8f);
        ParticleBurst.Spawn(transform.position + Vector3.up * 3f, new Color(1f, 0.55f, 0.15f), 20, 6f);

        var parts = new System.Collections.Generic.List<Transform>();
        foreach (Transform c in transform) parts.Add(c);
        var start = new Vector3[parts.Count];
        for (int i = 0; i < parts.Count; i++) start[i] = parts[i].localPosition;

        float t0 = Time.time, dur = 1.3f;
        while (Time.time - t0 < dur)
        {
            float k = (Time.time - t0) / dur;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] != null)
                {
                    parts[i].localPosition = start[i] + Vector3.down * (6f * k * k) + new Vector3(Mathf.Sin(i * 7.3f), 0, Mathf.Cos(i * 5.1f)) * 0.6f * k;
                    parts[i].localRotation *= Quaternion.Euler(40f * Time.deltaTime * Mathf.Sin(i), 0, 30f * Time.deltaTime * Mathf.Cos(i));
                }
            yield return null;
        }
        foreach (var p in parts) if (p != null) p.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!self.IsAlive) return;
        cooldown -= Time.deltaTime;

        if (target != null && (!target.IsAlive || !InRange(target))) target = null;

        if (Time.time >= nextScan)
        {
            nextScan = Time.time + 0.2f;
            var aggressor = FindHeroAttackingAlly();
            if (aggressor != null) target = aggressor;
            else if (target == null) target = FindTarget();
        }

        if (target != null && cooldown <= 0f)
        {
            cooldown = attackInterval;
            Projectile.Spawn(transform.position + Vector3.up * firePointHeight, target, self,
                damage, projectileSpeed, projectileShape, projectileColor, projectileSize);
        }
    }

    bool InRange(Unit u) => u.DistanceFrom(transform.position) <= range;

    Unit FindHeroAttackingAlly()
    {
        foreach (var u in Unit.All)
        {
            if (u.kind != UnitKind.Hero || !u.IsAlive || !self.IsEnemyOf(u) || !InRange(u)) continue;
            var victim = u.Attack != null ? u.Attack.Target : null;
            if (victim != null && victim.kind == UnitKind.Hero && victim.team == self.team && InRange(victim))
                return u;
        }
        return null;
    }

    Unit FindTarget()
    {
        Unit bestCreep = null, bestHero = null;
        float creepDist = float.MaxValue, heroDist = float.MaxValue;

        foreach (var u in Unit.All)
        {
            if (!u.IsAlive || !self.IsEnemyOf(u) || u.kind == UnitKind.Structure || u.team == Team.Neutral) continue;
            float d = u.DistanceFrom(transform.position);
            if (d > range) continue;

            if (u.kind == UnitKind.Creep && d < creepDist) { creepDist = d; bestCreep = u; }
            else if (u.kind == UnitKind.Hero && d < heroDist) { heroDist = d; bestHero = u; }
        }
        return bestCreep != null ? bestCreep : bestHero;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
