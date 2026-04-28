// ============================================================
// FILE: DeliveryGameManager.cs
// DESCRIPTION: Main controller for the Delivery Racing Minigame
// THIS IS THE UNIQUE PROJECT 2 MECHANIC - Different from cafe!
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DeliveryGameManager : MonoBehaviour
{
    public static DeliveryGameManager Instance { get; private set; }
    
    // ==================== GAME STATE ====================
    [Header("Game State")]
    public bool isPlaying = false;
    public bool isPaused = false;
    public int currentDeliveryIndex = 0;
    public int totalDeliveries = 3;
    public int successfulDeliveries = 0;
    public int totalTips = 0;
    
    // ==================== TIMER ====================
    [Header("Timer Settings")]
    public float startingTime = 60f;
    public float currentTime;
    public float bonusTimePerDelivery = 15f;
    public float penaltyTimeOnCrash = 3f;
    
    // ==================== DELIVERY POINTS ====================
    [Header("Delivery Points")]
    public List<DeliveryPoint> allDeliveryPoints = new List<DeliveryPoint>();
    public DeliveryPoint currentTarget;
    public int baseDeliveryReward = 20;
    
    // ==================== PLAYER ====================
    [Header("Player")]
    public DeliveryScooter playerScooter;
    public Transform playerSpawnPoint;
    
    // ==================== UI - HUD ====================
    [Header("UI - HUD")]
    public GameObject gameHUD;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI deliveryCountText;
    public TextMeshProUGUI tipsText;
    public Slider boostSlider;
    public Image boostFillImage;
    public RectTransform arrowIndicator;
    public TextMeshProUGUI distanceText;
    
    // ==================== UI - SCREENS ====================
    [Header("UI - Start Screen")]
    public GameObject startPanel;
    public TextMeshProUGUI startInfoText;
    public Button startButton;
    
    [Header("UI - Results")]
    public GameObject resultsPanel;
    public TextMeshProUGUI resultsTitleText;
    public TextMeshProUGUI resultsStatsText;
    public Button continueButton;
    public Button retryButton;
    
    [Header("UI - Pause")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public Button resumeButton;
    public Button settingsButton;
    public Button quitToMenuButton;
    public Button quitGameButton;
    
    // ==================== AUDIO ====================
    [Header("Audio")]
    public AudioClip deliveryMusic;
    public AudioClip deliveryCompleteSound;
    public AudioClip gameCompleteSound;
    public AudioClip crashSound;
    public AudioClip countdownBeep;
    public AudioClip tipSound;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
    
    void Start()
    {
        SetupButtons();
        FindDeliveryPoints();
        ShowStartScreen();
        
        // Play music
        if (AudioManager.Instance != null && deliveryMusic != null)
            AudioManager.Instance.PlayMusic(deliveryMusic);

        // Debug: verify all panel/button references
        Debug.Log($"[Delivery] startPanel assigned: {startPanel != null}");
        Debug.Log($"[Delivery] startButton assigned: {startButton != null}");
        Debug.Log($"[Delivery] gameHUD assigned: {gameHUD != null}");
        Debug.Log($"[Delivery] pausePanel assigned: {pausePanel != null}");
        Debug.Log($"[Delivery] resultsPanel assigned: {resultsPanel != null}");
    }
    
    void Update()
    {
        // Allow Enter to start the game when start panel is showing
        if (!isPlaying && startPanel != null && startPanel.activeSelf)
        {
            if (GameInput.EnterPressed)
            {
                Debug.Log("[Delivery] Starting game via Enter key!");
                StartGame();
                return;
            }
        }

        if (!isPlaying) return;

        if (GameInput.PausePressed)
            TogglePause();

        if (isPaused) return;

        UpdateTimer();
        UpdateHUD();
        UpdateArrowIndicator();
    }
    
    // ==================== SETUP ====================
    
    void SetupButtons()
    {
        if (startButton) startButton.onClick.AddListener(StartGame);
        if (continueButton) continueButton.onClick.AddListener(ReturnToCafe);
        if (retryButton) retryButton.onClick.AddListener(RestartGame);
        if (resumeButton) resumeButton.onClick.AddListener(ResumeGame);
        if (settingsButton) settingsButton.onClick.AddListener(OpenSettings);
        if (quitToMenuButton) quitToMenuButton.onClick.AddListener(ReturnToCafe);
        if (quitGameButton) quitGameButton.onClick.AddListener(QuitGame);
        
        // Hide panels
        if (resultsPanel) resultsPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (gameHUD) gameHUD.SetActive(false);
        WireSettingsBackButton();
    }
    
    void FindDeliveryPoints()
    {
        if (allDeliveryPoints.Count == 0)
        {
            DeliveryPoint[] points = FindObjectsByType<DeliveryPoint>();
            allDeliveryPoints.AddRange(points);
        }
        
        // Disable all initially
        foreach (var point in allDeliveryPoints)
            point.SetActive(false);
        
        Debug.Log($"[Delivery] Found {allDeliveryPoints.Count} delivery points");
    }
    
    // ==================== GAME FLOW ====================
    
    void ShowStartScreen()
    {
        if (startPanel) startPanel.SetActive(true);
        
        // Get pending deliveries from cafe
        totalDeliveries = PlayerPrefs.GetInt("PendingDeliveries", 3);
        totalDeliveries = Mathf.Clamp(totalDeliveries, 1, 5);
        
        if (startInfoText)
            startInfoText.text = $"{totalDeliveries} Orders to Deliver!\n\n" +
                                 $"Time Limit: {startingTime}s\n" +
                                 $"Earn tips for fast delivery!\n\n" +
                                 $"Controls:\n" +
                                 $"WASD - Drive\n" +
                                 $"SPACE - Boost";
    }
    
    public void StartGame()
    {
        Debug.Log("[Delivery] Starting game!");
        
        // Hide start, show HUD
        if (startPanel) startPanel.SetActive(false);
        if (gameHUD) gameHUD.SetActive(true);
        
        if (totalDeliveries <= 0)
        {
            Debug.LogWarning("[Delivery] StartGame called with no pending deliveries.");
            return;
        }

        // Reset state
        currentDeliveryIndex = 0;
        successfulDeliveries = 0;
        totalTips = 0;
        currentTime = startingTime;
        isPlaying = true;
        isPaused = false;
        beeped10 = beeped5 = beeped3 = false;
        
        // Reset player
        if (playerScooter)
        {
            playerScooter.EnableControls(true);
            if (playerSpawnPoint)
                playerScooter.ResetToPosition(playerSpawnPoint.position);
        }
        
        // Start first delivery
        SetNextTarget();
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }
    
    public void EndGame(bool allComplete)
    {
        isPlaying = false;
        
        if (playerScooter)
            playerScooter.EnableControls(false);
        
        // Bonus for completing all
        int bonus = allComplete ? successfulDeliveries * 15 : 0;
        totalTips += bonus;
        
        // Save earnings
        if (GameManager.Instance != null)
            GameManager.Instance.AddMoney(totalTips, isTip: true);
        
        // Show results
        ShowResults(allComplete, bonus);
        
        // Sound
        if (allComplete && AudioManager.Instance != null && gameCompleteSound != null)
            AudioManager.Instance.PlaySFX(gameCompleteSound);
        
        Debug.Log($"[Delivery] Game Over! Complete: {allComplete}, Tips: ${totalTips}");
    }
    
    void ShowResults(bool success, int bonus)
    {
        if (resultsPanel) resultsPanel.SetActive(true);
        
        if (resultsTitleText)
            resultsTitleText.text = success ? "All Delivered!" : "Time is Up!";
        
        if (resultsStatsText)
        {
            string stats = $"Deliveries: {successfulDeliveries}/{totalDeliveries}\n\n";
            stats += $"Base Tips: ${totalTips - bonus}\n";
            if (bonus > 0)
                stats += $"Completion Bonus: +${bonus}\n";
            stats += $"\nTOTAL: ${totalTips}";
            resultsStatsText.text = stats;
        }
        
        if (retryButton)
            retryButton.gameObject.SetActive(!success);
    }
    
    public void RestartGame()
    {
        if (resultsPanel) resultsPanel.SetActive(false);
        
        foreach (var point in allDeliveryPoints)
            point.SetActive(false);
        
        StartGame();
    }
    
    public void ReturnToCafe()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("PendingDeliveries", 0);
        if (GameManager.Instance != null)
            GameManager.Instance.ResetDayStats();
        SceneManager.LoadScene(SceneNames.ShopScene);
    }
    
    // ==================== DELIVERY LOGIC ====================
    
    void SetNextTarget()
    {
        currentDeliveryIndex++;
        
        if (currentDeliveryIndex > totalDeliveries)
        {
            EndGame(true);
            return;
        }
        
        // Deactivate current
        if (currentTarget != null)
            currentTarget.SetActive(false);
        
        if (allDeliveryPoints.Count == 0)
        {
            Debug.LogError("[Delivery] No delivery points in scene!");
            EndGame(false);
            return;
        }

        // Pick random new target (avoid repeating the previous one)
        {
            DeliveryPoint previousTarget = currentTarget;
            
            List<DeliveryPoint> candidates = new List<DeliveryPoint>(allDeliveryPoints);
            if (candidates.Count > 1 && previousTarget != null)
                candidates.Remove(previousTarget);
            
            int index = Random.Range(0, candidates.Count);
            currentTarget = candidates[index];
            currentTarget.SetActive(true);
            
            Debug.Log($"[Delivery] Target: {currentTarget.locationName} ({currentDeliveryIndex}/{totalDeliveries})");
        }
    }
    
    public void OnDeliveryComplete(DeliveryPoint point)
    {
        if (!isPlaying || point != currentTarget) return;
        
        successfulDeliveries++;
        
        // Calculate tip (more time left = more tip)
        float timeBonus = currentTime / startingTime;
        int tip = Mathf.RoundToInt(baseDeliveryReward * (0.5f + timeBonus));
        tip += point.bonusTip;
        totalTips += tip;
        
        // Add bonus time
        currentTime += bonusTimePerDelivery;
        
        // Sound
        if (AudioManager.Instance != null && deliveryCompleteSound != null)
            AudioManager.Instance.PlaySFX(deliveryCompleteSound);
        
        Debug.Log($"[Delivery] Complete! Tip: ${tip}, +{bonusTimePerDelivery}s");
        
        // Next delivery
        SetNextTarget();
    }
    
    public void OnPlayerCrash()
    {
        if (!isPlaying) return;
        
        currentTime -= penaltyTimeOnCrash;
        
        if (AudioManager.Instance != null && crashSound != null)
            AudioManager.Instance.PlaySFX(crashSound);
        
        Debug.Log($"[Delivery] Crash! -{penaltyTimeOnCrash}s");
    }
    
    public void OnTipCollected(int amount)
    {
        totalTips += amount;
        
        if (AudioManager.Instance != null && tipSound != null)
            AudioManager.Instance.PlaySFX(tipSound);
    }
    
    private bool beeped10, beeped5, beeped3;

    void UpdateTimer()
    {
        currentTime -= Time.deltaTime;
        
        if (currentTime <= 10f && !beeped10) { beeped10 = true; PlayCountdownBeep(); }
        if (currentTime <= 5f  && !beeped5)  { beeped5  = true; PlayCountdownBeep(); }
        if (currentTime <= 3f  && !beeped3)  { beeped3  = true; PlayCountdownBeep(); }
        
        if (currentTime <= 0)
        {
            currentTime = 0;
            EndGame(false);
        }
    }
    
    void PlayCountdownBeep()
    {
        if (AudioManager.Instance != null && countdownBeep != null)
            AudioManager.Instance.PlaySFX(countdownBeep);
    }
    
    // ==================== UI ====================
    
    void UpdateHUD()
    {
        // Timer
        if (timerText)
        {
            timerText.text = $"{currentTime:F1}s";
            timerText.color = currentTime <= 10f ? Color.red : Color.white;
        }
        
        // Delivery count
        if (deliveryCountText)
            deliveryCountText.text = $"Order {currentDeliveryIndex}/{totalDeliveries}";
        
        // Tips
        if (tipsText)
            tipsText.text = $"Tips: ${totalTips}";
        
        // Boost
        if (boostSlider && playerScooter)
        {
            boostSlider.value = playerScooter.GetBoostPercent();
            if (boostFillImage)
                boostFillImage.color = playerScooter.CanBoost() ? Color.cyan : Color.gray;
        }
        
        // Distance
        if (distanceText && currentTarget && playerScooter)
        {
            float dist = Vector2.Distance(playerScooter.transform.position, currentTarget.transform.position);
            distanceText.text = $"{dist:F0}m";
        }
    }
    
    void UpdateArrowIndicator()
    {
        if (!arrowIndicator || !currentTarget || !playerScooter) return;
        
        Vector3 dir = currentTarget.transform.position - playerScooter.transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        arrowIndicator.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    // ==================== PAUSE ====================
    
    public void TogglePause()
    {
        // If settings is open, close it first
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }
        
        if (isPaused) ResumeGame();
        else PauseGame();
    }
    
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);
        if (playerScooter) playerScooter.EnableControls(false);
    }
    
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (playerScooter) playerScooter.EnableControls(true);
    }
    
    // ==================== SETTINGS ====================
    
    void WireSettingsBackButton()
    {
        if (settingsPanel == null) return;
        Button[] buttons = settingsPanel.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string lower = btn.gameObject.name.ToLowerInvariant();
            if (lower.Contains("back") || lower.Contains("return"))
            {
                btn.onClick.RemoveListener(CloseSettings);
                btn.onClick.AddListener(CloseSettings);
            }
        }
    }
    
    public void OpenSettings()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
    }
    
    public void CloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }
    
    public void QuitGame()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null && SaveManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
