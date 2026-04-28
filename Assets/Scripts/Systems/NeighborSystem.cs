using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NeighborSystem : MonoBehaviour
{
    public static NeighborSystem Instance { get; private set; }
    
    [System.Serializable]
    public class Neighbor
    {
        public string name;
        public string favoriteDrink;
        public int friendship;
        public string[] dialogues;
    }
    
    [Header("Neighbors")]
    [SerializeField] private List<Neighbor> neighbors = new List<Neighbor>();
    
    [Header("UI")]
    [SerializeField] private GameObject neighborPanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Slider friendshipBar;
    [SerializeField] private Button giveTeaButton;
    [SerializeField] private Button chatButton;
    [SerializeField] private Button closeButton;
    
    private Neighbor current;
    
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    void OnDestroy() { if (Instance == this) Instance = null; }
    
    void Start()
    {
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (giveTeaButton) giveTeaButton.onClick.AddListener(GiveTea);
        if (chatButton) chatButton.onClick.AddListener(Chat);
        InitNeighbors();
        LoadFriendships();
    }
    
    void InitNeighbors()
    {
        neighbors.Clear();
        neighbors.Add(new Neighbor { name = "Granny Whiskers", favoriteDrink = "Green Tea", dialogues = new[] { "Hello dear!", "Try adding honey~" } });
        neighbors.Add(new Neighbor { name = "Chef Mittens", favoriteDrink = "Taro Milk Tea", dialogues = new[] { "Fresh ingredients!", "Experiment more!" } });
        neighbors.Add(new Neighbor { name = "Luna", favoriteDrink = "Matcha Latte", dialogues = new[] { "The moon is lovely...", "Matcha calms the spirit." } });
        neighbors.Add(new Neighbor { name = "Boba Jr.", favoriteDrink = "Brown Sugar Boba", dialogues = new[] { "More sugar!", "Can I help at your cafe?" } });
    }
    
    public void VisitNeighbor(int index)
    {
        if (index < 0 || index >= neighbors.Count) return;
        current = neighbors[index];
        if (neighborPanel) neighborPanel.SetActive(true);
        UpdateUI();
    }
    
    public void GiveTea()
    {
        if (current == null) return;

        if (InventorySystem.Instance != null)
        {
            if (InventorySystem.Instance.GetStockByName(current.favoriteDrink) <= 0)
            {
                if (dialogueText) dialogueText.text = $"You don't have any {current.favoriteDrink}...";
                return;
            }
            InventorySystem.Instance.RemoveItemByName(current.favoriteDrink);
        }

        current.friendship = Mathf.Min(current.friendship + 20, 100);
        if (dialogueText) dialogueText.text = $"Oh, {current.favoriteDrink}! My favorite! Thank you!";
        UpdateUI();
        SaveFriendships();
    }
    
    public void Chat()
    {
        if (current == null) return;
        current.friendship = Mathf.Min(current.friendship + 5, 100);
        if (dialogueText && current.dialogues.Length > 0) dialogueText.text = current.dialogues[Random.Range(0, current.dialogues.Length)];
        UpdateUI();
        SaveFriendships();
    }
    
    void UpdateUI()
    {
        if (nameText) nameText.text = current.name;
        if (friendshipBar) friendshipBar.value = current.friendship / 100f;
    }
    
    public void Close() { if (neighborPanel) neighborPanel.SetActive(false); current = null; }

    /// <summary>
    /// Add friendship points to a neighbor by name.
    /// Called by QuestSystem when giving quest rewards.
    /// </summary>
    public void AddFriendship(string npcName, int points)
    {
        if (string.IsNullOrEmpty(npcName) || points <= 0) return;
        foreach (var neighbor in neighbors)
        {
            if (neighbor.name == npcName)
            {
                neighbor.friendship = Mathf.Min(neighbor.friendship + points, 100);
                SaveFriendships();
                Debug.Log($"[Neighbor] {npcName} friendship +{points} (now {neighbor.friendship})");
                return;
            }
        }
    }

    /// <summary>
    /// Get friendship level for a neighbor by name.
    /// </summary>
    public int GetFriendship(string npcName)
    {
        foreach (var neighbor in neighbors)
        {
            if (neighbor.name == npcName)
                return neighbor.friendship;
        }
        return 0;
    }
    
    void SaveFriendships()
    {
        foreach (var neighbor in neighbors)
            PlayerPrefs.SetInt($"Friend_{neighbor.name}", neighbor.friendship);
        PlayerPrefs.Save();
    }
    
    void LoadFriendships()
    {
        foreach (var neighbor in neighbors)
            neighbor.friendship = PlayerPrefs.GetInt($"Friend_{neighbor.name}", 0);
    }
}
