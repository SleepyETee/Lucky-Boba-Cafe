// ============================================================
// FILE: DeliveryArrowUI.cs
// DESCRIPTION: UI arrow that points to delivery target
// Also shows distance
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeliveryArrowUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform arrowRect;
    public Image arrowImage;
    public TextMeshProUGUI distanceText;
    
    [Header("Target")]
    public Transform player;
    public Transform target;
    
    [Header("Settings")]
    public float smoothSpeed = 10f;
    public Color farColor = Color.white;
    public Color closeColor = Color.green;
    public float closeDistance = 10f;
    
    [Header("Pulse")]
    public bool pulseWhenClose = true;
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.2f;
    
    private Vector3 baseScale;
    private bool playerSearched;
    
    void Start()
    {
        if (arrowRect)
            baseScale = arrowRect.localScale;
    }
    
    void Update()
    {
        TryFindReferences();

        if (!player || !target)
            return;

        UpdateArrowRotation();
        UpdateDistance();
        UpdateColor();
        UpdatePulse();
    }
    
    void TryFindReferences()
    {
        if (!player && !playerSearched)
        {
            DeliveryScooter scooter = FindAnyObjectByType<DeliveryScooter>();
            if (scooter) player = scooter.transform;
            playerSearched = true;
        }

        if (DeliveryGameManager.Instance != null)
        {
            DeliveryPoint point = DeliveryGameManager.Instance.currentTarget;
            Transform managerTarget = point ? point.transform : null;
            if (target != managerTarget)
                target = managerTarget;
        }
    }
    
    void UpdateArrowRotation()
    {
        if (!arrowRect) return;
        
        Vector3 direction = target.position - player.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        
        Quaternion targetRot = Quaternion.Euler(0, 0, angle);
        arrowRect.rotation = Quaternion.Slerp(arrowRect.rotation, targetRot, smoothSpeed * Time.deltaTime);
    }
    
    void UpdateDistance()
    {
        if (!distanceText) return;
        
        float distance = Vector2.Distance(player.position, target.position);
        distanceText.text = $"{distance:F0}m";
    }
    
    void UpdateColor()
    {
        if (!arrowImage) return;
        
        float distance = Vector2.Distance(player.position, target.position);
        float t = Mathf.Clamp01(distance / closeDistance);
        arrowImage.color = Color.Lerp(closeColor, farColor, t);
    }
    
    void UpdatePulse()
    {
        if (!pulseWhenClose || !arrowRect) return;
        
        float distance = Vector2.Distance(player.position, target.position);
        
        if (distance < closeDistance)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            arrowRect.localScale = baseScale * pulse;
        }
        else
        {
            arrowRect.localScale = baseScale;
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
