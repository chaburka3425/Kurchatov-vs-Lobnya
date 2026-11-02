using UnityEngine;
using UnityEngine.AI;

// Обычная атака: подойти к цели и бить с заданной скоростью.
// Поддерживает дальний бой (снаряд), криты и рассечение от предметов.
[RequireComponent(typeof(Unit))]
public class AutoAttack : MonoBehaviour
{
    [Header("Урон")]
    public float damage = 40f;
    public float range = 2f;              // дальность атаки (2 = ближний бой)
    public float baseAttackTime = 1.7f;   // базовый интервал между ударами
    public float attackSpeed = 100f;      // 100 = обычная, 200 = в два раза быстрее

    [Header("Дальний бой")]
    public bool ranged = false;
    public Color projectileColor = new Color(0.9f, 0.9f, 0.6f);
    public float projectileSpeed = 18f;
    public bool bullet = false;           // золотая пуля Макса

    [Header("Автоматический выбор цели")]
    public bool autoAcquire = true;       // сам атакует врагов рядом, когда стоит
    public bool acquireWhileMoving = false; // крипы: ищут врагов и на ходу
    public float acquireRange = 6f;
    public float loseRange = 12f;         // дальше этого бросает погоню

    [Header("От предметов")]
    public float critChance = 0f;
    public float critMultiplier = 1f;
    public float cleave = 0f;
    public float cleaveRadius = 0f;

    public Unit Target { get; private set; }

    Unit self;
    NavMeshAgent agent;
    StatusEffects status;
    float cooldown;
    float nextScan;
    bool manualTarget; // цель выбрал игрок — не бросаем погоню
    ItemEffects itemFx;
    float stuckTime;   // сколько агент «идёт», но не двигается (упёрся в толпу)
    bool moveOrder;    // игрок велел идти — пока не дошёл, сам никого не бьёт
    UnitAnimator anim;
    Unit windupTarget; // замахнулся на эту цель — удар через windupLeft
    float windupLeft;

    float AttackInterval =>
        baseAttackTime / ((attackSpeed + (status != null ? status.BonusAttackSpeed : 0f)) / 100f);

    // Замах перед ударом, как в Доте (чем быстрее атака, тем короче)
    float AttackPoint => Mathf.Min(0.3f, AttackInterval * 0.3f);

    public bool IsWindingUp => windupTarget != null;
    public void CancelSwing() => CancelWindup();

    // Замах сорван (ушёл, оглушили, сменил цель) — удар не тратится
    void CancelWindup()
    {
        if (windupTarget == null) return;
        windupTarget = null;
        cooldown = 0f;
        if (anim != null) anim.Cancel();
    }

    void Awake()
    {
        self = GetComponent<Unit>();
        agent = GetComponent<NavMeshAgent>();
        status = GetComponent<StatusEffects>();
        if (self.kind != UnitKind.Structure) anim = UnitAnimator.For(gameObject);
    }

    public void SetTarget(Unit target) { if (target != Target) CancelWindup(); Target = target; manualTarget = true; moveOrder = false; }
    public void ClearTarget() { CancelWindup(); Target = null; manualTarget = false; }

    // Приказ идти: бросаем цель и не ищем новую, пока не дойдём
    public void OrderMove() { CancelWindup(); Target = null; manualTarget = false; moveOrder = true; stuckTime = 0f; }

    void Update()
    {
        if (!self.IsAlive) { windupTarget = null; return; }
        cooldown -= Time.deltaTime;
        if (status != null && status.IsStunned) { CancelWindup(); return; } // оглушён — не бьёт и не ходит
        if (itemFx == null) itemFx = GetComponent<ItemEffects>();
        if (itemFx != null && itemFx.IsTeleporting) { CancelWindup(); return; }  // едет на электричке — стоит

        bool moving = agent != null && agent.isOnNavMesh && agent.hasPath && !agent.isStopped;
        if (moving && agent.velocity.sqrMagnitude < 0.09f) stuckTime += Time.deltaTime; else stuckTime = 0f;

        if (Target != null && (!Target.IsAlive || !Target.isActiveAndEnabled))
        {
            // цель умерла, пока мы к ней шли — не идём дальше в пустую точку
            if (!manualTarget || moving) StopMoving();
            Target = null;
            manualTarget = false;
        }
        if (Target != null && !FogOfWar.CanSee(self.team, Target)) { StopMoving(); Target = null; }   // ушёл в туман

        if (moveOrder)
        {
            bool arrived = agent == null || !agent.isOnNavMesh || (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.3f));
            if (arrived || stuckTime > 1.2f || agent.isStopped) moveOrder = false;
            else return;
        }

