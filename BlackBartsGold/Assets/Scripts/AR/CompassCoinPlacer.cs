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
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
            }
            
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
            
            // Get compass heading (where phone is pointing)
            float compassHeading = Input.compass.trueHeading;
            if (compassHeading == 0)
            {
                compassHeading = Input.compass.magneticHeading;
            }
            
            // Calculate relative bearing
            // 0 = target straight ahead, 90 = target to right, -90 = target to left
            float relativeBearing = bearingToTarget - compassHeading;
            
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
                Debug.Log($"[CompassCoinPlacer] Bearing={bearingToTarget:F0}°, Compass={compassHeading:F0}°, Relative={relativeBearing:F0}°, GPS={gpsDistance:F1}m");
            }
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
