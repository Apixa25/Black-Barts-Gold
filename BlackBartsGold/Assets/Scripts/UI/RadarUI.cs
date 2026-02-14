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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;
using BlackBartsGold.AR;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Mini radar display showing the TARGET coin only (single-target mode).
    /// Player is at center, target coin appears as a dot.
    /// Tap radar to open full map and select a different coin.
    /// Implements IPointerClickHandler for reliable tap detection.
    /// </summary>
    public class RadarUI : MonoBehaviour, IPointerClickHandler
    {
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
        private float radarRadius = 60f;
        
        [SerializeField]
        [Tooltip("Rotate radar with device heading")]
        private bool rotateWithHeading = true;
        
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
        /// Number of coins on radar
        /// </summary>
        public int CoinCount => activeDots.Count;
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<string, RectTransform> activeDots = new Dictionary<string, RectTransform>();
        private Queue<RectTransform> dotPool = new Queue<RectTransform>();
        private float lastUpdateTime = 0f;
        private float currentHeading = 0f;
        
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
            // Without this, IPointerClickHandler won't receive events!
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
        /// Handle radar tap - opens full map
        /// </summary>
        private void OnRadarTapped()
        {
            Debug.Log("[RadarUI] RADAR TAPPED! Opening full map...");
            
            // Open full map UI
            if (FullMapUI.Exists)
            {
                Debug.Log("[RadarUI] FullMapUI exists, calling Show()");
                FullMapUI.Instance.Show();
            }
            else
            {
                Debug.LogWarning("[RadarUI] FullMapUI not found in scene!");
                if (ARHUD.Instance != null)
                {
                    ARHUD.Instance.OnRadarTapped();
                }
            }
        }
        
        /// <summary>
        /// IPointerClickHandler implementation - called when radar is tapped.
        /// This is more reliable than Button.onClick for UI elements.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("[RadarUI] OnPointerClick triggered!");
            OnRadarTapped();
        }
        
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }
        
        private void OnDestroy()
        {
            EnhancedTouchSupport.Disable();
            
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
            
            // Debug: Check for any touch input (new Input System)
            var activeTouches = Touch.activeTouches;
            if (activeTouches.Count > 0)
            {
                var touch = activeTouches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    Debug.Log($"[RadarUI] Touch detected at screen position: {touch.screenPosition}");
                    
                    // Check if touch is within radar bounds
                    if (radarContainer != null)
                    {
                        Vector2 localPoint;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            radarContainer, touch.screenPosition, null, out localPoint);
                        Debug.Log($"[RadarUI] Local point in radar: {localPoint}, Radar size: {radarContainer.rect.size}");
                        
                        if (radarContainer.rect.Contains(localPoint))
                        {
                            Debug.Log("[RadarUI] Touch IS inside radar bounds - should trigger click!");
                        }
                    }
                }
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
            UpdateCoinDot(targetData, displayDistance, bearing, targetCoin.IsLocked, targetCoin.IsInRange);
        }
        
        /// <summary>
        /// Update or create a coin dot
        /// </summary>
        private void UpdateCoinDot(Coin coin, float distance, float bearing, bool isLocked, bool isInRange)
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
            
            // Calculate position on radar
            // Adjust bearing for heading if rotating
            float adjustedBearing = bearing;
            if (rotateWithHeading)
            {
                adjustedBearing = bearing - currentHeading;
            }
            
            float bearingRad = adjustedBearing * Mathf.Deg2Rad;
            float normalizedDistance = distance / radarRange;
            float pixelDistance = normalizedDistance * radarRadius;
            
            // Position (0,0 is center, up is north/forward)
            float x = Mathf.Sin(bearingRad) * pixelDistance;
            float y = Mathf.Cos(bearingRad) * pixelDistance;
            
            dot.anchoredPosition = new Vector2(x, y);
            
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
                return dotPool.Dequeue();
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
                rt.sizeDelta = new Vector2(8, 8);
            }
            
            return dotObj.GetComponent<RectTransform>();
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
            // Uses DeviceCompass (New Input System) — legacy Input.compass broken on Android 16+
            if (DeviceCompass.IsAvailable)
            {
                currentHeading = DeviceCompass.Heading;
            }
            
            // Rotate north indicator
            if (northIndicator != null && rotateWithHeading)
            {
                northIndicator.localRotation = Quaternion.Euler(0, 0, currentHeading);
            }
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
            radarRange = Mathf.Max(10f, meters);
            UpdateRadar();
        }
        
        /// <summary>
        /// Zoom in (decrease range)
        /// </summary>
        public void ZoomIn()
        {
            SetRange(radarRange * 0.5f);
        }
        
        /// <summary>
        /// Zoom out (increase range)
        /// </summary>
        public void ZoomOut()
        {
            SetRange(radarRange * 2f);
        }
        
        #endregion
        
        #region Helpers
        
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
            // Create test coins at various bearings
            float[] bearings = { 0, 45, 90, 135, 180, 225, 270, 315 };
            float[] distances = { 10, 20, 30, 40, 25, 35, 15, 45 };
            
            for (int i = 0; i < bearings.Length; i++)
            {
                Coin testCoin = Coin.CreateTestCoin((i + 1) * 1.00f);
                testCoin.id = $"test-{i}";
                
                UpdateCoinDot(testCoin, distances[i], bearings[i], i == 3, i == 0);
            }
        }
        
        #endregion
    }
}
