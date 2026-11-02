using UnityEngine;

// Способности Шашлык Ассасин (по мотивам Templar Assassin: Refraction → Маринад, Meld → Засада,
// Psi Blades → Длинный шампур, Psionic Trap → Охотничий капкан).

// Q — Маринад: щит на N ударов и +урон на N своих ударов
public class MarinadeAbility : Ability
{
    public MarinadeAbility(AbilityInfo info) : base(info) { }
    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx)
    {
        var kit = caster.GetComponent<ShashlikKit>();
        if (kit != null) kit.StartMarinade(Mathf.RoundToInt(V(0)), V(1), 17f);
    }
}

// W — Засада: невидимость, пока стоит; удар из засады — урон и минус броня
public class AmbushAbility : Ability
{
    public AmbushAbility(AbilityInfo info) : base(info) { }
    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx)
    {
        var kit = caster.GetComponent<ShashlikKit>();
        if (kit != null) kit.StartAmbush(V(0), V(1));
    }
}

// E — Длинный шампур: пассивка (дальность + удар насквозь), работает через ShashlikKit
public class LongSkewerAbility : Ability
{
    public LongSkewerAbility(AbilityInfo info) : base(info) { }
    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) { }
}

// R — Охотничий капкан: в точку, до N штук
public class HunterTrapAbility : Ability
{
    public HunterTrapAbility(AbilityInfo info) : base(info) { }
    public override bool IsValidTarget(Unit caster, Unit target) => true;
    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) =>
        CastAtPoint(caster, target != null ? target.transform.position : caster.transform.position, ctx);
    public override void CastAtPoint(Unit caster, Vector3 point, AbilityCaster ctx)
    {
        var kit = caster.GetComponent<ShashlikKit>();
        if (kit != null) kit.PlaceTrap(point, Mathf.RoundToInt(V(0)), V(1));
    }
}
