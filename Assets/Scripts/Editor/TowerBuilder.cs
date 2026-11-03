using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

// Собирает башни из простых фигур.
// Курчатов — кафе «Чикен Пицца» с огромной пиццей и куриной ножкой на крыше.
// Москва — «Бургер Кинг» с огромным бургером на крыше.
public static class TowerBuilder
{
    // ---------- Курчатов: Чикен Пицца ----------
    public static GameObject BuildChickenPizza(string name, Vector3 pos)
    {
        var root = CreateTowerRoot(name, Team.Kurchatov, pos);
        var t = root.transform;
        Cafe(t, "CP", new Color(1f, 0.62f, 0.18f), new Color(0.72f, 0.13f, 0.09f), new Color(0.98f, 0.95f, 0.88f),
             "ЧИКЕН ПИЦЦА", new Color(1f, 0.9f, 0.55f), new Color(1f, 0.3f, 0.2f), TeamUtil.GetColor(Team.Kurchatov));

        // Огромная пицца на крыше (наклонена к камере): толстый бортик, сыр, пепперони, базилик и капли сыра
        var pizza = Pivot(t, "Pizza", new Vector3(0, 3.95f, 0), Quaternion.Euler(-14, 0, 0));
        BuildParts.Lathe(pizza, "PizzaBase", "CP_PizzaBase", new[] { new Vector2(0f, -0.08f), new Vector2(1.75f, -0.08f), new Vector2(1.82f, 0f), new Vector2(0f, 0f) },
            Vector3.zero, Vector3.one, "CP_Crust", new Color(0.86f, 0.58f, 0.28f), 0f, 0.3f, 40);
        BuildParts.Torus(pizza, "PizzaRim", "CP_PizzaRim", 1.72f, 0.16f, new Vector3(0, 0.06f, 0), Quaternion.identity, new Vector3(1f, 0.9f, 1f), "CP_Crust", new Color(0.86f, 0.58f, 0.28f), 0f, 0.3f);
        BuildParts.Lathe(pizza, "Cheese", "CP_Cheese", new[] { new Vector2(0f, 0f), new Vector2(1.62f, 0f), new Vector2(1.6f, 0.06f), new Vector2(0f, 0.12f) },
            Vector3.zero, Vector3.one, "CP_Cheese", new Color(1f, 0.82f, 0.28f), 0f, 0.65f, 40);
        var rnd = new System.Random(3);
        for (int i = 0; i < 11; i++)
        {
            float a = i * 2.4f, r = 0.35f + (i % 4) * 0.33f;
            BuildParts.Lathe(pizza, "Pepperoni", "CP_Pepperoni", new[] { new Vector2(0f, 0f), new Vector2(0.24f, 0f), new Vector2(0.25f, 0.03f), new Vector2(0.2f, 0.05f), new Vector2(0f, 0.055f) },
                new Vector3(Mathf.Cos(a) * r, 0.1f, Mathf.Sin(a) * r), Vector3.one, "CP_Pepperoni", new Color(0.72f, 0.14f, 0.1f), 0f, 0.6f, 16);
            if (i % 3 == 0) BuildParts.Prim(pizza, "Basil", PrimitiveType.Sphere, new Vector3(Mathf.Cos(a + 1.2f) * r * 0.8f, 0.13f, Mathf.Sin(a + 1.2f) * r * 0.8f),
                new Vector3(0.22f, 0.03f, 0.12f), "CP_Basil", new Color(0.2f, 0.55f, 0.15f), 0f, 0.5f, Quaternion.Euler(0, (float)rnd.NextDouble() * 180f, 0));
        }
        for (int i = 0; i < 6; i++)   // сыр стекает с бортика
        {
            float a = i * 1.05f + 0.3f;
            Vector3 p0 = new Vector3(Mathf.Cos(a) * 1.75f, 0.12f, Mathf.Sin(a) * 1.75f);
            BuildParts.Tube(pizza, "CheeseDrip", "CP_Drip" + i, new[] { p0, p0 + new Vector3(Mathf.Cos(a) * 0.12f, -0.1f, Mathf.Sin(a) * 0.12f), p0 + new Vector3(Mathf.Cos(a) * 0.14f, -0.35f - (i % 3) * 0.12f, Mathf.Sin(a) * 0.14f) },
                new[] { 0.07f, 0.06f, 0.045f }, "CP_Cheese", new Color(1f, 0.82f, 0.28f), 0f, 0.65f, 8);
        }

        // Куриная ножка стоит на пицце: мясо + кость с двумя шишечками
        var leg = Pivot(t, "Drumstick", new Vector3(1.05f, 4.05f, 0.65f), Quaternion.Euler(0, 0, -12));
        BuildParts.Lathe(leg, "Meat", "CP_Meat", new[] { new Vector2(0f, 0f), new Vector2(0.35f, 0.05f), new Vector2(0.55f, 0.35f), new Vector2(0.58f, 0.7f), new Vector2(0.45f, 1.05f),
            new Vector2(0.22f, 1.3f), new Vector2(0.14f, 1.4f), new Vector2(0f, 1.42f) }, Vector3.zero, Vector3.one, "CP_Chicken", new Color(0.78f, 0.44f, 0.14f), 0f, 0.55f, 24);
        BuildParts.Lathe(leg, "Bone", "CP_Bone", new[] { new Vector2(0f, 1.3f), new Vector2(0.1f, 1.3f), new Vector2(0.09f, 1.85f), new Vector2(0f, 1.85f) },
            Vector3.zero, Vector3.one, "CP_Bone", new Color(0.97f, 0.95f, 0.9f), 0f, 0.5f, 12);
        foreach (float sd in new[] { -1f, 1f })
            BuildParts.Prim(leg, "BoneKnob", PrimitiveType.Sphere, new Vector3(0.11f * sd, 1.93f, 0), Vector3.one * 0.28f, "CP_Bone", new Color(0.97f, 0.95f, 0.9f), 0f, 0.5f);

        // Стреляет пиццей
        var tower = root.GetComponent<Tower>();
        tower.projectileShape = ProjectileShape.Disc;
        tower.projectileColor = new Color(1f, 0.75f, 0.25f);
        tower.projectileSize = 0.9f;
        return root;
    }

