using UnityEngine;

// Цифры урона над целью: белые — физический, фиолетовые — магия, красные крупные — крит.
// Показывает урон, который нанёс или получил твой герой.
public class DamageNumbers : MonoBehaviour
{

    void Start()
    {
        Health.AnyDamaged += OnDamaged;
    }

    void OnDestroy() => Health.AnyDamaged -= OnDamaged;

    void OnDamaged(Health target, DamageInfo info, float dealt)
    {
        if (dealt < 1f) return;
        var tu = target.GetComponent<Unit>();
        NetWorld.DmgEvent(tu, info.source, dealt, info.crit, info.type);
        Show(tu, info.source, dealt, info.crit, info.type);
    }

    // Показать цифру, если урон нанёс или получил свой герой (по сети приходит от хоста)
    public static void Show(Unit tu, Unit source, float dealt, bool crit, DamageType type)
    {
        var hero = HeroController.Player != null ? HeroController.Player.GetComponent<Unit>() : null;
        if (hero == null || tu == null) return;
        var info = new DamageInfo { source = source, crit = crit, type = type };
        bool byHero = info.source == hero;
        bool toHero = tu == hero;
        if (!byHero && !toHero) return;

        Color c;
        int size;
        string text = Mathf.RoundToInt(dealt).ToString();
        if (toHero) { c = new Color(1f, 0.35f, 0.35f); size = 18; text = "-" + text; }
        else if (info.crit) { c = new Color(1f, 0.2f, 0.15f); size = 34; text += "!"; }
        else if (info.type == DamageType.Magical) { c = new Color(0.8f, 0.5f, 1f); size = dealt < 20 ? 16 : 22; }
        else if (info.type == DamageType.Pure) { c = new Color(1f, 0.85f, 0.5f); size = 16; }
        else { c = Color.white; size = 20; }

        Vector3 jitter = new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
        FloatingText.NoNet = true;
        FloatingText.Show(tu.transform.position + Vector3.up * 2.2f + jitter, text, c, size);
        FloatingText.NoNet = false;
    }
}
