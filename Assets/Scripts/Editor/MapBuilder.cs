using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Меню Tools → MOBA → «Создать карту Курчатов vs Москва».
// Одной кнопкой собирает тестовую сцену: земля, линия, два трона, герой, враги, камера.
public static class MapBuilder
{
    public const string ScenePath ="Assets/Scenes/KurchatovVsLobnya.unity";
    const string NavMeshPath = "Assets/Scenes/KurchatovVsLobnya_NavMesh.asset";
    const string MatFolder = "Assets/Materials";

    [MenuItem("Tools/MOBA/Создать карту Курчатов vs Москва")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MatFolder);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Земля и центральная линия ---
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(12 * LaneData.Scale, 1, 12 * LaneData.Scale); // 120×120 × масштаб
        Paint(ground, "Ground", new Color(0.33f, 0.55f, 0.3f));

        var lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lane.name = "MidLane";
        lane.transform.position = new Vector3(0, 0.01f, 0);
        lane.transform.rotation = Quaternion.Euler(0, 45, 0);
        lane.transform.localScale = new Vector3(LaneData.LaneHalfWidth * 2f, 0.02f, 130 * LaneData.Scale);
        Object.DestroyImmediate(lane.GetComponent<Collider>());
        Paint(lane, "Lane", new Color(0.6f, 0.5f, 0.35f));

        // Боковые линии: верх (левый и верхний край) и низ (нижний и правый край)
        int ri = 0;
        foreach (var r in LaneData.SideRects)
        {
            var side = GameObject.CreatePrimitive(PrimitiveType.Cube);
            side.name = "SideLane_" + (ri++);
            side.transform.position = new Vector3((r.x + r.z) / 2f, 0.011f, (r.y + r.w) / 2f);
            side.transform.localScale = new Vector3(r.z - r.x, 0.02f, r.w - r.y);
            Object.DestroyImmediate(side.GetComponent<Collider>());
            Paint(side, "Lane", new Color(0.6f, 0.5f, 0.35f));
        }

