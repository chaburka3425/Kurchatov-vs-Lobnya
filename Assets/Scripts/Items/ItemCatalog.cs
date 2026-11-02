using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum ItemCategory { Consumable, Attributes, Equipment, Misc, Upgrade }

public enum ItemActive { None, HealOverTime, ManaOverTime, TeleportBase, SpeedBurst, MagicImmunity, Blink, Bottle, RevealInvis }

// Бонусы предмета (и сумма бонусов всех предметов героя).
[Serializable]
public struct ItemStats
{
    public float str, agi, intel;
    public float damage, attackSpeed, armor;
    public float health, mana, healthRegen, manaRegen;
    public float moveSpeed;      // в единицах Unity (0.5 = +50 к скорости)
    public float magicResist;    // 0.15 = 15%
    public float critChance, critMult;
    public float cleave, cleaveRadius;
    public float blockChance, blockAmount;
    public float burnDps, burnRadius;

    public static ItemStats operator +(ItemStats a, ItemStats b)
    {
        return new ItemStats
        {
            str = a.str + b.str, agi = a.agi + b.agi, intel = a.intel + b.intel,
            damage = a.damage + b.damage, attackSpeed = a.attackSpeed + b.attackSpeed, armor = a.armor + b.armor,
            health = a.health + b.health, mana = a.mana + b.mana,
            healthRegen = a.healthRegen + b.healthRegen, manaRegen = a.manaRegen + b.manaRegen,
            moveSpeed = a.moveSpeed + b.moveSpeed,
            magicResist = 1f - (1f - a.magicResist) * (1f - b.magicResist),
            // Пассивки одного типа не складываются — берётся лучшая (как в MOBA)
            critChance = Mathf.Max(a.critChance, b.critChance),
            critMult = Mathf.Max(a.critMult, b.critMult),
            cleave = Mathf.Max(a.cleave, b.cleave), cleaveRadius = Mathf.Max(a.cleaveRadius, b.cleaveRadius),
            blockChance = Mathf.Max(a.blockChance, b.blockChance), blockAmount = Mathf.Max(a.blockAmount, b.blockAmount),
            burnDps = Mathf.Max(a.burnDps, b.burnDps), burnRadius = Mathf.Max(a.burnRadius, b.burnRadius),
        };
    }
}

public class ItemDef
{
    public string id;
    public string name;
    public string lore;
    public ItemCategory category;
    public int cost;                 // базовый предмет — цена; улучшение — цена рецепта (0 = без рецепта)
    public string[] components = new string[0];
    public ItemStats stats;
    public Color color = Color.gray;

    public ItemActive active = ItemActive.None;
    public float activeValue, activeDuration, activeCooldown;
    public bool consumable;
    public int maxCharges;           // заряды (Бутылка кваса), 0 — без зарядов

    public bool IsUpgrade => components.Length > 0;
    public bool HasRecipe => IsUpgrade && cost > 0;
}

