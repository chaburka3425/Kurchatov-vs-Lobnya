using UnityEngine;

// Настоящие 3D-лица вместо фоток: форма головы, глаза (белок, радужка, зрачок, блик, веки, ресницы),
// брови, нос, губы (улыбка / хмурый / язык наружу), уши и причёска. Черты подбираются по фото друга.
// Голова собирается в единичном размере (ширина ≈ 1) и масштабируется. «Перед» лица — минус Z.
public static partial class HeroBuilder
{
    public enum HairStyle { None, Buzz, Short, Bowl, Messy, Long }
    public enum MouthStyle { Neutral, Smile, Frown, Smirk, Tongue }

    public class FaceStyle
    {
        public string key;                      // префикс для мешей и материалов
        public Color skin = new Color(0.96f, 0.8f, 0.7f), hair = new Color(0.4f, 0.28f, 0.18f), brow = new Color(0.3f, 0.2f, 0.12f);
        public Color iris = new Color(0.4f, 0.5f, 0.6f), lip = new Color(0.8f, 0.5f, 0.48f);
        public float width = 1f, height = 1.1f, depth = 1f;
        public float chub = 0.4f, jaw = 0.3f;    // пухлые щёки, узкая челюсть
        public HairStyle hairStyle = HairStyle.Short;
        public float hairFront = 0.45f, hairBack = -0.45f, hairVolume = 0.05f, hairNoise = 0f;
        public float eyeSize = 1f, eyeOpen = 0.75f, lookX = 0f;
        public float lash = 1f;                 // >1.3 — густые ресницы и подводка-стрелки
        public float hairLength = 1.3f;         // для длинных волос: насколько ниже подбородка (в размерах головы)
        public Color hairTip = Color.clear;     // кончики волос другого цвета (розоватые)
        public float browThick = 0.022f, browAngle = 0f, browRaise = 0f;
        public float noseSize = 1f;
        public MouthStyle mouth = MouthStyle.Neutral;
        public float mouthWidth = 1f;
        public int seed = 1;
    }

    // Форма головы по направлению на единичной сфере
    static Vector3 HeadShape(FaceStyle f, Vector3 d)
    {
        float x = d.x * 0.5f * f.width, y = d.y * 0.5f * f.height, z = d.z * 0.5f * f.depth;
        if (d.y < 0f)
        {
            float k = Mathf.Pow(-d.y, 1.5f);
            x *= 1f - f.jaw * 0.38f * k;               // челюсть уже
            z *= 1f - f.jaw * 0.12f * k;
        }
        float cheek = Mathf.Exp(-Mathf.Pow((d.y + 0.2f) / 0.28f, 2f)) * Mathf.Max(0f, -d.z);
        x *= 1f + f.chub * 0.16f * cheek;              // щёки
        if (d.z < 0f) z *= 0.9f + 0.1f * (1f - Mathf.Abs(d.x));   // лицо чуть площе
        if (d.z > 0f && d.y > -0.3f) z *= 1.06f;       // затылок
        if (d.y < -0.75f && d.z < 0f) z -= 0.03f * f.depth;   // подбородок вперёд
        return new Vector3(x, y, z);
    }

    // Точка на лице для координат (x, y) — ищем перебором по передней половине головы
    static Vector3 Surface(FaceStyle f, float x, float y)
    {
        Vector3 best = Vector3.zero; float bd = float.MaxValue;
        for (int i = 0; i <= 60; i++)
            for (int j = 0; j <= 60; j++)
            {
                float th = Mathf.Lerp(0.05f, Mathf.PI - 0.05f, i / 60f), ph = Mathf.Lerp(Mathf.PI * 1.02f, Mathf.PI * 1.98f, j / 60f);
                var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                var p = HeadShape(f, d);
                float dd = (p.x - x) * (p.x - x) + (p.y - y) * (p.y - y);
                if (dd < bd) { bd = dd; best = p; }
            }
        return best;
    }

