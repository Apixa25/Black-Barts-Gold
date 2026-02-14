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
using UnityEngine.InputSystem;
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
            
            // Add AuthService (and ApiClient via its usage) - ensures real login/register work from first scene
            root.AddComponent<AuthService>();
            
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
            eventSystemGO.AddComponent<EventSystemFixer>(); // Ensures RefreshEventSystem() finds our persistent ES
            var inputSystemModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();
            
            // Assign UI Input Actions so the module gets mouse (Editor) and touch (device) input.
            // Asset lives in Resources so it can be loaded at runtime before any scene.
            var uiActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (uiActions != null)
            {
                inputSystemModule.actionsAsset = uiActions;
                Debug.Log("[AppBootstrap] EventSystem created with InputSystemUIInputModule + UI Actions (Editor mouse + device touch)");
            }
            else
            {
                Debug.LogWarning("[AppBootstrap] InputSystem_Actions not found in Resources; UI may not respond in Editor. Add Assets/Resources/InputSystem_Actions.inputactions.");
            }
        }
        
        /// <summary>
        /// Scenes that have their own full UI - they should use their OWN EventSystem,
        /// not the persistent one. Fixes Wallet/Settings freeze (Input System touch stops after scene load).
        /// </summary>
        private static bool SceneHasOwnUI(string sceneName)
        {
            return SceneConfig.SceneHasOwnUI(sceneName);
        }
        
        /// <summary>
        /// Called when any scene loads - manages EventSystem based on scene type.
        /// For scenes-with-own-UI: use scene's EventSystem, disable persistent (fixes touch freeze).
        /// For ARHunt etc: use persistent, destroy scene's duplicate.
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            Debug.Log($"[AppBootstrap] 📍 Scene loaded: {scene.name} | EventSystems found: {eventSystems.Length}");
            
            if (SceneHasOwnUI(scene.name))
            {
                Debug.Log($"[AppBootstrap] 📱 SceneHasOwnUI=true → using scene's EventSystem");
                if (_persistentEventSystem != null)
                {
                    // DESTROY persistent — having 2 EventSystems (even one disabled) can break touch on Android.
                    // We'll recreate it when loading ARHunt etc.
                    Object.Destroy(_persistentEventSystem.gameObject);
                    _persistentEventSystem = null;
                    Debug.Log("[AppBootstrap]   → Destroyed persistent EventSystem (will recreate for ARHunt)");
                }
                foreach (var es in eventSystems)
                {
                    if (es == _persistentEventSystem) continue;
                    es.enabled = true;
                    es.SetSelectedGameObject(null);
                    var mod = es.GetComponent<InputSystemUIInputModule>();
                    var hasActions = mod != null && mod.actionsAsset != null;
                    if (mod != null) mod.enabled = true;
                    Debug.Log($"[AppBootstrap]   → Using scene ES: {es.gameObject.name} InputModule={mod != null} actionsAsset={hasActions}");
                }
                Debug.Log($"[AppBootstrap] 📍 EventSystem.current after setup: {EventSystem.current?.name ?? "null"}");
            }
            else
            {
                // ARHunt etc: need persistent EventSystem — recreate if we destroyed it earlier
                if (_persistentEventSystem == null)
                {
                    var root = GameObject.Find("[BlackBartsGold]");
                    if (root != null)
                    {
                        CreateEventSystem(root.transform);
                        Debug.Log("[AppBootstrap]   → Recreated persistent EventSystem for ARHunt");
                    }
                }
                // Destroy scene's duplicate, use persistent
                foreach (var es in eventSystems)
                {
                    if (es != _persistentEventSystem)
                    {
                        Debug.Log($"[AppBootstrap] Destroying duplicate EventSystem: {es.gameObject.name}");
                        Object.Destroy(es.gameObject);
                    }
                }
                if (_persistentEventSystem != null)
                {
                    _persistentEventSystem.enabled = true;
                    _persistentEventSystem.SetSelectedGameObject(null);
                    var inputModule = _persistentEventSystem.GetComponent<InputSystemUIInputModule>();
                    if (inputModule != null) inputModule.enabled = true;
                    Debug.Log("[AppBootstrap]   → Using persistent EventSystem (ARHunt etc)");
                }
            }
        }
    }
}
