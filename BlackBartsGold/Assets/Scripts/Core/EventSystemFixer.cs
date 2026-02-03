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
            Debug.Log("[EventSystemFixer] Initialized");
            // SIMPLIFIED: Just get references, don't destroy anything
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

            // Clear stale selection from previous scene - prevents "frozen" UI (Wallet, Settings)
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
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
                // Fallback if no EventSystemFixer found; refresh EventSystem.current directly
                Debug.Log("[EventSystemFixer] No EventSystemFixer component found - using EventSystem.current fallback");
                var eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    eventSystem.SetSelectedGameObject(null); // Clear stale selection
                    eventSystem.enabled = false;
                    eventSystem.enabled = true;
                    eventSystem.UpdateModules();
                    Debug.Log("[EventSystemFixer] Refreshed EventSystem.current (persistent)");
                }
                else
                {
                    Debug.LogWarning("[EventSystemFixer] EventSystem.current is null - cannot refresh");
                }
            }
        }
    }
}
