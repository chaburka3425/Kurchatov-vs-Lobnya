using System;
using UnityEngine;

public enum DamageType { Physical, Magical, Pure }

public struct DamageInfo
{
    public float amount;
    public DamageType type;
    public Unit source; // кто нанёс урон
    public bool crit;   // критический удар (для цифр урона)
}

// Здоровье, броня и смерть. Весь урон в игре идёт через TakeDamage.
[RequireComponent(typeof(Unit))]
public class Health : MonoBehaviour
{
    public float maxHealth = 500f;
    public float armor = 2f;
    [Range(0f, 1f)] public float magicResist = 0.25f;
    public float regenPerSecond = 1f;
    public bool destroyOnDeath = false;
    [Tooltip("Неуязвим — урон не проходит (например, пока стоит башня перед ним)")]
    public bool invulnerable = false;

    [Header("Блок урона (от предметов)")]
    public float blockChance = 0f;
    public float blockAmount = 0f;

    public float Current { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<DamageInfo> OnDamaged;
    public event Action<Unit> OnDeath; // параметр — убийца
    public static event Action<Health, Unit> AnyDeath; // любая смерть на карте (для опыта/золота)
    public static event Action<Health, DamageInfo, float> AnyDamaged; // для цифр урона

    StatusEffects status;

    // Щиты, которые целиком гасят удар (вернуть true — урона не будет)
    public Func<DamageInfo, bool> BlockHook;

    void Awake()
    {
        Current = maxHealth;
        status = GetComponent<StatusEffects>();
    }

    void Update()
    {
        if (!IsDead && Current < maxHealth)
            Current = Mathf.Min(maxHealth, Current + regenPerSecond * Time.deltaTime);
    }

    // Классическая MOBA-формула: 5 брони ≈ -23% урона.
    public static float ArmorMultiplier(float armor) =>
        1f - (0.06f * armor) / (1f + 0.06f * Mathf.Abs(armor));

    Unit selfUnit;

    // Клиент сети: здоровье приходит от хоста
    public void NetSet(float cur, float max, bool dead) { Current = cur; maxHealth = max; IsDead = dead; }

    public void TakeDamage(DamageInfo info)
    {
        if (IsDead || invulnerable || Net.IsClient) return;
        if (selfUnit == null) selfUnit = GetComponent<Unit>();
        if (Glyph.Protects(selfUnit)) return;   // глиф: строения и крипы не получают урон

        if (status == null) status = GetComponent<StatusEffects>();
        if (info.type == DamageType.Magical && status != null && status.IsMagicImmune) return;
        if (BlockHook != null && BlockHook(info)) return;   // щит (Маринад) съел удар

        float dmg = info.amount;
        if (info.type == DamageType.Physical && blockChance > 0f && UnityEngine.Random.value < blockChance)
            dmg = Mathf.Max(0f, dmg - blockAmount);

        switch (info.type)
        {
            case DamageType.Physical: dmg *= ArmorMultiplier(armor - (status != null ? status.ArmorReduction : 0f)); break;
            case DamageType.Magical: dmg *= 1f - magicResist; break;
        }

        Current -= dmg;
        if (dmg > 0f && info.source != null && info.source != selfUnit && (info.source.kind == UnitKind.Hero || info.source.kind == UnitKind.Structure)
            && status != null && status.BreakHeal())
            FloatingText.Show(transform.position + Vector3.up * 3.6f, "Шаурму выбили!", new Color(1f, 0.6f, 0.5f), 20);
        OnDamaged?.Invoke(info);
        AnyDamaged?.Invoke(this, info, dmg);

        if (Current <= 0f)
        {
            Current = 0f;
            IsDead = true;
            OnDeath?.Invoke(info.source);
            AnyDeath?.Invoke(this, info.source);
            if (destroyOnDeath) Destroy(gameObject, 0.1f);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
    }

    // Новый максимум, процент здоровья сохраняется (при повышении уровня).
    public void SetMaxKeepRatio(float max)
    {
        float ratio = maxHealth > 0f ? Current / maxHealth : 1f;
        maxHealth = max;
        if (!IsDead) Current = max * ratio;
    }

    public void SetMax(float max, bool refill)
    {
        maxHealth = max;
        Current = refill ? max : Mathf.Min(Current, max);
    }

    public void Revive()
    {
        Current = maxHealth;
        IsDead = false;
    }
}
