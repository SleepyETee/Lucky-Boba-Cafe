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
        collected = true;
        // Complete quest objective
        if (!string.IsNullOrEmpty(questId) && !string.IsNullOrEmpty(objectiveId))
        {
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.CompleteObjective(questId, objectiveId);
            }
        }
        
        // Play sound
        if (AudioManager.Instance != null && pickupSound != null)
            AudioManager.Instance.PlaySFX(pickupSound);
        
        // Show notification
        Debug.Log($"[Collectible] Picked up: {itemName}");
        
        // Destroy item
        Destroy(gameObject);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
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
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
            
            if (glowEffect != null)
                glowEffect.SetActive(false);
        }
    }
}
