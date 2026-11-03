using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Курьер как в Доте: живёт у бани, забирает покупки из тайника и несёт их герою.
// F (или кнопка у мини-карты) — «доставить». Его можно убить — тогда вернётся через 40 сек.
// Курьер есть у каждого героя, боты зовут своего сами. «Самокат курьеру» в магазине — быстрее и крепче.
[RequireComponent(typeof(Unit))]
public class Courier : MonoBehaviour
{
    public const float Speed = 7.5f, ScooterSpeed = 12f, RespawnTime = 40f;
    public const int BaseHp = 250, ScooterHp = 450;
    static Texture2D face, portrait;
    static readonly List<Courier> all = new List<Courier>();

    // Курьер этого героя (или null)
    public static Courier Of(GameObject hero)
    {
        foreach (var c in all) if (c != null && c.owner != null && c.owner.gameObject == hero) return c;
        return null;
    }

    public bool Upgraded { get; private set; }

    public HeroController owner;
    Inventory ownerInv;
    Unit unit;
    Health health;
    NavMeshAgent agent;
    BeerBath bath;
    readonly List<ItemDef> carried = new List<ItemDef>();
    enum State { Idle, ToBase, ToHero, Returning, Dead }
    State state = State.Idle;
    float respawnAt;
    Transform model;

