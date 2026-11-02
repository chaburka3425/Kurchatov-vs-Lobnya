using System.Collections.Generic;
using UnityEngine;

public enum UnitKind { Hero, Creep, Structure }

// Базовый компонент для всего, что участвует в бою: герои, крипы, башни, троны.
public class Unit : MonoBehaviour
{
    public Team team = Team.Kurchatov;
    public UnitKind kind = UnitKind.Creep;
    public string displayName = "Юнит";

    [Header("Награда за убийство")]
    public float xpBounty = 0f;
    public int goldBounty = 0;

    // Список всех живых юнитов на карте — для быстрого поиска целей.
    public static readonly List<Unit> All = new List<Unit>();

    // Ищем компоненты «лениво»: крипы создаются в игре и компоненты добавляются по очереди.
    Health health;
    AutoAttack attack;
    public Health Health { get { if (health == null) health = GetComponent<Health>(); return health; } }
    public AutoAttack Attack { get { if (attack == null) attack = GetComponent<AutoAttack>(); return attack; } }
    public bool IsAlive => Health == null || !Health.IsDead;
    StatusEffects status;
    public bool IsInvisibleNow { get { if (status == null) status = GetComponent<StatusEffects>(); return status != null && status.IsInvisible; } }

    Collider col;

    void Awake()
    {
        col = GetComponentInChildren<Collider>();
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public bool IsEnemyOf(Unit other) => other != null && TeamUtil.AreEnemies(team, other.team);

    // Расстояние от точки до края юнита (важно для больших объектов вроде трона).
    public float DistanceFrom(Vector3 point)
    {
        if (col == null) col = GetComponentInChildren<Collider>();
        Vector3 closest = (col != null && col.enabled) ? col.ClosestPoint(point) : transform.position;
        closest.y = point.y;
        return Vector3.Distance(point, closest);
    }
}
