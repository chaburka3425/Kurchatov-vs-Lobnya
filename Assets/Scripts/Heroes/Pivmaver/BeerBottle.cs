using UnityEngine;
using UnityEngine.AI;

// Маленькая злая бутылочка Пивмавера (как паучок у паучихи): бегает за хозяином,
// бьёт его цель или врагов рядом, живёт 60 сек.
[RequireComponent(typeof(Unit))]
public class BeerBottle : MonoBehaviour
{
    public const float LifeTime = 60f, Leash = 6f;

    PivmaverKit owner;
    public PivmaverKit Owner => owner;
    Unit ownerUnit, unit;
    Health health;
    NavMeshAgent agent;
    AutoAttack attack;
    StatusEffects status;
    float dieAt, think, foamTick;

    public static BeerBottle Spawn(PivmaverKit kit, Vector3 pos, float hp, float dmg)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 4f, NavMesh.AllAreas)) pos = hit.position;
        var ou = kit.GetComponent<Unit>();

        var go = new GameObject("Бутылочка");
        go.SetActive(false);
        go.transform.position = pos;
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.6f, 0); col.height = 1.2f; col.radius = 0.35f;
        var ag = go.AddComponent<NavMeshAgent>();
        ag.radius = 0.3f; ag.height = 1.2f; ag.speed = 5.8f; ag.angularSpeed = 720f; ag.acceleration = 30f;
        var u = go.AddComponent<Unit>();
        u.team = ou.team;
        u.kind = UnitKind.Creep;
        u.displayName = "Бутылочка";
        u.xpBounty = 10; u.goldBounty = 8;
        var h = go.AddComponent<Health>();
        h.maxHealth = hp; h.armor = 1; h.regenPerSecond = 1; h.magicResist = 0.25f;
        h.SetMax(hp, true);
        h.destroyOnDeath = true;
        go.AddComponent<StatusEffects>();
        var at = go.AddComponent<AutoAttack>();
        at.damage = dmg; at.range = 1.5f; at.baseAttackTime = 1.2f; at.acquireRange = 6f; at.loseRange = 10f;
        var bar = go.AddComponent<HealthBarGUI>();
        bar.heightOffset = 1.7f; bar.width = 36f; bar.height = 5f;
        var b = go.AddComponent<BeerBottle>();
        b.owner = kit;
        BuildModel(go.transform, ou.team);
        go.SetActive(true);
        kit.Bottles.Add(b);
        return b;
    }

    // Коричневая бутылка с этикеткой, крышкой и красными глазками
    static void BuildModel(Transform root, Team team)
    {
        var model = new GameObject("Model").transform;
        model.SetParent(root, false);
        var glass = RuntimeMats.Lit(new Color(0.36f, 0.18f, 0.05f));
        var label = RuntimeMats.Lit(new Color(0.95f, 0.8f, 0.4f));
        var cap = RuntimeMats.Lit(new Color(0.85f, 0.65f, 0.15f));
        var eye = RuntimeMats.Glow(new Color(1f, 0.15f, 0.08f));
        var leg = RuntimeMats.Lit(new Color(0.12f, 0.1f, 0.12f));

        RuntimeMats.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 0.02f, 0), new Vector3(0.9f, 0.01f, 0.9f), RuntimeMats.Glow(TeamUtil.GetColor(team)));
        // гладкая бутылка (тело вращения), этикетка, крышка-корона
        var body = MeshObj(model, "Glass", ProcMesh.Bottle, new Vector3(0, 0.12f, 0), Quaternion.identity, Vector3.one * 1.25f,
            glassMat != null ? glassMat : (glassMat = Glossy(new Color(0.36f, 0.17f, 0.04f), 0.95f)));
        MeshObj(body.transform, "Label", LabelMesh, Vector3.zero, Quaternion.identity, Vector3.one, label);
        MeshObj(body.transform, "Cap", CapMesh, new Vector3(0, 0.975f, 0), Quaternion.identity, Vector3.one, cap);
        foreach (float sd in new[] { -1f, 1f })
            RuntimeMats.Prim(PrimitiveType.Sphere, model, new Vector3(0.08f * sd, 0.82f, -0.19f), Vector3.one * 0.08f, eye);   // глазки (перед — минус Z)
        // шесть изогнутых паучьих ножек
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + 30f) * Mathf.Deg2Rad;
            MeshObj(model, "Leg", LegMesh, new Vector3(Mathf.Cos(a) * 0.17f, 0.35f, Mathf.Sin(a) * 0.17f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), Vector3.one, leg);
        }
        model.localRotation = Quaternion.Euler(0, 180, 0);
    }

    static Mesh labelMesh, capMesh, legMesh;
    static Mesh LabelMesh => labelMesh != null ? labelMesh : (labelMesh = ProcMesh.Lathe(new[] { new Vector2(0.168f, 0.18f), new Vector2(0.168f, 0.42f) }, 20));
    static Mesh CapMesh => capMesh != null ? capMesh : (capMesh = ProcMesh.Torus(0.062f, 0.02f, 16, 6));
    // ножка вдоль +X: вверх-наружу, потом вниз к земле
    static Mesh LegMesh => legMesh != null ? legMesh : (legMesh = ProcMesh.Tube(
        ProcMesh.Bezier(Vector3.zero, new Vector3(0.18f, 0.2f, 0), new Vector3(0.38f, 0.15f, 0), new Vector3(0.45f, -0.33f, 0), 8), ProcMesh.Taper(8, 0.045f, 0.01f), 6));

    static Material Glossy(Color c, float smooth)
    {
        var m = new Material(RuntimeMats.Lit(c));
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.1f);
        return m;
    }
    static Material glassMat;

    static GameObject MeshObj(Transform parent, string name, Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    void Awake()
    {
        unit = GetComponent<Unit>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<AutoAttack>();
        status = GetComponent<StatusEffects>();
        dieAt = Time.time + LifeTime;
    }

    void Update()
    {
        if (!unit.IsAlive) return;
        if (owner == null || Time.time >= dieAt) { Expire(); return; }
        if (ownerUnit == null) ownerUnit = owner.GetComponent<Unit>();

        // в пивной пене тоже быстрее и лечится
        if (owner.InFoam(transform.position, out float sp, out float regen))
        {
            health.Heal(regen * Time.deltaTime);
            if (Time.time >= foamTick) { foamTick = Time.time + 0.25f; status.AddMoveSpeed(sp, 0.25f, "Пивная пена", "pivo_foam"); }
        }

        if (Time.time < think) return;
        think = Time.time + 0.3f;
        if (!agent.isOnNavMesh) return;

        // бьём цель хозяина, если она рядом
        var ot = ownerUnit != null && ownerUnit.IsAlive && ownerUnit.Attack != null ? ownerUnit.Attack.Target : null;
        if (ot != null && ot != attack.Target && ot.IsAlive && unit.IsEnemyOf(ot) && ot.DistanceFrom(transform.position) < 12f) { attack.SetTarget(ot); return; }

        // далеко убежали — возвращаемся к хозяину
        if (ownerUnit != null && ownerUnit.IsAlive)
        {
            float d = Vector3.Distance(transform.position, ownerUnit.transform.position);
            if (d > Leash + 6f || (attack.Target == null && d > Leash))
            {
                attack.OrderMove();
                agent.isStopped = false;
                agent.SetDestination(ownerUnit.transform.position);
            }
        }
    }

    void Expire()
    {
        ParticleBurst.Spawn(transform.position + Vector3.up * 0.8f, PivmaverKit.Amber, 8, 3f);
        Destroy(gameObject);
    }
}
