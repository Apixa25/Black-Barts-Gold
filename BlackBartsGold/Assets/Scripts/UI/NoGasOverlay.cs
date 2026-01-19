// ============================================================================
// NoGasOverlay.cs
// Black Bart's Gold - No Gas Full Screen Overlay
// Path: Assets/Scripts/UI/NoGasOverlay.cs
// ============================================================================
// Full-screen overlay shown when player has no gas. Blocks gameplay and
// provides options to buy gas or unpark coins.
// Reference: BUILD-GUIDE.md Sprint 7, Prompt 7.2
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Economy;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// No gas overlay controller.
    /// Displays full screen blocking overlay when player has no fuel.
    /// </summary>
    public class NoGasOverlay : MonoBehaviour
    {
        #region Singleton
        
        private static NoGasOverlay _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static NoGasOverlay Instance => _instance;
        
        /// <summary>
        /// Check if instance exists
        /// </summary>
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Main Panel")]
        [SerializeField]
        private GameObject overlayPanel;
        
        [SerializeField]
        private CanvasGroup canvasGroup;
        
        [Header("Text Elements")]
        [SerializeField]
        private TMP_Text titleText;
        
        [SerializeField]
        private TMP_Text messageText;
        
        [SerializeField]
        private TMP_Text parkedBalanceText;
        
        [Header("Buttons")]
        [SerializeField]
        private Button buyGasButton;
        
        [SerializeField]
        private TMP_Text buyGasButtonText;
        
        [SerializeField]
        private Button unparkButton;
        
        [SerializeField]
        private TMP_Text unparkButtonText;
        
        [SerializeField]
        private Button mainMenuButton;
        
        [Header("Visual Elements")]
        [SerializeField]
        private Image shipImage;
        
        [SerializeField]
        private Image backgroundImage;
        
        [Header("Animation")]
        [SerializeField]
        private float fadeInDuration = 0.5f;
        
        [SerializeField]
        private float bobAmplitude = 10f;
        
        [SerializeField]
        private float bobSpeed = 1f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private bool isVisible = false;
        private float bobTime = 0f;
        private Vector3 shipStartPos;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when buy gas is clicked
        /// </summary>
        public event Action OnBuyGasClicked;
        
        /// <summary>
        /// Fired when unpark is clicked
        /// </summary>
        public event Action OnUnparkClicked;
        
        /// <summary>
        /// Fired when main menu is clicked
        /// </summary>
        public event Action OnMainMenuClicked;
        
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
            
            // Store ship start position for bobbing
            if (shipImage != null)
            {
                shipStartPos = shipImage.transform.localPosition;
            }
            
            // Start hidden
            Hide(immediate: true);
            
            // Subscribe to gas events
            if (GasService.Exists)
            {
                GasService.Instance.OnGasEmpty += HandleGasEmpty;
                GasService.Instance.OnGasRefilled += HandleGasRefilled;
            }
        }
        
        private void Update()
        {
            if (isVisible)
            {
                AnimateShip();
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
                GasService.Instance.OnGasEmpty -= HandleGasEmpty;
                GasService.Instance.OnGasRefilled -= HandleGasRefilled;
            }
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupListeners()
        {
            if (buyGasButton != null)
            {
                buyGasButton.onClick.AddListener(OnBuyGasButtonClicked);
            }
            
            if (unparkButton != null)
            {
                unparkButton.onClick.AddListener(OnUnparkButtonClicked);
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
            }
        }
        
        /// <summary>
        /// Setup content text
        /// </summary>
        private void SetupContent()
        {
            if (titleText != null)
            {
                titleText.text = "⚓ Ye've Run Aground, Matey!";
            }
            
            if (messageText != null)
            {
                messageText.text = "Yer ship be out of fuel!\nAdd more gas to continue the hunt for treasure!";
            }
            
            if (buyGasButtonText != null)
            {
                buyGasButtonText.text = "⛽ Buy More Gas";
            }
            
            if (unparkButtonText != null)
            {
                unparkButtonText.text = "🅿️ Use Parked Coins";
            }
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the overlay
        /// </summary>
        public void Show()
        {
            Log("Showing no gas overlay");
            
            isVisible = true;
            
            if (overlayPanel != null)
            {
                overlayPanel.SetActive(true);
            }
            
            UpdateParkedBalance();
            
            // Fade in
            if (canvasGroup != null)
            {
                StartCoroutine(FadeIn());
            }
        }
        
        /// <summary>
        /// Hide the overlay
        /// </summary>
        public void Hide(bool immediate = false)
        {
            Log("Hiding no gas overlay");
            
            isVisible = false;
            
            if (immediate)
            {
                if (overlayPanel != null)
                {
                    overlayPanel.SetActive(false);
                }
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }
        
        /// <summary>
        /// Check if overlay is visible
        /// </summary>
        public bool IsVisible => isVisible;
        
        #endregion
        
        #region Button Handlers
        
        private void OnBuyGasButtonClicked()
        {
            Log("Buy gas clicked");
            OnBuyGasClicked?.Invoke();
            
            // Navigate to wallet for now (purchase flow stub)
            SceneLoader.LoadScene(SceneNames.Wallet);
        }
        
        private void OnUnparkButtonClicked()
        {
            Log("Unpark clicked");
            OnUnparkClicked?.Invoke();
            
            // Navigate to wallet
            SceneLoader.LoadScene(SceneNames.Wallet);
        }
        
        private void OnMainMenuButtonClicked()
        {
            Log("Main menu clicked");
            OnMainMenuClicked?.Invoke();
            
            Hide();
            SceneLoader.LoadScene(SceneNames.MainMenu);
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// Update parked balance display
        /// </summary>
        private void UpdateParkedBalance()
        {
            if (parkedBalanceText == null) return;
            
            float parked = 0f;
            
            if (WalletService.Exists)
            {
                parked = WalletService.Instance.GetParkedBalance();
            }
            else if (PlayerData.Exists && PlayerData.Instance.Wallet != null)
            {
                parked = PlayerData.Instance.Wallet.parked;
            }
            
            if (parked > 0)
            {
                parkedBalanceText.text = $"Ye have ${parked:F2} parked that could fuel yer ship!";
                parkedBalanceText.gameObject.SetActive(true);
                
                if (unparkButton != null)
                {
                    unparkButton.gameObject.SetActive(true);
                    unparkButton.interactable = parked > GasService.DAILY_RATE;
                }
            }
            else
            {
                parkedBalanceText.text = "No parked coins available";
                parkedBalanceText.gameObject.SetActive(false);
                
                if (unparkButton != null)
                {
                    unparkButton.gameObject.SetActive(false);
                }
            }
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// Fade in animation
        /// </summary>
        private System.Collections.IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            
            float elapsed = 0f;
            canvasGroup.alpha = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        /// <summary>
        /// Fade out animation
        /// </summary>
        private System.Collections.IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                if (overlayPanel != null) overlayPanel.SetActive(false);
                yield break;
            }
            
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeInDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            
            if (overlayPanel != null)
            {
                overlayPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Animate ship bobbing
        /// </summary>
        private void AnimateShip()
        {
            if (shipImage == null) return;
            
            bobTime += Time.deltaTime * bobSpeed;
            float yOffset = Mathf.Sin(bobTime) * bobAmplitude;
            
            shipImage.transform.localPosition = shipStartPos + new Vector3(0f, yOffset, 0f);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleGasEmpty()
        {
            Show();
        }
        
        private void HandleGasRefilled(float amount)
        {
            if (GasService.Exists && GasService.Instance.CanPlay())
            {
                Hide();
            }
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[NoGasOverlay] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Force show overlay
        /// </summary>
        [ContextMenu("Debug: Show Overlay")]
        public void DebugShow()
        {
            Show();
        }
        
        /// <summary>
        /// Debug: Force hide overlay
        /// </summary>
        [ContextMenu("Debug: Hide Overlay")]
        public void DebugHide()
        {
            Hide();
        }
        
        #endregion
    }
}
