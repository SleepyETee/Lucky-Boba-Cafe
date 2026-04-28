// ============================================================
// FILE: PauseMenuController.cs
// AUTHOR: Long + Claude
// DESCRIPTION: In-game pause menu with resume, settings, quit
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    
    private bool isPaused = false;
    
    void Start()
    {
        // Setup buttons
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        
        // Start unpaused
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        WireSettingsBackButton();
    }
    
    void Update()
    {
        // Toggle pause with Escape
        if (GameInput.PausePressed)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
                CloseSettings();
            else
                TogglePause();
        }
    }
    
    // ==================== PAUSE CONTROL ====================
    
    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }
    
    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pausePanel != null) pausePanel.SetActive(true);
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(true);
        
        Debug.Log("[PauseMenu] Game paused");
    }
    
    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        
        Debug.Log("[PauseMenu] Game resumed");
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
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }
    
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }
    
    // ==================== NAVIGATION ====================
    
    public void ReturnToMainMenu()
    {
        // Auto-save before leaving gameplay
        if (SaveManager.Instance != null && GameManager.Instance != null)
            SaveManager.Instance.SaveToDisk(GameManager.Instance.BuildSaveDataSnapshot());

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.MainMenu);
        Debug.Log("[PauseMenu] Returning to main menu");
    }
    
    public void QuitGame()
    {
        Debug.Log("[PauseMenu] Quitting game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
