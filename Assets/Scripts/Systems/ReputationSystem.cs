// ============================================================
// FILE: ReputationSystem.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Tracks cafe reputation (1-5 stars) based on
//              customer satisfaction. Gates content unlocks:
//              ⭐ Basic menu, one supplier
//              ⭐⭐ New tea types, first employee slot
//              ⭐⭐⭐ Catering orders, delivery bike
//              ⭐⭐⭐⭐ Matcha ceremony (Elder Cat quest)
//              ⭐⭐⭐⭐⭐ B&B expansion, legendary recipes
// ============================================================
using UnityEngine;
using System;

public class ReputationSystem : MonoBehaviour
{
    public static ReputationSystem Instance { get; private set; }

    [Header("Reputation")]
    [SerializeField] private float reputationPoints = 0f;
    [SerializeField] private float[] starThresholds = { 0f, 50f, 150f, 350f, 700f };

    [Header("Gain / Loss Rates")]
    [SerializeField] private float satisfiedGain = 5f;
    [SerializeField] private float perfectGain = 12f;
    [SerializeField] private float dissatisfiedLoss = 8f;
    [SerializeField] private float angryLeftLoss = 15f;

    /// <summary>Current star rating (1-5).</summary>
    public int CurrentStars { get; private set; } = 1;
    public float ReputationPoints => reputationPoints;
    public float PointsToNextStar
    {
        get
        {
            if (CurrentStars >= 5) return 0f;
            return starThresholds[CurrentStars] - reputationPoints;
        }
    }

    /// <summary>Fired when the star rating changes. Passes (oldStars, newStars).</summary>
    public event Action<int, int> OnStarChanged;
    /// <summary>Fired whenever reputation points change.</summary>
    public event Action<float> OnReputationChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        RecalculateStars();
    }

    // ==================== PUBLIC API ====================

    /// <summary>
    /// Call after serving a drink. Quality 0-1.
    /// </summary>
    public void RecordService(float quality)
    {
        if (quality >= 0.9f)
            AddReputation(perfectGain);
        else if (quality >= 0.5f)
            AddReputation(satisfiedGain * quality);
        else
            AddReputation(-dissatisfiedLoss * (1f - quality));
    }

    /// <summary>
    /// Call when a customer leaves angry (patience ran out).
    /// </summary>
    public void RecordAngryCustomer()
    {
        AddReputation(-angryLeftLoss);
    }

    public void AddReputation(float amount)
    {
        reputationPoints = Mathf.Max(0f, reputationPoints + amount);
        OnReputationChanged?.Invoke(reputationPoints);
        RecalculateStars();
    }

    /// <summary>
    /// Check if a feature requiring a certain star level is unlocked.
    /// </summary>
    public bool IsUnlocked(int requiredStars)
    {
        return CurrentStars >= requiredStars;
    }

    // ==================== SAVE / LOAD ====================

    public void SetReputation(float points)
    {
        reputationPoints = Mathf.Max(0f, points);
        RecalculateStars();
    }

    public float GetReputation() => reputationPoints;

    // ==================== INTERNAL ====================

    void RecalculateStars()
    {
        int oldStars = CurrentStars;
        int newStars = 1;

        for (int i = starThresholds.Length - 1; i >= 0; i--)
        {
            if (reputationPoints >= starThresholds[i])
            {
                newStars = i + 1;
                break;
            }
        }

        CurrentStars = Mathf.Clamp(newStars, 1, 5);

        if (CurrentStars != oldStars)
        {
            OnStarChanged?.Invoke(oldStars, CurrentStars);
            Debug.Log($"[Reputation] Star rating changed: {oldStars} → {CurrentStars} ({reputationPoints:F0} pts)");

            if (CurrentStars > oldStars && AudioManager.Instance != null)
                AudioManager.Instance.PlaySuccess();
        }
    }
}
