// ============================================================
// FILE: UpgradeData.cs
// AUTHOR: Long + Claude
// DESCRIPTION: ScriptableObject that defines a single upgrade
//              purchasable in the between-day shop.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Lucky Boba/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName = "New Upgrade";
    [TextArea] public string description = "";
    public int baseCost = 50;
    public int costIncreasePerLevel = 25;
    public int maxLevel = 3;

    public int CostForLevel(int currentLevel)
    {
        return baseCost + costIncreasePerLevel * currentLevel;
    }
}
