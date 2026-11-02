using System.Collections.Generic;
using UnityEngine;

// Анимации из кода (как в Доте): замах и удар при атаке, жесты при способностях.
// Модели собраны из примитивов без костей, поэтому при старте руку с оружием
// собираем под «плечо» и крутим его. Пересоздавать карту не нужно.
public class UnitAnimator : MonoBehaviour
{
    // Что относится к правой / левой руке (имена частей из HeroBuilder)
    static readonly HashSet<string> RightParts = new HashSet<string> { "ArmR", "HandR", "ForkHandle", "ForkTine", "ForkSausage", "ClubHandle", "ClubHead", "Spike", "BottleR" };
    static readonly HashSet<string> LeftParts = new HashSet<string> { "ArmL", "HandL", "HandSausage", "Flame", "FlameCore", "CanBody", "CanTop", "CanBolt", "BottleL" };
    static readonly HashSet<string> GunParts = new HashSet<string> { "Rifle", "ArmR", "ArmL", "HandR", "HandL", "Watch" };

    Transform model, armR, armL;
    Vector3 armRPos, lastOffset;
    Quaternion modelRot;
    bool gun;       // Макс: обе руки держат винтовку — отдача вместо замаха
    bool ready;

    enum Mode { None, Windup, Strike, Cast }
    Mode mode;
    float t, dur;
    int slot;

    public static UnitAnimator For(GameObject go)
    {
        var a = go.GetComponent<UnitAnimator>();
        return a != null ? a : go.AddComponent<UnitAnimator>();
    }

    void Start()
    {
        var u = GetComponent<Unit>();
        model = transform.Find("Model");
        if (model == null || (u != null && u.kind == UnitKind.Structure)) { enabled = false; return; }
        modelRot = model.localRotation;

        var parts = new List<Transform>();
        foreach (Transform ch in model) parts.Add(ch);
        gun = System.Array.Exists(model.GetComponentsInChildren<Transform>(true), p => p.name == "Rifle");
        // новые модели (Пивмавер) приходят уже с шарнирами в плечах
        armR = model.Find("ArmRPivot");
        armL = model.Find("ArmLPivot");
        if (armR == null && armL == null)
        {
            if (gun) armR = Group(parts, p => GunParts.Contains(p.name), "ArmR");
            else
            {
                armR = Group(parts, p => RightParts.Contains(p.name) || (p.name == "ArmStripe" && p.localPosition.x > 0f), "ArmR");
                armL = Group(parts, p => LeftParts.Contains(p.name) || (p.name == "ArmStripe" && p.localPosition.x < 0f), "ArmL");
            }
        }
        if (armR != null) armRPos = armR.localPosition;

        // паучьи ноги: LegPivot_L0..R3 — шагают «четвёрками» (L0, L2, R1, R3 и остальные)
        foreach (var p in parts)
            if (p.name.StartsWith("LegPivot_") && p.name.Length >= 11)
            {
                float side = p.name[9] == 'L' ? -1f : 1f;
                int i = p.name[10] - '0';
                legs.Add(new Leg { t = p, side = side, phase = ((i + (side < 0 ? 0 : 1)) % 2) * Mathf.PI, baseRot = p.localRotation });
            }
            // ноги людей: HipPivot_L / HipPivot_R — шагают по очереди
            else if (p.name == "HipPivot_L" || p.name == "HipPivot_R")
                legs.Add(new Leg { t = p, side = p.name.EndsWith("L") ? -1f : 1f, phase = p.name.EndsWith("L") ? 0f : Mathf.PI, baseRot = p.localRotation, biped = true });
        biped = legs.Exists(l => l.biped);
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        ready = true;
    }

    class Leg { public Transform t; public float side, phase; public Quaternion baseRot; public bool biped; }
    bool biped;
    readonly List<Leg> legs = new List<Leg>();
    UnityEngine.AI.NavMeshAgent agent;
    float walk, walkBlend;

    // Собрать части под общий шарнир в плече (верх части armName)
    Transform Group(List<Transform> parts, System.Predicate<Transform> pick, string armName)
    {
        var list = parts.FindAll(pick);
        var arm = list.Find(p => p.name == armName);
        if (arm == null) return null;
        // капсула высотой 2 → её верх на scale.y вдоль своей оси (рендерер в тумане может быть выключен, поэтому без bounds)
        Vector3 axis = arm.localRotation * Vector3.up;
        if (axis.y < 0f) axis = -axis;
        Vector3 top = arm.localPosition + axis * arm.localScale.y;
        var pivot = new GameObject(armName + "Pivot").transform;
        pivot.SetParent(model, false);
        pivot.localPosition = top;
        foreach (var p in list) p.SetParent(pivot, true);
        return pivot;
    }

