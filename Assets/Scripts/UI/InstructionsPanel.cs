// ============================================================
// FILE: InstructionsPanel.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Manages the display and hiding of the instructions
//              panel at the start of the game, pausing time until
//              the player presses Space.
// ============================================================
using UnityEngine;

public class InstructionsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelContainer;
    
    [Header("Input")]
    [SerializeField] private KeyCode dismissKey = KeyCode.Space;

    private bool isShowing = false;

    void Start()
    {
        // Automatically show instructions if a panel is assigned
        if (panelContainer != null)
        {
            Show();
        }
    }

    void Update()
    {
        // Listen for the dismiss key when showing
        if (isShowing && Input.GetKeyDown(dismissKey))
        {
            Hide();
        }
    }

    public void Show()
    {
        if (panelContainer == null) return;
        
        isShowing = true;
        panelContainer.SetActive(true);
        
        // Pause the game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPaused(true);
        }
    }

    public void Hide()
    {
        isShowing = false;
        
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
        
        // Unpause the game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPaused(false);
        }
    }
}
