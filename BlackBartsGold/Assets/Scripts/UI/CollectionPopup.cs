// ============================================================================
// CollectionPopup.cs
// Black Bart's Gold - Coin Collection Success Popup
// Path: Assets/Scripts/UI/CollectionPopup.cs
// ============================================================================
// Popup shown after successfully collecting a coin. Displays value, 
// congratulation message, and auto-dismisses.
// Reference: BUILD-GUIDE.md Sprint 7, Prompt 7.3
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using BlackBartsGold.Core;
using BlackBartsGold.Economy;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Collection success popup controller.
    /// Shows celebratory popup with coin value and pirate message.
    /// </summary>
    public class CollectionPopup : MonoBehaviour
    {
        #region Singleton
        
        private static CollectionPopup _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static CollectionPopup Instance => _instance;
        
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
        private CanvasGroup canvasGroup;
        
        [SerializeField]
        private RectTransform popupRect;
        
        [Header("Text Elements")]
        [SerializeField]
        private TMP_Text valueText;
        
        [SerializeField]
        private TMP_Text messageText;
        
        [SerializeField]
        private TMP_Text pendingText;
        
        [Header("Visual Elements")]
        [SerializeField]
        private Image coinIcon;
        
        [SerializeField]
        private Image backgroundImage;
        
        [SerializeField]
        private Image glowEffect;
        
        [Header("Tier Colors")]
        [SerializeField]
        private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        
        [SerializeField]
        private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        
        [SerializeField]
        private Color goldColor = new Color(1f, 0.84f, 0f);
        
        [SerializeField]
        private Color platinumColor = new Color(0.9f, 0.9f, 1f);
        
        [SerializeField]
        private Color diamondColor = new Color(0.7f, 0.9f, 1f);
        
        [Header("Animation")]
        [SerializeField]
        private float showDuration = 2.5f;
        
        [SerializeField]
        private float fadeInDuration = 0.3f;
        
        [SerializeField]
        private float fadeOutDuration = 0.5f;
        
        [SerializeField]
        private float scaleFrom = 0.5f;
        
        [SerializeField]
        private float scaleTo = 1f;
        
        [SerializeField]
        private float bounceAmount = 0.1f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private bool isShowing = false;
        private Coroutine hideCoroutine;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when popup is shown
        /// </summary>
        public event Action<float> OnPopupShown;
        
        /// <summary>
        /// Fired when popup is dismissed
        /// </summary>
        public event Action OnPopupDismissed;
        
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
            // Start hidden
            Hide(immediate: true);
            
            // Subscribe to collection events
            if (CollectionService.Exists)
            {
                CollectionService.Instance.OnCollectionSuccess += HandleCollectionSuccess;
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
                CollectionService.Instance.OnCollectionSuccess -= HandleCollectionSuccess;
            }
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the collection popup
        /// </summary>
        public void Show(float value, string message, CoinTier tier = CoinTier.Bronze)
        {
            Log($"Showing collection popup: ${value:F2}");
            
            // Cancel any pending hide
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
            
            isShowing = true;
            
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }
            
            // Update content
            UpdateContent(value, message, tier);
            
            // Play animation
            StartCoroutine(ShowAnimation());
            
            // Auto-hide after duration
            hideCoroutine = StartCoroutine(AutoHide());
            
            OnPopupShown?.Invoke(value);
        }
        
        /// <summary>
        /// Show with CollectionResult
        /// </summary>
        public void Show(CollectionResult result)
        {
            if (result == null || !result.success) return;
            
            Show(result.value, result.message, result.tier);
        }
        
        /// <summary>
        /// Hide the popup
        /// </summary>
        public void Hide(bool immediate = false)
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
            
            if (immediate)
            {
                isShowing = false;
                if (popupPanel != null) popupPanel.SetActive(false);
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
        private void UpdateContent(float value, string message, CoinTier tier)
        {
            // Value text with formatting
            if (valueText != null)
            {
                valueText.text = $"+${value:F2}";
                valueText.color = GetTierColor(tier);
            }
            
            // Message
            if (messageText != null)
            {
                messageText.text = message;
            }
            
            // Pending notice
            if (pendingText != null)
            {
                pendingText.text = "Pending confirmation (24h)";
                pendingText.gameObject.SetActive(true);
            }
            
            // Tier color effects
            Color tierColor = GetTierColor(tier);
            
            if (glowEffect != null)
            {
                glowEffect.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.5f);
            }
            
            if (coinIcon != null)
            {
                coinIcon.color = tierColor;
            }
        }
        
        /// <summary>
        /// Get color for coin tier
        /// </summary>
        private Color GetTierColor(CoinTier tier)
        {
            return tier switch
            {
                CoinTier.Bronze => bronzeColor,
                CoinTier.Silver => silverColor,
                CoinTier.Gold => goldColor,
                CoinTier.Platinum => platinumColor,
                CoinTier.Diamond => diamondColor,
                _ => goldColor
            };
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// Show animation coroutine
        /// </summary>
        private IEnumerator ShowAnimation()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (popupRect != null) popupRect.localScale = Vector3.one * scaleFrom;
            
            float elapsed = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                
                // Ease out with overshoot
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                float bounceT = easedT + Mathf.Sin(easedT * Mathf.PI) * bounceAmount;
                
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = easedT;
                }
                
                if (popupRect != null)
                {
                    float scale = Mathf.Lerp(scaleFrom, scaleTo, bounceT);
                    popupRect.localScale = Vector3.one * scale;
                }
                
                yield return null;
            }
            
            // Ensure final state
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (popupRect != null) popupRect.localScale = Vector3.one * scaleTo;
            
            // Continue with subtle pulse
            StartCoroutine(PulseAnimation());
        }
        
        /// <summary>
        /// Hide animation coroutine
        /// </summary>
        private IEnumerator HideAnimation()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup?.alpha ?? 1f;
            Vector3 startScale = popupRect?.localScale ?? Vector3.one;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                }
                
                if (popupRect != null)
                {
                    float scale = Mathf.Lerp(startScale.x, scaleFrom, t);
                    popupRect.localScale = Vector3.one * scale;
                }
                
                yield return null;
            }
            
            isShowing = false;
            if (popupPanel != null) popupPanel.SetActive(false);
            
            OnPopupDismissed?.Invoke();
        }
        
        /// <summary>
        /// Subtle pulse animation while showing
        /// </summary>
        private IEnumerator PulseAnimation()
        {
            while (isShowing)
            {
                float scale = scaleTo + Mathf.Sin(Time.time * 2f) * 0.02f;
                
                if (popupRect != null)
                {
                    popupRect.localScale = Vector3.one * scale;
                }
                
                yield return null;
            }
        }
        
        /// <summary>
        /// Auto-hide after duration
        /// </summary>
        private IEnumerator AutoHide()
        {
            yield return new WaitForSeconds(showDuration);
            Hide();
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleCollectionSuccess(CollectionResult result)
        {
            Show(result);
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[CollectionPopup] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Show test popup
        /// </summary>
        [ContextMenu("Debug: Show Bronze ($0.50)")]
        public void DebugShowBronze()
        {
            Show(0.50f, "Every coin counts on the high seas!", CoinTier.Bronze);
        }
        
        /// <summary>
        /// Debug: Show gold popup
        /// </summary>
        [ContextMenu("Debug: Show Gold ($5.00)")]
        public void DebugShowGold()
        {
            Show(5.00f, "GREAT FIND! Yer treasure grows!", CoinTier.Gold);
        }
        
        /// <summary>
        /// Debug: Show diamond popup
        /// </summary>
        [ContextMenu("Debug: Show Diamond ($100.00)")]
        public void DebugShowDiamond()
        {
            Show(100.00f, "LEGENDARY FIND! Ye've struck gold, Captain!", CoinTier.Diamond);
        }
        
        #endregion
    }
}
