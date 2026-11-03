using System;
using System.Collections.Generic;
using UnityEngine;

// Предмет в инвентаре (у каждого своя перезарядка).
public class ItemInstance
{
    public ItemDef def;
    public float cooldownLeft;
    public int charges;              // заряды (если у предмета они есть)
    public ItemInstance(ItemDef d) { def = d; charges = d.maxCharges; }
}

// Инвентарь героя: 6 основных слотов (работают) и 3 слота рюкзака (не дают бонусов).
// Слоты 0–5 — основные, 6–8 — рюкзак.
public class Inventory : MonoBehaviour
{
    public const int MainSlots = 6;
    public const int BackpackSlots = 3;
    public const int TotalSlots = MainSlots + BackpackSlots;

    readonly ItemInstance[] slots = new ItemInstance[TotalSlots];

    public event Action OnChanged;
    public int SelectedSlot = -1; // для перестановки предметов

    // Отдельный слот «Билет на электричку» (клавиша T): у всех с начала игры, после смерти докупается сам
    public const string TicketId = "train_ticket";
    public const int MaxTickets = 5;
    public int TpCharges = 1;
    public float TpCooldownLeft;

    // Тайник у бани: что купил вдали от магазина — туда, привезёт курьер
    public const int StashSlots = 6;
    public readonly List<ItemDef> Stash = new List<ItemDef>();

    // «Самокат курьеру» — не предмет, а улучшение курьера
    public const string ScooterId = "courier_scooter";

    public void NotifyChanged() => Changed();

    // Сколько стоят все вещи героя (инвентарь + тайник) — для «ценности» команды
    public int ItemValue()
    {
        int v = 0;
        foreach (var s in slots) if (s != null) v += ItemCatalog.TotalCost(s.def);
        foreach (var d in Stash) v += ItemCatalog.TotalCost(d);
        return v;
    }

    bool BuyScooter(ItemDef def, HeroProgress wallet, out string error)
    {
        var c = Courier.Of(gameObject);
        if (c == null) { error = "Курьера ещё нет"; return false; }
        if (c.Upgraded) { error = "Курьер уже на самокате"; return false; }
        if (wallet.Gold < def.cost) { error = $"Не хватает {def.cost - wallet.Gold} золота"; return false; }
        wallet.AddGold(-def.cost);
        c.Upgrade();
        Changed();
        error = null;
        return true;
    }

    // Покупка вдали от бани: детали, что уже есть у героя, идут в сборку (платишь только за недостающее)
    public bool BuyToStash(ItemDef def, HeroProgress wallet, out string error)
    {
        if (def.id == ScooterId) return BuyScooter(def, wallet, out error);
        var consume = new List<int>();
        int cost = PurchaseCost(def, consume);
        if (wallet.Gold < cost) { error = $"Не хватает {cost - wallet.Gold} золота"; return false; }
        if (Stash.Count >= StashSlots) { error = "Тайник полон — забери вещи курьером (F)"; return false; }
        wallet.AddGold(-cost);
        foreach (int slot in consume) Set(slot, null);
        Stash.Add(def);
        Changed();
        error = null;
        return true;
    }

    // Отдать предмет герою (курьер, тайник). Билет — в слот T.
    public bool Give(ItemDef def)
    {
        if (def.id == TicketId)
        {
            if (TpCharges >= MaxTickets) return false;
            TpCharges++;
            Changed();
            return true;
        }
        return Add(def);
    }

    public void AutoBuyTicket(HeroProgress wallet)
    {
        var d = ItemCatalog.Get(TicketId);
        if (TpCharges > 0 || wallet == null || d == null || wallet.Gold < d.cost) return;
        wallet.AddGold(-d.cost);
        TpCharges = 1;
        Changed();
    }

    public ItemInstance Get(int slot) => slot >= 0 && slot < TotalSlots ? slots[slot] : null;

    void Set(int slot, ItemInstance item)
    {
        slots[slot] = item;
    }

    // Клиент сети: предметы приходят от хоста. true — что-то поменялось
    public bool NetSetSlot(int slot, ItemDef def, float cd, int charges)
    {
        var cur = slots[slot];
        if (def == null) { if (cur == null) return false; slots[slot] = null; return true; }
        if (cur == null || cur.def != def) { slots[slot] = new ItemInstance(def) { cooldownLeft = cd, charges = charges }; return true; }
        cur.cooldownLeft = cd; cur.charges = charges;
        return false;
    }

    void Changed() => OnChanged?.Invoke();

    public int FreeSlot()
    {
        for (int i = 0; i < TotalSlots; i++) if (slots[i] == null) return i;
        return -1;
    }

