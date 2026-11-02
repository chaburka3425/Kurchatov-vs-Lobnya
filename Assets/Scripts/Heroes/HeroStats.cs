using UnityEngine;

// Переводит атрибуты героя (сила/ловкость/интеллект) и предметы в здоровье, ману, броню и урон.
[RequireComponent(typeof(Unit))]
public class HeroStats : MonoBehaviour
{
    public string heroId = "ogr_mog";
    public int level = 1;

    public HeroDefinition Definition { get; private set; }

    // Итоговые атрибуты (с предметами)
    public float Str { get; private set; }
    public float Agi { get; private set; }
    public float Int { get; private set; }
    public ItemStats Items { get; private set; }

    Inventory inventory;

    // Доп. урон (сосиски Сани и т.п.)
    public float BonusDamage;

    public void SetHero(string id)
    {
        heroId = id;
        Definition = HeroCatalog.Get(id);
        Recalculate(true);
    }

    void Awake()
    {
        Definition = HeroCatalog.Get(heroId);
        inventory = GetComponent<Inventory>();
        if (inventory != null) inventory.OnChanged += () => Recalculate(false);
    }

    void Start() => Recalculate(true);

    public void Recalculate(bool refill)
    {
        var d = Definition;
        int l = level;
        var it = inventory != null ? inventory.TotalStats() : new ItemStats();
        Items = it;

        Str = d.StrAt(l) + it.str;
        Agi = d.AgiAt(l) + it.agi;
        Int = d.IntAt(l) + it.intel;

        var health = GetComponent<Health>();
        if (health != null)
        {
            float maxHp = 120f + 22f * Str + it.health;
            if (refill) health.SetMax(maxHp, true); else health.SetMaxKeepRatio(maxHp);
            health.regenPerSecond = 1f + 0.1f * Str + it.healthRegen;
            health.armor = d.baseArmor + Agi / 6f + it.armor;
            health.magicResist = 1f - (1f - 0.25f) * (1f - it.magicResist);
            health.blockChance = it.blockChance;
            health.blockAmount = it.blockAmount;
        }

        var mana = GetComponent<Mana>();
        if (mana != null)
        {
            float maxMp = 75f + 12f * Int + it.mana;
            if (refill) mana.SetMax(maxMp, true); else mana.SetMaxKeepRatio(maxMp);
            mana.regenPerSecond = 0.5f + 0.05f * Int + it.manaRegen;
        }

        var attack = GetComponent<AutoAttack>();
        if (attack != null)
        {
            float primary = d.attribute switch
            {
                HeroAttribute.Strength => Str,
                HeroAttribute.Agility => Agi,
                HeroAttribute.Intelligence => Int,
                _ => 0.7f * (Str + Agi + Int)
            };
            attack.damage = d.baseDamage + primary + it.damage + BonusDamage;
            attack.ranged = d.ranged;
            if (d.ranged) attack.projectileColor = d.projectileColor;
            attack.attackSpeed = 100f + Agi + it.attackSpeed;
            attack.range = d.attackRange;
            var kit = GetComponent<MaxGoldKit>();
            if (kit != null) attack.range += kit.RangeBonus;
            var shash = GetComponent<ShashlikKit>();
            if (shash != null) attack.range += shash.RangeBonus;
            if (d.projectileSpeed > 0f) attack.projectileSpeed = d.projectileSpeed;
            attack.bullet = d.bulletProjectile;
            if (d.ranged) { attack.acquireRange = Mathf.Max(7f, attack.range + 0.5f); attack.loseRange = Mathf.Max(12f, attack.range + 4f); }
            attack.baseAttackTime = d.baseAttackTime;
            attack.critChance = it.critChance;
            attack.critMultiplier = it.critMult;
            attack.cleave = it.cleave;
            attack.cleaveRadius = it.cleaveRadius;
        }

        var status = GetComponent<StatusEffects>();
        if (status != null) status.SetBaseMoveSpeed(d.moveSpeed + it.moveSpeed);
    }
}