        // Полянки лесных лагерей
        foreach (var c in LaneData.Camps)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = "CampGround";
            g.transform.position = c.pos + Vector3.up * 0.012f;
            g.transform.localScale = new Vector3(6.5f, 0.01f, 5f);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            Paint(g, "CampGround", new Color(0.36f, 0.33f, 0.22f));
        }

        // --- Красивая земля, дороги, река с мостом, площади, свет и пост-обработка ---
        MapLookBuilder.Apply(ground);

        // --- Троны ---
        var kurchatovThrone = CreateThrone("Трон Курчатова", Team.Kurchatov, LaneData.Throne(Team.Kurchatov));
        var lobnyaThrone = CreateThrone("Трон Москвы", Team.Lobnya, LaneData.Throne(Team.Lobnya));

        // --- Башни: по 2 на каждой из трёх линий ---
        // Курчатов — «Чикен Пицца», Москва — «Бургер Кинг». Вторая башня (ближе к трону) намного сильнее.
        // Порядок: башня 1 → башня 2 на линии; трон открывается, когда сломана любая вторая башня.
        foreach (var team in new[] { Team.Kurchatov, Team.Lobnya })
        {
            var tier1 = new Dictionary<LaneData.Lane, GameObject>();
            var tier2 = new List<Health>();
            foreach (var spot in LaneData.Towers(team))
            {
                string nm = (team == Team.Kurchatov ? "Чикен Пицца" : "Бургер Кинг") + $" ({LaneData.Ru(spot.lane)} {spot.tier})";
                var t = team == Team.Kurchatov ? TowerBuilder.BuildChickenPizza(nm, spot.pos) : TowerBuilder.BuildBurger(nm, spot.pos);
                var tower = t.GetComponent<Tower>();
                tower.lane = spot.lane;
                tower.tier = spot.tier;
                var tu = t.GetComponent<Unit>();
                var th = t.GetComponent<Health>();
                if (spot.tier == 2)
                {
                    // вторая башня (у трона) — крепость: много здоровья и брони, бьёт сильно и часто
                    th.maxHealth = 2400f; th.armor = 13f; th.regenPerSecond = 4f;
                    tower.damage = 105f; tower.range = 11.5f; tower.attackInterval = 1.05f;
                    tower.projectileSize = 1.1f;
                    tu.xpBounty = 220; tu.goldBounty = 350;
                    t.transform.localScale = Vector3.one * 1.3f;
                    tier2.Add(th);
                }
                else tier1[spot.lane] = t;
            }
            foreach (var t2 in tier2)
            {
                var tw = t2.GetComponent<Tower>();
                Protect(t2.gameObject, tier1[tw.lane]);
            }
            var throneProt = (team == Team.Kurchatov ? kurchatovThrone : lobnyaThrone).AddComponent<Protected>();
            throneProt.protectors = tier2.ToArray();
            throneProt.untilAnyDestroyed = true;
        }

        // --- Пивные бани у тронов (лечат свою команду) ---
        BathBuilder.Build("Пивная баня Курчатова", Team.Kurchatov, LaneData.W(-36, -36), LaneData.W(-31, -41));
        BathBuilder.Build("Пивная баня Москвы", Team.Lobnya, LaneData.W(36, 36), LaneData.W(41, 31));

        // --- Деревья и камни (до запекания NavMesh, чтобы их обходили) ---
        DecorBuilder.Build();

        // --- Запекаем NavMesh (до того как поставить юнитов) ---
        var surface = ground.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.BuildNavMesh();
        AssetDatabase.DeleteAsset(NavMeshPath);
        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);

        // --- Подписи и портреты над тронами ---
        CreateLabel("КУРЧАТОВ", kurchatovThrone.transform.position + Vector3.up * 6f, TeamUtil.GetColor(Team.Kurchatov));
        CreateLabel("МОСКВА", lobnyaThrone.transform.position + Vector3.up * 6f, TeamUtil.GetColor(Team.Lobnya));
        AddPortraitsToThrones();

        // --- Места появления героев ---
        var spawn = new GameObject("Spawn_Kurchatov");
        spawn.transform.position = LaneData.W(-38, -38);
        var spawnMoscow = new GameObject("Spawn_Moscow");
        spawnMoscow.transform.position = LaneData.W(38, 38);

        // --- Заготовки героев (выключены; в начале матча игрок и бот выбирают героя в лобби) ---
        var templates = new GameObject("HeroTemplates").transform;
        var hero = HeroBuilder.BuildOgrMog(Team.Kurchatov, spawn.transform.position, spawn.transform);
        var sanya = HeroBuilder.BuildSanya(Team.Kurchatov, spawn.transform.position + new Vector3(3, 0, 0), spawn.transform);
        var maxGold = HeroBuilder.BuildMaxGold(Team.Kurchatov, spawn.transform.position + new Vector3(-3, 0, 0), spawn.transform);
        var pivmaver = HeroBuilder.BuildPivmaver(Team.Kurchatov, spawn.transform.position + new Vector3(0, 0, 3), spawn.transform);
        var shashlik = HeroBuilder.BuildShashlik(Team.Kurchatov, spawn.transform.position + new Vector3(0, 0, -3), spawn.transform);
        shashlik.transform.SetParent(templates, true);
        shashlik.SetActive(false);
        hero.transform.SetParent(templates, true);
        sanya.transform.SetParent(templates, true);
        maxGold.transform.SetParent(templates, true);
        pivmaver.transform.SetParent(templates, true);
        hero.SetActive(false);
        sanya.SetActive(false);
        maxGold.SetActive(false);
        pivmaver.SetActive(false);

        // --- Волны крипов: на каждую из трёх линий ---
        foreach (var team in new[] { Team.Kurchatov, Team.Lobnya })
            foreach (var ln in LaneData.All)
            {
                var sp = new GameObject($"CreepSpawner_{team}_{ln}").AddComponent<CreepSpawner>();
                sp.transform.position = LaneData.Spawn(team, ln);
                sp.team = team;
                sp.lane = ln;
                sp.enemyThrone = (team == Team.Kurchatov ? lobnyaThrone : kurchatovThrone).transform;
            }

        // --- Лес с гопниками ---
        new GameObject("Jungle").AddComponent<JungleManager>();
        new GameObject("Runes").AddComponent<RuneManager>();
        new GameObject("FogOfWar").AddComponent<FogOfWar>();

        // --- Камера ---
        var cam = Camera.main;
        var rig = new GameObject("CameraRig");
        rig.transform.position = new Vector3(spawn.transform.position.x, 0, spawn.transform.position.z);
        cam.transform.SetParent(rig.transform);
        cam.transform.localPosition = new Vector3(0, 18, -12);
        cam.transform.LookAt(rig.transform);
        rig.AddComponent<RTSCamera>();

        // --- Менеджер игры ---
        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("Minimap").AddComponent<MinimapGUI>();
        new GameObject("DamageNumbers").AddComponent<DamageNumbers>();
        new GameObject("TouchInput").AddComponent<TouchInput>();

        // --- Начало матча: случайная команда, лобби, бот ---
        var match = new GameObject("MatchSetup").AddComponent<MatchSetup>();
        match.spawnKurchatov = spawn.transform;
        match.spawnMoscow = spawnMoscow.transform;
        match.heroTemplates = new[] { hero, sanya, maxGold, pivmaver, shashlik };

        // --- Сохраняем сцену и добавляем в Build Settings ---
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == ScenePath))
        {
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        EditorUtility.DisplayDialog("Готово",
            "Карта «Курчатов vs Москва» создана: три линии, лес с гопниками, по 6 башен на город.\n\nЖми Play: сначала лобби (выбор героя и линии, стартовый закуп), потом игра против бота.\n" +
            "ПКМ — идти/атаковать, S — стоп, G — глиф, пробел — камера к герою.",
            "Поехали");
    }

    // Меню: поставить/обновить портреты на тронах в уже открытой сцене.
    [MenuItem("Tools/MOBA/Обновить портреты на тронах")]
    public static void UpdatePortraitsMenu()
    {
        AddPortraitsToThrones();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    static void AddPortraitsToThrones()
    {
        AddPortrait("Трон Курчатова", "Label_КУРЧАТОВ", "Assets/Textures/Portrait_Kurchatov.png");
        AddPortrait("Трон Москвы", "Label_МОСКВА", "Assets/Textures/Portrait_Lobnya.png");
    }

    static void AddPortrait(string throneName, string labelName, string texPath)
    {
        var throne = GameObject.Find(throneName);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (throne == null || tex == null)
        {
            Debug.LogWarning($"Портрет не поставлен: нет объекта «{throneName}» или картинки {texPath}");
            return;
        }

        // Удаляем старый портрет, если был.
        var oldPortrait = GameObject.Find("Portrait_" + throneName);
        if (oldPortrait != null) Object.DestroyImmediate(oldPortrait);

        // Размер под пропорции фото: большая сторона = 6 метров.
        float aspect = (float)tex.width / tex.height;
        float w = aspect >= 1f ? 6f : 6f * aspect;
        float h = aspect >= 1f ? 6f / aspect : 6f;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Portrait_" + throneName;
        Object.DestroyImmediate(quad.GetComponent<Collider>()); // чтобы клик попадал в трон
        // верх трона — по самой высокой детали (у нового трона много украшений)
        float throneTop = throne.transform.position.y + throne.transform.localScale.y / 2f;
        foreach (var r in throne.GetComponentsInChildren<Renderer>()) throneTop = Mathf.Max(throneTop, r.bounds.max.y);
        throneTop = Mathf.Min(throneTop, throne.transform.position.y + 7f);   // портрет не улетает слишком высоко над камерой
        quad.transform.position = new Vector3(throne.transform.position.x, throneTop + 0.5f + h / 2f, throne.transform.position.z);
        quad.transform.rotation = Quaternion.Euler(30, 0, 0); // слегка наклонён к камере
        quad.transform.localScale = new Vector3(w, h, 1f);

        // Материал без освещения, чтобы фото было ярким.
        string matPath = $"{MatFolder}/Portrait_{tex.name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
        quad.GetComponent<Renderer>().sharedMaterial = mat;

        // Подпись поднимаем над портретом.
        var label = GameObject.Find(labelName);
        if (label != null)
            label.transform.position = new Vector3(quad.transform.position.x, quad.transform.position.y + h / 2f + 1.2f, quad.transform.position.z);

        // Портрет и подпись уходят под землю вместе с троном.
        var throneComp = throne.GetComponent<Throne>();
        if (throneComp != null)
        {
            throneComp.sinkWithThrone = label != null
                ? new[] { quad.transform, label.transform }
                : new[] { quad.transform };
            EditorUtility.SetDirty(throneComp);
        }
    }

    static void Protect(GameObject target, GameObject protector)
    {
        var p = target.AddComponent<Protected>();
        p.protectors = new[] { protector.GetComponent<Health>() };
    }

    static GameObject CreateThrone(string name, Team team, Vector3 pos)
    {
        // корень без масштаба: коллайдер-кубик 6×4×6 для кликов, а красивая модель — дочерние детали
        var throne = new GameObject(name);
        throne.transform.position = pos;
        var box = throne.AddComponent<BoxCollider>();
        box.center = new Vector3(0, 2f, 0);
        box.size = new Vector3(6f, 4f, 6f);
        if (team == Team.Kurchatov) ThroneBuilder.BuildAtom(throne.transform, TeamUtil.GetColor(team));
        else ThroneBuilder.BuildKremlin(throne.transform, TeamUtil.GetColor(team));
        // как у башен: ступени, чаши и флаги в навигацию не запекаем (на трон не залезают),
        // а вместо них — ровное препятствие-квадрат вокруг постамента
        throne.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        var obstacle = throne.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = new Vector3(0, 1.5f, 0);
        obstacle.size = new Vector3(6.8f, 3f, 6.8f);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;

        var unit = throne.AddComponent<Unit>();
        unit.team = team;
        unit.kind = UnitKind.Structure;
        unit.displayName = name;

        var health = throne.AddComponent<Health>();
        health.maxHealth = 5000;
        health.armor = 15;
        health.regenPerSecond = 5;

        throne.AddComponent<Throne>();

        // трон сам защищается: стреляет мощными шарами цвета города
        var tw = throne.AddComponent<Tower>();
        tw.damage = 140f; tw.range = 13f; tw.attackInterval = 1.2f;
        tw.lane = LaneData.Lane.Mid; tw.tier = 9;
        tw.projectileShape = ProjectileShape.Ball; tw.projectileSize = 1.3f; tw.projectileSpeed = 18f;
        tw.projectileColor = Color.Lerp(TeamUtil.GetColor(team), Color.white, 0.3f);
        tw.firePointHeight = 4.5f;

        var bar = throne.AddComponent<HealthBarGUI>();
        bar.width = 140;
        bar.height = 10;
        bar.heightOffset = 8.5f;
        bar.showName = true;
        return throne;
    }

    static GameObject CreateUnit(string name, Team team, Vector3 pos, float maxHp, float damage, float attackSpeed, bool destroyOnDeath)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.position = pos + Vector3.up; // капсула высотой 2, центр на 1
        Paint(go, "Unit_" + team, TeamUtil.GetColor(team));

        var agent = go.AddComponent<NavMeshAgent>();
        agent.baseOffset = 1f;
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.speed = 4.5f;
        agent.angularSpeed = 720f;
        agent.acceleration = 30f;

        var unit = go.AddComponent<Unit>();
        unit.team = team;
        unit.displayName = name;

        var health = go.AddComponent<Health>();
        health.maxHealth = maxHp;
        health.destroyOnDeath = destroyOnDeath;

        var attack = go.AddComponent<AutoAttack>();
        attack.damage = damage;
        attack.attackSpeed = attackSpeed;

        go.AddComponent<StatusEffects>();
        go.AddComponent<HealthBarGUI>();
        return go;
    }

    static void CreateLabel(string text, Vector3 pos, Color color)
    {
        var go = new GameObject("Label_" + text);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(56, 0, 0); // наклон к камере
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 80;
        tm.characterSize = 0.15f;
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

    public static void Paint(GameObject go, string matName, Color color)
    {
        string path = $"{MatFolder}/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;
        EditorUtility.SetDirty(mat);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
