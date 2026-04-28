// ============================================================
// FILE: DaySummaryUI.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Day timer + win/lose + end-of-day summary.
//              All UI references are assigned in the Inspector.
//              Supports both keyboard shortcuts AND clickable buttons.
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
 
public class DaySummaryUI : MonoBehaviour
{
    public static DaySummaryUI Instance { get; private set; }
 
    // Public events so other scripts (e.g. CafeDeliveryOrders) can subscribe
    public System.Action<DayPeriod> OnPeriodChangedEvent;
    public System.Action<bool> OnDayEndedEvent;
 
    [Header("Day Settings")]
    [SerializeField] private float dayLengthSeconds = 180f;
    [SerializeField] private int totalDays = 5;
 
    [Header("Time-of-Day Periods (seconds into the day)")]
    [SerializeField] private float lunchRushStart = 60f;
    #pragma warning disable CS0414
    [SerializeField] private float lunchRushEnd = 110f;
    #pragma warning restore CS0414
    [SerializeField] private float afternoonStart = 110f;
    [SerializeField] private float closingStart = 160f;
 
    [Header("Win / Lose")]
    [SerializeField] private int baseDailyGoal = 60;
    [SerializeField] private int goalIncreasePerDay = 25;
    [SerializeField] private int maxAngryCustomers = 3;
    [SerializeField] private bool endDayEarlyWhenGoalMet = false;
 
    [Header("Difficulty (Day 1 baselines)")]
    [SerializeField] private float day1MinSpawnDelay = 8f;
    [SerializeField] private float day1MaxSpawnDelay = 20f;
    [SerializeField] private int day1MaxActiveCustomers = 3;
    [SerializeField] private float day1CustomerPatienceSeconds = 45f;
    [SerializeField] private float day1CustomerMoveSpeed = 3f;
 
    [Header("Difficulty Scaling (by final day)")]
    [SerializeField] private float finalDaySpawnDelayMultiplier = 0.6f;
    [SerializeField] private float finalDayPatienceMultiplier = 0.6f;
    [SerializeField] private float finalDayMoveSpeedMultiplier = 1.2f;
    [SerializeField] private int finalDayExtraMaxCustomers = 2;
 
    private const Key endDayKey = Key.T;
    private const Key alternateEndDayKey = Key.F5;
    private const Key retryKey = Key.R;
    private const Key mainMenuKey = Key.M;
 
    [Header("Summary UI — Text (assign in Inspector)")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI ratingText;
    [SerializeField] private TextMeshProUGUI continueText;
 
    [Header("Summary UI — Buttons (assign in Inspector)")]
    [SerializeField] private Button nextDayButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button visitVillageButton;
    [SerializeField] private Button makeDeliveryButton;
    [SerializeField] private Button mainMenuButton;
 
    private bool dayEnded = false;
    private float dayTimer = 0f;
    private DayPeriod currentPeriod = DayPeriod.Morning;
 
    public enum DayPeriod { Morning, LunchRush, Afternoon, Closing }
 
    // End-of-day state
    private int dailyGoal;
    private int endedDayNumber;
    private bool dayWasWin;
    private bool finalWin;
    private string endReason;
    private bool endedManually;
 
    // References
    private CustomerSpawner customerSpawner;
 
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
 
    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayEnd += ShowSummary;
 
        customerSpawner = FindAnyObjectByType<CustomerSpawner>();
 
        if (summaryPanel != null)
            summaryPanel.SetActive(false);
 
        // Wire button listeners
        if (nextDayButton != null)
            nextDayButton.onClick.AddListener(StartNextDay);
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryDay);
        if (visitVillageButton != null)
            visitVillageButton.onClick.AddListener(GoToVillage);
        if (makeDeliveryButton != null)
            makeDeliveryButton.onClick.AddListener(GoToDelivery);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
 
