using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Начало матча как в Доте: случайная команда → выбор героя → выбор линии → стартовый закуп → игра.
// Пока идёт лобби, время на карте стоит (крипы и лес ещё не появились).
// Против игрока — бот на случайном герое (одного героя дважды взять нельзя).
public class MatchSetup : MonoBehaviour
{
    public bool randomTeam = true;
    public Transform spawnKurchatov;
    public Transform spawnMoscow;
    [Tooltip("Неактивные заготовки героев (их собирает MapBuilder)")]
    public GameObject[] heroTemplates;

    public static Team PlayerTeam { get; private set; } = Team.Kurchatov;
    public static Team BotTeam => PlayerTeam == Team.Kurchatov ? Team.Lobnya : Team.Kurchatov;
    public static bool InLobby { get; private set; }
    public static bool InBuyPhase => InLobby && phase == Phase.Buy;
    public static GameObject PlayerHero { get; private set; }
    public static GameObject BotHero { get; private set; }   // первый вражеский бот
    public static readonly List<GameObject> Bots = new List<GameObject>();
    public static readonly int[] TeamKills = new int[3];
    static float matchStartTime = -1f;
    // Сколько секунд идёт бой (0 — ещё лобби)
    public static float MatchTime => InLobby || matchStartTime < 0f ? 0f : Time.time - matchStartTime;

    // Бот в лобби: ник, команда, выбранный герой, когда выберет, линия (0 верх, 1 мид, 2 низ, 3 лес)
    // По сети: human — живой игрок (client — его номер), entity — номер его героя
    class Slot { public string nick; public Team team; public HeroDefinition def; public float pickAt; public int lane; public GameObject go; public bool human; public ulong client = NetEntity.NoOwner; public int entity; }
    public static MatchSetup Instance { get; private set; }
    float waitPlayersUntil, lobbySendAt;
    readonly List<Slot> slots = new List<Slot>();
    static readonly string[][] Nicks = { new[] { "Петя", "Вова", "Димон", "Лёха", "Серый" }, new[] { "Артём", "Кирилл", "Макс", "Тимур", "Егор" } };
    static readonly int[] LanePool = { 0, 1, 2, 3, 2 };

    // Лента убийств
    struct Feed { public string text; public List<string> lines; public Color color; public float until; public bool mine; }
    readonly List<Feed> feed = new List<Feed>();

    enum Phase { Pick, Lane, Buy, Done }
    static Phase phase = Phase.Done;

    // Выбор линии: 0 верх, 1 мид, 2 низ, 3 лес
    static readonly string[] LaneNames = { "ВЕРХ", "МИД", "НИЗ", "ЛЕС" };
    static readonly string[] LaneInfo =
    {
        "Длинная линия по левому и верхнему краю карты. Две башни на каждый город, крипы каждые 30 секунд.",
        "Короткая линия по диагонали — самая быстрая дорога к трону врага. Тут больше всего драк.",
        "Длинная линия по нижнему и правому краю. Подальше от чужих глаз.",
        "Между линиями стоят лагеря гопников. Появляются с 0:30 и раз в минуту. Главный гопник бьёт больно — приходи с полным здоровьем."
    };

    float phaseEnd;
    HeroDefinition pick, myHero;
    float botLaneAt;
    int myLane = 1;
    bool botLaneShown;
    float bannerUntil;
    float matchStart;
    Texture2D mapTex;
    Vector2 detailScroll;
    readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();

    // Убийства / смерти / помощь и кто кого бил (для помощи)
    class Kda { public int k, d, a, lh, dn; }   // убийства, смерти, помощь, добитые крипы, денаи
    readonly Dictionary<GameObject, Kda> kda = new Dictionary<GameObject, Kda>();
    // кто бил героя: время последнего удара и сколько урона нанёс (сбрасывается, если не бил 15 сек.)
    class Hit { public float time, dmg; }
    readonly Dictionary<Health, Dictionary<GameObject, Hit>> hitBy = new Dictionary<Health, Dictionary<GameObject, Hit>>();
    const float AssistWindow = 15f;
    GameObject inspect;      // чей инвентарь/скиллы смотрим (клик по портрету сверху)
    bool scoreOpen;          // таблица счёта (клавиша Ё)
    Kda KdaOf(GameObject go) { if (!kda.TryGetValue(go, out var x)) { x = new Kda(); kda[go] = x; } return x; }

    void Start()
    {
        Instance = this;
        Glyph.ResetAll();
        matchStartTime = -1f;
        PlayerTeam = randomTeam ? (Random.value < 0.5f ? Team.Kurchatov : Team.Lobnya) : Team.Kurchatov;
        if (Net.Online && Net.Me != null) PlayerTeam = Net.Me.team;
        foreach (var t in heroTemplates) if (t != null) t.SetActive(false);

        // 4 союзных бота и 5 вражеских (по сети — живые игроки, остальные места — боты)
        Bots.Clear();
        for (int i = 0; i < 3; i++) TeamKills[i] = 0;
        if (Net.IsHost)
            foreach (var p in Net.Players)
                if (p.id != Net.MyId) slots.Add(new Slot { nick = p.nick, team = p.team, human = true, client = p.id, lane = 1 });
        if (!Net.IsClient)
            foreach (var team in new[] { PlayerTeam, BotTeam })
            {
                var names = new List<string>(Nicks[(int)team]);
                Shuffle(names);
                int count = (team == PlayerTeam ? 4 : 5) - slots.FindAll(sl => sl.team == team).Count;
                for (int i = 0; i < count; i++) slots.Add(new Slot { nick = names[i], team = team });
            }
        var enemyLanes = new List<int>(LanePool); Shuffle(enemyLanes);
        int ei = 0;
        foreach (var sl in slots) if (sl.team == BotTeam) sl.lane = enemyLanes[ei++];
        AssignAllyLanes();
        Health.AnyDeath += OnAnyDeath;
        Health.AnyDamaged += OnAnyDamaged;

        InLobby = true;
        Time.timeScale = 0f;
        SetPhase(Phase.Pick);

        if (Net.Online)
        {
            gameObject.AddComponent<NetWorld>();
            waitPlayersUntil = Time.unscaledTime + 25f;   // ждём, пока все загрузят карту
            if (Net.IsClient)
            {
                Net.On(Msg.LobbyState, (f, r) => OnLobbyState(r));
                Net.On(Msg.Feed, (f, r) => OnNetFeed(r));
            }
            else
            {
                Net.On(Msg.Pick, (from, r) => { var sl = slots.Find(x => x.client == from); var d = HeroCatalog.Get(r.ReadString()); if (sl != null && sl.def == null && phase == Phase.Pick) sl.def = d; });
                Net.On(Msg.Lane, (from, r) => { var sl = slots.Find(x => x.client == from); int l = r.ReadByte(); if (sl != null && l < 4) sl.lane = l; });
            }
        }
    }

    void OnDestroy()
    {
        InLobby = false;
        phase = Phase.Done;
        Health.AnyDeath -= OnAnyDeath;
        Health.AnyDamaged -= OnAnyDamaged;
        if (Instance == this) Instance = null;
        foreach (var t in new[] { Msg.LobbyState, Msg.Feed, Msg.Pick, Msg.Lane }) Net.Off(t);
    }

    // ================= СЕТЬ =================
    bool AllPlayersReady() { foreach (var p in Net.Players) if (!p.ready) return false; return true; }

    // Хост → клиенты: фаза лобби, время, кто кем играет
    void SendLobbyState()
    {
        var all = new List<Slot>(slots);
        all.Insert(0, new Slot { nick = Net.MyNick, team = PlayerTeam, def = myHero, lane = myLane, go = PlayerHero, human = true, client = Net.MyId });
        float left = Mathf.Max(0f, phaseEnd - Time.unscaledTime);
        Net.SendToClients(Msg.LobbyState, w =>
        {
            w.Write((byte)phase);
            w.Write(left);
            w.Write((byte)all.Count);
            foreach (var sl in all)
            {
                w.Write(sl.client); w.Write(sl.human); w.Write(sl.nick ?? ""); w.Write((byte)sl.team);
                w.Write(sl.def != null ? sl.def.id : ""); w.Write((byte)sl.lane);
                w.Write(sl.go != null ? NetEntity.IdOf(sl.go.transform) : 0);
            }
        });
    }

    void OnLobbyState(BinaryReader r)
    {
        var ph = (Phase)r.ReadByte();
        float left = r.ReadSingle();
        int n = r.ReadByte();
        slots.Clear();
        for (int i = 0; i < n; i++)
        {
            var sl = new Slot { client = r.ReadUInt64(), human = r.ReadBoolean(), nick = r.ReadString(), team = (Team)r.ReadByte() };
            string hid = r.ReadString();
            sl.def = hid.Length > 0 ? HeroCatalog.Get(hid) : null;
            sl.lane = r.ReadByte();
            sl.entity = r.ReadInt32();
            var u = NetEntity.FindUnit(sl.entity);
            sl.go = u != null ? u.gameObject : null;
            if (sl.client == Net.MyId)
            {
                if (myHero == null && sl.def != null) myHero = sl.def;   // не успел — хост дал случайного
                continue;
            }
            if (sl.go != null) { if (!Bots.Contains(sl.go)) Bots.Add(sl.go); if (BotHero == null && sl.team == BotTeam) BotHero = sl.go; }
            slots.Add(sl);
        }
        phaseEnd = Time.unscaledTime + left;
        if (ph != phase)
        {
            if (ph == Phase.Done) { ClientStartMatch(); return; }
            phase = ph;
            if (ph == Phase.Lane) botLaneShown = true;
            if (ph == Phase.Buy && PlayerHero != null) { var shop = PlayerHero.GetComponent<ShopGUI>(); if (shop != null) shop.Open(); }
        }
    }

    void ClientStartMatch()
    {
        if (PlayerHero != null) { var shop = PlayerHero.GetComponent<ShopGUI>(); if (shop != null) shop.Close(); }
        InLobby = false;
        phase = Phase.Done;
        Time.timeScale = 1f;
        bannerUntil = Time.time + 4f;
    }

