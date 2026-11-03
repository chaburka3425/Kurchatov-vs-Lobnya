using UnityEngine;

// Глиф: на 5 секунд башни, трон и крипы команды перестают получать урон. Перезарядка 2 минуты.
public static class Glyph
{
    public const float Duration = 5f;
    public const float Cooldown = 120f;

    static readonly float[] endTime = new float[3];
    static readonly float[] readyTime = new float[3];

    public static void ResetAll()
    {
        for (int i = 0; i < 3; i++) { endTime[i] = 0f; readyTime[i] = 0f; }
    }

    public static bool IsActive(Team t) => t != Team.Neutral && Time.time < endTime[(int)t];
    public static float CooldownLeft(Team t) => Mathf.Max(0f, readyTime[(int)t] - Time.time);
    public static float ActiveLeft(Team t) => t == Team.Neutral ? 0f : Mathf.Max(0f, endTime[(int)t] - Time.time);

    // Защищён ли юнит глифом прямо сейчас
    public static bool Protects(Unit u)
    {
        if (u == null || !IsActive(u.team)) return false;
        return u.kind == UnitKind.Structure || u.kind == UnitKind.Creep;
    }

    // Клиент сети: таймеры глифа приходят от хоста
    public static void NetSet(Team t, float activeLeft, float cdLeft) { endTime[(int)t] = Time.time + activeLeft; readyTime[(int)t] = Time.time + cdLeft; }

    public static bool Use(Team t)
    {
        if (t == Team.Neutral) return false;
        if (Net.IsClient) { if (CooldownLeft(t) > 0f) return false; NetCmd.Glyph(); return true; }
        if (CooldownLeft(t) > 0f) return false;
        endTime[(int)t] = Time.time + Duration;
        readyTime[(int)t] = Time.time + Cooldown;
        foreach (var u in Unit.All)
            if (u.IsAlive && u.team == t && (u.kind == UnitKind.Structure || u.kind == UnitKind.Creep))
                ParticleBurst.Spawn(u.transform.position + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.3f), u.kind == UnitKind.Creep ? 5 : 16);
        var player = HeroController.Player;
        if (player != null)
        {
            var pu = player.GetComponent<Unit>();
            string text = pu != null && pu.team == t ? "ГЛИФ! Строения и крипы неуязвимы 5 сек" : "Враг включил глиф!";
            FloatingText.Show(player.transform.position + Vector3.up * 5f, text, new Color(1f, 0.85f, 0.3f), 30);
        }
        return true;
    }
}
