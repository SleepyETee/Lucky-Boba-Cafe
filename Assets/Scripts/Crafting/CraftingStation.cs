// ============================================================
// FILE: CraftingStation.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Interactable station that triggers crafting
// ============================================================
using UnityEngine;
using TMPro;

public class CraftingStation : MonoBehaviour, IInteractable
{
    [Header("Station Info")]
    [SerializeField] private string stationName = "Boba Station";
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject highlightEffect;
    
    [Header("References")]
    [SerializeField] private CraftingMinigame craftingMinigame;
    [SerializeField] private CustomerSpawner customerSpawner;
    
    private bool playerInRange = false;
    private TextMeshPro promptText;
    
    void Start()
    {
        // Auto-find CraftingMinigame if not assigned
        if (craftingMinigame == null)
        {
            craftingMinigame = FindFirstObjectByType<CraftingMinigame>();
            if (craftingMinigame == null)
            {
                CraftingMinigame[] all = FindObjectsByType<CraftingMinigame>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (all.Length > 0)
                    craftingMinigame = all[0];
            }
        }
        
        // Auto-find CustomerSpawner if not assigned
        if (customerSpawner == null)
            customerSpawner = FindFirstObjectByType<CustomerSpawner>();
        
        // Create the "Press E" prompt
        CreateInteractionPrompt();
    }
    
    void CreateInteractionPrompt()
    {
        GameObject promptObj = new GameObject("InteractionPrompt");
        promptObj.transform.parent = transform;
        promptObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        
        promptText = promptObj.AddComponent<TextMeshPro>();
        promptText.text = "Press [E]";
        promptText.fontSize = 4f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.sortingOrder = 10;
        
        // Set the text rect size
        RectTransform rect = promptObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(3f, 1f);
        
        promptObj.SetActive(false);
    }
    
    // ==================== IInteractable Implementation ====================
    
    public void Interact()
    {
        if (!playerInRange) return;
        
        // Check if there's a customer waiting for a drink
        if (customerSpawner != null && !customerSpawner.HasWaitingCustomer())
        {
            // Flash the prompt text to say "No orders!"
            if (promptText != null)
            {
                promptText.text = "No orders!";
                promptText.color = Color.red;
                CancelInvoke(nameof(ResetPromptText));
                Invoke(nameof(ResetPromptText), 1f);
            }
            return;
        }
        
        // Get the current order name to pass to the minigame
        string drinkName = "";
        if (customerSpawner != null)
        {
            Customer customer = customerSpawner.GetWaitingCustomer();
            if (customer != null)
                drinkName = customer.CurrentOrder;
        }
        
        // Start minigame
        if (craftingMinigame != null)
        {
            craftingMinigame.StartMinigame(drinkName);
            
            // Hide prompt while in minigame
            if (promptText != null)
                promptText.gameObject.SetActive(false);
            
            // Freeze player
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.Freeze();
        }
    }
    
    void ResetPromptText()
    {
        if (promptText != null)
        {
            promptText.text = "";
            promptText.color = Color.white;
        }
    }
    
    public void OnPlayerEnterRange()
    {
        playerInRange = true;
        
        if (highlightEffect != null)
            highlightEffect.SetActive(true);
        
        if (promptText != null)
        {
            ResetPromptText();
            promptText.gameObject.SetActive(true);
        }
    }
    
    public void OnPlayerExitRange()
    {
        playerInRange = false;
        
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
        
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }
    
    public string GetInteractionPrompt()
    {
        return $"Press [E] to use {stationName}";
    }
}
