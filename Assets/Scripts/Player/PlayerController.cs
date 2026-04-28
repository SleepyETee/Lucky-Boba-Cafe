// ============================================================
// FILE: PlayerController.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Handles player movement, sprint, and interaction
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    // Movement state
    private Vector2 moveInput;
    private bool isSprinting;
    private bool canMove = true;
    
    // Interaction tracking
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();
    private IInteractable currentTarget;
    private IInteractable previousTarget;
    
    // Animation hashes for performance
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int InteractHash = Animator.StringToHash("Interact");
    
    void Update()
    {
        // Don't process input when paused
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return;
        
        if (!canMove) return;
        
        HandleMovementInput();
        HandleInteractionInput();
        UpdateAnimation();
    }
    
    void FixedUpdate()
    {
        if (!canMove || (GameManager.Instance != null && GameManager.Instance.IsPaused))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        ApplyMovement();
    }
    
    // ==================== INPUT HANDLING ====================
    
    void HandleMovementInput()
    {
        moveInput = GameInput.MoveInput;

        if (moveInput.magnitude > 1f)
            moveInput.Normalize();

        isSprinting = GameInput.SprintHeld;
    }
    
    void HandleInteractionInput()
    {
        if (GameInput.InteractPressed && currentTarget != null)
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetTrigger(InteractHash);

            currentTarget.Interact();
        }
        
        UpdateCurrentTarget();
    }
    
    // ==================== MOVEMENT ====================
    
    void ApplyMovement()
    {
        float speed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector2 newPosition = rb.position + moveInput * speed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }
    
    // ==================== ANIMATION ====================
    
    void UpdateAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled) return;
        
        if (moveInput.magnitude > 0.1f)
        {
            animator.SetFloat(MoveXHash, moveInput.x);
            animator.SetFloat(MoveYHash, moveInput.y);
        }
        animator.SetBool(IsMovingHash, moveInput.magnitude > 0.1f);
        
        // Flip sprite
        if (spriteRenderer != null && moveInput.x != 0)
            spriteRenderer.flipX = moveInput.x < 0;
    }
    
    // ==================== INTERACTION SYSTEM ====================
    
    void UpdateCurrentTarget()
    {
        // Clean up null references
        nearbyInteractables.RemoveAll(i => i == null || i.Equals(null));
        
        if (nearbyInteractables.Count == 0)
        {
            currentTarget = null;
            return;
        }
        
        // Find closest interactable
        float closestDist = float.MaxValue;
        IInteractable closest = null;
        
        foreach (var interactable in nearbyInteractables)
        {
            MonoBehaviour mb = interactable as MonoBehaviour;
            if (mb == null) continue;
            
            float dist = Vector2.Distance(transform.position, mb.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }
        
        previousTarget = currentTarget;
        currentTarget = closest;
        
        // Update UI Prompt if target has changed
        if (currentTarget != previousTarget)
        {
            if (HUDController.Instance != null)
            {
                if (currentTarget != null)
                {
                    HUDController.Instance.ShowPrompt(currentTarget.GetInteractionPrompt());
                }
                else
                {
                    HUDController.Instance.HidePrompt();
                }
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
            interactable.OnPlayerEnterRange();
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            nearbyInteractables.Remove(interactable);
            interactable.OnPlayerExitRange();
        }
    }
    
    // ==================== PUBLIC METHODS ====================
    
    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!canMove) rb.linearVelocity = Vector2.zero;
    }
    
    public void Freeze() => SetCanMove(false);
    public void Unfreeze() => SetCanMove(true);
    public IInteractable GetCurrentTarget() => currentTarget;
}
