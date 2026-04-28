// ============================================================
// FILE: RecipeData.cs
// AUTHOR: Long + Claude
// DESCRIPTION: ScriptableObject defining a drink recipe with
//              required ingredients, difficulty, and unlock
//              conditions tied to the reputation system.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Lucky Boba/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("Basic Info")]
    public string drinkName = "New Drink";
    [TextArea] public string description = "";
    public Sprite icon;

    [Header("Ingredients Required")]
    public IngredientEntry[] ingredients;

    [Header("Crafting")]
    [Range(0.5f, 3f)] public float difficultyMultiplier = 1f;
    [Tooltip("Brew target time override (0 = use default)")]
    public float brewTargetOverride = 0f;

    [Header("Economy")]
    public int basePrice = 20;
    public int baseTip = 15;

    [Header("Unlock")]
    [Tooltip("Minimum reputation stars needed to unlock this recipe")]
    [Range(0, 5)] public int requiredReputation = 0;
    [Tooltip("If true, must be discovered through neighbor hints or experimentation")]
    public bool requiresDiscovery = false;

    [Header("Popularity")]
    [Tooltip("Higher = ordered more often by customers")]
    [Range(1, 10)] public int orderWeight = 5;

    [System.Serializable]
    public struct IngredientEntry
    {
        public ItemData item;
        public int quantity;
    }

    /// <summary>
    /// Check if the player has all required ingredients in their inventory.
    /// </summary>
    public bool CanCraft(InventorySystem inventory)
    {
        if (inventory == null || ingredients == null) return false;
        foreach (var entry in ingredients)
        {
            if (entry.item == null) continue;
            if (inventory.GetStock(entry.item) < entry.quantity)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Consume the required ingredients from inventory.
    /// </summary>
    public void ConsumeIngredients(InventorySystem inventory)
    {
        if (!CanCraft(inventory)) return;
        foreach (var entry in ingredients)
        {
            if (entry.item == null) continue;
            inventory.RemoveItem(entry.item, entry.quantity);
        }
    }
}
