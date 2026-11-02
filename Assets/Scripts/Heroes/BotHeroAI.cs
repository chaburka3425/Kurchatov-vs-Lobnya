using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Вражеский бот-герой: стоит на линии за своими крипами, фармит, дерётся с игроком,
// отступает лечиться в баню, сам качает способности, покупает предметы и включает глиф.
// Сложность — в настройках главного меню (PlayerPrefs "mpka_diff": 0 лёгкий, 1 обычный, 2 сложный).
[RequireComponent(typeof(Unit))]
public class BotHeroAI : MonoBehaviour
{
    public const string DiffKey = "mpka_diff";
    // Роли и закупы: керри фармит и бьёт, мид и саппорт ходят ганкать, оффлейн танкует и начинает драки
    public enum Role { Carry, Mid, Offlane, Support, Jungler }
    public Role role = Role.Carry;
    static readonly Dictionary<Role, string[]> Builds = new Dictionary<Role, string[]>
    {
        { Role.Carry,   new[] { "band_agi", "sneakers", "crystal", "bkb", "crown", "mangal" } },
        { Role.Mid,     new[] { "kvass_bottle", "band_str", "phase", "blink_scooter", "bkb", "crystal", "crown" } },
        { Role.Offlane, new[] { "band_str", "phase", "vanguard", "blink_scooter", "bkb", "mangal" } },
        { Role.Support, new[] { "kvass_bottle", "pepper_dust", "band_int", "phase", "blink_scooter", "hood", "bkb" } },
        { Role.Jungler, new[] { "band_str", "sneakers", "vanguard", "bkb", "crystal", "crown" } },
    };
    string[] Build => Builds[role];

    // Общий план команды: в какую линию идём пушить толпой
    class TeamPlan { public LaneData.Lane lane; public float until; }
    static readonly Dictionary<Team, TeamPlan> plans = new Dictionary<Team, TeamPlan>();

    Unit gankTarget;
    float gankUntil, gankCheck, bottleAt, dustAt, planCheck;
    LaneData.Lane homeLane = LaneData.Lane.Mid;

    Unit unit;
    Health health;
    Mana mana;
    NavMeshAgent agent;
    AutoAttack attack;
    AbilityCaster caster;
    HeroProgress progress;
    Inventory inv;
    ItemEffects items;
    StatusEffects status;
    HeroController hc;
    SausageStash stash;
    BeerBath bath;
    Transform ownThrone;

    int laneChoice = 1;          // 0 верх, 1 мид, 2 низ, 3 лес
    LaneData.Lane lane = LaneData.Lane.Mid;
    bool retreat;
    float think, react, shopAt, laneCheck, farmAt, glyphAt;
    readonly HashSet<string> built = new HashSet<string>();
    int diff = 1;

    bool IsSanya => GetComponent<SausageStash>() != null;
    bool IsMax => GetComponent<MaxGoldKit>() != null;
    PivmaverKit Piv => GetComponent<PivmaverKit>();
    float snipeAt;

    public void SetLane(int choice)
    {
        laneChoice = choice;
        lane = choice == 0 ? LaneData.Lane.Top : choice == 2 ? LaneData.Lane.Bot : LaneData.Lane.Mid;
        homeLane = lane;
        // роль по линии: лес — лесник, мид — мидер, на боковых — керри / саппорт / оффлейнер
        if (choice == 3) role = Role.Jungler;
        else if (choice == 1) role = Role.Mid;
        else
        {
            int k = 0;
            foreach (var b in FindObjectsByType<BotHeroAI>(FindObjectsSortMode.None))
                if (b != this && b.unit != null && unit != null && b.unit.team == unit.team && (b.laneChoice == 0 || b.laneChoice == 2)) k++;
            role = k == 0 ? Role.Carry : k == 1 ? Role.Support : Role.Offlane;
        }
    }

