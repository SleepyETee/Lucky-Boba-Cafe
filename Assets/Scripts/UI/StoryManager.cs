// ============================================================
// FILE: StoryManager.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Shows a dialogue/briefing panel at the start of
//              each day. All UI references assigned in Inspector.
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{
    [Header("UI (assign in Inspector)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI continuePrompt;

    [Header("Day Briefings (one per day, index 0 = Day 1)")]
    [SerializeField] [TextArea] private string[] dayBriefings = new string[]
    {
        "Welcome to Lucky Boba Cafe!\n\nYou just opened your very first cafe on the edge of the city.\nServe boba to the cats of Catpasteddon and earn enough PawCoins to stay open!\n\nGood luck!",
        "Day 2 - Word is spreading!\n\nMore customers are coming, and they're a bit less patient.\nKeep up the pace!",
        "Day 3 - New drinks on the menu!\n\nCustomers want more variety now.\nWatch out for Rushers -- they won't wait long!",
        "Day 4 - The competition heats up!\n\nA rival cafe opened across the street.\nYou need to impress everyone today. VIPs might show up!",
        "Day 5 - The Final Day!\n\nThis is it. Prove that Lucky Boba Cafe is the best in town.\nGive it everything you've got!"
    };

    private bool isShowing = false;

    void Start()
    {
        EnsureRuntimeUI();

        if (continuePrompt != null)
            continuePrompt.text = "Press Space to start";

        ShowBriefing();
    }

    void Update()
    {
        if (!isShowing) return;

        if (GameInput.ConfirmPressed || GameInput.EnterPressed || GameInput.ClickPressed)
        {
            Debug.Log("[StoryManager] Input detected — dismissing briefing.");
            Dismiss();
        }
    }

    void ShowBriefing()
    {
        EnsureRuntimeUI();

        int day = 1;
        if (GameManager.Instance != null)
            day = Mathf.Max(1, GameManager.Instance.CurrentDay);

        int index = day - 1;

        string message;
        if (dayBriefings != null && index < dayBriefings.Length && !string.IsNullOrEmpty(dayBriefings[index]))
            message = dayBriefings[index];
        else
            message = $"Day {day}\n\nGood luck!";

        if (dialogueText != null)
            dialogueText.text = message;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            isShowing = true;

            if (GameManager.Instance != null)
                GameManager.Instance.RequestPause();
        }
    }

    void EnsureRuntimeUI()
    {
        if (dialoguePanel != null && dialogueText != null)
            return;

        GameObject canvasObj = new GameObject("StoryBriefingCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        dialoguePanel = new GameObject("StoryBriefingPanel");
        dialoguePanel.transform.SetParent(canvasObj.transform, false);
        Image panelImage = dialoguePanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);

        RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        dialogueText = CreateTMP("StoryBriefingText", dialoguePanel.transform, new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.78f), 40, TextAlignmentOptions.Center);
        continuePrompt = CreateTMP("StoryBriefingPrompt", dialoguePanel.transform, new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.22f), 26, TextAlignmentOptions.Center);
        dialoguePanel.SetActive(false);
    }

    TextMeshProUGUI CreateTMP(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.color = Color.white;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }

    void Dismiss()
    {
        isShowing = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.ReleasePause();
    }
}
