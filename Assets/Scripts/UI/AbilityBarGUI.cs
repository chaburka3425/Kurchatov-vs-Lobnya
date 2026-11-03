using UnityEngine;

// Нижняя панель героя (как в MOBA):
// портрет + уровень + характеристики | способности с прокачкой, здоровье, мана | инвентарь | золото.
[RequireComponent(typeof(AbilityCaster))]
public class AbilityBarGUI : MonoBehaviour
{
    public Texture2D faceLeft;
    public Texture2D faceRight;

    AbilityCaster caster;
    Health health;
    Mana mana;
    HeroStats stats;
    HeroProgress progress;
    Inventory inventory;
    ItemEffects itemFx;
    ShopGUI shop;
    Unit unit;
    StatusEffects status;
    AutoAttack attack;
    UnityEngine.AI.NavMeshAgent agent;
    float s;

    static readonly Color PanelColor = new Color(0.06f, 0.05f, 0.06f, 0.92f);
    static readonly Color Accent = new Color(0.9f, 0.12f, 0.1f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.2f);

    void Awake()
    {
        caster = GetComponent<AbilityCaster>();
        health = GetComponent<Health>();
        mana = GetComponent<Mana>();
        stats = GetComponent<HeroStats>();
        progress = GetComponent<HeroProgress>();
        inventory = GetComponent<Inventory>();
        itemFx = GetComponent<ItemEffects>();
        shop = GetComponent<ShopGUI>();
        unit = GetComponent<Unit>();
        status = GetComponent<StatusEffects>();
        attack = GetComponent<AutoAttack>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    void OnGUI()
    {
        GUI.depth = -10; // панель героя поверх полосок здоровья
        if (caster.Abilities == null) return;
        s = Mobile.UiScale;

        float h = 236 * s;
        float portraitW = 330 * s;
        int n = caster.Abilities.Length;
        float slot = 86 * s, gap = 10 * s, pad = 16 * s;
        float abilW = n * slot + (n - 1) * gap + 2 * pad;
        float invW = 330 * s;
        float total = portraitW + abilW + invW;
        float minimapRight = MinimapGUI.SizeFor(s) + 20 * s;
        float x0 = Mathf.Max(minimapRight, (Screen.width - total) / 2f);
        float y0 = Screen.height - h;

        var portraitRect = new Rect(x0, y0, portraitW, h);
        var abilRect = new Rect(x0 + portraitW, y0, abilW, h);
        var invRect = new Rect(abilRect.xMax, y0, invW, h);

        HudArea.Add(new Rect(x0, y0 - 80 * s, total, h + 80 * s));

        string tooltip = null;
        DrawPortrait(portraitRect);
        DrawAbilities(abilRect, slot, gap, pad, ref tooltip);
        DrawInventory(invRect, ref tooltip);
        DrawGold();

        if (tooltip != null) DrawTooltip(tooltip, abilRect);
        DrawTargetingHint();
        DrawTeleportBar();
    }

    // ---------------- Портрет и характеристики ----------------
    void DrawPortrait(Rect r)
    {
        Box(r, PanelColor);
        Box(new Rect(r.x, r.y, r.width, 4 * s), Accent);

        var def = stats != null ? stats.Definition : null;
        string heroName = def != null ? def.name.ToUpper() : unit.displayName;

        // Плашка с именем
        Box(new Rect(r.x + 8 * s, r.y + 8 * s, 160 * s, 30 * s), new Color(0.35f, 0.06f, 0.05f));
        GUI.Label(new Rect(r.x + 8 * s, r.y + 8 * s, 160 * s, 30 * s), heroName,
            Style(18, FontStyle.Bold, TextAnchor.MiddleCenter, Gold));

        // Портрет с двумя лицами
        var pr = new Rect(r.x + 8 * s, r.y + 42 * s, 160 * s, 140 * s);
        var tc = TeamUtil.GetColor(unit.team);
        Box(pr, new Color(tc.r * 0.35f, tc.g * 0.35f, tc.b * 0.35f));
        if (faceLeft != null && faceRight == null) GUI.DrawTexture(new Rect(pr.x + 4 * s, pr.y + 4 * s, pr.width - 8 * s, pr.height - 8 * s), faceLeft, ScaleMode.ScaleAndCrop);
        else if (faceLeft != null) GUI.DrawTexture(new Rect(pr.x + 2 * s, pr.y + 14 * s, 88 * s, 110 * s), faceLeft, ScaleMode.ScaleToFit);
        if (faceRight != null) GUI.DrawTexture(new Rect(pr.x + 70 * s, pr.y + 14 * s, 88 * s, 110 * s), faceRight, ScaleMode.ScaleToFit);
        var stash = GetComponent<SausageStash>();
        if (stash != null)
        {
            var sr = new Rect(pr.x + 30 * s, pr.yMax - 26 * s, pr.width - 30 * s, 24 * s);
            Box(sr, new Color(0, 0, 0, 0.75f));
            GUI.Label(sr, $"Сосиски {stash.Count}/{stash.max}", Style(14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.65f, 0.4f)));
        }
        if (!unit.IsAlive)
        {
            Box(pr, new Color(0, 0, 0, 0.6f));
            GUI.Label(pr, "МЁРТВ", Style(26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.4f)));
        }