    // Клиент: хост прислал героя — делаем копию из заготовки
    public GameObject NetSpawnHero(string heroId, Team team, ulong owner, string disp, Vector3 pos)
    {
        GameObject tpl = null;
        foreach (var t in heroTemplates)
            if (t != null && t.GetComponent<HeroStats>() != null && t.GetComponent<HeroStats>().heroId == heroId) tpl = t;
        if (tpl == null) return null;
        bool mine = owner == Net.MyId;
        var go = Instantiate(tpl, pos, Quaternion.identity);
        go.name = disp;
        var unit = go.GetComponent<Unit>();
        unit.team = team;
        unit.displayName = mine ? HeroCatalog.Get(heroId).name : disp;
        var hc = go.GetComponent<HeroController>();
        hc.playerControlled = mine;
        hc.respawnPoint = team == Team.Kurchatov ? spawnKurchatov : spawnMoscow;
        if (!mine)
        {
            DestroyImmediate(go.GetComponent<AbilityBarGUI>());
            DestroyImmediate(go.GetComponent<ShopGUI>());
        }
        var ring = go.transform.Find("Model/TeamRing");
        if (ring != null) Recolor(ring.gameObject, TeamUtil.GetColor(team));
        var e = go.AddComponent<NetEntity>();
        e.owner = owner;
        go.SetActive(true);
        if (mine)
        {
            PlayerHero = go;
            myHero = HeroCatalog.Get(heroId);
            var cam = FindFirstObjectByType<RTSCamera>();
            if (cam != null)
            {
                cam.followTarget = go.transform;
                cam.transform.position = new Vector3(pos.x, 0f, pos.z);
            }
            if (phase == Phase.Buy) { var shop = go.GetComponent<ShopGUI>(); if (shop != null) shop.Open(); }
        }
        return go;
    }

    public int[] KdaArray(GameObject go) { var x = KdaOf(go); return new[] { x.k, x.d, x.a, x.lh, x.dn }; }
    public void NetSetKda(GameObject go, int[] v) { var x = KdaOf(go); x.k = v[0]; x.d = v[1]; x.a = v[2]; x.lh = v[3]; x.dn = v[4]; }
    public static void NetSetClock(float matchTime) { if (!InLobby) matchStartTime = Time.time - matchTime; }

    void SendFeed(List<string> lines, Unit dead, Unit killer)
    {
        Net.SendToClients(Msg.Feed, w =>
        {
            w.Write((byte)lines.Count); foreach (var l in lines) w.Write(l ?? "");
            w.Write((byte)dead.team);
            w.Write(NetEntity.IdOf(dead)); w.Write(NetEntity.IdOf(killer));
        });
    }

    void OnNetFeed(BinaryReader r)
    {
        int n = r.ReadByte();
        var lines = new List<string>(); for (int i = 0; i < n; i++) lines.Add(r.ReadString());
        var deadTeam = (Team)r.ReadByte();
        var dead = NetEntity.FindUnit(r.ReadInt32()); var killer = NetEntity.FindUnit(r.ReadInt32());
        var me = PlayerHero != null ? PlayerHero.GetComponent<Unit>() : null;
        bool mine = me != null && (dead == me || killer == me);
        Color frame = deadTeam == PlayerTeam ? new Color(0.75f, 0.2f, 0.18f) : new Color(0.2f, 0.6f, 0.25f);
        feed.Add(new Feed { lines = lines, color = frame, until = Time.time + 9f, mine = mine });
        if (feed.Count > 5) feed.RemoveAt(0);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
    }

    // Союзники занимают линии, которые не выбрал игрок
    void AssignAllyLanes()
    {
        var pool = new List<int>(LanePool);
        int k = pool.IndexOf(myLane); if (k >= 0) pool.RemoveAt(k); else pool.RemoveAt(pool.Count - 1);
        Shuffle(pool);
        int i = 0;
        foreach (var sl in slots)
            if (sl.team == PlayerTeam && i < pool.Count)
            {
                sl.lane = pool[i++];
                var ai = sl.go != null ? sl.go.GetComponent<BotHeroAI>() : null;
                if (ai != null) ai.SetLane(sl.lane);
            }
    }

    void OnAnyDamaged(Health h, DamageInfo info, float dmg)
    {
        if (info.source == null || info.source.kind != UnitKind.Hero) return;
        var hu = h.GetComponent<Unit>();
        if (hu == null || hu.kind != UnitKind.Hero) return;
        if (!hu.IsEnemyOf(info.source)) return;
        if (!hitBy.TryGetValue(h, out var map)) { map = new Dictionary<GameObject, Hit>(); hitBy[h] = map; }
        if (!map.TryGetValue(info.source.gameObject, out var hit)) { hit = new Hit(); map[info.source.gameObject] = hit; }
        if (Time.time - hit.time > AssistWindow) hit.dmg = 0f;
        hit.time = Time.time;
        hit.dmg += Mathf.Max(1f, dmg);
    }

    // ---------- Золото за героев ----------
    // «Ценность» команды: золото в кармане + стоимость вещей всех её героев
    static int NetWorth(Team t)
    {
        int sum = 0;
        foreach (var u in Unit.All)
        {
            if (u.kind != UnitKind.Hero || u.team != t) continue;
            var p = u.GetComponent<HeroProgress>(); var inv = u.GetComponent<Inventory>();
            sum += (p != null ? p.Gold : 0) + (inv != null ? inv.ItemValue() : 0);
        }
        return sum;
    }

    // Отстающая команда получает за убийство больше (до +60%), лидирующая — меньше (до −30%)
    static float CatchUp(Team killers)
    {
        Team other = killers == Team.Kurchatov ? Team.Lobnya : Team.Kurchatov;
        float mine = NetWorth(killers), theirs = NetWorth(other);
        float diff = (theirs - mine) / Mathf.Max(3000f, Mathf.Max(mine, theirs));   // >0 — мы отстаём
        return Mathf.Clamp(1f + diff * 1.5f, 0.7f, 1.6f);
    }

    static Vector3 payFrom;   // откуда летят монеты (где умер герой)

    static void Pay(GameObject hero, int gold)
    {
        if (hero == null || gold <= 0) return;
        var p = hero.GetComponent<HeroProgress>();
        if (p == null) return;
        p.AddGold(gold);
        FloatingText.Show(hero.transform.position + Vector3.up * 3.4f, $"+{gold}", new Color(1f, 0.82f, 0.2f), 24);
        var u = hero.GetComponent<Unit>();
        if (u != null && u.IsAlive && FogOfWar.VisibleToPlayer(u)) Fx.Coins(payFrom, hero.transform, Mathf.Clamp(gold / 40, 2, 8));
    }

    static string Colored(Unit u) => u == null ? "?" : $"<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(TeamUtil.GetColor(u.team), Color.white, 0.25f))}>{u.displayName}</color>";

    static string Got(GameObject go, int g) => $"{Colored(go.GetComponent<Unit>())} <color=#ffd84a>+{g}</color>";

    // Раздать золото за смерть героя du. Возвращает одну строку для ленты справа: кто кого и кто сколько получил.
    string HeroBounty(Unit du, Unit killer, Dictionary<GameObject, Hit> hits)
    {
        Team winners = du.team == Team.Kurchatov ? Team.Lobnya : Team.Kurchatov;
        int lvl = du.GetComponent<HeroStats>() != null ? du.GetComponent<HeroStats>().level : 1;
        float mult = CatchUp(winners);   // отстающим больше, лидерам меньше (в ленте не пишем)
        int killGold = Mathf.RoundToInt((150f + 10f * lvl) * mult);
        int assistPool = Mathf.RoundToInt((70f + 8f * lvl) * mult);
        var got = new List<string>();

        // кто помогал: враги-герои, бившие недавно
        var helpers = new List<(GameObject go, float dmg)>();
        if (hits != null)
            foreach (var kv in hits)
                if (kv.Key != null && Time.time - kv.Value.time < AssistWindow)
                {
                    var au = kv.Key.GetComponent<Unit>();
                    if (au != null && au.kind == UnitKind.Hero && au.team == winners) helpers.Add((kv.Key, kv.Value.dmg));
                }

        bool heroKill = killer != null && killer.kind == UnitKind.Hero && killer.team == winners;
        if (heroKill)
        {
            Pay(killer.gameObject, killGold);
            got.Add(Got(killer.gameObject, killGold));
            helpers.RemoveAll(h => h.go == killer.gameObject);
            if (helpers.Count > 0)
            {
                int each = Mathf.RoundToInt(assistPool / (float)helpers.Count);
                foreach (var h in helpers) { Pay(h.go, each); KdaOf(h.go).a++; got.Add(Got(h.go, each)); }
            }
            return $"{Colored(killer)} убил {Colored(du)}:  {string.Join(", ", got)}";
        }

        // убили крипы, башня, лес — награду делят те, кто бил (по урону), иначе вся команда поровну
        int pool = killGold + assistPool;
        string by = killer == null ? "погиб" : killer.kind == UnitKind.Structure ? "погиб от башни" : killer.team == Team.Neutral ? "погиб в лесу" : "погиб от крипов";
        if (helpers.Count > 0)
        {
            float total = 0f; foreach (var h in helpers) total += h.dmg;
            foreach (var h in helpers)
            {
                int g = Mathf.RoundToInt(pool * h.dmg / Mathf.Max(1f, total));
                Pay(h.go, g); KdaOf(h.go).a++;
                got.Add(Got(h.go, g));
            }
        }
        else
        {
            // вся команда, включая мёртвых
            var team = new List<GameObject>();
            if (PlayerHero != null && PlayerTeam == winners) team.Add(PlayerHero);
            foreach (var sl in slots) if (sl.go != null && sl.team == winners) team.Add(sl.go);
            if (team.Count > 0)
            {
                int each = Mathf.RoundToInt(pool / (float)team.Count);
                foreach (var go in team) Pay(go, each);
                got.Add($"всей команде <color=#ffd84a>+{each}</color>");
            }
        }
        return $"{Colored(du)} {by}:  {string.Join(", ", got)}";
    }

