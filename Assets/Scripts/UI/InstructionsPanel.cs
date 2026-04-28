// ============================================================
// FILE: InstructionsPanel.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Manages the display and hiding of the instructions
//              panel at the start of the game, pausing time until
//              the player presses Space.
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;

public class InstructionsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelContainer;

    private bool isShowing = false;

    void Start()
    {
        if (panelContainer != null)
        {
            Show();
        }
    }

    void Update()
    {
        if (!isShowing) return;

        if (GameInput.ConfirmPressed || GameInput.EnterPressed || GameInput.ClickPressed)
        {
            Debug.Log("[InstructionsPanel] Input detected — hiding panel.");
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
            GameManager.Instance.RequestPause();
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
            GameManager.Instance.ReleasePause();
        }
    }
}
