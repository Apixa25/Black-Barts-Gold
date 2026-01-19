// ============================================================================
// FindLimitPopup.cs
// Black Bart's Gold - Find Limit / Locked Coin Popup
// Path: Assets/Scripts/UI/FindLimitPopup.cs
// ============================================================================
// Modal shown when tapping a locked coin (above player's find limit).
// Explains the limit system and provides hint to unlock.
// Reference: BUILD-GUIDE.md Sprint 7, Prompt 7.4
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Economy;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Find limit popup controller.
    /// Shown when player tries to collect a coin above their limit.
    /// </summary>
    public class FindLimitPopup : MonoBehaviour
    {
        #region Singleton
        
        private static FindLimitPopup _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static FindLimitPopup Instance => _instance;
        
        /// <summary>
        /// Check if instance exists
        /// </summary>
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Popup Panel")]
        [SerializeField]
        private GameObject popupPanel;
        
        [SerializeField]
        private GameObject modalBackground;
        
        [SerializeField]
        private CanvasGroup canvasGroup;
        
        [SerializeField]
        private RectTransform popupRect;
        
        [Header("Text Elements")]
        [SerializeField]
        private TMP_Text titleText;
        
        [SerializeField]
        private TMP_Text messageText;
        
        [SerializeField]
        private TMP_Text coinValueText;
        
        [SerializeField]
        private TMP_Text playerLimitText;
        
        [SerializeField]
        private TMP_Text unlockHintText;
        
        [Header("Visual Elements")]
        [SerializeField]
        private Image coinIcon;
        
        [SerializeField]
        private Image lockIcon;
        
        [SerializeField]
        private Image tierColorBar;
        
        [Header("Buttons")]
        [SerializeField]
        private Button hideCoinsButton;
        
        [SerializeField]
        private TMP_Text hideCoinsButtonText;
        
        [SerializeField]
        private Button cancelButton;
        
        [SerializeField]
        private TMP_Text cancelButtonText;
        
        [Header("Colors")]
        [SerializeField]
        private Color lockedColor = new Color(0.94f, 0.27f, 0.27f); // Red
        
        [SerializeField]
        private Color goldColor = new Color(1f, 0.84f, 0f);
        
        [Header("Animation")]
        [SerializeField]
        private float fadeInDuration = 0.3f;
        
        [SerializeField]
        private float fadeOutDuration = 0.2f;
        
        [SerializeField]
        private float shakeAmount = 5f;
        
        [SerializeField]
        private float shakeDuration = 0.3f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private bool isShowing = false;
        private Coin currentCoin;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when hide coins button is clicked
        /// </summary>
        public event Action OnHideCoinsClicked;
        
        /// <summary>
        /// Fired when popup is cancelled
        /// </summary>
        public event Action OnCancelled;
        
        /// <summary>
        /// Fired when popup is shown
        /// </summary>
        public event Action<Coin> OnPopupShown;
        
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
            SetupContent();
            
            // Start hidden
            Hide(immediate: true);
            
            // Subscribe to collection events
            if (CollectionService.Exists)
            {
                CollectionService.Instance.OnCoinOverLimit += HandleCoinOverLimit;
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            if (CollectionService.Exists)
            {
                CollectionService.Instance.OnCoinOverLimit -= HandleCoinOverLimit;
            }
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupListeners()
        {
            if (hideCoinsButton != null)
            {
                hideCoinsButton.onClick.AddListener(OnHideCoinsButtonClicked);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
            
            // Click background to dismiss
            if (modalBackground != null)
            {
                Button bgButton = modalBackground.GetComponent<Button>();
                if (bgButton == null)
                {
                    bgButton = modalBackground.AddComponent<Button>();
                    bgButton.transition = Selectable.Transition.None;
                }
                bgButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }
        
        /// <summary>
        /// Setup static content
        /// </summary>
        private void SetupContent()
        {
            if (titleText != null)
            {
                titleText.text = "🔒 Treasure Locked!";
            }
            
            if (hideCoinsButtonText != null)
            {
                hideCoinsButtonText.text = "💰 Hide a Coin";
            }
            
            if (cancelButtonText != null)
            {
                cancelButtonText.text = "Cancel";
            }
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the popup for a locked coin
        /// </summary>
        public void Show(Coin coin, float playerLimit)
        {
            if (coin == null) return;
            
            Log($"Showing find limit popup for coin ${coin.GetEffectiveValue():F2}");
            
            currentCoin = coin;
            isShowing = true;
            
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }
            
            if (modalBackground != null)
            {
                modalBackground.SetActive(true);
            }
            
            // Update content
            UpdateContent(coin, playerLimit);
            
            // Play animation
            StartCoroutine(ShowAnimation());
            
            OnPopupShown?.Invoke(coin);
        }
        
        /// <summary>
        /// Show with just the coin (gets limit from service)
        /// </summary>
        public void Show(Coin coin)
        {
            float limit = FindLimitService.Exists 
                ? FindLimitService.Instance.GetCurrentLimit()
                : PlayerData.Exists ? PlayerData.Instance.FindLimit : 1.00f;
            
            Show(coin, limit);
        }
        
        /// <summary>
        /// Hide the popup
        /// </summary>
        public void Hide(bool immediate = false)
        {
            if (immediate)
            {
                isShowing = false;
                currentCoin = null;
                if (popupPanel != null) popupPanel.SetActive(false);
                if (modalBackground != null) modalBackground.SetActive(false);
                if (canvasGroup != null) canvasGroup.alpha = 0f;
            }
            else
            {
                StartCoroutine(HideAnimation());
            }
        }
        
        /// <summary>
        /// Check if popup is showing
        /// </summary>
        public bool IsShowing => isShowing;
        
        #endregion
        
        #region Content Update
        
        /// <summary>
        /// Update popup content
        /// </summary>
        private void UpdateContent(Coin coin, float playerLimit)
        {
            float coinValue = coin.GetEffectiveValue();
            
            // Main message
            if (messageText != null)
            {
                messageText.text = "This treasure be above yer limit, matey!\n" +
                    "Ye must prove yer worth by hiding treasure first!";
            }
            
            // Coin value
            if (coinValueText != null)
            {
                coinValueText.text = $"Coin Value: ${coinValue:F2}";
                coinValueText.color = GetTierColor(coin.GetTier());
            }
            
            // Player limit
            if (playerLimitText != null)
            {
                playerLimitText.text = $"Your Limit: ${playerLimit:F2}";
                playerLimitText.color = lockedColor;
            }
            
            // Unlock hint
            if (unlockHintText != null)
            {
                unlockHintText.text = $"💡 Hide ${coinValue:F2} to unlock finds up to ${coinValue:F2}!";
            }
            
            // Tier color
            if (tierColorBar != null)
            {
                tierColorBar.color = GetTierColor(coin.GetTier());
            }
            
            // Coin icon color
            if (coinIcon != null)
            {
                coinIcon.color = GetTierColor(coin.GetTier());
            }
        }
        
        /// <summary>
        /// Get color for coin tier
        /// </summary>
        private Color GetTierColor(CoinTier tier)
        {
            if (FindLimitService.Exists)
            {
                return FindLimitService.Instance.GetTier(
                    tier switch
                    {
                        CoinTier.Bronze => FindLimitTier.CabinBoy,
                        CoinTier.Silver => FindLimitTier.DeckHand,
                        CoinTier.Gold => FindLimitTier.TreasureHunter,
                        CoinTier.Platinum => FindLimitTier.Captain,
                        CoinTier.Diamond => FindLimitTier.PirateLegend,
                        _ => FindLimitTier.CabinBoy
                    }
                ).color;
            }
            
            return tier switch
            {
                CoinTier.Bronze => new Color(0.8f, 0.5f, 0.2f),
                CoinTier.Silver => new Color(0.75f, 0.75f, 0.75f),
                CoinTier.Gold => new Color(1f, 0.84f, 0f),
                CoinTier.Platinum => new Color(0.9f, 0.9f, 1f),
                CoinTier.Diamond => new Color(0.7f, 0.9f, 1f),
                _ => goldColor
            };
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnHideCoinsButtonClicked()
        {
            Log("Hide coins clicked");
            
            Hide();
            OnHideCoinsClicked?.Invoke();
            
            // TODO: Navigate to hide coins screen
            // For now, show a message
            Debug.Log("[FindLimitPopup] Hide coins feature coming soon!");
        }
        
        private void OnCancelButtonClicked()
        {
            Log("Cancel clicked");
            
            Hide();
            OnCancelled?.Invoke();
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// Show animation coroutine
        /// </summary>
        private IEnumerator ShowAnimation()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (popupRect != null) popupRect.localScale = Vector3.one * 0.8f;
            
            float elapsed = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                
                // Ease out
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = easedT;
                }
                
                if (popupRect != null)
                {
                    float scale = Mathf.Lerp(0.8f, 1f, easedT);
                    popupRect.localScale = Vector3.one * scale;
                }
                
                yield return null;
            }
            
            // Ensure final state
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (popupRect != null) popupRect.localScale = Vector3.one;
            
            // Small shake to emphasize "locked"
            StartCoroutine(ShakeAnimation());
        }
        
        /// <summary>
        /// Hide animation coroutine
        /// </summary>
        private IEnumerator HideAnimation()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup?.alpha ?? 1f;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                }
                
                yield return null;
            }
            
            isShowing = false;
            currentCoin = null;
            
            if (popupPanel != null) popupPanel.SetActive(false);
            if (modalBackground != null) modalBackground.SetActive(false);
        }
        
        /// <summary>
        /// Shake animation for emphasis
        /// </summary>
        private IEnumerator ShakeAnimation()
        {
            if (popupRect == null) yield break;
            
            Vector3 originalPos = popupRect.localPosition;
            float elapsed = 0f;
            
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shakeDuration;
                
                // Decreasing intensity
                float intensity = (1f - t) * shakeAmount;
                float x = Mathf.Sin(elapsed * 50f) * intensity;
                
                popupRect.localPosition = originalPos + new Vector3(x, 0f, 0f);
                yield return null;
            }
            
            popupRect.localPosition = originalPos;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleCoinOverLimit(Coin coin, float playerLimit)
        {
            Show(coin, playerLimit);
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[FindLimitPopup] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Show test popup
        /// </summary>
        [ContextMenu("Debug: Show Test Popup")]
        public void DebugShowTestPopup()
        {
            var testCoin = Coin.CreateTestCoin(CoinType.Fixed, 10.00f);
            Show(testCoin, 1.00f);
        }
        
        /// <summary>
        /// Debug: Show high value popup
        /// </summary>
        [ContextMenu("Debug: Show $50 Locked Coin")]
        public void DebugShowHighValue()
        {
            var testCoin = Coin.CreateTestCoin(CoinType.Fixed, 50.00f);
            Show(testCoin, 5.00f);
        }
        
        #endregion
    }
}
