using System;
using System.Text;
using UnityEngine;

public enum AbilityTarget { Passive, NoTarget, EnemyUnit, AllyUnit, Point }

// Описание способности: все цифры баланса по уровням — здесь.
public class AbilityInfo
{
    public string id;
    public string key;            // Q / W / E / R
    public string name;
    public string description;    // {0}, {1}, {2} — подставляются values
    public AbilityTarget target;
    public bool isUltimate;       // ульта: максимум 3 уровня, открывается на 6/12/18
    public int maxLevel = 4;
    public float castRange;
    public float[] manaCosts = { 0 };   // по уровням
    public float[] cooldowns = { 0 };   // по уровням
    public float[][] values = new float[0][]; // values[i][уровень-1]
    public Color color = Color.gray;
    public string shareGroup;     // способности с одной группой качаются вместе (койлы Сани)
    public float distance;        // койл: на сколько метров перед героем
    public float radius;          // койл: радиус круга
    public string shortName;      // короткое имя для кнопки
    public int charges;           // заряды (Золотой дождь): 0 — обычная перезарядка
    public float chargeCooldown;  // за сколько копится один заряд

    static float At(float[] a, int level) =>
        a == null || a.Length == 0 ? 0f : a[Mathf.Clamp(level - 1, 0, a.Length - 1)];

    public float ManaCost(int level) => At(manaCosts, level);
    public float Cooldown(int level) => At(cooldowns, level);
    public float Value(int i, int level) => At(values[i], level);

    // "60/120/180/240", текущий уровень подсвечен (0 — без подсветки)
    public static string Join(float[] arr, int current)
    {
        if (arr == null || arr.Length == 0) return "0";
        bool same = true;
        foreach (var v in arr) if (!Mathf.Approximately(v, arr[0])) same = false;
        if (same) return arr[0].ToString("0.##");

        var sb = new StringBuilder();
        for (int i = 0; i < arr.Length; i++)
        {
            if (i > 0) sb.Append('/');
            string v = arr[i].ToString("0.##");
            sb.Append(i == current - 1 ? $"<color=#ffd84a><b>{v}</b></color>" : v);
        }
        return sb.ToString();
    }

    public string FormatDescription(int currentLevel)
    {
        var args = new object[values.Length];
        for (int i = 0; i < values.Length; i++) args[i] = Join(values[i], currentLevel);
        return string.Format(description, args);
    }

    public string FormattedDescription => FormatDescription(0);
    public string ManaText(int lvl) => Join(manaCosts, lvl);
    public string CooldownText(int lvl) => Join(cooldowns, lvl);
}

// Описание героя: атрибуты, статы, способности.
public class HeroDefinition
{
    public string id;
    public string name;
    public string title;
    public string lore;
    public HeroAttribute attribute;

    public float baseStr, strGain;
    public float baseAgi, agiGain;
    public float baseInt, intGain;

    public float baseDamage = 40f;
    public float baseArmor = 2f;
    public float attackRange = 2f;
    public float baseAttackTime = 1.7f;
    public float moveSpeed = 5f;

    public Color bodyColor = Color.white;
    public bool ranged;
    public Color projectileColor = new Color(0.7f, 0.3f, 0.2f);
    public string innate;          // врождённая способность (текст)
    public string deathLine;       // фраза при смерти
    public int deathVoice = -1;    // номер голосовой фразы смерти (line1 = 0)
    public float projectileSpeed;  // 0 — обычная скорость снаряда
    public bool bulletProjectile;  // снаряд — пуля (а не шарик)
    public AbilityInfo[] abilities = new AbilityInfo[0];

    // --- Формулы атрибутов (как в MOBA) ---
    public float StrAt(int level) => baseStr + strGain * (level - 1);
    public float AgiAt(int level) => baseAgi + agiGain * (level - 1);
    public float IntAt(int level) => baseInt + intGain * (level - 1);

