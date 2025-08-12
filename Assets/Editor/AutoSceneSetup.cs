#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AutoSceneSetup
{
    // Target scene we want in the build
    private const string ScenePath = "Assets/Scenes/CounterDrone.unity";

    static AutoSceneSetup()
    {
        EnsureSceneExists();
        EnsureSceneInBuildSettings();
    }

    private static void EnsureSceneExists()
    {
        var dir = Path.GetDirectoryName(ScenePath)?.Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        if (!File.Exists(ScenePath))
        {
            // Create a new empty scene and save it at the expected path
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var ok = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!ok)
            {
                Debug.LogError("[AutoSceneSetup] Failed to create default scene at " + ScenePath);
            }
            else
            {
                Debug.Log("[AutoSceneSetup] Created default scene at " + ScenePath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void EnsureSceneInBuildSettings()
    {
        var wanted = new EditorBuildSettingsScene(ScenePath, true);
        var current = EditorBuildSettings.scenes ?? new EditorBuildSettingsScene[0];

        // Already present and enabled?
        foreach (var s in current)
        {
            if (s.path == ScenePath && s.enabled) return;
        }

        // Replace list with only our scene (keep it simple for Cloud Build)
        EditorBuildSettings.scenes = new[] { wanted };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AutoSceneSetup] Build Settings updated with scene: " + ScenePath);
    }
}
#endif
