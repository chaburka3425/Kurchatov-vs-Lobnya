using UnityEngine;

// Опыт, уровни, очки навыков и золото героя.
// Опыт — за врагов, умерших рядом (или убитых тобой). Золото — за свои добивания крипов и строений;
// за героев золото раздаёт MatchSetup (убийца, помощь, догонялка по золоту).
[RequireComponent(typeof(HeroStats), typeof(AbilityCaster))]
public class HeroProgress : MonoBehaviour
{
    public const int MaxLevel = 18;
    public const float XpShareRadius = 12f;
    public const float PassiveGold = 1.6f;   // золото в секунду просто так (≈100 в минуту, как в Доте)
    float goldAcc;

    public float Xp { get; private set; }
    public int SkillPoints { get; private set; } = 1;   // на 1-м уровне одно очко
    public int Gold { get; private set; } = 600; // стартовое золото

    public int Level => stats.level;
    public float XpToNext => 80f + 40f * (Level - 1);
    public float XpProgress => Level >= MaxLevel ? 1f : Xp / XpToNext;

    Unit unit;
    HeroStats stats;
    AbilityCaster caster;

    void Awake()
    {
        unit = GetComponent<Unit>();
        stats = GetComponent<HeroStats>();
        caster = GetComponent<AbilityCaster>();
        Health.AnyDeath += OnAnyDeath;
    }

    void OnDestroy() => Health.AnyDeath -= OnAnyDeath;

    void OnAnyDeath(Health dead, Unit killer)
    {
        var du = dead.GetComponent<Unit>();
        if (du == null || du == unit || !unit.IsEnemyOf(du)) return;

        bool mine = killer == unit;
        // свой крип добит своими (денай) — врагам только половина опыта
        bool denied = killer != null && killer != du && !killer.IsEnemyOf(du);
        Vector3 d = du.transform.position - transform.position;
        d.y = 0f;
        if (mine || (unit.IsAlive && d.magnitude <= XpShareRadius)) AddXp(denied ? du.xpBounty * 0.5f : du.xpBounty);

        // за героев золото раздаёт MatchSetup (убийца, помощь, догонялка)
        if (mine && du.goldBounty > 0 && du.kind != UnitKind.Hero)
        {
            Gold += du.goldBounty;
            FloatingText.Show(du.transform.position + Vector3.up * 2.5f, $"+{du.goldBounty}", new Color(1f, 0.82f, 0.2f), 26);
            if (FogOfWar.VisibleToPlayer(unit)) Fx.Coins(du.transform.position, transform, Mathf.Clamp(du.goldBounty / 25, 1, 6));
        }
    }

    public void AddXp(float amount)
    {
        if (Level >= MaxLevel || amount <= 0f) return;
        Xp += amount;
        while (Level < MaxLevel && Xp >= XpToNext)
        {
            Xp -= XpToNext;
            stats.level++;
            SkillPoints++;
            stats.Recalculate(false);
            FloatingText.Show(transform.position + Vector3.up * 4.2f, $"УРОВЕНЬ {Level}!", new Color(1f, 0.9f, 0.3f), 34);
            if (FogOfWar.VisibleToPlayer(unit))
            {
                // золотой столб света, кольцо и искры
                var gold = new Color(1f, 0.85f, 0.35f);
                Fx.Pillar(transform.position, gold, 7f, 0.8f, 1.3f);
                Fx.Ring(transform.position, gold, 0.4f, 3.2f, 0.7f, 0.2f);
                Fx.Glints(transform.position, gold, 16, 1f, 2.4f, 1.4f);
            }
        }
        if (Level >= MaxLevel) Xp = 0f;
    }

    public void AddGold(int amount) => Gold = Mathf.Max(0, Gold + amount);

    void Update()
    {
        if (MatchSetup.InLobby || MatchSetup.MatchTime <= 0f) return;
        var gm = GameManager.Instance;
        if (gm != null && (gm.IsPaused || gm.IsGameOver)) return;
        goldAcc += PassiveGold * Time.deltaTime;
        if (goldAcc >= 1f) { int g = Mathf.FloorToInt(goldAcc); goldAcc -= g; Gold += g; }
    }

    // Какой уровень героя нужен, чтобы взять следующий уровень способности.
    public int RequiredHeroLevel(Ability a) =>
        a.Info.isUltimate ? (a.Level + 1) * 6   // ульта: 6 / 12 / 18
                          : a.Level * 2 + 1;    // обычные: 1 / 3 / 5 / 7

    public bool CanLearn(int index)
    {
        if (caster.Abilities == null || index < 0 || index >= caster.Abilities.Length) return false;
        var a = caster.Abilities[index];
        return SkillPoints > 0 && a.Level < a.MaxLevel && Level >= RequiredHeroLevel(a);
    }

    public bool Learn(int index)
    {
        if (!CanLearn(index)) return false;
        var a = caster.Abilities[index];
        int lvl = a.Level + 1;
        foreach (var b in caster.Abilities)
            if (b == a || (!string.IsNullOrEmpty(a.Info.shareGroup) && b.Info.shareGroup == a.Info.shareGroup)) b.Level = lvl;
        SkillPoints--;
        return true;
    }
}
