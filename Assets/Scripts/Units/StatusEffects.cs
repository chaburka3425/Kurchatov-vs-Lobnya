using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Оглушения, замедления, баффы скорости, урон со временем (горение),
// лечение со временем (шаурма/чипсы) и иммунитет к магии (Чёрный пояс).
// Всё, что висит на юните, видно иконками у полоски здоровья (Shown).
public class StatusEffects : MonoBehaviour
{
    public enum Kind { Stun, Slow, AttackSpeed, MoveSpeed, Dot, Heal, Mana, Immune, Invisible, ArmorDown, Shield }

    // Невидимость (Засада): враги не видят, не могут выбрать целью. Снимается вручную или по времени.
    float invisEnd, revealEnd;
    public bool IsInvisible => Time.time < invisEnd && Time.time >= revealEnd;   // перец проявляет невидимых
    public bool IsRevealed => Time.time < revealEnd;
    public void Reveal(float duration, string name, string icon)
    {
        revealEnd = Mathf.Max(revealEnd, Time.time + duration);
        Show(Kind.ArmorDown, name, icon, false, duration);
    }
    public void SetInvisible(float duration, string name, string icon)
    {
        invisEnd = Time.time + duration;
        Show(Kind.Invisible, name, icon, true, duration);
    }
    public void BreakInvisible()
    {
        invisEnd = 0f;
        shown.RemoveAll(s => s.kind == Kind.Invisible);
    }

    // Снижение брони (удар из засады)
    readonly List<Timed> armorDown = new List<Timed>();
    public float ArmorReduction { get { float s = 0; foreach (var b in armorDown) s += b.value; return s; } }
    public void AddArmorReduction(float amount, float duration, string name, string icon)
    {
        armorDown.Add(new Timed { value = amount, endTime = Time.time + duration });
        Show(Kind.ArmorDown, name, icon, false, duration);
    }

    // Иконка щита с зарядами (Маринад): время показывает, сколько щит ещё держится
    public void ShowShield(float duration, string name, string icon) => Show(Kind.Shield, name, icon, true, duration);
    public void HideShield() => shown.RemoveAll(s => s.kind == Kind.Shield);

    // Эффект для показа: имя, иконка (id способности или предмета), хороший/плохой, время
    public class Shown
    {
        public Kind kind;
        public string name, icon;
        public bool good;
        public float start, end;
        public float Left => Mathf.Max(0f, end - Time.time);
        public float Fraction => end > start ? Left / (end - start) : 0f;
    }
    readonly List<Shown> shown = new List<Shown>();
    public IReadOnlyList<Shown> Active => shown;

    // Тот же эффект от того же источника — продлеваем, а не плодим иконки
    // (соус = горение + замедление, энергетик = атака + бег — одна иконка)
    void Show(Kind kind, string name, string icon, bool good, float duration)
    {
        float end = Time.time + duration;
        var s = shown.Find(x => icon != null ? x.icon == icon : x.kind == kind && x.name == name);
        if (s != null) { if (end > s.end) { s.start = Time.time; s.end = end; } return; }
        shown.Add(new Shown { kind = kind, name = name, icon = icon, good = good, start = Time.time, end = end });
    }

    class Timed { public float value, endTime; }
    class Dot { public float dps, endTime, tick; public Unit source; public DamageType type; }
    class Hot { public float hps, mps, endTime; }

    readonly List<Timed> slows = new List<Timed>();
    readonly List<Timed> attackSpeedBuffs = new List<Timed>();
    readonly List<Timed> moveSpeedBuffs = new List<Timed>();
    readonly List<Dot> dots = new List<Dot>();
    readonly List<Hot> hots = new List<Hot>();

    float stunEnd;
    float hotFxAt;
    Unit unitRef;
    float immuneEnd;
    bool wasStunned;
    float baseMoveSpeed;
    NavMeshAgent agent;
    Health health;
    Mana mana;

    public bool IsStunned => Time.time < stunEnd;
    public bool IsMagicImmune => Time.time < immuneEnd;
    public bool IsBurning => dots.Count > 0;
    public bool IsSlowed => slows.Count > 0;
    public bool IsBuffed => attackSpeedBuffs.Count > 0 || moveSpeedBuffs.Count > 0;
    public bool IsHealing => hots.Count > 0;

