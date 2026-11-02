using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Способности Огр Mog (по мотивам двухголового огра-мага).

// Q — Горячая пицца: урон + оглушение. Мультикаст — пицца летит повторно в ту же цель.
public class HotPizzaAbility : Ability
{
    public HotPizzaAbility(AbilityInfo info) : base(info) { }

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) =>
        ctx.StartCoroutine(Throw(caster, target, times));

    IEnumerator Throw(Unit caster, Unit target, int times)
    {
        for (int i = 0; i < times; i++)
        {
            if (target == null || !target.IsAlive) yield break;
            float stun = V(1);
            Projectile.Spawn(caster.transform.position + Vector3.up * 2.4f, target, caster, V(0), 26f,
                ProjectileShape.Disc, new Color(1f, 0.6f, 0.15f), 1.1f, DamageType.Magical,
                t => { var st = t.GetComponent<StatusEffects>(); if (st != null) st.ApplyStun(stun, "Горячая пицца", "hot_pizza"); });
            yield return new WaitForSeconds(0.4f);
        }
    }
}

// W — Острый соус: горение + замедление. Мультикаст — соус летит ещё в соседних врагов.
public class SpicySauceAbility : Ability
{
    public SpicySauceAbility(AbilityInfo info) : base(info) { }

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx)
    {
        var targets = new List<Unit> { target };
        targets.AddRange(AbilityCaster.FindNearby(target.transform.position, 7f, times - 1,
            u => IsValidTarget(caster, u) && !targets.Contains(u)));

        float dps = V(0), duration = V(1), slow = V(2);
        foreach (var t in targets)
        {
            Projectile.Spawn(caster.transform.position + Vector3.up * 2.4f, t, caster, 0f, 18f,
                ProjectileShape.Ball, new Color(0.9f, 0.1f, 0.05f), 0.6f, DamageType.Magical,
                hit =>
                {
                    var st = hit.GetComponent<StatusEffects>();
                    if (st == null) return;
                    st.AddDot(dps, duration, caster, DamageType.Magical, "Острый соус", "spicy_sauce");
                    st.ApplySlow(slow, duration, "Острый соус", "spicy_sauce");
                });
        }
    }
}

// E — Энергетик: скорость атаки и бега союзнику. Мультикаст — достаётся соседним союзникам.
public class EnergyDrinkAbility : Ability
{
    public EnergyDrinkAbility(AbilityInfo info) : base(info) { }

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx)
    {
        var targets = new List<Unit> { target };
        targets.AddRange(AbilityCaster.FindNearby(caster.transform.position, 7f, times - 1,
            u => IsValidTarget(caster, u) && !targets.Contains(u)));

        foreach (var t in targets)
        {
            var st = t.GetComponent<StatusEffects>();
            if (st == null) continue;
            st.AddAttackSpeed(V(0), V(2), "Энергетик", "energy_drink");
            st.AddMoveSpeed(V(1), V(2), "Энергетик", "energy_drink");
            FloatingText.Show(t.transform.position + Vector3.up * 3f, "Энергетик!", new Color(0.4f, 1f, 0.4f), 24);
        }
    }
}

// R — Двойная порция: пассивный мультикаст.
public class DoublePortionAbility : Ability
{
    public DoublePortionAbility(AbilityInfo info) : base(info) { }

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) { }

    // Сколько раз сработает способность: 1, 2, 3 или 4.
    public int Roll()
    {
        float r = Random.value * 100f;
        if (r < V(2)) return 4;
        if (r < V(2) + V(1)) return 3;
        if (r < V(2) + V(1) + V(0)) return 2;
        return 1;
    }
}