// Все предметы МПКА 2. Новые предметы добавляем сюда.
public static class ItemCatalog
{
    public static readonly List<ItemDef> All = new List<ItemDef>
    {
        // ===== РАСХОДНИКИ =====
        new ItemDef { id = "shawarma", name = "Шаурма", category = ItemCategory.Consumable, cost = 110,
            color = new Color(0.85f, 0.6f, 0.3f), lore = "С двойным мясом. Говорят, лечит всё.",
            active = ItemActive.HealOverTime, activeValue = 50, activeDuration = 8, consumable = true },
        new ItemDef { id = "chips", name = "Чипсы", category = ItemCategory.Consumable, cost = 60,
            color = new Color(1f, 0.85f, 0.2f), lore = "Со вкусом краба. Хрустишь — думаешь лучше.",
            active = ItemActive.ManaOverTime, activeValue = 15, activeDuration = 10, consumable = true },
        new ItemDef { id = "train_ticket", name = "Билет на электричку", category = ItemCategory.Consumable, cost = 100,
            color = new Color(0.4f, 0.7f, 0.9f), lore = "Ложится в отдельный слот (T). Довезёт к любой своей башне или в баню — до 5 билетов.",
            active = ItemActive.TeleportBase, activeDuration = 3, consumable = true },
        new ItemDef { id = "kvass_bottle", name = "Бутылка кваса", category = ItemCategory.Consumable, cost = 650,
            color = new Color(0.55f, 0.32f, 0.12f), lore = "3 глотка: каждый за 2.5 сек. даёт 110 здоровья и 60 маны. В бане бутылка снова полная.",
            active = ItemActive.Bottle, activeValue = 110, activeDuration = 2.5f, activeCooldown = 0.5f, maxCharges = 3 },
        new ItemDef { id = "pepper_dust", name = "Молотый перец", category = ItemCategory.Consumable, cost = 80,
            color = new Color(0.8f, 0.25f, 0.12f), lore = "Сыпанул — и все, кто прятался рядом (12 м), видны 12 сек. и чихают: −20% к бегу.",
            active = ItemActive.RevealInvis, activeValue = 12, activeDuration = 12, activeCooldown = 30, consumable = true },
        new ItemDef { id = "courier_scooter", name = "Самокат курьеру", category = ItemCategory.Consumable, cost = 150,
            color = new Color(0.3f, 0.85f, 0.35f), lore = "Электросамокат для твоего курьера: бегает в 1.6 раза быстрее и держит 450 урона. Покупается один раз, в инвентарь не идёт." },

        // ===== АТРИБУТЫ =====
        new ItemDef { id = "seeds", name = "Семечки", category = ItemCategory.Attributes, cost = 50,
            color = new Color(0.3f, 0.24f, 0.17f), lore = "Кулёк с рынка. Понемногу всего.",
            stats = new ItemStats { str = 1, agi = 1, intel = 1 } },
        new ItemDef { id = "dumbbell", name = "Гантелька", category = ItemCategory.Attributes, cost = 140,
            color = new Color(0.75f, 0.2f, 0.18f), lore = "Три кило. Для начала хватит.",
            stats = new ItemStats { str = 3 } },
        new ItemDef { id = "jump_rope", name = "Скакалка", category = ItemCategory.Attributes, cost = 140,
            color = new Color(0.25f, 0.7f, 0.3f), lore = "Из спортзала школы. Никто не заметит.",
            stats = new ItemStats { agi = 3 } },
        new ItemDef { id = "notebook", name = "Тетрадка", category = ItemCategory.Attributes, cost = 140,
            color = new Color(0.25f, 0.5f, 0.9f), lore = "В клетку, 48 листов. С ответами на полях.",
            stats = new ItemStats { intel = 3 } },
        new ItemDef { id = "tracksuit", name = "Олимпийка", category = ItemCategory.Attributes, cost = 800,
            color = new Color(0.12f, 0.12f, 0.15f), lore = "Тёплая, надёжная, с лампасами.",
            stats = new ItemStats { health = 200 } },
        new ItemDef { id = "energos", name = "Энергос", category = ItemCategory.Attributes, cost = 800,
            color = new Color(0.2f, 0.8f, 0.85f), lore = "Литровая банка. Сердце стучит, мана растёт.",
            stats = new ItemStats { mana = 200 } },

        // ===== СНАРЯЖЕНИЕ =====
        new ItemDef { id = "brass", name = "Кастет", category = ItemCategory.Equipment, cost = 450,
            color = new Color(0.6f, 0.6f, 0.65f), lore = "Найден у гаражей.",
            stats = new ItemStats { damage = 10 } },
        new ItemDef { id = "gloves", name = "Перчатки без пальцев", category = ItemCategory.Equipment, cost = 450,
            color = new Color(0.35f, 0.25f, 0.2f), lore = "Чтобы удобнее было бить и листать телефон.",
            stats = new ItemStats { attackSpeed = 20 } },
        new ItemDef { id = "caps_mail", name = "Кольчуга из крышек", category = ItemCategory.Equipment, cost = 550,
            color = new Color(0.7f, 0.7f, 0.55f), lore = "Собирали всем подъездом целое лето.",
            stats = new ItemStats { armor = 4 } },
        new ItemDef { id = "bat", name = "Бита", category = ItemCategory.Equipment, cost = 1000,
            color = new Color(0.65f, 0.45f, 0.25f), lore = "Бейсбольная. В бейсбол никто не играет.",
            stats = new ItemStats { damage = 18 } },

        // ===== РАЗНОЕ =====
        new ItemDef { id = "slippers", name = "Тапки", category = ItemCategory.Misc, cost = 500,
            color = new Color(0.9f, 0.4f, 0.6f), lore = "Домашние, с зайчиками. Зато быстро.",
            stats = new ItemStats { moveSpeed = 0.5f } },
        new ItemDef { id = "sausage", name = "Сосиска в тесте", category = ItemCategory.Misc, cost = 500,
            color = new Color(0.9f, 0.55f, 0.35f), lore = "Из школьной столовой. Восстанавливает силы.",
            stats = new ItemStats { healthRegen = 4 } },
        new ItemDef { id = "kefir", name = "Кефир", category = ItemCategory.Misc, cost = 175,
            color = new Color(0.95f, 0.95f, 0.95f), lore = "Полезно для головы. Так бабушка говорит.",
            stats = new ItemStats { manaRegen = 1.2f } },
        new ItemDef { id = "ushanka", name = "Ушанка", category = ItemCategory.Misc, cost = 500,
            color = new Color(0.45f, 0.35f, 0.3f), lore = "Защищает уши от мороза и от магии.",
            stats = new ItemStats { magicResist = 0.15f } },

        new ItemDef { id = "blink_scooter", name = "Самокат-блинк", category = ItemCategory.Misc, cost = 2250,
            color = new Color(0.4f, 0.75f, 1f), lore = "Электросамокат на нитро: мгновенно переносит на 12 м туда, где курсор. Если тебя ударил герой — 3 сек. не заводится.",
            active = ItemActive.Blink, activeValue = 12, activeCooldown = 15 },

        // ===== УЛУЧШЕНИЯ (собираются из предметов выше) =====
        new ItemDef { id = "band_str", name = "Браслет из резинок", category = ItemCategory.Upgrade, cost = 210,
            components = new[] { "seeds", "dumbbell" }, color = new Color(0.9f, 0.3f, 0.25f),
            lore = "Сплела сестра. Даёт силы.",
            stats = new ItemStats { str = 5, agi = 2, intel = 2, damage = 3, healthRegen = 0.75f } },
        new ItemDef { id = "band_agi", name = "Чётки", category = ItemCategory.Upgrade, cost = 210,
            components = new[] { "seeds", "jump_rope" }, color = new Color(0.3f, 0.85f, 0.35f),
            lore = "Щёлкаешь — руки быстрее.",
            stats = new ItemStats { agi = 5, str = 2, intel = 2, attackSpeed = 5, armor = 1 } },
        new ItemDef { id = "band_int", name = "Шпаргалка", category = ItemCategory.Upgrade, cost = 210,
            components = new[] { "seeds", "notebook" }, color = new Color(0.3f, 0.55f, 1f),
            lore = "Мелким почерком, на все случаи жизни.",
            stats = new ItemStats { intel = 5, str = 2, agi = 2, manaRegen = 0.6f } },

        new ItemDef { id = "sneakers", name = "Разные кроссовки", category = ItemCategory.Upgrade, cost = 0,
            components = new[] { "slippers", "gloves", "dumbbell" }, color = new Color(0.85f, 0.15f, 0.15f),
            lore = "Левый красный, правый белый. Стиль.",
            stats = new ItemStats { moveSpeed = 0.55f, attackSpeed = 25, str = 10 } },
        new ItemDef { id = "phase", name = "Самоходы", category = ItemCategory.Upgrade, cost = 0,
            components = new[] { "slippers", "caps_mail", "brass" }, color = new Color(0.9f, 0.6f, 0.2f),
            lore = "Сами несут тебя в драку.",
            stats = new ItemStats { moveSpeed = 0.6f, armor = 5, damage = 18 },
            active = ItemActive.SpeedBurst, activeValue = 25, activeDuration = 3, activeCooldown = 8 },
        new ItemDef { id = "crystal", name = "Дубина Mega", category = ItemCategory.Upgrade, cost = 500,
            components = new[] { "brass", "bat" }, color = new Color(0.75f, 0.3f, 0.9f),
            lore = "Иногда бьёт так, что слышно в Москве.",
            stats = new ItemStats { damage = 32, critChance = 0.2f, critMult = 1.75f } },
        new ItemDef { id = "vanguard", name = "Пуховик", category = ItemCategory.Upgrade, cost = 0,
            components = new[] { "tracksuit", "sausage" }, color = new Color(0.25f, 0.3f, 0.45f),
            lore = "В нём не больно. И не холодно.",
            stats = new ItemStats { health = 250, healthRegen = 5, blockChance = 0.5f, blockAmount = 15 } },
        new ItemDef { id = "hood", name = "Ушанка деда", category = ItemCategory.Upgrade, cost = 450,
            components = new[] { "ushanka", "sausage" }, color = new Color(0.55f, 0.4f, 0.3f),
            lore = "Прошла с дедом всё. Магия отскакивает.",
            stats = new ItemStats { magicResist = 0.25f, healthRegen = 6, armor = 2 } },
        new ItemDef { id = "shampur", name = "Шампур", category = ItemCategory.Upgrade, cost = 400,
            components = new[] { "bat", "sausage", "kefir" }, color = new Color(0.8f, 0.8f, 0.85f),
            lore = "Насаживает сразу несколько врагов.",
            stats = new ItemStats { damage = 30, healthRegen = 4.5f, manaRegen = 1.5f, cleave = 0.4f, cleaveRadius = 3.5f } },
        new ItemDef { id = "mangal", name = "Мангал", category = ItemCategory.Upgrade, cost = 1000,
            components = new[] { "bat", "caps_mail" }, color = new Color(1f, 0.45f, 0.1f),
            lore = "Носишь с собой — враги рядом поджариваются.",
            stats = new ItemStats { damage = 30, armor = 3, burnDps = 25, burnRadius = 6 } },
        new ItemDef { id = "bkb", name = "Чёрный пояс", category = ItemCategory.Upgrade, cost = 600,
            components = new[] { "tracksuit", "bat" }, color = new Color(0.08f, 0.08f, 0.08f),
            lore = "По карате. Магия не берёт.",
            stats = new ItemStats { str = 10, damage = 24, health = 100 },
            active = ItemActive.MagicImmunity, activeDuration = 6, activeCooldown = 70 },

        // ===== ВТОРОЙ УРОВЕНЬ СБОРКИ =====
        new ItemDef { id = "travel", name = "Сапоги-скороходы", category = ItemCategory.Upgrade, cost = 1500,
            components = new[] { "sneakers" }, color = new Color(1f, 0.8f, 0.3f),
            lore = "Из разных кроссовок — в сапоги, которые сами возят до бани.",
            stats = new ItemStats { moveSpeed = 0.75f, attackSpeed = 30, str = 12 },
            active = ItemActive.TeleportBase, activeDuration = 3, activeCooldown = 45 },
        new ItemDef { id = "crown", name = "Корона Курчатова", category = ItemCategory.Upgrade, cost = 1200,
            components = new[] { "crystal", "vanguard" }, color = new Color(1f, 0.85f, 0.15f),
            lore = "Носит только тот, кто за Курчатов реально убьёт.",
            stats = new ItemStats { damage = 45, health = 350, healthRegen = 7, critChance = 0.25f, critMult = 2.2f,
                                    blockChance = 0.6f, blockAmount = 25 } },
    };

