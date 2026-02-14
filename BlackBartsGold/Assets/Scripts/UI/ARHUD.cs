// ============================================================================
// ARHUD.cs
// Black Bart's Gold - AR Heads-Up Display Controller
// Path: Assets/Scripts/UI/ARHUD.cs
// ============================================================================
// Main controller for the AR HUD overlay. Manages HUD elements including
// compass, gas meter, find limit, and mini-map. (Crosshairs removed - code-based AR setup.)
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
using BlackBartsGold.Utils;

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
        
        [Header("HUD Components (code-based - populated at runtime)")]
        private CompassUI compass;
        private RadarUI radar;
        private GasMeterUI gasMeter;
        private FindLimitUI findLimit;
        private ARTrackingUI trackingUI;
        private CoinDirectionIndicator directionIndicator;
        
        [Header("Coin Info Panel (code-based - populated at runtime)")]
        private GameObject coinInfoPanel;
        private TMP_Text coinValueText;
        private TMP_Text coinDistanceText;
        private TMP_Text coinStatusText;
        private Image coinTierIcon;
        
        [Header("Message Display (code-based - populated at runtime)")]
        private TMP_Text messageText;
        private CanvasGroup messageCanvasGroup;
        
        [SerializeField]
        private float messageDuration = 3f;
        
        [Header("Collection Popup (code-based - populated at runtime)")]
        private GameObject collectionPopup;
        private TMP_Text collectionValueText;
        private TMP_Text collectionMessageText;
        
        [Header("Locked Popup (code-based - populated at runtime)")]
        private GameObject lockedPopup;
        private TMP_Text lockedValueText;
        private TMP_Text lockedMessageText;
        
        [Header("Main Canvas (code-based - populated at runtime)")]
        private Canvas hudCanvas;
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
        
        [SerializeField]
        [Tooltip("Show large direction indicator when hunting")]
        private bool showDirectionIndicator = true;
        
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
        
        #region Runtime Initialization (Code-Based Setup)
        
        /// <summary>
        /// Resolve references to code-created UI panels. Called by ARHuntSceneSetup
        /// after it creates MessagePanel, LockedPopup, CollectionPopup, CoinInfoPanel.
        /// No Inspector wiring required - all panels created at runtime.
        /// </summary>
        /// <param name="root">Transform that has the panels as children (Canvas root)</param>
        public void InitializeRuntimeReferences(Transform root)
        {
            if (root == null) root = transform;

            // MessagePanel
            var msgPanel = root.Find("MessagePanel");
            if (msgPanel != null)
            {
                messageText = msgPanel.Find("MessageText")?.GetComponent<TMP_Text>();
                messageCanvasGroup = msgPanel.GetComponent<CanvasGroup>();
            }

            // LockedPopup
            var lockedPanel = root.Find("LockedPopup");
            if (lockedPanel != null)
            {
                lockedPopup = lockedPanel.gameObject;
                lockedValueText = lockedPanel.Find("LockedValueText")?.GetComponent<TMP_Text>();
                lockedMessageText = lockedPanel.Find("LockedMessageText")?.GetComponent<TMP_Text>();
            }

            // CollectionPopup
            var collPanel = root.Find("CollectionPopup");
            if (collPanel != null)
            {
                collectionPopup = collPanel.gameObject;
                collectionValueText = collPanel.Find("CollectionValueText")?.GetComponent<TMP_Text>();
                collectionMessageText = collPanel.Find("CollectionMessageText")?.GetComponent<TMP_Text>();
            }

            // CoinInfoPanel
            var coinPanel = root.Find("CoinInfoPanel");
            if (coinPanel != null)
            {
                coinInfoPanel = coinPanel.gameObject;
                coinValueText = coinPanel.Find("CoinValueText")?.GetComponent<TMP_Text>();
                coinDistanceText = coinPanel.Find("CoinDistanceText")?.GetComponent<TMP_Text>();
                coinStatusText = coinPanel.Find("CoinStatusText")?.GetComponent<TMP_Text>();
                coinTierIcon = coinPanel.Find("CoinTierIcon")?.GetComponent<Image>();
            }

            // HUD Components (Compass, Radar, Gas, FindLimit, DirectionIndicator)
            var compassPanel = root.Find("CompassPanel");
            if (compassPanel != null)
                compass = compassPanel.GetComponent<CompassUI>();

            var radarPanel = root.Find("RadarPanel");
            if (radarPanel != null)
                radar = radarPanel.GetComponent<RadarUI>();

            var gasPanel = root.Find("GasMeterPanel");
            if (gasPanel != null)
                gasMeter = gasPanel.GetComponent<GasMeterUI>();

            var findLimitPanel = root.Find("FindLimitPanel");
            if (findLimitPanel != null)
                findLimit = findLimitPanel.GetComponent<FindLimitUI>();

            var dirIndicatorPanel = root.Find("DirectionIndicatorPanel");
            if (dirIndicatorPanel != null)
                directionIndicator = dirIndicatorPanel.GetComponent<CoinDirectionIndicator>();

            // Main Canvas (root is the Canvas)
            hudCanvas = root.GetComponent<Canvas>();
            if (hudCanvas == null)
                hudCanvas = root.GetComponentInParent<Canvas>();
            hudCanvasGroup = root.GetComponent<CanvasGroup>();
            if (hudCanvasGroup == null)
                hudCanvasGroup = root.GetComponentInParent<CanvasGroup>();

            if (debugMode)
                Debug.Log("[ARHUD] InitializeRuntimeReferences complete");
        }
        
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
            
            // Check if a target already exists (target is set on the map screen BEFORE
            // the AR scene loads, so the OnTargetSet event won't fire again)
            if (CoinManager.Instance != null && CoinManager.Instance.HasTarget)
            {
                Coin existingTarget = CoinManager.Instance.TargetCoinData;
                if (existingTarget != null)
                {
                    Debug.Log($"[ARHUD] Found existing target on Start: {existingTarget.GetDisplayValue()} — initializing HUD");
                    ShowCoinInfo(existingTarget, existingTarget.isLocked);
                    
                    // Show direction indicator for existing target
                    if (directionIndicator != null && showDirectionIndicator)
                    {
                        directionIndicator.Show();
                        Debug.Log("[ARHUD] Direction indicator shown for existing target");
                    }
                }
            }
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
            
            // Coin manager events (single-target architecture)
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.OnCoinCollected += OnCoinCollected;
                CoinManager.Instance.OnCoinSelectionChanged += OnCoinSelectionChanged;
                CoinManager.Instance.OnHuntModeChanged += OnHuntModeChanged;
                CoinManager.Instance.OnTargetSet += OnTargetSet;
                CoinManager.Instance.OnTargetCleared += OnTargetCleared;
                CoinManager.Instance.OnTargetCollected += OnTargetCollected;
                CoinManager.Instance.OnCoinMaterialized += OnCoinMaterialized;
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
                CoinManager.Instance.OnHuntModeChanged -= OnHuntModeChanged;
                CoinManager.Instance.OnTargetSet -= OnTargetSet;
                CoinManager.Instance.OnTargetCleared -= OnTargetCleared;
                CoinManager.Instance.OnTargetCollected -= OnTargetCollected;
                CoinManager.Instance.OnCoinMaterialized -= OnCoinMaterialized;
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
            // Crosshairs removed - zone changes handled by code-based AR setup (CollectionSizeCircle, etc.)
        }
        
        /// <summary>
        /// Handle hunt mode changed (single-target architecture)
        /// </summary>
        private void OnHuntModeChanged(HuntMode mode)
        {
            Log($"Hunt mode changed: {mode}");
            
            switch (mode)
            {
                case HuntMode.MapView:
                    // In map view - show "tap radar to select coin" hint
                    ShowMessage("Tap the mini-map to select a treasure to hunt!");
                    HideCoinInfo();
                    break;
                    
                case HuntMode.Hunting:
                    // Hunting - show target coin info
                    if (CoinManager.Instance?.TargetCoinData != null)
                    {
                        ShowCoinInfo(CoinManager.Instance.TargetCoinData, 
                                     CoinManager.Instance.TargetCoin?.IsLocked ?? false);
                    }
                    break;
                    
                case HuntMode.Collecting:
                    // Collecting - handled by collection popup
                    break;
            }
        }
        
        /// <summary>
        /// Handle target coin set
        /// </summary>
        private void OnTargetSet(Coin coin)
        {
            Log($"Target set: {coin.GetDisplayValue()}");
            ShowCoinInfo(coin, coin.isLocked);
            ShowMessage($"Hunting: {coin.GetDisplayValue()} treasure!");
            
            // Explicitly show direction indicator (belt-and-suspenders with CoinDirectionIndicator's own event)
            if (directionIndicator != null && showDirectionIndicator)
            {
                directionIndicator.Show();
                Log("Direction indicator shown for target");
            }
        }
        
        /// <summary>
        /// Handle target cleared
        /// </summary>
        private void OnTargetCleared()
        {
            Log("Target cleared");
            HideCoinInfo();
            
            // Hide direction indicator when target is cleared
            if (directionIndicator != null)
            {
                directionIndicator.Hide();
                Log("Direction indicator hidden (target cleared)");
            }
        }
        
        /// <summary>
        /// Handle target collected (single-target architecture)
        /// </summary>
        private void OnTargetCollected(Coin coin, float value)
        {
            Log($"Target collected! Value: ${value:F2}");
            ShowCollectionPopup(value);
        }
        
        /// <summary>
        /// Handle coin materialization (Pokemon GO pattern).
        /// Called when player gets close enough and coin appears in AR view.
        /// </summary>
        private void OnCoinMaterialized(CoinController coin)
        {
            Log($"Coin materialized! {coin.CoinData?.GetDisplayValue()}");
            
            // Show exciting message
            ShowMessage("A Gold Doubloon appears! Walk closer to collect!");
            
            // Crosshairs removed - target state shown by CollectionSizeCircle in code-based AR setup
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
        
        /// <summary>
        /// Show/hide direction indicator
        /// </summary>
        public void SetDirectionIndicatorVisible(bool visible)
        {
            showDirectionIndicator = visible;
            if (directionIndicator != null)
            {
                if (visible) directionIndicator.Show();
                else directionIndicator.Hide();
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
                    coinStatusText.text = EmojiHelper.Sanitize("🔒 LOCKED");
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
            
            messageText.text = EmojiHelper.Sanitize(message);
            
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
        
        #region Full Map Integration
        
        /// <summary>
        /// Open the full map view to select a coin.
        /// Uses UIManager.OnMiniMapClicked() which handles both FullMapUI and code-generated fallback.
        /// </summary>
        public void OpenFullMap()
        {
            Log($"OpenFullMap: UIManager.Instance={UIManager.Instance != null}, FullMapUI.Exists={FullMapUI.Exists}");
            if (UIManager.Instance != null)
            {
                Log("OpenFullMap path: UIManager.OnMiniMapClicked()");
                UIManager.Instance.OnMiniMapClicked();
            }
            else if (FullMapUI.Exists)
            {
                FullMapUI.Instance.Show();
            }
            else
            {
                Log("UIManager and FullMapUI not found");
                ShowMessage("Map not available");
            }
        }
        
        /// <summary>
        /// Called when radar/mini-map is tapped
        /// </summary>
        public void OnRadarTapped()
        {
            Log("Radar tapped - opening full map");
            OpenFullMap();
        }
        
        /// <summary>
        /// Return to map view (abandon current hunt)
        /// </summary>
        public void ReturnToMapView()
        {
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.EnterMapView();
            }
            OpenFullMap();
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
