// ============================================================================
// CoinController.cs
// Black Bart's Gold - AR Coin Controller (Pokemon GO Pattern)
// Path: Assets/Scripts/AR/CoinController.cs
// ============================================================================
// REFACTORED for Pokemon GO materialization pattern.
// 
// This controller coordinates ARCoinRenderer and ARCoinPositioner.
// The coin is HIDDEN until player gets close, then MATERIALIZES in AR.
// ============================================================================

using UnityEngine;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// GPS accuracy requirements for coin collection.
    /// </summary>
    public static class GPSAccuracyRequirements
    {
        /// <summary>Maximum GPS accuracy (meters) required to collect a coin</summary>
        public const float COLLECTION_ACCURACY = 25f;
        
        /// <summary>Minimum GPS accuracy to track coins</summary>
        public const float TRACKING_ACCURACY = 100f;
    }
    
    /// <summary>
    /// Main controller for an AR coin using Pokemon GO materialization pattern.
    /// Coordinates ARCoinRenderer (visuals) and ARCoinPositioner (GPS tracking).
    /// </summary>
    [RequireComponent(typeof(ARCoinRenderer))]
    [RequireComponent(typeof(ARCoinPositioner))]
    public class CoinController : MonoBehaviour
    {
        #region Components
        
        private ARCoinRenderer coinRenderer;
        private ARCoinPositioner coinPositioner;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Visual")]
        [SerializeField]
        [Tooltip("The coin model/visual child object")]
        private GameObject coinModel;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>Coin data model</summary>
        public Coin CoinData { get; private set; }
        
        /// <summary>Unique ID of this coin</summary>
        public string CoinId => CoinData?.id ?? "";
        
        /// <summary>Is this coin locked (above player's find limit)?</summary>
        public bool IsLocked { get; private set; } = false;
        
        /// <summary>Is this coin within collection range?</summary>
        public bool IsInRange => coinRenderer?.IsInCollectionRange ?? false;
        
        /// <summary>Is collection in progress?</summary>
        public bool IsCollecting { get; private set; } = false;
        
        /// <summary>Has this coin been collected?</summary>
        public bool IsCollected { get; private set; } = false;
        
        /// <summary>GPS distance from player (meters)</summary>
        public float DistanceFromPlayer => coinPositioner?.GPSDistance ?? float.MaxValue;
        
        /// <summary>Current display mode</summary>
        public CoinDisplayMode DisplayMode => coinRenderer?.CurrentMode ?? CoinDisplayMode.Hidden;
        
        /// <summary>Is coin currently visible in AR?</summary>
        public bool IsVisible => coinRenderer?.IsVisible ?? false;
        
        /// <summary>Has coin materialized?</summary>
        public bool HasMaterialized => DisplayMode == CoinDisplayMode.Visible || 
                                       DisplayMode == CoinDisplayMode.Collectible;
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when coin materializes into view</summary>
        public event Action<CoinController> OnMaterialized;
        
        /// <summary>Fired when coin is collected</summary>
        public event Action<CoinController> OnCollected;
        
        /// <summary>Fired when locked coin is tapped</summary>
        public event Action<CoinController> OnLockedTap;
        
        /// <summary>Fired when out-of-range coin is tapped</summary>
        public event Action<CoinController> OnOutOfRangeTap;
        
        /// <summary>Fired when coin enters collection range</summary>
        public event Action<CoinController> OnEnteredRange;
        
        /// <summary>Fired when coin exits collection range</summary>
        public event Action<CoinController> OnExitedRange;
        
        /// <summary>Fired when display mode changes</summary>
        public event Action<CoinController, CoinDisplayMode> OnDisplayModeChanged;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            Debug.Log($"[CoinController] ===== AWAKE T+{Time.realtimeSinceStartup:F2}s on '{gameObject.name}' =====");
            
            // Get required components
            coinRenderer = GetComponent<ARCoinRenderer>();
            if (coinRenderer == null)
            {
                coinRenderer = gameObject.AddComponent<ARCoinRenderer>();
                Debug.Log($"[CoinController] Added ARCoinRenderer to {gameObject.name}");
            }
            
            coinPositioner = GetComponent<ARCoinPositioner>();
            if (coinPositioner == null)
            {
                coinPositioner = gameObject.AddComponent<ARCoinPositioner>();
                Debug.Log($"[CoinController] Added ARCoinPositioner to {gameObject.name}");
            }
            
            // Add CompassCoinPlacer for gyroscope-based positioning (works without AR tracking)
            var compassPlacer = GetComponent<CompassCoinPlacer>();
            if (compassPlacer == null)
            {
                compassPlacer = gameObject.AddComponent<CompassCoinPlacer>();
                Debug.Log($"[CoinController] Added CompassCoinPlacer to {gameObject.name}");
            }
            
            // Auto-find coin model
            if (coinModel == null)
            {
                Transform modelTransform = transform.Find("CoinModel");
                if (modelTransform != null)
                {
                    coinModel = modelTransform.gameObject;
                }
                else if (transform.childCount > 0)
                {
                    coinModel = transform.GetChild(0).gameObject;
                }
            }
            
            // Set up collider
            EnsureCollider();
            
            // Set tag and layer
            gameObject.tag = "Coin";
            int coinLayer = LayerMask.NameToLayer("Coin");
            if (coinLayer >= 0)
            {
                gameObject.layer = coinLayer;
            }
            
            // Log full component inventory
            Debug.Log($"[CoinController] Components on '{gameObject.name}':");
            Debug.Log($"[CoinController]   ARCoinRenderer: {coinRenderer != null}");
            Debug.Log($"[CoinController]   ARCoinPositioner: {coinPositioner != null}");
            Debug.Log($"[CoinController]   CompassCoinPlacer: {compassPlacer != null}");
            Debug.Log($"[CoinController]   CoinModel: {(coinModel != null ? coinModel.name : "NULL")}");
            Debug.Log($"[CoinController]   ChildCount: {transform.childCount}");
            Debug.Log($"[CoinController]   Position: {transform.position}, Scale: {transform.localScale}");
        }
        
        private void Start()
        {
            // Subscribe to renderer events
            if (coinRenderer != null)
            {
                coinRenderer.OnMaterialized += HandleMaterialized;
                coinRenderer.OnEnteredCollectionRange += HandleEnteredRange;
                coinRenderer.OnExitedCollectionRange += HandleExitedRange;
                coinRenderer.OnModeChanged += HandleModeChanged;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe
            if (coinRenderer != null)
            {
                coinRenderer.OnMaterialized -= HandleMaterialized;
                coinRenderer.OnEnteredCollectionRange -= HandleEnteredRange;
                coinRenderer.OnExitedCollectionRange -= HandleExitedRange;
                coinRenderer.OnModeChanged -= HandleModeChanged;
            }
        }
        
        private void Update()
        {
            // Update visual state based on locked status
            UpdateVisualState();
            
            // Check for stuck collection
            CheckCollectionTimeout();
            
            // Debug logging
            if (debugMode && Time.frameCount % 180 == 0)
            {
                Log($"Mode={DisplayMode}, GPS={DistanceFromPlayer:F1}m, InRange={IsInRange}, Visible={IsVisible}");
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize coin from data model
        /// </summary>
        public void Initialize(Coin coinData, bool locked = false, bool inRange = false)
        {
            CoinData = coinData;
            IsLocked = locked;
            
            Debug.Log($"[CoinController] ===== INITIALIZE T+{Time.realtimeSinceStartup:F2}s =====");
            Debug.Log($"[CoinController]   CoinID: {coinData.id}");
            Debug.Log($"[CoinController]   Value: {coinData.GetDisplayValue()}");
            Debug.Log($"[CoinController]   GPS: ({coinData.latitude:F6}, {coinData.longitude:F6})");
            Debug.Log($"[CoinController]   Locked: {locked}");
            Debug.Log($"[CoinController]   HeightOffset: {coinData.heightOffset}");
            
            // Initialize positioner with GPS coordinates
            if (coinPositioner != null)
            {
                coinPositioner.Initialize(coinData);
                Debug.Log($"[CoinController]   Positioner initialized, GPSDist={coinPositioner.GPSDistance:F1}m");
            }
            else
            {
                Debug.LogError("[CoinController]   Positioner is NULL! Cannot track GPS position.");
            }
            
            // Check AR tracking state at initialization time
            var arState = UnityEngine.XR.ARFoundation.ARSession.state;
            Debug.Log($"[CoinController]   AR State at init: {arState}");
            Debug.Log($"[CoinController] ===== END INITIALIZE =====");
            
            // Update visuals
            UpdateVisualState();
        }
        
        /// <summary>
        /// Initialize with just basic values (for testing)
        /// </summary>
        public void InitializeSimple(float value, bool locked = false)
        {
            var testCoin = Coin.CreateTestCoin(value);
            Initialize(testCoin, locked);
        }
        
        #endregion
        
        #region Visual State
        
        private void UpdateVisualState()
        {
            if (coinRenderer == null) return;
            
            // Update color based on state
            if (IsLocked)
            {
                coinRenderer.SetColor(coinRenderer.Settings.lockedColor);
            }
            else if (IsInRange)
            {
                coinRenderer.SetColor(coinRenderer.Settings.inRangeColor);
            }
            else
            {
                coinRenderer.SetColor(coinRenderer.Settings.goldColor);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleMaterialized()
        {
            Log($"MATERIALIZED! Now visible in AR");
            
            OnMaterialized?.Invoke(this);
        }
        
        private void HandleEnteredRange()
        {
            Log($"Entered collection range");
            
            if (CoinData != null)
            {
                CoinData.isInRange = true;
            }
            
            OnEnteredRange?.Invoke(this);
        }
        
        private void HandleExitedRange()
        {
            Log($"Exited collection range");
            
            if (CoinData != null)
            {
                CoinData.isInRange = false;
            }
            
            OnExitedRange?.Invoke(this);
        }
        
        private void HandleModeChanged(CoinDisplayMode oldMode, CoinDisplayMode newMode)
        {
            Log($"Mode changed: {oldMode} → {newMode}");
            
            OnDisplayModeChanged?.Invoke(this, newMode);
        }
        
        #endregion
        
        #region Collection
        
        /// <summary>
        /// Attempt to collect this coin
        /// </summary>
        public bool TryCollect()
        {
            if (IsCollecting || IsCollected)
            {
                return false;
            }
            
            // Check locked
            if (IsLocked)
            {
                OnLockedTap?.Invoke(this);
                Log($"Collection blocked - LOCKED");
                return false;
            }
            
            // Check if materialized and in range
            if (!HasMaterialized)
            {
                Log($"Collection blocked - not materialized (Mode: {DisplayMode})");
                return false;
            }
            
            if (!IsInRange)
            {
                OnOutOfRangeTap?.Invoke(this);
                Log($"Collection blocked - out of range (GPS: {DistanceFromPlayer:F1}m)");
                return false;
            }
            
            // Check GPS accuracy
            if (GPSManager.Exists && GPSManager.Instance.CurrentLocation != null)
            {
                float accuracy = GPSManager.Instance.CurrentLocation.horizontalAccuracy;
                if (accuracy > GPSAccuracyRequirements.COLLECTION_ACCURACY)
                {
                    Log($"Collection blocked - GPS accuracy: {accuracy:F0}m (need < {GPSAccuracyRequirements.COLLECTION_ACCURACY}m)");
                    return false;
                }
            }
            
            // Collect!
            StartCollection();
            return true;
        }
        
        private void StartCollection()
        {
            IsCollecting = true;
            collectionStartTime = Time.time;
            Log($"Collection started!");
            
            // Tell renderer to play collection animation
            if (coinRenderer != null)
            {
                coinRenderer.StartCollection();
            }
            
            // Wait for animation then complete
            StartCoroutine(WaitForCollectionComplete());
        }
        
        // Track when collection started for timeout
        private float collectionStartTime;
        private const float COLLECTION_TIMEOUT = 3f; // Force complete after 3 seconds
        
        private System.Collections.IEnumerator WaitForCollectionComplete()
        {
            // Wait for collection animation
            yield return new WaitForSeconds(0.9f);
            
            FinishCollection();
        }
        
        // Check for stuck collection in Update
        private void CheckCollectionTimeout()
        {
            if (IsCollecting && !IsCollected && Time.time - collectionStartTime > COLLECTION_TIMEOUT)
            {
                Debug.LogWarning($"[CoinController] Collection TIMEOUT! Forcing completion after {COLLECTION_TIMEOUT}s");
                FinishCollection();
            }
        }
        
        private void FinishCollection()
        {
            IsCollecting = false;
            IsCollected = true;
            
            Log($"Collection COMPLETE! Value: {CoinData?.value ?? 0}");
            
            // Notify
            OnCollected?.Invoke(this);
            
            // Deactivate
            gameObject.SetActive(false);
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Set locked status
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (IsLocked == locked) return;
            
            IsLocked = locked;
            
            if (CoinData != null)
            {
                CoinData.isLocked = locked;
            }
            
            UpdateVisualState();
        }
        
        /// <summary>
        /// Force coin to materialize (for testing)
        /// </summary>
        public void ForceMaterialize()
        {
            coinRenderer?.ForceMaterialize();
        }
        
        #endregion
        
        #region Helpers
        
        private void EnsureCollider()
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.3f;
            }
        }
        
        /// <summary>
        /// Get proximity zone based on GPS distance
        /// </summary>
        public ProximityZone GetProximityZone()
        {
            float distance = DistanceFromPlayer;
            
            if (distance <= 5f) return ProximityZone.Collectible;
            if (distance <= 15f) return ProximityZone.Near;
            if (distance <= 30f) return ProximityZone.Medium;
            if (distance <= 50f) return ProximityZone.Far;
            return ProximityZone.OutOfRange;
        }
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[CoinController:{CoinId}] {message}");
            }
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Force Collect")]
        public void DebugForceCollect()
        {
            IsLocked = false;
            coinRenderer?.ForceMaterialize();
            StartCoroutine(DebugDelayedCollect());
        }
        
        private System.Collections.IEnumerator DebugDelayedCollect()
        {
            yield return new WaitForSeconds(1f);
            StartCollection();
        }
        
        [ContextMenu("Debug: Toggle Locked")]
        public void DebugToggleLocked()
        {
            SetLocked(!IsLocked);
        }
        
        [ContextMenu("Debug: Force Materialize")]
        public void DebugForceMaterialize()
        {
            ForceMaterialize();
        }
        
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log($"=== Coin {CoinId} ===");
            Debug.Log($"Value: {CoinData?.GetDisplayValue() ?? "null"}");
            Debug.Log($"Display Mode: {DisplayMode}");
            Debug.Log($"GPS Distance: {DistanceFromPlayer:F1}m");
            Debug.Log($"Locked: {IsLocked}");
            Debug.Log($"In Range: {IsInRange}");
            Debug.Log($"Visible: {IsVisible}");
            Debug.Log($"Has Materialized: {HasMaterialized}");
            Debug.Log($"Collected: {IsCollected}");
            Debug.Log("==================");
        }
        
        #endregion
    }
}