    void Awake()
    {
        unit = GetComponent<Unit>();
        health = GetComponent<Health>();
        mana = GetComponent<Mana>();
        agent = GetComponent<NavMeshAgent>();
        attack = GetComponent<AutoAttack>();
        caster = GetComponent<AbilityCaster>();
        progress = GetComponent<HeroProgress>();
        inv = GetComponent<Inventory>();
        items = GetComponent<ItemEffects>();
        status = GetComponent<StatusEffects>();
        hc = GetComponent<HeroController>();
        stash = GetComponent<SausageStash>();
        diff = PlayerPrefs.GetInt(DiffKey, 1);
        if (hc != null) hc.OnHeroDied += _ => retreat = false;
    }

    void Start()
    {
        foreach (var b in FindObjectsByType<BeerBath>(FindObjectsSortMode.None)) if (b.team == unit.team) bath = b;
        foreach (var t in FindObjectsByType<Throne>(FindObjectsSortMode.None))
            if (t.GetComponent<Unit>().team == unit.team) ownThrone = t.transform;
    }

    float HpR => health.Current / Mathf.Max(1f, health.maxHealth);

    void Update()
    {
        LearnSkills();
        UseGlyphIfNeeded();
        if (!unit.IsAlive || MatchSetup.InLobby) return;
        if (IsMax && Time.time >= snipeAt) { snipeAt = Time.time + 0.7f; if (MaxSnipe()) return; }
        if ((status != null && status.IsStunned) || (hc != null && hc.IsBusyChanneling)) return;
        if (Time.time < think) return;
        think = Time.time + 0.2f;

        bool atBath = bath != null && Flat(transform.position, bath.transform.position) <= bath.radius + 0.5f;
        if (atBath) Shop();

        // Отступление
        if (!retreat && HpR < 0.28f)
        {
            retreat = true;
            attack.ClearTarget();
            UseItem(ItemActive.HealOverTime);
        }
        if (retreat)
        {
            if (atBath) { if (HpR > 0.95f && (mana == null || mana.Current > mana.maxMana * 0.7f)) retreat = false; else { Stop(); return; } }
            else
            {
                if (status != null && (status.IsBurning || status.IsSlowed)) UseItem(ItemActive.MagicImmunity);
                if (items != null && items.IsTeleporting) return;
                if (items != null && items.CanTp && bath != null && Flat(transform.position, bath.transform.position) > 30f && HpR > 0.12f)
                {
                    Stop();
                    items.TicketTo(bath.transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f)), "Еду в баню...");
                    return;
                }
                MoveTo(bath != null ? bath.transform.position : transform.position);
                return;
            }
        }

        // Враг-герой рядом
        Unit foe = null; float foeD = 99f;
        int allyHeroes = 1, foeHeroes = 0;
        foreach (var u in Unit.All)
        {
            if (u == unit || u.kind != UnitKind.Hero || !u.IsAlive || u.team == Team.Neutral) continue;
            float d = Flat(u.transform.position, transform.position);
            if (d > 12f) continue;
            if (!unit.IsEnemyOf(u)) { allyHeroes++; continue; }
            if (!FogOfWar.CanSee(unit.team, u)) continue;
            foeHeroes++;
            if (d < 10f && (foe == null || u.Health.Current < foe.Health.Current || d < foeD - 3f)) { foe = u; foeD = d; }
        }

        // Вдали от бани покупаем в тайник и зовём курьера; без курьера — идём в баню сами
        if (!atBath) RemoteShop();
        var cour = Courier.Of(gameObject);
        bool courierOk = cour != null && !cour.IsDead;
        if (foe == null && !courierOk && progress.Gold >= 1100 && !atBath && NextBuyAffordable()) { retreat = true; attack.ClearTarget(); return; }

        if (foe != null)
        {
            float fR = foe.Health.Current / Mathf.Max(1f, foe.Health.maxHealth);
            float brave = diff == 0 ? -0.15f : diff == 2 ? 0.2f : 0f;
            brave += (allyHeroes - foeHeroes) * 0.15f;
            bool want = (fR < HpR + brave || foe.Health.Current < 250f) && HpR > 0.45f && (EnemyTowerAt(foe.transform.position) == null || foe.Health.Current < 150f);
            // Первые 40 секунд (пока крипы не дошли) сами в драку не лезем — только отвечаем, если бьют нас или союзника рядом
            if (want && MatchSetup.MatchTime < 40f)
            {
                var ft = foe.Attack != null ? foe.Attack.Target : null;
                bool provoked = ft != null && ft.kind == UnitKind.Hero && !unit.IsEnemyOf(ft) && Flat(ft.transform.position, transform.position) < 8f;
                if (!provoked) want = false;
            }
            if (want)
            {
                gankTarget = null;
                if (HpR < 0.6f) UseItem(ItemActive.MagicImmunity);
                // самокат-блинк: запрыгнуть на врага
                if (items != null && foeD > 5f && foeD < 12.5f && HpR > 0.6f && items.BlinkReady)
                {
                    Vector3 to = foe.transform.position + (transform.position - foe.transform.position).normalized * 1.5f;
                    if (items.BotBlink(to)) { CastAt(foe, true); attack.SetTarget(foe); return; }
                }
                if (Time.time >= react)
                {
                    react = Time.time + (diff == 0 ? 1.6f : diff == 2 ? 0.5f : 0.9f);
                    float skill = diff == 0 ? 0.35f : diff == 2 ? 0.95f : 0.65f;
                    if (Random.value < skill && CastAt(foe, true)) return;
                }
                attack.SetTarget(foe);
                return;
            }
            // драться невыгодно: врагов больше или мало здоровья — отходим под свою башню
            if (foeHeroes >= allyHeroes + 2 || (HpR < 0.45f && foeHeroes > 0))
            {
                gankTarget = null;
                var safe = OwnTowerBehind();
                if (Flat(safe, transform.position) > 2f) { if (HpR < 0.7f) TryBottle(); MoveTo(safe); return; }
            }
        }

        UseSmartItems(foe);

        // Руна рядом и никого нет — бежим взять
        if (foe == null)
        {
            var rune = RuneManager.Nearest(transform.position, role == Role.Mid || role == Role.Support ? 40f : 22f);
            if (rune != null && HpR > 0.35f) { MoveTo(rune.pos); return; }
        }

        // Ганг: идём к одинокому или раненому врагу
        if (foe == null && UpdateGank()) return;

        // Общий пуш командой после 10-й минуты
        UpdateTeamPush();

        // Линия или лес
        if (laneChoice != 3 && Time.time >= laneCheck)
        {
            laneCheck = Time.time + 5f;
            if (!EnemyTowersAlive(lane))
            {
                var rest = new List<LaneData.Lane>();
                foreach (var l in LaneData.All) if (EnemyTowersAlive(l)) rest.Add(l);
                if (rest.Count > 0) lane = rest[Random.Range(0, rest.Count)];
            }
        }

        if (laneChoice == 3 && HpR > 0.45f)
        {
            Unit best = null; float bd = float.MaxValue;
            foreach (var u in Unit.All)
            {
                if (!u.IsAlive || u.team != Team.Neutral) continue;
                var n = u.GetComponent<NeutralAI>();
                if (n == null || LaneData.Camps[n.camp].side != unit.team) continue;
                float d = Flat(u.transform.position, transform.position);
                if (d < bd) { bd = d; best = u; }
            }
            if (best != null)
            {
                if (Time.time >= react && best.Health.maxHealth > 500f) { react = Time.time + 1f; CastAt(best, false); }
                attack.SetTarget(best);
                return;
            }
        }

        LaneLogic();
    }

    // ---------- Линия ----------
    void LaneLogic()
    {
        var l = laneChoice == 3 ? LaneData.Lane.Mid : lane;
        Vector3 spot;
        CreepAI front = null; float best = float.MinValue;
        foreach (var u in Unit.All)
        {
            if (!u.IsAlive || u.kind != UnitKind.Creep || u.team != unit.team) continue;
            var ai = u.GetComponent<CreepAI>();
            if (ai == null || ai.lane != l) continue;
            if (ai.Progress > best) { best = ai.Progress; front = ai; }
        }
        if (front != null)
        {
            Vector3 d = front.NextPoint - front.transform.position; d.y = 0;
            spot = front.transform.position - (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.zero) * 1.6f;
        }
        else
        {
            Tower own = null; int tier = 99;
            foreach (var t in FindObjectsByType<Tower>(FindObjectsSortMode.None))
                if (t.lane == l && t.GetComponent<Unit>().team == unit.team && t.GetComponent<Unit>().IsAlive && t.tier < tier) { own = t; tier = t.tier; }
            spot = own != null ? ToBase(own.transform.position, 3.5f) : LaneData.Spawn(unit.team, l);
        }
        var et = EnemyTowerAt(spot);
        if (et != null && !Safe(spot)) spot = ToBase(et.transform.position, et.range + 2.5f);

        // Цель рядом: крипы, потом строения (только под прикрытием своих крипов)
        Unit target = attack.Target;
        if (target != null && (!target.IsAlive || Flat(target.transform.position, transform.position) > 7f || !Safe(target.transform.position) || target.team == Team.Neutral))
        {
            attack.ClearTarget(); target = null;
        }
        // Денай: свой крип почти умер — добиваем, чтобы враг не получил золото
        if (target == null && attack != null)
        {
            float chance = diff == 0 ? 0.12f : diff == 2 ? 0.6f : 0.3f;
            foreach (var u in Unit.All)
            {
                if (!u.IsAlive || u.kind != UnitKind.Creep || u.team != unit.team || u.Health == null || u.GetComponent<Courier>() != null || u.GetComponent<BeerBottle>() != null) continue;
                if (u.Health.Current > u.Health.maxHealth * 0.5f || u.Health.Current > attack.damage * 0.9f) continue;
                if (u.DistanceFrom(transform.position) > attack.range + 1f) continue;
                if (Random.value < chance) { target = u; attack.SetTarget(u); }
                break;
            }
        }

        if (target == null)
        {
            float bd = 7f;
            foreach (var u in Unit.All)
            {
                if (!u.IsAlive || !unit.IsEnemyOf(u) || u.team == Team.Neutral || u.kind == UnitKind.Hero) continue;
                if (u.Health != null && u.Health.invulnerable) continue;
                if (!Safe(u.transform.position)) continue;
                if (u.kind == UnitKind.Structure)
                {
                    var tw = u.GetComponent<Tower>();
                    if (HpR < 0.5f || AlliesNear(u.transform.position, tw != null ? tw.range : 8f) < 3) continue;
                }
                float d = u.DistanceFrom(transform.position) + (u.kind == UnitKind.Creep ? 0f : 2f);
                if (d < bd) { bd = d; target = u; }
            }
            if (target != null) attack.SetTarget(target);
        }

        // Макс фармит пачки крипов золотым дождём
        if (target != null && IsMax && mana != null && mana.Current > mana.maxMana * 0.6f && Time.time >= farmAt)
        {
            farmAt = Time.time + 2f;
            int clump = 0;
            foreach (var u in Unit.All)
                if (u.IsAlive && u.kind == UnitKind.Creep && unit.IsEnemyOf(u) && u.team != Team.Neutral && Flat(u.transform.position, target.transform.position) < 3.5f) clump++;
            if (clump >= 4) CastAt(target, false);
        }

        // Саня фармит пачки крипов койлами
        if (target != null && IsSanya && mana != null && mana.Current > mana.maxMana * 0.55f && Time.time >= farmAt)
        {
            farmAt = Time.time + 1.5f;
            int clump = 0;
            foreach (var u in Unit.All)
                if (u.IsAlive && u.kind == UnitKind.Creep && unit.IsEnemyOf(u) && u.team != Team.Neutral && Flat(u.transform.position, target.transform.position) < 2.4f) clump++;
            if (clump >= 3) CastAt(target, false);
        }

        // Пивмавер добивает крипа бутылкой — из него вылезут бутылочки
        if (target != null && Piv != null && mana != null && mana.Current > mana.maxMana * 0.45f && Time.time >= farmAt && caster.CanCast(0))
        {
            farmAt = Time.time + 1f;
            var q = caster.Abilities[0];
            float qd = q.Info.Value(0, q.Level);
            foreach (var u in Unit.All)
            {
                if (!u.IsAlive || u.kind != UnitKind.Creep || !unit.IsEnemyOf(u) || u.team == Team.Neutral || u.Health == null) continue;
                if (u.Health.Current > qd * (1f - u.Health.magicResist) * 0.95f || !Safe(u.transform.position)) continue;
                if (Flat(u.transform.position, transform.position) <= q.CastRange && caster.BotCast(0, u)) break;
            }
        }

        if (target == null && Flat(spot, transform.position) > 1.5f) MoveTo(spot);
    }

    // ---------- Способности ----------
    bool CastAt(Unit t, bool isHero)
    {
        if (caster == null || caster.Abilities == null) return false;
        float d = Flat(t.transform.position, transform.position);
        if (IsMax)
        {
            if (MaxSnipe()) return true;
            var hp = t.Health;
            if (isHero && hp != null && caster.CanCast(3) && hp.Current < hp.maxHealth * 0.45f && d > 6f && caster.BotCast(3, t)) return true;
            var q = caster.Abilities[0];
            if (caster.CanCast(0) && d <= q.CastRange && (isHero || q.Charges >= 2) && caster.BotCastPoint(0, t.transform.position)) return true;
            if (isHero && attack != null && d > attack.range - 1f && d < attack.range + 3f && caster.BotCast(2, unit)) return true;
            return false;
        }
        if (IsSanya)
        {
            if (isHero && caster.CanCast(3) && d < 4.5f && ((stash != null && stash.Count >= 8) || t.Health.Current < 500f))
                return caster.BotCast(3, unit);
            for (int i = 0; i < 3 && i < caster.Abilities.Length; i++)
            {
                var a = caster.Abilities[i];
                if (!caster.CanCast(i)) continue;
                if (Mathf.Abs(d - a.Info.distance) <= a.Info.radius * 0.7f)
                {
                    Vector3 dir = t.transform.position - transform.position; dir.y = 0;
                    if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
                    return caster.BotCast(i, unit);
                }
            }
            return false;
        }
        // Шашлык Ассасин: маринад-щит, капкан под ноги врагу, засада перед ударом
        if (GetComponent<ShashlikKit>() != null)
        {
            if (!isHero) return false;
            if (d < 7f && caster.CanCast(0) && caster.BotCast(0, unit)) return true;
            if (caster.CanCast(3) && d <= caster.Abilities[3].CastRange && caster.BotCastPoint(3, t.transform.position)) return true;
            if (attack != null && d <= attack.range + 0.3f && caster.CanCast(1) && caster.BotCast(1, unit)) return true;
            return false;
        }
        var piv = Piv;
        if (piv != null)
        {
            if (!isHero) return false;   // крипов добивает отдельно (FarmLane)
            if (d < 4f && caster.CanCast(3) && caster.BotCast(3, unit)) return true;
            if (caster.CanCast(1) && !piv.InFoam(transform.position, out _, out _) && caster.BotCastPoint(1, transform.position)) return true;
            if (caster.BotCast(0, t)) return true;
            return false;
        }
        // Огр: пицца, потом соус; энергетик — себе в драке
        if (caster.BotCast(0, t) || caster.BotCast(1, t)) return true;
        if (isHero && d < 5f && caster.BotCast(2, unit)) return true;
        return false;
    }

    // Макс: добить ультой любого видимого врага-героя, если пуля его убьёт
    bool MaxSnipe()
    {
        if (caster == null || caster.Abilities == null || caster.Abilities.Length < 4 || !caster.CanCast(3)) return false;
        var r = caster.Abilities[3];
        float dmg = r.Info.Value(0, r.Level);
        foreach (var u in Unit.All)
        {
            if (u.kind != UnitKind.Hero || !u.IsAlive || !unit.IsEnemyOf(u) || u.team == Team.Neutral) continue;
            if (!FogOfWar.CanSee(unit.team, u) || Flat(u.transform.position, transform.position) > r.CastRange) continue;
            var st = u.GetComponent<StatusEffects>();
            if (st != null && st.IsMagicImmune) continue;
            if (u.Health.Current < dmg * (1f - u.Health.magicResist) * 0.95f) return caster.BotCast(3, u);
        }
        return false;
    }

    void LearnSkills()
    {
        if (progress == null || caster == null || caster.Abilities == null) return;
        int[] order = { 3, 0, 1, 2 };
        for (int guard = 0; guard < 8 && progress.SkillPoints > 0; guard++)
        {
            bool done = false;
            foreach (int i in order)
                if (i < caster.Abilities.Length && progress.Learn(i)) { done = true; break; }
            if (!done) break;
        }
    }

    // ---------- Магазин ----------
    void Shop()
    {
        if (Time.time < shopAt || inv == null) return;
        shopAt = Time.time + 1f;
        // у бани вещи из тайника сами переходят в инвентарь
        for (int i = inv.Stash.Count - 1; i >= 0; i--)
            if (inv.Give(inv.Stash[i])) inv.Stash.RemoveAt(i);
        MarkBuilt();
        if (!inv.Has("shawarma") && progress.Gold >= 310) inv.TryBuy(ItemCatalog.Get("shawarma"), progress, out _);
        BuyScooterIfRich();
        string next = NextBuild();
        if (next == null) return;
        if (BuyStep(ItemCatalog.Get(next)) && inv.Has(next)) built.Add(next);
    }

    // Предмет из сборки уже у героя (мог приехать курьером) — отмечаем
    void MarkBuilt()
    {
        foreach (var id in Build) if (!built.Contains(id) && inv.Has(id)) built.Add(id);
    }

    string NextBuild()
    {
        foreach (var id in Build) if (!built.Contains(id)) return id;
        return null;
    }

    void BuyScooterIfRich()
    {
        var c = Courier.Of(gameObject);
        if (c != null && !c.Upgraded && MatchSetup.MatchTime > 180f && progress.Gold >= 400)
            inv.TryBuy(ItemCatalog.Get(Inventory.ScooterId), progress, out _);
    }

    // Покупка в тайник и вызов курьера, когда он свободен
    void RemoteShop()
    {
        if (Time.time < shopAt || inv == null) return;
        shopAt = Time.time + 1.5f;
        var c = Courier.Of(gameObject);
        if (c == null || c.IsDead || !c.IsFree) return;
        MarkBuilt();
        BuyScooterIfRich();
        string next = NextBuild();
        if (next != null && inv.Stash.Count == 0) BuyStepToStash(ItemCatalog.Get(next));
        if (inv.Stash.Count > 0 && inv.FreeSlotCount() > 0) c.Deliver();
    }

    // Как BuyStep, но в тайник: целиком, если хватает, иначе первую недостающую деталь
    bool BuyStepToStash(ItemDef d)
    {
        if (d == null) return false;
        if (inv.BuyToStash(d, progress, out _)) return true;
        var have = new List<string>();
        for (int i = 0; i < Inventory.TotalSlots; i++) { var it = inv.Get(i); if (it != null) have.Add(it.def.id); }
        foreach (var p in d.components)
        {
            int k = have.IndexOf(p);
            if (k >= 0) { have.RemoveAt(k); continue; }
            if (BuyStepToStash(ItemCatalog.Get(p))) return true;
        }
        return false;
    }

    bool NextBuyAffordable()
    {
        foreach (var id in Build) if (!built.Contains(id)) return true;
        return false;
    }

    bool BuyStep(ItemDef d)
    {
        if (d == null) return false;
        if (inv.TryBuy(d, progress, out _)) return true;
        var have = new List<string>();
        for (int i = 0; i < Inventory.TotalSlots; i++) { var it = inv.Get(i); if (it != null) have.Add(it.def.id); }
        foreach (var p in d.components)
        {
            int k = have.IndexOf(p);
            if (k >= 0) { have.RemoveAt(k); continue; }
            if (BuyStep(ItemCatalog.Get(p))) return true;
        }
        return false;
    }

    void UseItem(ItemActive kind)
    {
        if (inv == null || items == null) return;
        for (int i = 0; i < Inventory.MainSlots; i++)
        {
            var it = inv.Get(i);
            if (it != null && it.def.active == kind && it.cooldownLeft <= 0f) { items.Use(i); return; }
        }
    }

    // ---------- Глиф ----------
    void UseGlyphIfNeeded()
    {
        if (Time.time < glyphAt || MatchSetup.InLobby || unit.team == MatchSetup.PlayerTeam) return;
        glyphAt = Time.time + 0.5f;
        if (Glyph.CooldownLeft(unit.team) > 0f) return;
        foreach (var u in Unit.All)
        {
            if (!u.IsAlive || u.team != unit.team || u.kind != UnitKind.Structure || u.Health.invulnerable) continue;
            if (u.Health.Current > u.Health.maxHealth * 0.3f) continue;
            int enemies = 0;
            foreach (var e in Unit.All)
                if (e.IsAlive && unit.IsEnemyOf(e) && e.team != Team.Neutral && Flat(e.transform.position, u.transform.position) < 10f) enemies++;
            if (enemies >= 3) { Glyph.Use(unit.team); return; }
        }
    }

    // ---------- Помощники ----------
    Tower EnemyTowerAt(Vector3 p)
    {
        foreach (var t in FindObjectsByType<Tower>(FindObjectsSortMode.None))
        {
            var tu = t.GetComponent<Unit>();
            if (tu.IsAlive && unit.IsEnemyOf(tu) && Flat(t.transform.position, p) < t.range + 1.2f) return t;
        }
        return null;
    }

    int AlliesNear(Vector3 p, float r)
    {
        int n = 0;
        foreach (var u in Unit.All)
            if (u.IsAlive && u.kind == UnitKind.Creep && u.team == unit.team && Flat(u.transform.position, p) < r) n++;
        return n;
    }

    bool Safe(Vector3 p)
    {
        var t = EnemyTowerAt(p);
        return t == null || AlliesNear(t.transform.position, t.range) >= 3 || AllyHeroesNear(t.transform.position, t.range + 3f) >= 3;
    }

    int AllyHeroesNear(Vector3 p, float r)
    {
        int n = 0;
        foreach (var u in Unit.All)
            if (u.IsAlive && u.kind == UnitKind.Hero && u.team == unit.team && Flat(u.transform.position, p) < r) n++;
        return n;
    }

    // ---------- Умное поведение ----------
    // Своя ближайшая живая башня — встаём за ней (со стороны базы)
    Vector3 OwnTowerBehind()
    {
        Vector3 best = ownThrone != null ? ownThrone.position : transform.position; float bd = float.MaxValue;
        foreach (var t in FindObjectsByType<Tower>(FindObjectsSortMode.None))
        {
            var tu = t.GetComponent<Unit>();
            if (!tu.IsAlive || tu.team != unit.team) continue;
            float d = Flat(t.transform.position, transform.position);
            if (d < bd) { bd = d; best = t.transform.position; }
        }
        return ToBase(best, 3.5f);
    }

    void TryBottle()
    {
        if (items == null || Time.time < bottleAt) return;
        bottleAt = Time.time + 3f;
        items.BotUse(ItemActive.Bottle);
    }

    void UseSmartItems(Unit foe)
    {
        if (items == null) return;
        // квас — когда здоровья мало и никто не бьёт
        if (foe == null && (HpR < 0.6f || (mana != null && mana.Current < mana.maxMana * 0.35f))) TryBottle();
        // перец — если рядом прячется невидимый враг (охотница в засаде)
        if (Time.time >= dustAt)
        {
            dustAt = Time.time + 1f;
            foreach (var u in Unit.All)
            {
                if (!u.IsAlive || u.kind != UnitKind.Hero || !unit.IsEnemyOf(u) || !u.IsInvisibleNow) continue;
                if (Flat(u.transform.position, transform.position) > 10f) continue;
                if (items.BotUse(ItemActive.RevealInvis)) { attack.SetTarget(u); break; }
            }
        }
    }

    // Ганг: мид, саппорт и лесник после 3-й минуты ищут врага, которого можно поймать
    bool UpdateGank()
    {
        if (role == Role.Carry || role == Role.Offlane || MatchSetup.MatchTime < 180f) return false;
        if (gankTarget != null)
        {
            bool ok = gankTarget.IsAlive && Time.time < gankUntil && FogOfWar.CanSee(unit.team, gankTarget) && HpR > 0.5f
                      && EnemyTowerAt(gankTarget.transform.position) == null;
            if (!ok) { gankTarget = null; return false; }
            MoveTo(gankTarget.transform.position);
            return true;
        }
        if (Time.time < gankCheck) return false;
        gankCheck = Time.time + (diff == 0 ? 60f : diff == 2 ? 25f : 40f);
        if (HpR < 0.7f || (mana != null && mana.Current < mana.maxMana * 0.5f)) return false;
        Unit best = null; float bestScore = 0f;
        foreach (var u in Unit.All)
        {
            if (!u.IsAlive || u.kind != UnitKind.Hero || !unit.IsEnemyOf(u) || u.team == Team.Neutral) continue;
            if (!FogOfWar.CanSee(unit.team, u) || EnemyTowerAt(u.transform.position) != null) continue;
            float d = Flat(u.transform.position, transform.position);
            if (d > 60f || d < 12f) continue;
            int friends = 0;
            foreach (var o in Unit.All) if (o != u && o.IsAlive && o.kind == UnitKind.Hero && o.team == u.team && Flat(o.transform.position, u.transform.position) < 14f) friends++;
            float hr = u.Health.Current / Mathf.Max(1f, u.Health.maxHealth);
            float score = (1.2f - hr) + (friends == 0 ? 0.6f : 0f) - d / 120f;
            if (friends <= 1 && score > bestScore) { bestScore = score; best = u; }
        }
        if (best == null || bestScore < 0.45f) return false;
        gankTarget = best;
        gankUntil = Time.time + 25f;
        if (unit.team == MatchSetup.PlayerTeam) FloatingText.Show(transform.position + Vector3.up * 4f, "Иду на ганг!", new Color(1f, 0.6f, 0.3f), 18);
        return true;
    }

    // Пуш толпой: команда выбирает линию с самой слабой вражеской башней и идёт туда вместе
    void UpdateTeamPush()
    {
        if (MatchSetup.MatchTime < 600f || laneChoice == 3) { lane = EnemyTowersAlive(homeLane) ? homeLane : lane; return; }
        if (Time.time >= planCheck)
        {
            planCheck = Time.time + 5f;
            if (!plans.TryGetValue(unit.team, out var p) || Time.time > p.until || !EnemyTowersAlive(p.lane))
            {
                Tower weakest = null; float wr = float.MaxValue;
                foreach (var t in FindObjectsByType<Tower>(FindObjectsSortMode.None))
                {
                    var tu = t.GetComponent<Unit>();
                    if (!tu.IsAlive || !unit.IsEnemyOf(tu)) continue;
                    float r = tu.Health.Current / Mathf.Max(1f, tu.Health.maxHealth) + t.tier * 0.5f;
                    if (r < wr) { wr = r; weakest = t; }
                }
                if (weakest != null) plans[unit.team] = new TeamPlan { lane = weakest.lane, until = Time.time + 75f };
            }
        }
        // керри до 15-й минуты продолжает фармить свою линию
        if (role == Role.Carry && MatchSetup.MatchTime < 900f) return;
        if (plans.TryGetValue(unit.team, out var plan) && HpR > 0.5f) lane = plan.lane;
    }

    bool EnemyTowersAlive(LaneData.Lane l)
    {
        foreach (var t in FindObjectsByType<Tower>(FindObjectsSortMode.None))
        {
            var tu = t.GetComponent<Unit>();
            if (t.lane == l && tu.IsAlive && unit.IsEnemyOf(tu)) return true;
        }
        return false;
    }

    Vector3 ToBase(Vector3 p, float k)
    {
        Vector3 home = ownThrone != null ? ownThrone.position : Vector3.zero;
        Vector3 d = home - p; d.y = 0;
        return p + (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.zero) * k;
    }

    void MoveTo(Vector3 p)
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (attack.Target != null) attack.ClearTarget();
        if (NavMesh.SamplePosition(p, out NavMeshHit hit, 4f, NavMesh.AllAreas)) p = hit.position;
        agent.isStopped = false;
        agent.SetDestination(p);
    }

    void Stop()
    {
        if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
    }

    static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }
}
