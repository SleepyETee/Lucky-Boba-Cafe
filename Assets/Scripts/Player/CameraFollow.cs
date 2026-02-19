// ============================================================
// FILE: CameraFollow.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Smooth camera following for 2D player
// ============================================================
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Boundaries (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 10f;
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Calculate target position
        Vector3 desiredPosition = target.position + offset;
        
        // Apply boundaries
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }
        
        // Smooth follow
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        
        transform.position = smoothedPosition;
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SnapToTarget()
    {
        if (target != null)
            transform.position = target.position + offset;
    }
}
