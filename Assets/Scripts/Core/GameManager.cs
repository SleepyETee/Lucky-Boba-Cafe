// ============================================================
// FILE: GameManager.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Core singleton managing game state, money, pause
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Starting Values")]
    [SerializeField] private int startingMoney = 100;
    
    // Game State Properties
    public int PawCoins { get; private set; }
    public bool IsPaused { get; private set; }
    public bool CafeIsOpen { get; set; } = true;
    public int CustomersServed { get; private set; }
    public int TotalTips { get; private set; }
    
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
    
    void Awake()
    {
        // Singleton pattern - persist between scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize
        PawCoins = startingMoney;
    }
    
    // ==================== MONEY MANAGEMENT ====================
    
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        PawCoins += amount;
        TotalTips += amount;
        OnMoneyChanged?.Invoke(PawCoins);
        Debug.Log($"[GameManager] Added ${amount}. Total: ${PawCoins}");
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
    
    // ==================== PAUSE SYSTEM ====================
    
    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        OnPauseChanged?.Invoke(paused);
    }
    
    public void TogglePause() => SetPaused(!IsPaused);
    
    // ==================== SCENE MANAGEMENT ====================
    
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
    
    public void LoadScene(int sceneIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }
    
    public void RestartGame()
    {
        PawCoins = startingMoney;
        CustomersServed = 0;
        TotalTips = 0;
        LoadScene(1);
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
