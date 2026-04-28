using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MissingScriptTools
{
    [MenuItem("Tools/Lucky Boba/Find Missing Scripts In Open Scenes")]
    public static void FindMissingScriptsInOpenScenes()
    {
        int total = 0;
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
                total += LogMissingInHierarchy(root, $"{scene.path}");
        }

        Debug.Log($"[MissingScriptTools] Missing scripts found in open scenes: {total}");
    }

    [MenuItem("Tools/Lucky Boba/Remove Missing Scripts In Open Scenes")]
    public static void RemoveMissingScriptsInOpenScenes()
    {
        int removed = 0;
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            bool sceneChanged = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                int r = RemoveMissingInHierarchy(root);
                if (r > 0) sceneChanged = true;
                removed += r;
            }

            if (sceneChanged)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log($"[MissingScriptTools] Removed missing scripts from open scenes: {removed}");
    }

    [MenuItem("Tools/Lucky Boba/Find Missing Scripts In Prefabs")]
    public static void FindMissingScriptsInPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            total += LogMissingInHierarchy(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"[MissingScriptTools] Missing scripts found in prefabs: {total}");
    }

    [MenuItem("Tools/Lucky Boba/Remove Missing Scripts In Prefabs")]
    public static void RemoveMissingScriptsInPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int removed = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            int r = RemoveMissingInHierarchy(root);
            if (r > 0)
            {
                removed += r;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MissingScriptTools] Removed missing scripts from prefabs: {removed}");
    }

    static int LogMissingInHierarchy(GameObject go, string ownerPath)
    {
        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (missing > 0)
            Debug.LogWarning($"[MissingScriptTools] Missing scripts: {missing} | {ownerPath} | {GetHierarchyPath(go)}", go);

        foreach (Transform child in go.transform)
            missing += LogMissingInHierarchy(child.gameObject, ownerPath);

        return missing;
    }

    static int RemoveMissingInHierarchy(GameObject go)
    {
        int removed = 0;
        int here = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (here > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            removed += here;
        }

        foreach (Transform child in go.transform)
            removed += RemoveMissingInHierarchy(child.gameObject);

        return removed;
    }

    static string GetHierarchyPath(GameObject go)
    {
        List<string> parts = new List<string>();
        Transform t = go.transform;
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
