// ============================================================================
// ARCoinRenderer.cs
// Black Bart's Gold - Pokemon GO-Style AR Coin Renderer
// Path: Assets/Scripts/AR/ARCoinRenderer.cs
// ============================================================================
// FULL REFACTOR: Implements Pokemon GO's proven materialization pattern.
// 
// Key insight from research: Pokemon GO NEVER shows creatures at their GPS
// distance through the AR camera. Instead:
//   1. Player navigates using map/compass to get close
//   2. When within range, creature "materializes" at a comfortable viewing distance
//   3. Player can then walk around and interact with it
//
// This approach works because:
//   - GPS accuracy (±5-15m) makes distant AR positioning unreliable
//   - Small objects at 50-100m are literally invisible (< 5 pixels)
//   - AR tracking works best in local space (~20m range)
//
// Reference: Pokemon GO patterns research, Niantic documentation
// ============================================================================

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System;
using System.Collections;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Display state for AR coins using Pokemon GO materialization pattern.
    /// </summary>
    public enum CoinDisplayMode
    {
        /// <summary>Coin not yet materialized - use direction indicator to find it</summary>
        Hidden,
        
        /// <summary>Coin is materializing with entrance effect</summary>
        Materializing,
        
        /// <summary>Coin visible and interactive in AR space</summary>
        Visible,
        
        /// <summary>Coin in collection range - can be collected</summary>
        Collectible,
        
        /// <summary>Coin being collected</summary>
        Collecting
    }
    
    /// <summary>
    /// Pokemon GO-style AR coin renderer with materialization pattern.
    /// 
    /// THE KEY DIFFERENCE: Coins don't exist in AR until player is close enough.
    /// Before that, player uses the CoinDirectionIndicator to navigate.
    /// </summary>
    public class ARCoinRenderer : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Visual References")]
        [SerializeField]
        [Tooltip("The 3D coin mesh (child object)")]
        private Transform coinVisual;
        
        [SerializeField]
        [Tooltip("Mesh renderer for material/color changes")]
        private MeshRenderer meshRenderer;
        
        [SerializeField]
        [Tooltip("Optional particle system for sparkle effects")]
        private ParticleSystem sparkleParticles;
        
        [SerializeField]
        [Tooltip("Optional particle system for materialization effect")]
        private ParticleSystem materializeParticles;
        
        [Header("Settings")]
        [SerializeField]
        private CoinDisplaySettings settings;
        
        [Header("Materialization Settings")]
        [SerializeField]
        [Tooltip("Distance in front of camera where coin materializes")]
        private float materializeDistance = 4f;
        
        [SerializeField]
        [Tooltip("Height offset when materialized")]
        private float materializeHeight = 1.2f;
        
        [SerializeField]
        [Tooltip("Duration of materialization animation")]
        private float materializeDuration = 0.8f;
        
        [Header("Animation")]
        [SerializeField]
        [Tooltip("Degrees per second around the Y axis (slow top-like spin)")]
        private float spinSpeed = 15f;
        
        [SerializeField]
        private float bobAmplitude = 0.05f;
        
        [SerializeField]
        private float bobFrequency = 2f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>Current display mode</summary>
        public CoinDisplayMode CurrentMode { get; private set; } = CoinDisplayMode.Hidden;
        
        /// <summary>GPS distance to coin (from positioner)</summary>
        public float GPSDistance { get; private set; } = float.MaxValue;
        
        /// <summary>AR distance from camera to coin visual</summary>
        public float ARDistance { get; private set; } = float.MaxValue;
        
        /// <summary>Is coin visible in AR?</summary>
        public bool IsVisible => CurrentMode == CoinDisplayMode.Visible || 
                                 CurrentMode == CoinDisplayMode.Collectible;
        
        /// <summary>Is coin in collection range?</summary>
        public bool IsInCollectionRange => CurrentMode == CoinDisplayMode.Collectible;
        
        /// <summary>Settings in use</summary>
        public CoinDisplaySettings Settings => settings ?? CoinDisplaySettings.Default;
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when coin materializes into view</summary>
        public event Action OnMaterialized;
        
        /// <summary>Fired when coin enters collection range</summary>
        public event Action OnEnteredCollectionRange;
        
        /// <summary>Fired when coin exits collection range</summary>
        public event Action OnExitedCollectionRange;
        
        /// <summary>Fired when display mode changes</summary>
        public event Action<CoinDisplayMode, CoinDisplayMode> OnModeChanged;
        
        #endregion
        
        #region Private Fields
        
        // Camera reference
        private Camera arCamera;
        private Transform cameraTransform;
        private bool cameraFound = false;
        
        // Positioner reference
        private ARCoinPositioner positioner;
        
        // Animation state
        private float spinAngle = 0f;
        private float bobOffset;
        private Vector3 baseScale;
        
        // Materialization state
        private Vector3 materializedPosition;
        private bool isMaterializing = false;
        private float materializeProgress = 0f;
        
        // Collection range tracking
        private bool wasInCollectionRange = false;
        
        // Debug: throttle periodic visual state logs (every 2.5s when visible)
        private float nextVisualLogTime = 0f;
        // Color diag: throttle periodic material read-back logs (every 3s when Visible+texture)
        private float nextColorDiagTime = 0f;
        private bool colorSnapshot2sDone = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Random bob phase
            bobOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            
            // Cache the prefab's original scale as the reference base.
            // Distance-based scaling (CalculateDistanceScale) handles all size changes.
            baseScale = transform.localScale;
            
            // Auto-find visual
            if (coinVisual == null && transform.childCount > 0)
            {
                coinVisual = transform.GetChild(0);
            }
            
            // Auto-find renderer
            if (meshRenderer == null && coinVisual != null)
            {
                meshRenderer = coinVisual.GetComponent<MeshRenderer>();
            }
            
            // Note: positioner is found in Start() to ensure CoinController has added it
        }
        
        private void Start()
        {
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: Start() BEGIN on {gameObject.name}");
            
            // Get positioner - must be in Start() to ensure CoinController.Awake() has added it
            positioner = GetComponent<ARCoinPositioner>();
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: Positioner={positioner != null}");
            
            // Start hidden - Direction Indicator guides player to us
            SetMode(CoinDisplayMode.Hidden);
            
            // Check AR session state at start
            var arState = UnityEngine.XR.ARFoundation.ARSession.state;
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: INITIAL AR STATE: {arState}");
            
            // Log all relevant component states
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: STARTED! Components:");
            Debug.Log($"[ARCoinRenderer]   - Settings: {settings != null}");
            Debug.Log($"[ARCoinRenderer]   - Positioner: {positioner != null}");
            Debug.Log($"[ARCoinRenderer]   - CoinVisual: {coinVisual != null}");
            Debug.Log($"[ARCoinRenderer]   - MeshRenderer: {meshRenderer != null}");
            Debug.Log($"[ARCoinRenderer]   - MaterializeDist: {Settings.materializationDistance}m");
            Debug.Log($"[ARCoinRenderer]   - CollectionDist: {Settings.collectionDistance}m");
            
            // Debug: prefab/hierarchy and mesh/material (so we can verify Quad + BB texture in build)
            Debug.Log($"[ARCoinRenderer] === PREFAB/HIERARCHY ===");
            Debug.Log($"[ARCoinRenderer]   Root: name={gameObject.name}, childCount={transform.childCount}, localScale={transform.localScale}");
            if (transform.childCount > 0)
            {
                Transform ch = transform.GetChild(0);
                Debug.Log($"[ARCoinRenderer]   Child0: name={ch.name}, localPos={ch.localPosition}, localEuler={ch.localEulerAngles}, localScale={ch.localScale}");
            }
            if (meshRenderer != null)
            {
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                string meshName = (meshFilter != null && meshFilter.sharedMesh != null) ? meshFilter.sharedMesh.name : "null";
                string texName = (meshRenderer.sharedMaterial != null && meshRenderer.sharedMaterial.mainTexture != null)
                    ? meshRenderer.sharedMaterial.mainTexture.name : "null";
                Debug.Log($"[ARCoinRenderer]   Mesh: {meshName}, MainTex: {texName}");
            }
            Debug.Log($"[ARCoinRenderer] === END PREFAB/HIERARCHY ===");
            
            // ── FIX GOLD COLOR (or preserve full-color texture) ───────────────
            // Gold coins: tint + emission so they read as treasure gold in AR.
            // Full-color coins (e.g. Color BB with skin, silver, etc.): use white _Color
            // so the basecolor texture shows through without washing out.
            if (meshRenderer != null && meshRenderer.material != null)
            {
                Material mat = meshRenderer.material;
                bool isFullColorCoin = IsFullColorCoinMaterial(mat);
                
                mat.SetFloat("_Metallic", 0.4f);
                mat.SetFloat("_Glossiness", 0.65f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                
                if (isFullColorCoin)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.SetColor("_Color", Color.white);
                    Debug.Log($"[ARCoinRenderer] 🎨 FULL-COLOR COIN: _Color=white, no emission (texture drives look)");
                }
                else
                {
                    Color goldEmission = new Color(0.98f, 0.76f, 0.2f, 1f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", goldEmission);
                    mat.SetColor("_Color", new Color(1f, 0.82f, 0.28f, 1f));
                    Debug.Log($"[ARCoinRenderer] 🪙 GOLD FIX APPLIED: Metallic=0.4, Glossiness=0.65, Emission=({goldEmission.r:F2},{goldEmission.g:F2},{goldEmission.b:F2})");
                }
                
                bool isInstance = (mat != meshRenderer.sharedMaterial);
                Color readColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.clear;
                Color readEmission = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.clear;
                float readMetallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : -1f;
                float readGloss = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : -1f;
                string shaderName = mat.shader != null ? mat.shader.name : "null";
                Debug.Log($"[ARCoinRenderer] 🎨 READBACK: _Color=({readColor.r:F2},{readColor.g:F2},{readColor.b:F2}) _Emission=({readEmission.r:F2},{readEmission.g:F2},{readEmission.b:F2}) _Metallic={readMetallic:F2} _Gloss={readGloss:F2} shader={shaderName} isInstance={isInstance}");
            }
            // One-time log so ADB can confirm this build uses the device orientation (X=270, Z=180)
            Debug.Log("[ARCoinRenderer] COIN ROTATION BUILD: Using X=270, Z=180 for upright Black Bart on device");
        }
        
        private void Update()
        {
            // Find camera if needed
            if (!cameraFound)
            {
                TryFindCamera();
                if (!cameraFound)
                {
                    // Log once per second if camera not found
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning($"[ARCoinRenderer] Camera NOT FOUND! Searching...");
                    }
                    return;
                }
            }
            
            // Update distances
            UpdateDistances();
            
            // COLOR DIAG: one-time snapshot ~2s after Start to see if material was overwritten
            if (!colorSnapshot2sDone && Time.realtimeSinceStartup >= 2f && meshRenderer != null && meshRenderer.material != null)
            {
                colorSnapshot2sDone = true;
                Material mat = meshRenderer.material;
                Color c = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.clear;
                Color e = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.clear;
                float m = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : -1f;
                float g = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : -1f;
                Debug.Log($"[ARCoinRenderer] 🎨 COLOR SNAPSHOT T+2s: _Color=({c.r:F2},{c.g:F2},{c.b:F2}) _Emission=({e.r:F2},{e.g:F2},{e.b:F2}) _Metallic={m:F2} _Gloss={g:F2} shader={mat.shader?.name ?? "null"}");
            }
            
            // Check for mode transitions based on GPS distance
            UpdateModeFromDistance();
            
            // Apply current mode rendering
            switch (CurrentMode)
            {
                case CoinDisplayMode.Hidden:
                    // Nothing to render - Direction Indicator handles navigation
                    break;
                    
                case CoinDisplayMode.Materializing:
                    UpdateMaterialization();
                    break;
                    
                case CoinDisplayMode.Visible:
                case CoinDisplayMode.Collectible:
                    UpdateVisibleCoin();
                    break;
                    
                case CoinDisplayMode.Collecting:
                    // Handled by collection animation coroutine
                    break;
            }
        }
        
        #endregion
        
        #region Camera Finding
        
        private void TryFindCamera()
        {
            arCamera = Camera.main;
            
            if (arCamera == null)
            {
                var arCamManager = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>();
                if (arCamManager != null)
                {
                    arCamera = arCamManager.GetComponent<Camera>();
                }
            }
            
            if (arCamera == null)
            {
                var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    arCamera = xrOrigin.Camera;
                }
            }
            
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
        
        #region Distance Updates
        
        private void UpdateDistances()
        {
            // Get positioner if we don't have it yet
            if (positioner == null)
            {
                positioner = GetComponent<ARCoinPositioner>();
            }
            
            // Get GPS distance from positioner
            if (positioner != null)
            {
                GPSDistance = positioner.GPSDistance;
            }
            
            // Calculate AR distance (visual to camera)
            if (cameraTransform != null)
            {
                ARDistance = Vector3.Distance(transform.position, cameraTransform.position);
            }
        }
        
        #endregion
        
        #region Mode Management
        
        /// <summary>
        /// Update display mode based on GPS distance.
        /// This is the CORE of the Pokemon GO pattern.
        /// </summary>
        private void UpdateModeFromDistance()
        {
            // Skip if currently in a transition state
            if (CurrentMode == CoinDisplayMode.Materializing || 
                CurrentMode == CoinDisplayMode.Collecting)
            {
                return;
            }
            
            float distance = GPSDistance;
            
            // Log periodically to verify this method is running
            if (Time.frameCount % 180 == 0)
            {
                Debug.Log($"[ARCoinRenderer] UpdateMode: GPS={distance:F1}m, MaterializeDist={Settings.materializationDistance}m, Mode={CurrentMode}");
            }
            
            // ================================================================
            // POKEMON GO PATTERN:
            // 1. HIDDEN when far - player uses Direction Indicator
            // 2. MATERIALIZE when player gets close enough
            // 3. VISIBLE once materialized
            // 4. COLLECTIBLE when very close
            // ================================================================
            
            if (CurrentMode == CoinDisplayMode.Hidden)
            {
                // Check if player is close enough to materialize
                if (distance <= Settings.materializationDistance)
                {
                    StartMaterialization();
                }
            }
            else if (CurrentMode == CoinDisplayMode.Visible || 
                     CurrentMode == CoinDisplayMode.Collectible)
            {
                // Check collection range
                if (distance <= Settings.collectionDistance)
                {
                    if (CurrentMode != CoinDisplayMode.Collectible)
                    {
                        SetMode(CoinDisplayMode.Collectible);
                    }
                }
                else if (CurrentMode == CoinDisplayMode.Collectible)
                {
                    // Exited collection range
                    SetMode(CoinDisplayMode.Visible);
                }
                
                // Check if player moved too far away - hide coin again
                // (hideDistance provides hysteresis to prevent flickering)
                if (distance > Settings.hideDistance)
                {
                    SetMode(CoinDisplayMode.Hidden);
                    
                    if (debugMode)
                    {
                        Debug.Log($"[ARCoinRenderer] Player moved away ({distance:F1}m) - hiding coin");
                    }
                }
            }
            
            // Track collection range events
            bool inRange = CurrentMode == CoinDisplayMode.Collectible;
            if (inRange && !wasInCollectionRange)
            {
                wasInCollectionRange = true;
                OnEnteredCollectionRange?.Invoke();
            }
            else if (!inRange && wasInCollectionRange)
            {
                wasInCollectionRange = false;
                OnExitedCollectionRange?.Invoke();
            }
        }
        
        /// <summary>
        /// Set display mode and fire events
        /// </summary>
        private void SetMode(CoinDisplayMode newMode)
        {
            if (newMode == CurrentMode) return;
            
            CoinDisplayMode oldMode = CurrentMode;
            CurrentMode = newMode;
            
            // Update visibility
            bool shouldBeVisible = newMode != CoinDisplayMode.Hidden;
            SetVisibility(shouldBeVisible);
            
            if (debugMode)
            {
                Debug.Log($"[ARCoinRenderer] Mode: {oldMode} → {newMode} (GPS: {GPSDistance:F1}m)");
            }
            
            OnModeChanged?.Invoke(oldMode, newMode);
        }
        
        #endregion
        
        #region Materialization
        
        // Gyroscope for positioning
        private GyroscopeCoinPositioner gyroPositioner;
        private bool useGyroPositioning = false;
        
        /// <summary>
        /// Start the materialization animation.
        /// Coin appears in front of camera with sparkle effect.
        /// </summary>
        private void StartMaterialization()
        {
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: StartMaterialization() called, isMaterializing={isMaterializing}");
            
            if (isMaterializing)
            {
                Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: Already materializing, ignoring");
                return;
            }
            
            isMaterializing = true;
            materializeProgress = 0f;
            
            // Check if we should use gyroscope positioning (AR tracking not working)
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: Checking AR tracking state for gyro decision...");
            CheckAndEnableGyroPositioning();
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: UseGyroPositioning={useGyroPositioning}, GyroPositioner={(gyroPositioner != null ? "present" : "NULL")}");
            
            // ARCoinRenderer now owns positioning — disable CompassCoinPlacer to prevent fights
            var compassPlacer = GetComponent<CompassCoinPlacer>();
            if (compassPlacer != null && compassPlacer.enabled)
            {
                compassPlacer.enabled = false;
                Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: CompassCoinPlacer DISABLED (ARCoinRenderer owns positioning)");
            }
            
            // Calculate materialized position - in front of camera at comfortable distance
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: Calculating materialized position, camera={(cameraTransform != null ? cameraTransform.position.ToString() : "NULL")}");
            CalculateMaterializedPosition();
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: MaterializedPosition={materializedPosition}");
            
            // Set initial state
            transform.position = materializedPosition;
            transform.localScale = Vector3.zero; // Start at zero scale
            
            SetMode(CoinDisplayMode.Materializing);
            
            // Play materialization particles
            if (materializeParticles != null)
            {
                materializeParticles.transform.position = materializedPosition;
                materializeParticles.Play();
                Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: Playing materialize particles");
            }
            
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: *** MATERIALIZING *** Position={materializedPosition}, GPSDist={GPSDistance:F1}m, UseGyro={useGyroPositioning}");
        }
        
        /// <summary>
        /// Check if AR tracking is working. If not, enable gyroscope-based positioning.
        /// </summary>
        private void CheckAndEnableGyroPositioning()
        {
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: CheckAndEnableGyroPositioning() BEGIN");
            
            // Check if AR session is actually tracking
            var arSession = UnityEngine.XR.ARFoundation.ARSession.state;
            bool arTracking = arSession == UnityEngine.XR.ARFoundation.ARSessionState.SessionTracking;
            
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: ARSession.state={arSession}, arTracking={arTracking}");
            
            // ================================================================
            // NEW: Check if XR is actually providing tracked device data
            // ARSession.state=SessionTracking doesn't mean camera pose is updating!
            // We need XR Input Devices for TrackedPoseDriver to work.
            // ================================================================
            var xrDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, xrDevices);
            bool hasXRDevice = xrDevices.Count > 0;
            
            // Also check if camera has actually moved (strongest proof of 6DOF)
            bool cameraHasMoved = false;
            if (cameraTransform != null)
            {
                cameraHasMoved = Vector3.Distance(cameraTransform.position, Vector3.zero) > 0.1f ||
                                 cameraTransform.rotation != Quaternion.identity;
            }
            
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: TRACKING CHECK:");
            Debug.Log($"[ARCoinRenderer]   ARSession.state={arSession} (tracking={arTracking})");
            Debug.Log($"[ARCoinRenderer]   XR CenterEye Devices={xrDevices.Count} (has={hasXRDevice})");
            Debug.Log($"[ARCoinRenderer]   Camera has moved from origin={cameraHasMoved}");
            Debug.Log($"[ARCoinRenderer]   Camera pos={cameraTransform?.position}, rot={cameraTransform?.eulerAngles}");
            
            // Active XR loader
            string loaderName = XRGeneralSettings.Instance?.Manager?.activeLoader?.name ?? "NONE";
            Debug.Log($"[ARCoinRenderer]   Active XR Loader: {loaderName}");
            
            // Decision: use gyro if AR is not tracking OR if there are no XR devices
            bool shouldUseGyro = !arTracking || (!hasXRDevice && !cameraHasMoved);
            
            if (shouldUseGyro)
            {
                Debug.LogWarning($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: DECISION → USE GYRO POSITIONING");
                Debug.LogWarning($"[ARCoinRenderer]   Reason: arTracking={arTracking}, hasXRDevice={hasXRDevice}, cameraHasMoved={cameraHasMoved}");
                useGyroPositioning = true;
                
                // Add gyroscope positioner if not present
                gyroPositioner = GetComponent<GyroscopeCoinPositioner>();
                Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: GetComponent<GyroscopeCoinPositioner>() = {(gyroPositioner != null ? "found" : "NULL")}");
                
                if (gyroPositioner == null)
                {
                    Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: Adding GyroscopeCoinPositioner component...");
                    gyroPositioner = gameObject.AddComponent<GyroscopeCoinPositioner>();
                    Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: GyroscopeCoinPositioner ADDED");
                }
                
                // Disable CompassCoinPlacer so it doesn't fight with GyroscopeCoinPositioner
                var compassPlacer = GetComponent<CompassCoinPlacer>();
                if (compassPlacer != null && compassPlacer.enabled)
                {
                    compassPlacer.enabled = false;
                    Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: CompassCoinPlacer DISABLED (gyro active)");
                }
            }
            else
            {
                Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: DECISION → USE AR TRACKING (6DOF!)");
                Debug.Log($"[ARCoinRenderer]   arTracking={arTracking}, hasXRDevice={hasXRDevice}, cameraHasMoved={cameraHasMoved}");
                useGyroPositioning = false;
                Debug.Log("[ARCoinRenderer] AR tracking working - using AR camera positioning");
            }
        }
        
        // For tracking camera movement to detect AR failure
        private Vector3 lastCameraPosition = Vector3.zero;
        private float cameraStationaryTime = 0f;
        private const float CAMERA_STATIONARY_THRESHOLD = 3f; // seconds
        
        /// <summary>
        /// Continuously check AR state and camera movement.
        /// If AR stops working (camera not moving), switch to gyroscope.
        /// </summary>
        private void ContinuousARStateCheck()
        {
            if (cameraTransform == null)
            {
                if (Time.frameCount % 300 == 0)
                {
                    Debug.LogWarning($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: ContinuousARStateCheck - cameraTransform is NULL!");
                }
                return;
            }
            
            // Check AR session state
            var arState = UnityEngine.XR.ARFoundation.ARSession.state;
            bool arTracking = arState == UnityEngine.XR.ARFoundation.ARSessionState.SessionTracking;
            
            // Check if camera is moving (AR actually working)
            float cameraMoved = Vector3.Distance(cameraTransform.position, lastCameraPosition);
            
            if (cameraMoved < 0.001f)
            {
                cameraStationaryTime += Time.deltaTime;
            }
            else
            {
                cameraStationaryTime = 0f;
                lastCameraPosition = cameraTransform.position;
            }
            
            // Periodic AR status logging (every 3 seconds = ~180 frames at 60fps)
            if (Time.frameCount % 180 == 0)
            {
                // Check XR devices
                var xrDevices = new List<InputDevice>();
                InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, xrDevices);
                string loaderName = XRGeneralSettings.Instance?.Manager?.activeLoader?.name ?? "NoLoader";
                
                Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: AR CHECK - " +
                          $"State={arState}, Tracking={arTracking}, " +
                          $"CamStationary={cameraStationaryTime:F1}s (threshold={CAMERA_STATIONARY_THRESHOLD}s), " +
                          $"CamMoved={cameraMoved:F4}m, UseGyro={useGyroPositioning}, " +
                          $"XRDevices={xrDevices.Count}, Loader={loaderName}, " +
                          $"CamPos={cameraTransform.position}, CamRot={cameraTransform.eulerAngles}");
            }
            
            // If AR state is not tracking OR camera hasn't moved in a while, use gyro
            bool shouldUseGyro = !arTracking || cameraStationaryTime > CAMERA_STATIONARY_THRESHOLD;
            
            // Switch to gyro if needed
            if (shouldUseGyro && !useGyroPositioning)
            {
                Debug.LogWarning($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: *** SWITCHING TO GYRO ***");
                Debug.LogWarning($"[ARCoinRenderer]   Reason: arState={arState} (tracking={arTracking}), cameraStationary={cameraStationaryTime:F1}s (threshold={CAMERA_STATIONARY_THRESHOLD}s)");
                useGyroPositioning = true;
                
                // Add gyroscope positioner if not present
                if (gyroPositioner == null)
                {
                    gyroPositioner = GetComponent<GyroscopeCoinPositioner>();
                    if (gyroPositioner == null)
                    {
                        Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: Adding GyroscopeCoinPositioner component...");
                        gyroPositioner = gameObject.AddComponent<GyroscopeCoinPositioner>();
                        Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: GyroscopeCoinPositioner added successfully");
                    }
                    else
                    {
                        Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: Found existing GyroscopeCoinPositioner component");
                    }
                }
                
                // Disable CompassCoinPlacer so it doesn't fight with GyroscopeCoinPositioner
                var compassPlacer = GetComponent<CompassCoinPlacer>();
                if (compassPlacer != null && compassPlacer.enabled)
                {
                    compassPlacer.enabled = false;
                    Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: CompassCoinPlacer DISABLED (gyro active)");
                }
            }
            
            // Switch BACK to AR tracking if camera starts moving again
            // (means ARCore recovered or was just slow to initialize)
            if (!shouldUseGyro && useGyroPositioning && arTracking && cameraMoved > 0.01f)
            {
                Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: *** SWITCHING BACK TO AR TRACKING ***");
                Debug.Log($"[ARCoinRenderer]   Camera is moving again! cameraMoved={cameraMoved:F4}m, arState={arState}");
                useGyroPositioning = false;
                
                // Disable gyroscope positioner so it doesn't fight AR tracking
                if (gyroPositioner != null)
                {
                    gyroPositioner.enabled = false;
                    Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: GyroscopeCoinPositioner DISABLED (AR tracking active)");
                }
            }
        }
        
        /// <summary>
        /// Calculate where coin should materialize.
        /// Uses gyroscope+compass when AR tracking isn't available.
        /// </summary>
        private void CalculateMaterializedPosition()
        {
            if (cameraTransform == null) return;
            
            if (useGyroPositioning && gyroPositioner != null)
            {
                // Gyroscope positioner handles positioning in Update()
                // Just set initial position in front of camera
                materializedPosition = cameraTransform.position + 
                                       Vector3.forward * materializeDistance +
                                       Vector3.up * materializeHeight;
                Debug.Log("[ARCoinRenderer] Using gyroscope-based positioning");
            }
            else
            {
                // Standard AR: Position in front of camera
                Vector3 forward = cameraTransform.forward;
                forward.y = 0; // Keep horizontal
                forward.Normalize();
                
                materializedPosition = cameraTransform.position + 
                                       forward * materializeDistance +
                                       Vector3.up * materializeHeight;
            }
        }
        
        /// <summary>
        /// Update materialization animation
        /// </summary>
        private void UpdateMaterialization()
        {
            materializeProgress += Time.deltaTime / materializeDuration;
            
            if (materializeProgress >= 1f)
            {
                // Materialization complete - start at distance-appropriate scale
                materializeProgress = 1f;
                isMaterializing = false;
                float distScale = CalculateDistanceScale();
                transform.localScale = baseScale * distScale;
                
                SetMode(CoinDisplayMode.Visible);
                OnMaterialized?.Invoke();
                
                // Start sparkles
                if (sparkleParticles != null)
                {
                    sparkleParticles.Play();
                }
                
                if (debugMode)
                {
                    Debug.Log($"[ARCoinRenderer] Materialization COMPLETE - coin now visible!");
                }
            }
            else
            {
                // Animate scale (ease out) toward distance-appropriate size
                float easeT = 1f - Mathf.Pow(1f - materializeProgress, 3f);
                float targetDistScale = CalculateDistanceScale();
                transform.localScale = baseScale * targetDistScale * easeT;
                
                // Gentle spin during materialization (Y-axis, same as visible spin)
                if (coinVisual != null)
                {
                    spinAngle += 180f * Time.deltaTime;
                    // X=270 shows other face upright; Z=180 flips so Black Bart is right-side up in AR on device
                    coinVisual.localRotation = Quaternion.Euler(270, spinAngle, 180);
                }
            }
        }
        
        #endregion
        
        #region Distance-Based Scale
        
        /// <summary>
        /// Calculate scale multiplier based on GPS distance using 10 discrete steps.
        /// Step 1 (100m) = tiny circle, Step 10 (10m) = full treasure coin.
        /// Each step covers Settings.metersPerStep meters (default 10m).
        /// </summary>
        private float CalculateDistanceScale()
        {
            // Clamp GPS distance to our visible range (10m to materializationDistance)
            float maxDist = Settings.materializationDistance; // 100m default
            float minDist = Settings.metersPerStep;           // 10m default
            float clampedDist = Mathf.Clamp(GPSDistance, minDist, maxDist);
            
            // Calculate which step we're in (1-10):
            // 100m = step 1 (farthest, smallest)
            // 90m  = step 2
            // ...
            // 10m  = step 10 (closest, largest)
            int totalSteps = Mathf.Max(1, Mathf.RoundToInt(maxDist / Settings.metersPerStep));
            int step = Mathf.Clamp(
                totalSteps + 1 - Mathf.CeilToInt(clampedDist / Settings.metersPerStep),
                1, totalSteps
            );
            
            // Lerp between far scale (step 1) and near scale (step 10)
            float t = (step - 1) / (float)(totalSteps - 1);
            float scaleMultiplier = Mathf.Lerp(Settings.scaleAtFar, Settings.scaleAtNear, t);
            
            return scaleMultiplier;
        }
        
        /// <summary>
        /// Get the current distance step (1-10) for debug display.
        /// </summary>
        private int GetCurrentDistanceStep()
        {
            float maxDist = Settings.materializationDistance;
            float clampedDist = Mathf.Clamp(GPSDistance, Settings.metersPerStep, maxDist);
            int totalSteps = Mathf.Max(1, Mathf.RoundToInt(maxDist / Settings.metersPerStep));
            return Mathf.Clamp(
                totalSteps + 1 - Mathf.CeilToInt(clampedDist / Settings.metersPerStep),
                1, totalSteps
            );
        }
        
        #endregion
        
        #region Visible Coin Rendering
        
        /// <summary>
        /// Update visible coin - spin, bob, and transition toward GPS position
        /// </summary>
        private void UpdateVisibleCoin()
        {
            if (coinVisual == null) return;
            
            // ================================================================
            // CONTINUOUS AR STATE CHECK - Enable gyro if AR stops working
            // This fixes the issue where AR briefly tracks then stops
            // ================================================================
            ContinuousARStateCheck();
            
            // Log camera position periodically to verify AR tracking
            if (debugMode && Time.frameCount % 180 == 0)
            {
                Debug.Log($"[ARCoinRenderer] Visible coin at {transform.position}, Camera at {cameraTransform?.position}, AR dist={ARDistance:F1}m, UseGyro={useGyroPositioning}");
            }
            
            // Debug: periodic root/child transform state (every 2.5s) so we can reason from logs
            if (debugMode && Time.realtimeSinceStartup >= nextVisualLogTime)
            {
                nextVisualLogTime = Time.realtimeSinceStartup + 2.5f;
                Vector3 rootEuler = transform.eulerAngles;
                Vector3 rootScale = transform.lossyScale;
                Vector3 childPos = coinVisual.localPosition;
                Vector3 childEuler = coinVisual.localEulerAngles;
                Vector3 childScale = coinVisual.localScale;
                string rotationBy = useGyroPositioning ? "Placer" : "Renderer";
                Debug.Log($"[ARCoinRenderer] VISUAL STATE | rootPos={transform.position} rootEuler=({rootEuler.x:F1},{rootEuler.y:F1},{rootEuler.z:F1}) rootScale=({rootScale.x:F2},{rootScale.y:F2},{rootScale.z:F2}) rotationBy={rotationBy} | childLocalPos={childPos} childLocalEuler=({childEuler.x:F1},{childEuler.y:F1},{childEuler.z:F1}) childLocalScale=({childScale.x:F2},{childScale.y:F2},{childScale.z:F2})");
            }
            
            // ================================================================
            // GYROSCOPE POSITIONING - When AR tracking isn't working
            // The GyroscopeCoinPositioner component handles position updates
            // based on compass bearing. We just do visual effects here.
            // ================================================================
            if (useGyroPositioning && gyroPositioner != null)
            {
                // GyroscopeCoinPositioner.Update() handles position
                // We just do the visual effects below
            }
            
            // ================================================================
            // FACE CAMERA (Billboard) - Makes coin look good from any angle
            // ================================================================
            if (cameraTransform != null && !useGyroPositioning)
            {
                // Only do billboard when NOT using gyro (gyro handles its own rotation)
                Vector3 lookDir = cameraTransform.position - transform.position;
                lookDir.y = 0; // Keep upright
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(-lookDir, Vector3.up);
                }
            }
            
            // ================================================================
            // SPIN ANIMATION (on visual child) - rotate around local Y axis so
            // coin spins like a top/turntable, facing the user as it turns.
            // ================================================================
            spinAngle += spinSpeed * Time.deltaTime;
            // X=270 shows other face upright; Z=180 flips so Black Bart is right-side up in AR on device
            coinVisual.localRotation = Quaternion.Euler(270, spinAngle, 180);
            
            // ================================================================
            // BOB ANIMATION (disabled for jitter testing)
            // ================================================================
            // float bobY = Mathf.Sin((Time.time * bobFrequency * Mathf.PI) + bobOffset) * bobAmplitude;
            // coinVisual.localPosition = new Vector3(0, bobY, 0);
            coinVisual.localPosition = Vector3.zero;
            
            // ================================================================
            // POSITION TRANSITION
            // As player gets closer, coin transitions from materialized position
            // toward its true GPS position for natural approach feel.
            // ================================================================
            if (positioner != null && !positioner.IsPositionLocked)
            {
                // Get GPS-based position from positioner
                // The positioner handles the GPS-to-AR conversion
                // We smoothly blend between our materialized position and GPS position
                
                // When very close (collection range), lock to position
                if (GPSDistance <= Settings.collectionDistance * 1.5f)
                {
                    positioner.LockPosition();
                }
            }
            
            // ================================================================
            // DISTANCE-BASED SCALE (10 steps, every 10m)
            // Step 1 (tiny) at 100m → Step 10 (full size) at 10m
            // In the final 10m: ramp from 1x to 2x so coin is 2x at collection.
            // ================================================================
            float distScale = CalculateDistanceScale();
            float closeRangeMultiplier = 1f;
            if (GPSDistance <= Settings.finalMetersForScaleRamp && Settings.finalMetersForScaleRamp > 0f)
            {
                closeRangeMultiplier = Mathf.Lerp(Settings.scaleAtCollectionMultiplier, 1f, GPSDistance / Settings.finalMetersForScaleRamp);
            }
            
            if (CurrentMode == CoinDisplayMode.Collectible)
            {
                // Pulse effect when collectible (at max near scale, with final-10m 2x)
                float pulse = 1f + 0.1f * Mathf.Sin(Time.time * 4f);
                transform.localScale = baseScale * Settings.scaleAtNear * closeRangeMultiplier * pulse;
            }
            else
            {
                transform.localScale = baseScale * distScale * closeRangeMultiplier;
            }
            
            // Debug: log scale step periodically
            if (debugMode && Time.frameCount % 180 == 0)
            {
                int step = GetCurrentDistanceStep();
                Debug.Log($"[ARCoinRenderer] DISTANCE SCALE: step={step}/10, GPS={GPSDistance:F1}m, " +
                          $"scaleMultiplier={distScale:F3}, finalScale={transform.localScale}");
            }
            
            // ================================================================
            // COLOR BASED ON STATE
            // ================================================================
            UpdateColorForState();
        }
        
        /// <summary>
        /// True if this material is for a full-color coin (e.g. Color BB with skin, silver, etc.).
        /// We use white _Color and no emission so the basecolor texture drives the look.
        /// </summary>
        private static bool IsFullColorCoinMaterial(Material mat)
        {
            if (mat == null) return false;
            string name = mat.name ?? "";
            return name.IndexOf("ColorBB", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
        
        /// <summary>
        /// Update coin color based on current state
        /// </summary>
        private void UpdateColorForState()
        {
            if (meshRenderer == null || meshRenderer.material == null) return;
            
            Material mat = meshRenderer.material;
            bool hasTexture = mat.mainTexture != null;
            
            bool isFullColorCoin = IsFullColorCoinMaterial(mat);
            
            if (CurrentMode == CoinDisplayMode.Collectible)
            {
                mat.color = Settings.inRangeColor;
                Color collectEmission = isFullColorCoin ? Color.black : new Color(0.98f, 0.76f, 0.2f, 1f);
                mat.SetColor("_EmissionColor", collectEmission);
            }
            else if (hasTexture)
            {
                if (isFullColorCoin)
                {
                    mat.SetColor("_Color", Color.white);
                    mat.SetColor("_EmissionColor", Color.black);
                }
                else
                {
                    mat.SetColor("_Color", new Color(1f, 0.82f, 0.28f, 1f));
                    mat.SetColor("_EmissionColor", new Color(0.98f, 0.76f, 0.2f, 1f));
                }
                
                if (Time.realtimeSinceStartup >= nextColorDiagTime)
                {
                    nextColorDiagTime = Time.realtimeSinceStartup + 3f;
                    Color readColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.clear;
                    Color readEmission = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.clear;
                    Debug.Log($"[ARCoinRenderer] 🎨 COLOR DIAG (Visible+texture): fullColor={isFullColorCoin} _Color=({readColor.r:F2},{readColor.g:F2},{readColor.b:F2}) _Emission=({readEmission.r:F2},{readEmission.g:F2},{readEmission.b:F2}) shader={mat.shader?.name ?? "null"}");
                }
            }
            else
            {
                mat.color = isFullColorCoin ? Color.white : Settings.goldColor;
            }
        }
        
        #endregion
        
        #region Visibility Control
        
        private void SetVisibility(bool visible)
        {
            // Enable/disable all renderers
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.enabled = visible;
            }
            
            // Enable/disable colliders
            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders)
            {
                c.enabled = visible;
            }
            
            if (debugMode)
            {
                Debug.Log($"[ARCoinRenderer] Visibility set to {visible}");
            }
        }
        
        #endregion
        
        #region Collection
        
        /// <summary>
        /// Start collection animation
        /// </summary>
        public void StartCollection()
        {
            if (CurrentMode == CoinDisplayMode.Collecting) return;
            
            SetMode(CoinDisplayMode.Collecting);
            StartCoroutine(CollectionAnimation());
        }
        
        private IEnumerator CollectionAnimation()
        {
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: CollectionAnimation STARTED");
            
            float duration = 0.8f;
            float timer = 0f;
            
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            
            // Target: fly toward camera
            Vector3 targetPos = cameraTransform != null
                ? cameraTransform.position + cameraTransform.forward * 0.3f
                : startPos + Vector3.up;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f);
                
                // Move toward camera
                transform.position = Vector3.Lerp(startPos, targetPos, easeT);
                
                // Shrink
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easeT);
                
                // Fast spin (same Y axis as idle spin - like a top speeding up)
                if (coinVisual != null)
                {
                    coinVisual.Rotate(0, 720f * Time.deltaTime, 0, Space.Self);
                }
                
                yield return null;
            }
            
            Debug.Log($"[ARCoinRenderer] T+{Time.realtimeSinceStartup:F2}s: CollectionAnimation FINISHED, resetting mode");
            
            // Collection complete - reset to Hidden mode so we can select new coins
            SetMode(CoinDisplayMode.Hidden);
            SetVisibility(false);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Force coin to materialize (for testing)
        /// </summary>
        public void ForceMaterialize()
        {
            if (CurrentMode == CoinDisplayMode.Hidden)
            {
                StartMaterialization();
            }
        }
        
        /// <summary>
        /// Force hide coin
        /// </summary>
        public void ForceHide()
        {
            SetMode(CoinDisplayMode.Hidden);
        }
        
        /// <summary>
        /// Set custom settings
        /// </summary>
        public void SetSettings(CoinDisplaySettings newSettings)
        {
            settings = newSettings;
        }
        
        /// <summary>
        /// Set coin color (for locked state, etc.)
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
            Debug.Log("=== ARCoinRenderer Status (Pokemon GO Pattern) ===");
            Debug.Log($"Mode: {CurrentMode}");
            Debug.Log($"GPS Distance: {GPSDistance:F1}m");
            Debug.Log($"AR Distance: {ARDistance:F1}m");
            Debug.Log($"Materialization Distance: {Settings.materializationDistance}m");
            Debug.Log($"Collection Distance: {Settings.collectionDistance}m");
            Debug.Log($"Is Visible: {IsVisible}");
            Debug.Log($"Is In Collection Range: {IsInCollectionRange}");
            Debug.Log($"Camera Found: {cameraFound}");
            Debug.Log("=================================================");
        }
        
        [ContextMenu("Debug: Force Materialize")]
        public void DebugForceMaterialize()
        {
            ForceMaterialize();
        }
        
        [ContextMenu("Debug: Force Hide")]
        public void DebugForceHide()
        {
            ForceHide();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            // Draw materialization range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Settings?.materializationDistance ?? 20f);
            
            // Draw collection range
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, Settings?.collectionDistance ?? 5f);
        }
        
        #endregion
    }
}
