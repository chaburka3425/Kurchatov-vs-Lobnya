using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NetMode { Offline, Host, Client }

// Типы сообщений (первый байт каждого пакета)
public static class Msg
{
    // лобби в меню
    public const byte Hello = 1, Roster = 2, SetTeam = 3, StartMatch = 4, SceneReady = 5;
    // лобби матча
    public const byte LobbyState = 10, Pick = 11, Lane = 12;
    // мир
    public const byte Spawn = 20, Despawn = 21, Snapshot = 22, Heroes = 23, Fx = 24, Text = 25, Proj = 26, Anim = 27, Feed = 28, Dmg = 29, GameOver = 30, Voice = 31;
    // приказы клиента
    public const byte Cmd = 40;
}

// Сеть: хост считает всю игру, клиенты присылают приказы и получают снимки мира.
// Связь — Unity Relay по коду (работает и в браузере), либо напрямую по IP (для теста в одной сети).
public static class Net
{
    public static NetMode Mode { get; private set; } = NetMode.Offline;
    public static bool Online => Mode != NetMode.Offline;
    public static bool IsHost => Mode == NetMode.Host;
    public static bool IsClient => Mode == NetMode.Client;
    public static string Code { get; private set; } = "";
    public static string Status = "";
    public static bool Busy;              // идёт подключение
    public static bool InMatch;           // все в сцене карты
    public static ulong MyId => nm != null ? nm.LocalClientId : 0;
    public const string MapScene = "KurchatovVsLobnya";
    public const string MenuScene = "MainMenu";

    public class Player { public ulong id; public string nick; public Team team; public bool ready; }
    public static readonly List<Player> Players = new List<Player>();
    public static Player Me { get { foreach (var p in Players) if (p.id == MyId) return p; return null; } }
    public static Player Find(ulong id) { foreach (var p in Players) if (p.id == id) return p; return null; }
    public static string MyNick = "Игрок";

    // Ссылка на игру с кодом: друг открывает её — и сразу заходит в комнату
    public static string InviteLink
    {
        get
        {
            string url = Application.absoluteURL;
            if (string.IsNullOrEmpty(url) || Code.StartsWith("IP:")) return "";
            int q = url.IndexOf('?'); if (q >= 0) url = url.Substring(0, q);
            int h = url.IndexOf('#'); if (h >= 0) url = url.Substring(0, h);
            return url + "?join=" + Code;
        }
    }

    // Код из ссылки (?join=КОД), если игру открыли по приглашению
    public static string CodeFromUrl()
    {
        string url = Application.absoluteURL ?? "";
        int i = url.IndexOf("join=", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return "";
        string c = url.Substring(i + 5);
        int end = c.IndexOfAny(new[] { '&', '#' });
        return (end >= 0 ? c.Substring(0, end) : c).Trim().ToUpperInvariant();
    }

    static NetworkManager nm;
    static UnityTransport transport;
    static ISession session;
    static bool registered;
    const string Channel = "mpka";

    static readonly Dictionary<byte, Action<ulong, BinaryReader>> handlers = new Dictionary<byte, Action<ulong, BinaryReader>>();
    public static void On(byte type, Action<ulong, BinaryReader> h) => handlers[type] = h;
    public static void Off(byte type) => handlers.Remove(type);

    // ---------- Подключение ----------
    static void EnsureManager()
    {
        if (nm != null) return;
        var go = new GameObject("NetworkManager");
        UnityEngine.Object.DontDestroyOnLoad(go);
        transport = go.AddComponent<UnityTransport>();
        transport.MaxPayloadSize = 64 * 1024;        // снимок мира бывает больше стандартных 6 КБ
        transport.MaxSendQueueSize = 1024 * 1024;
        nm = go.AddComponent<NetworkManager>();
        nm.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false, ConnectionApproval = false };
        go.AddComponent<NetRunner>();
        nm.OnClientConnectedCallback += OnConnected;
        nm.OnClientDisconnectCallback += OnDisconnected;
    }

