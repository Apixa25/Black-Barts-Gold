// ============================================================================
// SceneConfig.cs
// Black Bart's Gold - Central Scene Configuration
// Path: Assets/Scripts/Core/SceneConfig.cs
// ============================================================================
// Single source of truth for which scenes have their own UI.
// Used by UIManager, AppBootstrap, GameBootstrapper.
// ============================================================================

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Central config for which scenes have their own UI (scene-based workflow).
    /// When true, UIManager disables its canvas and preserves the scene's UI.
    /// </summary>
    public static class SceneConfig
    {
        private static readonly string[] ScenesWithOwnUI =
        {
            "Login",
            "Register",
            "MainMenu",
            "Wallet",
            "Settings",
            "ARHunt"
        };

        /// <summary>
        /// Returns true if the scene has its own UI built in the Unity Editor.
        /// </summary>
        public static bool SceneHasOwnUI(string sceneName)
        {
            foreach (var name in ScenesWithOwnUI)
            {
                if (name == sceneName) return true;
            }
            return false;
        }
    }
}
