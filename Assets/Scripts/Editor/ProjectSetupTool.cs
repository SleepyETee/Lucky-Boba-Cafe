// ============================================================
// FILE: ProjectSetupTool.cs
// AUTHOR: Long + Claude
// DESCRIPTION: One-click setup tool that handles ALL remaining
//              Unity Editor configuration:
//              1. Generate Animation Controllers & Clips
//              2. Assign Animators to Player/Customer prefabs
//              3. Add SimpleAnimator fallback to prefabs
//              4. Fix Build Settings scene order
//              5. Auto-assign audio clips to AudioManager
//              Run via: Tools > Lucky Boba > Setup Everything
// ============================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ProjectSetupTool : EditorWindow
{
    private static readonly string AnimFolder = "Assets/Animations";
    private static readonly string PrefabFolder = "Assets/Prefabs";
    private static readonly string SceneFolder = "Assets/Scenes";
    private static readonly string AudioFolder = "Assets/Audio";

    private Vector2 scrollPos;
    private static List<string> log = new List<string>();

    // ==================== MENU ITEMS ====================

    [MenuItem("Tools/Lucky Boba/Setup Everything (One Click)", false, 0)]
    public static void SetupEverything()
    {
        log.Clear();
        Log("========================================");
        Log("  LUCKY BOBA CAFE — FULL PROJECT SETUP");
        Log("========================================\n");

        Step1_GenerateAnimations();
        Step2_AssignAnimatorsToPrefabs();
        Step3_AddSimpleAnimatorToPrefabs();
        Step4_FixBuildSettings();
        Step5_AssignAudioClips();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log("\n========================================");
        Log("  SETUP COMPLETE!");
        Log("========================================");
        Log("\nNext: Enter Play Mode and test your game.");

        // Show results window
        var window = GetWindow<ProjectSetupTool>("Setup Results");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    public static void Step1_GenerateAnimations()
    {
        Log("--- STEP 1: Generate Animations ---");
        AnimationSetupTool.GenerateAllAnimations();
        Log("  OK: Animation controllers and clips created in Assets/Animations/");
    }

    [MenuItem("Tools/Lucky Boba/2. Assign Animators to Prefabs", false, 12)]
    public static void Step2_AssignAnimatorsToPrefabs()
    {
        Log("\n--- STEP 2: Assign Animator Controllers to Prefabs ---");

        // Player prefab
        string playerControllerPath = $"{AnimFolder}/PlayerAnimator.controller";
        string playerPrefabPath = $"{PrefabFolder}/Player.prefab";
        AssignAnimatorToPrefab(playerPrefabPath, playerControllerPath, "Player");

        // Customer prefab
        string customerControllerPath = $"{AnimFolder}/CustomerAnimator.controller";
        string customerPrefabPath = $"{PrefabFolder}/Customer.prefab";
        AssignAnimatorToPrefab(customerPrefabPath, customerControllerPath, "Customer");
    }

    [MenuItem("Tools/Lucky Boba/3. Add SimpleAnimator to Prefabs", false, 13)]
    public static void Step3_AddSimpleAnimatorToPrefabs()
    {
        Log("\n--- STEP 3: Add SimpleAnimator Fallback ---");

        AddComponentToPrefab<SimpleAnimator>($"{PrefabFolder}/Player.prefab", "Player");
        AddComponentToPrefab<SimpleAnimator>($"{PrefabFolder}/Customer.prefab", "Customer");
    }

    [MenuItem("Tools/Lucky Boba/4. Fix Build Settings", false, 14)]
    public static void Step4_FixBuildSettings()
    {
        Log("\n--- STEP 4: Fix Build Settings Scene Order ---");

        // Required scenes in exact order
        string[] requiredScenes = new string[]
        {
            $"{SceneFolder}/Main.unity",           // 0: Main Menu
            $"{SceneFolder}/GameScene.unity",       // 1: Cafe gameplay
            $"{SceneFolder}/ShopScene.unity",       // 2: Upgrade shop
            $"{SceneFolder}/VillageScene.unity",    // 3: Village
            $"{SceneFolder}/DeliveryScene.unity",   // 4: Delivery minigame
        };

        var buildScenes = new List<EditorBuildSettingsScene>();
        bool anyMissing = false;

        for (int i = 0; i < requiredScenes.Length; i++)
        {
            string path = requiredScenes[i];
            if (File.Exists(path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                Log($"  [{i}] {Path.GetFileNameWithoutExtension(path)} — OK");
            }
            else
            {
                Log($"  [{i}] {Path.GetFileNameWithoutExtension(path)} — MISSING at {path}");
                anyMissing = true;

                // Try to find it elsewhere
                string fileName = Path.GetFileName(path);
                string[] found = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(path), new[] { "Assets" });
                foreach (string guid in found)
                {
                    string altPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (altPath.EndsWith(".unity") && altPath.Contains(Path.GetFileNameWithoutExtension(path)))
                    {
                        buildScenes.Add(new EditorBuildSettingsScene(altPath, true));
                        Log($"       Found at: {altPath} — using this instead");
                        anyMissing = false;
                        break;
                    }
                }
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();

        if (anyMissing)
            Log("  WARNING: Some scenes are missing. Create them or check paths.");
        else
            Log("  OK: Build Settings updated with all 5 scenes in correct order.");
    }

    [MenuItem("Tools/Lucky Boba/5. Assign Audio Clips", false, 15)]
    public static void Step5_AssignAudioClips()
    {
        Log("\n--- STEP 5: Assign Audio Clips to AudioManager ---");

        // Find audio clips
        AudioClip morningCafe = LoadAudioClip("Open Morning Cafe");
        AudioClip coinClip = LoadAudioClip("Coin");
        AudioClip teaClip = LoadAudioClip("Tea");

        if (morningCafe == null && coinClip == null && teaClip == null)
        {
            Log("  WARNING: No audio clips found in Assets/Audio/");
            Log("  You need to add audio files to Assets/Audio/Music/ and Assets/Audio/SFX/");
            return;
        }

        // We need to find AudioManager in each scene that has one
        // AudioManager is typically in the Main scene (persists via DontDestroyOnLoad)
        string[] scenesToCheck = new string[]
        {
            $"{SceneFolder}/Main.unity",
            $"{SceneFolder}/GameScene.unity",
        };

        bool assigned = false;

        foreach (string scenePath in scenesToCheck)
        {
            if (!File.Exists(scenePath)) continue;

            // Save current scene
            Scene currentScene = EditorSceneManager.GetActiveScene();
            string currentScenePath = currentScene.path;
            bool needsRestore = !string.IsNullOrEmpty(currentScenePath);

            if (currentScene.isDirty)
                EditorSceneManager.SaveScene(currentScene);

            // Open target scene
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find AudioManager
            AudioManager[] managers = Object.FindObjectsByType<AudioManager>(FindObjectsInactive.Include);

            if (managers.Length > 0)
            {
                AudioManager am = managers[0];
                SerializedObject so = new SerializedObject(am);

                int assignCount = 0;

                // Assign music clips
                assignCount += TryAssignClip(so, "bgMusic", morningCafe);
                assignCount += TryAssignClip(so, "menuMusic", morningCafe);
                assignCount += TryAssignClip(so, "morningMusic", morningCafe);
                assignCount += TryAssignClip(so, "lunchRushMusic", morningCafe);
                assignCount += TryAssignClip(so, "afternoonMusic", morningCafe);
                assignCount += TryAssignClip(so, "eveningMusic", morningCafe);

                // Assign SFX clips
                assignCount += TryAssignClip(so, "coinSFX", coinClip);
                assignCount += TryAssignClip(so, "moneySound", coinClip);
                assignCount += TryAssignClip(so, "successSound", coinClip);
                assignCount += TryAssignClip(so, "buttonClick", coinClip);
                assignCount += TryAssignClip(so, "customerHappy", coinClip);

                assignCount += TryAssignClip(so, "brewStartSFX", teaClip);
                assignCount += TryAssignClip(so, "brewPerfectSFX", teaClip);
                assignCount += TryAssignClip(so, "brewGoodSFX", teaClip);
                assignCount += TryAssignClip(so, "brewBadSFX", teaClip);
                assignCount += TryAssignClip(so, "customerArriveSFX", teaClip);
                assignCount += TryAssignClip(so, "customerAngySFX", teaClip);
                assignCount += TryAssignClip(so, "failSound", teaClip);

                so.ApplyModifiedProperties();

                Log($"  OK: Assigned {assignCount} audio clips to AudioManager in {Path.GetFileName(scenePath)}");
                Log($"      Music slots → Open Morning Cafe.mp3");
                Log($"      Coin SFX slots → Coin.mp3");
                Log($"      Tea/Brew SFX slots → Tea.mp3");

                EditorSceneManager.SaveScene(scene);
                assigned = true;
                break; // Only need to assign once (DontDestroyOnLoad)
            }
            else
            {
                Log($"  No AudioManager found in {Path.GetFileName(scenePath)}");
            }
        }

        if (!assigned)
        {
            Log("  WARNING: Could not find AudioManager in any scene.");
            Log("  Make sure an AudioManager GameObject exists in your Main or GameScene.");
        }

        Log("\n  TIP: For better audio variety, add more .mp3/.wav files to");
        Log("       Assets/Audio/Music/ and Assets/Audio/SFX/");
        Log("       Then re-run this tool or assign manually in Inspector.");
    }

    // ==================== HELPER: Assign Animator to Prefab ====================

    static void AssignAnimatorToPrefab(string prefabPath, string controllerPath, string label)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

        if (prefab == null)
        {
            Log($"  SKIP: {label} prefab not found at {prefabPath}");
            return;
        }

        if (controller == null)
        {
            Log($"  SKIP: {label} animator controller not found at {controllerPath}");
            Log($"       Run Step 1 (Generate Animations) first!");
            return;
        }

        // Open prefab for editing
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        Animator animator = prefabRoot.GetComponent<Animator>();
        if (animator == null)
            animator = prefabRoot.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Log($"  OK: {label} prefab → {controller.name} assigned");
    }

    // ==================== HELPER: Add Component to Prefab ====================

    static void AddComponentToPrefab<T>(string prefabPath, string label) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Log($"  SKIP: {label} prefab not found at {prefabPath}");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

        if (prefabRoot.GetComponent<T>() == null)
        {
            prefabRoot.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            Log($"  OK: Added {typeof(T).Name} to {label} prefab");
        }
        else
        {
            Log($"  OK: {label} already has {typeof(T).Name}");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    // ==================== HELPER: Load Audio Clip ====================

    static AudioClip LoadAudioClip(string searchName)
    {
        string[] guids = AssetDatabase.FindAssets(searchName + " t:AudioClip", new[] { AudioFolder });
        if (guids.Length == 0)
        {
            // Try broader search
            guids = AssetDatabase.FindAssets(searchName + " t:AudioClip", new[] { "Assets" });
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                return clip;
        }
        return null;
    }

    // ==================== HELPER: Try Assign Clip to SerializedProperty ====================

    static int TryAssignClip(SerializedObject so, string fieldName, AudioClip clip)
    {
        if (clip == null) return 0;

        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null) return 0;

        // Only assign if currently empty
        if (prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = clip;
            return 1;
        }
        return 0;
    }

    // ==================== LOGGING ====================

    static void Log(string msg)
    {
        log.Add(msg);
        Debug.Log("[ProjectSetup] " + msg);
    }

    // ==================== RESULTS WINDOW ====================

    void OnGUI()
    {
        GUILayout.Label("Lucky Boba Cafe — Setup Results", EditorStyles.boldLabel);
        GUILayout.Space(5);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = true,
            fontSize = 12
        };

        foreach (string line in log)
        {
            string colored = line;
            if (line.Contains("OK:"))
                colored = "<color=green>" + line + "</color>";
            else if (line.Contains("WARNING") || line.Contains("MISSING"))
                colored = "<color=yellow>" + line + "</color>";
            else if (line.Contains("SKIP"))
                colored = "<color=orange>" + line + "</color>";
            else if (line.Contains("==="))
                colored = "<color=cyan>" + line + "</color>";
            
            GUILayout.Label(colored, style);
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.Label("After setup, test with: Tools > Lucky Boba > Test Checklist", EditorStyles.miniLabel);

        if (GUILayout.Button("Close", GUILayout.Height(30)))
            Close();
    }

    // ==================== TEST CHECKLIST WINDOW ====================

    [MenuItem("Tools/Lucky Boba/Test Checklist", false, 30)]
    public static void ShowTestChecklist()
    {
        var window = GetWindow<TestChecklistWindow>("Test Checklist");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }
}

// ============================================================
// TEST CHECKLIST WINDOW
// ============================================================
public class TestChecklistWindow : EditorWindow
{
    private Vector2 scrollPos;
    private bool[] checks = new bool[20];

    private readonly string[] items = new string[]
    {
        "--- MAIN MENU ---",
        "New Game starts fresh (Day 1, $100)",
        "Continue loads saved progress",
        "Settings opens — volume sliders work",
        "Credits panel shows and closes",
        "Quit button closes application",
        "--- CAFE SCENE (GAMEPLAY) ---",
        "Player moves with WASD, sprints with Shift",
        "Day timer advances (6 AM → 7 PM)",
        "Customers spawn and walk to counter",
        "Press E at crafting station starts minigame",
        "5-step crafting works (Brew/Mix/Shake/Top/Serve)",
        "Tips are earned based on quality",
        "Patience bar decreases, angry customers counted",
        "Pause menu works (Escape key)",
        "Day end summary shows (win or lose)",
        "--- SHOP SCENE ---",
        "Upgrade cards display with costs",
        "Buy upgrades deducts money",
        "Start Next Day loads GameScene",
    };

    void OnGUI()
    {
        GUILayout.Label("Pre-Build Test Checklist", EditorStyles.boldLabel);
        GUILayout.Label("Test each item, then check it off:", EditorStyles.miniLabel);
        GUILayout.Space(5);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].StartsWith("---"))
            {
                GUILayout.Space(10);
                GUILayout.Label(items[i], EditorStyles.boldLabel);
            }
            else
            {
                checks[i] = EditorGUILayout.ToggleLeft(items[i], checks[i]);
            }
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        int total = items.Count(s => !s.StartsWith("---"));
        int done = 0;
        for (int i = 0; i < items.Length; i++)
            if (!items[i].StartsWith("---") && checks[i]) done++;

        EditorGUILayout.HelpBox($"Progress: {done}/{total} items checked", MessageType.Info);

        if (done == total)
            EditorGUILayout.HelpBox("All checks passed! Ready to build.", MessageType.Info);

        GUILayout.Space(5);

        if (GUILayout.Button("Build Game (File > Build Settings > Build)", GUILayout.Height(30)))
        {
            EditorApplication.ExecuteMenuItem("File/Build Settings...");
        }
    }
}
#endif
