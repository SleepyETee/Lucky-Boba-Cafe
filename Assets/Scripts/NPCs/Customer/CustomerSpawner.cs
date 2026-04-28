// ============================================================
// FILE: CustomerSpawner.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Spawns customers at random intervals with queue
// ============================================================
using UnityEngine;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject customerPrefab;
    
    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform exitPoint;
    
    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnDelay = 8f;
    [SerializeField] private float maxSpawnDelay = 20f;
    [SerializeField] private int maxActiveCustomers = 3;
    
    [Header("Queue Settings")]
    [SerializeField] private float queueSpacing = 1.5f;
    [SerializeField] private Vector2 queueDirection = Vector2.right;
    [SerializeField] private bool verboseQueueDebug = false;
    private Vector2 runtimeQueueDirection = Vector2.right;
    
    // Tracking
    private List<Customer> activeCustomers = new List<Customer>();
    private float spawnTimer;
    private float nextSpawnTime;

    // Difficulty overrides (set by day/progression systems)
    private float customerPatienceOverrideSeconds = -1f;
    private float customerMoveSpeedOverride = -1f;
    
    void Start()
    {
        SetNextSpawnTime();
        RefreshRuntimeQueueDirection();
    }
    
    void Update()
    {
        // Don't spawn if cafe is closed
        if (GameManager.Instance != null && !GameManager.Instance.CafeIsOpen)
            return;
        
        // Don't spawn if at max capacity (exclude leaving customers)
        int nonLeavingCount = 0;
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            if (activeCustomers[i] != null && activeCustomers[i].CurrentState != Customer.CustomerState.Leaving)
                nonLeavingCount++;
        }
        if (nonLeavingCount >= maxActiveCustomers)
            return;
        
        // Update timer
        spawnTimer += Time.deltaTime;
        
        if (spawnTimer >= nextSpawnTime)
        {
            SpawnCustomer();
            spawnTimer = 0f;
            SetNextSpawnTime();
        }
    }
    
    void SpawnCustomer()
    {
        if (customerPrefab == null || spawnPoint == null)
        {
            Debug.LogError("[CustomerSpawner] Missing customerPrefab or spawnPoint.");
            return;
        }
        
        GameObject customerObj = Instantiate(
            customerPrefab, 
            spawnPoint.position, 
            Quaternion.identity
        );
        
        Customer customer = customerObj.GetComponent<Customer>();
        if (customer == null)
        {
            Debug.LogError("[CustomerSpawner] Spawned prefab has no Customer component on root. Destroying spawned object.");
            Destroy(customerObj);
            return;
        }

        if (!customer.isActiveAndEnabled)
        {
            Debug.LogWarning("[CustomerSpawner] Spawned Customer component was disabled. Enabling at runtime.");
            customer.enabled = true;
        }

        customer.SetCustomerType(PickRandomType());

        float patienceBonus = 0f;
        if (GameManager.Instance != null)
        {
            int charmLevel = GameManager.Instance.GetUpgradeLevelByName("Cat Charm");
            patienceBonus = charmLevel * 5f;
        }

        if (customerPatienceOverrideSeconds > 0f)
            customer.SetMaxPatience(customerPatienceOverrideSeconds + patienceBonus);
        else if (patienceBonus > 0f)
            customer.SetMaxPatience(customer.GetBaseMaxPatience() + patienceBonus);

        if (customerMoveSpeedOverride > 0f)
            customer.SetMoveSpeed(customerMoveSpeedOverride);

        if (counterPoint == null)
            Debug.LogWarning("[CustomerSpawner] CounterPoint is not assigned. Queue uses fallback anchor.");
        if (exitPoint == null)
            Debug.LogWarning("[CustomerSpawner] ExitPoint is not assigned. Customers will despawn when leaving.");

        customer.Initialize(counterPoint, exitPoint);
        // Ensure first target is a valid queue anchor, even if scene points overlap.
        customer.SetQueuePosition(activeCustomers.Count, GetQueueAnchorPosition());
        customer.OnCustomerLeft += HandleCustomerLeft;
        activeCustomers.Add(customer);
        
        UpdateAllQueuePositions();
        
        Debug.Log($"[CustomerSpawner] Spawned {customer.Type} customer. Active: {activeCustomers.Count}" +
            $"\n  SpawnPoint: {(spawnPoint != null ? spawnPoint.position.ToString() : "NULL")}" +
            $"\n  CounterPoint: {(counterPoint != null ? counterPoint.position.ToString() : "NULL")}" +
            $"\n  ExitPoint: {(exitPoint != null ? exitPoint.position.ToString() : "NULL")}" +
            $"\n  QueueAnchor: {GetQueueAnchorPosition()}" +
            $"\n  QueueDirection(raw): {queueDirection}" +
            $"\n  QueueDirection(runtime): {runtimeQueueDirection}, Spacing: {queueSpacing}" +
            $"\n  Customer position: {customerObj.transform.position}");
    }

    Customer.CustomerType PickRandomType()
    {
        int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

        // Weights shift as days progress
        float regular = Mathf.Max(10f, 60f - day * 8f);
        float rusher = 10f + day * 4f;
        float foodie = 5f + day * 3f;
        float vip = Mathf.Max(0f, day * 2f - 2f);

        float total = regular + rusher + foodie + vip;
        float roll = Random.Range(0f, total);

        if (roll < regular) return Customer.CustomerType.Regular;
        roll -= regular;
        if (roll < rusher) return Customer.CustomerType.Rusher;
        roll -= rusher;
        if (roll < foodie) return Customer.CustomerType.Foodie;
        return Customer.CustomerType.VIP;
    }
    
    void HandleCustomerLeft(Customer customer, bool satisfied)
    {
        activeCustomers.Remove(customer);
        customer.OnCustomerLeft -= HandleCustomerLeft;
        
        // Shift remaining customers forward in the queue
        UpdateAllQueuePositions();
        
        Debug.Log($"[CustomerSpawner] Customer left. Satisfied: {satisfied}. Active: {activeCustomers.Count}");
    }
    
    /// <summary>
    /// Recalculate queue positions for all active customers.
    /// Customer 0 stands at the counter, each subsequent customer
    /// is offset by queueSpacing in the queueDirection.
    /// </summary>
    void UpdateAllQueuePositions()
    {
        RefreshRuntimeQueueDirection();
        activeCustomers.RemoveAll(c => c == null);

        float effectiveSpacing = Mathf.Max(0.1f, queueSpacing);
        Vector3 queueAnchor = GetQueueAnchorPosition();

        for (int i = 0; i < activeCustomers.Count; i++)
        {
            if (activeCustomers[i] == null) continue;
            effectiveSpacing = Mathf.Max(effectiveSpacing, activeCustomers[i].GetRecommendedQueueSpacing());
        }

        int queueIndex = 0;
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            if (activeCustomers[i] == null) continue;
            
            Vector3 pos = queueAnchor +
                (Vector3)(runtimeQueueDirection * effectiveSpacing * queueIndex);
            
            activeCustomers[i].SetQueuePosition(queueIndex, pos);

            if (verboseQueueDebug)
            {
                Debug.Log($"[CustomerSpawner] Queue slot {queueIndex} => {pos} " +
                    $"(anchor={queueAnchor}, dir={runtimeQueueDirection}, spacing={effectiveSpacing})");
            }

            queueIndex++;
        }
    }

    Vector3 GetQueueAnchorPosition()
    {
        RefreshRuntimeQueueDirection();

        if (counterPoint == null)
            return spawnPoint != null ? spawnPoint.position : transform.position;

        Vector3 anchor = counterPoint.position;

        // If points are accidentally overlapping in scene, create a usable queue anchor.
        if (spawnPoint != null && Vector2.Distance(spawnPoint.position, counterPoint.position) < 0.01f)
            anchor = spawnPoint.position + (Vector3)(runtimeQueueDirection * Mathf.Max(queueSpacing, 1.5f));

        return anchor;
    }

    void RefreshRuntimeQueueDirection()
    {
        // Keep inspector value untouched; use normalized runtime direction for movement math.
        runtimeQueueDirection = queueDirection.sqrMagnitude > 0.0001f
            ? queueDirection.normalized
            : Vector2.left;
    }
    
    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    // ==================== DAY/DIFFICULTY HELPERS ====================

    public void ConfigureDaySettings(
        float newMinSpawnDelay,
        float newMaxSpawnDelay,
        int newMaxActiveCustomers,
        float newCustomerPatienceSeconds,
        float newCustomerMoveSpeed = -1f)
    {
        minSpawnDelay = Mathf.Max(0.1f, newMinSpawnDelay);
        maxSpawnDelay = Mathf.Max(minSpawnDelay, newMaxSpawnDelay);
        maxActiveCustomers = Mathf.Max(1, newMaxActiveCustomers);
        if (GameManager.Instance != null)
            maxActiveCustomers += GameManager.Instance.GetUpgradeLevelByName("Extra Counter");

        customerPatienceOverrideSeconds = newCustomerPatienceSeconds > 0f ? newCustomerPatienceSeconds : -1f;
        customerMoveSpeedOverride = newCustomerMoveSpeed > 0f ? newCustomerMoveSpeed : -1f;

        spawnTimer = 0f;
        SetNextSpawnTime();
    }

    public void ResetForNewDay()
    {
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            Customer c = activeCustomers[i];
            if (c == null) continue;
            c.OnCustomerLeft -= HandleCustomerLeft;
            Destroy(c.gameObject);
        }

        activeCustomers.Clear();
        spawnTimer = 0f;
        SetNextSpawnTime();
    }
    
    /// <summary>
    /// Gets the first customer waiting for their drink (front of queue)
    /// </summary>
    public Customer GetWaitingCustomer()
    {
        foreach (var customer in activeCustomers)
        {
            if (customer == null) continue;
            if (customer.CurrentState == Customer.CustomerState.WaitingForDrink)
                return customer;
        }
        return null;
    }
    
    /// <summary>
    /// Check if any customer has placed an order and is waiting
    /// </summary>
    public bool HasWaitingCustomer()
    {
        return GetWaitingCustomer() != null;
    }
    
    /// <summary>
    /// Returns count of active customers
    /// </summary>
    public int GetActiveCustomerCount()
    {
        return activeCustomers.Count;
    }
}