        // Уровень и опыт
        if (progress != null)
        {
            var xpBar = new Rect(pr.x, pr.yMax + 6 * s, pr.width, 12 * s);
            Box(xpBar, new Color(0.15f, 0.13f, 0.1f));
            Box(new Rect(xpBar.x, xpBar.y, xpBar.width * progress.XpProgress, xpBar.height), Gold);
            var badge = new Rect(pr.x - 4 * s, pr.yMax - 40 * s, 44 * s, 44 * s);
            Box(badge, Gold);
            Box(new Rect(badge.x + 3 * s, badge.y + 3 * s, badge.width - 6 * s, badge.height - 6 * s), new Color(0.12f, 0.08f, 0.04f));
            GUI.Label(badge, progress.Level.ToString(), Style(24, FontStyle.Bold, TextAnchor.MiddleCenter, Gold));
        }

        // Характеристики
        if (def != null && attack != null && health != null)
        {
            float sx = r.x + 178 * s, sy = r.y + 12 * s, lh = 27 * s;
            float bonusAS = status != null ? status.BonusAttackSpeed : 0f;
            float speed = (agent != null ? agent.speed : def.moveSpeed) * 60f;
            StatLine(sx, sy + lh * 0, "Урон", attack.damage.ToString("0"));
            StatLine(sx, sy + lh * 1, "Скор. атаки", (attack.attackSpeed + bonusAS).ToString("0"));
            StatLine(sx, sy + lh * 2, "Броня", health.armor.ToString("0.#"));
            StatLine(sx, sy + lh * 3, "Маг. защита", (health.magicResist * 100f).ToString("0") + "%");
            StatLine(sx, sy + lh * 4, "Скорость", speed.ToString("0"));

            int lvl = stats.level;
            float ay = sy + lh * 5 + 8 * s;
            float aw = 46 * s;
            AttrBox(new Rect(sx, ay, aw, 40 * s), "СИЛ", stats.Str, AttributeUtil.GetColor(HeroAttribute.Strength), def.attribute == HeroAttribute.Strength);
            AttrBox(new Rect(sx + aw + 4 * s, ay, aw, 40 * s), "ЛОВ", stats.Agi, AttributeUtil.GetColor(HeroAttribute.Agility), def.attribute == HeroAttribute.Agility);
            AttrBox(new Rect(sx + 2 * (aw + 4 * s), ay, aw, 40 * s), "ИНТ", stats.Int, AttributeUtil.GetColor(HeroAttribute.Intelligence), def.attribute == HeroAttribute.Intelligence);
        }
    }

    void StatLine(float x, float y, string label, string value)
    {
        GUI.Label(new Rect(x, y, 100 * s, 26 * s), label, Style(16, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.75f, 0.72f, 0.72f)));
        GUI.Label(new Rect(x, y, 145 * s, 26 * s), value, Style(17, FontStyle.Bold, TextAnchor.MiddleRight, Color.white));
    }

    void AttrBox(Rect r, string label, float value, Color c, bool primary)
    {
        Box(r, primary ? new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f) : new Color(0.14f, 0.13f, 0.14f));
        if (primary) Box(new Rect(r.x, r.y, r.width, 3 * s), c);
        GUI.Label(new Rect(r.x, r.y + 2 * s, r.width, 16 * s), label, Style(11, FontStyle.Bold, TextAnchor.MiddleCenter, c));
        GUI.Label(new Rect(r.x, r.y + 16 * s, r.width, 22 * s), value.ToString("0"), Style(17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
    }

    // ---------------- Способности ----------------
    void DrawAbilities(Rect r, float slot, float gap, float pad, ref string tooltip)
    {
        Box(r, PanelColor);
        Box(new Rect(r.x, r.y, r.width, 4 * s), Accent);

        float sy = r.y + 16 * s;
        for (int i = 0; i < caster.Abilities.Length; i++)
        {
            var a = caster.Abilities[i];
            var sr = new Rect(r.x + pad + i * (slot + gap), sy, slot, slot);
            DrawSlot(a, sr, i == caster.TargetingIndex);
            DrawPips(a, new Rect(sr.x, sr.yMax + 5 * s, sr.width, 7 * s));
            if (sr.Contains(Event.current.mousePosition)) { tooltip = AbilityTooltip(a); RangeIndicator.For(gameObject).Hover(i); }
            // закреплённый радиус (Shift + клавиша) — значок-кружок в углу кнопки
            if (RangeIndicator.For(gameObject).IsPinned(i))
                GUI.Label(new Rect(sr.x + 4 * s, sr.yMax - 44 * s, sr.width - 8 * s, 20 * s), "радиус", Style(11, FontStyle.Bold, TextAnchor.LowerLeft, new Color(0.6f, 0.9f, 1f)));

            // Клик/тап по способности — то же, что клавиша
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && sr.Contains(ev.mousePosition))
            {
                caster.PressAbility(i);
                ev.Use();
            }

            // Кнопка прокачки «+»
            if (progress != null && progress.CanLearn(i))
            {
                var plus = new Rect(sr.x + 8 * s, r.y - 46 * s, slot - 16 * s, 40 * s);
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
                Box(plus, new Color(Gold.r * pulse, Gold.g * pulse, Gold.b * 0.5f));
                Box(new Rect(plus.x + 3 * s, plus.y + 3 * s, plus.width - 6 * s, plus.height - 6 * s), new Color(0.3f, 0.22f, 0.08f));
                GUI.Label(plus, "+", Style(30, FontStyle.Bold, TextAnchor.MiddleCenter, Gold));
                if (GUI.Button(plus, GUIContent.none, GUIStyle.none)) progress.Learn(i);
            }
        }

        bool anyLearnable = false;
        if (progress != null && caster != null && caster.Abilities != null)
            for (int k = 0; k < caster.Abilities.Length; k++) if (progress.CanLearn(k)) anyLearnable = true;
        if (anyLearnable)
            GUI.Label(new Rect(r.x, r.y - 78 * s, r.width, 28 * s),
                $"Очки навыков: {progress.SkillPoints}  (нажми «+» или Ctrl+клавиша)",
                Style(16, FontStyle.Bold, TextAnchor.MiddleCenter, Gold));


        // Здоровье и мана
        float bw = r.width - 2 * pad;
        float by = sy + slot + 22 * s;
        if (health != null)
            Bar(new Rect(r.x + pad, by, bw, 30 * s), health.Current, health.maxHealth,
                new Color(0.22f, 0.72f, 0.22f), "+" + health.regenPerSecond.ToString("0.0"));
        if (mana != null)
            Bar(new Rect(r.x + pad, by + 36 * s, bw, 26 * s), mana.Current, mana.maxMana,
                new Color(0.22f, 0.42f, 1f), "+" + mana.regenPerSecond.ToString("0.0"));
    }

    static readonly System.Collections.Generic.Dictionary<string, Texture2D> icons = new System.Collections.Generic.Dictionary<string, Texture2D>();
    public static Texture2D AbilityIcon(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!icons.TryGetValue(id, out var t)) { t = Resources.Load<Texture2D>("AbilityIcons/" + id); icons[id] = t; }
        return t;
    }

    void DrawSlot(Ability a, Rect r, bool targeting)
    {
        Color c = a.Info.color;
        Color frame = targeting ? new Color(1f, 0.9f, 0.3f) : a.IsPassive ? new Color(0.8f, 0.65f, 0.2f) : new Color(0.28f, 0.25f, 0.25f);
        Box(new Rect(r.x - 3 * s, r.y - 3 * s, r.width + 6 * s, r.height + 6 * s), frame);
        Box(r, new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f));
        var icon = AbilityIcon(a.Info.id);
        if (icon != null) GUI.DrawTexture(r, icon, ScaleMode.ScaleAndCrop);
        else GUI.Label(new Rect(r.x + 4 * s, r.y + 20 * s, r.width - 8 * s, r.height - 38 * s), string.IsNullOrEmpty(a.Info.shortName) ? a.Info.name : a.Info.shortName,
            Style(14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, true));
        if (a.Info.charges > 0 && a.IsLearned)
            GUI.Label(new Rect(r.x, r.y + 2 * s, r.width - 6 * s, 24 * s), a.Charges.ToString(),
                Style(20, FontStyle.Bold, TextAnchor.UpperRight, new Color(1f, 0.85f, 0.3f)));
        GUI.Label(new Rect(r.x + 6 * s, r.y + 2 * s, 30 * s, 24 * s), a.Info.key,
            Style(19, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.9f)));

        if (!a.IsLearned)
        {
            Box(r, new Color(0f, 0f, 0f, 0.62f));
            GUI.Label(new Rect(r.x, r.yMax - 22 * s, r.width, 20 * s), "не изучена",
                Style(12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f)));
            return;
        }

        if (a.IsPassive)
        {
            GUI.Label(new Rect(r.x, r.yMax - 22 * s, r.width, 20 * s), "пассивная",
                Style(12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f)));
            return;
        }

        GUI.Label(new Rect(r.x, r.yMax - 22 * s, r.width - 6 * s, 20 * s), a.ManaCost.ToString("0"),
            Style(14, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.55f, 0.7f, 1f)));

        if (mana != null && mana.Current < a.ManaCost) Box(r, new Color(0.1f, 0.2f, 0.6f, 0.45f));

        if (a.CooldownLeft > 0f)
        {
            float k = Mathf.Clamp01(a.CooldownLeft / Mathf.Max(0.01f, a.Info.charges > 0 ? a.Info.chargeCooldown : a.Cooldown));
            Box(new Rect(r.x, r.y + r.height * (1f - k), r.width, r.height * k), new Color(0, 0, 0, 0.65f));
            GUI.Label(r, Mathf.CeilToInt(a.CooldownLeft).ToString(), Style(32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        }
    }

    void DrawPips(Ability a, Rect r)
    {
        int m = a.MaxLevel;
        float g = 4 * s, w = (r.width - g * (m - 1)) / m;
        for (int i = 0; i < m; i++)
            Box(new Rect(r.x + i * (w + g), r.y, w, r.height), i < a.Level ? Gold : new Color(0.25f, 0.23f, 0.2f));
    }

    string AbilityTooltip(Ability a)
    {
        var info = a.Info;
        string head = $"<b>{info.name}</b> [{info.key}]   <color=#ffd84a>уровень {a.Level}/{a.MaxLevel}</color>";
        string cost = a.IsPassive ? "Пассивная" :
            info.charges > 0 ? $"Мана: {info.ManaText(a.Level)} · {info.charges} заряда, новый — раз в {info.chargeCooldown:0} сек. · Дальность: {info.castRange:0}"
            : $"Мана: {info.ManaText(a.Level)} · Перезарядка: {info.CooldownText(a.Level)} сек. · Дальность: {info.castRange:0}";
        string req = "";
        if (progress != null && a.Level < a.MaxLevel)
        {
            int need = progress.RequiredHeroLevel(a);
            req = progress.Level >= need
                ? "\n<color=#8f8>Можно изучить следующий уровень</color>"
                : $"\n<color=#f88>Следующий уровень — с {need}-го уровня героя</color>";
        }
        return $"{head}\n<color=#9db7ff>{cost}</color>\n{info.FormatDescription(a.Level)}{req}";
    }

    // ---------------- Инвентарь ----------------
    void DrawInventory(Rect r, ref string tooltip)
    {
        Box(r, PanelColor);
        Box(new Rect(r.x, r.y, r.width, 4 * s), Accent);
        if (inventory == null) return;

        float sz = 66 * s, g = 7 * s;
        float sx = r.x + 14 * s, sy = r.y + 14 * s;
        for (int i = 0; i < Inventory.TotalSlots; i++)
        {
            bool backpack = i >= Inventory.MainSlots;
            Rect sr;
            if (!backpack) sr = new Rect(sx + (i % 3) * (sz + g), sy + (i / 3) * (sz + g), sz, sz);
            else
            {
                float bs = 44 * s, by = sy + 2 * (sz + g) + 14 * s;
                int k = i - Inventory.MainSlots;
                sr = new Rect(sx + k * (sz + g) + (sz - bs) / 2f, by, bs, bs);
            }

            var item = inventory.Get(i);
            ItemSlot(sr, item, backpack, i);
            if (sr.Contains(Event.current.mousePosition)) tooltip = ItemTooltip(item, i);
            HandleSlotClick(sr, i, item);
        }
        DrawTicketSlot(new Rect(sx + 3 * (sz + g) + 8 * s, sy + (sz + g) / 2f, sz, sz), ref tooltip);
        GUI.Label(new Rect(sx, sy + 2 * (sz + g) - 1 * s, 120 * s, 14 * s), "рюкзак (не работает)",
            Style(11, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.6f)));
    }

    // Слот «Билет на электричку» — клавиша T
    void DrawTicketSlot(Rect r, ref string tooltip)
    {
        bool targeting = itemFx != null && itemFx.TpTargeting;
        Box(new Rect(r.x - 2 * s, r.y - 2 * s, r.width + 4 * s, r.height + 4 * s), targeting ? new Color(1f, 0.9f, 0.3f) : new Color(0.3f, 0.5f, 0.7f));
        Box(r, new Color(0.08f, 0.1f, 0.13f));
        var tex = ItemIcons.Get(Inventory.TicketId);
        var old = GUI.color;
        if (inventory.TpCharges <= 0) GUI.color = new Color(1, 1, 1, 0.3f);
        if (tex != null) GUI.DrawTexture(new Rect(r.x + 2 * s, r.y + 2 * s, r.width - 4 * s, r.height - 4 * s), tex, ScaleMode.ScaleAndCrop);
        else GUI.Label(r, "Билет", Style(12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        GUI.color = old;
        if (inventory.TpCooldownLeft > 0f)
        {
            float k = Mathf.Clamp01(inventory.TpCooldownLeft / ItemEffects.TicketCooldown);
            Box(new Rect(r.x, r.y + r.height * (1f - k), r.width, r.height * k), new Color(0, 0, 0, 0.65f));
            GUI.Label(r, Mathf.CeilToInt(inventory.TpCooldownLeft).ToString(), Style(22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        }
        GUI.Label(new Rect(r.x + 4 * s, r.y + 1 * s, 24 * s, 20 * s), "T", Style(15, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
        GUI.Label(new Rect(r.x, r.yMax - 22 * s, r.width - 4 * s, 20 * s), inventory.TpCharges.ToString(),
            Style(17, FontStyle.Bold, TextAnchor.LowerRight, inventory.TpCharges > 0 ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.5f, 0.5f)));
        GUI.Label(new Rect(r.x - 10 * s, r.yMax + 4 * s, r.width + 20 * s, 16 * s), "электричка", Style(11, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.6f, 0.75f, 0.9f)));

        if (r.Contains(Event.current.mousePosition))
            tooltip = $"<b>Билет на электричку</b> [T]   <color=#9db7ff>билетов: {inventory.TpCharges}</color>\n" +
                      $"Нажми T и кликни рядом со своей башней (или по мини-карте) — через {ItemEffects.TicketChannel:0} сек. окажешься там. " +
                      $"Двойное T — сразу в баню. Перезарядка {ItemEffects.TicketCooldown:0} сек.\n" +
                      "<color=#aaaaaa>Есть у всех с начала игры. Если билет кончился — после смерти купится сам (100 золота).</color>";

        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition) && itemFx != null)
        {
            itemFx.PressTicket();
            e.Use();
        }
    }

    void HandleSlotClick(Rect r, int slot, ItemInstance item)
    {
        var e = Event.current;
        if (e.type != EventType.MouseDown || !r.Contains(e.mousePosition)) return;

        if (e.button == 1)
        {
            // ПКМ — продать (только у бани)
            if (item != null && shop != null && progress != null)
            {
                if (shop.NearShop())
                {
                    int price = inventory.SellPrice(slot);
                    inventory.Sell(slot, progress);
                    FloatingText.Show(transform.position + Vector3.up * 3.5f, $"Продано +{price}", Gold, 22);
                }
                else FloatingText.Show(transform.position + Vector3.up * 3.5f, "Продать можно у своей бани", new Color(1f, 0.6f, 0.6f), 20);
            }
            e.Use();
            return;
        }
        if (e.button != 0) return;

        if (inventory.SelectedSlot >= 0)
        {
            // Второй клик — переставить предметы
            inventory.Swap(inventory.SelectedSlot, slot);
            inventory.SelectedSlot = -1;
        }
        else if (item != null)
        {
            bool canUse = slot < Inventory.MainSlots && item.def.active != ItemActive.None && !e.shift;
            if (canUse && itemFx != null) itemFx.Use(slot);
            else inventory.SelectedSlot = slot; // выбрать для перестановки
        }
        e.Use();
    }

    string ItemTooltip(ItemInstance item, int slot)
    {
        if (item == null)
            return slot < Inventory.MainSlots
                ? "Пустой слот\n<color=#aaa>Покупай предметы в магазине (B)</color>"
                : "Рюкзак\n<color=#aaa>Предметы здесь хранятся, но не дают бонусов</color>";
        var d = item.def;
        string key = slot < Inventory.MainSlots ? $" [{slot + 1}]" : " (в рюкзаке)";
        string hint = "\n<color=#aaa>ЛКМ — применить/выбрать · Shift+ЛКМ — переставить · ПКМ у бани — продать за " +
                      inventory.SellPrice(slot) + "</color>";
        return $"<b>{d.name}</b>{key}\n{ItemCatalog.DescribeStats(d)}{hint}";
    }

    void ItemSlot(Rect r, ItemInstance item, bool dim, int slot)
    {
        bool sel = inventory.SelectedSlot == slot;
        Box(new Rect(r.x - 2 * s, r.y - 2 * s, r.width + 4 * s, r.height + 4 * s), sel ? Gold : new Color(0.22f, 0.2f, 0.2f));
        Box(r, dim ? new Color(0.07f, 0.07f, 0.08f) : new Color(0.1f, 0.1f, 0.11f));

        if (item != null)
        {
            var c = item.def.color;
            var tex = ItemIcons.Get(item.def.id);
            if (tex != null)
            {
                var old = GUI.color;
                if (dim) GUI.color = new Color(1, 1, 1, 0.55f);
                GUI.DrawTexture(new Rect(r.x + 2 * s, r.y + 2 * s, r.width - 4 * s, r.height - 4 * s), tex, ScaleMode.ScaleAndCrop);
                GUI.color = old;
            }
            else
            {
                Box(new Rect(r.x + 3 * s, r.y + 3 * s, r.width - 6 * s, r.height - 6 * s),
                    new Color(c.r * 0.55f + 0.05f, c.g * 0.55f + 0.05f, c.b * 0.55f + 0.05f, dim ? 0.6f : 1f));
                Box(new Rect(r.x + r.width * 0.3f, r.y + r.height * 0.15f, r.width * 0.4f, r.height * 0.28f), c);
                GUI.Label(new Rect(r.x + 2 * s, r.y + r.height * 0.42f, r.width - 4 * s, r.height * 0.56f), item.def.name,
                    Style(dim ? 9 : 11, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, true));
            }

            // заряды (Бутылка кваса)
            if (item.def.maxCharges > 0)
                GUI.Label(new Rect(r.x, r.y + 2 * s, r.width - 4 * s, 20 * s), item.charges.ToString(),
                    Style(15, FontStyle.Bold, TextAnchor.UpperRight, item.charges > 0 ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.4f, 0.35f)));
            if (item.cooldownLeft > 0f && item.def.activeCooldown > 0f)
            {
                float k = item.cooldownLeft / item.def.activeCooldown;
                Box(new Rect(r.x, r.y + r.height * (1f - k), r.width, r.height * k), new Color(0, 0, 0, 0.65f));
                GUI.Label(r, Mathf.CeilToInt(item.cooldownLeft).ToString(), Style(22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            }
            if (!dim && item.def.active != ItemActive.None)
                Box(new Rect(r.xMax - 10 * s, r.y + 3 * s, 7 * s, 7 * s), new Color(1f, 0.85f, 0.3f));
        }
        if (slot < Inventory.MainSlots)
            GUI.Label(new Rect(r.x + 4 * s, r.yMax - 18 * s, 20 * s, 16 * s), (slot + 1).ToString(),
                Style(12, FontStyle.Bold, TextAnchor.LowerLeft, new Color(1, 1, 1, 0.6f)));
    }

    // ---------------- Золото ----------------
    void DrawGold()
    {
        if (progress == null) return;
        var r = new Rect(Screen.width - 240 * s, Screen.height - 74 * s, 220 * s, 58 * s);
        HudArea.Add(r);
        Box(new Rect(r.x - 3 * s, r.y - 3 * s, r.width + 6 * s, r.height + 6 * s), new Color(0.45f, 0.32f, 0.05f));
        Box(r, new Color(0.85f, 0.6f, 0.1f));
        GUI.Label(new Rect(r.x + 14 * s, r.y, r.width - 70 * s, r.height), progress.Gold.ToString(),
            Style(30, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
        // «Монетки»
        for (int i = 0; i < 3; i++)
        {
            var c = new Rect(r.xMax - 56 * s + i * 6 * s, r.y + 14 * s - i * 4 * s, 30 * s, 30 * s);
            Box(c, new Color(1f, 0.85f, 0.3f));
            Box(new Rect(c.x + 4 * s, c.y + 4 * s, c.width - 8 * s, c.height - 8 * s), new Color(0.9f, 0.65f, 0.12f));
        }
        GUI.Label(new Rect(r.x, r.y - 22 * s, r.width, 20 * s), "ЗОЛОТО", Style(13, FontStyle.Bold, TextAnchor.MiddleLeft, Gold));
    }

    // ---------------- Подсказки ----------------
    void DrawTooltip(string text, Rect anchor)
    {
        float w = 560 * s;
        var st = Style(17, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.92f, 0.9f, 0.9f), true);
        st.richText = true;
        float h = st.CalcHeight(new GUIContent(text), w - 28 * s) + 24 * s;
        var r = new Rect(anchor.center.x - w / 2f, anchor.y - h - 90 * s, w, h);
        Box(r, new Color(0.03f, 0.03f, 0.04f, 0.96f));
        Box(new Rect(r.x, r.y, r.width, 3 * s), Accent);
        GUI.Label(new Rect(r.x + 14 * s, r.y + 10 * s, r.width - 28 * s, r.height - 20 * s), text, st);
    }

    void DrawTeleportBar()
    {
        if (itemFx != null && itemFx.TpTargeting)
        {
            Vector2 m = Event.current.mousePosition;
            var hr = new Rect(m.x + 18 * s, m.y + 10 * s, 330 * s, 26 * s);
            Box(hr, new Color(0, 0, 0, 0.75f));
            GUI.Label(hr, "Куда едем? Клик у своей башни · ПКМ — отмена", Style(14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.6f, 0.85f, 1f)));
        }
        if (itemFx == null || !itemFx.IsTeleporting) return;
        var r = new Rect(Screen.width / 2f - 200 * s, Screen.height - 330 * s, 400 * s, 26 * s);
        Box(r, new Color(0, 0, 0, 0.7f));
        Box(new Rect(r.x, r.y, r.width * itemFx.TeleportProgress, r.height), new Color(0.4f, 0.7f, 1f));
        GUI.Label(r, itemFx.TeleportLabel, Style(16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
    }

    void DrawTargetingHint()
    {
        if (caster.TargetingIndex < 0) return;
        var a = caster.Abilities[caster.TargetingIndex];
        Vector2 m = Event.current.mousePosition;
        string extra = a.Target == AbilityTarget.AllyUnit ? $"\n{a.Info.key} ещё раз — на себя" : "";
        GUI.Label(new Rect(m.x + 24 * s, m.y + 10 * s, 520 * s, 80 * s),
            $"{a.Info.name}: выбери цель (ЛКМ), ПКМ — отмена{extra}",
            Style(19, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.4f)));
    }

    // ---------------- Помощники ----------------
    void Bar(Rect r, float cur, float max, Color color, string regen)
    {
        Box(r, new Color(0.12f, 0.12f, 0.12f));
        Box(new Rect(r.x, r.y, r.width * Mathf.Clamp01(cur / Mathf.Max(1f, max)), r.height), color);
        Box(new Rect(r.x, r.y, r.width * Mathf.Clamp01(cur / Mathf.Max(1f, max)), r.height * 0.35f), new Color(1, 1, 1, 0.12f));
        GUI.Label(r, $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}",
            Style(r.height / s * 0.65f, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        GUI.Label(new Rect(r.x, r.y, r.width - 8 * s, r.height), regen,
            Style(r.height / s * 0.55f, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.85f, 1f, 0.85f, 0.9f)));
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
            clipping = TextClipping.Overflow
        };
        st.normal.textColor = color;
        return st;
    }
}
