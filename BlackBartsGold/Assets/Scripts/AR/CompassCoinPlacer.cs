// ============================================================================
// CompassCoinPlacer.cs
// Black Bart's Gold - Simple Compass-Based AR Coin Placement
// Path: Assets/Scripts/AR/CompassCoinPlacer.cs
// ============================================================================
// SIMPLE APPROACH: Place coin based on compass direction.
// When you point your phone at the coin's GPS bearing, it appears in front.
// When you turn away, it moves off screen.
// 
// This does NOT rely on AR tracking at all - just compass + gyroscope.
// ============================================================================

using UnityEngine;
using BlackBartsGold.Location;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Simple compass-based coin placer. Places coin based on where phone is pointing.
    /// </summary>
    public class CompassCoinPlacer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float displayDistance = 4f;
        [SerializeField] private float displayHeight = 0f; // Relative to camera
        [SerializeField] private float smoothSpeed = 8f;
        
        [Header("Debug")]
        [SerializeField] private bool logEnabled = true;
        
        // References
        private Camera arCamera;
        private Transform coinTransform;
        private CoinController coinController;
        
        // State
        private bool hasTarget = false;
        private double targetLat;
        private double targetLon;
        private float lastLogTime;
        
        private void Awake()
        {
            Debug.Log("[CompassCoinPlacer] AWAKE");
        }
        
        private void Start()
        {
            Debug.Log("[CompassCoinPlacer] START - Enabling compass and gyroscope");
            
            // Enable sensors
            Input.compass.enabled = true;
            Input.location.Start(); // Location might be needed for compass on some devices
            
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                Debug.Log($"[CompassCoinPlacer] Gyroscope enabled: {Input.gyro.enabled}");
            }
            else
            {
                Debug.LogWarning("[CompassCoinPlacer] Gyroscope NOT supported on this device!");
            }
            
            Debug.Log($"[CompassCoinPlacer] Compass enabled: {Input.compass.enabled}, Gyro supported: {SystemInfo.supportsGyroscope}");
            
            // Find camera
            arCamera = Camera.main;
            if (arCamera == null)
            {
                arCamera = FindFirstObjectByType<Camera>();
            }
            
            // Get our coin controller
            coinController = GetComponent<CoinController>();
            coinTransform = transform;
            
            // Subscribe to target events
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet += OnTargetSet;
                CoinManager.Instance.OnTargetCleared += OnTargetCleared;
                
                // Check if already have target
                if (CoinManager.Instance.HasTarget && CoinManager.Instance.TargetCoinData != null)
                {
                    var coin = CoinManager.Instance.TargetCoinData;
                    targetLat = coin.latitude;
                    targetLon = coin.longitude;
                    hasTarget = true;
                    Debug.Log($"[CompassCoinPlacer] Already has target at {targetLat}, {targetLon}");
                }
            }
            
            Debug.Log($"[CompassCoinPlacer] Started. Camera={arCamera != null}, Compass={Input.compass.enabled}");
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
            targetLat = coin.latitude;
            targetLon = coin.longitude;
            hasTarget = true;
            Debug.Log($"[CompassCoinPlacer] Target set: {targetLat}, {targetLon}");
        }
        
        private void OnTargetCleared()
        {
            hasTarget = false;
            Debug.Log("[CompassCoinPlacer] Target cleared");
        }
        
        private void Update()
        {
            if (!hasTarget || arCamera == null) return;
            
            // Get player location
            LocationData playerLoc = null;
            if (GPSManager.Exists)
            {
                playerLoc = GPSManager.Instance.GetCurrentLocation();
            }
            if (playerLoc == null) return;
            
            // Calculate bearing to target (0° = North, 90° = East)
            float bearingToTarget = (float)GeoUtils.CalculateBearing(
                playerLoc.latitude, playerLoc.longitude,
                targetLat, targetLon
            );
            
            // Get device heading - TRY MULTIPLE METHODS
            float deviceHeading = GetDeviceHeading();
            
            // Calculate relative bearing
            // 0 = target straight ahead, 90 = target to right, -90 = target to left
            float relativeBearing = bearingToTarget - deviceHeading;
            
            // Normalize to -180 to 180
            while (relativeBearing > 180) relativeBearing -= 360;
            while (relativeBearing < -180) relativeBearing += 360;
            
            // Position coin based on relative bearing
            PositionCoin(relativeBearing);
            
            // Log periodically
            if (logEnabled && Time.time - lastLogTime > 3f)
            {
                lastLogTime = Time.time;
                float gpsDistance = (float)GeoUtils.CalculateDistance(
                    playerLoc.latitude, playerLoc.longitude, targetLat, targetLon);
                Debug.Log($"[CompassCoinPlacer] Bearing={bearingToTarget:F0}°, DeviceHeading={deviceHeading:F0}°, Relative={relativeBearing:F0}°, GPS={gpsDistance:F1}m");
            }
        }
        
        /// <summary>
        /// Get device heading using multiple fallback methods.
        /// </summary>
        private float GetDeviceHeading()
        {
            // Method 1: Try compass
            float compassHeading = Input.compass.trueHeading;
            if (compassHeading == 0)
            {
                compassHeading = Input.compass.magneticHeading;
            }
            
            // If compass is working (not stuck at 0), use it
            if (compassHeading != 0)
            {
                return compassHeading;
            }
            
            // Method 2: Use gyroscope attitude
            if (SystemInfo.supportsGyroscope && Input.gyro.enabled)
            {
                // Get gyro rotation and convert to heading
                Quaternion gyroAttitude = Input.gyro.attitude;
                
                // Convert gyro attitude to euler angles
                // Gyro uses different coordinate system, need to convert
                Quaternion rotFix = Quaternion.Euler(90f, 0f, 0f);
                Quaternion camRotation = rotFix * gyroAttitude;
                
                // Extract Y rotation (yaw) as heading
                float gyroHeading = camRotation.eulerAngles.y;
                
                return gyroHeading;
            }
            
            // Method 3: Use camera's Y rotation (last resort)
            if (arCamera != null)
            {
                return arCamera.transform.eulerAngles.y;
            }
            
            return 0f;
        }
        
        private void PositionCoin(float relativeBearing)
        {
            // Convert to radians
            float rad = relativeBearing * Mathf.Deg2Rad;
            
            // Calculate offset from camera center
            // X = how far right/left (sin)
            // Z = how far forward (cos)
            float x = Mathf.Sin(rad) * displayDistance;
            float z = Mathf.Cos(rad) * displayDistance;
            
            // Target position relative to camera
            Vector3 camPos = arCamera.transform.position;
            Vector3 targetPos = camPos + new Vector3(x, displayHeight, z);
            
            // Smooth movement
            coinTransform.position = Vector3.Lerp(
                coinTransform.position, 
                targetPos, 
                Time.deltaTime * smoothSpeed
            );
            
            // Face camera (billboard)
            Vector3 lookDir = camPos - coinTransform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                coinTransform.rotation = Quaternion.LookRotation(-lookDir, Vector3.up);
            }
        }
    }
}
