// ============================================================================
// RadarUI.cs
// Black Bart's Gold - Mini Radar/Map UI Component (Single-Target Mode)
// Path: Assets/Scripts/UI/RadarUI.cs
// Last Modified: 2026-01-27 17:30 - Force recompile for tap fix
// ============================================================================
// Displays a radar-style mini-map showing the TARGET COIN only.
// In single-target architecture, only shows the coin being actively hunted.
// Reference: BUILD-GUIDE.md Prompt 5.1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using UIManager = BlackBartsGold.Core.UIManager;
using BlackBartsGold.Location;
using BlackBartsGold.AR;
using UnityEngine.InputSystem;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Mini radar display showing the TARGET coin only (single-target mode).
    /// Player is at center, target coin appears as a dot.
    /// Tap radar to open full map and select a different coin.
    /// Uses a dedicated Button click handler for map open.
    /// </summary>
    public class RadarUI : MonoBehaviour
    {
        public enum MiniMapOrientationMode
        {
            NorthUp = 0,
            ForwardUp = 1
        }

        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Container for the radar")]
        private RectTransform radarContainer;
        
        [SerializeField]
        [Tooltip("Player dot at center")]
        private RectTransform playerDot;
        
        [SerializeField]
        [Tooltip("Prefab for coin dots")]
        private GameObject coinDotPrefab;
        
        [SerializeField]
        [Tooltip("Sprite for coin dots when no prefab is set (e.g. 'location coin')")]
        private Sprite coinDotSprite;
        
        [SerializeField]
        [Tooltip("Sweep line that rotates")]
        private RectTransform sweepLine;
        
        [SerializeField]
        [Tooltip("Range rings")]
        private Image[] rangeRings;
        
        [SerializeField]
        [Tooltip("North indicator")]
        private RectTransform northIndicator;
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Radar range in meters")]
        private float radarRange = 50f;
        
        [SerializeField]
        [Tooltip("Radar radius in pixels")]
        private float radarRadius = 120f;

        [SerializeField]
        [Tooltip("Visual scale multiplier for radar internals")]
        private float miniMapScale = 2f;

        [SerializeField]
        [Tooltip("Auto-adjust radar range to target distance")]
        private bool autoZoomEnabled = false;
        
        [SerializeField]
        [Tooltip("NorthUp = map stays north-up; ForwardUp = player-forward-up mini-map")]
private MiniMapOrientationMode orientationMode = MiniMapOrientationMode.ForwardUp;
        
        [SerializeField]
        [Tooltip("Sweep animation speed (degrees/second)")]
        private float sweepSpeed = 90f;
        
        [SerializeField]
        [Tooltip("Update interval (seconds)")]
        private float updateInterval = 0.5f;
        
        [Header("Dot Colors")]
        [SerializeField]
        private Color normalCoinColor = new Color(1f, 0.84f, 0f); // Gold
        
        [SerializeField]
        private Color lockedCoinColor = new Color(0.94f, 0.27f, 0.27f); // Red
        
        [SerializeField]
        private Color inRangeCoinColor = new Color(0.29f, 0.87f, 0.5f); // Green
        
        [SerializeField]
        private Color poolCoinColor = new Color(0.5f, 0.8f, 1f); // Light blue
        
        [Header("Tap to Open Map")]
        [SerializeField]
        [Tooltip("Button component on radar for tap detection")]
        private Button radarButton;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Runtime Setup (Code-Only UI)
        
        /// <summary>
        /// Set radar references at runtime. Called by ARHuntSceneSetup when building UI from code.
        /// </summary>
        public void SetRuntimeReferences(RectTransform container, RectTransform player, RectTransform sweep, RectTransform north, Sprite coinSprite)
        {
            radarContainer = container;
            playerDot = player;
            sweepLine = sweep;
            northIndicator = north;
            coinDotSprite = coinSprite;
            Debug.Log("[RadarUI] Runtime references set (code-only setup)");
        }
        
        #endregion
        
        #region Awake - Very Early Init
        
        private void Awake()
        {
            Debug.Log("========================================");
            Debug.Log("[RadarUI] AWAKE - RadarUI component initializing!");
            Debug.Log($"[RadarUI] On GameObject: {gameObject.name}");
            Debug.Log("========================================");
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is radar visible?
        /// </summary>
        public bool IsVisible { get; private set; } = true;
        
        /// <summary>
        /// Current radar range
        /// </summary>
        public float Range => radarRange;

        /// <summary>
        /// Is automatic range zoom enabled?
        /// </summary>
        public bool AutoZoomEnabled => autoZoomEnabled;

        /// <summary>
        /// Current mini-map orientation mode.
        /// </summary>
        public MiniMapOrientationMode OrientationMode => orientationMode;
        
        /// <summary>
        /// Number of coins on radar
        /// </summary>
        public int CoinCount => activeDots.Count;
        
        #endregion
        
        #region Private Fields

        private const float BaseRadarRadiusPixels = 60f;
        private const float BaseDotSizePixels = 16f;
        private const float MiniMapTileRequestPixels = 400f;
        private const float WebMercatorTileSize = 512f;
        private const float MinRadarRangeMeters = 15f;
        private const float MaxRadarRangeMeters = 200f;
        private const float AutoZoomPaddingMultiplier = 1.35f;
        private const float AutoZoomRangeChangeThreshold = 0.5f;
        private const string RadarRangePrefKey = "RadarUI.RangeMeters";
        private const string RadarAutoPrefKey = "RadarUI.AutoZoom";
        private const string RadarScalePrefKey = "RadarUI.MiniMapScale";
        
        private Dictionary<string, RectTransform> activeDots = new Dictionary<string, RectTransform>();
        private Queue<RectTransform> dotPool = new Queue<RectTransform>();
        private float lastUpdateTime = 0f;
        private float currentHeading = 0f;
        private float _lastTapHealthCheckTime = 0f;
        private const float TapHealthCheckInterval = 1f;
        private float _lastFallbackTapTime = -10f;
        private const float FallbackTapCooldownSeconds = 0.35f;
        private int currentMiniMapZoom = 19;
        private float _lastForwardUpVerifyLogTime = -10f;
        private const float ForwardUpVerifyLogInterval = 2f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            Debug.Log("[RadarUI] Start() BEGIN");
            
            try
            {
                // Initialize DeviceCompass (New Input System replacement for legacy Input.compass)
                DeviceCompass.Initialize();
                Debug.Log("[RadarUI] DeviceCompass initialized");
                
                // Auto-find references if not assigned
                AutoFindReferences();
                Debug.Log("[RadarUI] AutoFindReferences done");

                // Restore user zoom preferences before first radar render
                LoadPreferences();
                SetMiniMapScale(miniMapScale);
                
                // ============================================================
                // CRITICAL: Ensure Canvas has GraphicRaycaster for UI clicks!
                // ============================================================
                EnsureGraphicRaycaster();
                
                // Subscribe to GPS events
                if (GPSManager.Exists)
                {
                    GPSManager.Instance.OnLocationUpdated += OnLocationUpdated;
                    Debug.Log("[RadarUI] Subscribed to GPS");
                }
                
                // Subscribe to CoinManager events (single-target mode)
                if (CoinManager.Exists)
                {
                    CoinManager.Instance.OnTargetSet += OnTargetSet;
                    CoinManager.Instance.OnTargetCleared += OnTargetCleared;
                    CoinManager.Instance.OnHuntModeChanged += OnHuntModeChanged;
                    Debug.Log("[RadarUI] Subscribed to CoinManager");
                }
                
                // Setup radar tap to open full map
                SetupRadarTap();
                Debug.Log("[RadarUI] SetupRadarTap done");
                
                // Initial update
                UpdateRadar();
                Debug.Log("[RadarUI] UpdateRadar done");
                
                Debug.Log($"[RadarUI] Start() COMPLETE - radarButton:{radarButton != null}, radarContainer:{radarContainer != null}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RadarUI] Start() EXCEPTION: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Ensure the parent Canvas has a GraphicRaycaster for UI click detection.
        /// Also check for EventSystem in scene.
        /// </summary>
        private void EnsureGraphicRaycaster()
        {
            // Find parent Canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[RadarUI] No parent Canvas found! UI clicks won't work.");
                return;
            }
            
            // Ensure GraphicRaycaster exists
            UnityEngine.UI.GraphicRaycaster raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.LogWarning("[RadarUI] Added GraphicRaycaster to Canvas - was missing!");
            }
            else
            {
                Debug.Log("[RadarUI] Canvas has GraphicRaycaster OK");
            }
            
            // Check for EventSystem
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                Debug.LogError("[RadarUI] No EventSystem in scene! UI clicks won't work.");
                
                // Try to find one
                var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
                if (eventSystem == null)
                {
                    Debug.LogError("[RadarUI] Creating EventSystem...");
                    GameObject esGO = new GameObject("EventSystem");
                    esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }
            else
            {
                Debug.Log("[RadarUI] EventSystem found OK");
            }
        }
        
        /// <summary>
        /// Auto-find references if not assigned in Inspector
        /// </summary>
        private void AutoFindReferences()
        {
            // Use this object as the radar container if not assigned
            if (radarContainer == null)
            {
                radarContainer = GetComponent<RectTransform>();
            }
            
            // ============================================================
            // CRITICAL: Ensure we have an Image with raycastTarget = true
            // Without this, Button.onClick won't receive events.
            // ============================================================
            Image radarImage = GetComponent<Image>();
            if (radarImage == null)
            {
                radarImage = gameObject.AddComponent<Image>();
                // Make it invisible but still catch raycasts
                radarImage.color = new Color(0, 0, 0, 0.01f); // Nearly invisible
                Debug.Log("[RadarUI] Added invisible Image for raycast detection");
            }
            
            // CRITICAL: Enable raycastTarget!
            if (!radarImage.raycastTarget)
            {
                radarImage.raycastTarget = true;
                Debug.Log("[RadarUI] Enabled raycastTarget on Image");
            }
            
            // Get or create button on this object
            if (radarButton == null)
            {
                radarButton = GetComponent<Button>();
            }

            RecalculateRadarRadiusFromContainer();
            
            Debug.Log($"[RadarUI] AutoFindReferences - container:{radarContainer != null}, button:{radarButton != null}, image:{radarImage != null}, raycastTarget:{radarImage?.raycastTarget}");
        }
        
        /// <summary>
        /// Setup tap handler to open full map
        /// </summary>
        private void SetupRadarTap()
        {
            // If still no button, try to add one
            if (radarButton == null)
            {
                radarButton = gameObject.AddComponent<Button>();
                radarButton.transition = Selectable.Transition.None;
                Debug.Log("[RadarUI] Added Button component dynamically");
            }
            
            if (radarButton != null)
            {
                EnsureTapReadiness();
                radarButton.onClick.RemoveAllListeners(); // Clear any existing
                radarButton.onClick.AddListener(OnRadarTapped);
                Debug.Log("[RadarUI] Radar tap handler configured successfully");
            }
            else
            {
                Debug.LogError("[RadarUI] Failed to setup radar tap - no button!");
            }
        }
        
        /// <summary>
        /// Handle radar tap - opens full map.
        /// Single source of truth: UIManager's code-based map.
        /// </summary>
        private void OnRadarTapped()
        {
            Debug.Log($"[RadarUI][TAP] Radar tapped at t={Time.time:F2}s; opening full map...");
            
            if (Core.UIManager.Instance != null)
            {
                Core.UIManager.Instance.OnMiniMapClicked();
            }
            else
            {
                Debug.LogWarning("[RadarUI] UIManager not found!");
            }
        }

        private void EnsureTapReadiness()
        {
            // Keep radar panel at top to reduce click interception by overlapping overlays.
            transform.SetAsLastSibling();

            Image radarImage = GetComponent<Image>();
            if (radarImage == null)
            {
                radarImage = gameObject.AddComponent<Image>();
                radarImage.color = new Color(0, 0, 0, 0.01f);
            }
            radarImage.raycastTarget = true;

            if (radarButton == null)
            {
                radarButton = GetComponent<Button>();
                if (radarButton == null)
                {
                    radarButton = gameObject.AddComponent<Button>();
                    radarButton.transition = Selectable.Transition.None;
                }
            }

            radarButton.enabled = true;
            radarButton.interactable = true;
            radarButton.targetGraphic = radarImage;
        }

        private void HandleFallbackTap()
        {
            if (!IsVisible) return;
            if (Time.time - _lastFallbackTapTime < FallbackTapCooldownSeconds) return;

            Vector2 tapPos;
            bool tapped = false;

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                tapPos = touch.primaryTouch.position.ReadValue();
                tapped = true;
            }
            else
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    tapPos = mouse.position.ReadValue();
                    tapped = true;
                }
                else
                {
                    return;
                }
            }

            if (!tapped) return;

            RectTransform tapRect = radarContainer != null ? radarContainer : GetComponent<RectTransform>();
            if (tapRect == null) return;

            if (RectTransformUtility.RectangleContainsScreenPoint(tapRect, tapPos, null))
            {
                _lastFallbackTapTime = Time.time;
                Debug.Log($"[RadarUI][FALLBACK_TAP] Radar area hit at t={Time.time:F2}s");
                OnRadarTapped();
            }
        }
        
        private void OnDestroy()
        {
            if (GPSManager.Exists)
            {
                GPSManager.Instance.OnLocationUpdated -= OnLocationUpdated;
            }
            
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet -= OnTargetSet;
                CoinManager.Instance.OnTargetCleared -= OnTargetCleared;
                CoinManager.Instance.OnHuntModeChanged -= OnHuntModeChanged;
            }
        }
        
        private void Update()
        {
            if (Time.time - _lastTapHealthCheckTime >= TapHealthCheckInterval)
            {
                _lastTapHealthCheckTime = Time.time;
                EnsureTapReadiness();
            }

            HandleFallbackTap();

            // Update heading
            UpdateHeading();
            
            // Animate sweep
            AnimateSweep();
            
            // Periodic radar update
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateRadar();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle GPS location update
        /// </summary>
        private void OnLocationUpdated(LocationData location)
        {
            UpdateRadar();
        }
        
        /// <summary>
        /// Handle target coin set
        /// </summary>
        private void OnTargetSet(Coin coin)
        {
            Log($"Target set: {coin.id}");
            UpdateRadar();
        }
        
        /// <summary>
        /// Handle target cleared
        /// </summary>
        private void OnTargetCleared()
        {
            Log("Target cleared");
            ClearAllDots();
        }
        
        /// <summary>
        /// Handle hunt mode changed
        /// </summary>
        private void OnHuntModeChanged(HuntMode mode)
        {
            Log($"Hunt mode: {mode}");
            
            if (mode == HuntMode.MapView)
            {
                // In map view, radar shows nothing (user views full map)
                ClearAllDots();
            }
            else
            {
                // In hunting mode, show target
                UpdateRadar();
            }
        }
        
        #endregion
        
        #region Radar Updates
        
        /// <summary>
        /// Update radar display (single-target mode).
        /// Only shows the TARGET coin, not all coins.
        /// </summary>
        public void UpdateRadar()
        {
            if (!IsVisible) return;
            RecalculateRadarRadiusFromContainer();
            
            LocationData playerLocation = GetPlayerLocation();
            if (playerLocation == null) return;
            
            // In single-target mode, only show the target coin
            if (!CoinManager.Exists || !CoinManager.Instance.HasTarget)
            {
                ClearAllDots();
                return;
            }
            
            // Only show radar content when in hunting mode
            if (CoinManager.Instance.CurrentMode != HuntMode.Hunting)
            {
                ClearAllDots();
                return;
            }
            
            var targetCoin = CoinManager.Instance.TargetCoin;
            var targetData = CoinManager.Instance.TargetCoinData;
            
            if (targetCoin == null || targetData == null)
            {
                ClearAllDots();
                return;
            }
            
            // Calculate distance to target
            float distance = GeoUtils.CalculateDistance(
                playerLocation.latitude, playerLocation.longitude,
                targetData.latitude, targetData.longitude
            );

            if (autoZoomEnabled)
            {
                float desiredRange = Mathf.Clamp(distance * AutoZoomPaddingMultiplier, MinRadarRangeMeters, MaxRadarRangeMeters);
                if (Mathf.Abs(radarRange - desiredRange) > AutoZoomRangeChangeThreshold)
                {
                    radarRange = desiredRange;
                }
            }
            
            // Calculate bearing to target
            float bearing = GeoUtils.CalculateBearing(
                playerLocation.latitude, playerLocation.longitude,
                targetData.latitude, targetData.longitude
            );
            
            // Clear any old dots (shouldn't be any, but just in case)
            List<string> toRemove = new List<string>();
            foreach (var id in activeDots.Keys)
            {
                if (id != targetData.id)
                {
                    toRemove.Add(id);
                }
            }
            foreach (var id in toRemove)
            {
                RemoveCoinDot(id);
            }
            
            // Update or create the target dot
            // For radar, we can show it even if beyond radar range (just at the edge)
            float displayDistance = Mathf.Min(distance, radarRange * 0.95f);
            UpdateCoinDot(targetData, displayDistance, bearing, targetCoin.IsLocked, targetCoin.IsInRange, playerLocation);
        }
        
        /// <summary>
        /// Update or create a coin dot
        /// </summary>
        private void UpdateCoinDot(Coin coin, float distance, float bearing, bool isLocked, bool isInRange, LocationData playerLocation)
        {
            RectTransform dot;
            
            if (!activeDots.TryGetValue(coin.id, out dot))
            {
                // Create new dot
                dot = GetDotFromPool();
                dot.SetParent(radarContainer);
                dot.gameObject.SetActive(true);
                activeDots[coin.id] = dot;
            }
            
            // Calculate position on radar.
            // NorthUp: use world bearing directly (matches north-up map tile).
            // ForwardUp: rotate bearing into player-forward frame.
            float adjustedBearing = bearing;
            if (orientationMode == MiniMapOrientationMode.ForwardUp)
            {
                adjustedBearing = bearing - currentHeading;
            }
            
            float bearingRad = adjustedBearing * Mathf.Deg2Rad;
            float normalizedDistance = distance / radarRange;

            if (TryProjectCoinToMiniMap(coin, playerLocation, out Vector2 projectedPosition))
            {
                dot.anchoredPosition = projectedPosition;
            }
            else
            {
                float pixelDistance = normalizedDistance * radarRadius;

                // Fallback radar projection when map projection context is unavailable.
                float x = Mathf.Sin(bearingRad) * pixelDistance;
                float y = Mathf.Cos(bearingRad) * pixelDistance;
                dot.anchoredPosition = new Vector2(x, y);
            }
            
            // Set color based on state
            Image dotImage = dot.GetComponent<Image>();
            if (dotImage != null)
            {
                if (isLocked)
                {
                    dotImage.color = lockedCoinColor;
                }
                else if (isInRange)
                {
                    dotImage.color = inRangeCoinColor;
                }
                else if (coin.coinType == CoinType.Pool)
                {
                    dotImage.color = poolCoinColor;
                }
                else
                {
                    dotImage.color = normalCoinColor;
                }
            }
            
            // Scale based on distance (closer = bigger)
            float scale = Mathf.Lerp(1.5f, 0.5f, normalizedDistance);
            dot.localScale = Vector3.one * scale;

            MaybeLogForwardUpProjection(coin, bearing, adjustedBearing, dot.anchoredPosition);
        }
        
        /// <summary>
        /// Remove a coin dot
        /// </summary>
        private void RemoveCoinDot(string coinId)
        {
            if (activeDots.TryGetValue(coinId, out RectTransform dot))
            {
                ReturnDotToPool(dot);
                activeDots.Remove(coinId);
            }
        }
        
        /// <summary>
        /// Clear all coin dots
        /// </summary>
        public void ClearAllDots()
        {
            foreach (var dot in activeDots.Values)
            {
                ReturnDotToPool(dot);
            }
            activeDots.Clear();
        }
        
        #endregion
        
        #region Object Pool
        
        /// <summary>
        /// Get a dot from the pool or create new
        /// </summary>
        private RectTransform GetDotFromPool()
        {
            if (dotPool.Count > 0)
            {
                var pooledDot = dotPool.Dequeue();
                ApplyDotSize(pooledDot);
                return pooledDot;
            }
            
            // Create new dot
            GameObject dotObj;
            if (coinDotPrefab != null)
            {
                dotObj = Instantiate(coinDotPrefab);
            }
            else
            {
                // Create default dot
                dotObj = new GameObject("CoinDot");
                Image img = dotObj.AddComponent<Image>();
                if (coinDotSprite != null)
                    img.sprite = coinDotSprite;
                img.color = normalCoinColor;
                
                RectTransform rt = dotObj.GetComponent<RectTransform>();
                ApplyDotSize(rt);
            }
            
            return dotObj.GetComponent<RectTransform>();
        }

        private void ApplyDotSizes()
        {
            foreach (var dot in activeDots.Values)
            {
                ApplyDotSize(dot);
            }

            foreach (var pooledDot in dotPool)
            {
                ApplyDotSize(pooledDot);
            }
        }

        private void ApplyDotSize(RectTransform dot)
        {
            if (dot == null) return;
            float dotSize = BaseDotSizePixels * miniMapScale;
            dot.sizeDelta = new Vector2(dotSize, dotSize);
        }
        
        /// <summary>
        /// Return a dot to the pool
        /// </summary>
        private void ReturnDotToPool(RectTransform dot)
        {
            dot.gameObject.SetActive(false);
            dotPool.Enqueue(dot);
        }
        
        #endregion
        
        #region Heading & Animation
        
        /// <summary>
        /// Update device heading
        /// </summary>
        private void UpdateHeading()
        {
            // Use gameplay heading so the mini-map matches the same forward direction
            // that the live AR target/coin system uses on device.
            if (DeviceCompass.IsAvailable)
            {
                currentHeading = DeviceCompass.GameplayHeading;
            }
            
            // Rotate north indicator only in ForwardUp mode.
            if (northIndicator != null)
            {
                if (orientationMode == MiniMapOrientationMode.ForwardUp)
                {
                    northIndicator.localRotation = Quaternion.Euler(0, 0, currentHeading);
                }
                else
                {
                    northIndicator.localRotation = Quaternion.identity;
                }
            }

            MaybeLogForwardUpHeading();
        }
        
        /// <summary>
        /// Animate the sweep line
        /// </summary>
        private void AnimateSweep()
        {
            if (sweepLine == null) return;
            
            sweepLine.Rotate(0, 0, -sweepSpeed * Time.deltaTime);
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the radar
        /// </summary>
        public void Show()
        {
            if (radarContainer != null)
            {
                radarContainer.gameObject.SetActive(true);
            }
            IsVisible = true;
            UpdateRadar();
        }
        
        /// <summary>
        /// Hide the radar
        /// </summary>
        public void Hide()
        {
            if (radarContainer != null)
            {
                radarContainer.gameObject.SetActive(false);
            }
            IsVisible = false;
        }
        
        /// <summary>
        /// Toggle visibility
        /// </summary>
        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// Set radar range
        /// </summary>
        public void SetRange(float meters)
        {
            if (autoZoomEnabled)
            {
                autoZoomEnabled = false;
            }

            radarRange = Mathf.Clamp(meters, MinRadarRangeMeters, MaxRadarRangeMeters);
            SavePreferences();
            UpdateRadar();
        }
        
        /// <summary>
        /// Zoom in (decrease range)
        /// </summary>
        public void ZoomIn()
        {
            SetRange(radarRange * 0.8f);
        }
        
        /// <summary>
        /// Zoom out (increase range)
        /// </summary>
        public void ZoomOut()
        {
            SetRange(radarRange * 1.25f);
        }

        /// <summary>
        /// Set mini-map visual scale (radar radius + marker sizing).
        /// </summary>
        public void SetMiniMapScale(float scale)
        {
            miniMapScale = Mathf.Clamp(scale, 0.5f, 4f);
            RecalculateRadarRadiusFromContainer();
            ApplyDotSizes();
            SavePreferences();
            UpdateRadar();
        }

        /// <summary>
        /// Set the Mapbox zoom level currently used by the mini-map background tile.
        /// Coin markers must use the same zoom so they stay aligned with real map geography.
        /// </summary>
        public void SetMapProjectionZoom(int zoom)
        {
            currentMiniMapZoom = Mathf.Clamp(zoom, 0, 22);
            UpdateRadar();
        }

        /// <summary>
        /// Enable/disable automatic radar range adaptation.
        /// </summary>
        public void SetAutoZoomEnabled(bool enabled)
        {
            autoZoomEnabled = enabled;
            SavePreferences();
            UpdateRadar();
        }

        public void SetOrientationMode(MiniMapOrientationMode mode)
        {
            orientationMode = mode;
            UpdateRadar();
        }

        /// <summary>
        /// Toggle automatic radar range adaptation.
        /// </summary>
        public void ToggleAutoZoom()
        {
            SetAutoZoomEnabled(!autoZoomEnabled);
        }
        
        #endregion
        
        #region Helpers

        #region Persistence

        private void LoadPreferences()
        {
            radarRange = Mathf.Clamp(PlayerPrefs.GetFloat(RadarRangePrefKey, radarRange), MinRadarRangeMeters, MaxRadarRangeMeters);
            miniMapScale = Mathf.Clamp(PlayerPrefs.GetFloat(RadarScalePrefKey, miniMapScale), 0.5f, 4f);
            autoZoomEnabled = PlayerPrefs.GetInt(RadarAutoPrefKey, autoZoomEnabled ? 1 : 0) == 1;
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetFloat(RadarRangePrefKey, radarRange);
            PlayerPrefs.SetFloat(RadarScalePrefKey, miniMapScale);
            PlayerPrefs.SetInt(RadarAutoPrefKey, autoZoomEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion
        
        /// <summary>
        /// Get player location
        /// </summary>
        private LocationData GetPlayerLocation()
        {
            if (GPSManager.Exists)
            {
                return GPSManager.Instance.GetBestLocation();
            }
            
            if (PlayerData.Exists)
            {
                return PlayerData.Instance.GetBestLocation();
            }
            
            return null;
        }

        /// <summary>
        /// Keep radar projection radius aligned to actual on-screen mini-map size.
        /// Prevents coin dots from clustering toward center when panel size differs
        /// from older hardcoded assumptions.
        /// </summary>
        private void RecalculateRadarRadiusFromContainer()
        {
            if (radarContainer != null)
            {
                float width = radarContainer.rect.width;
                float height = radarContainer.rect.height;
                float minDim = Mathf.Min(width, height);
                if (minDim > 1f)
                {
                    // Use most of the circular map area while keeping slight edge padding.
                    radarRadius = (minDim * 0.5f) * 0.95f;
                    return;
                }
            }

            // Fallback for early lifecycle frames before layout is resolved.
            radarRadius = BaseRadarRadiusPixels * miniMapScale;
        }

        private bool TryProjectCoinToMiniMap(Coin coin, LocationData playerLocation, out Vector2 anchoredPosition)
        {
            anchoredPosition = Vector2.zero;

            if (coin == null || playerLocation == null || radarContainer == null)
            {
                return false;
            }

            float width = radarContainer.rect.width;
            float height = radarContainer.rect.height;
            if (width <= 1f || height <= 1f)
            {
                return false;
            }

            Vector2 centerPixels = ProjectMercator(playerLocation.latitude, playerLocation.longitude, currentMiniMapZoom);
            Vector2 coinPixels = ProjectMercator(coin.latitude, coin.longitude, currentMiniMapZoom);
            Vector2 deltaPixels = coinPixels - centerPixels;

            // Mapbox static tile is requested at 400x400 CSS pixels, then stretched into the radar container.
            float scaleX = width / MiniMapTileRequestPixels;
            float scaleY = height / MiniMapTileRequestPixels;
            Vector2 displayOffset = new Vector2(deltaPixels.x * scaleX, -deltaPixels.y * scaleY);

            if (orientationMode == MiniMapOrientationMode.ForwardUp)
            {
                displayOffset = Quaternion.Euler(0f, 0f, currentHeading) * displayOffset;
            }

            float maxX = (width * 0.5f) * 0.95f;
            float maxY = (height * 0.5f) * 0.95f;
            anchoredPosition = new Vector2(
                Mathf.Clamp(displayOffset.x, -maxX, maxX),
                Mathf.Clamp(displayOffset.y, -maxY, maxY)
            );

            return true;
        }

        private static Vector2 ProjectMercator(double latitude, double longitude, int zoom)
        {
            double worldSize = WebMercatorTileSize * System.Math.Pow(2d, zoom);
            double x = (longitude + 180d) / 360d * worldSize;

            double latRad = latitude * System.Math.PI / 180d;
            double sinLat = System.Math.Sin(latRad);
            double y = (0.5d - System.Math.Log((1d + sinLat) / (1d - sinLat)) / (4d * System.Math.PI)) * worldSize;

            return new Vector2((float)x, (float)y);
        }

        private void MaybeLogForwardUpHeading()
        {
            if (orientationMode != MiniMapOrientationMode.ForwardUp) return;
            if (Time.unscaledTime - _lastForwardUpVerifyLogTime < ForwardUpVerifyLogInterval) return;

            _lastForwardUpVerifyLogTime = Time.unscaledTime;

            float northZ = northIndicator != null ? northIndicator.localEulerAngles.z : float.NaN;
            Debug.Log($"[BBG][Verify][Radar] heading={currentHeading:F1} mode={orientationMode} zoom={currentMiniMapZoom} range={radarRange:F1} northZ={northZ:F1}");
        }

        private void MaybeLogForwardUpProjection(Coin coin, float worldBearing, float adjustedBearing, Vector2 anchoredPosition)
        {
            if (orientationMode != MiniMapOrientationMode.ForwardUp) return;
            if (coin == null) return;
            if (Time.unscaledTime - _lastForwardUpVerifyLogTime > 0.25f) return;

            Debug.Log($"[BBG][Verify][RadarDot] coin={coin.id} worldBearing={worldBearing:F1} adjustedBearing={adjustedBearing:F1} heading={currentHeading:F1} pos=({anchoredPosition.x:F1},{anchoredPosition.y:F1})");
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[RadarUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print radar state
        /// </summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== Radar State ===");
            Debug.Log($"Visible: {IsVisible}");
            Debug.Log($"Range: {radarRange}m");
            Debug.Log($"Coins: {CoinCount}");
            Debug.Log($"Heading: {currentHeading:F0}°");
            Debug.Log("===================");
        }
        
        /// <summary>
        /// Debug: Add test dots
        /// </summary>
        [ContextMenu("Debug: Add Test Dots")]
        public void DebugAddTestDots()
        {
            LocationData playerLocation = GetPlayerLocation();
            if (playerLocation == null)
            {
                Debug.LogWarning("[RadarUI] DebugAddTestDots skipped: player location unavailable");
                return;
            }

            // Create test coins at various bearings
            float[] bearings = { 0, 45, 90, 135, 180, 225, 270, 315 };
            float[] distances = { 10, 20, 30, 40, 25, 35, 15, 45 };
            
            for (int i = 0; i < bearings.Length; i++)
            {
                Coin testCoin = Coin.CreateTestCoin((i + 1) * 1.00f);
                testCoin.id = $"test-{i}";
                
                UpdateCoinDot(testCoin, distances[i], bearings[i], i == 3, i == 0, playerLocation);
            }
        }
        
        #endregion
    }
}
