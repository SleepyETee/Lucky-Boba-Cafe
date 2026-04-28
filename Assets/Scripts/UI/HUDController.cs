// ============================================================
// FILE: HUDController.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Updates HUD elements (money, stats, day info).
//              All UI references are assigned in the Inspector.
// ============================================================
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI customersText;

    [Header("Day HUD (assign in Inspector)")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI goalText;
    [SerializeField] private TextMeshProUGUI strikesText;

    [Header("Interaction Prompt")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Reputation HUD")]
    [SerializeField] private TextMeshProUGUI reputationText;
    [SerializeField] private TextMeshProUGUI periodText;

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
        SubscribeToManagers();
        RefreshAll();
    }

    void SubscribeToManagers()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged -= UpdateMoney;
            GameManager.Instance.OnCustomerServed -= UpdateCustomers;
            GameManager.Instance.OnMoneyChanged += UpdateMoney;
            GameManager.Instance.OnCustomerServed += UpdateCustomers;
            gmSubscribed = true;
        }

        if (ReputationSystem.Instance != null)
        {
            ReputationSystem.Instance.OnReputationChanged -= UpdateReputation;
            ReputationSystem.Instance.OnStarChanged -= OnStarChanged;
            ReputationSystem.Instance.OnReputationChanged += UpdateReputation;
            ReputationSystem.Instance.OnStarChanged += OnStarChanged;
        }
    }

    private bool gmSubscribed = false;

    void LateUpdate()
    {
        if (!gmSubscribed && GameManager.Instance != null)
        {
            SubscribeToManagers();
            RefreshAll();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged -= UpdateMoney;
            GameManager.Instance.OnCustomerServed -= UpdateCustomers;
        }

        if (ReputationSystem.Instance != null)
        {
            ReputationSystem.Instance.OnReputationChanged -= UpdateReputation;
            ReputationSystem.Instance.OnStarChanged -= OnStarChanged;
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
            Debug.LogError("[HUDController] GameManager.Instance is null at Start! HUD will not update.");
            UpdateMoney(0);
            UpdateCustomers(0);
        }

        if (ReputationSystem.Instance != null)
            UpdateReputation(ReputationSystem.Instance.ReputationPoints);
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

    // ==================== DAY HUD ====================

    public void SetDayInfo(int day, int totalDays)
    {
        if (dayText == null) return;
        if (totalDays > 0)
            dayText.text = $"Day: {day}/{totalDays}";
        else
            dayText.text = $"Day: {day}";
    }

    public void SetTimer(float remainingSeconds)
    {
        if (timerText == null) return;
        remainingSeconds = Mathf.Max(0f, remainingSeconds);
        int total = Mathf.CeilToInt(remainingSeconds);
        int minutes = total / 60;
        int seconds = total % 60;
        timerText.text = $"Time: {minutes:0}:{seconds:00}";
    }

    public void SetGoalInfo(int currentTips, int goal)
    {
        if (goalText == null) return;
        goalText.text = $"Goal: ${currentTips}/${goal}";
    }

    public void SetStrikes(int angryCount, int maxAngry)
    {
        if (strikesText == null) return;
        if (maxAngry > 0)
            strikesText.text = $"Angry: {angryCount}/{maxAngry}";
        else
            strikesText.text = "";
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

    // ==================== REPUTATION HUD ====================

    void UpdateReputation(float points)
    {
        if (reputationText == null) return;
        int stars = ReputationSystem.Instance != null ? ReputationSystem.Instance.CurrentStars : 1;
        reputationText.text = $"Rep: {stars}/5";
    }

    void OnStarChanged(int oldStars, int newStars)
    {
        UpdateReputation(0f);
    }

    // ==================== PERIOD DISPLAY ====================

    public void SetPeriodText(string text)
    {
        if (periodText != null) periodText.text = text;
    }
}
