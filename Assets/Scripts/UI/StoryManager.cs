// ============================================================
// FILE: StoryManager.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Shows a dialogue/briefing panel at the start of
//              each day. All UI references assigned in Inspector.
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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

    void Dismiss()
    {
        isShowing = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.ReleasePause();
    }
}