    static Dictionary<string, ItemDef> byId;

    public static ItemDef Get(string id)
    {
        if (byId == null)
        {
            byId = new Dictionary<string, ItemDef>();
            foreach (var d in All) byId[d.id] = d;
        }
        return id != null && byId.TryGetValue(id, out var r) ? r : null;
    }

    public static int TotalCost(ItemDef d)
    {
        int c = d.cost;
        foreach (var comp in d.components) c += TotalCost(Get(comp));
        return c;
    }

    public static List<ItemDef> UsedIn(string id)
    {
        var list = new List<ItemDef>();
        foreach (var d in All)
            if (Array.IndexOf(d.components, id) >= 0) list.Add(d);
        return list;
    }

    public static List<ItemDef> InCategory(ItemCategory c) => All.FindAll(d => d.category == c);

    public static string CategoryName(ItemCategory c) => c switch
    {
        ItemCategory.Consumable => "РАСХОДНИКИ",
        ItemCategory.Attributes => "АТРИБУТЫ",
        ItemCategory.Equipment => "СНАРЯЖЕНИЕ",
        ItemCategory.Misc => "РАЗНОЕ",
        _ => "УЛУЧШЕНИЯ"
    };

    // Текст бонусов для подсказок и магазина.
    public static string DescribeStats(ItemDef d)
    {
        var s = d.stats;
        var sb = new StringBuilder();
        void L(float v, string text, string fmt = "0.##") { if (v != 0) sb.Append($"+{v.ToString(fmt)} {text}\n"); }
        L(s.str, "к силе"); L(s.agi, "к ловкости"); L(s.intel, "к интеллекту");
        L(s.damage, "к урону"); L(s.attackSpeed, "к скорости атаки"); L(s.armor, "к броне");
        L(s.health, "к здоровью"); L(s.mana, "к мане");
        L(s.healthRegen, "к регенерации здоровья"); L(s.manaRegen, "к регенерации маны");
        L(s.moveSpeed * 100f, "к скорости бега", "0");
        L(s.magicResist * 100f, "% к сопротивлению магии", "0");
        if (s.critChance > 0) sb.Append($"<color=#ff8080>Крит: {s.critChance * 100:0}% шанс ударить в {s.critMult:0.##} раза сильнее</color>\n");
        if (s.cleave > 0) sb.Append($"<color=#ffb080>Рассечение: {s.cleave * 100:0}% урона по врагам вокруг цели (ближний бой)</color>\n");
        if (s.blockChance > 0) sb.Append($"<color=#a0c0ff>Блок: {s.blockChance * 100:0}% шанс заблокировать {s.blockAmount:0} физ. урона</color>\n");
        if (s.burnDps > 0) sb.Append($"<color=#ffa040>Жар: {s.burnDps:0} урона в сек. всем врагам в радиусе {s.burnRadius:0}</color>\n");

        string act = d.active switch
        {
            ItemActive.HealOverTime => $"Съесть: +{d.activeValue * d.activeDuration:0} здоровья за {d.activeDuration:0} сек. Сбивается, если ударит герой или башня",
            ItemActive.ManaOverTime => $"Съесть: +{d.activeValue * d.activeDuration:0} маны за {d.activeDuration:0} сек.",
            ItemActive.TeleportBase => d.id == "train_ticket" ? $"Клавиша T: через {d.activeDuration:0} сек. телепорт к своей башне, трону или в баню" : $"Применить: через {d.activeDuration:0} сек. телепорт в свою пивную баню",
            ItemActive.SpeedBurst => $"Применить: +{d.activeValue:0}% к скорости бега на {d.activeDuration:0} сек.",
            ItemActive.MagicImmunity => $"Применить: иммунитет к магии на {d.activeDuration:0} сек.",
            _ => null
        };
        if (act != null)
        {
            sb.Append($"<color=#ffd84a>{act}</color>");
            if (d.activeCooldown > 0) sb.Append($" <color=#9db7ff>(перезарядка {d.activeCooldown:0} сек.)</color>");
            if (d.consumable) sb.Append(" <color=#aaaaaa>(одноразовый)</color>");
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }
}
