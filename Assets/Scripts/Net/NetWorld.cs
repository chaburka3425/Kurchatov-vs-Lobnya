using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

// Сетевой номер юнита. У хоста и у клиентов один и тот же юнит имеет один номер.
public class NetEntity : MonoBehaviour
{
    public const ulong NoOwner = ulong.MaxValue;
    public int id;
    public ulong owner = NoOwner;          // какой игрок управляет (для героев людей)
    public bool spawnSent;
    public static readonly Dictionary<int, NetEntity> ById = new Dictionary<int, NetEntity>();

    // клиент: куда двигаться (сглаживание между снимками)
    internal Vector3 targetPos;
    internal float targetYaw;
    internal bool hasState, alive = true;
    internal Unit unit;
    internal UnitAnimator anim;

    public static Unit FindUnit(int id) => id != 0 && ById.TryGetValue(id, out var e) && e != null ? e.unit : null;
    public static int IdOf(Component c) { var e = c != null ? c.GetComponentInParent<NetEntity>() : null; return e != null ? e.id : 0; }

    public void Register(int newId) { id = newId; ById[id] = this; unit = GetComponent<Unit>(); }

    void OnDestroy()
    {
        if (id == 0) return;
        if (ById.TryGetValue(id, out var e) && e == this) ById.Remove(id);
        if (Net.IsHost && spawnSent && NetWorld.Instance != null) NetWorld.Instance.SendDespawn(id);
    }
}

// Мир по сети. Хост: раздаёт номера юнитам, рассылает снимки мира, события и выполняет приказы.
// Клиент: создаёт копии юнитов (без своей логики), двигает их по снимкам, показывает эффекты.
public class NetWorld : MonoBehaviour
{
    public static NetWorld Instance { get; private set; }
    public static bool Replaying;          // клиент: сейчас проигрываем событие от хоста
    int nextId = 10000;
    float snapT, heroT;
    const float SnapEvery = 0.1f, HeroEvery = 0.25f;

    enum Kind : byte { Hero = 1, Creep, Neutral, Courier, Bottle }

