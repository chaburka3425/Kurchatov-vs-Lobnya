using UnityEngine;

// Простая полоска здоровья над юнитом (цвет — цвет города, серая — неуязвим).
[RequireComponent(typeof(Health))]
public class HealthBarGUI : MonoBehaviour
{
    public float heightOffset = 1.6f;
    public float width = 60f;
    public float height = 7f;
    public bool showName = false;

    Health health;
    Unit unit;
    StatusEffects status;
    static readonly Color GoodCol = new Color(0.3f, 0.9f, 0.35f), BadCol = new Color(0.95f, 0.25f, 0.2f);

    void Awake()
    {
        health = GetComponent<Health>();
        unit = GetComponent<Unit>();
    }

    void OnGUI()
    {
        if (health == null || health.IsDead) return;
        if (!FogOfWar.VisibleToPlayer(unit)) return;   // враг в тумане
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * heightOffset);
        if (sp.z < 0f) return;

        float x = sp.x - width / 2f;
        float y = Screen.height - sp.y;

        bool glyph = Glyph.Protects(unit);
        GUI.color = glyph ? new Color(1f, 0.85f, 0.3f, 0.6f + 0.4f * Mathf.Sin(Time.time * 10f)) : Color.black;
        GUI.DrawTexture(new Rect(x - (glyph ? 3 : 1), y - (glyph ? 3 : 1), width + (glyph ? 6 : 2), height + (glyph ? 6 : 2)), Texture2D.whiteTexture);
        if (glyph) { GUI.color = Color.black; GUI.DrawTexture(new Rect(x - 1, y - 1, width + 2, height + 2), Texture2D.whiteTexture); }
        GUI.color = health.invulnerable ? Color.gray : TeamUtil.GetColor(unit.team);
        GUI.DrawTexture(new Rect(x, y, width * (health.Current / health.maxHealth), height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Эффекты под полоской, как в Доте: иконка, рамка (зелёная — бафф, красная — дебафф), сколько осталось
        if (status == null) status = GetComponent<StatusEffects>();
        if (status != null && status.Active.Count > 0) DrawEffects(x, y + height + 3);

        if (showName)
        {
            string extra = health.invulnerable ? "  (неуязвим)" : "";
            var pl = HeroController.Player != null ? HeroController.Player.GetComponent<Unit>() : null;
            bool enemyHero = unit.kind == UnitKind.Hero && pl != null && pl.IsEnemyOf(unit);
            var ns = new GUIStyle(GUI.skin.label) { fontStyle = enemyHero ? FontStyle.Bold : FontStyle.Normal };
            ns.normal.textColor = enemyHero ? new Color(1f, 0.55f, 0.5f) : Color.white;
            GUI.Label(new Rect(x, y - 20, width + 160, 20),
                $"{unit.displayName}  {Mathf.CeilToInt(health.Current)}/{Mathf.RoundToInt(health.maxHealth)}{extra}", ns);
        }
    }

    void DrawEffects(float x, float y)
    {
        bool hero = unit.kind == UnitKind.Hero;
        float size = hero ? 24f : 15f, gap = 3f;
        var list = status.Active;
        StatusEffects.Shown hover = null;
        Rect hoverRect = default;

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            var r = new Rect(x + i * (size + gap), y, size, size);
            Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), e.good ? GoodCol : BadCol);

            var tex = Icon(e.icon);
            if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleAndCrop);
            else
            {
                Fill(r, e.good ? new Color(0.12f, 0.3f, 0.14f) : new Color(0.35f, 0.1f, 0.08f));
                if (hero) GUI.Label(r, Short(e.name), Style(10, Color.white, TextAnchor.MiddleCenter));
            }

            // сколько прошло — темнеет сверху вниз
            Fill(new Rect(r.x, r.y, r.width, r.height * (1f - e.Fraction)), new Color(0f, 0f, 0f, 0.55f));
            if (hero)
            {
                string left = e.Left < 1f ? e.Left.ToString("0.0") : Mathf.CeilToInt(e.Left).ToString();
                var sh = Style(11, Color.black, TextAnchor.LowerRight);
                GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), left, sh);
                GUI.Label(r, left, Style(11, Color.white, TextAnchor.LowerRight));
            }
            if (r.Contains(Event.current.mousePosition)) { hover = e; hoverRect = r; }
        }

        if (hover != null)
        {
            string txt = $"<b>{hover.name}</b>\n{(hover.good ? "Бафф" : "Дебафф")} — осталось {hover.Left:0.0} сек.";
            var tr = new Rect(hoverRect.x, hoverRect.yMax + 4, 220, 40);
            Fill(tr, new Color(0f, 0f, 0f, 0.85f));
            GUI.Label(new Rect(tr.x + 6, tr.y, tr.width - 12, tr.height), txt, Style(12, hover.good ? GoodCol : new Color(1f, 0.6f, 0.55f), TextAnchor.MiddleLeft));
        }
    }

    static Texture2D Icon(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var t = AbilityBarGUI.AbilityIcon(id);
        return t != null ? t : ItemIcons.Get(id);
    }

    // Две буквы вместо иконки: «Оглушение» → «ОГ»
    static string Short(string name) => string.IsNullOrEmpty(name) ? "?" : name.Substring(0, Mathf.Min(2, name.Length)).ToUpper();

    static void Fill(Rect r, Color c)
    {
        var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
    }

    static GUIStyle Style(int size, Color c, TextAnchor a)
    {
        var st = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold, alignment = a, richText = true, wordWrap = true, padding = new RectOffset(0, 1, 0, 0) };
        st.normal.textColor = c;
        return st;
    }
}
