using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class VillageEntrance : MonoBehaviour
{
    public enum EntranceType { Shop, Neighbor, GoalBoard, Home }
    
    [Header("Settings")]
    public EntranceType type;
    public int neighborIndex;
    public string locationName = "Shop";
    
    [Header("Prompt")]
    [SerializeField] private GameObject prompt;
    [SerializeField] private TextMeshProUGUI promptText;
    
    private bool inRange = false;
    
    void Start()
    {
        if (prompt) prompt.SetActive(false);
        if (promptText) promptText.text = $"Press E to enter {locationName}";
    }
    
    void Update()
    {
        if (inRange && GameInput.InteractPressed)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            switch (type)
            {
                case EntranceType.Shop:
                    if (ShopSystem.Instance != null) ShopSystem.Instance.OpenShop();
                    break;
                case EntranceType.Neighbor:
                    if (NeighborSystem.Instance != null) NeighborSystem.Instance.VisitNeighbor(neighborIndex);
                    break;
                case EntranceType.GoalBoard:
                    if (GoalSystem.Instance != null) GoalSystem.Instance.OpenGoalBoard();
                    break;
                case EntranceType.Home:
                    if (VillageManager.Instance != null) VillageManager.Instance.ReturnHome();
                    break;
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) { inRange = true; if (prompt) prompt.SetActive(true); }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) { inRange = false; if (prompt) prompt.SetActive(false); }
    }
}
