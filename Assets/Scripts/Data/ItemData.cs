// ============================================================
// FILE: ItemData.cs
// AUTHOR: Long + Claude
// DESCRIPTION: ScriptableObject defining an ingredient/item
//              with popularity ranks, costs, and categories
//              as described in the game design doc.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Lucky Boba/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName = "New Ingredient";
    [TextArea] public string description = "";
    public Sprite icon;

    [Header("Category")]
    public ItemCategory category = ItemCategory.TeaLeaf;

    [Header("Economy")]
    public int buyCost = 10;
    public int servingsPerPurchase = 5;

    [Header("Popularity (1 = niche, 5 = mainstream)")]
    [Range(1, 5)] public int popularityRank = 3;

    [Header("Special")]
    [Tooltip("If true, buying this before opening gives a big tip bonus when a customer requests it")]
    public bool isSpecialtyItem = false;
    public float specialtyTipMultiplier = 2f;

    [Header("Unlock")]
    [Tooltip("Minimum reputation stars needed to see this in shops")]
    [Range(0, 5)] public int requiredReputation = 0;

    public enum ItemCategory
    {
        TeaLeaf,
        Milk,
        Sweetener,
        Topping,
        Powder,
        Special
    }
}
