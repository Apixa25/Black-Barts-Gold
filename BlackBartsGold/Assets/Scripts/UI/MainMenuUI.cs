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
using System.IO;
using System.Text.RegularExpressions;
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
        private TMP_InputField profileAgeInput;
        private TMP_InputField profileEmailInput;
        private TMP_InputField profilePhoneInput;
        private Image profilePhotoPreview;
        private TMP_Text profileValidationText;
        private TMP_Text profilePhotoStatusText;
        private TMP_Text profileWalletHintText;
        private Button profilePickGalleryButton;
        private Button profileTakePhotoButton;
        private Button profileSaveButton;
        private Button profileCloseButton;
        private Button profileSkipButton;
        private Texture2D pendingProfileTexture;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            ResolveRuntimeReferences();
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
        /// Resolve main menu references from runtime-created hierarchy.
        /// This keeps MainMenuUI working without inspector wiring.
        /// </summary>
        private void ResolveRuntimeReferences()
        {
            titleText = ResolveTmpText(titleText, "TitleText");
            balanceText = ResolveTmpText(balanceText, "BalanceText");
            gasStatusText = ResolveTmpText(gasStatusText, "GasStatusText");
            playerNameText = ResolveTmpText(playerNameText, "PlayerNameText");
            startHuntingText = ResolveTmpText(startHuntingText, "StartHuntButton/ButtonText", "StartHuntButton/Text");
            coinsFoundText = ResolveTmpText(coinsFoundText, "CoinsFoundText");
            findLimitText = ResolveTmpText(findLimitText, "FindLimitText");
            coinsHiddenText = ResolveTmpText(coinsHiddenText, "CoinsHiddenText");
            tierText = ResolveTmpText(tierText, "TierText");

            startHuntingButton = ResolveButton(startHuntingButton, "StartHuntButton");
            treasureMapButton = ResolveButton(treasureMapButton, "TreasureMapButton", "MapButton");
            walletButton = ResolveButton(walletButton, "WalletButton");
            settingsButton = ResolveButton(settingsButton, "SettingsButton");
            profileButton = ResolveButton(profileButton, "ProfileButton");
            buyGasButton = ResolveButton(buyGasButton, "BuyGasButton");

            noGasPanel = ResolveGameObject(noGasPanel, "NoGasPanel");
            loadingPanel = ResolveGameObject(loadingPanel, "LoadingPanel");
        }

        private Button ResolveButton(Button current, params string[] paths)
        {
            if (current != null) return current;
            foreach (var path in paths)
            {
                var t = transform.Find(path);
                if (t != null && t.TryGetComponent<Button>(out var button))
                    return button;
            }
            return null;
        }

        private TMP_Text ResolveTmpText(TMP_Text current, params string[] paths)
        {
            if (current != null) return current;
            foreach (var path in paths)
            {
                var t = transform.Find(path);
                if (t != null && t.TryGetComponent<TMP_Text>(out var text))
                    return text;
            }
            return null;
        }

        private GameObject ResolveGameObject(GameObject current, params string[] paths)
        {
            if (current != null) return current;
            foreach (var path in paths)
            {
                var t = transform.Find(path);
                if (t != null) return t.gameObject;
            }
            return null;
        }
        
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
            bool profileReady = PlayerData.Exists && PlayerData.Instance.CurrentUser != null && PlayerData.Instance.CurrentUser.IsProfileComplete();
            canHunt = canPlay && profileReady;
            
            if (startHuntingButton != null)
            {
                startHuntingButton.interactable = canHunt;
            }
            
            if (startHuntingText != null)
            {
                if (!profileReady)
                {
                    startHuntingText.text = "Complete Profile";
                }
                else
                {
                    startHuntingText.text = canPlay ? "Start Hunting" : "Out of Gas";
                }
            }
            
            if (noGasPanel != null)
            {
                noGasPanel.SetActive(profileReady && !canPlay);
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
            
            if (PlayerData.Exists && PlayerData.Instance.CurrentUser != null && !PlayerData.Instance.CurrentUser.IsProfileComplete())
            {
                OpenProfilePanel(true);
                if (profileValidationText != null)
                {
                    profileValidationText.text = "Complete your profile before you can start hunting.";
                }
                return;
            }

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
                var existingProfileButton = transform.Find("ProfileButton");
                if (existingProfileButton != null)
                {
                    profileButton = existingProfileButton.GetComponent<Button>();
                    if (profileButton == null)
                    {
                        profileButton = existingProfileButton.gameObject.AddComponent<Button>();
                    }
                }
                else
                {
                    profileButton = CreateMainMenuButton("ProfileButton", "MY PROFILE", new Vector2(0, -290), new Vector2(550, 90));
                }
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
            rect.sizeDelta = new Vector2(860, 1220);

            var image = panelGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.9f);

            CreateLabel(panelGo.transform, "Title", "Player Profile", new Vector2(0, -50), new Vector2(760, 70), 46, goldColor, TextAlignmentOptions.Center);
            CreateLabel(panelGo.transform, "IdentityHeader", "Identity", new Vector2(-300, -130), new Vector2(300, 40), 32, goldColor, TextAlignmentOptions.Left);

            CreateLabel(panelGo.transform, "DisplayNameLabel", "Display Name", new Vector2(-300, -190), new Vector2(260, 40), 26, Color.white, TextAlignmentOptions.Left);
            profileDisplayNameInput = CreateInputField(panelGo.transform, "DisplayNameInput", "Enter display name", TMP_InputField.ContentType.Standard, 20, new Vector2(0, -245), new Vector2(620, 72));

            CreateLabel(panelGo.transform, "AgeLabel", "Age", new Vector2(-300, -325), new Vector2(260, 40), 26, Color.white, TextAlignmentOptions.Left);
            profileAgeInput = CreateInputField(panelGo.transform, "AgeInput", "Enter age", TMP_InputField.ContentType.IntegerNumber, 3, new Vector2(0, -380), new Vector2(620, 72));

            CreateLabel(panelGo.transform, "EmailLabel", "Email", new Vector2(-300, -460), new Vector2(260, 40), 26, Color.white, TextAlignmentOptions.Left);
            profileEmailInput = CreateInputField(panelGo.transform, "EmailInput", "name@example.com", TMP_InputField.ContentType.EmailAddress, 80, new Vector2(0, -515), new Vector2(620, 72));

            CreateLabel(panelGo.transform, "PhoneLabel", "Phone (Optional, E.164)", new Vector2(-300, -595), new Vector2(520, 40), 26, Color.white, TextAlignmentOptions.Left);
            profilePhoneInput = CreateInputField(panelGo.transform, "PhoneInput", "+14155552671", TMP_InputField.ContentType.Standard, 20, new Vector2(0, -650), new Vector2(620, 72));

            CreateLabel(panelGo.transform, "PhotoLabel", "Profile Picture", new Vector2(-300, -730), new Vector2(300, 40), 26, Color.white, TextAlignmentOptions.Left);
            profilePhotoPreview = CreatePhotoPreview(panelGo.transform, "ProfilePhotoPreview", new Vector2(-185, -805), new Vector2(120, 120));
            profileTakePhotoButton = CreatePanelButton(panelGo.transform, "TakePhotoButton", "Camera", new Vector2(30, -785), new Vector2(200, 64), 24);
            profilePickGalleryButton = CreatePanelButton(panelGo.transform, "PickGalleryButton", "Gallery", new Vector2(250, -785), new Vector2(200, 64), 24);
            profilePhotoStatusText = CreateLabel(panelGo.transform, "PhotoStatus", "Upload from camera or gallery.", new Vector2(130, -850), new Vector2(500, 40), 22, new Color(0.9f, 0.9f, 0.9f, 1f), TextAlignmentOptions.Left);
            profileValidationText = CreateLabel(panelGo.transform, "ProfileValidationText", "", new Vector2(0, -900), new Vector2(700, 48), 24, warningColor, TextAlignmentOptions.Center);

            profileWalletHintText = CreateLabel(
                panelGo.transform,
                "WalletHint",
                "Wallet balances and transactions now live in MY WALLET.",
                new Vector2(0, -950),
                new Vector2(700, 90),
                22,
                new Color(0.9f, 0.9f, 0.9f, 1f),
                TextAlignmentOptions.Center
            );

            profileSaveButton = CreatePanelButton(panelGo.transform, "SaveProfileButton", "Save Profile", new Vector2(-180, -1060), new Vector2(240, 72), 30);
            profileCloseButton = CreatePanelButton(panelGo.transform, "CloseProfileButton", "Close", new Vector2(95, -1060), new Vector2(180, 72), 30);
            profileSkipButton = CreatePanelButton(panelGo.transform, "SkipProfileButton", "Skip For Now", new Vector2(320, -1060), new Vector2(240, 72), 24);

            profileTakePhotoButton.onClick.RemoveAllListeners();
            profileTakePhotoButton.onClick.AddListener(OnTakePhotoClicked);
            profilePickGalleryButton.onClick.RemoveAllListeners();
            profilePickGalleryButton.onClick.AddListener(OnPickGalleryClicked);
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

        private TMP_InputField CreateInputField(
            Transform parent,
            string name,
            string placeholderValue,
            TMP_InputField.ContentType contentType,
            int characterLimit,
            Vector2 anchoredPosition,
            Vector2 size
        )
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
            placeholderText.text = placeholderValue;
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
            input.contentType = contentType;
            input.characterLimit = characterLimit;

            return input;
        }

        private Image CreatePhotoPreview(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
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
            image.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            image.preserveAspect = true;
            return image;
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
            profileAgeInput.text = user.age > 0 ? user.age.ToString() : "";
            profileEmailInput.text = user.email ?? "";
            profilePhoneInput.text = user.phoneNumber ?? "";
            profileValidationText.text = "";
            pendingProfileTexture = null;
            LoadProfileImagePreview(user);
        }

        private void CloseProfilePanel()
        {
            if (profilePanel != null)
            {
                profilePanel.SetActive(false);
            }
        }

        private void OnTakePhotoClicked()
        {
            ProfileImagePicker.PickFromCamera(HandleImagePicked);
        }

        private void OnPickGalleryClicked()
        {
            ProfileImagePicker.PickFromGallery(HandleImagePicked);
        }

        private void HandleImagePicked(Texture2D texture, string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                if (profilePhotoStatusText != null)
                {
                    profilePhotoStatusText.text = error;
                }
                return;
            }

            if (texture == null) return;
            pendingProfileTexture = texture;

            if (profilePhotoPreview != null)
            {
                profilePhotoPreview.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }

            if (profilePhotoStatusText != null)
            {
                profilePhotoStatusText.text = "Photo ready to save.";
            }
        }

        private async void OnProfileSaveClicked()
        {
            if (!PlayerData.Exists || PlayerData.Instance.CurrentUser == null) return;

            string displayName = profileDisplayNameInput != null ? profileDisplayNameInput.text.Trim() : "";
            string ageText = profileAgeInput != null ? profileAgeInput.text.Trim() : "";
            string email = profileEmailInput != null ? profileEmailInput.text.Trim().ToLowerInvariant() : "";
            string rawPhone = profilePhoneInput != null ? profilePhoneInput.text : "";
            string phone = User.NormalizePhoneNumber(rawPhone);

            if (displayName.Length < 3 || displayName.Length > 20)
            {
                if (profileValidationText != null) profileValidationText.text = "Display name must be 3-20 characters.";
                return;
            }

            if (!int.TryParse(ageText, out int age) || age < 13 || age > 120)
            {
                if (profileValidationText != null) profileValidationText.text = "Age must be between 13 and 120.";
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                if (profileValidationText != null) profileValidationText.text = "Enter a valid email address.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^\+[1-9]\d{7,14}$"))
            {
                if (profileValidationText != null) profileValidationText.text = "Phone must include country code, e.g. +14155552671.";
                return;
            }

            string existingAvatar = PlayerData.Instance.CurrentUser.avatarUrl;
            bool hasExistingPhoto = !string.IsNullOrWhiteSpace(existingAvatar) &&
                (File.Exists(existingAvatar) || existingAvatar.StartsWith("http", StringComparison.OrdinalIgnoreCase) || existingAvatar.StartsWith("preset://", StringComparison.OrdinalIgnoreCase));
            if (pendingProfileTexture == null && !hasExistingPhoto)
            {
                if (profileValidationText != null) profileValidationText.text = "Add a profile picture from camera or gallery.";
                return;
            }

            var user = PlayerData.Instance.CurrentUser;
            user.displayName = displayName;
            user.age = age;
            user.email = email;
            user.phoneNumber = phone;
            user.profileOnboardingDismissed = true;

            if (pendingProfileTexture != null)
            {
                string localPath = SaveProfileImageLocally(pendingProfileTexture, user.id);
                if (!string.IsNullOrWhiteSpace(localPath))
                {
                    user.avatarUrl = localPath;
                    user.avatarPresetId = null;
                }
            }

            user.profileOnboardingDismissed = true;

            PlayerData.Instance.UpdateUser(user);
            await SyncProfileToServer(user);
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
                    ? new Vector2(95, -1060)
                    : new Vector2(180, -1060);
            }

            if (profileWalletHintText != null)
            {
                profileWalletHintText.gameObject.SetActive(true);
            }
        }

        private void LoadProfileImagePreview(User user)
        {
            if (profilePhotoPreview == null || user == null) return;

            if (string.IsNullOrWhiteSpace(user.avatarUrl) || !File.Exists(user.avatarUrl))
            {
                if (!string.IsNullOrWhiteSpace(user.avatarUrl) &&
                    (user.avatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) || user.avatarUrl.StartsWith("preset://", StringComparison.OrdinalIgnoreCase)))
                {
                    profilePhotoPreview.sprite = null;
                    profilePhotoPreview.color = new Color(0.35f, 0.35f, 0.35f, 1f);
                    if (profilePhotoStatusText != null)
                    {
                        profilePhotoStatusText.text = "Existing profile photo on account.";
                    }
                    return;
                }

                profilePhotoPreview.sprite = null;
                profilePhotoPreview.color = new Color(0.25f, 0.25f, 0.25f, 1f);
                if (profilePhotoStatusText != null)
                {
                    profilePhotoStatusText.text = "Upload from camera or gallery.";
                }
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(user.avatarUrl);
                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(bytes))
                {
                    profilePhotoPreview.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    profilePhotoPreview.color = Color.white;
                    if (profilePhotoStatusText != null)
                    {
                        profilePhotoStatusText.text = "Current photo loaded.";
                    }
                }
            }
            catch
            {
                profilePhotoPreview.sprite = null;
                profilePhotoPreview.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            }
        }

        private string SaveProfileImageLocally(Texture2D texture, string userId)
        {
            if (texture == null) return null;

            try
            {
                string safeUserId = string.IsNullOrWhiteSpace(userId) ? "player" : userId.Replace(":", "_");
                string fileName = $"profile_{safeUserId}.jpg";
                string path = Path.Combine(Application.persistentDataPath, fileName);
                byte[] jpg = texture.EncodeToJPG(85);
                File.WriteAllBytes(path, jpg);
                return path;
            }
            catch (Exception ex)
            {
                Log($"Failed to save profile image: {ex.Message}");
                return null;
            }
        }

        private async System.Threading.Tasks.Task SyncProfileToServer(User user)
        {
            if (user == null || ApiConfig.UseMockApi) return;

            try
            {
                string avatarBase64 = null;
                string avatarMimeType = null;
                if (pendingProfileTexture != null)
                {
                    byte[] jpg = pendingProfileTexture.EncodeToJPG(80);
                    avatarBase64 = Convert.ToBase64String(jpg);
                    avatarMimeType = "image/jpeg";
                }

                string avatarUrl = user.avatarUrl;
                if (!string.IsNullOrWhiteSpace(avatarUrl) && !avatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !avatarUrl.StartsWith("preset://", StringComparison.OrdinalIgnoreCase))
                {
                    avatarUrl = null;
                }

                var payload = new ProfileUpdateRequest
                {
                    email = user.email,
                    displayName = user.displayName,
                    age = user.age,
                    phoneNumber = string.IsNullOrWhiteSpace(user.phoneNumber) ? null : user.phoneNumber,
                    avatarUrl = avatarUrl,
                    avatarPresetId = user.avatarPresetId,
                    avatarBase64 = avatarBase64,
                    avatarMimeType = avatarMimeType
                };

                var response = await ApiClient.Instance.Patch<ProfileUpdateResponse>(ApiConfig.User.PROFILE, payload);
                if (response != null && response.success && response.profile != null)
                {
                    if (!string.IsNullOrWhiteSpace(response.profile.avatarUrl))
                    {
                        user.avatarUrl = response.profile.avatarUrl;
                    }

                    if (!string.IsNullOrWhiteSpace(response.profile.phoneNumber))
                    {
                        user.phoneNumber = response.profile.phoneNumber;
                    }

                    PlayerData.Instance.UpdateUser(user);
                }
            }
            catch (Exception ex)
            {
                Log($"Profile sync skipped/failed: {ex.Message}");
            }
        }

        #endregion

        [Serializable]
        private class ProfileUpdateRequest
        {
            public string email;
            public string displayName;
            public int age;
            public string phoneNumber;
            public string avatarUrl;
            public string avatarPresetId;
            public string avatarBase64;
            public string avatarMimeType;
        }

        [Serializable]
        private class ProfileUpdateResponse
        {
            public bool success;
            public string error;
            public ProfilePayload profile;
        }

        [Serializable]
        private class ProfilePayload
        {
            public string avatarUrl;
            public string phoneNumber;
        }
        
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
