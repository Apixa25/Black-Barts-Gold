// ============================================================================
// ARHuntSceneSetup.cs
// Black Bart's Gold - ARHunt Scene Setup
// Path: Assets/Scripts/UI/ARHuntSceneSetup.cs
// Last Modified: 2026-01-27 20:30 - Added radar setup and diagnostics
// ============================================================================
// Sets up the AR Hunt scene HUD overlay at runtime.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TMPro;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BlackBartsGold.UI
{
    public class ARHuntSceneSetup : MonoBehaviour
    {
        private readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        private readonly Color SemiTransparentBlack = new Color(0, 0, 0, 0.5f);

        private RectTransform radarRect;
        private int touchLogCount = 0;
        private TextMeshProUGUI _debugDiagnosticsText;
        private float _lastDiagnosticUpdate;
        private const float _diagnosticUpdateInterval = 0.5f;
        
        private void Start()
        {
            Debug.Log("========================================");
            Debug.Log("[ARHuntSceneSetup] START - Setting up AR HUD...");
            Debug.Log($"[ARHuntSceneSetup] GameObject: {gameObject.name}");
            Debug.Log("========================================");
            
            SetupCanvas();
            SetupBackButton();
            SetupCrosshairs();
            SetupRadarPanel();
            SetupDebugPanel();
            VerifyEventSystem();
            SetupDirectTouchHandler();
            SetupEmergencyButton();
            SetupLightship(); // Pokemon GO technology!
            
            Debug.Log("[ARHuntSceneSetup] AR HUD setup complete!");
        }
        
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }
        
        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }
        
        private void Update()
        {
            // Update debug diagnostics panel
            if (_debugDiagnosticsText != null && Time.time - _lastDiagnosticUpdate >= _diagnosticUpdateInterval)
            {
                _lastDiagnosticUpdate = Time.time;
                _debugDiagnosticsText.text = Core.UIManager.GetDiagnosticsString();
            }

            // Debug: Log touches every frame (first 20 touches only to avoid spam)
            var activeTouches = Touch.activeTouches;
            if (activeTouches.Count > 0 && touchLogCount < 20)
            {
                var touch = activeTouches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    touchLogCount++;
                    Debug.Log($"[ARHuntSceneSetup] TOUCH #{touchLogCount} at {touch.screenPosition}");
                    
                    // Check if touch is over radar
                    if (radarRect != null)
                    {
                        Vector2 localPoint;
                        bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            radarRect, touch.screenPosition, null, out localPoint);
                        bool contains = radarRect.rect.Contains(localPoint);
                        Debug.Log($"[ARHuntSceneSetup] Radar check: localPoint={localPoint}, contains={contains}, rect={radarRect.rect}");
                        
                        // Manual click if touch is on radar
                        if (contains)
                        {
                            Debug.Log("[ARHuntSceneSetup] Touch IS on radar - manually triggering click!");
                            OnRadarClicked();
                        }
                    }
                    
                    // Log EventSystem info
                    if (EventSystem.current != null)
                    {
                        var eventData = new PointerEventData(EventSystem.current);
                        eventData.position = touch.screenPosition;
                        var results = new System.Collections.Generic.List<RaycastResult>();
                        EventSystem.current.RaycastAll(eventData, results);
                        Debug.Log($"[ARHuntSceneSetup] EventSystem raycast hit {results.Count} objects");
                        foreach (var r in results)
                        {
                            Debug.Log($"[ARHuntSceneSetup]   - Hit: {r.gameObject.name}");
                        }
                    }
                }
            }
        }

        private void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Render on top of AR
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void SetupBackButton()
        {
            var btn = transform.Find("BackButton");
            if (btn == null) return;

            var rect = btn.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Position in top-left corner
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(30, -50);
                rect.sizeDelta = new Vector2(120, 60);
            }

            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = SemiTransparentBlack;
            }

            // Add text
            var textTransform = btn.Find("Text");
            if (textTransform == null)
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(btn, false);
                textTransform = textGO.transform;
                
                var textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                
                var tmpText = textGO.AddComponent<TextMeshProUGUI>();
                tmpText.text = "← Back";
                tmpText.fontSize = 24;
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.color = Color.white;
            }

            // Wire Back button to exit AR and return to MainMenu
            var button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (Core.UIManager.Instance != null)
                        Core.UIManager.Instance.ExitARHunt();
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                });
            }
        }

        /// <summary>
        /// CODE-ONLY setup for crosshairs and collection size ring.
        /// Loads sprites from Resources/UI (crosshairs.jpg, gold ring.png).
        /// No Unity Editor wiring required - everything built at runtime.
        /// </summary>
        private void SetupCrosshairs()
        {
            // Find or create Crosshairs container
            var crosshairs = transform.Find("Crosshairs");
            if (crosshairs == null)
            {
                crosshairs = new GameObject("Crosshairs").transform;
                crosshairs.SetParent(transform, false);
                crosshairs.gameObject.AddComponent<RectTransform>();
                crosshairs.gameObject.AddComponent<CrosshairsController>();
                Debug.Log("[ARHuntSceneSetup] Created Crosshairs from code");
            }

            var rect = crosshairs.GetComponent<RectTransform>();
            if (rect == null)
            {
                Debug.LogError("[ARHuntSceneSetup] Crosshairs has no RectTransform!");
                return;
            }

            // Center of screen
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(120, 120);

            // Load crosshairs sprite from Resources/UI
            // Expected: Assets/Resources/UI/crosshairs.jpg and Assets/Resources/UI/gold ring.png
            var crosshairsSprite = Resources.Load<Sprite>("UI/crosshairs");
            if (crosshairsSprite == null) crosshairsSprite = Resources.Load<Sprite>("crosshairs");
            if (crosshairsSprite == null)
            {
                Debug.LogWarning("[ARHuntSceneSetup] crosshairs.jpg not in Resources/UI - using programmatic fallback");
            }

            // Main crosshairs Image (on parent - used by CrosshairsController for color/state)
            var image = crosshairs.GetComponent<Image>();
            if (image == null) image = crosshairs.gameObject.AddComponent<Image>();
            image.sprite = crosshairsSprite;
            image.color = new Color(1f, 0.84f, 0f, 0.9f);
            image.raycastTarget = false;
            image.preserveAspect = crosshairsSprite != null;
            // No sprite = gold square fallback (better than white or nothing)

            // Remove old CrosshairText (font doesn't support ⊕) - we use sprite now
            var oldText = crosshairs.Find("CrosshairText");
            if (oldText != null)
            {
                Destroy(oldText.gameObject);
            }

            // Create CollectionSizeCircle (gold ring) - shows when targeting coin
            var collectionCircle = crosshairs.Find("CollectionSizeCircle");
            if (collectionCircle == null)
            {
                var circleGO = new GameObject("CollectionSizeCircle");
                circleGO.transform.SetParent(crosshairs, false);
                collectionCircle = circleGO.transform;

                var circleRect = circleGO.AddComponent<RectTransform>();
                circleRect.anchorMin = new Vector2(0.5f, 0.5f);
                circleRect.anchorMax = new Vector2(0.5f, 0.5f);
                circleRect.pivot = new Vector2(0.5f, 0.5f);
                circleRect.anchoredPosition = Vector2.zero;
                circleRect.sizeDelta = new Vector2(80, 80);

                var circleImage = circleGO.AddComponent<Image>();
                var goldRingSprite = Resources.Load<Sprite>("UI/gold ring");
                if (goldRingSprite == null) goldRingSprite = Resources.Load<Sprite>("gold ring");
                circleImage.sprite = goldRingSprite;
                circleImage.color = new Color(1f, 0.84f, 0f, 0.7f);
                circleImage.raycastTarget = false;
                circleImage.preserveAspect = true;
                Debug.Log("[ARHuntSceneSetup] Created CollectionSizeCircle from code");
            }
            else
            {
                // Ensure existing circle has sprite and settings
                var circleImage = collectionCircle.GetComponent<Image>();
                if (circleImage != null)
                {
                    if (circleImage.sprite == null)
                    {
                        var goldRingSprite = Resources.Load<Sprite>("UI/gold ring");
                        if (goldRingSprite == null) goldRingSprite = Resources.Load<Sprite>("gold ring");
                        circleImage.sprite = goldRingSprite;
                    }
                    circleImage.raycastTarget = false;
                }
            }

            // Wire CrosshairsController references at runtime
            var controller = crosshairs.GetComponent<CrosshairsController>();
            if (controller != null)
            {
                var circleImg = collectionCircle.GetComponent<Image>();
                controller.SetRuntimeReferences(image, circleImg);
            }
        }
        
        /// <summary>
        /// Setup RadarPanel for click detection.
        /// This ensures the radar can be tapped to open the full map.
        /// </summary>
        private void SetupRadarPanel()
        {
            Debug.Log("[ARHuntSceneSetup] Setting up RadarPanel...");
            
            var radar = transform.Find("RadarPanel");
            if (radar == null)
            {
                Debug.LogWarning("[ARHuntSceneSetup] RadarPanel not found!");
                return;
            }
            
            Debug.Log($"[ARHuntSceneSetup] Found RadarPanel: {radar.name}");
            
            // Get or add RectTransform and store for touch detection
            var rect = radar.GetComponent<RectTransform>();
            radarRect = rect; // Store for Update() touch detection
            if (rect != null)
            {
                // Position in bottom-right corner with safe margin
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = new Vector2(-20, 20);
                rect.sizeDelta = new Vector2(180, 180);
                Debug.Log($"[ARHuntSceneSetup] RadarPanel positioned: anchor BR, pos (-20, 20), size 180x180");
            }
            
            // CRITICAL: Ensure there's an Image with raycastTarget = true
            var image = radar.GetComponent<Image>();
            if (image == null)
            {
                image = radar.gameObject.AddComponent<Image>();
                Debug.Log("[ARHuntSceneSetup] Added Image to RadarPanel");
            }
            image.raycastTarget = true;
            Debug.Log($"[ARHuntSceneSetup] RadarPanel Image raycastTarget: {image.raycastTarget}");
            
            // CRITICAL: Ensure there's a Button component
            var button = radar.GetComponent<Button>();
            if (button == null)
            {
                button = radar.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                Debug.Log("[ARHuntSceneSetup] Added Button to RadarPanel");
            }
            
            // Wire up button to open full map
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnRadarClicked);
            Debug.Log("[ARHuntSceneSetup] RadarPanel button click handler registered");
            
            // Wire RadarUI and create radar content (player dot, coin sprite) - code-only
            var radarUI = radar.GetComponent<RadarUI>();
            if (radarUI != null)
            {
                SetupRadarContent(radar, radarUI);
                Debug.Log("[ARHuntSceneSetup] RadarUI wired with code-based setup");
            }
            else
            {
                Debug.LogWarning("[ARHuntSceneSetup] RadarUI component NOT found on RadarPanel!");
            }
        }

        /// <summary>
        /// Create radar content (player dot, sweep line) and wire RadarUI at runtime.
        /// Uses player.png and map-coin-icon.png from Resources/UI.
        /// </summary>
        private void SetupRadarContent(Transform radar, RadarUI radarUI)
        {
            var rect = radar.GetComponent<RectTransform>();
            if (rect == null) return;

            // Player dot at center
            var playerDot = radar.Find("PlayerDot");
            if (playerDot == null)
            {
                var playerGO = new GameObject("PlayerDot");
                playerDot = playerGO.transform;
                playerDot.SetParent(radar, false);
                var playerRect = playerGO.AddComponent<RectTransform>();
                playerRect.anchorMin = new Vector2(0.5f, 0.5f);
                playerRect.anchorMax = new Vector2(0.5f, 0.5f);
                playerRect.pivot = new Vector2(0.5f, 0.5f);
                playerRect.anchoredPosition = Vector2.zero;
                playerRect.sizeDelta = new Vector2(24, 24);
                var playerImg = playerGO.AddComponent<Image>();
                var playerSprite = Resources.Load<Sprite>("UI/player");
                if (playerSprite == null) playerSprite = Resources.Load<Sprite>("player");
                playerImg.sprite = playerSprite;
                playerImg.color = Color.white;
                playerImg.raycastTarget = false;
                playerImg.preserveAspect = true;
            }

            // Sweep line (optional - thin rotating line)
            var sweepLine = radar.Find("SweepLine");
            if (sweepLine == null)
            {
                var sweepGO = new GameObject("SweepLine");
                sweepLine = sweepGO.transform;
                sweepLine.SetParent(radar, false);
                var sweepRect = sweepGO.AddComponent<RectTransform>();
                sweepRect.anchorMin = new Vector2(0.5f, 0.5f);
                sweepRect.anchorMax = new Vector2(0.5f, 0.5f);
                sweepRect.pivot = new Vector2(0.5f, 0f);
                sweepRect.anchoredPosition = Vector2.zero;
                sweepRect.sizeDelta = new Vector2(4, 70);
                var sweepImg = sweepGO.AddComponent<Image>();
                sweepImg.color = new Color(1f, 0.84f, 0f, 0.4f);
                sweepImg.raycastTarget = false;
            }

            // North indicator (optional)
            var northIndicator = radar.Find("NorthIndicator");
            if (northIndicator == null)
            {
                var northGO = new GameObject("NorthIndicator");
                northIndicator = northGO.transform;
                northIndicator.SetParent(radar, false);
                var northRect = northGO.AddComponent<RectTransform>();
                northRect.anchorMin = new Vector2(0.5f, 1f);
                northRect.anchorMax = new Vector2(0.5f, 1f);
                northRect.pivot = new Vector2(0.5f, 1f);
                northRect.anchoredPosition = new Vector2(0, -10);
                northRect.sizeDelta = new Vector2(8, 12);
                var northImg = northGO.AddComponent<Image>();
                northImg.color = new Color(1f, 0.3f, 0.3f, 0.8f);
                northImg.raycastTarget = false;
            }

            var coinSprite = Resources.Load<Sprite>("UI/map-coin-icon");
            if (coinSprite == null) coinSprite = Resources.Load<Sprite>("map-coin-icon");
            radarUI.SetRuntimeReferences(rect, playerDot.GetComponent<RectTransform>(), sweepLine.GetComponent<RectTransform>(), northIndicator.GetComponent<RectTransform>(), coinSprite);
        }

        /// <summary>
        /// Create debug diagnostics panel (bottom-left) - same as UIManager's AR overlay.
        /// Shows AR status, GPS, planes, coins. Code-only, no Editor wiring.
        /// </summary>
        private void SetupDebugPanel()
        {
            var existing = transform.Find("DebugDiagnosticsPanel");
            if (existing != null)
            {
                _debugDiagnosticsText = existing.GetComponentInChildren<TextMeshProUGUI>();
                return;
            }

            var debugPanel = new GameObject("DebugDiagnosticsPanel");
            debugPanel.transform.SetParent(transform, false);
            var panelRect = debugPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = new Vector2(20, 20);
            panelRect.sizeDelta = new Vector2(400, 320);

            var bgImage = debugPanel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);
            bgImage.raycastTarget = false;

            var titleGO = new GameObject("DebugTitle");
            titleGO.transform.SetParent(debugPanel.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "🔧 DEBUG INFO";
            titleText.fontSize = 28;
            titleText.color = GoldColor;
            titleText.alignment = TextAlignmentOptions.Center;

            var diagGO = new GameObject("DiagnosticsText");
            diagGO.transform.SetParent(debugPanel.transform, false);
            var diagRect = diagGO.AddComponent<RectTransform>();
            diagRect.anchorMin = new Vector2(0, 0);
            diagRect.anchorMax = new Vector2(1, 1);
            diagRect.pivot = new Vector2(0, 1);
            diagRect.anchoredPosition = new Vector2(15, -55);
            diagRect.sizeDelta = new Vector2(-30, -70);
            _debugDiagnosticsText = diagGO.AddComponent<TextMeshProUGUI>();
            _debugDiagnosticsText.text = "Loading...";
            _debugDiagnosticsText.fontSize = 20;
            _debugDiagnosticsText.color = Color.white;
            _debugDiagnosticsText.alignment = TextAlignmentOptions.TopLeft;
            _debugDiagnosticsText.enableWordWrapping = true;
            _debugDiagnosticsText.richText = true;

            Debug.Log("[ARHuntSceneSetup] Debug diagnostics panel created");
        }
        
        /// <summary>
        /// Called when radar is clicked - opens full map.
        /// This is a backup handler in case RadarUI's handler isn't working.
        /// </summary>
        private void OnRadarClicked()
        {
            Debug.Log("[ARHuntSceneSetup] RADAR CLICKED! Opening full map...");
            
            // Try FullMapUI first
            if (FullMapUI.Exists)
            {
                Debug.Log("[ARHuntSceneSetup] Calling FullMapUI.Show()");
                FullMapUI.Instance.Show();
            }
            // Then try ARHUD
            else if (ARHUD.Instance != null)
            {
                Debug.Log("[ARHuntSceneSetup] Calling ARHUD.OnRadarTapped()");
                ARHUD.Instance.OnRadarTapped();
            }
            else
            {
                Debug.LogError("[ARHuntSceneSetup] Neither FullMapUI nor ARHUD found!");
            }
        }
        
        /// <summary>
        /// Setup the emergency map button - guaranteed to work if scripts are running.
        /// This uses OnGUI which bypasses Canvas/EventSystem entirely.
        /// </summary>
        private void SetupEmergencyButton()
        {
            Debug.Log("[ARHuntSceneSetup] Creating EmergencyMapButton...");
            EmergencyMapButton.EnsureExists();
            Debug.Log("[ARHuntSceneSetup] EmergencyMapButton created!");
        }
        
        /// <summary>
        /// Setup Niantic Lightship for Pokemon GO-style AR features.
        /// Enables occlusion, meshing, semantics, and depth.
        /// </summary>
        private void SetupLightship()
        {
            Debug.Log("[ARHuntSceneSetup] Setting up Niantic Lightship (Pokemon GO technology)...");
            
            // Check if LightshipManager already exists
            var existing = FindFirstObjectByType<BlackBartsGold.AR.LightshipManager>();
            if (existing != null)
            {
                Debug.Log("[ARHuntSceneSetup] LightshipManager already exists");
                return;
            }
            
            // Create LightshipManager
            var lightshipGO = new GameObject("LightshipManager");
            lightshipGO.AddComponent<BlackBartsGold.AR.LightshipManager>();
            
            Debug.Log("[ARHuntSceneSetup] LightshipManager created - Pokemon GO features enabled!");
            Debug.Log("  - Occlusion: Coins hide behind real objects");
            Debug.Log("  - Meshing: Coins sit on real surfaces");
            Debug.Log("  - Semantics: Sky/ground detection");
            Debug.Log("  - Depth: Better AR placement");
        }
        
        /// <summary>
        /// Setup the DirectTouchHandler for Pokemon GO style touch detection.
        /// This bypasses Unity's EventSystem entirely for maximum reliability.
        /// </summary>
        private void SetupDirectTouchHandler()
        {
            Debug.Log("[ARHuntSceneSetup] Setting up DirectTouchHandler...");
            
            // Check if already exists
            var existing = GetComponent<DirectTouchHandler>();
            if (existing != null)
            {
                Debug.Log("[ARHuntSceneSetup] DirectTouchHandler already exists");
                return;
            }
            
            // Add the component
            var handler = gameObject.AddComponent<DirectTouchHandler>();
            Debug.Log("[ARHuntSceneSetup] Added DirectTouchHandler component!");
        }
        
        /// <summary>
        /// Verify the EventSystem is properly set up for UI input.
        /// </summary>
        private void VerifyEventSystem()
        {
            Debug.Log("[ARHuntSceneSetup] Verifying EventSystem...");
            
            // Check for EventSystem
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = FindFirstObjectByType<EventSystem>();
                if (eventSystem == null)
                {
                    Debug.LogError("[ARHuntSceneSetup] NO EventSystem found! Creating one...");
                    var esGO = new GameObject("EventSystem_Runtime");
                    eventSystem = esGO.AddComponent<EventSystem>();
                    esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    Debug.Log("[ARHuntSceneSetup] Created EventSystem at runtime");
                }
            }
            Debug.Log($"[ARHuntSceneSetup] EventSystem: {eventSystem.name}, enabled: {eventSystem.enabled}");
            
            // Check for InputModule (using new Input System)
            var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule != null)
            {
                Debug.Log($"[ARHuntSceneSetup] InputSystemUIInputModule found, enabled: {inputModule.enabled}");
            }
            else
            {
                // Check if any BaseInputModule exists
                var baseInputModule = eventSystem.GetComponent<BaseInputModule>();
                if (baseInputModule != null)
                {
                    Debug.Log($"[ARHuntSceneSetup] BaseInputModule found: {baseInputModule.GetType().Name}");
                }
                else
                {
                    Debug.LogWarning("[ARHuntSceneSetup] No input module found! Adding InputSystemUIInputModule...");
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }
            
            // Check for GraphicRaycaster on this canvas
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[ARHuntSceneSetup] Canvas render mode: {canvas.renderMode}");
                
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.LogWarning("[ARHuntSceneSetup] Added GraphicRaycaster to Canvas");
                }
                Debug.Log($"[ARHuntSceneSetup] GraphicRaycaster enabled: {raycaster.enabled}");
            }
        }
    }
}
