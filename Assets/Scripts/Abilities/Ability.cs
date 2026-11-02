using UnityEngine;

// Базовый класс способности. Цифры берутся из AbilityInfo (HeroCatalog) по текущему уровню.
public abstract class Ability
{
    public AbilityInfo Info { get; }
    public float CooldownLeft;
    public int Level;            // 0 — не изучена
    public int Charges;          // заряды (если у способности они есть)
    public float ChargeTimer;    // сколько осталось до нового заряда

    protected Ability(AbilityInfo info) { Info = info; Charges = info.charges; }

    public AbilityTarget Target => Info.target;
    public bool IsPassive => Info.target == AbilityTarget.Passive;
    public bool IsLearned => Level > 0;
    public int MaxLevel => Info.maxLevel;
    public float ManaCost => Info.ManaCost(Mathf.Max(1, Level));
    public float Cooldown => Info.Cooldown(Mathf.Max(1, Level));
    public float CastRange => Info.castRange;
    protected float V(int i) => Info.Value(i, Mathf.Max(1, Level));

    public virtual bool IsValidTarget(Unit caster, Unit target)
    {
        if (target == null || !target.IsAlive || target.kind == UnitKind.Structure) return false;
        return Target switch
        {
            AbilityTarget.EnemyUnit => caster.IsEnemyOf(target),
            AbilityTarget.AllyUnit => !caster.IsEnemyOf(target),
            _ => true
        };
    }

    // times — сколько раз сработала (мультикаст), обычно 1.
    public abstract void Cast(Unit caster, Unit target, int times, AbilityCaster ctx);

    // Для способностей «в точку» (Золотой дождь)
    public virtual void CastAtPoint(Unit caster, Vector3 point, AbilityCaster ctx) { }
}

public static class AbilityFactory
{
    public static Ability Create(AbilityInfo info) => info.id switch
    {
        "hot_pizza" => new HotPizzaAbility(info),
        "spicy_sauce" => new SpicySauceAbility(info),
        "energy_drink" => new EnergyDrinkAbility(info),
        "double_portion" => new DoublePortionAbility(info),
        "sausage_coil_near" or "sausage_coil_mid" or "sausage_coil_far" => new SausageCoilAbility(info),
        "sausage_requiem" => new SausageRequiemAbility(info),
        "gold_rain" => new GoldRainAbility(info),
        "gold_shot" => new GoldShotAbility(info),
        "gabil_ladder" => new GabilLadderAbility(info),
        "hunters_peak" => new HuntersPeakAbility(info),
        "pivo_bottles" => new BottleBroodAbility(info),
        "pivo_foam" => new BeerFoamAbility(info),
        "pivo_bite" => new HangoverBiteAbility(info),
        "pivo_hunger" => new BeerHungerAbility(info),
        "shash_marinade" => new MarinadeAbility(info),
        "shash_ambush" => new AmbushAbility(info),
        "shash_skewer" => new LongSkewerAbility(info),
        "shash_trap" => new HunterTrapAbility(info),
        _ => null
    };
}
