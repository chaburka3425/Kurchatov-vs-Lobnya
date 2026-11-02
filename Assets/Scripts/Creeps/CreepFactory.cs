using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Создаёт крипов прямо во время игры (из простых фигур).
public static class CreepFactory
{
    static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();

    public static GameObject Create(Team team, bool ranged, Vector3 pos, Vector3 destination, float power) =>
        Create(team, ranged, pos, new[] { destination }, power);

    public static GameObject Create(Team team, bool ranged, Vector3 pos, Vector3[] path, float power)
    {
        Vector3 destination = path[path.Length - 1];
        string city = team == Team.Kurchatov ? "Курчатова" : "Москвы";
        string name = ranged ? $"Пацан с рогаткой из {city}" : $"Пацан из {city}";

        var root = new GameObject(name);
        root.transform.position = pos;

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.85f, 0);
        col.height = 1.7f;
        col.radius = 0.45f;

        var agent = root.AddComponent<NavMeshAgent>();
        agent.baseOffset = 0f;
        agent.radius = 0.45f;
        agent.height = 1.7f;
        agent.speed = 4.2f;
        agent.angularSpeed = 600f;
        agent.acceleration = 20f;
        agent.avoidancePriority = Random.Range(40, 60);
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);

        var unit = root.AddComponent<Unit>();
        unit.team = team;
        unit.kind = UnitKind.Creep;
        unit.displayName = name;
        unit.xpBounty = ranged ? 60 : 50;
        unit.goldBounty = ranged ? 48 : 38;

        var health = root.AddComponent<Health>();
        health.maxHealth = (ranged ? 220f : 300f) * power;
        health.armor = ranged ? 0f : 2f;
        health.regenPerSecond = 0.5f;
        health.destroyOnDeath = true;

        root.AddComponent<StatusEffects>();

        var attack = root.AddComponent<AutoAttack>();
        attack.damage = (ranged ? 24f : 18f) * power;
        attack.range = ranged ? 6f : 1.6f;
        attack.baseAttackTime = 1f;
        attack.attackSpeed = 100f;
        attack.acquireWhileMoving = true;
        attack.acquireRange = ranged ? 8f : 7f;
        attack.loseRange = 11f;
        attack.ranged = ranged;
        attack.projectileColor = new Color(0.6f, 0.45f, 0.3f);

        var ai = root.AddComponent<CreepAI>();
        ai.destination = destination;
        ai.path = path;

        var bar = root.AddComponent<HealthBarGUI>();
        bar.width = 42f;
        bar.height = 5f;
        bar.heightOffset = 2.2f;

        // --- Внешний вид ---
        var tc = TeamUtil.GetColor(team);
        var skin = new Color(0.96f, 0.78f, 0.66f);
        Part(root, PrimitiveType.Capsule, new Vector3(0, 0.75f, 0), new Vector3(0.75f, 0.55f, 0.6f), tc);           // худи цвета города
        Part(root, PrimitiveType.Sphere, new Vector3(0, 1.5f, 0), Vector3.one * 0.5f, skin);                       // голова
        Part(root, PrimitiveType.Sphere, new Vector3(0, 1.68f, 0.02f), new Vector3(0.52f, 0.22f, 0.52f), tc * 0.7f); // кепка
        Part(root, PrimitiveType.Cube, new Vector3(0, 1.62f, 0.25f), new Vector3(0.38f, 0.04f, 0.3f), tc * 0.7f);    // козырёк
        Part(root, PrimitiveType.Capsule, new Vector3(-0.18f, 0.25f, 0), new Vector3(0.25f, 0.25f, 0.25f), new Color(0.1f, 0.1f, 0.12f));
        Part(root, PrimitiveType.Capsule, new Vector3(0.18f, 0.25f, 0), new Vector3(0.25f, 0.25f, 0.25f), new Color(0.1f, 0.1f, 0.12f));

        if (ranged)
        {
            // Рогатка
            var wood = new Color(0.5f, 0.32f, 0.15f);
            Part(root, PrimitiveType.Cube, new Vector3(0.42f, 0.9f, 0.25f), new Vector3(0.06f, 0.35f, 0.06f), wood);
            Part(root, PrimitiveType.Cube, new Vector3(0.36f, 1.12f, 0.25f), new Vector3(0.05f, 0.18f, 0.05f), wood)
                .transform.localRotation = Quaternion.Euler(0, 0, 25);
            Part(root, PrimitiveType.Cube, new Vector3(0.48f, 1.12f, 0.25f), new Vector3(0.05f, 0.18f, 0.05f), wood)
                .transform.localRotation = Quaternion.Euler(0, 0, -25);
        }
        else
        {
            // Палка
            Part(root, PrimitiveType.Cube, new Vector3(0.42f, 0.9f, 0.3f), new Vector3(0.08f, 0.08f, 0.8f), new Color(0.45f, 0.3f, 0.15f));
        }
        return root;
    }

    // Лесной гопник: стоит в лагере, дерётся, только если его ударить.
    public static GameObject CreateNeutral(int camp, Vector3 pos, bool boss)
    {
        string name = boss ? "Главный гопник" : "Гопник с района";
        var root = new GameObject(name);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        float k = boss ? 1.35f : 1f;

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.85f * k, 0);
        col.height = 1.7f * k;
        col.radius = 0.45f * k;

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.45f * k;
        agent.height = 1.7f * k;
        agent.speed = 4f;
        agent.angularSpeed = 600f;
        agent.acceleration = 20f;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);

        var unit = root.AddComponent<Unit>();
        unit.team = Team.Neutral;
        unit.kind = UnitKind.Creep;
        unit.displayName = name;
        unit.xpBounty = boss ? 90 : 45;
        unit.goldBounty = boss ? 75 : 40;

        var health = root.AddComponent<Health>();
        health.maxHealth = boss ? 700f : 380f;
        health.armor = boss ? 3f : 1f;
        health.regenPerSecond = 1f;
        health.destroyOnDeath = true;

        root.AddComponent<StatusEffects>();
        var attack = root.AddComponent<AutoAttack>();
        attack.damage = boss ? 34f : 20f;
        attack.range = 1.7f;
        attack.baseAttackTime = boss ? 1.4f : 1.1f;
        attack.autoAcquire = false;
        attack.loseRange = 9f;

        var ai = root.AddComponent<NeutralAI>();
        ai.camp = camp;
        ai.home = pos;

        var bar = root.AddComponent<HealthBarGUI>();
        bar.width = boss ? 60f : 42f;
        bar.height = 5f;
        bar.heightOffset = 2.2f * k;
        bar.showName = boss;

        // Серый спортивный костюм, кепка, семечки
        var suit = boss ? new Color(0.2f, 0.2f, 0.24f) : new Color(0.36f, 0.36f, 0.4f);
        var skin = new Color(0.96f, 0.78f, 0.66f);
        Part(root, PrimitiveType.Capsule, new Vector3(0, 0.75f, 0) * k, new Vector3(0.8f, 0.55f, 0.65f) * k, suit);
        Part(root, PrimitiveType.Cube, new Vector3(0, 0.95f, 0.33f) * k, new Vector3(0.04f, 0.6f, 0.02f) * k, new Color(0.9f, 0.9f, 0.9f));
        Part(root, PrimitiveType.Sphere, new Vector3(0, 1.5f, 0) * k, Vector3.one * 0.5f * k, skin);
        Part(root, PrimitiveType.Sphere, new Vector3(0, 1.68f, 0.02f) * k, new Vector3(0.52f, 0.2f, 0.52f) * k, new Color(0.08f, 0.08f, 0.1f));
        Part(root, PrimitiveType.Cube, new Vector3(0, 1.62f, 0.25f) * k, new Vector3(0.38f, 0.04f, 0.3f) * k, new Color(0.08f, 0.08f, 0.1f));
        Part(root, PrimitiveType.Capsule, new Vector3(-0.18f, 0.25f, 0) * k, new Vector3(0.25f, 0.25f, 0.25f) * k, suit * 0.8f);
        Part(root, PrimitiveType.Capsule, new Vector3(0.18f, 0.25f, 0) * k, new Vector3(0.25f, 0.25f, 0.25f) * k, suit * 0.8f);
        if (boss) Part(root, PrimitiveType.Cube, new Vector3(0.45f, 0.95f, 0.3f) * k, new Vector3(0.09f, 0.09f, 0.9f) * k, new Color(0.3f, 0.3f, 0.32f)); // труба
        return root;
    }

    static GameObject Part(GameObject root, PrimitiveType type, Vector3 pos, Vector3 scale, Color c)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat(c);
        return go;
    }

    static Material Mat(Color c)
    {
        c.a = 1f;
        if (mats.TryGetValue(c, out var m) && m != null) return m;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        mats[c] = m;
        return m;
    }
}
