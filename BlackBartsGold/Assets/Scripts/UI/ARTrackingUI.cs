// ============================================================================
// ARTrackingUI.cs
// Black Bart's Gold - AR Tracking State UI
// Path: Assets/Scripts/UI/ARTrackingUI.cs
// ============================================================================
// Displays AR tracking status to the user. Shows messages when tracking is
// initializing, lost, or when errors occur.
// Reference: BUILD-GUIDE.md Prompt 2.1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using BlackBartsGold.AR;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Displays AR tracking state to user with appropriate messages and visuals.
    /// Shows "Looking for surfaces..." during initialization, etc.
    /// </summary>
    public class ARTrackingUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Container panel for tracking messages")]
        private GameObject trackingPanel;
        
        [SerializeField]
        [Tooltip("Text component for tracking message")]
        private TMP_Text trackingText;
        
        [SerializeField]
        [Tooltip("Fallback text component (if not using TextMeshPro)")]
        private Text trackingTextLegacy;
        
        [SerializeField]
        [Tooltip("Icon/image for tracking state")]
        private Image trackingIcon;
        
        [SerializeField]
        [Tooltip("Loading spinner/animation")]
        private GameObject loadingSpinner;
        
        [SerializeField]
        [Tooltip("Warning icon")]
        private GameObject warningIcon;
        
        [SerializeField]
        [Tooltip("Error icon")]
        private GameObject errorIcon;
        
        [Header("Colors")]
        [SerializeField]
        private Color normalColor = Color.white;
        
        [SerializeField]
        private Color warningColor = new Color(0.98f, 0.75f, 0.14f); // Yellow
        
        [SerializeField]
        private Color errorColor = new Color(0.94f, 0.27f, 0.27f); // Red
        
        [SerializeField]
        private Color successColor = new Color(0.29f, 0.87f, 0.5f); // Green
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Auto-hide panel when tracking is good")]
        private bool autoHideOnTracking = true;
        
        [SerializeField]
        [Tooltip("Delay before hiding panel after tracking established")]
        private float hideDelay = 2f;
        
        [SerializeField]
        [Tooltip("Show detailed tracking hints")]
        private bool showHints = true;
        
        #endregion
        
        #region Private Fields
        
        private float hideTimer = 0f;
        private bool isHiding = false;
        private ARSessionState lastState = ARSessionState.None;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Initial state
            ShowPanel(true);
            SetMessage("Starting AR...", TrackingUIState.Loading);
            
            // Subscribe to AR session events
            if (ARSessionManager.Instance != null)
            {
                ARSessionManager.Instance.OnStateChanged += OnARStateChanged;
                ARSessionManager.Instance.OnTrackingEstablished += OnTrackingEstablished;
                ARSessionManager.Instance.OnTrackingLost += OnTrackingLost;
                ARSessionManager.Instance.OnError += OnARError;
            }
            else
            {
                // Fallback: subscribe to AR Session directly
                ARSession.stateChanged += OnARSessionStateChangedDirect;
            }
        }
        
        private void OnDestroy()
        {
            if (ARSessionManager.Instance != null)
            {
                ARSessionManager.Instance.OnStateChanged -= OnARStateChanged;
                ARSessionManager.Instance.OnTrackingEstablished -= OnTrackingEstablished;
                ARSessionManager.Instance.OnTrackingLost -= OnTrackingLost;
                ARSessionManager.Instance.OnError -= OnARError;
            }
            
            ARSession.stateChanged -= OnARSessionStateChangedDirect;
        }
        
        private void Update()
        {
            // Handle auto-hide timer
            if (isHiding)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0)
                {
                    ShowPanel(false);
                    isHiding = false;
                }
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnARStateChanged(ARSessionState state)
        {
            UpdateForState(state);
        }
        
        private void OnARSessionStateChangedDirect(ARSessionStateChangedEventArgs args)
        {
            UpdateForState(args.state);
        }
        
        private void OnTrackingEstablished()
        {
            SetMessage("Ready! Search for gold!", TrackingUIState.Success);
            
            if (autoHideOnTracking)
            {
                StartHideTimer();
            }
        }
        
        private void OnTrackingLost()
        {
            CancelHideTimer();
            ShowPanel(true);
            SetMessage("Tracking lost. Look around slowly...", TrackingUIState.Warning);
        }
        
        private void OnARError(string error)
        {
            CancelHideTimer();
            ShowPanel(true);
            SetMessage(error, TrackingUIState.Error);
        }
        
        #endregion
        
        #region State Updates
        
        /// <summary>
        /// Update UI based on AR session state
        /// </summary>
        private void UpdateForState(ARSessionState state)
        {
            if (state == lastState) return;
            lastState = state;
            
            CancelHideTimer();
            
            switch (state)
            {
                case ARSessionState.None:
                case ARSessionState.CheckingAvailability:
                    ShowPanel(true);
                    SetMessage("Checking AR availability...", TrackingUIState.Loading);
                    break;
                    
                case ARSessionState.NeedsInstall:
                    ShowPanel(true);
                    SetMessage("AR software needs to be installed", TrackingUIState.Warning);
                    break;
                    
                case ARSessionState.Installing:
                    ShowPanel(true);
                    SetMessage("Installing AR software...", TrackingUIState.Loading);
                    break;
                    
                case ARSessionState.Unsupported:
                    ShowPanel(true);
                    SetMessage("Sorry matey, yer device doesn't support AR!", TrackingUIState.Error);
                    break;
                    
                case ARSessionState.Ready:
                    ShowPanel(true);
                    SetMessage("AR ready! Starting session...", TrackingUIState.Loading);
                    break;
                    
                case ARSessionState.SessionInitializing:
                    ShowPanel(true);
                    SetInitializingMessage();
                    break;
                    
                case ARSessionState.SessionTracking:
                    SetMessage("Ready! Search for gold!", TrackingUIState.Success);
                    if (autoHideOnTracking)
                    {
                        StartHideTimer();
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Set message for initializing state with hints
        /// </summary>
        private void SetInitializingMessage()
        {
            string message = "Looking for surfaces...";
            
            if (showHints)
            {
                message += "\n\n<size=80%>Tips:\n• Point camera at floor or table\n• Move phone slowly side to side\n• Make sure area is well lit</size>";
            }
            
            SetMessage(message, TrackingUIState.Loading);
        }
        
        #endregion
        
        #region UI Control
        
        /// <summary>
        /// Set the tracking message
        /// </summary>
        public void SetMessage(string message, TrackingUIState state)
        {
            // Update text
            if (trackingText != null)
            {
                trackingText.text = message;
                trackingText.color = GetColorForState(state);
            }
            else if (trackingTextLegacy != null)
            {
                trackingTextLegacy.text = message;
                trackingTextLegacy.color = GetColorForState(state);
            }
            
            // Update icons
            UpdateIcons(state);
        }
        
        /// <summary>
        /// Show or hide the tracking panel
        /// </summary>
        public void ShowPanel(bool show)
        {
            if (trackingPanel != null)
            {
                trackingPanel.SetActive(show);
            }
        }
        
        /// <summary>
        /// Update icon visibility based on state
        /// </summary>
        private void UpdateIcons(TrackingUIState state)
        {
            // Loading spinner
            if (loadingSpinner != null)
            {
                loadingSpinner.SetActive(state == TrackingUIState.Loading);
            }
            
            // Warning icon
            if (warningIcon != null)
            {
                warningIcon.SetActive(state == TrackingUIState.Warning);
            }
            
            // Error icon
            if (errorIcon != null)
            {
                errorIcon.SetActive(state == TrackingUIState.Error);
            }
            
            // Tracking icon color
            if (trackingIcon != null)
            {
                trackingIcon.color = GetColorForState(state);
            }
        }
        
        /// <summary>
        /// Get color for tracking UI state
        /// </summary>
        private Color GetColorForState(TrackingUIState state)
        {
            return state switch
            {
                TrackingUIState.Normal => normalColor,
                TrackingUIState.Loading => normalColor,
                TrackingUIState.Warning => warningColor,
                TrackingUIState.Error => errorColor,
                TrackingUIState.Success => successColor,
                _ => normalColor
            };
        }
        
        #endregion
        
        #region Timer
        
        private void StartHideTimer()
        {
            hideTimer = hideDelay;
            isHiding = true;
        }
        
        private void CancelHideTimer()
        {
            isHiding = false;
            hideTimer = 0;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Force show tracking panel with custom message
        /// </summary>
        public void ShowMessage(string message, TrackingUIState state, float duration = 0)
        {
            ShowPanel(true);
            SetMessage(message, state);
            
            if (duration > 0)
            {
                hideTimer = duration;
                isHiding = true;
            }
        }
        
        /// <summary>
        /// Hide the panel immediately
        /// </summary>
        public void Hide()
        {
            CancelHideTimer();
            ShowPanel(false);
        }
        
        #endregion
    }
    
    #region Enums
    
    /// <summary>
    /// Tracking UI visual state
    /// </summary>
    public enum TrackingUIState
    {
        /// <summary>Normal state</summary>
        Normal,
        
        /// <summary>Loading/initializing</summary>
        Loading,
        
        /// <summary>Warning (tracking lost, etc.)</summary>
        Warning,
        
        /// <summary>Error (unsupported, etc.)</summary>
        Error,
        
        /// <summary>Success (tracking established)</summary>
        Success
    }
    
    #endregion
}
