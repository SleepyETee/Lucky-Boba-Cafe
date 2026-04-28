// ============================================================
// FILE: GameManager.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Core singleton managing game state, money, pause
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Starting Values")]
    [SerializeField] private int startingMoney = 100;

    [Header("Upgrades (assign ScriptableObjects here)")]
    [SerializeField] private UpgradeData[] availableUpgrades;

    private int[] upgradeLevels = new int[0];

    // Game State Properties
    public int PawCoins { get; private set; }
    public int CurrentDay { get; private set; } = 1;
    public bool IsPaused { get; private set; }
    public bool CafeIsOpen { get; set; } = true;
    public int CustomersServed { get; private set; }
    public int TotalTips { get; private set; }
    public int LifetimeCustomersServed { get; private set; }
    public int LifetimeTips { get; private set; }
    public int HighScore { get; private set; }
    
    // Satisfaction tracking
    public float TotalSatisfaction { get; private set; }
    public int SatisfactionCount { get; private set; }
    public float AverageSatisfaction => SatisfactionCount > 0 ? TotalSatisfaction / SatisfactionCount : 0f;
    public int DissatisfiedCount { get; private set; }
    
    // Events for UI updates
    public event Action<int> OnMoneyChanged;
    public event Action<bool> OnPauseChanged;
    public event Action<int> OnCustomerServed;
    public event Action OnDayEnd;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null) return;
        GameManager existing = FindAnyObjectByType<GameManager>();
        if (existing != null) return;
        GameObject go = new GameObject("GameManager (Auto)");
        go.AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        TryLoadUpgradesFromResources();
        ApplyNewGameDefaults();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pauseRequestCount = 0;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    void TryLoadUpgradesFromResources()
    {
        if (availableUpgrades != null && availableUpgrades.Length > 0) return;

        UpgradeData[] res = Resources.LoadAll<UpgradeData>("Upgrades");
        if (res == null || res.Length == 0) return;

        System.Array.Sort(res, (a, b) => string.CompareOrdinal(a != null ? a.upgradeName : "", b != null ? b.upgradeName : ""));
        availableUpgrades = res;
    }
    
    // ==================== MONEY MANAGEMENT ====================
    
    public void AddMoney(int amount, bool isTip = false)
    {
        if (amount <= 0) return;
        PawCoins += amount;
        if (isTip)
        {
            TotalTips += amount;
            LifetimeTips += amount;
        }
        if (PawCoins > HighScore) HighScore = PawCoins;
        OnMoneyChanged?.Invoke(PawCoins);
        Debug.Log($"[GameManager] Added ${amount}{(isTip ? " (tip)" : "")}. Total: ${PawCoins}");
    }
    
    public bool SpendMoney(int amount)
    {
        if (amount <= 0 || PawCoins < amount) return false;
        PawCoins -= amount;
        OnMoneyChanged?.Invoke(PawCoins);
        return true;
    }
    
    // ==================== CUSTOMER TRACKING ====================
    
    public void RecordCustomerServed()
    {
        CustomersServed++;
        LifetimeCustomersServed++;
        Debug.Log($"[GameManager] Customer served! Total today: {CustomersServed}, Lifetime: {LifetimeCustomersServed}");
        OnCustomerServed?.Invoke(CustomersServed);
    }
    
    public void RecordSatisfaction(float satisfaction)
    {
        TotalSatisfaction += satisfaction;
        SatisfactionCount++;
        if (satisfaction < 0.01f) DissatisfiedCount++;
    }
    
    // ==================== DAY MANAGEMENT ====================
    
    public void EndDay()
    {
        CafeIsOpen = false;
        OnDayEnd?.Invoke();
    }
    
    public void ResetDayStats()
    {
        CustomersServed = 0;
        TotalTips = 0;
        TotalSatisfaction = 0;
        SatisfactionCount = 0;
        DissatisfiedCount = 0;
        CafeIsOpen = true;
    }

    // ==================== UPGRADE SYSTEM ====================

    public int UpgradeCount => availableUpgrades != null ? availableUpgrades.Length : 0;

    public UpgradeData GetUpgradeData(int index)
    {
        if (availableUpgrades == null || index < 0 || index >= availableUpgrades.Length)
            return null;
        return availableUpgrades[index];
    }

    public int GetUpgradeLevel(int index)
    {
        if (index < 0 || index >= upgradeLevels.Length) return 0;
        return upgradeLevels[index];
    }

    public void SetUpgradeLevel(int index, int level)
    {
        EnsureUpgradeArray();
        if (index < 0 || index >= upgradeLevels.Length) return;
        upgradeLevels[index] = level;
    }

    public int GetUpgradeLevelByName(string name)
    {
        if (availableUpgrades == null) return 0;
        for (int i = 0; i < availableUpgrades.Length; i++)
        {
            if (availableUpgrades[i] != null && availableUpgrades[i].upgradeName == name)
                return GetUpgradeLevel(i);
        }
        return 0;
    }

    void EnsureUpgradeArray()
    {
        int count = availableUpgrades != null ? availableUpgrades.Length : 0;
        if (upgradeLevels.Length != count)
        {
            int[] resized = new int[count];
            for (int i = 0; i < Mathf.Min(upgradeLevels.Length, count); i++)
                resized[i] = upgradeLevels[i];
            upgradeLevels = resized;
        }
    }

    // ==================== SAVE/LOAD SUPPORT ====================

    public void ApplyNewGameDefaults()
    {
        PawCoins = startingMoney;
        CurrentDay = 1;
        LifetimeCustomersServed = 0;
        LifetimeTips = 0;
        HighScore = PawCoins;
        upgradeLevels = new int[availableUpgrades != null ? availableUpgrades.Length : 0];
        ResetDayStats();
        OnMoneyChanged?.Invoke(PawCoins);
    }

    public void ApplySaveData(GameData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[GameManager] ApplySaveData called with null data");
            return;
        }

        PawCoins = Mathf.Max(0, data.pawCoins);
        CurrentDay = Mathf.Max(1, data.currentDay);
        LifetimeCustomersServed = Mathf.Max(0, data.lifetimeCustomersServed);
        LifetimeTips = Mathf.Max(0, data.lifetimeTips);
        HighScore = Mathf.Max(PawCoins, data.highScore);

        int count = availableUpgrades != null ? availableUpgrades.Length : 0;
        upgradeLevels = new int[count];
        if (data.upgradeLevels != null)
        {
            for (int i = 0; i < Mathf.Min(data.upgradeLevels.Length, count); i++)
                upgradeLevels[i] = data.upgradeLevels[i];
        }

        // Restore reputation (defer if not ready yet)
        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.SetReputation(data.reputationPoints);
        else if (data.reputationPoints > 0f)
            StartCoroutine(DeferredReputationRestore(data.reputationPoints));

        // Restore volume settings (in case PlayerPrefs are missing, e.g. new device)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(data.masterVolume);
            AudioManager.Instance.SetMusicVolume(data.musicVolume);
            AudioManager.Instance.SetSFXVolume(data.sfxVolume);
        }

        ResetDayStats();
        OnMoneyChanged?.Invoke(PawCoins);
    }

    public GameData BuildSaveDataSnapshot()
    {
        EnsureUpgradeArray();
        GameData data = GameData.NewDefault();
        data.pawCoins = PawCoins;
        data.currentDay = CurrentDay;
        data.lifetimeCustomersServed = LifetimeCustomersServed;
        data.lifetimeTips = LifetimeTips;
        data.highScore = HighScore;
        data.upgradeLevels = (int[])upgradeLevels.Clone();
        data.masterVolume = PlayerPrefs.GetFloat("MasterVolume", AudioListener.volume);
        data.musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        data.sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        // Save reputation
        data.reputationPoints = ReputationSystem.Instance != null
            ? ReputationSystem.Instance.GetReputation()
            : 0f;

        return data;
    }

    public void AdvanceToNextDay()
    {
        CurrentDay++;
    }
    
    // ==================== PAUSE SYSTEM ====================

    private int pauseRequestCount = 0;
    
    /// <summary>
    /// Request a pause (reference-counted). Call ReleasePause to undo.
    /// Multiple systems can request pause simultaneously; the game only
    /// unpauses when ALL requests have been released.
    /// </summary>
    public void RequestPause()
    {
        pauseRequestCount++;
        if (!IsPaused)
        {
            IsPaused = true;
            Time.timeScale = 0f;
            OnPauseChanged?.Invoke(true);
        }
    }

    /// <summary>
    /// Release one pause request. Game unpauses when count reaches zero.
    /// </summary>
    public void ReleasePause()
    {
        pauseRequestCount = Mathf.Max(0, pauseRequestCount - 1);
        if (pauseRequestCount == 0 && IsPaused)
        {
            IsPaused = false;
            Time.timeScale = 1f;
            OnPauseChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Force-set pause state (resets counter). Use for hard transitions
    /// like scene loads or game over. Prefer RequestPause/ReleasePause
    /// for overlay panels.
    /// </summary>
    public void SetPaused(bool paused)
    {
        pauseRequestCount = paused ? 1 : 0;
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        OnPauseChanged?.Invoke(paused);
    }
    
    public void TogglePause() => SetPaused(!IsPaused);
    
    // ==================== SCENE MANAGEMENT ====================
    
    public void LoadScene(string sceneName)
    {
        pauseRequestCount = 0;
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
    
    public void LoadScene(int sceneIndex)
    {
        pauseRequestCount = 0;
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }
    
    public void RestartGame()
    {
        ApplyNewGameDefaults();
        LoadScene(SceneNames.GameScene);
    }
    
    IEnumerator DeferredReputationRestore(float points)
    {
        float timeout = 5f;
        while (ReputationSystem.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.SetReputation(points);
    }

    // ==================== APPLICATION EXIT ====================
    
    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting application...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
