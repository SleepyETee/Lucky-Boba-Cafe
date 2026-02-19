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
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    
    [Header("Patience")]
    [SerializeField] private float maxPatience = 45f;
    [SerializeField] private float warningThreshold = 0.3f;
    
    [Header("Visuals — Auto-created if not assigned")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject orderBubble;
    [SerializeField] private TextMeshPro orderText;
    [SerializeField] private GameObject angryIndicator;
    [SerializeField] private SpriteRenderer patienceBarBg;
    [SerializeField] private SpriteRenderer patienceBarFill;
    [SerializeField] private TextMeshPro reactionText;
    
    // State
    public CustomerState CurrentState { get; private set; }
    public string CurrentOrder { get; private set; }
    public float PatienceRemaining { get; private set; }
    public float PatiencePercent => PatienceRemaining / maxPatience;
    public int QueueIndex { get; private set; }
    
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
    
    public void Initialize(Transform counter, Transform exit)
    {
        counterTarget = counter;
        exitTarget = exit;
        PatienceRemaining = maxPatience;
        
        // Generate random order
        string[] orders = { "Classic Milk Tea", "Taro Boba", "Brown Sugar", "Matcha Latte", "Green Tea" };
        CurrentOrder = orders[UnityEngine.Random.Range(0, orders.Length)];
        
        // Default queue position is the counter
        queuePosition = counter.position;
        
        // Auto-create any visuals not assigned in prefab
        EnsureVisuals();
        
        // Hide visuals at start
        ShowOrderBubble(false);
        ShowAngryIndicator(false);
        ShowPatienceBar(false);
        if (reactionText != null) reactionText.gameObject.SetActive(false);
        
        ChangeState(CustomerState.Entering);
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCustomerArrive();
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
            orderBubble.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            
            orderText = orderBubble.AddComponent<TextMeshPro>();
            orderText.fontSize = 3f;
            orderText.alignment = TextAlignmentOptions.Center;
            orderText.color = Color.white;
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
            bgObj.transform.localScale = new Vector3(1.2f, 0.15f, 1f);
            patienceBarBg = bgObj.AddComponent<SpriteRenderer>();
            patienceBarBg.sprite = CreatePixelSprite();
            patienceBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            patienceBarBg.sortingOrder = 8;
            
            // Fill
            GameObject fillObj = new GameObject("PatienceFill");
            fillObj.transform.SetParent(barParent.transform);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localScale = new Vector3(1.15f, 0.1f, 1f);
            patienceBarFill = fillObj.AddComponent<SpriteRenderer>();
            patienceBarFill.sprite = CreatePixelSprite();
            patienceBarFill.color = Color.green;
            patienceBarFill.sortingOrder = 9;
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
    }
    
    Sprite CreatePixelSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
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
                MoveToward(exitTarget.position);
                if (HasReached(exitTarget.position))
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
            case CustomerState.WaitingToOrder:
                ShowOrderBubble(true);
                ShowPatienceBar(false);
                ShowAngryIndicator(false);
                OnOrderReady?.Invoke(this);
                break;
                
            case CustomerState.WaitingForDrink:
                ShowOrderBubble(true);
                ShowPatienceBar(true);
                break;
                
            case CustomerState.Leaving:
                ShowOrderBubble(false);
                ShowAngryIndicator(false);
                ShowPatienceBar(false);
                if (reactionText != null) reactionText.gameObject.SetActive(false);
                break;
        }
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
        if (CurrentState != CustomerState.WaitingForDrink) return;
        
        float satisfaction = quality * (0.5f + PatiencePercent * 0.5f);
        int baseTip = 15;
        int tip = Mathf.RoundToInt(baseTip * satisfaction);
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(tip);
            GameManager.Instance.RecordCustomerServed();
            GameManager.Instance.RecordSatisfaction(satisfaction);
            
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCoin();
        }
        
        // Show reaction based on satisfaction
        ShowOrderBubble(false);
        ShowPatienceBar(false);
        ShowAngryIndicator(false);
        
        if (satisfaction >= 0.8f)
            ShowReaction("Purrfect!\n+$" + tip, Color.green);
        else if (satisfaction >= 0.5f)
            ShowReaction("Thanks!\n+$" + tip, Color.yellow);
        else
            ShowReaction("Meh...\n+$" + tip, new Color(1f, 0.5f, 0.3f));
        
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
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCustomerAngry();
        
        if (GameManager.Instance != null)
            GameManager.Instance.RecordSatisfaction(0f);
        
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
        if (show && orderText != null) orderText.text = CurrentOrder;
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
}
