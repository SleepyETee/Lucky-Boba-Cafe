// ============================================================
// FILE: HUDController.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Updates HUD elements (money, stats)
// ============================================================
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI customersText;
    
    [Header("Interaction Prompt")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged += UpdateMoney;
            GameManager.Instance.OnCustomerServed += UpdateCustomers;
        }
        
        RefreshAll();
    }
    
    void OnDestroy()
    {
        // Unsubscribe
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged -= UpdateMoney;
            GameManager.Instance.OnCustomerServed -= UpdateCustomers;
        }
    }
    
    // ==================== UPDATES ====================
    
    void RefreshAll()
    {
        if (GameManager.Instance != null)
        {
            UpdateMoney(GameManager.Instance.PawCoins);
            UpdateCustomers(GameManager.Instance.CustomersServed);
        }
        else
        {
            UpdateMoney(100);
            UpdateCustomers(0);
        }
    }
    
    void UpdateMoney(int amount)
    {
        if (moneyText != null)
            moneyText.text = $"${amount}";
    }
    
    void UpdateCustomers(int count)
    {
        if (customersText != null)
            customersText.text = $"Served: {count}";
    }
    
    // ==================== INTERACTION PROMPT ====================
    
    public void ShowPrompt(string message)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText != null) promptText.text = message;
    }
    
    public void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }
}
