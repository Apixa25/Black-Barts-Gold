// ============================================================================
// ARCoinPositioner.cs
// Black Bart's Gold - GPS to AR Position Converter
// Path: Assets/Scripts/AR/ARCoinPositioner.cs
// ============================================================================
// Converts GPS coordinates to AR world positions with continuous updates.
// Uses compass-aligned positioning (Pokémon GO pattern).
// Reference: Docs/AR-COIN-DISPLAY-SPEC.md, pokemon-go-patterns/SKILL.md
// ============================================================================

using UnityEngine;
using BlackBartsGold.Location;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Converts GPS coordinates to AR world positions.
    /// Continuously updates position based on player movement and compass.
    /// Stops updating when coin is world-locked (near player).
    /// </summary>
    public class ARCoinPositioner : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("GPS Data")]
        [SerializeField]
        [Tooltip("Target latitude")]
        private double latitude;
        
        [SerializeField]
        [Tooltip("Target longitude")]
        private double longitude;
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Height above ground (meters)")]
        private float heightAboveGround = 1.0f;
        
        [SerializeField]
        [Tooltip("Position smoothing time")]
        private float positionSmoothTime = 0.3f;
        
        [SerializeField]
        [Tooltip("Minimum movement to trigger update (meters)")]
        private float minMovementThreshold = 0.5f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Target GPS latitude
        /// </summary>
        public double Latitude
        {
            get => latitude;
            set => latitude = value;
        }
        
        /// <summary>
        /// Target GPS longitude
        /// </summary>
        public double Longitude
        {
            get => longitude;
            set => longitude = value;
        }
        
        /// <summary>
        /// Is position locked (world-locked mode)?
        /// </summary>
        public bool IsPositionLocked { get; private set; } = false;
        
        /// <summary>
        /// Current GPS distance to coin (meters)
        /// </summary>
        public float GPSDistance { get; private set; } = float.MaxValue;
        
        /// <summary>
        /// Current GPS bearing to coin (degrees, 0=North)
        /// </summary>
        public float GPSBearing { get; private set; } = 0f;
        
        #endregion
        
        #region Static Compass Alignment
        
        // Initial compass heading (captured at AR session start)
        // Shared across all coin positioners for consistency
        private static float _initialCompassHeading = 0f;
        private static bool _hasInitialHeading = false;
        
        /// <summary>
        /// Capture initial compass heading. Call this once when AR session starts.
        /// This aligns GPS north with AR coordinate system.
        /// </summary>
        public static void CaptureInitialCompassHeading()
        {
            if (Input.compass.enabled && Input.compass.headingAccuracy >= 0)
            {
                _initialCompassHeading = Input.compass.trueHeading;
                _hasInitialHeading = true;
                Debug.Log($"[ARCoinPositioner] Captured initial compass heading: {_initialCompassHeading:F1}°");
            }
            else
            {
                // Fallback: use 0 (assume camera forward is north)
                _initialCompassHeading = 0f;
                _hasInitialHeading = true;
                Debug.LogWarning("[ARCoinPositioner] Compass not available, using 0° as initial heading");
            }
        }
        
        /// <summary>
        /// Reset compass heading (call when AR session restarts)
        /// </summary>
        public static void ResetCompassHeading()
        {
            _hasInitialHeading = false;
            _initialCompassHeading = 0f;
        }
        
        /// <summary>
        /// Has initial compass heading been captured?
        /// </summary>
        public static bool HasInitialHeading => _hasInitialHeading;
        
        #endregion
        
        #region Private Fields
        
        // Position smoothing
        private Vector3 targetPosition;
        private Vector3 positionVelocity;
        private Vector3 lastCalculatedPosition;
        
        // Reference to renderer for mode checking
        private ARCoinRenderer coinRenderer;
        
        // Update timing
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.2f; // 5 Hz for GPS updates
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            coinRenderer = GetComponent<ARCoinRenderer>();
            targetPosition = transform.position;
            lastCalculatedPosition = transform.position;
        }
        
        private void Start()
        {
            // Ensure compass is enabled
            Input.compass.enabled = true;
            
            // Capture initial heading if not already done
            if (!_hasInitialHeading)
            {
                StartCoroutine(CaptureCompassHeadingCoroutine());
            }
            
            // Calculate initial position
            UpdatePositionFromGPS();
        }
        
        private void Update()
        {
            // Don't update if position is locked
            if (IsPositionLocked) return;
            
            // Throttle updates
            if (Time.time - lastUpdateTime < UPDATE_INTERVAL) return;
            lastUpdateTime = Time.time;
            
            // Update position from GPS
            UpdatePositionFromGPS();
            
            // Smooth position update
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref positionVelocity,
                positionSmoothTime
            );
            
            // ================================================================
            // POSITION LOCKING: Only lock when GPS says we're ACTUALLY close
            // Don't use AR mode - use GPS distance directly!
            // This prevents the bug where AR distance triggers early lock.
            // ================================================================
            if (GPSDistance < 5f) // Within collection range based on GPS
            {
                LockPosition();
                
                if (debugMode)
                {
                    Debug.Log($"[ARCoinPositioner] Position LOCKED - GPS distance: {GPSDistance:F1}m (within 5m collection range)");
                }
            }
        }
        
        #endregion
        
        #region GPS Position Calculation
        
        /// <summary>
        /// Calculate AR position from GPS coordinates.
        /// Uses compass-aligned positioning (Pokémon GO pattern).
        /// </summary>
        private void UpdatePositionFromGPS()
        {
            // Get player location
            LocationData playerLocation = GetPlayerLocation();
            if (playerLocation == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning("[ARCoinPositioner] No player location available");
                }
                return;
            }
            
            // Create coin location
            LocationData coinLocation = new LocationData(latitude, longitude);
            
            // Calculate distance and bearing
            GPSDistance = (float)playerLocation.DistanceTo(coinLocation);
            GPSBearing = (float)playerLocation.BearingTo(coinLocation);
            
            // ================================================================
            // COMPASS ALIGNMENT (Pokémon GO pattern)
            // ================================================================
            // GPS bearing is relative to true north (0° = North, 90° = East)
            // AR's +Z axis points wherever the camera was facing when AR started
            // We adjust bearing by initial compass heading to align
            //
            // Example:
            //   - Coin is at GPS bearing 90° (due east)
            //   - User was facing 45° (northeast) when AR started
            //   - Adjusted bearing = 90° - 45° = 45°
            //   - Coin appears 45° to the right of AR forward
            // ================================================================
            
            float adjustedBearing = GPSBearing;
            if (_hasInitialHeading)
            {
                adjustedBearing = GPSBearing - _initialCompassHeading;
            }
            
            // Convert bearing to radians
            float bearingRad = adjustedBearing * Mathf.Deg2Rad;
            
            // Calculate AR position
            // In Unity: +X is right, +Z is forward
            float x = GPSDistance * Mathf.Sin(bearingRad);
            float z = GPSDistance * Mathf.Cos(bearingRad);
            float y = heightAboveGround;
            
            Vector3 newPosition = new Vector3(x, y, z);
            
            // Only update if significantly different (reduces jitter)
            if (Vector3.Distance(newPosition, lastCalculatedPosition) > minMovementThreshold)
            {
                targetPosition = newPosition;
                lastCalculatedPosition = newPosition;
                
                if (debugMode)
                {
                    Debug.Log($"[ARCoinPositioner] GPS update: dist={GPSDistance:F1}m, " +
                              $"bearing={GPSBearing:F0}° (adj={adjustedBearing:F0}°) → " +
                              $"AR pos=({x:F1}, {y:F1}, {z:F1})");
                }
            }
        }
        
        /// <summary>
        /// Get current player location from GPSManager
        /// </summary>
        private LocationData GetPlayerLocation()
        {
            if (GPSManager.Exists && GPSManager.Instance.CurrentLocation != null)
            {
                return GPSManager.Instance.CurrentLocation;
            }
            return null;
        }
        
        #endregion
        
        #region Position Locking
        
        /// <summary>
        /// Lock position (stop GPS updates). Called when entering world-locked mode.
        /// </summary>
        public void LockPosition()
        {
            if (IsPositionLocked) return;
            
            IsPositionLocked = true;
            targetPosition = transform.position;
            positionVelocity = Vector3.zero;
            
            if (debugMode)
            {
                Debug.Log($"[ARCoinPositioner] Position LOCKED at {transform.position}");
            }
        }
        
        /// <summary>
        /// Unlock position (resume GPS updates). Called when exiting world-locked mode.
        /// </summary>
        public void UnlockPosition()
        {
            if (!IsPositionLocked) return;
            
            IsPositionLocked = false;
            
            if (debugMode)
            {
                Debug.Log("[ARCoinPositioner] Position UNLOCKED, resuming GPS updates");
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize with coin data
        /// </summary>
        public void Initialize(Coin coinData)
        {
            if (coinData == null) return;
            
            latitude = coinData.latitude;
            longitude = coinData.longitude;
            heightAboveGround = coinData.heightOffset > 0 ? coinData.heightOffset : 1.0f;
            
            // Reset lock state for new coin
            IsPositionLocked = false;
            
            // Calculate initial position
            UpdatePositionFromGPS();
            
            // Set initial position immediately (no smoothing)
            transform.position = targetPosition;
            
            if (debugMode)
            {
                Debug.Log($"[ARCoinPositioner] Initialized: ({latitude:F6}, {longitude:F6}), GPSDistance={GPSDistance:F1}m, Position={transform.position}");
            }
            
            // If GPS distance is still MaxValue, player location wasn't available yet
            // Try again in a coroutine
            if (GPSDistance >= float.MaxValue - 1f)
            {
                StartCoroutine(RetryInitialPositionUpdate());
            }
        }
        
        /// <summary>
        /// Retry getting initial position if GPS wasn't ready
        /// </summary>
        private System.Collections.IEnumerator RetryInitialPositionUpdate()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForSeconds(0.5f);
                
                UpdatePositionFromGPS();
                transform.position = targetPosition;
                
                if (GPSDistance < float.MaxValue - 1f)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[ARCoinPositioner] Retry successful: GPSDistance={GPSDistance:F1}m, Position={transform.position}");
                    }
                    yield break;
                }
            }
            
            Debug.LogWarning("[ARCoinPositioner] Could not get GPS position after 10 retries");
        }
        
        /// <summary>
        /// Initialize with raw coordinates
        /// </summary>
        public void Initialize(double lat, double lng, float height = 1.0f)
        {
            latitude = lat;
            longitude = lng;
            heightAboveGround = height;
            
            // Calculate initial position
            UpdatePositionFromGPS();
            
            // Set initial position immediately
            transform.position = targetPosition;
        }
        
        #endregion
        
        #region Compass Capture Coroutine
        
        private System.Collections.IEnumerator CaptureCompassHeadingCoroutine()
        {
            // Wait for compass to stabilize
            yield return new WaitForSeconds(0.5f);
            
            // Try multiple times
            for (int i = 0; i < 10; i++)
            {
                if (Input.compass.enabled && Input.compass.headingAccuracy >= 0)
                {
                    CaptureInitialCompassHeading();
                    yield break;
                }
                yield return new WaitForSeconds(0.2f);
            }
            
            // Fallback
            Debug.LogWarning("[ARCoinPositioner] Could not get compass reading, using 0°");
            _initialCompassHeading = 0f;
            _hasInitialHeading = true;
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Print Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== ARCoinPositioner Status ===");
            Debug.Log($"GPS: ({latitude:F6}, {longitude:F6})");
            Debug.Log($"Distance: {GPSDistance:F1}m");
            Debug.Log($"Bearing: {GPSBearing:F0}°");
            Debug.Log($"Position Locked: {IsPositionLocked}");
            Debug.Log($"AR Position: {transform.position}");
            Debug.Log($"Initial Compass: {_initialCompassHeading:F0}° (has={_hasInitialHeading})");
            Debug.Log("===============================");
        }
        
        [ContextMenu("Debug: Force Update")]
        public void DebugForceUpdate()
        {
            IsPositionLocked = false;
            UpdatePositionFromGPS();
            transform.position = targetPosition;
        }
        
        #endregion
    }
}
