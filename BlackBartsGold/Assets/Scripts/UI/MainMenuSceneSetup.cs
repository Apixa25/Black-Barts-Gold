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
using System.Collections;
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
        private Coroutine _debugOverlayCleanupRoutine;

        private void OnEnable()
        {
            ApplySetup();
            StartDebugOverlayCleanupSweep();
        }

        private void Start()
        {
            ApplySetup();
            StartDebugOverlayCleanupSweep();
        }

        private void OnDisable()
        {
            if (_debugOverlayCleanupRoutine != null)
            {
                StopCoroutine(_debugOverlayCleanupRoutine);
                _debugOverlayCleanupRoutine = null;
            }
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
                DisableDiagnosticsLikeTextOverlays();
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
                var lower = obj.name.ToLowerInvariant();
                if (obj.name == "DebugDiagnosticsPanel" || lower == "diagnosticstext")
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

        private void DisableDiagnosticsLikeTextOverlays()
        {
            var texts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (var text in texts)
            {
                if (text == null || !text.gameObject.activeInHierarchy) continue;
                string content = text.text ?? string.Empty;
                string name = text.gameObject.name.ToLowerInvariant();

                bool looksLikeDiagnosticsName = name.Contains("diagnostic") || name.Contains("debug") || name.Contains("console");
                bool looksLikeDiagnosticsContent = content.Contains("DEVELOPMENT CONSOLE")
                    || content.Contains("DEBUG INFO")
                    || content.Contains("<b>AR:</b>")
                    || content.Contains("<b>GPS:</b>")
                    || content.Contains("Tracking:");

                if (!looksLikeDiagnosticsName && !looksLikeDiagnosticsContent) continue;

                var root = FindDebugOverlayRoot(text.transform);
                if (root != null)
                {
                    root.gameObject.SetActive(false);
                    Debug.Log($"[MainMenuSceneSetup] Disabled diagnostics overlay root: {root.name}");
                }
                else
                {
                    text.gameObject.SetActive(false);
                    Debug.Log($"[MainMenuSceneSetup] Disabled diagnostics text: {text.gameObject.name}");
                }
            }
        }

        private static Transform FindDebugOverlayRoot(Transform leaf)
        {
            if (leaf == null) return null;

            Transform current = leaf;
            for (int i = 0; i < 8 && current != null; i++)
            {
                string lower = current.name.ToLowerInvariant();
                if (lower.Contains("debug") || lower.Contains("diagnostic") || lower.Contains("console"))
                {
                    return current;
                }
                current = current.parent;
            }

            return null;
        }

        private void StartDebugOverlayCleanupSweep()
        {
            if (!isActiveAndEnabled) return;
            if (_debugOverlayCleanupRoutine != null)
            {
                StopCoroutine(_debugOverlayCleanupRoutine);
            }
            _debugOverlayCleanupRoutine = StartCoroutine(DebugOverlayCleanupSweep());
        }

        private IEnumerator DebugOverlayCleanupSweep()
        {
            // Some persistent overlays re-enable a frame after scene load; sweep for a few seconds.
            const float duration = 6f;
            const float interval = 0.25f;
            float endTime = Time.realtimeSinceStartup + duration;

            while (Time.realtimeSinceStartup < endTime && isActiveAndEnabled)
            {
                DisableDebugPanels();
                DisableDiagnosticsLikeTextOverlays();
                yield return new WaitForSeconds(interval);
            }

            _debugOverlayCleanupRoutine = null;
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

            // Avoid emoji glyph fallback squares on device TMP font asset.
            SetupButtonText(btn, "MY WALLET", 32);
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
            var root = transform;
            if (root == null)
            {
                Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') failed: transform is null");
                return null;
            }

            try
            {
                var btn = root.Find(buttonName);
                if (btn != null && !btn) btn = null;

                // Standardize malformed scene leftovers: UI buttons must have RectTransform.
                if (btn != null && btn.GetComponent<RectTransform>() == null)
                {
                    Debug.LogWarning($"[MainMenuSceneSetup][Trace] Rebuilding malformed {buttonName} (missing RectTransform)");
                    if (Application.isPlaying) Destroy(btn.gameObject); else DestroyImmediate(btn.gameObject);
                    btn = null;
                }

                if (btn == null)
                {
                    var buttonGO = new GameObject(
                        buttonName,
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image),
                        typeof(Button)
                    );
                    buttonGO.transform.SetParent(root, false);
                    btn = buttonGO.transform;
                    Debug.Log($"[MainMenuSceneSetup] Created {buttonName} from code");
                }

                if (btn == null || !btn)
                {
                    btn = root.Find(buttonName);
                }
                if (btn == null || !btn)
                {
                    Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') failed: button transform unresolved after create/find");
                    return null;
                }

                if (btn.gameObject == null)
                {
                    Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') failed: button gameObject is null");
                    return null;
                }

                var rect = btn.GetComponent<RectTransform>();
                if (rect == null)
                {
                    Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') failed: RectTransform unresolved");
                    return null;
                }
                var image = btn.GetComponent<Image>() ?? btn.gameObject.AddComponent<Image>();
                var button = btn.GetComponent<Button>() ?? btn.gameObject.AddComponent<Button>();
                if (rect == null || image == null || button == null)
                {
                    Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') failed: required components missing after add");
                    return null;
                }
                button.transition = Selectable.Transition.ColorTint;

                // Self-heal label child so SetupButtonText never receives a malformed node.
                var labelTransform = btn.Find("ButtonText");
                if (labelTransform == null)
                {
                    labelTransform = btn.Find("Text");
                }
                if (labelTransform == null)
                {
                    var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    textGO.transform.SetParent(btn, false);
                    labelTransform = textGO.transform;
                }

                if (labelTransform == null || !labelTransform)
                {
                    Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') label transform invalid");
                    return btn;
                }

                var labelRect = labelTransform.GetComponent<RectTransform>();
                var labelTmp = labelTransform.GetComponent<TMP_Text>();
                if (labelRect == null || labelTmp == null)
                {
                    // Recreate malformed labels instead of layering components onto invalid nodes.
                    if (Application.isPlaying) Destroy(labelTransform.gameObject); else DestroyImmediate(labelTransform.gameObject);
                    var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    textGO.transform.SetParent(btn, false);
                    labelTransform = textGO.transform;
                    labelRect = labelTransform.GetComponent<RectTransform>();
                    labelTmp = labelTransform.GetComponent<TMP_Text>();
                }
                if (labelRect == null || labelTmp == null)
                {
                    Debug.LogError($"[MainMenuSceneSetup][Trace] EnsureMainMenuButton('{buttonName}') failed: label components unresolved");
                    return null;
                }

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
