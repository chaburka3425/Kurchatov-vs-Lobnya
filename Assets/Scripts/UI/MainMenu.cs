using UnityEngine;
using UnityEngine.SceneManagement;

// Главное меню в стиле MOBA-клиента: верхняя панель с вкладками,
// профиль, новости, большая кнопка «ИГРАТЬ» с поиском игры.
// Вкладки «Герои», «Арсенал» и др. пока заглушки — наполним позже.
public class MainMenu : MonoBehaviour
{
    public string title = "МПКА 2";
    public string subtitle = "Курчатов  vs  Москва";
    public string gameSceneName = "KurchatovVsLobnya";
    public Texture2D leftPortrait;   // Курчатов
    public Texture2D rightPortrait;  // Москва
    public string playerName = "Игрок";
    public Texture2D ogrFaceLeft;    // лица героя Огр Mog
    public Texture2D ogrFaceRight;

    enum Tab { Home, Heroes, Arsenal, Learn, Settings }
    static readonly string[] TabNames = { "ГЛАВНАЯ", "ГЕРОИ", "АРСЕНАЛ", "ОБУЧЕНИЕ", "НАСТРОЙКИ" };
    Tab tab = Tab.Home;

    enum Search { Idle, Searching, Found }
    Search search = Search.Idle;
    float searchStart, foundAt, searchDuration;

    // Стили и текстуры
    GUIStyle label, bold, titleStyle, outlineStyle, tabStyle, tabActive, playStyle, smallBtn, panelTitle, center;
    Texture2D bg, topBar, panel, red, redHover, redDark, darkBtn, darkBtnHover, line, locked;
    float s; // масштаб под экран

    static readonly Color Accent = new Color(0.9f, 0.12f, 0.1f);

    void Start() => Time.timeScale = 1f;

    void Update()
    {
        if (search == Search.Searching && Time.unscaledTime - searchStart >= searchDuration)
        {
            search = Search.Found;
            foundAt = Time.unscaledTime;
        }
        if (search == Search.Found && Time.unscaledTime - foundAt > 10f)
            search = Search.Idle; // не успел принять
    }

    void OnGUI()
    {
        if (label == null) InitStyles();
        s = Screen.height / 1080f;
        ApplyFontSizes();

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bg, ScaleMode.StretchToFill);

        DrawTopBar();

        var content = new Rect(40 * s, 110 * s, Screen.width - 80 * s, Screen.height - 150 * s);
        switch (tab)
        {
            case Tab.Home: DrawHome(content); break;
            case Tab.Heroes: DrawHeroes(content); break;
            case Tab.Arsenal: DrawArsenal(content); break;
            case Tab.Learn: DrawLearn(content); break;
            case Tab.Settings: DrawSettings(content); break;
        }

