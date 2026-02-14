// ============================================================================
// DiagnosticLog.cs
// Black Bart's Gold - ADB-Friendly Diagnostic Logging
// Path: Assets/Scripts/Utils/DiagnosticLog.cs
// ============================================================================
// Use [BBG] prefix for easy ADB filtering: adb logcat | grep "\[BBG\]"
// ============================================================================

using UnityEngine;

namespace BlackBartsGold.Utils
{
    /// <summary>
    /// Diagnostic logging with [BBG] prefix for ADB logcat filtering.
    /// Filter: adb logcat -s Unity | findstr "BBG"
    /// Or:   adb logcat | grep "\[BBG\]"
    /// </summary>
    public static class DiagnosticLog
    {
        private const string Prefix = "[BBG]";

        /// <summary>Log with [BBG][tag] prefix. Always logs.</summary>
        public static void Log(string tag, string message)
        {
            Debug.Log($"{Prefix}[{tag}] {message}");
        }

        /// <summary>Log warning with [BBG][tag] prefix.</summary>
        public static void Warn(string tag, string message)
        {
            Debug.LogWarning($"{Prefix}[{tag}] {message}");
        }

        /// <summary>Log error with [BBG][tag] prefix.</summary>
        public static void Error(string tag, string message)
        {
            Debug.LogError($"{Prefix}[{tag}] {message}");
        }
    }
}
