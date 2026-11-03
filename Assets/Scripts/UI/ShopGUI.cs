using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Магазин «Пивная баня» в стиле MOBA:
// вкладки категорий, сетка предметов, справа — подробности и дерево сборки.
// ЛКМ — выбрать, ПКМ — быстрая покупка. Открыть/закрыть — B или кнопка «МАГАЗИН».
[RequireComponent(typeof(Inventory))]
public class ShopGUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Inventory inv;
    HeroProgress wallet;
    ItemEffects fx;
    Unit unit;

    ItemCategory tab = ItemCategory.Consumable;
    ItemDef selected;
    string message;
    Color messageColor;
    float messageUntil;
    float s;
    Vector2 detailScroll;

    static readonly ItemCategory[] Tabs =
        { ItemCategory.Consumable, ItemCategory.Attributes, ItemCategory.Equipment, ItemCategory.Misc, ItemCategory.Upgrade };
    static readonly Color Gold = new Color(1f, 0.78f, 0.2f);
    static readonly Color Accent = new Color(0.9f, 0.12f, 0.1f);

    void Awake()
    {
        inv = GetComponent<Inventory>();
        wallet = GetComponent<HeroProgress>();
        fx = GetComponent<ItemEffects>();
        unit = GetComponent<Unit>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        var gm = GameManager.Instance;
        if (gm != null && (gm.IsPaused || gm.IsGameOver)) return;
        if (MatchSetup.InLobby && !MatchSetup.InBuyPhase) return;
        if (kb != null && kb.bKey.wasPressedThisFrame) Toggle();

        TransferStash();
    }

    // у бани вещи из тайника сами переходят в инвентарь (по сети — только на хосте)
    public void TransferStash()
    {
        if (Net.IsClient) return;
        if (inv != null && inv.Stash.Count > 0 && unit.IsAlive && !MatchSetup.InLobby && NearShop())
        {
            for (int i = inv.Stash.Count - 1; i >= 0; i--)
                if (inv.Give(inv.Stash[i])) inv.Stash.RemoveAt(i);
        }
    }

    public void Open() { if (!IsOpen) Toggle(); }
    public void Close() { if (IsOpen) Toggle(); }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        if (IsOpen && selected == null) selected = ItemCatalog.Get("shawarma");
    }

    // Покупать можно у своей бани или когда герой мёртв.
    public bool NearShop()
    {
        if (!unit.IsAlive) return true;
        var b = fx != null ? fx.FindOwnBath() : null;
        if (b == null) return true;
        Vector3 d = transform.position - b.transform.position; d.y = 0f;
        return d.magnitude <= b.radius + 9f;
    }

    public void Buy(ItemDef d)
    {
        if (d == null) return;
        if (Net.IsClient) { NetCmd.Buy(d.id); ParticleBurst.Spawn(transform.position + Vector3.up * 2.5f, Gold, 10); return; }
        if (!NearShop() && d.id != Inventory.ScooterId)
        {
            // далеко от бани — в тайник, курьер привезёт
            if (inv.BuyToStash(d, wallet, out string serr))
            {
                Say($"В тайник: {d.name} — нажми F, курьер привезёт", new Color(1f, 0.85f, 0.4f));
                ParticleBurst.Spawn(transform.position + Vector3.up * 2.5f, Gold, 10);
            }
            else Say(serr, new Color(1f, 0.5f, 0.5f));
            return;
        }
        if (inv.TryBuy(d, wallet, out string err))
        {
            Say($"Куплено: {d.name}", new Color(0.5f, 1f, 0.5f));
            ParticleBurst.Spawn(transform.position + Vector3.up * 2.5f, Gold, 16);
        }
        else Say(err, new Color(1f, 0.5f, 0.5f));
    }

    public void Say(string text, Color c)
    {
        message = text;
        messageColor = c;
        messageUntil = Time.unscaledTime + 2.5f;
    }

    int PlanCost(ItemDef d) => inv.PurchaseCost(d, new List<int>());

    // ================= ОТРИСОВКА =================
    void OnGUI()
    {
        GUI.depth = -20; // магазин поверх всего, кроме паузы
        if (wallet == null) return;
        s = Mobile.UiScale;

        // Кнопка «МАГАЗИН» над золотом
        var btn = new Rect(Screen.width - 240 * s, Screen.height - 150 * s, 220 * s, 46 * s);
        HudArea.Add(btn);
        Box(btn, IsOpen ? new Color(0.55f, 0.1f, 0.08f) : new Color(0.3f, 0.06f, 0.05f));
        Box(new Rect(btn.x, btn.yMax - 3 * s, btn.width, 3 * s), Gold);
        GUI.Label(btn, "МАГАЗИН  (B)", Style(20, FontStyle.Bold, TextAnchor.MiddleCenter, Gold));
        if (GUI.Button(btn, GUIContent.none, GUIStyle.none)) Toggle();

        if (!IsOpen) return;

        float top = 80 * s, bottom = Screen.height - 320 * s;
        float w = Mathf.Min(1000 * s, Screen.width - 40 * s);
        var panel = new Rect(Screen.width - w - 20 * s, top, w, Mathf.Max(420 * s, bottom - top));
        HudArea.Add(panel);

        Box(new Rect(panel.x - 3 * s, panel.y - 3 * s, panel.width + 6 * s, panel.height + 6 * s), new Color(0.3f, 0.06f, 0.05f));
        Box(panel, new Color(0.05f, 0.045f, 0.05f, 0.97f));

        // Заголовок
        GUI.Label(new Rect(panel.x + 20 * s, panel.y + 8 * s, 600 * s, 40 * s), "МАГАЗИН «ПИВНАЯ БАНЯ»",
            Style(26, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
        GUI.Label(new Rect(panel.xMax - 320 * s, panel.y + 8 * s, 230 * s, 40 * s), $"Золото: {wallet.Gold}",
            Style(22, FontStyle.Bold, TextAnchor.MiddleRight, Gold));
        var close = new Rect(panel.xMax - 60 * s, panel.y + 10 * s, 44 * s, 36 * s);
        Box(close, new Color(0.25f, 0.08f, 0.07f));
        GUI.Label(close, "X", Style(22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        if (GUI.Button(close, GUIContent.none, GUIStyle.none)) IsOpen = false;

        bool near = NearShop();
        GUI.Label(new Rect(panel.x + 20 * s, panel.y + 48 * s, panel.width - 40 * s, 26 * s),
            near ? "Ты у бани — можно покупать.  ЛКМ — выбрать,  ПКМ — быстро купить."
                 : "Ты далеко от бани — покупка ляжет в тайник, курьер привезёт (F).",
            Style(16, FontStyle.Normal, TextAnchor.MiddleLeft, near ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f)));

        float leftW = Mathf.Min(580 * s, panel.width * 0.58f);
        var left = new Rect(panel.x + 16 * s, panel.y + 82 * s, leftW, panel.height - 96 * s);
        var right = new Rect(left.xMax + 18 * s, left.y, panel.xMax - left.xMax - 34 * s, left.height);

        DrawTabs(new Rect(left.x, left.y, left.width, 40 * s));
        DrawGrid(new Rect(left.x, left.y + 52 * s, left.width, left.height - 52 * s));
        DrawDetails(right);

        if (message != null && Time.unscaledTime < messageUntil)
        {
            var mr = new Rect(panel.x, panel.yMax - 40 * s, panel.width, 36 * s);
            Box(mr, new Color(0, 0, 0, 0.7f));
            GUI.Label(mr, message, Style(19, FontStyle.Bold, TextAnchor.MiddleCenter, messageColor));
        }
    }

    void DrawTabs(Rect r)
    {
        float tw = r.width / Tabs.Length;
        for (int i = 0; i < Tabs.Length; i++)
        {
            var tr = new Rect(r.x + i * tw, r.y, tw - 4 * s, r.height);
            bool active = tab == Tabs[i];
            Box(tr, active ? new Color(0.3f, 0.07f, 0.06f) : new Color(0.12f, 0.11f, 0.12f));
            if (active) Box(new Rect(tr.x, tr.yMax - 4 * s, tr.width, 4 * s), Accent);
            GUI.Label(tr, ItemCatalog.CategoryName(Tabs[i]),
                Style(14, FontStyle.Bold, TextAnchor.MiddleCenter, active ? Color.white : new Color(0.7f, 0.68f, 0.68f)));
            if (GUI.Button(tr, GUIContent.none, GUIStyle.none)) tab = Tabs[i];
        }
    }

    void DrawGrid(Rect r)
    {
        var items = ItemCatalog.InCategory(tab);
        float cw = 104 * s, ch = 126 * s, g = 10 * s;
        int cols = Mathf.Max(1, Mathf.FloorToInt((r.width + g) / (cw + g)));
        for (int i = 0; i < items.Count; i++)
        {
            var cr = new Rect(r.x + (i % cols) * (cw + g), r.y + (i / cols) * (ch + g), cw, ch);
            ItemCard(cr, items[i], true);
        }
    }

    // Карточка предмета: иконка, название, цена. ЛКМ — выбрать, ПКМ — купить.
    void ItemCard(Rect r, ItemDef d, bool showCost)
    {
        bool owned = inv.Has(d.id);
        bool isSel = selected == d;
        Color border = isSel ? Gold : owned ? new Color(0.35f, 0.8f, 0.35f) : new Color(0.25f, 0.23f, 0.23f);
        Box(new Rect(r.x - 2 * s, r.y - 2 * s, r.width + 4 * s, r.height + 4 * s), border);
        Box(r, new Color(0.1f, 0.1f, 0.11f));

        float iconH = showCost ? r.height - 28 * s : r.height;
        var icon = new Rect(r.x + 4 * s, r.y + 4 * s, r.width - 8 * s, iconH - 8 * s);
        Icon(icon, d);

        if (showCost)
        {
            int plan = PlanCost(d);
            Color cc = wallet.Gold >= plan ? Gold : new Color(0.75f, 0.45f, 0.4f);
            GUI.Label(new Rect(r.x, r.yMax - 28 * s, r.width, 26 * s), plan.ToString(),
                Style(17, FontStyle.Bold, TextAnchor.MiddleCenter, cc));
        }

        var e = Event.current;
        if (e.type == EventType.MouseDown && r.Contains(e.mousePosition))
        {
            if (e.button == 0) { selected = d; detailScroll = Vector2.zero; }
            else if (e.button == 1) { selected = d; Buy(d); }
            e.Use();
        }
    }

    void Icon(Rect r, ItemDef d)
    {
        var c = d.color;
        var tex = ItemIcons.Get(d.id);
        if (tex != null)
        {
            GUI.DrawTexture(r, tex, ScaleMode.ScaleAndCrop);
            if (r.height > 60 * s)
            {
                Box(new Rect(r.x, r.yMax - 22 * s, r.width, 22 * s), new Color(0, 0, 0, 0.6f));
                GUI.Label(new Rect(r.x + 2 * s, r.yMax - 22 * s, r.width - 4 * s, 22 * s), d.name,
                    Style(12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, true));
            }
            return;
        }
        Box(r, new Color(c.r * 0.55f + 0.05f, c.g * 0.55f + 0.05f, c.b * 0.55f + 0.05f));
        Box(new Rect(r.x, r.y, r.width, r.height * 0.35f), new Color(1, 1, 1, 0.07f));
        Box(new Rect(r.x + r.width * 0.3f, r.y + r.height * 0.18f, r.width * 0.4f, r.height * 0.3f), c);
        GUI.Label(new Rect(r.x + 2 * s, r.y + r.height * 0.5f, r.width - 4 * s, r.height * 0.5f), d.name,
            Style(r.height > 60 * s ? 13 : 10, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, true));
    }

    void DrawDetails(Rect r)
    {
        Box(r, new Color(0.08f, 0.075f, 0.08f));
        if (selected == null)
        {
            GUI.Label(r, "Выбери предмет слева", Style(18, FontStyle.Normal, TextAnchor.MiddleCenter, Color.gray));
            return;
        }
        var d = selected;
        int total = ItemCatalog.TotalCost(d);
        int plan = PlanCost(d);
        float iw = r.width - 24 * s;

        var buyRect = new Rect(r.x + 12 * s, r.yMax - 64 * s, r.width - 24 * s, 54 * s);
        var area = new Rect(r.x + 12 * s, r.y + 10 * s, r.width - 24 * s, buyRect.y - r.y - 16 * s);

        GUILayout.BeginArea(area);
        detailScroll = GUILayout.BeginScrollView(detailScroll, GUIStyle.none, GUIStyle.none);

        // Шапка: иконка + имя + цена
        var head = GUILayoutUtility.GetRect(iw, 96 * s);
        Icon(new Rect(head.x, head.y, 90 * s, 90 * s), d);
        GUI.Label(new Rect(head.x + 104 * s, head.y, iw - 104 * s, 40 * s), d.name,
            Style(24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, true));
        GUI.Label(new Rect(head.x + 104 * s, head.y + 40 * s, iw - 104 * s, 26 * s), $"Цена: {total}",
            Style(18, FontStyle.Bold, TextAnchor.MiddleLeft, Gold));
        if (plan < total)
            GUI.Label(new Rect(head.x + 104 * s, head.y + 64 * s, iw - 104 * s, 26 * s), $"Тебе доплатить: {plan}",
                Style(17, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.5f, 1f, 0.5f)));

        // Бонусы
        var st = Style(17, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.92f, 0.9f, 0.9f), true);
        st.richText = true;
        string stats = ItemCatalog.DescribeStats(d);
        if (!string.IsNullOrEmpty(stats)) GUILayout.Label(stats, st, GUILayout.Width(iw));

        var lore = Style(15, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.6f, 0.58f, 0.55f), true);
        GUILayout.Space(6 * s);
        GUILayout.Label(d.lore, lore, GUILayout.Width(iw));

        // Дерево сборки
        if (d.IsUpgrade)
        {
            GUILayout.Space(10 * s);
            GUILayout.Label("СОБИРАЕТСЯ ИЗ:", Style(16, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.6f, 0.5f)));
            DrawRecipeTree(d, iw);
        }

        // Где используется
        var usedIn = ItemCatalog.UsedIn(d.id);
        if (usedIn.Count > 0)
        {
            GUILayout.Space(10 * s);
            GUILayout.Label("ИСПОЛЬЗУЕТСЯ В:", Style(16, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.6f, 0.5f)));
            float sz = 72 * s, g = 8 * s;
            int cols = Mathf.Max(1, Mathf.FloorToInt((iw + g) / (sz + g)));
            int rows = (usedIn.Count + cols - 1) / cols;
            var row = GUILayoutUtility.GetRect(iw, rows * (sz + g));
            for (int i = 0; i < usedIn.Count; i++)
                ItemCard(new Rect(row.x + (i % cols) * (sz + g), row.y + (i / cols) * (sz + g), sz, sz), usedIn[i], false);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // Кнопка покупки
        if (!NearShop()) plan = ItemCatalog.TotalCost(d);
        bool can = wallet.Gold >= plan;
        Box(buyRect, can ? new Color(0.2f, 0.55f, 0.2f) : new Color(0.25f, 0.22f, 0.22f));
        Box(new Rect(buyRect.x, buyRect.y, buyRect.width, 4 * s), can ? new Color(0.5f, 1f, 0.5f) : Color.gray);
        GUI.Label(buyRect, $"КУПИТЬ — {plan}", Style(24, FontStyle.Bold, TextAnchor.MiddleCenter, can ? Color.white : new Color(0.7f, 0.7f, 0.7f)));
        if (GUI.Button(buyRect, GUIContent.none, GUIStyle.none)) Buy(d);
    }

    // Результат сверху, линии вниз к частям; рецепт — отдельной карточкой.
    void DrawRecipeTree(ItemDef d, float width)
    {
        int n = d.components.Length + (d.HasRecipe ? 1 : 0);
        float sz = Mathf.Min(78 * s, (width - (n - 1) * 10 * s) / n);
        float g = 10 * s;
        var area = GUILayoutUtility.GetRect(width, sz * 2 + 40 * s);

        var top = new Rect(area.x + width / 2f - sz / 2f, area.y, sz, sz);
        ItemCard(top, d, false);

        float rowW = n * sz + (n - 1) * g;
        float rx = area.x + (width - rowW) / 2f;
        float ry = top.yMax + 36 * s;

        // Линии
        Box(new Rect(top.center.x - 1.5f * s, top.yMax, 3 * s, 18 * s), Gold);
        Box(new Rect(rx + sz / 2f, top.yMax + 16 * s, rowW - sz, 3 * s), Gold);

        for (int i = 0; i < n; i++)
        {
            var cr = new Rect(rx + i * (sz + g), ry, sz, sz);
            Box(new Rect(cr.center.x - 1.5f * s, top.yMax + 16 * s, 3 * s, ry - top.yMax - 16 * s), Gold);

            if (i < d.components.Length)
            {
                var comp = ItemCatalog.Get(d.components[i]);
                if (comp == null) continue;
                ItemCard(cr, comp, false);
                if (inv.Has(comp.id))
                {
                    var tag = new Rect(cr.x, cr.y, cr.width, 18 * s);
                    Box(tag, new Color(0.15f, 0.5f, 0.15f, 0.9f));
                    GUI.Label(tag, "есть", Style(12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
                }
            }
            else
            {
                // Рецепт
                Box(new Rect(cr.x - 2 * s, cr.y - 2 * s, cr.width + 4 * s, cr.height + 4 * s), new Color(0.6f, 0.5f, 0.3f));
                Box(cr, new Color(0.85f, 0.75f, 0.5f));
                GUI.Label(new Rect(cr.x, cr.y + 6 * s, cr.width, cr.height * 0.5f), "РЕЦЕПТ",
                    Style(12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.35f, 0.2f, 0.05f)));
                GUI.Label(new Rect(cr.x, cr.y + cr.height * 0.5f, cr.width, cr.height * 0.4f), d.cost.ToString(),
                    Style(15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.45f, 0.25f, 0.05f)));
            }
        }
    }

    static void Box(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    GUIStyle Style(float size, FontStyle fs, TextAnchor anchor, Color color, bool wrap = false)
    {
        var st = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(8, Mathf.RoundToInt(size * s)),
            fontStyle = fs,
            alignment = anchor,
            wordWrap = wrap,
            clipping = TextClipping.Clip
        };
        st.normal.textColor = color;
        return st;
    }
}
