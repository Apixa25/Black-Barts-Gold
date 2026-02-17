// ============================================================================
// CoinDirectionIndicator.cs
// Black Bart's Gold - Large On-Screen Direction Arrow
// Path: Assets/Scripts/UI/CoinDirectionIndicator.cs
// ============================================================================
// Shows a PROMINENT direction arrow pointing toward the target coin.
// Based on Pokemon GO research: players need clear visual guidance to find
// objects that aren't visible in the AR camera view.
// 
// This is MORE prominent than CompassUI - it's center-screen when the coin
// is behind the camera, and moves to the edge when coin is off-screen.
// Reference: Docs/AR-COIN-DISPLAY-SPEC.md
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;
using BlackBartsGold.AR;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Large on-screen arrow showing direction to target coin.
    /// Positioned at screen edges when coin is off-screen.
    /// Shows distance and pulses when getting close.
    /// 
    /// Key insight from Pokemon GO research: AR objects at distance are
    /// effectively invisible. Players need explicit direction guidance.
    /// </summary>
    public class CoinDirectionIndicator : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField]
        [Tooltip("The arrow image that points toward the coin")]
        private RectTransform arrowTransform;
        
        [SerializeField]
        [Tooltip("Distance text (e.g., '47m')")]
        private TMP_Text distanceText;
        
        [SerializeField]
        [Tooltip("Value text (e.g., '$5.00')")]
        private TMP_Text valueText;
        
        [SerializeField]
        [Tooltip("Status text (e.g., 'Walk toward treasure!')")]
        private TMP_Text statusText;
        
        [SerializeField]
        [Tooltip("Container for the entire indicator")]
        private RectTransform indicatorContainer;
        
        [SerializeField]
        [Tooltip("Background panel")]
        private Image backgroundPanel;
        
        [SerializeField]
        [Tooltip("Arrow image for color changes")]
        private Image arrowImage;
        
        [Header("Edge Positioning")]
        [SerializeField]
        [Tooltip("Padding from screen edges in pixels")]
        private float edgePadding = 80f;
        
        [SerializeField]
        [Tooltip("Center zone - if coin is within this angle, show at edge")]
        private float onScreenAngleThreshold = 30f;
        
        [SerializeField]
        [Tooltip("Deadzone around center to prevent jitter when nearly aligned")]
        private float centerDeadzoneDegrees = 4f;
        
        [SerializeField]
        [Tooltip("Extra buffer to avoid on-screen/off-screen flicker")]
        private float onScreenHysteresisDegrees = 6f;
        
        [Header("Colors")]
        [SerializeField]
        private Color farColor = new Color(1f, 0.84f, 0f, 0.9f); // Gold
        
        [SerializeField]
        private Color nearColor = new Color(0.29f, 0.87f, 0.5f, 0.9f); // Green
        
        [SerializeField]
        private Color veryNearColor = new Color(0.29f, 0.87f, 0.5f, 1f); // Bright green
        
        [Header("Distance Thresholds")]
        [SerializeField]
        [Tooltip("Distance at which coin materializes in AR (indicator changes)")]
        private float materializationDistance = 100f;
        
        [SerializeField]
        [Tooltip("Distance for collection")]
        private float collectionDistance = 5f;
        
        [Header("Animation")]
        [SerializeField]
        private float pulseSpeed = 2f;
        
        [SerializeField]
        private float pulseMinScale = 0.9f;
        
        [SerializeField]
        private float pulseMaxScale = 1.15f;
        
        [SerializeField]
        private float rotationSmoothSpeed = 8f;
        
        [Header("Messages")]
        [SerializeField]
        private string farMessage = "Walk toward the treasure!";
        
        [SerializeField]
        private string nearMessage = "Getting close...";
        
        [SerializeField]
        private string veryNearMessage = "Almost there!";
        
        [SerializeField]
        private string inRangeMessage = "TAP COIN TO COLLECT!";
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Runtime Setup (Code-Only UI)
        
        /// <summary>
        /// Set direction indicator references at runtime. Called by ARHuntSceneSetup when building UI from code.
        /// </summary>
        public void SetRuntimeReferences(RectTransform arrow, TMP_Text distText, TMP_Text valText, TMP_Text statText, RectTransform cont, Image bgPanel, Image arrowImg)
        {
            arrowTransform = arrow;
            distanceText = distText;
            valueText = valText;
            statusText = statText;
            indicatorContainer = cont;
            backgroundPanel = bgPanel;
            arrowImage = arrowImg;
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is the indicator currently visible?
        /// </summary>
        public bool IsVisible { get; private set; } = false;
        
        /// <summary>
        /// Current distance to target coin
        /// </summary>
        public float CurrentDistance { get; private set; } = float.MaxValue;
        
        /// <summary>
        /// Current bearing to target coin
        /// </summary>
        public float CurrentBearing { get; private set; } = 0f;
        
        /// <summary>
        /// Is the coin currently on-screen (in camera FOV)?
        /// </summary>
        public bool IsCoinOnScreen { get; private set; } = false;
        
        #endregion
        
        #region Private Fields
        
        private Camera arCamera;
        private float currentArrowRotation = 0f;
        private float targetArrowRotation = 0f;
        private Vector3 baseScale;
        private bool isPulsing = false;
        private float deviceHeading = 0f;
        private float smoothedRelativeBearing = 0f;
        private float relativeBearingVelocity = 0f;
        
        // CanvasGroup for show/hide without deactivating the GameObject.
        private CanvasGroup canvasGroup;
        
        // Screen bounds for positioning
        private float screenWidth;
        private float screenHeight;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Cache base scale
            if (arrowTransform != null)
            {
                baseScale = arrowTransform.localScale;
            }
            else if (indicatorContainer != null)
            {
                baseScale = indicatorContainer.localScale;
            }
            else
            {
                baseScale = Vector3.one;
            }
            
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            
            // Get or add CanvasGroup — we use alpha to hide/show instead of SetActive.
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                Debug.Log("[CoinDirectionIndicator] Added CanvasGroup for visibility control");
            }
        }
        
        private void Start()
        {
            // Initialize DeviceCompass (New Input System replacement for legacy Input.compass)
            DeviceCompass.Initialize();
            
            // Find AR camera
            FindCamera();
            
            // Subscribe to CoinManager events
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet += OnTargetSet;
                CoinManager.Instance.OnTargetCleared += OnTargetCleared;
                CoinManager.Instance.OnHuntModeChanged += OnHuntModeChanged;
                Debug.Log($"[CoinDirectionIndicator] Subscribed to CoinManager events");
            }
            else
            {
                Debug.LogWarning($"[CoinDirectionIndicator] CoinManager NOT FOUND - cannot subscribe to events!");
            }
            
            // Subscribe to GPS updates
            if (GPSManager.Exists)
            {
                GPSManager.Instance.OnLocationUpdated += OnLocationUpdated;
            }
            
            // Initial state - hidden until we have a target
            Hide();
            
            // Log reference status
            Debug.Log($"[CoinDirectionIndicator] STARTED! ArrowTransform={arrowTransform != null}, ArrowImage={arrowImage != null}, DistanceText={distanceText != null}, ValueText={valueText != null}");
            
            // CRITICAL: Check if a target already exists (target was set on map screen
            // BEFORE the AR scene loaded, so OnTargetSet already fired and won't fire again)
            if (CoinManager.Exists && CoinManager.Instance.HasTarget)
            {
                Coin existingTarget = CoinManager.Instance.TargetCoinData;
                if (existingTarget != null)
                {
                    Debug.Log($"[CoinDirectionIndicator] Found existing target: {existingTarget.GetDisplayValue()} — showing indicator!");
                    OnTargetSet(existingTarget);
                }
            }
            else
            {
                Debug.Log("[CoinDirectionIndicator] No existing target — indicator stays hidden until target is set");
            }
        }
        
        private void OnDestroy()
        {
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet -= OnTargetSet;
                CoinManager.Instance.OnTargetCleared -= OnTargetCleared;
                CoinManager.Instance.OnHuntModeChanged -= OnHuntModeChanged;
            }
            
            if (GPSManager.Exists)
            {
                GPSManager.Instance.OnLocationUpdated -= OnLocationUpdated;
            }
        }
        
        private void Update()
        {
            if (!IsVisible) return;
            
            // Update device heading
            UpdateDeviceHeading();
            
            // Update direction to target
            UpdateDirectionToTarget();
            
            // Update arrow rotation (smooth)
            UpdateArrowRotation();
            
            // Update position on screen
            UpdateScreenPosition();
            
            // Update visuals based on distance
            UpdateVisuals();
            
            // Pulse animation when close
            if (isPulsing)
            {
                UpdatePulseAnimation();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle target coin set
        /// </summary>
        private void OnTargetSet(Coin coin)
        {
            Log($"Target set: {coin.GetDisplayValue()}");
            
            // Update value display
            if (valueText != null)
            {
                valueText.text = coin.GetDisplayValue();
            }
            
            // Show indicator
            Show();
        }
        
        /// <summary>
        /// Handle target cleared
        /// </summary>
        private void OnTargetCleared()
        {
            Log("Target cleared");
            Hide();
        }
        
        /// <summary>
        /// Handle hunt mode changed
        /// </summary>
        private void OnHuntModeChanged(HuntMode mode)
        {
            Log($"Hunt mode: {mode}");
            
            if ((mode == HuntMode.Hunting || mode == HuntMode.Collecting) && CoinManager.Instance.HasTarget)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        
        /// <summary>
        /// Handle GPS location update
        /// </summary>
        private void OnLocationUpdated(LocationData location)
        {
            // Trigger a direction update
            UpdateDirectionToTarget();
        }
        
        #endregion
        
        #region Direction Calculation
        
        /// <summary>
        /// Update device compass heading
        /// </summary>
        private void UpdateDeviceHeading()
        {
            // Uses DeviceCompass (New Input System) — legacy Input.compass broken on Android 16+
            if (DeviceCompass.IsAvailable)
            {
                deviceHeading = DeviceCompass.Heading;
            }
        }
        
        /// <summary>
        /// Calculate direction and distance to target coin
        /// </summary>
        private void UpdateDirectionToTarget()
        {
            // Get player location
            LocationData playerLocation = GetPlayerLocation();
            if (playerLocation == null) return;
            
            // Get target coin
            if (!CoinManager.Exists || !CoinManager.Instance.HasTarget) return;
            
            Coin targetCoin = CoinManager.Instance.TargetCoinData;
            if (targetCoin == null) return;
            
            // Calculate distance
            CurrentDistance = (float)GeoUtils.CalculateDistance(
                playerLocation.latitude, playerLocation.longitude,
                targetCoin.latitude, targetCoin.longitude
            );
            
            // Calculate GPS bearing (degrees from north)
            CurrentBearing = (float)GeoUtils.CalculateBearing(
                playerLocation.latitude, playerLocation.longitude,
                targetCoin.latitude, targetCoin.longitude
            );
            
            // Calculate relative bearing (direction to turn)
            // This is the angle between where we're facing and where the coin is.
            float relativeBearing = GeoUtils.CalculateRelativeBearing(CurrentBearing, deviceHeading);
            
            // When the coin is near and has a live AR object, use camera-to-coin
            // horizontal angle for a more intuitive final approach.
            if (CurrentDistance <= materializationDistance && CoinManager.Instance.TargetCoin != null && arCamera != null)
            {
                Vector3 toCoin = CoinManager.Instance.TargetCoin.transform.position - arCamera.transform.position;
                Vector3 toCoinFlat = Vector3.ProjectOnPlane(toCoin, Vector3.up);
                Vector3 camForwardFlat = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);
                if (toCoinFlat.sqrMagnitude > 0.0001f && camForwardFlat.sqrMagnitude > 0.0001f)
                {
                    relativeBearing = Vector3.SignedAngle(camForwardFlat, toCoinFlat, Vector3.up);
                }
            }
            
            // Prevent tiny heading jitter from constantly twitching the arrow.
            if (Mathf.Abs(relativeBearing) < centerDeadzoneDegrees)
            {
                relativeBearing = 0f;
            }
            
            smoothedRelativeBearing = Mathf.SmoothDampAngle(
                smoothedRelativeBearing,
                relativeBearing,
                ref relativeBearingVelocity,
                0.08f
            );
            
            // Arrow rotation (negative because UI rotates clockwise positive)
            targetArrowRotation = -smoothedRelativeBearing;
            
            // Update distance text
            if (distanceText != null)
            {
                distanceText.text = FormatDistance(CurrentDistance);
            }
            
            // Check if coin is "on screen" (within camera FOV)
            CheckIfCoinOnScreen(smoothedRelativeBearing);
        }
        
        /// <summary>
        /// Check if the coin direction is within the camera's field of view
        /// </summary>
        private void CheckIfCoinOnScreen(float relativeBearing)
        {
            float absBearing = Mathf.Abs(relativeBearing);
            
            float halfFOV = arCamera != null ? arCamera.fieldOfView * 0.5f : 30f;
            float threshold = Mathf.Min(halfFOV, onScreenAngleThreshold);
            
            if (IsCoinOnScreen)
            {
                IsCoinOnScreen = absBearing <= threshold + onScreenHysteresisDegrees;
            }
            else
            {
                IsCoinOnScreen = absBearing <= threshold - onScreenHysteresisDegrees;
            }
        }
        
        #endregion
        
        #region Arrow Updates
        
        /// <summary>
        /// Smoothly rotate arrow to target direction
        /// </summary>
        private void UpdateArrowRotation()
        {
            if (arrowTransform == null) return;
            
            // Smooth rotation
            currentArrowRotation = Mathf.LerpAngle(
                currentArrowRotation,
                targetArrowRotation,
                Time.deltaTime * rotationSmoothSpeed
            );
            
            arrowTransform.localRotation = Quaternion.Euler(0, 0, currentArrowRotation);
        }
        
        /// <summary>
        /// Update indicator position on screen based on coin direction
        /// </summary>
        private void UpdateScreenPosition()
        {
            if (indicatorContainer == null) return;
            
            if (Mathf.Abs(screenWidth - Screen.width) > 0.1f || Mathf.Abs(screenHeight - Screen.height) > 0.1f)
            {
                screenWidth = Screen.width;
                screenHeight = Screen.height;
            }
            
            float halfW = screenWidth * 0.5f;
            float halfH = screenHeight * 0.5f;
            float sideX = Mathf.Max(80f, halfW - edgePadding);
            float topY = Mathf.Max(120f, halfH - edgePadding);
            
            if (IsCoinOnScreen)
            {
                indicatorContainer.anchoredPosition = new Vector2(0f, 140f);
                return;
            }
            
            float absBearing = Mathf.Abs(smoothedRelativeBearing);
            float sign = Mathf.Sign(smoothedRelativeBearing);
            
            if (absBearing < 120f)
            {
                indicatorContainer.anchoredPosition = new Vector2(sign * sideX, 100f);
            }
            else
            {
                float xHint = sign * Mathf.Min(sideX * 0.35f, 220f);
                indicatorContainer.anchoredPosition = new Vector2(xHint, topY);
            }
        }
        
        #endregion
        
        #region Visual Updates
        
        /// <summary>
        /// Update colors and messages based on distance
        /// </summary>
        private void UpdateVisuals()
        {
            Color targetColor;
            string message;
            
            if (CurrentDistance <= collectionDistance)
            {
                // In collection range!
                targetColor = veryNearColor;
                message = inRangeMessage;
                isPulsing = true;
            }
            else if (CurrentDistance <= materializationDistance)
            {
                // Very close - coin should be visible in AR
                targetColor = nearColor;
                message = veryNearMessage;
                isPulsing = true;
            }
            else if (CurrentDistance <= materializationDistance * 2)
            {
                // Getting closer
                targetColor = nearColor;
                message = nearMessage;
                isPulsing = false;
            }
            else
            {
                // Far away
                targetColor = farColor;
                message = farMessage;
                isPulsing = false;
            }
            
            // Update colors
            if (arrowImage != null)
            {
                arrowImage.color = targetColor;
            }
            
            if (backgroundPanel != null)
            {
                Color bgColor = targetColor;
                bgColor.a = 0.7f;
                backgroundPanel.color = bgColor;
            }
            
            // Update status message
            if (statusText != null)
            {
                statusText.text = message;
            }
            
            // Reset scale if not pulsing
            if (!isPulsing)
            {
                ResetScale();
            }
        }
        
        /// <summary>
        /// Pulse animation for when player is close
        /// </summary>
        private void UpdatePulseAnimation()
        {
            if (arrowTransform == null && indicatorContainer == null) return;
            
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, pulse);
            
            if (arrowTransform != null)
            {
                arrowTransform.localScale = baseScale * scale;
            }
            else if (indicatorContainer != null)
            {
                indicatorContainer.localScale = baseScale * scale;
            }
        }
        
        /// <summary>
        /// Reset scale to base
        /// </summary>
        private void ResetScale()
        {
            if (arrowTransform != null)
            {
                arrowTransform.localScale = baseScale;
            }
            else if (indicatorContainer != null)
            {
                indicatorContainer.localScale = baseScale;
            }
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the direction indicator.
        /// Uses CanvasGroup alpha so the GameObject stays active.
        /// </summary>
        public void Show()
        {
            Debug.Log($"[CoinDirectionIndicator] Show() called! CanvasGroup={canvasGroup != null}, Container={indicatorContainer != null}");
            
            // Always ensure the root is active first. CanvasGroup alpha has no effect if the object is inactive.
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            
            // Prefer CanvasGroup for visibility.
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            else if (indicatorContainer != null)
            {
                indicatorContainer.gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
            
            IsVisible = true;
            Debug.Log($"[CoinDirectionIndicator] Now VISIBLE, alpha={canvasGroup?.alpha ?? -1f}, activeSelf={gameObject.activeSelf}");
        }
        
        /// <summary>
        /// Hide the direction indicator.
        /// Uses CanvasGroup alpha so the GameObject stays active.
        /// </summary>
        public void Hide()
        {
            // Never deactivate root object; we need updates/events alive so Show() is always recoverable.
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            
            // Prefer CanvasGroup for visibility.
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            else if (indicatorContainer != null)
            {
                indicatorContainer.gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            
            IsVisible = false;
            isPulsing = false;
            Log("Direction indicator hidden (CanvasGroup alpha=0)");
        }
        
        /// <summary>
        /// Toggle visibility
        /// </summary>
        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }
        
        #endregion
        
        #region Helpers
        
        /// <summary>
        /// Find the AR camera
        /// </summary>
        private void FindCamera()
        {
            arCamera = Camera.main;
            
            if (arCamera == null)
            {
                var arCameraManager = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>();
                if (arCameraManager != null)
                {
                    arCamera = arCameraManager.GetComponent<Camera>();
                }
            }
            
            if (arCamera == null)
            {
                arCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        /// <summary>
        /// Get player location
        /// </summary>
        private LocationData GetPlayerLocation()
        {
            if (GPSManager.Exists && GPSManager.Instance.CurrentLocation != null)
            {
                return GPSManager.Instance.CurrentLocation;
            }
            
            if (PlayerData.Exists)
            {
                return PlayerData.Instance.GetBestLocation();
            }
            
            return null;
        }
        
        /// <summary>
        /// Format distance for display
        /// </summary>
        private string FormatDistance(float meters)
        {
            if (meters < 1f)
            {
                return "< 1m";
            }
            else if (meters < 1000f)
            {
                return $"{meters:F0}m";
            }
            else
            {
                return $"{meters / 1000f:F1}km";
            }
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[CoinDirectionIndicator] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print state
        /// </summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== CoinDirectionIndicator State ===");
            Debug.Log($"Visible: {IsVisible}");
            Debug.Log($"Distance: {CurrentDistance:F1}m");
            Debug.Log($"Bearing: {CurrentBearing:F0}°");
            Debug.Log($"Device Heading: {deviceHeading:F0}°");
            Debug.Log($"Arrow Rotation: {currentArrowRotation:F0}°");
            Debug.Log($"Coin On Screen: {IsCoinOnScreen}");
            Debug.Log($"Pulsing: {isPulsing}");
            Debug.Log("=====================================");
        }
        
        /// <summary>
        /// Debug: Simulate target north
        /// </summary>
        [ContextMenu("Debug: Simulate Target North")]
        public void DebugSimulateNorth()
        {
            Show();
            CurrentDistance = 50f;
            CurrentBearing = 0f;
            targetArrowRotation = -GeoUtils.CalculateRelativeBearing(0f, deviceHeading);
            
            if (distanceText != null) distanceText.text = "50m";
            if (valueText != null) valueText.text = "$5.00";
            if (statusText != null) statusText.text = farMessage;
        }
        
        /// <summary>
        /// Debug: Simulate close target
        /// </summary>
        [ContextMenu("Debug: Simulate Close Target")]
        public void DebugSimulateClose()
        {
            Show();
            CurrentDistance = 8f;
            CurrentBearing = 45f;
            targetArrowRotation = -GeoUtils.CalculateRelativeBearing(45f, deviceHeading);
            
            if (distanceText != null) distanceText.text = "8m";
            if (valueText != null) valueText.text = "$10.00";
            if (statusText != null) statusText.text = veryNearMessage;
            
            isPulsing = true;
        }
        
        #endregion
    }
}