        if (Target == null && autoAcquire && (acquireWhileMoving || IsIdle()) && Time.time >= nextScan)
        {
            nextScan = Time.time + 0.25f;
            Target = FindNearestEnemy(acquireRange);
            manualTarget = false;
        }

        if (windupTarget != null && windupTarget != Target) CancelWindup();
        if (Target == null) return;

        float dist = Target.DistanceFrom(transform.position);

        if (!manualTarget && dist > loseRange) { StopMoving(); Target = null; return; }

        // уже замахнулся — цель может отойти чуть дальше, удар всё равно долетит
        if (dist > range + (windupTarget != null ? 0.6f : 0f))
        {
            CancelWindup();
            // Толпа не пускает к цели — бьём любого врага, до которого уже достаём
            if (stuckTime > 0.6f && !(self.kind == UnitKind.Hero && Target.kind == UnitKind.Hero && manualTarget))
            {
                var near = FindNearestEnemy(range + 0.3f);
                if (near != null && near != Target) { Target = near; stuckTime = 0f; return; }
            }
            // Цель далеко — идём к ней.
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(Target.transform.position);
            }
            return;
        }

        // Цель в радиусе — стоим и бьём.
        StopMoving();
        FaceTowards(Target.transform.position);

        if (windupTarget != null)
        {
            windupLeft -= Time.deltaTime;
            if (windupLeft <= 0f)
            {
                windupTarget = null;
                if (anim != null) anim.Strike();
                Hit(Target);
            }
            return;
        }

        if (cooldown <= 0f && Target.Health != null)
        {
            cooldown = AttackInterval;
            windupTarget = Target;
            windupLeft = AttackPoint;
            if (anim != null) anim.Windup(AttackPoint);
        }
    }

    void Hit(Unit target)
    {
        float dmg = damage;
        bool crit = critChance > 0f && Random.value < critChance;
        if (crit) dmg *= critMultiplier;

        if (ranged)
        {
            var kit = GetComponent<MaxGoldKit>();
            float hs = kit != null ? kit.RollHeadshot() : 0f;
            Projectile.Spawn(transform.position + Vector3.up * (bullet ? 1.6f : 1.3f), target, self, dmg + hs, projectileSpeed,
                bullet ? ProjectileShape.Bullet : ProjectileShape.Ball, projectileColor, bullet ? 0.32f : 0.3f, DamageType.Physical,
                hs > 0f ? new System.Action<Unit>(kit.OnHeadshot) : null, crit);
            return;
        }

        target.Health.TakeDamage(new DamageInfo { amount = dmg, type = DamageType.Physical, source = self, crit = crit });
        ParticleBurst.Spawn(target.transform.position + Vector3.up * 1.2f, crit ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.95f, 0.8f), crit ? 12 : 5);
        if (FogOfWar.VisibleToPlayer(target))
        {
            if (crit) { Fx.Flash(target.transform.position + Vector3.up * 1.3f, new Color(1f, 0.25f, 0.15f), 1.8f, 0.25f); Fx.Ring(target.transform.position, new Color(1f, 0.3f, 0.2f), 0.3f, 2f, 0.35f, 0.15f); }
            else if (self.kind == UnitKind.Hero) Fx.Flash(target.transform.position + Vector3.up * 1.3f, new Color(1f, 0.95f, 0.8f), 0.7f, 0.15f);
        }

        // Рассечение: часть урона по врагам вокруг цели
        if (cleave > 0f && cleaveRadius > 0f)
        {
            foreach (var u in Unit.All)
            {
                if (u == target || !u.IsAlive || !self.IsEnemyOf(u) || u.kind == UnitKind.Structure) continue;
                if (u.DistanceFrom(target.transform.position) > cleaveRadius) continue;
                u.Health.TakeDamage(new DamageInfo { amount = dmg * cleave, type = DamageType.Pure, source = self });
            }
        }
    }

    bool IsIdle() =>
        agent == null || !agent.isOnNavMesh || !agent.hasPath || agent.isStopped || stuckTime > 0.4f ||
        (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.6f);

    void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    Unit FindNearestEnemy(float radius)
    {
        Unit best = null;
        float bestDist = radius;
        foreach (var u in Unit.All)
        {
            if (u == self || !u.IsAlive || !self.IsEnemyOf(u)) continue;
            if (u.team == Team.Neutral && self.team != Team.Neutral) continue; // лесных не трогаем сами
            if (u.Health != null && u.Health.invulnerable) continue; // неуязвимых не трогаем
            if (!FogOfWar.CanSee(self.team, u)) continue; // в тумане не видно
            float d = u.DistanceFrom(transform.position);
            if (d <= bestDist) { bestDist = d; best = u; }
        }
        return best;
    }

    void FaceTowards(Vector3 point)
    {
        Vector3 dir = point - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 15f * Time.deltaTime);
    }
}
