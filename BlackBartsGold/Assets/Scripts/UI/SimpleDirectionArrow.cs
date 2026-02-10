// ============================================================================
// SimpleDirectionArrow.cs
// Black Bart's Gold - Simple Compass-Based Direction Arrow
// Path: Assets/Scripts/UI/SimpleDirectionArrow.cs
// ============================================================================
// A SIMPLE, PROVEN direction indicator that points toward target coin.
// Based on successful off-screen indicator implementations.
// 
// Key insight: Don't try to calculate 3D AR positions - just use compass!
// The arrow shows "turn this way to face the coin" relative to where
// your phone is pointing.
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
    /// Always visible when hunting a coin, always points the right way.
    /// </summary>
    public class SimpleDirectionArrow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform arrowImage;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text statusText;
        
        [Header("Settings")]
        [SerializeField] private float rotationSmoothSpeed = 5f;
        [SerializeField] private Color farColor = new Color(1f, 0.84f, 0f); // Gold
        [SerializeField] private Color nearColor = new Color(0.29f, 0.87f, 0.5f); // Green
        
        // State
        private float currentRotation = 0f;
        private float targetRotation = 0f;
        private Image arrowImageComponent;
        private bool hasTarget = false;
        
        private void Awake()
        {
            // ALWAYS log on Awake to verify component is active
            Debug.Log("[SimpleDirectionArrow] AWAKE - Component is running!");
            
            if (arrowImage != null)
            {
                arrowImageComponent = arrowImage.GetComponent<Image>();
            }
        }
        
        private void Start()
        {
            Debug.Log("[SimpleDirectionArrow] START - Initializing DeviceCompass");
            DeviceCompass.Initialize();
            
            // Subscribe to events
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet += OnTargetSet;
                CoinManager.Instance.OnTargetCleared += OnTargetCleared;
                Debug.Log("[SimpleDirectionArrow] Subscribed to CoinManager events");
            }
            else
            {
                Debug.LogWarning("[SimpleDirectionArrow] CoinManager not found!");
            }
            
            // Check if we already have a target
            if (CoinManager.Exists && CoinManager.Instance.HasTarget)
            {
                hasTarget = true;
                Debug.Log("[SimpleDirectionArrow] Already has target - showing arrow");
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
        
        private void Update()
        {
            if (!hasTarget) return;
            
            UpdateDirection();
            UpdateUI();
        }
        
        private void OnTargetSet(Coin coin)
        {
            Debug.Log($"[SimpleDirectionArrow] Target set: {coin.GetDisplayValue()}");
            hasTarget = true;
            gameObject.SetActive(true);
        }
        
        private void OnTargetCleared()
        {
            Debug.Log("[SimpleDirectionArrow] Target cleared");
            hasTarget = false;
        }
        
        /// <summary>
        /// Calculate which direction to point the arrow.
        /// Uses GPS bearing and compass heading.
        /// </summary>
        private void UpdateDirection()
        {
            // Get player location
            LocationData playerLoc = null;
            if (GPSManager.Exists)
            {
                playerLoc = GPSManager.Instance.GetCurrentLocation();
            }
            
            if (playerLoc == null)
            {
                Debug.LogWarning("[SimpleDirectionArrow] No player location");
                return;
            }
            
            // Get target coin
            Coin targetCoin = null;
            if (CoinManager.Exists && CoinManager.Instance.HasTarget)
            {
                targetCoin = CoinManager.Instance.TargetCoinData;
            }
            
            if (targetCoin == null)
            {
                return;
            }
            
            // Calculate GPS bearing to target (0° = North, 90° = East)
            float gpsBearing = (float)GeoUtils.CalculateBearing(
                playerLoc.latitude, playerLoc.longitude,
                targetCoin.latitude, targetCoin.longitude
            );
            
            // Get device compass heading (where phone is pointing)
            // Uses DeviceCompass (New Input System) — legacy Input.compass is broken on Android 16+
            float deviceHeading = DeviceCompass.Heading;
            
            // Calculate relative bearing (how much to turn)
            // If relative bearing is 0, target is straight ahead
            // If relative bearing is 90, target is to the right
            // If relative bearing is -90, target is to the left
            float relativeBearing = gpsBearing - deviceHeading;
            
            // Normalize to -180 to 180
            while (relativeBearing > 180) relativeBearing -= 360;
            while (relativeBearing < -180) relativeBearing += 360;
            
            // Arrow rotation (Unity UI: 0° = up, positive = clockwise)
            // So relativeBearing maps directly: positive = turn right = arrow points right
            targetRotation = -relativeBearing; // Negate because UI rotation is opposite
            
            // Smooth rotation
            currentRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
            
            // Apply rotation to arrow
            if (arrowImage != null)
            {
                arrowImage.localRotation = Quaternion.Euler(0, 0, currentRotation);
            }
        }
        
        /// <summary>
        /// Update UI text and colors
        /// </summary>
        private void UpdateUI()
        {
            if (!CoinManager.Exists || !CoinManager.Instance.HasTarget) return;
            
            // Get distance from CoinManager's target
            float distance = CoinManager.Instance.TargetCoin?.DistanceFromPlayer ?? 0f;
            
            // Update distance text
            if (distanceText != null)
            {
                if (distance < 1000)
                {
                    distanceText.text = $"{distance:F0}m";
                }
                else
                {
                    distanceText.text = $"{distance/1000f:F1}km";
                }
            }
            
            // Update status text
            if (statusText != null)
            {
                if (distance <= 5f)
                {
                    statusText.text = "TAP TO COLLECT!";
                }
                else if (distance <= 20f)
                {
                    statusText.text = "Almost there!";
                }
                else if (distance <= 50f)
                {
                    statusText.text = "Getting closer...";
                }
                else
                {
                    statusText.text = "Walk toward treasure!";
                }
            }
            
            // Update color based on distance
            if (arrowImageComponent != null)
            {
                float t = Mathf.InverseLerp(50f, 5f, distance);
                arrowImageComponent.color = Color.Lerp(farColor, nearColor, t);
            }
        }
        
        #region Debug
        
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== SimpleDirectionArrow State ===");
            Debug.Log($"Has Target: {hasTarget}");
            Debug.Log($"Current Rotation: {currentRotation:F0}°");
            Debug.Log($"Target Rotation: {targetRotation:F0}°");
            Debug.Log($"Compass Heading: {DeviceCompass.Heading:F0}° (method: {DeviceCompass.ActiveMethod})");
            Debug.Log($"Arrow Image: {arrowImage != null}");
            Debug.Log($"CoinManager Exists: {CoinManager.Exists}");
            if (CoinManager.Exists)
            {
                Debug.Log($"Has Target: {CoinManager.Instance.HasTarget}");
            }
            Debug.Log("==================================");
        }
        
        [ContextMenu("Debug: Force Show")]
        public void DebugForceShow()
        {
            hasTarget = true;
            gameObject.SetActive(true);
            Debug.Log("[SimpleDirectionArrow] Force shown");
        }
        
        #endregion
    }
}
