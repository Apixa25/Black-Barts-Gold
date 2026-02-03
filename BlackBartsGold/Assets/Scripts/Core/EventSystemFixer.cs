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
            Debug.Log($"[EventSystemFixer] Awake on {gameObject.name} | ES={eventSystem != null} InputMod={inputModule != null} actionsAsset={inputModule?.actionsAsset != null}");
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
        /// Call this from other scripts if input stops working.
        /// CRITICAL: Only refresh EventSystem.current (the ACTIVE one). Do NOT use FindFirstObjectByType
        /// — that can return the fixer on the DISABLED persistent EventSystem, and running FixEventSystem
        /// on it would re-enable the persistent ES, creating 2 active EventSystems → touch breaks on Android.
        /// </summary>
        public static void RefreshEventSystem()
        {
            var esCurrent = EventSystem.current;
            Debug.Log($"[EventSystemFixer] RefreshEventSystem | EventSystem.current={esCurrent?.name ?? "null"}");
            if (esCurrent == null)
            {
                Debug.LogWarning("[EventSystemFixer] EventSystem.current is null - cannot refresh");
                return;
            }
            // Only fix the ACTIVE EventSystem — never touch the disabled persistent one
            var fixer = esCurrent.GetComponent<EventSystemFixer>();
            if (fixer != null)
            {
                fixer.FixEventSystem();
            }
            else
            {
                // No fixer on current ES — refresh it directly
                esCurrent.SetSelectedGameObject(null);
                esCurrent.enabled = false;
                esCurrent.enabled = true;
                var inputModule = esCurrent.GetComponent<InputSystemUIInputModule>();
                if (inputModule != null)
                {
                    inputModule.enabled = false;
                    inputModule.enabled = true;
                }
                esCurrent.UpdateModules();
                Debug.Log("[EventSystemFixer] Refreshed EventSystem.current (no fixer component)");
            }
        }
    }
}
