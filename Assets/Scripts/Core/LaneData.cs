using UnityEngine;

// Три линии, башни и лесные лагеря. Координаты мира: Курчатов — внизу слева, Москва — вверху справа.
// Все точки ниже записаны для карты 120×120 и умножаются на Scale (карта стала больше).
public static class LaneData
{
    public enum Lane { Top, Mid, Bot }
    public static readonly Lane[] All = { Lane.Top, Lane.Mid, Lane.Bot };

    public const float Scale = 1.8f;          // во сколько раз карта больше исходной
    public const float Half = 60f * Scale;    // от центра до края карты
    public const float LaneHalfWidth = 4.2f;  // полширины дороги (не масштабируется)

    public static string Ru(Lane l) => l switch { Lane.Top => "верх", Lane.Mid => "мид", _ => "низ" };

    static Vector3 P(float x, float z) => new Vector3(x * Scale, 0, z * Scale);
    // Точка «старой» карты 120×120 → настоящая точка мира
    public static Vector3 W(float x, float z) => P(x, z);
    public static Vector3 Throne(Team t) => t == Team.Kurchatov ? P(-45, -45) : P(45, 45);

    // Точка выхода крипов
    public static Vector3 Spawn(Team team, Lane lane) => (team, lane) switch
    {
        (Team.Kurchatov, Lane.Top) => P(-50, -32),
        (Team.Kurchatov, Lane.Mid) => P(-38, -26),
        (Team.Kurchatov, Lane.Bot) => P(-32, -50),
        (_, Lane.Top) => P(32, 50),
        (_, Lane.Mid) => P(38, 26),
        _ => P(50, 32),
    };

    // Путь крипов по линии до вражеского трона
    public static Vector3[] Path(Team team, Lane lane)
    {
        bool k = team == Team.Kurchatov;
        switch (lane)
        {
            case Lane.Top:
                return k ? new[] { P(-50, -20), P(-50, 20), P(-48, 48), P(-20, 50), P(20, 50), P(40, 45) }
                         : new[] { P(20, 50), P(-20, 50), P(-48, 48), P(-50, 20), P(-50, -20), P(-45, -40) };
            case Lane.Mid:
                return k ? new[] { P(-24, -22), P(-7, -7), P(7, 7), P(22, 22), P(40, 40) }
                         : new[] { P(24, 22), P(7, 7), P(-7, -7), P(-22, -22), P(-40, -40) };
            default:
                return k ? new[] { P(-20, -50), P(20, -50), P(48, -48), P(50, -20), P(50, 20), P(45, 40) }
                         : new[] { P(50, 20), P(50, -20), P(48, -48), P(20, -50), P(-20, -50), P(-40, -45) };
        }
    }

    // Башни Курчатова; у Москвы — зеркально (-x, -z), верх и низ меняются местами.
    public struct TowerSpot { public Lane lane; public int tier; public Vector3 pos; }

    public static TowerSpot[] Towers(Team team)
    {
        var k = new[]
        {
            new TowerSpot { lane = Lane.Top, tier = 1, pos = P(-50, 5) },
            new TowerSpot { lane = Lane.Top, tier = 2, pos = P(-50, -22) },
            new TowerSpot { lane = Lane.Mid, tier = 1, pos = P(-15, -15) },
            new TowerSpot { lane = Lane.Mid, tier = 2, pos = P(-30, -30) },
            new TowerSpot { lane = Lane.Bot, tier = 1, pos = P(5, -50) },
            new TowerSpot { lane = Lane.Bot, tier = 2, pos = P(-22, -50) },
        };
        if (team == Team.Kurchatov) return k;
        for (int i = 0; i < k.Length; i++)
        {
            k[i].pos = new Vector3(-k[i].pos.x, 0, -k[i].pos.z);
            k[i].lane = k[i].lane == Lane.Top ? Lane.Bot : k[i].lane == Lane.Bot ? Lane.Top : Lane.Mid;
        }
        return k;
    }

    // Лагеря гопников: позиция, чья половина леса, большой ли (с «Главным гопником»)
    public struct Camp { public Vector3 pos; public Team side; public bool big; }

    public static readonly Camp[] Camps =
    {
        new Camp { pos = P(-36, 12), side = Team.Kurchatov, big = true },
        new Camp { pos = P(-30, 38), side = Team.Lobnya, big = true },
        new Camp { pos = P(12, -36), side = Team.Kurchatov, big = true },
        new Camp { pos = P(38, -30), side = Team.Lobnya, big = true },
        new Camp { pos = P(-12, 36), side = Team.Lobnya, big = false },
        new Camp { pos = P(36, -12), side = Team.Lobnya, big = false },
        new Camp { pos = P(-38, 30), side = Team.Kurchatov, big = false },
        new Camp { pos = P(30, -38), side = Team.Kurchatov, big = false },
        // новые лагеря: ближе к своей базе и между линиями
        new Camp { pos = P(-22, -4), side = Team.Kurchatov, big = true },
        new Camp { pos = P(-4, -22), side = Team.Kurchatov, big = false },
        new Camp { pos = P(-40, -8), side = Team.Kurchatov, big = false },
        new Camp { pos = P(-8, -40), side = Team.Kurchatov, big = false },
        new Camp { pos = P(22, 4), side = Team.Lobnya, big = true },
        new Camp { pos = P(4, 22), side = Team.Lobnya, big = false },
        new Camp { pos = P(40, 8), side = Team.Lobnya, big = false },
        new Camp { pos = P(8, 40), side = Team.Lobnya, big = false },
    };

    // Прямоугольники боковых линий (x1, z1, x2, z2) — для земли, мини-карты и декора
    // ширина дороги та же, длина — по масштабу карты
    const float E = 50f * Scale, W0 = LaneHalfWidth, L0 = 46f * Scale;
    public static readonly Vector4[] SideRects =
    {
        new Vector4(-E - W0, -L0, -E + W0, E + W0),   // верх: левый край
        new Vector4(-E - W0, E - W0, L0, E + W0),     // верх: верхний край
        new Vector4(-L0, -E - W0, E + W0, -E + W0),   // низ: нижний край
        new Vector4(E - W0, -E - W0, E + W0, L0),     // низ: правый край
    };

    public static bool OnAnyLane(float x, float z, float margin)
    {
        if (Mathf.Abs(x - z) / 1.4142f < margin) return true;   // мид
        float m = Mathf.Min(margin, 6.5f);   // у края карты оставляем лес
        foreach (var r in SideRects)
            if (x > r.x - m + 4.2f && x < r.z + m - 4.2f && z > r.y - m + 4.2f && z < r.w + m - 4.2f) return true;
        return false;
    }

    // Куда встать в начале игры на выбранной линии: чуть позади своей первой башни
    public static Vector3 LaneStart(Team team, Lane lane)
    {
        foreach (var t in Towers(team))
            if (t.lane == lane && t.tier == 1)
            {
                Vector3 throne = Throne(team);
                return t.pos + (throne - t.pos).normalized * 4f;
            }
        return P(0, 0);
    }

    public static Vector3 JungleStart(Team team)
    {
        foreach (var c in Camps)
            if (c.side == team && c.big) return c.pos + (team == Team.Kurchatov ? P(-4, -4) : P(4, 4));
        return P(0, 0);
    }
}
