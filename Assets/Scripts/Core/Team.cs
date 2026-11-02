using UnityEngine;

// Стороны игры. Neutral — на будущее (лесные крипы).
public enum Team { Kurchatov, Lobnya, Neutral }

public static class TeamUtil
{
    public static bool AreEnemies(Team a, Team b) => a != b;

    public static string GetName(Team team) => team switch
    {
        Team.Kurchatov => "Курчатов",
        Team.Lobnya => "Москва",
        _ => "Лес"
    };

    public static Color GetColor(Team team) => team switch
    {
        Team.Kurchatov => new Color(0.25f, 0.55f, 1f),
        Team.Lobnya => new Color(1f, 0.3f, 0.25f),
        _ => new Color(0.85f, 0.65f, 0.3f)
    };
}
