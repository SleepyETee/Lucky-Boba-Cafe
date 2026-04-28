// ============================================================
// FILE: Items.cs
// AUTHOR: Long + Claude
// DESCRIPTION: ScriptableObject container for a collection of
//              ItemData assets, used by shops and inventory.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "Items", menuName = "Scriptable Objects/Items")]
public class Items : ScriptableObject
{
    [Header("All Available Ingredients")]
    public ItemData[] allItems;

    /// <summary>
    /// Get items filtered by category.
    /// </summary>
    public ItemData[] GetByCategory(ItemData.ItemCategory category)
    {
        var list = new System.Collections.Generic.List<ItemData>();
        if (allItems == null) return list.ToArray();
        foreach (var item in allItems)
        {
            if (item != null && item.category == category)
                list.Add(item);
        }
        return list.ToArray();
    }

    /// <summary>
    /// Get items the player can see based on their current reputation.
    /// </summary>
    public ItemData[] GetAvailable(int currentStars)
    {
        var list = new System.Collections.Generic.List<ItemData>();
        if (allItems == null) return list.ToArray();
        foreach (var item in allItems)
        {
            if (item != null && item.requiredReputation <= currentStars)
                list.Add(item);
        }
        return list.ToArray();
    }

    /// <summary>
    /// Find an item by name.
    /// </summary>
    public ItemData Find(string itemName)
    {
        if (allItems == null) return null;
        foreach (var item in allItems)
        {
            if (item != null && item.itemName == itemName)
                return item;
        }
        return null;
    }
}
