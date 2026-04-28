// ============================================================
// FILE: DeliveryObstacle.cs
// DESCRIPTION: Static obstacles on the road
// Cones, trash cans, barriers, etc.
// ============================================================
using UnityEngine;

public class DeliveryObstacle : MonoBehaviour
{
    public enum ObstacleType
    {
        Cone,
        TrashCan,
        Barrier,
        Pothole,
        Puddle
    }
    
    [Header("Type")]
    public ObstacleType type = ObstacleType.Cone;
    
    [Header("Behavior")]
    public bool canBeDestroyed = false;
    public bool slowsPlayer = false;
    public float slowAmount = 0.5f;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public AudioClip hitSound;
    
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        
        if (hitEffect)
        {
            GameObject fx = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(fx, 3f);
        }
        
        // Play sound
        if (hitSound && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hitSound);
        }
        
        // Destroy if breakable
        if (canBeDestroyed)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (!slowsPlayer) return;
        if (!other.CompareTag("Player")) return;
        
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity *= slowAmount;
    }
}
