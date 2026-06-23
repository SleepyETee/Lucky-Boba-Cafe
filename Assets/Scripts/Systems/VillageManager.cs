using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class VillageManager : MonoBehaviour
{
    public static VillageManager Instance { get; private set; }
    
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Button returnHomeButton;
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    
    private bool isPaused;
    
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    
    void Start()
    {
        PlayerController existingPlayer = FindAnyObjectByType<PlayerController>();
        if (existingPlayer != null && spawnPoint != null)
            existingPlayer.transform.position = spawnPoint.position;
        else if (playerPrefab && spawnPoint && existingPlayer == null)
            Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        
        if (returnHomeButton) returnHomeButton.onClick.AddListener(ReturnHome);
        SetButtonLabel(returnHomeButton, "Prepare Tomorrow");
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (settingsButton) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
        
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        WireSettingsBackButton();
        
        // Subscribe to money changes for event-driven UI
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged += UpdateMoney;
        
        UpdateUI();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
    }
    
    void Update()
    {
        if (GameInput.PausePressed)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
                CloseSettings();
            else
                TogglePause();
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged -= UpdateMoney;
    }
    
    void UpdateUI()
    {
        if (moneyText && GameManager.Instance != null)
            moneyText.text = "PawCoins: $" + GameManager.Instance.PawCoins;
    }
    
    void UpdateMoney(int amount)
    {
        if (moneyText != null)
            moneyText.text = "PawCoins: $" + amount;
    }

    static void SetButtonLabel(Button button, string label)
    {
        if (button == null || string.IsNullOrEmpty(label)) return;
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) text.text = label;
    }
    
    // ==================== PAUSE ====================
    
    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }
    
    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;
        if (GameManager.Instance != null) GameManager.Instance.RequestPause();
        else Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);
    }
    
    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.ReleasePause();
        else Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
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
    
    // ==================== NAVIGATION ====================
    
    public void ReturnHome()
    {
        isPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        else Time.timeScale = 1f;
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
        SceneManager.LoadScene(SceneNames.ShopScene);
    }

    public void ReturnToMainMenu()
    {
        isPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        else Time.timeScale = 1f;
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
    
    public void QuitGame()
    {
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}