    public float MaxHealthAt(int l) => 120f + 22f * StrAt(l);
    public float HealthRegenAt(int l) => 1f + 0.1f * StrAt(l);
    public float MaxManaAt(int l) => 75f + 12f * IntAt(l);
    public float ManaRegenAt(int l) => 0.5f + 0.05f * IntAt(l);
    public float ArmorAt(int l) => baseArmor + AgiAt(l) / 6f;
    public float AttackSpeedAt(int l) => 100f + AgiAt(l);

    public float DamageAt(int l) => baseDamage + attribute switch
    {
        HeroAttribute.Strength => StrAt(l),
        HeroAttribute.Agility => AgiAt(l),
        HeroAttribute.Intelligence => IntAt(l),
        _ => 0.7f * (StrAt(l) + AgiAt(l) + IntAt(l))
    };
}

// Все герои игры. Новых героев добавляем сюда.
public static class HeroCatalog
{
    // Огр Mog — по мотивам двухголового огра-мага.
    public static readonly HeroDefinition OgrMog = new HeroDefinition
    {
        id = "ogr_mog",
        name = "Огр Mog",
        title = "Двухголовый повар из «Чикен Пиццы»",
        lore = "Две головы — две порции. Левая отвечает за тесто, правая за соус, " +
               "и они ни разу в жизни не договорились. Зато когда обе злятся одновременно — " +
               "пиццы летят без остановки.",
        attribute = HeroAttribute.Strength,
        baseStr = 23, strGain = 3.5f,
        baseAgi = 14, agiGain = 1.9f,
        baseInt = 17, intGain = 2.5f,
        baseDamage = 40, baseArmor = 3,
        attackRange = 2.2f, baseAttackTime = 1.7f, moveSpeed = 5.5f,
        bodyColor = new Color(0.95f, 0.95f, 0.95f),
        abilities = new[]
        {
            new AbilityInfo
            {
                id = "hot_pizza", key = "Q", name = "Горячая пицца",
                description = "Швыряет раскалённую пиццу во врага: {0} магического урона и оглушение на {1} сек.",
                target = AbilityTarget.EnemyUnit, castRange = 6,
                manaCosts = new float[] { 75, 85, 95, 105 },
                cooldowns = new float[] { 11, 10, 9, 8 },
                values = new[] { new float[] { 60, 120, 180, 240 }, new float[] { 1.5f, 1.5f, 1.5f, 1.5f } },
                color = new Color(1f, 0.55f, 0.12f)
            },
            new AbilityInfo
            {
                id = "spicy_sauce", key = "W", name = "Острый соус",
                description = "Обливает врага острым соусом: {0} урона в секунду в течение {1} сек. и замедление на {2}%.",
                target = AbilityTarget.EnemyUnit, castRange = 8,
                manaCosts = new float[] { 90 },
                cooldowns = new float[] { 15 },
                values = new[] { new float[] { 20, 30, 40, 50 }, new float[] { 5, 6, 7, 8 }, new float[] { 20, 22, 24, 26 } },
                color = new Color(0.85f, 0.12f, 0.1f)
            },
            new AbilityInfo
            {
                id = "energy_drink", key = "E", name = "Энергетик",
                description = "Даёт союзнику +{0} к скорости атаки и +{1}% к скорости бега на {2} сек. " +
                              "Нажми E два раза, чтобы выпить самому.",
                target = AbilityTarget.AllyUnit, castRange = 6,
                manaCosts = new float[] { 50 },
                cooldowns = new float[] { 20 },
                values = new[] { new float[] { 20, 30, 40, 50 }, new float[] { 10, 12, 14, 16 }, new float[] { 20 } },
                color = new Color(0.3f, 0.8f, 0.3f)
            },
            new AbilityInfo
            {
                id = "double_portion", key = "R", name = "Двойная порция",
                description = "Пассивная ульта. Способности иногда срабатывают несколько раз: " +
                              "x2 — {0}%, x3 — {1}%, x4 — {2}%. Пицца летит повторно, соус разлетается " +
                              "по соседним врагам, энергетик достаётся соседним союзникам.",
                target = AbilityTarget.Passive, isUltimate = true, maxLevel = 3,
                values = new[] { new float[] { 30, 40, 50 }, new float[] { 10, 15, 20 }, new float[] { 0, 5, 10 } },
                color = new Color(1f, 0.8f, 0.2f)
            }
        }
    };