    void Awake() { Instance = this; NetEntity.ById.Clear(); }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (var t in new[] { Msg.Spawn, Msg.Despawn, Msg.Snapshot, Msg.Heroes, Msg.Fx, Msg.Text, Msg.Proj, Msg.Anim, Msg.Dmg, Msg.GameOver, Msg.Cmd }) Net.Off(t);
        NetEntity.ById.Clear();
    }

    void Start()
    {
        AssignSceneIds();
        if (Net.IsClient)
        {
            // у клиента своя логика мира выключена — всё решает хост
            foreach (var b in FindObjectsByType<CreepSpawner>(FindObjectsSortMode.None)) b.enabled = false;
            foreach (var b in FindObjectsByType<JungleManager>(FindObjectsSortMode.None)) b.enabled = false;
            foreach (var b in FindObjectsByType<RuneManager>(FindObjectsSortMode.None)) b.enabled = false;
            foreach (var e in NetEntity.ById.Values) Puppetize(e.gameObject, false);
            Net.On(Msg.Spawn, (f, r) => OnSpawn(r));
            Net.On(Msg.Despawn, (f, r) => { var u = NetEntity.FindUnit(r.ReadInt32()); if (u != null) Destroy(u.gameObject); });
            Net.On(Msg.Snapshot, (f, r) => OnSnapshot(r));
            Net.On(Msg.Heroes, (f, r) => OnHeroes(r));
            Net.On(Msg.Fx, (f, r) => OnFx(r));
            Net.On(Msg.Text, (f, r) => { var p = r.V3(); var t = r.ReadString(); var c = r.Col(); int s = r.ReadByte(); FloatingText.Show(p, t, c, s); });
            Net.On(Msg.Proj, (f, r) => OnProj(r));
            Net.On(Msg.Anim, (f, r) => OnAnim(r));
            Net.On(Msg.Dmg, (f, r) => { var t = NetEntity.FindUnit(r.ReadInt32()); var s = NetEntity.FindUnit(r.ReadInt32()); float d = r.ReadSingle(); bool crit = r.ReadBoolean(); var ty = (DamageType)r.ReadByte(); DamageNumbers.Show(t, s, d, crit, ty); });
            Net.On(Msg.GameOver, (f, r) => { var w = (Team)r.ReadByte(); if (GameManager.Instance != null) GameManager.Instance.NetGameOver(w); });
            Net.SendToHost(Msg.SceneReady, w => { });
        }
        else
        {
            Net.On(Msg.Cmd, (from, r) => { var hero = HeroOf(from); if (hero != null) NetCmd.Apply(hero, r); });
        }
    }

    // Юниты, которые уже стоят на карте (башни, троны): номер по месту в иерархии — у всех одинаковый
    void AssignSceneIds()
    {
        var list = new List<(string key, Unit u)>();
        foreach (var u in FindObjectsByType<Unit>(FindObjectsSortMode.None))
            if (u.gameObject.scene.IsValid() && u.GetComponent<NetEntity>() == null) list.Add((PathOf(u.transform), u));
        list.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
        int id = 1;
        foreach (var (key, u) in list)
        {
            var e = u.gameObject.AddComponent<NetEntity>();
            e.Register(id++);
            e.spawnSent = true;   // создавать не нужно — уже есть у всех
        }
    }

    static string PathOf(Transform t)
    {
        var s = "";
        for (; t != null; t = t.parent) s = t.GetSiblingIndex().ToString("D4") + t.name + "/" + s;
        return s;
    }

    public static GameObject HeroOf(ulong client)
    {
        foreach (var e in NetEntity.ById.Values)
            if (e != null && e.owner == client && e.GetComponent<HeroStats>() != null) return e.gameObject;
        return null;
    }

    // ================= ХОСТ =================
    void Update()
    {
        if (Net.IsHost) HostUpdate();
        else if (Net.IsClient) ClientUpdate();
    }

    void HostUpdate()
    {
        // новые юниты: номер и сообщение «создай»
        foreach (var u in Unit.All)
        {
            if (!u.TryGetComponent<NetEntity>(out var e)) e = u.gameObject.AddComponent<NetEntity>();
            if (e.id == 0) e.Register(nextId++);
            if (!e.spawnSent)
            {
                e.spawnSent = true;
                var data = SpawnData(e);
                if (data != null) Net.SendToClients(Msg.Spawn, w => w.Write(data));
            }
        }
        // герои людей: тайник у бани (у них нет своего окна магазина)
        foreach (var e in NetEntity.ById.Values)
            if (e != null && e.owner != NetEntity.NoOwner && e.owner != 0)
            {
                var shop = e.GetComponent<ShopGUI>();
                if (shop != null) shop.TransferStash();
            }

        snapT -= Time.unscaledDeltaTime;
        if (snapT <= 0f) { snapT = SnapEvery; Net.SendToClients(Msg.Snapshot, WriteSnapshot, false); }
        heroT -= Time.unscaledDeltaTime;
        if (heroT <= 0f) { heroT = HeroEvery; Net.SendToClients(Msg.Heroes, WriteHeroes); }
    }

    // Как создать копию этого юнита у клиента. null — не пересылаем.
    static byte[] SpawnData(NetEntity e)
    {
        var u = e.unit;
        if (u == null) return null;
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            w.Write(e.id);
            w.W(u.transform.position);
            w.Write(u.transform.eulerAngles.y);
            var stats = u.GetComponent<HeroStats>();
            var courier = u.GetComponent<Courier>();
            var bottle = u.GetComponent<BeerBottle>();
            if (stats != null)
            {
                w.Write((byte)Kind.Hero);
                w.Write(stats.heroId);
                w.Write((byte)u.team);
                w.Write(e.owner);
                w.Write(u.displayName ?? "");
            }
            else if (courier != null)
            {
                w.Write((byte)Kind.Courier);
                w.Write(courier.owner != null ? NetEntity.IdOf(courier.owner) : 0);
            }
            else if (bottle != null)
            {
                w.Write((byte)Kind.Bottle);
                var kit = bottle.Owner;
                w.Write(kit != null ? NetEntity.IdOf(kit) : 0);
                w.Write(u.Health != null ? u.Health.maxHealth : 100f);
            }
            else if (u.GetComponent<NeutralAI>() != null)
            {
                w.Write((byte)Kind.Neutral);
                w.Write(u.displayName == "Главный гопник");
            }
            else if (u.GetComponent<CreepAI>() != null)
            {
                w.Write((byte)Kind.Creep);
                w.Write((byte)u.team);
                w.Write(u.displayName != null && u.displayName.Contains("рогатк"));
            }
            else return null;
            w.Flush();
            return ms.ToArray();
        }
    }

    // Клиент подгрузил карту — шлём ему всё, что уже создано
    public static void OnClientReady(ulong client)
    {
        if (!Net.IsHost || Instance == null) return;
        foreach (var e in new List<NetEntity>(NetEntity.ById.Values))
        {
            if (e == null || e.id < 10000) continue;
            var data = SpawnData(e);
            if (data != null) Net.SendTo(client, Msg.Spawn, w => w.Write(data));
        }
    }

    // Игрок вышел — его герой дальше играет ботом
    public static void OnPlayerLeft(ulong client, string nick)
    {
        var hero = HeroOf(client);
        if (hero == null) return;
        hero.GetComponent<NetEntity>().owner = NetEntity.NoOwner;
        if (hero.GetComponent<BotHeroAI>() == null) hero.AddComponent<BotHeroAI>();
        FloatingText.Show(hero.transform.position + Vector3.up * 4f, nick + " вышел — играет бот", new Color(1f, 0.6f, 0.4f), 24);
    }

    public void SendDespawn(int id) => Net.SendToClients(Msg.Despawn, w => w.Write(id));

    static short Sh(float v, float k) => (short)Mathf.Clamp(Mathf.RoundToInt(v * k), short.MinValue, short.MaxValue);
    static ushort Us(float v) => (ushort)Mathf.Clamp(Mathf.RoundToInt(v), 0, ushort.MaxValue);

    static void WriteSnapshot(BinaryWriter w)
    {
        w.Write(MatchSetup.MatchTime);
        for (int t = 0; t < 3; t++) w.Write((short)MatchSetup.TeamKills[t]);
        foreach (var t in new[] { Team.Kurchatov, Team.Lobnya }) { w.Write(Glyph.ActiveLeft(t)); w.Write(Glyph.CooldownLeft(t)); }
        var list = new List<NetEntity>();
        foreach (var e in NetEntity.ById.Values) if (e != null && e.unit != null) list.Add(e);
        w.Write((ushort)list.Count);
        foreach (var e in list)
        {
            var u = e.unit;
            var p = u.transform.position;
            w.Write(e.id);
            w.Write(Sh(p.x, 50f)); w.Write(Sh(p.y, 100f)); w.Write(Sh(p.z, 50f));
            w.Write((byte)Mathf.RoundToInt(Mathf.Repeat(u.transform.eulerAngles.y, 360f) / 360f * 255f));
            var mana = u.GetComponent<Mana>();
            var st = u.GetComponent<StatusEffects>();
            byte flags = 0;
            if (u.IsAlive) flags |= 1;
            if (u.gameObject.activeInHierarchy) flags |= 2;
            if (st != null && st.IsInvisible) flags |= 4;
            if (mana != null) flags |= 8;
            w.Write(flags);
            var h = u.Health;
            w.Write(Us(h != null ? h.Current : 0f)); w.Write(Us(h != null ? h.maxHealth : 0f));
            if (mana != null) { w.Write(Us(mana.Current)); w.Write(Us(mana.maxMana)); }
        }
    }

    static void WriteHeroes(BinaryWriter w)
    {
        var list = new List<NetEntity>();
        foreach (var e in NetEntity.ById.Values) if (e != null && e.GetComponent<HeroStats>() != null) list.Add(e);
        w.Write((byte)list.Count);
        var ms = MatchSetup.Instance;
        foreach (var e in list)
        {
            var go = e.gameObject;
            var stats = go.GetComponent<HeroStats>();
            var pr = go.GetComponent<HeroProgress>();
            var hc = go.GetComponent<HeroController>();
            var caster = go.GetComponent<AbilityCaster>();
            var inv = go.GetComponent<Inventory>();
            var st = go.GetComponent<StatusEffects>();
            var fx = go.GetComponent<ItemEffects>();
            w.Write(e.id);
            w.Write((byte)stats.level);
            w.Write(pr != null ? pr.Gold : 0);
            w.Write(pr != null ? pr.Xp : 0f);
            w.Write((byte)(pr != null ? pr.SkillPoints : 0));
            w.Write(hc != null ? Mathf.Max(0f, hc.RespawnAt - Time.time) : 0f);
            var k = ms != null ? ms.KdaArray(go) : new int[5];
            for (int i = 0; i < 5; i++) w.Write((short)k[i]);
            int na = caster != null && caster.Abilities != null ? caster.Abilities.Length : 0;
            w.Write((byte)na);
            for (int i = 0; i < na; i++)
            {
                var a = caster.Abilities[i];
                w.Write((byte)a.Level); w.Write(a.CooldownLeft); w.Write((byte)a.Charges); w.Write(a.ChargeTimer);
            }
            for (int i = 0; i < Inventory.TotalSlots; i++)
            {
                var it = inv != null ? inv.Get(i) : null;
                w.Write(it != null ? it.def.id : "");
                if (it != null) { w.Write(it.cooldownLeft); w.Write((byte)it.charges); }
            }
            w.Write((byte)(inv != null ? inv.TpCharges : 0));
            w.Write(inv != null ? inv.TpCooldownLeft : 0f);
            w.Write((byte)(inv != null ? inv.Stash.Count : 0));
            if (inv != null) foreach (var d in inv.Stash) w.Write(d.id);
            var shown = st != null ? st.Active : null;
            w.Write((byte)(shown != null ? shown.Count : 0));
            if (shown != null)
                foreach (var s in shown)
                {
                    w.Write((byte)s.kind); w.Write(s.name ?? ""); w.Write(s.icon ?? ""); w.Write(s.good);
                    w.Write(s.Left); w.Write(s.end - s.start);
                }
            w.Write(fx != null && fx.IsTeleporting);
            if (fx != null && fx.IsTeleporting) { w.Write(fx.TeleportProgress); w.Write(fx.TeleportLabel ?? ""); }
            var cr = Courier.Of(go);
            w.Write(cr != null);
            if (cr != null) { w.Write(cr.NetState); w.Write(cr.RespawnLeft); w.Write((byte)cr.CarriedCount); w.Write(cr.Upgraded); }
        }
    }

    // ---------- События (хост → клиенты) ----------
    // Эффект: хост рассылает и рисует сам; клиент рисует только пришедшие от хоста
    public static bool SkipFx(byte kind, Vector3 pos, Color c, float a = 0f, float b = 0f, float c2 = 0f, float d = 0f, int n = 0)
    {
        if (Net.IsHost && Instance != null)
        {
            Net.SendToClients(Msg.Fx, w => { w.Write(kind); w.W(pos); w.W(c); w.Write(a); w.Write(b); w.Write(c2); w.Write(d); w.Write(n); }, false);
            return false;
        }
        return Net.IsClient && !Replaying;
    }

    public static bool SkipCoins(Vector3 from, Transform to, int n)
    {
        if (Net.IsHost && Instance != null)
        {
            int id = NetEntity.IdOf(to);
            if (id != 0) Net.SendToClients(Msg.Fx, w => { w.Write((byte)7); w.W(from); w.W(Color.white); w.Write(0f); w.Write(0f); w.Write(0f); w.Write(0f); w.Write(n * 100000 + id % 100000); }, false);
            return false;
        }
        return Net.IsClient && !Replaying;
    }

    static void OnFx(BinaryReader r)
    {
        byte kind = r.ReadByte();
        var p = r.V3(); var c = r.Col();
        float a = r.ReadSingle(), b = r.ReadSingle(), c2 = r.ReadSingle(), d = r.ReadSingle();
        int n = r.ReadInt32();
        Replaying = true;
        try
        {
            switch (kind)
            {
                case 1: Fx.Ring(p, c, a, b, c2, d); break;
                case 2: Fx.Shockwave(p, c, a, b); break;
                case 3: Fx.Pillar(p, c, a, b, c2); break;
                case 4: Fx.Flash(p, c, a, b); break;
                case 5: Fx.Glints(p, c, n, a, b, c2); break;
                case 6: Fx.Burst(p, c, a); break;
                case 7: { var u = NetEntity.FindUnit(n % 100000); if (u != null) Fx.Coins(p, u.transform, n / 100000); break; }
            }
        }
        finally { Replaying = false; }
    }

    public static void TextEvent(Vector3 pos, string text, Color c, int size)
    {
        if (!Net.IsHost || Instance == null) return;
        Net.SendToClients(Msg.Text, w => { w.W(pos); w.Write(text ?? ""); w.W(c); w.Write((byte)Mathf.Clamp(size, 1, 255)); }, false);
    }

    public static void ProjEvent(Vector3 from, Unit target, float speed, ProjectileShape shape, Color c, float size)
    {
        if (Net.IsClient && !Replaying) return;
        if (!Net.IsHost || Instance == null) return;
        int id = NetEntity.IdOf(target);
        if (id == 0) return;
        Net.SendToClients(Msg.Proj, w => { w.W(from); w.Write(id); w.Write(speed); w.Write((byte)shape); w.W(c); w.Write(size); }, false);
    }

    static void OnProj(BinaryReader r)
    {
        var from = r.V3(); var t = NetEntity.FindUnit(r.ReadInt32()); float speed = r.ReadSingle();
        var shape = (ProjectileShape)r.ReadByte(); var c = r.Col(); float size = r.ReadSingle();
        if (t == null) return;
        Replaying = true;
        try { Projectile.Spawn(from, t, null, 0f, speed, shape, c, size); }
        finally { Replaying = false; }
    }

    public static void Anim(GameObject go, byte mode, float arg)
    {
        if (!Net.IsHost || Instance == null) return;
        int id = NetEntity.IdOf(go.transform);
        if (id == 0) return;
        Net.SendToClients(Msg.Anim, w => { w.Write(id); w.Write(mode); w.Write(arg); }, false);
    }

    static void OnAnim(BinaryReader r)
    {
        var u = NetEntity.FindUnit(r.ReadInt32()); byte mode = r.ReadByte(); float arg = r.ReadSingle();
        if (u == null) return;
        var an = UnitAnimator.For(u.gameObject);
        switch (mode)
        {
            case 1: an.Windup(arg); break;
            case 2: an.Strike(); break;
            case 3: an.Cancel(); break;
            case 4: an.Cast((int)arg); break;
        }
    }

    public static void DmgEvent(Unit target, Unit source, float dealt, bool crit, DamageType type)
    {
        if (!Net.IsHost || Instance == null) return;
        int t = NetEntity.IdOf(target), s = NetEntity.IdOf(source);
        if (t == 0) return;
        // цифры урона нужны только тем, чей герой участвует
        foreach (var p in Net.Players)
        {
            if (p.id == 0) continue;
            var hero = HeroOf(p.id);
            if (hero == null) continue;
            int hid = NetEntity.IdOf(hero.transform);
            if (hid != t && hid != s) continue;
            Net.SendTo(p.id, Msg.Dmg, w => { w.Write(t); w.Write(s); w.Write(dealt); w.Write(crit); w.Write((byte)type); }, false);
        }
    }

    public static void GameOverEvent(Team winner)
    {
        if (!Net.IsHost || Instance == null) return;
        Net.SendToClients(Msg.GameOver, w => w.Write((byte)winner));
    }

    // ================= КЛИЕНТ =================
    // Копия юнита: только картинка и данные для интерфейса, вся логика — у хоста
    static readonly HashSet<string> SimTypes = new HashSet<string>
    {
        "AutoAttack", "BotHeroAI", "CreepAI", "NeutralAI", "Tower", "Protected", "CampRespawn", "HeroProgress",
        "MaxGoldKit", "PivmaverKit", "ShashlikKit", "SanyaAbilities", "SausageStash", "BeerBottle", "RuneBuff"
    };

    static void Puppetize(GameObject go, bool mine)
    {
        foreach (var ag in go.GetComponentsInChildren<NavMeshAgent>(true)) ag.enabled = false;
        foreach (var ob in go.GetComponentsInChildren<NavMeshObstacle>(true)) ob.enabled = false;
        foreach (var b in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (b == null) continue;
            string n = b.GetType().Name;
            if (SimTypes.Contains(n)) b.enabled = false;
            else if (!mine && n == "ItemEffects") b.enabled = false;
        }
        var e = go.GetComponent<NetEntity>();
        if (e != null) { e.anim = UnitAnimator.For(go); e.targetPos = go.transform.position; e.targetYaw = go.transform.eulerAngles.y; }
    }

    void OnSpawn(BinaryReader r)
    {
        int id = r.ReadInt32();
        var pos = r.V3();
        float yaw = r.ReadSingle();
        var kind = (Kind)r.ReadByte();
        if (NetEntity.ById.ContainsKey(id)) return;
        GameObject go = null;
        ulong owner = NetEntity.NoOwner;
        switch (kind)
        {
            case Kind.Hero:
            {
                string heroId = r.ReadString();
                var team = (Team)r.ReadByte();
                owner = r.ReadUInt64();
                string disp = r.ReadString();
                go = MatchSetup.Instance != null ? MatchSetup.Instance.NetSpawnHero(heroId, team, owner, disp, pos) : null;
                break;
            }
            case Kind.Creep:
            {
                var team = (Team)r.ReadByte(); bool ranged = r.ReadBoolean();
                go = CreepFactory.Create(team, ranged, pos, new[] { pos }, 1f);
                break;
            }
            case Kind.Neutral:
                go = CreepFactory.CreateNeutral(0, pos, r.ReadBoolean());
                break;
            case Kind.Courier:
            {
                var hero = NetEntity.FindUnit(r.ReadInt32());
                var hc = hero != null ? hero.GetComponent<HeroController>() : null;
                if (hc != null) { var c = Courier.Spawn(hc); go = c != null ? c.gameObject : null; }
                break;
            }
            case Kind.Bottle:
            {
                var hero = NetEntity.FindUnit(r.ReadInt32()); float hp = r.ReadSingle();
                var kit = hero != null ? hero.GetComponent<PivmaverKit>() : null;
                if (kit != null) { var b = BeerBottle.Spawn(kit, pos, hp, 0f); go = b != null ? b.gameObject : null; }
                break;
            }
        }
        if (go == null) return;
        spawns++;
        var e = go.GetComponent<NetEntity>();
        if (e == null) e = go.AddComponent<NetEntity>();
        e.owner = owner;
        e.Register(id);
        e.spawnSent = true;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        Puppetize(go, owner == Net.MyId);
    }

    static float Fs(short v, float k) => v / k;

    void OnSnapshot(BinaryReader r)
    {
        snaps++;
        float mt = r.ReadSingle();
        MatchSetup.NetSetClock(mt);
        for (int t = 0; t < 3; t++) MatchSetup.TeamKills[t] = r.ReadInt16();
        foreach (var t in new[] { Team.Kurchatov, Team.Lobnya }) { float a = r.ReadSingle(), c = r.ReadSingle(); Glyph.NetSet(t, a, c); }
        int n = r.ReadUInt16();
        for (int i = 0; i < n; i++)
        {
            int id = r.ReadInt32();
            var p = new Vector3(Fs(r.ReadInt16(), 50f), Fs(r.ReadInt16(), 100f), Fs(r.ReadInt16(), 50f));
            float yaw = r.ReadByte() / 255f * 360f;
            byte flags = r.ReadByte();
            float hp = r.ReadUInt16(), maxHp = r.ReadUInt16();
            float mana = 0, maxMana = 0;
            if ((flags & 8) != 0) { mana = r.ReadUInt16(); maxMana = r.ReadUInt16(); }
            if (!NetEntity.ById.TryGetValue(id, out var e) || e == null) continue;
            bool alive = (flags & 1) != 0;
            if (!e.hasState || (e.targetPos - p).sqrMagnitude > 64f) { e.transform.position = p; e.transform.rotation = Quaternion.Euler(0, yaw, 0); }
            e.targetPos = p; e.targetYaw = yaw; e.hasState = true;
            var u = e.unit;
            if (u == null) continue;
            if (u.Health != null) u.Health.NetSet(hp, Mathf.Max(1f, maxHp), !alive);
            var m = u.GetComponent<Mana>();
            if (m != null && (flags & 8) != 0) m.NetSet(mana, maxMana);
            var st = u.GetComponent<StatusEffects>();
            if (st != null) st.NetInvisible = (flags & 4) != 0;
            bool active = (flags & 2) != 0;
            if (u.gameObject.activeSelf != active) u.gameObject.SetActive(active);
            // смерть героя: прячем модель, воскрешение — показываем (туман войны потом сам спрячет врагов)
            if (u.kind == UnitKind.Hero && e.alive != alive)
            {
                foreach (var rr in u.GetComponentsInChildren<Renderer>()) rr.enabled = alive;
                foreach (var c in u.GetComponentsInChildren<Collider>()) c.enabled = alive;
            }
            e.alive = alive;
        }
    }

    readonly List<StatusEffects.Shown> shownTmp = new List<StatusEffects.Shown>();

    void OnHeroes(BinaryReader r)
    {
        int n = r.ReadByte();
        var ms = MatchSetup.Instance;
        for (int h = 0; h < n; h++)
        {
            int id = r.ReadInt32();
            NetEntity.ById.TryGetValue(id, out var e);
            var go = e != null ? e.gameObject : null;
            int level = r.ReadByte(); int gold = r.ReadInt32(); float xp = r.ReadSingle(); int sp = r.ReadByte(); float respawn = r.ReadSingle();
            var k = new int[5]; for (int i = 0; i < 5; i++) k[i] = r.ReadInt16();
            int na = r.ReadByte();
            var al = new (int lvl, float cd, int ch, float ct)[na];
            for (int i = 0; i < na; i++) al[i] = (r.ReadByte(), r.ReadSingle(), r.ReadByte(), r.ReadSingle());
            var items = new (string id, float cd, int ch)[Inventory.TotalSlots];
            for (int i = 0; i < Inventory.TotalSlots; i++)
            {
                string iid = r.ReadString();
                items[i] = iid.Length > 0 ? (iid, r.ReadSingle(), (int)r.ReadByte()) : ("", 0f, 0);
            }
            int tp = r.ReadByte(); float tpCd = r.ReadSingle();
            int ns = r.ReadByte(); var stash = new List<string>(); for (int i = 0; i < ns; i++) stash.Add(r.ReadString());
            int nsh = r.ReadByte();
            shownTmp.Clear();
            for (int i = 0; i < nsh; i++)
            {
                var kind = (StatusEffects.Kind)r.ReadByte(); string nm = r.ReadString(), icon = r.ReadString(); bool good = r.ReadBoolean();
                float left = r.ReadSingle(), total = r.ReadSingle();
                shownTmp.Add(new StatusEffects.Shown { kind = kind, name = nm, icon = icon.Length > 0 ? icon : null, good = good, start = Time.time - (total - left), end = Time.time + left });
            }
            bool tele = r.ReadBoolean(); float teleP = 0; string teleL = "";
            if (tele) { teleP = r.ReadSingle(); teleL = r.ReadString(); }
            bool hasCourier = r.ReadBoolean(); byte cst = 0; float cres = 0; int ccar = 0; bool cup = false;
            if (hasCourier) { cst = r.ReadByte(); cres = r.ReadSingle(); ccar = r.ReadByte(); cup = r.ReadBoolean(); }
            if (go == null) continue;

            var stats = go.GetComponent<HeroStats>();
            bool changed = stats.level != level;
            stats.level = level;
            var pr = go.GetComponent<HeroProgress>(); if (pr != null) pr.NetSet(xp, sp, gold);
            var hc = go.GetComponent<HeroController>(); if (hc != null) hc.NetSetRespawn(Time.time + respawn);
            if (ms != null) ms.NetSetKda(go, k);
            var caster = go.GetComponent<AbilityCaster>();
            if (caster != null && caster.Abilities != null)
                for (int i = 0; i < na && i < caster.Abilities.Length; i++)
                {
                    var a = caster.Abilities[i];
                    a.Level = al[i].lvl; a.CooldownLeft = al[i].cd; a.Charges = al[i].ch; a.ChargeTimer = al[i].ct;
                }
            var inv = go.GetComponent<Inventory>();
            if (inv != null)
            {
                bool invChanged = false;
                for (int i = 0; i < Inventory.TotalSlots; i++)
                    invChanged |= inv.NetSetSlot(i, items[i].id.Length > 0 ? ItemCatalog.Get(items[i].id) : null, items[i].cd, items[i].ch);
                inv.TpCharges = tp; inv.TpCooldownLeft = tpCd;
                inv.Stash.Clear(); foreach (var s in stash) { var d = ItemCatalog.Get(s); if (d != null) inv.Stash.Add(d); }
                if (invChanged) { inv.NotifyChanged(); changed = true; }
            }
            if (changed) stats.Recalculate(false);
            var st = go.GetComponent<StatusEffects>(); if (st != null) st.NetSetShown(shownTmp);
            var fx = go.GetComponent<ItemEffects>(); if (fx != null) fx.NetSetTeleport(tele, teleP, teleL);
            if (hasCourier) { var cr = Courier.Of(go); if (cr != null) cr.NetSet(cst, cres, ccar, cup); }
        }
    }

    // Отладка сети: index.html?debug=1
    int snaps, spawns;
    static readonly bool DebugOn = (Application.absoluteURL ?? "").Contains("debug=1");
    static string lastErr = "";
    void OnEnable() { if (DebugOn) Application.logMessageReceived += OnLog; }
    void OnDisable() { Application.logMessageReceived -= OnLog; }
    static void OnLog(string msg, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) lastErr = msg + "\n" + (stack.Length > 300 ? stack.Substring(0, 300) : stack); }
    void OnGUI()
    {
        if (!DebugOn) return;
        GUI.depth = -100;
        var me = HeroController.Player;
        var cam = Camera.main;
        string t = $"{Net.Mode} id={Net.MyId} ents={NetEntity.ById.Count} units={Unit.All.Count} snaps={snaps} spawns={spawns}\n" +
                   $"me={(me != null ? me.name + " " + me.transform.position.ToString("F0") : "нет")} cam={(cam != null ? cam.transform.position.ToString("F0") : "нет")}\n" +
                   $"lobby={MatchSetup.InLobby} t={MatchSetup.MatchTime:F0} ts={Time.timeScale}\n" + lastErr;
        GUI.color = Color.black; GUI.Label(new Rect(11, 101, 900, 400), t); GUI.color = Color.yellow; GUI.Label(new Rect(10, 100, 900, 400), t); GUI.color = Color.white;
    }

    void ClientUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        float k = 1f - Mathf.Exp(-14f * dt);
        foreach (var e in NetEntity.ById.Values)
        {
            if (e == null || !e.hasState) continue;
            var t = e.transform;
            var before = t.position;
            t.position = Vector3.Lerp(before, e.targetPos, k);
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(0, e.targetYaw, 0), k);
            if (e.anim != null && dt > 0f)
            {
                Vector3 d = t.position - before; d.y = 0;
                e.anim.NetSpeed = Mathf.Lerp(e.anim.NetSpeed, d.magnitude / dt, 0.3f);
            }
        }
    }
}
