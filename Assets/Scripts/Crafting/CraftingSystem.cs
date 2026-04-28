using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance { get; private set; }
    
    public enum CraftingStep { None, Brew, Mix, Shake, Top, Serve }
    
    [Header("State")]
    [SerializeField] private CraftingStep currentStep = CraftingStep.None;
    private bool isCrafting = false;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private GameObject brewPanel;
    [SerializeField] private GameObject mixPanel;
    [SerializeField] private GameObject shakePanel;
    [SerializeField] private GameObject topPanel;
    [SerializeField] private GameObject servePanel;
    [SerializeField] private Image[] stepIndicators;
    
    [Header("Brew")]
    [SerializeField] private Slider brewSlider;
    [SerializeField] private float brewTargetTime = 3f;
    private float brewTime = 0f;
    private bool brewing = false;
    
    [Header("Shake")]
    [SerializeField] private Slider shakeSlider;
    [SerializeField] private int requiredShakes = 10;
    private int shakeCount = 0;
    
    [Header("Top")]
    [SerializeField] private Slider topSlider;
    private bool topRunning = false;
    
    // Scores
    private float brewScore, mixScore, shakeScore, topScore;
    
    public System.Action<float> OnDrinkCompleted;
    
    private Keyboard keyboard;
    private bool spaceWasPressed; // flag for coroutine input sync
    
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    void OnDestroy() { if (Instance == this) Instance = null; }
    void Start()
    {
        // If CraftingMinigame exists, it is the primary crafting system.
        // Disable CraftingSystem to avoid duplicate crafting UIs.
        if (FindAnyObjectByType<CraftingMinigame>() != null)
        {
            Debug.Log("[CraftingSystem] CraftingMinigame found — disabling CraftingSystem to avoid conflicts.");
            if (craftingPanel) craftingPanel.SetActive(false);
            enabled = false;
            return;
        }
        
        keyboard = Keyboard.current;
        HideAll();
        if (craftingPanel) craftingPanel.SetActive(false);
    }
    
    void Update()
    {
        keyboard = Keyboard.current;
        if (!isCrafting || keyboard == null) return;
        
        // Track space press for coroutine consumption
        if (keyboard.spaceKey.wasPressedThisFrame)
            spaceWasPressed = true;
        
        if (currentStep == CraftingStep.Brew)
        {
            if (keyboard.spaceKey.wasPressedThisFrame) brewing = true;
            if (brewing && keyboard.spaceKey.isPressed) { brewTime += Time.deltaTime; if (brewSlider) brewSlider.value = brewTime / (brewTargetTime * 1.5f); }
            if (brewing && keyboard.spaceKey.wasReleasedThisFrame) { float diff = Mathf.Abs(brewTime - brewTargetTime); brewScore = diff < 0.3f ? 1f : diff < 0.6f ? 0.8f : diff < 1f ? 0.6f : 0.3f; CompleteStep(); }
        }
        else if (currentStep == CraftingStep.Shake)
        {
            if (keyboard.spaceKey.wasPressedThisFrame) { shakeCount++; if (shakeSlider) shakeSlider.value = (float)shakeCount / requiredShakes; if (shakeCount >= requiredShakes) { shakeScore = 1f; CompleteStep(); } }
        }
        else if (currentStep == CraftingStep.Top && topRunning)
        {
            float t = Mathf.PingPong(Time.time * 2f, 1f);
            if (topSlider) topSlider.value = t;
            if (keyboard.spaceKey.wasPressedThisFrame) { topRunning = false; float dist = Mathf.Abs(t - 0.5f); topScore = dist < 0.1f ? 1f : dist < 0.2f ? 0.8f : dist < 0.35f ? 0.6f : 0.3f; CompleteStep(); }
        }
        
        if (keyboard.escapeKey.wasPressedThisFrame) CancelCrafting();
    }
    
    public void StartCrafting(DrinkOrder order)
    {
        isCrafting = true; brewScore = mixScore = shakeScore = topScore = 0f;
        if (craftingPanel) craftingPanel.SetActive(true);
        GoToStep(CraftingStep.Brew);
    }
    
    public void CancelCrafting() { isCrafting = false; currentStep = CraftingStep.None; HideAll(); if (craftingPanel) craftingPanel.SetActive(false); }
    
    void GoToStep(CraftingStep step)
    {
        currentStep = step; HideAll();
        if (step == CraftingStep.Brew) { if (brewPanel) brewPanel.SetActive(true); brewTime = 0; brewing = false; if (brewSlider) brewSlider.value = 0; }
        else if (step == CraftingStep.Mix) { if (mixPanel) mixPanel.SetActive(true); StartCoroutine(AutoMix()); }
        else if (step == CraftingStep.Shake) { if (shakePanel) shakePanel.SetActive(true); shakeCount = 0; if (shakeSlider) shakeSlider.value = 0; }
        else if (step == CraftingStep.Top) { if (topPanel) topPanel.SetActive(true); topRunning = true; }
        else if (step == CraftingStep.Serve) { if (servePanel) servePanel.SetActive(true); StartCoroutine(AutoServe()); }
    }
    
    IEnumerator AutoMix() { yield return new WaitForSeconds(0.5f); spaceWasPressed = false; while (!spaceWasPressed) yield return null; spaceWasPressed = false; mixScore = 0.8f; CompleteStep(); }
    IEnumerator AutoServe() { yield return new WaitForSeconds(0.5f); spaceWasPressed = false; while (!spaceWasPressed) yield return null; spaceWasPressed = false; CompleteDrink(); }
    
    void CompleteStep()
    {
        if (currentStep == CraftingStep.Brew) GoToStep(CraftingStep.Mix);
        else if (currentStep == CraftingStep.Mix) GoToStep(CraftingStep.Shake);
        else if (currentStep == CraftingStep.Shake) GoToStep(CraftingStep.Top);
        else if (currentStep == CraftingStep.Top) GoToStep(CraftingStep.Serve);
    }
    
    void CompleteDrink()
    {
        float total = (brewScore + mixScore + shakeScore + topScore) / 4f;
        isCrafting = false; currentStep = CraftingStep.None; HideAll();
        if (craftingPanel) craftingPanel.SetActive(false);
        if (AudioManager.Instance != null) { if (total >= 0.7f) AudioManager.Instance.PlaySuccess(); else AudioManager.Instance.PlayFail(); }
        OnDrinkCompleted?.Invoke(total);
    }
    
    void HideAll() { if (brewPanel) brewPanel.SetActive(false); if (mixPanel) mixPanel.SetActive(false); if (shakePanel) shakePanel.SetActive(false); if (topPanel) topPanel.SetActive(false); if (servePanel) servePanel.SetActive(false); }
    public bool IsCrafting() => isCrafting;
}

[System.Serializable]
public class DrinkOrder
{
    public string drinkName;
    public string teaType;
    public string milkType;
    public string sweetness;
    public string[] toppings;
    public float difficultyMultiplier = 1f;
    public DrinkOrder(string name) { drinkName = name; teaType = "Green"; milkType = "Regular"; sweetness = "Regular"; toppings = new string[] { "Boba" }; }
}
