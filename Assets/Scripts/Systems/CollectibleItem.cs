// ============================================================
// FILE: CollectibleItem.cs
// DESCRIPTION: Items that can be found/picked up in the village
// Used for quest objectives like "Find the Recipe Book"
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CollectibleItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Item";
    public string itemDescription = "A mysterious item.";
    public Sprite itemIcon;
    
    [Header("Quest")]
    public string questId; // If picking up completes a quest objective
    public string objectiveId;
    
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject glowEffect;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    
    private Vector3 startPos;
    private bool playerInRange;
    private bool collected;
    
    void Start()
    {
        startPos = transform.position;
        
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }
    
    void Update()
    {
        // Bobbing animation
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
        
        // Check for pickup
        if (playerInRange && GameInput.InteractPressed)
        {
            Pickup();
        }
    }
    
    void Pickup()
    {
        if (collected) return;

        // Only pick this up if it actually advances an active quest objective.
        // Otherwise leave it in the world (prevents consuming an item for a quest
        // that isn't active yet, which would soft-lock that quest).
        bool tiedToQuest = !string.IsNullOrEmpty(questId) && !string.IsNullOrEmpty(objectiveId);
        if (tiedToQuest)
        {
            if (QuestSystem.Instance == null || !QuestSystem.Instance.IsObjectiveActive(questId, objectiveId))
            {
                Debug.Log($"[Collectible] {itemName} isn't needed right now — leaving it.");
                return;
            }
            QuestSystem.Instance.CompleteObjective(questId, objectiveId);
        }

        collected = true;

        // Play sound
        if (AudioManager.Instance != null && pickupSound != null)
            AudioManager.Instance.PlaySFX(pickupSound);
        
        Debug.Log($"[Collectible] Picked up: {itemName}");
        
        // Destroy item
        Destroy(gameObject);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            playerInRange = true;
            
            if (interactPrompt != null)
                interactPrompt.SetActive(true);
            
            if (glowEffect != null)
                glowEffect.SetActive(true);
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            playerInRange = false;
            
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
            
            if (glowEffect != null)
                glowEffect.SetActive(false);
        }
    }
}
