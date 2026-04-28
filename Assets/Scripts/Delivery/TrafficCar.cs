// ============================================================
// FILE: TrafficCar.cs
// DESCRIPTION: Moving traffic obstacle
// Drives along paths, player must avoid
// ============================================================
using UnityEngine;

public class TrafficCar : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float speedVariation = 1f;
    
    [Header("Path")]
    public Transform[] waypoints;
    public bool loop = true;
    public bool pingPong = false;
    
    [Header("Behavior")]
    public bool randomSpeed = true;
    public bool stopAtEnd = false;
    
    private int currentWaypointIndex = 0;
    private float actualSpeed;
    private int direction = 1; // 1 forward, -1 backward (for pingPong)
    
    void Start()
    {
        // Random speed variation
        if (randomSpeed)
            actualSpeed = speed + Random.Range(-speedVariation, speedVariation);
        else
            actualSpeed = speed;
        
        // Start at first waypoint, then move toward the second one
        if (waypoints.Length > 0 && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
            currentWaypointIndex = waypoints.Length > 1 ? 1 : 0;
        }
    }
    
    void Update()
    {
        if (waypoints.Length < 2) return;
        
        MoveTowardsWaypoint();
    }
    
    void MoveTowardsWaypoint()
    {
        Transform target = waypoints[currentWaypointIndex];
        if (target == null) { NextWaypoint(); return; }
        
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * actualSpeed * Time.deltaTime;
        
        // Rotate to face direction
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        // Check if reached
        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            NextWaypoint();
        }
    }
    
    void NextWaypoint()
    {
        if (pingPong)
        {
            currentWaypointIndex += direction;
            
            if (currentWaypointIndex >= waypoints.Length)
            {
                direction = -1;
                currentWaypointIndex = waypoints.Length - 2;
            }
            else if (currentWaypointIndex < 0)
            {
                direction = 1;
                currentWaypointIndex = 1;
            }
        }
        else
        {
            currentWaypointIndex++;
            
            if (currentWaypointIndex >= waypoints.Length)
            {
                if (loop)
                    currentWaypointIndex = 0;
                else if (stopAtEnd)
                    enabled = false;
                else
                    Destroy(gameObject);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] && waypoints[i + 1])
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
        
        if (loop && waypoints[0] && waypoints[waypoints.Length - 1])
            Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
    }
}