    static async Task SignIn()
    {
        // свой профиль на каждый запуск: две вкладки на одном компьютере — два разных игрока
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync(new InitializationOptions().SetProfile("p" + UnityEngine.Random.Range(100000, 999999)));
        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public static async void HostRelay(Team team)
    {
        if (Busy) return;
        Busy = true; Status = "Создаю игру...";
        try
        {
            EnsureManager();
            transport.UseWebSockets = true;
            await SignIn();
            var opts = new SessionOptions { MaxPlayers = 10, IsPrivate = true }
                .WithRelayNetwork()
                .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });
            session = await MultiplayerService.Instance.CreateSessionAsync(opts);
            Code = session.Code;
            BecomeHost(team);
        }
        catch (Exception e) { Fail("Не получилось создать игру", e); }
        Busy = false;
    }

    public static async void JoinRelay(string code)
    {
        if (Busy) return;
        Busy = true; Status = "Подключаюсь...";
        try
        {
            EnsureManager();
            transport.UseWebSockets = true;
            await SignIn();
            Mode = NetMode.Client;   // заранее: соединение может открыться ещё до конца ожидания
            var opts = new JoinSessionOptions().WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant(), opts);
            Code = session.Code;
            if (!helloSent) Status = "Подключено, ждём хоста...";
        }
        catch (Exception e) { Fail("Не получилось войти (проверь код)", e); }
        Busy = false;
    }

    // Напрямую по IP (одна сеть / тест на одном компьютере). Порт 7777, веб-сокеты — чтобы зашёл и браузер.
    public static void HostDirect(Team team, ushort port = 7777)
    {
        EnsureManager();
        transport.UseWebSockets = true;
        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        if (!nm.StartHost()) { Fail("Порт занят", null); return; }
        Code = "IP:" + port;
        BecomeHost(team);
    }

    public static void JoinDirect(string ip, ushort port = 7777)
    {
        EnsureManager();
        transport.UseWebSockets = true;
        transport.SetConnectionData(ip, port);
        if (!nm.StartClient()) { Fail("Не получилось подключиться", null); return; }
        Mode = NetMode.Client;
        Status = "Подключаюсь к " + ip + "...";
    }

    static void BecomeHost(Team team)
    {
        Mode = NetMode.Host;
        Players.Clear();
        Players.Add(new Player { id = NetworkManager.ServerClientId, nick = MyNick, team = team, ready = true });
        Status = "Игра создана. Код: " + Code;
    }

    static void Fail(string what, Exception e)
    {
        if (e != null) Debug.LogException(e);
        Status = what + (e != null ? ": " + e.Message : "");
        Leave(false);
    }

    public static async void Leave(bool toMenu = true)
    {
        var wasOnline = Online;
        Mode = NetMode.Offline;
        InMatch = false;
        Players.Clear();
        registered = false;
        helloSent = false;
        Code = "";
        try { if (session != null) await session.LeaveAsync(); } catch (Exception e) { Debug.LogWarning(e.Message); }
        session = null;
        if (nm != null && nm.IsListening) nm.Shutdown();
        if (toMenu && wasOnline && SceneManager.GetActiveScene().name != MenuScene) { Time.timeScale = 1f; SceneManager.LoadScene(MenuScene); }
    }

    static bool helloSent;

    static void OnConnected(ulong id) { }

    // клиент: как только соединение с хостом готово — представляемся
    static void TrySendHello()
    {
        if (!IsClient || helloSent || nm == null || !nm.IsConnectedClient) return;
        Register();
        if (!registered) return;
        helloSent = true;
        Status = "Подключено!";
        SendToHost(Msg.Hello, w => w.Write(MyNick));
    }

    static void OnDisconnected(ulong id)
    {
        if (IsHost)
        {
            var p = Find(id);
            if (p != null) { Players.Remove(p); BroadcastRoster(); NetWorld.OnPlayerLeft(id, p.nick); }
            return;
        }
        if (IsClient && (id == MyId || id == NetworkManager.ServerClientId))
        {
            Status = "Связь с хостом потеряна";
            Leave();
        }
    }

    internal static void Register()
    {
        if (registered || nm == null || nm.CustomMessagingManager == null) return;
        nm.CustomMessagingManager.RegisterNamedMessageHandler(Channel, Receive);
        registered = true;
        On(Msg.Hello, (from, r) => { if (!IsHost) return; var nick = r.ReadString(); var team = BalancedTeam(); Players.Add(new Player { id = from, nick = nick, team = team }); BroadcastRoster(); });
        On(Msg.SetTeam, (from, r) => { if (!IsHost || InMatch) return; var p = Find(from); if (p != null) { p.team = (Team)r.ReadByte(); BroadcastRoster(); } });
        On(Msg.Roster, (from, r) =>
        {
            Players.Clear();
            int n = r.ReadByte();
            for (int i = 0; i < n; i++) Players.Add(new Player { id = r.ReadUInt64(), nick = r.ReadString(), team = (Team)r.ReadByte() });
        });
        On(Msg.StartMatch, (from, r) => { if (IsClient) LoadMap(); });
        On(Msg.SceneReady, (from, r) => { var p = Find(from); if (p != null) p.ready = true; NetWorld.OnClientReady(from); });
    }

    internal static void Tick()
    {
        if (!registered && nm != null && nm.IsListening) Register();
        TrySendHello();
    }

    static Team BalancedTeam()
    {
        int k = 0, m = 0;
        foreach (var p in Players) if (p.team == Team.Kurchatov) k++; else m++;
        return k <= m ? Team.Kurchatov : Team.Lobnya;
    }

    public static void ChooseTeam(Team t)
    {
        if (IsHost) { var me = Me; if (me != null) me.team = t; BroadcastRoster(); }
        else SendToHost(Msg.SetTeam, w => w.Write((byte)t));
    }

    public static void BroadcastRoster()
    {
        if (!IsHost) return;
        SendToClients(Msg.Roster, w =>
        {
            w.Write((byte)Players.Count);
            foreach (var p in Players) { w.Write(p.id); w.Write(p.nick); w.Write((byte)p.team); }
        });
    }

    // Хост: все — в бой
    public static void StartMatch()
    {
        if (!IsHost) return;
        foreach (var p in Players) p.ready = p.id == NetworkManager.ServerClientId;
        BroadcastRoster();
        SendToClients(Msg.StartMatch, w => { });
        LoadMap();
    }

    static void LoadMap()
    {
        InMatch = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(MapScene);
    }

    // ---------- Отправка ----------
    static readonly MemoryStream buf = new MemoryStream(4096);
    static readonly BinaryWriter bw = new BinaryWriter(buf);

    static byte[] Pack(byte type, Action<BinaryWriter> body)
    {
        buf.SetLength(0);
        bw.Write(type);
        body(bw);
        bw.Flush();
        return buf.ToArray();
    }

    static void SendRaw(byte[] data, ulong? to, bool reliable)
    {
        if (nm == null || !nm.IsListening || nm.CustomMessagingManager == null) return;
        var delivery = data.Length > 1100 ? NetworkDelivery.ReliableFragmentedSequenced : reliable ? NetworkDelivery.ReliableSequenced : NetworkDelivery.UnreliableSequenced;
        using (var w = new FastBufferWriter(data.Length + 8, Allocator.Temp, data.Length + 64))
        {
            w.WriteValueSafe(data.Length);
            w.WriteBytesSafe(data, data.Length);
            if (to.HasValue) nm.CustomMessagingManager.SendNamedMessage(Channel, to.Value, w, delivery);
            else
            {
                // всем клиентам, кроме самого хоста
                var ids = new List<ulong>();
                foreach (var id in nm.ConnectedClientsIds) if (id != NetworkManager.ServerClientId) ids.Add(id);
                if (ids.Count > 0) nm.CustomMessagingManager.SendNamedMessage(Channel, ids, w, delivery);
            }
        }
    }

    public static void SendToClients(byte type, Action<BinaryWriter> body, bool reliable = true)
    {
        if (!IsHost) return;
        SendRaw(Pack(type, body), null, reliable);
    }

    public static void SendTo(ulong client, byte type, Action<BinaryWriter> body, bool reliable = true)
    {
        if (!IsHost || client == NetworkManager.ServerClientId) return;
        SendRaw(Pack(type, body), client, reliable);
    }

    public static void SendToHost(byte type, Action<BinaryWriter> body)
    {
        if (!IsClient) return;
        SendRaw(Pack(type, body), NetworkManager.ServerClientId, true);
    }

    static void Receive(ulong sender, FastBufferReader reader)
    {
        reader.ReadValueSafe(out int len);
        var data = new byte[len];
        reader.ReadBytesSafe(ref data, len);
        using (var ms = new MemoryStream(data))
        using (var r = new BinaryReader(ms))
        {
            byte type = r.ReadByte();
            if (handlers.TryGetValue(type, out var h))
            {
                try { h(sender, r); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

    // ---------- Запись/чтение ----------
    public static void W(this BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
    public static Vector3 V3(this BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    public static void W(this BinaryWriter w, Color c) { w.Write((byte)(Mathf.Clamp01(c.r) * 255)); w.Write((byte)(Mathf.Clamp01(c.g) * 255)); w.Write((byte)(Mathf.Clamp01(c.b) * 255)); w.Write((byte)(Mathf.Clamp01(c.a) * 255)); }
    public static Color Col(this BinaryReader r) => new Color32(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte());
}

// Держит сеть живой между сценами
public class NetRunner : MonoBehaviour
{
    void Update() => Net.Tick();
    void OnApplicationQuit() { if (Net.Online) Net.Leave(false); }
}
