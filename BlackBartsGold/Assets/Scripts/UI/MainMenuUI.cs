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
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
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

        [SerializeField]
        private Button profileButton;
        
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
        private GameObject profilePanel;
        private TMP_InputField profileDisplayNameInput;
        private TMP_Text profileAgeText;
        private TMP_Text profileAvatarPresetText;
        private TMP_Text profileWalletHintText;
        private Button profilePrevAvatarButton;
        private Button profileNextAvatarButton;
        private Button profileSaveButton;
        private Button profileCloseButton;
        private Button profileSkipButton;
        private int selectedAvatarPresetIndex;

        private static readonly List<string> AvatarPresetIds = new List<string>
        {
            "outlaw-hat-01",
            "outlaw-bandana-02",
            "stagecoach-scout-03",
            "desert-ranger-04",
            "gold-rush-05",
            "frontier-captain-06"
        };
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            EnsureProfileUi();

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

            MaybeOpenProfileOnboarding();
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

            if (profileButton != null)
            {
                profileButton.onClick.RemoveAllListeners();
                profileButton.onClick.AddListener(OnProfileClicked);
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
                gasStatusText.text = "OUT OF GAS!";
                gasStatusText.color = dangerColor;
            }
            else if (daysRemaining < 5)
            {
                gasStatusText.text = $"{daysRemaining:F1} days remaining";
                gasStatusText.color = warningColor;
            }
            else
            {
                gasStatusText.text = $"{daysRemaining:F0} days remaining";
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
                startHuntingText.text = canPlay ? "Start Hunting" : "Out of Gas";
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
        /// Handle Profile button click
        /// </summary>
        private void OnProfileClicked()
        {
            Log("Profile clicked");
            OpenProfilePanel(false);
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

        #region Profile UI

        private void EnsureProfileUi()
        {
            if (profileButton == null)
            {
                profileButton = CreateMainMenuButton("ProfileButton", "\u263a PROFILE", new Vector2(0, -290), new Vector2(550, 90));
            }

            if (profilePanel == null)
            {
                profilePanel = BuildProfilePanel();
            }
        }

        private Button CreateMainMenuButton(string objectName, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var buttonGo = new GameObject(objectName);
            buttonGo.transform.SetParent(transform, false);

            var rect = buttonGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.961f, 0.902f, 0.827f, 1f);

            var button = buttonGo.AddComponent<Button>();

            var textGo = new GameObject("ButtonText");
            textGo.transform.SetParent(buttonGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 32;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.239f, 0.161f, 0.078f, 1f);
            text.raycastTarget = false;

            return button;
        }

        private GameObject BuildProfilePanel()
        {
            var panelGo = new GameObject("ProfilePanel");
            panelGo.transform.SetParent(transform, false);

            var rect = panelGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(860, 980);

            var image = panelGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.9f);

            CreateLabel(panelGo.transform, "Title", "Player Profile", new Vector2(0, -50), new Vector2(760, 70), 46, goldColor, TextAlignmentOptions.Center);
            CreateLabel(panelGo.transform, "IdentityHeader", "Identity", new Vector2(-300, -130), new Vector2(300, 40), 32, goldColor, TextAlignmentOptions.Left);

            CreateLabel(panelGo.transform, "DisplayNameLabel", "Display Name", new Vector2(-300, -190), new Vector2(260, 40), 26, Color.white, TextAlignmentOptions.Left);
            profileDisplayNameInput = CreateInputField(panelGo.transform, "DisplayNameInput", new Vector2(0, -245), new Vector2(620, 72));

            CreateLabel(panelGo.transform, "AgeLabel", "Age", new Vector2(-300, -325), new Vector2(260, 40), 26, Color.white, TextAlignmentOptions.Left);
            profileAgeText = CreateLabel(panelGo.transform, "AgeValue", "-", new Vector2(0, -325), new Vector2(620, 48), 30, Color.white, TextAlignmentOptions.Left);

            CreateLabel(panelGo.transform, "AvatarLabel", "Avatar Preset", new Vector2(-300, -390), new Vector2(260, 40), 26, Color.white, TextAlignmentOptions.Left);
            profilePrevAvatarButton = CreatePanelButton(panelGo.transform, "AvatarPrevButton", "<", new Vector2(-210, -445), new Vector2(70, 56), 32);
            profileAvatarPresetText = CreateLabel(panelGo.transform, "AvatarPresetValue", "-", new Vector2(0, -445), new Vector2(420, 56), 28, Color.white, TextAlignmentOptions.Center);
            profileNextAvatarButton = CreatePanelButton(panelGo.transform, "AvatarNextButton", ">", new Vector2(210, -445), new Vector2(70, 56), 32);

            profileWalletHintText = CreateLabel(
                panelGo.transform,
                "WalletHint",
                "Wallet balances and transactions now live in MY WALLET.",
                new Vector2(0, -585),
                new Vector2(700, 90),
                24,
                new Color(0.9f, 0.9f, 0.9f, 1f),
                TextAlignmentOptions.Center
            );

            profileSaveButton = CreatePanelButton(panelGo.transform, "SaveProfileButton", "Save Profile", new Vector2(-180, -840), new Vector2(240, 72), 30);
            profileCloseButton = CreatePanelButton(panelGo.transform, "CloseProfileButton", "Close", new Vector2(95, -840), new Vector2(180, 72), 30);
            profileSkipButton = CreatePanelButton(panelGo.transform, "SkipProfileButton", "Skip For Now", new Vector2(320, -840), new Vector2(240, 72), 24);

            profilePrevAvatarButton.onClick.RemoveAllListeners();
            profilePrevAvatarButton.onClick.AddListener(OnAvatarPrevClicked);
            profileNextAvatarButton.onClick.RemoveAllListeners();
            profileNextAvatarButton.onClick.AddListener(OnAvatarNextClicked);
            profileSaveButton.onClick.RemoveAllListeners();
            profileSaveButton.onClick.AddListener(OnProfileSaveClicked);
            profileCloseButton.onClick.RemoveAllListeners();
            profileCloseButton.onClick.AddListener(CloseProfilePanel);
            profileSkipButton.onClick.RemoveAllListeners();
            profileSkipButton.onClick.AddListener(OnProfileSkipClicked);

            panelGo.SetActive(false);
            return panelGo;
        }

        private TMP_Text CreateLabel(Transform parent, string name, string textValue, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = textValue;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private TMP_InputField CreateInputField(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            var input = go.AddComponent<TMP_InputField>();

            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholderRect = placeholderGo.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(16, 10);
            placeholderRect.offsetMax = new Vector2(-16, -10);
            var placeholderText = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Enter display name";
            placeholderText.fontSize = 28;
            placeholderText.color = new Color(0.75f, 0.75f, 0.75f, 0.75f);
            placeholderText.alignment = TextAlignmentOptions.Left;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 10);
            textRect.offsetMax = new Vector2(-16, -10);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 30;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;

            input.textViewport = rect;
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.characterLimit = 20;

            return input;
        }

        private Button CreatePanelButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var button = go.AddComponent<Button>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            return button;
        }

        private void MaybeOpenProfileOnboarding()
        {
            var user = PlayerData.Exists ? PlayerData.Instance.CurrentUser : null;
            if (user == null) return;
            if (user.profileOnboardingDismissed) return;
            if (user.IsProfileComplete()) return;

            OpenProfilePanel(true);
        }

        private void OpenProfilePanel(bool onboarding)
        {
            EnsureProfileUi();
            if (profilePanel == null || !PlayerData.Exists || PlayerData.Instance.CurrentUser == null) return;

            profilePanel.SetActive(true);
            UpdateProfileActionLayout(onboarding);

            var user = PlayerData.Instance.CurrentUser;
            profileDisplayNameInput.text = user.displayName ?? "";
            profileAgeText.text = user.age > 0 ? user.age.ToString() : "-";

            var avatarId = !string.IsNullOrWhiteSpace(user.avatarPresetId) ? user.avatarPresetId : AvatarPresetIds[0];
            selectedAvatarPresetIndex = Mathf.Max(0, AvatarPresetIds.IndexOf(avatarId));
            if (selectedAvatarPresetIndex < 0) selectedAvatarPresetIndex = 0;
            RefreshAvatarPresetLabel();
        }

        private void CloseProfilePanel()
        {
            if (profilePanel != null)
            {
                profilePanel.SetActive(false);
            }
        }

        private void OnAvatarPrevClicked()
        {
            if (AvatarPresetIds.Count == 0) return;
            selectedAvatarPresetIndex = (selectedAvatarPresetIndex - 1 + AvatarPresetIds.Count) % AvatarPresetIds.Count;
            RefreshAvatarPresetLabel();
        }

        private void OnAvatarNextClicked()
        {
            if (AvatarPresetIds.Count == 0) return;
            selectedAvatarPresetIndex = (selectedAvatarPresetIndex + 1) % AvatarPresetIds.Count;
            RefreshAvatarPresetLabel();
        }

        private void RefreshAvatarPresetLabel()
        {
            if (profileAvatarPresetText == null || AvatarPresetIds.Count == 0) return;
            profileAvatarPresetText.text = AvatarPresetIds[selectedAvatarPresetIndex];
        }

        private void OnProfileSaveClicked()
        {
            if (!PlayerData.Exists || PlayerData.Instance.CurrentUser == null) return;

            string displayName = profileDisplayNameInput != null ? profileDisplayNameInput.text.Trim() : "";
            if (displayName.Length < 3 || displayName.Length > 20)
            {
                Log("Profile save blocked: display name must be 3-20 chars");
                return;
            }

            var user = PlayerData.Instance.CurrentUser;
            user.displayName = displayName;
            user.SetAvatarPreset(AvatarPresetIds[selectedAvatarPresetIndex]);
            user.profileOnboardingDismissed = true;

            PlayerData.Instance.UpdateUser(user);
            RefreshUI();
            CloseProfilePanel();
        }

        private void OnProfileSkipClicked()
        {
            if (!PlayerData.Exists || PlayerData.Instance.CurrentUser == null)
            {
                CloseProfilePanel();
                return;
            }

            var user = PlayerData.Instance.CurrentUser;
            user.profileOnboardingDismissed = true;
            PlayerData.Instance.UpdateUser(user);
            CloseProfilePanel();
        }

        private void UpdateProfileActionLayout(bool onboarding)
        {
            if (profileCloseButton == null || profileSkipButton == null)
            {
                return;
            }

            profileSkipButton.gameObject.SetActive(onboarding);

            var closeRect = profileCloseButton.GetComponent<RectTransform>();
            if (closeRect != null)
            {
                closeRect.anchoredPosition = onboarding
                    ? new Vector2(95, -840)
                    : new Vector2(180, -840);
            }

            if (profileWalletHintText != null)
            {
                profileWalletHintText.gameObject.SetActive(true);
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