    public static Courier Spawn(HeroController hero)
    {
        var inv = hero.GetComponent<Inventory>();
        var fx = hero.GetComponent<ItemEffects>();
        var b = fx != null ? fx.FindOwnBath() : null;
        Vector3 pos = b != null ? b.transform.position + new Vector3(2.5f, 0, -2.5f) : hero.transform.position;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 6f, NavMesh.AllAreas)) pos = hit.position;

        var go = new GameObject("Курьер");
        go.SetActive(false);
        go.transform.position = pos;
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.9f, 0); col.height = 1.8f; col.radius = 0.45f;
        var ag = go.AddComponent<NavMeshAgent>();
        ag.radius = 0.35f; ag.height = 1.8f; ag.speed = Speed; ag.angularSpeed = 720f; ag.acceleration = 40f;
        ag.avoidancePriority = 10; ag.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        var u = go.AddComponent<Unit>();
        u.team = hero.GetComponent<Unit>().team;
        u.kind = UnitKind.Creep;
        u.displayName = "Курьер";
        u.xpBounty = 0; u.goldBounty = 100;
        var h = go.AddComponent<Health>();
        h.maxHealth = BaseHp; h.armor = 0; h.regenPerSecond = 2; h.magicResist = 0f;
        h.SetMax(BaseHp, true);
        go.AddComponent<StatusEffects>();
        var bar = go.AddComponent<HealthBarGUI>();
        bar.heightOffset = 2.3f; bar.width = 50f; bar.height = 6f; bar.showName = true;
        var c = go.AddComponent<Courier>();
        c.owner = hero;
        c.ownerInv = inv;
        c.bath = b;
        c.BuildModel();
        go.SetActive(true);
        return c;
    }

    void Awake()
    {
        unit = GetComponent<Unit>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        health.OnDeath += OnDied;
        all.RemoveAll(c => c == null);
        all.Add(this);
    }

    void OnDestroy() => all.Remove(this);

    bool IsPlayers => owner != null && owner.playerControlled;

    // «Самокат курьеру»: быстрее, крепче, стоит на самокате
    public bool Upgrade()
    {
        if (Upgraded) return false;
        Upgraded = true;
        agent.speed = ScooterSpeed;
        health.SetMaxKeepRatio(ScooterHp);
        BuildScooter();
        if (state == State.Dead) SetVisible(false);
        if (IsPlayers) FloatingText.Show(transform.position + Vector3.up * 3f, "Курьер на самокате!", new Color(1f, 0.85f, 0.3f), 24);
        return true;
    }

    void BuildScooter()
    {
        if (model == null) return;
        var deck = RuntimeMats.Lit(new Color(0.12f, 0.12f, 0.14f));
        var green = RuntimeMats.Lit(new Color(0.3f, 0.85f, 0.35f));
        var wheel = RuntimeMats.Lit(new Color(0.05f, 0.05f, 0.05f));
        // поднимаем пацана на деку
        for (int i = 1; i < model.childCount; i++) model.GetChild(i).localPosition += Vector3.up * 0.18f; // 0 — кольцо команды, остаётся на земле
        var sc = new GameObject("Scooter").transform;
        sc.SetParent(model, false);
        RuntimeMats.Prim(PrimitiveType.Cube, sc, new Vector3(0, 0.14f, 0.05f), new Vector3(0.34f, 0.06f, 1.1f), green);       // дека
        foreach (float z in new[] { -0.45f, 0.55f })
        {
            var w = RuntimeMats.Prim(PrimitiveType.Cylinder, sc, new Vector3(0, 0.09f, z), new Vector3(0.18f, 0.04f, 0.18f), wheel);
            w.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        }
        RuntimeMats.Prim(PrimitiveType.Cylinder, sc, new Vector3(0, 0.62f, 0.58f), new Vector3(0.05f, 0.48f, 0.05f), deck);    // руль-стойка
        var bar = RuntimeMats.Prim(PrimitiveType.Cylinder, sc, new Vector3(0, 1.1f, 0.58f), new Vector3(0.05f, 0.26f, 0.05f), deck);
        bar.transform.localRotation = Quaternion.Euler(0, 0, 90f);
    }

    static Texture2D Face() => face != null ? face : (face = Resources.Load<Texture2D>("Courier/courier_face"));
    public static Texture2D Portrait() => portrait != null ? portrait : (portrait = Resources.Load<Texture2D>("Courier/courier_portrait"));

    // Маленький пацан в чёрном пуховике с золотым рюкзаком и лицом с фотки
    void BuildModel()
    {
        model = new GameObject("Model").transform;
        model.SetParent(transform, false);
        var jacket = RuntimeMats.Lit(new Color(0.07f, 0.07f, 0.08f));
        var pants = RuntimeMats.Lit(new Color(0.15f, 0.15f, 0.18f));
        var white = RuntimeMats.Lit(new Color(0.95f, 0.95f, 0.95f));
        var skin = RuntimeMats.Lit(new Color(0.95f, 0.8f, 0.68f));
        var hair = RuntimeMats.Lit(new Color(0.3f, 0.22f, 0.15f));
        var gold = new Material(RuntimeMats.Lit(new Color(1f, 0.78f, 0.24f)));
        if (gold.HasProperty("_Metallic")) gold.SetFloat("_Metallic", 0.85f);
        if (gold.HasProperty("_Smoothness")) gold.SetFloat("_Smoothness", 0.7f);
        var ring = RuntimeMats.Glow(TeamUtil.GetColor(GetComponent<Unit>().team));

        RuntimeMats.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 0.02f, 0), new Vector3(1.1f, 0.01f, 1.1f), ring);
        foreach (float sd in new[] { -1f, 1f })
        {
            RuntimeMats.Prim(PrimitiveType.Capsule, model, new Vector3(0.13f * sd, 0.35f, 0), new Vector3(0.2f, 0.3f, 0.2f), pants);
            RuntimeMats.Prim(PrimitiveType.Cube, model, new Vector3(0.13f * sd, 0.06f, 0.06f), new Vector3(0.2f, 0.12f, 0.34f), white);
            var arm = RuntimeMats.Prim(PrimitiveType.Capsule, model, new Vector3(0.36f * sd, 0.85f, 0), new Vector3(0.15f, 0.27f, 0.15f), jacket);
            arm.transform.localRotation = Quaternion.Euler(0, 0, 14f * sd);
        }
        RuntimeMats.Prim(PrimitiveType.Capsule, model, new Vector3(0, 0.88f, 0), new Vector3(0.6f, 0.38f, 0.46f), jacket);       // пуховик
        foreach (float y in new[] { 0.72f, 0.92f })
            RuntimeMats.Prim(PrimitiveType.Sphere, model, new Vector3(0, y, 0), new Vector3(0.64f, 0.14f, 0.5f), jacket);
        RuntimeMats.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.88f, 0.26f), new Vector3(0.02f, 0.55f, 0.02f), gold);     // молния
        RuntimeMats.Prim(PrimitiveType.Cube, model, new Vector3(0, 0.95f, -0.32f), new Vector3(0.42f, 0.48f, 0.24f), gold);   // золотой рюкзак
        RuntimeMats.Prim(PrimitiveType.Cylinder, model, new Vector3(0, 1.22f, 0), new Vector3(0.14f, 0.06f, 0.14f), skin);
        var head = RuntimeMats.Prim(PrimitiveType.Sphere, model, new Vector3(0, 1.48f, 0), new Vector3(0.48f, 0.52f, 0.48f), hair);

        // лицо с фотки — только у курьера игрока, у ботов просто голова
        var tex = IsPlayers ? Face() : null;
        if (tex != null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "Face";
            quad.transform.SetParent(model, false);
            quad.transform.localScale = Vector3.one * 0.62f;
            var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
            quad.GetComponent<Renderer>().sharedMaterial = mat;
            var bb = quad.AddComponent<Billboard>();
            bb.anchor = head.transform;
            bb.towardCamera = 0.35f;
        }
    }

    bool OwnerAlive => owner != null && owner.GetComponent<Unit>().IsAlive;
    Vector3 BasePos => bath != null ? bath.transform.position + new Vector3(2.5f, 0, -2.5f) : transform.position;
    bool AtBase => bath != null && Flat(transform.position, bath.transform.position) <= bath.radius + 3f;
    static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

    public bool IsDead => state == State.Dead;
    public bool IsFree => state == State.Idle;
    public float RespawnLeft => Mathf.Max(0f, respawnAt - Time.time);
    public int CarriedCount => Net.IsClient ? netCarried : carried.Count;
    public string StateText => state switch
    {
        State.ToBase => "идёт за вещами",
        State.ToHero => "несёт вещи",
        State.Returning => "возвращается",
        State.Dead => $"убит, вернётся через {Mathf.CeilToInt(RespawnLeft)}",
        _ => "ждёт у бани"
    } + (Upgraded ? " (на самокате)" : "");

    // F / кнопка: доставить вещи из тайника
    public void Deliver()
    {
        if (Net.IsClient) { NetCmd.Courier(); return; }
        if (state == State.Dead) { Say($"Курьер убит — вернётся через {Mathf.CeilToInt(RespawnLeft)} сек."); return; }
        if (ownerInv == null) return;
        if (carried.Count == 0 && ownerInv.Stash.Count == 0) { Say("Тайник пуст — купи что-нибудь в магазине (B)"); return; }
        if (!OwnerAlive) { Say("Герой мёртв — вещи заберёшь у бани"); return; }
        state = carried.Count > 0 ? State.ToHero : State.ToBase;
        agent.isStopped = false;
        Say("Курьер в пути!", new Color(0.6f, 1f, 0.6f));
    }

    // Клиент сети: состояние курьера приходит от хоста
    int netCarried;
    public byte NetState => (byte)state;
    public void NetSet(byte st, float respawnLeft, int carriedCount, bool upgraded)
    {
        var ns = (State)st;
        if ((ns == State.Dead) != (state == State.Dead)) SetVisible(ns != State.Dead);
        state = ns; respawnAt = Time.time + respawnLeft; netCarried = carriedCount;
        if (upgraded && !Upgraded) Upgrade();
    }

    void Update()
    {
        if (MatchSetup.InLobby) return;
        if (Net.IsClient)
        {
            var k = Keyboard.current;
            if (k != null && k.fKey.wasPressedThisFrame && owner != null && owner.playerControlled) NetCmd.Courier();
            return;
        }

        if (state == State.Dead)
        {
            if (Time.time >= respawnAt) Respawn();
            return;
        }

        var kb = Keyboard.current;
        var gm = GameManager.Instance;
        bool blocked = gm != null && (gm.IsPaused || gm.IsGameOver);
        if (!blocked && kb != null && kb.fKey.wasPressedThisFrame && owner != null && owner.playerControlled) Deliver();

        if (!agent.isOnNavMesh) return;
        switch (state)
        {
            case State.ToBase:
                agent.SetDestination(BasePos);
                if (AtBase)
                {
                    carried.AddRange(ownerInv.Stash);
                    ownerInv.Stash.Clear();
                    ownerInv.NotifyChanged();
                    state = carried.Count > 0 && OwnerAlive ? State.ToHero : State.Idle;
                }
                break;
            case State.ToHero:
                if (!OwnerAlive) { state = State.Returning; break; }
                agent.SetDestination(owner.transform.position);
                if (Flat(transform.position, owner.transform.position) < 2.6f)
                {
                    for (int i = carried.Count - 1; i >= 0; i--)
                        if (ownerInv.Give(carried[i])) carried.RemoveAt(i);
                    if (carried.Count > 0) Say("У героя нет места — несу обратно");
                    else if (IsPlayers) FloatingText.Show(owner.transform.position + Vector3.up * 3.8f, "Вещи доставлены!", new Color(0.6f, 1f, 0.6f), 22);
                    ParticleBurst.Spawn(owner.transform.position + Vector3.up * 1.5f, new Color(1f, 0.82f, 0.3f), 12);
                    state = State.Returning;
                }
                break;
            case State.Returning:
                agent.SetDestination(BasePos);
                if (AtBase)
                {
                    // что не влезло — обратно в тайник
                    foreach (var d in carried) if (ownerInv.Stash.Count < Inventory.StashSlots) ownerInv.Stash.Add(d);
                    carried.Clear();
                    ownerInv.NotifyChanged();
                    state = State.Idle;
                }
                break;
            case State.Idle:
                if (!AtBase) agent.SetDestination(BasePos);
                else if (agent.hasPath && agent.remainingDistance < 0.5f) agent.ResetPath();
                break;
        }
    }

    void OnDied(Unit killer)
    {
        // вещи не теряются — возвращаются в тайник
        foreach (var d in carried) if (ownerInv != null && ownerInv.Stash.Count < Inventory.StashSlots) ownerInv.Stash.Add(d);
        carried.Clear();
        if (ownerInv != null) ownerInv.NotifyChanged();
        state = State.Dead;
        respawnAt = Time.time + RespawnTime;
        if (agent.isOnNavMesh) agent.ResetPath();
        SetVisible(false);
        if (owner != null && owner.playerControlled)
            FloatingText.Show(owner.transform.position + Vector3.up * 4.2f, "Курьера убили!", new Color(1f, 0.45f, 0.4f), 26);
    }

    void Respawn()
    {
        agent.Warp(BasePos);
        health.Revive();
        GetComponent<StatusEffects>().ClearAll();
        SetVisible(true);
        state = State.Idle;
    }

    void SetVisible(bool v)
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = v;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = v;
    }

    void Say(string t) => Say(t, new Color(1f, 0.65f, 0.55f));
    void Say(string t, Color c)
    {
        if (!IsPlayers) return; // боты зовут курьера молча
        var at = owner != null ? owner.transform.position : transform.position;
        FloatingText.Show(at + Vector3.up * 3.8f, t, c, 22);
    }

    // ---------- Кнопка курьера у мини-карты и тайник ----------
    void OnGUI()
    {
        if (MatchSetup.InLobby || owner == null || !owner.playerControlled) return;
        GUI.depth = -10;
        float s = Screen.height / 1080f, size = MinimapGUI.SizeFor(s);
        var map = new Rect(10 * s, Screen.height - size - 10 * s, size, size);
        var b = new Rect(map.xMax + 14 * s, map.yMax - 92 * s - 110 * s, 92 * s, 92 * s);
        HudArea.Add(new Rect(b.x, b.y, b.width, b.height + 18 * s));

        Box(new Rect(b.x - 3 * s, b.y - 3 * s, b.width + 6 * s, b.height + 6 * s), state == State.Dead ? new Color(0.5f, 0.15f, 0.12f) : new Color(0.3f, 0.75f, 0.35f));
        var pic = Portrait();
        if (pic != null) GUI.DrawTexture(b, pic, ScaleMode.ScaleAndCrop); else Box(b, new Color(0.15f, 0.15f, 0.18f));
        if (state == State.Dead)
        {
            Box(b, new Color(0, 0, 0, 0.65f));
            GUI.Label(b, Mathf.CeilToInt(RespawnLeft).ToString(), St(30 * s, Color.white, TextAnchor.MiddleCenter));
        }
        GUI.Label(new Rect(b.x + 4 * s, b.y + 2 * s, 30 * s, 24 * s), "F", St(18 * s, Color.white, TextAnchor.UpperLeft));
        int n = carried.Count;
        if (n > 0) GUI.Label(new Rect(b.x, b.yMax - 24 * s, b.width - 4 * s, 22 * s), n.ToString(), St(18 * s, new Color(1f, 0.85f, 0.3f), TextAnchor.LowerRight));
        Box(new Rect(b.x, b.yMax + 2 * s, b.width, 16 * s), new Color(0, 0, 0, 0.7f));
        GUI.Label(new Rect(b.x, b.yMax + 2 * s, b.width, 16 * s), "КУРЬЕР", St(12 * s, new Color(0.7f, 1f, 0.7f), TextAnchor.MiddleCenter));
        if (b.Contains(Event.current.mousePosition))
        {
            var tr = new Rect(b.xMax + 8 * s, b.y, 300 * s, (Upgraded ? 54 : 76) * s);
            Box(tr, new Color(0, 0, 0, 0.85f));
            GUI.Label(new Rect(tr.x + 8 * s, tr.y, tr.width - 16 * s, tr.height), $"<b>Курьер</b> — {StateText}\nF — привезти вещи из тайника" + (Upgraded ? "" : "\nСамокат курьеру — в магазине (B)"), St(14 * s, Color.white, TextAnchor.MiddleLeft));
        }
        if (GUI.Button(b, GUIContent.none, GUIStyle.none)) Deliver();

        // Тайник: вещи, купленные вдали от бани
        if (ownerInv != null && ownerInv.Stash.Count > 0)
        {
            float sz = 46 * s, g = 5 * s;
            var st = new Rect(map.x, map.y - sz - 34 * s, Inventory.StashSlots * (sz + g) + 10 * s, sz + 30 * s);
            HudArea.Add(st);
            Box(st, new Color(0.05f, 0.06f, 0.07f, 0.88f));
            GUI.Label(new Rect(st.x + 6 * s, st.y + 2 * s, st.width, 18 * s), "ТАЙНИК — F, курьер привезёт", St(12 * s, new Color(1f, 0.82f, 0.4f), TextAnchor.MiddleLeft));
            for (int i = 0; i < Inventory.StashSlots; i++)
            {
                var r = new Rect(st.x + 5 * s + i * (sz + g), st.y + 22 * s, sz, sz);
                Box(r, new Color(0.14f, 0.14f, 0.16f));
                if (i >= ownerInv.Stash.Count) continue;
                var d = ownerInv.Stash[i];
                var ic = ItemIcons.Get(d.id);
                if (ic != null) GUI.DrawTexture(r, ic, ScaleMode.ScaleAndCrop);
                else { Box(r, d.color); GUI.Label(r, d.name, St(9 * s, Color.white, TextAnchor.MiddleCenter)); }
            }
        }
    }

    static void Box(Rect r, Color c)
    {
        var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
    }

    static GUIStyle St(float size, Color c, TextAnchor a)
    {
        var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(8, Mathf.RoundToInt(size)), alignment = a, richText = true, fontStyle = FontStyle.Bold, wordWrap = true };
        st.normal.textColor = c;
        return st;
    }
}