    static AbilityInfo Coil(string id, string key, string name, string shortName, float dist) => new AbilityInfo
    {
        id = id, key = key, name = name, shortName = shortName,
        description = $"Колбаски падают в круг в {dist:0.#} м перед Саней: {{0}} магического урона + {{1}} за каждую сосиску. " +
                      "Q, W и E качаются вместе одним очком.",
        target = AbilityTarget.NoTarget, castRange = dist, distance = dist, radius = 2.4f, shareGroup = "coil",
        manaCosts = new float[] { 75, 80, 85, 90 },
        cooldowns = new float[] { 10 },
        values = new[] { new float[] { 90, 150, 210, 270 }, new float[] { 3 } },
        color = new Color(0.78f, 0.2f, 0.17f)
    };

    // Саня Финд — копит сосиски вместо душ.
    public static readonly HeroDefinition SanyaFiend = new HeroDefinition
    {
        id = "sanya_fiend",
        name = "Саня Финд",
        title = "Повелитель колбасы",
        lore = "Саня всегда приходит с шампуром. Говорят, он ни разу не ушёл с мангала голодным — " +
               "за каждого побеждённого он забирает себе сосиску, а когда их набирается много, " +
               "начинается колбасный реквием.",
        attribute = HeroAttribute.Agility,
        baseStr = 19, strGain = 2.3f,
        baseAgi = 20, agiGain = 3.2f,
        baseInt = 18, intGain = 2.2f,
        baseDamage = 30, baseArmor = 1,
        attackRange = 5.5f, baseAttackTime = 1.6f, moveSpeed = 5.6f,
        ranged = true, projectileColor = new Color(0.72f, 0.3f, 0.2f),
        bodyColor = new Color(0.1f, 0.1f, 0.12f),
        innate = "Сосисочный запас: +1 сосиска за добитого крипа, +3 за героя (до 24). " +
                 "Каждая сосиска — +2 к урону. При смерти теряет половину.",
        deathLine = "Даже моя колбаска не вечна...",
        deathVoice = 4,
        abilities = new[]
        {
            Coil("sausage_coil_near", "Q", "Койл близко", "Койл близко", 2.5f),
            Coil("sausage_coil_mid", "W", "Койл средне", "Койл средне", 5f),
            Coil("sausage_coil_far", "E", "Койл далеко", "Койл далеко", 7.5f),
            new AbilityInfo
            {
                id = "sausage_requiem", key = "R", name = "Колбасный реквием", shortName = "Реквием",
                description = "Саня 1.7 сек. собирает сосиски, потом стреляет колбасками во все стороны: " +
                              "по одной за каждую сосиску (минимум {1}), каждая — {0} магического урона. " +
                              "Если Саню оглушить — ульта сорвётся.",
                target = AbilityTarget.NoTarget, isUltimate = true, maxLevel = 3, castRange = 9,
                manaCosts = new float[] { 150, 175, 200 },
                cooldowns = new float[] { 120, 110, 100 },
                values = new[] { new float[] { 80, 110, 140 }, new float[] { 3 } },
                color = new Color(1f, 0.48f, 0.1f)
            }
        }
    };

