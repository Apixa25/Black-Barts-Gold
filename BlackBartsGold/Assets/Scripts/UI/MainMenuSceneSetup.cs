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
    public class MainMenuSceneSetup : MonoBehaviour
    {
        // Colors from project vision
        private readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        private readonly Color DeepSeaBlue = new Color(0.102f, 0.212f, 0.365f);
        private readonly Color Parchment = new Color(0.961f, 0.902f, 0.827f);
        private readonly Color DarkBrown = new Color(0.239f, 0.161f, 0.078f);
        private bool _isApplyingSetup;
        private int _lastAppliedFrame = -1;

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
            if (_isApplyingSetup || _lastAppliedFrame == Time.frameCount) return;
#if UNITY_EDITOR
            // Skip during player build so we don't hit MissingReferenceException from edit-mode destroy/recreate
            if (UnityEditor.BuildPipeline.isBuildingPlayer) return;
#endif
            _isApplyingSetup = true;
            Debug.Log("[MainMenuSceneSetup] Applying MainMenu UI setup...");
            try
            {
                Debug.Log($"[MainMenuSceneSetup][Trace] Root='{gameObject.name}' children={transform.childCount} active={gameObject.activeInHierarchy}");
                SetupCanvas();
                Debug.Log("[MainMenuSceneSetup][Trace] SetupCanvas complete");
                SetupBackground();
                Debug.Log("[MainMenuSceneSetup][Trace] SetupBackground complete");
                SetupTitle();
                Debug.Log("[MainMenuSceneSetup][Trace] SetupTitle complete");
                SetupStartHuntButton();
                Debug.Log("[MainMenuSceneSetup][Trace] SetupStartHuntButton complete");
                SetupWalletButton();
                LogButtonState("WalletButton");
                SetupProfileButton();
                LogButtonState("ProfileButton");
                SetupSettingsButton();
                LogButtonState("SettingsButton");
                DisableDebugPanels();
                CleanupCenteredWhiteSquareArtifacts();

                Debug.Log("[MainMenuSceneSetup] MainMenu UI setup complete!");
            }
            finally
            {
                _isApplyingSetup = false;
                _lastAppliedFrame = Time.frameCount;
            }
        }
        
        /// <summary>
        /// Disable all debug/diagnostic panels. Fixes bug: debug panel reappears when returning from AR.
        /// </summary>
        private void DisableDebugPanels()
        {
            // 1. Disable every DebugDiagnosticsPanel instance in loaded objects.
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                if (obj.name == "DebugDiagnosticsPanel")
                {
                    obj.SetActive(false);
                    Debug.Log("[MainMenuSceneSetup] Debug panel explicitly disabled.");
                }
            }
            
            // 2. Disable EmergencyMapButton overlay (persists with DontDestroyOnLoad)
            var emergencyBtn = FindFirstObjectByType<EmergencyMapButton>();
            if (emergencyBtn != null)
            {
                emergencyBtn.showDebugInfo = false;
                emergencyBtn.showButton = false;
                emergencyBtn.enabled = false;
                Debug.Log("[MainMenuSceneSetup] EmergencyMapButton debug overlay disabled.");
            }

            // 3. Defensive cleanup: disable legacy diagnostic overlays by naming convention.
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                var name = obj.name.ToLowerInvariant();
                if (name.Contains("debug") || name.Contains("diagnostic") || name.Contains("console"))
                {
                    // Keep this setup root active; disable only children/foreign overlays.
                    if (obj == gameObject) continue;
                    if (!obj.activeSelf) continue;
                    obj.SetActive(false);
                    Debug.Log($"[MainMenuSceneSetup] Disabled legacy debug overlay object: {obj.name}");
                }
            }
            
        }

        private void CleanupCenteredWhiteSquareArtifacts()
        {
            // Safety pass for scene-authored leftovers that appear as centered white squares.
            var allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (var image in allImages)
            {
                if (image == null) continue;
                var rect = image.rectTransform;
                if (rect == null) continue;

                bool centeredAnchor = Mathf.Abs(rect.anchorMin.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMin.y - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.y - 0.5f) < 0.01f;
                bool centeredPosition = rect.anchoredPosition.sqrMagnitude < 9f;
                bool looksLikeWhiteSquare = image.color.a > 0.95f
                    && image.color.r > 0.95f
                    && image.color.g > 0.95f
                    && image.color.b > 0.95f;
                bool hasNoVisualSource = image.sprite == null;
                string lowerName = image.gameObject.name.ToLowerInvariant();
                bool explicitArtifactName = lowerName.Contains("crosshair")
                    || lowerName.Contains("compassarrow")
                    || lowerName.Contains("center")
                    || lowerName.Contains("reticle");

                // Be intentionally aggressive (same policy as ARHunt): any centered, pure-white, sprite-less image
                // (or explicitly named artifact) is treated as a stray artifact and disabled, regardless of size.
                if (centeredAnchor && centeredPosition && ((looksLikeWhiteSquare && hasNoVisualSource) || explicitArtifactName))
                {
                    image.enabled = false;
                    Debug.Log($"[MainMenuSceneSetup] Disabled centered white square artifact on {image.gameObject.name}");
                }
            }

            var allRawImages = FindObjectsByType<RawImage>(FindObjectsSortMode.None);
            foreach (var rawImage in allRawImages)
            {
                if (rawImage == null || rawImage.texture != null) continue;
                var rect = rawImage.rectTransform;
                if (rect == null) continue;

                bool centeredAnchor = Mathf.Abs(rect.anchorMin.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMin.y - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.y - 0.5f) < 0.01f;
                bool centeredPosition = rect.anchoredPosition.sqrMagnitude < 9f;
                bool looksLikeWhiteSquare = rawImage.color.a > 0.95f
                    && rawImage.color.r > 0.95f
                    && rawImage.color.g > 0.95f
                    && rawImage.color.b > 0.95f;

                // RawImage artifacts: if they're centered, pure white, and have no texture, they are almost certainly the white box.
                if (centeredAnchor && centeredPosition && looksLikeWhiteSquare)
                {
                    rawImage.enabled = false;
                    Debug.Log($"[MainMenuSceneSetup] Disabled centered white RawImage artifact on {rawImage.gameObject.name}");
                }
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

            SetupButtonText(btn, "START HUNTING", 40);
        }

        private void SetupWalletButton()
        {
            var btn = EnsureMainMenuButton("WalletButton");
            if (btn == null || !btn) return;
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
            Debug.Log("[MainMenuSceneSetup][Trace] WalletButton text+style applied");
        }

        private void SetupSettingsButton()
        {
            var btn = EnsureMainMenuButton("SettingsButton");
            if (btn == null || !btn) return;
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

            SetupButtonText(btn, "SETTINGS", 32);
            Debug.Log("[MainMenuSceneSetup][Trace] SettingsButton text+style applied");
        }

        private void SetupProfileButton()
        {
            var btn = EnsureMainMenuButton("ProfileButton");
            if (btn == null || !btn) return;

            var rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0, -290);
                rect.sizeDelta = new Vector2(550, 90);
            }

            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = Parchment;
            }

            SetupButtonText(btn, "MY PROFILE", 32);
            Debug.Log("[MainMenuSceneSetup][Trace] ProfileButton text+style applied");
        }

        private Transform EnsureMainMenuButton(string buttonName)
        {
            if (transform == null)
            {
                Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') failed: transform is null");
                return null;
            }

            try
            {
                var btn = transform.Find(buttonName);
                if (btn != null && !btn)
                {
                    btn = null;
                }
                if (btn == null)
                {
                    var buttonGO = new GameObject(buttonName);
                    buttonGO.transform.SetParent(transform, false);
                    btn = buttonGO.transform;
                    buttonGO.AddComponent<RectTransform>();
                    buttonGO.AddComponent<Image>();
                    buttonGO.AddComponent<Button>();
                    Debug.Log($"[MainMenuSceneSetup] Created {buttonName} from code");
                }

                if (btn == null || !btn)
                {
                    btn = transform.Find(buttonName);
                }
                if (btn == null || !btn)
                {
                    Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') failed: button transform unresolved after create/find");
                    return null;
                }

                var rect = btn.GetComponent<RectTransform>();
                if (rect == null) rect = btn.gameObject.AddComponent<RectTransform>();
                var image = btn.GetComponent<Image>();
                if (image == null) image = btn.gameObject.AddComponent<Image>();
                var button = btn.GetComponent<Button>();
                if (button == null) button = btn.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;

                // Self-heal label child so SetupButtonText never receives a malformed node.
                var labelTransform = btn.Find("ButtonText");
                if (labelTransform == null)
                {
                    labelTransform = btn.Find("Text");
                }
                if (labelTransform == null)
                {
                    var textGO = new GameObject("Text");
                    textGO.transform.SetParent(btn, false);
                    labelTransform = textGO.transform;
                }

                if (labelTransform == null || !labelTransform)
                {
                    Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') label transform invalid");
                    return btn;
                }

                var labelRect = labelTransform.GetComponent<RectTransform>();
                if (labelRect == null) labelRect = labelTransform.gameObject.AddComponent<RectTransform>();
                var labelTmp = labelTransform.GetComponent<TMP_Text>();
                if (labelTmp == null) labelTmp = labelTransform.gameObject.AddComponent<TextMeshProUGUI>();

                Debug.Log($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') ready | label='{labelTransform.name}' hasTMP={labelTmp != null}");
                return btn;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') exception: {ex}");
                return null;
            }
        }

        private void SetupButtonText(Transform button, string label, int fontSize)
        {
            if (button == null || !button) return;

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
            if (tmpText == null)
            {
                return;
            }

            tmpText.text = label;
            tmpText.fontSize = fontSize;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = DarkBrown;
            tmpText.raycastTarget = false;
            Debug.Log($"[MainMenuSceneSetup][Trace] SetupButtonText applied label='{label}' font={fontSize} on '{button.name}'");
        }

        private void LogButtonState(string buttonName)
        {
            var btn = transform != null ? transform.Find(buttonName) : null;
            var exists = btn != null && btn;
            var active = exists && btn.gameObject.activeInHierarchy;
            var hasImage = exists && btn.GetComponent<Image>() != null;
            var hasButton = exists && btn.GetComponent<Button>() != null;
            var textNode = exists ? btn.Find("Text") : null;
            var hasTmp = textNode != null && textNode.GetComponent<TMP_Text>() != null;
            Debug.Log($"[MainMenuSceneSetup][Trace] {buttonName}: exists={exists} active={active} image={hasImage} button={hasButton} textNode={(textNode != null)} tmp={hasTmp}");
        }
    }
}
