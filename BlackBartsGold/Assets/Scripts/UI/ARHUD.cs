// ============================================================================
// ARHUD.cs
// Black Bart's Gold - AR Heads-Up Display Controller
// Path: Assets/Scripts/UI/ARHUD.cs
// ============================================================================
// Main controller for the AR HUD overlay. Manages all HUD elements including
// compass, gas meter, find limit, crosshairs, and mini-map.
// Reference: BUILD-GUIDE.md Prompt 5.1
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
    /// Main controller for the AR HUD overlay.
    /// Coordinates all HUD elements and updates them based on game state.
    /// </summary>
    public class ARHUD : MonoBehaviour
    {
        #region Singleton
        
        private static ARHUD _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ARHUD Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ARHUD>();
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("HUD Components")]
        [SerializeField]
        [Tooltip("Compass UI component")]
        private CompassUI compass;
        
        [SerializeField]
        [Tooltip("Radar/Mini-map UI component")]
        private RadarUI radar;
        
        [SerializeField]
        [Tooltip("Crosshairs UI component")]
        private CrosshairsController crosshairs;
        
        [SerializeField]
        [Tooltip("Gas meter UI component")]
        private GasMeterUI gasMeter;
        
        [SerializeField]
        [Tooltip("Find limit UI component")]
        private FindLimitUI findLimit;
        
        [SerializeField]
        [Tooltip("AR tracking status UI")]
        private ARTrackingUI trackingUI;
        
        [Header("Coin Info Panel")]
        [SerializeField]
        [Tooltip("Panel showing selected coin info")]
        private GameObject coinInfoPanel;
        
        [SerializeField]
        private TMP_Text coinValueText;
        
        [SerializeField]
        private TMP_Text coinDistanceText;
        
        [SerializeField]
        private TMP_Text coinStatusText;
        
        [SerializeField]
        private Image coinTierIcon;
        
        [Header("Message Display")]
        [SerializeField]
        [Tooltip("Temporary message display")]
        private TMP_Text messageText;
        
        [SerializeField]
        private CanvasGroup messageCanvasGroup;
        
        [SerializeField]
        private float messageDuration = 3f;
        
        [Header("Collection Popup")]
        [SerializeField]
        private GameObject collectionPopup;
        
        [SerializeField]
        private TMP_Text collectionValueText;
        
        [SerializeField]
        private TMP_Text collectionMessageText;
        
        [Header("Locked Popup")]
        [SerializeField]
        private GameObject lockedPopup;
        
        [SerializeField]
        private TMP_Text lockedValueText;
        
        [SerializeField]
        private TMP_Text lockedMessageText;
        
        [Header("Main Canvas")]
        [SerializeField]
        private Canvas hudCanvas;
        
        [SerializeField]
        private CanvasGroup hudCanvasGroup;
        
        [Header("Settings")]
        [SerializeField]
        private bool showCompass = true;
        
        [SerializeField]
        private bool showRadar = true;
        
        [SerializeField]
        private bool showGasMeter = true;
        
        [SerializeField]
        private bool showFindLimit = true;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is HUD currently visible?
        /// </summary>
        public bool IsVisible { get; private set; } = true;
        
        /// <summary>
        /// Currently selected coin (if any)
        /// </summary>
        public Coin SelectedCoin { get; private set; }
        
        #endregion
        
        #region Private Fields
        
        private float messageTimer = 0f;
        private bool isShowingMessage = false;
        
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
            // Subscribe to events
            SubscribeToEvents();
            
            // Initial UI update
            UpdateHUD();
            
            // Hide popups
            HideCollectionPopup();
            HideLockedPopup();
            HideCoinInfo();
            HideMessage();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void Update()
        {
            // Update HUD each frame
            UpdateHUD();
            
            // Handle message fade
            UpdateMessageFade();
        }
        
        #endregion
        
        #region Event Subscriptions
        
        /// <summary>
        /// Subscribe to game events
        /// </summary>
        private void SubscribeToEvents()
        {
            // Player data events
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnBalanceChanged += OnBalanceChanged;
                PlayerData.Instance.OnGasChanged += OnGasChanged;
                PlayerData.Instance.OnFindLimitChanged += OnFindLimitChanged;
            }
            
            // Coin manager events
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.OnCoinCollected += OnCoinCollected;
                CoinManager.Instance.OnCoinSelectionChanged += OnCoinSelectionChanged;
            }
            
            // Proximity events
            if (ProximityManager.Instance != null)
            {
                ProximityManager.Instance.OnZoneChanged += OnProximityZoneChanged;
            }
        }
        
        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnBalanceChanged -= OnBalanceChanged;
                PlayerData.Instance.OnGasChanged -= OnGasChanged;
                PlayerData.Instance.OnFindLimitChanged -= OnFindLimitChanged;
            }
            
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.OnCoinCollected -= OnCoinCollected;
                CoinManager.Instance.OnCoinSelectionChanged -= OnCoinSelectionChanged;
            }
            
            if (ProximityManager.Instance != null)
            {
                ProximityManager.Instance.OnZoneChanged -= OnProximityZoneChanged;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnBalanceChanged(float newBalance)
        {
            Log($"Balance changed: ${newBalance:F2}");
        }
        
        private void OnGasChanged(float daysRemaining)
        {
            if (gasMeter != null)
            {
                gasMeter.UpdateGas(daysRemaining);
            }
        }
        
        private void OnFindLimitChanged(float newLimit)
        {
            if (findLimit != null)
            {
                findLimit.UpdateFindLimit(newLimit);
            }
        }
        
        private void OnCoinCollected(CoinController coin, float value)
        {
            ShowCollectionPopup(value);
        }
        
        private void OnCoinSelectionChanged(CoinController coin)
        {
            if (coin != null && coin.CoinData != null)
            {
                ShowCoinInfo(coin.CoinData, coin.IsLocked);
            }
            else
            {
                HideCoinInfo();
            }
        }
        
        private void OnProximityZoneChanged(ProximityZone oldZone, ProximityZone newZone)
        {
            // Update crosshairs based on zone
            if (crosshairs != null)
            {
                switch (newZone)
                {
                    case ProximityZone.Collectible:
                        crosshairs.SetState(CrosshairsState.InRange);
                        break;
                    case ProximityZone.Near:
                    case ProximityZone.Medium:
                        crosshairs.SetState(CrosshairsState.Hovering);
                        break;
                    default:
                        crosshairs.SetState(CrosshairsState.Normal);
                        break;
                }
            }
        }
        
        #endregion
        
        #region HUD Updates
        
        /// <summary>
        /// Update all HUD elements
        /// </summary>
        public void UpdateHUD()
        {
            if (!IsVisible) return;
            
            // Update gas meter
            if (gasMeter != null && PlayerData.Exists)
            {
                gasMeter.UpdateGas(PlayerData.Instance.GasDays);
            }
            
            // Update find limit
            if (findLimit != null && PlayerData.Exists)
            {
                findLimit.UpdateFindLimit(PlayerData.Instance.FindLimit);
            }
            
            // Update coin info if selected
            if (SelectedCoin != null)
            {
                UpdateCoinInfoDistance();
            }
        }
        
        /// <summary>
        /// Update coin info distance display
        /// </summary>
        private void UpdateCoinInfoDistance()
        {
            if (coinDistanceText == null || SelectedCoin == null) return;
            
            coinDistanceText.text = GeoUtils.FormatDistance(SelectedCoin.distanceFromPlayer);
        }
        
        #endregion
        
        #region Show/Hide HUD
        
        /// <summary>
        /// Show the entire HUD
        /// </summary>
        public void Show()
        {
            if (hudCanvas != null)
            {
                hudCanvas.gameObject.SetActive(true);
            }
            
            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = 1f;
            }
            
            IsVisible = true;
            Log("HUD shown");
        }
        
        /// <summary>
        /// Hide the entire HUD
        /// </summary>
        public void Hide()
        {
            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = 0f;
            }
            
            IsVisible = false;
            Log("HUD hidden");
        }
        
        /// <summary>
        /// Toggle HUD visibility
        /// </summary>
        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }
        
        /// <summary>
        /// Fade HUD to specified alpha
        /// </summary>
        public void FadeTo(float alpha, float duration = 0.3f)
        {
            if (hudCanvasGroup != null)
            {
                StartCoroutine(FadeCoroutine(alpha, duration));
            }
        }
        
        private System.Collections.IEnumerator FadeCoroutine(float targetAlpha, float duration)
        {
            float startAlpha = hudCanvasGroup.alpha;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                hudCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
                yield return null;
            }
            
            hudCanvasGroup.alpha = targetAlpha;
            IsVisible = targetAlpha > 0;
        }
        
        #endregion
        
        #region Component Visibility
        
        /// <summary>
        /// Show/hide compass
        /// </summary>
        public void SetCompassVisible(bool visible)
        {
            showCompass = visible;
            if (compass != null)
            {
                if (visible) compass.Show();
                else compass.Hide();
            }
        }
        
        /// <summary>
        /// Show/hide radar
        /// </summary>
        public void SetRadarVisible(bool visible)
        {
            showRadar = visible;
            if (radar != null)
            {
                if (visible) radar.Show();
                else radar.Hide();
            }
        }
        
        /// <summary>
        /// Show/hide gas meter
        /// </summary>
        public void SetGasMeterVisible(bool visible)
        {
            showGasMeter = visible;
            if (gasMeter != null)
            {
                gasMeter.gameObject.SetActive(visible);
            }
        }
        
        /// <summary>
        /// Show/hide find limit
        /// </summary>
        public void SetFindLimitVisible(bool visible)
        {
            showFindLimit = visible;
            if (findLimit != null)
            {
                findLimit.gameObject.SetActive(visible);
            }
        }
        
        #endregion
        
        #region Coin Info Panel
        
        /// <summary>
        /// Show coin info panel
        /// </summary>
        public void ShowCoinInfo(Coin coin, bool isLocked = false)
        {
            if (coinInfoPanel == null) return;
            
            SelectedCoin = coin;
            coinInfoPanel.SetActive(true);
            
            // Update value
            if (coinValueText != null)
            {
                coinValueText.text = coin.GetDisplayValue();
                coinValueText.color = isLocked ? 
                    new Color(0.94f, 0.27f, 0.27f) : // Red for locked
                    new Color(1f, 0.84f, 0f);        // Gold for normal
            }
            
            // Update distance
            if (coinDistanceText != null)
            {
                coinDistanceText.text = GeoUtils.FormatDistance(coin.distanceFromPlayer);
            }
            
            // Update status
            if (coinStatusText != null)
            {
                if (isLocked)
                {
                    coinStatusText.text = "🔒 LOCKED";
                    coinStatusText.color = new Color(0.94f, 0.27f, 0.27f);
                }
                else if (coin.isInRange)
                {
                    coinStatusText.text = "✓ IN RANGE";
                    coinStatusText.color = new Color(0.29f, 0.87f, 0.5f);
                }
                else
                {
                    coinStatusText.text = "→ GET CLOSER";
                    coinStatusText.color = Color.white;
                }
            }
            
            Log($"Showing coin info: {coin.GetDisplayValue()}");
        }
        
        /// <summary>
        /// Hide coin info panel
        /// </summary>
        public void HideCoinInfo()
        {
            if (coinInfoPanel != null)
            {
                coinInfoPanel.SetActive(false);
            }
            SelectedCoin = null;
        }
        
        #endregion
        
        #region Collection Popup
        
        /// <summary>
        /// Show collection success popup
        /// </summary>
        public void ShowCollectionPopup(float value)
        {
            if (collectionPopup == null) return;
            
            collectionPopup.SetActive(true);
            
            if (collectionValueText != null)
            {
                collectionValueText.text = $"+${value:F2}";
            }
            
            if (collectionMessageText != null)
            {
                collectionMessageText.text = GetCollectionMessage(value);
            }
            
            // Auto-hide after delay
            Invoke(nameof(HideCollectionPopup), 3f);
            
            Log($"Collection popup: +${value:F2}");
        }
        
        /// <summary>
        /// Hide collection popup
        /// </summary>
        public void HideCollectionPopup()
        {
            if (collectionPopup != null)
            {
                collectionPopup.SetActive(false);
            }
        }
        
        /// <summary>
        /// Get pirate-themed collection message
        /// </summary>
        private string GetCollectionMessage(float value)
        {
            if (value >= 25f) return "Shiver me timbers! A king's ransom!";
            if (value >= 10f) return "Arr! A fine treasure, matey!";
            if (value >= 5f) return "Yo ho ho! Nice find!";
            if (value >= 1f) return "Every doubloon counts!";
            return "A bit of booty for ye!";
        }
        
        #endregion
        
        #region Locked Popup
        
        /// <summary>
        /// Show locked coin popup
        /// </summary>
        public void ShowLockedPopup(float coinValue, float playerLimit)
        {
            if (lockedPopup == null) return;
            
            lockedPopup.SetActive(true);
            
            if (lockedValueText != null)
            {
                lockedValueText.text = $"${coinValue:F2}";
            }
            
            if (lockedMessageText != null)
            {
                lockedMessageText.text = $"This treasure be above yer limit!\n" +
                                         $"Hide ${coinValue:F2} to unlock.";
            }
            
            // Auto-hide after delay
            Invoke(nameof(HideLockedPopup), 4f);
            
            Log($"Locked popup: ${coinValue:F2} (limit: ${playerLimit:F2})");
        }
        
        /// <summary>
        /// Hide locked popup
        /// </summary>
        public void HideLockedPopup()
        {
            if (lockedPopup != null)
            {
                lockedPopup.SetActive(false);
            }
        }
        
        #endregion
        
        #region Message Display
        
        /// <summary>
        /// Show a temporary message
        /// </summary>
        public void ShowMessage(string message, float duration = 0f)
        {
            if (messageText == null) return;
            
            messageText.text = message;
            
            if (messageCanvasGroup != null)
            {
                messageCanvasGroup.alpha = 1f;
            }
            
            messageTimer = duration > 0 ? duration : messageDuration;
            isShowingMessage = true;
            
            Log($"Message: {message}");
        }
        
        /// <summary>
        /// Hide message immediately
        /// </summary>
        public void HideMessage()
        {
            if (messageCanvasGroup != null)
            {
                messageCanvasGroup.alpha = 0f;
            }
            isShowingMessage = false;
        }
        
        /// <summary>
        /// Update message fade
        /// </summary>
        private void UpdateMessageFade()
        {
            if (!isShowingMessage || messageCanvasGroup == null) return;
            
            messageTimer -= Time.deltaTime;
            
            if (messageTimer <= 0)
            {
                HideMessage();
            }
            else if (messageTimer < 1f)
            {
                // Fade out in last second
                messageCanvasGroup.alpha = messageTimer;
            }
        }
        
        #endregion
        
        #region Quick Messages
        
        /// <summary>
        /// Show "Get closer!" message
        /// </summary>
        public void ShowGetCloserMessage(float distance)
        {
            ShowMessage($"Get closer! {GeoUtils.FormatDistance(distance)} away");
        }
        
        /// <summary>
        /// Show "Low gas!" warning
        /// </summary>
        public void ShowLowGasWarning(float daysRemaining)
        {
            ShowMessage($"⚠️ Low gas! Only {daysRemaining:F1} days remaining!");
        }
        
        /// <summary>
        /// Show "No gas!" error
        /// </summary>
        public void ShowNoGasError()
        {
            ShowMessage("⛽ Out of gas! Add more to continue hunting.");
        }
        
        /// <summary>
        /// Show GPS status message
        /// </summary>
        public void ShowGPSMessage(string status)
        {
            ShowMessage($"📍 {status}");
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[ARHUD] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print HUD state
        /// </summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== ARHUD State ===");
            Debug.Log($"Visible: {IsVisible}");
            Debug.Log($"Compass: {(compass != null ? compass.IsVisible.ToString() : "null")}");
            Debug.Log($"Radar: {(radar != null ? radar.IsVisible.ToString() : "null")}");
            Debug.Log($"Selected Coin: {SelectedCoin?.GetDisplayValue() ?? "none"}");
            Debug.Log("===================");
        }
        
        /// <summary>
        /// Debug: Show test collection
        /// </summary>
        [ContextMenu("Debug: Test Collection Popup")]
        public void DebugTestCollection()
        {
            ShowCollectionPopup(5.00f);
        }
        
        /// <summary>
        /// Debug: Show test locked
        /// </summary>
        [ContextMenu("Debug: Test Locked Popup")]
        public void DebugTestLocked()
        {
            ShowLockedPopup(25.00f, 5.00f);
        }
        
        /// <summary>
        /// Debug: Show test message
        /// </summary>
        [ContextMenu("Debug: Test Message")]
        public void DebugTestMessage()
        {
            ShowMessage("Ahoy! This be a test message, matey! 🏴‍☠️");
        }
        
        #endregion
    }
}
