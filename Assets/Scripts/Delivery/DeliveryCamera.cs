// ============================================================
// FILE: DeliveryCamera.cs
// DESCRIPTION: Smooth camera follow for delivery minigame
// ============================================================
using UnityEngine;

public class DeliveryCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Follow")]
    public float smoothSpeed = 8f;
    public float lookAheadAmount = 2f;
    public float lookAheadSpeed = 3f;
    
    [Header("Zoom")]
    public Camera cam;
    public float normalZoom = 8f;
    public float boostZoom = 10f;
    public float zoomSpeed = 3f;
    
    [Header("Bounds (Optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    
    private Vector3 currentLookAhead;
    private Vector3 velocity;
    private DeliveryScooter scooter;
    
    void Start()
    {
        if (!cam)
            cam = GetComponent<Camera>();
        
        if (target)
            scooter = target.GetComponent<DeliveryScooter>();
        
        // Snap to target initially
        if (target)
            transform.position = target.position + offset;
    }
    
    void LateUpdate()
    {
        if (!target) return;
        
        // Calculate look ahead based on velocity
        Vector3 targetLookAhead = Vector3.zero;
        if (scooter)
        {
            targetLookAhead = target.up * scooter.GetSpeed() / 12f * lookAheadAmount;
        }
        
        currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.deltaTime);
        
        // Target position
        Vector3 desiredPos = target.position + offset + currentLookAhead;
        
        // Apply bounds
        if (useBounds)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, minBounds.x, maxBounds.x);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minBounds.y, maxBounds.y);
        }
        
        float smoothTime = smoothSpeed > 0.01f ? 1f / smoothSpeed : 0.1f;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);
        
        // Zoom based on boost
        if (cam && scooter)
        {
            float targetZoom = scooter.IsBoosting() ? boostZoom : normalZoom;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        scooter = target != null ? target.GetComponent<DeliveryScooter>() : null;
    }
    
    public void SnapToTarget()
    {
        if (target)
            transform.position = target.position + offset;
    }
}