    public float BonusAttackSpeed { get { float s = 0; foreach (var b in attackSpeedBuffs) s += b.value; return s; } }
    float SlowPercent { get { float m = 0; foreach (var b in slows) m = Mathf.Max(m, b.value); return m; } }
    float MoveBonusPercent { get { float s = 0; foreach (var b in moveSpeedBuffs) s += b.value; return s; } }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        mana = GetComponent<Mana>();
        if (agent != null) baseMoveSpeed = agent.speed;
    }

    public void SetBaseMoveSpeed(float speed) => baseMoveSpeed = speed;

    // name/icon — что показать у полоски здоровья (icon — id способности или предмета)
    public void ApplyStun(float duration, string name = "Оглушение", string icon = null)
    {
        if (IsMagicImmune) return;
        stunEnd = Mathf.Max(stunEnd, Time.time + duration);
        wasStunned = true;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        Show(Kind.Stun, name, icon, false, duration);
    }

    public void ApplySlow(float percent, float duration, string name = "Замедление", string icon = null)
    {
        if (IsMagicImmune) return;
        slows.Add(new Timed { value = percent, endTime = Time.time + duration });
        Show(Kind.Slow, name, icon, false, duration);
    }

    public void AddAttackSpeed(float amount, float duration, string name = "Скорость атаки", string icon = null)
    {
        attackSpeedBuffs.Add(new Timed { value = amount, endTime = Time.time + duration });
        Show(Kind.AttackSpeed, name, icon, amount >= 0f, duration);
    }

    // good — показать зелёным, даже если бег медленнее (лестница Макса — это бафф)
    public void AddMoveSpeed(float percent, float duration, string name = null, string icon = null, bool? good = null)
    {
        moveSpeedBuffs.Add(new Timed { value = percent, endTime = Time.time + duration });
        Show(Kind.MoveSpeed, name ?? (percent >= 0f ? "Ускорение" : "Замедление"), icon, good ?? percent >= 0f, duration);
    }

    public void AddDot(float dps, float duration, Unit source, DamageType type, string name = "Горение", string icon = null)
    {
        if (IsMagicImmune && type != DamageType.Physical) return;
        dots.Add(new Dot { dps = dps, endTime = Time.time + duration, source = source, type = type });
        Show(Kind.Dot, name, icon, false, duration);
    }

    // Шаурма сбивается, если ударил вражеский герой или башня (как банка в Доте)
    public bool BreakHeal()
    {
        int n = hots.RemoveAll(h => h.hps > 0f);
        shown.RemoveAll(s => s.kind == Kind.Heal);
        return n > 0;
    }

    public void AddHealOverTime(float hpPerSec, float manaPerSec, float duration, string name = null, string icon = null)
    {
        hots.Add(new Hot { hps = hpPerSec, mps = manaPerSec, endTime = Time.time + duration });
        bool hp = hpPerSec > 0f;
        Show(hp ? Kind.Heal : Kind.Mana, name ?? (hp ? "Лечение" : "Восстановление маны"), icon, true, duration);
    }

    public void AddMagicImmunity(float duration, string name = "Иммунитет к магии", string icon = null)
    {
        immuneEnd = Mathf.Max(immuneEnd, Time.time + duration);
        // Иммунитет снимает плохие магические эффекты
        slows.Clear();
        dots.RemoveAll(d => d.type != DamageType.Physical);
        stunEnd = Mathf.Min(stunEnd, Time.time);
        shown.RemoveAll(s => s.kind == Kind.Slow || s.kind == Kind.Stun || (s.kind == Kind.Dot && dots.Count == 0));
        Show(Kind.Immune, name, icon, true, duration);
    }

    public void ClearAll()
    {
        slows.Clear(); attackSpeedBuffs.Clear(); moveSpeedBuffs.Clear(); dots.Clear(); hots.Clear();
        armorDown.Clear(); invisEnd = 0f; revealEnd = 0f;
        shown.Clear();
        stunEnd = 0f;
        immuneEnd = 0f;
        wasStunned = false;
    }

    void Update()
    {
        if (health != null && health.IsDead) { ClearAll(); return; }

        float now = Time.time;
        slows.RemoveAll(b => b.endTime <= now);
        attackSpeedBuffs.RemoveAll(b => b.endTime <= now);
        moveSpeedBuffs.RemoveAll(b => b.endTime <= now);
        hots.RemoveAll(b => b.endTime <= now);
        armorDown.RemoveAll(b => b.endTime <= now);
        shown.RemoveAll(s => s.end <= now);

        // Конец оглушения — снова можно двигаться.
        if (wasStunned && !IsStunned)
        {
            wasStunned = false;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }

        // Урон со временем: тик каждые 0.5 сек.
        for (int i = dots.Count - 1; i >= 0; i--)
        {
            if (i >= dots.Count) continue;
            var d = dots[i];
            d.tick += Time.deltaTime;
            while (d.tick >= 0.5f)
            {
                d.tick -= 0.5f;
                if (health != null)
                    health.TakeDamage(new DamageInfo { amount = d.dps * 0.5f, type = d.type, source = d.source });
            }
            if (d.endTime <= now && i < dots.Count) dots.RemoveAt(i);
        }

        // Лечение со временем (зелёные/синие искорки над героем)
        if (hots.Count > 0 && Time.time >= hotFxAt)
        {
            hotFxAt = Time.time + 0.4f;
            if (unitRef == null) unitRef = GetComponent<Unit>();
            if (unitRef != null && unitRef.kind == UnitKind.Hero && FogOfWar.VisibleToPlayer(unitRef))
                Fx.Glints(transform.position, hots.Exists(x => x.hps > 0f) ? new Color(0.45f, 1f, 0.5f) : new Color(0.45f, 0.7f, 1f), 2, 0.6f, 2f, 0.9f);
        }
        foreach (var h in hots)
        {
            if (health != null && h.hps > 0f) health.Heal(h.hps * Time.deltaTime);
            if (mana != null && h.mps > 0f) mana.Restore(h.mps * Time.deltaTime);
        }

        if (agent != null && baseMoveSpeed > 0f)
            agent.speed = baseMoveSpeed * Mathf.Max(0.1f, 1f - SlowPercent / 100f + MoveBonusPercent / 100f);
    }
}
