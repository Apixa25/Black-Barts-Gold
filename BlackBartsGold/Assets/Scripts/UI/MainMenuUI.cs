// ============================================================================
// MainMenuUI.cs
// Black Bart's Gold - Main Menu Screen Controller
// Path: Assets/Scripts/UI/MainMenuUI.cs
// ============================================================================
// Controls the main menu/home screen. Displays player stats, navigation
// buttons, and handles scene transitions.
// Reference: BUILD-GUIDE.md Prompt 5.2
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Utils;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Main menu screen controller.
    /// Displays player info and provides navigation to all game screens.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Header Section")]
        [SerializeField]
        [Tooltip("Game title text")]
        private TMP_Text titleText;
        
        [SerializeField]
        [Tooltip("Player balance display")]
        private TMP_Text balanceText;
        
        [SerializeField]
        [Tooltip("Gas status display")]
        private TMP_Text gasStatusText;
        
        [SerializeField]
        [Tooltip("Player name display")]
        private TMP_Text playerNameText;
        
        [Header("Main Action Buttons")]
        [SerializeField]
        private Button startHuntingButton;
        
        [SerializeField]
        private TMP_Text startHuntingText;
        
        [SerializeField]
        private Button treasureMapButton;
        
        [SerializeField]
        private Button walletButton;
        
        [SerializeField]
        private Button settingsButton;
        
        [Header("Quick Stats")]
        [SerializeField]
        private TMP_Text coinsFoundText;
        
        [SerializeField]
        private TMP_Text findLimitText;
        
        [SerializeField]
        private TMP_Text coinsHiddenText;
        
        [SerializeField]
        private TMP_Text tierText;
        
        [Header("No Gas Panel")]
        [SerializeField]
        private GameObject noGasPanel;
        
        [SerializeField]
        private Button buyGasButton;
        
        [Header("Loading")]
        [SerializeField]
        private GameObject loadingPanel;
        
        [Header("Styling")]
        [SerializeField]
        private Color goldColor = new Color(1f, 0.84f, 0f);
        
        [SerializeField]
        private Color warningColor = new Color(0.98f, 0.75f, 0.14f);
        
        [SerializeField]
        private Color dangerColor = new Color(0.94f, 0.27f, 0.27f);
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private bool canHunt = true;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Setup button listeners
            SetupButtons();
            
            // Load player data
            LoadPlayerData();
            
            // Subscribe to data changes
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnDataChanged += RefreshUI;
            }
            
            // Hide loading
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnDataChanged -= RefreshUI;
            }
        }
        
        private void OnEnable()
        {
            // Refresh when screen becomes visible
            RefreshUI();
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Setup button click listeners
        /// </summary>
        private void SetupButtons()
        {
            if (startHuntingButton != null)
            {
                startHuntingButton.onClick.AddListener(OnStartHuntingClicked);
            }
            
            if (treasureMapButton != null)
            {
                treasureMapButton.onClick.AddListener(OnTreasureMapClicked);
            }
            
            if (walletButton != null)
            {
                walletButton.onClick.AddListener(OnWalletClicked);
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }
            
            if (buyGasButton != null)
            {
                buyGasButton.onClick.AddListener(OnBuyGasClicked);
            }
        }
        
        /// <summary>
        /// Load player data
        /// </summary>
        private void LoadPlayerData()
        {
            if (!PlayerData.Exists || !PlayerData.Instance.IsDataLoaded)
            {
                // Try to load saved data
                PlayerData.Instance.LoadData();
            }
            
            // If still no data, initialize test data for development
            if (!PlayerData.Instance.IsDataLoaded)
            {
                Log("No player data, initializing test data");
                PlayerData.Instance.InitializeTestData();
            }
            
            RefreshUI();
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// Refresh all UI elements
        /// </summary>
        public void RefreshUI()
        {
            if (!PlayerData.Exists) return;
            
            var player = PlayerData.Instance;
            
            // Update balance
            if (balanceText != null)
            {
                balanceText.text = $"${player.Balance:F2} BBG";
                balanceText.color = goldColor;
            }
            
            // Update gas status
            UpdateGasStatus(player.GasDays);
            
            // Update player name
            if (playerNameText != null)
            {
                playerNameText.text = $"Ahoy, {player.DisplayName}!";
            }
            
            // Update quick stats
            UpdateQuickStats();
            
            // Update hunt button state
            UpdateHuntButtonState(player.CanPlay);
            
            Log("UI refreshed");
        }
        
        /// <summary>
        /// Update gas status display
        /// </summary>
        private void UpdateGasStatus(float daysRemaining)
        {
            if (gasStatusText == null) return;
            
            if (daysRemaining <= 0)
            {
                gasStatusText.text = EmojiHelper.Sanitize("⛽ OUT OF GAS!");
                gasStatusText.color = dangerColor;
            }
            else if (daysRemaining < 5)
            {
                gasStatusText.text = EmojiHelper.Sanitize($"⛽ {daysRemaining:F1} days remaining");
                gasStatusText.color = warningColor;
            }
            else
            {
                gasStatusText.text = EmojiHelper.Sanitize($"⛽ {daysRemaining:F0} days remaining");
                gasStatusText.color = Color.white;
            }
        }
        
        /// <summary>
        /// Update quick stats section
        /// </summary>
        private void UpdateQuickStats()
        {
            if (!PlayerData.Exists) return;
            
            var player = PlayerData.Instance;
            var user = player.CurrentUser;
            
            if (coinsFoundText != null)
            {
                int found = user?.stats?.totalCoinsFound ?? 0;
                coinsFoundText.text = $"Coins Found: {found}";
            }
            
            if (findLimitText != null)
            {
                findLimitText.text = $"Find Limit: ${player.FindLimit:F2}";
                findLimitText.color = GetTierColor(player.FindLimit);
            }
            
            if (coinsHiddenText != null)
            {
                int hidden = user?.stats?.totalCoinsHidden ?? 0;
                coinsHiddenText.text = $"Hidden: {hidden} coins";
            }
            
            if (tierText != null)
            {
                tierText.text = player.TierName;
                tierText.color = GetTierColor(player.FindLimit);
            }
        }
        
        /// <summary>
        /// Update hunt button state based on gas
        /// </summary>
        private void UpdateHuntButtonState(bool canPlay)
        {
            canHunt = canPlay;
            
            if (startHuntingButton != null)
            {
                startHuntingButton.interactable = canPlay;
            }
            
            if (startHuntingText != null)
            {
                startHuntingText.text = EmojiHelper.Sanitize(canPlay ? "🏴‍☠️ Start Hunting" : "⛽ Out of Gas");
            }
            
            if (noGasPanel != null)
            {
                noGasPanel.SetActive(!canPlay);
            }
        }
        
        /// <summary>
        /// Get color for tier based on find limit
        /// </summary>
        private Color GetTierColor(float limit)
        {
            if (limit >= 100f) return new Color(1f, 0.4f, 0.7f);      // King - Pink
            if (limit >= 50f) return new Color(0.5f, 0.8f, 1f);       // Legend - Diamond
            if (limit >= 25f) return new Color(0.9f, 0.9f, 0.95f);    // Captain - Platinum
            if (limit >= 10f) return goldColor;                        // Hunter - Gold
            if (limit >= 5f) return new Color(0.75f, 0.75f, 0.75f);   // Deck Hand - Silver
            return new Color(0.8f, 0.5f, 0.2f);                        // Cabin Boy - Bronze
        }
        
        #endregion
        
        #region Button Handlers
        
        /// <summary>
        /// Handle Start Hunting button click
        /// </summary>
        private void OnStartHuntingClicked()
        {
            Log("Start Hunting clicked");
            
            if (!canHunt)
            {
                // Show no gas message
                ShowNoGasMessage();
                return;
            }
            
            // Load AR Hunt scene
            LoadScene(SceneNames.ARHunt);
        }
        
        /// <summary>
        /// Handle Treasure Map button click
        /// </summary>
        private void OnTreasureMapClicked()
        {
            Log("Treasure Map clicked");
            LoadScene(SceneNames.Map);
        }
        
        /// <summary>
        /// Handle Wallet button click
        /// </summary>
        private void OnWalletClicked()
        {
            Log("Wallet clicked");
            LoadScene(SceneNames.Wallet);
        }
        
        /// <summary>
        /// Handle Settings button click
        /// </summary>
        private void OnSettingsClicked()
        {
            Log("Settings clicked");
            LoadScene(SceneNames.Settings);
        }
        
        /// <summary>
        /// Handle Buy Gas button click
        /// </summary>
        private void OnBuyGasClicked()
        {
            Log("Buy Gas clicked");
            // For now, just go to wallet
            // TODO: Implement purchase flow
            LoadScene(SceneNames.Wallet);
        }
        
        #endregion
        
        #region Navigation
        
        /// <summary>
        /// Load a scene
        /// </summary>
        private void LoadScene(SceneNames scene)
        {
            // Show loading
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }
            
            // Use SceneLoader
            SceneLoader.LoadScene(scene);
        }
        
        #endregion
        
        #region Messages
        
        /// <summary>
        /// Show no gas message
        /// </summary>
        private void ShowNoGasMessage()
        {
            if (noGasPanel != null)
            {
                noGasPanel.SetActive(true);
            }
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[MainMenuUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print state
        /// </summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== MainMenu State ===");
            Debug.Log($"Can Hunt: {canHunt}");
            if (PlayerData.Exists)
            {
                Debug.Log($"Balance: ${PlayerData.Instance.Balance:F2}");
                Debug.Log($"Gas: {PlayerData.Instance.GasDays:F1} days");
                Debug.Log($"Find Limit: ${PlayerData.Instance.FindLimit:F2}");
            }
            Debug.Log("======================");
        }
        
        /// <summary>
        /// Debug: Refresh UI
        /// </summary>
        [ContextMenu("Debug: Refresh UI")]
        public void DebugRefreshUI()
        {
            RefreshUI();
        }
        
        /// <summary>
        /// Debug: Set no gas
        /// </summary>
        [ContextMenu("Debug: Set No Gas")]
        public void DebugSetNoGas()
        {
            if (PlayerData.Exists && PlayerData.Instance.Wallet != null)
            {
                PlayerData.Instance.Wallet.gasTank = 0;
                RefreshUI();
            }
        }
        
        #endregion
    }
}
