// ============================================================
// FILE: DaySummaryUI.cs
// AUTHOR: Long + Claude
// DESCRIPTION: End-of-day summary showing stats and ratings.
//              Auto-generates its own UI at runtime.
// ============================================================
using UnityEngine;
using TMPro;
using System.Collections;

public class DaySummaryUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float dayLengthSeconds = 180f; // 3 minutes per day
    [SerializeField] private KeyCode endDayKey = KeyCode.T; // Manual end day (debug)
    
    // Runtime UI
    private GameObject summaryPanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI ratingText;
    private bool dayEnded = false;
    private float dayTimer = 0f;
    private int currentDay = 1;
    
    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayEnd += ShowSummary;
        
        CreateSummaryUI();
    }
    
    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayEnd -= ShowSummary;
    }
    
    void Update()
    {
        if (dayEnded) 
        {
            // Press Space to start next day
            if (Input.GetKeyDown(KeyCode.Space))
                StartNextDay();
            return;
        }
        
        // Day timer
        dayTimer += Time.deltaTime;
        if (dayTimer >= dayLengthSeconds)
        {
            EndDay();
        }
        
        // Debug: manual end day
        if (Input.GetKeyDown(endDayKey))
        {
            EndDay();
        }
    }
    
    void EndDay()
    {
        if (dayEnded) return; // Prevent double-trigger
        
        if (GameManager.Instance != null)
            GameManager.Instance.EndDay();
        else
            ShowSummary(); // Fallback if no GameManager
    }
    
    // ==================== UI CREATION ====================
    
    void CreateSummaryUI()
    {
        // Create a Canvas for the summary
        GameObject canvasObj = new GameObject("DaySummaryCanvas");
        canvasObj.transform.parent = transform;
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        
        // Dark background panel
        summaryPanel = new GameObject("SummaryPanel");
        summaryPanel.transform.parent = canvasObj.transform;
        
        RectTransform panelRect = summaryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        UnityEngine.UI.Image bgImage = summaryPanel.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.12f, 0.92f);
        
        // Title
        titleText = CreateUIText("Title", summaryPanel.transform, 
            new Vector2(0, 120f), 42f, Color.white);
        
        // Stats
        statsText = CreateUIText("Stats", summaryPanel.transform, 
            new Vector2(0, -20f), 26f, new Color(0.85f, 0.85f, 0.95f));
        
        // Rating
        ratingText = CreateUIText("Rating", summaryPanel.transform, 
            new Vector2(0, -180f), 36f, Color.yellow);
        
        // "Press Space" instruction
        TextMeshProUGUI continueText = CreateUIText("Continue", summaryPanel.transform, 
            new Vector2(0, -280f), 20f, new Color(0.6f, 0.6f, 0.7f));
        continueText.text = "Press [SPACE] to start next day";
        
        summaryPanel.SetActive(false);
    }
    
    TextMeshProUGUI CreateUIText(string name, Transform parent, Vector2 position, float fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = parent;
        
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 200f);
        rect.anchoredPosition = position;
        
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        
        return tmp;
    }
    
    // ==================== SUMMARY DISPLAY ====================
    
    void ShowSummary()
    {
        dayEnded = true;
        
        // Freeze game
        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(true);
        
        // Gather stats
        int served = GameManager.Instance != null ? GameManager.Instance.CustomersServed : 0;
        int tips = GameManager.Instance != null ? GameManager.Instance.TotalTips : 0;
        float avgSat = GameManager.Instance != null ? GameManager.Instance.AverageSatisfaction : 0f;
        int angry = GameManager.Instance != null ? GameManager.Instance.DissatisfiedCount : 0;
        
        // Title
        titleText.text = $"~ Day {currentDay} Complete ~";
        
        // Stats text
        statsText.text = 
            $"Customers Served:  {served}\n" +
            $"Tips Earned:  ${tips}\n" +
            $"Avg Satisfaction:  {avgSat:P0}\n" +
            $"Customers Lost:  {angry}";
        
        // Rating based on average satisfaction
        string rating;
        Color ratingColor;
        
        if (avgSat >= 0.9f)
        {
            rating = "S RANK\nMeow-sterful!";
            ratingColor = new Color(1f, 0.85f, 0f);
        }
        else if (avgSat >= 0.7f)
        {
            rating = "A RANK\nPaw-some!";
            ratingColor = Color.green;
        }
        else if (avgSat >= 0.5f)
        {
            rating = "B RANK\nNot bad!";
            ratingColor = Color.yellow;
        }
        else if (avgSat >= 0.3f)
        {
            rating = "C RANK\nNeeds work...";
            ratingColor = new Color(1f, 0.5f, 0.2f);
        }
        else
        {
            rating = "D RANK\nRuff day...";
            ratingColor = Color.red;
        }
        
        ratingText.text = rating;
        ratingText.color = ratingColor;
        
        summaryPanel.SetActive(true);
    }
    
    void StartNextDay()
    {
        dayEnded = false;
        dayTimer = 0f;
        currentDay++;
        
        summaryPanel.SetActive(false);
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetDayStats();
            GameManager.Instance.SetPaused(false);
        }
    }
}