    public int FreeSlotCount()
    {
        int n = 0;
        foreach (var s in slots) if (s == null) n++;
        return n;
    }

    public bool Add(ItemDef def)
    {
        int slot = FreeSlot();
        if (slot < 0) return false;
        Set(slot, new ItemInstance(def));
        Changed();
        return true;
    }

    public void Remove(int slot)
    {
        if (Get(slot) == null) return;
        Set(slot, null);
        if (SelectedSlot == slot) SelectedSlot = -1;
        Changed();
    }

    public void Swap(int a, int b)
    {
        if (a == b || a < 0 || b < 0 || a >= TotalSlots || b >= TotalSlots) return;
        if (Net.IsClient) { NetCmd.Swap(a, b); (slots[a], slots[b]) = (slots[b], slots[a]); Changed(); return; }
        (slots[a], slots[b]) = (slots[b], slots[a]);
        Changed();
    }

    // В бане заряды (Бутылка кваса) восстанавливаются
    public void RefillCharges()
    {
        bool any = false;
        foreach (var s in slots) if (s != null && s.def.maxCharges > 0 && s.charges < s.def.maxCharges) { s.charges = s.def.maxCharges; any = true; }
        if (any) Changed();
    }

    public bool Has(string id)
    {
        foreach (var s in slots) if (s != null && s.def.id == id) return true;
        return false;
    }

    // Сумма бонусов предметов в основных слотах.
    public ItemStats TotalStats()
    {
        var total = new ItemStats();
        for (int i = 0; i < MainSlots; i++)
            if (slots[i] != null) total += slots[i].def.stats;
        return total;
    }

    public void TickCooldowns(float dt)
    {
        foreach (var s in slots)
            if (s != null && s.cooldownLeft > 0f) s.cooldownLeft = Mathf.Max(0f, s.cooldownLeft - dt);
        if (TpCooldownLeft > 0f) TpCooldownLeft = Mathf.Max(0f, TpCooldownLeft - dt);
    }

    // ---------- Покупка со сборкой, как в MOBA ----------
    // Сколько стоит купить предмет с учётом того, что уже лежит в инвентаре.
    // consume — какие слоты уйдут в сборку.
    public int PurchaseCost(ItemDef def, List<int> consume)
    {
        var used = new HashSet<int>();
        consume.Clear();
        return Plan(def, true, used, consume);
    }

    int Plan(ItemDef d, bool isTop, HashSet<int> used, List<int> consume)
    {
        if (!isTop)
        {
            for (int i = 0; i < TotalSlots; i++)
                if (!used.Contains(i) && slots[i] != null && slots[i].def.id == d.id)
                {
                    used.Add(i);
                    consume.Add(i);
                    return 0; // уже есть — платить не надо
                }
        }
        int c = d.cost;
        foreach (var compId in d.components)
        {
            var comp = ItemCatalog.Get(compId);
            if (comp != null) c += Plan(comp, false, used, consume);
        }
        return c;
    }

    public bool TryBuy(ItemDef def, HeroProgress wallet, out string error)
    {
        if (def.id == ScooterId) return BuyScooter(def, wallet, out error);
        // билет ложится в свой слот, а не в инвентарь
        if (def.id == TicketId)
        {
            if (TpCharges >= MaxTickets) { error = $"Больше {MaxTickets} билетов не влезает"; return false; }
            if (wallet.Gold < def.cost) { error = $"Не хватает {def.cost - wallet.Gold} золота"; return false; }
            wallet.AddGold(-def.cost);
            TpCharges++;
            Changed();
            error = null;
            return true;
        }
        var consume = new List<int>();
        int cost = PurchaseCost(def, consume);
        if (wallet.Gold < cost) { error = $"Не хватает {cost - wallet.Gold} золота"; return false; }
        if (FreeSlotCount() + consume.Count < 1) { error = "Инвентарь полон"; return false; }

        wallet.AddGold(-cost);
        foreach (int slot in consume) Set(slot, null);
        Set(FreeSlot(), new ItemInstance(def));
        SelectedSlot = -1;
        Changed();
        error = null;
        return true;
    }

    public int SellPrice(int slot)
    {
        var it = Get(slot);
        return it == null ? 0 : ItemCatalog.TotalCost(it.def) / 2;
    }

    public void Sell(int slot, HeroProgress wallet)
    {
        int price = SellPrice(slot);
        if (price <= 0 && Get(slot) == null) return;
        if (Net.IsClient) { NetCmd.Sell(slot); return; }
        wallet.AddGold(price);
        Remove(slot);
    }
}
