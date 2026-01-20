// ============================================================================
// GameBootstrapper.cs
// Black Bart's Gold - Persistent Game Manager
// Path: Assets/Scripts/Core/GameBootstrapper.cs
// ============================================================================
// A persistent singleton that survives scene changes and handles navigation.
// This ensures consistent behavior across all scene transitions.
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Persistent game manager that handles scene transitions reliably.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        private static GameBootstrapper _instance;
        public static GameBootstrapper Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Create the bootstrapper if it doesn't exist
            if (_instance == null)
            {
                var go = new GameObject("[GameBootstrapper]");
                _instance = go.AddComponent<GameBootstrapper>();
                DontDestroyOnLoad(go);
                Debug.Log("[GameBootstrapper] Created persistent instance");
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log("[GameBootstrapper] Duplicate found, destroying...");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log("[GameBootstrapper] Initialized");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameBootstrapper] Scene loaded: {scene.name}");
            
            // Give Unity a frame to set up, then fix input
            StartCoroutine(FixInputNextFrame());
        }

        private System.Collections.IEnumerator FixInputNextFrame()
        {
            // Wait for end of frame
            yield return new WaitForEndOfFrame();
            
            // Wait another frame
            yield return null;
            
            FixEventSystem();
        }

        private void FixEventSystem()
        {
            // Find all EventSystems
            var eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            
            Debug.Log($"[GameBootstrapper] Found {eventSystems.Length} EventSystem(s)");

            if (eventSystems.Length == 0)
            {
                Debug.LogWarning("[GameBootstrapper] No EventSystem found! Creating one...");
                CreateEventSystem();
                return;
            }

            // Keep only one EventSystem
            EventSystem keepThis = eventSystems[0];
            for (int i = 1; i < eventSystems.Length; i++)
            {
                Debug.Log($"[GameBootstrapper] Destroying duplicate EventSystem: {eventSystems[i].name}");
                Destroy(eventSystems[i].gameObject);
            }

            // Force refresh the EventSystem
            if (keepThis != null)
            {
                keepThis.enabled = false;
                keepThis.enabled = true;
                
                // Also refresh input module
                var inputModule = keepThis.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (inputModule != null)
                {
                    inputModule.enabled = false;
                    inputModule.enabled = true;
                }
                
                keepThis.UpdateModules();
                Debug.Log("[GameBootstrapper] EventSystem refreshed");
            }
        }

        private void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[GameBootstrapper] Created new EventSystem");
        }

        /// <summary>
        /// Load a scene by name - use this instead of SceneManager directly
        /// </summary>
        public static void LoadScene(string sceneName)
        {
            Debug.Log($"[GameBootstrapper] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Load a scene by build index
        /// </summary>
        public static void LoadScene(int buildIndex)
        {
            Debug.Log($"[GameBootstrapper] Loading scene index: {buildIndex}");
            SceneManager.LoadScene(buildIndex);
        }
    }
}
