using UnityEngine;

// Неуязвим, пока живо хоть одно из указанных строений.
// Пример: вторая башня неуязвима, пока стоит первая; трон — пока стоит вторая.
[RequireComponent(typeof(Health))]
public class Protected : MonoBehaviour
{
    public Health[] protectors;
    [Tooltip("Трон: неуязвим, пока живы ВСЕ защитники (открывается, когда сломана любая вторая башня)")]
    public bool untilAnyDestroyed = false;

    Health health;

    void Awake() => health = GetComponent<Health>();

    void Update()
    {
        bool anyAlive = false, allAlive = true;
        if (protectors != null)
            foreach (var p in protectors)
            {
                bool alive = p != null && !p.IsDead;
                if (alive) anyAlive = true; else allAlive = false;
            }
        health.invulnerable = untilAnyDestroyed ? (protectors != null && protectors.Length > 0 && allAlive) : anyAlive;
    }
}
