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
    
    static bool IsScooter(GameObject go)
    {
        return go.GetComponent<DeliveryScooter>() != null || go.CompareTag("Player");
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsScooter(col.gameObject)) return;
        
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
        if (!IsScooter(other.gameObject)) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        // Cap speed to a fixed fraction of the scooter's top speed while inside
        // the trigger. (The old code multiplied velocity every physics frame,
        // which compounded down to ~0 and stuck the player.)
        DeliveryScooter scooter = other.GetComponent<DeliveryScooter>()
            ?? other.GetComponentInParent<DeliveryScooter>();
        float baseSpeed = scooter != null ? scooter.maxSpeed : 12f;
        float speedCap = baseSpeed * Mathf.Clamp01(slowAmount);

        if (rb.linearVelocity.magnitude > speedCap)
            rb.linearVelocity = rb.linearVelocity.normalized * speedCap;
    }
}
