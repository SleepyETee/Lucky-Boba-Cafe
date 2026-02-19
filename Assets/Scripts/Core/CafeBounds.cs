// ============================================================
// FILE: CafeBounds.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Creates invisible wall colliders around the cafe
//              to keep the player inside. Attach to an empty
//              GameObject and set the bounds in the Inspector.
// ============================================================
using UnityEngine;

public class CafeBounds : MonoBehaviour
{
    [Header("Cafe Area (in world units)")]
    [SerializeField] private float left = -9f;
    [SerializeField] private float right = 5f;
    [SerializeField] private float top = 5f;
    [SerializeField] private float bottom = -6f;
    
    [Header("Wall Thickness")]
    [SerializeField] private float thickness = 1f;
    
    void Awake()
    {
        CreateWall("Wall_Top",    
            new Vector2((left + right) / 2f, top + thickness / 2f),
            new Vector2(right - left + thickness * 2, thickness));
        
        CreateWall("Wall_Bottom", 
            new Vector2((left + right) / 2f, bottom - thickness / 2f),
            new Vector2(right - left + thickness * 2, thickness));
        
        CreateWall("Wall_Left",   
            new Vector2(left - thickness / 2f, (top + bottom) / 2f),
            new Vector2(thickness, top - bottom));
        
        CreateWall("Wall_Right",  
            new Vector2(right + thickness / 2f, (top + bottom) / 2f),
            new Vector2(thickness, top - bottom));
    }
    
    void CreateWall(string wallName, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.parent = transform;
        wall.transform.position = position;
        
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = false;
        
        // Add a static Rigidbody2D for reliable collision
        Rigidbody2D rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        
        Debug.Log($"[CafeBounds] Created {wallName} at {position}, size={size}");
    }
    
    // Show the bounds in the Scene view for easy editing
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        // Top
        Gizmos.DrawLine(new Vector3(left, top, 0), new Vector3(right, top, 0));
        // Bottom
        Gizmos.DrawLine(new Vector3(left, bottom, 0), new Vector3(right, bottom, 0));
        // Left
        Gizmos.DrawLine(new Vector3(left, top, 0), new Vector3(left, bottom, 0));
        // Right
        Gizmos.DrawLine(new Vector3(right, top, 0), new Vector3(right, bottom, 0));
    }
}
