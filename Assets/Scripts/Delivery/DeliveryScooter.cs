// ============================================================
// FILE: DeliveryScooter.cs
// DESCRIPTION: Player vehicle controller for delivery minigame
// Top-down driving with boost mechanic
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class DeliveryScooter : MonoBehaviour
{
    // ==================== MOVEMENT ====================
    [Header("Movement")]
    public float maxSpeed = 12f;
    public float acceleration = 20f;
    public float brakeForce = 25f;
    public float turnSpeed = 180f;
    public float driftFactor = 0.1f; // 1 = no drift, 0 = full drift
    
    // ==================== BOOST ====================
    [Header("Boost")]
    public float boostMultiplier = 1.6f;
    public float boostDuration = 2f;
    public float boostCooldown = 4f;
    private float boostTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isBoosting = false;
    
    // ==================== CRASH ====================
    [Header("Crash")]
    public float crashSlowdown = 0.2f;
    public float invulnerabilityTime = 1.5f;
    private bool isInvulnerable = false;
    
    // ==================== REFERENCES ====================
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public ParticleSystem boostParticles;
    public ParticleSystem smokeParticles;
    public TrailRenderer leftTrail;
    public TrailRenderer rightTrail;
    
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite boostSprite;
    
    [Header("Audio")]
    public AudioSource engineSource;
    public AudioClip boostSound;
    
    // State
    private Rigidbody2D rb;
    private float currentSpeed = 0f;
    private float inputH, inputV;
    private bool controlsEnabled = true;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    void Start()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        SetTrails(false);
    }
    
    void Update()
    {
        if (!controlsEnabled) return;
        
        Vector2 move = GameInput.MoveInput;
        inputH = move.x;
        inputV = move.y;

        if (GameInput.ConfirmPressed && CanBoost())
            StartBoost();
        
        UpdateBoost();
        UpdateEngineSound();
    }
    
    void FixedUpdate()
    {
        if (!controlsEnabled)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        // Speed calculation
        float targetSpeed = inputV * maxSpeed;
        if (isBoosting) targetSpeed *= boostMultiplier;
        
        // Acceleration
        if (Mathf.Abs(inputV) > 0.1f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeForce * Time.fixedDeltaTime);
        }
        
        // Forward velocity
        Vector2 forwardVel = transform.up * currentSpeed;
        
        // Kill sideways velocity (drift)
        Vector2 rightVel = Vector2.Dot(rb.linearVelocity, transform.right) * (Vector2)transform.right;
        rb.linearVelocity = forwardVel + rightVel * driftFactor;
        
        // Turning — allow slow rotation even while nearly stopped
        if (Mathf.Abs(currentSpeed) > 0.1f || Mathf.Abs(inputH) > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / 3f);
            float turn = -inputH * turnSpeed * Time.fixedDeltaTime * Mathf.Max(0.25f, speedFactor);

            float speedRatio = Mathf.Abs(currentSpeed) / (maxSpeed * boostMultiplier);
            turn *= (1f - speedRatio * 0.4f);

            if (currentSpeed < 0) turn *= -1;

            rb.MoveRotation(rb.rotation + turn);
        }
        
        // Smoke particles
        if (smokeParticles)
        {
            var emission = smokeParticles.emission;
            emission.rateOverTime = Mathf.Abs(currentSpeed) * 3f;
        }
    }
    
    // ==================== BOOST ====================
    
    void StartBoost()
    {
        isBoosting = true;
        boostTimer = boostDuration;
        
        if (boostParticles) boostParticles.Play();
        if (spriteRenderer && boostSprite) spriteRenderer.sprite = boostSprite;
        SetTrails(true);
        
        if (AudioManager.Instance != null && boostSound != null)
            AudioManager.Instance.PlaySFX(boostSound);
        
        Debug.Log("[Scooter] BOOST!");
    }
    
    void UpdateBoost()
    {
        if (isBoosting)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0)
                EndBoost();
        }
        
        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;
    }
    
    void EndBoost()
    {
        isBoosting = false;
        cooldownTimer = boostCooldown;
        
        if (boostParticles) boostParticles.Stop();
        if (spriteRenderer && normalSprite) spriteRenderer.sprite = normalSprite;
        SetTrails(false);
    }
    
    public bool CanBoost() => !isBoosting && cooldownTimer <= 0;
    
    public float GetBoostPercent()
    {
        if (isBoosting) return boostDuration > 0f ? boostTimer / boostDuration : 0f;
        return boostCooldown > 0f ? 1f - (cooldownTimer / boostCooldown) : 1f;
    }
    
    void SetTrails(bool on)
    {
        if (leftTrail) leftTrail.emitting = on;
        if (rightTrail) rightTrail.emitting = on;
    }
    
    // ==================== COLLISION ====================
    
    void OnCollisionEnter2D(Collision2D col)
    {
        if (isInvulnerable) return;
        
        if (col.gameObject.CompareTag("Obstacle") || col.gameObject.CompareTag("TrafficCar"))
        {
            Crash();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Use components (not tags) so typos / missing Tag Manager entries cannot break triggers.
        DeliveryPoint point = other.GetComponent<DeliveryPoint>() ?? other.GetComponentInParent<DeliveryPoint>();
        if (point != null && point.IsActive() && DeliveryGameManager.Instance != null)
            DeliveryGameManager.Instance.OnDeliveryComplete(point);

        TipCollectible tip = other.GetComponent<TipCollectible>() ?? other.GetComponentInParent<TipCollectible>();
        if (tip != null && DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.OnTipCollected(tip.value);
            tip.Collect();
        }
    }
    
    void Crash()
    {
        rb.linearVelocity *= crashSlowdown;
        currentSpeed *= crashSlowdown;
        
        if (isBoosting) EndBoost();
        
        if (DeliveryGameManager.Instance != null)
            DeliveryGameManager.Instance.OnPlayerCrash();
        
        StartCoroutine(InvulnerabilityFlash());
    }
    
    IEnumerator InvulnerabilityFlash()
    {
        isInvulnerable = true;
        float timer = invulnerabilityTime;
        
        while (timer > 0)
        {
            if (spriteRenderer)
                spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSecondsRealtime(0.1f);
            timer -= 0.1f;
        }
        
        if (spriteRenderer) spriteRenderer.enabled = true;
        isInvulnerable = false;
    }
    
    // ==================== AUDIO ====================
    
    void UpdateEngineSound()
    {
        if (!engineSource) return;
        
        float speedRatio = Mathf.Abs(currentSpeed) / (maxSpeed * boostMultiplier);
        engineSource.pitch = 0.8f + speedRatio * 0.6f;
        engineSource.volume = 0.3f + speedRatio * 0.4f;
    }
    
    // ==================== CONTROL ====================
    
    public void EnableControls(bool on)
    {
        controlsEnabled = on;
        
        if (!on)
        {
            currentSpeed = 0;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
            if (isBoosting) EndBoost();
            if (engineSource) engineSource.Stop();
        }
        else
        {
            if (engineSource) engineSource.Play();
        }
    }
    
    public void ResetToPosition(Vector3 pos)
    {
        transform.position = pos;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        currentSpeed = 0;
    }
    
    public float GetSpeed() => currentSpeed;
    public bool IsBoosting() => isBoosting;
}
