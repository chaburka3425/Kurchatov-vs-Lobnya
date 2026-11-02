using UnityEngine;

// Цифры урона над целью: белые — физический, фиолетовые — магия, красные крупные — крит.
// Показывает урон, который нанёс или получил твой герой.
public class DamageNumbers : MonoBehaviour
{
    Unit hero => HeroController.Player != null ? HeroController.Player.GetComponent<Unit>() : null;

    void Start()
    {
        Health.AnyDamaged += OnDamaged;
    }

    void OnDestroy() => Health.AnyDamaged -= OnDamaged;

    void OnDamaged(Health target, DamageInfo info, float dealt)
    {
        if (hero == null || dealt < 1f) return;
        var tu = target.GetComponent<Unit>();
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
        FloatingText.Show(target.transform.position + Vector3.up * 2.2f + jitter, text, c, size);
    }
}
