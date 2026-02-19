// ============================================================
// FILE: MainMenuController.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Controls main menu buttons and navigation
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    
    void Start()
    {
        // Ensure time is running
        Time.timeScale = 1f;
        
        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        
        ShowMainMenu();
    }
    
    // ==================== BUTTON HANDLERS ====================
    
    public void OnPlayClicked()
    {
        Debug.Log("[MainMenu] Starting game...");
        SceneManager.LoadScene(1); // Game scene
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
    
    // ==================== PANEL MANAGEMENT ====================
    
    void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }
}