    // ---------- Москва: Бургер Кинг ----------
    public static GameObject BuildBurger(string name, Vector3 pos)
    {
        var root = CreateTowerRoot(name, Team.Lobnya, pos);
        var t = root.transform;
        Cafe(t, "BG", new Color(0.55f, 0.28f, 0.15f), new Color(1f, 0.5f, 0.1f), new Color(0.96f, 0.86f, 0.66f),
             "БУРГЕР КИНГ", new Color(1f, 0.8f, 0.3f), new Color(1f, 0.55f, 0.1f), TeamUtil.GetColor(Team.Lobnya));

        // Огромный бургер на крыше: булки, котлета, сыр уголками, салат волной, помидоры, кунжут
        var bun = new Color(0.9f, 0.57f, 0.24f);
        var b = Pivot(t, "Burger", new Vector3(0, 3.6f, 0), Quaternion.identity);
        BuildParts.Lathe(b, "BunBottom", "BG_BunBottom", new[] { new Vector2(0f, 0f), new Vector2(1.25f, 0f), new Vector2(1.45f, 0.1f), new Vector2(1.5f, 0.28f), new Vector2(1.42f, 0.4f), new Vector2(0f, 0.42f) },
            Vector3.zero, Vector3.one, "BG_Bun", bun, 0f, 0.45f, 36);
        BuildParts.Lathe(b, "Patty", "BG_Patty", new[] { new Vector2(0f, 0.4f), new Vector2(1.5f, 0.4f), new Vector2(1.62f, 0.5f), new Vector2(1.6f, 0.7f), new Vector2(1.48f, 0.78f), new Vector2(0f, 0.78f) },
            Vector3.zero, Vector3.one, "BG_Patty", new Color(0.32f, 0.16f, 0.07f), 0f, 0.25f, 36);
        var cheese = BuildParts.Prim(b, "Cheese", PrimitiveType.Cube, new Vector3(0, 0.82f, 0), new Vector3(2.5f, 0.06f, 2.5f), "BG_Cheese", new Color(1f, 0.8f, 0.15f), 0f, 0.6f, Quaternion.Euler(0, 45, 0));
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f;
            Vector3 c0 = new Vector3(Mathf.Cos(a) * 1.72f, 0.82f, Mathf.Sin(a) * 1.72f);
            BuildParts.Tube(b, "CheeseCorner", "BG_CheeseCorner" + i, new[] { c0 * 0.85f + Vector3.up * 0.82f * 0.15f, c0, c0 + new Vector3(Mathf.Cos(a) * 0.1f, -0.3f, Mathf.Sin(a) * 0.1f) },
                new[] { 0.06f, 0.05f, 0.03f }, "BG_Cheese", new Color(1f, 0.8f, 0.15f), 0f, 0.6f, 6);
        }
        BuildParts.Torus(b, "Lettuce", "BG_Lettuce", 1.5f, 0.11f, new Vector3(0, 0.92f, 0), Quaternion.identity, new Vector3(1f, 0.6f, 1f), "BG_Lettuce", new Color(0.35f, 0.75f, 0.2f), 0f, 0.55f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f + 0.4f;
            BuildParts.Lathe(b, "Tomato", "BG_Tomato", new[] { new Vector2(0f, 0f), new Vector2(0.45f, 0f), new Vector2(0.48f, 0.05f), new Vector2(0.45f, 0.1f), new Vector2(0f, 0.1f) },
                new Vector3(Mathf.Cos(a) * 0.75f, 0.95f, Mathf.Sin(a) * 0.75f), Vector3.one, "BG_Tomato", new Color(0.88f, 0.15f, 0.1f), 0f, 0.7f, 18);
        }
        var topProfile = new[] { new Vector2(0f, 1.0f), new Vector2(1.45f, 1.0f), new Vector2(1.52f, 1.12f), new Vector2(1.45f, 1.45f), new Vector2(1.2f, 1.8f),
                                 new Vector2(0.8f, 2.02f), new Vector2(0.4f, 2.12f), new Vector2(0f, 2.15f) };
        BuildParts.Lathe(b, "BunTop", "BG_BunTop", topProfile, Vector3.zero, Vector3.one, "BG_Bun", bun, 0f, 0.5f, 36);
        var rnd = new System.Random(5);
        for (int i = 0; i < 22; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f, rr = Mathf.Sqrt((float)rnd.NextDouble()) * 1.3f;
            float y = 1.0f + 1.15f * Mathf.Sqrt(Mathf.Max(0f, 1f - (rr / 1.5f) * (rr / 1.5f)));
            BuildParts.Prim(b, "Seed", PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr), new Vector3(0.14f, 0.06f, 0.09f), "BG_Seed",
                new Color(1f, 0.96f, 0.84f), 0f, 0.5f, Quaternion.Euler((float)rnd.NextDouble() * 30f, (float)rnd.NextDouble() * 180f, 0));
        }

        // Стреляет бургерами
        var tower = root.GetComponent<Tower>();
        tower.projectileShape = ProjectileShape.Ball;
        tower.projectileColor = bun;
        tower.projectileSize = 0.7f;
        return root;
    }

    // Общий «кафе-домик»: каменное основание-восьмиугольник, стены с угловыми колоннами, светящиеся окна,
    // полосатый козырёк, неоновая вывеска, фонари, светящееся кольцо команды и маяк-кристалл над крышей.
    static void Cafe(Transform t, string k, Color walls, Color trim, Color stripe, string sign, Color neonText, Color neon, Color team)
    {
        var stone = new Color(0.5f, 0.49f, 0.47f);
        var warm = new Color(1f, 0.82f, 0.45f);
        BuildParts.Lathe(t, "Base", "Tower_Base", new[] { new Vector2(0f, 0f), new Vector2(2.85f, 0f), new Vector2(2.85f, 0.22f), new Vector2(2.6f, 0.28f), new Vector2(2.6f, 0.45f), new Vector2(0f, 0.45f) },
            Vector3.zero, Vector3.one, "Tower_Stone", stone, 0f, 0.25f, 8, Quaternion.Euler(0, 22.5f, 0));
        BuildParts.GlowMesh(t, "TeamRing", BuildParts.Save("Tower_TeamRing", ProcMesh.Torus(2.95f, 0.06f, 48, 6)), new Vector3(0, 0.05f, 0), Quaternion.identity, Vector3.one, "Tower_Ring_" + k, team);

        // стены и колонны по углам
        BuildParts.Prim(t, "Walls", PrimitiveType.Cube, new Vector3(0, 1.85f, 0), new Vector3(3.8f, 2.8f, 3.8f), k + "_Walls", walls, 0f, 0.3f);
        BuildParts.Prim(t, "Stripe", PrimitiveType.Cube, new Vector3(0, 0.7f, 0), new Vector3(3.86f, 0.45f, 3.86f), k + "_Stripe", stripe, 0f, 0.4f);
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { -1f, 1f })
                BuildParts.Lathe(t, "Pillar", "Tower_Pillar", new[] { new Vector2(0f, 0f), new Vector2(0.3f, 0f), new Vector2(0.3f, 0.15f), new Vector2(0.22f, 0.25f), new Vector2(0.22f, 2.55f),
                    new Vector2(0.3f, 2.65f), new Vector2(0.3f, 2.8f), new Vector2(0f, 2.8f) }, new Vector3(1.9f * x, 0.45f, 1.9f * z), Vector3.one, k + "_Trim", trim, 0.1f, 0.5f, 16);
        BuildParts.Prim(t, "RoofTrim", PrimitiveType.Cube, new Vector3(0, 3.35f, 0), new Vector3(4.3f, 0.3f, 4.3f), k + "_Trim", trim, 0.1f, 0.5f);
        BuildParts.Prim(t, "Parapet", PrimitiveType.Cube, new Vector3(0, 3.55f, 0), new Vector3(4.0f, 0.15f, 4.0f), k + "_Stripe", stripe, 0f, 0.4f);

        // окна с тёплым светом и рамами — спереди и по бокам
        foreach (var (pos, rot) in new[] { (new Vector3(-1.15f, 1.95f, -1.91f), 0f), (new Vector3(1.15f, 1.95f, -1.91f), 0f),
                                           (new Vector3(-1.91f, 1.95f, 0f), 90f), (new Vector3(1.91f, 1.95f, 0f), -90f) })
        {
            var q = Quaternion.Euler(0, rot, 0);
            BuildParts.Glow(t, "WindowLight", PrimitiveType.Cube, pos, new Vector3(1.0f, 0.9f, 0.04f), "Tower_Window", warm, q);
            BuildParts.Prim(t, "WindowFrame", PrimitiveType.Cube, pos + q * new Vector3(0, 0.5f, -0.02f), new Vector3(1.15f, 0.1f, 0.08f), k + "_Trim", trim, 0.1f, 0.5f, q);
            BuildParts.Prim(t, "WindowSill", PrimitiveType.Cube, pos + q * new Vector3(0, -0.5f, -0.04f), new Vector3(1.2f, 0.08f, 0.14f), k + "_Stripe", stripe, 0f, 0.4f, q);
            BuildParts.Prim(t, "WindowCross", PrimitiveType.Cube, pos + q * new Vector3(0, 0, -0.03f), new Vector3(0.06f, 0.9f, 0.05f), k + "_Trim", trim, 0.1f, 0.5f, q);
        }

        // дверь, ступенька, ручка
        BuildParts.Prim(t, "Door", PrimitiveType.Cube, new Vector3(0, 1.3f, -1.92f), new Vector3(0.95f, 1.7f, 0.06f), k + "_Door", new Color(0.3f, 0.18f, 0.1f), 0f, 0.4f);
        BuildParts.Prim(t, "DoorFrame", PrimitiveType.Cube, new Vector3(0, 2.2f, -1.94f), new Vector3(1.15f, 0.12f, 0.08f), k + "_Trim", trim, 0.1f, 0.5f);
        BuildParts.Glow(t, "DoorKnob", PrimitiveType.Sphere, new Vector3(0.32f, 1.25f, -1.97f), Vector3.one * 0.09f, "Tower_Brass", new Color(1f, 0.85f, 0.4f));
        BuildParts.Prim(t, "Step", PrimitiveType.Cube, new Vector3(0, 0.52f, -2.15f), new Vector3(1.4f, 0.14f, 0.5f), "Tower_Stone", stone, 0f, 0.25f);

        // полосатый козырёк над дверью
        for (int i = 0; i < 7; i++)
        {
            float x = Mathf.Lerp(-1.2f, 1.2f, i / 6f);
            BuildParts.Prim(t, "Awning", PrimitiveType.Cube, new Vector3(x, 2.5f, -2.3f), new Vector3(0.36f, 0.05f, 0.85f), i % 2 == 0 ? k + "_Trim" : k + "_Stripe", i % 2 == 0 ? trim : stripe, 0f, 0.4f,
                Quaternion.Euler(-25, 0, 0));
        }

        // неоновая вывеска: тёмная доска, светящаяся рамка, яркий текст
        BuildParts.Prim(t, "Sign", PrimitiveType.Cube, new Vector3(0, 3.0f, -2.05f), new Vector3(3.5f, 0.55f, 0.1f), "Tower_SignBoard", new Color(0.08f, 0.06f, 0.06f), 0f, 0.6f);
        foreach (var (p, s) in new[] { (new Vector3(0, 3.27f, -2.11f), new Vector3(3.5f, 0.05f, 0.04f)), (new Vector3(0, 2.73f, -2.11f), new Vector3(3.5f, 0.05f, 0.04f)),
                                       (new Vector3(1.73f, 3f, -2.11f), new Vector3(0.05f, 0.55f, 0.04f)), (new Vector3(-1.73f, 3f, -2.11f), new Vector3(0.05f, 0.55f, 0.04f)) })
            BuildParts.Glow(t, "Neon", PrimitiveType.Cube, p, s, "Tower_Neon_" + k, neon);
        BuildParts.Text(t, sign, new Vector3(0, 3.0f, -2.12f), neonText, 0.05f);

        // фонари у входа
        BuildParts.Lamp(t, "Warm", new Vector3(-1.6f, 0.45f, -2.55f), warm, 2.1f);
        BuildParts.Lamp(t, "Warm", new Vector3(1.6f, 0.45f, -2.55f), warm, 2.1f);

        // маяк над крышей: кристалл цвета команды с вращающимся кольцом и искрами
        var beacon = new GameObject("Beacon").transform;
        beacon.SetParent(t, false);
        beacon.localPosition = new Vector3(-1.3f, 3.65f, 1.3f);
        BuildParts.Lathe(beacon, "BeaconPost", "Tower_BeaconPost", new[] { new Vector2(0f, 0f), new Vector2(0.18f, 0f), new Vector2(0.08f, 0.2f), new Vector2(0.06f, 1.4f), new Vector2(0.16f, 1.5f), new Vector2(0f, 1.55f) },
            Vector3.zero, Vector3.one, "Tower_Iron", new Color(0.15f, 0.15f, 0.18f), 0.7f, 0.6f, 12);
        BuildParts.Glow(beacon, "Crystal", PrimitiveType.Sphere, new Vector3(0, 1.85f, 0), new Vector3(0.35f, 0.6f, 0.35f), "Tower_Crystal_" + k, Color.Lerp(team, Color.white, 0.25f), Quaternion.Euler(0, 45, 0));
        var spin = new GameObject("Orbit").transform;
        spin.SetParent(beacon, false);
        spin.localPosition = new Vector3(0, 1.85f, 0);
        spin.localRotation = Quaternion.Euler(20, 0, 10);
        var sfx = spin.gameObject.AddComponent<SpinFx>(); sfx.speed = 90f; sfx.bob = 0.05f;
        BuildParts.GlowMesh(spin, "Halo", BuildParts.Save("Tower_Halo", ProcMesh.Torus(0.5f, 0.03f, 32, 6)), Vector3.zero, Quaternion.identity, Vector3.one, "Tower_Ring_" + k, team);
        BuildParts.Ambient(beacon, "Sparkle", new Vector3(0, 1.85f, 0), AmbientFx.Kind.Sparkle, Color.Lerp(team, Color.white, 0.4f), 0.5f, 0.3f, 1);
    }

    static Transform Pivot(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        var p = new GameObject(name).transform;
        p.SetParent(parent, false);
        p.localPosition = pos; p.localRotation = rot;
        return p;
    }

    // ---------- Общее ----------
    static GameObject CreateTowerRoot(string name, Team team, Vector3 pos)
    {
        var root = new GameObject(name);
        root.transform.position = pos;

        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0, 2.2f, 0);
        box.size = new Vector3(4.4f, 4.4f, 4.4f);

        // В NavMesh башню не запекаем: дыру в сетке вырезает препятствие, и после разрушения она исчезает
        var mod = root.AddComponent<NavMeshModifier>();
        mod.ignoreFromBuild = true;
        var obstacle = root.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = new Vector3(0, 1.5f, 0);
        obstacle.size = new Vector3(3.4f, 3f, 3.4f);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;

        var unit = root.AddComponent<Unit>();
        unit.team = team;
        unit.kind = UnitKind.Structure;
        unit.displayName = name;

        unit.xpBounty = 100;
        unit.goldBounty = 150;

        var health = root.AddComponent<Health>();
        health.maxHealth = 700;
        health.armor = 5;
        health.regenPerSecond = 0;

        var tower = root.AddComponent<Tower>();
        tower.damage = 40;
        tower.range = 9;
        tower.attackInterval = 1.5f;
        tower.firePointHeight = 4.5f;

        var bar = root.AddComponent<HealthBarGUI>();
        bar.width = 100;
        bar.height = 8;
        bar.heightOffset = 7f;
        bar.showName = true;
        return root;
    }

    static void Windows(GameObject root, string mat)
    {
        var glass = new Color(0.15f, 0.25f, 0.35f);
        Part(root, "WindowL", PrimitiveType.Cube, new Vector3(-1.2f, 1.4f, -2.01f), new Vector3(1.2f, 1.1f, 0.05f), mat, glass);
        Part(root, "WindowR", PrimitiveType.Cube, new Vector3(1.2f, 1.4f, -2.01f), new Vector3(1.2f, 1.1f, 0.05f), mat, glass);
    }

    static GameObject Part(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, string matName, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // клики ловит общий коллайдер башни
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        MapBuilder.Paint(go, matName, color);
        return go;
    }

    static void SignText(GameObject root, string text, Vector3 localPos, Color color, float charSize)
    {
        var go = new GameObject("SignText");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 60;
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var font = GameFont.Get();
        if (font != null)
        {
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }
}
