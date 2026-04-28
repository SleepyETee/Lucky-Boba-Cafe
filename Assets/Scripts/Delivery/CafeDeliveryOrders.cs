// ============================================================
// FILE: CafeDeliveryOrders.cs
// DESCRIPTION: Manages delivery orders from cafe scene
// Add this to your Cafe Scene to track online orders
// Then player can choose to do deliveries at day end
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class CafeDeliveryOrders : MonoBehaviour
{
    public static CafeDeliveryOrders Instance { get; private set; }
    
    [Header("Orders")]
    public int pendingDeliveries = 0;
    public int maxDeliveries = 5;
    public float orderChance = 0.3f; // 30% chance per period
    
    [Header("UI - Order Notification")]
    public GameObject orderNotification;
    public TextMeshProUGUI orderCountText;
    public AudioClip orderSound;
    
    [Header("UI - Day End")]
    public GameObject deliveryChoicePanel;
    public TextMeshProUGUI deliveryInfoText;
    public Button doDeliveriesButton;
    public Button skipDeliveriesButton;
    
    private DaySummaryUI subscribedSummary;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        if (doDeliveriesButton)
            doDeliveriesButton.onClick.AddListener(StartDeliveryGame);
        if (skipDeliveriesButton)
            skipDeliveriesButton.onClick.AddListener(SkipDeliveries);
        
        if (deliveryChoicePanel)
            deliveryChoicePanel.SetActive(false);
        
        if (!TrySubscribeToDaySummary())
            StartCoroutine(DeferredSubscribe());
        
        UpdateUI();
    }

    bool TrySubscribeToDaySummary()
    {
        DaySummaryUI summary = DaySummaryUI.Instance;
        if (summary == null)
            summary = FindAnyObjectByType<DaySummaryUI>();

        if (summary == null)
            return false;

        subscribedSummary = summary;
        summary.OnPeriodChangedEvent += OnPeriodChanged;
        summary.OnDayEndedEvent += OnDayEnded;
        return true;
    }

    System.Collections.IEnumerator DeferredSubscribe()
    {
        yield return null;
        TrySubscribeToDaySummary();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (subscribedSummary != null)
        {
            subscribedSummary.OnPeriodChangedEvent -= OnPeriodChanged;
            subscribedSummary.OnDayEndedEvent -= OnDayEnded;
            subscribedSummary = null;
        }
    }
    
    void OnPeriodChanged(DaySummaryUI.DayPeriod period)
    {
        // Random chance for delivery order each period
        if (period != DaySummaryUI.DayPeriod.Closing)
        {
            if (Random.value < orderChance && pendingDeliveries < maxDeliveries)
            {
                AddDeliveryOrder();
            }
        }
    }
    
    void OnDayEnded(bool success)
    {
        if (pendingDeliveries > 0)
        {
            ShowDeliveryChoice();
        }
    }
    
    private Coroutine hideNotifCoroutine;

    public void AddDeliveryOrder()
    {
        if (pendingDeliveries >= maxDeliveries) return;
        pendingDeliveries++;
        
        // Show notification (cancel previous hide timer)
        if (orderNotification)
        {
            orderNotification.SetActive(true);
            if (hideNotifCoroutine != null) StopCoroutine(hideNotifCoroutine);
            hideNotifCoroutine = StartCoroutine(HideNotification());
        }
        
        // Play sound
        if (AudioManager.Instance != null && orderSound != null)
            AudioManager.Instance.PlaySFX(orderSound);
        
        UpdateUI();
        
        Debug.Log($"[Delivery] New online order! Total: {pendingDeliveries}");
    }
    
    System.Collections.IEnumerator HideNotification()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (orderNotification)
            orderNotification.SetActive(false);
    }
    
    void UpdateUI()
    {
        if (orderCountText)
            orderCountText.text = $"Orders: {pendingDeliveries}";
    }
    
    void ShowDeliveryChoice()
    {
        if (deliveryChoicePanel)
            deliveryChoicePanel.SetActive(true);
        
        if (deliveryInfoText)
        {
            int potentialTips = pendingDeliveries * 25; // Rough estimate
            deliveryInfoText.text = $"You have {pendingDeliveries} delivery orders!\n\n" +
                                    $"Potential tips: ~${potentialTips}\n\n" +
                                    $"Do you want to make deliveries?";
        }
    }
    
    public void StartDeliveryGame()
    {
        if (pendingDeliveries <= 0)
        {
            Debug.LogWarning("[Delivery] StartDeliveryGame called with no pending deliveries.");
            return;
        }
        
        // Save pending count for delivery scene
        PlayerPrefs.SetInt("PendingDeliveries", pendingDeliveries);
        PlayerPrefs.Save();
        
        // Load delivery scene
        SceneManager.LoadScene(SceneNames.DeliveryScene);
    }
    
    public void SkipDeliveries()
    {
        // Clear orders (lost potential money)
        pendingDeliveries = 0;
        
        if (deliveryChoicePanel)
            deliveryChoicePanel.SetActive(false);
        
        // Navigate to shop to prepare for next day
        SceneManager.LoadScene(SceneNames.ShopScene);
        
        Debug.Log("[Delivery] Skipped deliveries, heading to shop");
    }
    
    // Call this when returning from delivery scene
    public void OnReturnFromDelivery()
    {
        pendingDeliveries = 0;
        PlayerPrefs.SetInt("PendingDeliveries", 0);
    }
}
