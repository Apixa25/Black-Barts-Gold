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
            
            // Add UIManager
            root.AddComponent<UIManager>();
            
            // Add single persistent EventSystem
            CreateEventSystem(root.transform);
            
            // Subscribe to scene loaded to clean up duplicate EventSystems
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log("[AppBootstrap] ✅ Persistent systems initialized");
            Debug.Log("[AppBootstrap] ✅ Single EventSystem created");
            Debug.Log("[AppBootstrap] ✅ UIManager ready");
        }
        
        private static void CreateEventSystem(Transform parent)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(parent);
            
            _persistentEventSystem = eventSystemGO.AddComponent<EventSystem>();
            var inputSystemModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();
            
#if UNITY_EDITOR
            // In Editor, use StandaloneInputModule so mouse clicks work in the Game view.
            // InputSystemUIInputModule often doesn't receive mouse input in Editor without
            // a custom UI Actions asset; StandaloneInputModule works out of the box.
            eventSystemGO.AddComponent<StandaloneInputModule>();
            inputSystemModule.enabled = false;
            Debug.Log("[AppBootstrap] EventSystem created with StandaloneInputModule (Editor - mouse works in Game view)");
#else
            // On device, use InputSystemUIInputModule for touch and new Input System.
            Debug.Log("[AppBootstrap] EventSystem created with InputSystemUIInputModule");
#endif
        }
        
        /// <summary>
        /// Called when any scene loads - destroys duplicate EventSystems
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[AppBootstrap] Scene loaded: {scene.name}");
            
            // Find and destroy any EventSystems in the loaded scene
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            
            foreach (var es in eventSystems)
            {
                if (es != _persistentEventSystem)
                {
                    Debug.Log($"[AppBootstrap] Destroying duplicate EventSystem: {es.gameObject.name}");
                    Object.Destroy(es.gameObject);
                }
            }
            
            // Ensure our EventSystem is active and working
            if (_persistentEventSystem != null)
            {
                _persistentEventSystem.enabled = true;
#if UNITY_EDITOR
                var standalone = _persistentEventSystem.GetComponent<StandaloneInputModule>();
                if (standalone != null) standalone.enabled = true;
#else
                var inputModule = _persistentEventSystem.GetComponent<InputSystemUIInputModule>();
                if (inputModule != null) inputModule.enabled = true;
#endif
            }
        }
    }
}
