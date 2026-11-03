using System.IO;
using UnityEngine;

// Приказы игрока-клиента: уходят хосту, хост выполняет их над героем этого игрока.
public static class NetCmd
{
    enum C : byte { Move = 1, Attack, Stop, Cast, CastPoint, Learn, UseItem, Ticket, Buy, Sell, Swap, Courier, Glyph }

    static void Send(C c, System.Action<BinaryWriter> body = null) =>
        Net.SendToHost(Msg.Cmd, w => { w.Write((byte)c); body?.Invoke(w); });

    static int Id(Unit u) { var e = u != null ? u.GetComponent<NetEntity>() : null; return e != null ? e.id : 0; }

    public static void Move(Vector3 p) => Send(C.Move, w => w.W(p));
    public static void Attack(Unit target) => Send(C.Attack, w => w.Write(Id(target)));
    public static void Stop() => Send(C.Stop);
    public static void Cast(int i, Unit target) => Send(C.Cast, w => { w.Write((byte)i); w.Write(Id(target)); });
    public static void CastPoint(int i, Vector3 p) => Send(C.CastPoint, w => { w.Write((byte)i); w.W(p); });
    public static void Learn(int i) => Send(C.Learn, w => w.Write((byte)i));
    public static void UseItem(int slot, Vector3 p) => Send(C.UseItem, w => { w.Write((byte)slot); w.W(p); });
    public static void Ticket(Vector3 p, bool exact) => Send(C.Ticket, w => { w.W(p); w.Write(exact); });
    public static void Buy(string itemId) => Send(C.Buy, w => w.Write(itemId));
    public static void Sell(int slot) => Send(C.Sell, w => w.Write((byte)slot));
    public static void Swap(int a, int b) => Send(C.Swap, w => { w.Write((byte)a); w.Write((byte)b); });
    public static void Courier() => Send(C.Courier);
    public static void Glyph() => Send(C.Glyph);

    // ---------- Хост: выполнить приказ ----------
    public static void Apply(GameObject hero, BinaryReader r)
    {
        var c = (C)r.ReadByte();
        var unit = hero.GetComponent<Unit>();
        var hc = hero.GetComponent<HeroController>();
        var attack = hero.GetComponent<AutoAttack>();
        var caster = hero.GetComponent<AbilityCaster>();
        var fx = hero.GetComponent<ItemEffects>();
        var inv = hero.GetComponent<Inventory>();
        var wallet = hero.GetComponent<HeroProgress>();
        var gm = GameManager.Instance;
        bool blocked = gm != null && gm.IsGameOver;

        switch (c)
        {
            case C.Move:
            {
                var p = r.V3();
                if (blocked || MatchSetup.InLobby || (hc != null && hc.IsBusyChanneling)) return;
                hc.MoveTo(p);
                break;
            }
            case C.Attack:
            {
                var t = NetEntity.FindUnit(r.ReadInt32());
                if (blocked || MatchSetup.InLobby || t == null || !t.IsAlive || !unit.IsAlive || (hc != null && hc.IsBusyChanneling)) return;
                var st = hero.GetComponent<StatusEffects>();
                if (st != null && st.IsStunned) return;
                bool deny = t.team == unit.team && t.kind == UnitKind.Creep && t.Health != null && t.Health.Current < t.Health.maxHealth * 0.5f;
                if (!unit.IsEnemyOf(t) && !deny) return;
                if (caster != null) caster.CancelPending();
                attack.SetTarget(t);
                break;
            }
            case C.Stop:
                if (!blocked) hc.Stop();
                break;
            case C.Cast:
            {
                int i = r.ReadByte();
                var t = NetEntity.FindUnit(r.ReadInt32());
                if (!blocked && !MatchSetup.InLobby && caster != null) caster.NetCast(i, t, Vector3.zero, false);
                break;
            }
            case C.CastPoint:
            {
                int i = r.ReadByte();
                var p = r.V3();
                if (!blocked && !MatchSetup.InLobby && caster != null) caster.NetCast(i, null, p, true);
                break;
            }
            case C.Learn:
            {
                int i = r.ReadByte();
                if (wallet != null) wallet.Learn(i);
                break;
            }
            case C.UseItem:
            {
                int slot = r.ReadByte();
                var p = r.V3();
                if (!blocked && !MatchSetup.InLobby && fx != null && unit.IsAlive) fx.NetUse(slot, p);
                break;
            }
            case C.Ticket:
            {
                var p = r.V3();
                bool exact = r.ReadBoolean();
                if (blocked || MatchSetup.InLobby || fx == null) return;
                if (exact) fx.TicketTo(p, "Еду в баню...");
                else fx.TicketAtWorld(p);
                break;
            }
            case C.Buy:
            {
                var d = ItemCatalog.Get(r.ReadString());
                var shop = hero.GetComponent<ShopGUI>();
                if (d != null && shop != null && !blocked) shop.Buy(d);
                break;
            }
            case C.Sell:
            {
                int slot = r.ReadByte();
                if (inv != null && wallet != null && !blocked) inv.Sell(slot, wallet);
                break;
            }
            case C.Swap:
            {
                int a = r.ReadByte(), b = r.ReadByte();
                if (inv != null) inv.Swap(a, b);
                break;
            }
            case C.Courier:
            {
                var cr = global::Courier.Of(hero);
                if (cr != null && !blocked) cr.Deliver();
                break;
            }
            case C.Glyph:
                if (!blocked && !MatchSetup.InLobby) global::Glyph.Use(unit.team);
                break;
        }
    }
}
