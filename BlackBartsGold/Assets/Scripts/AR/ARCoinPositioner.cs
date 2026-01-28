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
            // Throttle updates
            if (Time.time - lastUpdateTime < UPDATE_INTERVAL) return;
            lastUpdateTime = Time.time;
            
            // ================================================================
            // ALWAYS update GPS distance - even when position is locked!
            // We need to know if user moves away from a locked coin.
            // ================================================================
            UpdateGPSDistanceOnly();
            
            // ================================================================
            // WORLD-SPACE ANCHORING BEHAVIOR
            // ================================================================
            // Once coin is placed in AR world space, it should STAY there.
            // AR Foundation's tracking handles keeping it stable as camera moves.
            // We only recalculate position if:
            //   1. This is initial placement (lastCalculatedPosition == zero)
            //   2. Player physically MOVED (GPS shows significant movement)
            //   3. Position is unlocked AND we need to update
            // 
            // We do NOT recalculate just because user rotated camera!
            // ================================================================
            
            if (IsPositionLocked)
            {
                // Position is locked - coin stays exactly where it is
                // AR tracking handles camera rotation around it
                
                // Check if user moved FAR away - unlock so coin can reposition
                if (GPSDistance > 15f) // Moved beyond billboard range
                {
                    UnlockPosition();
                    if (debugMode)
                    {
                        Debug.Log($"[ARCoinPositioner] Position UNLOCKED - user moved far away, GPS: {GPSDistance:F1}m");
                    }
                }
                return;
            }
            
            // Update position from GPS (only if player moved or first calculation)
            // The UpdatePositionFromGPS method now checks for significant movement
            UpdatePositionFromGPS();
            
            // Smooth position update (gentler smoothing for world-space feel)
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref positionVelocity,
                positionSmoothTime
            );
            
            // ================================================================
            // POSITION LOCKING: Lock when player is close
            // Once locked, coin stays fixed and AR tracking keeps it stable
            // ================================================================
            if (GPSDistance < 8f) // Lock a bit earlier for smoother transition
            {
                LockPosition();
                
                if (debugMode)
                {
                    Debug.Log($"[ARCoinPositioner] Position LOCKED at {transform.position} - GPS: {GPSDistance:F1}m (world-anchored)");
                }
            }
        }
        
        #endregion
        
        #region GPS Position Calculation
        
        // Track last GPS position for movement detection
        private double lastPlayerLat = 0;
        private double lastPlayerLng = 0;
        private const float GPS_MOVEMENT_THRESHOLD = 3f; // Only recalculate if moved 3+ meters
        
        /// <summary>
        /// Calculate AR position from GPS coordinates.
        /// Uses compass-aligned positioning (Pokémon GO pattern).
        /// 
        /// KEY FIX: Uses INITIAL compass heading, not current!
        /// This allows AR tracking to handle rotation while GPS handles translation.
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
            // WORLD-SPACE ANCHORING FIX
            // ================================================================
            // The critical insight from Pokémon GO research:
            // 
            // 1. Capture INITIAL compass heading when AR session starts
            // 2. Use that INITIAL heading to convert GPS bearing to AR bearing ONCE
            // 3. Let AR Foundation's tracking handle rotation as user moves camera
            // 4. Only recalculate position when GPS shows player MOVED (not rotated)
            //
            // Previous bug: Using CURRENT compass heading every frame caused
            // coins to move when user rotated phone, fighting AR tracking.
            //
            // How it works now:
            //   - AR session starts, camera facing north (compass = 0°)
            //   - _initialCompassHeading = 0°
            //   - Coin at GPS bearing 90° (due east)
            //   - adjustedBearing = 90° - 0° = 90°
            //   - Coin placed at AR position (x, 0, z) based on this bearing
            //   - User rotates phone to face east - AR TRACKING handles this!
            //   - Coin stays at same world position, appears in front now
            //   - This is CORRECT world-space behavior!
            // ================================================================
            
            // Check if player has physically moved (GPS change, not just rotation)
            bool playerMoved = false;
            if (lastPlayerLat != 0 && lastPlayerLng != 0)
            {
                LocationData lastPlayerLocation = new LocationData(lastPlayerLat, lastPlayerLng);
                float movedDistance = (float)lastPlayerLocation.DistanceTo(playerLocation);
                playerMoved = movedDistance > GPS_MOVEMENT_THRESHOLD;
                
                if (debugMode && playerMoved)
                {
                    Debug.Log($"[ARCoinPositioner] Player moved {movedDistance:F1}m - recalculating position");
                }
            }
            else
            {
                // First position calculation
                playerMoved = true;
            }
            
            // Update last known player position
            lastPlayerLat = playerLocation.latitude;
            lastPlayerLng = playerLocation.longitude;
            
            // Only recalculate coin position if this is initial placement or player moved significantly
            // This prevents constant position updates that would fight AR tracking
            if (!playerMoved && lastCalculatedPosition != Vector3.zero)
            {
                // Player hasn't moved significantly - keep current position
                // AR tracking will handle camera rotation
                return;
            }
            
            // Use INITIAL compass heading (captured when AR started) - NOT current!
            // This is the key fix for world-space anchoring
            float adjustedBearing = GPSBearing - _initialCompassHeading;
            
            // Convert bearing to radians
            float bearingRad = adjustedBearing * Mathf.Deg2Rad;
            
            // Calculate AR world position
            // In Unity: +X is right, +Z is forward (relative to initial camera facing)
            float x = GPSDistance * Mathf.Sin(bearingRad);
            float z = GPSDistance * Mathf.Cos(bearingRad);
            float y = heightAboveGround;
            
            Vector3 newPosition = new Vector3(x, y, z);
            
            // ================================================================
            // LIGHTSHIP ENHANCEMENT: Place on real surfaces if available
            // Uses Niantic meshing to find real-world surfaces
            // ================================================================
            if (LightshipManager.Exists && LightshipManager.Instance.IsMeshReady)
            {
                // Try to find a real surface at this position
                if (LightshipManager.Instance.IsPositionOnSurface(newPosition, out Vector3 surfacePosition))
                {
                    // Place coin on the detected surface + small offset
                    newPosition = surfacePosition + Vector3.up * 0.1f; // 10cm above surface
                    
                    if (debugMode)
                    {
                        Debug.Log($"[ARCoinPositioner] LIGHTSHIP: Placed on surface at {newPosition}");
                    }
                }
            }
            
            targetPosition = newPosition;
            lastCalculatedPosition = newPosition;
            
            if (debugMode)
            {
                Debug.Log($"[ARCoinPositioner] WORLD-ANCHORED: InitialCompass={_initialCompassHeading:F0}°, GPSBearing={GPSBearing:F0}° → adj={adjustedBearing:F0}°, dist={GPSDistance:F1}m, pos=({x:F1}, {y:F1}, {z:F1})");
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
        
        /// <summary>
        /// Update only GPS distance and bearing WITHOUT updating position.
        /// Called even when position is locked to track if user moves away.
        /// </summary>
        private void UpdateGPSDistanceOnly()
        {
            LocationData playerLocation = GetPlayerLocation();
            if (playerLocation == null) return;
            
            LocationData coinLocation = new LocationData(latitude, longitude);
            
            // Update distance and bearing
            GPSDistance = (float)playerLocation.DistanceTo(coinLocation);
            GPSBearing = (float)playerLocation.BearingTo(coinLocation);
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
            
            // Reset state for new coin
            IsPositionLocked = false;
            lastPlayerLat = 0;
            lastPlayerLng = 0;
            lastCalculatedPosition = Vector3.zero;
            
            // Ensure we have initial compass heading before calculating position
            if (!_hasInitialHeading)
            {
                // Try to capture now, or start coroutine
                if (Input.compass.enabled && Input.compass.headingAccuracy >= 0)
                {
                    CaptureInitialCompassHeading();
                }
                else
                {
                    StartCoroutine(CaptureCompassHeadingCoroutine());
                }
            }
            
            // Calculate initial position using INITIAL compass heading
            UpdatePositionFromGPS();
            
            // Set initial position immediately (no smoothing)
            transform.position = targetPosition;
            
            if (debugMode)
            {
                Debug.Log($"[ARCoinPositioner] WORLD-ANCHORED Init: ({latitude:F6}, {longitude:F6}), GPS={GPSDistance:F1}m, Compass={_initialCompassHeading:F0}°, Pos={transform.position}");
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
            float currentCompass = Input.compass.enabled ? Input.compass.trueHeading : -1f;
            float adjustedBearing = GPSBearing - _initialCompassHeading;
            
            Debug.Log("=== ARCoinPositioner Status (WORLD-ANCHORED) ===");
            Debug.Log($"GPS Location: ({latitude:F6}, {longitude:F6})");
            Debug.Log($"GPS Distance: {GPSDistance:F1}m");
            Debug.Log($"GPS Bearing: {GPSBearing:F0}° (to true north)");
            Debug.Log($"---");
            Debug.Log($"Initial Compass: {_initialCompassHeading:F0}° (captured={_hasInitialHeading})");
            Debug.Log($"Current Compass: {currentCompass:F0}°");
            Debug.Log($"Adjusted Bearing: {adjustedBearing:F0}° (GPS - Initial = AR direction)");
            Debug.Log($"---");
            Debug.Log($"Position Locked: {IsPositionLocked}");
            Debug.Log($"AR World Position: {transform.position}");
            Debug.Log($"Target Position: {targetPosition}");
            Debug.Log("==============================================");
        }
        
        [ContextMenu("Debug: Force Update Position")]
        public void DebugForceUpdate()
        {
            IsPositionLocked = false;
            lastPlayerLat = 0; // Force recalculation
            lastPlayerLng = 0;
            lastCalculatedPosition = Vector3.zero;
            UpdatePositionFromGPS();
            transform.position = targetPosition;
            Debug.Log($"[ARCoinPositioner] Forced position update to {transform.position}");
        }
        
        [ContextMenu("Debug: Recapture Compass")]
        public void DebugRecaptureCompass()
        {
            ResetCompassHeading();
            CaptureInitialCompassHeading();
            Debug.Log($"[ARCoinPositioner] Recaptured compass: {_initialCompassHeading:F0}°");
            
            // Force position recalculation with new heading
            DebugForceUpdate();
        }
        
        #endregion
    }
}
