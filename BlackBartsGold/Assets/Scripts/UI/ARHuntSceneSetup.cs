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
using System.Collections;
using BlackBartsGold.Location;
using BlackBartsGold.Core;
using BlackBartsGold.Utils;
using BlackBartsGold.AR;

namespace BlackBartsGold.UI
{
    [DefaultExecutionOrder(-100)] // Run before ARHUD so panels exist when InitializeRuntimeReferences is called
    public class ARHuntSceneSetup : MonoBehaviour
    {
        private readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        private readonly Color SemiTransparentBlack = new Color(0, 0, 0, 0.5f);

        private TextMeshProUGUI _debugDiagnosticsText;
        private RawImage _radarMapTileImage;
        private float _radarMapLastUpdate;
        private double _radarMapLastLat, _radarMapLastLng;
        private bool _radarMapUpdatePending;
        private Texture2D _radarMapCurrentTile;
        private bool _radarMapTileIsOurCopy;
        private float _lastDiagnosticUpdate;
        private const float _diagnosticUpdateInterval = 0.5f;
        private int _radarZoom = 19; // 19 = default (3 levels closer); 21 = zoomed in when hunting
        private Sprite _cachedMapCoinIconSprite;
        private bool _mapCoinIconLoadLogged = false;
        
        private void Start()
        {
            DiagnosticLog.Log("Setup", $"AR SCENE START T+{Time.realtimeSinceStartup:F2}s");
            DiagnosticLog.Log("Setup", $"GameObject: {gameObject.name}");
            
            // Single source of truth: remove scene-based FullMapPanel - we use UIManager's code-based map only
            var fullMap = transform.Find("FullMapPanel");
            if (fullMap != null)
            {
                Destroy(fullMap.gameObject);
                DiagnosticLog.Log("Setup", "Removed scene-based FullMapPanel - using code-based map only");
            }
            
            SetupCanvas();
            CleanupStrayCenteredImages(); // Remove white square from orphan CompassArrowPanel etc.
            SetupBackButton();
            SetupCrosshairs();
            SetupRadarPanel();
            SetupDebugPanel();
            SetupMessagePanel();
            SetupLockedPopup();
            SetupCollectionPopup();
            SetupCoinInfoPanel();
            SetupCompassPanel();
            SetupGasMeterPanel();
            SetupFindLimitPanel();
            SetupDirectionIndicatorPanel();
            VerifyEventSystem();
            SetupLightship(); // Pokemon GO technology!
            
            // Subscribe to hunt mode - zoom radar in when coin selected
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnHuntModeChanged += OnHuntModeChanged;
            }

            // Wire ARHUD to code-created panels (must run after all Setup* methods)
            var arhud = GetComponentInChildren<ARHUD>(true);
            if (arhud != null)
            {
                arhud.InitializeRuntimeReferences(transform);
                DiagnosticLog.Log("Setup", "ARHUD runtime references initialized");
            }
            else
            {
                DiagnosticLog.Warn("Setup", "ARHUD not found - panels may not work");
            }
            
