// ============================================================
// FILE: InventorySystem.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Tracks ingredient stock, handles purchasing,
//              morning restocking, and waste from unsold prep.
// ============================================================
using UnityEngine;
using System;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Starting Inventory (auto-stocked on Day 1)")]
    [SerializeField] private InventoryEntry[] startingStock;

    [Header("Morning Restock (free daily delivery)")]
    [SerializeField] private InventoryEntry[] dailyRestock;

    // Runtime inventory: ItemData name → current servings
    private Dictionary<string, int> stock = new Dictionary<string, int>();
    // Map names back to ItemData for lookup
    private Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>();

    // Prep tracking: how much was prepped this morning (for waste calc)
    private Dictionary<string, int> preppedToday = new Dictionary<string, int>();

    public event Action OnInventoryChanged;

    [Serializable]
    public struct InventoryEntry
    {
        public ItemData item;
        public int quantity;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    const string SaveKey = "Inventory";
    const string LastRestockDayKey = "InventoryLastRestockDay";

    void Start()
    {
        // Restore persisted stock first so purchases carry across scenes/days.
        if (PlayerPrefs.HasKey(SaveKey))
        {
            LoadStock();
        }
        else if (GameManager.Instance != null && GameManager.Instance.CurrentDay <= 1)
        {
            // First ever load on Day 1: seed the starting stock.
            ApplyStartingStock();
        }
    }

    // ==================== PERSISTENCE ====================

    void SaveStock()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in stock)
        {
            if (kvp.Value <= 0) continue;
            sb.Append(kvp.Key).Append(':').Append(kvp.Value).Append(';');
        }
        PlayerPrefs.SetString(SaveKey, sb.ToString());
        PlayerPrefs.Save();
    }

    void LoadStock()
    {
        stock.Clear();
        string data = PlayerPrefs.GetString(SaveKey, "");
        foreach (string entry in data.Split(';'))
        {
            if (string.IsNullOrEmpty(entry)) continue;
            string[] parts = entry.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int count) && count > 0)
                stock[parts[0]] = count;
        }
        OnInventoryChanged?.Invoke();
    }

    // ==================== STOCK MANAGEMENT ====================

    public void AddItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return;
        Register(item);
        stock[item.itemName] += quantity;
        OnInventoryChanged?.Invoke();
        SaveStock();
    }

    public bool RemoveItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return false;
        if (GetStock(item) < quantity) return false;
        stock[item.itemName] -= quantity;
        OnInventoryChanged?.Invoke();
        SaveStock();
        return true;
    }

    public int GetStock(ItemData item)
    {
        if (item == null) return 0;
        return stock.TryGetValue(item.itemName, out int val) ? val : 0;
    }

    public bool HasStock(ItemData item, int quantity = 1)
    {
        return GetStock(item) >= quantity;
    }

    /// <summary>
    /// Buy an item from the shop, deducting PawCoins.
    /// </summary>
    public bool PurchaseItem(ItemData item, int bulkCount = 1)
    {
        if (item == null || GameManager.Instance == null) return false;

        // Check reputation gate
        if (ReputationSystem.Instance != null &&
            ReputationSystem.Instance.CurrentStars < item.requiredReputation)
            return false;

        int totalCost = item.buyCost * bulkCount;
        if (!GameManager.Instance.SpendMoney(totalCost)) return false;

        AddItem(item, item.servingsPerPurchase * bulkCount);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCoin();

        Debug.Log($"[Inventory] Purchased {bulkCount}x {item.itemName} for ${totalCost}");
        return true;
    }

    // ==================== DAY LIFECYCLE ====================

    /// <summary>
    /// Called at the start of each day — free basic restock arrives.
    /// </summary>
    public void ApplyMorningRestock()
    {
        if (dailyRestock != null && dailyRestock.Length > 0)
        {
            foreach (var entry in dailyRestock)
            {
                if (entry.item == null) continue;
                AddItem(entry.item, entry.quantity);
            }
        }
        else
        {
            ApplyDefaultDailyRestock();
        }

        preppedToday.Clear();
        Debug.Log("[Inventory] Morning restock delivered.");
    }

    public void ApplyMorningRestockForDay(int day)
    {
        int safeDay = Mathf.Max(1, day);
        if (PlayerPrefs.GetInt(LastRestockDayKey, 0) >= safeDay)
            return;

        ApplyMorningRestock();
        PlayerPrefs.SetInt(LastRestockDayKey, safeDay);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Track ingredients prepped in the morning (for waste calculation).
    /// </summary>
    public void RecordPrep(ItemData item, int quantity)
    {
        if (item == null) return;
        if (!preppedToday.ContainsKey(item.itemName))
            preppedToday[item.itemName] = 0;
        preppedToday[item.itemName] += quantity;
    }

    /// <summary>
    /// Called at end of day. Perishable prepped items that weren't
    /// used are wasted (design doc: "If you prepare too much food
    /// and drink on a slow day, those ingredients will be wasted").
    /// Returns the total value of wasted ingredients.
    /// </summary>
    public int CalculateAndApplyWaste()
    {
        int wastedValue = 0;
        // For now, prepped items that weren't consumed are lost
        // (The actual consumption happens through RecipeData.ConsumeIngredients)
        // This is a stub for future prep-phase implementation
        preppedToday.Clear();
        return wastedValue;
    }

    // ==================== HELPERS ====================

    void ApplyStartingStock()
    {
        if (startingStock != null && startingStock.Length > 0)
        {
            foreach (var entry in startingStock)
            {
                if (entry.item == null) continue;
                AddItem(entry.item, entry.quantity);
            }
        }
        else
        {
            ApplyDefaultStartingStock();
        }

        Debug.Log("[Inventory] Starting stock applied.");
    }

    void ApplyDefaultStartingStock()
    {
        AddItemByName("Green Tea", 12);
        AddItemByName("Black Tea", 12);
        AddItemByName("Regular Milk", 12);
        AddItemByName("Tapioca", 8);
        AddItemByName("Brown Sugar", 6);
        AddItemByName("Taro Powder", 4);
        AddItemByName("Matcha", 3);
        AddItemByName("Local Honey", 2);
        AddItemByName("Mountain Herbs", 2);
    }

    void ApplyDefaultDailyRestock()
    {
        AddItemByName("Green Tea", 6);
        AddItemByName("Black Tea", 6);
        AddItemByName("Regular Milk", 6);
        AddItemByName("Tapioca", 4);
    }

    void Register(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemName)) return;
        if (!stock.ContainsKey(item.itemName))
            stock[item.itemName] = 0;
        if (!itemLookup.ContainsKey(item.itemName))
            itemLookup[item.itemName] = item;
    }

    /// <summary>
    /// Add stock by name (no ItemData ScriptableObject required).
    /// Used by ShopSystem for string-based shop items.
    /// </summary>
    public void AddItemByName(string itemName, int quantity)
    {
        if (string.IsNullOrEmpty(itemName) || quantity <= 0) return;
        if (!stock.ContainsKey(itemName))
            stock[itemName] = 0;
        stock[itemName] += quantity;
        OnInventoryChanged?.Invoke();
        SaveStock();
    }

    public bool RemoveItemByName(string itemName, int quantity = 1)
    {
        if (string.IsNullOrEmpty(itemName) || quantity <= 0) return false;
        if (GetStockByName(itemName) < quantity) return false;
        stock[itemName] -= quantity;
        OnInventoryChanged?.Invoke();
        SaveStock();
        return true;
    }

    /// <summary>
    /// Get stock count by name.
    /// </summary>
    public int GetStockByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return 0;
        return stock.TryGetValue(itemName, out int val) ? val : 0;
    }

    public bool HasAnyStock()
    {
        foreach (int count in stock.Values)
        {
            if (count > 0)
                return true;
        }

        return false;
    }

    public ItemData GetItemByName(string name)
    {
        return itemLookup.TryGetValue(name, out ItemData item) ? item : null;
    }

    /// <summary>
    /// Returns all items currently in stock (name + count).
    /// </summary>
    public Dictionary<string, int> GetAllStock()
    {
        return new Dictionary<string, int>(stock);
    }
}
