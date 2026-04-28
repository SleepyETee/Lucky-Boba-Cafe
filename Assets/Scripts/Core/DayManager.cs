using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }
    
    public enum TimePeriod { Morning, LunchRush, Afternoon, Delivery, Closing, Ended }
    
    [Header("State")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private TimePeriod currentPeriod = TimePeriod.Morning;
    [SerializeField] private float currentTime = 6f;
    
    [Header("Settings")]
    [SerializeField] private float realSecondsPerGameHour = 30f;
    [SerializeField] private int[] dayMoneyGoals = { 100, 150, 200, 300, 500 };
    
    [Header("Daily Stats")]
    private int todayEarnings = 0;
    private int todayAngryCustomers = 0;
    private int maxAngryCustomers = 3;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI periodText;
    [SerializeField] private TextMeshProUGUI goalText;
    [SerializeField] private Slider dayProgressSlider;
    [SerializeField] private GameObject dayEndPanel;
    [SerializeField] private TextMeshProUGUI dayEndTitle;
    [SerializeField] private TextMeshProUGUI dayEndStats;
    [SerializeField] private Button nextDayButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button goToVillageButton;
    [SerializeField] private Button goToDeliveryButton;
    
    private bool dayRunning = false;
    
    public System.Action<TimePeriod> OnPeriodChanged;
    public System.Action<bool> OnDayEnded;
    
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    void OnDestroy() { if (Instance == this) Instance = null; }
    
    void Start()
    {
        // If DaySummaryUI exists in this scene, it is the authoritative day system.
        // Disable DayManager to prevent duplicate timers, music changes, and end-day calls.
        if (FindAnyObjectByType<DaySummaryUI>() != null)
        {
            Debug.Log("[DayManager] DaySummaryUI found — disabling DayManager to avoid conflicts.");
            enabled = false;
            return;
        }
        
        if (dayEndPanel) dayEndPanel.SetActive(false);
        if (nextDayButton) nextDayButton.onClick.AddListener(StartNextDay);
        if (retryButton) retryButton.onClick.AddListener(RetryDay);
        if (goToVillageButton) goToVillageButton.onClick.AddListener(GoToVillage);
        if (goToDeliveryButton) goToDeliveryButton.onClick.AddListener(GoToDelivery);
    }
    
    void Update()
    {
        if (!dayRunning) return;
        currentTime += (1f / Mathf.Max(0.01f, realSecondsPerGameHour)) * Time.deltaTime;
        UpdatePeriod();
        UpdateUI();

        // Drive visual day/night cycle
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.SetGameHour(currentTime);

        if (currentTime >= 19f) EndDay();
    }
    
    public void StartDay(int day)
    {
        currentDay = day; currentTime = 6f; currentPeriod = TimePeriod.Morning;
        todayEarnings = 0; todayAngryCustomers = 0; dayRunning = true;
        
        if (GameManager.Instance != null)
            GameManager.Instance.ResetDayStats();

        if (dayEndPanel) dayEndPanel.SetActive(false);
        OnPeriodChanged?.Invoke(currentPeriod);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMorningMusic();

        // Snap lighting to dawn
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.SnapToHour(6f);
    }
    
    public void EndDay()
    {
        if (!dayRunning) return;
        dayRunning = false;
        int goal = dayMoneyGoals[Mathf.Min(currentDay - 1, dayMoneyGoals.Length - 1)];
        bool success = todayEarnings >= goal && todayAngryCustomers < maxAngryCustomers;
        ShowDayEnd(success, goal);
        OnDayEnded?.Invoke(success);
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
    }
    
    void ShowDayEnd(bool success, int goal)
    {
        if (dayEndPanel) dayEndPanel.SetActive(true);
        if (dayEndTitle) dayEndTitle.text = success ? $"Day {currentDay} Complete!" : $"Day {currentDay} Failed...";
        if (dayEndStats) dayEndStats.text = $"Earnings: ${todayEarnings} / ${goal}\nAngry Customers: {todayAngryCustomers}";
        if (nextDayButton) nextDayButton.gameObject.SetActive(success);
        if (goToVillageButton) goToVillageButton.gameObject.SetActive(success);
        if (goToDeliveryButton) goToDeliveryButton.gameObject.SetActive(success);
        if (retryButton) retryButton.gameObject.SetActive(!success);
    }
    
    public void StartNextDay()
    {
        if (dayEndPanel) dayEndPanel.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.AdvanceToNextDay();
        StartDay(currentDay + 1);
    }
    public void RetryDay() { if (dayEndPanel) dayEndPanel.SetActive(false); StartDay(currentDay); }
    public void GoToVillage() { UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.VillageScene); }
    public void GoToDelivery()
    {
        int deliveries = Random.Range(1, 4);
        PlayerPrefs.SetInt("PendingDeliveries", deliveries);
        PlayerPrefs.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.DeliveryScene);
    }
    
    void UpdatePeriod()
    {
        TimePeriod newPeriod = currentTime < 11 ? TimePeriod.Morning : currentTime < 14 ? TimePeriod.LunchRush : currentTime < 17 ? TimePeriod.Afternoon : currentTime < 18 ? TimePeriod.Delivery : TimePeriod.Closing;
        if (newPeriod != currentPeriod) { currentPeriod = newPeriod; OnPeriodChanged?.Invoke(currentPeriod); }
    }
    
    void UpdateUI()
    {
        if (timeText) { int h = Mathf.FloorToInt(currentTime); int m = Mathf.FloorToInt((currentTime - h) * 60); timeText.text = $"{(h > 12 ? h - 12 : h)}:{m:D2} {(h < 12 ? "AM" : "PM")}"; }
        if (dayText) dayText.text = $"Day {currentDay}";
        if (periodText) periodText.text = currentPeriod == TimePeriod.LunchRush ? "LUNCH RUSH" : currentPeriod.ToString();
        if (goalText) goalText.text = $"${todayEarnings} / ${dayMoneyGoals[Mathf.Min(currentDay - 1, dayMoneyGoals.Length - 1)]}";
        if (dayProgressSlider) dayProgressSlider.value = (currentTime - 6f) / 13f;
    }
    
    public void AddEarnings(int amount) { todayEarnings += amount; }
    public void AddAngryCustomer() { todayAngryCustomers++; if (todayAngryCustomers >= maxAngryCustomers) EndDay(); }
    public int CurrentDay => currentDay;
    public TimePeriod CurrentPeriod => currentPeriod;
    public float GetCustomerSpawnRate() => currentPeriod == TimePeriod.LunchRush ? 5f : currentPeriod == TimePeriod.Morning ? 15f : 10f;
    public float GetCustomerPatienceMultiplier() => currentPeriod == TimePeriod.LunchRush ? 0.6f : 1f;
}
