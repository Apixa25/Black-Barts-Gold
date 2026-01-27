// ============================================================================
// ARSessionManager.cs
// Black Bart's Gold - AR Session Management
// Path: Assets/Scripts/AR/ARSessionManager.cs
// ============================================================================
// Manages the AR session lifecycle, tracking state, and provides events for
// other systems to respond to AR state changes.
// Reference: BUILD-GUIDE.md Prompt 2.1
// ============================================================================

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Manages AR session state and provides tracking information.
    /// Should be placed on the same GameObject as ARSession or reference it.
    /// </summary>
    public class ARSessionManager : MonoBehaviour
    {
        #region Singleton
        
        private static ARSessionManager _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ARSessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ARSessionManager>();
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to the ARSession component")]
        private ARSession arSession;
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Automatically pause AR when app loses focus")]
        private bool autoPauseOnFocusLost = true;
        
        [SerializeField]
        [Tooltip("Show debug logs")]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current AR session state
        /// </summary>
        public ARSessionState CurrentState { get; private set; } = ARSessionState.None;
        
        /// <summary>
        /// Previous AR session state (for detecting changes)
        /// </summary>
        public ARSessionState PreviousState { get; private set; } = ARSessionState.None;
        
        /// <summary>
        /// Is AR currently tracking?
        /// </summary>
        public bool IsTracking => CurrentState == ARSessionState.SessionTracking;
        
        /// <summary>
        /// Is AR initialized and ready (tracking or limited)?
        /// </summary>
        public bool IsReady => CurrentState == ARSessionState.SessionTracking || 
                               CurrentState == ARSessionState.SessionInitializing;
        
        /// <summary>
        /// Is the AR session paused?
        /// </summary>
        public bool IsPaused { get; private set; } = false;
        
        /// <summary>
        /// Tracking quality description
        /// </summary>
        public string TrackingQualityDescription => GetTrackingQualityDescription();
        
        /// <summary>
        /// Time since tracking was established (seconds)
        /// </summary>
        public float TimeSinceTracking { get; private set; } = 0f;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when AR session state changes
        /// </summary>
        public event Action<ARSessionState> OnStateChanged;
        
        /// <summary>
        /// Fired when tracking is established
        /// </summary>
        public event Action OnTrackingEstablished;
        
        /// <summary>
        /// Fired when tracking is lost
        /// </summary>
        public event Action OnTrackingLost;
        
        /// <summary>
        /// Fired when AR session encounters an error
        /// </summary>
        public event Action<string> OnError;
        
        /// <summary>
        /// Fired when AR session is paused
        /// </summary>
        public event Action OnSessionPaused;
        
        /// <summary>
        /// Fired when AR session is resumed
        /// </summary>
        public event Action OnSessionResumed;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // Find ARSession if not assigned
            if (arSession == null)
            {
                arSession = FindFirstObjectByType<ARSession>();
            }
            
            if (arSession == null)
            {
                LogError("❌ ARSession not found! AR features will not work.");
            }
        }
        
        private void OnEnable()
        {
            // Subscribe to AR session state changes
            ARSession.stateChanged += OnARSessionStateChanged;
        }
        
        private void OnDisable()
        {
            // Unsubscribe
            ARSession.stateChanged -= OnARSessionStateChanged;
        }
        
        private void Update()
        {
            // Track time since tracking established
            if (IsTracking)
            {
                TimeSinceTracking += Time.deltaTime;
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!autoPauseOnFocusLost) return;
            
            if (!hasFocus)
            {
                // Lost focus - pause AR
                PauseSession();
            }
            else
            {
                // Gained focus - resume AR
                ResumeSession();
            }
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Called when AR session state changes
        /// </summary>
        private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            PreviousState = CurrentState;
            CurrentState = args.state;
            
            Log($"🔄 AR State: {PreviousState} → {CurrentState}");
            
            // Check for tracking established/lost
            if (CurrentState == ARSessionState.SessionTracking && 
                PreviousState != ARSessionState.SessionTracking)
            {
                TimeSinceTracking = 0f;
                Log("✅ AR Tracking established!");
                
                // ================================================================
                // CAPTURE INITIAL COMPASS HEADING FOR WORLD-SPACE ANCHORING
                // ================================================================
                // This is the optimal time to capture compass heading:
                // - AR is now tracking (camera position/rotation is known)
                // - User is holding device steady (just started AR)
                // - All coins will be positioned relative to this initial facing
                // ================================================================
                CaptureInitialCompassForWorldAnchoring();
                
                OnTrackingEstablished?.Invoke();
            }
            else if (CurrentState != ARSessionState.SessionTracking && 
                     PreviousState == ARSessionState.SessionTracking)
            {
                Log("⚠️ AR Tracking lost!");
                OnTrackingLost?.Invoke();
            }
            
            // Check for errors
            if (CurrentState == ARSessionState.Unsupported)
            {
                LogError("❌ AR is not supported on this device!");
                OnError?.Invoke("AR is not supported on this device");
            }
            else if (CurrentState == ARSessionState.NeedsInstall)
            {
                Log("📦 AR software needs to be installed");
                OnError?.Invoke("AR software needs to be installed");
            }
            
            // Notify listeners
            OnStateChanged?.Invoke(CurrentState);
        }
        
        #endregion
        
        #region Session Control
        
        /// <summary>
        /// Pause the AR session
        /// </summary>
        public void PauseSession()
        {
            if (IsPaused) return;
            
            if (arSession != null)
            {
                arSession.enabled = false;
                IsPaused = true;
                Log("⏸️ AR Session paused");
                OnSessionPaused?.Invoke();
            }
        }
        
        /// <summary>
        /// Resume the AR session
        /// </summary>
        public void ResumeSession()
        {
            if (!IsPaused) return;
            
            if (arSession != null)
            {
                arSession.enabled = true;
                IsPaused = false;
                Log("▶️ AR Session resumed");
                OnSessionResumed?.Invoke();
            }
        }
        
        /// <summary>
        /// Reset the AR session (clears tracking data)
        /// </summary>
        public void ResetSession()
        {
            Log("🔄 Resetting AR Session...");
            
            if (arSession != null)
            {
                // Reset by disabling and re-enabling
                arSession.Reset();
                TimeSinceTracking = 0f;
            }
        }
        
        /// <summary>
        /// Check if AR is available on this device
        /// </summary>
        public static bool CheckARAvailability()
        {
            var state = ARSession.state;
            return state != ARSessionState.Unsupported && 
                   state != ARSessionState.NeedsInstall;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get human-readable description of current tracking quality
        /// </summary>
        private string GetTrackingQualityDescription()
        {
            return CurrentState switch
            {
                ARSessionState.None => "AR not initialized",
                ARSessionState.Unsupported => "AR not supported",
                ARSessionState.CheckingAvailability => "Checking AR availability...",
                ARSessionState.NeedsInstall => "AR software required",
                ARSessionState.Installing => "Installing AR...",
                ARSessionState.Ready => "AR ready",
                ARSessionState.SessionInitializing => "Looking for surfaces...",
                ARSessionState.SessionTracking => "Tracking!",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Get user-friendly message for current state
        /// </summary>
        public string GetUserMessage()
        {
            return CurrentState switch
            {
                ARSessionState.None => "Starting AR...",
                ARSessionState.Unsupported => "Sorry matey, yer device doesn't support AR!",
                ARSessionState.CheckingAvailability => "Checking the seas...",
                ARSessionState.NeedsInstall => "Ye need to install AR software!",
                ARSessionState.Installing => "Installing AR software...",
                ARSessionState.Ready => "Ready to hunt!",
                ARSessionState.SessionInitializing => "🔍 Look around slowly to find the treasure...",
                ARSessionState.SessionTracking => "🏴‍☠️ Ready! Search for gold!",
                _ => "Something went wrong..."
            };
        }
        
        /// <summary>
        /// Get color for UI based on state
        /// </summary>
        public Color GetStateColor()
        {
            return CurrentState switch
            {
                ARSessionState.SessionTracking => Color.green,
                ARSessionState.SessionInitializing => Color.yellow,
                ARSessionState.Unsupported => Color.red,
                ARSessionState.NeedsInstall => Color.red,
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Should we show tracking hints to user?
        /// </summary>
        public bool ShouldShowTrackingHints()
        {
            return CurrentState == ARSessionState.SessionInitializing && 
                   TimeSinceTracking < 5f;
        }
        
        #endregion
        
        #region Compass & World Anchoring
        
        /// <summary>
        /// Capture initial compass heading for world-space AR anchoring.
        /// Called when AR tracking is first established.
        /// This heading is used to convert GPS bearings to AR world positions.
        /// </summary>
        private void CaptureInitialCompassForWorldAnchoring()
        {
            // Enable compass if not already
            Input.compass.enabled = true;
            
            // Wait a moment for compass to stabilize, then capture
            StartCoroutine(CaptureCompassCoroutine());
        }
        
        private System.Collections.IEnumerator CaptureCompassCoroutine()
        {
            // Wait for compass to stabilize
            yield return new WaitForSeconds(0.3f);
            
            // Try to capture compass heading
            for (int i = 0; i < 5; i++)
            {
                if (Input.compass.enabled && Input.compass.headingAccuracy >= 0)
                {
                    ARCoinPositioner.CaptureInitialCompassHeading();
                    Log($"🧭 Compass captured for world anchoring: {Input.compass.trueHeading:F0}°");
                    yield break;
                }
                yield return new WaitForSeconds(0.2f);
            }
            
            // Fallback: capture anyway (will use 0° if compass unavailable)
            ARCoinPositioner.CaptureInitialCompassHeading();
            Log("🧭 Compass captured (fallback)");
        }
        
        /// <summary>
        /// Manually recapture compass heading.
        /// Use this if user wants to reset their AR reference direction.
        /// </summary>
        public void RecaptureCompassHeading()
        {
            ARCoinPositioner.ResetCompassHeading();
            CaptureInitialCompassForWorldAnchoring();
            Log("🧭 Compass heading recaptured");
        }
        
        #endregion
        
        #region Logging
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[ARSessionManager] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[ARSessionManager] {message}");
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Print current AR state info
        /// </summary>
        [ContextMenu("Debug: Print AR State")]
        public void DebugPrintState()
        {
            Debug.Log("=== AR Session State ===");
            Debug.Log($"State: {CurrentState}");
            Debug.Log($"Is Tracking: {IsTracking}");
            Debug.Log($"Is Ready: {IsReady}");
            Debug.Log($"Is Paused: {IsPaused}");
            Debug.Log($"Time Since Tracking: {TimeSinceTracking:F1}s");
            Debug.Log($"Message: {GetUserMessage()}");
            Debug.Log("========================");
        }
        
        #endregion
    }
}
