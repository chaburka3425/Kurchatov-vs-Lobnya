using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Способности Макса Gold.

static class MaxFx
{
    static Material lineMat;
    public static Material LineMat => lineMat != null ? lineMat : (lineMat = new Material(Shader.Find("Sprites/Default")));

    public static LineRenderer Line(string name, Color color, float width, bool loop)
    {
        var lr = new GameObject(name).AddComponent<LineRenderer>();
        lr.material = LineMat;
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        lr.loop = loop;
        lr.useWorldSpace = true;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }

    public static void Circle(LineRenderer lr, Vector3 c, float r, int n = 40)
    {
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
        }
    }

    public static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
}

// Q — Золотой дождь: через 1.2 сек. на круг льётся золото — урон в секунду и замедление.
public class GoldRainAbility : Ability
{
    public GoldRainAbility(AbilityInfo info) : base(info) { }

    public override bool IsValidTarget(Unit caster, Unit target) => true;

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) =>
        CastAtPoint(caster, target != null ? target.transform.position : caster.transform.position, ctx);

    public override void CastAtPoint(Unit caster, Vector3 point, AbilityCaster ctx)
    {
        point.y = 0f;
        ctx.StartCoroutine(Rain(caster, point, Info.radius, V(0), V(1), V(2)));
        VoiceLines.Speak(MaxGoldKit.HeroId, caster.transform.position, 1f, false, -1, MaxGoldKit.DeathLineIndex);
    }

    IEnumerator Rain(Unit caster, Vector3 c, float r, float dps, float slow, float dur)
    {
        const float delay = 1.2f;
        Team team = caster.team;
        FogOfWar.AddVision(team, c, r + 1f, delay + dur);

        var ring = MaxFx.Line("GoldRainRing", new Color(1f, 0.82f, 0.3f, 0.9f), 0.12f, true);
        MaxFx.Circle(ring, c + Vector3.up * 0.12f, r);
        var disc = RuntimeMats.Prim(PrimitiveType.Cylinder, null, c + Vector3.up * 0.03f, new Vector3(0.1f, 0.01f, 0.1f),
            RuntimeMats.Glow(new Color(0.55f, 0.4f, 0.1f)));
        var drops = new List<Transform>();
        var dropMat = RuntimeMats.Glow(new Color(1f, 0.85f, 0.35f));

        float t0 = Time.time;
        while (Time.time - t0 < delay)
        {
            float k = (Time.time - t0) / delay;
            disc.transform.localScale = new Vector3(r * 2f * k, 0.01f, r * 2f * k);
            yield return null;
        }
        disc.transform.localScale = new Vector3(r * 2f, 0.01f, r * 2f);

        float end = Time.time + dur, tick = 0f, spawn = 0f;
        while (Time.time < end)
        {
            // золото падает с неба
            spawn += Time.deltaTime * 40f;
            while (spawn >= 1f)
            {
                spawn -= 1f;
                Vector2 p = Random.insideUnitCircle * r * 0.95f;
                var d = RuntimeMats.Prim(PrimitiveType.Capsule, null, c + new Vector3(p.x + 0.8f, 7f, p.y), new Vector3(0.07f, 0.22f, 0.07f), dropMat);
                d.transform.rotation = Quaternion.Euler(0, 0, 8f);
                drops.Add(d.transform);
            }
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                d.position += new Vector3(-0.12f, -1f, 0) * 22f * Time.deltaTime;
                if (d.position.y < 0.15f) { Object.Destroy(d.gameObject); drops.RemoveAt(i); }
            }

            tick += Time.deltaTime;
            while (tick >= 0.5f)
            {
                tick -= 0.5f;
                if (caster == null) break;
                foreach (var u in new List<Unit>(Unit.All))
                {
                    if (!u.IsAlive || u.team == team || u.kind == UnitKind.Structure) continue;
                    if (u.DistanceFrom(c) > r) continue;
                    u.Health.TakeDamage(new DamageInfo { amount = dps * 0.5f, type = DamageType.Magical, source = caster });
                    var st = u.GetComponent<StatusEffects>();
                    if (st != null) st.ApplySlow(slow, 0.6f, "Золотой дождь", "gold_rain");
                }
            }
            yield return null;
        }

        foreach (var d in drops) if (d != null) Object.Destroy(d.gameObject);
        Object.Destroy(disc);
        Object.Destroy(ring.gameObject);
    }
}

// W — Голдшот: пассивка, работает через MaxGoldKit (шанс выстрела в голову).
public class GoldShotAbility : Ability
{
    public GoldShotAbility(AbilityInfo info) : base(info) { }
    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) { }
}

// E — Лестница Габилджона: пассивно +дальность, по нажатию — залезает на лестницу.
public class GabilLadderAbility : Ability
{
    public GabilLadderAbility(AbilityInfo info) : base(info) { }

    public override bool IsValidTarget(Unit caster, Unit target) => true;

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx)
    {
        var kit = caster.GetComponent<MaxGoldKit>();
        if (kit != null) kit.StartLadder(V(2));
        FloatingText.Show(caster.transform.position + Vector3.up * 5f, "НА ЛЕСТНИЦУ!", MaxFx.Gold, 26);
        ParticleBurst.Spawn(caster.transform.position + Vector3.up * 0.8f, MaxFx.Gold, 16, 4f);
        VoiceLines.Speak(MaxGoldKit.HeroId, caster.transform.position, 1f, false, -1, MaxGoldKit.DeathLineIndex);
    }
}

