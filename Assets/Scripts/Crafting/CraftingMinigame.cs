// ============================================================
// FILE: CraftingMinigame.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Timing-based minigame for crafting drinks
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class CraftingMinigame : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI resultText;
    
    [Header("Default Timing")]
    [SerializeField] private float tolerancePerfect = 0.2f;
    [SerializeField] private float toleranceGood = 0.5f;
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color perfectColor = Color.green;
    [SerializeField] private Color goodColor = Color.yellow;
    [SerializeField] private Color badColor = Color.red;
    
    [Header("References")]
    [SerializeField] private CustomerSpawner customerSpawner;
    
    // Drink recipes: name -> (targetTime, maxTime)
    private Dictionary<string, (float target, float max)> drinkRecipes;
    
    // Current brew state
    private float currentTime;
    private float targetTime;
    private float maxTime;
    private string currentDrinkName;
    private bool isHolding;
    private bool isComplete;
    private bool isActive;
    
    // Events
    public event Action<float> OnCraftingComplete;
    
    void Start()
    {
        // Initialize drink recipes with different brew times
        drinkRecipes = new Dictionary<string, (float target, float max)>
        {
            { "Classic Milk Tea", (2.5f, 4.0f) },
            { "Taro Boba",       (3.0f, 4.5f) },
            { "Brown Sugar",     (3.5f, 5.0f) },
            { "Matcha Latte",    (2.0f, 3.5f) },
            { "Green Tea",       (1.5f, 3.0f) },
        };
        
        // Auto-find CustomerSpawner if not assigned
        if (customerSpawner == null)
            customerSpawner = FindFirstObjectByType<CustomerSpawner>();
        
        // If minigamePanel is not assigned, try to find it as a child
        if (minigamePanel == null)
        {
            Transform panel = transform.Find("MinigamePanel");
            if (panel != null)
                minigamePanel = panel.gameObject;
        }
        
        // Auto-find UI elements if not assigned
        if (minigamePanel != null)
        {
            if (progressBar == null) 
                progressBar = minigamePanel.GetComponentInChildren<Slider>(true);
            if (instructionText == null || timerText == null || resultText == null)
            {
                var texts = minigamePanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    string lower = t.gameObject.name.ToLower();
                    if (lower.Contains("instruction") && instructionText == null) 
                        instructionText = t;
                    else if (lower.Contains("timer") && timerText == null) 
                        timerText = t;
                    else if (lower.Contains("result") && resultText == null) 
                        resultText = t;
                }
            }
            if (progressFill == null && progressBar != null)
            {
                Transform fill = progressBar.transform.Find("Fill Area/Fill");
                if (fill != null) progressFill = fill.GetComponent<Image>();
            }
        }
        
        // Hide panel at start
        if (minigamePanel != null) minigamePanel.SetActive(false);
    }
    
    // ==================== UPDATE ====================
    
    void Update()
    {
        if (!isActive || isComplete) return;
        
        // Start holding
        if (Input.GetKeyDown(KeyCode.Space) && !isHolding)
        {
            isHolding = true;
            if (instructionText != null)
                instructionText.text = "Release at the perfect moment!";
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayBrewStart();
        }
        
        // While holding spacebar
        if (isHolding && Input.GetKey(KeyCode.Space))
        {
            currentTime += Time.deltaTime;
            UpdateUI();
            
            // Auto-complete at max
            if (currentTime >= maxTime)
                CompleteMinigame();
        }
        
        // Release
        if (isHolding && Input.GetKeyUp(KeyCode.Space))
        {
            CompleteMinigame();
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    public void StartMinigame(string drinkName = "")
    {
        // Make sure this GameObject is active (so Update runs!)
        gameObject.SetActive(true);
        
        currentDrinkName = drinkName;
        
        // Look up brew times for this drink
        if (!string.IsNullOrEmpty(drinkName) && drinkRecipes.ContainsKey(drinkName))
        {
            var recipe = drinkRecipes[drinkName];
            targetTime = recipe.target;
            maxTime = recipe.max;
        }
        else
        {
            // Default brew times
            targetTime = 3f;
            maxTime = 5f;
        }
        
        currentTime = 0f;
        isHolding = false;
        isComplete = false;
        isActive = true;
        
        // Setup UI
        if (minigamePanel != null) minigamePanel.SetActive(true);
        if (progressBar != null) progressBar.value = 0;
        if (resultText != null) resultText.text = "";
        if (progressFill != null) progressFill.color = normalColor;
        
        // Show drink name and instruction
        if (instructionText != null)
        {
            if (!string.IsNullOrEmpty(drinkName))
                instructionText.text = $"Brewing: {drinkName}\nHold [SPACE] to brew...";
            else
                instructionText.text = "Hold [SPACE] to brew...";
        }
    }
    
    // ==================== UI UPDATES ====================
    
    void UpdateUI()
    {
        // Update progress bar
        if (progressBar != null)
            progressBar.value = currentTime / maxTime;
        
        // Update timer
        if (timerText != null)
            timerText.text = $"{currentTime:F1}s";
        
        // Update color based on position relative to target
        if (progressFill != null)
        {
            float diff = Mathf.Abs(currentTime - targetTime);
            
            if (diff <= tolerancePerfect)
                progressFill.color = perfectColor;
            else if (diff <= toleranceGood)
                progressFill.color = goodColor;
            else
                progressFill.color = normalColor;
        }
    }
    
    // ==================== COMPLETION ====================
    
    void CompleteMinigame()
    {
        isComplete = true;
        isHolding = false;
        
        // Calculate quality based on timing
        float timeDiff = Mathf.Abs(currentTime - targetTime);
        float quality;
        string resultMessage;
        Color resultColor;
        
        if (timeDiff <= tolerancePerfect)
        {
            quality = 1.0f;
            resultMessage = "★ PERFECT! ★";
            resultColor = perfectColor;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBrewPerfect();
        }
        else if (timeDiff <= toleranceGood)
        {
            quality = 0.8f;
            resultMessage = "Great!";
            resultColor = goodColor;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBrewGood();
        }
        else if (timeDiff <= toleranceGood * 2)
        {
            quality = 0.6f;
            resultMessage = "Good";
            resultColor = Color.yellow;
        }
        else if (currentTime < targetTime)
        {
            quality = 0.4f;
            resultMessage = "Underbrewed...";
            resultColor = badColor;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBrewBad();
        }
        else
        {
            quality = 0.3f;
            resultMessage = "Overbrewed!";
            resultColor = badColor;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayBrewBad();
        }
        
        // Show result with drink name
        if (resultText != null)
        {
            resultText.text = resultMessage;
            resultText.color = resultColor;
        }
        
        StartCoroutine(FinishAfterDelay(quality));
    }
    
    IEnumerator FinishAfterDelay(float quality)
    {
        yield return new WaitForSeconds(1.5f);
        
        isActive = false;
        if (minigamePanel != null) minigamePanel.SetActive(false);
        
        // Unfreeze player
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.Unfreeze();
        
        // Serve to waiting customer
        if (customerSpawner != null)
        {
            Customer customer = customerSpawner.GetWaitingCustomer();
            if (customer != null)
            {
                customer.ServeDrink(quality);
            }
        }
        
        OnCraftingComplete?.Invoke(quality);
    }
}
