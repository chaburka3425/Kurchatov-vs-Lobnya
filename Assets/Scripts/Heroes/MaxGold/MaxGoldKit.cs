using UnityEngine;
using UnityEngine.AI;

// Всё «постоянное» у Макса Gold: дальность от Лестницы Габилджона, сама лестница,
// пассивка Голдшот и голос (фраза смерти — voice-message 9).
[RequireComponent(typeof(Unit))]
public class MaxGoldKit : MonoBehaviour
{
    public const string HeroId = "max_gold";
    public const int DeathLineIndex = 5;      // line6 = voice-message 9
    public const float LadderHeight = 1.1f;

    public float LadderUntil;
    public bool OnLadder => Time.time < LadderUntil;

    Unit unit;
    AbilityCaster caster;
    HeroStats stats;
    StatusEffects status;
    Transform model, ladder;
    float appliedBonus = -1f;
    bool wasAlive = true;
    Renderer[] ladderParts;

    void Awake()
    {
        unit = GetComponent<Unit>();
        caster = GetComponent<AbilityCaster>();
        stats = GetComponent<HeroStats>();
        status = GetComponent<StatusEffects>();
        model = transform.Find("Model");
        var hc = GetComponent<HeroController>();
        if (hc != null) hc.OnHeroDied += OnDied;
        Health.AnyDeath += OnAnyDeath;
        BuildLadder();
    }

    void OnDestroy() => Health.AnyDeath -= OnAnyDeath;

    Ability Ab(int i) => caster != null && caster.Abilities != null && i < caster.Abilities.Length ? caster.Abilities[i] : null;

    // +дальность: пассивно от уровня E и ещё больше, пока стоит на лестнице
    public float RangeBonus
    {
        get
        {
            var e = Ab(2);
            if (e == null || !e.IsLearned) return 0f;
            float b = e.Info.Value(0, e.Level);
            if (OnLadder) b += e.Info.Value(1, e.Level);
            return b;
        }
    }

    public void StartLadder(float duration)
    {
        LadderUntil = Time.time + duration;
        if (status != null) status.AddMoveSpeed(-30f, duration, "Лестница Габилджона", "gabil_ladder", true);
    }

    // Голдшот: сколько добавить урона к этому выстрелу (0 — не сработал)
    public float RollHeadshot()
    {
        var w = Ab(1);
        if (w == null || !w.IsLearned) return 0f;
        bool hit = OnLadder || Random.value < w.Info.Value(0, w.Level) / 100f;
        return hit ? w.Info.Value(1, w.Level) : 0f;
    }

    public void OnHeadshot(Unit target)
    {
        if (target == null || target.kind == UnitKind.Structure) return;
        var st = target.GetComponent<StatusEffects>();
        if (st != null && !st.IsMagicImmune) st.ApplySlow(100f, 0.5f, "Голдшот", "gold_shot");
        var ag = target.GetComponent<NavMeshAgent>();
        Vector3 dir = target.transform.position - transform.position; dir.y = 0;
        if (ag != null && ag.enabled && ag.isOnNavMesh && dir.sqrMagnitude > 0.01f && !Glyph.Protects(target)) ag.Move(dir.normalized * 0.45f);
        bool mine = HeroController.Player != null && (HeroController.Player.gameObject == gameObject || HeroController.Player.gameObject == target.gameObject);
        if (mine) FloatingText.Show(target.transform.position + Vector3.up * 3.6f, "Голдшот!", new Color(1f, 0.82f, 0.3f), 22);
        ParticleBurst.Spawn(target.transform.position + Vector3.up * 2.2f, new Color(1f, 0.82f, 0.3f), 9, 4f);
        if (target.kind == UnitKind.Hero) VoiceLines.Speak(HeroId, transform.position, 0.35f, false, -1, DeathLineIndex);
    }

    void OnDied(Unit killer)
    {
        LadderUntil = 0f;
        VoiceLines.Speak(HeroId, transform.position, 1f, true, DeathLineIndex);
        var def = HeroCatalog.MaxGold;
        FloatingText.Show(transform.position + Vector3.up * 4.4f, def.deathLine, new Color(1f, 0.75f, 0.55f), 24);
    }

    void OnAnyDeath(Health dead, Unit killer)
    {
        if (killer != unit || dead == null) return;
        var du = dead.GetComponent<Unit>();
        if (du != null && du != unit && du.kind == UnitKind.Hero)
            VoiceLines.Speak(HeroId, transform.position, 1f, false, -1, DeathLineIndex);
    }

    void Update()
    {
        float b = RangeBonus;
        if (!Mathf.Approximately(b, appliedBonus)) { appliedBonus = b; if (stats != null) stats.Recalculate(false); }

        // фраза после воскрешения
        if (unit.IsAlive && !wasAlive) VoiceLines.Speak(HeroId, transform.position, 1f, false, -1, DeathLineIndex);
        wasAlive = unit.IsAlive;

        bool up = OnLadder && unit.IsAlive;
        if (model != null)
        {
            var p = model.localPosition;
            p.y = Mathf.MoveTowards(p.y, up ? LadderHeight : 0f, 5f * Time.deltaTime);
            model.localPosition = p;
        }
        bool vis = up && FogOfWar.VisibleToPlayer(unit);
        if (ladderParts != null) foreach (var r in ladderParts) if (r != null) r.enabled = vis;
    }

    // Золотая стремянка под ногами (собирается в игре)
    void BuildLadder()
    {
        ladder = new GameObject("GoldLadder").transform;
        ladder.SetParent(transform, false);
        var gold = new Material(RuntimeMats.Lit(new Color(1f, 0.78f, 0.24f)));
        if (gold.HasProperty("_Metallic")) gold.SetFloat("_Metallic", 0.9f);
        if (gold.HasProperty("_Smoothness")) gold.SetFloat("_Smoothness", 0.75f);
        foreach (float side in new[] { -1f, 1f })
            foreach (float fb in new[] { -1f, 1f })
            {
                var rail = RuntimeMats.Prim(PrimitiveType.Cylinder, ladder, new Vector3(0.55f * side, LadderHeight / 2f, 0.32f * fb), new Vector3(0.09f, LadderHeight / 2f + 0.05f, 0.09f), gold);
                rail.transform.localRotation = Quaternion.Euler(-14f * fb, 0, 0);
            }
        for (int k = 1; k <= 2; k++)
            foreach (float fb in new[] { -1f, 1f })
            {
                float y = LadderHeight * k / 3f, z = 0.32f * fb - fb * 0.27f * y;
                var rung = RuntimeMats.Prim(PrimitiveType.Cylinder, ladder, new Vector3(0, y, z), new Vector3(0.06f, 0.55f, 0.06f), gold);
                rung.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
        RuntimeMats.Prim(PrimitiveType.Cube, ladder, new Vector3(0, LadderHeight - 0.03f, 0), new Vector3(1.2f, 0.07f, 0.55f), gold);
        ladderParts = ladder.GetComponentsInChildren<Renderer>();
        foreach (var r in ladderParts) r.enabled = false;
    }
}
