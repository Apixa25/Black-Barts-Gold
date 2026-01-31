// ============================================================================
// EventSystemFixer.cs
// Black Bart's Gold - Fixes EventSystem after scene transitions
// Path: Assets/Scripts/Core/EventSystemFixer.cs
// ============================================================================
// Ensures the EventSystem works properly after navigating between scenes.
// Attach this to every EventSystem in every scene.
// ============================================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Fixes a common Unity bug where EventSystem stops responding after scene transitions.
    /// </summary>
    public class EventSystemFixer : MonoBehaviour
    {
        private EventSystem eventSystem;
        private InputSystemUIInputModule inputModule;

        private void Awake()
        {
            eventSystem = GetComponent<EventSystem>();
            inputModule = GetComponent<InputSystemUIInputModule>();
            
            // Check for duplicate EventSystems
            // IMPORTANT: Don't destroy the persistent EventSystem from AppBootstrap!
            EventSystem[] allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (allEventSystems.Length > 1)
            {
                Debug.Log($"[EventSystemFixer] Found {allEventSystems.Length} EventSystems");
                
                // Find the persistent one (it's under [BlackBartsGold] root)
                EventSystem persistentES = null;
                foreach (var es in allEventSystems)
                {
                    if (es.transform.root.name == "[BlackBartsGold]")
                    {
                        persistentES = es;
                        Debug.Log($"[EventSystemFixer] Found persistent EventSystem from AppBootstrap");
                        break;
                    }
                }
                
                // If there's a persistent one, destroy THIS scene's EventSystem (self-destruct)
                if (persistentES != null && eventSystem != persistentES)
                {
                    Debug.Log($"[EventSystemFixer] Destroying THIS scene's EventSystem (keeping AppBootstrap's)");
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private void Start()
        {
            FixEventSystem();
        }

        private void OnEnable()
        {
            FixEventSystem();
        }

        /// <summary>
        /// Forces the EventSystem to reinitialize and start responding to input
        /// </summary>
        public void FixEventSystem()
        {
            if (eventSystem == null)
            {
                eventSystem = GetComponent<EventSystem>();
            }

            if (inputModule == null)
            {
                inputModule = GetComponent<InputSystemUIInputModule>();
            }

            // Disable and re-enable to force refresh
            if (eventSystem != null)
            {
                Debug.Log("[EventSystemFixer] Refreshing EventSystem...");
                eventSystem.enabled = false;
                eventSystem.enabled = true;
            }

            if (inputModule != null)
            {
                Debug.Log("[EventSystemFixer] Refreshing InputModule...");
                inputModule.enabled = false;
                inputModule.enabled = true;
            }

            // Force the EventSystem to update
            if (eventSystem != null)
            {
                eventSystem.UpdateModules();
            }

            Debug.Log("[EventSystemFixer] EventSystem fixed!");
        }

        /// <summary>
        /// Call this from other scripts if input stops working
        /// </summary>
        public static void RefreshEventSystem()
        {
            var fixer = FindFirstObjectByType<EventSystemFixer>();
            if (fixer != null)
            {
                fixer.FixEventSystem();
            }
            else
            {
                // Try to refresh EventSystem directly
                var eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    eventSystem.enabled = false;
                    eventSystem.enabled = true;
                    eventSystem.UpdateModules();
                    Debug.Log("[EventSystemFixer] Refreshed EventSystem.current");
                }
            }
        }
    }
}
