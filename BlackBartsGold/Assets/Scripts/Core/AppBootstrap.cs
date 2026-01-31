// ============================================================================
// AppBootstrap.cs
// Black Bart's Gold - Application Bootstrap
// Path: Assets/Scripts/Core/AppBootstrap.cs
// ============================================================================
// MARKET STANDARD: Single entry point that sets up all persistent systems.
// This runs BEFORE any scene loads and creates the one EventSystem and 
// UIManager that will survive the entire app session.
// ============================================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Bootstraps the application with persistent systems.
    /// Runs before any scene loads.
    /// </summary>
    public static class AppBootstrap
    {
        private static bool _initialized = false;
        private static EventSystem _persistentEventSystem;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            Debug.Log("==============================================");
            Debug.Log("🏴‍☠️ BLACK BART'S GOLD - Starting Up!");
            Debug.Log("==============================================");
            
            // Create the persistent game root
            var root = new GameObject("[BlackBartsGold]");
            Object.DontDestroyOnLoad(root);
            
            // SIMPLIFIED: Don't create UIManager here - let scenes manage their own UI
            // UIManager will be created only when entering AR scenes
            // root.AddComponent<UIManager>();
            
            // SIMPLIFIED: Don't create EventSystem here - let scenes use their own
            // CreateEventSystem(root.transform);
            
            // Subscribe to scene loaded for any needed setup
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log("[AppBootstrap] ✅ Simplified bootstrap complete");
            Debug.Log("[AppBootstrap] ✅ Scenes will manage their own UI and EventSystem");
        }
        
        private static void CreateEventSystem(Transform parent)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(parent);
            
            _persistentEventSystem = eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<InputSystemUIInputModule>();
            
            Debug.Log("[AppBootstrap] EventSystem created with InputSystemUIInputModule");
        }
        
        /// <summary>
        /// Called when any scene loads
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[AppBootstrap] Scene loaded: {scene.name}");
            // SIMPLIFIED: Let scenes manage their own EventSystems
            // No more destroying or managing EventSystems from here
        }
    }
}
