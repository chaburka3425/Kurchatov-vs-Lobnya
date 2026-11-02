using UnityEngine;
using UnityEngine.AI;

// Лесной гопник: стоит у лагеря. Ударили его или соседа — весь лагерь бежит драться.
// Убежал далеко от лагеря — возвращается и лечится.
public class NeutralAI : MonoBehaviour
{
    public int camp;
    public Vector3 home;
    public float leash = 11f;

    NavMeshAgent agent;
    AutoAttack attack;
    Unit unit;
    Health health;
    StatusEffects status;
    bool returning;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<AutoAttack>();
        unit = GetComponent<Unit>();
        health = GetComponent<Health>();
        status = GetComponent<StatusEffects>();
    }

    void Start() => health.OnDamaged += OnHit;
    void OnDestroy() { if (health != null) health.OnDamaged -= OnHit; }

    void OnHit(DamageInfo info)
    {
        if (info.source == null || info.source.team == Team.Neutral) return;
        foreach (var u in Unit.All)
        {
            var n = u.GetComponent<NeutralAI>();
            if (n != null && n.camp == camp) n.Aggro(info.source);
        }
    }

    public void Aggro(Unit who)
    {
        if (returning || !unit.IsAlive || who == null || !who.IsAlive) return;
        if (attack.Target == null) attack.SetTarget(who);
    }

    void Update()
    {
        if (!unit.IsAlive || (status != null && status.IsStunned)) return;
        Vector3 d = transform.position - home; d.y = 0;
        float away = d.magnitude;

        if (!returning && attack.Target != null && (away > leash || !attack.Target.IsAlive))
        {
            attack.ClearTarget();
            returning = true;
        }
        if (returning)
        {
            health.Heal(health.maxHealth * 0.1f * Time.deltaTime);
            if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(home); }
            if (away < 0.8f) { returning = false; if (agent.isOnNavMesh) agent.ResetPath(); }
        }
    }
}