        BeginDay();
    }
 
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayEnd -= ShowSummary;
    }
 
    void Update()
    {
        if (dayEnded)
        {
            HandleEndScreenInput();
            return;
        }
 
        dayTimer += Time.deltaTime;
        UpdateDayHud();
        UpdateDayPeriod();
 
        // Drive the visual day/night cycle
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.SetTimeFromDayProgress(dayTimer, dayLengthSeconds);
 
        // Update HUD period text with clock time
        if (HUDController.Instance != null && DayNightCycle.Instance != null)
        {
            float hour = DayNightCycle.Instance.GameHour;
            int h = Mathf.FloorToInt(hour);
            int m = Mathf.FloorToInt((hour - h) * 60f);
            string ampm = h < 12 ? "AM" : "PM";
            int h12 = h > 12 ? h - 12 : (h == 0 ? 12 : h);
            string periodName = DayNightCycle.Instance.CurrentPeriod.ToString();
            HUDController.Instance.SetPeriodText($"{h12}:{m:D2} {ampm} - {periodName}");
        }
 
        if (GameManager.Instance != null && maxAngryCustomers > 0 &&
            GameManager.Instance.DissatisfiedCount >= maxAngryCustomers)
        {
            TriggerDayEnd(win: false, reason: $"Too many angry customers ({GameManager.Instance.DissatisfiedCount}/{maxAngryCustomers})");
            return;
        }
 
        if (endDayEarlyWhenGoalMet && GameManager.Instance != null &&
            GameManager.Instance.TotalTips >= dailyGoal)
        {
            TriggerDayEnd(win: true, reason: $"Goal reached (${GameManager.Instance.TotalTips}/${dailyGoal})");
            return;
        }
 
        if (dayTimer >= dayLengthSeconds)
        {
            EvaluateAndTriggerDayEnd();
        }
 
        if (GameInput.WasKeyPressedThisFrame(endDayKey) || GameInput.WasKeyPressedThisFrame(alternateEndDayKey))
        {
            TriggerManualDayEnd();
        }
    }

    void TriggerManualDayEnd()
    {
        int tips = GameManager.Instance != null ? GameManager.Instance.TotalTips : 0;
        endedManually = true;
        TriggerDayEnd(win: false, reason: $"Closed early (${tips}/${dailyGoal})");
    }
 
    void UpdateDayPeriod()
    {
        DayPeriod newPeriod;
        if (dayTimer >= closingStart)
            newPeriod = DayPeriod.Closing;
        else if (dayTimer >= afternoonStart)
            newPeriod = DayPeriod.Afternoon;
        else if (dayTimer >= lunchRushStart)
            newPeriod = DayPeriod.LunchRush;
        else
            newPeriod = DayPeriod.Morning;
 
        if (newPeriod != currentPeriod)
        {
            currentPeriod = newPeriod;
            OnPeriodChanged(newPeriod);
            OnPeriodChangedEvent?.Invoke(newPeriod);
        }
    }
 
    void OnPeriodChanged(DayPeriod period)
    {
        if (AudioManager.Instance == null) return;
 
        switch (period)
        {
            case DayPeriod.Morning:
                AudioManager.Instance.PlayMorningMusic();
                break;
            case DayPeriod.LunchRush:
                AudioManager.Instance.PlayLunchRushMusic();
                if (customerSpawner != null)
                {
                    int day = GetCurrentDaySafe();
                    float t = totalDays <= 1 ? 1f : Mathf.Clamp01((day - 1f) / (totalDays - 1f));
                    float spawnMult = Mathf.Lerp(1f, finalDaySpawnDelayMultiplier, t);
                    float patienceMult = Mathf.Lerp(1f, finalDayPatienceMultiplier, t);
                    float moveSpeedMult = Mathf.Lerp(1f, finalDayMoveSpeedMultiplier, t);
                    int extraCustomers = Mathf.RoundToInt(Mathf.Lerp(0f, finalDayExtraMaxCustomers, t));

                    float rushMin = Mathf.Max(1f, day1MinSpawnDelay * spawnMult * 0.4f);
                    float rushMax = Mathf.Max(2f, day1MaxSpawnDelay * spawnMult * 0.4f);
                    customerSpawner.ConfigureDaySettings(
                        rushMin, rushMax,
                        day1MaxActiveCustomers + extraCustomers + 2,
                        day1CustomerPatienceSeconds * patienceMult * 0.6f,
                        day1CustomerMoveSpeed * moveSpeedMult * 1.2f);
                }
                Debug.Log("[DaySummary] LUNCH RUSH started!");
                break;
            case DayPeriod.Afternoon:
                AudioManager.Instance.PlayAfternoonMusic();
                ApplyDifficultyForDay(GetCurrentDaySafe());
                Debug.Log("[DaySummary] Afternoon — rush is over.");
                break;
            case DayPeriod.Closing:
                AudioManager.Instance.PlayEveningMusic();
                Debug.Log("[DaySummary] Closing time approaching...");
                break;
        }
    }
 
    void BeginDay()
    {
        dayTimer = 0f;
        dayEnded = false;
        endedManually = false;
        currentPeriod = DayPeriod.Morning;

        if (GameManager.Instance != null)
            GameManager.Instance.ResetDayStats();

        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.SnapToHour(6f);

        endedDayNumber = GetCurrentDaySafe();
        if (endedDayNumber > totalDays && totalDays > 0)
        {
            endedDayNumber = totalDays;
            dailyGoal = CalculateDailyGoal(endedDayNumber);
            dayWasWin = true;
            finalWin = true;
            endReason = "All days completed!";
            dayEnded = true;
 
            if (GameManager.Instance != null)
                GameManager.Instance.EndDay();
            else
                ShowSummary();
            return;
        }
 
        dailyGoal = CalculateDailyGoal(endedDayNumber);
        ApplyDifficultyForDay(endedDayNumber);
        UpdateDayHud();
    }
 
    int GetCurrentDaySafe()
    {
        if (GameManager.Instance == null) return 1;
        return Mathf.Max(1, GameManager.Instance.CurrentDay);
    }
 
    int CalculateDailyGoal(int day)
    {
        return Mathf.Max(0, baseDailyGoal + (day - 1) * goalIncreasePerDay);
    }
 
    void ApplyDifficultyForDay(int day)
    {
        if (customerSpawner == null)
            customerSpawner = FindAnyObjectByType<CustomerSpawner>();
        if (customerSpawner == null)
            return;
 
        float t = totalDays <= 1 ? 1f : Mathf.Clamp01((day - 1f) / (totalDays - 1f));
 
        float spawnMult = Mathf.Lerp(1f, finalDaySpawnDelayMultiplier, t);
        float patienceMult = Mathf.Lerp(1f, finalDayPatienceMultiplier, t);
        float moveSpeedMult = Mathf.Lerp(1f, finalDayMoveSpeedMultiplier, t);
        int extraCustomers = Mathf.RoundToInt(Mathf.Lerp(0f, finalDayExtraMaxCustomers, t));
 
        float minDelay = day1MinSpawnDelay * spawnMult;
        float maxDelay = day1MaxSpawnDelay * spawnMult;
        int maxCustomers = day1MaxActiveCustomers + extraCustomers;
 
        float patience = day1CustomerPatienceSeconds * patienceMult;
        float moveSpeed = day1CustomerMoveSpeed * moveSpeedMult;
 
        customerSpawner.ConfigureDaySettings(minDelay, maxDelay, maxCustomers, patience, moveSpeed);
        customerSpawner.ResetForNewDay();
    }
 
    void UpdateDayHud()
    {
        if (HUDController.Instance == null) return;
 
        int day = GetCurrentDaySafe();
        float remaining = Mathf.Max(0f, dayLengthSeconds - dayTimer);
        int tips = GameManager.Instance != null ? GameManager.Instance.TotalTips : 0;
        int angry = GameManager.Instance != null ? GameManager.Instance.DissatisfiedCount : 0;
 
        HUDController.Instance.SetDayInfo(day, totalDays);
        HUDController.Instance.SetTimer(remaining);
        HUDController.Instance.SetGoalInfo(tips, dailyGoal);
        HUDController.Instance.SetStrikes(angry, maxAngryCustomers);
    }
 
    void EvaluateAndTriggerDayEnd()
    {
        int tips = GameManager.Instance != null ? GameManager.Instance.TotalTips : 0;
        bool goalMet = tips >= dailyGoal;
 
        if (!goalMet)
        {
            TriggerDayEnd(win: false, reason: $"Missed goal (${tips}/${dailyGoal})");
            return;
        }
 
        TriggerDayEnd(win: true, reason: $"Goal met (${tips}/${dailyGoal})");
    }
 
    void TriggerDayEnd(bool win, string reason)
    {
        if (dayEnded) return;
 
        endedDayNumber = GetCurrentDaySafe();
        dailyGoal = CalculateDailyGoal(endedDayNumber);
 
        dayWasWin = win;
        finalWin = win && endedDayNumber >= totalDays;
        endReason = reason ?? "";
 
        dayEnded = true;
        OnDayEndedEvent?.Invoke(win);
 
        if (GameManager.Instance != null)
        {
            if (win)
                GameManager.Instance.AdvanceToNextDay();
 
            GameManager.Instance.EndDay();
 
            if (win && SaveManager.Instance != null)
                SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
        }
        else
        {
            ShowSummary();
        }
    }
 
    // ==================== SUMMARY DISPLAY ====================
 
    void ShowSummary()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(true);
 
        int served = GameManager.Instance != null ? GameManager.Instance.CustomersServed : 0;
        int tips = GameManager.Instance != null ? GameManager.Instance.TotalTips : 0;
        float avgSat = GameManager.Instance != null ? GameManager.Instance.AverageSatisfaction : 0f;
        int angry = GameManager.Instance != null ? GameManager.Instance.DissatisfiedCount : 0;
        int totalMoney = GameManager.Instance != null ? GameManager.Instance.PawCoins : 0;
        int lifetimeServed = GameManager.Instance != null ? GameManager.Instance.LifetimeCustomersServed : 0;
        int lifetimeTips = GameManager.Instance != null ? GameManager.Instance.LifetimeTips : 0;

        // Title
        if (titleText != null)
        {
            if (finalWin)
                titleText.text = "~ YOU WIN! ~";
            else if (endedManually)
                titleText.text = $"~ Day {endedDayNumber} Closed Early ~";
            else if (dayWasWin)
                titleText.text = $"~ Day {endedDayNumber} Complete ~";
            else
                titleText.text = $"~ GAME OVER (Day {endedDayNumber}) ~";
        }

        // Stats
        if (statsText != null)
        {
            statsText.text =
                $"Goal:  ${dailyGoal}\n" +
                $"Result:  {(tips >= dailyGoal ? $"${tips} (met)" : $"${tips} (missed)")}\n" +
                (string.IsNullOrEmpty(endReason) ? "" : $"Reason:  {endReason}\n") +
                $"\nCustomers Served:  {served}\n" +
                $"Tips Earned:  ${tips}\n" +
                $"Avg Satisfaction:  {avgSat:P0}\n" +
                $"Customers Lost:  {angry}\n" +
                $"\n--- Lifetime ---\n" +
                $"PawCoins:  ${totalMoney}\n" +
                $"Total Served:  {lifetimeServed}\n" +
                $"Total Tips:  ${lifetimeTips}";
        }
 
        // Rating
        string rating;
        Color ratingColor;
 
        if (finalWin)              { rating = "S RANK\nMeow-sterful!"; ratingColor = new Color(1f, 0.85f, 0f); }
        else if (!dayWasWin)       { rating = "TRY AGAIN"; ratingColor = Color.red; }
        else if (avgSat >= 0.9f)   { rating = "S RANK\nMeow-sterful!"; ratingColor = new Color(1f, 0.85f, 0f); }
        else if (avgSat >= 0.7f)   { rating = "A RANK\nPaw-some!"; ratingColor = Color.green; }
        else if (avgSat >= 0.5f)   { rating = "B RANK\nNot bad!"; ratingColor = Color.yellow; }
        else if (avgSat >= 0.3f)   { rating = "C RANK\nNeeds work..."; ratingColor = new Color(1f, 0.5f, 0.2f); }
        else                       { rating = "D RANK\nRuff day..."; ratingColor = Color.red; }
 
        if (ratingText != null) { ratingText.text = rating; ratingText.color = ratingColor; }
 
        // Configure which buttons are visible
        ConfigureSummaryButtons();
 
        // Update helper text
        if (continueText != null)
        {
            if (finalWin)
                continueText.text = "Congratulations! You completed all days!";
            else if (endedManually)
                continueText.text = "Day ended early. Choose what to do next:";
            else if (dayWasWin)
                continueText.text = "Choose what to do next:";
            else
                continueText.text = "Don't give up!";
        }
 
        if (summaryPanel != null)
            summaryPanel.SetActive(true);
    }
 
    /// <summary>
    /// Shows/hides buttons based on win, lose, or final win state.
    /// </summary>
    void ConfigureSummaryButtons()
    {
        if (finalWin)
        {
            // Final win — only main menu
            SetButtonActive(nextDayButton, false);
            SetButtonActive(retryButton, false);
            SetButtonActive(visitVillageButton, false);
            SetButtonActive(makeDeliveryButton, false);
            SetButtonActive(mainMenuButton, true);
        }
        else if (dayWasWin)
        {
            // Day won — show next day, village, and delivery only when deliveries exist
            int pending = CafeDeliveryOrders.Instance != null ? CafeDeliveryOrders.Instance.pendingDeliveries : 0;
            bool hasDeliveries = pending > 0;

            SetButtonActive(nextDayButton, true);
            SetButtonActive(retryButton, false);
            SetButtonActive(visitVillageButton, true);
            SetButtonActive(makeDeliveryButton, hasDeliveries);
            SetButtonActive(mainMenuButton, true);
        }
        else if (endedManually)
        {
            // Closed early — allow navigation options without counting as a full clear.
            int pending = CafeDeliveryOrders.Instance != null ? CafeDeliveryOrders.Instance.pendingDeliveries : 0;
            bool hasDeliveries = pending > 0;

            SetButtonActive(nextDayButton, false);
            SetButtonActive(retryButton, true);
            SetButtonActive(visitVillageButton, true);
            SetButtonActive(makeDeliveryButton, hasDeliveries);
            SetButtonActive(mainMenuButton, true);
        }
        else
        {
            // Day lost — show retry and main menu; hide win-only options
            SetButtonActive(nextDayButton, false);
            SetButtonActive(retryButton, true);
            SetButtonActive(visitVillageButton, false);
            SetButtonActive(makeDeliveryButton, false);
            SetButtonActive(mainMenuButton, true);
        }
    }
 
    void SetButtonActive(Button btn, bool active)
    {
        if (btn != null) btn.gameObject.SetActive(active);
    }
 
    // ==================== KEYBOARD SHORTCUTS (still work) ====================
 
    void HandleEndScreenInput()
    {
        if (finalWin)
        {
            if (GameInput.ConfirmPressed || GameInput.EnterPressed)
                ReturnToMainMenu();
            return;
        }
 
        if (dayWasWin)
        {
            if (GameInput.ConfirmPressed || GameInput.EnterPressed)
                StartNextDay();
            return;
        }
 
        if (GameInput.WasKeyPressedThisFrame(retryKey))
            RetryDay();
        else if (GameInput.WasKeyPressedThisFrame(mainMenuKey))
            ReturnToMainMenu();
    }
 
    // ==================== NAVIGATION METHODS ====================
    // These are called by both buttons (via onClick) and keyboard shortcuts.
 
    public void StartNextDay()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);
 
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.ShopScene);
    }
 
    public void RetryDay()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetDayStats();
            GameManager.Instance.SetPaused(false);
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
 
    public void GoToVillage()
    {
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
 
        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);
 
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.VillageScene);
    }
 
    public void GoToDelivery()
    {
        int pending = CafeDeliveryOrders.Instance != null
            ? CafeDeliveryOrders.Instance.pendingDeliveries
            : 0;

        if (pending <= 0)
        {
            Debug.LogWarning("[DaySummary] GoToDelivery called, but there are no pending deliveries.");
            return;
        }

        PlayerPrefs.SetInt("PendingDeliveries", pending);
        PlayerPrefs.Save();

        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());

        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.DeliveryScene);
    }
 
    public void ReturnToMainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);
 
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
}