using UnityEditor;
using UnityEngine;

// Шашлык Ассасин — в стиле Templar Assassin: облегающий тёмно-бирюзовый костюм с золотой отделкой,
// розовая накидка с высоким воротником, бирюзовый наплечник, широкие рукава, сапоги с золотыми манжетами.
// Лицо — 3D по фото: бледная кожа, длинные платиновые волосы с розоватыми прядями, тёмные глаза со стрелками.
// В правой руке — длинный шампур с шашлыком (им и бьёт), в левой — шампурчик поменьше.
public static partial class HeroBuilder
{
    public static GameObject BuildShashlik(Team team, Vector3 pos, Transform respawn)
    {
        var def = HeroCatalog.Shashlik;
        var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Portraits/shashlik.png");
        var root = BaseHero(def, team, pos, respawn, 3.0f, 0.6f, portrait, null, 3.6f);
        root.AddComponent<ShashlikKit>();
        var model = ModelRoot(root);
        var m = model.transform;

        var skin = new Color(0.95f, 0.86f, 0.88f);       // бледная, чуть розоватая
        var suit = new Color(0.07f, 0.11f, 0.12f);       // тёмно-бирюзовый, почти чёрный
        var teal = new Color(0.12f, 0.3f, 0.32f);
        var gold = new Color(1f, 0.68f, 0.18f);
        var pink = new Color(0.95f, 0.35f, 0.62f);
        var steel = new Color(0.82f, 0.82f, 0.88f);
        var meat = new Color(0.55f, 0.22f, 0.1f);
        var onion = new Color(0.96f, 0.93f, 0.82f);
        var pepper = new Color(0.85f, 0.15f, 0.1f);

        Part(model, "TeamRing", PrimitiveType.Cylinder, new Vector3(0, 0.02f, 0), new Vector3(1.9f, 0.02f, 1.9f), "Hero_Ring_" + team, TeamUtil.GetColor(team));

        // ---------- Ноги в облегающем костюме и высоких сапогах ----------
        foreach (float sd in new[] { -1f, 1f })
        {
            string s = sd < 0 ? "L" : "R";
            Vector3 hip = new Vector3(0.17f * sd, 1.18f, 0f), knee = new Vector3(0.24f * sd, 0.68f, -0.06f), ankle = new Vector3(0.26f * sd, 0.24f, 0.02f);
            var leg = Leg(m, "Shash", s, hip, knee, ankle, 0.15f, 0.085f, "Shash_Suit", suit, 0.2f, 0.85f, "Shash_Boot", new Color(0.05f, 0.06f, 0.07f), new Color(0.04f, 0.04f, 0.05f), 0.85f);
            // голенище сапога и золотая манжета
            Vector3 k = knee - hip, a = ankle - hip;
            TubePart(leg, "Boot", $"Shash_Boot{s}", new[] { a + Vector3.down * 0.05f, Vector3.Lerp(a, k, 0.5f), Vector3.Lerp(a, k, 0.88f) }, new[] { 0.1f, 0.11f, 0.125f },
                "Shash_Boot", new Color(0.05f, 0.06f, 0.07f), 0.3f, 0.85f, 12);
            TorusPart(leg, "BootCuff", "Shash_BootCuff", 0.125f, 0.04f, Vector3.Lerp(a, k, 0.9f), Quaternion.FromToRotation(Vector3.up, k - a), new Vector3(1f, 1.4f, 1f), "Shash_Gold", gold, 0.85f, 0.7f);
        }

        // ---------- Таз, талия, корсет с золотом ----------
        LathePart(m, "Hips", "Shash_Hips", new[] { new Vector2(0f, 1.0f), new Vector2(0.24f, 1.02f), new Vector2(0.3f, 1.15f), new Vector2(0.28f, 1.3f), new Vector2(0.22f, 1.42f), new Vector2(0f, 1.44f) },
            Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.8f), "Shash_Suit", suit, 0.2f, 0.85f, 28);
        LathePart(m, "Torso", "Shash_Torso", new[] { new Vector2(0f, 1.38f), new Vector2(0.2f, 1.4f), new Vector2(0.21f, 1.55f), new Vector2(0.27f, 1.75f), new Vector2(0.3f, 1.92f),
            new Vector2(0.27f, 2.05f), new Vector2(0.15f, 2.12f), new Vector2(0f, 2.14f) }, Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.75f), "Shash_Suit", suit, 0.2f, 0.85f, 28);
        foreach (float sd in new[] { -1f, 1f })
            Ball(m, "Bust", new Vector3(0.11f * sd, 1.84f, -0.15f), new Vector3(0.21f, 0.2f, 0.18f), "Shash_Suit", suit, 0.2f, 0.85f);
        TorusPart(m, "CorsetTop", "Shash_CorsetTop", 0.29f, 0.025f, new Vector3(0, 1.95f, -0.01f), Quaternion.Euler(-12, 0, 0), new Vector3(1f, 1f, 0.8f), "Shash_Gold", gold, 0.85f, 0.7f);
        TorusPart(m, "CorsetLow", "Shash_CorsetLow", 0.22f, 0.025f, new Vector3(0, 1.6f, 0), Quaternion.identity, new Vector3(1f, 1f, 0.78f), "Shash_Gold", gold, 0.85f, 0.7f);
        TorusPart(m, "Belt", "Shash_Belt", 0.28f, 0.045f, new Vector3(0, 1.3f, 0), Quaternion.identity, new Vector3(1f, 1f, 0.82f), "Shash_Belt", new Color(0.55f, 0.55f, 0.6f), 0.6f, 0.6f);

        // набедренники-«фартуки» спереди и сзади с золотой каймой и эмблемой-каплей
        foreach (float fb in new[] { -1f, 1f })
        {
            var tab = Pivot(m, fb < 0 ? "TabardFront" : "TabardBack", new Vector3(0, 1.28f, 0.25f * fb), Quaternion.Euler(-8f * fb, 0, 0));
            Shiny(tab.gameObject, "Tabard", PrimitiveType.Cube, new Vector3(0, -0.26f, 0), new Vector3(0.42f, 0.5f, 0.03f), "Shash_Suit", suit, 0.2f, 0.85f);
            foreach (var (p, sc) in new[] { (new Vector3(0, -0.01f, 0), new Vector3(0.46f, 0.05f, 0.04f)), (new Vector3(0, -0.51f, 0), new Vector3(0.3f, 0.05f, 0.04f)),
                                            (new Vector3(0.2f, -0.26f, 0), new Vector3(0.05f, 0.52f, 0.04f)), (new Vector3(-0.2f, -0.26f, 0), new Vector3(0.05f, 0.52f, 0.04f)) })
                Shiny(tab.gameObject, "TabardTrim", PrimitiveType.Cube, p, sc, "Shash_Gold", gold, 0.85f, 0.7f);
            if (fb < 0) Shiny(tab.gameObject, "Emblem", PrimitiveType.Sphere, new Vector3(0, -0.3f, -0.03f), new Vector3(0.08f, 0.1f, 0.02f), "Shash_Gold", gold, 0.85f, 0.7f);
        }

        // ---------- Розовая накидка с высоким воротником, бирюзовый наплечник ----------
        LathePart(m, "Collar", "Shash_Collar", new[] { new Vector2(0.42f, 0f), new Vector2(0.36f, 0.06f), new Vector2(0.22f, 0.18f), new Vector2(0.14f, 0.32f), new Vector2(0.15f, 0.42f),
            new Vector2(0.12f, 0.42f), new Vector2(0.11f, 0.32f), new Vector2(0.19f, 0.18f), new Vector2(0.33f, 0.05f), new Vector2(0.38f, 0f) },
            new Vector3(0, 1.98f, 0.02f), Quaternion.identity, new Vector3(1.1f, 1f, 0.95f), "Shash_Pink", pink, 0.1f, 0.6f, 32);
        TorusPart(m, "CollarTrim", "Shash_CollarTrim", 0.43f, 0.025f, new Vector3(0, 1.98f, 0.02f), Quaternion.identity, new Vector3(1.1f, 1f, 0.95f), "Shash_Gold", gold, 0.85f, 0.7f);
        // накидка на плечах: плоские розовые «лепестки», правый — бирюзовый слоёный
        var capeL = Ball(m, "CapeL", new Vector3(-0.38f, 2.02f, 0.05f), new Vector3(0.55f, 0.14f, 0.5f), "Shash_Pink", pink, 0.1f, 0.6f);
        capeL.transform.localRotation = Quaternion.Euler(0, 0, 18f);
        for (int i = 0; i < 3; i++)
        {
            var pad = Ball(m, "PauldronR", new Vector3(0.4f + i * 0.04f, 2.05f - i * 0.07f, 0.05f), new Vector3(0.5f - i * 0.06f, 0.13f, 0.48f - i * 0.05f),
                i == 1 ? "Shash_Silver" : "Shash_Teal", i == 1 ? new Color(0.75f, 0.78f, 0.8f) : teal, 0.4f, 0.75f);
            pad.transform.localRotation = Quaternion.Euler(0, 0, -20f - i * 6f);
        }
        // лента-хвост от затылка к плечу (оранжево-розовая)
        TubePart(m, "Ribbon", "Shash_Ribbon", ProcMesh.Bezier(new Vector3(0.12f, 2.6f, 0.15f), new Vector3(0.3f, 2.5f, 0.35f), new Vector3(0.38f, 2.2f, 0.3f), new Vector3(0.3f, 1.85f, 0.25f), 12),
            ProcMesh.Taper(12, 0.05f, 0.04f), "Shash_Ribbon", new Color(1f, 0.5f, 0.35f), 0.1f, 0.6f, 8);

        // ---------- Голова по фото ----------
        LathePart(m, "Neck", "Shash_Neck", new[] { new Vector2(0.085f, 0f), new Vector2(0.075f, 0.25f) }, new Vector3(0, 2.1f, 0), Quaternion.identity, Vector3.one, "Shash_Skin", skin);
        BuildFace(m, "Head", new Vector3(0, 2.58f, -0.02f), 0.5f, new FaceStyle
        {
            key = "Shash", skin = skin, hair = new Color(0.96f, 0.9f, 0.82f), hairTip = new Color(0.96f, 0.7f, 0.78f),
            brow = new Color(0.72f, 0.62f, 0.55f), iris = new Color(0.16f, 0.1f, 0.08f), lip = new Color(0.84f, 0.58f, 0.58f),
            width = 0.98f, height = 1.12f, chub = 0.45f, jaw = 0.4f, hairStyle = HairStyle.Long, hairFront = 0.42f, hairBack = -0.65f, hairVolume = 0.05f, hairLength = 1.5f,
            eyeSize = 1.22f, eyeOpen = 0.8f, lash = 1.9f, browThick = 0.014f, browRaise = 0.02f, noseSize = 0.82f,
            mouth = MouthStyle.Neutral, mouthWidth = 0.9f, seed = 9
        });

        // ---------- Руки: бледные, в широких бирюзовых рукавах с золотом ----------
        var armR = Arm(m, "Shash", "R", new Vector3(0.36f, 1.98f, 0f), new Vector3(0.62f, 1.6f, 0.02f), new Vector3(0.72f, 1.2f, -0.18f), 0.075f, 0.055f,
            "Shash_Skin", skin, 0f, 0.4f, skin, 0.13f, out var handR);
        var armL = Arm(m, "Shash", "L", new Vector3(-0.36f, 1.98f, 0f), new Vector3(-0.62f, 1.6f, 0.02f), new Vector3(-0.72f, 1.2f, -0.18f), 0.075f, 0.055f,
            "Shash_Skin", skin, 0f, 0.4f, skin, 0.13f, out var handL);
        foreach (var (arm, s, sh, el) in new[] { (armR, "R", new Vector3(0.36f, 1.98f, 0f), new Vector3(0.62f, 1.6f, 0.02f)), (armL, "L", new Vector3(-0.36f, 1.98f, 0f), new Vector3(-0.62f, 1.6f, 0.02f)) })
        {
            Vector3 e = el - sh, dirE = e.normalized;
            TubePart(arm, "Sleeve", $"Shash_Sleeve{s}", new[] { Vector3.zero, e * 0.5f, e }, new[] { 0.1f, 0.095f, 0.1f }, "Shash_Teal", teal, 0.3f, 0.7f, 12);
            // широкий раструб от локтя
            var bell = Pivot(arm, "SleeveBell", e, Quaternion.FromToRotation(Vector3.up, -dirE));
            LathePart(bell, "Bell", "Shash_Bell", new[] { new Vector2(0.1f, 0f), new Vector2(0.13f, 0.1f), new Vector2(0.2f, 0.3f), new Vector2(0.26f, 0.5f), new Vector2(0.25f, 0.5f),
                new Vector2(0.19f, 0.3f), new Vector2(0.12f, 0.1f), new Vector2(0.09f, 0f) }, Vector3.zero, Quaternion.identity, Vector3.one, "Shash_Teal", teal, 0.3f, 0.7f, 20);
            TorusPart(bell, "BellTrim", "Shash_BellTrim", 0.255f, 0.022f, new Vector3(0, 0.5f, 0), Quaternion.identity, Vector3.one, "Shash_Gold", gold, 0.85f, 0.7f);
        }

        // длинный шампур с шашлыком в правой руке: вперёд и чуть вниз
        var big = Pivot(armR, "Skewer", handR, Quaternion.Euler(-80, 0, 0));
        BuildSkewerMesh(big, "Big", 1.9f, steel, meat, onion, pepper, gold, 5);
        // шампурчик поменьше в левой
        var small = Pivot(armL, "SmallSkewer", handL, Quaternion.Euler(-70, 0, 10));
        BuildSkewerMesh(small, "Small", 1.1f, steel, meat, onion, pepper, gold, 3);
        return root;
    }

    // Шампур вдоль +Y: кольцо-рукоять, стальной прут с фиолетовым отблеском, куски мяса, лук и перец
    static void BuildSkewerMesh(Transform parent, string key, float len, Color steel, Color meat, Color onion, Color pepper, Color gold, int chunks)
    {
        TorusPart(parent, "Handle", "Shash_SkewerHandle", 0.07f, 0.014f, new Vector3(0, -0.12f, 0), Quaternion.Euler(0, 0, 90), Vector3.one, "Shash_Gold", gold, 0.85f, 0.7f);
        TubePart(parent, "Rod", $"Shash_Rod{key}", new[] { new Vector3(0, -0.05f, 0), new Vector3(0, len * 0.6f, 0), new Vector3(0, len, 0) }, new[] { 0.018f, 0.016f, 0f },
            "Shash_Steel", steel, 0.95f, 0.9f, 8);
        GlowMesh(parent, "Aura", SaveMesh($"Shash_Aura{key}", ProcMesh.Tube(new[] { new Vector3(0, 0.1f, 0), new Vector3(0, len * 0.95f, 0) }, new[] { 0.035f, 0.01f }, 6)),
            Vector3.zero, Quaternion.identity, Vector3.one, "Shash_Violet", new Color(0.75f, 0.45f, 1f));
        var rnd = new System.Random(len.GetHashCode());
        for (int i = 0; i < chunks; i++)
        {
            float y = Mathf.Lerp(0.3f, len - 0.25f, i / (float)Mathf.Max(1, chunks - 1));
            int kind = i % 3;   // мясо, лук, мясо/перец
            var rot = Quaternion.Euler((float)rnd.NextDouble() * 30f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 30f);
            if (kind == 1)
                TorusPart(parent, "Onion", "Shash_Onion", 0.07f, 0.035f, new Vector3(0, y, 0), rot, Vector3.one, "Shash_Onion", onion, 0f, 0.7f);
            else
            {
                Ball(parent, "Meat", new Vector3(0, y, 0), new Vector3(0.17f, 0.14f, 0.15f), "Shash_Meat", meat, 0f, 0.55f).transform.localRotation = rot;
                Ball(parent, "Char", new Vector3(0.05f, y + 0.03f, -0.04f), new Vector3(0.08f, 0.06f, 0.07f), "Shash_Char", new Color(0.25f, 0.1f, 0.05f), 0f, 0.4f);
                if (i % 2 == 0) Ball(parent, "Pepper", new Vector3(-0.02f, y + 0.1f, 0.02f), new Vector3(0.12f, 0.05f, 0.1f), "Shash_Pepper", pepper, 0f, 0.8f);
            }
        }
    }
}
