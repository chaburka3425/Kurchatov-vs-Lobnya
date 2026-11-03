using UnityEngine;
using UnityEngine.InputSystem;

// Мини-карта в левом нижнем углу.
// ЛКМ по карте — перенести камеру, ПКМ — отправить героя.
public class MinimapGUI : MonoBehaviour
{
    public Vector2 worldMin = new Vector2(-LaneData.Half, -LaneData.Half);
    public Vector2 worldMax = new Vector2(LaneData.Half, LaneData.Half);

    Texture2D background, circle;
    BeerBath[] baths;
    HeroController hero => HeroController.Player;
    RTSCamera rig;

    public static float SizeFor(float s) => 280f * s;

    void Start()
    {
        background = BuildMapTexture(256);
        circle = BuildCircle(32);
        baths = FindObjectsByType<BeerBath>(FindObjectsSortMode.None);
        rig = FindFirstObjectByType<RTSCamera>();
    }

    // Курсор над мини-картой (экранные координаты Input System)
    bool waitRelease;   // после выбора электрички не дёргаем камеру, пока кнопка зажата

    public static bool IsOverMap(Vector2 screenPos)
    {
        float s = Mobile.UiScale, size = SizeFor(s);
        var r = new Rect(10 * s, Screen.height - size - 10 * s, size, size);
        return r.Contains(new Vector2(screenPos.x, Screen.height - screenPos.y));
    }

    Rect MapRect()
    {
        float s = Mobile.UiScale;
        float size = SizeFor(s);
        return new Rect(10 * s, Screen.height - size - 10 * s, size, size);
    }

