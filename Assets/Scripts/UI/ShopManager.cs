// ============================================================
// FILE: ShopManager.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Between-day shop where player buys upgrades.
//              Now includes Visit Village navigation.
//              All UI references assigned in the Inspector.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
 
public class ShopManager : MonoBehaviour
{
    [Header("UI References (assign in Inspector)")]
    [SerializeField] private TextMeshProUGUI pawCoinsText;
    [SerializeField] private TextMeshProUGUI dayLabel;
    [SerializeField] private Button startNextDayButton;
    [SerializeField] private Button visitVillageButton;
 
    [Header("Upgrade Card Template (assign in Inspector)")]
    [SerializeField] private Transform upgradeCardParent;
    [SerializeField] private GameObject upgradeCardPrefab;
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    
    private bool isPaused;
 
    void Start()
    {
        Time.timeScale = 1f;
 
        if (startNextDayButton != null)
            startNextDayButton.onClick.AddListener(OnStartNextDay);
 
        if (visitVillageButton != null)
            visitVillageButton.onClick.AddListener(OnVisitVillage);
        
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (settingsButton) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
        
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        WireSettingsBackButton();
        RefreshCoinsDisplay();
        BuildUpgradeCards();
        UpdateDayLabel();
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
 
    void RefreshCoinsDisplay()
    {
        if (pawCoinsText == null || GameManager.Instance == null) return;
        pawCoinsText.text = $"PawCoins: ${GameManager.Instance.PawCoins}";
    }
 
    void UpdateDayLabel()
    {
        if (dayLabel == null || GameManager.Instance == null) return;
        dayLabel.text = $"Preparing for Day {GameManager.Instance.CurrentDay}";
    }
 
    void BuildUpgradeCards()
    {
        if (GameManager.Instance == null || upgradeCardParent == null || upgradeCardPrefab == null)
            return;
 
        for (int i = 0; i < GameManager.Instance.UpgradeCount; i++)
        {
            UpgradeData data = GameManager.Instance.GetUpgradeData(i);
            if (data == null) continue;
 
            GameObject card = Instantiate(upgradeCardPrefab, upgradeCardParent);
            card.name = $"UpgradeCard_{data.upgradeName}";
 
            SetupCard(card, i, data);
        }
    }
 
    void SetupCard(GameObject card, int index, UpgradeData data)
    {
        int level = GameManager.Instance.GetUpgradeLevel(index);
        bool maxed = level >= data.maxLevel;
        int cost = data.CostForLevel(level);
 
        TextMeshProUGUI nameText = FindChildTMP(card, "NameText");
        TextMeshProUGUI descText = FindChildTMP(card, "DescText");
        TextMeshProUGUI costText = FindChildTMP(card, "CostText");
        TextMeshProUGUI levelText = FindChildTMP(card, "LevelText");
        Button buyButton = card.GetComponentInChildren<Button>(true);
 
        if (nameText != null) nameText.text = data.upgradeName;
        if (descText != null) descText.text = data.description;
        if (levelText != null) levelText.text = $"Lv {level}/{data.maxLevel}";
 
        if (buyButton != null)
            buyButton.onClick.RemoveAllListeners();

        if (maxed)
        {
            if (costText != null) costText.text = "MAX";
            if (buyButton != null) buyButton.interactable = false;
        }
        else
        {
            if (costText != null) costText.text = $"${cost}";
            if (buyButton != null)
            {
                int capturedIndex = index;
                buyButton.onClick.AddListener(() => OnBuyUpgrade(capturedIndex, card));
            }
        }
    }
 
    void OnBuyUpgrade(int index, GameObject card)
    {
        if (GameManager.Instance == null) return;
 
        UpgradeData data = GameManager.Instance.GetUpgradeData(index);
        if (data == null) return;
 
        int level = GameManager.Instance.GetUpgradeLevel(index);
        if (level >= data.maxLevel) return;
 
        int cost = data.CostForLevel(level);
        if (!GameManager.Instance.SpendMoney(cost)) return;
 
        GameManager.Instance.SetUpgradeLevel(index, level + 1);
 
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCoin();
 
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
 
        RefreshCoinsDisplay();
        SetupCard(card, index, data);
    }
    
    // ==================== PAUSE ====================
    
    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }
    
    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);
    }
    
    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
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
    
    void OnStartNextDay()
    {
        Time.timeScale = 1f;
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
 
        if (GameManager.Instance != null)
            GameManager.Instance.ResetDayStats();
 
        SceneManager.LoadScene(SceneNames.GameScene);
    }
 
    void OnVisitVillage()
    {
        Time.timeScale = 1f;
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
 
        SceneManager.LoadScene(SceneNames.VillageScene);
    }
    
    void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
    
    void QuitGame()
    {
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
 
    static TextMeshProUGUI FindChildTMP(GameObject parent, string childName)
    {
        Transform t = parent.transform.Find(childName);
        if (t == null) return null;
        return t.GetComponent<TextMeshProUGUI>();
    }
}

 