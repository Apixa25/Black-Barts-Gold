// ============================================================================
// ARTrackingDebug.cs
// Black Bart's Gold - AR Tracking Diagnostic
// Path: Assets/Scripts/AR/ARTrackingDebug.cs
// ============================================================================
// Logs AR camera position to verify tracking is working.
// If camera position never changes, AR tracking is broken.
//
// IMPORTANT: Uses the MODERN XR Plug-in Management APIs, NOT the legacy
// UnityEngine.XR.XRSettings API. XRSettings.enabled is ALWAYS false with
// AR Foundation + XR Plug-in Management — it's the OLD XR system!
//
// The correct APIs for the modern XR system:
//   - XRGeneralSettings.Instance.Manager.activeLoader  (active XR loader)
//   - SubsystemManager.GetSubsystems<T>()              (running subsystems)
//   - InputDevices.GetDevicesAtXRNode()                 (tracked XR devices)
//   - ARSession.state                                   (AR session state)
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Diagnostic component to verify AR tracking is working.
    /// Uses proper XR Plug-in Management APIs (not legacy XRSettings).
    /// </summary>
    public class ARTrackingDebug : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera arCamera;
        [SerializeField] private ARSession arSession;
        
        [Header("Settings")]
        [SerializeField] private float logInterval = 3f;
        [SerializeField] private float heartbeatInterval = 10f;
        [SerializeField] private float fullDumpInterval = 30f;
        
        private Vector3 lastLoggedPosition;
        private Quaternion lastLoggedRotation;
        private float lastLogTime;
        private float lastHeartbeatTime;
        private float lastFullDumpTime;
        private int frameCount = 0;
        private bool startupDiagnosticsDone = false;
        
        // Track cumulative camera movement to detect if tracking ever worked
        private float totalCameraMovement = 0f;
        private Vector3 previousFramePosition;
        private bool everMoved = false;
        
        private void Start()
        {
            Debug.Log("[ARTrackingDebug] ===================================================");
            Debug.Log("[ARTrackingDebug] AR TRACKING DIAGNOSTIC - STARTUP");
            Debug.Log("[ARTrackingDebug] ===================================================");
            
            // Auto-find camera
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
            
            if (arCamera == null)
            {
                var camManager = FindFirstObjectByType<ARCameraManager>();
                if (camManager != null)
                {
                    arCamera = camManager.GetComponent<Camera>();
                }
            }
            
            // Auto-find AR session
            if (arSession == null)
            {
                arSession = FindFirstObjectByType<ARSession>();
            }
            
            if (arCamera != null)
            {
                lastLoggedPosition = arCamera.transform.position;
                lastLoggedRotation = arCamera.transform.rotation;
                previousFramePosition = lastLoggedPosition;
                Debug.Log($"[ARTrackingDebug] Found camera: {arCamera.name} at {lastLoggedPosition}");
            }
            else
            {
                Debug.LogError("[ARTrackingDebug] NO CAMERA FOUND!");
            }
            
            // Run startup diagnostics
            LogXRSubsystemDiagnostics("STARTUP");
        }
        
        /// <summary>
        /// Log comprehensive XR subsystem diagnostics using the CORRECT modern APIs.
        /// </summary>
        private void LogXRSubsystemDiagnostics(string context)
        {
            Debug.Log($"[ARTrackingDebug] === XR DIAGNOSTICS ({context}) ===");
            
            // ================================================================
            // 1. XR LOADER STATUS (the correct way to check XR initialization)
            // ================================================================
            string loaderName = "NONE";
            bool loaderActive = false;
            
            if (XRGeneralSettings.Instance != null && 
                XRGeneralSettings.Instance.Manager != null)
            {
                var activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
                if (activeLoader != null)
                {
                    loaderName = activeLoader.name;
                    loaderActive = true;
                }
                Debug.Log($"[ARTrackingDebug]   XR Loader: {loaderName} (active={loaderActive})");
                Debug.Log($"[ARTrackingDebug]   XR Manager initialized: {XRGeneralSettings.Instance.Manager.isInitializationComplete}");
            }
            else
            {
                Debug.LogWarning("[ARTrackingDebug]   XR General Settings NOT FOUND! XR Plug-in Management may not be configured.");
            }
            
            // ================================================================
            // 2. AR SESSION STATE
            // ================================================================
            string sessionState = ARSession.state.ToString();
            Debug.Log($"[ARTrackingDebug]   AR Session State: {sessionState}");
            
            // ================================================================
            // 3. XR INPUT DEVICES (this is what TrackedPoseDriver reads from)
            // ================================================================
            var headDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, headDevices);
            Debug.Log($"[ARTrackingDebug]   XR Input Devices at CenterEye: {headDevices.Count}");
            
            foreach (var device in headDevices)
            {
                Debug.Log($"[ARTrackingDebug]     Device: '{device.name}', valid={device.isValid}");
                
                // Try to read position
                if (device.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 pos))
                {
                    Debug.Log($"[ARTrackingDebug]     CenterEyePosition: {pos}");
                }
                else
                {
                    Debug.LogWarning($"[ARTrackingDebug]     CenterEyePosition: NOT AVAILABLE");
                }
                
                // Try to read rotation
                if (device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rot))
                {
                    Debug.Log($"[ARTrackingDebug]     CenterEyeRotation: {rot.eulerAngles}");
                }
                else
                {
                    Debug.LogWarning($"[ARTrackingDebug]     CenterEyeRotation: NOT AVAILABLE");
                }
                
                // Try to read tracking state
                if (device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState trackState))
                {
                    Debug.Log($"[ARTrackingDebug]     TrackingState: {trackState}");
                }
                else
                {
                    Debug.LogWarning($"[ARTrackingDebug]     TrackingState: NOT AVAILABLE");
                }
            }
            
            // Also check Head node (some implementations use this instead)
            var headNodeDevices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.Head, headNodeDevices);
            if (headNodeDevices.Count != headDevices.Count)
            {
                Debug.Log($"[ARTrackingDebug]   XR Input Devices at Head node: {headNodeDevices.Count}");
                foreach (var device in headNodeDevices)
                {
                    Debug.Log($"[ARTrackingDebug]     Head Device: '{device.name}', valid={device.isValid}");
                    
                    if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 dPos))
                    {
                        Debug.Log($"[ARTrackingDebug]     DevicePosition: {dPos}");
                    }
                    if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion dRot))
                    {
                        Debug.Log($"[ARTrackingDebug]     DeviceRotation: {dRot.eulerAngles}");
                    }
                }
            }
            
            // ================================================================
            // 4. ALL XR INPUT DEVICES (see everything that's registered)
            // ================================================================
            var allDevices = new List<InputDevice>();
            InputDevices.GetDevices(allDevices);
            Debug.Log($"[ARTrackingDebug]   Total XR Input Devices registered: {allDevices.Count}");
            foreach (var device in allDevices)
            {
                Debug.Log($"[ARTrackingDebug]     [{device.role}] '{device.name}' valid={device.isValid}");
            }
            
            // ================================================================
            // 5. TRACKED POSE DRIVER STATUS
            // ================================================================
            if (arCamera != null)
            {
                // Check for Input System TrackedPoseDriver
                var tpd = arCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if (tpd != null)
                {
                    Debug.Log($"[ARTrackingDebug]   TrackedPoseDriver (Input System): enabled={tpd.enabled}");
                    Debug.Log($"[ARTrackingDebug]     TrackingType: {tpd.trackingType}");
                    Debug.Log($"[ARTrackingDebug]     UpdateType: {tpd.updateType}");
                }
                else
                {
                    Debug.LogWarning("[ARTrackingDebug]   TrackedPoseDriver: NOT FOUND on camera!");
                }
            }
            
            // ================================================================
            // 6. LEGACY API NOTE (so we know not to trust these)
            // ================================================================
            Debug.Log($"[ARTrackingDebug]   [LEGACY - IGNORE] XRSettings.enabled={XRSettings.enabled} (always false with XR Plug-in Management!)");
            Debug.Log($"[ARTrackingDebug]   [LEGACY - IGNORE] XRSettings.loadedDeviceName='{XRSettings.loadedDeviceName}' (always empty with XR Plug-in Management!)");
            
            Debug.Log($"[ARTrackingDebug] === END XR DIAGNOSTICS ({context}) ===");
        }
        
        private void Update()
        {
            frameCount++;
            
            // Track per-frame camera movement
            if (arCamera != null)
            {
                float frameDist = Vector3.Distance(arCamera.transform.position, previousFramePosition);
                totalCameraMovement += frameDist;
                previousFramePosition = arCamera.transform.position;
                
                if (!everMoved && frameDist > 0.001f)
                {
                    everMoved = true;
                    Debug.Log($"[ARTrackingDebug] *** CAMERA MOVED FOR THE FIRST TIME! *** dist={frameDist:F4}m at frame {frameCount}");
                }
            }
            
            // Run detailed diagnostics once after a few seconds (give subsystems time to init)
            if (!startupDiagnosticsDone && frameCount > 300) // ~5 seconds at 60fps
            {
                startupDiagnosticsDone = true;
                LogXRSubsystemDiagnostics("DELAYED_STARTUP_5s");
                
                if (!everMoved)
                {
                    Debug.LogWarning("[ARTrackingDebug] *** CAMERA HAS NOT MOVED AFTER 5 SECONDS ***");
                    Debug.LogWarning("[ARTrackingDebug] This means TrackedPoseDriver is NOT receiving pose data from ARCore.");
                    Debug.LogWarning("[ARTrackingDebug] Check the XR Input Devices log above - if no devices are registered,");
                    Debug.LogWarning("[ARTrackingDebug] the Lightship/ARCore loader is not creating an XR HMD device.");
                    
                    // Try to read pose directly from XR subsystem as diagnostic
                    TryReadSubsystemPoseDirectly();
                }
            }
            
            if (Time.time - lastLogTime < logInterval) return;
            lastLogTime = Time.time;
            
            if (arCamera == null) return;
            
            Vector3 currentPos = arCamera.transform.position;
            Vector3 currentRot = arCamera.transform.eulerAngles;
            float movedDistance = Vector3.Distance(currentPos, lastLoggedPosition);
            
            // Get AR session state
            string sessionState = ARSession.state.ToString();
            
            // Get loader status (compact form for periodic logging)
            string loaderInfo = "NoLoader";
            if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
            {
                loaderInfo = XRGeneralSettings.Instance.Manager.activeLoader.name;
            }
            
            // Check for XR input devices (compact)
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
            int deviceCount = devices.Count;
            
            // Try to read pose from XR device
            string xrPoseInfo = "NoDev";
            foreach (var device in devices)
            {
                if (device.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 xrPos))
                {
                    xrPoseInfo = $"XRPos={xrPos}";
                }
                else
                {
                    xrPoseInfo = $"Dev='{device.name}'/NoPose";
                }
            }
            
            Debug.Log($"[ARTrackingDebug] Camera: pos={currentPos}, rot=({currentRot.x:F0},{currentRot.y:F0},{currentRot.z:F0}), moved={movedDistance:F4}m, ARState={sessionState}, Loader={loaderInfo}, XRDevices={deviceCount}, {xrPoseInfo}");
            
            // Warn if camera is stationary after initial period
            if (movedDistance < 0.001f && frameCount > 300 && !everMoved)
            {
                Debug.LogWarning($"[ARTrackingDebug] CAMERA STATIONARY for {frameCount} frames! Total movement: {totalCameraMovement:F4}m");
                Debug.LogWarning($"[ARTrackingDebug] The TrackedPoseDriver is NOT getting data. XR Input Devices: {deviceCount}");
                
                if (deviceCount == 0)
                {
                    Debug.LogError("[ARTrackingDebug] NO XR INPUT DEVICES REGISTERED! " +
                                   "The XR loader ({loaderInfo}) is not creating tracked devices. " +
                                   "This is the root cause - TrackedPoseDriver has nothing to read from.");
                }
            }
            
            lastLoggedPosition = currentPos;
            lastLoggedRotation = arCamera.transform.rotation;
            
            // ================================================================
            // HEARTBEAT - Compact 1-line summary every 10 seconds
            // Easy to grep in ADB: "[HEARTBEAT]"
            // ================================================================
            if (Time.time - lastHeartbeatTime >= heartbeatInterval)
            {
                lastHeartbeatTime = Time.time;
                
                string loader = XRGeneralSettings.Instance?.Manager?.activeLoader?.name ?? "NoLoader";
                string arSt = ARSession.state.ToString();
                int xrDevCount = deviceCount;
                bool camMoving = everMoved;
                float totalMoved = totalCameraMovement;
                
                // Check if gyro fallback is active on any coin
                string gyroStatus = "unknown";
                var coinRenderer = FindFirstObjectByType<ARCoinRenderer>();
                if (coinRenderer != null)
                {
                    var gyro = coinRenderer.GetComponent<GyroscopeCoinPositioner>();
                    gyroStatus = gyro != null && gyro.enabled ? "ACTIVE" : "off";
                }
                
                Debug.Log($"[HEARTBEAT] T+{Time.realtimeSinceStartup:F0}s | AR={arSt} | Loader={loader} | XRDevices={xrDevCount} | CamMoved={camMoving} | TotalMovement={totalMoved:F3}m | CamPos={currentPos} | Gyro={gyroStatus}");
            }
            
            // ================================================================
            // FULL DUMP - Complete subsystem state every 30 seconds
            // ================================================================
            if (Time.time - lastFullDumpTime >= fullDumpInterval)
            {
                lastFullDumpTime = Time.time;
                LogXRSubsystemDiagnostics($"PERIODIC_{Time.realtimeSinceStartup:F0}s");
            }
        }
        
        /// <summary>
        /// Try to read pose data directly from XR subsystems.
        /// This bypasses TrackedPoseDriver and the Input System to check if
        /// the underlying XR subsystem is actually providing tracking data.
        /// </summary>
        private void TryReadSubsystemPoseDirectly()
        {
            Debug.Log("[ARTrackingDebug] === DIRECT SUBSYSTEM POSE CHECK ===");
            
            // Try XR Input Subsystem
            var inputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(inputSubsystems);
            Debug.Log($"[ARTrackingDebug]   XRInputSubsystems: {inputSubsystems.Count}");
            
            foreach (var sub in inputSubsystems)
            {
                Debug.Log($"[ARTrackingDebug]   InputSubsystem: running={sub.running}, trackingOriginMode={sub.GetTrackingOriginMode()}");
                
                // Try to get tracked devices through the subsystem
                var trackingDevices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, trackingDevices);
                Debug.Log($"[ARTrackingDebug]   HeadMounted devices from subsystem: {trackingDevices.Count}");
                
                foreach (var dev in trackingDevices)
                {
                    Debug.Log($"[ARTrackingDebug]     HMD: '{dev.name}' valid={dev.isValid}");
                    if (dev.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p))
                    {
                        Debug.Log($"[ARTrackingDebug]     Position: {p}");
                    }
                }
            }
            
            // Check AR Camera subsystem
            var cameraSubsystems = new List<UnityEngine.XR.ARSubsystems.XRCameraSubsystem>();
            SubsystemManager.GetSubsystems(cameraSubsystems);
            Debug.Log($"[ARTrackingDebug]   XRCameraSubsystems: {cameraSubsystems.Count}");
            foreach (var cam in cameraSubsystems)
            {
                Debug.Log($"[ARTrackingDebug]   CameraSubsystem: running={cam.running}");
            }
            
            // Check AR Session subsystem
            var sessionSubsystems = new List<UnityEngine.XR.ARSubsystems.XRSessionSubsystem>();
            SubsystemManager.GetSubsystems(sessionSubsystems);
            Debug.Log($"[ARTrackingDebug]   XRSessionSubsystems: {sessionSubsystems.Count}");
            foreach (var sess in sessionSubsystems)
            {
                Debug.Log($"[ARTrackingDebug]   SessionSubsystem: running={sess.running}, trackingState={sess.trackingState}");
            }
            
            // Check AR Plane subsystem (useful to know if plane detection works)
            var planeSubsystems = new List<UnityEngine.XR.ARSubsystems.XRPlaneSubsystem>();
            SubsystemManager.GetSubsystems(planeSubsystems);
            Debug.Log($"[ARTrackingDebug]   XRPlaneSubsystems: {planeSubsystems.Count}");
            
            Debug.Log("[ARTrackingDebug] === END DIRECT SUBSYSTEM POSE CHECK ===");
        }
    }
}
