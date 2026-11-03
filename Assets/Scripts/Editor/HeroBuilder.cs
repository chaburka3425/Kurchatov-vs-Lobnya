using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Собирает героя-огра из фигур: две головы с лицами-фотками,
// белая рубашка с тёмно-синим галстуком и дубина.
public static partial class HeroBuilder
{
    const string MatFolder = "Assets/Materials";

    // Общие компоненты любого героя: физика, бой, способности, инвентарь, интерфейс.
    static GameObject BaseHero(HeroDefinition def, Team team, Vector3 pos, Transform respawn, float height, float radius,
                               Texture2D faceLeft, Texture2D faceRight, float barOffset)
    {
        var root = new GameObject(def.name);
        root.transform.position = pos;

        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, height / 2f, 0);
        col.height = height;
        col.radius = radius;

        var agent = root.AddComponent<NavMeshAgent>();
        agent.baseOffset = 0f;
        agent.radius = 0.7f;
        agent.height = height - 0.2f;
        agent.speed = def.moveSpeed;
        agent.angularSpeed = 720f;
        agent.acceleration = 30f;

        var unit = root.AddComponent<Unit>();
        unit.team = team;
        unit.kind = UnitKind.Hero;
        unit.displayName = def.name;
        unit.xpBounty = 150;
        unit.goldBounty = 200;

        root.AddComponent<Health>();
        root.AddComponent<Mana>();
        root.AddComponent<StatusEffects>();
        var attack = root.AddComponent<AutoAttack>();
        attack.acquireRange = def.ranged ? 7f : 5f;
        attack.ranged = def.ranged;
        attack.projectileColor = def.projectileColor;
        root.AddComponent<HeroStats>().heroId = def.id;
        root.AddComponent<AbilityCaster>().heroId = def.id;
        root.AddComponent<HeroController>().respawnPoint = respawn;
        var bar = root.AddComponent<HealthBarGUI>();
        bar.heightOffset = barOffset;
        bar.width = 80f;
        bar.showName = true;
        root.AddComponent<HeroProgress>();
        root.AddComponent<Inventory>();
        root.AddComponent<ItemEffects>();
        root.AddComponent<ShopGUI>();
        var hud = root.AddComponent<AbilityBarGUI>();
        hud.faceLeft = faceLeft;
        hud.faceRight = faceRight;
        return root;
    }

    static GameObject ModelRoot(GameObject root)
    {
        // Модель повёрнута на 180°: «перед» героя смотрит туда, куда он идёт.
        var model = new GameObject("Model");
        model.transform.SetParent(root.transform, false);
        model.transform.localRotation = Quaternion.Euler(0, 180, 0);
        return model;
    }

    static GameObject Shiny(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, string matName, Color color, float metallic, float smooth)
    {
        var go = Part(root, name, type, localPos, scale, matName, color);
        var mat = go.GetComponent<Renderer>().sharedMaterial;
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);
        EditorUtility.SetDirty(mat);
        return go;
    }

    // Капсула между двумя точками (рука, нога)
    static GameObject Limb(GameObject root, string name, Vector3 from, Vector3 to, float thick, string matName, Color color, float metallic, float smooth)
    {
        Vector3 d = to - from;
        var go = Shiny(root, name, PrimitiveType.Capsule, (from + to) / 2f, new Vector3(thick, d.magnitude / 2f, thick), matName, color, metallic, smooth);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        return go;
    }

    static GameObject Glow(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, string matName, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        string path = $"{MatFolder}/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); else mat.color = color;
        EditorUtility.SetDirty(mat);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void Face(GameObject root, Transform head, string texPath, string name, float size = 1f)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null) { Debug.LogWarning("Нет картинки лица: " + texPath); return; }

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(root.transform, false);
        quad.transform.position = head.position;
        quad.transform.localScale = Vector3.one * size;

        string matPath = $"{MatFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.mainTexture = tex;
        EditorUtility.SetDirty(mat);
        quad.GetComponent<Renderer>().sharedMaterial = mat;

        var bb = quad.AddComponent<Billboard>();
        bb.anchor = head;
        bb.towardCamera = 0.5f;
    }

    static void Sneaker(GameObject root, Vector3 pos, Color color, string mat)
    {
        Part(root, "Sneaker", PrimitiveType.Cube, pos, new Vector3(0.58f, 0.24f, 0.85f), mat, color);
        Part(root, "Sole", PrimitiveType.Cube, pos + new Vector3(0, -0.1f, 0), new Vector3(0.62f, 0.08f, 0.9f), "Hero_Sole", new Color(0.12f, 0.12f, 0.12f));
        Part(root, "Laces", PrimitiveType.Cube, pos + new Vector3(0, 0.12f, -0.15f), new Vector3(0.3f, 0.03f, 0.35f), "Hero_Laces", new Color(0.9f, 0.9f, 0.9f));
    }

    static void SignText(GameObject root, string text, Vector3 localPos, Color color, float charSize)
    {
        var go = new GameObject("ChainText");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 60;
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;
        var font = GameFont.Get();
        if (font != null)
        {
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }

    static GameObject Part(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, string matName, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        MapBuilder.Paint(go, matName, color);
        return go;
    }
}
