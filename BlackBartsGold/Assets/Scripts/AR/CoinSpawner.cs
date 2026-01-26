// ============================================================================
// CoinSpawner.cs
// Black Bart's Gold - GPS to AR Position Converter
// Path: Assets/Scripts/AR/CoinSpawner.cs
// ============================================================================
// Converts GPS coordinates to AR world positions. Updates coin positions
// as the player moves through the real world.
// Reference: BUILD-GUIDE.md Prompt 3.3
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Converts GPS coordinates to AR world positions.
    /// Works with CoinManager to position coins in the AR scene.
    /// </summary>
    public class CoinSpawner : MonoBehaviour
    {
        #region Singleton
        
        private static CoinSpawner _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static CoinSpawner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CoinSpawner>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CoinSpawner");
                        _instance = go.AddComponent<CoinSpawner>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Maximum distance to render coins (meters)")]
        private float maxRenderDistance = 500f; // Increased for better visibility during testing
        
        [SerializeField]
        [Tooltip("Height above ground for coins (meters)")]
        private float coinHeight = 0.5f; // Lowered for easier visibility (knee height)
        
        [SerializeField]
        [Tooltip("Update positions every X seconds")]
        private float updateInterval = 2f;
        
        [SerializeField]
        [Tooltip("Minimum player movement before recalculating (meters)")]
        private float minMovementThreshold = 2f;
        
        [Header("AR Origin")]
        [SerializeField]
        [Tooltip("Reference to XR Origin/AR Session Origin")]
        private Transform arOrigin;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        [SerializeField]
        private bool drawDebugGizmos = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current player GPS position (origin for AR calculations)
        /// </summary>
        public LocationData PlayerLocation { get; private set; }
        
        /// <summary>
        /// Last position where we updated AR positions
        /// </summary>
        public LocationData LastUpdateLocation { get; private set; }
        
        /// <summary>
        /// Is GPS tracking active?
        /// </summary>
        public bool IsTrackingGPS { get; private set; } = false;
        
        #endregion
        
        #region Private Fields
        
        private float lastUpdateTime = 0f;
        private Dictionary<string, Vector3> coinARPositions = new Dictionary<string, Vector3>();
        
        // Compass heading when AR started (for aligning AR world with real world)
        private float _initialCompassHeading = 0f;
        private bool _hasInitialHeading = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // Find AR Origin if not set
            if (arOrigin == null)
            {
                var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    arOrigin = xrOrigin.transform;
                }
            }
        }
        
        private void Start()
        {
            // ================================================================
            // COMPASS SETUP - Pokémon GO style world alignment
            // This aligns AR coordinates with real-world compass directions
            // ================================================================
            Input.compass.enabled = true;
            StartCoroutine(CaptureInitialCompassHeading());
            
            // Subscribe to GPSManager location updates instead of managing our own GPS
            if (BlackBartsGold.Location.GPSManager.Instance != null)
            {
                BlackBartsGold.Location.GPSManager.Instance.OnLocationUpdated += OnGPSLocationUpdated;
                Log("Subscribed to GPSManager location updates");
                
                // Get initial location if already available
                var currentLocation = BlackBartsGold.Location.GPSManager.Instance.CurrentLocation;
                if (currentLocation != null)
                {
                    PlayerLocation = currentLocation.Clone();
                    LastUpdateLocation = PlayerLocation?.Clone();
                    Log($"Got initial location from GPSManager: {PlayerLocation?.ToCoordinateString()}");
                }
            }
            else
            {
                // Fallback to own GPS tracking if GPSManager not available
                Log("GPSManager not found, using fallback GPS tracking");
                StartGPSTracking();
            }
        }
        
        /// <summary>
        /// Capture the compass heading when AR starts
        /// This is used to align GPS bearings with the AR coordinate system
        /// </summary>
        private System.Collections.IEnumerator CaptureInitialCompassHeading()
        {
            // Wait for compass to stabilize
            yield return new WaitForSeconds(0.5f);
            
            // Try to get a valid compass reading
            for (int i = 0; i < 10; i++)
            {
                if (Input.compass.enabled && Input.compass.headingAccuracy >= 0)
                {
                    _initialCompassHeading = Input.compass.trueHeading;
                    _hasInitialHeading = true;
                    Debug.Log($"[CoinSpawner] 🧭 Captured initial compass heading: {_initialCompassHeading:F1}°");
                    yield break;
                }
                yield return new WaitForSeconds(0.2f);
            }
            
            // Fallback: use camera forward direction as "north"
            Debug.LogWarning("[CoinSpawner] ⚠️ Compass not available, using camera forward as north");
            _initialCompassHeading = 0f;
            _hasInitialHeading = true;
        }
        
        /// <summary>
        /// Handle GPS location updates from GPSManager
        /// </summary>
        private void OnGPSLocationUpdated(LocationData location)
        {
            if (location == null) return;
            
            PlayerLocation = location.Clone();
            IsTrackingGPS = true;
            
            // Check if we've moved enough to warrant recalculation
            if (ShouldRecalculatePositions())
            {
                RecalculateAllCoinPositions();
            }
        }
        
        private void Update()
        {
            // Periodic position updates (fallback for when not using GPSManager events)
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                
                // Only use our own GPS tracking if not using GPSManager
                if (BlackBartsGold.Location.GPSManager.Instance == null)
                {
                    UpdatePlayerLocation();
                }
                
                // Check if we've moved enough to warrant recalculation
                if (ShouldRecalculatePositions())
                {
                    RecalculateAllCoinPositions();
                }
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from GPSManager events
            if (GPSManager.Instance != null)
            {
                GPSManager.Instance.OnLocationUpdated -= OnGPSLocationUpdated;
            }
            
            StopGPSTracking();
        }
        
        #endregion
        
        #region GPS Tracking
        
        /// <summary>
        /// Start GPS location tracking
        /// </summary>
        public void StartGPSTracking()
        {
            if (IsTrackingGPS) return;
            
            // Check if location services are available
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[CoinSpawner] Location services not enabled by user");
                return;
            }
            
            // Start location service
            Input.location.Start(5f, 2f); // 5m accuracy, update every 2m
            
            IsTrackingGPS = true;
            Log("GPS tracking started");
            
            // Get initial position
            StartCoroutine(WaitForInitialGPS());
        }
        
        private System.Collections.IEnumerator WaitForInitialGPS()
        {
            // Wait for GPS to initialize
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }
            
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("[CoinSpawner] GPS initialization failed");
                IsTrackingGPS = false;
                yield break;
            }
            
            // Get initial location
            UpdatePlayerLocation();
            LastUpdateLocation = PlayerLocation?.Clone();
            
            Log($"GPS initialized: {PlayerLocation?.ToCoordinateString() ?? "null"}");
        }
        
        /// <summary>
        /// Stop GPS tracking
        /// </summary>
        public void StopGPSTracking()
        {
            if (!IsTrackingGPS) return;
            
            Input.location.Stop();
            IsTrackingGPS = false;
            
            Log("GPS tracking stopped");
        }
        
        /// <summary>
        /// Update player's current GPS location
        /// </summary>
        private void UpdatePlayerLocation()
        {
            if (Input.location.status != LocationServiceStatus.Running)
            {
                return;
            }
            
            LocationInfo info = Input.location.lastData;
            
            PlayerLocation = new LocationData
            {
                latitude = info.latitude,
                longitude = info.longitude,
                altitude = info.altitude,
                horizontalAccuracy = info.horizontalAccuracy,
                verticalAccuracy = info.verticalAccuracy,
                timestamp = System.DateTime.UtcNow.ToString("o")
            };
            
            // Also update PlayerData if available
            if (PlayerData.Exists)
            {
                PlayerData.Instance.UpdateLocation(PlayerLocation);
            }
        }
        
        /// <summary>
        /// Set player location manually (for testing without GPS)
        /// </summary>
        public void SetPlayerLocationManually(double latitude, double longitude)
        {
            Debug.Log($"[CoinSpawner] 📍 SetPlayerLocationManually called: ({latitude:F6}, {longitude:F6})");
            
            PlayerLocation = new LocationData(latitude, longitude);
            LastUpdateLocation = PlayerLocation.Clone();
            IsTrackingGPS = true; // Mark as tracking since we have a location
            
            Debug.Log($"[CoinSpawner] ✅ PlayerLocation set: {PlayerLocation.ToCoordinateString()}");
            
            // Note: We don't call RecalculateAllCoinPositions here anymore
            // because UIManager will call it separately after setting the location
        }
        
        #endregion
        
        #region Position Calculation
        
        /// <summary>
        /// Check if we've moved enough to recalculate
        /// </summary>
        private bool ShouldRecalculatePositions()
        {
            if (PlayerLocation == null) return false;
            if (LastUpdateLocation == null) return true;
            
            float distance = PlayerLocation.DistanceTo(LastUpdateLocation);
            return distance >= minMovementThreshold;
        }
        
        /// <summary>
        /// Recalculate AR positions for all active coins
        /// </summary>
        public void RecalculateAllCoinPositions()
        {
            Debug.Log($"[CoinSpawner] 🔄 RecalculateAllCoinPositions called");
            Debug.Log($"[CoinSpawner] PlayerLocation: {(PlayerLocation != null ? $"({PlayerLocation.latitude:F6}, {PlayerLocation.longitude:F6})" : "NULL")}");
            
            if (PlayerLocation == null)
            {
                Debug.LogWarning("[CoinSpawner] ⚠️ Cannot recalculate - PlayerLocation is NULL!");
                return;
            }
            
            if (CoinManager.Instance == null)
            {
                Debug.LogWarning("[CoinSpawner] ⚠️ Cannot recalculate - CoinManager.Instance is NULL!");
                return;
            }
            
            Debug.Log($"[CoinSpawner] Active coins to position: {CoinManager.Instance.ActiveCoinCount}");
            
            LastUpdateLocation = PlayerLocation.Clone();
            
            int positionedCount = 0;
            foreach (var coin in CoinManager.Instance.ActiveCoins)
            {
                if (coin != null)
                {
                    Debug.Log($"[CoinSpawner] Positioning coin: {coin.CoinId}");
                    Debug.Log($"[CoinSpawner]   Coin GPS: ({coin.CoinData?.latitude:F6}, {coin.CoinData?.longitude:F6})");
                    
                    UpdateCoinPosition(coin);
                    
                    Debug.Log($"[CoinSpawner]   Final AR position: {coin.transform.position}");
                    positionedCount++;
                }
            }
            
            Debug.Log($"[CoinSpawner] ✅ Recalculated {positionedCount} coin positions");
        }
        
        /// <summary>
        /// Update a single coin's AR position
        /// </summary>
        public void UpdateCoinPosition(CoinController coin)
        {
            if (coin == null)
            {
                Debug.LogWarning("[CoinSpawner] UpdateCoinPosition: coin is null");
                return;
            }
            
            if (coin.CoinData == null)
            {
                Debug.LogWarning($"[CoinSpawner] UpdateCoinPosition: coin {coin.name} has null CoinData");
                return;
            }
            
            if (PlayerLocation == null)
            {
                Debug.LogWarning("[CoinSpawner] UpdateCoinPosition: PlayerLocation is null");
                return;
            }
            
            // Create LocationData for coin
            LocationData coinLocation = new LocationData(
                coin.CoinData.latitude,
                coin.CoinData.longitude
            );
            
            // Check if within render distance
            float distance = PlayerLocation.DistanceTo(coinLocation);
            Debug.Log($"[CoinSpawner]   Distance to coin: {distance:F1}m (max: {maxRenderDistance}m)");
            
            if (distance > maxRenderDistance)
            {
                Debug.LogWarning($"[CoinSpawner]   Coin is too far ({distance:F1}m > {maxRenderDistance}m)");
                return;
            }
            
            // Convert GPS to AR position
            Vector3 arPosition = GpsToArPosition(coinLocation);
            Debug.Log($"[CoinSpawner]   Calculated AR position: {arPosition}");
            
            // Apply to coin
            coin.transform.position = arPosition;
            
            // Store for debug
            coinARPositions[coin.CoinId] = arPosition;
        }
        
        /// <summary>
        /// Convert GPS coordinates to AR world position
        /// Uses compass heading to align AR world with real-world directions (Pokémon GO style)
        /// </summary>
        public Vector3 GpsToArPosition(LocationData targetLocation)
        {
            if (PlayerLocation == null)
            {
                Debug.LogWarning("[CoinSpawner] GpsToArPosition: PlayerLocation is null, returning zero");
                return Vector3.zero;
            }
            
            // Calculate distance and bearing from player to target
            float distance = PlayerLocation.DistanceTo(targetLocation);
            float gpsBearing = PlayerLocation.BearingTo(targetLocation); // 0° = North, 90° = East
            
            // ================================================================
            // COMPASS ALIGNMENT (Pokémon GO approach)
            // ================================================================
            // GPS bearing is relative to true north
            // But AR's +Z axis points wherever the camera was facing when AR started
            // We need to adjust the bearing by the initial compass heading
            //
            // Example: 
            //   - Coin is at GPS bearing 90° (due east)
            //   - User was facing 45° (northeast) when AR started
            //   - Adjusted bearing = 90° - 45° = 45°
            //   - Coin should appear 45° to the right of where user was looking
            // ================================================================
            
            float adjustedBearing = gpsBearing;
            if (_hasInitialHeading)
            {
                // Subtract initial heading to align GPS north with AR forward
                adjustedBearing = gpsBearing - _initialCompassHeading;
                Debug.Log($"[CoinSpawner]   Compass adjustment: GPS bearing {gpsBearing:F1}° - heading {_initialCompassHeading:F1}° = {adjustedBearing:F1}°");
            }
            
            // Convert adjusted bearing to radians
            float bearingRad = adjustedBearing * Mathf.Deg2Rad;
            
            // Calculate X (left-right) and Z (forward-back) offsets
            // In Unity: +X is right, +Z is forward
            float x = distance * Mathf.Sin(bearingRad);
            float z = distance * Mathf.Cos(bearingRad);
            
            // Y is fixed height - using lower height for easier visibility
            float y = coinHeight;
            
            // Create position relative to AR origin (or world origin if no AR origin)
            Vector3 position = new Vector3(x, y, z);
            
            Debug.Log($"[CoinSpawner]   GPS conversion: dist={distance:F1}m, bearing={gpsBearing:F1}° → AR pos: ({x:F1}, {y:F1}, {z:F1})");
            
            if (arOrigin != null)
            {
                Vector3 worldPos = arOrigin.TransformPoint(position);
                Debug.Log($"[CoinSpawner]   World position (via AR origin): {worldPos}");
                position = worldPos;
            }
            
            return position;
        }
        
        /// <summary>
        /// Convert AR position back to GPS (for placing coins)
        /// </summary>
        public LocationData ArPositionToGps(Vector3 arPosition)
        {
            if (PlayerLocation == null)
            {
                return null;
            }
            
            // Get position relative to AR origin
            Vector3 relativePos = arPosition;
            if (arOrigin != null)
            {
                relativePos = arOrigin.InverseTransformPoint(arPosition);
            }
            
            // Calculate bearing and distance
            float distance = new Vector2(relativePos.x, relativePos.z).magnitude;
            float bearing = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
            
            // Normalize bearing to 0-360
            if (bearing < 0) bearing += 360f;
            
            // Calculate GPS offset
            // This is approximate and works well for short distances
            double latOffset = distance * Mathf.Cos(bearing * Mathf.Deg2Rad) / 111320.0;
            double lngOffset = distance * Mathf.Sin(bearing * Mathf.Deg2Rad) / 
                              (111320.0 * System.Math.Cos(PlayerLocation.latitude * System.Math.PI / 180.0));
            
            return new LocationData(
                PlayerLocation.latitude + latOffset,
                PlayerLocation.longitude + lngOffset
            );
        }
        
        #endregion
        
        #region Spawn Helpers
        
        /// <summary>
        /// Spawn coins from a list of Coin data
        /// </summary>
        public void SpawnCoinsFromData(List<Coin> coins)
        {
            if (coins == null || CoinManager.Instance == null) return;
            
            foreach (var coinData in coins)
            {
                SpawnCoinFromData(coinData);
            }
        }
        
        /// <summary>
        /// Spawn a single coin from data
        /// </summary>
        public CoinController SpawnCoinFromData(Coin coinData)
        {
            if (coinData == null || CoinManager.Instance == null) return null;
            
            // Spawn through CoinManager
            CoinController coin = CoinManager.Instance.SpawnCoin(coinData);
            
            if (coin != null)
            {
                // Update position based on GPS
                UpdateCoinPosition(coin);
            }
            
            return coin;
        }
        
        /// <summary>
        /// Create and spawn a coin at GPS coordinates
        /// </summary>
        public CoinController SpawnCoinAtGPS(double latitude, double longitude, float value)
        {
            Coin coinData = new Coin(
                System.Guid.NewGuid().ToString(),
                CoinType.Fixed,
                value,
                latitude,
                longitude
            );
            
            return SpawnCoinFromData(coinData);
        }
        
        /// <summary>
        /// Spawn a coin at offset from player (for testing)
        /// </summary>
        public CoinController SpawnCoinNearPlayer(float metersNorth, float metersEast, float value)
        {
            if (PlayerLocation == null)
            {
                Debug.LogWarning("[CoinSpawner] No player location - using default");
                SetPlayerLocationManually(37.7749, -122.4194); // San Francisco
            }
            
            LocationData offsetLocation = LocationData.CreateAtOffset(
                PlayerLocation, metersNorth, metersEast
            );
            
            return SpawnCoinAtGPS(offsetLocation.latitude, offsetLocation.longitude, value);
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[CoinSpawner] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print current GPS info
        /// </summary>
        [ContextMenu("Debug: Print GPS Info")]
        public void DebugPrintGPSInfo()
        {
            Debug.Log("=== GPS Info ===");
            Debug.Log($"Tracking: {IsTrackingGPS}");
            Debug.Log($"Status: {Input.location.status}");
            Debug.Log($"Player Location: {PlayerLocation?.ToCoordinateString() ?? "null"}");
            Debug.Log($"Accuracy: {PlayerLocation?.horizontalAccuracy ?? 0}m");
            Debug.Log($"Last Update: {LastUpdateLocation?.ToCoordinateString() ?? "null"}");
            Debug.Log("================");
        }
        
        /// <summary>
        /// Debug: Spawn test coins around player
        /// </summary>
        [ContextMenu("Debug: Spawn Test Coins Around Player")]
        public void DebugSpawnTestCoinsAroundPlayer()
        {
            // Spawn coins in a circle around player
            float[] distances = { 5, 10, 20, 30, 50 };
            float[] values = { 1, 2, 5, 10, 25 };
            
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72 * Mathf.Deg2Rad; // 72 degrees apart
                float north = distances[i] * Mathf.Cos(angle);
                float east = distances[i] * Mathf.Sin(angle);
                
                SpawnCoinNearPlayer(north, east, values[i]);
            }
            
            Debug.Log("[CoinSpawner] Spawned 5 test coins around player");
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawDebugGizmos) return;
            
            // Draw coin positions
            Gizmos.color = Color.yellow;
            foreach (var kvp in coinARPositions)
            {
                Gizmos.DrawWireSphere(kvp.Value, 0.2f);
            }
            
            // Draw max render distance circle
            if (arOrigin != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                DrawGizmoCircle(arOrigin.position, maxRenderDistance, 32);
            }
        }
        
        private void DrawGizmoCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
        #endif
        
        #endregion
    }
}
