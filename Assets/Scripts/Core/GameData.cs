using System;

[Serializable]
public class GameData
{
    public int saveVersion = 1;

    // Progress
    public int pawCoins;
    public int currentDay = 1;
    public int lifetimeCustomersServed;
    public int lifetimeTips;
    public int highScore;

    // Reputation
    public float reputationPoints;

    // Settings
    public float masterVolume = 0.75f;
    public float musicVolume = 0.75f;
    public float sfxVolume = 0.75f;

    // Upgrades (index matches UpgradeData array order in GameManager)
    public int[] upgradeLevels = new int[0];

    // Metadata
    public long lastSavedUnixMs;

    public static GameData NewDefault()
    {
        return new GameData
        {
            pawCoins = 0,
            currentDay = 1,
            lifetimeCustomersServed = 0,
            lifetimeTips = 0,
            highScore = 0,
            reputationPoints = 0f,
            upgradeLevels = new int[0],
            masterVolume = 0.75f,
            musicVolume = 0.75f,
            sfxVolume = 0.75f,
            lastSavedUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
    }

    public GameData Clone()
    {
        GameData copy = (GameData)MemberwiseClone();
        if (upgradeLevels != null)
            copy.upgradeLevels = (int[])upgradeLevels.Clone();
        return copy;
    }
}

