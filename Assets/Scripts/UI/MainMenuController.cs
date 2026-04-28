// ============================================================
// FILE: MainMenuController.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Controls main menu buttons and navigation
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Optional explicit Back buttons")]
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button creditsBackButton;
    
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    Button runtimeNewGameButton;
    
    void Start()
    {
        // Ensure time is running
        Time.timeScale = 1f;

        ConfigurePlayFlow();
        WireBackButtons();
        EnsureCreditsBackButtonVisible();

        // Setup button listeners (non-playflow)
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        
        ShowMainMenu();
    }

    void Update()
    {
        if (GameInput.PausePressed)
        {
            bool showingSubPanel =
                (settingsPanel != null && settingsPanel.activeSelf) ||
                (creditsPanel != null && creditsPanel.activeSelf);
            if (showingSubPanel)
                ShowMainMenu();
        }
    }

    void ConfigurePlayFlow()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();

        Button continueBtn = continueButton != null ? continueButton : playButton;
        Button newGameBtn = newGameButton;

        if (hasSave)
        {
            // Continue button (reuse play button by default)
            if (continueBtn != null)
            {
                continueBtn.onClick.RemoveAllListeners();
                continueBtn.onClick.AddListener(OnContinueClicked);
                SetButtonLabel(continueBtn, "Continue");
            }

            // New Game button (clone play if none assigned)
            if (newGameBtn == null && playButton != null)
            {
                runtimeNewGameButton = Instantiate(playButton, playButton.transform.parent);
                runtimeNewGameButton.gameObject.name = "NewGameButton (Runtime)";
                newGameBtn = runtimeNewGameButton;

                RectTransform playRt = playButton.GetComponent<RectTransform>();
                RectTransform newRt = newGameBtn.GetComponent<RectTransform>();
                float buttonSpacing = 70f;
                
                if (playRt != null && newRt != null)
                {
                    // Place New Game just below Continue
                    newRt.anchoredPosition = playRt.anchoredPosition + new Vector2(0f, -buttonSpacing);
                    
                    // Push all other buttons down to make room
                    Button[] allButtons = { settingsButton, creditsButton, quitButton };
                    foreach (var btn in allButtons)
                    {
                        if (btn == null) continue;
                        RectTransform rt = btn.GetComponent<RectTransform>();
                        if (rt != null)
                            rt.anchoredPosition += new Vector2(0f, -buttonSpacing);
                    }
                }
            }

            if (newGameBtn != null)
            {
                newGameBtn.onClick.RemoveAllListeners();
                newGameBtn.onClick.AddListener(OnNewGameClicked);
                SetButtonLabel(newGameBtn, "New Game");
                newGameBtn.gameObject.SetActive(true);
            }
        }
        else
        {
            // No save: play starts a new game
            if (continueBtn != null)
            {
                continueBtn.onClick.RemoveAllListeners();
                continueBtn.onClick.AddListener(OnNewGameClicked);
            }

            // Hide standalone new game button if one exists
            if (newGameBtn != null)
                newGameBtn.gameObject.SetActive(false);
            if (runtimeNewGameButton != null)
                runtimeNewGameButton.gameObject.SetActive(false);
        }
    }
    
    // ==================== BUTTON HANDLERS ====================
    
    public void OnContinueClicked()
    {
        Debug.Log("[MainMenu] Continuing game...");

        if (SaveManager.Instance == null || !SaveManager.Instance.QueueContinue())
        {
            Debug.LogWarning("[MainMenu] No valid save found; starting new game instead.");
            if (SaveManager.Instance != null)
                SaveManager.Instance.QueueNewGame(deleteExistingSave: true);
        }

        SceneManager.LoadScene(SceneNames.GameScene);
    }

    public void OnNewGameClicked()
    {
        Debug.Log("[MainMenu] Starting new game...");
        if (SaveManager.Instance != null)
            SaveManager.Instance.QueueNewGame(deleteExistingSave: true);
        SceneManager.LoadScene(SceneNames.GameScene);
    }

    // Backwards-compatible hook for older button setups.
    public void OnPlayClicked() => OnNewGameClicked();

    static void SetButtonLabel(Button button, string label)
    {
        if (button == null) return;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = label;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label;
        }
    }
    
    public void OnSettingsClicked()
    {
        Debug.Log("[MainMenu] Opening settings...");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }
    
    public void OnCreditsClicked()
    {
        Debug.Log("[MainMenu] Showing credits...");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }
    
    public void OnQuitClicked()
    {
        Debug.Log("[MainMenu] Quitting application...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    public void OnBackClicked()
    {
        ShowMainMenu();
    }

    void WireBackButtons()
    {
        WireExplicitBackButton(settingsBackButton);
        WireExplicitBackButton(creditsBackButton);
        WireBackButtonsInPanel(settingsPanel);
        WireBackButtonsInPanel(creditsPanel);
    }

    void WireExplicitBackButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveListener(OnBackClicked);
        button.onClick.AddListener(OnBackClicked);
    }

    void WireBackButtonsInPanel(GameObject panel)
    {
        if (panel == null) return;

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            if (btn == null) continue;

            string name = btn.gameObject.name;
            if (string.IsNullOrEmpty(name)) continue;

            string lower = name.ToLowerInvariant();
            if (lower.Contains("back") || lower.Contains("return"))
            {
                btn.onClick.RemoveListener(OnBackClicked);
                btn.onClick.AddListener(OnBackClicked);
            }
        }
    }

    void EnsureCreditsBackButtonVisible()
    {
        if (creditsPanel == null) return;

        Button back = creditsBackButton;
        if (back == null)
        {
            Button[] buttons = creditsPanel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i];
                if (btn == null) continue;
                string name = btn.gameObject.name;
                if (string.IsNullOrEmpty(name)) continue;

                string lower = name.ToLowerInvariant();
                if (lower.Contains("back") || lower.Contains("return"))
                {
                    back = btn;
                    break;
                }
            }
        }

        if (back == null)
        {
            Debug.LogWarning("[MainMenu] No Credits back button found under CreditsPanel.");
            return;
        }

        // Ensure visible and top-most so other graphics don't hide/block it.
        back.gameObject.SetActive(true);
        back.transform.SetAsLastSibling();
        back.onClick.RemoveListener(OnBackClicked);
        back.onClick.AddListener(OnBackClicked);
    }
    
    // ==================== PANEL MANAGEMENT ====================
    
    void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }
}
