// ============================================================
// FILE: DeliveryPoint.cs
// DESCRIPTION: Delivery destination marker
// Player drives here to complete delivery
// ============================================================
using UnityEngine;
using TMPro;

public class DeliveryPoint : MonoBehaviour
{
    [Header("Info")]
    public string locationName = "House";
    public int bonusTip = 0;
    
    [Header("State")]
    [SerializeField] private bool isActive = false;
    
    [Header("Visuals")]
    public GameObject markerObject;
    public SpriteRenderer markerSprite;
    public GameObject glowEffect;
    public GameObject bounceArrow;
    public TextMeshPro nameLabel;
    
    [Header("Animation")]
    public float pulseSpeed = 3f;
    public float pulseScale = 0.15f;
    public float arrowBounce = 0.3f;
    
    [Header("Colors")]
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.gray;
    
    private Vector3 originalScale;
    private Vector3 arrowStartPos;
    private Collider2D cachedCollider;
    
    void Start()
    {
        cachedCollider = GetComponent<Collider2D>();
        if (markerObject)
            originalScale = markerObject.transform.localScale;
        
        if (bounceArrow)
            arrowStartPos = bounceArrow.transform.localPosition;
        
        SetActive(false);
    }
    
    void Update()
    {
        if (!isActive) return;
        
        // Pulse animation
        if (markerObject)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            markerObject.transform.localScale = originalScale * pulse;
        }
        
        // Arrow bounce
        if (bounceArrow)
        {
            float bounce = Mathf.Sin(Time.time * pulseSpeed * 2f) * arrowBounce;
            bounceArrow.transform.localPosition = arrowStartPos + Vector3.up * bounce;
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (markerObject) markerObject.SetActive(active);
        if (glowEffect) glowEffect.SetActive(active);
        if (bounceArrow) bounceArrow.SetActive(active);
        
        if (markerSprite)
            markerSprite.color = active ? activeColor : inactiveColor;
        
        if (nameLabel)
        {
            nameLabel.gameObject.SetActive(active);
            nameLabel.text = locationName;
        }
        
        if (cachedCollider) cachedCollider.enabled = active;
    }
    
    public bool IsActive() => isActive;
    
    void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
