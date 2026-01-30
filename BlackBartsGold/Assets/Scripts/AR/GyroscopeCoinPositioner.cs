// ============================================================================
// GyroscopeCoinPositioner.cs
// Black Bart's Gold - Gyroscope-Based AR Coin Positioning
// Path: Assets/Scripts/AR/GyroscopeCoinPositioner.cs
// ============================================================================
// SOLUTION: When ARCore tracking fails (camera position never changes),
// we use gyroscope + compass to position the coin in world space.
//
// This is how Pokemon GO's "basic AR mode" works:
// 1. Calculate the COMPASS BEARING to the target (GPS)
// 2. Use GYROSCOPE to know which direction the phone is pointing
// 3. When phone points at target bearing, coin appears in front
// 4. Rotate phone away, coin moves off screen
//
// This approach works WITHOUT ARCore tracking!
// ============================================================================

using System.Collections;
using UnityEngine;
using BlackBartsGold.Location;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Positions the AR coin using gyroscope and compass when AR tracking isn't available.
    /// This is the Pokemon GO "basic AR" approach.
    /// </summary>
    public class GyroscopeCoinPositioner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float displayDistance = 4f;
        [SerializeField] private float displayHeight = 1.2f;
        [SerializeField] private float smoothSpeed = 5f;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = true;
        
        // Target coin data
        private double targetLatitude;
        private double targetLongitude;
        private bool hasTarget = false;
        
        // Gyroscope
        private Gyroscope gyro;
        private bool gyroEnabled = false;
        private Quaternion gyroRotation;
        
        // Camera reference
        private Camera arCamera;
        private Transform cameraTransform;
        
        // Calculated values
        private float bearingToTarget = 0f;
        private float gpsDistance = 0f;
        
        private void Start()
        {
            Debug.Log("[GyroscopeCoinPositioner] Starting...");
            
            // Enable gyroscope
            EnableGyroscope();
            
            // Enable compass - IMPORTANT: Start location service first on Android
            StartCoroutine(EnableCompassCoroutine());
            
            // Find camera
            arCamera = Camera.main;
            if (arCamera != null)
            {
                cameraTransform = arCamera.transform;
            }
            
            // Subscribe to target changes
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet += OnTargetSet;
                CoinManager.Instance.OnTargetCleared += OnTargetCleared;
                
                // Check if we already have a target
                if (CoinManager.Instance.HasTarget && CoinManager.Instance.TargetCoinData != null)
                {
                    var coin = CoinManager.Instance.TargetCoinData;
                    SetTarget(coin.latitude, coin.longitude);
                }
            }
            
            Debug.Log($"[GyroscopeCoinPositioner] Started. Gyro={gyroEnabled}");
        }
        
        private System.Collections.IEnumerator EnableCompassCoroutine()
        {
            // Start location service first - required on some Android devices
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[GyroscopeCoinPositioner] Location not enabled by user!");
            }
            
            if (Input.location.status == LocationServiceStatus.Stopped)
            {
                Input.location.Start(1f, 0.1f);
                Debug.Log("[GyroscopeCoinPositioner] Starting location service for compass...");
                
                // Wait for location to initialize
                int maxWait = 15;
                while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
                {
                    yield return new WaitForSeconds(1);
                    maxWait--;
                }
            }
            
            // Now enable compass
            Input.compass.enabled = true;
            
            // Wait a moment for compass to warm up
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log($"[GyroscopeCoinPositioner] Compass status: enabled={Input.compass.enabled}, " +
                      $"trueHeading={Input.compass.trueHeading:F1}, " +
                      $"magneticHeading={Input.compass.magneticHeading:F1}, " +
                      $"timestamp={Input.compass.timestamp}");
        }
        
        private void EnableGyroscope()
        {
            if (SystemInfo.supportsGyroscope)
            {
                gyro = Input.gyro;
                gyro.enabled = true;
                gyroEnabled = true;
                Debug.Log("[GyroscopeCoinPositioner] Gyroscope enabled");
            }
            else
            {
                Debug.LogWarning("[GyroscopeCoinPositioner] Gyroscope NOT supported!");
            }
        }
        
        private void OnDestroy()
        {
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet -= OnTargetSet;
                CoinManager.Instance.OnTargetCleared -= OnTargetCleared;
            }
        }
        
        private void OnTargetSet(Coin coin)
        {
            SetTarget(coin.latitude, coin.longitude);
        }
        
        private void OnTargetCleared()
        {
            hasTarget = false;
            Debug.Log("[GyroscopeCoinPositioner] Target cleared");
        }
        
        public void SetTarget(double latitude, double longitude)
        {
            targetLatitude = latitude;
            targetLongitude = longitude;
            hasTarget = true;
            Debug.Log($"[GyroscopeCoinPositioner] Target set: {latitude}, {longitude}");
        }
        
        private void Update()
        {
            if (!hasTarget) return;
            if (cameraTransform == null) return;
            
            // Get player location
            LocationData playerLoc = null;
            if (GPSManager.Exists)
            {
                playerLoc = GPSManager.Instance.GetCurrentLocation();
            }
            if (playerLoc == null) return;
            
            // Calculate bearing and distance to target
            bearingToTarget = (float)GeoUtils.CalculateBearing(
                playerLoc.latitude, playerLoc.longitude,
                targetLatitude, targetLongitude
            );
            
            gpsDistance = (float)GeoUtils.CalculateDistance(
                playerLoc.latitude, playerLoc.longitude,
                targetLatitude, targetLongitude
            );
            
            // Get device heading (which direction phone is pointing)
            float deviceHeading = GetDeviceHeading();
            
            // Calculate relative angle: how much to rotate from current view to see target
            // If relativeBearing = 0, target is straight ahead
            // If relativeBearing = 90, target is to the right
            float relativeBearing = bearingToTarget - deviceHeading;
            
            // Normalize to -180 to 180
            while (relativeBearing > 180) relativeBearing -= 360;
            while (relativeBearing < -180) relativeBearing += 360;
            
            // Position the coin based on relative bearing
            PositionCoin(relativeBearing);
            
            // Debug logging
            if (debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[GyroscopeCoinPositioner] Bearing={bearingToTarget:F0}°, Heading={deviceHeading:F0}°, Relative={relativeBearing:F0}°, GPS={gpsDistance:F1}m");
            }
        }
        
        /// <summary>
        /// Get device heading using compass or gyroscope
        /// </summary>
        private float GetDeviceHeading()
        {
            // Method 1: Try compass first (most accurate for absolute heading)
            if (Input.compass.enabled)
            {
                float heading = Input.compass.trueHeading;
                if (heading == 0 || float.IsNaN(heading))
                {
                    heading = Input.compass.magneticHeading;
                }
                
                // If compass is giving real data, use it
                if (heading != 0 && !float.IsNaN(heading))
                {
                    return heading;
                }
            }
            
            // Method 2: Gyroscope with proper coordinate conversion for Android
            if (gyroEnabled && gyro != null)
            {
                // Unity's gyro uses a right-handed coordinate system
                // Need to convert to get heading (Y-axis rotation)
                Quaternion gyroAttitude = gyro.attitude;
                
                // Apply rotation fix for Android (90 degree X rotation)
                // This converts from gyro space to Unity world space
                Quaternion rotFix = Quaternion.Euler(90f, 0f, 0f);
                Quaternion camRot = rotFix * new Quaternion(gyroAttitude.x, gyroAttitude.y, -gyroAttitude.z, -gyroAttitude.w);
                
                // Extract heading (yaw)
                float heading = camRot.eulerAngles.y;
                
                // Debug periodically
                if (debugMode && Time.frameCount % 300 == 0)
                {
                    Debug.Log($"[GyroscopeCoinPositioner] GYRO: attitude={gyroAttitude.eulerAngles}, converted heading={heading:F1}°");
                }
                
                return heading;
            }
            
            // Method 3: Camera Y rotation (last resort, works if AR is tracking)
            if (arCamera != null)
            {
                return arCamera.transform.eulerAngles.y;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Position the coin based on relative bearing.
        /// Coin appears in front of camera when pointing at target.
        /// </summary>
        private void PositionCoin(float relativeBearing)
        {
            // Convert bearing to radians
            float radians = relativeBearing * Mathf.Deg2Rad;
            
            // Calculate position relative to camera
            // X = sin(bearing) - positive is to the right
            // Z = cos(bearing) - positive is forward
            float x = Mathf.Sin(radians) * displayDistance;
            float z = Mathf.Cos(radians) * displayDistance;
            
            // Target position in world space (relative to camera)
            Vector3 targetPos = cameraTransform.position + new Vector3(x, displayHeight, z);
            
            // Smooth movement
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
            
            // Face camera
            Vector3 lookDir = cameraTransform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(-lookDir, Vector3.up);
            }
        }
        
        /// <summary>
        /// Get the GPS distance to target
        /// </summary>
        public float GPSDistance => gpsDistance;
        
        /// <summary>
        /// Get the bearing to target
        /// </summary>
        public float BearingToTarget => bearingToTarget;
        
        /// <summary>
        /// Is the target in front of the camera? (within ~60 degrees)
        /// </summary>
        public bool IsTargetInView
        {
            get
            {
                if (!hasTarget) return false;
                
                float deviceHeading = GetDeviceHeading();
                float relative = bearingToTarget - deviceHeading;
                while (relative > 180) relative -= 360;
                while (relative < -180) relative += 360;
                
                return Mathf.Abs(relative) < 60f;
            }
        }
    }
}
