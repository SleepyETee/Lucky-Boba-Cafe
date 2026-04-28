using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public enum PendingStartMode
    {
        None,
        NewGame,
        Continue,
    }

    [Header("Save File")]
    [SerializeField] private string saveFileName = "lucky_boba_save.json";
    [SerializeField] private bool prettyPrintJson = true;

    public PendingStartMode PendingMode { get; private set; } = PendingStartMode.None;
    private GameData loadedData;
    public GameData LoadedData
    {
        get => loadedData;
        private set => loadedData = value;
    }

    string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);



    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null) return;
        SaveManager existing = FindAnyObjectByType<SaveManager>();
        if (existing != null) return;
        GameObject go = new GameObject("SaveManager (Auto)");
        go.AddComponent<SaveManager>();
    }

    static SaveManager GetOrCreate()
    {
        SaveManager existing = FindAnyObjectByType<SaveManager>();
        if (existing != null) return existing;

        GameObject go = new GameObject("SaveManager");
        return go.AddComponent<SaveManager>();
    }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public bool TryLoadFromDisk(out GameData data)
    {
        data = null;

        if (!File.Exists(SavePath))
            return false;

        try
        {
            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            GameData loaded = JsonUtility.FromJson<GameData>(json);
            if (loaded == null)
                return false;

            LoadedData = loaded;
            data = loaded;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Load failed: {e.Message}");
            LoadedData = null;
            return false;
        }
    }

    public void SaveToDisk(GameData data)
    {
        if (data == null) return;

        data.lastSavedUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            string json = JsonUtility.ToJson(data, prettyPrintJson);
            File.WriteAllText(SavePath, json);
            LoadedData = data;
            Debug.Log($"[SaveManager] Saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Save failed: {e.Message}");
        }
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Delete failed: {e.Message}");
        }

        LoadedData = null;

        // Delete save/progress-related PlayerPrefs only.
        // Preserve settings (MasterVolume, MusicVolume, SFXVolume,
        // Fullscreen, ResolutionIndex).
        string[] saveKeys = {
            "HasSave",
            "ActiveQuests", "CompletedQuests",
            "Recipes",
            "PendingDeliveries",
        };
        foreach (string key in saveKeys)
            PlayerPrefs.DeleteKey(key);

        // Dynamic keys: Friend_<name>, CanHire_<name>
        // Must match the NPC names used by NeighborSystem
        string[] npcNames = { "Granny Whiskers", "Chef Mittens", "Luna", "Boba Jr." };
        foreach (string npc in npcNames)
        {
            PlayerPrefs.DeleteKey($"Friend_{npc}");
            PlayerPrefs.DeleteKey($"CanHire_{npc}");
        }
        
        // Also clear any leftover legacy index-based keys
        for (int i = 0; i < 10; i++)
            PlayerPrefs.DeleteKey($"Friend_{i}");

        PlayerPrefs.Save();
    }

    public void QueueNewGame(bool deleteExistingSave)
    {
        if (deleteExistingSave)
            DeleteSave();

        PendingMode = PendingStartMode.NewGame;
    }

    public bool QueueContinue()
    {
        PendingMode = PendingStartMode.Continue;
        return TryLoadFromDisk(out _);
    }

    public void ClearPendingMode()
    {
        PendingMode = PendingStartMode.None;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneNames.GameScene)
            return;

        if (PendingMode == PendingStartMode.None)
            return;

        GameManager gm = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
        if (gm == null)
            return;

        if (PendingMode == PendingStartMode.NewGame)
        {
            gm.ApplyNewGameDefaults();
            PendingMode = PendingStartMode.None;
            return;
        }

        if (PendingMode == PendingStartMode.Continue)
        {
            if (LoadedData == null)
                TryLoadFromDisk(out _);

            if (LoadedData != null)
                gm.ApplySaveData(LoadedData);

            PendingMode = PendingStartMode.None;
            return;
        }
    }

    public bool HasSaveData() => HasSave();

    void OnApplicationQuit()
    {
        if (GameManager.Instance != null && PendingMode == PendingStartMode.None && HasSave())
        {
            GameData snapshot = GameManager.Instance.BuildSaveDataSnapshot();
            SaveToDisk(snapshot);
        }
    }
}