    void OnAnyDeath(Health dead, Unit killer)
    {
        var du = dead.GetComponent<Unit>();
        if (du != null && du.kind == UnitKind.Structure)
        {
            // разрушение башни/трона: большая огненная волна
            Fx.Burst(du.transform.position, new Color(1f, 0.55f, 0.15f), 7f);
            Fx.Pillar(du.transform.position, new Color(1f, 0.45f, 0.1f), 10f, 1.6f, 1.2f);
        }
        if (du != null && du.kind == UnitKind.Creep && killer != null && killer.kind == UnitKind.Hero)
        {
            if (killer.team == du.team)
            {
                KdaOf(killer.gameObject).dn++;
                FloatingText.Show(du.transform.position + Vector3.up * 2.4f, "!", new Color(1f, 0.35f, 0.3f), 44);
            }
            else KdaOf(killer.gameObject).lh++;
        }
        if (du == null || du.kind != UnitKind.Hero) return;
        KdaOf(du.gameObject).d++;
        if (killer != null && killer.kind == UnitKind.Hero && killer.IsEnemyOf(du)) KdaOf(killer.gameObject).k++;
        hitBy.TryGetValue(dead, out var hits);
        payFrom = du.transform.position;
        if (FogOfWar.VisibleToPlayer(du) || du.team == PlayerTeam || (killer != null && killer.team == PlayerTeam))
        {
            // смерть героя: тёмная волна и «душа» вверх
            var tc = TeamUtil.GetColor(du.team);
            Fx.Shockwave(du.transform.position, new Color(0.9f, 0.15f, 0.12f), 3.5f, 0.6f);
            Fx.Flash(du.transform.position + Vector3.up * 1.5f, Color.Lerp(tc, Color.white, 0.4f), 2.2f, 0.35f);
            Fx.Glints(du.transform.position, Color.Lerp(tc, Color.white, 0.5f), 18, 0.8f, 3.5f, 1.6f);
        }
        var lines = new List<string> { HeroBounty(du, killer, hits) };   // золото: убийце, за помощь, догонялка
        if (hits != null) hits.Clear();
        TeamKills[(int)(du.team == Team.Kurchatov ? Team.Lobnya : Team.Kurchatov)]++;

        // лента справа: свои убийства/смерти — с рамкой
        var me = PlayerHero != null ? PlayerHero.GetComponent<Unit>() : null;
        bool mine = du == me || killer == me;
        Color frame = du.team == PlayerTeam ? new Color(0.75f, 0.2f, 0.18f) : new Color(0.2f, 0.6f, 0.25f);
        feed.Add(new Feed { lines = lines, color = frame, until = Time.time + 9f, mine = mine });
        if (feed.Count > 5) feed.RemoveAt(0);
        if (Net.IsHost) SendFeed(lines, du, killer);
    }

    void SetPhase(Phase p)
    {
        phase = p;
        float dur = p == Phase.Pick ? 30f : p == Phase.Lane ? 15f : 30f;
        phaseEnd = Time.unscaledTime + dur;
        if (p == Phase.Pick) { pick = HeroCatalog.All[0]; foreach (var sl in slots) sl.pickAt = Time.unscaledTime + Random.Range(1.5f, 9f); }
        if (p == Phase.Lane) botLaneAt = Time.unscaledTime + Random.Range(1f, 3f);
        if (p == Phase.Buy && PlayerHero != null)
        {
            var shop = PlayerHero.GetComponent<ShopGUI>();
            if (shop != null) shop.Open();
        }
    }

    void Update()
    {
        // хост рассылает лобби (и после начала — кто кем играет, для таблицы)
        if (Net.IsHost && Time.unscaledTime >= lobbySendAt) { lobbySendAt = Time.unscaledTime + (InLobby ? 0.25f : 1f); SendLobbyState(); }
        if (Net.IsClient && InLobby)
        {
            // у клиента время лобби ведёт хост
            return;
        }
        if (!InLobby)
        {
            var kb = Keyboard.current;
            if (kb != null && PlayerHero != null)
            {
                scoreOpen = kb.backquoteKey.isPressed;   // Ё: таблица видна, пока клавиша зажата
                if (kb.escapeKey.wasPressedThisFrame) { inspect = null; scoreOpen = false; }
            }
            return;
        }
        float now = Time.unscaledTime;

        if (phase == Phase.Pick)
        {
            // по сети: пока не все загрузили карту — таймер стоит
            if (Net.IsHost && !AllPlayersReady() && now < waitPlayersUntil) phaseEnd = Mathf.Max(phaseEnd, now + 30f);
            bool all = true;
            foreach (var sl in slots)
            {
                if (sl.def == null && !sl.human && now >= sl.pickAt) sl.def = HeroCatalog.All[Random.Range(0, HeroCatalog.All.Length)];
                if (sl.def == null) all = false;
            }
            if (all && myHero != null) phaseEnd = Mathf.Min(phaseEnd, now + 0.9f);
        }
        if (phase == Phase.Lane && !botLaneShown && now >= botLaneAt) botLaneShown = true;

        if (now >= phaseEnd) Next();
    }

    static HeroDefinition RandomFree(HeroDefinition taken)
    {
        var list = new List<HeroDefinition>();
        foreach (var h in HeroCatalog.All) if (h != taken) list.Add(h);
        if (list.Count == 0) return HeroCatalog.All[0];
        return list[Random.Range(0, list.Count)];
    }

    void Next()
    {
        switch (phase)
        {
            case Phase.Pick:
                if (myHero == null) myHero = HeroCatalog.All[Random.Range(0, HeroCatalog.All.Length)];
                foreach (var sl in slots) if (sl.def == null) sl.def = HeroCatalog.All[Random.Range(0, HeroCatalog.All.Length)];
                SpawnHeroes();
                SetPhase(Phase.Lane);
                break;
            case Phase.Lane:
                botLaneShown = true;
                SetPhase(Phase.Buy);
                break;
            case Phase.Buy:
                StartMatch();
                break;
        }
    }

    // ---------- Герои ----------
    bool HasTemplate(HeroDefinition def)
    {
        foreach (var t in heroTemplates)
            if (t != null && t.GetComponent<HeroStats>() != null && t.GetComponent<HeroStats>().heroId == def.id) return true;
        return false;
    }

    // Героя нет на карте (карту не пересоздали после добавления героя) — берём другого, чтобы матч не сломался
    HeroDefinition Playable(HeroDefinition def)
    {
        if (def != null && HasTemplate(def)) return def;
        Debug.LogWarning($"Нет заготовки героя {def?.id} — пересоздай карту (Tools → MOBA)");
        missingHero = def != null ? def.name : missingHero;
        var list = new List<HeroDefinition>();
        foreach (var h in HeroCatalog.All) if (HasTemplate(h)) list.Add(h);
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : HeroCatalog.All[0];
    }
    string missingHero;

    void SpawnHeroes()
    {
        myHero = Playable(myHero);
        foreach (var sl in slots) sl.def = Playable(sl.def);
        if (missingHero != null)
            feed.Add(new Feed { text = $"«{missingHero}» нет на карте — пересоздай карту: Tools → MOBA", color = new Color(1f, 0.5f, 0.45f), until = Time.time + 30f });
        PlayerHero = Spawn(myHero, PlayerTeam, true, null, 0);
        int[] idx = new int[2];
        foreach (var sl in slots)
        {
            int k = ++idx[sl.team == PlayerTeam ? 0 : 1];
            sl.go = Spawn(sl.def, sl.team, false, sl.nick, k, sl.human ? sl.client : NetEntity.NoOwner);
            if (sl.go == null) continue;
            Bots.Add(sl.go);
            var ai = sl.go.GetComponent<BotHeroAI>();
            if (ai != null) ai.SetLane(sl.lane);
            if (BotHero == null && sl.team == BotTeam) BotHero = sl.go;
        }

        var cam = FindFirstObjectByType<RTSCamera>();
        if (cam != null && PlayerHero != null)
        {
            cam.followTarget = PlayerHero.transform;
            cam.transform.position = new Vector3(PlayerHero.transform.position.x, 0f, PlayerHero.transform.position.z);
        }
    }