    // Где растут волосы: 1 — волосы, 0 — кожа (плавно)
    static float HairMask(FaceStyle f, Vector3 d)
    {
        float back = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.55f, 0.45f, d.z));
        float line = Mathf.Lerp(f.hairFront, f.hairBack, back);
        if (f.hairStyle == HairStyle.Bowl) line = Mathf.Lerp(line, Mathf.Min(line, 0.0f), Mathf.Abs(d.x) * 0.8f);   // по бокам ниже
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(line - 0.06f, line + 0.06f, d.y));
    }

    static float Hash(Vector3 d, int seed) => Mathf.Repeat(Mathf.Sin(Vector3.Dot(d, new Vector3(12.9898f, 78.233f, 37.719f)) + seed * 7.13f) * 43758.5453f, 1f);

    // Голова целиком. Возвращает трансформ головы (pos — центр головы, size — ширина в метрах).
    static Transform BuildFace(Transform parent, string name, Vector3 pos, float size, FaceStyle f)
    {
        var head = Pivot(parent, name, pos);
        head.localScale = Vector3.one * size;
        string k = f.key;
        var eyeWhite = new Color(0.97f, 0.96f, 0.94f);

        MeshPart(head, "Skull", SaveMesh(k + "_Head", ProcMesh.Blob(d => HeadShape(f, d))), Vector3.zero, Quaternion.identity, Vector3.one,
            k + "_Skin", f.skin, 0f, 0.38f);

        // ---------- Глаза ----------
        float er = 0.075f * f.eyeSize, ey = 0.04f, ex = 0.17f * f.width;
        foreach (float sd in new[] { -1f, 1f })
        {
            Vector3 s = Surface(f, ex * sd, ey);
            Vector3 c = s + new Vector3(0, 0, er * 0.45f);           // глаз утоплен в голову
            Ball(head, "EyeWhite", c, new Vector3(er * 2f, er * 1.9f, er * 1.6f), "Face_EyeWhite", eyeWhite, 0f, 0.9f);
            Vector3 front = c + new Vector3(f.lookX * er * 0.35f, -er * 0.05f, -er * 0.74f);
            Ball(head, "Iris", front, new Vector3(er * 1.05f, er * 1.05f, er * 0.3f), k + "_Iris", f.iris, 0f, 0.9f);
            Ball(head, "Pupil", front + new Vector3(0, 0, -er * 0.06f), new Vector3(er * 0.48f, er * 0.48f, er * 0.2f), "Face_Pupil", new Color(0.03f, 0.03f, 0.04f), 0f, 0.95f);
            Glow(head.gameObject, "Glint", PrimitiveType.Sphere, front + new Vector3(er * 0.18f, er * 0.2f, -er * 0.12f), Vector3.one * er * 0.2f, "Face_Glint", Color.white);
            // веки: шар цвета кожи чуть больше глаза, сдвинутый вверх — чем меньше eyeOpen, тем ниже
            float o = er * (0.45f + f.eyeOpen * 0.95f);
            Ball(head, "LidUp", c + new Vector3(0, o, -er * 0.05f), new Vector3(er * 2.25f, er * 2.1f, er * 1.9f), k + "_Skin", f.skin, 0f, 0.38f);
            Ball(head, "LidDown", c + new Vector3(0, -er * 1.5f, -er * 0.02f), new Vector3(er * 2.2f, er * 1.9f, er * 1.8f), k + "_Skin", f.skin, 0f, 0.38f);
            // ресницы по краю верхнего века
            float edgeY = c.y + o - er * 1.05f;
            var lash = new Vector3[7];
            for (int i = 0; i < 7; i++)
            {
                float t = Mathf.Lerp(-0.95f, 0.95f, i / 6f);
                float lx = t * er, lz = -Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) * er * 0.82f;
                lash[i] = c + new Vector3(lx, edgeY - c.y + (1f - t * t) * er * 0.12f, lz - er * 0.06f);
            }
            TubePart(head, "Lash", $"{k}_Lash{(sd < 0 ? "L" : "R")}", lash, ProcMesh.Taper(7, er * 0.09f * f.lash, er * 0.09f * f.lash), "Face_Lash", new Color(0.08f, 0.06f, 0.05f), 0f, 0.3f, 5);
            if (f.lash > 1.3f)
            {
                // подводка-стрелка наружу и вверх + нижняя подводка
                Vector3 outer = lash[sd > 0 ? 6 : 0];
                TubePart(head, "Liner", $"{k}_Liner{(sd < 0 ? "L" : "R")}", new[] { outer, outer + new Vector3(er * 0.45f * sd, er * 0.22f, er * 0.12f) },
                    new[] { er * 0.11f, er * 0.02f }, "Face_Lash", new Color(0.08f, 0.06f, 0.05f), 0f, 0.3f, 5);
                var low = new Vector3[5];
                for (int i = 0; i < 5; i++)
                {
                    float t = Mathf.Lerp(-0.85f, 0.85f, i / 4f);
                    low[i] = c + new Vector3(t * er, -er * 0.62f - (1f - t * t) * er * 0.12f, -Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) * er * 0.78f - er * 0.04f);
                }
                TubePart(head, "LinerLow", $"{k}_LinerLow{(sd < 0 ? "L" : "R")}", low, ProcMesh.Taper(5, er * 0.06f, er * 0.06f), "Face_Lash", new Color(0.1f, 0.07f, 0.07f), 0f, 0.3f, 5);
                for (int i = 0; i < 5; i++)   // отдельные длинные реснички
                {
                    Vector3 r0 = lash[1 + i];
                    Spike(head, "Lashes", $"{k}_LashHair{(sd < 0 ? "L" : "R")}{i}", r0, (new Vector3((i - 2) * 0.25f * sd + 0.3f * sd, 0.9f, -0.6f)).normalized, er * 0.35f, er * 0.03f,
                        "Face_Lash", new Color(0.08f, 0.06f, 0.05f), 0f);
                }
            }

            // бровь: дуга над глазом, внутренний край ниже, если хмурится
            var brow = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;                                     // 0 — у переносицы, 1 — снаружи
                float bx = (0.07f + t * 0.22f) * f.width * sd;
                float by = 0.17f + f.browRaise + Mathf.Sin(t * Mathf.PI) * 0.025f - (1f - t) * f.browAngle * 0.004f;
                brow[i] = Surface(f, bx, by) + new Vector3(0, 0, -f.browThick * 0.6f);
            }
            TubePart(head, "Brow", $"{k}_Brow{(sd < 0 ? "L" : "R")}", brow, ProcMesh.Taper(6, f.browThick * 1.15f, f.browThick * 0.6f), k + "_Brow", f.brow, 0f, 0.25f, 6);

            // ухо
            Vector3 ear = new Vector3(0.5f * f.width * sd * 0.98f, -0.02f, 0.02f);
            Ball(head, "Ear", ear, new Vector3(0.07f, 0.17f, 0.12f), k + "_Skin", f.skin, 0f, 0.38f);
            Ball(head, "EarIn", ear + new Vector3(0.012f * sd, 0, -0.015f), new Vector3(0.04f, 0.11f, 0.07f), k + "_SkinDark", f.skin * 0.82f, 0f, 0.3f);
        }

        // ---------- Нос ----------
        float ns = f.noseSize;
        Vector3 tip = Surface(f, 0f, -0.12f) + new Vector3(0, 0, -0.07f * ns);
        TubePart(head, "NoseBridge", k + "_Nose", new[] { Surface(f, 0f, 0.07f) + new Vector3(0, 0, -0.005f), Surface(f, 0f, -0.02f) + new Vector3(0, 0, -0.035f * ns), tip + new Vector3(0, 0.03f, 0.01f) },
            new[] { 0.028f * ns, 0.04f * ns, 0.05f * ns }, k + "_Skin", f.skin, 0f, 0.38f, 10);
        Ball(head, "NoseTip", tip, new Vector3(0.1f, 0.085f, 0.085f) * ns, k + "_Skin", f.skin, 0f, 0.38f);
        foreach (float sd in new[] { -1f, 1f })
        {
            Ball(head, "Nostril", tip + new Vector3(0.052f * sd * ns, -0.012f, 0.03f), new Vector3(0.065f, 0.055f, 0.06f) * ns, k + "_Skin", f.skin, 0f, 0.38f);
            Ball(head, "NostrilHole", tip + new Vector3(0.028f * sd * ns, -0.04f * ns, 0.0f), new Vector3(0.025f, 0.012f, 0.03f) * ns, "Face_Dark", new Color(0.25f, 0.12f, 0.1f));
        }

        // ---------- Рот ----------
        float my = -0.29f, mw = 0.12f * f.mouthWidth * f.width;
        float lEnd = 0f, rEnd = 0f;
        switch (f.mouth)
        {
            case MouthStyle.Smile: lEnd = rEnd = 0.035f; break;
            case MouthStyle.Frown: lEnd = rEnd = -0.025f; break;
            case MouthStyle.Smirk: rEnd = 0.035f; lEnd = 0.005f; break;
        }
        if (f.mouth == MouthStyle.Tongue)
        {
            // широко открытый рот и язык наружу (как на фотке Макса)
            Vector3 mc = Surface(f, 0f, my - 0.04f);
            Ball(head, "MouthHole", mc + new Vector3(0, 0, 0.02f), new Vector3(0.2f, 0.16f, 0.08f), "Face_MouthIn", new Color(0.22f, 0.06f, 0.06f), 0f, 0.4f);
            Ball(head, "Teeth", mc + new Vector3(0, 0.055f, -0.005f), new Vector3(0.15f, 0.04f, 0.05f), "Face_Teeth", new Color(0.97f, 0.96f, 0.9f), 0f, 0.7f);
            TorusPart(head, "Lips", k + "_LipsO", 0.1f, 0.018f, mc + new Vector3(0, 0, -0.012f), Quaternion.Euler(90, 0, 0), new Vector3(1f, 1f, 0.8f), k + "_Lip", f.lip, 0f, 0.55f);
            var tongue = Ball(head, "Tongue", mc + new Vector3(0, -0.075f, -0.05f), new Vector3(0.12f, 0.16f, 0.06f), "Face_Tongue", new Color(0.92f, 0.45f, 0.48f), 0f, 0.75f);
            tongue.transform.localRotation = Quaternion.Euler(-35, 0, 0);
        }
        else
        {
            var up = new Vector3[7]; var low = new Vector3[7];
            for (int i = 0; i < 7; i++)
            {
                float t = Mathf.Lerp(-1f, 1f, i / 6f);
                float end = t < 0 ? lEnd : rEnd;
                float yy = my + end * t * t + (1f - t * t) * 0.006f;
                up[i] = Surface(f, t * mw, yy + 0.012f) + new Vector3(0, 0, -0.012f);
                low[i] = Surface(f, t * mw * 0.85f, yy - 0.014f - (1f - t * t) * 0.006f) + new Vector3(0, 0, -0.014f);
            }
            TubePart(head, "LipUp", k + "_LipUp", up, new[] { 0.006f, 0.012f, 0.014f, 0.012f, 0.014f, 0.012f, 0.006f }, k + "_Lip", f.lip, 0f, 0.5f, 8);
            TubePart(head, "LipLow", k + "_LipLow", low, new[] { 0.006f, 0.014f, 0.019f, 0.021f, 0.019f, 0.014f, 0.006f }, k + "_Lip", f.lip, 0f, 0.5f, 8);
        }

        // ---------- Волосы: «шапка» по форме головы + пряди ----------
        if (f.hairStyle != HairStyle.None)
        {
            float vol = f.hairVolume;
            MeshPart(head, "Hair", SaveMesh(k + "_HairCap", ProcMesh.Blob(d =>
            {
                float mk = HairMask(f, d);
                var p = HeadShape(f, d);
                float noise = f.hairNoise * (Hash(d * 7f, f.seed) - 0.5f) * mk;
                return p * (1f - 0.06f * (1f - mk)) + p.normalized * (vol * mk + noise);
            }, 48, 36)), Vector3.zero, Quaternion.identity, Vector3.one, k + "_Hair", f.hair, 0f, 0.3f);

            var rnd = new System.Random(f.seed);
            if (f.hairStyle == HairStyle.Bowl || f.hairStyle == HairStyle.Messy)
            {
                // чёлка: пряди с макушки на лоб, до бровей
                int n = f.hairStyle == HairStyle.Bowl ? 13 : 9;
                for (int i = 0; i < n; i++)
                {
                    float t = Mathf.Lerp(-0.85f, 0.85f, i / (float)(n - 1)) + ((float)rnd.NextDouble() - 0.5f) * 0.08f;
                    float x = t * 0.32f * f.width;
                    float endY = (f.hairStyle == HairStyle.Bowl ? 0.2f : 0.22f) + Mathf.Abs(t) * 0.04f + (float)rnd.NextDouble() * 0.03f;
                    Vector3 a = Surface(f, x * 0.8f, 0.42f) + new Vector3(0, 0, -vol);
                    Vector3 b = Surface(f, x, endY + 0.1f) + new Vector3(0, 0, -vol - 0.02f);
                    Vector3 e = Surface(f, x * 1.05f + (f.hairStyle == HairStyle.Messy ? ((float)rnd.NextDouble() - 0.5f) * 0.06f : 0f), endY) + new Vector3(0, 0, -vol * 0.6f - 0.03f);
                    TubePart(head, "Fringe", $"{k}_Fringe{i}", ProcMesh.Bezier(a, Vector3.Lerp(a, b, 0.6f) + new Vector3(0, 0, -0.03f), b, e, 8),
                        ProcMesh.Taper(8, 0.045f, 0.008f, 1.3f), k + "_Hair", f.hair, 0f, 0.3f, 7);
                }
            }
            if (f.hairStyle == HairStyle.Long)
            {
                // длинные волосы: пряди с висков и затылка свисают ниже плеч, спереди — обрамляют лицо
                Color tipC = f.hairTip.a > 0f ? f.hairTip : f.hair;
                int n = 30;
                for (int i = 0; i < n; i++)
                {
                    float a = Mathf.Lerp(-110f, 110f, i / (float)(n - 1)) * Mathf.Deg2Rad;    // 0 — затылок
                    float jitter = ((float)rnd.NextDouble() - 0.5f) * 0.1f;
                    var d = new Vector3(Mathf.Sin(a), 0.25f + jitter, Mathf.Cos(a)).normalized;
                    Vector3 root = HeadShape(f, d) * (1f + vol);
                    Vector3 outDir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    float len = f.hairLength + 0.3f + (float)rnd.NextDouble() * 0.25f - Mathf.Abs(Mathf.Sin(a)) * 0.15f;
                    Vector3 end = root + Vector3.down * len + outDir * 0.18f;
                    var path = ProcMesh.Bezier(root, root + outDir * 0.12f + Vector3.down * 0.15f, end + Vector3.up * len * 0.45f + outDir * 0.1f, end, 10);
                    bool pink = i % 3 == 1;
                    TubePart(head, "LongHair", $"{k}_Long{i}", path, ProcMesh.Taper(10, 0.075f, 0.02f, 0.8f), pink ? k + "_HairTip" : k + "_Hair", pink ? tipC : f.hair, 0f, 0.45f, 7);
                }
                // передние пряди по бокам лица (чёлка-шторка с пробором)
                foreach (float sd in new[] { -1f, 1f })
                    for (int j = 0; j < 3; j++)
                    {
                        float x = (0.2f + j * 0.06f) * f.width * sd;
                        Vector3 a = Surface(f, x * 0.6f, 0.45f) + new Vector3(0, 0, -vol);
                        Vector3 b = Surface(f, x * 1.25f, 0.1f) + new Vector3(0, 0, -vol - 0.02f);
                        Vector3 e = new Vector3(x * 1.35f, -0.55f - j * 0.12f - (float)rnd.NextDouble() * 0.1f, b.z + 0.08f);
                        TubePart(head, "FrontHair", $"{k}_Front{(sd < 0 ? "L" : "R")}{j}", ProcMesh.Bezier(a, b, Vector3.Lerp(b, e, 0.5f) + new Vector3(0.03f * sd, 0, -0.02f), e, 12),
                            ProcMesh.Taper(12, 0.06f, 0.015f, 0.9f), j == 1 ? k + "_HairTip" : k + "_Hair", j == 1 ? tipC : f.hair, 0f, 0.45f, 7);
                    }
            }
            if (f.hairStyle == HairStyle.Messy || f.hairStyle == HairStyle.Short)
            {
                // торчащие пучки на макушке и затылке
                int n = f.hairStyle == HairStyle.Messy ? 22 : 9;
                for (int i = 0; i < n; i++)
                {
                    float th = (float)rnd.NextDouble() * 1.25f, ph = (float)rnd.NextDouble() * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    if (HairMask(f, d) < 0.8f) continue;
                    Vector3 root = HeadShape(f, d) * (1f + vol * 0.6f);
                    Vector3 dir = (d + new Vector3(0, 0.25f, 0.35f)).normalized;   // назад и вверх
                    float len = (f.hairStyle == HairStyle.Messy ? 0.16f : 0.08f) + (float)rnd.NextDouble() * 0.08f;
                    Spike(head, "Tuft", $"{k}_Tuft{i}", root, dir, len, f.hairStyle == HairStyle.Messy ? 0.06f : 0.04f, k + "_Hair", f.hair, 0.3f);
                }
            }
        }
        return head;
    }
}
