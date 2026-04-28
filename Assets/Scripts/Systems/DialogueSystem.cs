// ============================================================
// FILE: DialogueSystem.cs
// DESCRIPTION: RPG Dialogue system with choices
// Part of the unique Project 2 Village exploration mechanic
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }
    
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image speakerPortrait;
    [SerializeField] private TextMeshProUGUI speakerName;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Button continueButton;
    
    [Header("Settings")]
    [SerializeField] private float textSpeed = 0.03f;
    [SerializeField] private bool skipOnClick = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip talkSound;
    [SerializeField] private AudioClip choiceSound;
    
    // State
    private Dialogue currentDialogue;
    private int currentLineIndex;
    private bool isTyping;
    private bool waitingForChoice;
    private Coroutine typingCoroutine;
    
    // Callbacks
    private System.Action onDialogueEnd;
    private System.Action<int> onChoiceMade;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
    
    void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
    
    void Update()
    {
        // Skip to end of text on click
        if (isTyping && skipOnClick && GameInput.ClickPressed)
        {
            FinishTyping();
        }
    }
    
    // ==================== PUBLIC API ====================
    
    public void StartDialogue(Dialogue dialogue, System.Action onEnd = null)
    {
        currentDialogue = dialogue;
        currentLineIndex = 0;
        onDialogueEnd = onEnd;
        
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        
        // Pause game time (optional)
        // Time.timeScale = 0;
        
        ShowCurrentLine();
    }
    
    public void StartDialogue(string speakerNameText, string[] lines, Sprite portrait = null, System.Action onEnd = null)
    {
        Dialogue dialogue = new Dialogue
        {
            speakerName = speakerNameText,
            portrait = portrait,
            lines = new List<DialogueLine>()
        };
        
        foreach (string line in lines)
        {
            dialogue.lines.Add(new DialogueLine { text = line });
        }
        
        StartDialogue(dialogue, onEnd);
    }
    
    public void ShowChoices(string[] choices, System.Action<int> onChoice)
    {
        onChoiceMade = onChoice;
        waitingForChoice = true;
        
        // Hide continue button
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        
        // Clear old choices
        if (choicesContainer != null)
        {
            foreach (Transform child in choicesContainer)
                Destroy(child.gameObject);
        }
        
        // Create choice buttons
        for (int i = 0; i < choices.Length; i++)
        {
            int choiceIndex = i;
            GameObject btn = Instantiate(choiceButtonPrefab, choicesContainer);
            
            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = choices[i];
            
            Button button = btn.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
        }
    }
    
    public void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        waitingForChoice = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        
        currentDialogue = null;

        System.Action callback = onDialogueEnd;
        onDialogueEnd = null;
        callback?.Invoke();
    }
    
    public bool IsDialogueActive()
    {
        return dialoguePanel != null && dialoguePanel.activeSelf;
    }
    
    // ==================== INTERNAL ====================
    
    void ShowCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
            return;
        }
        
        DialogueLine line = currentDialogue.lines[currentLineIndex];
        
        // Update speaker info
        if (speakerName != null)
            speakerName.text = line.speakerOverride ?? currentDialogue.speakerName;
        
        if (speakerPortrait != null)
        {
            Sprite portrait = line.portraitOverride ?? currentDialogue.portrait;
            if (portrait != null)
            {
                speakerPortrait.sprite = portrait;
                speakerPortrait.gameObject.SetActive(true);
            }
            else
            {
                speakerPortrait.gameObject.SetActive(false);
            }
        }
        
        // Type out text
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        typingCoroutine = StartCoroutine(TypeText(line.text));
        
        // Check if this line has choices
        if (line.choices != null && line.choices.Length > 0)
        {
            // Will show choices after text finishes
        }
    }
    
    IEnumerator TypeText(string text)
    {
        isTyping = true;
        if (dialogueText != null) dialogueText.text = "";
        
        // Hide choices and show continue
        if (choicesContainer != null)
        {
            foreach (Transform child in choicesContainer)
                Destroy(child.gameObject);
        }
        
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        
        // Type each character
        foreach (char c in text)
        {
            if (dialogueText != null) dialogueText.text += c;
            
            // Play talk sound occasionally
            if (Random.value > 0.7f && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(talkSound);
            
            yield return new WaitForSecondsRealtime(textSpeed);
        }
        
        isTyping = false;
        OnTextComplete();
    }
    
    void FinishTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        if (currentDialogue != null && currentLineIndex < currentDialogue.lines.Count && dialogueText != null)
        {
            dialogueText.text = currentDialogue.lines[currentLineIndex].text;
        }
        
        isTyping = false;
        OnTextComplete();
    }
    
    void OnTextComplete()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.lines.Count)
            return;
        
        DialogueLine line = currentDialogue.lines[currentLineIndex];
        
        // Check for choices
        if (line?.choices != null && line.choices.Length > 0)
        {
            ShowChoices(line.choices, (choiceIndex) =>
            {
                // Handle choice result
                line.onChoiceMade?.Invoke(choiceIndex);
                waitingForChoice = false;
                currentLineIndex++;
                ShowCurrentLine();
            });
        }
        else
        {
            // Show continue button
            if (continueButton != null)
                continueButton.gameObject.SetActive(true);
        }
    }
    
    void OnContinueClicked()
    {
        if (isTyping)
        {
            FinishTyping();
        }
        else if (!waitingForChoice)
        {
            currentLineIndex++;
            ShowCurrentLine();
        }
    }
    
    void OnChoiceSelected(int index)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(choiceSound);
        
        if (choicesContainer != null)
        {
            foreach (Transform child in choicesContainer)
                Destroy(child.gameObject);
        }
        
        onChoiceMade?.Invoke(index);
    }
}

// ============================================================
// Dialogue Data Classes
// ============================================================
[System.Serializable]
public class Dialogue
{
    public string speakerName;
    public Sprite portrait;
    public List<DialogueLine> lines;
}

[System.Serializable]
public class DialogueLine
{
    public string text;
    public string speakerOverride;
    public Sprite portraitOverride;
    public string[] choices;
    public System.Action<int> onChoiceMade;
}
