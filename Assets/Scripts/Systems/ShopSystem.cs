using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance { get; private set; }
    
    [Header("UI")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button closeButton;
    
    [Header("Items")]
    private List<ShopItem> items = new List<ShopItem>();
    
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    void OnDestroy() { if (Instance == this) Instance = null; }
    
    void Start()
    {
        if (closeButton) closeButton.onClick.AddListener(CloseShop);
        InitItems();
    }
    
    void InitItems()
    {
        items.Add(new ShopItem("Green Tea", "Basic tea", 10));
        items.Add(new ShopItem("Black Tea", "Bold tea", 10));
        items.Add(new ShopItem("Regular Milk", "Fresh milk", 5));
        items.Add(new ShopItem("Tapioca", "Boba pearls", 8));
        items.Add(new ShopItem("Oat Milk", "Plant-based", 15, requiredStars: 2));
        items.Add(new ShopItem("Brown Sugar", "Caramel", 12, requiredStars: 2));
        items.Add(new ShopItem("Taro Powder", "Purple", 20, requiredStars: 2));
        items.Add(new ShopItem("Matcha", "Premium", 50, requiredStars: 3));
        items.Add(new ShopItem("Local Honey", "Village honey", 25, requiredStars: 2));
        items.Add(new ShopItem("Mountain Herbs", "Fragrant herbs", 30, requiredStars: 3));
    }
    
    public void OpenShop()
    {
        if (shopPanel) shopPanel.SetActive(true);
        if (dialogueText) dialogueText.text = "Welcome to Whisker's Supplies!";
        UpdateMoney();
        PopulateItems();
    }
    
    public void CloseShop() { if (shopPanel) shopPanel.SetActive(false); }
    
    void PopulateItems()
    {
        if (itemContainer == null || itemPrefab == null) return;
        foreach (Transform c in itemContainer) Destroy(c.gameObject);
        foreach (var item in items)
        {
            var obj = Instantiate(itemPrefab, itemContainer);
            var txt = obj.GetComponentInChildren<TextMeshProUGUI>();
            bool unlocked = IsItemUnlocked(item);
            if (txt)
            {
                txt.text = unlocked
                    ? $"{item.name} - ${item.price}"
                    : $"{item.name} - Unlocks at {item.requiredStars} stars";
            }
            var btn = obj.GetComponent<Button>();
            if (btn)
            {
                btn.interactable = unlocked;
                ShopItem i = item;
                btn.onClick.AddListener(() => Buy(i));
            }
        }
    }
    
    void Buy(ShopItem item)
    {
        if (!IsItemUnlocked(item))
        {
            if (dialogueText) dialogueText.text = $"Reach {item.requiredStars} stars to buy {item.name}.";
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.SpendMoney(item.price))
        {
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.AddItemByName(item.name, 1);

            if (dialogueText) dialogueText.text = $"Great choice! Here's your {item.name}~";
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMoney();
        }
        else
        {
            if (dialogueText) dialogueText.text = "You need more PawCoins...";
        }
        UpdateMoney();
    }
    
    void UpdateMoney() { if (moneyText && GameManager.Instance != null) moneyText.text = "$" + GameManager.Instance.PawCoins; }

    bool IsItemUnlocked(ShopItem item)
    {
        int stars = ReputationSystem.Instance != null ? ReputationSystem.Instance.CurrentStars : 1;
        return stars >= item.requiredStars;
    }
}

[System.Serializable]
public class ShopItem
{
    public string name;
    public string description;
    public int price;
    public int requiredStars;
    public Sprite icon;
    public ShopItem(string n, string d, int p, int requiredStars = 0)
    {
        name = n;
        description = d;
        price = p;
        this.requiredStars = requiredStars;
    }
}
