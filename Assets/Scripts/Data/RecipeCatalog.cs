using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime recipe catalog used by the cafe loop until RecipeData assets exist.
/// Quest rewards already persist recipe names in PlayerPrefs, so this keeps
/// progression visible in customer orders and crafting immediately.
/// </summary>
public static class RecipeCatalog
{
    public readonly struct IngredientNeed
    {
        public readonly string Name;
        public readonly int Quantity;

        public IngredientNeed(string name, int quantity)
        {
            Name = name;
            Quantity = Mathf.Max(1, quantity);
        }
    }

    public readonly struct RecipeInfo
    {
        public readonly string Name;
        public readonly float BrewTarget;
        public readonly float BrewMax;
        public readonly bool NeedsTopping;
        public readonly int RequiredStars;
        public readonly string RequiredUnlock;
        public readonly int BaseTip;
        public readonly int OrderWeight;
        public readonly IngredientNeed[] Ingredients;

        public RecipeInfo(
            string name,
            float brewTarget,
            float brewMax,
            bool needsTopping,
            int requiredStars,
            string requiredUnlock,
            int baseTip,
            int orderWeight,
            params IngredientNeed[] ingredients)
        {
            Name = name;
            BrewTarget = brewTarget;
            BrewMax = brewMax;
            NeedsTopping = needsTopping;
            RequiredStars = requiredStars;
            RequiredUnlock = requiredUnlock;
            BaseTip = baseTip;
            OrderWeight = Mathf.Max(1, orderWeight);
            Ingredients = ingredients ?? Array.Empty<IngredientNeed>();
        }
    }

    static readonly RecipeInfo[] Recipes =
    {
        new RecipeInfo("Classic Milk Tea", 2.5f, 4.0f, false, 0, "", 15, 6,
            Need("Black Tea"), Need("Regular Milk")),
        new RecipeInfo("Green Tea", 1.5f, 3.0f, false, 0, "", 12, 5,
            Need("Green Tea")),

        new RecipeInfo("Taro Boba", 3.0f, 4.5f, true, 2, "", 20, 3,
            Need("Black Tea"), Need("Regular Milk"), Need("Taro Powder"), Need("Tapioca")),
        new RecipeInfo("Brown Sugar", 3.5f, 5.0f, true, 2, "", 22, 3,
            Need("Black Tea"), Need("Regular Milk"), Need("Brown Sugar"), Need("Tapioca")),
        new RecipeInfo("Matcha Latte", 2.0f, 3.5f, false, 3, "", 24, 2,
            Need("Matcha"), Need("Regular Milk")),

        new RecipeInfo("Honey Green Tea", 1.8f, 3.2f, false, 0, "Honey Green Tea", 24, 3,
            Need("Green Tea"), Need("Local Honey")),
        new RecipeInfo("Ancient Herbal Tea", 2.2f, 3.8f, false, 0, "Ancient Herbal Tea", 30, 2,
            Need("Green Tea"), Need("Mountain Herbs")),
        new RecipeInfo("Taro Milk Tea", 3.0f, 4.5f, true, 0, "Taro Milk Tea", 28, 3,
            Need("Black Tea"), Need("Regular Milk"), Need("Taro Powder"), Need("Tapioca")),
        new RecipeInfo("Brown Sugar Boba", 3.5f, 5.0f, true, 0, "Brown Sugar Boba", 32, 3,
            Need("Black Tea"), Need("Regular Milk"), Need("Brown Sugar"), Need("Tapioca")),
        new RecipeInfo("Moonlight Matcha", 2.1f, 3.6f, false, 0, "Moonlight Matcha", 36, 2,
            Need("Matcha"), Need("Regular Milk")),
        new RecipeInfo("Ceremonial Matcha", 1.8f, 3.0f, false, 0, "Ceremonial Matcha", 45, 1,
            Need("Matcha")),
    };

    static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Brown Sugar Boba", "Brown Sugar Boba" },
        { "Brown Sugar", "Brown Sugar" },
        { "Taro Milk Tea", "Taro Milk Tea" },
        { "Taro Boba", "Taro Boba" },
    };

    static IngredientNeed Need(string name, int quantity = 1) => new IngredientNeed(name, quantity);

    public static bool TryGetRecipe(string drinkName, out RecipeInfo recipe)
    {
        string canonical = Canonicalize(drinkName);
        foreach (RecipeInfo candidate in Recipes)
        {
            if (string.Equals(candidate.Name, canonical, StringComparison.OrdinalIgnoreCase))
            {
                recipe = candidate;
                return true;
            }
        }

        recipe = default;
        return false;
    }

    public static List<RecipeInfo> GetAvailableRecipes()
    {
        List<RecipeInfo> available = new List<RecipeInfo>();
        int stars = ReputationSystem.Instance != null ? ReputationSystem.Instance.CurrentStars : 1;
        string unlocked = PlayerPrefs.GetString("Recipes", "");

        foreach (RecipeInfo recipe in Recipes)
        {
            if (recipe.RequiredStars > stars)
                continue;

            if (!string.IsNullOrEmpty(recipe.RequiredUnlock) && !HasUnlockedRecipe(unlocked, recipe.RequiredUnlock))
                continue;

            available.Add(recipe);
        }

        if (available.Count == 0)
        {
            TryGetRecipe("Classic Milk Tea", out RecipeInfo fallback);
            available.Add(fallback);
        }

        return available;
    }

    public static string GetRandomAvailableOrder()
    {
        List<RecipeInfo> available = GetAvailableRecipes();
        int totalWeight = 0;
        foreach (RecipeInfo recipe in available)
            totalWeight += recipe.OrderWeight;

        int roll = UnityEngine.Random.Range(0, Mathf.Max(1, totalWeight));
        foreach (RecipeInfo recipe in available)
        {
            roll -= recipe.OrderWeight;
            if (roll < 0)
                return recipe.Name;
        }

        return available[0].Name;
    }

    public static bool NeedsTopping(string drinkName)
    {
        return TryGetRecipe(drinkName, out RecipeInfo recipe) && recipe.NeedsTopping;
    }

    public static int GetBaseTip(string drinkName)
    {
        return TryGetRecipe(drinkName, out RecipeInfo recipe) ? recipe.BaseTip : 15;
    }

    public static bool HasEnoughIngredients(string drinkName, InventorySystem inventory, out string missingItem)
    {
        missingItem = "";
        if (inventory == null || !inventory.HasAnyStock())
            return true;

        if (!TryGetRecipe(drinkName, out RecipeInfo recipe))
            return true;

        foreach (IngredientNeed need in recipe.Ingredients)
        {
            if (inventory.GetStockByName(need.Name) < need.Quantity)
            {
                missingItem = need.Name;
                return false;
            }
        }

        return true;
    }

    public static void ConsumeIngredients(string drinkName, InventorySystem inventory)
    {
        if (inventory == null || !TryGetRecipe(drinkName, out RecipeInfo recipe))
            return;

        foreach (IngredientNeed need in recipe.Ingredients)
            inventory.RemoveItemByName(need.Name, need.Quantity);
    }

    static bool HasUnlockedRecipe(string unlockedList, string recipeName)
    {
        string[] owned = unlockedList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        return Array.IndexOf(owned, recipeName) >= 0;
    }

    static string Canonicalize(string drinkName)
    {
        if (string.IsNullOrWhiteSpace(drinkName))
            return "Classic Milk Tea";

        return Aliases.TryGetValue(drinkName, out string canonical) ? canonical : drinkName;
    }
}
