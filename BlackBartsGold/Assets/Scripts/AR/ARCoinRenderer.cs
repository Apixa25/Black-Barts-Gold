// ============================================================================
// ARCoinRenderer.cs
// Black Bart's Gold - Distance-Adaptive AR Coin Renderer
// Path: Assets/Scripts/AR/ARCoinRenderer.cs
// ============================================================================
// Handles distance-adaptive rendering for AR coins.
// Implements ViroReact-style billboard behavior with world-locking transition.
// Reference: Docs/AR-COIN-DISPLAY-SPEC.md
// ============================================================================

using UnityEngine;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Distance-adaptive coin renderer.
    /// - Billboard mode (far): Always faces camera, constant screen size
    /// - World-locked mode (near): Fixed position, natural perspective scaling
    /// </summary>
    public class ARCoinRenderer : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("References")]
        [SerializeField]
        [Tooltip("The 3D coin mesh transform (child object)")]
        private Transform coinVisual;
        
        [SerializeField]
        [Tooltip("Optional mesh renderer for material changes")]
        private MeshRenderer meshRenderer;
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Display settings (uses default if null)")]
        private CoinDisplaySettings settings;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;  // Keep enabled for now to diagnose visibility issues
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current display mode
        /// </summary>
        public CoinDisplayMode CurrentMode { get; private set; } = CoinDisplayMode.Hidden;
        
        /// <summary>
        /// Distance to camera in meters
        /// </summary>
        public float DistanceToCamera { get; private set; } = float.MaxValue;
        
        /// <summary>
        /// Is the coin visible?
        /// </summary>
        public bool IsVisible => CurrentMode != CoinDisplayMode.Hidden;
        
        /// <summary>
        /// Is the coin in collection range? Uses GPS distance for accuracy.
        /// </summary>
        public bool IsInCollectionRange
        {
            get
            {
                // Use GPS distance if available (more accurate for collection)
                float distance = coinPositioner != null ? coinPositioner.GPSDistance : DistanceToCamera;
                return distance <= Settings.collectionDistance;
            }
        }
        
        /// <summary>
        /// Settings (uses default if not assigned)
        /// </summary>
        public CoinDisplaySettings Settings => settings ?? CoinDisplaySettings.Default;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when display mode changes
        /// </summary>
        public event Action<CoinDisplayMode, CoinDisplayMode> OnModeChanged;
        
        /// <summary>
        /// Fired when coin enters collection range
        /// </summary>
        public event Action OnEnteredCollectionRange;
        
        /// <summary>
        /// Fired when coin exits collection range
        /// </summary>
        public event Action OnExitedCollectionRange;
        
        #endregion
        
        #region Private Fields
        
        // Camera reference
        private Camera arCamera;
        private Transform cameraTransform;
        private bool cameraFound = false;
        
        // GPS positioner reference (for GPS-based distance)
        private ARCoinPositioner coinPositioner;
        
        // Mode transition
        private CoinDisplayMode targetMode = CoinDisplayMode.Hidden;
        private float modeTransitionProgress = 0f;
        private bool isTransitioning = false;
        
        // Scaling
        private float currentScale = 1f;
        private float scaleVelocity = 0f;
        
        // Animation
        private float bobOffset;
        private float spinAngle = 0f;
        
        // Update timing
        private float lastUpdateTime = 0f;
        
        // Collection range tracking
        private bool wasInCollectionRange = false;
        
        // Initial state
        private Vector3 initialLocalScale;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Cache initial scale
            initialLocalScale = transform.localScale;
            
            // Random bob phase so coins don't all bob in sync
            bobOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            
            // Auto-find visual if not set
            if (coinVisual == null && transform.childCount > 0)
            {
                coinVisual = transform.GetChild(0);
            }
            
            // Auto-find renderer
            if (meshRenderer == null && coinVisual != null)
            {
                meshRenderer = coinVisual.GetComponent<MeshRenderer>();
            }
            
            // Get GPS positioner for distance-based mode decisions
            coinPositioner = GetComponent<ARCoinPositioner>();
        }
        
        private void Start()
        {
            // Try to find positioner again (may have been added after Awake)
            if (coinPositioner == null)
            {
                coinPositioner = GetComponent<ARCoinPositioner>();
            }
            
            // Start in Hidden mode - will transition to visible when GPS distance is known
            SetVisibility(false);
            CurrentMode = CoinDisplayMode.Hidden;
            targetMode = CoinDisplayMode.Hidden;
            
            if (debugMode)
            {
                Debug.Log($"[ARCoinRenderer] Started - initial mode: Hidden, coinVisual: {(coinVisual != null ? coinVisual.name : "NULL")}, positioner: {(coinPositioner != null ? "Found" : "NULL")}");
            }
        }
        
        private void Update()
        {
            // Find camera if needed
            if (!cameraFound)
            {
                TryFindCamera();
                if (!cameraFound)
                {
                    // Log only occasionally to avoid spam
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning($"[ARCoinRenderer] Camera not found! Frame {Time.frameCount}");
                    }
                    return;
                }
            }
            
            // Try to find positioner if not found yet
            if (coinPositioner == null)
            {
                coinPositioner = GetComponent<ARCoinPositioner>();
            }
            
            // Check update timing based on mode
            float updateInterval = Settings.GetUpdateInterval(CurrentMode);
            if (Time.time - lastUpdateTime < updateInterval) return;
            lastUpdateTime = Time.time;
            
            // Log status periodically (every 3 seconds)
            if (debugMode && Time.frameCount % 90 == 0)
            {
                float gpsDistance = coinPositioner != null ? coinPositioner.GPSDistance : -1f;
                Vector3 camPos = cameraTransform != null ? cameraTransform.position : Vector3.zero;
                Vector3 camRot = cameraTransform != null ? cameraTransform.eulerAngles : Vector3.zero;
                string parentName = transform.parent != null ? transform.parent.name : "NULL";
                Debug.Log($"[ARCoinRenderer] Mode={CurrentMode}, GPS={gpsDistance:F1}m, AR={DistanceToCamera:F1}m | CoinPos={transform.position} | CamPos={camPos} | CamRotY={camRot.y:F0}° | Parent={parentName}");
            }
            
            // Core update loop
            UpdateDistance();
            UpdateDisplayMode();
            ApplyModeRendering();
            CheckCollectionRange();
        }
        
        #endregion
        
        #region Camera Finding
        
        private void TryFindCamera()
        {
            // Try Camera.main
            arCamera = Camera.main;
            
            // Fallback: AR Camera Manager
            if (arCamera == null)
            {
                var arCameraManager = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>();
                if (arCameraManager != null)
                {
                    arCamera = arCameraManager.GetComponent<Camera>();
                }
            }
            
            // Fallback: XROrigin
            if (arCamera == null)
            {
                var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    arCamera = xrOrigin.Camera;
                }
            }
            
            // Last fallback: Any camera
            if (arCamera == null)
            {
                arCamera = FindFirstObjectByType<Camera>();
            }
            
            if (arCamera != null)
            {
                cameraTransform = arCamera.transform;
                cameraFound = true;
                
                if (debugMode)
                {
                    Debug.Log($"[ARCoinRenderer] Found camera: {arCamera.name}");
                }
            }
        }
        
        #endregion
        
        #region Distance & Mode Updates
        
        private void UpdateDistance()
        {
            if (cameraTransform == null) return;
            DistanceToCamera = Vector3.Distance(transform.position, cameraTransform.position);
        }
        
        private void UpdateDisplayMode()
        {
            // ================================================================
            // USE GPS DISTANCE FOR MODE DECISIONS (not AR distance)
            // GPS distance is the "real" distance to the coin's physical location.
            // AR distance can be misleading because AR camera doesn't track GPS movement.
            // ================================================================
            float distanceForMode = coinPositioner != null ? coinPositioner.GPSDistance : DistanceToCamera;
            
            // ================================================================
            // FALLBACK: If GPS distance is unknown (MaxValue), use Billboard mode
            // Don't hide coins just because GPS isn't ready yet!
            // ================================================================
            CoinDisplayMode newTargetMode;
            if (distanceForMode >= float.MaxValue - 1f)
            {
                // GPS not ready - show in billboard mode as fallback
                newTargetMode = CoinDisplayMode.Billboard;
                
                if (debugMode && CurrentMode == CoinDisplayMode.Hidden)
                {
                    Debug.Log($"[ARCoinRenderer] GPS distance unknown, using Billboard mode as fallback");
                }
            }
            else
            {
                // Normal mode determination based on GPS distance
                newTargetMode = Settings.GetModeForDistance(distanceForMode, CurrentMode);
            }
            
            if (newTargetMode != targetMode)
            {
                // Start transition
                CoinDisplayMode oldMode = CurrentMode;
                targetMode = newTargetMode;
                isTransitioning = true;
                modeTransitionProgress = 0f;
                
                if (debugMode)
                {
                    Debug.Log($"[ARCoinRenderer] Mode transition: {oldMode} → {targetMode} (GPS dist: {distanceForMode:F1}m, AR dist: {DistanceToCamera:F1}m)");
                }
                
                // Immediate visibility change for Hidden mode
                if (targetMode == CoinDisplayMode.Hidden)
                {
                    SetVisibility(false);
                    CurrentMode = CoinDisplayMode.Hidden;
                    isTransitioning = false;
                    OnModeChanged?.Invoke(oldMode, CurrentMode);
                }
                else if (CurrentMode == CoinDisplayMode.Hidden)
                {
                    // Becoming visible
                    SetVisibility(true);
                    CurrentMode = targetMode;
                    isTransitioning = false;
                    OnModeChanged?.Invoke(oldMode, CurrentMode);
                }
            }
            
            // Process ongoing transition
            if (isTransitioning)
            {
                modeTransitionProgress += Time.deltaTime / Settings.transitionSmoothTime;
                
                if (modeTransitionProgress >= 1f)
                {
                    // Transition complete
                    CoinDisplayMode oldMode = CurrentMode;
                    CurrentMode = targetMode;
                    isTransitioning = false;
                    modeTransitionProgress = 1f;
                    
                    OnModeChanged?.Invoke(oldMode, CurrentMode);
                }
            }
        }
        
        #endregion
        
        #region Mode Rendering
        
        private void ApplyModeRendering()
        {
            switch (CurrentMode)
            {
                case CoinDisplayMode.Hidden:
                    // Already handled
                    break;
                    
                case CoinDisplayMode.Billboard:
                    ApplyBillboardMode();
                    break;
                    
                case CoinDisplayMode.WorldLocked:
                    ApplyWorldLockedMode();
                    break;
            }
        }
        
        /// <summary>
        /// Billboard mode: Always faces camera with constant screen size.
        /// Based on ViroReact's transformBehaviors: ["billboard"] pattern.
        /// </summary>
        private void ApplyBillboardMode()
        {
            if (cameraTransform == null) return;
            
            // ============================================================
            // STEP 1: BILLBOARD ROTATION (ViroReact pattern)
            // Make coin always face the camera (billboardY style - upright)
            // ============================================================
            Vector3 lookDirection = cameraTransform.position - transform.position;
            lookDirection.y = 0; // Keep upright
            
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-lookDirection, Vector3.up);
            }
            
            // ============================================================
            // STEP 2: CONSTANT SCREEN SIZE SCALING
            // This is the KEY formula for distance-independent visibility
            // ============================================================
            float targetScale = CalculateScaleForScreenSize(Settings.minScreenSizePixels);
            
            // Smooth scale transition
            currentScale = Mathf.SmoothDamp(currentScale, targetScale, ref scaleVelocity, 0.1f);
            transform.localScale = initialLocalScale * currentScale;
            
            // ============================================================
            // STEP 3: SPIN ANIMATION
            // ============================================================
            if (coinVisual != null)
            {
                spinAngle += Settings.spinSpeed * Time.deltaTime;
                coinVisual.localRotation = Quaternion.Euler(0, spinAngle, 0);
            }
        }
        
        /// <summary>
        /// World-locked mode: Fixed in AR space with natural perspective.
        /// Based on ViroReact's dragType: "FixedToWorld" pattern.
        /// </summary>
        private void ApplyWorldLockedMode()
        {
            // ============================================================
            // STEP 1: PERSPECTIVE SCALING
            // Closer = bigger, but don't exceed maxWorldScale
            // ============================================================
            float normalizedDistance = Mathf.InverseLerp(
                Settings.collectionDistance,
                Settings.billboardDistance,
                DistanceToCamera
            );
            
            // Inverse: closer (0) = bigger, further (1) = smaller
            float targetWorldScale = Mathf.Lerp(
                Settings.maxWorldScale,
                Settings.baseWorldScale,
                normalizedDistance
            );
            
            // Apply scale relative to base
            float scaleMultiplier = targetWorldScale / Settings.baseWorldScale;
            currentScale = Mathf.SmoothDamp(currentScale, scaleMultiplier, ref scaleVelocity, 0.1f);
            transform.localScale = initialLocalScale * currentScale;
            
            // ============================================================
            // STEP 2: Y-AXIS SPIN (world-locked, not billboard)
            // ============================================================
            if (coinVisual != null)
            {
                spinAngle += Settings.spinSpeed * Time.deltaTime;
                coinVisual.localRotation = Quaternion.Euler(0, spinAngle, 0);
                
                // ============================================================
                // STEP 3: BOB ANIMATION
                // ============================================================
                float bobY = Mathf.Sin((Time.time * Settings.bobFrequency * Mathf.PI) + bobOffset) * Settings.bobAmplitude;
                coinVisual.localPosition = new Vector3(0, bobY, 0);
            }
            
            // ============================================================
            // STEP 4: ROTATION - Stop billboarding, stay fixed
            // Only reset rotation during transition, then leave it alone
            // ============================================================
            if (isTransitioning && modeTransitionProgress < 0.5f)
            {
                // Gradually stop facing camera
                Vector3 lookDir = cameraTransform.position - transform.position;
                lookDir.y = 0;
                
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    float billboardInfluence = 1f - (modeTransitionProgress * 2f);
                    Quaternion billboardRot = Quaternion.LookRotation(-lookDir, Vector3.up);
                    Quaternion worldRot = Quaternion.identity;
                    transform.rotation = Quaternion.Slerp(worldRot, billboardRot, billboardInfluence);
                }
            }
        }
        
        #endregion
        
        #region Screen Size Calculation
        
        /// <summary>
        /// Calculate the world scale needed to achieve a specific screen size in pixels.
        /// This is the KEY FORMULA for distance-independent visibility.
        /// 
        /// Math: screenSize = (worldSize / distance) * (screenHeight / (2 * tan(fov/2)))
        /// Solved for worldSize: worldSize = (targetScreenPixels * distance * 2 * tan(fov/2)) / screenHeight
        /// </summary>
        private float CalculateScaleForScreenSize(float targetScreenPixels)
        {
            if (arCamera == null) return 1f;
            
            // Camera field of view and screen height
            float fov = arCamera.fieldOfView;
            float screenHeight = Screen.height;
            
            // Calculate world size that produces targetScreenPixels at current distance
            float halfFovRad = fov * 0.5f * Mathf.Deg2Rad;
            float tanHalfFov = Mathf.Tan(halfFovRad);
            
            float worldScale = (targetScreenPixels * DistanceToCamera * 2f * tanHalfFov) / screenHeight;
            
            // Convert to scale multiplier relative to base scale
            float scaleMultiplier = worldScale / Settings.baseWorldScale;
            
            // Clamp to reasonable bounds
            float minMultiplier = 0.5f;
            float maxMultiplier = 10f;
            
            return Mathf.Clamp(scaleMultiplier, minMultiplier, maxMultiplier);
        }
        
        #endregion
        
        #region Collection Range
        
        private void CheckCollectionRange()
        {
            bool isInRange = IsInCollectionRange;
            float gpsDistance = coinPositioner != null ? coinPositioner.GPSDistance : DistanceToCamera;
            
            if (isInRange && !wasInCollectionRange)
            {
                // Entered collection range
                wasInCollectionRange = true;
                OnEnteredCollectionRange?.Invoke();
                
                if (debugMode)
                {
                    Debug.Log($"[ARCoinRenderer] Entered collection range (GPS: {gpsDistance:F1}m, AR: {DistanceToCamera:F1}m)");
                }
            }
            else if (!isInRange && wasInCollectionRange)
            {
                // Exited collection range
                wasInCollectionRange = false;
                OnExitedCollectionRange?.Invoke();
                
                if (debugMode)
                {
                    Debug.Log($"[ARCoinRenderer] Exited collection range (GPS: {gpsDistance:F1}m, AR: {DistanceToCamera:F1}m)");
                }
            }
        }
        
        #endregion
        
        #region Visibility Control
        
        private void SetVisibility(bool visible)
        {
            // Enable/disable renderers
            if (meshRenderer != null)
            {
                meshRenderer.enabled = visible;
            }
            
            // Also check child renderers
            var renderers = GetComponentsInChildren<Renderer>(true);
            int rendererCount = 0;
            foreach (var r in renderers)
            {
                r.enabled = visible;
                rendererCount++;
            }
            
            // Enable/disable colliders
            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders)
            {
                c.enabled = visible;
            }
            
            if (debugMode)
            {
                float gpsDistance = coinPositioner != null ? coinPositioner.GPSDistance : -1f;
                Debug.Log($"[ARCoinRenderer] Visibility={visible}, Renderers={rendererCount}, Mode={CurrentMode}, GPS dist={gpsDistance:F1}m, Pos={transform.position}");
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Force a specific display mode (for testing or special cases)
        /// </summary>
        public void ForceMode(CoinDisplayMode mode)
        {
            CoinDisplayMode oldMode = CurrentMode;
            targetMode = mode;
            CurrentMode = mode;
            isTransitioning = false;
            
            SetVisibility(mode != CoinDisplayMode.Hidden);
            
            OnModeChanged?.Invoke(oldMode, mode);
        }
        
        /// <summary>
        /// Set custom settings
        /// </summary>
        public void SetSettings(CoinDisplaySettings newSettings)
        {
            settings = newSettings;
        }
        
        /// <summary>
        /// Update material color (for locked/in-range states)
        /// </summary>
        public void SetColor(Color color)
        {
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = color;
            }
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Print Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== ARCoinRenderer Status ===");
            Debug.Log($"Mode: {CurrentMode}");
            Debug.Log($"Target Mode: {targetMode}");
            Debug.Log($"Distance: {DistanceToCamera:F1}m");
            Debug.Log($"Scale: {currentScale:F2}");
            Debug.Log($"Is Visible: {IsVisible}");
            Debug.Log($"In Collection Range: {IsInCollectionRange}");
            Debug.Log($"Camera Found: {cameraFound}");
            Debug.Log("=============================");
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            // Draw distance rings
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Settings?.collectionDistance ?? 5f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Settings?.billboardDistance ?? 15f);
            
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, Settings?.hideDistance ?? 100f);
        }
        
        #endregion
    }
}