    // Макс Gold — снайпер с золотой снайперкой.
    public static readonly HeroDefinition MaxGold = new HeroDefinition
    {
        id = "max_gold",
        name = "Макс Gold",
        title = "Золотой снайпер",
        lore = "Макс ничего не делает наполовину: кроссовки — золотые, наплечники — золотые, " +
               "снайперка — тоже золотая. С Лестницы Габилджона он видит полкарты, " +
               "а если навёл прицел — лучше сразу бежать в баню.",
        attribute = HeroAttribute.Agility,
        baseStr = 17, strGain = 1.9f,
        baseAgi = 21, agiGain = 3.2f,
        baseInt = 15, intGain = 2.4f,
        baseDamage = 32, baseArmor = 1,
        attackRange = 5.5f, baseAttackTime = 1.7f, moveSpeed = 5.3f,
        ranged = true, projectileColor = new Color(1f, 0.8f, 0.25f), projectileSpeed = 42f, bulletProjectile = true,
        bodyColor = new Color(0.08f, 0.08f, 0.1f),
        innate = "Золотая снайперка: Лестница Габилджона даёт дальность атаки, а Хантерас пик достаёт врага через полкарты.",
        deathLine = "Макс Gold пал...",
        deathVoice = 5,   // voice-message 9
        abilities = new[]
        {
            new AbilityInfo
            {
                id = "gold_rain", key = "Q", name = "Золотой дождь", shortName = "Дождь",
                description = "Через 1.2 сек. на круг радиусом 4.5 льётся золото: {0} магического урона в секунду " +
                              "и замедление {1}% на {2} сек. Под дождём всё видно. 3 заряда, новый — раз в 35 сек.",
                target = AbilityTarget.Point, castRange = 18, radius = 4.5f, charges = 3, chargeCooldown = 35,
                manaCosts = new float[] { 50 },
                cooldowns = new float[] { 0 },
                values = new[] { new float[] { 20, 45, 70, 95 }, new float[] { 15, 20, 25, 30 }, new float[] { 10 } },
                color = new Color(1f, 0.76f, 0.23f)
            },
            new AbilityInfo
            {
                id = "gold_shot", key = "W", name = "Голдшот",
                description = "Пассивка. Выстрел с шансом {0}% попадает в голову: +{1} урона, " +
                              "цель отбрасывает и на 0.5 сек. почти останавливает.",
                target = AbilityTarget.Passive,
                values = new[] { new float[] { 40 }, new float[] { 20, 50, 80, 110 } },
                color = new Color(0.88f, 0.63f, 0.13f)
            },
            new AbilityInfo
            {
                id = "gabil_ladder", key = "E", name = "Лестница Габилджона", shortName = "Лестница",
                description = "Пассивно: +{0} к дальности атаки. Нажми — Макс залезает на золотую лестницу: " +
                              "ещё +{1} к дальности, и каждый выстрел — Голдшот, {2} сек. На лестнице бегает на 30% медленнее.",
                target = AbilityTarget.NoTarget,
                manaCosts = new float[] { 50 },
                cooldowns = new float[] { 20, 18, 16, 14 },
                values = new[] { new float[] { 1, 2, 3, 4 }, new float[] { 2 }, new float[] { 3 } },
                color = new Color(0.35f, 0.63f, 0.88f)
            },
            new AbilityInfo
            {
                id = "hunters_peak", key = "R", name = "Хантерас пик", shortName = "Хантерас",
                description = "Макс {1} сек. целится через оптику (цель подсвечена), потом стреляет из золотой снайперки " +
                              "прямо в голову: {0} магического урона и короткое оглушение. Если Макса оглушить — прицел сбит.",
                target = AbilityTarget.EnemyUnit, isUltimate = true, maxLevel = 3, castRange = 30,
                manaCosts = new float[] { 175, 225, 275 },
                cooldowns = new float[] { 20, 15, 10 },
                values = new[] { new float[] { 320, 485, 650 }, new float[] { 2 } },
                color = new Color(0.85f, 0.2f, 0.12f)
            }
        }
    };

