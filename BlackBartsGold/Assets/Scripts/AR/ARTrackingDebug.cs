// ============================================================================
// ARTrackingDebug.cs
// Black Bart's Gold - AR Tracking Diagnostic
// Path: Assets/Scripts/AR/ARTrackingDebug.cs
// ============================================================================
// Logs AR camera position to verify tracking is working.
// If camera position never changes, AR tracking is broken.
// ============================================================================

using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Diagnostic component to verify AR tracking is working.
    /// </summary>
    public class ARTrackingDebug : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera arCamera;
        [SerializeField] private ARSession arSession;
        
        [Header("Settings")]
        [SerializeField] private float logInterval = 3f;
        
        private Vector3 lastLoggedPosition;
        private float lastLogTime;
        private int frameCount = 0;
        
        private void Start()
        {
            Debug.Log("[ARTrackingDebug] Started - will log camera position every 3 seconds");
            
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
                Debug.Log($"[ARTrackingDebug] Found camera: {arCamera.name} at {lastLoggedPosition}");
            }
            else
            {
                Debug.LogError("[ARTrackingDebug] NO CAMERA FOUND!");
            }
        }
        
        private void Update()
        {
            frameCount++;
            
            if (Time.time - lastLogTime < logInterval) return;
            lastLogTime = Time.time;
            
            if (arCamera == null) return;
            
            Vector3 currentPos = arCamera.transform.position;
            Vector3 currentRot = arCamera.transform.eulerAngles;
            float movedDistance = Vector3.Distance(currentPos, lastLoggedPosition);
            
            // Get AR session state
            string sessionState = "Unknown";
            if (arSession != null)
            {
                sessionState = ARSession.state.ToString();
            }
            
            Debug.Log($"[ARTrackingDebug] Camera: pos={currentPos}, rot={currentRot:F0}, moved={movedDistance:F2}m, ARState={sessionState}");
            
            if (movedDistance < 0.001f && frameCount > 180)
            {
                Debug.LogWarning("[ARTrackingDebug] Camera position NOT CHANGING - AR tracking may not be working!");
            }
            
            lastLoggedPosition = currentPos;
        }
    }
}
