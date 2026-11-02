using System.Collections.Generic;
using UnityEngine;

// Радиус способностей на земле (как в Доте):
// навёл мышь на кнопку способности — виден её радиус; Shift + Q/W/E/R — радиус виден всё время.
// Пока выбираешь цель для способности — радиус тоже виден. Для способностей в точку — ещё круг у курсора.
public class RangeIndicator : MonoBehaviour
{
    AbilityCaster caster;
    AutoAttack attack;
    readonly HashSet<int> pinned = new HashSet<int>();
    int hover = -1, hoverFrame = -10;
    readonly List<LineRenderer> pool = new List<LineRenderer>();

    public static RangeIndicator For(GameObject go)
    {
        var r = go.GetComponent<RangeIndicator>();
        return r != null ? r : go.AddComponent<RangeIndicator>();
    }

    void Awake()
    {
        caster = GetComponent<AbilityCaster>();
        attack = GetComponent<AutoAttack>();
    }

    public void Hover(int i) { hover = i; hoverFrame = Time.frameCount; }
    public bool IsPinned(int i) => pinned.Contains(i);
    public void TogglePin(int i)
    {
        if (caster == null || caster.Abilities == null || i >= caster.Abilities.Length) return;
        bool on = !pinned.Remove(i);
        if (on) pinned.Add(i);
        var a = caster.Abilities[i];
        FloatingText.Show(transform.position + Vector3.up * 3.6f, (on ? "Радиус: " : "Радиус убран: ") + (string.IsNullOrEmpty(a.Info.shortName) ? a.Info.name : a.Info.shortName),
            new Color(0.6f, 0.9f, 1f), 18);
    }

    void OnDisable() { foreach (var l in pool) if (l != null) l.enabled = false; }
    void OnDestroy() { foreach (var l in pool) if (l != null) Destroy(l.gameObject); }

    LineRenderer Line(int n)
    {
        while (pool.Count <= n)
        {
            var lr = MaxFx.Line("RangeRing", Color.white, 0.09f, true);
            pool.Add(lr);
        }
        pool[n].enabled = true;
        return pool[n];
    }

    void LateUpdate()
    {
        int used = 0;
        var unit = GetComponent<Unit>();
        bool show = caster != null && caster.Abilities != null && unit != null && unit.IsAlive && !MatchSetup.InLobby;
        if (show)
        {
            var list = new List<int>(pinned);
            if (Time.frameCount - hoverFrame <= 2 && hover >= 0 && !list.Contains(hover)) list.Add(hover);
            if (caster.TargetingIndex >= 0 && !list.Contains(caster.TargetingIndex)) list.Add(caster.TargetingIndex);
            foreach (int i in list)
            {
                if (i < 0 || i >= caster.Abilities.Length) continue;
                bool bright = i == hover && Time.frameCount - hoverFrame <= 2 || i == caster.TargetingIndex;
                used = Draw(caster.Abilities[i], used, bright);
            }
        }
        for (int k = used; k < pool.Count; k++) if (pool[k] != null) pool[k].enabled = false;
    }

    int Draw(Ability a, int n, bool bright)
    {
        var info = a.Info;
        Color c = Color.Lerp(info.color, Color.white, 0.35f);
        c.a = bright ? 0.95f : 0.55f;
        Vector3 p = transform.position + Vector3.up * 0.15f;

        if (info.distance > 0f)
        {
            // койлы Сани: круг на расстоянии перед героем
            Vector3 f = transform.forward; f.y = 0f;
            Ring(n++, p + f.normalized * info.distance, info.radius, c);
            return n;
        }
        float range = info.castRange;
        if (range <= 0f && info.radius <= 0f && attack != null) range = attack.range;   // пассивки — дальность атаки
        if (range > 0f) Ring(n++, p, range, c);
        if (info.radius > 0f)
        {
            if (info.target == AbilityTarget.Point)
            {
                // область у курсора (там, куда упадёт способность)
                var cam = Camera.main; var mouse = UnityEngine.InputSystem.Mouse.current;
                if (cam != null && mouse != null)
                {
                    var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                    if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d))
                    {
                        Vector3 at = ray.GetPoint(d);
                        Vector3 off = at - transform.position; off.y = 0f;
                        if (off.magnitude > range && range > 0f) at = transform.position + off.normalized * range;
                        Ring(n++, at + Vector3.up * 0.15f, info.radius, new Color(c.r, c.g, c.b, c.a * 0.9f));
                    }
                }
            }
            else if (range <= 0f) Ring(n++, p, info.radius, c);
        }
        return n;
    }

    void Ring(int n, Vector3 center, float r, Color c)
    {
        var lr = Line(n);
        lr.startColor = lr.endColor = c;
        lr.widthMultiplier = 1f;
        MaxFx.Circle(lr, center, r, Mathf.Clamp(Mathf.RoundToInt(r * 10f), 32, 120));
    }
}
