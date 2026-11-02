using UnityEngine;

// Способности Пивмавера (по мотивам паучихи: паучки → бутылочки, паутина → пивная пена).

// Q — Бутылочный выводок: бутылка во врага; если цель умрёт за 3 сек. — из неё вылезают бутылочки.
public class BottleBroodAbility : Ability
{
    public BottleBroodAbility(AbilityInfo info) : base(info) { }

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx)
    {
        var kit = caster.GetComponent<PivmaverKit>();
        // метка ставится сразу — если цель умрёт от самой бутылки, выводок тоже появится
        if (kit != null) kit.MarkTarget(target, 3.6f, Mathf.RoundToInt(V(1)), V(2), V(3));
        Projectile.Spawn(caster.transform.position + Vector3.up * 2.2f, target, caster, V(0), 20f,
            ProjectileShape.Ball, PivmaverKit.Amber, 0.55f, DamageType.Magical);
        if (kit != null) kit.Say(0.4f);
    }
}

// W — Пивная пена: лужа пены в точке, в ней Пивмавер и бутылочки быстрее и лечатся.
public class BeerFoamAbility : Ability
{
    public BeerFoamAbility(AbilityInfo info) : base(info) { }

    public override bool IsValidTarget(Unit caster, Unit target) => true;

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) =>
        CastAtPoint(caster, target != null ? target.transform.position : caster.transform.position, ctx);

    public override void CastAtPoint(Unit caster, Vector3 point, AbilityCaster ctx)
    {
        var kit = caster.GetComponent<PivmaverKit>();
        if (kit == null) return;
        kit.AddFoam(point, Info.radius, V(0), V(1), V(2));
        ParticleBurst.Spawn(point + Vector3.up * 0.5f, PivmaverKit.Foam, 20, 5f);
    }
}

// E — Похмельный укус: пассивка, работает через PivmaverKit (замедление и доп. урон с каждого удара).
public class HangoverBiteAbility : Ability
{
    public HangoverBiteAbility(AbilityInfo info) : base(info) { }
    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) { }
}

// R — Пивной голод: +урон и вампиризм на время.
public class BeerHungerAbility : Ability
{
    public BeerHungerAbility(AbilityInfo info) : base(info) { }

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx)
    {
        var kit = caster.GetComponent<PivmaverKit>();
        if (kit == null) return;
        kit.StartHunger(V(0), V(1) / 100f, V(2));
        kit.Say(1f, true);
    }
}
