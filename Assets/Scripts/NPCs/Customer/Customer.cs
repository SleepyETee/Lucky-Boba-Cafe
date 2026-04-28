// ============================================================
// FILE: Customer.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Customer AI with state machine, visual feedback
//              (patience bar, order bubble, reactions).
//              Visuals can be assigned in prefab or auto-created.
// ============================================================
using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class Customer : MonoBehaviour
{
    // Customer state machine
    public enum CustomerState
    {
        Entering,
        WaitingToOrder,
        WaitingForDrink,
        Receiving,
        Leaving
    }

    public enum CustomerType { Regular, Rusher, Foodie, VIP }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    
    [Header("Patience")]
    [SerializeField] private float maxPatience = 45f;
    [SerializeField] private float warningThreshold = 0.3f;

    [Header("Type")]
    [SerializeField] private CustomerType customerType = CustomerType.Regular;
    private float tipMultiplier = 1f;
    private float patienceMultiplier = 1f;
    private float speedMultiplier = 1f;
    
    [Header("Animation (assign in prefab if available)")]
    [SerializeField] private Animator animator;

    [Header("Visuals — Auto-created if not assigned")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject orderBubble;
    [SerializeField] private TextMeshPro orderText;
    [SerializeField] private GameObject angryIndicator;
    [SerializeField] private SpriteRenderer patienceBarBg;
    [SerializeField] private SpriteRenderer patienceBarFill;
    [SerializeField] private TextMeshPro reactionText;
    
    [Header("Visual Sorting")]
    [SerializeField] private int overlaySortingOffset = 60;
    
    // State
    public CustomerState CurrentState { get; private set; }
    public CustomerType Type => customerType;
    public string CurrentOrder { get; private set; }
    public bool CurrentOrderNeedsTopping { get; private set; }
    public float PatienceRemaining { get; private set; }
    public float PatiencePercent => PatienceRemaining / maxPatience;
    public int QueueIndex { get; private set; }
    
    // Animation hashes
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int HappyHash = Animator.StringToHash("Happy");
    private static readonly int AngryHash = Animator.StringToHash("Angry");

    // Original fill scale (for patience bar resizing)
    private float fillOriginalScaleX = 1f;
    
    // Targets
    private Transform counterTarget;
    private Transform exitTarget;
    private Vector3 queuePosition;
    
    // Events
    public event Action<Customer> OnOrderReady;
    public event Action<Customer, bool> OnCustomerLeft;
    
    // ==================== INITIALIZATION ====================

    public void SetMaxPatience(float seconds)
    {
        maxPatience = Mathf.Max(1f, seconds);
        if (PatienceRemaining > maxPatience)
            PatienceRemaining = maxPatience;
    }

    /// <summary> Current max patience (after type modifiers / SetMaxPatience ). </summary>
    public float GetBaseMaxPatience() => maxPatience;

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetCustomerType(CustomerType type)
    {
        customerType = type;

        // Store multipliers — they are applied in Initialize() after all
        // base values (overrides, upgrade bonuses, etc.) have been set.
        switch (type)
        {
            case CustomerType.Regular:
                patienceMultiplier = 1f;
                speedMultiplier = 1f;
                tipMultiplier = 1f;
                break;
            case CustomerType.Rusher:
                patienceMultiplier = 0.6f;
                speedMultiplier = 1.3f;
                tipMultiplier = 1.2f;
                break;
            case CustomerType.Foodie:
                patienceMultiplier = 1.3f;
                speedMultiplier = 1f;
                tipMultiplier = 1.5f;
                break;
            case CustomerType.VIP:
                patienceMultiplier = 0.5f;
                speedMultiplier = 1f;
                tipMultiplier = 3f;
                break;
        }
    }
    
    private bool initialized;

    public void Initialize(Transform counter, Transform exit)
    {
        counterTarget = counter;
        exitTarget = exit;

        if (!initialized)
        {
            maxPatience *= patienceMultiplier;
            moveSpeed *= speedMultiplier;
            initialized = true;
        }

        PatienceRemaining = maxPatience;
        
        // Generate random order
        string[] orders = { "Classic Milk Tea", "Taro Boba", "Brown Sugar", "Matcha Latte", "Green Tea" };
        CurrentOrder = orders[UnityEngine.Random.Range(0, orders.Length)];
        CurrentOrderNeedsTopping = DetermineIfOrderNeedsTopping(CurrentOrder);
        
        // Default queue position is the counter (with fallback if not assigned).
        if (counter != null)
            queuePosition = counter.position;
        else
        {
            queuePosition = transform.position + Vector3.right;
            Debug.LogWarning("[Customer] CounterPoint is not assigned. Using fallback queue target.");
        }
        
        // Auto-create any visuals not assigned in prefab
        EnsureVisuals();
        
        // Hide visuals at start
        ShowOrderBubble(false);
        ShowAngryIndicator(false);
        ShowPatienceBar(false);
        if (reactionText != null) reactionText.gameObject.SetActive(false);
        
        ApplyTypeTint();
        ChangeState(CustomerState.Entering);
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCustomerArrive();
    }

    void ApplyTypeTint()
    {
        if (spriteRenderer == null) return;
        switch (customerType)
        {
            case CustomerType.Regular: break;
            case CustomerType.Rusher:  spriteRenderer.color = new Color(1f, 0.7f, 0.7f); break;
            case CustomerType.Foodie:  spriteRenderer.color = new Color(0.7f, 1f, 0.7f); break;
            case CustomerType.VIP:     spriteRenderer.color = new Color(1f, 0.85f, 0.4f); break;
        }
    }
    
    // ==================== AUTO-CREATE VISUALS ====================
    
    void EnsureVisuals()
    {
        // Get sprite renderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Order bubble + text
        if (orderBubble == null)
        {
            orderBubble = new GameObject("OrderBubble");
            orderBubble.transform.SetParent(transform);
            orderBubble.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            
            orderText = orderBubble.AddComponent<TextMeshPro>();
            orderText.fontSize = 5f;
            orderText.alignment = TextAlignmentOptions.Center;
            orderText.color = Color.black;
            orderText.sortingOrder = 10;
            
            RectTransform rect = orderBubble.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4f, 1f);
        }
        else if (orderText == null)
        {
            orderText = orderBubble.GetComponentInChildren<TextMeshPro>(true);
        }
        
        // Patience bar (bg + fill)
        if (patienceBarBg == null)
        {
            // Parent object
            GameObject barParent = new GameObject("PatienceBar");
            barParent.transform.SetParent(transform);
            barParent.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            
            // Background
            GameObject bgObj = new GameObject("PatienceBG");
            bgObj.transform.SetParent(barParent.transform);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localScale = new Vector3(2.5f, 0.3f, 1f);
            patienceBarBg = bgObj.AddComponent<SpriteRenderer>();
            patienceBarBg.sprite = GetSharedPixelSprite();
            patienceBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            patienceBarBg.sortingOrder = 50;
            
            // Fill
            GameObject fillObj = new GameObject("PatienceFill");
            fillObj.transform.SetParent(barParent.transform);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localScale = new Vector3(2.4f, 0.22f, 1f);
            patienceBarFill = fillObj.AddComponent<SpriteRenderer>();
            patienceBarFill.sprite = GetSharedPixelSprite();
            patienceBarFill.color = Color.green;
            patienceBarFill.sortingOrder = 51;
        }
        
        // Store original fill scale
        if (patienceBarFill != null)
            fillOriginalScaleX = patienceBarFill.transform.localScale.x;
        
        // Angry indicator
        if (angryIndicator == null)
        {
            angryIndicator = new GameObject("AngryIndicator");
            angryIndicator.transform.SetParent(transform);
            angryIndicator.transform.localPosition = new Vector3(0.7f, 1.5f, 0f);
            
            TextMeshPro angryText = angryIndicator.AddComponent<TextMeshPro>();
            angryText.text = "!!";
            angryText.fontSize = 5f;
            angryText.alignment = TextAlignmentOptions.Center;
            angryText.color = Color.red;
            angryText.sortingOrder = 11;
            
            RectTransform rect = angryIndicator.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2f, 1f);
        }
        
        // Reaction text
        if (reactionText == null)
        {
            GameObject reactionObj = new GameObject("ReactionText");
            reactionObj.transform.SetParent(transform);
            reactionObj.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            
            reactionText = reactionObj.AddComponent<TextMeshPro>();
            reactionText.fontSize = 4f;
            reactionText.alignment = TextAlignmentOptions.Center;
            reactionText.sortingOrder = 12;
            
            RectTransform rect = reactionObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4f, 1.5f);
        }

        // Keep visual elements separated even when sprite size/scale changes.
        NormalizeVisualLayout();
        ApplyVisualSorting();
    }
    
    // Shared 1x1 white pixel sprite — created once, reused by all customers
    private static Sprite s_sharedPixelSprite;

    static Sprite GetSharedPixelSprite()
    {
        if (s_sharedPixelSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            s_sharedPixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            s_sharedPixelSprite.hideFlags = HideFlags.HideAndDontSave;
        }
        return s_sharedPixelSprite;
    }
    
    // ==================== QUEUE SYSTEM ====================
    
    public void SetQueuePosition(int index, Vector3 position)
    {
        QueueIndex = index;
        queuePosition = position;
    }
    
    // ==================== UPDATE ====================
    
    void Update()
    {
        switch (CurrentState)
        {
            case CustomerState.Entering:
                MoveToward(queuePosition);
                if (HasReached(queuePosition))
                    ChangeState(CustomerState.WaitingToOrder);
                break;
                
            case CustomerState.WaitingToOrder:
                MoveToward(queuePosition);
                if (HasReached(queuePosition))
                    TakeOrder();
                break;
                
            case CustomerState.WaitingForDrink:
                MoveToward(queuePosition);
                UpdatePatience();
                UpdatePatienceBar();
                break;
                
            case CustomerState.Leaving:
                Vector3 leaveTarget;
                if (exitTarget != null)
                    leaveTarget = exitTarget.position;
                else
                    leaveTarget = transform.position + Vector3.left * 20f; // walk offscreen
                
                MoveToward(leaveTarget);
                if (HasReached(leaveTarget))
                    Destroy(gameObject);
                break;
        }
    }
    
    // ==================== STATE MANAGEMENT ====================
    
    void ChangeState(CustomerState newState)
    {
        CurrentState = newState;
        
        switch (newState)
        {
            case CustomerState.Entering:
                TrySetAnimTrigger(WalkHash);
                break;

            case CustomerState.WaitingToOrder:
                TrySetAnimTrigger(IdleHash);
                ShowOrderBubble(true);
                ShowPatienceBar(false);
                ShowAngryIndicator(false);
                OnOrderReady?.Invoke(this);
                break;
                
            case CustomerState.WaitingForDrink:
                TrySetAnimTrigger(IdleHash);
                ShowOrderBubble(true);
                ShowPatienceBar(true);
                break;
                
            case CustomerState.Leaving:
                TrySetAnimTrigger(WalkHash);
                ShowOrderBubble(false);
                ShowAngryIndicator(false);
                ShowPatienceBar(false);
                if (reactionText != null) reactionText.gameObject.SetActive(false);
                break;
        }
    }

    void TrySetAnimTrigger(int hash)
    {
        if (animator != null && animator.isActiveAndEnabled)
            animator.SetTrigger(hash);
    }
    
    // ==================== MOVEMENT ====================
    
    void MoveToward(Vector2 target)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );
        
        if (spriteRenderer != null)
        {
            Vector2 dir = target - (Vector2)transform.position;
            if (Mathf.Abs(dir.x) > 0.01f)
                spriteRenderer.flipX = dir.x < 0;
        }
    }
    
    bool HasReached(Vector2 target)
    {
        return Vector2.Distance(transform.position, target) < 0.1f;
    }
    
    // ==================== PATIENCE ====================
    
    void UpdatePatience()
    {
        PatienceRemaining -= Time.deltaTime;
        
        if (PatienceRemaining <= 0)
            LeaveDissatisfied();
    }
    
    void UpdatePatienceBar()
    {
        if (patienceBarFill == null) return;
        
        float pct = PatiencePercent;
        
        // Scale fill bar width based on patience
        Vector3 scale = patienceBarFill.transform.localScale;
        scale.x = fillOriginalScaleX * Mathf.Clamp01(pct);
        patienceBarFill.transform.localScale = scale;
        
        // Color gradient: green → yellow → red
        if (pct > 0.5f)
            patienceBarFill.color = Color.Lerp(Color.yellow, Color.green, (pct - 0.5f) * 2f);
        else
            patienceBarFill.color = Color.Lerp(Color.red, Color.yellow, pct * 2f);
        
        // Show angry indicator when patience is low
        if (pct <= warningThreshold)
            ShowAngryIndicator(true);
    }
    
    // ==================== SERVICE ====================
    
    public void TakeOrder()
    {
        if (CurrentState == CustomerState.WaitingToOrder)
            ChangeState(CustomerState.WaitingForDrink);
    }
    
    public void ServeDrink(float quality)
    {
        if (CurrentState != CustomerState.WaitingForDrink)
        {
            Debug.LogWarning($"[Customer] ServeDrink called but state is {CurrentState}, not WaitingForDrink — ignoring.");
            return;
        }
        ChangeState(CustomerState.Receiving);
        
        float satisfaction = quality * (0.5f + PatiencePercent * 0.5f);
        int baseTip = 15;

        float upgradeBonus = 1f;
        if (GameManager.Instance != null)
        {
            int tipJarLevel = GameManager.Instance.GetUpgradeLevelByName("Tip Jar");
            upgradeBonus += tipJarLevel * 0.15f;
        }

        int tip = Mathf.Max(1, Mathf.RoundToInt(baseTip * satisfaction * tipMultiplier * upgradeBonus));

        Debug.Log($"[Customer] ServeDrink: quality={quality:F2}, satisfaction={satisfaction:F2}, tip=${tip}, GM exists={GameManager.Instance != null}");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(tip, isTip: true);
            GameManager.Instance.RecordCustomerServed();
            GameManager.Instance.RecordSatisfaction(satisfaction);
            
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCoin();
        }
        else
        {
            Debug.LogError("[Customer] GameManager.Instance is NULL — tips and stats were NOT recorded!");
        }

        // Update reputation based on service quality
        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.RecordService(satisfaction);
        
        // Show reaction based on satisfaction
        ShowOrderBubble(false);
        ShowPatienceBar(false);
        ShowAngryIndicator(false);
        
        if (satisfaction >= 0.8f)
        {
            TrySetAnimTrigger(HappyHash);
            ShowReaction("Purrfect!\n+$" + tip, Color.green);
        }
        else if (satisfaction >= 0.5f)
        {
            TrySetAnimTrigger(HappyHash);
            ShowReaction("Thanks!\n+$" + tip, Color.yellow);
        }
        else
        {
            TrySetAnimTrigger(AngryHash);
            ShowReaction("Meh...\n+$" + tip, new Color(1f, 0.5f, 0.3f));
        }
        
        OnCustomerLeft?.Invoke(this, satisfaction >= 0.5f);
        StartCoroutine(LeaveAfterDelay(1.5f));
    }
    
    IEnumerator LeaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangeState(CustomerState.Leaving);
    }
    
    void LeaveDissatisfied()
    {
        if (CurrentState == CustomerState.Leaving || CurrentState == CustomerState.Receiving) return;
        CurrentState = CustomerState.Receiving;

        TrySetAnimTrigger(AngryHash);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCustomerAngry();
        
        if (GameManager.Instance != null)
            GameManager.Instance.RecordSatisfaction(0f);

        // Angry customer hurts reputation
        if (ReputationSystem.Instance != null)
            ReputationSystem.Instance.RecordAngryCustomer();
        
        ShowOrderBubble(false);
        ShowPatienceBar(false);
        ShowReaction("Hmph!", Color.red);
        
        OnCustomerLeft?.Invoke(this, false);
        StartCoroutine(LeaveAfterDelay(1f));
    }
    
    // ==================== VISUALS ====================
    
    void ShowOrderBubble(bool show)
    {
        if (orderBubble != null) orderBubble.SetActive(show);
        if (show && orderText != null)
        {
            string typeTag = customerType != CustomerType.Regular ? $"{customerType} - " : "";
            string toppingTag = CurrentOrderNeedsTopping ? " + Topping" : " (No Topping)";
            orderText.text = $"{typeTag}{CurrentOrder}{toppingTag}";
        }
    }

    bool DetermineIfOrderNeedsTopping(string drinkName)
    {
        switch (drinkName)
        {
            case "Taro Boba":
            case "Brown Sugar":
                return true;
            default:
                return false;
        }
    }
    
    void ShowAngryIndicator(bool show)
    {
        if (angryIndicator != null) angryIndicator.SetActive(show);
    }
    
    void ShowPatienceBar(bool show)
    {
        if (patienceBarBg != null) patienceBarBg.gameObject.SetActive(show);
        if (patienceBarFill != null) patienceBarFill.gameObject.SetActive(show);
        // Also show/hide the parent if it exists
        if (patienceBarBg != null && patienceBarBg.transform.parent != transform)
            patienceBarBg.transform.parent.gameObject.SetActive(show);
    }
    
    void ShowReaction(string text, Color color)
    {
        if (reactionText == null) return;
        reactionText.gameObject.SetActive(true);
        reactionText.text = text;
        reactionText.color = color;
    }

    void NormalizeVisualLayout()
    {
        float spriteHalfHeight = 0.7f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            spriteHalfHeight = Mathf.Max(0.5f, spriteRenderer.sprite.bounds.extents.y);

        if (orderBubble != null)
            orderBubble.transform.localPosition = new Vector3(0f, spriteHalfHeight + 0.95f, 0f);

        if (patienceBarBg != null)
        {
            Transform barParent = patienceBarBg.transform.parent;
            if (barParent != null)
                barParent.localPosition = new Vector3(0f, spriteHalfHeight + 0.55f, 0f);
        }

        if (angryIndicator != null)
            angryIndicator.transform.localPosition = new Vector3(0.7f, spriteHalfHeight + 0.75f, 0f);

        if (reactionText != null)
            reactionText.transform.localPosition = new Vector3(0f, spriteHalfHeight + 1.2f, 0f);
    }

    void ApplyVisualSorting()
    {
        int baseOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
        int overlayOrder = baseOrder + Mathf.Max(10, overlaySortingOffset);

        if (patienceBarBg != null)
            patienceBarBg.sortingOrder = overlayOrder;
        if (patienceBarFill != null)
            patienceBarFill.sortingOrder = overlayOrder + 1;

        SetTextRenderOrder(orderText, overlayOrder + 2);
        SetTextRenderOrder(reactionText, overlayOrder + 3);

        if (angryIndicator != null)
        {
            TextMeshPro angryText = angryIndicator.GetComponentInChildren<TextMeshPro>(true);
            SetTextRenderOrder(angryText, overlayOrder + 4);
        }
    }

    void SetTextRenderOrder(TextMeshPro text, int order)
    {
        if (text == null) return;

        text.sortingOrder = order;
        if (spriteRenderer != null)
            text.sortingLayerID = spriteRenderer.sortingLayerID;

        Renderer r = text.GetComponent<Renderer>();
        if (r == null) return;

        r.sortingOrder = order;
        if (spriteRenderer != null)
            r.sortingLayerID = spriteRenderer.sortingLayerID;
    }

    public float GetRecommendedQueueSpacing()
    {
        if (spriteRenderer != null && spriteRenderer.bounds.size.x > 0f)
            return Mathf.Max(1f, spriteRenderer.bounds.size.x * 1.25f);

        return 1.5f;
    }

    void OnDestroy()
    {
        OnOrderReady = null;
        OnCustomerLeft = null;
    }
}