    // ---------- Клики ----------
    float lastTouchTap = -10f;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && (gm.IsPaused || gm.IsGameOver)) return;
        if (MatchSetup.InLobby) return;

        // G — глиф
        var kbd = Keyboard.current;
        if (kbd != null && kbd.gKey.wasPressedThisFrame) PressGlyph();

        // Телефон: тап — камера, двойной тап — герой
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 tp = ts.primaryTouch.position.ReadValue();
            var tg = new Vector2(tp.x, Screen.height - tp.y);
            var tr = MapRect();
            if (tr.Contains(tg))
            {
                Vector3 w = MapToWorld(tg, tr);
                var fxt = HeroController.Player != null ? HeroController.Player.GetComponent<ItemEffects>() : null;
                if (fxt != null && fxt.TpTargeting) { fxt.TicketAtWorld(w); return; }
                if (Time.unscaledTime - lastTouchTap < 0.35f && hero != null) hero.MoveTo(w);
                else if (rig != null) { rig.JumpTo(w); Mobile.Follow = false; }
                lastTouchTap = Time.unscaledTime;
            }
        }

        var mouse = Mouse.current;
        if (mouse == null || TouchInput.UsingTouch) return;

        Vector2 ip = mouse.position.ReadValue();
        var gp = new Vector2(ip.x, Screen.height - ip.y);
        var r = MapRect();
        if (!r.Contains(gp)) return;

        Vector3 world = MapToWorld(gp, r);
        var fxp = HeroController.Player != null ? HeroController.Player.GetComponent<ItemEffects>() : null;
        if (fxp != null && fxp.TpTargeting && mouse.leftButton.wasPressedThisFrame) { fxp.TicketAtWorld(world); waitRelease = true; return; }
        if (waitRelease) { if (!mouse.leftButton.isPressed) waitRelease = false; return; }
        if (mouse.leftButton.isPressed && rig != null) rig.JumpTo(world);
        if (mouse.rightButton.wasPressedThisFrame && hero != null) hero.MoveTo(world);
    }

    // ---------- Рисование ----------
    void OnGUI()
    {
        GUI.depth = -10;
        if (background == null) return;
        float s = Mobile.UiScale;
        var r = MapRect();
        HudArea.Add(r);

        Box(new Rect(r.x - 5 * s, r.y - 5 * s, r.width + 10 * s, r.height + 10 * s), new Color(0.35f, 0.07f, 0.05f));
        Box(new Rect(r.x - 2 * s, r.y - 2 * s, r.width + 4 * s, r.height + 4 * s), new Color(0.05f, 0.04f, 0.04f));
        GUI.DrawTexture(r, background);

        // Бани
        if (baths != null)
            foreach (var b in baths)
            {
                if (b == null) continue;
                float rad = b.radius / (worldMax.x - worldMin.x) * r.width;
                Dot(WorldToMap(b.transform.position, r), rad * 2f, new Color(1f, 0.7f, 0.1f, 0.8f), false);
            }

        // Туман войны поверх карты (текстура: x — мир X, y — мир Z снизу вверх)
        if (FogOfWar.Active && FogOfWar.Texture != null) GUI.DrawTexture(r, FogOfWar.Texture);

        // Руны на реке: мигающий ромбик цвета руны
        foreach (var rn in RuneManager.Active)
        {
            Vector2 p = WorldToMap(rn.pos, r);
            float k = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, p);
            Box(new Rect(p.x - 7 * s, p.y - 7 * s, 14 * s, 14 * s), Color.white * k);
            Box(new Rect(p.x - 5 * s, p.y - 5 * s, 10 * s, 10 * s), RuneManager.Col(rn.kind));
            GUI.matrix = m;
        }

        // Юниты
        foreach (var u in Unit.All)
        {
            if (!u.IsAlive || !FogOfWar.VisibleToPlayer(u)) continue;
            Vector2 p = WorldToMap(u.transform.position, r);
            Color c = TeamUtil.GetColor(u.team);
            bool isThrone = u.GetComponent<Throne>() != null;
            switch (u.kind)
            {
                case UnitKind.Structure:
                    float sz = (isThrone ? 18 : 11) * s;
                    Box(new Rect(p.x - sz / 2 - 1.5f * s, p.y - sz / 2 - 1.5f * s, sz + 3 * s, sz + 3 * s), Color.black);
                    Box(new Rect(p.x - sz / 2, p.y - sz / 2, sz, sz), c);
                    break;
                case UnitKind.Hero:
                    bool me = hero != null && u.gameObject == hero.gameObject;
                    Dot(p, 20 * s, me ? Color.white : Color.black, false);
                    Dot(p, 15 * s, c, false);
                    break;
                default:
                    Dot(p, 8 * s, c * 0.9f, false);
                    break;
            }
        }

        DrawCameraFrame(r, s);
        DrawGlyphButton(r, s);

        GUI.Label(new Rect(r.x + 6 * s, r.y + 4 * s, r.width, 20 * s), "МПКА 2",
            Style(13 * s, new Color(1f, 0.85f, 0.7f, 0.8f)));
    }

    // ---------- Глиф: кнопка рядом с картой ----------
    static Team MyTeam => HeroController.Player != null ? HeroController.Player.GetComponent<Unit>().team : MatchSetup.PlayerTeam;

    void PressGlyph()
    {
        if (!Glyph.Use(MyTeam) && HeroController.Player != null)
            FloatingText.Show(HeroController.Player.transform.position + Vector3.up * 4f,
                $"Глиф перезаряжается: {Mathf.CeilToInt(Glyph.CooldownLeft(MyTeam))} сек", new Color(1f, 0.6f, 0.6f), 22);
    }

    void DrawGlyphButton(Rect map, float s)
    {
        var b = new Rect(map.xMax + 14 * s, map.yMax - 92 * s, 92 * s, 92 * s);
        HudArea.Add(b);
        bool on = Glyph.IsActive(MyTeam);
        float cd = Glyph.CooldownLeft(MyTeam);
        var gold = new Color(1f, 0.78f, 0.23f);
        Box(new Rect(b.x - 3 * s, b.y - 3 * s, b.width + 6 * s, b.height + 6 * s), on ? new Color(1f, 0.9f, 0.4f, 0.6f + 0.4f * Mathf.Sin(Time.time * 10f)) : gold);
        Box(b, on ? new Color(0.75f, 0.55f, 0.1f) : new Color(0.2f, 0.14f, 0.05f));
        GUI.Label(new Rect(b.x, b.y + 4 * s, b.width, b.height * 0.6f), "✦", Style(42 * s, on ? Color.white : gold, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(b.x, b.y + b.height * 0.58f, b.width, b.height * 0.4f), "ГЛИФ (G)", Style(14 * s, on ? Color.white : gold, TextAnchor.MiddleCenter));
        if (cd > 0f && !on)
        {
            Box(b, new Color(0, 0, 0, 0.7f));
            int c = Mathf.CeilToInt(cd);
            GUI.Label(b, c >= 60 ? $"{c / 60}:{c % 60:00}" : c.ToString(), Style(24 * s, Color.white, TextAnchor.MiddleCenter));
        }
        if (GUI.Button(b, GUIContent.none, GUIStyle.none)) PressGlyph();
    }

    void DrawCameraFrame(Rect r, float s)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector2[] pts = new Vector2[4];
        Vector2[] corners = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        var plane = new Plane(Vector3.up, Vector3.zero);
        for (int i = 0; i < 4; i++)
        {
            Ray ray = cam.ViewportPointToRay(corners[i]);
            if (!plane.Raycast(ray, out float d)) d = 200f;
            pts[i] = WorldToMap(ray.GetPoint(Mathf.Min(d, 200f)), r);
        }
        GUI.BeginGroup(r);
        for (int i = 0; i < 4; i++)
            Line(pts[i] - r.position, pts[(i + 1) % 4] - r.position, 2f * s, new Color(1, 1, 1, 0.85f));
        GUI.EndGroup();
    }

    Vector2 WorldToMap(Vector3 w, Rect r)
    {
        float u = Mathf.InverseLerp(worldMin.x, worldMax.x, w.x);
        float v = Mathf.InverseLerp(worldMin.y, worldMax.y, w.z);
        return new Vector2(r.x + u * r.width, r.y + (1f - v) * r.height);
    }

    Vector3 MapToWorld(Vector2 gp, Rect r)
    {
        float u = (gp.x - r.x) / r.width;
        float v = 1f - (gp.y - r.y) / r.height;
        return new Vector3(Mathf.Lerp(worldMin.x, worldMax.x, u), 0f, Mathf.Lerp(worldMin.y, worldMax.y, v));
    }

    void Dot(Vector2 p, float size, Color c, bool _)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(p.x - size / 2, p.y - size / 2, size, size), circle);
        GUI.color = old;
    }

    static void Line(Vector2 a, Vector2 b, float width, Color c)
    {
        var matrix = GUI.matrix;
        var old = GUI.color;
        GUI.color = c;
        float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
        float len = (b - a).magnitude;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - width / 2, len, width), Texture2D.whiteTexture);
        GUI.matrix = matrix;
        GUI.color = old;
    }

    static void Box(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static GUIStyle Style(float size, Color c, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(8, Mathf.RoundToInt(size)), fontStyle = FontStyle.Bold, alignment = anchor };
        st.normal.textColor = c;
        return st;
    }

    // Рисуем фон карты: трава, три линии, лагеря, базы городов.
    public static Texture2D BuildMapTexture(int n)
    {
        var tex = new Texture2D(n, n) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var grass = new Color(0.2f, 0.36f, 0.18f);
        var grassDark = new Color(0.16f, 0.3f, 0.15f);
        var lane = new Color(0.5f, 0.42f, 0.28f);
        var kur = TeamUtil.GetColor(Team.Kurchatov);
        var mos = TeamUtil.GetColor(Team.Lobnya);

        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)(n - 1), v = y / (float)(n - 1);
                Color c = ((x / 16 + y / 16) % 2 == 0) ? grass : grassDark;

                // Диагональная линия от Курчатова (низ-лево) к Москве (верх-право)
                float distToLane = Mathf.Abs(u - v) / 1.4142f;
                if (distToLane < LaneData.LaneHalfWidth / (LaneData.Half * 2f)) c = lane;
                float wx = -LaneData.Half + u * LaneData.Half * 2f, wz = -LaneData.Half + v * LaneData.Half * 2f;
                // река из левого верхнего угла в правый нижний (ширина как в MapLookBuilder)
                if (Mathf.Abs(wx + wz) / 1.4142f < 5f) c = new Color(0.2f, 0.45f, 0.65f);
                if (distToLane < LaneData.LaneHalfWidth / (LaneData.Half * 2f)) c = lane;
                foreach (var r in LaneData.SideRects)
                    if (wx >= r.x && wx <= r.z && wz >= r.y && wz <= r.w) c = lane;
                foreach (var camp in LaneData.Camps)
                    if (Vector2.Distance(new Vector2(wx, wz), new Vector2(camp.pos.x, camp.pos.z)) < 2.6f) c = new Color(0.42f, 0.3f, 0.14f);

                // Базы в углах
                float dk = Vector2.Distance(new Vector2(u, v), new Vector2(0.12f, 0.12f));
                float dm = Vector2.Distance(new Vector2(u, v), new Vector2(0.88f, 0.88f));
                if (dk < 0.2f) c = Color.Lerp(c, kur, 0.35f * (1f - dk / 0.2f) + 0.1f);
                if (dm < 0.2f) c = Color.Lerp(c, mos, 0.35f * (1f - dm / 0.2f) + 0.1f);

                // Затемнение по краям
                float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
                if (edge < 0.04f) c *= 0.6f + 10f * edge;
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return tex;
    }

    static Texture2D BuildCircle(int n)
    {
        var tex = new Texture2D(n, n) { filterMode = FilterMode.Bilinear };
        float r = n / 2f - 1f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
            }
        tex.Apply();
        return tex;
    }
}