            DiagnosticLog.Log("Setup", "AR HUD setup COMPLETE");
        }
        
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }
        
        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void OnDestroy()
        {
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnHuntModeChanged -= OnHuntModeChanged;
            }
            if (_radarMapCurrentTile != null && _radarMapTileIsOurCopy)
                Destroy(_radarMapCurrentTile);
        }
        
        private void OnHuntModeChanged(HuntMode mode)
        {
            _radarZoom = (mode == HuntMode.Hunting || mode == HuntMode.Collecting) ? 21 : 19;
            _radarMapUpdatePending = false;
            _radarMapLastUpdate = -999f; // Force immediate refresh on next Update
        }
        
        private void Update()
        {
            // Update radar map tile from Mapbox
            UpdateRadarMapTile();

            // Update debug diagnostics panel
            if (_debugDiagnosticsText != null && Time.time - _lastDiagnosticUpdate >= _diagnosticUpdateInterval)
            {
                _lastDiagnosticUpdate = Time.time;
                _debugDiagnosticsText.text = Core.UIManager.GetDiagnosticsString();
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

        /// <summary>
        /// Remove stray scene-baked UI that causes a white square at screen center.
        /// CompassArrowPanel has an Image with no sprite (renders white). Not used by code-based CompassPanel.
        /// </summary>
        private void CleanupStrayCenteredImages()
        {
            var compassArrow = transform.Find("CompassArrowPanel");
            if (compassArrow != null)
            {
                var img = compassArrow.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = false;
                    Destroy(img);
                    DiagnosticLog.Log("Setup", "Disabled orphan CompassArrowPanel Image (was causing white square)");
                }
                // Deactivate entire panel - it's unused (CompassPanel is created in code)
                compassArrow.gameObject.SetActive(false);
            }
        }

        private void SetupBackButton()
        {
            var btn = transform.Find("BackButton");
            if (btn == null)
            {
                // Create BackButton from code for fully code-based setup
                var btnGO = new GameObject("BackButton");
                btnGO.transform.SetParent(transform, false);
                btn = btnGO.transform;
                btnGO.AddComponent<RectTransform>();
                btnGO.AddComponent<Image>();
                btnGO.AddComponent<Button>();
                DiagnosticLog.Log("Setup", "Created BackButton from code");
            }

            var rect = btn.GetComponent<RectTransform>();
            if (rect == null) rect = btn.gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(30, -50);
            rect.sizeDelta = new Vector2(120, 60);

            var image = btn.GetComponent<Image>();
            if (image == null) image = btn.gameObject.AddComponent<Image>();
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
            if (button == null) button = btn.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                DiagnosticLog.Log("BackButton", "Tapped - exiting AR");
                if (Core.UIManager.Instance != null)
                    Core.UIManager.Instance.ExitARHunt();
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            });
            DiagnosticLog.Log("Setup", "BackButton wired");
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

            // Main crosshairs Image - REMOVED: Crosshairs cover the coin - we want players to see the beautiful coin!
            // Gold ring (CollectionSizeCircle) still shows when in range - that stays visible.
            var image = crosshairs.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = false;  // Hide immediately (prevents white square from broken/missing sprite)
                Destroy(image);
                image = null;
            }
            // Don't add Image - we intentionally have no crosshairs visual

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
            DiagnosticLog.Log("Setup", "Setting up RadarPanel...");
            
            var radar = transform.Find("RadarPanel");
            if (radar == null)
            {
                // Create RadarPanel from code for fully code-based setup
                var radarGO = new GameObject("RadarPanel");
                radarGO.transform.SetParent(transform, false);
                radar = radarGO.transform;
                radarGO.AddComponent<RectTransform>();
                radarGO.AddComponent<RadarUI>();
                DiagnosticLog.Log("Setup", "Created RadarPanel from code");
            }
            else
            {
                DiagnosticLog.Log("Setup", $"Found RadarPanel: {radar.name}");
            }
            
            // Get or add RectTransform
            var rect = radar.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Position in top-right corner with safe margin (below status bar)
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-20, -20);
                rect.sizeDelta = new Vector2(360, 360); // 2x larger for better visibility
                Debug.Log($"[ARHuntSceneSetup] RadarPanel positioned: anchor TR, pos (-20, -20), size 360x360");
            }
            
            // CRITICAL: Ensure there's an Image with raycastTarget = true
            var image = radar.GetComponent<Image>();
            if (image == null)
            {
                image = radar.gameObject.AddComponent<Image>();
                Debug.Log("[ARHuntSceneSetup] Added Image to RadarPanel");
            }
            image.raycastTarget = true;
            image.color = new Color(1f, 1f, 1f, 0.01f); // Nearly invisible so map tile shows through
            Debug.Log($"[ARHuntSceneSetup] RadarPanel Image raycastTarget: {image.raycastTarget}");
            
            // CRITICAL: Ensure there's a Button component
            var button = radar.GetComponent<Button>();
            if (button == null)
            {
                button = radar.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                Debug.Log("[ARHuntSceneSetup] Added Button to RadarPanel");
            }
            
            // Radar click ownership lives in RadarUI.SetupRadarTap() to avoid duplicate handlers.
            button.onClick.RemoveAllListeners();
            
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
        /// Create radar content (map tile, player dot, sweep line) and wire RadarUI at runtime.
        /// Uses player.png and map-coin-icon.png from Resources/UI.
        /// Map tile from Mapbox shows real streets behind the radar overlay.
        /// </summary>
        private void SetupRadarContent(Transform radar, RadarUI radarUI)
        {
            var rect = radar.GetComponent<RectTransform>();
            if (rect == null)
            {
                Debug.LogError("[ARHuntSceneSetup] Radar has no RectTransform!");
                return;
            }

            RectTransform playerRect = null;
            RectTransform sweepRect = null;
            RectTransform northRect = null;

            // === MAP TILE (Mapbox) - First child, behind everything ===
            var mapTile = radar.Find("MapTile");
            RawImage mapTileImage = null;
            if (mapTile == null || !mapTile.gameObject)
            {
                var mapTileGO = new GameObject("MapTile");
                mapTileGO.transform.SetParent(radar, false);
                mapTileGO.transform.SetAsFirstSibling(); // Behind player, sweep, north
                var mapTileRect = mapTileGO.AddComponent<RectTransform>();
                mapTileRect.anchorMin = Vector2.zero;
                mapTileRect.anchorMax = Vector2.one;
                mapTileRect.offsetMin = Vector2.zero;
                mapTileRect.offsetMax = Vector2.zero;
                mapTileImage = mapTileGO.AddComponent<RawImage>();
                mapTileImage.color = new Color(0.15f, 0.2f, 0.25f, 0.95f); // Dark placeholder while loading
                mapTileImage.raycastTarget = false;
                Debug.Log("[ARHuntSceneSetup] Created MapTile RawImage for radar");
            }
            else
            {
                mapTileImage = mapTile.GetComponent<RawImage>();
                if (mapTileImage == null) mapTileImage = mapTile.gameObject.AddComponent<RawImage>();
            }

            _radarMapTileImage = mapTileImage;
            EnsureMapboxService();
            Debug.Log("[ARHuntSceneSetup] Map tile wired - will fetch from Mapbox");

            // Player dot at center
            var playerDot = radar.Find("PlayerDot");
            if (playerDot == null || !playerDot.gameObject)
            {
                var playerGO = new GameObject("PlayerDot");
                playerGO.transform.SetParent(radar, false);
                playerRect = playerGO.AddComponent<RectTransform>();
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
            else
            {
                playerRect = playerDot.GetComponent<RectTransform>();
                if (playerRect == null) playerRect = playerDot.gameObject.AddComponent<RectTransform>();
            }

            // Sweep line (optional - thin rotating line)
            var sweepLine = radar.Find("SweepLine");
            if (sweepLine == null || !sweepLine.gameObject)
            {
                var sweepGO = new GameObject("SweepLine");
                sweepGO.transform.SetParent(radar, false);
                sweepRect = sweepGO.AddComponent<RectTransform>();
                sweepRect.anchorMin = new Vector2(0.5f, 0.5f);
                sweepRect.anchorMax = new Vector2(0.5f, 0.5f);
                sweepRect.pivot = new Vector2(0.5f, 0f);
                sweepRect.anchoredPosition = Vector2.zero;
                sweepRect.sizeDelta = new Vector2(4, 70);
                var sweepImg = sweepGO.AddComponent<Image>();
                sweepImg.color = new Color(1f, 0.84f, 0f, 0.4f);
                sweepImg.raycastTarget = false;
            }
            else
            {
                sweepRect = sweepLine.GetComponent<RectTransform>();
                if (sweepRect == null) sweepRect = sweepLine.gameObject.AddComponent<RectTransform>();
            }

            // North indicator (optional)
            var northIndicator = radar.Find("NorthIndicator");
            if (northIndicator == null || !northIndicator.gameObject)
            {
                var northGO = new GameObject("NorthIndicator");
                northGO.transform.SetParent(radar, false);
                northRect = northGO.AddComponent<RectTransform>();
                northRect.anchorMin = new Vector2(0.5f, 1f);
                northRect.anchorMax = new Vector2(0.5f, 1f);
                northRect.pivot = new Vector2(0.5f, 1f);
                northRect.anchoredPosition = new Vector2(0, -10);
                northRect.sizeDelta = new Vector2(8, 12);
                var northImg = northGO.AddComponent<Image>();
                northImg.color = new Color(1f, 0.3f, 0.3f, 0.8f);
                northImg.raycastTarget = false;
            }
            else
            {
                northRect = northIndicator.GetComponent<RectTransform>();
                if (northRect == null) northRect = northIndicator.gameObject.AddComponent<RectTransform>();
            }

            if (playerRect == null)
            {
                Debug.LogError("[ARHuntSceneSetup] Failed to get player dot RectTransform!");
                return;
            }

            var coinSprite = GetMapCoinIconSprite();
            radarUI.SetRuntimeReferences(rect, playerRect, sweepRect, northRect, coinSprite);
            Debug.Log("[ARHuntSceneSetup] Radar content wired successfully");
        }
        
        /// <summary>
        /// Resolve map-coin-icon from Resources with a Texture2D fallback.
        /// Handles projects where import mode is Texture2D instead of Sprite.
        /// </summary>
        private Sprite GetMapCoinIconSprite()
        {
            if (_cachedMapCoinIconSprite != null)
                return _cachedMapCoinIconSprite;
            
            var sprite = Resources.Load<Sprite>("UI/map-coin-icon") ?? Resources.Load<Sprite>("map-coin-icon");
            if (sprite != null)
            {
                _cachedMapCoinIconSprite = sprite;
                if (!_mapCoinIconLoadLogged)
                {
                    _mapCoinIconLoadLogged = true;
                    Debug.Log($"[ARHuntSceneSetup][MapIcon] Loaded as Sprite: {_cachedMapCoinIconSprite.texture.width}x{_cachedMapCoinIconSprite.texture.height}");
                }
                return _cachedMapCoinIconSprite;
            }
            
            var tex = Resources.Load<Texture2D>("UI/map-coin-icon") ?? Resources.Load<Texture2D>("map-coin-icon");
            if (tex != null)
            {
                _cachedMapCoinIconSprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                if (!_mapCoinIconLoadLogged)
                {
                    _mapCoinIconLoadLogged = true;
                    Debug.Log($"[ARHuntSceneSetup][MapIcon] Loaded via Texture2D fallback: {tex.width}x{tex.height}");
                }
                return _cachedMapCoinIconSprite;
            }
            
            if (!_mapCoinIconLoadLogged)
            {
                _mapCoinIconLoadLogged = true;
                Debug.LogWarning("[ARHuntSceneSetup][MapIcon] map-coin-icon not found in Resources (Sprite/Texture2D)");
            }
            return null;
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
            panelRect.sizeDelta = new Vector2(800, 640); // 2x larger for easier reading

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
            titleText.text = "DEBUG INFO";
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
            _debugDiagnosticsText.fontSize = 40; // 2x larger for easier reading
            _debugDiagnosticsText.color = Color.white;
            _debugDiagnosticsText.alignment = TextAlignmentOptions.TopLeft;
            _debugDiagnosticsText.enableWordWrapping = true;
            _debugDiagnosticsText.richText = true;

            Debug.Log("[ARHuntSceneSetup] Debug diagnostics panel created");
        }

        /// <summary>
        /// Create MessagePanel for ARHUD.ShowMessage() - code-only, no Inspector wiring.
        /// Center-bottom placement. ARHUD finds via transform.Find("MessagePanel").
        /// </summary>
        private void SetupMessagePanel()
        {
            var existing = transform.Find("MessagePanel");
            if (existing != null) return;

            var panel = new GameObject("MessagePanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 120);
            panelRect.sizeDelta = new Vector2(600, 80);

            var cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            bgImage.raycastTarget = false;

            var textGO = new GameObject("MessageText");
            textGO.transform.SetParent(panel.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 10);
            textRect.offsetMax = new Vector2(-15, -10);
            var tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.text = "";
            tmpText.fontSize = 28;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.enableWordWrapping = true;

            panel.SetActive(true);
            DiagnosticLog.Log("Setup", "MessagePanel created");
        }

        /// <summary>
        /// Create LockedPopup for ARHUD.ShowLockedPopup() - code-only, no Inspector wiring.
        /// Center of screen. ARHUD finds via transform.Find("LockedPopup").
        /// </summary>
        private void SetupLockedPopup()
        {
            var existing = transform.Find("LockedPopup");
            if (existing != null) return;

            var panel = new GameObject("LockedPopup");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 200);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            bgImage.raycastTarget = true;

            var valueGO = new GameObject("LockedValueText");
            valueGO.transform.SetParent(panel.transform, false);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0.7f);
            valueRect.anchorMax = new Vector2(0.5f, 0.7f);
            valueRect.pivot = new Vector2(0.5f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = new Vector2(360, 50);
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "$0.00";
            valueText.fontSize = 36;
            valueText.color = GoldColor;
            valueText.alignment = TextAlignmentOptions.Center;

            var msgGO = new GameObject("LockedMessageText");
            msgGO.transform.SetParent(panel.transform, false);
            var msgRect = msgGO.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.5f, 0.3f);
            msgRect.anchorMax = new Vector2(0.5f, 0.3f);
            msgRect.pivot = new Vector2(0.5f, 0.5f);
            msgRect.anchoredPosition = Vector2.zero;
            msgRect.sizeDelta = new Vector2(360, 80);
            var msgText = msgGO.AddComponent<TextMeshProUGUI>();
            msgText.text = "";
            msgText.fontSize = 24;
            msgText.color = Color.white;
            msgText.alignment = TextAlignmentOptions.Center;
            msgText.enableWordWrapping = true;

            panel.SetActive(false);
            DiagnosticLog.Log("Setup", "LockedPopup created");
        }

        /// <summary>
        /// Create CollectionPopup for ARHUD.ShowCollectionPopup() - code-only, no Inspector wiring.
        /// Center of screen. ARHUD finds via transform.Find("CollectionPopup").
        /// </summary>
        private void SetupCollectionPopup()
        {
            var existing = transform.Find("CollectionPopup");
            if (existing != null) return;

            var panel = new GameObject("CollectionPopup");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(350, 150);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.2f, 0.1f, 0.95f);
            bgImage.raycastTarget = false;

            var valueGO = new GameObject("CollectionValueText");
            valueGO.transform.SetParent(panel.transform, false);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0.65f);
            valueRect.anchorMax = new Vector2(0.5f, 0.65f);
            valueRect.pivot = new Vector2(0.5f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = new Vector2(320, 50);
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "+$0.00";
            valueText.fontSize = 40;
            valueText.color = GoldColor;
            valueText.alignment = TextAlignmentOptions.Center;

            var msgGO = new GameObject("CollectionMessageText");
            msgGO.transform.SetParent(panel.transform, false);
            var msgRect = msgGO.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.5f, 0.3f);
            msgRect.anchorMax = new Vector2(0.5f, 0.3f);
            msgRect.pivot = new Vector2(0.5f, 0.5f);
            msgRect.anchoredPosition = Vector2.zero;
            msgRect.sizeDelta = new Vector2(320, 40);
            var msgText = msgGO.AddComponent<TextMeshProUGUI>();
            msgText.text = "Treasure collected!";
            msgText.fontSize = 24;
            msgText.color = Color.white;
            msgText.alignment = TextAlignmentOptions.Center;

            panel.SetActive(false);
            DiagnosticLog.Log("Setup", "CollectionPopup created");
        }

        /// <summary>
        /// Create CoinInfoPanel for ARHUD.ShowCoinInfo() - code-only, no Inspector wiring.
        /// Top-center, below radar. ARHUD finds via transform.Find("CoinInfoPanel").
        /// </summary>
        private void SetupCoinInfoPanel()
        {
            var existing = transform.Find("CoinInfoPanel");
            if (existing != null) return;

            var panel = new GameObject("CoinInfoPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1);
            panelRect.anchorMax = new Vector2(0.5f, 1);
            panelRect.pivot = new Vector2(0.5f, 1);
            panelRect.anchoredPosition = new Vector2(0, -420);
            panelRect.sizeDelta = new Vector2(320, 100);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.75f);
            bgImage.raycastTarget = false;

            var valueGO = new GameObject("CoinValueText");
            valueGO.transform.SetParent(panel.transform, false);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 0.6f);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.offsetMin = new Vector2(10, 5);
            valueRect.offsetMax = new Vector2(-10, -5);
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "$0.00";
            valueText.fontSize = 32;
            valueText.color = GoldColor;
            valueText.alignment = TextAlignmentOptions.Center;

            var distGO = new GameObject("CoinDistanceText");
            distGO.transform.SetParent(panel.transform, false);
            var distRect = distGO.AddComponent<RectTransform>();
            distRect.anchorMin = new Vector2(0, 0.25f);
            distRect.anchorMax = new Vector2(1, 0.6f);
            distRect.offsetMin = new Vector2(10, 2);
            distRect.offsetMax = new Vector2(-10, -2);
            var distText = distGO.AddComponent<TextMeshProUGUI>();
            distText.text = "0m";
            distText.fontSize = 24;
            distText.color = Color.white;
            distText.alignment = TextAlignmentOptions.Center;

            var statusGO = new GameObject("CoinStatusText");
            statusGO.transform.SetParent(panel.transform, false);
            var statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0);
            statusRect.anchorMax = new Vector2(1, 0.25f);
            statusRect.offsetMin = new Vector2(10, 2);
            statusRect.offsetMax = new Vector2(-10, -2);
            var statusText = statusGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.fontSize = 20;
            statusText.color = Color.white;
            statusText.alignment = TextAlignmentOptions.Center;

            var iconGO = new GameObject("CoinTierIcon");
            iconGO.transform.SetParent(panel.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1, 0.5f);
            iconRect.anchorMax = new Vector2(1, 0.5f);
            iconRect.pivot = new Vector2(1, 0.5f);
            iconRect.anchoredPosition = new Vector2(-10, 0);
            iconRect.sizeDelta = new Vector2(40, 40);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = new Color(1, 1, 1, 0.5f);
            iconImage.raycastTarget = false;

            panel.SetActive(false);
            DiagnosticLog.Log("Setup", "CoinInfoPanel created");
        }

        /// <summary>
        /// Create CompassPanel for ARHUD - code-only. CompassUI shows direction to target coin.
        /// </summary>
        private void SetupCompassPanel()
        {
            var existing = transform.Find("CompassPanel");
            if (existing != null) return;

            var panel = new GameObject("CompassPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1);
            panelRect.anchorMax = new Vector2(0.5f, 1);
            panelRect.pivot = new Vector2(0.5f, 1);
            panelRect.anchoredPosition = new Vector2(0, -120);
            panelRect.sizeDelta = new Vector2(200, 80);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.21f, 0.36f, 0.8f);
            bgImage.raycastTarget = false;

            var arrowGO = new GameObject("ArrowImage");
            arrowGO.transform.SetParent(panel.transform, false);
            var arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-60, 0);
            arrowRect.sizeDelta = new Vector2(40, 40);
            var arrowImg = arrowGO.AddComponent<Image>();
            arrowImg.color = Color.white;
            arrowImg.raycastTarget = false;

            var distGO = new GameObject("DistanceText");
            distGO.transform.SetParent(panel.transform, false);
            var distRect = distGO.AddComponent<RectTransform>();
            distRect.anchorMin = new Vector2(0.5f, 0.5f);
            distRect.anchorMax = new Vector2(0.5f, 0.5f);
            distRect.pivot = new Vector2(0.5f, 0.5f);
            distRect.anchoredPosition = new Vector2(0, 0);
            distRect.sizeDelta = new Vector2(80, 30);
            var distText = distGO.AddComponent<TextMeshProUGUI>();
            distText.text = "0m";
            distText.fontSize = 20;
            distText.color = Color.white;
            distText.alignment = TextAlignmentOptions.Center;

            var dirGO = new GameObject("DirectionText");
            dirGO.transform.SetParent(panel.transform, false);
            var dirRect = dirGO.AddComponent<RectTransform>();
            dirRect.anchorMin = new Vector2(0.5f, 0);
            dirRect.anchorMax = new Vector2(0.5f, 0.5f);
            dirRect.pivot = new Vector2(0.5f, 0.5f);
            dirRect.anchoredPosition = new Vector2(20, 5);
            dirRect.sizeDelta = new Vector2(50, 25);
            var dirText = dirGO.AddComponent<TextMeshProUGUI>();
            dirText.text = "N";
            dirText.fontSize = 18;
            dirText.color = Color.white;
            dirText.alignment = TextAlignmentOptions.Center;

            var valGO = new GameObject("ValueText");
            valGO.transform.SetParent(panel.transform, false);
            var valRect = valGO.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.5f, 0.5f);
            valRect.anchorMax = new Vector2(0.5f, 1);
            valRect.pivot = new Vector2(0.5f, 0.5f);
            valRect.anchoredPosition = new Vector2(20, -5);
            valRect.sizeDelta = new Vector2(80, 25);
            var valText = valGO.AddComponent<TextMeshProUGUI>();
            valText.text = "$0.00";
            valText.fontSize = 18;
            valText.color = GoldColor;
            valText.alignment = TextAlignmentOptions.Center;

            var compassUI = panel.AddComponent<CompassUI>();
            compassUI.SetRuntimeReferences(arrowRect, distText, dirText, valText, panel, bgImage);
            panel.SetActive(false);
            DiagnosticLog.Log("Setup", "CompassPanel created");
        }

        /// <summary>
        /// Create GasMeterPanel for ARHUD - code-only. Vertical gauge showing fuel days.
        /// </summary>
        private void SetupGasMeterPanel()
        {
            var existing = transform.Find("GasMeterPanel");
            if (existing != null) return;

            var panel = new GameObject("GasMeterPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(0, 0.5f);
            panelRect.pivot = new Vector2(0, 0.5f);
            panelRect.anchoredPosition = new Vector2(20, 0);
            panelRect.sizeDelta = new Vector2(60, 120);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            bgImage.raycastTarget = false;

            var fillGO = new GameObject("FillImage");
            fillGO.transform.SetParent(panel.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.offsetMin = new Vector2(4, 4);
            fillRect.offsetMax = new Vector2(-4, -4);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Vertical;
            fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
            fillImg.fillAmount = 1f;
            fillImg.color = new Color(0.29f, 0.87f, 0.5f);
            fillImg.raycastTarget = false;

            var daysGO = new GameObject("DaysText");
            daysGO.transform.SetParent(panel.transform, false);
            var daysRect = daysGO.AddComponent<RectTransform>();
            daysRect.anchorMin = Vector2.zero;
            daysRect.anchorMax = Vector2.one;
            daysRect.offsetMin = Vector2.zero;
            daysRect.offsetMax = Vector2.zero;
            var daysText = daysGO.AddComponent<TextMeshProUGUI>();
            daysText.text = "30d";
            daysText.fontSize = 18;
            daysText.color = Color.white;
            daysText.alignment = TextAlignmentOptions.Center;

            var iconGO = new GameObject("GasIcon");
            iconGO.transform.SetParent(panel.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1);
            iconRect.anchorMax = new Vector2(0.5f, 1);
            iconRect.pivot = new Vector2(0.5f, 1);
            iconRect.anchoredPosition = new Vector2(0, 5);
            iconRect.sizeDelta = new Vector2(24, 24);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.color = new Color(0.29f, 0.87f, 0.5f);
            iconImg.raycastTarget = false;

            var gasMeterUI = panel.AddComponent<GasMeterUI>();
            gasMeterUI.SetRuntimeReferences(fillImg, bgImage, daysText, iconImg, panelRect);
            DiagnosticLog.Log("Setup", "GasMeterPanel created");
        }

        /// <summary>
        /// Create FindLimitPanel for ARHUD - code-only. Shows player's find limit tier.
        /// </summary>
        private void SetupFindLimitPanel()
        {
            var existing = transform.Find("FindLimitPanel");
            if (existing != null) return;

            var panel = new GameObject("FindLimitPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 0.5f);
            panelRect.anchorMax = new Vector2(1, 0.5f);
            panelRect.pivot = new Vector2(1, 0.5f);
            panelRect.anchoredPosition = new Vector2(-20, 0);
            panelRect.sizeDelta = new Vector2(140, 60);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.8f, 0.5f, 0.2f, 0.2f);
            bgImage.raycastTarget = false;

            var limitGO = new GameObject("LimitText");
            limitGO.transform.SetParent(panel.transform, false);
            var limitRect = limitGO.AddComponent<RectTransform>();
            limitRect.anchorMin = new Vector2(0, 0.5f);
            limitRect.anchorMax = new Vector2(1, 0.5f);
            limitRect.pivot = new Vector2(0.5f, 0.5f);
            limitRect.anchoredPosition = Vector2.zero;
            limitRect.sizeDelta = new Vector2(-50, 30);
            var limitText = limitGO.AddComponent<TextMeshProUGUI>();
            limitText.text = "Find: $1.00";
            limitText.fontSize = 20;
            limitText.color = new Color(0.8f, 0.5f, 0.2f);
            limitText.alignment = TextAlignmentOptions.Center;

            var tierGO = new GameObject("TierText");
            tierGO.transform.SetParent(panel.transform, false);
            var tierRect = tierGO.AddComponent<RectTransform>();
            tierRect.anchorMin = new Vector2(0, 0);
            tierRect.anchorMax = new Vector2(1, 0.5f);
            tierRect.pivot = new Vector2(0.5f, 0.5f);
            tierRect.anchoredPosition = Vector2.zero;
            tierRect.sizeDelta = new Vector2(-50, 25);
            var tierText = tierGO.AddComponent<TextMeshProUGUI>();
            tierText.text = "Cabin Boy";
            tierText.fontSize = 14;
            tierText.color = new Color(0.8f, 0.5f, 0.2f);
            tierText.alignment = TextAlignmentOptions.Center;

            var iconGO = new GameObject("TierIcon");
            iconGO.transform.SetParent(panel.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1, 0.5f);
            iconRect.anchorMax = new Vector2(1, 0.5f);
            iconRect.pivot = new Vector2(1, 0.5f);
            iconRect.anchoredPosition = new Vector2(-5, 0);
            iconRect.sizeDelta = new Vector2(36, 36);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.color = new Color(0.8f, 0.5f, 0.2f);
            iconImg.raycastTarget = false;

            var findLimitUI = panel.AddComponent<FindLimitUI>();
            findLimitUI.SetRuntimeReferences(limitText, tierText, bgImage, iconImg, panelRect);
            DiagnosticLog.Log("Setup", "FindLimitPanel created");
        }

        /// <summary>
        /// Create DirectionIndicatorPanel for ARHUD - code-only. Large arrow pointing to target coin.
        /// </summary>
        private void SetupDirectionIndicatorPanel()
        {
            var existing = transform.Find("DirectionIndicatorPanel");
            if (existing != null)
            {
                // Keep scene-authored panel, but enforce a single controller.
                var legacyArrow = existing.GetComponent<SimpleDirectionArrow>();
                if (legacyArrow != null)
                {
                    Destroy(legacyArrow);
                    DiagnosticLog.Log("Setup", "Removed legacy SimpleDirectionArrow from DirectionIndicatorPanel");
                }
                
                var panelRectExisting = existing.GetComponent<RectTransform>();
                var bgPanelExisting = existing.GetComponent<Image>();
                var arrowRectExisting = existing.Find("ArrowTransform")?.GetComponent<RectTransform>();
                var distTextExisting = existing.Find("DistanceText")?.GetComponent<TextMeshProUGUI>();
                var valTextExisting = existing.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
                var statTextExisting = existing.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
                var arrowImgExisting = arrowRectExisting != null ? arrowRectExisting.GetComponent<Image>() : null;
                
                var dirIndicatorExisting = existing.GetComponent<CoinDirectionIndicator>();
                if (dirIndicatorExisting == null)
                {
                    dirIndicatorExisting = existing.gameObject.AddComponent<CoinDirectionIndicator>();
                }
                
                if (panelRectExisting != null && bgPanelExisting != null &&
                    arrowRectExisting != null && distTextExisting != null &&
                    valTextExisting != null && statTextExisting != null && arrowImgExisting != null)
                {
                    // Keep the panel active in hierarchy. CoinDirectionIndicator controls visibility via CanvasGroup.
                    existing.gameObject.SetActive(true);
                    dirIndicatorExisting.SetRuntimeReferences(
                        arrowRectExisting,
                        distTextExisting,
                        valTextExisting,
                        statTextExisting,
                        panelRectExisting,
                        bgPanelExisting,
                        arrowImgExisting
                    );
                    DiagnosticLog.Log("Setup", "DirectionIndicatorPanel found and sanitized");
                    return;
                }
                
                DiagnosticLog.Warn("Setup", "Existing DirectionIndicatorPanel missing references - rebuilding");
                Destroy(existing.gameObject);
            }

            var panel = new GameObject("DirectionIndicatorPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(200, 120);

            var bgPanel = panel.AddComponent<Image>();
            bgPanel.color = new Color(0, 0, 0, 0.6f);
            bgPanel.raycastTarget = false;

            var arrowGO = new GameObject("ArrowTransform");
            arrowGO.transform.SetParent(panel.transform, false);
            var arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = Vector2.zero;
            arrowRect.sizeDelta = new Vector2(60, 60);
            var arrowImg = arrowGO.AddComponent<Image>();
            arrowImg.color = new Color(1f, 0.84f, 0f, 0.9f);
            arrowImg.raycastTarget = false;

            var distGO = new GameObject("DistanceText");
            distGO.transform.SetParent(panel.transform, false);
            var distRect = distGO.AddComponent<RectTransform>();
            distRect.anchorMin = new Vector2(0.5f, 0.7f);
            distRect.anchorMax = new Vector2(0.5f, 0.7f);
            distRect.pivot = new Vector2(0.5f, 0.5f);
            distRect.anchoredPosition = Vector2.zero;
            distRect.sizeDelta = new Vector2(120, 30);
            var distText = distGO.AddComponent<TextMeshProUGUI>();
            distText.text = "47m";
            distText.fontSize = 24;
            distText.color = Color.white;
            distText.alignment = TextAlignmentOptions.Center;

            var valGO = new GameObject("ValueText");
            valGO.transform.SetParent(panel.transform, false);
            var valRect = valGO.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.5f, 0.5f);
            valRect.anchorMax = new Vector2(0.5f, 0.5f);
            valRect.pivot = new Vector2(0.5f, 0.5f);
            valRect.anchoredPosition = Vector2.zero;
            valRect.sizeDelta = new Vector2(120, 25);
            var valText = valGO.AddComponent<TextMeshProUGUI>();
            valText.text = "$5.00";
            valText.fontSize = 20;
            valText.color = GoldColor;
            valText.alignment = TextAlignmentOptions.Center;

            var statGO = new GameObject("StatusText");
            statGO.transform.SetParent(panel.transform, false);
            var statRect = statGO.AddComponent<RectTransform>();
            statRect.anchorMin = new Vector2(0.5f, 0.25f);
            statRect.anchorMax = new Vector2(0.5f, 0.25f);
            statRect.pivot = new Vector2(0.5f, 0.5f);
            statRect.anchoredPosition = Vector2.zero;
            statRect.sizeDelta = new Vector2(180, 30);
            var statText = statGO.AddComponent<TextMeshProUGUI>();
            statText.text = "Walk toward the treasure!";
            statText.fontSize = 18;
            statText.color = Color.white;
            statText.alignment = TextAlignmentOptions.Center;
            statText.enableWordWrapping = true;

            var dirIndicator = panel.AddComponent<CoinDirectionIndicator>();
            dirIndicator.SetRuntimeReferences(arrowRect, distText, valText, statText, panelRect, bgPanel, arrowImg);
            // Keep active so lifecycle/event subscriptions run. Visibility is handled by CanvasGroup alpha.
            panel.SetActive(true);
            dirIndicator.Hide();
            DiagnosticLog.Log("Setup", "DirectionIndicatorPanel created");
        }
        
        /// <summary>
        /// Fetch and apply Mapbox map tile to radar. Updates when location changes.
        /// </summary>
        private void UpdateRadarMapTile()
        {
            if (_radarMapTileImage == null) return;
            if (!MapboxService.Exists) return;
            if (GPSManager.Instance == null || !GPSManager.Instance.IsTracking) return;

            var loc = GPSManager.Instance.CurrentLocation;
            if (loc == null) return;

            float timeSince = Time.time - _radarMapLastUpdate;
            double latDiff = System.Math.Abs(loc.latitude - _radarMapLastLat);
            double lngDiff = System.Math.Abs(loc.longitude - _radarMapLastLng);

            bool needsUpdate = timeSince >= 2f &&
                (latDiff > 0.0001 || lngDiff > 0.0001);

            if (needsUpdate && !_radarMapUpdatePending)
            {
                _radarMapUpdatePending = true;
                _radarMapLastUpdate = Time.time;
                _radarMapLastLat = loc.latitude;
                _radarMapLastLng = loc.longitude;

                MapboxService.Instance.GetMiniMapTile(loc.latitude, loc.longitude, _radarZoom, 0f, OnRadarMapTileReceived);
            }
        }

        private void OnRadarMapTileReceived(Texture2D texture)
        {
            if (texture == null || _radarMapTileImage == null)
            {
                _radarMapUpdatePending = false;
                return;
            }
            StartCoroutine(ApplyRadarMapTileNextFrame(texture));
        }

        private IEnumerator ApplyRadarMapTileNextFrame(Texture2D texture)
        {
            yield return null;
            if (texture == null || _radarMapTileImage == null)
            {
                _radarMapUpdatePending = false;
                yield break;
            }
            bool useCopy = Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
            Texture2D displayTex = useCopy ? EnsureRadarTextureForUI(texture) : texture;
            if (displayTex == null)
            {
                _radarMapUpdatePending = false;
                yield break;
            }
            if (_radarMapCurrentTile != null && _radarMapTileIsOurCopy)
                Destroy(_radarMapCurrentTile);
            _radarMapCurrentTile = displayTex;
            _radarMapTileIsOurCopy = useCopy;
            _radarMapTileImage.texture = _radarMapCurrentTile;
            _radarMapTileImage.enabled = true;
            _radarMapTileImage.color = Color.white;
            Canvas.ForceUpdateCanvases();
            _radarMapUpdatePending = false;
            Debug.Log("[ARHuntSceneSetup] Map tile applied to radar");
        }

        private static Texture2D EnsureRadarTextureForUI(Texture2D source)
        {
            if (source == null) return null;
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, rt);
                var copy = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
                copy.filterMode = FilterMode.Bilinear;
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply(false, false);
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                return copy;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ARHuntSceneSetup] EnsureRadarTextureForUI failed: {e.Message}");
                return source;
            }
#else
            return source;
#endif
        }

        private static void EnsureMapboxService()
        {
            if (!MapboxService.Exists)
            {
                var go = new GameObject("MapboxService");
                go.AddComponent<MapboxService>();
                DontDestroyOnLoad(go);
                Debug.Log("[ARHuntSceneSetup] Created MapboxService");
            }
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
