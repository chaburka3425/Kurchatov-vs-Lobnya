using UnityEngine;

// Главный атрибут героя — как в MOBA.
public enum HeroAttribute { Strength, Agility, Intelligence, Universal }

public static class AttributeUtil
{
    public static readonly HeroAttribute[] All =
        { HeroAttribute.Strength, HeroAttribute.Agility, HeroAttribute.Intelligence, HeroAttribute.Universal };

    public static string GetName(HeroAttribute a) => a switch
    {
        HeroAttribute.Strength => "СИЛА",
        HeroAttribute.Agility => "ЛОВКОСТЬ",
        HeroAttribute.Intelligence => "ИНТЕЛЛЕКТ",
        _ => "УНИВЕРСАЛ"
    };

    public static Color GetColor(HeroAttribute a) => a switch
    {
        HeroAttribute.Strength => new Color(0.92f, 0.28f, 0.2f),
        HeroAttribute.Agility => new Color(0.3f, 0.85f, 0.35f),
        HeroAttribute.Intelligence => new Color(0.3f, 0.62f, 1f),
        _ => new Color(0.85f, 0.65f, 1f)
    };
}