        DrawPlayButton();
        if (search == Search.Found) DrawMatchFound();
    }

    // ---------------- Верхняя панель ----------------
    void DrawTopBar()
    {
        float h = 80 * s;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, h), topBar);
        GUI.DrawTexture(new Rect(0, h - 3 * s, Screen.width, 3 * s), line);

        // Логотип
        var logo = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(40 * s), alignment = TextAnchor.MiddleLeft };
        logo.normal.textColor = Accent;
        GUI.Label(new Rect(30 * s, 0, 220 * s, h), title, logo);

        // Вкладки
        float x = 260 * s;
        for (int i = 0; i < TabNames.Length; i++)
        {
            float w = 190 * s;
            var r = new Rect(x, 0, w, h - 3 * s);
            bool active = (int)tab == i;
            if (GUI.Button(r, TabNames[i], active ? tabActive : tabStyle)) tab = (Tab)i;
            if (active) GUI.DrawTexture(new Rect(r.x + 20 * s, h - 9 * s, w - 40 * s, 6 * s), red);
            x += w;
        }

        // Профиль справа
        float px = Screen.width - 430 * s;
        if (leftPortrait != null)
        {
            GUI.DrawTexture(new Rect(px - 3 * s, 9 * s, 66 * s, 66 * s), line);
            GUI.DrawTexture(new Rect(px, 12 * s, 60 * s, 60 * s), leftPortrait, ScaleMode.ScaleAndCrop);
        }
        GUI.Label(new Rect(px + 75 * s, 12 * s, 200 * s, 32 * s), playerName, bold);
        var lvl = new GUIStyle(label);
        lvl.normal.textColor = new Color(1f, 0.75f, 0.3f);
        GUI.Label(new Rect(px + 75 * s, 42 * s, 200 * s, 28 * s), "Уровень 1", lvl);

        if (GUI.Button(new Rect(Screen.width - 140 * s, 18 * s, 115 * s, 44 * s), "ВЫХОД", smallBtn)) Quit();
    }

    // ---------------- Главная ----------------
    void DrawHome(Rect c)
    {
        // Большой заголовок
        int size = Mathf.RoundToInt(200 * s * (1f + 0.025f * Mathf.Sin(Time.unscaledTime * 2.5f)));
        titleStyle.fontSize = outlineStyle.fontSize = size;
        var tr = new Rect(c.x, c.y + 10 * s, c.width * 0.55f, 260 * s);
        float o = Mathf.Max(2f, 6f * s);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0) GUI.Label(Offset(tr, dx * o, dy * o), title, outlineStyle);
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.Label(Offset(tr, 12 * s, 12 * s), title, outlineStyle);
        GUI.color = Color.white;
        float glow = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f);
        titleStyle.normal.textColor = new Color(Mathf.Min(1f, glow + 0.15f), 0.08f, 0.08f);
        GUI.Label(tr, title, titleStyle);

        var sub = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(44 * s), alignment = TextAnchor.MiddleCenter };
        sub.normal.textColor = new Color(1f, 0.85f, 0.8f);
        GUI.Label(new Rect(tr.x, tr.yMax - 20 * s, tr.width, 60 * s), subtitle, sub);

        // Новости
        var news = new Rect(c.x, tr.yMax + 50 * s, c.width * 0.45f, Mathf.Min(430 * s, Screen.height - tr.yMax - 90 * s));
        Panel(news, "НОВОСТИ");
        GUI.Label(new Rect(news.x + 20 * s, news.y + 60 * s, news.width - 40 * s, news.height - 70 * s),
            "<b>Обновление 0.7</b>\n" +
            "• <b>5 на 5:</b> 4 союзных бота и 5 вражеских, у каждого своя линия\n" +
            "• <b>Туман войны:</b> видно только то, что рядом с твоей командой\n" +
            "• Сверху — счёт убийств, портреты и таймеры воскрешения\n" +
            "• Новый герой: <b>Саня Финд</b> — сосиски, колбасные койлы и реквием\n" +
            "• <b>Лобби как в Доте:</b> выбор героя → выбор линии → стартовый закуп\n" +
            "• <b>Три линии</b> (верх, мид, низ) и <b>лес</b> с лагерями гопников\n" +
            "• <b>Глиф</b> (G): строения и крипы неуязвимы 5 сек, перезарядка 2 мин\n" +
            "• Вторые башни намного крепче; трон открывается после любой второй башни", label);

        // Карточки городов
        float cw = 300 * s, ch = 380 * s;
        float cx = c.x + c.width * 0.55f, cy = c.y + 40 * s;
        CityCard(new Rect(cx, cy, cw, ch), leftPortrait, "КУРЧАТОВ", TeamUtil.GetColor(Team.Kurchatov));
        var vs = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(56 * s), alignment = TextAnchor.MiddleCenter };
        vs.normal.textColor = Color.white;
        GUI.Label(new Rect(cx + cw, cy + ch / 2 - 40 * s, 110 * s, 80 * s), "VS", vs);
        CityCard(new Rect(cx + cw + 110 * s, cy, cw, ch), rightPortrait, "МОСКВА", TeamUtil.GetColor(Team.Lobnya));
    }

    void CityCard(Rect r, Texture2D tex, string name, Color color)
    {
        float b = 6 * s;
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x - b, r.y - b, r.width + 2 * b, r.height + 2 * b), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleAndCrop);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 60 * s, r.width, 60 * s), panel);
        var st = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(34 * s), alignment = TextAnchor.MiddleCenter };
        st.normal.textColor = color;
        GUI.Label(new Rect(r.x, r.yMax - 60 * s, r.width, 60 * s), name, st);
    }

    // ---------------- Герои ----------------
    string selectedHeroId = "ogr_mog";
    Vector2 heroScroll;

    void DrawHeroes(Rect c)
    {
        PageTitle(c, "ГЕРОИ", "Нажми на героя, чтобы посмотреть способности");
        float gridW = c.width * 0.56f, gap = 18 * s;
        float colW = (gridW - 3 * gap) / 4f;
        float top = c.y + 125 * s;
        float cardH = 190 * s;

        for (int a = 0; a < AttributeUtil.All.Length; a++)
        {
            var attr = AttributeUtil.All[a];
            float x = c.x + a * (colW + gap);
            var hc = AttributeUtil.GetColor(attr);

            GUI.color = hc;
            GUI.DrawTexture(new Rect(x, top, colW, 6 * s), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var hs = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(26 * s), alignment = TextAnchor.MiddleCenter };
            hs.normal.textColor = hc;
            GUI.Label(new Rect(x, top + 8 * s, colW, 40 * s), AttributeUtil.GetName(attr), hs);

            float y = top + 56 * s;
            int count = 0;
            foreach (var h in HeroCatalog.All)
            {
                if (h.attribute != attr) continue;
                if (HeroCard(new Rect(x, y, colW, cardH), h)) { if (selectedHeroId != h.id) VoiceLines.Speak(h.id, Vector3.zero, 1f, true, -1, h.deathVoice, true); selectedHeroId = h.id; heroScroll = Vector2.zero; }
                y += cardH + gap;
                count++;
            }
            for (int k = count; k < 2; k++)
            {
                var r = new Rect(x, y, colW, cardH);
                if (r.yMax > Screen.height - 40 * s) break;
                GUI.DrawTexture(r, locked);
                GUI.Label(r, "?\n\nСКОРО", center);
                y += cardH + gap;
            }
        }

        var detail = new Rect(c.x + gridW + 40 * s, top, c.width - gridW - 40 * s, Screen.height - top - 230 * s);
        DrawHeroDetails(detail, HeroCatalog.Get(selectedHeroId));
    }

    bool HeroCard(Rect r, HeroDefinition h)
    {
        bool selected = h.id == selectedHeroId;
        GUI.color = selected ? AttributeUtil.GetColor(h.attribute) : new Color(0.25f, 0.22f, 0.22f);
        GUI.DrawTexture(new Rect(r.x - 3 * s, r.y - 3 * s, r.width + 6 * s, r.height + 6 * s), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(r, panel);

        var img = new Rect(r.x, r.y, r.width, r.height - 50 * s);
        var solo = Resources.Load<Texture2D>("Portraits/" + h.id);
        if (solo != null)
        {
            GUI.DrawTexture(new Rect(img.x + 6 * s, img.y + 6 * s, img.width - 12 * s, img.height - 6 * s), solo, ScaleMode.ScaleAndCrop);
        }
        else if (h.id == "ogr_mog" && ogrFaceLeft != null && ogrFaceRight != null)
        {
            float fw = Mathf.Min(img.height * 0.9f, img.width / 2f + 6 * s);
            GUI.DrawTexture(new Rect(img.center.x - fw + 6 * s, img.y + 8 * s, fw, fw), ogrFaceLeft, ScaleMode.ScaleToFit);
            GUI.DrawTexture(new Rect(img.center.x - 6 * s, img.y + 8 * s, fw, fw), ogrFaceRight, ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.color = h.bodyColor;
            GUI.DrawTexture(img, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        GUI.Label(new Rect(r.x, r.yMax - 50 * s, r.width, 50 * s), h.name, center);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    void DrawHeroDetails(Rect p, HeroDefinition h)
    {
        Panel(p, h.name.ToUpper());
        var inner = new Rect(p.x + 20 * s, p.y + 55 * s, p.width - 40 * s, p.height - 65 * s);

        string attrHex = ColorUtility.ToHtmlStringRGB(AttributeUtil.GetColor(h.attribute));
        var sb = new System.Text.StringBuilder();
        sb.Append($"<i>{h.title}</i>\n");
        sb.Append($"<color=#{attrHex}><b>Главный атрибут: {AttributeUtil.GetName(h.attribute)}</b></color>\n\n");
        sb.Append($"<color=#ff7a66>Сила {h.baseStr:0.#} (+{h.strGain:0.#})</color> · ");
        sb.Append($"<color=#6fe07a>Ловкость {h.baseAgi:0.#} (+{h.agiGain:0.#})</color> · ");
        sb.Append($"<color=#6fa8ff>Интеллект {h.baseInt:0.#} (+{h.intGain:0.#})</color>\n");
        sb.Append($"Здоровье {h.MaxHealthAt(1):0} · Мана {h.MaxManaAt(1):0} · Урон {h.DamageAt(1):0} · " +
                  $"Броня {h.ArmorAt(1):0.#} · Скорость {h.moveSpeed:0.#}\n\n");
        sb.Append($"{(h.ranged ? "Дальний бой" : "Ближний бой")}\n\n");
        if (!string.IsNullOrEmpty(h.innate)) sb.Append($"<b><color=#ffb07a>ВРОЖДЁННАЯ</color></b>\n{h.innate}\n\n");
        sb.Append("<b><color=#ff9980>СПОСОБНОСТИ</color></b>\n");
        foreach (var a in h.abilities)
        {
            string cost = a.target == AbilityTarget.Passive
                ? "пассивная"
                : a.charges > 0 ? $"мана {a.ManaText(0)} · {a.charges} заряда, новый — раз в {a.chargeCooldown:0} сек."
                : $"мана {a.ManaText(0)} · перезарядка {a.CooldownText(0)} сек.";
            string hex = ColorUtility.ToHtmlStringRGB(a.color);
            sb.Append($"<color=#{hex}><b>[{a.key}] {a.name}</b></color>  <color=#9db7ff>{cost}</color>\n");
            string maxLvl = a.isUltimate ? " (ульта: уровни 6/12/18)" : "";
            sb.Append($"{a.FormattedDescription}<color=#aaaaaa>{maxLvl}</color>\n\n");
        }
        sb.Append("<b><color=#ff9980>ИСТОРИЯ</color></b>\n");
        sb.Append(h.lore);

        GUILayout.BeginArea(inner);
        heroScroll = GUILayout.BeginScrollView(heroScroll);
        GUILayout.Label(sb.ToString(), label);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // ---------------- Арсенал ----------------
    ItemDef arsenalSelected;

    void DrawArsenal(Rect c)
    {
        PageTitle(c, "АРСЕНАЛ", "Все предметы магазина «Пивная баня». Нажми на предмет — увидишь, из чего он собирается.");
        float sz = 92 * s, gap = 10 * s;
        float gridW = c.width * 0.6f;
        int cols = Mathf.Max(1, Mathf.FloorToInt((gridW + gap) / (sz + gap)));
        float y = c.y + 120 * s;

        foreach (ItemCategory cat in System.Enum.GetValues(typeof(ItemCategory)))
        {
            var items = ItemCatalog.InCategory(cat);
            GUI.Label(new Rect(c.x, y, gridW, 30 * s), ItemCatalog.CategoryName(cat), panelTitle);
            y += 32 * s;
            for (int i = 0; i < items.Count; i++)
            {
                var r = new Rect(c.x + (i % cols) * (sz + gap), y + (i / cols) * (sz + gap), sz, sz);
                var d = items[i];
                GUI.color = arsenalSelected == d ? new Color(1f, 0.78f, 0.2f) : new Color(0.25f, 0.22f, 0.22f);
                GUI.DrawTexture(new Rect(r.x - 2 * s, r.y - 2 * s, r.width + 4 * s, r.height + 4 * s), Texture2D.whiteTexture);
                GUI.color = new Color(d.color.r * 0.55f + 0.05f, d.color.g * 0.55f + 0.05f, d.color.b * 0.55f + 0.05f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = Color.white;
                var st = new GUIStyle(center) { fontSize = Mathf.RoundToInt(13 * s) };
                GUI.Label(new Rect(r.x + 2 * s, r.y, r.width - 4 * s, r.height - 20 * s), d.name, st);
                var cs = new GUIStyle(center) { fontSize = Mathf.RoundToInt(14 * s) };
                cs.normal.textColor = new Color(1f, 0.78f, 0.2f);
                GUI.Label(new Rect(r.x, r.yMax - 22 * s, r.width, 20 * s), ItemCatalog.TotalCost(d).ToString(), cs);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) arsenalSelected = d;
            }
            y += Mathf.Ceil(items.Count / (float)cols) * (sz + gap) + 8 * s;
        }

        if (arsenalSelected != null)
        {
            var d = arsenalSelected;
            var p = new Rect(c.x + gridW + 30 * s, c.y + 120 * s, c.width - gridW - 30 * s, Screen.height - c.y - 360 * s);
            Panel(p, d.name.ToUpper());
            var sb = new System.Text.StringBuilder();
            sb.Append($"<color=#ffc83a><b>Цена: {ItemCatalog.TotalCost(d)}</b></color>\n\n");
            sb.Append(ItemCatalog.DescribeStats(d)).Append("\n\n");
            if (d.IsUpgrade)
            {
                sb.Append("<b><color=#ff9980>Собирается из:</color></b>\n");
                foreach (var id in d.components)
                {
                    var comp = ItemCatalog.Get(id);
                    if (comp != null) sb.Append($"• {comp.name} ({ItemCatalog.TotalCost(comp)})\n");
                }
                if (d.HasRecipe) sb.Append($"• Рецепт ({d.cost})\n");
                sb.Append("\n");
            }
            var used = ItemCatalog.UsedIn(d.id);
            if (used.Count > 0)
            {
                sb.Append("<b><color=#ff9980>Используется в:</color></b>\n");
                foreach (var u in used) sb.Append($"• {u.name}\n");
                sb.Append("\n");
            }
            sb.Append($"<i>{d.lore}</i>");
            GUI.Label(new Rect(p.x + 20 * s, p.y + 55 * s, p.width - 40 * s, p.height - 65 * s), sb.ToString(), label);
        }
    }

    // ---------------- Обучение ----------------
    void DrawLearn(Rect c)
    {
        PageTitle(c, "ОБУЧЕНИЕ", "Как играть");
        var p = new Rect(c.x, c.y + 120 * s, c.width * 0.5f, 520 * s);
        Panel(p, "УПРАВЛЕНИЕ");
        GUI.Label(new Rect(p.x + 20 * s, p.y + 60 * s, p.width - 40 * s, p.height - 70 * s),
            "<b>ПКМ по земле</b> — идти\n" +
            "<b>ПКМ по врагу</b> — атаковать\n" +
            "<b>S</b> — стоп\n" +
            "<b>Стрелки / мышь у края</b> — камера\n" +
            "<b>Колёсико</b> — зум\n" +
            "<b>Пробел</b> — камера к герою\n" +
            "<b>Esc</b> — пауза\n" +
            "<b>Q W E R</b> — способности, <b>Ctrl+Q/W/E/R</b> — прокачать\n" +
            "<b>B</b> — магазин, <b>1–6</b> — предметы, <b>G</b> — глиф\n\n" +
            "<b>Цель:</b> разрушить трон врага.\n" +
            "На каждой линии сначала башня 1, потом башня 2. Трон открывается, когда сломана любая вторая башня.\n" +
            "Гопники в лесу дерутся, только если их ударить.", label);
    }

    // ---------------- Настройки ----------------
    void DrawSettings(Rect c)
    {
        PageTitle(c, "НАСТРОЙКИ", "");
        var p = new Rect(c.x, c.y + 120 * s, 820 * s, 520 * s);
        Panel(p, "ИЗОБРАЖЕНИЕ, ЗВУК И ИГРА");
        float y = p.y + 75 * s, lx = p.x + 30 * s, vx = p.x + 330 * s;

        GUI.Label(new Rect(lx, y, 300 * s, 40 * s), "Полный экран", bold);
        if (GUI.Button(new Rect(vx, y, 200 * s, 44 * s), Screen.fullScreen ? "ВКЛ" : "ВЫКЛ", smallBtn))
            Screen.fullScreen = !Screen.fullScreen;

        y += 80 * s;
        GUI.Label(new Rect(lx, y, 300 * s, 40 * s), "Качество графики", bold);
        var names = QualitySettings.names;
        int q = QualitySettings.GetQualityLevel();
        if (GUI.Button(new Rect(vx, y, 50 * s, 44 * s), "<", smallBtn)) QualitySettings.SetQualityLevel(Mathf.Max(0, q - 1));
        GUI.Label(new Rect(vx + 55 * s, y, 220 * s, 44 * s), names.Length > 0 ? names[q] : "-", center);
        if (GUI.Button(new Rect(vx + 280 * s, y, 50 * s, 44 * s), ">", smallBtn)) QualitySettings.SetQualityLevel(Mathf.Min(names.Length - 1, q + 1));

        y += 80 * s;
        GUI.Label(new Rect(lx, y, 300 * s, 40 * s), $"Громкость: {Mathf.RoundToInt(AudioListener.volume * 100)}%", bold);
        AudioListener.volume = GUI.HorizontalSlider(new Rect(vx, y + 15 * s, 330 * s, 30 * s), AudioListener.volume, 0f, 1f);

        y += 80 * s;
        GUI.Label(new Rect(lx, y, 300 * s, 40 * s), "Голоса героев", bold);
        bool voice = VoiceLines.Enabled;
        if (GUI.Button(new Rect(vx, y, 200 * s, 44 * s), voice ? "ВКЛ" : "ВЫКЛ", smallBtn))
            PlayerPrefs.SetInt(VoiceLines.PrefKey, voice ? 0 : 1);

        y += 80 * s;
        GUI.Label(new Rect(lx, y, 300 * s, 40 * s), "Сложность бота", bold);
        string[] diffs = { "ЛЁГКИЙ", "ОБЫЧНЫЙ", "СЛОЖНЫЙ" };
        int d = PlayerPrefs.GetInt(BotHeroAI.DiffKey, 1);
        for (int i = 0; i < 3; i++)
        {
            var dr = new Rect(vx - 40 * s + i * 150 * s, y, 144 * s, 44 * s);
            if (i == d) { GUI.color = new Color(1f, 0.35f, 0.25f); GUI.DrawTexture(new Rect(dr.x - 3 * s, dr.y - 3 * s, dr.width + 6 * s, dr.height + 6 * s), Texture2D.whiteTexture); GUI.color = Color.white; }
            if (GUI.Button(dr, diffs[i], smallBtn)) PlayerPrefs.SetInt(BotHeroAI.DiffKey, i);
        }
    }

    // ---------------- Кнопка «Играть» и поиск ----------------
    void DrawPlayButton()
    {
        float w = 420 * s, h = 110 * s;
        var r = new Rect(Screen.width - w - 40 * s, Screen.height - h - 40 * s, w, h);

        var mode = new GUIStyle(label) { alignment = TextAnchor.MiddleRight, wordWrap = false, clipping = TextClipping.Overflow };
        mode.fontSize = Mathf.RoundToInt(20 * s);
        GUI.Label(new Rect(r.x - 200 * s, r.y - 38 * s, r.width + 200 * s, 32 * s), "Режим: против ботов · команда случайная", mode);

        if (search == Search.Idle)
        {
            if (GUI.Button(r, "ИГРАТЬ", playStyle))
            {
                search = Search.Searching;
                searchStart = Time.unscaledTime;
                searchDuration = Random.Range(2f, 4f);
            }
        }
        else
        {
            GUI.DrawTexture(r, redDark);
            int secs = Mathf.FloorToInt(Time.unscaledTime - searchStart);
            var st = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(34 * s), alignment = TextAnchor.MiddleCenter };
            st.normal.textColor = Color.white;
            string dots = new string('.', 1 + (int)(Time.unscaledTime * 2) % 3);
            GUI.Label(new Rect(r.x, r.y, r.width, r.height * 0.6f), "ПОИСК ИГРЫ" + dots, st);
            GUI.Label(new Rect(r.x, r.y + r.height * 0.5f, r.width, r.height * 0.5f), $"{secs / 60}:{secs % 60:00}", center);
            if (search == Search.Searching &&
                GUI.Button(new Rect(r.x - 70 * s, r.y + r.height / 2 - 25 * s, 60 * s, 50 * s), "X", smallBtn))
                search = Search.Idle;
        }
    }

    void DrawMatchFound()
    {
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = 640 * s, h = 360 * s;
        var r = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
        GUI.DrawTexture(r, panel);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 6 * s), red);

        var st = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(56 * s), alignment = TextAnchor.MiddleCenter };
        st.normal.textColor = Color.white;
        GUI.Label(new Rect(r.x, r.y + 30 * s, r.width, 80 * s), "ИГРА НАЙДЕНА!", st);

        float left = Mathf.Max(0f, 10f - (Time.unscaledTime - foundAt));
        GUI.DrawTexture(new Rect(r.x + 60 * s, r.y + 130 * s, (r.width - 120 * s) * (left / 10f), 10 * s), red);

        if (GUI.Button(new Rect(r.x + 60 * s, r.y + 190 * s, 300 * s, 100 * s), "ПРИНЯТЬ", playStyle))
            SceneManager.LoadScene(gameSceneName);
        if (GUI.Button(new Rect(r.x + 380 * s, r.y + 210 * s, 200 * s, 60 * s), "ОТКЛОНИТЬ", smallBtn))
            search = Search.Idle;
    }

    // ---------------- Вспомогательное ----------------
    void PageTitle(Rect c, string text, string hint)
    {
        var st = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(64 * s), alignment = TextAnchor.UpperLeft };
        st.normal.textColor = Color.white;
        GUI.Label(new Rect(c.x, c.y, c.width, 80 * s), text, st);
        if (!string.IsNullOrEmpty(hint))
        {
            var h = new GUIStyle(label);
            h.normal.textColor = new Color(0.75f, 0.7f, 0.7f);
            GUI.Label(new Rect(c.x, c.y + 75 * s, c.width, 36 * s), hint, h);
        }
    }

    void Panel(Rect r, string header)
    {
        GUI.DrawTexture(r, panel);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 4 * s), red);
        GUI.Label(new Rect(r.x + 20 * s, r.y + 12 * s, r.width, 40 * s), header, panelTitle);
    }

    static Rect Offset(Rect r, float dx, float dy) => new Rect(r.x + dx, r.y + dy, r.width, r.height);

    void ApplyFontSizes()
    {
        label.fontSize = Mathf.RoundToInt(26 * s);
        bold.fontSize = Mathf.RoundToInt(28 * s);
        center.fontSize = Mathf.RoundToInt(26 * s);
        tabStyle.fontSize = tabActive.fontSize = Mathf.RoundToInt(26 * s);
        playStyle.fontSize = Mathf.RoundToInt(56 * s);
        smallBtn.fontSize = Mathf.RoundToInt(22 * s);
        panelTitle.fontSize = Mathf.RoundToInt(26 * s);
    }

    void InitStyles()
    {
        bg = Gradient(new Color(0.22f, 0.03f, 0.03f), new Color(0.03f, 0.02f, 0.03f));
        topBar = Solid(new Color(0.05f, 0.04f, 0.05f, 0.95f));
        panel = Solid(new Color(0.07f, 0.06f, 0.07f, 0.85f));
        locked = Solid(new Color(0.12f, 0.11f, 0.12f, 0.9f));
        red = Solid(Accent);
        redHover = Solid(new Color(1f, 0.25f, 0.18f));
        redDark = Solid(new Color(0.45f, 0.05f, 0.05f));
        darkBtn = Solid(new Color(0.18f, 0.16f, 0.17f));
        darkBtnHover = Solid(new Color(0.3f, 0.26f, 0.27f));
        line = Solid(new Color(0.55f, 0.08f, 0.06f));
        var clear = Solid(new Color(0, 0, 0, 0));
        var tabHover = Solid(new Color(1, 1, 1, 0.06f));

        label = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        label.normal.textColor = new Color(0.9f, 0.88f, 0.88f);
        bold = new GUIStyle(label) { fontStyle = FontStyle.Bold, wordWrap = false };
        bold.normal.textColor = Color.white;
        center = new GUIStyle(bold) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        panelTitle = new GUIStyle(bold);
        panelTitle.normal.textColor = new Color(1f, 0.6f, 0.5f);

        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        outlineStyle = new GUIStyle(titleStyle);
        outlineStyle.normal.textColor = new Color(0.1f, 0f, 0f);

        tabStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        tabStyle.normal.background = clear;
        tabStyle.hover.background = tabHover;
        tabStyle.active.background = tabHover;
        tabStyle.normal.textColor = new Color(0.65f, 0.62f, 0.62f);
        tabStyle.hover.textColor = tabStyle.active.textColor = Color.white;
        tabActive = new GUIStyle(tabStyle);
        tabActive.normal.textColor = Color.white;

        playStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        playStyle.normal.background = red;
        playStyle.hover.background = redHover;
        playStyle.active.background = redDark;
        playStyle.normal.textColor = playStyle.hover.textColor = playStyle.active.textColor = Color.white;

        smallBtn = new GUIStyle(playStyle);
        smallBtn.normal.background = darkBtn;
        smallBtn.hover.background = darkBtnHover;
        smallBtn.active.background = redDark;
    }

    static Texture2D Solid(Color c)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, c);
        tex.Apply();
        return tex;
    }

    static Texture2D Gradient(Color top, Color bottom)
    {
        var tex = new Texture2D(1, 256) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 256; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, y / 255f));
        tex.Apply();
        return tex;
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
