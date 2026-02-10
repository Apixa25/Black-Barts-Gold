// ============================================================================
// CompassArrow.cs
// Black Bart's Gold - Simple Compass Direction Arrow
// Path: Assets/Scripts/UI/CompassArrow.cs
// ============================================================================
// SIMPLE APPROACH: Arrow that points toward target based on compass.
// No complex state management, no dependencies on other systems.
// Just works.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlackBartsGold.AR;
using BlackBartsGold.Location;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Simple compass-based direction arrow.
    /// </summary>
    public class CompassArrow : MonoBehaviour
    {
        [Header("UI References - Set in Inspector")]
        [SerializeField] private RectTransform arrowRect;
        [SerializeField] private TMP_Text distanceLabel;
        
        [Header("Settings")]
        [SerializeField] private float smoothSpeed = 5f;
        
        // State
        private float currentAngle = 0f;
        private bool hasTarget = false;
        private double targetLat;
        private double targetLon;
        private float lastLogTime;
        
        private void Awake()
        {
            Debug.Log("[CompassArrow] AWAKE - Arrow direction indicator starting");
        }
        
        private void Start()
        {
            Debug.Log("[CompassArrow] START");
            
            // Initialize DeviceCompass (New Input System replacement for legacy Input.compass)
            DeviceCompass.Initialize();
            
            // Auto-find arrow if not set
            if (arrowRect == null)
            {
                arrowRect = GetComponent<RectTransform>();
            }
            
            // Subscribe to events
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
                    Debug.Log($"[CompassArrow] Already has target");
                }
            }
            else
            {
                Debug.LogWarning("[CompassArrow] CoinManager not found!");
            }
            
            Debug.Log($"[CompassArrow] Started. ArrowRect={arrowRect != null}, DistanceLabel={distanceLabel != null}");
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
            gameObject.SetActive(true);
            Debug.Log($"[CompassArrow] Target set!");
        }
        
        private void OnTargetCleared()
        {
            hasTarget = false;
            Debug.Log("[CompassArrow] Target cleared");
        }
        
        private void Update()
        {
            if (!hasTarget)
            {
                return;
            }
            
            // Get player location
            LocationData playerLoc = null;
            if (GPSManager.Exists)
            {
                playerLoc = GPSManager.Instance.GetCurrentLocation();
            }
            
            if (playerLoc == null)
            {
                return;
            }
            
            // Calculate bearing to target
            float bearingToTarget = (float)GeoUtils.CalculateBearing(
                playerLoc.latitude, playerLoc.longitude,
                targetLat, targetLon
            );
            
            // Get compass heading using DeviceCompass (New Input System) — legacy broken on Android 16+
            float compassHeading = DeviceCompass.Heading;
            
            // Calculate relative bearing (how much to turn)
            float relativeBearing = bearingToTarget - compassHeading;
            
            // Normalize
            while (relativeBearing > 180) relativeBearing -= 360;
            while (relativeBearing < -180) relativeBearing += 360;
            
            // Arrow rotation: 
            // In Unity UI, Z rotation: positive = counter-clockwise
            // relativeBearing: positive = target is to the right
            // So we negate to make arrow point correctly
            float targetAngle = -relativeBearing;
            
            // Smooth rotation
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);
            
            // Apply rotation
            if (arrowRect != null)
            {
                arrowRect.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }
            
            // Update distance
            float distance = (float)GeoUtils.CalculateDistance(
                playerLoc.latitude, playerLoc.longitude,
                targetLat, targetLon
            );
            
            if (distanceLabel != null)
            {
                distanceLabel.text = $"{distance:F0}m";
            }
            
            // Log periodically
            if (Time.time - lastLogTime > 3f)
            {
                lastLogTime = Time.time;
                Debug.Log($"[CompassArrow] UPDATE: Bearing={bearingToTarget:F0}°, Compass={compassHeading:F0}°, Arrow={currentAngle:F0}°, Dist={distance:F0}m");
            }
        }
        
        // Called from inspector or code to force enable
        public void ForceEnable()
        {
            hasTarget = true;
            gameObject.SetActive(true);
            Debug.Log("[CompassArrow] Force enabled");
        }
    }
}
