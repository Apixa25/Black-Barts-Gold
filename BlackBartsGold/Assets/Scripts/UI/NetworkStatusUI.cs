// ============================================================================
// NetworkStatusUI.cs
// Black Bart's Gold - Network Status Indicator
// Path: Assets/Scripts/UI/NetworkStatusUI.cs
// ============================================================================
// UI indicator showing online/offline status and pending sync actions.
// Displays connection type and provides sync controls.
// Reference: BUILD-GUIDE.md Sprint 8, Prompt 8.2
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using BlackBartsGold.Core;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Network status UI controller.
    /// Shows online/offline status and sync queue count.
    /// </summary>
    public class NetworkStatusUI : MonoBehaviour
    {
        #region Singleton
        
        private static NetworkStatusUI _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static NetworkStatusUI Instance => _instance;
        
        /// <summary>
        /// Check if instance exists
        /// </summary>
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Main Panel")]
        [SerializeField]
        private GameObject statusPanel;
        
        [SerializeField]
        private CanvasGroup canvasGroup;
        
        [Header("Status Elements")]
        [SerializeField]
        private Image statusIcon;
        
        [SerializeField]
        private TMP_Text statusText;
        
        [SerializeField]
        private TMP_Text detailText;
        
        [Header("Sync Elements")]
        [SerializeField]
        private GameObject syncPanel;
        
        [SerializeField]
        private TMP_Text syncCountText;
        
        [SerializeField]
        private Button syncButton;
        
        [SerializeField]
        private Image syncSpinner;
        
        [Header("Icons")]
        [SerializeField]
        private Sprite onlineIcon;
        
        [SerializeField]
        private Sprite offlineIcon;
        
        [SerializeField]
        private Sprite wifiIcon;
        
        [SerializeField]
        private Sprite mobileDataIcon;
        
        [Header("Colors")]
        [SerializeField]
        private Color onlineColor = new Color(0.29f, 0.87f, 0.5f);
        
        [SerializeField]
        private Color offlineColor = new Color(0.94f, 0.27f, 0.27f);
        
        [SerializeField]
        private Color syncingColor = new Color(0.98f, 0.75f, 0.14f);
        
        [Header("Animation")]
        [SerializeField]
        private float showDuration = 3f;
        
        [SerializeField]
        private float fadeInDuration = 0.3f;
        
        [SerializeField]
        private float fadeOutDuration = 0.5f;
        
        [Header("Behavior")]
        [SerializeField]
        private bool autoHideWhenOnline = true;
        
        [SerializeField]
        private bool alwaysShowOffline = true;
        
        [SerializeField]
        private bool showOnStatusChange = true;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private bool isVisible = false;
        private bool isAnimating = false;
        private Coroutine hideCoroutine;
        private Coroutine spinnerCoroutine;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when sync is triggered
        /// </summary>
        public event Action OnSyncTriggered;
        
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
        }
        
        private void Start()
        {
            SetupListeners();
            
            // Initial state
            UpdateStatus();
            
            // Subscribe to offline manager events
            if (OfflineManager.Exists)
            {
                OfflineManager.Instance.OnNetworkStatusChanged += HandleNetworkStatusChanged;
                OfflineManager.Instance.OnSyncStarted += HandleSyncStarted;
                OfflineManager.Instance.OnSyncCompleted += HandleSyncCompleted;
                OfflineManager.Instance.OnActionQueued += HandleActionQueued;
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            if (OfflineManager.Exists)
            {
                OfflineManager.Instance.OnNetworkStatusChanged -= HandleNetworkStatusChanged;
                OfflineManager.Instance.OnSyncStarted -= HandleSyncStarted;
                OfflineManager.Instance.OnSyncCompleted -= HandleSyncCompleted;
                OfflineManager.Instance.OnActionQueued -= HandleActionQueued;
            }
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupListeners()
        {
            if (syncButton != null)
            {
                syncButton.onClick.AddListener(OnSyncButtonClicked);
            }
        }
        
        #endregion
        
        #region Status Update
        
        /// <summary>
        /// Update status display
        /// </summary>
        public void UpdateStatus()
        {
            bool isOnline = OfflineManager.Exists ? OfflineManager.Instance.IsOnline : true;
            bool isSyncing = OfflineManager.Exists ? OfflineManager.Instance.IsSyncing : false;
            int queueCount = OfflineManager.Exists ? OfflineManager.Instance.QueuedActionCount : 0;
            
            UpdateStatusDisplay(isOnline, isSyncing);
            UpdateSyncDisplay(queueCount, isSyncing);
            
            // Auto-show/hide logic
            if (alwaysShowOffline && !isOnline)
            {
                Show();
            }
            else if (autoHideWhenOnline && isOnline && queueCount == 0)
            {
                ScheduleHide();
            }
        }
        
        /// <summary>
        /// Update status icon and text
        /// </summary>
        private void UpdateStatusDisplay(bool isOnline, bool isSyncing)
        {
            if (statusIcon != null)
            {
                if (isSyncing)
                {
                    statusIcon.color = syncingColor;
                }
                else if (isOnline)
                {
                    statusIcon.sprite = GetConnectionIcon();
                    statusIcon.color = onlineColor;
                }
                else
                {
                    statusIcon.sprite = offlineIcon;
                    statusIcon.color = offlineColor;
                }
            }
            
            if (statusText != null)
            {
                if (isSyncing)
                {
                    statusText.text = "Syncing...";
                    statusText.color = syncingColor;
                }
                else if (isOnline)
                {
                    statusText.text = "Online";
                    statusText.color = onlineColor;
                }
                else
                {
                    statusText.text = "Offline";
                    statusText.color = offlineColor;
                }
            }
            
            if (detailText != null)
            {
                if (OfflineManager.Exists)
                {
                    detailText.text = OfflineManager.Instance.GetNetworkStatusString();
                }
            }
        }
        
        /// <summary>
        /// Update sync panel display
        /// </summary>
        private void UpdateSyncDisplay(int queueCount, bool isSyncing)
        {
            if (syncPanel != null)
            {
                syncPanel.SetActive(queueCount > 0 || isSyncing);
            }
            
            if (syncCountText != null)
            {
                if (isSyncing)
                {
                    syncCountText.text = "Syncing...";
                }
                else if (queueCount > 0)
                {
                    syncCountText.text = $"{queueCount} pending";
                }
                else
                {
                    syncCountText.text = "Synced";
                }
            }
            
            if (syncButton != null)
            {
                syncButton.interactable = !isSyncing && queueCount > 0 && 
                    (OfflineManager.Exists && OfflineManager.Instance.IsOnline);
            }
            
            // Spinner animation
            if (syncSpinner != null)
            {
                syncSpinner.gameObject.SetActive(isSyncing);
                if (isSyncing && spinnerCoroutine == null)
                {
                    spinnerCoroutine = StartCoroutine(SpinAnimation());
                }
                else if (!isSyncing && spinnerCoroutine != null)
                {
                    StopCoroutine(spinnerCoroutine);
                    spinnerCoroutine = null;
                }
            }
        }
        
        /// <summary>
        /// Get icon based on connection type
        /// </summary>
        private Sprite GetConnectionIcon()
        {
            if (!OfflineManager.Exists) return onlineIcon;
            
            if (OfflineManager.Instance.IsWifi && wifiIcon != null)
            {
                return wifiIcon;
            }
            else if (OfflineManager.Instance.IsMobileData && mobileDataIcon != null)
            {
                return mobileDataIcon;
            }
            
            return onlineIcon;
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the status panel
        /// </summary>
        public void Show()
        {
            if (isAnimating) return;
            
            Log("Showing status panel");
            
            CancelHide();
            isVisible = true;
            
            if (statusPanel != null)
            {
                statusPanel.SetActive(true);
            }
            
            UpdateStatus();
            
            StartCoroutine(FadeIn());
        }
        
        /// <summary>
        /// Hide the status panel
        /// </summary>
        public void Hide()
        {
            if (isAnimating) return;
            
            Log("Hiding status panel");
            
            CancelHide();
            StartCoroutine(FadeOut());
        }
        
        /// <summary>
        /// Schedule auto-hide
        /// </summary>
        private void ScheduleHide()
        {
            CancelHide();
            hideCoroutine = StartCoroutine(AutoHide());
        }
        
        /// <summary>
        /// Cancel scheduled hide
        /// </summary>
        private void CancelHide()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
        }
        
        /// <summary>
        /// Show briefly then hide
        /// </summary>
        public void ShowBriefly()
        {
            Show();
            ScheduleHide();
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnSyncButtonClicked()
        {
            Log("Sync button clicked");
            
            OnSyncTriggered?.Invoke();
            
            if (OfflineManager.Exists)
            {
                _ = OfflineManager.Instance.SyncQueuedActions();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleNetworkStatusChanged(bool isOnline)
        {
            Log($"Network status changed: {(isOnline ? "ONLINE" : "OFFLINE")}");
            
            UpdateStatus();
            
            if (showOnStatusChange)
            {
                ShowBriefly();
            }
        }
        
        private void HandleSyncStarted()
        {
            Log("Sync started");
            UpdateStatus();
            Show();
        }
        
        private void HandleSyncCompleted(int success, int failed)
        {
            Log($"Sync completed: {success} success, {failed} failed");
            UpdateStatus();
            
            // Show result briefly
            if (syncCountText != null)
            {
                if (failed > 0)
                {
                    syncCountText.text = $"✅ {success} synced, ❌ {failed} failed";
                }
                else
                {
                    syncCountText.text = $"✅ {success} synced";
                }
            }
            
            ScheduleHide();
        }
        
        private void HandleActionQueued(QueuedAction action)
        {
            UpdateStatus();
            
            // Show if not visible
            if (!isVisible)
            {
                ShowBriefly();
            }
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// Fade in animation
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            
            isAnimating = true;
            float elapsed = 0f;
            canvasGroup.alpha = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
            isAnimating = false;
        }
        
        /// <summary>
        /// Fade out animation
        /// </summary>
        private IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                isVisible = false;
                if (statusPanel != null) statusPanel.SetActive(false);
                yield break;
            }
            
            isAnimating = true;
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            isVisible = false;
            
            if (statusPanel != null)
            {
                statusPanel.SetActive(false);
            }
            
            isAnimating = false;
        }
        
        /// <summary>
        /// Auto-hide after duration
        /// </summary>
        private IEnumerator AutoHide()
        {
            yield return new WaitForSeconds(showDuration);
            Hide();
        }
        
        /// <summary>
        /// Spinner rotation animation
        /// </summary>
        private IEnumerator SpinAnimation()
        {
            while (true)
            {
                if (syncSpinner != null)
                {
                    syncSpinner.transform.Rotate(0f, 0f, -180f * Time.deltaTime);
                }
                yield return null;
            }
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[NetworkStatusUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Force show
        /// </summary>
        [ContextMenu("Debug: Show Status")]
        public void DebugShow()
        {
            Show();
        }
        
        /// <summary>
        /// Debug: Force hide
        /// </summary>
        [ContextMenu("Debug: Hide Status")]
        public void DebugHide()
        {
            Hide();
        }
        
        /// <summary>
        /// Debug: Simulate offline
        /// </summary>
        [ContextMenu("Debug: Simulate Offline")]
        public void DebugSimulateOffline()
        {
            HandleNetworkStatusChanged(false);
        }
        
        #endregion
    }
}
