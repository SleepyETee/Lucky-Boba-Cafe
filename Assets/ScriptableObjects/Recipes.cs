// ============================================================
// FILE: Recipes.cs
// AUTHOR: Long + Claude
// DESCRIPTION: ScriptableObject container for all drink recipes.
//              Used by customers to generate orders and by the
//              crafting system to validate ingredient availability.
// ============================================================
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Recipes", menuName = "Scriptable Objects/Recipes")]
public class Recipes : ScriptableObject
{
    [Header("All Drink Recipes")]
    public RecipeData[] allRecipes;

    /// <summary>
    /// Get recipes the player has unlocked based on reputation.
    /// </summary>
    public RecipeData[] GetUnlocked(int currentStars)
    {
        var list = new List<RecipeData>();
        if (allRecipes == null) return list.ToArray();
        foreach (var recipe in allRecipes)
        {
            if (recipe != null && recipe.requiredReputation <= currentStars && !recipe.requiresDiscovery)
                list.Add(recipe);
        }
        return list.ToArray();
    }

    /// <summary>
    /// Pick a random order weighted by recipe popularity (orderWeight).
    /// Only picks from unlocked recipes the player can currently craft.
    /// </summary>
    public RecipeData PickRandomOrder(int currentStars, InventorySystem inventory = null)
    {
        var available = new List<RecipeData>();
        int totalWeight = 0;

        if (allRecipes == null) return null;

        foreach (var recipe in allRecipes)
        {
            if (recipe == null) continue;
            if (recipe.requiredReputation > currentStars) continue;
            if (recipe.requiresDiscovery) continue;

            // If inventory exists, only allow recipes we have stock for
            if (inventory != null && !recipe.CanCraft(inventory)) continue;

            available.Add(recipe);
            totalWeight += recipe.orderWeight;
        }

        if (available.Count == 0) return null;

        int roll = Random.Range(0, totalWeight);
        int running = 0;
        foreach (var recipe in available)
        {
            running += recipe.orderWeight;
            if (roll < running)
                return recipe;
        }

        return available[available.Count - 1];
    }

    /// <summary>
    /// Find a recipe by drink name.
    /// </summary>
    public RecipeData Find(string drinkName)
    {
        if (allRecipes == null) return null;
        foreach (var recipe in allRecipes)
        {
            if (recipe != null && recipe.drinkName == drinkName)
                return recipe;
        }
        return null;
    }
}
