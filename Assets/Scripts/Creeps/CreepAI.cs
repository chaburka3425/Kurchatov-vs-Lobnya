using UnityEngine;
using UnityEngine.AI;

// Крип идёт по своей линии (по точкам пути) к вражескому трону. Встретил врага — дерётся (это делает AutoAttack).
public class CreepAI : MonoBehaviour
{
    public Vector3 destination;
    public Vector3[] path;
    public LaneData.Lane lane = LaneData.Lane.Mid;

    NavMeshAgent agent;
    AutoAttack attack;
    Unit unit;
    StatusEffects status;
    float next;
    int wp;

    public int Waypoint => wp;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<AutoAttack>();
        unit = GetComponent<Unit>();
        status = GetComponent<StatusEffects>();
    }

    // Насколько крип продвинулся по линии (для бота: держаться за передним крипом)
    public float Progress
    {
        get
        {
            if (path == null || path.Length == 0) return 0f;
            int i = Mathf.Clamp(wp, 0, path.Length - 1);
            Vector3 d = path[i] - transform.position; d.y = 0;
            return wp * 1000f - d.magnitude;
        }
    }

    public Vector3 NextPoint => path != null && path.Length > 0 ? path[Mathf.Clamp(wp, 0, path.Length - 1)] : destination;

    void Update()
    {
        if (!unit.IsAlive || Time.time < next) return;
        next = Time.time + 0.3f;
        if (status != null && status.IsStunned) return;

        Vector3 target = destination;
        if (path != null && path.Length > 0)
        {
            Vector3 d = path[wp] - transform.position; d.y = 0;
            if (d.magnitude < 3f && wp < path.Length - 1) wp++;
            target = path[wp];
        }

        if (attack.Target == null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target);
        }
    }
}
