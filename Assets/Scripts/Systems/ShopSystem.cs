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
        items.Add(new ShopItem("Oat Milk", "Plant-based", 15));
        items.Add(new ShopItem("Tapioca", "Boba pearls", 8));
        items.Add(new ShopItem("Brown Sugar", "Caramel", 12));
        items.Add(new ShopItem("Taro Powder", "Purple", 20));
        items.Add(new ShopItem("Matcha", "Premium", 50));
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
            if (txt) txt.text = $"{item.name} - ${item.price}";
            var btn = obj.GetComponent<Button>();
            if (btn) { ShopItem i = item; btn.onClick.AddListener(() => Buy(i)); }
        }
    }
    
    void Buy(ShopItem item)
    {
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
        PopulateItems();
    }
    
    void UpdateMoney() { if (moneyText && GameManager.Instance != null) moneyText.text = "$" + GameManager.Instance.PawCoins; }
}

[System.Serializable]
public class ShopItem
{
    public string name;
    public string description;
    public int price;
    public Sprite icon;
    public ShopItem(string n, string d, int p) { name = n; description = d; price = p; }
}
