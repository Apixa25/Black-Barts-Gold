// ============================================================================
// CompassUI.cs
// Black Bart's Gold - Compass Direction UI Component
// Path: Assets/Scripts/UI/CompassUI.cs
// ============================================================================
// Displays compass direction to nearest coin with arrow, distance, and
// cardinal direction. Updates based on device heading and coin position.
// Reference: BUILD-GUIDE.md Prompt 5.1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// UI component showing compass direction to nearest coin.
    /// Displays arrow, distance, and cardinal direction.
    /// </summary>
    public class CompassUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Arrow image that rotates to point at coin")]
        private RectTransform arrowImage;
        
        [SerializeField]
        [Tooltip("Distance text (e.g., '25m')")]
        private TMP_Text distanceText;
        
        [SerializeField]
        [Tooltip("Cardinal direction text (e.g., 'NE')")]
        private TMP_Text directionText;
        
        [SerializeField]
        [Tooltip("Coin value text (e.g., '$5.00')")]
        private TMP_Text valueText;
        
        [SerializeField]
        [Tooltip("Container to show/hide")]
        private GameObject compassContainer;
        
        [SerializeField]
        [Tooltip("Background image for color changes")]
        private Image backgroundImage;
        
        [Header("Colors")]
        [SerializeField]
        private Color normalColor = new Color(0.1f, 0.21f, 0.36f, 0.8f); // Deep sea blue
        
        [SerializeField]
        private Color nearColor = new Color(0.29f, 0.87f, 0.5f, 0.8f); // Green
        
        [SerializeField]
        private Color collectibleColor = new Color(1f, 0.84f, 0f, 0.8f); // Gold
        
        [Header("Animation")]
        [SerializeField]
        private float rotationSmoothSpeed = 10f;
        
        [SerializeField]
        private float pulseSpeed = 2f;
        
        [SerializeField]
        private float pulseMinScale = 0.9f;
        
        [SerializeField]
        private float pulseMaxScale = 1.1f;
        
        [Header("Settings")]
        [SerializeField]
        private bool useDeviceCompass = true;
        
        [SerializeField]
        private bool showWhenNoCoin = false;
        
        [SerializeField]
        private float hideDistance = 100f; // Hide compass beyond this distance
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is compass currently visible?
        /// </summary>
        public bool IsVisible { get; private set; } = false;
        
        /// <summary>
        /// Current target bearing
        /// </summary>
        public float TargetBearing { get; private set; } = 0f;
        
        /// <summary>
        /// Current device heading
        /// </summary>
        public float DeviceHeading { get; private set; } = 0f;
        
        /// <summary>
        /// Relative bearing (target - device heading)
        /// </summary>
        public float RelativeBearing { get; private set; } = 0f;
        
        #endregion
        
        #region Private Fields
        
        private float currentArrowRotation = 0f;
        private float targetArrowRotation = 0f;
        private ProximityZone currentZone = ProximityZone.OutOfRange;
        private bool isPulsing = false;
        private Vector3 baseScale;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (arrowImage != null)
            {
                baseScale = arrowImage.localScale;
            }
        }
        
        private void Start()
        {
            // Initialize DeviceCompass (New Input System replacement for legacy Input.compass)
            if (useDeviceCompass)
            {
                DeviceCompass.Initialize();
            }
            
            // Subscribe to proximity updates
            if (ProximityManager.Instance != null)
            {
                ProximityManager.Instance.OnDistanceUpdated += OnDistanceUpdated;
                ProximityManager.Instance.OnZoneChanged += OnZoneChanged;
                ProximityManager.Instance.OnNearestCoinChanged += OnNearestCoinChanged;
            }
            
            // Initial state
            if (!showWhenNoCoin)
            {
                Hide();
            }
        }
        
        private void OnDestroy()
        {
            if (ProximityManager.Instance != null)
            {
                ProximityManager.Instance.OnDistanceUpdated -= OnDistanceUpdated;
                ProximityManager.Instance.OnZoneChanged -= OnZoneChanged;
                ProximityManager.Instance.OnNearestCoinChanged -= OnNearestCoinChanged;
            }
        }
        
        private void Update()
        {
            if (!IsVisible) return;
            
            // Update device heading
            UpdateDeviceHeading();
            
            // Smooth arrow rotation
            UpdateArrowRotation();
            
            // Pulse animation when collectible
            if (isPulsing)
            {
                UpdatePulseAnimation();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle distance update from ProximityManager
        /// </summary>
        private void OnDistanceUpdated(float distance, float bearing)
        {
            TargetBearing = bearing;
            
            // Update target rotation
            UpdateTargetRotation();
            
            // Update distance text
            if (distanceText != null)
            {
                distanceText.text = GeoUtils.FormatDistance(distance);
            }
            
            // Update direction text
            if (directionText != null)
            {
                directionText.text = GeoUtils.GetCardinalDirection(bearing);
            }
            
            // Show/hide based on distance
            if (distance > hideDistance)
            {
                Hide();
            }
            else if (!IsVisible)
            {
                Show();
            }
        }
        
        /// <summary>
        /// Handle zone change
        /// </summary>
        private void OnZoneChanged(ProximityZone oldZone, ProximityZone newZone)
        {
            currentZone = newZone;
            UpdateAppearance();
        }
        
        /// <summary>
        /// Handle nearest coin change
        /// </summary>
        private void OnNearestCoinChanged(Coin coin)
        {
            if (coin == null)
            {
                if (!showWhenNoCoin)
                {
                    Hide();
                }
                if (valueText != null)
                {
                    valueText.text = "";
                }
            }
            else
            {
                Show();
                if (valueText != null)
                {
                    valueText.text = coin.GetDisplayValue();
                }
            }
        }
        
        #endregion
        
        #region Rotation Updates
        
        /// <summary>
        /// Update device compass heading
        /// </summary>
        private void UpdateDeviceHeading()
        {
            // Uses DeviceCompass (New Input System) — legacy Input.compass broken on Android 16+
            if (useDeviceCompass && DeviceCompass.IsAvailable)
            {
                DeviceHeading = DeviceCompass.Heading;
            }
            
            UpdateTargetRotation();
        }
        
        /// <summary>
        /// Calculate target arrow rotation
        /// </summary>
        private void UpdateTargetRotation()
        {
            // Calculate relative bearing (how much to rotate arrow)
            RelativeBearing = GeoUtils.CalculateRelativeBearing(TargetBearing, DeviceHeading);
            
            // Arrow rotation (negative because UI rotates clockwise)
            targetArrowRotation = -RelativeBearing;
        }
        
        /// <summary>
        /// Smoothly rotate arrow to target
        /// </summary>
        private void UpdateArrowRotation()
        {
            if (arrowImage == null) return;
            
            // Smooth rotation
            currentArrowRotation = Mathf.LerpAngle(
                currentArrowRotation, 
                targetArrowRotation, 
                Time.deltaTime * rotationSmoothSpeed
            );
            
            arrowImage.localRotation = Quaternion.Euler(0, 0, currentArrowRotation);
        }
        
        #endregion
        
        #region Appearance
        
        /// <summary>
        /// Update appearance based on proximity zone
        /// </summary>
        private void UpdateAppearance()
        {
            Color targetColor;
            
            switch (currentZone)
            {
                case ProximityZone.Collectible:
                    targetColor = collectibleColor;
                    isPulsing = true;
                    break;
                    
                case ProximityZone.Near:
                    targetColor = nearColor;
                    isPulsing = false;
                    break;
                    
                default:
                    targetColor = normalColor;
                    isPulsing = false;
                    break;
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = targetColor;
            }
            
            // Reset scale if not pulsing
            if (!isPulsing && arrowImage != null)
            {
                arrowImage.localScale = baseScale;
            }
        }
        
        /// <summary>
        /// Update pulse animation
        /// </summary>
        private void UpdatePulseAnimation()
        {
            if (arrowImage == null) return;
            
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, pulse);
            
            arrowImage.localScale = baseScale * scale;
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the compass
        /// </summary>
        public void Show()
        {
            if (compassContainer != null)
            {
                compassContainer.SetActive(true);
            }
            IsVisible = true;
            
            Log("Compass shown");
        }
        
        /// <summary>
        /// Hide the compass
        /// </summary>
        public void Hide()
        {
            if (compassContainer != null)
            {
                compassContainer.SetActive(false);
            }
            IsVisible = false;
            isPulsing = false;
            
            Log("Compass hidden");
        }
        
        /// <summary>
        /// Toggle visibility
        /// </summary>
        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
        
        #endregion
        
        #region Manual Updates
        
        /// <summary>
        /// Manually set target bearing (for testing)
        /// </summary>
        public void SetTargetBearing(float bearing)
        {
            TargetBearing = bearing;
            UpdateTargetRotation();
        }
        
        /// <summary>
        /// Manually set distance display
        /// </summary>
        public void SetDistance(float meters)
        {
            if (distanceText != null)
            {
                distanceText.text = GeoUtils.FormatDistance(meters);
            }
            
            if (directionText != null)
            {
                directionText.text = GeoUtils.GetCardinalDirection(TargetBearing);
            }
        }
        
        /// <summary>
        /// Manually set coin value display
        /// </summary>
        public void SetCoinValue(string value)
        {
            if (valueText != null)
            {
                valueText.text = value;
            }
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[CompassUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print compass state
        /// </summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== Compass State ===");
            Debug.Log($"Visible: {IsVisible}");
            Debug.Log($"Target Bearing: {TargetBearing:F0}°");
            Debug.Log($"Device Heading: {DeviceHeading:F0}°");
            Debug.Log($"Relative Bearing: {RelativeBearing:F0}°");
            Debug.Log($"Arrow Rotation: {currentArrowRotation:F0}°");
            Debug.Log($"Zone: {currentZone}");
            Debug.Log($"Pulsing: {isPulsing}");
            Debug.Log("=====================");
        }
        
        /// <summary>
        /// Debug: Simulate coin at bearing
        /// </summary>
        [ContextMenu("Debug: Simulate North")]
        public void DebugSimulateNorth()
        {
            Show();
            SetTargetBearing(0);
            SetDistance(25);
            SetCoinValue("$5.00");
        }
        
        /// <summary>
        /// Debug: Simulate coin to east
        /// </summary>
        [ContextMenu("Debug: Simulate East")]
        public void DebugSimulateEast()
        {
            Show();
            SetTargetBearing(90);
            SetDistance(10);
            SetCoinValue("$2.50");
        }
        
        /// <summary>
        /// Debug: Rotate through bearings
        /// </summary>
        [ContextMenu("Debug: Rotate Test")]
        public void DebugRotateTest()
        {
            StartCoroutine(RotateTestCoroutine());
        }
        
        private System.Collections.IEnumerator RotateTestCoroutine()
        {
            Show();
            SetCoinValue("$10.00");
            
            for (float bearing = 0; bearing < 360; bearing += 10)
            {
                SetTargetBearing(bearing);
                SetDistance(20);
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        #endregion
    }
}
