using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Способности Сани Финда.

// Q / W / E — Колбасный койл: колбаски падают в круг перед Саней на 2.5 / 5 / 7.5 м.
public class SausageCoilAbility : Ability
{
    public SausageCoilAbility(AbilityInfo info) : base(info) { }

    public override bool IsValidTarget(Unit caster, Unit target) => true;

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx)
    {
        Vector3 fwd = caster.transform.forward; fwd.y = 0; fwd.Normalize();
        Vector3 center = caster.transform.position + fwd * Info.distance;
        var stash = caster.GetComponent<SausageStash>();
        float dmg = V(0) + (stash != null ? stash.Count : 0) * V(1);
        ctx.StartCoroutine(Rain(caster, center, Info.radius, dmg));
        VoiceLines.Speak("sanya_fiend", caster.transform.position, 0.35f, false, -1, SausageStash.DeathLineIndex);
    }

    IEnumerator Rain(Unit caster, Vector3 center, float radius, float dmg)
    {
        const float fall = 0.55f;
        // Круг на земле
        var disc = RuntimeMats.Prim(PrimitiveType.Cylinder, null, center + Vector3.up * 0.03f, new Vector3(radius * 2f, 0.01f, radius * 2f),
            RuntimeMats.Glow(new Color(0.85f, 0.22f, 0.15f)));
        // Колбаски падают с неба
        var drops = new List<(Transform t, Vector3 ground, float speed)>();
        for (int i = 0; i < 9; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius * 0.85f;
            Vector3 g = center + new Vector3(r.x, 0.25f, r.y);
            var s = RuntimeMats.Sausage(null, g + Vector3.up * 7f, 1.3f).transform;
            s.rotation = Random.rotation;
            drops.Add((s, g, Random.Range(0.8f, 1f)));
        }
        float t0 = Time.time;
        while (Time.time - t0 < fall)
        {
            float k = (Time.time - t0) / fall;
            foreach (var d in drops)
            {
                float kk = Mathf.Clamp01(k / d.speed);
                d.t.position = Vector3.Lerp(d.ground + Vector3.up * 7f, d.ground, kk * kk);
                d.t.Rotate(720f * Time.deltaTime, 360f * Time.deltaTime, 0);
            }
            disc.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f) * (0.6f + 0.4f * k);
            yield return null;
        }

        if (caster != null)
            foreach (var u in new List<Unit>(Unit.All))
            {
                if (!u.IsAlive || !caster.IsEnemyOf(u) || u.kind == UnitKind.Structure) continue;
                if (u.DistanceFrom(center) > radius) continue;
                u.Health.TakeDamage(new DamageInfo { amount = dmg, type = DamageType.Magical, source = caster });
            }
        ParticleBurst.Spawn(center + Vector3.up * 0.5f, new Color(0.8f, 0.25f, 0.15f), 18, 6f);

        // Шлёп — сплющились и пропали
        float t1 = Time.time;
        while (Time.time - t1 < 0.4f)
        {
            float k = (Time.time - t1) / 0.4f;
            foreach (var d in drops) d.t.localScale = new Vector3(1f + k * 0.5f, 1f - k * 0.8f, 1f + k * 0.5f);
            yield return null;
        }
        foreach (var d in drops) Object.Destroy(d.t.gameObject);
        Object.Destroy(disc);
    }
}

// R — Колбасный реквием: 1.67 сек. собирает сосиски, потом колбаски летят во все стороны.
public class SausageRequiemAbility : Ability
{
    public SausageRequiemAbility(AbilityInfo info) : base(info) { }

    public override bool IsValidTarget(Unit caster, Unit target) => true;

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) =>
        ctx.StartCoroutine(Channel(caster));

    IEnumerator Channel(Unit caster)
    {
        var hc = caster.GetComponent<HeroController>();
        var agent = caster.GetComponent<NavMeshAgent>();
        var status = caster.GetComponent<StatusEffects>();
        var stash = caster.GetComponent<SausageStash>();
        var attack = caster.GetComponent<AutoAttack>();

        if (hc != null) hc.IsBusyChanneling = true;
        if (attack != null) attack.ClearTarget();
        if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
        if (stash != null) stash.Charging = true;
        FloatingText.Show(caster.transform.position + Vector3.up * 4.5f, "КОЛБАСНЫЙ РЕКВИЕМ...", new Color(1f, 0.5f, 0.1f), 30);
        VoiceLines.Speak("sanya_fiend", caster.transform.position, 1f, true, -1, SausageStash.DeathLineIndex);

        float end = Time.time + 1.67f;
        bool broken = false;
        while (Time.time < end)
        {
            if (!caster.IsAlive || (status != null && status.IsStunned)) { broken = true; break; }
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            if (Random.value < 0.3f) ParticleBurst.Spawn(caster.transform.position + Vector3.up * 1.6f, new Color(1f, 0.5f, 0.1f), 2, 2f);
            yield return null;
        }
        if (hc != null) hc.IsBusyChanneling = false;
        if (stash != null) stash.Charging = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        if (broken) { if (caster.IsAlive) FloatingText.Show(caster.transform.position + Vector3.up * 4f, "Реквием сорван", new Color(1f, 0.6f, 0.6f), 22); yield break; }

        int n = Mathf.Clamp(Mathf.Max(stash != null ? stash.Count : 0, Mathf.RoundToInt(V(1))), 1, 30);
        float baseAngle = caster.transform.eulerAngles.y;
        for (int i = 0; i < n; i++)
        {
            float a = baseAngle + 360f * i / n;
            Vector3 dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
            var go = RuntimeMats.Sausage(null, caster.transform.position + Vector3.up * 1.2f, 1.5f);
            go.AddComponent<SausageLine>().Launch(caster, dir, V(0), Info.castRange);
        }
        ParticleBurst.Spawn(caster.transform.position + Vector3.up * 1.5f, new Color(1f, 0.5f, 0.1f), 30, 7f);
    }
}

// Колбаска реквиема: летит наружу и бьёт каждого врага, которого заденет, один раз.
public class SausageLine : MonoBehaviour
{
    Unit owner;
    Vector3 dir;
    float damage, left;
    const float Speed = 11f, HitRadius = 1f;
    readonly HashSet<Unit> hit = new HashSet<Unit>();

    public void Launch(Unit owner, Vector3 dir, float damage, float range)
    {
        this.owner = owner; this.dir = dir; this.damage = damage; left = range;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    void Update()
    {
        float step = Speed * Time.deltaTime;
        transform.position += dir * step;
        transform.Rotate(0, 0, 900f * Time.deltaTime, Space.Self);
        left -= step;
        if (owner != null)
            foreach (var u in new List<Unit>(Unit.All))
            {
                if (!u.IsAlive || !owner.IsEnemyOf(u) || u.kind == UnitKind.Structure || hit.Contains(u)) continue;
                if (u.DistanceFrom(transform.position) > HitRadius) continue;
                hit.Add(u);
                u.Health.TakeDamage(new DamageInfo { amount = damage, type = DamageType.Magical, source = owner });
            }
        if (left <= 0f) Destroy(gameObject);
    }
}
