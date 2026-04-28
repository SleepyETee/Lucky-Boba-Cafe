// ============================================================
// FILE: VillageNPC.cs
// AUTHOR: Long + Claude
// DESCRIPTION: NPC controller for village characters
// Handles dialogue, quests, and interactions
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class VillageNPC : MonoBehaviour, IInteractable
{
    [Header("NPC Info")]
    public string npcName = "Villager";
    public Sprite portrait;
    [TextArea(3, 5)]
    public string[] greetingDialogues;
    
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    
    [Header("Quest Giver")]
    [SerializeField] private bool isQuestGiver = true;
    [SerializeField] private GameObject questAvailableIcon; // ! icon
    [SerializeField] private GameObject questInProgressIcon; // ? icon
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    private Transform player;
    private bool playerInRange;
    private bool useTriggerMode;
    
    void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        
        UpdateQuestIcons();
    }
    
    void Update()
    {
        if (!useTriggerMode)
            CheckPlayerDistance();
        
        if (playerInRange && GameInput.InteractPressed)
        {
            Interact();
        }
        
        if (playerInRange)
        {
            RefreshPlayerRef();
            if (player != null)
            {
                Vector3 dir = player.position - transform.position;
                if (dir.x != 0)
                {
                    transform.localScale = new Vector3(
                        Mathf.Sign(dir.x) * Mathf.Abs(transform.localScale.x),
                        transform.localScale.y,
                        transform.localScale.z
                    );
                }
            }
        }
    }
    
    // ==================== IInteractable Implementation ====================

    public void OnPlayerEnterRange()
    {
        useTriggerMode = true;
        playerInRange = true;
        RefreshPlayerRef();

        if (interactPrompt != null)
            interactPrompt.SetActive(true);
        if (promptText != null)
            promptText.text = $"Press [E] to talk to {npcName}";
    }

    public void OnPlayerExitRange()
    {
        playerInRange = false;
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void RefreshPlayerRef()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            player = playerObj != null ? playerObj.transform : null;
        }
    }

    public string GetInteractionPrompt()
    {
        return $"Press [E] to talk to {npcName}";
    }
    
    void CheckPlayerDistance()
    {
        RefreshPlayerRef();
        if (player == null) return;
        
        float distance = Vector2.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactRange;
        
        if (playerInRange != wasInRange)
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(playerInRange);
            
            if (promptText != null)
                promptText.text = $"Press [E] to talk to {npcName}";
        }
    }
    
    // ==================== INTERACTION (UPDATED for quest objectives) ====================

    public void Interact()
    {
        if (DialogueSystem.Instance == null || DialogueSystem.Instance.IsDialogueActive())
            return;
        
        bool completedSomething = false;

        if (QuestSystem.Instance != null)
        {
            // First: complete any active quest objectives involving this NPC
            completedSomething = TryCompleteTalkObjectives() | TryCompleteDeliverObjectives();

            // Second: offer new quests if this NPC is a quest giver
            if (isQuestGiver)
            {
                List<Quest> availableQuests = QuestSystem.Instance.GetAvailableQuests(npcName);

                if (availableQuests.Count > 0)
                {
                    OfferQuest(availableQuests[0]);
                    return;
                }
            }

            // If we completed an objective, show a thank-you
            if (completedSomething)
            {
                DialogueSystem.Instance.StartDialogue(npcName,
                    new string[] { "Thank you so much! You have been a great help." }, portrait);
                UpdateQuestIcons();
                return;
            }
        }
        
        // Regular greeting dialogue
        ShowGreeting();
    }

    /// <summary>
    /// Completes any "Talk to [this NPC]" objectives in active quests.
    /// </summary>
    bool TryCompleteTalkObjectives()
    {
        if (QuestSystem.Instance == null) return false;

        bool completed = false;

        foreach (var quest in QuestSystem.Instance.GetActiveQuests())
        {
            foreach (var obj in quest.objectives)
            {
                if (!obj.isCompleted
                    && obj.type == ObjectiveType.Talk
                    && obj.targetNPC == npcName)
                {
                    QuestSystem.Instance.CompleteObjective(quest.id, obj.id);
                    completed = true;
                }
            }
        }

        return completed;
    }

    /// <summary>
    /// Auto-completes "Deliver [item] to [this NPC]" objectives when
    /// the player talks to the quest giver. In a full inventory system
    /// you'd check whether the player actually holds the item; here
    /// we treat the conversation itself as the hand-off.
    /// </summary>
    bool TryCompleteDeliverObjectives()
    {
        if (QuestSystem.Instance == null) return false;

        bool completed = false;

        foreach (var quest in QuestSystem.Instance.GetActiveQuests())
        {
            // Only complete deliver objectives if this NPC is the quest giver
            if (quest.giver != npcName) continue;

            foreach (var obj in quest.objectives)
            {
                if (obj.isCompleted || obj.type != ObjectiveType.Deliver) continue;

                // Guard: don't auto-complete Deliver if a preceding Find
                // objective for the same item hasn't been finished yet.
                bool prerequisiteMet = true;
                foreach (var other in quest.objectives)
                {
                    if (other == obj) break; // only check objectives listed before this one
                    if (!other.isCompleted && other.type == ObjectiveType.Find
                        && other.targetItem == obj.targetItem)
                    {
                        prerequisiteMet = false;
                        break;
                    }
                }
                if (!prerequisiteMet) continue;

                QuestSystem.Instance.CompleteObjective(quest.id, obj.id);
                completed = true;
            }
        }

        return completed;
    }
    
    // ==================== DIALOGUE ====================
    
    void ShowGreeting()
    {
        if (greetingDialogues == null || greetingDialogues.Length == 0)
        {
            DialogueSystem.Instance.StartDialogue(npcName, new string[] { "Hello there!" }, portrait);
        }
        else
        {
            DialogueSystem.Instance.StartDialogue(npcName, greetingDialogues, portrait);
        }
    }
    
    void OfferQuest(Quest quest)
    {
        // Show quest dialogue
        Dialogue questDialogue = new Dialogue
        {
            speakerName = npcName,
            portrait = portrait,
            lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    text = quest.description
                },
                new DialogueLine
                {
                    text = "Will you help me?",
                    choices = new string[] { "Yes, I will help!", "Not right now" },
                    onChoiceMade = (choice) =>
                    {
                        if (choice == 0)
                        {
                            QuestSystem.Instance.StartQuest(quest.id);
                        }
                    }
                }
            }
        };
        
        DialogueSystem.Instance.StartDialogue(questDialogue, () =>
        {
            UpdateQuestIcons();
        });
    }
    
    void UpdateQuestIcons()
    {
        if (!isQuestGiver || QuestSystem.Instance == null) return;
        
        List<Quest> available = QuestSystem.Instance.GetAvailableQuests(npcName);

        // Check if this NPC has a quest currently in progress
        bool hasActiveQuest = false;
        foreach (var quest in QuestSystem.Instance.GetActiveQuests())
        {
            if (quest.giver == npcName)
            {
                hasActiveQuest = true;
                break;
            }
        }
        
        if (questAvailableIcon != null)
            questAvailableIcon.SetActive(available.Count > 0 && !hasActiveQuest);
        
        if (questInProgressIcon != null)
            questInProgressIcon.SetActive(hasActiveQuest);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