    // ---------- Вызовы из атаки и способностей ----------
    public void Windup(float time) { Play(Mode.Windup, Mathf.Max(0.05f, time)); }
    public void Strike() { Play(Mode.Strike, gun ? 0.22f : 0.3f); }
    public void Cancel() { if (mode == Mode.Windup) mode = Mode.None; }
    public void Cast(int abilitySlot) { slot = abilitySlot; Play(Mode.Cast, abilitySlot == 3 ? 0.65f : 0.45f); }

    void Play(Mode m, float d) { mode = m; t = 0f; dur = d; }

    void LateUpdate()
    {
        if (!ready) return;
        float right = 0f, left = 0f, lean = 0f, lunge = 0f, hop = 0f, kick = 0f;

        if (mode != Mode.None)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            switch (mode)
            {
                case Mode.Windup:
                    float w = Mathf.SmoothStep(0f, 1f, p);
                    if (gun) { right = -8f * w; lean = 3f * w; }          // прицеливается
                    else { right = -130f * w; lean = 7f * w; }           // замах над головой
                    if (p >= 1f) { t = dur; }                             // держим замах до удара
                    break;
                case Mode.Strike:
                    float a = p < 0.35f ? p / 0.35f : 1f - (p - 0.35f) / 0.65f;
                    if (gun) { right = 14f * a; kick = 0.18f * a; lean = 4f * a; }   // отдача
                    else
                    {
                        right = p < 0.35f ? Mathf.Lerp(-130f, 45f, p / 0.35f) : Mathf.Lerp(45f, 0f, (p - 0.35f) / 0.65f);
                        lean = -12f * a; lunge = 0.3f * a;
                    }
                    if (p >= 1f) mode = Mode.None;
                    break;
                case Mode.Cast:
                    float e = p < 0.3f ? Mathf.SmoothStep(0f, 1f, p / 0.3f) : 1f - Mathf.SmoothStep(0f, 1f, (p - 0.3f) / 0.7f);
                    if (gun) { right = (slot == 0 ? 35f : 18f) * e; kick = slot == 3 ? 0.25f * e : 0f; }
                    else if (slot == 0) { right = 100f * e; lunge = 0.25f * e; }          // Q — бросок правой
                    else if (slot == 1) { left = 100f * e; lunge = 0.25f * e; }           // W — бросок левой
                    else { right = left = 165f * e; }                                     // E, R — руки вверх
                    if (slot == 3) hop = Mathf.Sin(p * Mathf.PI) * 0.6f;                  // ульта — подпрыгивает
                    lean = -6f * e;
                    if (p >= 1f) mode = Mode.None;
                    break;
            }
        }

        // Ходьба: покачивание корпуса, у паучьих ног — шаги
        float speed = agent != null && agent.enabled ? new Vector2(agent.velocity.x, agent.velocity.z).magnitude : 0f;
        walkBlend = Mathf.MoveTowards(walkBlend, speed > 0.4f ? 1f : 0f, Time.deltaTime * 5f);
        walk += Time.deltaTime * (2.5f + speed * 1.4f);
        float bob = legs.Count > 0 && !biped ? 0.05f * Mathf.Sin(walk * 2f) : 0.07f * Mathf.Abs(Mathf.Sin(walk));
        bob *= walkBlend;
        if (biped && mode == Mode.None && !gun)
        {
            // руки машут в такт шагам
            right += -16f * Mathf.Sin(walk) * walkBlend;
            left += 16f * Mathf.Sin(walk) * walkBlend;
        }
        foreach (var l in legs)
        {
            float ph = walk + l.phase;
            if (l.biped)
            {
                // модель повёрнута на 180°: «вперёд» — +X
                l.t.localRotation = l.baseRot * Quaternion.Euler(26f * Mathf.Sin(ph) * walkBlend, 0f, 0f);
                continue;
            }
            float swing = 20f * Mathf.Sin(ph) * walkBlend + 1.5f * Mathf.Sin(Time.time * 1.7f + l.phase) * (1f - walkBlend);
            float lift = 16f * Mathf.Max(0f, Mathf.Cos(ph)) * walkBlend;
            l.t.localRotation = l.baseRot * Quaternion.Euler(0f, swing * l.side, lift * l.side);
        }

        // Модель повёрнута на 180°: «вперёд» для руки — +X поворот, наклон вперёд — минус
        if (armR != null) { armR.localRotation = Quaternion.Euler(right, 0f, 0f); armR.localPosition = armRPos + new Vector3(0f, 0f, kick); }
        if (armL != null) armL.localRotation = Quaternion.Euler(left, 0f, 0f);
        model.localRotation = modelRot * Quaternion.Euler(lean, 0f, 0f);
        // смещение — добавкой: Макс сам поднимает модель на лестницу
        Vector3 offset = Vector3.forward * lunge + Vector3.up * (hop + bob);
        model.localPosition += offset - lastOffset;
        lastOffset = offset;
    }
}
