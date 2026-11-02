using UnityEngine;

// Строит пивную баню: бассейн с пивом и пеной + деревянный домик-баня и огромная кружка.
public static class BathBuilder
{
    public static GameObject Build(string name, Team team, Vector3 center, Vector3 hutPos)
    {
        var root = new GameObject(name);
        root.transform.position = center;
        var bath = root.AddComponent<BeerBath>();
        bath.team = team;
        bath.radius = 4.5f;

        var wood = new Color(0.55f, 0.35f, 0.18f);
        var woodDark = new Color(0.36f, 0.21f, 0.1f);
        var beer = new Color(0.98f, 0.66f, 0.1f);
        var foam = new Color(1f, 0.98f, 0.9f);
        var warm = new Color(1f, 0.82f, 0.45f);
        var r = root.transform;

        // ---------- Бассейн: бортик из досок, пивная гладь с пеной (по нему можно ходить) ----------
        // бортик низкий (до 0.25 м), чтобы герои свободно заходили в бассейн лечиться
        BuildParts.Torus(r, "Rim", "Bath_Rim", 4.7f, 0.28f, new Vector3(0, 0.1f, 0), Quaternion.identity, new Vector3(1f, 0.5f, 1f), "Bath_Wood", wood, 0f, 0.35f);
        for (int i = 0; i < 40; i++)   // доски-клёпки по кругу
        {
            float a = i * 360f / 40f;
            BuildParts.Prim(r, "Plank", PrimitiveType.Cube, Quaternion.Euler(0, a, 0) * new Vector3(0, 0.12f, 4.98f), new Vector3(0.68f, 0.24f, 0.1f),
                i % 2 == 0 ? "Bath_Wood" : "Bath_WoodDark", i % 2 == 0 ? wood : woodDark, 0f, 0.35f, Quaternion.Euler(0, a, 0));
        }
        foreach (float y in new[] { 0.06f, 0.2f })
            BuildParts.Torus(r, "Hoop", "Bath_Hoop", 5.04f, 0.035f, new Vector3(0, y, 0), Quaternion.identity, Vector3.one, "Bath_Iron", new Color(0.4f, 0.4f, 0.45f), 0.85f, 0.6f);
        BuildParts.Lathe(r, "Beer", "Bath_Beer", new[] { new Vector2(0f, 0.1f), new Vector2(4.6f, 0.1f), new Vector2(4.5f, 0.17f), new Vector2(0f, 0.2f) },
            Vector3.zero, Vector3.one, "Bath_Beer", beer, 0.05f, 0.95f, 48);
        BuildParts.Torus(r, "FoamRing", "Bath_FoamRing", 4.3f, 0.32f, new Vector3(0, 0.22f, 0), Quaternion.identity, new Vector3(1f, 0.45f, 1f), "Bath_Foam", foam, 0f, 0.55f);
        var rnd = new System.Random(8);
        for (int i = 0; i < 20; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f, rad = 3.2f + (float)rnd.NextDouble() * 1.1f, s = 0.5f + (float)rnd.NextDouble() * 0.6f;
            BuildParts.Prim(r, "Foam", PrimitiveType.Sphere, new Vector3(Mathf.Cos(ang) * rad, 0.28f, Mathf.Sin(ang) * rad), new Vector3(s, s * 0.3f, s), "Bath_Foam", foam, 0f, 0.55f);
        }
        BuildParts.Ambient(r, "Steam", new Vector3(0, 0.3f, 0), AmbientFx.Kind.Steam, new Color(1f, 0.97f, 0.9f, 0.6f), 0.25f, 3.5f, 2);
        BuildParts.Text(r, "+ЗДОРОВЬЕ +МАНА", new Vector3(0, 0.6f, -5.4f), new Color(1f, 0.9f, 0.4f), 0.05f, Quaternion.Euler(56, 0, 0));

        // бассейн целиком проходимый: бортик, доски, пена и гладь в навигацию не попадают
        foreach (Transform ch in r)
            if (ch.name == "Rim" || ch.name == "Plank" || ch.name == "Hoop" || ch.name == "Beer" || ch.name == "FoamRing" || ch.name == "Foam")
                ch.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;

        // гирлянда тёплых лампочек вокруг бассейна на высоких столбиках.
        // В навигацию не попадает: раньше провода на высоте 2 м «закрывали» бассейн, и из бани нельзя было выйти.
        var lights = new GameObject("StringLights").transform;
        lights.SetParent(r, false);
        lights.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;
        for (int i = 0; i < 6; i++)
        {
            float a0 = i * Mathf.PI / 3f, a1 = (i + 1) * Mathf.PI / 3f;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * 5.5f, 0f, Mathf.Sin(a0) * 5.5f);
            BuildParts.Lathe(lights, "LightPost", "Bath_LightPost", new[] { new Vector2(0f, 0f), new Vector2(0.08f, 0f), new Vector2(0.06f, 4.0f), new Vector2(0f, 4.05f) }, p0, Vector3.one, "Bath_WoodDark", woodDark, 0f, 0.35f, 8);
            Vector3 p1 = new Vector3(Mathf.Cos(a1) * 5.5f, 0f, Mathf.Sin(a1) * 5.5f);
            for (int k = 1; k < 6; k++)
            {
                float tt = k / 6f;
                Vector3 p = Vector3.Lerp(p0, p1, tt) + Vector3.up * (3.95f - Mathf.Sin(tt * Mathf.PI) * 0.45f);
                Color bulb = (i + k) % 3 == 0 ? new Color(1f, 0.55f, 0.3f) : (i + k) % 3 == 1 ? warm : new Color(1f, 0.95f, 0.7f);
                BuildParts.Glow(lights, "Bulb", PrimitiveType.Sphere, p, Vector3.one * 0.16f, "Bath_Bulb" + ((i + k) % 3), bulb);
            }
            BuildParts.Tube(lights, "Wire", "Bath_Wire" + i, new[] { p0 + Vector3.up * 4.0f, Vector3.Lerp(p0, p1, 0.5f) + Vector3.up * 3.52f, p1 + Vector3.up * 4.0f }, new[] { 0.015f, 0.015f, 0.015f },
                "Bath_Iron", new Color(0.15f, 0.15f, 0.15f), 0f, 0.3f, 5);
        }

