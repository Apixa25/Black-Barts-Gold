// ============================================================================
// MainMenuSceneSetup.cs
// Black Bart's Gold - MainMenu Scene Complete Setup
// Path: Assets/Scripts/UI/MainMenuSceneSetup.cs
// ============================================================================
// Properly sets up all MainMenu UI elements at runtime with correct positioning.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace BlackBartsGold.UI
{
    [ExecuteAlways]
    public class MainMenuSceneSetup : MonoBehaviour
    {
        // Colors from project vision
        private readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        private readonly Color DeepSeaBlue = new Color(0.102f, 0.212f, 0.365f);
        private readonly Color Parchment = new Color(0.961f, 0.902f, 0.827f);
        private readonly Color DarkBrown = new Color(0.239f, 0.161f, 0.078f);

        private void OnEnable()
        {
            ApplySetup();
        }

        private void Start()
        {
            ApplySetup();
        }

        private void ApplySetup()
        {
            if (transform == null) return;
#if UNITY_EDITOR
            // Skip during player build so we don't hit MissingReferenceException from edit-mode destroy/recreate
            if (UnityEditor.BuildPipeline.isBuildingPlayer) return;
#endif
            Debug.Log("[MainMenuSceneSetup] Applying MainMenu UI setup...");
            
            SetupCanvas();
            SetupBackground();
            SetupTitle();
            SetupStartHuntButton();
            SetupWalletButton();
            SetupSettingsButton();
            DisableDebugPanels();
            
            Debug.Log("[MainMenuSceneSetup] MainMenu UI setup complete!");
        }
        
        /// <summary>
        /// Disable all debug/diagnostic panels. Fixes bug: debug panel reappears when returning from AR.
        /// </summary>
        private void DisableDebugPanels()
        {
            // 1. Find DebugDiagnosticsPanel in scene (from ARHunt if it persisted, or MainMenu hierarchy)
            var debugPanel = GameObject.Find("DebugDiagnosticsPanel");
            if (debugPanel != null)
            {
                debugPanel.SetActive(false);
                Debug.Log("[MainMenuSceneSetup] Debug panel explicitly disabled.");
            }
            
            // 2. Disable EmergencyMapButton overlay (persists with DontDestroyOnLoad)
            var emergencyBtn = FindFirstObjectByType<EmergencyMapButton>();
            if (emergencyBtn != null)
            {
                emergencyBtn.showDebugInfo = false;
                emergencyBtn.showButton = false;
                Debug.Log("[MainMenuSceneSetup] EmergencyMapButton debug overlay disabled.");
            }
            
        }

        private void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void SetupBackground()
        {
            var bg = transform.Find("BackgroundPanel");
            if (bg == null) return;

            var rect = bg.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var image = bg.GetComponent<Image>();
            if (image != null)
            {
                image.color = DeepSeaBlue;
                // CRITICAL: Disable raycast so it doesn't block button clicks!
                image.raycastTarget = false;
            }
            
            // Move background to be first sibling so buttons render on top
            bg.SetAsFirstSibling();
        }

        private void SetupTitle()
        {
            var title = transform.Find("TitleText");
            if (title == null) return;

            var rect = title.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0, -150);
                rect.sizeDelta = new Vector2(900, 280); // Taller for multi-line banner
            }

            var text = title.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = "I've hidden treasure out there. Get out there and find it!";
                text.fontSize = 56;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = GoldColor;
                text.enableWordWrapping = true; // Long banner text
                text.raycastTarget = false; // Don't block touches
            }
        }

        private void SetupStartHuntButton()
        {
            var btn = transform.Find("StartHuntButton");
            if (btn == null) return;

            var rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0, 100);
                rect.sizeDelta = new Vector2(650, 120);
            }

            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = GoldColor;
            }

            SetupButtonText(btn, "🏴‍☠️ START HUNTING", 40);
        }

        private void SetupWalletButton()
        {
            var btn = transform.Find("WalletButton");
            if (btn == null) { Debug.Log("[MainMenuSceneSetup] ⚠️ WalletButton not found"); return; }
            var btnComp = btn.GetComponent<Button>();
            Debug.Log($"[MainMenuSceneSetup] WalletButton found interactable={btnComp?.interactable}");

            var rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0, -50);
                rect.sizeDelta = new Vector2(550, 90);
            }

            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = Parchment;
            }

            SetupButtonText(btn, "👛 MY WALLET", 32);
        }

        private void SetupSettingsButton()
        {
            var btn = transform.Find("SettingsButton");
            if (btn == null) { Debug.Log("[MainMenuSceneSetup] ⚠️ SettingsButton not found"); return; }
            var btnComp = btn.GetComponent<Button>();
            Debug.Log($"[MainMenuSceneSetup] SettingsButton found interactable={btnComp?.interactable}");

            var rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0, -170);
                rect.sizeDelta = new Vector2(550, 90);
            }

            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = Parchment;
            }

            SetupButtonText(btn, "⚙️ SETTINGS", 32);
        }

        private void SetupButtonText(Transform button, string label, int fontSize)
        {
            var textTransform = button.Find("ButtonText");
            if (textTransform == null)
            {
                textTransform = button.Find("Text");
            }
            // If existing child has no RectTransform (invalid UI child from scene), replace it
            if (textTransform != null && textTransform.GetComponent<RectTransform>() == null)
            {
                if (Application.isPlaying)
                    Destroy(textTransform.gameObject);
                else
                    DestroyImmediate(textTransform.gameObject);
                textTransform = null;
            }
            // Treat destroyed refs as null (e.g. stale from previous ApplySetup or edit-mode)
            if (textTransform != null && !textTransform)
                textTransform = null;
            if (textTransform == null)
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(button, false);
                textTransform = textGO.transform;
            }

            var textRect = textTransform.GetComponent<RectTransform>();
            if (textRect == null)
            {
                textRect = textTransform.gameObject.AddComponent<RectTransform>();
            }

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            var tmpText = textTransform.GetComponent<TMP_Text>();
            if (tmpText == null)
            {
                tmpText = textTransform.gameObject.AddComponent<TextMeshProUGUI>();
            }

            tmpText.text = label;
            tmpText.fontSize = fontSize;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = DarkBrown;
            tmpText.raycastTarget = false;
        }
    }
}