    // Пивмавер — пивная «паучиха»: вместо паучков плодит маленькие бутылочки.
    public static readonly HeroDefinition Pivmaver = new HeroDefinition
    {
        id = "pivmaver",
        name = "Пивмавер",
        title = "Мать бутылочек",
        lore = "Восемь ног, бочонок на спине и вечно недовольное лицо. Пивмавер плетёт пивную пену " +
               "по всему лесу, а из каждого, кого он добил, вылезают маленькие злые бутылочки. " +
               "Когда начинается Пивной голод — лучше не стоять рядом.",
        attribute = HeroAttribute.Agility,
        baseStr = 18, strGain = 2.6f,
        baseAgi = 19, agiGain = 3.0f,
        baseInt = 17, intGain = 2.0f,
        baseDamage = 34, baseArmor = 2,
        attackRange = 2.2f, baseAttackTime = 1.7f, moveSpeed = 5.7f,
        bodyColor = new Color(0.16f, 0.17f, 0.32f),
        innate = "Бутылочный выводок: бутылочки бьют врагов рядом с Пивмавером и живут 60 сек.",
        deathLine = "Пиво кончилось...",
        deathVoice = 5,   // последнее голосовое (line6)
        abilities = new[]
        {
            new AbilityInfo
            {
                id = "pivo_bottles", key = "Q", name = "Бутылочный выводок", shortName = "Бутылочки",
                description = "Швыряет бутылку во врага: {0} магического урона. Если цель умрёт в течение 3 сек., " +
                              "из неё вылезут {1} маленьких бутылочки (живут 60 сек., у каждой {2} здоровья и {3} урона).",
                target = AbilityTarget.EnemyUnit, castRange = 7,
                manaCosts = new float[] { 70, 85, 100, 115 },
                cooldowns = new float[] { 10, 9, 8, 7 },
                values = new[] { new float[] { 75, 150, 225, 300 }, new float[] { 2, 3, 3, 4 }, new float[] { 180, 220, 260, 300 }, new float[] { 12, 16, 20, 24 } },
                color = new Color(0.8f, 0.45f, 0.12f)
            },
            new AbilityInfo
            {
                id = "pivo_foam", key = "W", name = "Пивная пена", shortName = "Пена",
                description = "Разливает пивную пену радиусом 7 м на {2} сек. В пене Пивмавер и его бутылочки бегают на {0}% быстрее " +
                              "и лечатся на {1} здоровья в секунду. 2 заряда, новый — раз в 25 сек.",
                target = AbilityTarget.Point, castRange = 10, radius = 7f, charges = 2, chargeCooldown = 25,
                manaCosts = new float[] { 50 },
                cooldowns = new float[] { 0 },
                values = new[] { new float[] { 10, 15, 20, 25 }, new float[] { 4, 6, 8, 10 }, new float[] { 40 } },
                color = new Color(0.95f, 0.88f, 0.6f)
            },
            new AbilityInfo
            {
                id = "pivo_bite", key = "E", name = "Похмельный укус", shortName = "Укус",
                description = "Пассивка. Каждый удар Пивмавера замедляет цель на {0}% на 2 сек. и наносит ещё {1} урона.",
                target = AbilityTarget.Passive,
                values = new[] { new float[] { 20, 30, 40, 50 }, new float[] { 8, 14, 20, 26 } },
                color = new Color(0.7f, 0.15f, 0.12f)
            },
            new AbilityInfo
            {
                id = "pivo_hunger", key = "R", name = "Пивной голод", shortName = "Голод",
                description = "На {2} сек. Пивмавер звереет: +{0} к урону, и {1}% урона от ударов возвращается ему здоровьем.",
                target = AbilityTarget.NoTarget, isUltimate = true, maxLevel = 3,
                manaCosts = new float[] { 100 },
                cooldowns = new float[] { 45, 40, 35 },
                values = new[] { new float[] { 60, 80, 100 }, new float[] { 50, 70, 90 }, new float[] { 14 } },
                color = new Color(0.9f, 0.2f, 0.1f)
            }
        }
    };