    // human — номер игрока по сети (его героем управляют приказы оттуда, а не бот)
    GameObject Spawn(HeroDefinition def, Team team, bool player, string nick, int index, ulong human = NetEntity.NoOwner)
    {
        GameObject tpl = null;
        foreach (var t in heroTemplates)
            if (t != null && t.GetComponent<HeroStats>() != null && t.GetComponent<HeroStats>().heroId == def.id) tpl = t;
        if (tpl == null) { Debug.LogWarning("Нет заготовки героя " + def.id + " — пересоздай карту (Tools → MOBA)"); return null; }

        var spawn = team == Team.Kurchatov ? spawnKurchatov : spawnMoscow;
        Vector3 pos = spawn != null ? spawn.position : Vector3.zero;
        float ang = index * Mathf.PI * 2f / 5f;
        if (index > 0) pos += new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 2.5f;
        var go = Instantiate(tpl, pos, Quaternion.identity);
        go.name = player ? def.name : $"{def.name} ({nick})";

        var unit = go.GetComponent<Unit>();
        unit.team = team;
        unit.displayName = go.name;
        var hc = go.GetComponent<HeroController>();
        hc.playerControlled = player;
        hc.respawnPoint = spawn;

        if (!player)
        {
            DestroyImmediate(go.GetComponent<AbilityBarGUI>());
            if (human == NetEntity.NoOwner) { DestroyImmediate(go.GetComponent<ShopGUI>()); go.AddComponent<BotHeroAI>(); }
            else go.GetComponent<ShopGUI>().enabled = false;   // магазин нужен для покупок по сети, окно — у игрока
        }
        if (Net.Online) go.AddComponent<NetEntity>().owner = player ? Net.MyId : human;

        var ring = go.transform.Find("Model/TeamRing");
        if (ring != null) Recolor(ring.gameObject, TeamUtil.GetColor(team));

        go.SetActive(true);
        var agent = go.GetComponent<NavMeshAgent>();
        if (agent != null && NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas)) agent.Warp(hit.position);
        return go;
    }

    void StartMatch()
    {
        matchStart = Time.timeSinceLevelLoad;
        matchStartTime = Time.time;
        if (PlayerHero != null)
        {
            var shop = PlayerHero.GetComponent<ShopGUI>();
            if (shop != null) shop.Close();
        }
        AssignAllyLanes();
        InLobby = false;
        phase = Phase.Done;
        Time.timeScale = 1f;
        bannerUntil = Time.time + 4f;

        // Курьеры: у игрока и у каждого бота
        if (PlayerHero != null) Courier.Spawn(PlayerHero.GetComponent<HeroController>());
        foreach (var b in Bots)
            if (b != null && b.GetComponent<HeroController>() != null) Courier.Spawn(b.GetComponent<HeroController>());

        // Игроки по сети тоже идут на свои линии
        foreach (var sl in slots)
            if (sl.human && sl.go != null)
            {
                var team = sl.go.GetComponent<Unit>().team;
                Vector3 to = sl.lane == 3 ? LaneData.JungleStart(team) : LaneData.LaneStart(team, (LaneData.Lane)sl.lane);
                sl.go.GetComponent<HeroController>().MoveTo(to);
            }

        // Идём на выбранную линию
        if (PlayerHero != null)
        {
            Vector3 dest = myLane == 3 ? LaneData.JungleStart(PlayerTeam) : LaneData.LaneStart(PlayerTeam, (LaneData.Lane)myLane);
            PlayerHero.GetComponent<HeroController>().MoveTo(dest);
            FloatingText.Show(PlayerHero.transform.position + Vector3.up * 4.5f, "Идёшь: " + LaneNames[myLane].ToLower(), new Color(0.5f, 1f, 0.55f), 26);
        }
    }

    static void Recolor(GameObject go, Color c)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var m = r.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    // ================= ИНТЕРФЕЙС ЛОББИ =================
    float s;
    GUIStyle big, title, label, bold, center, btn;

    void Styles()
    {
        s = Screen.height / 1080f;
        big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(56 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        big.normal.textColor = new Color(1f, 0.8f, 0.25f);
        title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(30 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        title.normal.textColor = Color.white;
        label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s), wordWrap = true, richText = true };
        label.normal.textColor = new Color(0.85f, 0.8f, 0.78f);
        bold = new GUIStyle(label) { fontStyle = FontStyle.Bold };
        bold.normal.textColor = Color.white;
        center = new GUIStyle(bold) { alignment = TextAnchor.MiddleCenter };
        btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(30 * s), fontStyle = FontStyle.Bold };
        btn.normal.textColor = btn.hover.textColor = Color.white;
    }

    void OnGUI()
    {
        GUI.depth = -40;
        Styles();

        if (!InLobby)
        {
            if (PlayerHero != null) { DrawTopBar(); DrawMyStats(); DrawFeed(); if (scoreOpen) DrawScoreboard(); else if (inspect != null) DrawInspect(); }
            DrawBanner();
            return;
        }

        int left = Mathf.Max(0, Mathf.CeilToInt(phaseEnd - Time.unscaledTime));

        if (phase == Phase.Buy)
        {
            // Небольшая плашка поверх магазина
            var r = new Rect(Screen.width / 2f - 260 * s, 10 * s, 520 * s, 66 * s);
            HudArea.Add(r);
            Box(r, new Color(0, 0, 0, 0.88f));
            Box(new Rect(r.x, r.yMax - 3 * s, r.width, 3 * s), new Color(1f, 0.78f, 0.23f));
            var st = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(24 * s), alignment = TextAnchor.MiddleLeft };
            st.normal.textColor = new Color(1f, 0.8f, 0.25f);
            GUI.Label(new Rect(r.x + 18 * s, r.y, 300 * s, r.height), $"СТАРТОВЫЙ ЗАКУП  {left}", st);
            if (!Net.IsClient && GUI.Button(new Rect(r.xMax - 170 * s, r.y + 10 * s, 156 * s, 46 * s), "ГОТОВ", btn)) Next();
            return;
        }

        // Полноэкранное лобби
        Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0.07f, 0.04f, 0.045f, 1f));
        Box(new Rect(0, 0, Screen.width, Screen.height * 0.5f), new Color(0.25f, 0.03f, 0.02f, 0.35f));

        float pad = 40 * s;
        var top = new Rect(pad, 20 * s, Screen.width - 2 * pad, 140 * s);
        float tw = (top.width - 360 * s) / 2f;
        TeamPanel(new Rect(top.x, top.y, tw, top.height), Team.Kurchatov);
        TeamPanel(new Rect(top.xMax - tw, top.y, tw, top.height), Team.Lobnya);
        GUI.Label(new Rect(top.x + tw, top.y, 360 * s, 50 * s), phase == Phase.Pick ? "ВЫБОР ГЕРОЯ" : "ВЫБОР ЛИНИИ", title);
        GUI.Label(new Rect(top.x + tw, top.y + 45 * s, 360 * s, 75 * s), left.ToString(), big);

        var body = new Rect(pad, top.yMax + 24 * s, Screen.width - 2 * pad, Screen.height - top.yMax - 150 * s);
        var foot = new Rect(pad, body.yMax + 20 * s, Screen.width - 2 * pad, 80 * s);

        if (phase == Phase.Pick) DrawPick(body, foot);
        else DrawLane(body, foot);
    }

    void TeamPanel(Rect r, Team team)
    {
        bool me = team == PlayerTeam;
        var c = TeamUtil.GetColor(team);
        Box(r, new Color(0.11f, 0.08f, 0.08f));
        Box(new Rect(r.x, r.yMax - 5 * s, r.width, 5 * s), c);
        var nm = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(22 * s), alignment = TextAnchor.UpperCenter };
        nm.normal.textColor = c;
        GUI.Label(new Rect(r.x, r.y + 4 * s, r.width, 28 * s), TeamUtil.GetName(team).ToUpper(), nm);

        // 5 карточек: игрок (если это его команда) и боты
        var cards = new List<(string name, HeroDefinition def, int lane, bool isMe)>();
        if (me) cards.Add(("Ты", myHero, myLane, true));
        foreach (var sl in slots) if (sl.team == team) cards.Add((sl.nick, sl.def, sl.lane, false));
        float cw = (r.width - 20 * s) / 5f, size = Mathf.Min(cw - 8 * s, 58 * s);
        var small = new GUIStyle(label) { fontSize = Mathf.RoundToInt(15 * s), alignment = TextAnchor.UpperCenter, wordWrap = false, clipping = TextClipping.Clip };
        var laneSt = new GUIStyle(small); laneSt.normal.textColor = new Color(1f, 0.8f, 0.3f);
        for (int i = 0; i < cards.Count; i++)
        {
            int k = me ? i : cards.Count - 1 - i;   // у врагов — справа налево
            var cd = cards[k];
            float x = r.x + 10 * s + i * cw + (cw - size) / 2f;
            var pr = new Rect(x, r.y + 32 * s, size, size);
            if (cd.isMe) Box(new Rect(pr.x - 3 * s, pr.y - 3 * s, pr.width + 6 * s, pr.height + 6 * s), new Color(1f, 0.8f, 0.25f));
            Box(pr, new Color(0.2f, 0.16f, 0.16f));
            if (cd.def != null) DrawPortrait(pr, cd.def); else GUI.Label(pr, "?", title);
            var ns = new GUIStyle(small);
            if (cd.isMe) { ns.normal.textColor = new Color(1f, 0.8f, 0.25f); ns.fontStyle = FontStyle.Bold; }
            GUI.Label(new Rect(r.x + 10 * s + i * cw, pr.yMax + 2 * s, cw, 22 * s), cd.name, ns);
            if (phase == Phase.Lane)
            {
                string ln = cd.isMe ? LaneNames[myLane] : !me ? "???" : botLaneShown ? LaneNames[cd.lane] : "…";
                GUI.Label(new Rect(r.x + 10 * s + i * cw, pr.yMax + 20 * s, cw, 22 * s), ln, laneSt);
            }
        }
    }

    void DrawPick(Rect body, Rect foot)
    {
        float colW = (body.width * 0.55f - 3 * 14 * s) / 4f;
        for (int k = 0; k < 4; k++)
        {
            var attr = AttributeUtil.All[k];
            var col = new Rect(body.x + k * (colW + 14 * s), body.y, colW, body.height);
            Box(col, new Color(0.11f, 0.08f, 0.08f));
            var hs = new GUIStyle(bold) { alignment = TextAnchor.MiddleCenter };
            hs.normal.textColor = AttributeUtil.GetColor(attr);
            GUI.Label(new Rect(col.x, col.y + 6 * s, col.width, 34 * s), AttributeUtil.GetName(attr), hs);
            float y = col.y + 46 * s;
            bool any = false;
            foreach (var h in HeroCatalog.All)
            {
                if (h.attribute != attr) continue;
                any = true;
                var card = new Rect(col.x + 10 * s, y, col.width - 20 * s, (col.width - 20 * s) * 0.8f);
                bool taken = false, sel = h == pick;
                Box(new Rect(card.x - 3 * s, card.y - 3 * s, card.width + 6 * s, card.height + 6 * s), sel ? new Color(1f, 0.8f, 0.25f) : new Color(0.25f, 0.2f, 0.2f));
                DrawPortrait(card, h);
                if (taken) Box(card, new Color(0, 0, 0, 0.6f));
                Box(new Rect(card.x, card.yMax - 34 * s, card.width, 34 * s), new Color(0, 0, 0, 0.7f));
                GUI.Label(new Rect(card.x, card.yMax - 34 * s, card.width, 34 * s), h.name + (taken ? " — занят" : ""), center);
                if (GUI.Button(card, GUIContent.none, GUIStyle.none) && myHero == null)
                {
                    if (!taken) pick = h;
                }
                y += card.height + 18 * s;
            }
            if (!any)
            {
                var soon = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(col.x, y, col.width, 80 * s), "скоро\nновые герои", soon);
            }
        }

        // Описание героя
        var det = new Rect(body.x + body.width * 0.55f + 20 * s, body.y, body.width * 0.45f - 20 * s, body.height);
        Box(det, new Color(0.11f, 0.08f, 0.08f));
        GUILayout.BeginArea(new Rect(det.x + 20 * s, det.y + 16 * s, det.width - 40 * s, det.height - 30 * s));
        detailScroll = GUILayout.BeginScrollView(detailScroll);
        GUILayout.Label(HeroText(pick), label);
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUI.Label(new Rect(foot.x, foot.y, foot.width - 400 * s, foot.height),
            Net.IsHost && !AllPlayersReady() ? "Ждём, пока все игроки загрузят карту…" : myHero != null ? "Ждём, пока все выберут героев…" : "Нажми на героя, потом «ВЫБРАТЬ». Не успеешь — дадут случайного.", label);
        GUI.enabled = myHero == null;
        if (GUI.Button(new Rect(foot.xMax - 360 * s, foot.y, 360 * s, foot.height), myHero != null ? "ВЫБРАН" : "ВЫБРАТЬ", btn))
        {
            myHero = pick;
            if (Net.IsClient) Net.SendToHost(Msg.Pick, w => w.Write(myHero.id));
            VoiceLines.Speak(myHero.id, Vector3.zero, 1f, true, -1, myHero.deathVoice, true);
            bool all = true; foreach (var sl in slots) if (sl.def == null) all = false;
            if (all) phaseEnd = Mathf.Min(phaseEnd, Time.unscaledTime + 0.7f);
        }
        GUI.enabled = true;
    }

    string HeroText(HeroDefinition h)
    {
        if (h == null) return "";
        string hex = ColorUtility.ToHtmlStringRGB(AttributeUtil.GetColor(h.attribute));
        var sb = new System.Text.StringBuilder();
        sb.Append($"<size={Mathf.RoundToInt(34 * s)}><b><color=#ffffff>{h.name}</color></b></size>\n");
        sb.Append($"<color=#{hex}><b>{AttributeUtil.GetName(h.attribute)}</b></color> · {(h.ranged ? "дальний бой" : "ближний бой")}\n");
        sb.Append($"Здоровье <b>{h.MaxHealthAt(1):0}</b> · Мана <b>{h.MaxManaAt(1):0}</b> · Урон <b>{h.DamageAt(1):0}</b> · Броня <b>{h.ArmorAt(1):0.#}</b>\n\n");
        if (!string.IsNullOrEmpty(h.innate)) sb.Append($"<color=#ffb07a><b>Врождённая</b></color>\n{h.innate}\n\n");
        foreach (var a in h.abilities)
        {
            string ah = ColorUtility.ToHtmlStringRGB(a.color);
            sb.Append($"<color=#{ah}><b>[{a.key}] {a.name}</b></color>{(a.isUltimate ? " (ульта)" : "")}\n{a.FormattedDescription}\n\n");
        }
        return sb.ToString();
    }

    void DrawLane(Rect body, Rect foot)
    {
        float size = Mathf.Min(body.height, body.width * 0.45f);
        var map = new Rect(body.x, body.y, size, size);
        if (mapTex == null) mapTex = MinimapGUI.BuildMapTexture(256);
        GUI.DrawTexture(map, mapTex);

        Vector2[] spots = { new(0.14f, 0.12f), new(0.5f, 0.5f), new(0.86f, 0.88f), new(0.32f, 0.34f) };
        // Мини-карта: верх — левый и верхний край; низ — нижний и правый
        for (int i = 0; i < 4; i++)
        {
            var c = new Vector2(map.x + spots[i].x * map.width, map.y + spots[i].y * map.height);
            var b = new Rect(c.x - 70 * s, c.y - 30 * s, 140 * s, 60 * s);
            Box(new Rect(b.x - 3 * s, b.y - 3 * s, b.width + 6 * s, b.height + 6 * s), new Color(1f, 0.78f, 0.23f));
            Box(b, myLane == i ? new Color(1f, 0.78f, 0.23f) : new Color(0, 0, 0, 0.85f));
            var ls = new GUIStyle(center) { fontSize = Mathf.RoundToInt(24 * s) };
            ls.normal.textColor = myLane == i ? new Color(0.1f, 0.06f, 0.02f) : new Color(1f, 0.8f, 0.25f);
            GUI.Label(b, LaneNames[i], ls);
            if (GUI.Button(b, GUIContent.none, GUIStyle.none)) { myLane = i; AssignAllyLanes(); if (Net.IsClient) Net.SendToHost(Msg.Lane, w => w.Write((byte)i)); }
        }

        var info = new Rect(map.xMax + 24 * s, body.y, body.xMax - map.xMax - 24 * s, body.height);
        Box(info, new Color(0.11f, 0.08f, 0.08f));
        var h = new GUIStyle(big) { alignment = TextAnchor.UpperLeft, fontSize = Mathf.RoundToInt(44 * s) };
        GUI.Label(new Rect(info.x + 24 * s, info.y + 16 * s, info.width, 60 * s), LaneNames[myLane], h);
        GUI.Label(new Rect(info.x + 24 * s, info.y + 90 * s, info.width - 48 * s, info.height - 100 * s),
            LaneInfo[myLane] + "\n\n<color=#a89a96>Твоя база — " + (PlayerTeam == Team.Kurchatov ? "внизу слева" : "вверху справа") +
            ". Союзные боты займут остальные линии, куда пойдут враги — секрет.</color>", label);

        GUI.Label(new Rect(foot.x, foot.y, foot.width - 400 * s, foot.height), "Выбери, куда встанешь в начале игры.", label);
        if (!Net.IsClient && GUI.Button(new Rect(foot.xMax - 360 * s, foot.y, 360 * s, foot.height), "ГОТОВ", btn)) Next();
    }

    void DrawPortrait(Rect r, HeroDefinition h)
    {
        if (h.id == "ogr_mog" && Portrait("ogr_mog") == null)   // нет аватарки из модели — две фотки голов
        {
            var l = Portrait("ogr_mog_l"); var rr = Portrait("ogr_mog_r");
            if (l != null) GUI.DrawTexture(new Rect(r.x, r.y, r.width / 2f, r.height), l, ScaleMode.ScaleAndCrop);
            if (rr != null) GUI.DrawTexture(new Rect(r.x + r.width / 2f, r.y, r.width / 2f, r.height), rr, ScaleMode.ScaleAndCrop);
            return;
        }
        var t = Portrait(h.id);
        if (t != null) GUI.DrawTexture(r, t, ScaleMode.ScaleAndCrop);
        else Box(r, h.bodyColor);
    }

    Texture2D Portrait(string name)
    {
        if (!portraits.TryGetValue(name, out var t)) { t = Resources.Load<Texture2D>("Portraits/" + name); portraits[name] = t; }
        return t;
    }

    void DrawBanner()
    {
        if (Time.time > bannerUntil) return;
        float a = Mathf.Clamp01(bannerUntil - Time.time);
        var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(64 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        var r = new Rect(0, Screen.height * 0.18f, Screen.width, 120 * s);
        string text = $"ТЫ ИГРАЕШЬ ЗА {(PlayerTeam == Team.Kurchatov ? "КУРЧАТОВ" : "МОСКВУ")}!";
        st.normal.textColor = new Color(0, 0, 0, a);
        GUI.Label(new Rect(r.x + 4 * s, r.y + 4 * s, r.width, r.height), text, st);
        var c = TeamUtil.GetColor(PlayerTeam);
        st.normal.textColor = new Color(c.r, c.g, c.b, a);
        GUI.Label(r, text, st);
        if (Bots.Count > 0)
        {
            var sub = new GUIStyle(st) { fontSize = Mathf.RoundToInt(30 * s) };
            sub.normal.textColor = new Color(1f, 0.55f, 0.5f, a);
            sub.normal.textColor = new Color(0.5f, 1f, 0.55f, a);
            GUI.Label(new Rect(r.x, r.yMax, r.width, 50 * s), "5 на 5: с тобой 4 союзных бота", sub);
        }
    }

    // ---------- Верхняя панель как в Доте: Курчатов слева, Москва справа, время и счёт посередине ----------
    static readonly Color[] SlotColors =
    {
        new Color(0.2f, 0.46f, 1f), new Color(0.4f, 1f, 0.75f), new Color(0.75f, 0f, 0.73f), new Color(0.95f, 0.94f, 0.04f), new Color(1f, 0.42f, 0f),
        new Color(1f, 0.53f, 0.76f), new Color(0.63f, 0.71f, 0.28f), new Color(0.4f, 0.85f, 0.97f), new Color(0f, 0.51f, 0.13f), new Color(0.64f, 0.41f, 0f)
    };
    Texture2D barGrad;

    void DrawTopBar()
    {
        float cw = 84 * s, ch = 48 * s, gap = 3 * s, mid = 168 * s, cap = 86 * s, strip = 5 * s;
        float side = cw * 5 + gap * 4;
        float total = cap * 2 + side * 2 + mid + 12 * s;
        float x0 = Screen.width / 2f - total / 2f, y0 = 0f, h = ch + strip + 12 * s;
        var bar = new Rect(x0, y0, total, h);
        HudArea.Add(new Rect(bar.x, bar.y, bar.width, bar.height + 24 * s));

        // Каменная панель с тёмными концами
        if (barGrad == null)
        {
            barGrad = new Texture2D(1, 32) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++) barGrad.SetPixel(0, y, Color.Lerp(new Color(0.05f, 0.06f, 0.06f), new Color(0.19f, 0.2f, 0.2f), y / 31f));
            barGrad.Apply();
        }
        GUI.DrawTexture(bar, barGrad);
        Box(new Rect(bar.x, bar.yMax - 2 * s, bar.width, 2 * s), new Color(0.42f, 0.36f, 0.24f));
        foreach (float cx in new[] { bar.x, bar.xMax - cap })
        {
            var cr = new Rect(cx, bar.y, cap, h);
            Box(cr, new Color(0.07f, 0.08f, 0.08f, 0.95f));
            for (int k = 0; k < 3; k++)
                Box(new Rect(cr.x + 10 * s + k * 24 * s, cr.y + h / 2f - 6 * s, 16 * s, 12 * s), new Color(0.2f, 0.21f, 0.2f));
            Box(new Rect(cr.x, cr.yMax - 2 * s, cr.width, 2 * s), new Color(0.42f, 0.36f, 0.24f));
        }

        // Герои по командам: Курчатов всегда слева
        var kur = new List<(HeroDefinition def, GameObject go)>();
        var mos = new List<(HeroDefinition def, GameObject go)>();
        (PlayerTeam == Team.Kurchatov ? kur : mos).Add((myHero, PlayerHero));
        foreach (var sl in slots) (sl.team == Team.Kurchatov ? kur : mos).Add((sl.def, sl.go));

        float leftStart = bar.x + cap + 6 * s, rightStart = leftStart + side + mid;
        for (int i = 0; i < 5; i++)
        {
            if (i < kur.Count) Cell(new Rect(leftStart + i * (cw + gap), y0 + strip + 4 * s, cw, ch), kur[i], Team.Kurchatov, SlotColors[i]);
            if (i < mos.Count) Cell(new Rect(rightStart + i * (cw + gap), y0 + strip + 4 * s, cw, ch), mos[i], Team.Lobnya, SlotColors[5 + i]);
        }

        // Центр: счёт убийств и часы с солнцем/луной (день и ночь по 5 минут)
        float mx = leftStart + side;
        var center = new Rect(mx + 46 * s, 0, mid - 92 * s, h + 18 * s);
        Box(center, new Color(0.08f, 0.09f, 0.1f, 0.97f));
        Box(new Rect(center.x, center.yMax - 2 * s, center.width, 2 * s), new Color(0.42f, 0.36f, 0.24f));
        int sec = Mathf.Max(0, Mathf.FloorToInt(Time.timeSinceLevelLoad - matchStart));
        bool day = (sec / 300) % 2 == 0;
        var ic = new Vector2(center.center.x, center.y + 20 * s);
        if (day)
        {
            GUI.color = new Color(1f, 0.82f, 0.3f);
            for (int r = 0; r < 8; r++)
            {
                var m0 = GUI.matrix;
                GUIUtility.RotateAroundPivot(r * 45f, ic);
                GUI.DrawTexture(new Rect(ic.x - 1.5f * s, ic.y - 15 * s, 3 * s, 7 * s), Texture2D.whiteTexture);
                GUI.matrix = m0;
            }
            GUI.color = Color.white;
            Box(new Rect(ic.x - 7 * s, ic.y - 7 * s, 14 * s, 14 * s), new Color(1f, 0.85f, 0.35f));
        }
        else
        {
            Box(new Rect(ic.x - 8 * s, ic.y - 8 * s, 16 * s, 16 * s), new Color(0.75f, 0.82f, 1f));
            Box(new Rect(ic.x - 3 * s, ic.y - 10 * s, 14 * s, 14 * s), new Color(0.08f, 0.09f, 0.1f));
        }
        var tm = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(17 * s), alignment = TextAnchor.MiddleCenter };
        tm.normal.textColor = new Color(0.92f, 0.92f, 0.9f);
        GUI.Label(new Rect(center.x, center.y + 34 * s, center.width, 24 * s), $"{sec / 60}:{sec % 60:00}", tm);

        var sc = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(30 * s), alignment = TextAnchor.MiddleCenter };
        sc.normal.textColor = Color.white;
        GUI.Label(new Rect(mx, 0, 46 * s, h), TeamKills[(int)Team.Kurchatov].ToString(), sc);
        GUI.Label(new Rect(mx + mid - 46 * s, 0, 46 * s, h), TeamKills[(int)Team.Lobnya].ToString(), sc);
    }

    void Cell(Rect r, (HeroDefinition def, GameObject go) hh, Team team, Color slot)
    {
        Box(new Rect(r.x - 1 * s, r.y - 1 * s, r.width + 2 * s, r.height + 2 * s), Color.black);
        if (hh.def != null) DrawPortrait(r, hh.def); else Box(r, Color.gray);
        Box(new Rect(r.x, r.y - 5 * s, r.width, 5 * s), slot);   // цвет игрока сверху
        if (hh.go == null) return;
        var hp = hh.go.GetComponent<Health>();
        var st = hh.go.GetComponent<HeroStats>();
        var hc = hh.go.GetComponent<HeroController>();
        var u = hh.go.GetComponent<Unit>();
        bool ally = u != null && u.team == PlayerTeam;

        if (hp != null && hp.IsDead)
        {
            // мёртв: портрет темнеет, крупно — через сколько воскреснет
            Box(r, new Color(0.12f, 0.12f, 0.12f, 0.72f));
            float left = hc != null ? Mathf.Max(0f, hc.RespawnAt - Time.time) : 0f;
            var d = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(26 * s), alignment = TextAnchor.MiddleCenter };
            d.normal.textColor = new Color(0, 0, 0, 0.8f);
            GUI.Label(new Rect(r.x + 2 * s, r.y + 2 * s, r.width, r.height), Mathf.CeilToInt(left).ToString(), d);
            d.normal.textColor = Color.white;
            GUI.Label(r, Mathf.CeilToInt(left).ToString(), d);
        }
        else if (hp != null && (u == null || ally || FogOfWar.VisibleToPlayer(u)))
        {
            // здоровье и мана под портретом
            Box(new Rect(r.x, r.yMax + 1 * s, r.width, 5 * s), new Color(0, 0, 0, 0.85f));
            Box(new Rect(r.x, r.yMax + 1 * s, r.width * Mathf.Clamp01(hp.Current / Mathf.Max(1f, hp.maxHealth)), 5 * s), new Color(0.25f, 0.8f, 0.2f));
            var mp = hh.go.GetComponent<Mana>();
            if (mp != null)
            {
                Box(new Rect(r.x, r.yMax + 6 * s, r.width, 3 * s), new Color(0, 0, 0, 0.85f));
                Box(new Rect(r.x, r.yMax + 6 * s, r.width * Mathf.Clamp01(mp.Current / Mathf.Max(1f, mp.maxMana)), 3 * s), new Color(0.25f, 0.5f, 1f));
            }
        }

        // ульта готова — зелёный ромбик (только у своих, как в Доте)
        var cs = hh.go.GetComponent<AbilityCaster>();
        if (ally && hp != null && !hp.IsDead && cs != null && cs.Abilities != null && cs.Abilities.Length > 3)
        {
            var ult = cs.Abilities[3];
            if (ult.IsLearned && !ult.IsPassive)
            {
                var c = new Vector2(r.x + 9 * s, r.yMax - 9 * s);
                var m0 = GUI.matrix;
                GUIUtility.RotateAroundPivot(45f, c);
                Box(new Rect(c.x - 5 * s, c.y - 5 * s, 10 * s, 10 * s), Color.black);
                Box(new Rect(c.x - 4 * s, c.y - 4 * s, 8 * s, 8 * s), ult.CooldownLeft <= 0f ? new Color(0.3f, 1f, 0.3f) : new Color(0.45f, 0.45f, 0.45f));
                GUI.matrix = m0;
            }
        }

        if (st != null)
        {
            var lv = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(13 * s), alignment = TextAnchor.LowerRight };
            lv.normal.textColor = new Color(0, 0, 0, 0.9f);
            GUI.Label(new Rect(r.x + 1 * s, r.y + 1 * s, r.width - 3 * s, r.height), st.level.ToString(), lv);
            lv.normal.textColor = new Color(1f, 0.88f, 0.45f);
            GUI.Label(new Rect(r.x, r.y, r.width - 3 * s, r.height), st.level.ToString(), lv);
        }
        if (inspect == hh.go) Box(new Rect(r.x, r.yMax - 3 * s, r.width, 3 * s), Color.white);
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { inspect = inspect == hh.go ? null : hh.go; scoreOpen = false; }
        if (hh.go == PlayerHero)
        {
            var g = new Color(1f, 0.82f, 0.3f);
            Box(new Rect(r.x - 2 * s, r.y - 7 * s, r.width + 4 * s, 2 * s), g);
            Box(new Rect(r.x - 2 * s, r.yMax + 10 * s, r.width + 4 * s, 2 * s), g);
            Box(new Rect(r.x - 2 * s, r.y - 7 * s, 2 * s, r.height + 19 * s), g);
            Box(new Rect(r.xMax, r.y - 7 * s, 2 * s, r.height + 19 * s), g);
        }
    }


    // ---------- Кто в игре: (герой, объект, ник) по команде ----------
    List<(HeroDefinition def, GameObject go, string nick)> Roster(Team t)
    {
        var list = new List<(HeroDefinition, GameObject, string)>();
        if (PlayerTeam == t) list.Add((myHero, PlayerHero, "Ты"));
        foreach (var sl in slots) if (sl.team == t) list.Add((sl.def, sl.go, sl.nick));
        return list;
    }

    string NickOf(GameObject go)
    {
        if (go == PlayerHero) return "Ты";
        foreach (var sl in slots) if (sl.go == go) return sl.nick;
        return "";
    }

    HeroDefinition DefOf(GameObject go)
    {
        if (go == PlayerHero) return myHero;
        foreach (var sl in slots) if (sl.go == go) return sl.def;
        return null;
    }

    static int NetWorth(GameObject go)
    {
        int w = 0;
        var pr = go.GetComponent<HeroProgress>(); if (pr != null) w += pr.Gold;
        var inv = go.GetComponent<Inventory>();
        if (inv != null) for (int i = 0; i < Inventory.TotalSlots; i++) if (inv.Get(i) != null) w += inv.SellPrice(i) * 2;
        return w;
    }

    GUIStyle St(float size, Color c, TextAnchor a = TextAnchor.MiddleLeft, bool b = true)
    {
        var st = new GUIStyle(b ? bold : GUI.skin.label) { fontSize = Mathf.RoundToInt(size * s), alignment = a, richText = true, wordWrap = false, clipping = TextClipping.Clip };
        st.normal.textColor = c;
        return st;
    }

    // ---------- Карточка героя: клик по портрету сверху ----------
    void DrawInspect()
    {
        var go = inspect;
        if (go == null) { inspect = null; return; }
        var def = DefOf(go);
        var u = go.GetComponent<Unit>();
        var hp = go.GetComponent<Health>();
        var mp = go.GetComponent<Mana>();
        var st = go.GetComponent<HeroStats>();
        var at = go.GetComponent<AutoAttack>();
        var ag = go.GetComponent<NavMeshAgent>();
        var cs = go.GetComponent<AbilityCaster>();
        var inv = go.GetComponent<Inventory>();
        var pr = go.GetComponent<HeroProgress>();
        if (def == null || u == null) { inspect = null; return; }
        bool ally = u.team == PlayerTeam;
        var tc = TeamUtil.GetColor(u.team);

        var r = new Rect(Screen.width / 2f - 360 * s, 104 * s, 720 * s, 360 * s);
        HudArea.Add(r);
        Box(new Rect(r.x - 2 * s, r.y - 2 * s, r.width + 4 * s, r.height + 4 * s), new Color(0.42f, 0.36f, 0.24f));
        Box(r, new Color(0.07f, 0.08f, 0.09f, 0.97f));
        Box(new Rect(r.x, r.y, r.width, 4 * s), tc);

        // шапка
        var pic = new Rect(r.x + 16 * s, r.y + 18 * s, 128 * s, 76 * s);
        Box(new Rect(pic.x - 2 * s, pic.y - 2 * s, pic.width + 4 * s, pic.height + 4 * s), tc);
        DrawPortrait(pic, def);
        if (hp != null && hp.IsDead) Box(pic, new Color(0.1f, 0.1f, 0.1f, 0.7f));
        string nick = NickOf(go);
        GUI.Label(new Rect(pic.xMax + 14 * s, r.y + 14 * s, 380 * s, 34 * s), $"{def.name}" + (nick != "" ? $"  <color=#bbbbbb>({nick})</color>" : ""), St(26, Color.white));
        string lvl = st != null ? st.level.ToString() : "1";
        var k = KdaOf(go);
        GUI.Label(new Rect(pic.xMax + 14 * s, r.y + 48 * s, 460 * s, 24 * s),
            $"<color=#{ColorUtility.ToHtmlStringRGB(tc)}>{(u.team == Team.Kurchatov ? "Курчатов" : "Москва")}</color>   Уровень <b>{lvl}</b>   У/С/П <b>{k.k}/{k.d}/{k.a}</b>", St(17, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleLeft, false));
        string money = ally && pr != null ? $"Золото <color=#ffd84a><b>{pr.Gold}</b></color>   " : "";
        GUI.Label(new Rect(pic.xMax + 14 * s, r.y + 72 * s, 460 * s, 24 * s), money + $"Ценность <color=#ffd84a>{NetWorth(go)}</color>", St(17, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleLeft, false));
        if (!ally && hp != null && !hp.IsDead && !FogOfWar.VisibleToPlayer(u))
            GUI.Label(new Rect(r.xMax - 210 * s, r.y + 48 * s, 190 * s, 24 * s), "в тумане войны", St(16, new Color(0.6f, 0.65f, 0.8f), TextAnchor.MiddleRight, false));
        if (GUI.Button(new Rect(r.xMax - 40 * s, r.y + 10 * s, 30 * s, 30 * s), "X", St(20, new Color(1f, 0.6f, 0.55f), TextAnchor.MiddleCenter))) { inspect = null; return; }

        // здоровье и мана
        float by = r.y + 108 * s, bw = r.width - 32 * s;
        if (hp != null)
        {
            Box(new Rect(r.x + 16 * s, by, bw, 20 * s), Color.black);
            Box(new Rect(r.x + 16 * s, by, bw * Mathf.Clamp01(hp.Current / Mathf.Max(1f, hp.maxHealth)), 20 * s), new Color(0.22f, 0.7f, 0.2f));
            GUI.Label(new Rect(r.x + 16 * s, by, bw, 20 * s), $"{Mathf.CeilToInt(hp.Current)} / {Mathf.RoundToInt(hp.maxHealth)}", St(14, Color.white, TextAnchor.MiddleCenter));
        }
        if (mp != null)
        {
            Box(new Rect(r.x + 16 * s, by + 23 * s, bw, 16 * s), Color.black);
            Box(new Rect(r.x + 16 * s, by + 23 * s, bw * Mathf.Clamp01(mp.Current / Mathf.Max(1f, mp.maxMana)), 16 * s), new Color(0.22f, 0.42f, 1f));
            GUI.Label(new Rect(r.x + 16 * s, by + 23 * s, bw, 16 * s), $"{Mathf.CeilToInt(mp.Current)} / {Mathf.RoundToInt(mp.maxMana)}", St(12, Color.white, TextAnchor.MiddleCenter));
        }

        // характеристики
        float sy = by + 52 * s;
        if (st != null && at != null)
        {
            string stats =
                $"Урон <b>{Mathf.RoundToInt(at.damage)}</b>\nБроня <b>{(hp != null ? hp.armor : 0):0.#}</b>\n" +
                $"Скор. атаки <b>{Mathf.RoundToInt(at.attackSpeed)}</b>\nДальность <b>{at.range:0.#}</b>\n" +
                $"Скорость <b>{(ag != null ? ag.speed * 60f : 0):0}</b>\n" +
                $"<color=#ff7a66>Сила {st.Str:0}</color>  <color=#6fe07a>Ловк. {st.Agi:0}</color>  <color=#6fa8ff>Инт. {st.Int:0}</color>";
            var ss = St(15, new Color(0.88f, 0.88f, 0.88f), TextAnchor.UpperLeft, false); ss.wordWrap = true;
            GUI.Label(new Rect(r.x + 16 * s, sy, 230 * s, 180 * s), stats, ss);
        }

        // способности
        string tip = null;
        float ax = r.x + 260 * s, sz = 66 * s;
        GUI.Label(new Rect(ax, sy - 4 * s, 300 * s, 20 * s), "СПОСОБНОСТИ", St(13, new Color(0.7f, 0.65f, 0.55f)));
        if (cs != null && cs.Abilities != null)
            for (int i = 0; i < cs.Abilities.Length && i < 4; i++)
            {
                var a = cs.Abilities[i];
                var ar = new Rect(ax + i * (sz + 8 * s), sy + 18 * s, sz, sz);
                Box(new Rect(ar.x - 2 * s, ar.y - 2 * s, ar.width + 4 * s, ar.height + 4 * s), a.IsPassive ? new Color(0.8f, 0.65f, 0.2f) : new Color(0.28f, 0.25f, 0.25f));
                var c = a.Info.color; Box(ar, new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f));
                var icon = AbilityBarGUI.AbilityIcon(a.Info.id);
                if (icon != null) GUI.DrawTexture(ar, icon, ScaleMode.ScaleAndCrop);
                else { var ns = St(11, Color.white, TextAnchor.MiddleCenter); ns.wordWrap = true; GUI.Label(ar, string.IsNullOrEmpty(a.Info.shortName) ? a.Info.name : a.Info.shortName, ns); }
                GUI.Label(new Rect(ar.x + 4 * s, ar.y + 2 * s, 20 * s, 18 * s), a.Info.key, St(14, Color.white));
                if (!a.IsLearned) Box(ar, new Color(0, 0, 0, 0.6f));
                else if (a.CooldownLeft > 0f)
                {
                    Box(ar, new Color(0, 0, 0, 0.55f));
                    GUI.Label(ar, Mathf.CeilToInt(a.CooldownLeft).ToString(), St(22, Color.white, TextAnchor.MiddleCenter));
                }
                int m = a.MaxLevel; float pw = (sz - 3 * s * (m - 1)) / m;
                for (int p = 0; p < m; p++)
                    Box(new Rect(ar.x + p * (pw + 3 * s), ar.yMax + 4 * s, pw, 5 * s), p < a.Level ? new Color(1f, 0.82f, 0.3f) : new Color(0.25f, 0.23f, 0.2f));
                if (ar.Contains(Event.current.mousePosition)) tip = $"<b>{a.Info.name}</b> [{a.Info.key}] — уровень {a.Level}/{a.MaxLevel}\n{a.Info.FormatDescription(a.Level)}";
            }

        // инвентарь
        float iy = sy + 108 * s, isz = 54 * s;
        GUI.Label(new Rect(ax, iy - 4 * s, 300 * s, 20 * s), "ПРЕДМЕТЫ", St(13, new Color(0.7f, 0.65f, 0.55f)));
        for (int i = 0; i < Inventory.TotalSlots; i++)
        {
            bool back = i >= Inventory.MainSlots;
            float size = back ? isz * 0.75f : isz;
            float x = back ? ax + 6 * (isz + 6 * s) + 16 * s + (i - Inventory.MainSlots) * (size + 4 * s) : ax + i * (isz + 6 * s);
            var ir = new Rect(x, iy + 18 * s, size, size);
            Box(new Rect(ir.x - 1 * s, ir.y - 1 * s, ir.width + 2 * s, ir.height + 2 * s), new Color(0.3f, 0.27f, 0.22f));
            Box(ir, new Color(0.12f, 0.12f, 0.13f));
            var it = inv != null ? inv.Get(i) : null;
            if (it != null)
            {
                var ic = ItemIcons.Get(it.def.id);
                if (ic != null) GUI.DrawTexture(ir, ic, ScaleMode.ScaleAndCrop);
                else { Box(ir, it.def.color); var ns = St(10, Color.white, TextAnchor.MiddleCenter); ns.wordWrap = true; GUI.Label(ir, it.def.name, ns); }
                if (back) Box(ir, new Color(0, 0, 0, 0.4f));
                if (it.cooldownLeft > 0f) { Box(ir, new Color(0, 0, 0, 0.55f)); GUI.Label(ir, Mathf.CeilToInt(it.cooldownLeft).ToString(), St(16, Color.white, TextAnchor.MiddleCenter)); }
                if (ir.Contains(Event.current.mousePosition)) tip = $"<b>{it.def.name}</b>" + (back ? " (в рюкзаке)" : "") + $"\n<i>{it.def.lore}</i>";
            }
        }
        GUI.Label(new Rect(ax + 6 * (isz + 6 * s) + 16 * s, iy + 18 * s + isz * 0.75f + 2 * s, 200 * s, 18 * s), "рюкзак", St(11, new Color(0.6f, 0.6f, 0.6f), TextAnchor.UpperLeft, false));

        if (tip != null)
        {
            var ts = St(15, Color.white, TextAnchor.UpperLeft, false); ts.wordWrap = true;
            float th = ts.CalcHeight(new GUIContent(tip), 440 * s) + 16 * s;
            var tr = new Rect(Event.current.mousePosition.x + 16 * s, Event.current.mousePosition.y + 16 * s, 460 * s, th);
            if (tr.xMax > Screen.width) tr.x = Screen.width - tr.width - 8 * s;
            if (tr.yMax > Screen.height) tr.y = Screen.height - tr.height - 8 * s;
            Box(tr, new Color(0.03f, 0.03f, 0.04f, 0.96f));
            GUI.Label(new Rect(tr.x + 10 * s, tr.y + 8 * s, tr.width - 20 * s, tr.height - 16 * s), tip, ts);
        }
    }

    // ---------- Таблица счёта (Ё) ----------
    void DrawScoreboard()
    {
        float w = 900 * s, rowH = 62 * s, headH = 40 * s;
        var r = new Rect(16 * s, 92 * s, w, headH * 2 + rowH * 10 + 8 * s);
        HudArea.Add(r);
        Box(r, new Color(0.08f, 0.1f, 0.12f, 0.96f));
        float y = r.y;
        int slot = 0;
        foreach (var team in new[] { Team.Kurchatov, Team.Lobnya })
        {
            bool allyTeam = team == PlayerTeam;
            var tc = TeamUtil.GetColor(team);
            // шапка команды
            Box(new Rect(r.x, y, w, headH), new Color(0.12f, 0.14f, 0.16f));
            GUI.Label(new Rect(r.x + 12 * s, y, 60 * s, headH), TeamKills[(int)team].ToString(), St(28, Color.white));
            GUI.Label(new Rect(r.x + 60 * s, y, 300 * s, headH), team == Team.Kurchatov ? "КУРЧАТОВ" : "МОСКВА", St(24, tc));
            var hs = St(14, new Color(0.7f, 0.72f, 0.75f), TextAnchor.MiddleCenter);
            GUI.Label(new Rect(r.x + 420 * s, y, 60 * s, headH), "УР.", hs);
            GUI.Label(new Rect(r.x + 490 * s, y, 130 * s, headH), "ЗОЛОТО", hs);
            GUI.Label(new Rect(r.x + 630 * s, y, 50 * s, headH), "У", hs);
            GUI.Label(new Rect(r.x + 680 * s, y, 50 * s, headH), "С", hs);
            GUI.Label(new Rect(r.x + 730 * s, y, 50 * s, headH), "П", hs);
            GUI.Label(new Rect(r.x + 790 * s, y, 80 * s, headH), "УЛЬТ", hs);
            y += headH;

            foreach (var (def, go, nick) in Roster(team))
            {
                var row = new Rect(r.x, y, w, rowH);
                bool me = go == PlayerHero;
                Box(row, me ? new Color(0.22f, 0.25f, 0.28f) : (slot % 2 == 0 ? new Color(0.1f, 0.12f, 0.14f) : new Color(0.12f, 0.14f, 0.16f)));
                Box(new Rect(row.x, row.y + 2 * s, 6 * s, rowH - 4 * s), SlotColors[slot]);
                var pic = new Rect(row.x + 20 * s, row.y + 6 * s, 88 * s, rowH - 12 * s);
                if (def != null) DrawPortrait(pic, def);
                var hp = go != null ? go.GetComponent<Health>() : null;
                if (hp != null && hp.IsDead)
                {
                    Box(pic, new Color(0.1f, 0.1f, 0.1f, 0.7f));
                    var hc = go.GetComponent<HeroController>();
                    GUI.Label(pic, Mathf.CeilToInt(hc != null ? Mathf.Max(0f, hc.RespawnAt - Time.time) : 0f).ToString(), St(22, Color.white, TextAnchor.MiddleCenter));
                }
                GUI.Label(new Rect(pic.xMax + 14 * s, row.y + 8 * s, 290 * s, 26 * s), nick, St(19, me ? new Color(1f, 0.85f, 0.4f) : Color.white));
                GUI.Label(new Rect(pic.xMax + 14 * s, row.y + 32 * s, 290 * s, 20 * s), def != null ? def.name.ToUpper() : "", St(13, new Color(0.6f, 0.62f, 0.66f), TextAnchor.MiddleLeft, false));

                var st = go != null ? go.GetComponent<HeroStats>() : null;
                var lr = new Rect(r.x + 432 * s, row.y + rowH / 2f - 17 * s, 34 * s, 34 * s);
                Box(lr, new Color(0.2f, 0.2f, 0.18f));
                GUI.Label(lr, st != null ? st.level.ToString() : "1", St(16, new Color(0.9f, 0.85f, 0.6f), TextAnchor.MiddleCenter));

                if (allyTeam && go != null)
                {
                    var pr = go.GetComponent<HeroProgress>();
                    var gr = new Rect(r.x + 505 * s, row.y + rowH / 2f - 15 * s, 100 * s, 30 * s);
                    Box(gr, new Color(0.25f, 0.18f, 0.06f));
                    Box(new Rect(gr.x, gr.yMax - 3 * s, gr.width, 3 * s), new Color(0.8f, 0.55f, 0.15f));
                    GUI.Label(gr, pr != null ? pr.Gold.ToString() : "0", St(17, new Color(1f, 0.8f, 0.3f), TextAnchor.MiddleCenter));
                }
                var k = go != null ? KdaOf(go) : new Kda();
                GUI.Label(new Rect(r.x + 630 * s, row.y, 50 * s, rowH), k.k.ToString(), St(18, Color.white, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(r.x + 680 * s, row.y, 50 * s, rowH), k.d.ToString(), St(18, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter, false));
                GUI.Label(new Rect(r.x + 730 * s, row.y, 50 * s, rowH), k.a.ToString(), St(18, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter, false));

                // ульта — только у своих
                var cs = go != null ? go.GetComponent<AbilityCaster>() : null;
                if (allyTeam && cs != null && cs.Abilities != null && cs.Abilities.Length > 3 && cs.Abilities[3].IsLearned && !cs.Abilities[3].IsPassive)
                {
                    var c = new Vector2(r.x + 830 * s, row.y + rowH / 2f);
                    var m0 = GUI.matrix;
                    GUIUtility.RotateAroundPivot(45f, c);
                    Box(new Rect(c.x - 8 * s, c.y - 8 * s, 16 * s, 16 * s), Color.black);
                    Box(new Rect(c.x - 6 * s, c.y - 6 * s, 12 * s, 12 * s), cs.Abilities[3].CooldownLeft <= 0f ? new Color(0.3f, 1f, 0.3f) : new Color(0.45f, 0.45f, 0.45f));
                    GUI.matrix = m0;
                }
                // клик по строке — карточка героя
                if (go != null && GUI.Button(row, GUIContent.none, GUIStyle.none)) { inspect = go; scoreOpen = false; }
                y += rowH;
                slot++;
            }
        }
        GUI.Label(new Rect(r.x, r.yMax + 4 * s, w, 22 * s), "Отпусти Ё — закрыть   ·   клик по строке — инвентарь и способности", St(14, new Color(0.75f, 0.75f, 0.75f), TextAnchor.MiddleLeft, false));
    }

    // ---------- Слева сверху: мои У/С/П и добитые / денаи ----------
    void DrawMyStats()
    {
        var k = KdaOf(PlayerHero);
        var r = new Rect(8 * s, 6 * s, 250 * s, 74 * s);
        HudArea.Add(r);
        Box(r, new Color(0.05f, 0.06f, 0.07f, 0.85f));
        Box(new Rect(r.x, r.yMax - 2 * s, r.width, 2 * s), new Color(0.42f, 0.36f, 0.24f));
        var lab = St(13, new Color(0.62f, 0.64f, 0.68f), TextAnchor.MiddleLeft, false);
        var val = St(20, Color.white);
        // строка 1: убийства / смерти / помощь
        GUI.Label(new Rect(r.x + 10 * s, r.y + 4 * s, 90 * s, 32 * s), "У / С / П", lab);
        GUI.Label(new Rect(r.x + 96 * s, r.y + 4 * s, 150 * s, 32 * s),
            $"<color=#7dff8a>{k.k}</color> / <color=#ff7a6e>{k.d}</color> / <color=#9db7ff>{k.a}</color>", val);
        // строка 2: добитые крипы / денаи
        GUI.Label(new Rect(r.x + 10 * s, r.y + 38 * s, 90 * s, 32 * s), "Добито / Ден.", lab);
        GUI.Label(new Rect(r.x + 120 * s, r.y + 38 * s, 130 * s, 32 * s),
            $"<color=#ffd84a>{k.lh}</color> / <color=#ff9a6e>{k.dn}</color>", val);
    }

    // ---------- Лента убийств справа сверху ----------
    void DrawFeed()
    {
        feed.RemoveAll(f => Time.time > f.until);
        float y = 90 * s, w = 720 * s;
        var st = new GUIStyle(bold) { fontSize = Mathf.RoundToInt(18 * s), alignment = TextAnchor.MiddleRight, richText = true };
        var small = new GUIStyle(st) { fontSize = Mathf.RoundToInt(15 * s) };
        foreach (var f in feed)
        {
            float a = Mathf.Clamp01(f.until - Time.time);
            var lines = f.lines ?? new List<string> { $"<color=#{ColorUtility.ToHtmlStringRGB(f.color)}>{f.text}</color>" };
            float h = 8 * s + 26 * s + (lines.Count - 1) * 21 * s;
            var r = new Rect(Screen.width - w - 16 * s, y, w, h);
            HudArea.Add(r);
            var old = GUI.color;
            // плашка: полоса цвета (зелёная — убили врага, красная — нашего), своё — ярче
            GUI.color = new Color(0f, 0f, 0f, (f.mine ? 0.8f : 0.6f) * a);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(f.color.r, f.color.g, f.color.b, a);
            GUI.DrawTexture(new Rect(r.xMax - 5 * s, r.y, 5 * s, r.height), Texture2D.whiteTexture);
            if (f.mine) { GUI.color = new Color(1f, 0.82f, 0.3f, a); GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2 * s), Texture2D.whiteTexture); }
            GUI.color = new Color(1f, 1f, 1f, a);
            float ly = r.y + 4 * s;
            for (int i = 0; i < lines.Count; i++)
            {
                var style = i == 0 ? st : small;
                float lh = i == 0 ? 26 * s : 21 * s;
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(r.x + 8 * s, ly, r.width - 20 * s, lh), lines[i], style);
                ly += lh;
            }
            GUI.color = old;
            y += h + 6 * s;
        }
    }

    static void Box(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }
}
