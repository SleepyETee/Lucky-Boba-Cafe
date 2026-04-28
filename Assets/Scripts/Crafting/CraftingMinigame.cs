// ============================================================
// FILE: CraftingMinigame.cs
// AUTHOR: Long + Claude
// DESCRIPTION: 5-step crafting minigame for drinks:
//              Brew -> Mix -> Shake -> Top -> Serve
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class CraftingMinigame : MonoBehaviour
{
    public enum CraftStep { Brew, Mix, Shake, Top, Serve }

    [Header("UI Elements (assign in Inspector)")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI stepLabel;

    [Header("Topping Buttons (for Top step, assign in Inspector)")]
    [SerializeField] private GameObject toppingButtonsPanel;
    [SerializeField] private Button toppingButton1;
    [SerializeField] private Button toppingButton2;
    [SerializeField] private Button toppingButton3;

    [Header("Timing / Tolerances")]
    [SerializeField] private float tolerancePerfect = 0.2f;
    [SerializeField] private float toleranceGood = 0.5f;

    [Header("Mix Step")]
    [SerializeField] private float mixDuration = 3f;
    [SerializeField] private int mixTapsPerfect = 12;
    [SerializeField] private int mixTapsGood = 8;

    [Header("Shake Step")]
    [SerializeField] private float shakeOscillateSpeed = 3f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color perfectColor = Color.green;
    [SerializeField] private Color goodColor = Color.yellow;
    [SerializeField] private Color badColor = Color.red;

    [Header("References")]
    [SerializeField] private CustomerSpawner customerSpawner;

    private Dictionary<string, (float target, float max)> drinkRecipes;

    // Step state
    private CraftStep currentStep;
    private float[] stepScores = new float[4]; // Brew, Mix, Shake, Top
    private string currentDrinkName;
    private bool isActive;
    private bool stepComplete;

    // Brew state (hold + release)
    private float currentTime;
    private float targetTime;
    private float maxTime;
    private bool isHolding;

    // Mix state (tap rapidly)
    private float mixTimer;
    private int mixTapCount;

    // Shake state (oscillating slider)
    private float shakeTimer;
    private bool shakePressed;

    // Topping state
    private bool toppingChosen;
    private float toppingScore;
    private bool orderNeedsTopping = true;

    // Upgrade bonus
    private float toleranceBonus;

    public event Action<float> OnCraftingComplete;

    void Start()
    {
        drinkRecipes = new Dictionary<string, (float target, float max)>
        {
            { "Classic Milk Tea", (2.5f, 4.0f) },
            { "Taro Boba",       (3.0f, 4.5f) },
            { "Brown Sugar",     (3.5f, 5.0f) },
            { "Matcha Latte",    (2.0f, 3.5f) },
            { "Green Tea",       (1.5f, 3.0f) },
        };

        if (customerSpawner == null)
            customerSpawner = FindAnyObjectByType<CustomerSpawner>();

        if (minigamePanel != null) minigamePanel.SetActive(false);
        if (toppingButtonsPanel != null) toppingButtonsPanel.SetActive(false);

        if (toppingButton1 != null) toppingButton1.onClick.AddListener(() => OnToppingPicked(1.0f));
        if (toppingButton2 != null) toppingButton2.onClick.AddListener(() => OnToppingPicked(0.7f));
        if (toppingButton3 != null) toppingButton3.onClick.AddListener(() => OnToppingPicked(0.4f));
    }

    void Update()
    {
        if (!isActive || stepComplete) return;

        switch (currentStep)
        {
            case CraftStep.Brew:  UpdateBrew();  break;
            case CraftStep.Mix:   UpdateMix();   break;
            case CraftStep.Shake: UpdateShake(); break;
            case CraftStep.Top:   UpdateTop();   break; // buttons + keys 1–3
            case CraftStep.Serve: break; // auto
        }
    }

    // ==================== PUBLIC ====================

    public void StartMinigame(string drinkName = "", bool needsTopping = true)
    {
        if (isActive)
            return;

        StopAllCoroutines();
        gameObject.SetActive(true);
        currentDrinkName = drinkName;
        orderNeedsTopping = needsTopping;
        isActive = true;

        toleranceBonus = 0f;
        if (GameManager.Instance != null)
        {
            int quickPaws = GameManager.Instance.GetUpgradeLevelByName("Quick Paws");
            toleranceBonus = quickPaws * 0.1f;
        }

        if (!string.IsNullOrEmpty(drinkName) && drinkRecipes.ContainsKey(drinkName))
        {
            var recipe = drinkRecipes[drinkName];
            targetTime = recipe.target;
            maxTime = recipe.max;
        }
        else
        {
            targetTime = 3f;
            maxTime = 5f;
        }

        stepScores = new float[4];

        if (minigamePanel != null) minigamePanel.SetActive(true);
        if (toppingButtonsPanel != null) toppingButtonsPanel.SetActive(false);

        BeginStep(CraftStep.Brew);
    }

    // ==================== STEP LIFECYCLE ====================

    void BeginStep(CraftStep step)
    {
        currentStep = step;
        stepComplete = false;

        if (progressBar != null) progressBar.value = 0;
        if (resultText != null) resultText.text = "";
        if (progressFill != null) progressFill.color = normalColor;
        if (toppingButtonsPanel != null) toppingButtonsPanel.SetActive(false);

        int totalSteps = orderNeedsTopping ? 5 : 4;
        int stepNumber = step == CraftStep.Serve && !orderNeedsTopping ? 4 : (int)step + 1;
        string label = $"Step {stepNumber}/{totalSteps}";
        if (stepLabel != null) stepLabel.text = label;

        switch (step)
        {
            case CraftStep.Brew:
                currentTime = 0f;
                isHolding = false;
                if (instructionText != null)
                    instructionText.text = $"BREW: {currentDrinkName}\nHold SPACE and release at the right time!";
                break;

            case CraftStep.Mix:
                mixTimer = 0f;
                mixTapCount = 0;
                if (instructionText != null)
                    instructionText.text = "MIX: Tap SPACE rapidly!";
                break;

            case CraftStep.Shake:
                shakeTimer = 0f;
                shakePressed = false;
                if (instructionText != null)
                    instructionText.text = "SHAKE: Press SPACE when the bar is centered!";
                break;

            case CraftStep.Top:
                toppingChosen = false;
                toppingScore = 0f;
                if (toppingButtonsPanel != null) toppingButtonsPanel.SetActive(true);
                if (instructionText != null)
                    instructionText.text = "TOP: Choose a topping - click a button or press 1, 2, 3";
                break;

            case CraftStep.Serve:
                if (!orderNeedsTopping)
                    stepScores[3] = 1f;
                ShowServeResult();
                break;
        }
    }

    void FinishStep(float score, string message, Color color)
    {
        stepComplete = true;

        int idx = (int)currentStep;
        if (idx < stepScores.Length)
            stepScores[idx] = score;

        if (resultText != null)
        {
            resultText.text = message;
            resultText.color = color;
        }

        PlayStepSFX(score);
        StartCoroutine(AdvanceAfterDelay(0.8f));
    }

    IEnumerator AdvanceAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        CraftStep next;
        if (TryGetNextStep(currentStep, out next))
            BeginStep(next);
    }

    bool TryGetNextStep(CraftStep step, out CraftStep next)
    {
        switch (step)
        {
            case CraftStep.Brew:
                next = CraftStep.Mix;
                return true;
            case CraftStep.Mix:
                next = CraftStep.Shake;
                return true;
            case CraftStep.Shake:
                next = orderNeedsTopping ? CraftStep.Top : CraftStep.Serve;
                return true;
            case CraftStep.Top:
                next = CraftStep.Serve;
                return true;
            default:
                next = CraftStep.Serve;
                return false;
        }
    }

    // ==================== BREW (hold + release) ====================

    void UpdateBrew()
    {
        if (GameInput.ConfirmPressed && !isHolding)
        {
            isHolding = true;
            if (instructionText != null)
                instructionText.text = "Release at the perfect moment!";
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayBrewStart();
        }

        if (isHolding && GameInput.ConfirmHeld)
        {
            currentTime += Time.deltaTime;
            if (progressBar != null) progressBar.value = currentTime / maxTime;
            if (timerText != null) timerText.text = $"{currentTime:F1}s";

            if (progressFill != null)
            {
                float diff = Mathf.Abs(currentTime - targetTime);
                float perfTol = tolerancePerfect + toleranceBonus;
                float goodTol = toleranceGood + toleranceBonus;
                if (diff <= perfTol) progressFill.color = perfectColor;
                else if (diff <= goodTol) progressFill.color = goodColor;
                else progressFill.color = normalColor;
            }

            if (currentTime >= maxTime)
                CompleteBrew();
        }

        if (isHolding && GameInput.ConfirmReleased)
            CompleteBrew();
    }

    void CompleteBrew()
    {
        float diff = Mathf.Abs(currentTime - targetTime);
        float perfTol = tolerancePerfect + toleranceBonus;
        float goodTol = toleranceGood + toleranceBonus;

        if (diff <= perfTol)
            FinishStep(1.0f, "PERFECT BREW!", perfectColor);
        else if (diff <= goodTol)
            FinishStep(0.8f, "Great brew!", goodColor);
        else if (diff <= goodTol * 2)
            FinishStep(0.6f, "Good brew", Color.yellow);
        else if (currentTime < targetTime)
            FinishStep(0.4f, "Underbrewed...", badColor);
        else
            FinishStep(0.3f, "Overbrewed!", badColor);
    }

    // ==================== MIX (tap rapidly) ====================

    void UpdateMix()
    {
        mixTimer += Time.deltaTime;

        if (GameInput.ConfirmPressed)
            mixTapCount++;

        if (progressBar != null) progressBar.value = mixTimer / mixDuration;
        if (timerText != null) timerText.text = $"Taps: {mixTapCount}";

        if (progressFill != null)
        {
            if (mixTapCount >= mixTapsPerfect) progressFill.color = perfectColor;
            else if (mixTapCount >= mixTapsGood) progressFill.color = goodColor;
            else progressFill.color = normalColor;
        }

        if (mixTimer >= mixDuration)
            CompleteMix();
    }

    void CompleteMix()
    {
        if (mixTapCount >= mixTapsPerfect)
            FinishStep(1.0f, "PERFECT MIX!", perfectColor);
        else if (mixTapCount >= mixTapsGood)
            FinishStep(0.75f, "Good mix!", goodColor);
        else if (mixTapCount >= mixTapsGood / 2)
            FinishStep(0.5f, "Okay mix", Color.yellow);
        else
            FinishStep(0.3f, "Barely mixed...", badColor);
    }

    // ==================== SHAKE (oscillating slider) ====================

    void UpdateShake()
    {
        shakeTimer += Time.deltaTime * shakeOscillateSpeed;
        float value = (Mathf.Sin(shakeTimer) + 1f) / 2f; // 0..1 oscillation

        if (progressBar != null) progressBar.value = value;

        if (progressFill != null)
        {
            float distFromCenter = Mathf.Abs(value - 0.5f);
            float perfTol = 0.08f + toleranceBonus * 0.1f;
            float goodTol = 0.15f + toleranceBonus * 0.1f;
            if (distFromCenter <= perfTol) progressFill.color = perfectColor;
            else if (distFromCenter <= goodTol) progressFill.color = goodColor;
            else progressFill.color = normalColor;
        }

        if (timerText != null) timerText.text = "Press SPACE!";

        if (GameInput.ConfirmPressed && !shakePressed)
        {
            shakePressed = true;
            float distFromCenter = Mathf.Abs(value - 0.5f);
            float perfTol = 0.08f + toleranceBonus * 0.1f;
            float goodTol = 0.15f + toleranceBonus * 0.1f;

            if (distFromCenter <= perfTol)
                FinishStep(1.0f, "PERFECT SHAKE!", perfectColor);
            else if (distFromCenter <= goodTol)
                FinishStep(0.75f, "Good shake!", goodColor);
            else if (distFromCenter <= 0.3f)
                FinishStep(0.5f, "Okay shake", Color.yellow);
            else
                FinishStep(0.3f, "Bad shake...", badColor);
        }
    }

    // ==================== TOP (choose topping) ====================

    void UpdateTop()
    {
        if (GameInput.Option1Pressed)
            OnToppingPicked(1.0f);
        else if (GameInput.Option2Pressed)
            OnToppingPicked(0.7f);
        else if (GameInput.Option3Pressed)
            OnToppingPicked(0.4f);
    }

    void OnToppingPicked(float score)
    {
        if (toppingChosen || currentStep != CraftStep.Top) return;
        toppingChosen = true;
        toppingScore = score;

        if (toppingButtonsPanel != null) toppingButtonsPanel.SetActive(false);

        string msg = score >= 0.9f ? "PERFECT TOPPING!" :
                     score >= 0.6f ? "Nice topping!" : "Plain topping...";
        Color col = score >= 0.9f ? perfectColor :
                    score >= 0.6f ? goodColor : Color.yellow;

        FinishStep(score, msg, col);
    }

    // ==================== SERVE (final result) ====================

    void ShowServeResult()
    {
        float total = 0f;
        for (int i = 0; i < 4; i++)
            total += stepScores[i];
        float finalQuality = total / 4f;

        string grade;
        Color gradeColor;

        if (finalQuality >= 0.9f) { grade = "PERFECT DRINK!"; gradeColor = perfectColor; }
        else if (finalQuality >= 0.7f) { grade = "Great drink!"; gradeColor = goodColor; }
        else if (finalQuality >= 0.5f) { grade = "Decent drink"; gradeColor = Color.yellow; }
        else { grade = "Poor drink..."; gradeColor = badColor; }

        if (instructionText != null)
            instructionText.text = $"SERVE: {currentDrinkName}";
        if (resultText != null)
        {
            resultText.text = $"{grade}\nQuality: {finalQuality:P0}";
            resultText.color = gradeColor;
        }
        if (progressBar != null) progressBar.value = finalQuality;
        if (progressFill != null) progressFill.color = gradeColor;
        if (timerText != null) timerText.text = "";

        PlayStepSFX(finalQuality);
        StartCoroutine(FinishMinigame(finalQuality));
    }

    IEnumerator FinishMinigame(float quality)
    {
        yield return new WaitForSecondsRealtime(1.5f);

        isActive = false;
        if (minigamePanel != null) minigamePanel.SetActive(false);

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.Unfreeze();

        if (customerSpawner == null)
        {
            customerSpawner = FindAnyObjectByType<CustomerSpawner>();
            Debug.LogWarning($"[CraftingMinigame] customerSpawner was null, re-searched: {(customerSpawner != null ? "found" : "STILL NULL")}");
        }

        if (customerSpawner != null)
        {
            Customer customer = customerSpawner.GetWaitingCustomer();
            if (customer != null)
            {
                Debug.Log($"[CraftingMinigame] Serving drink to customer (quality={quality:F2}). Order: {customer.CurrentOrder}");
                customer.ServeDrink(quality);
            }
            else
            {
                Debug.LogWarning("[CraftingMinigame] No customer in WaitingForDrink state! Drink was wasted.");
            }
        }
        else
        {
            Debug.LogError("[CraftingMinigame] CustomerSpawner is null — cannot serve drink!");
        }

        OnCraftingComplete?.Invoke(quality);
    }

    // ==================== HELPERS ====================

    void OnDestroy()
    {
        if (toppingButton1 != null) toppingButton1.onClick.RemoveAllListeners();
        if (toppingButton2 != null) toppingButton2.onClick.RemoveAllListeners();
        if (toppingButton3 != null) toppingButton3.onClick.RemoveAllListeners();
    }

    void PlayStepSFX(float score)
    {
        if (AudioManager.Instance == null) return;
        if (score >= 0.9f) AudioManager.Instance.PlayBrewPerfect();
        else if (score >= 0.6f) AudioManager.Instance.PlayBrewGood();
        else AudioManager.Instance.PlayBrewBad();
    }
}