        // ---------- Бревенчатый домик-баня ----------
        var hutGo = new GameObject("Hut");
        hutGo.transform.SetParent(r, false);
        hutGo.transform.position = hutPos;
        var hut = hutGo.transform;
        var log = new Color(0.62f, 0.4f, 0.2f);
        var logMesh = BuildParts.Save("Bath_Log", ProcMesh.Lathe(new[] { new Vector2(0f, -0.5f), new Vector2(0.18f, -0.5f), new Vector2(0.2f, -0.46f), new Vector2(0.2f, 0.46f),
            new Vector2(0.18f, 0.5f), new Vector2(0f, 0.5f) }, 12));
        BuildParts.Prim(hut, "Core", PrimitiveType.Cube, new Vector3(0, 1.25f, 0), new Vector3(3.5f, 2.5f, 2.9f), "Bath_Logs", log, 0f, 0.3f);
        for (int i = 0; i < 6; i++)
        {
            float y = 0.2f + i * 0.4f;
            bool alt = i % 2 == 0;
            // по два бревна вдоль и поперёк, концы выступают за углы — как у сруба
            foreach (float z in new[] { -1.5f, 1.5f })
                BuildParts.Mesh(hut, "Log", logMesh, new Vector3(0, y + (alt ? 0f : 0.2f), z), Quaternion.Euler(0, 0, 90), new Vector3(1f, 4.1f, 1f), alt ? "Bath_Logs" : "Bath_Wood", alt ? log : wood, 0f, 0.3f);
            foreach (float x in new[] { -1.8f, 1.8f })
                BuildParts.Mesh(hut, "Log", logMesh, new Vector3(x, y + (alt ? 0.2f : 0f), 0), Quaternion.Euler(90, 0, 0), new Vector3(1f, 3.5f, 1f), alt ? "Bath_Wood" : "Bath_Logs", alt ? wood : log, 0f, 0.3f);
        }
        // двускатная крыша + фронтоны
        var roofC = new Color(0.32f, 0.18f, 0.09f);
        BuildParts.Prim(hut, "RoofL", PrimitiveType.Cube, new Vector3(-1.05f, 3.15f, 0), new Vector3(2.6f, 0.16f, 3.7f), "Bath_Roof", roofC, 0f, 0.3f, Quaternion.Euler(0, 0, 36));
        BuildParts.Prim(hut, "RoofR", PrimitiveType.Cube, new Vector3(1.05f, 3.15f, 0), new Vector3(2.6f, 0.16f, 3.7f), "Bath_Roof", roofC, 0f, 0.3f, Quaternion.Euler(0, 0, -36));
        BuildParts.Prim(hut, "Gable", PrimitiveType.Cube, new Vector3(0, 2.55f, 0), new Vector3(2.4f, 2.4f, 2.9f), "Bath_Logs", log, 0f, 0.3f, Quaternion.Euler(0, 0, 45));
        BuildParts.Prim(hut, "RoofRidge", PrimitiveType.Cube, new Vector3(0, 3.92f, 0), new Vector3(0.22f, 0.22f, 3.8f), "Bath_WoodDark", woodDark, 0f, 0.3f, Quaternion.Euler(0, 0, 45));
        // каменная труба с дымом
        BuildParts.Prim(hut, "Chimney", PrimitiveType.Cube, new Vector3(0.95f, 3.7f, 0.7f), new Vector3(0.45f, 1.2f, 0.45f), "Bath_Brick", new Color(0.55f, 0.25f, 0.2f), 0f, 0.25f);
        BuildParts.Prim(hut, "ChimneyTop", PrimitiveType.Cube, new Vector3(0.95f, 4.33f, 0.7f), new Vector3(0.55f, 0.1f, 0.55f), "Bath_Stone", new Color(0.5f, 0.49f, 0.47f), 0f, 0.25f);
        BuildParts.Ambient(hut, "Smoke", new Vector3(0.95f, 4.5f, 0.7f), AmbientFx.Kind.Smoke, new Color(0.6f, 0.6f, 0.62f, 0.7f), 0.3f, 0.2f, 2);
        // дверь, окно с тёплым светом, светящаяся вывеска, веник на двери
        BuildParts.Prim(hut, "Door", PrimitiveType.Cube, new Vector3(0, 0.95f, -1.72f), new Vector3(0.95f, 1.75f, 0.08f), "Bath_Door", new Color(0.3f, 0.17f, 0.08f), 0f, 0.3f);
        BuildParts.Glow(hut, "DoorKnob", PrimitiveType.Sphere, new Vector3(0.32f, 0.95f, -1.78f), Vector3.one * 0.09f, "Tower_Brass", new Color(1f, 0.85f, 0.4f));
        BuildParts.Glow(hut, "Window", PrimitiveType.Cube, new Vector3(-1.86f, 1.5f, 0), new Vector3(0.04f, 0.7f, 0.9f), "Tower_Window", warm);
        BuildParts.Prim(hut, "WindowFrame", PrimitiveType.Cube, new Vector3(-1.9f, 1.5f, 0), new Vector3(0.06f, 0.08f, 1.0f), "Bath_WoodDark", woodDark, 0f, 0.3f);
        BuildParts.Prim(hut, "SignBoard", PrimitiveType.Cube, new Vector3(0, 2.25f, -1.75f), new Vector3(2.6f, 0.5f, 0.08f), "Bath_Sign", new Color(0.3f, 0.17f, 0.08f), 0f, 0.3f);
        BuildParts.Text(hut, "ПИВНАЯ БАНЯ", new Vector3(0, 2.25f, -1.8f), new Color(1f, 0.85f, 0.4f), 0.045f);
        var broom = new GameObject("Broom").transform;
        broom.SetParent(hut, false);
        broom.localPosition = new Vector3(-0.32f, 1.35f, -1.8f);
        broom.localRotation = Quaternion.Euler(0, 0, 8);
        BuildParts.Prim(broom, "Handle", PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0), new Vector3(0.05f, 0.25f, 0.05f), "Bath_WoodDark", woodDark);
        for (int i = 0; i < 7; i++)
            BuildParts.Prim(broom, "Leaves", PrimitiveType.Sphere, new Vector3(((i % 3) - 1) * 0.07f, -0.15f - (i / 3) * 0.14f, 0), new Vector3(0.16f, 0.2f, 0.08f), "Bath_Leaves", new Color(0.3f, 0.55f, 0.2f), 0f, 0.4f);
        BuildParts.Lamp(hut, "Warm", new Vector3(1.4f, 0f, -2.3f), warm, 2.2f);
        // скамейка у входа
        BuildParts.Prim(hut, "Bench", PrimitiveType.Cube, new Vector3(-1.3f, 0.45f, -2.3f), new Vector3(1.3f, 0.1f, 0.4f), "Bath_Wood", wood, 0f, 0.35f);
        foreach (float x in new[] { -1.8f, -0.8f })
            BuildParts.Prim(hut, "BenchLeg", PrimitiveType.Cube, new Vector3(x, 0.22f, -2.3f), new Vector3(0.1f, 0.44f, 0.35f), "Bath_WoodDark", woodDark, 0f, 0.35f);

        // ---------- Огромная кружка пива с пеной через край и бочонок ----------
        var mug = new GameObject("Mug").transform;
        mug.SetParent(hut, false);
        mug.localPosition = new Vector3(2.7f, 0f, -0.5f);
        BuildParts.Lathe(mug, "MugBeer", "Bath_MugBeer", new[] { new Vector2(0f, 0.05f), new Vector2(0.62f, 0.05f), new Vector2(0.66f, 1.85f), new Vector2(0f, 1.85f) },
            Vector3.zero, Vector3.one, "Bath_Beer", beer, 0.05f, 0.95f, 32);
        BuildParts.Lathe(mug, "MugGlass", "Bath_MugGlass", new[] { new Vector2(0f, 0f), new Vector2(0.72f, 0f), new Vector2(0.76f, 0.12f), new Vector2(0.76f, 1.95f), new Vector2(0.7f, 1.95f), new Vector2(0.68f, 0.15f), new Vector2(0f, 0.15f) },
            Vector3.zero, Vector3.one, "Bath_Glass", new Color(0.85f, 0.92f, 0.95f), 0.1f, 0.95f, 32);
        for (int i = 0; i < 6; i++)   // грани стекла
        {
            float a = i * 60f;
            BuildParts.Prim(mug, "Facet", PrimitiveType.Cube, Quaternion.Euler(0, a, 0) * new Vector3(0, 1f, 0.77f), new Vector3(0.05f, 1.7f, 0.03f), "Bath_Glass", new Color(0.85f, 0.92f, 0.95f), 0.1f, 0.95f, Quaternion.Euler(0, a, 0));
        }
        BuildParts.Mesh(mug, "MugHandle", BuildParts.Save("Bath_MugHandle", ProcMesh.Torus(0.5f, 0.11f, 32, 10)), new Vector3(0.82f, 1.0f, 0), Quaternion.Euler(90, 0, 0), new Vector3(1f, 1f, 1.4f),
            "Bath_Glass", new Color(0.85f, 0.92f, 0.95f), 0.1f, 0.95f);
        BuildParts.Lathe(mug, "MugFoam", "Bath_MugFoam", new[] { new Vector2(0f, 1.82f), new Vector2(0.82f, 1.85f), new Vector2(0.85f, 2.0f), new Vector2(0.7f, 2.25f), new Vector2(0.35f, 2.4f), new Vector2(0f, 2.45f) },
            Vector3.zero, Vector3.one, "Bath_Foam", foam, 0f, 0.55f, 24);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 1.3f;
            Vector3 p0 = new Vector3(Mathf.Cos(a) * 0.78f, 1.95f, Mathf.Sin(a) * 0.78f);
            BuildParts.Tube(mug, "FoamDrip", "Bath_FoamDrip" + i, new[] { p0, p0 + new Vector3(Mathf.Cos(a) * 0.05f, -0.25f, Mathf.Sin(a) * 0.05f), p0 + new Vector3(Mathf.Cos(a) * 0.05f, -0.45f - i * 0.08f, Mathf.Sin(a) * 0.05f) },
                new[] { 0.1f, 0.08f, 0.06f }, "Bath_Foam", foam, 0f, 0.55f, 8);
        }
        BuildParts.Ambient(mug, "MugBubbles", new Vector3(0, 2.3f, 0), AmbientFx.Kind.Sparkle, new Color(1f, 0.95f, 0.75f), 0.4f, 0.4f, 1);
        // бочонок рядом
        var keg = new GameObject("Keg").transform;
        keg.SetParent(hut, false);
        keg.localPosition = new Vector3(2.4f, 0.55f, 1.0f);
        keg.localRotation = Quaternion.Euler(0, 30, 90);
        BuildParts.Lathe(keg, "KegBody", "Bath_Keg", ProcMesh.KegProfile, Vector3.zero, new Vector3(0.8f, 0.9f, 0.8f), "Bath_Wood", wood, 0f, 0.35f, 24);
        foreach (float y in new[] { -0.45f, 0.45f })
            BuildParts.Torus(keg, "KegHoop", "Bath_KegHoop", 0.53f, 0.03f, new Vector3(0, y, 0), Quaternion.identity, Vector3.one, "Bath_Iron", new Color(0.4f, 0.4f, 0.45f), 0.85f, 0.6f);
        return root;
    }

    static GameObject Part(GameObject parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, string matName, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        MapBuilder.Paint(go, matName, color);
        return go;
    }

    static GameObject SignText(GameObject parent, string text, Vector3 localPos, Color color, float charSize)
    {
        var go = new GameObject("SignText");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 60;
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
        return go;
    }
}