    // Шашлык Ассасин — охотница за шашлыком по мотивам Templar Assassin.
    public static readonly HeroDefinition Shashlik = new HeroDefinition
    {
        id = "shashlik",
        name = "Шашлык Ассасин",
        title = "Охотница за шашлыком",
        lore = "Она выходит на охоту, когда над районом тянет дымком мангала. Маринад держит любой удар, " +
               "из дыма она бьёт так, что броня слетает, а её длинный шампур нанизывает врагов, как куски мяса. " +
               "Наступил в капкан — считай, тебя уже жарят.",
        attribute = HeroAttribute.Agility,
        baseStr = 18, strGain = 2.4f,
        baseAgi = 23, agiGain = 3.3f,
        baseInt = 18, intGain = 2.0f,
        baseDamage = 34, baseArmor = 2,
        attackRange = 3.2f, baseAttackTime = 1.7f, moveSpeed = 5.8f,
        bodyColor = new Color(0.45f, 0.2f, 0.55f),
        innate = "Охотница: шампур бьёт на 3.2 м и дальше от Длинного шампура, удар проходит насквозь.",
        deathLine = "Шашлык остыл...",
        deathVoice = 5,   // последнее голосовое (line6)
        abilities = new[]
        {
            new AbilityInfo
            {
                id = "shash_marinade", key = "Q", name = "Маринад", shortName = "Маринад",
                description = "Шашлык в маринаде: следующие {0} ударов по героине (сильнее 5 урона) полностью гасятся, " +
                              "а её следующие {0} ударов шампуром наносят +{1} урона. Держится 17 сек.",
                target = AbilityTarget.NoTarget,
                manaCosts = new float[] { 100 },
                cooldowns = new float[] { 17, 15, 13, 11 },
                values = new[] { new float[] { 3, 4, 5, 6 }, new float[] { 20, 40, 60, 80 } },
                color = new Color(0.95f, 0.5f, 0.9f)
            },
            new AbilityInfo
            {
                id = "shash_ambush", key = "W", name = "Засада", shortName = "Засада",
                description = "Прячется в дыму мангала и становится невидимой, пока стоит на месте (до 10 сек.). " +
                              "Удар из засады наносит +{0} урона и снижает броню цели на {1} на 6 сек.",
                target = AbilityTarget.NoTarget,
                manaCosts = new float[] { 50 },
                cooldowns = new float[] { 6 },
                values = new[] { new float[] { 70, 110, 150, 190 }, new float[] { 2, 4, 6, 8 } },
                color = new Color(0.55f, 0.3f, 0.75f)
            },
            new AbilityInfo
            {
                id = "shash_skewer", key = "E", name = "Длинный шампур", shortName = "Шампур",
                description = "Пассивка. +{0} к дальности атаки, а удар нанизывает всех врагов за целью на {1} м: " +
                              "они получают столько же урона, как кусочки на шампуре.",
                target = AbilityTarget.Passive,
                values = new[] { new float[] { 0.6f, 1.2f, 1.8f, 2.4f }, new float[] { 5.5f, 6f, 6.5f, 7f } },
                color = new Color(0.75f, 0.55f, 1f)
            },
            new AbilityInfo
            {
                id = "shash_trap", key = "R", name = "Охотничий капкан", shortName = "Капкан",
                description = "Ставит невидимый для врагов капкан с кусочком шашлыка (до {0} капканов). Враг, наступивший на него, " +
                              "получает {1} урона и замедляется на 50% на 3 сек. Капкан видит всё вокруг на 6 м.",
                target = AbilityTarget.Point, isUltimate = true, maxLevel = 3, castRange = 14, radius = 1.6f,
                manaCosts = new float[] { 15 },
                cooldowns = new float[] { 9, 7, 5 },
                values = new[] { new float[] { 5, 8, 11 }, new float[] { 120, 200, 280 } },
                color = new Color(0.7f, 0.4f, 1f)
            }
        }
    };

    public static readonly HeroDefinition[] All = { OgrMog, SanyaFiend, MaxGold, Pivmaver, Shashlik };

    public static HeroDefinition Get(string id) => Array.Find(All, h => h.id == id) ?? OgrMog;
}
