// ============================================================================
// LowGasWarning.cs
// Black Bart's Gold - Low Gas Warning Banner
// Path: Assets/Scripts/UI/LowGasWarning.cs
// ============================================================================
// Warning banner displayed at the top of AR screen when gas is low (but not
// empty). Dismissible once per session.
// Reference: BUILD-GUIDE.md Sprint 7, Prompt 7.2
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using BlackBartsGold.Economy;
using BlackBartsGold.Utils;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Low gas warning banner controller.
    /// Shows flashing warning when fuel is below 15%.
    /// Dismissible once per session.
    /// </summary>
    public class LowGasWarning : MonoBehaviour
    {
        #region Singleton
        
        private static LowGasWarning _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static LowGasWarning Instance => _instance;
        
        /// <summary>
        /// Check if instance exists
        /// </summary>
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Banner Elements")]
        [SerializeField]
        private GameObject bannerPanel;
        
        [SerializeField]
        private RectTransform bannerRect;
        
        [SerializeField]
        private CanvasGroup canvasGroup;
        
        [Header("Text Elements")]
        [SerializeField]
        private TMP_Text warningText;
        
        [SerializeField]
        private TMP_Text daysText;
        
        [Header("Buttons")]
        [SerializeField]
        private Button dismissButton;
        
        [SerializeField]
        private Button addGasButton;
        
        [Header("Visual Elements")]
        [SerializeField]
        private Image backgroundImage;
        
        [SerializeField]
        private Image warningIcon;
        
        [Header("Colors")]
        [SerializeField]
        private Color warningColor = new Color(0.94f, 0.27f, 0.27f); // Red
        
        [SerializeField]
        private Color flashColor = new Color(1f, 0.5f, 0.1f); // Orange
        
        [Header("Animation")]
        [SerializeField]
        private float slideInDuration = 0.3f;
        
        [SerializeField]
        private float flashSpeed = 2f;
        
        [SerializeField]
        private float hiddenYOffset = 100f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private bool isVisible = false;
        private bool isDismissed = false;
        private Coroutine flashCoroutine;
        private Vector2 shownPosition;
        private Vector2 hiddenPosition;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when banner is dismissed
        /// </summary>
        public event Action OnDismissed;
        
        /// <summary>
        /// Fired when add gas is clicked
        /// </summary>
        public event Action OnAddGasClicked;
        
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
            SetupPositions();
            
            // Start hidden
            Hide(immediate: true);
            
            // Subscribe to gas events
            if (GasService.Exists)
            {
                GasService.Instance.OnGasLow += HandleGasLow;
                GasService.Instance.OnGasStatusChanged += HandleGasStatusChanged;
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            if (GasService.Exists)
            {
                GasService.Instance.OnGasLow -= HandleGasLow;
                GasService.Instance.OnGasStatusChanged -= HandleGasStatusChanged;
            }
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupListeners()
        {
            if (dismissButton != null)
            {
                dismissButton.onClick.AddListener(OnDismissClicked);
            }
            
            if (addGasButton != null)
            {
                addGasButton.onClick.AddListener(OnAddGasButtonClicked);
            }
        }
        
        /// <summary>
        /// Setup show/hide positions
        /// </summary>
        private void SetupPositions()
        {
            if (bannerRect != null)
            {
                shownPosition = bannerRect.anchoredPosition;
                hiddenPosition = shownPosition + new Vector2(0f, hiddenYOffset);
            }
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the warning banner
        /// </summary>
        public void Show(int daysRemaining)
        {
            if (isDismissed)
            {
                Log("Warning dismissed for this session");
                return;
            }
            
            Log($"Showing low gas warning: {daysRemaining} days");
            
            isVisible = true;
            
            if (bannerPanel != null)
            {
                bannerPanel.SetActive(true);
            }
            
            UpdateText(daysRemaining);
            StartFlashing();
            
            // Slide in
            StartCoroutine(SlideIn());
        }
        
        /// <summary>
        /// Show based on current gas status
        /// </summary>
        public void ShowIfNeeded()
        {
            if (isDismissed) return;
            
            if (GasService.Exists)
            {
                var status = GasService.Instance.GetGasStatus();
                if (status.isLow && !status.isEmpty)
                {
                    Show(status.daysLeft);
                }
            }
        }
        
        /// <summary>
        /// Hide the warning banner
        /// </summary>
        public void Hide(bool immediate = false)
        {
            Log("Hiding low gas warning");
            
            isVisible = false;
            StopFlashing();
            
            if (immediate)
            {
                if (bannerPanel != null)
                {
                    bannerPanel.SetActive(false);
                }
                if (bannerRect != null)
                {
                    bannerRect.anchoredPosition = hiddenPosition;
                }
            }
            else
            {
                StartCoroutine(SlideOut());
            }
        }
        
        /// <summary>
        /// Dismiss for this session
        /// </summary>
        public void Dismiss()
        {
            Log("Dismissing low gas warning");
            
            isDismissed = true;
            
            if (GasService.Exists)
            {
                GasService.Instance.DismissWarning();
            }
            
            Hide();
            OnDismissed?.Invoke();
        }
        
        /// <summary>
        /// Check if visible
        /// </summary>
        public bool IsVisible => isVisible;
        
        /// <summary>
        /// Check if dismissed
        /// </summary>
        public bool IsDismissed => isDismissed;
        
        #endregion
        
        #region Button Handlers
        
        private void OnDismissClicked()
        {
            Log("Dismiss clicked");
            Dismiss();
        }
        
        private void OnAddGasButtonClicked()
        {
            Log("Add gas clicked");
            OnAddGasClicked?.Invoke();
            
            // Navigate to wallet
            Core.SceneLoader.LoadScene(Core.SceneNames.Wallet);
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// Update warning text
        /// </summary>
        private void UpdateText(int daysRemaining)
        {
            if (warningText != null)
            {
                warningText.text = EmojiHelper.Sanitize("⚠️ LOW FUEL");
            }
            
            if (daysText != null)
            {
                if (daysRemaining <= 1)
                {
                    daysText.text = "LAST DAY OF FUEL!";
                }
                else
                {
                    daysText.text = $"{daysRemaining} days remaining";
                }
            }
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// Slide in animation
        /// </summary>
        private IEnumerator SlideIn()
        {
            if (bannerRect == null) yield break;
            
            bannerRect.anchoredPosition = hiddenPosition;
            
            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideInDuration;
                
                // Ease out
                t = 1f - Mathf.Pow(1f - t, 3f);
                
                bannerRect.anchoredPosition = Vector2.Lerp(hiddenPosition, shownPosition, t);
                yield return null;
            }
            
            bannerRect.anchoredPosition = shownPosition;
        }
        
        /// <summary>
        /// Slide out animation
        /// </summary>
        private IEnumerator SlideOut()
        {
            if (bannerRect == null)
            {
                if (bannerPanel != null) bannerPanel.SetActive(false);
                yield break;
            }
            
            Vector2 startPos = bannerRect.anchoredPosition;
            
            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideInDuration;
                
                bannerRect.anchoredPosition = Vector2.Lerp(startPos, hiddenPosition, t);
                yield return null;
            }
            
            bannerRect.anchoredPosition = hiddenPosition;
            
            if (bannerPanel != null)
            {
                bannerPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Start flashing animation
        /// </summary>
        private void StartFlashing()
        {
            StopFlashing();
            flashCoroutine = StartCoroutine(FlashAnimation());
        }
        
        /// <summary>
        /// Stop flashing animation
        /// </summary>
        private void StopFlashing()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            
            // Reset color
            if (backgroundImage != null)
            {
                backgroundImage.color = warningColor;
            }
        }
        
        /// <summary>
        /// Flash animation coroutine
        /// </summary>
        private IEnumerator FlashAnimation()
        {
            while (isVisible)
            {
                float t = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;
                
                if (backgroundImage != null)
                {
                    backgroundImage.color = Color.Lerp(warningColor, flashColor, t);
                }
                
                if (warningIcon != null)
                {
                    float scale = 1f + (t * 0.1f);
                    warningIcon.transform.localScale = new Vector3(scale, scale, 1f);
                }
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleGasLow(GasStatusInfo status)
        {
            if (!isDismissed)
            {
                Show(status.daysLeft);
            }
        }
        
        private void HandleGasStatusChanged(GasStatusInfo status)
        {
            if (status.isEmpty || !status.isLow)
            {
                // Gas is either empty or no longer low
                Hide();
            }
            else if (status.isLow && !isDismissed)
            {
                Show(status.daysLeft);
            }
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[LowGasWarning] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Force show
        /// </summary>
        [ContextMenu("Debug: Show Warning")]
        public void DebugShow()
        {
            isDismissed = false;
            Show(3);
        }
        
        /// <summary>
        /// Debug: Reset dismissed state
        /// </summary>
        [ContextMenu("Debug: Reset Dismissed")]
        public void DebugResetDismissed()
        {
            isDismissed = false;
            Debug.Log("[LowGasWarning] Dismissed state reset");
        }
        
        #endregion
    }
}
