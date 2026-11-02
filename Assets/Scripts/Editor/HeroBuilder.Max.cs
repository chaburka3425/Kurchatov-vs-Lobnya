using UnityEditor;
using UnityEngine;

// Макс Gold: чёрная кожанка с золотой отделкой, золотые наплечники с заклёпками, цепь с медальоном «G»,
// джинсы, золотые кроссовки и длинная золотая снайперка. Винтовка и обе руки — на одном шарнире
// ArmRPivot (UnitAnimator делает отдачу).
public static partial class HeroBuilder
{
    public static GameObject BuildMaxGold(Team team, Vector3 pos, Transform respawn)
    {
        var def = HeroCatalog.MaxGold;
        var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Portraits/max_gold.png");
        var root = BaseHero(def, team, pos, respawn, 3.1f, 0.65f, portrait, null, 3.7f);
        root.AddComponent<MaxGoldKit>();
        var model = ModelRoot(root);
        var m = model.transform;

        var skin = new Color(0.96f, 0.8f, 0.68f);
        var leather = new Color(0.04f, 0.04f, 0.05f);
        var jeans = new Color(0.11f, 0.15f, 0.25f);
        var gold = new Color(1f, 0.78f, 0.24f);
        var goldDark = new Color(0.55f, 0.36f, 0.05f);
        var hairC = new Color(0.78f, 0.62f, 0.42f);

        Part(model, "TeamRing", PrimitiveType.Cylinder, new Vector3(0, 0.02f, 0), new Vector3(1.9f, 0.02f, 1.9f), "Hero_Ring_" + team, TeamUtil.GetColor(team));

        // ---------- Кожанка ----------
        LathePart(m, "Jacket", "Max_Jacket", TorsoProfile(0f, 1.12f, 0.42f, 0.47f, 0.48f, 0, 0f), new Vector3(0, 0.98f, 0), Quaternion.identity, new Vector3(1f, 1f, 0.76f),
            "Max_Leather", leather, 0.25f, 0.85f, 36);
        TorusPart(m, "Hem", "Max_Hem", 0.43f, 0.035f, new Vector3(0, 1.0f, 0), Quaternion.identity, new Vector3(1f, 1f, 0.77f), "Max_Gold", gold, 0.9f, 0.75f);
        TorusPart(m, "Belt", "Max_Belt", 0.42f, 0.045f, new Vector3(0, 1.08f, 0), Quaternion.identity, new Vector3(1f, 1f, 0.77f), "Max_Belt", new Color(0.18f, 0.1f, 0.04f), 0.1f, 0.5f);
        Shiny(model, "Buckle", PrimitiveType.Cube, new Vector3(0, 1.08f, -0.36f), new Vector3(0.18f, 0.13f, 0.04f), "Max_Gold", gold, 0.9f, 0.75f);
        TubePart(m, "Zip", "Max_Zip", ProcMesh.Bezier(new Vector3(0, 1.12f, -0.34f), new Vector3(0, 1.45f, -0.38f), new Vector3(0, 1.75f, -0.37f), new Vector3(0, 1.98f, -0.27f), 10),
            ProcMesh.Taper(10, 0.018f, 0.018f), "Max_Gold", gold, 0.9f, 0.75f, 6);
        foreach (float sd in new[] { -1f, 1f })
        {
            var lapel = Shiny(model, "Lapel", PrimitiveType.Cube, new Vector3(0.14f * sd, 1.86f, -0.31f), new Vector3(0.2f, 0.32f, 0.04f), "Max_Leather", leather, 0.25f, 0.85f);
            lapel.transform.localRotation = Quaternion.Euler(-15, 0, 25f * sd);
            // золотые наплечники-купола с заклёпками
            LathePart(m, "ShoulderPad", "Max_Pad", new[] { new Vector2(0.3f, 0f), new Vector2(0.29f, 0.06f), new Vector2(0.24f, 0.14f), new Vector2(0.14f, 0.2f), new Vector2(0f, 0.22f) },
                new Vector3(0.48f * sd, 1.93f, 0), Quaternion.Euler(0, 0, -16f * sd), new Vector3(1f, 1f, 1.1f), "Max_Gold", gold, 0.9f, 0.75f, 24);
            foreach (float z in new[] { -0.17f, 0f, 0.17f })
                Ball(m, "Rivet", new Vector3(0.53f * sd, 2.06f, z), Vector3.one * 0.065f, "Max_GoldLight", new Color(1f, 0.92f, 0.6f), 0.9f, 0.9f);
        }

        // ---------- Цепь с медальоном ----------
        var chain = new Vector3[14];
        for (int i = 0; i < chain.Length; i++)
        {
            float t = Mathf.Lerp(-1.15f, 1.15f, i / (float)(chain.Length - 1));
            chain[i] = new Vector3(Mathf.Sin(t) * 0.3f, 1.64f + (1f - Mathf.Cos(t)) * 0.46f, -0.38f + (1f - Mathf.Cos(t)) * 0.12f);
        }
        TubePart(m, "Chain", "Max_Chain", chain, ProcMesh.Taper(chain.Length, 0.028f, 0.028f), "Max_Gold", gold, 0.95f, 0.8f, 8);
        LathePart(m, "Medallion", "Max_Medallion", new[] { new Vector2(0f, -0.025f), new Vector2(0.11f, -0.025f), new Vector2(0.125f, 0f), new Vector2(0.11f, 0.025f), new Vector2(0f, 0.03f) },
            new Vector3(0, 1.58f, -0.4f), Quaternion.Euler(90, 0, 0), Vector3.one, "Max_Gold", gold, 0.95f, 0.8f, 24);
        SignText(model, "G", new Vector3(0, 1.58f, -0.44f), goldDark, 0.05f);

        // ---------- Голова по фотке: круглое лицо, короткий светлый «ёжик», глаза навыкате, язык наружу ----------
        LathePart(m, "Neck", "Max_Neck", new[] { new Vector2(0.11f, 0f), new Vector2(0.1f, 0.2f) }, new Vector3(0, 2.05f, 0), Quaternion.identity, Vector3.one, "Hero_Skin", skin);
        BuildFace(m, "Head", new Vector3(0, 2.52f, -0.02f), 0.64f, new FaceStyle
        {
            key = "Max", skin = new Color(0.95f, 0.8f, 0.7f), hair = hairC, brow = new Color(0.55f, 0.42f, 0.3f),
            iris = new Color(0.42f, 0.5f, 0.55f), lip = new Color(0.82f, 0.5f, 0.48f),
            width = 1f, height = 1.12f, chub = 0.75f, jaw = 0.2f, hairStyle = HairStyle.Buzz, hairFront = 0.5f, hairVolume = 0.025f,
            eyeSize = 1.18f, eyeOpen = 1.15f, browThick = 0.02f, browRaise = 0.04f, browAngle = -6f, noseSize = 1.05f, mouth = MouthStyle.Tongue, seed = 5
        });

        // ---------- Золотая снайперка + обе руки на одном шарнире ----------
        var gun = Pivot(m, "ArmRPivot", new Vector3(0.22f, 1.6f, 0.05f));
        var rifle = Pivot(gun, "Rifle", new Vector3(0, -0.05f, -0.15f), Quaternion.Euler(-4f, 0, 0));
        // приклад: сужающийся профиль, лежит вдоль Z
        LathePart(rifle, "Stock", "Max_Stock", new[] { new Vector2(0f, 0f), new Vector2(0.1f, 0f), new Vector2(0.13f, 0.1f), new Vector2(0.14f, 0.35f), new Vector2(0.09f, 0.6f), new Vector2(0f, 0.62f) },
            new Vector3(0, -0.04f, 0.88f), Quaternion.Euler(-90, 0, 0), new Vector3(0.75f, 1f, 1.6f), "Max_Gold", gold, 0.9f, 0.75f, 16);
        Shiny(rifle.gameObject, "Grip", PrimitiveType.Capsule, new Vector3(0, -0.2f, 0.16f), new Vector3(0.09f, 0.14f, 0.11f), "Max_GoldDark", goldDark, 0.8f, 0.6f)
            .transform.localRotation = Quaternion.Euler(15, 0, 0);
        // ствольная коробка — вытянутое тело вращения
        LathePart(rifle, "Receiver", "Max_Receiver", new[] { new Vector2(0f, -0.45f), new Vector2(0.08f, -0.45f), new Vector2(0.11f, -0.38f), new Vector2(0.11f, 0.38f), new Vector2(0.08f, 0.45f), new Vector2(0f, 0.45f) },
            new Vector3(0, 0, -0.2f), Quaternion.Euler(90, 0, 0), Vector3.one, "Max_Gold", gold, 0.9f, 0.75f, 18);
        Shiny(rifle.gameObject, "Magazine", PrimitiveType.Cube, new Vector3(0, -0.17f, -0.25f), new Vector3(0.1f, 0.22f, 0.16f), "Max_GoldDark", goldDark, 0.8f, 0.6f);
        TubePart(rifle, "Barrel", "Max_Barrel", new[] { new Vector3(0, 0.03f, -0.6f), new Vector3(0, 0.03f, -2.3f) }, new[] { 0.055f, 0.042f }, "Max_Gold", gold, 0.9f, 0.75f, 14);
        LathePart(rifle, "MuzzleBrake", "Max_Muzzle", new[] { new Vector2(0f, 0f), new Vector2(0.07f, 0f), new Vector2(0.075f, 0.04f), new Vector2(0.06f, 0.06f), new Vector2(0.075f, 0.08f),
            new Vector2(0.075f, 0.14f), new Vector2(0.06f, 0.16f), new Vector2(0.075f, 0.18f), new Vector2(0.075f, 0.24f), new Vector2(0.05f, 0.25f), new Vector2(0f, 0.25f) },
            new Vector3(0, 0.03f, -2.28f), Quaternion.Euler(-90, 0, 0), Vector3.one, "Max_Gold", gold, 0.9f, 0.75f, 16);
        // прицел
        LathePart(rifle, "Scope", "Max_Scope", new[] { new Vector2(0f, -0.36f), new Vector2(0.085f, -0.36f), new Vector2(0.085f, -0.24f), new Vector2(0.055f, -0.16f), new Vector2(0.055f, 0.16f),
            new Vector2(0.08f, 0.24f), new Vector2(0.08f, 0.32f), new Vector2(0f, 0.32f) }, new Vector3(0, 0.22f, -0.2f), Quaternion.Euler(90, 0, 0), Vector3.one,
            "Max_Scope", new Color(0.05f, 0.05f, 0.06f), 0.4f, 0.85f, 18);
        foreach (float z in new[] { -0.36f, 0.04f })
            TorusPart(rifle, "ScopeRing", "Max_ScopeRing", 0.065f, 0.02f, new Vector3(0, 0.22f, z), Quaternion.Euler(90, 0, 0), Vector3.one, "Max_Gold", gold, 0.9f, 0.75f);
        Glow(rifle.gameObject, "Lens", PrimitiveType.Sphere, new Vector3(0, 0.22f, -0.56f), new Vector3(0.15f, 0.15f, 0.03f), "Max_Lens", new Color(0.55f, 0.85f, 1f));
        foreach (float sd in new[] { -1f, 1f })
        {
            TubePart(rifle, "Engrave", $"Max_Engrave{(sd < 0 ? "L" : "R")}", new[] { new Vector3(0.105f * sd, 0.03f, -0.55f), new Vector3(0.105f * sd, 0.03f, 0.15f) }, new[] { 0.012f, 0.012f },
                "Max_GoldDark", goldDark, 0.8f, 0.6f, 6);
            Glow(rifle.gameObject, "Gem", PrimitiveType.Sphere, new Vector3(0.11f * sd, -0.04f, -0.48f), Vector3.one * 0.06f, "Max_Gem", new Color(0.9f, 0.1f, 0.08f));
            TubePart(rifle, "Bipod", $"Max_Bipod{(sd < 0 ? "L" : "R")}", new[] { new Vector3(0.04f * sd, -0.02f, -1.15f), new Vector3(0.12f * sd, -0.32f, -1.32f) }, new[] { 0.022f, 0.016f },
                "Max_GoldDark", goldDark, 0.8f, 0.6f, 6);
        }

        // руки держат винтовку (в координатах шарнира)
        Vector3 shR = new Vector3(0.48f, 1.92f, 0f) - gun.localPosition, shL = new Vector3(-0.48f, 1.92f, 0f) - gun.localPosition;
        Vector3 handR = new Vector3(0.22f, 1.38f, 0.1f) - gun.localPosition, handL = new Vector3(0.18f, 1.6f, -0.78f) - gun.localPosition;
        TubePart(gun, "ArmR", "Max_ArmR", Through(shR, (shR + handR) / 2f + new Vector3(0.12f, -0.05f, 0.12f), handR, 12), ProcMesh.Taper(12, 0.14f, 0.11f), "Max_Leather", leather, 0.25f, 0.85f);
        TubePart(gun, "ArmL", "Max_ArmL", Through(shL, (shL + handL) / 2f + new Vector3(-0.15f, -0.18f, 0f), handL, 12), ProcMesh.Taper(12, 0.14f, 0.11f), "Max_Leather", leather, 0.25f, 0.85f);
        Ball(gun, "HandR", handR, Vector3.one * 0.19f, "Hero_Skin", skin, 0f, 0.35f);
        Ball(gun, "HandL", handL, Vector3.one * 0.19f, "Hero_Skin", skin, 0f, 0.35f);
        TorusPart(gun, "Watch", "Max_Watch", 0.11f, 0.03f, Vector3.Lerp(shR, handR, 0.82f), Quaternion.FromToRotation(Vector3.up, handR - shR), Vector3.one, "Max_Gold", gold, 0.95f, 0.8f);

        // ---------- Джинсы и золотые кроссовки ----------
        foreach (float sd in new[] { -1f, 1f })
        {
            string s = sd < 0 ? "L" : "R";
            Leg(m, "Max", s, new Vector3(0.2f * sd, 1.05f, 0), new Vector3(0.22f * sd, 0.6f, -0.05f), new Vector3(0.22f * sd, 0.24f, 0), 0.18f, 0.14f,
                "Max_Jeans", jeans, 0f, 0.2f, "Max_Gold", gold, new Color(0.97f, 0.97f, 0.97f), 0.95f);
        }
        return root;
    }
}
