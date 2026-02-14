// ============================================================================
// LightshipManager.cs
// Black Bart's Gold - AR Enhancement Manager (Pokemon GO Style!)
// Path: Assets/Scripts/AR/LightshipManager.cs
// Created: 2026-01-27 - AR Foundation features with Lightship-ready hooks
// ============================================================================
// This manager enables AR features using AR Foundation.
// When Lightship components are added in the scene, they automatically enhance
// these features. This script is compile-safe with or without Lightship.
//
// Features:
// 1. OCCLUSION - Coins hide behind real objects
// 2. MESHING - Coins can sit ON real surfaces with physics
// 3. DEPTH - Better AR object placement
// ============================================================================

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Manages AR features for enhanced AR experience.
    /// Works with AR Foundation and is enhanced by Lightship when available.
    /// </summary>
    public class LightshipManager : MonoBehaviour
    {
        #region Singleton
        
        private static LightshipManager _instance;
        public static LightshipManager Instance => _instance;
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Feature Toggles")]
        [Tooltip("Enable occlusion - coins hide behind real objects")]
        [SerializeField] private bool enableOcclusion = true;
        
        [Tooltip("Enable meshing - coins can sit on surfaces")]
        [SerializeField] private bool enableMeshing = true;
        
        [Tooltip("Enable depth effects")]
        [SerializeField] private bool enableDepth = true;
        
        [Header("Mesh Prefab")]
        [Tooltip("Prefab for mesh chunks - needs MeshFilter, MeshRenderer, MeshCollider")]
        [SerializeField] private GameObject meshChunkPrefab;
        
        [Header("References")]
        [SerializeField] private Camera arCamera;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = true;
        [SerializeField] private bool showMeshVisualization = false;
        
        #endregion
        
        #region Properties
        
        public bool IsOcclusionEnabled => enableOcclusion;
        public bool IsMeshingEnabled => enableMeshing;
        public bool IsDepthEnabled => enableDepth;
        
        /// <summary>
        /// Is the environment mesh ready for physics?
        /// </summary>
        public bool IsMeshReady { get; private set; }
        
        #endregion
        
        #region Private Fields
        
        private AROcclusionManager occlusionManager;
        private ARMeshManager meshManager;
        private bool initialized = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Log("========================================");
            Log("LightshipManager AWAKE");
            Log("Enabling Pokemon GO-style AR features!");
            Log("========================================");
        }
        
        private void Start()
        {
            Debug.Log($"[ARManager] T+{Time.realtimeSinceStartup:F2}s: Start() - Initializing AR features...");
            
            // Log XR initialization state before we do anything
            LogXRSubsystemState("BEFORE_INIT");
            
            Initialize();
            
            // Log XR state again after initialization
            LogXRSubsystemState("AFTER_INIT");
        }
        
        private void Initialize()
        {
            // Find AR Camera
            if (arCamera == null)
            {
                arCamera = Camera.main;
                if (arCamera == null)
                {
                    arCamera = FindFirstObjectByType<Camera>();
                }
            }
            
            if (arCamera == null)
            {
                LogError("No AR Camera found!");
                return;
            }
            
            Debug.Log($"[ARManager] AR Camera: {arCamera.name}, pos={arCamera.transform.position}, parent={(arCamera.transform.parent != null ? arCamera.transform.parent.name : "none")}");
            
            // Log camera components for debugging
            var components = arCamera.GetComponents<Component>();
            Debug.Log($"[ARManager] Camera components ({components.Length}):");
            foreach (var comp in components)
            {
                if (comp != null)
                    Debug.Log($"[ARManager]   - {comp.GetType().Name} (enabled={(comp is Behaviour b ? b.enabled.ToString() : "n/a")})");
            }
            
            // Setup features
            if (enableOcclusion) SetupOcclusion();
            if (enableMeshing) SetupMeshing();
            
            initialized = true;
            Log("LightshipManager initialized successfully!");
            LogFeatureStatus();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region Feature Setup
        
        /// <summary>
        /// Setup occlusion - makes coins hide behind real objects.
        /// </summary>
        private void SetupOcclusion()
        {
            Log("Setting up Occlusion...");
            
            // Get or add AROcclusionManager (AR Foundation standard)
            occlusionManager = arCamera.GetComponent<AROcclusionManager>();
            if (occlusionManager == null)
            {
                occlusionManager = arCamera.gameObject.AddComponent<AROcclusionManager>();
                Log("Added AROcclusionManager to camera");
            }
            
            // Configure occlusion manager
            occlusionManager.requestedEnvironmentDepthMode = UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Best;
            occlusionManager.requestedOcclusionPreferenceMode = UnityEngine.XR.ARSubsystems.OcclusionPreferenceMode.PreferEnvironmentOcclusion;
            
            Log("Occlusion setup complete!");
        }
        
        /// <summary>
        /// Setup meshing - creates a 3D mesh of the real world.
        /// ARMeshManager must be on XROrigin or a direct child.
        /// </summary>
        private void SetupMeshing()
        {
            Log("Setting up Meshing...");
            
            // Find XR Origin
            var xrOrigin = FindFirstObjectByType<XROrigin>();
            if (xrOrigin == null)
            {
                LogError("XR Origin not found! Meshing requires XR Origin.");
                return;
            }
            
            try
            {
                // ARMeshManager must be on a CHILD of XROrigin, not on XROrigin itself.
                var meshHolder = xrOrigin.transform.Find("AR Mesh Manager");
                if (meshHolder == null)
                {
                    var go = new GameObject("AR Mesh Manager");
                    go.transform.SetParent(xrOrigin.transform, false);
                    meshHolder = go.transform;
                }
                
                meshManager = meshHolder.GetComponent<ARMeshManager>();
                if (meshManager == null)
                {
                    meshManager = meshHolder.gameObject.AddComponent<ARMeshManager>();
                    Log("Added ARMeshManager to XR Origin child");
                }
            }
            catch (System.InvalidOperationException ex)
            {
                LogError($"ARMeshManager setup failed: {ex.Message}. Meshing disabled.");
                enableMeshing = false;
                return;
            }
            
            // Create mesh prefab if not assigned
            if (meshChunkPrefab == null)
            {
                meshChunkPrefab = CreateDefaultMeshPrefab();
            }
            
            // Assign mesh prefab
            var meshFilter = meshChunkPrefab.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshManager.meshPrefab = meshFilter;
            }
            
            // Subscribe to mesh events
            meshManager.meshesChanged += OnMeshesChanged;
            
            Log("Meshing setup complete!");
        }
        
        #endregion
        
        #region Mesh Prefab Creation
        
        /// <summary>
        /// Creates a default mesh prefab for meshing.
        /// </summary>
        private GameObject CreateDefaultMeshPrefab()
        {
            Log("Creating default mesh prefab...");
            
            GameObject prefab = new GameObject("MeshChunk");
            
            // Add MeshFilter - holds geometry
            prefab.AddComponent<MeshFilter>();
            
            // Add MeshRenderer - displays the mesh
            var renderer = prefab.AddComponent<MeshRenderer>();
            
            // Create invisible material - mesh is only for physics
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader != null)
            {
                Material meshMaterial = new Material(shader);
                meshMaterial.color = showMeshVisualization 
                    ? new Color(0, 1, 0, 0.3f)  // Semi-transparent green for debug
                    : new Color(0, 0, 0, 0);    // Invisible
                renderer.material = meshMaterial;
            }
            
            // Add MeshCollider - enables physics interactions
            prefab.AddComponent<MeshCollider>();
            
            // Don't show in hierarchy
            prefab.hideFlags = HideFlags.HideAndDontSave;
            
            Log("Default mesh prefab created");
            return prefab;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnMeshesChanged(ARMeshesChangedEventArgs args)
        {
            if (args.added.Count > 0 || args.updated.Count > 0)
            {
                IsMeshReady = true;
            }
            
            if (debugMode)
            {
                Log($"Meshes: +{args.added.Count}, ~{args.updated.Count}, -{args.removed.Count}");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Check if a world position is on a detected surface.
        /// </summary>
        public bool IsPositionOnSurface(Vector3 worldPosition, out Vector3 surfacePosition)
        {
            surfacePosition = worldPosition;
            
            if (!IsMeshReady) return false;
            
            // Raycast down to find surface
            Ray ray = new Ray(worldPosition + Vector3.up * 2f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                if (hit.collider is MeshCollider)
                {
                    surfacePosition = hit.point;
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Get the depth at a screen point.
        /// </summary>
        public float GetDepthAtScreenPoint(Vector2 screenPoint)
        {
            if (arCamera == null) return float.MaxValue;
            
            Ray ray = arCamera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                return hit.distance;
            }
            return float.MaxValue;
        }
        
        /// <summary>
        /// Toggle mesh visualization for debugging.
        /// </summary>
        public void SetMeshVisualization(bool show)
        {
            showMeshVisualization = show;
            Log($"Mesh visualization: {show}");
        }
        
        #endregion
        
        #region XR Subsystem Diagnostics
        
        /// <summary>
        /// Log detailed XR subsystem state - helps diagnose Lightship/ARCore initialization issues
        /// </summary>
        private void LogXRSubsystemState(string context)
        {
            Debug.Log($"[ARManager] === XR SUBSYSTEM STATE ({context}) T+{Time.realtimeSinceStartup:F2}s ===");
            
            // Active XR Loader
            string loaderName = "NONE";
            string loaderType = "N/A";
            if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
            {
                var loader = XRGeneralSettings.Instance.Manager.activeLoader;
                loaderName = loader.name;
                loaderType = loader.GetType().FullName;
            }
            Debug.Log($"[ARManager]   Active XR Loader: {loaderName} ({loaderType})");
            
            // AR Session state
            Debug.Log($"[ARManager]   ARSession.state: {ARSession.state}");
            
            // Running subsystems - enumerate ALL of them
            var sessionSubs = new List<UnityEngine.XR.ARSubsystems.XRSessionSubsystem>();
            SubsystemManager.GetSubsystems(sessionSubs);
            Debug.Log($"[ARManager]   XRSessionSubsystems: {sessionSubs.Count}");
            foreach (var s in sessionSubs)
                Debug.Log($"[ARManager]     Session: running={s.running}, trackingState={s.trackingState}");
            
            var cameraSubs = new List<UnityEngine.XR.ARSubsystems.XRCameraSubsystem>();
            SubsystemManager.GetSubsystems(cameraSubs);
            Debug.Log($"[ARManager]   XRCameraSubsystems: {cameraSubs.Count}");
            foreach (var s in cameraSubs)
                Debug.Log($"[ARManager]     Camera: running={s.running}");
            
            var inputSubs = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(inputSubs);
            Debug.Log($"[ARManager]   XRInputSubsystems: {inputSubs.Count}");
            foreach (var s in inputSubs)
                Debug.Log($"[ARManager]     Input: running={s.running}, trackingOriginMode={s.GetTrackingOriginMode()}");
            
            var planeSubs = new List<UnityEngine.XR.ARSubsystems.XRPlaneSubsystem>();
            SubsystemManager.GetSubsystems(planeSubs);
            Debug.Log($"[ARManager]   XRPlaneSubsystems: {planeSubs.Count}");
            
            var depthSubs = new List<UnityEngine.XR.ARSubsystems.XRPointCloudSubsystem>();
            SubsystemManager.GetSubsystems(depthSubs);
            Debug.Log($"[ARManager]   XRDepthSubsystems: {depthSubs.Count}");
            
            // XR Input Devices (what TrackedPoseDriver needs)
            var allDevices = new List<InputDevice>();
            InputDevices.GetDevices(allDevices);
            Debug.Log($"[ARManager]   XR Input Devices: {allDevices.Count}");
            foreach (var dev in allDevices)
            {
                Debug.Log($"[ARManager]     [{dev.role}] '{dev.name}' valid={dev.isValid}");
            }
            
            if (allDevices.Count == 0)
            {
                Debug.LogWarning($"[ARManager]   ⚠️ NO XR INPUT DEVICES! TrackedPoseDriver will NOT receive pose data!");
                Debug.LogWarning($"[ARManager]   This means the camera position will NOT update from ARCore tracking.");
            }
            
            Debug.Log($"[ARManager] === END XR SUBSYSTEM STATE ({context}) ===");
        }
        
        #endregion
        
        #region Debug
        
        private void LogFeatureStatus()
        {
            Log("=== AR Feature Status ===");
            Log($"Occlusion: {(enableOcclusion ? "ENABLED" : "disabled")}");
            Log($"Meshing: {(enableMeshing ? "ENABLED" : "disabled")}");
            Log($"Mesh Ready: {IsMeshReady}");
            Log("=========================");
        }
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[ARManager] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[ARManager] {message}");
        }
        
        [ContextMenu("Debug: Log Status")]
        public void DebugLogStatus()
        {
            LogFeatureStatus();
        }
        
        #endregion
    }
}
