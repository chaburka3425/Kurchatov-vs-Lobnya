using UnityEditor;
using UnityEngine;

// Огр Mog: толстый двухголовый огр в чёрном спортивном костюме с белыми лампасами,
// золотая цепь «#ЗА КУРЧАТОВ УБЬЮ», дубина с шипами в правой руке, энергетик в левой, разные кроссовки.
public static partial class HeroBuilder
{
    public static GameObject BuildOgrMog(Team team, Vector3 pos, Transform respawn)
    {
        var def = HeroCatalog.OgrMog;
        // аватарка из модели (Tools → MOBA → «Сделать аватарки из моделей»), иначе — две фотки голов
        var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Portraits/ogr_mog.png");
        var root = BaseHero(def, team, pos, respawn, 3.5f, 0.85f,
            portrait != null ? portrait : AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Face_Left.png"),
            portrait != null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Face_Right.png"), 3.9f);
        var model = ModelRoot(root);
        var m = model.transform;

        var skin = new Color(0.96f, 0.76f, 0.64f);
        var suit = new Color(0.07f, 0.07f, 0.09f);
        var stripe = new Color(0.95f, 0.95f, 0.95f);
        var gold = new Color(1f, 0.8f, 0.2f);
        var wood = new Color(0.55f, 0.34f, 0.15f);
        var iron = new Color(0.38f, 0.38f, 0.42f);

        Part(model, "TeamRing", PrimitiveType.Cylinder, new Vector3(0, 0.02f, 0), new Vector3(2.4f, 0.02f, 2.4f), "Hero_Ring_" + team, TeamUtil.GetColor(team));

        // ---------- Туловище-груша и пузо ----------
        LathePart(m, "Body", "Ogr_Body", TorsoProfile(0f, 1.55f, 0.72f, 1.0f, 0.95f, 0, 0f), new Vector3(0, 0.95f, 0), Quaternion.identity, new Vector3(1f, 1f, 0.82f),
            "Hero_Suit", suit, 0.05f, 0.45f, 36);
        Ball(m, "Belly", new Vector3(0, 1.42f, -0.38f), new Vector3(1.62f, 1.3f, 1.12f), "Hero_Suit", suit, 0.05f, 0.45f);
        TubePart(m, "Zipper", "Ogr_Zip", ProcMesh.Bezier(new Vector3(0, 0.98f, -0.78f), new Vector3(0, 1.4f, -0.98f), new Vector3(0, 2.0f, -0.98f), new Vector3(0, 2.4f, -0.72f), 12),
            ProcMesh.Taper(12, 0.025f, 0.025f), "Hero_Zip", new Color(0.78f, 0.78f, 0.82f), 0.8f, 0.7f, 6);
        TorusPart(m, "Collar", "Ogr_Collar", 0.62f, 0.13f, new Vector3(0, 2.45f, -0.05f), Quaternion.Euler(-8, 0, 0), new Vector3(1.15f, 1f, 0.95f), "Hero_Suit", suit, 0.05f, 0.45f);
        TorusPart(m, "Hem", "Ogr_Hem", 0.74f, 0.07f, new Vector3(0, 0.98f, -0.02f), Quaternion.identity, new Vector3(1f, 1f, 0.84f), "Hero_Stripe", stripe);

        // ---------- Две головы ----------
        foreach (float sd in new[] { -1f, 1f })
        {
            string s = sd < 0 ? "Left" : "Right";
            LathePart(m, "Neck", "Ogr_Neck", new[] { new Vector2(0.28f, 0f), new Vector2(0.26f, 0.2f), new Vector2(0.24f, 0.35f) }, new Vector3(0.52f * sd, 2.4f, 0), Quaternion.identity, Vector3.one,
                "Hero_Skin", skin);
            // Лица по фоткам: левая голова — круглая, короткие волосы, щурится и косится вбок;
            // правая — «горшок» с чёлкой до бровей и лёгкая улыбка
            var face = sd < 0
                ? new FaceStyle
                {
                    key = "OgrL", skin = new Color(0.97f, 0.77f, 0.7f), hair = new Color(0.42f, 0.31f, 0.22f), brow = new Color(0.36f, 0.25f, 0.17f),
                    iris = new Color(0.5f, 0.6f, 0.68f), lip = new Color(0.86f, 0.55f, 0.52f),
                    width = 1.05f, height = 1.08f, chub = 1f, jaw = 0.05f, hairStyle = HairStyle.Short, hairFront = 0.5f, hairVolume = 0.035f,
                    eyeSize = 0.9f, eyeOpen = 0.35f, lookX = -0.9f, browThick = 0.026f, browAngle = -3f, noseSize = 1.3f, mouth = MouthStyle.Smirk, seed = 3
                }
                : new FaceStyle
                {
                    key = "OgrR", skin = new Color(0.97f, 0.8f, 0.69f), hair = new Color(0.55f, 0.47f, 0.37f), brow = new Color(0.45f, 0.35f, 0.24f),
                    iris = new Color(0.48f, 0.37f, 0.22f), lip = new Color(0.85f, 0.56f, 0.52f),
                    width = 1f, height = 1.1f, chub = 0.6f, jaw = 0.3f, hairStyle = HairStyle.Bowl, hairFront = 0.22f, hairBack = -0.5f, hairVolume = 0.06f,
                    eyeSize = 0.95f, eyeOpen = 0.6f, browThick = 0.018f, noseSize = 1f, mouth = MouthStyle.Smile, seed = 4
                };
            BuildFace(m, "Head" + s, new Vector3(0.52f * sd, 3.0f, 0), 0.95f, face);
        }

        // ---------- Золотая цепь с подвеской ----------
        var chain = new Vector3[18];
        for (int i = 0; i < chain.Length; i++)
        {
            float t = Mathf.Lerp(-1.25f, 1.25f, i / (float)(chain.Length - 1));
            chain[i] = new Vector3(Mathf.Sin(t) * 0.75f, 1.9f + (1f - Mathf.Cos(t)) * 0.85f, -Mathf.Cos(t) * 0.72f - 0.27f);
        }
        TubePart(m, "Chain", "Ogr_Chain", chain, ProcMesh.Taper(chain.Length, 0.055f, 0.055f), "Hero_Gold", gold, 0.95f, 0.8f, 8);
        for (int i = 1; i < chain.Length - 1; i += 2)
            TorusPart(m, "ChainLink", "Ogr_Link", 0.07f, 0.025f, chain[i], Quaternion.FromToRotation(Vector3.up, chain[i + 1] - chain[i - 1]), Vector3.one, "Hero_Gold", gold, 0.95f, 0.8f);
        Shiny(model, "Pendant", PrimitiveType.Cube, new Vector3(0, 1.72f, -1.02f), new Vector3(1.25f, 0.32f, 0.06f), "Hero_Gold", gold, 0.95f, 0.8f);
        SignText(model, "#ЗА КУРЧАТОВ УБЬЮ", new Vector3(0, 1.72f, -1.06f), new Color(0.25f, 0.15f, 0.02f), 0.028f);

        // ---------- Руки: толстые рукава с лампасами ----------
        foreach (float sd in new[] { -1f, 1f })
        {
            string s = sd < 0 ? "L" : "R";
            Vector3 sh = new Vector3(0.98f * sd, 2.2f, 0f), el = new Vector3(1.38f * sd, 1.55f, 0.08f), wr = new Vector3(1.45f * sd, 1.0f, -0.12f);
            var arm = Arm(m, "Ogr", s, sh, el, wr, 0.38f, 0.3f, "Hero_Suit", suit, 0.05f, 0.45f, skin, 0.52f, out var hand);
            var path = Through(Vector3.zero, el - sh, wr - sh, 12);
            foreach (float off in new[] { -0.06f, 0.06f })
                TubePart(arm, "ArmStripe", $"Ogr_ArmStripe{s}{(off < 0 ? "a" : "b")}", Offset(path, new Vector3(0.34f * sd, 0, off)), ProcMesh.Taper(12, 0.025f, 0.025f), "Hero_Stripe", stripe, 0f, 0.3f, 6);
            TorusPart(arm, "Cuff", "Ogr_Cuff", 0.3f, 0.06f, wr - sh + Vector3.up * 0.12f, Quaternion.identity, Vector3.one, "Hero_Stripe", stripe);

            if (sd > 0)
            {
                // Дубина: рукоять вверх из кулака, шишка с шипами
                var club = Pivot(arm, "Club", hand, Quaternion.Euler(-8, 0, -6));
                LathePart(club, "ClubHandle", "Ogr_ClubHandle", new[] { new Vector2(0f, -0.35f), new Vector2(0.1f, -0.35f), new Vector2(0.09f, 0.1f), new Vector2(0.11f, 0.7f), new Vector2(0f, 0.72f) },
                    Vector3.zero, Quaternion.identity, Vector3.one, "Hero_Wood", wood, 0f, 0.3f, 14);
                LathePart(club, "ClubHead", "Ogr_ClubHead", new[] { new Vector2(0f, 0.55f), new Vector2(0.18f, 0.6f), new Vector2(0.36f, 0.82f), new Vector2(0.42f, 1.05f),
                    new Vector2(0.38f, 1.3f), new Vector2(0.22f, 1.45f), new Vector2(0f, 1.48f) }, Vector3.zero, Quaternion.identity, Vector3.one, "Hero_ClubWood", new Color(0.62f, 0.4f, 0.19f), 0f, 0.3f, 20);
                var rnd = new System.Random(4);
                for (int k = 0; k < 10; k++)
                {
                    float a = k * 2.4f, y = 0.8f + (float)rnd.NextDouble() * 0.5f;
                    float rr = 0.4f - Mathf.Abs(y - 1.05f) * 0.3f;
                    Vector3 dir = new Vector3(Mathf.Cos(a), (y - 1.05f) * 0.8f, Mathf.Sin(a)).normalized;
                    Spike(club, "Spike", "Ogr_Spike" + k, new Vector3(Mathf.Cos(a) * rr * 0.9f, y, Mathf.Sin(a) * rr * 0.9f), dir, 0.22f, 0.06f, "Hero_Iron", iron, 0f);
                }
                TorusPart(club, "Grip", "Ogr_Grip", 0.105f, 0.025f, new Vector3(0, -0.05f, 0), Quaternion.identity, Vector3.one, "Hero_Stripe", stripe);
            }
            else
            {
                // Банка энергетика
                var can = Pivot(arm, "Can", hand + new Vector3(0, -0.05f, -0.22f), Quaternion.Euler(-10, 0, 0));
                LathePart(can, "CanBody", "Ogr_Can", new[] { new Vector2(0f, -0.3f), new Vector2(0.15f, -0.3f), new Vector2(0.18f, -0.26f), new Vector2(0.18f, 0.24f),
                    new Vector2(0.15f, 0.3f), new Vector2(0f, 0.3f) }, Vector3.zero, Quaternion.identity, Vector3.one, "Hero_Can", new Color(0.2f, 0.75f, 0.25f), 0.7f, 0.75f, 20);
                TorusPart(can, "CanTop", "Ogr_CanRim", 0.15f, 0.018f, new Vector3(0, 0.3f, 0), Quaternion.identity, Vector3.one, "Hero_CanTop", new Color(0.8f, 0.82f, 0.85f), 0.9f, 0.8f);
                var bolt = Part(can.gameObject, "CanBolt", PrimitiveType.Cube, new Vector3(0, 0f, -0.18f), new Vector3(0.08f, 0.3f, 0.02f), "Hero_CanBolt", new Color(1f, 0.95f, 0.2f));
                bolt.transform.localRotation = Quaternion.Euler(0, 0, 20);
            }
        }

        // ---------- Ноги с лампасами и разные кроссовки ----------
        foreach (float sd in new[] { -1f, 1f })
        {
            string s = sd < 0 ? "L" : "R";
            Vector3 hip = new Vector3(0.45f * sd, 1.2f, 0f), knee = new Vector3(0.5f * sd, 0.72f, -0.08f), ankle = new Vector3(0.45f * sd, 0.32f, 0f);
            var leg = Leg(m, "Ogr", s, hip, knee, ankle, 0.38f, 0.27f, "Hero_Suit", suit, 0.05f, 0.45f,
                sd < 0 ? "Hero_SneakerRed" : "Hero_SneakerWhite", sd < 0 ? new Color(0.85f, 0.1f, 0.1f) : new Color(0.97f, 0.97f, 0.97f), new Color(0.12f, 0.12f, 0.12f), 1.45f);
            var path = Through(Vector3.zero, knee - hip, ankle - hip, 12);
            foreach (float off in new[] { -0.06f, 0.06f })
                TubePart(leg, "LegStripe", $"Ogr_LegStripe{s}{(off < 0 ? "a" : "b")}", Offset(path, new Vector3(0.33f * sd, 0, off)), ProcMesh.Taper(12, 0.025f, 0.025f), "Hero_Stripe", stripe, 0f, 0.3f, 6);
        }
        return root;
    }
}
