// BuildScript.cs - Black Bart's Gold
// Builds Android APK/AAB for command-line and menu.
// Path: Assets/Editor/BuildScript.cs

using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    const string BuildOutputFolder = "Builds/Android";
    const string AppName = "BlackBartsGold";

    /// <summary>
    /// Build Android APK. Call from command line: -executeMethod BuildScript.BuildAndroid
    /// </summary>
    public static void BuildAndroid()
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] No enabled scenes in Build Settings!");
            throw new System.Exception("No scenes to build. Enable scenes in File > Build Settings.");
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        if (!Directory.Exists(Path.Combine(projectRoot, BuildOutputFolder)))
        {
            Directory.CreateDirectory(Path.Combine(projectRoot, BuildOutputFolder));
        }

        // APK for direct install (use .aab for Play Store)
        bool useAAB = EditorUserBuildSettings.buildAppBundle;
        string extension = useAAB ? ".aab" : ".apk";
        string outputPath = Path.Combine(projectRoot, BuildOutputFolder, AppName + extension);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log($"[BuildScript] Building Android ({extension}) to: {outputPath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Build succeeded! Size: {report.summary.totalSize} bytes");
        }
        else
        {
            string errors = report.summary.result == BuildResult.Failed
                ? report.summary.ToString()
                : "Build failed. Check Editor log.";
            throw new System.Exception("Android build failed: " + errors);
        }
    }

    static string[] GetEnabledScenes()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path))
                list.Add(s.path);
        }
        return list.ToArray();
    }

    [MenuItem("Build/Build Android APK")]
    static void BuildAndroidFromMenu()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        BuildAndroid();
    }

    [MenuItem("Build/Build Android AAB (Play Store)")]
    static void BuildAndroidAABFromMenu()
    {
        EditorUserBuildSettings.buildAppBundle = true;
        BuildAndroid();
    }
}