// R — Хантерас пик: 2 сек. прицеливания, потом пуля прямо в голову.
public class HuntersPeakAbility : Ability
{
    public HuntersPeakAbility(AbilityInfo info) : base(info) { }

    public override bool IsValidTarget(Unit caster, Unit target) =>
        base.IsValidTarget(caster, target) && FogOfWar.CanSee(caster.team, target);

    public override void Cast(Unit caster, Unit target, int times, AbilityCaster ctx) =>
        ctx.StartCoroutine(Aim(caster, target));

    static float HeadHeight(Unit u) => u.kind == UnitKind.Hero ? 3.3f : 1.7f;

    IEnumerator Aim(Unit caster, Unit target)
    {
        var hc = caster.GetComponent<HeroController>();
        var agent = caster.GetComponent<NavMeshAgent>();
        var status = caster.GetComponent<StatusEffects>();
        var attack = caster.GetComponent<AutoAttack>();
        var kit = caster.GetComponent<MaxGoldKit>();

        if (hc != null) hc.IsBusyChanneling = true;
        if (attack != null) attack.ClearTarget();
        if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
        FloatingText.Show(caster.transform.position + Vector3.up * 4.8f, "ХАНТЕРАС ПИК...", new Color(1f, 0.35f, 0.25f), 28);
        VoiceLines.Speak(MaxGoldKit.HeroId, caster.transform.position, 1f, true, -1, MaxGoldKit.DeathLineIndex);
        if (HeroController.Player != null && target.gameObject == HeroController.Player.gameObject)
            FloatingText.Show(target.transform.position + Vector3.up * 4.4f, "Макс Gold целится в тебя!", new Color(1f, 0.45f, 0.4f), 26);

        var laser = MaxFx.Line("HuntersLaser", new Color(1f, 0.15f, 0.1f, 0.6f), 0.05f, false);
        laser.positionCount = 2;
        var ring = MaxFx.Line("HuntersRing", new Color(1f, 0.2f, 0.12f, 1f), 0.09f, true);
        var crossA = MaxFx.Line("HuntersCrossA", new Color(1f, 0.2f, 0.12f, 1f), 0.07f, false);
        var crossB = MaxFx.Line("HuntersCrossB", new Color(1f, 0.2f, 0.12f, 1f), 0.07f, false);
        crossA.positionCount = crossB.positionCount = 2;

        float aimTime = V(1), end = Time.time + aimTime;
        bool broken = false;
        while (Time.time < end)
        {
            if (!caster.IsAlive || (status != null && status.IsStunned) || target == null || !target.IsAlive) { broken = true; break; }
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            Vector3 dir = target.transform.position - caster.transform.position; dir.y = 0;
            if (dir.sqrMagnitude > 0.01f) caster.transform.rotation = Quaternion.LookRotation(dir);
            FogOfWar.Reveal(caster.team, target, 0.3f);

            float k = 1f - (end - Time.time) / aimTime;
            Vector3 head = target.transform.position + Vector3.up * HeadHeight(target);
            Vector3 muzzle = Muzzle(caster, kit);
            laser.SetPosition(0, muzzle);
            laser.SetPosition(1, head);
            laser.enabled = caster.team == MatchSetup.PlayerTeam || FogOfWar.VisibleToPlayer(caster);
            laser.startColor = laser.endColor = new Color(1f, 0.15f, 0.1f, 0.3f + 0.5f * k);
            float rr = Mathf.Lerp(1.6f, 0.55f, k);
            MaxFx.Circle(ring, head + Vector3.up * 0.4f, rr);
            crossA.SetPosition(0, head + new Vector3(-rr * 1.5f, 0.4f, 0)); crossA.SetPosition(1, head + new Vector3(rr * 1.5f, 0.4f, 0));
            crossB.SetPosition(0, head + new Vector3(0, 0.4f, -rr * 1.5f)); crossB.SetPosition(1, head + new Vector3(0, 0.4f, rr * 1.5f));
            yield return null;
        }

        Object.Destroy(laser.gameObject); Object.Destroy(ring.gameObject); Object.Destroy(crossA.gameObject); Object.Destroy(crossB.gameObject);
        if (hc != null) hc.IsBusyChanneling = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        if (broken)
        {
            if (caster.IsAlive) FloatingText.Show(caster.transform.position + Vector3.up * 4f, "Прицел сбит", new Color(1f, 0.6f, 0.6f), 22);
            yield break;
        }

        float dmg = V(0);
        Vector3 from = Muzzle(caster, kit);
        Projectile.Spawn(from, target, caster, dmg, 70f, ProjectileShape.Bullet, new Color(1f, 0.92f, 0.55f), 0.7f, DamageType.Magical, t =>
        {
            var st = t.GetComponent<StatusEffects>();
            if (st != null) st.ApplyStun(0.3f, "Хантерас пик", "hunters_peak");
            FloatingText.Show(t.transform.position + Vector3.up * 4.6f, "ХЕДШОТ!", MaxFx.Gold, 34);
            ParticleBurst.Spawn(t.transform.position + Vector3.up * HeadHeight(t), new Color(1f, 0.92f, 0.55f), 26, 6f);
        });
        ParticleBurst.Spawn(from, new Color(1f, 0.92f, 0.55f), 18, 5f);
    }

    static Vector3 Muzzle(Unit caster, MaxGoldKit kit) =>
        caster.transform.position + caster.transform.forward * 2.5f + Vector3.up * (1.6f + (kit != null && kit.OnLadder ? MaxGoldKit.LadderHeight : 0f));
}
