// ============================================================================
// CoinController.cs
// Black Bart's Gold - AR Coin Controller (Simplified)
// Path: Assets/Scripts/AR/CoinController.cs
// ============================================================================
// Main controller for coin behavior. Coordinates ARCoinRenderer and 
// ARCoinPositioner. Handles collection, locking, and state management.
// Reference: Docs/AR-COIN-DISPLAY-SPEC.md
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
    /// Prevents collection when GPS is inaccurate.
    /// </summary>
    public static class GPSAccuracyRequirements
    {
        /// <summary>Maximum GPS accuracy (meters) required to collect a coin</summary>
        public const float COLLECTION_ACCURACY = 25f;
        
        /// <summary>Minimum GPS accuracy to show coins on map</summary>
        public const float DISPLAY_ACCURACY = 200f;
    }
    
    /// <summary>
    /// Main controller for an AR coin.
    /// Coordinates rendering (ARCoinRenderer) and positioning (ARCoinPositioner).
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
        private bool debugMode = true;  // Enable for diagnostics
        
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
        
        /// <summary>Distance from player (meters) - uses GPS distance for accuracy</summary>
        public float DistanceFromPlayer => coinPositioner?.GPSDistance ?? float.MaxValue;
        
        /// <summary>AR distance from camera (for visual purposes only)</summary>
        public float ARDistanceFromCamera => coinRenderer?.DistanceToCamera ?? float.MaxValue;
        
        /// <summary>Current display mode</summary>
        public CoinDisplayMode DisplayMode => coinRenderer?.CurrentMode ?? CoinDisplayMode.Hidden;
        
        /// <summary>Is coin currently visible?</summary>
        public bool IsVisible => coinRenderer?.IsVisible ?? false;
        
        #endregion
        
        #region Events
        
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
            // Get or add required components
            coinRenderer = GetComponent<ARCoinRenderer>();
            if (coinRenderer == null)
            {
                coinRenderer = gameObject.AddComponent<ARCoinRenderer>();
            }
            
            coinPositioner = GetComponent<ARCoinPositioner>();
            if (coinPositioner == null)
            {
                coinPositioner = gameObject.AddComponent<ARCoinPositioner>();
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
            
            // Set layer and tag
            gameObject.tag = "Coin";
            int coinLayer = LayerMask.NameToLayer("Coin");
            if (coinLayer >= 0)
            {
                gameObject.layer = coinLayer;
            }
        }
        
        private void Start()
        {
            // Subscribe to renderer events
            if (coinRenderer != null)
            {
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
                coinRenderer.OnEnteredCollectionRange -= HandleEnteredRange;
                coinRenderer.OnExitedCollectionRange -= HandleExitedRange;
                coinRenderer.OnModeChanged -= HandleModeChanged;
            }
        }
        
        private void Update()
        {
            // Update visual states based on locked/in-range
            UpdateVisualState();
            
            // Debug: Log distance periodically
            if (debugMode && Time.frameCount % 180 == 0) // Every 3 seconds at 60fps
            {
                float gpsFromPositioner = coinPositioner?.GPSDistance ?? -1f;
                float arFromRenderer = coinRenderer?.DistanceToCamera ?? -1f;
                Debug.Log($"[CoinController] {CoinId} GPS={gpsFromPositioner:F1}m, AR={arFromRenderer:F1}m, InRange={IsInRange}, Mode={DisplayMode}");
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
            
            if (debugMode)
            {
                Debug.Log($"[CoinController] Initialized: {coinData.id}, Value: {coinData.GetDisplayValue()}, Locked: {locked}");
            }
            
            // Initialize positioner with GPS coordinates
            if (coinPositioner != null)
            {
                coinPositioner.Initialize(coinData);
            }
            
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
            var settings = coinRenderer.Settings;
            
            if (IsLocked)
            {
                coinRenderer.SetColor(settings.lockedColor);
            }
            else if (IsInRange)
            {
                coinRenderer.SetColor(settings.inRangeColor);
            }
            else
            {
                coinRenderer.SetColor(settings.goldColor);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleEnteredRange()
        {
            if (debugMode)
            {
                Debug.Log($"[CoinController] {CoinId} entered collection range");
            }
            
            // Update coin data
            if (CoinData != null)
            {
                CoinData.isInRange = true;
            }
            
            OnEnteredRange?.Invoke(this);
        }
        
        private void HandleExitedRange()
        {
            if (debugMode)
            {
                Debug.Log($"[CoinController] {CoinId} exited collection range");
            }
            
            // Update coin data
            if (CoinData != null)
            {
                CoinData.isInRange = false;
            }
            
            OnExitedRange?.Invoke(this);
        }
        
        private void HandleModeChanged(CoinDisplayMode oldMode, CoinDisplayMode newMode)
        {
            if (debugMode)
            {
                Debug.Log($"[CoinController] {CoinId} mode changed: {oldMode} → {newMode}");
            }
            
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
                
                if (debugMode)
                {
                    Debug.Log($"[CoinController] Collection blocked - locked: {CoinId}");
                }
                return false;
            }
            
            // Check range
            if (!IsInRange)
            {
                OnOutOfRangeTap?.Invoke(this);
                
                if (debugMode)
                {
                    Debug.Log($"[CoinController] Collection blocked - out of range: {CoinId}");
                }
                return false;
            }
            
            // Check GPS accuracy
            if (GPSManager.Exists && GPSManager.Instance.CurrentLocation != null)
            {
                float accuracy = GPSManager.Instance.CurrentLocation.horizontalAccuracy;
                if (accuracy > GPSAccuracyRequirements.COLLECTION_ACCURACY)
                {
                    Debug.LogWarning($"[CoinController] Collection blocked - GPS accuracy: {accuracy:F0}m");
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
            
            if (debugMode)
            {
                Debug.Log($"[CoinController] Collection started: {CoinId}");
            }
            
            // Play collection animation
            StartCoroutine(CollectionAnimation());
        }
        
        private System.Collections.IEnumerator CollectionAnimation()
        {
            float duration = 0.8f;
            float timer = 0f;
            
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            
            Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
            Vector3 targetPos = cam != null 
                ? cam.transform.position + cam.transform.forward * 0.5f 
                : startPos + Vector3.up;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
                
                // Move toward camera
                transform.position = Vector3.Lerp(startPos, targetPos, easeT);
                
                // Scale down
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easeT);
                
                // Spin faster
                if (coinModel != null)
                {
                    coinModel.transform.Rotate(0, 720f * Time.deltaTime, 0);
                }
                
                yield return null;
            }
            
            // Complete
            FinishCollection();
        }
        
        private void FinishCollection()
        {
            IsCollecting = false;
            IsCollected = true;
            
            if (debugMode)
            {
                Debug.Log($"[CoinController] Collection complete: {CoinId}, Value: {CoinData?.value ?? 0}");
            }
            
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
        /// Get proximity zone based on distance
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
        
        #endregion
        
        #region Debug
        
        /// <summary>Force collect this coin</summary>
        [ContextMenu("Debug: Force Collect")]
        public void DebugForceCollect()
        {
            IsLocked = false;
            if (coinRenderer != null)
            {
                coinRenderer.ForceMode(CoinDisplayMode.WorldLocked);
            }
            StartCollection();
        }
        
        /// <summary>Toggle locked state</summary>
        [ContextMenu("Debug: Toggle Locked")]
        public void DebugToggleLocked()
        {
            SetLocked(!IsLocked);
        }
        
        /// <summary>Print current state</summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log($"=== Coin {CoinId} ===");
            Debug.Log($"Value: {CoinData?.GetDisplayValue() ?? "null"}");
            Debug.Log($"Display Mode: {DisplayMode}");
            Debug.Log($"Distance: {DistanceFromPlayer:F1}m");
            Debug.Log($"Locked: {IsLocked}");
            Debug.Log($"InRange: {IsInRange}");
            Debug.Log($"Visible: {IsVisible}");
            Debug.Log($"Collected: {IsCollected}");
            Debug.Log("==================");
        }
        
        #endregion
    }
}
