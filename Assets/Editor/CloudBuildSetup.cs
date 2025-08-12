#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CloudBuildSetup
{
    // This method is called by Unity Cloud Build
    public static void Prepare()
    {
        Debug.Log("[CloudBuildSetup] Preparing build for Unity Cloud Build...");
        
        EnsureSceneExists();
        EnsureSceneInBuildSettings();
        
        Debug.Log("[CloudBuildSetup] Preparation complete!");
    }

    private static void EnsureSceneExists()
    {
        const string scenePath = "Assets/Scenes/CounterDrone.unity";
        
        // Create Scenes folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
            Debug.Log("[CloudBuildSetup] Created Assets/Scenes folder");
        }

        // Create scene if it doesn't exist
        if (!File.Exists(scenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            bool saved = EditorSceneManager.SaveScene(scene, scenePath);
            
            if (saved)
            {
                Debug.Log("[CloudBuildSetup] Created scene at " + scenePath);
            }
            else
            {
                Debug.LogError("[CloudBuildSetup] Failed to create scene at " + scenePath);
                return;
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.Log("[CloudBuildSetup] Scene already exists at " + scenePath);
        }
    }

    private static void EnsureSceneInBuildSettings()
    {
        const string scenePath = "Assets/Scenes/CounterDrone.unity";
        
        var currentScenes = EditorBuildSettings.scenes ?? new EditorBuildSettingsScene[0];
        
        // Check if scene is already in build settings and enabled
        foreach (var scene in currentScenes)
        {
            if (scene.path == scenePath && scene.enabled)
            {
                Debug.Log("[CloudBuildSetup] Scene already in build settings: " + scenePath);
                return;
            }
        }

        // Add the scene to build settings
        var newScene = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = new[] { newScene };
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[CloudBuildSetup] Added scene to build settings: " + scenePath);
    }

    // Optional: Build method if you want more control
    public static void BuildForCloudBuild()
    {
        Debug.Log("[CloudBuildSetup] Starting custom build process...");
        
        // Ensure everything is set up
        Prepare();
        
        // Define build options
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/CounterDrone.unity" },
            locationPathName = "Build/CounterDrone.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        // Execute the build
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[CloudBuildSetup] Build succeeded!");
        }
        else
        {
            Debug.LogError("[CloudBuildSetup] Build failed!");
        }
    }
}
#endif
