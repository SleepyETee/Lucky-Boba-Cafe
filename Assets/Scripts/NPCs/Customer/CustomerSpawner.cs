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
    
    // Tracking
    private List<Customer> activeCustomers = new List<Customer>();
    private float spawnTimer;
    private float nextSpawnTime;
    
    void Start()
    {
        SetNextSpawnTime();
        
        // Normalize queue direction
        if (queueDirection.magnitude > 0)
            queueDirection = queueDirection.normalized;
        else
            queueDirection = Vector2.right;
    }
    
    void Update()
    {
        // Don't spawn if cafe is closed
        if (GameManager.Instance != null && !GameManager.Instance.CafeIsOpen)
            return;
        
        // Don't spawn if at max capacity
        if (activeCustomers.Count >= maxActiveCustomers)
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
        if (customerPrefab == null || spawnPoint == null) return;
        
        GameObject customerObj = Instantiate(
            customerPrefab, 
            spawnPoint.position, 
            Quaternion.identity
        );
        
        Customer customer = customerObj.GetComponent<Customer>();
        
        if (customer != null)
        {
            customer.Initialize(counterPoint, exitPoint);
            customer.OnCustomerLeft += HandleCustomerLeft;
            activeCustomers.Add(customer);
            
            // Assign queue position (end of line)
            UpdateAllQueuePositions();
            
            Debug.Log($"[CustomerSpawner] Spawned customer. Active: {activeCustomers.Count}");
        }
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
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            if (activeCustomers[i] == null) continue;
            
            Vector3 pos = counterPoint.position + 
                (Vector3)(queueDirection * queueSpacing * i);
            
            activeCustomers[i].SetQueuePosition(i, pos);
        }
    }
    
    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnDelay, maxSpawnDelay);
    }
    
    /// <summary>
    /// Gets the first customer waiting for their drink (front of queue)
    /// </summary>
    public Customer GetWaitingCustomer()
    {
        foreach (var customer in activeCustomers)
        {
            if (customer.CurrentState == Customer.CustomerState.WaitingForDrink)
            {
                return customer;
            }
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
