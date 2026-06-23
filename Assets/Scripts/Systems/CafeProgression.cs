using UnityEngine;

public static class CafeProgression
{
    static readonly string[] HireableNpcs =
    {
        "Granny Whiskers",
        "Chef Mittens",
        "Boba Jr."
    };

    public static int HiredHelperCount
    {
        get
        {
            int count = 0;
            foreach (string npc in HireableNpcs)
            {
                if (IsHired(npc))
                    count++;
            }
            return count;
        }
    }

    public static float PatienceBonusSeconds
    {
        get
        {
            float bonus = HiredHelperCount * 2f;
            if (IsHired("Granny Whiskers"))
                bonus += 4f;
            if (IsHired("Boba Jr."))
                bonus += 2f;
            return bonus;
        }
    }

    public static float TipMultiplier
    {
        get
        {
            float multiplier = 1f + HiredHelperCount * 0.05f;
            if (IsHired("Chef Mittens"))
                multiplier += 0.1f;
            return multiplier;
        }
    }

    public static int ExtraCustomerCapacity => IsHired("Boba Jr.") ? 1 : 0;

    public static bool IsHired(string npcName)
    {
        return !string.IsNullOrEmpty(npcName) &&
            PlayerPrefs.GetInt($"CanHire_{npcName}", 0) == 1;
    }
}
