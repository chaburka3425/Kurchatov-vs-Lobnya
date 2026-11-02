using System.Collections.Generic;
using UnityEngine;

// Врождённая способность Сани «Сосисочный запас»:
// +1 сосиска за добитого крипа, +3 за героя (до 24), каждая — +2 к урону; при смерти теряет половину.
// Сосиски крутятся вокруг Сани.
[RequireComponent(typeof(Unit))]
public class SausageStash : MonoBehaviour
{
    public int max = 24;
    public float damagePerSausage = 2f;
    public const int DeathLineIndex = 4;   // «Фраза 5» — «Даже моя колбаска не вечна»

    public int Count { get; private set; }
    public bool Charging;   // во время реквиема сосиски сжимаются и крутятся быстрее

    Unit unit;
    HeroStats stats;
    readonly List<Transform> orbit = new List<Transform>();
    float angle;

    void Awake()
    {
        unit = GetComponent<Unit>();
        stats = GetComponent<HeroStats>();
        Health.AnyDeath += OnAnyDeath;
        var h = GetComponent<Health>();
        if (h != null) h.OnDeath += OnOwnDeath;
    }

    void OnDestroy() => Health.AnyDeath -= OnAnyDeath;

    void OnAnyDeath(Health dead, Unit killer)
    {
        if (killer != unit) return;
        var du = dead.GetComponent<Unit>();
        if (du == null || du == unit || du.kind == UnitKind.Structure) return;
        bool hero = du.kind == UnitKind.Hero;
        Add(hero ? 3 : 1);
        if (hero) VoiceLines.Speak("sanya_fiend", transform.position, 1f, false, -1, DeathLineIndex);
    }

    void OnOwnDeath(Unit killer)
    {
        if (Count > 0) Add(-Mathf.CeilToInt(Count / 2f));
        FloatingText.Show(transform.position + Vector3.up * 4.5f, "Даже моя колбаска не вечна...", new Color(1f, 0.7f, 0.5f), 28);
        VoiceLines.Speak("sanya_fiend", transform.position, 1f, true, DeathLineIndex);
    }

    public void Add(int n)
    {
        int old = Count;
        Count = Mathf.Clamp(Count + n, 0, max);
        if (Count == old) return;
        if (stats != null) { stats.BonusDamage = Count * damagePerSausage; stats.Recalculate(false); }
        if (n > 0 && GetComponent<HeroController>() == HeroController.Player)
            FloatingText.Show(transform.position + Vector3.up * 4.2f, n == 1 ? "+1 сосиска" : $"+{n} сосиски", new Color(1f, 0.6f, 0.3f), 22);
    }

    void Update()
    {
        int want = unit.IsAlive ? Mathf.Min(Count, 10) : 0;
        while (orbit.Count < want)
        {
            var s = RuntimeMats.Sausage(null, transform.position, 1f).transform;
            RuntimeMats.Prim(PrimitiveType.Sphere, s, new Vector3(0, 0.12f, 0), new Vector3(0.12f, 0.2f, 0.12f), RuntimeMats.Glow(new Color(1f, 0.55f, 0.1f)));
            orbit.Add(s);
        }
        while (orbit.Count > want) { Destroy(orbit[orbit.Count - 1].gameObject); orbit.RemoveAt(orbit.Count - 1); }
        if (orbit.Count == 0) return;
        bool vis = FogOfWar.VisibleToPlayer(unit);
        foreach (var o in orbit) foreach (var rr in o.GetComponentsInChildren<Renderer>()) rr.enabled = vis;

        float r = Charging ? 0.6f : 1.5f;
        angle += (Charging ? 360f : 60f) * Time.deltaTime;
        for (int i = 0; i < orbit.Count; i++)
        {
            float a = (angle + 360f * i / orbit.Count) * Mathf.Deg2Rad;
            var p = transform.position + new Vector3(Mathf.Cos(a) * r, 1.6f + Mathf.Sin(a * 3f) * 0.12f, Mathf.Sin(a) * r);
            orbit[i].position = Vector3.Lerp(orbit[i].position, p, 12f * Time.deltaTime);
            orbit[i].rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)));
        }
    }

    void OnDisable()
    {
        foreach (var t in orbit) if (t != null) Destroy(t.gameObject);
        orbit.Clear();
    }
}
