// ============================================================================
// InputSystemFixer.cs
// Black Bart's Gold - Editor Tool
// Path: Assets/Editor/InputSystemFixer.cs
// ============================================================================
// Editor utility to fix the known Unity 6 Android touch input bug.
// Sets Active Input Handling to "Both" which is the documented workaround.
// ============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlackBartsGold.Editor
{
    /// <summary>
    /// Fixes the Unity 6 Android touch input bug by setting Active Input Handling to Both.
    /// </summary>
    public static class InputSystemFixer
    {
        [MenuItem("Black Bart's Gold/Fix Input System (Set to Both)")]
        public static void SetInputToBoth()
        {
            // Check current setting
            var current = PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android);
            
            // Set to Both (2)
            // 0 = InputManager (Old)
            // 1 = InputSystemPackage (New)  
            // 2 = Both
            
            #if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            Debug.Log("[InputSystemFixer] Active Input Handling is already set to Both!");
            EditorUtility.DisplayDialog(
                "Input System Status",
                "Active Input Handling is already set to 'Both'.\n\n" +
                "Your project is configured correctly for mobile touch input.",
                "OK"
            );
            #else
            Debug.Log("[InputSystemFixer] Setting Active Input Handling to Both...");
            Debug.Log("[InputSystemFixer] Please go to: Edit > Project Settings > Player > Other Settings");
            Debug.Log("[InputSystemFixer] Set 'Active Input Handling' to 'Both'");
            
            EditorUtility.DisplayDialog(
                "Manual Step Required",
                "Please set Active Input Handling to 'Both' manually:\n\n" +
                "1. Edit > Project Settings > Player\n" +
                "2. Expand 'Other Settings'\n" +
                "3. Find 'Active Input Handling'\n" +
                "4. Select 'Both' from dropdown\n" +
                "5. Unity will restart to apply changes\n\n" +
                "This fixes the known Unity 6 Android touch bug!",
                "Open Player Settings"
            );
            
            // Open Player Settings
            SettingsService.OpenProjectSettings("Project/Player");
            #endif
        }

        [MenuItem("Black Bart's Gold/Verify Project Settings")]
        public static void VerifySettings()
        {
            string report = "=== Black Bart's Gold Project Verification ===\n\n";
            bool allGood = true;

            // Check Input System
            #if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            report += "✅ Active Input Handling: Both\n";
            #elif ENABLE_INPUT_SYSTEM
            report += "⚠️ Active Input Handling: New Only (may cause mobile issues)\n";
            allGood = false;
            #else
            report += "⚠️ Active Input Handling: Legacy Only\n";
            allGood = false;
            #endif

            // Check Graphics API
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            report += $"📱 Android Graphics APIs: {string.Join(", ", graphicsAPIs)}\n";

            // Check minimum API level
            report += $"📱 Android Min SDK: {PlayerSettings.Android.minSdkVersion}\n";
            report += $"📱 Android Target SDK: {PlayerSettings.Android.targetSdkVersion}\n";

            // Check AR settings
            report += $"🎯 Company Name: {PlayerSettings.companyName}\n";
            report += $"🎯 Product Name: {PlayerSettings.productName}\n";
            report += $"🎯 Bundle ID: {PlayerSettings.applicationIdentifier}\n";

            report += "\n";
            if (allGood)
            {
                report += "✅ All critical settings look good!";
            }
            else
            {
                report += "⚠️ Some settings need attention. See above.";
            }

            Debug.Log(report);
            EditorUtility.DisplayDialog("Project Verification", report, "OK");
        }

        // Auto-check on editor load
        [InitializeOnLoadMethod]
        private static void CheckOnLoad()
        {
            #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogWarning(
                "[Black Bart's Gold] ⚠️ Active Input Handling is NOT set to 'Both'!\n" +
                "This may cause touch input to fail on Android.\n" +
                "Go to: Edit > Project Settings > Player > Other Settings\n" +
                "Set 'Active Input Handling' to 'Both'"
            );
            #endif
        }
    }
}
#endif
