// ============================================================
// FILE: QuestSystem.cs
// DESCRIPTION: RPG Quest system for Village exploration
// THIS IS THE UNIQUE PROJECT 2 MECHANIC - Different from cafe!
// Handles quests, objectives, dialogue choices, rewards
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance { get; private set; }
    
    [Header("All Quests")]
    [SerializeField] private List<Quest> allQuests = new List<Quest>();
    [SerializeField] private List<Quest> activeQuests = new List<Quest>();
    [SerializeField] private List<Quest> completedQuests = new List<Quest>();
    
    [Header("Quest Log UI")]
    [SerializeField] private GameObject questLogPanel;
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questEntryPrefab;
    [SerializeField] private TextMeshProUGUI questDetailTitle;
    [SerializeField] private TextMeshProUGUI questDetailDescription;
    [SerializeField] private TextMeshProUGUI questDetailObjectives;
    [SerializeField] private Button closeLogButton;
    
    [Header("Quest Notification")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 3f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip questStartSound;
    [SerializeField] private AudioClip questCompleteSound;
    [SerializeField] private AudioClip objectiveCompleteSound;
    
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
        InitializeQuests();
        LoadQuestProgress();
        
        if (closeLogButton != null)
            closeLogButton.onClick.AddListener(CloseQuestLog);
        
        if (questLogPanel != null)
            questLogPanel.SetActive(false);
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    void InitializeQuests()
    {
        allQuests = new List<Quest>
        {
            // ========== GRANNY WHISKERS QUESTLINE ==========
            new Quest
            {
                id = "granny_01",
                title = "A Warm Welcome",
                description = "Granny Whiskers looks like she could use some company. Bring her a cup of green tea.",
                giver = "Granny Whiskers",
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "give_tea", description = "Bring Green Tea to Granny", type = ObjectiveType.Deliver, targetItem = "Green Tea", requiredAmount = 1 }
                },
                rewards = new QuestRewards { money = 25, friendshipPoints = 20, friendshipTarget = "Granny Whiskers" },
                nextQuestId = "granny_02"
            },
            new Quest
            {
                id = "granny_02",
                title = "Lost Memories",
                description = "Granny has lost her old recipe book somewhere in the village. Help her find it!",
                giver = "Granny Whiskers",
                prerequisites = new string[] { "granny_01" },
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "find_book", description = "Search the village for the Recipe Book", type = ObjectiveType.Find, targetItem = "Old Recipe Book", requiredAmount = 1 },
                    new QuestObjective { id = "return_book", description = "Return the book to Granny", type = ObjectiveType.Deliver, targetItem = "Old Recipe Book", requiredAmount = 1 }
                },
                rewards = new QuestRewards { money = 50, friendshipPoints = 30, friendshipTarget = "Granny Whiskers", unlockRecipe = "Honey Green Tea" },
                nextQuestId = "granny_03"
            },
            new Quest
            {
                id = "granny_03",
                title = "The Secret Ingredient",
                description = "Granny wants to teach you her special recipe, but first you need to prove yourself.",
                giver = "Granny Whiskers",
                prerequisites = new string[] { "granny_02" },
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "serve_customers", description = "Serve 10 customers with perfect drinks", type = ObjectiveType.Serve, requiredAmount = 10 },
                    new QuestObjective { id = "talk_granny", description = "Return to Granny Whiskers", type = ObjectiveType.Talk, targetNPC = "Granny Whiskers" }
                },
                rewards = new QuestRewards { money = 100, friendshipPoints = 50, friendshipTarget = "Granny Whiskers", unlockRecipe = "Ancient Herbal Tea", canHireNPC = "Granny Whiskers" }
            },
            
            // ========== CHEF MITTENS QUESTLINE ==========
            new Quest
            {
                id = "chef_01",
                title = "Taste Tester",
                description = "Chef Mittens is working on a new recipe and needs your opinion!",
                giver = "Chef Mittens",
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "taste_food", description = "Taste Chef's new creation", type = ObjectiveType.Talk, targetNPC = "Chef Mittens" },
                    new QuestObjective { id = "give_feedback", description = "Give your honest feedback", type = ObjectiveType.Choice }
                },
                rewards = new QuestRewards { money = 30, friendshipPoints = 15, friendshipTarget = "Chef Mittens" },
                nextQuestId = "chef_02"
            },
            new Quest
            {
                id = "chef_02",
                title = "Ingredient Hunt",
                description = "Chef needs rare ingredients from around the village for his masterpiece.",
                giver = "Chef Mittens",
                prerequisites = new string[] { "chef_01" },
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "find_taro", description = "Find Fresh Taro Root", type = ObjectiveType.Find, targetItem = "Fresh Taro", requiredAmount = 1 },
                    new QuestObjective { id = "find_honey", description = "Find Local Honey", type = ObjectiveType.Find, targetItem = "Local Honey", requiredAmount = 1 },
                    new QuestObjective { id = "find_herbs", description = "Find Mountain Herbs", type = ObjectiveType.Find, targetItem = "Mountain Herbs", requiredAmount = 1 }
                },
                rewards = new QuestRewards { money = 75, friendshipPoints = 35, friendshipTarget = "Chef Mittens", unlockRecipe = "Taro Milk Tea" },
                nextQuestId = "chef_03"
            },
            new Quest
            {
                id = "chef_03",
                title = "The Grand Feast",
                description = "Chef Mittens is ready to prepare his masterpiece, but he needs your help in the kitchen!",
                giver = "Chef Mittens",
                prerequisites = new string[] { "chef_02" },
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "serve_perfect", description = "Serve 8 customers with perfect drinks", type = ObjectiveType.Serve, requiredAmount = 8 },
                    new QuestObjective { id = "talk_chef", description = "Return to Chef Mittens", type = ObjectiveType.Talk, targetNPC = "Chef Mittens" }
                },
                rewards = new QuestRewards { money = 120, friendshipPoints = 50, friendshipTarget = "Chef Mittens", unlockRecipe = "Brown Sugar Boba", canHireNPC = "Chef Mittens" }
            },
            
            // ========== LUNA QUESTLINE (Mysterious) ==========
            new Quest
            {
                id = "luna_01",
                title = "Midnight Meeting",
                description = "Luna left a cryptic note: 'Meet me when the moon is high.'",
                giver = "Luna",
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "wait_night", description = "Wait until evening", type = ObjectiveType.Wait },
                    new QuestObjective { id = "find_luna", description = "Find Luna at the hilltop", type = ObjectiveType.Talk, targetNPC = "Luna" }
                },
                rewards = new QuestRewards { friendshipPoints = 25, friendshipTarget = "Luna" },
                nextQuestId = "luna_02"
            },
            new Quest
            {
                id = "luna_02",
                title = "The Moonflower",
                description = "Luna speaks of a rare flower that only blooms under moonlight...",
                giver = "Luna",
                prerequisites = new string[] { "luna_01" },
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "find_flower", description = "Find the Moonflower", type = ObjectiveType.Find, targetItem = "Moonflower", requiredAmount = 1 }
                },
                rewards = new QuestRewards { money = 50, friendshipPoints = 40, friendshipTarget = "Luna", unlockRecipe = "Moonlight Matcha" },
                nextQuestId = "luna_03"
            },
            new Quest
            {
                id = "luna_03",
                title = "The Elder's Secret",
                description = "Luna reveals she knows the Elder Cat who guards the secret of Matcha...",
                giver = "Luna",
                prerequisites = new string[] { "luna_02" },
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "reach_rep", description = "Reach 4-star reputation", type = ObjectiveType.Reputation, requiredAmount = 4 },
                    new QuestObjective { id = "meet_elder", description = "Meet the Elder Cat", type = ObjectiveType.Talk, targetNPC = "Elder Cat" }
                },
                rewards = new QuestRewards { money = 200, friendshipPoints = 50, friendshipTarget = "Luna", unlockRecipe = "Ceremonial Matcha", special = "Matcha Ceremony Unlocked!" }
            },
            
            // ========== BOBA JR QUESTLINE ==========
            new Quest
            {
                id = "boba_01",
                title = "Hide and Seek",
                description = "Boba Jr. wants to play! Can you find where they're hiding?",
                giver = "Boba Jr.",
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "find_boba", description = "Find Boba Jr.'s hiding spot", type = ObjectiveType.Find, targetItem = "Boba Jr.", requiredAmount = 1 }
                },
                rewards = new QuestRewards { friendshipPoints = 20, friendshipTarget = "Boba Jr." },
                nextQuestId = "boba_02"
            },
            new Quest
            {
                id = "boba_02",
                title = "Homework Help",
                description = "Boba Jr. is struggling with homework about tea history. Help them!",
                giver = "Boba Jr.",
                prerequisites = new string[] { "boba_01" },
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "talk_granny_history", description = "Ask Granny about tea history", type = ObjectiveType.Talk, targetNPC = "Granny Whiskers" },
                    new QuestObjective { id = "return_boba", description = "Tell Boba Jr. what you learned", type = ObjectiveType.Talk, targetNPC = "Boba Jr." }
                },
                rewards = new QuestRewards { friendshipPoints = 25, friendshipTarget = "Boba Jr.", money = 20 },
                nextQuestId = "boba_03"
            },
            new Quest
            {
                id = "boba_03",
                title = "The Big Audition",
                description = "Boba Jr. wants to prove they can work at your cafe!",
                giver = "Boba Jr.",
                prerequisites = new string[] { "boba_02" },
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "train_boba", description = "Let Boba Jr. help serve 5 customers", type = ObjectiveType.Serve, requiredAmount = 5 },
                    new QuestObjective { id = "evaluate", description = "Evaluate their performance", type = ObjectiveType.Choice }
                },
                rewards = new QuestRewards { friendshipPoints = 40, friendshipTarget = "Boba Jr.", canHireNPC = "Boba Jr." }
            }
        };
    }
    
    // ==================== QUEST MANAGEMENT ====================
    
    public void StartQuest(string questId)
    {
        Quest quest = GetQuest(questId);
        if (quest == null || activeQuests.Contains(quest)) return;
        
        // Check prerequisites
        if (quest.prerequisites != null)
        {
            foreach (string prereq in quest.prerequisites)
            {
                if (!IsQuestCompleted(prereq))
                {
                    Debug.Log($"[Quest] Cannot start {questId} - missing prerequisite: {prereq}");
                    return;
                }
            }
        }
        
        activeQuests.Add(quest);
        quest.isActive = true;
        
        ShowNotification($"New Quest: {quest.title}");
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(questStartSound);
        
        SaveQuestProgress();
        
        Debug.Log($"[Quest] Started: {quest.title}");
    }
    
    public void CompleteObjective(string questId, string objectiveId, int amount = 1)
    {
        Quest quest = GetActiveQuest(questId);
        if (quest == null) return;
        
        QuestObjective objective = quest.objectives.Find(o => o.id == objectiveId);
        if (objective == null || objective.isCompleted) return;
        
        objective.currentAmount += amount;
        
        if (objective.currentAmount >= objective.requiredAmount)
        {
            objective.isCompleted = true;
            
            ShowNotification($"Objective Complete: {objective.description}");
            
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(objectiveCompleteSound);
            
            Debug.Log($"[Quest] Objective complete: {objective.description}");
        }
        
        // Check if all objectives complete
        CheckQuestCompletion(quest);
        
        SaveQuestProgress();
    }
    
    void CheckQuestCompletion(Quest quest)
    {
        foreach (var obj in quest.objectives)
        {
            if (!obj.isCompleted) return;
        }
        
        // All objectives complete!
        CompleteQuest(quest);
    }
    
    void CompleteQuest(Quest quest)
    {
        activeQuests.Remove(quest);
        completedQuests.Add(quest);
        quest.isCompleted = true;
        quest.isActive = false;
        
        // Give rewards
        GiveRewards(quest.rewards);
        
        ShowNotification($"Quest Complete: {quest.title}!");
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(questCompleteSound);
        
        if (!string.IsNullOrEmpty(quest.nextQuestId))
        {
            string nextId = quest.nextQuestId;
            if (hideNotifCoroutine != null) StopCoroutine(hideNotifCoroutine);
            hideNotifCoroutine = StartCoroutine(HideNotificationAfterDelay(notificationDuration));
            StartCoroutine(DelayedStartQuest(nextId, notificationDuration + 0.5f));
        }
        
        SaveQuestProgress();
        
        Debug.Log($"[Quest] Completed: {quest.title}");
    }
    
    void GiveRewards(QuestRewards rewards)
    {
        if (rewards.money > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(rewards.money);
            Debug.Log($"[Quest] Reward: +${rewards.money}");
        }
        
        if (rewards.friendshipPoints > 0 && !string.IsNullOrEmpty(rewards.friendshipTarget))
        {
            if (NeighborSystem.Instance != null)
            {
                NeighborSystem.Instance.AddFriendship(rewards.friendshipTarget, rewards.friendshipPoints);
            }
            Debug.Log($"[Quest] Reward: +{rewards.friendshipPoints} friendship with {rewards.friendshipTarget}");
        }
        
        if (!string.IsNullOrEmpty(rewards.unlockRecipe))
        {
            string recipes = PlayerPrefs.GetString("Recipes", "");
            if (!recipes.Contains(rewards.unlockRecipe))
            {
                recipes += rewards.unlockRecipe + ",";
                PlayerPrefs.SetString("Recipes", recipes);
            }
            Debug.Log($"[Quest] Reward: Unlocked recipe - {rewards.unlockRecipe}");
        }
        
        if (!string.IsNullOrEmpty(rewards.canHireNPC))
        {
            PlayerPrefs.SetInt($"CanHire_{rewards.canHireNPC}", 1);
            Debug.Log($"[Quest] Reward: Can now hire {rewards.canHireNPC}!");
        }
        
        if (!string.IsNullOrEmpty(rewards.special))
        {
            ShowNotification($"Special: {rewards.special}");
        }
    }
    
    // ==================== QUEST QUERIES ====================
    
    public Quest GetQuest(string questId)
    {
        return allQuests.Find(q => q.id == questId);
    }
    
    public Quest GetActiveQuest(string questId)
    {
        return activeQuests.Find(q => q.id == questId);
    }
    
    public bool IsQuestActive(string questId)
    {
        return activeQuests.Exists(q => q.id == questId);
    }
    
    public bool IsQuestCompleted(string questId)
    {
        return completedQuests.Exists(q => q.id == questId);
    }
    
    public List<Quest> GetAvailableQuests(string npcName)
    {
        List<Quest> available = new List<Quest>();
        
        foreach (var quest in allQuests)
        {
            if (quest.giver != npcName) continue;
            if (quest.isActive || quest.isCompleted) continue;
            
            // Check prerequisites
            bool canStart = true;
            if (quest.prerequisites != null)
            {
                foreach (string prereq in quest.prerequisites)
                {
                    if (!IsQuestCompleted(prereq))
                    {
                        canStart = false;
                        break;
                    }
                }
            }
            
            if (canStart) available.Add(quest);
        }
        
        return available;
    }
    
    /// <summary>
    /// Returns a copy of the active quest list so other scripts
    /// (e.g. VillageNPC) can check for Talk/Deliver objectives.
    /// </summary>
    public List<Quest> GetActiveQuests()
    {
        return new List<Quest>(activeQuests);
    }

    // ==================== UI ====================
    
    public void OpenQuestLog()
    {
        if (questLogPanel != null)
            questLogPanel.SetActive(true);
        
        RefreshQuestList();
    }
    
    public void CloseQuestLog()
    {
        if (questLogPanel != null)
            questLogPanel.SetActive(false);
    }
    
    void RefreshQuestList()
    {
        if (questListContainer == null) return;
        
        // Clear existing
        foreach (Transform child in questListContainer)
            Destroy(child.gameObject);
        
        // Show active quests
        foreach (var quest in activeQuests)
        {
            if (questEntryPrefab != null)
            {
                var entry = Instantiate(questEntryPrefab, questListContainer);
                var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    int completed = quest.objectives.FindAll(o => o.isCompleted).Count;
                    text.text = $"{quest.title} ({completed}/{quest.objectives.Count})";
                }
                
                var button = entry.GetComponent<Button>();
                if (button != null)
                {
                    Quest q = quest;
                    button.onClick.AddListener(() => ShowQuestDetail(q));
                }
            }
        }
    }
    
    void ShowQuestDetail(Quest quest)
    {
        if (questDetailTitle != null)
            questDetailTitle.text = quest.title;
        
        if (questDetailDescription != null)
            questDetailDescription.text = quest.description;
        
        if (questDetailObjectives != null)
        {
            string objectives = "Objectives:\n";
            foreach (var obj in quest.objectives)
            {
                string status = obj.isCompleted ? "[DONE]" : "[ ]";
                objectives += $"{status} {obj.description}";
                if (obj.requiredAmount > 1)
                    objectives += $" ({obj.currentAmount}/{obj.requiredAmount})";
                objectives += "\n";
            }
            questDetailObjectives.text = objectives;
        }
    }
    
    private Coroutine hideNotifCoroutine;

    void ShowNotification(string message)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);
            if (hideNotifCoroutine != null) StopCoroutine(hideNotifCoroutine);
            hideNotifCoroutine = StartCoroutine(HideNotificationAfterDelay(notificationDuration));
        }
    }

    System.Collections.IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HideNotification();
        hideNotifCoroutine = null;
    }
    
    System.Collections.IEnumerator DelayedStartQuest(string questId, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        StartQuest(questId);
    }

    void HideNotification()
    {
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    // ==================== SAVE/LOAD ====================
    
    void SaveQuestProgress()
    {
        // Save active quests with objective progress
        // Format per quest: "questId:amount1,amount2,..." separated by ";"
        string activeData = "";
        foreach (var q in activeQuests)
        {
            activeData += q.id + ":";
            for (int i = 0; i < q.objectives.Count; i++)
            {
                if (i > 0) activeData += ",";
                activeData += q.objectives[i].currentAmount;
            }
            activeData += ";";
        }
        PlayerPrefs.SetString("ActiveQuests", activeData);
        
        // Save completed quests (no objective data needed)
        string completedIds = "";
        foreach (var q in completedQuests)
            completedIds += q.id + ",";
        PlayerPrefs.SetString("CompletedQuests", completedIds);
        
        PlayerPrefs.Save();
    }
    
    void LoadQuestProgress()
    {
        // Clear lists to prevent duplicates if called multiple times
        completedQuests.Clear();
        activeQuests.Clear();

        // Reset all quest flags
        foreach (var q in allQuests)
        {
            q.isActive = false;
            q.isCompleted = false;
            foreach (var obj in q.objectives)
            {
                obj.currentAmount = 0;
                obj.isCompleted = false;
            }
        }

        // Load completed quests
        string completedIds = PlayerPrefs.GetString("CompletedQuests", "");
        foreach (var id in completedIds.Split(','))
        {
            if (string.IsNullOrEmpty(id)) continue;
            Quest q = GetQuest(id);
            if (q != null && !completedQuests.Contains(q))
            {
                q.isCompleted = true;
                // Mark all objectives as complete
                foreach (var obj in q.objectives)
                {
                    obj.currentAmount = obj.requiredAmount;
                    obj.isCompleted = true;
                }
                completedQuests.Add(q);
            }
        }
        
        // Load active quests with objective progress
        string activeData = PlayerPrefs.GetString("ActiveQuests", "");
        foreach (var entry in activeData.Split(';'))
        {
            if (string.IsNullOrEmpty(entry)) continue;
            string[] parts = entry.Split(':');
            string questId = parts[0];
            if (string.IsNullOrEmpty(questId)) continue;

            Quest q = GetQuest(questId);
            if (q != null && !q.isCompleted && !activeQuests.Contains(q))
            {
                q.isActive = true;
                activeQuests.Add(q);

                // Restore objective progress if available
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    string[] amounts = parts[1].Split(',');
                    for (int i = 0; i < Mathf.Min(amounts.Length, q.objectives.Count); i++)
                    {
                        if (int.TryParse(amounts[i], out int amount))
                        {
                            q.objectives[i].currentAmount = amount;
                            q.objectives[i].isCompleted = amount >= q.objectives[i].requiredAmount;
                        }
                    }
                }
            }
        }
    }
}

// ============================================================
// Quest Data Classes
// ============================================================
[System.Serializable]
public class Quest
{
    public string id;
    public string title;
    public string description;
    public string giver;
    public string[] prerequisites;
    public List<QuestObjective> objectives;
    public QuestRewards rewards;
    public string nextQuestId;
    public bool isActive;
    public bool isCompleted;
}

[System.Serializable]
public class QuestObjective
{
    public string id;
    public string description;
    public ObjectiveType type;
    public string targetItem;
    public string targetNPC;
    public int requiredAmount = 1;
    public int currentAmount = 0;
    public bool isCompleted;
}

public enum ObjectiveType
{
    Talk,       // Talk to NPC
    Deliver,    // Give item to NPC
    Find,       // Find item in world
    Serve,      // Serve X customers
    Reputation, // Reach X reputation
    Wait,       // Wait for condition
    Choice      // Make a dialogue choice
}

[System.Serializable]
public class QuestRewards
{
    public int money;
    public int friendshipPoints;
    public string friendshipTarget;
    public string unlockRecipe;
    public string canHireNPC;
    public string special;
}
