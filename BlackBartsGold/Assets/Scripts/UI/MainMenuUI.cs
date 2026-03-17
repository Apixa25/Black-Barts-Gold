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

        private Transform GetUiRoot()
        {
            var parentCanvas = GetComponentInParent<Canvas>();
            return parentCanvas != null ? parentCanvas.transform : transform;
        }

        private Button ResolveButton(Button current, params string[] paths)
        {
            if (current != null) return current;
            Transform uiRoot = GetUiRoot();
            foreach (var path in paths)
            {
                var t = transform.Find(path);
                if (t == null && uiRoot != null && uiRoot != transform)
                {
                    t = uiRoot.Find(path);
                }
                if (t != null && t.TryGetComponent<Button>(out var button))
                    return button;
            }
            return null;
        }

        private TMP_Text ResolveTmpText(TMP_Text current, params string[] paths)
        {
            if (current != null) return current;
            Transform uiRoot = GetUiRoot();
            foreach (var path in paths)
            {
                var t = transform.Find(path);
                if (t == null && uiRoot != null && uiRoot != transform)
                {
                    t = uiRoot.Find(path);
                }
                if (t != null && t.TryGetComponent<TMP_Text>(out var text))
                    return text;
            }
            return null;
        }

        private GameObject ResolveGameObject(GameObject current, params string[] paths)
        {
            if (current != null) return current;
            Transform uiRoot = GetUiRoot();
            foreach (var path in paths)
            {
                var t = transform.Find(path);
                if (t == null && uiRoot != null && uiRoot != transform)
                {
                    t = uiRoot.Find(path);
                }
                if (t != null) return t.gameObject;
            }
            return null;
        }
        
        /// <summary>
        /// Setup button click listeners
        /// </summary>
        private void DetachQuickNavigation(Button button, string buttonName)
        {
            if (button == null) return;

            var quickNavigation = button.GetComponent<QuickNavigation>();
            if (quickNavigation == null) return;

            button.onClick.RemoveListener(quickNavigation.OnClick);
            Destroy(quickNavigation);
            Debug.Log($"[BBG][MainMenu][Nav] Removed QuickNavigation from {buttonName}; MainMenuUI owns navigation.");
        }

        private void SetupButtons()
        {
            if (startHuntingButton != null)
            {
                DetachQuickNavigation(startHuntingButton, "StartHuntButton");
                startHuntingButton.onClick.AddListener(OnStartHuntingClicked);
            }
            
            if (treasureMapButton != null)
            {
                treasureMapButton.onClick.AddListener(OnTreasureMapClicked);
            }
            
            if (walletButton != null)
            {
                DetachQuickNavigation(walletButton, "WalletButton");
                walletButton.onClick.AddListener(OnWalletClicked);
            }
            
            if (settingsButton != null)
            {
                DetachQuickNavigation(settingsButton, "SettingsButton");
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (profileButton != null)
            {
                profileButton.onClick.RemoveAllListeners();
                profileButton.onClick.AddListener(OnProfileClicked);
                Debug.Log("[BBG][ProfileUI][Setup] Profile button listener attached.");
            }
            else
            {
                Debug.LogWarning("[BBG][ProfileUI][Setup] Profile button reference is null after EnsureProfileUi.");
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
            var theme = BBGThemeProvider.Current;
            if (limit >= 100f) return theme.coinDiamond;
            if (limit >= 50f) return theme.coinPlatinum;
            if (limit >= 25f) return theme.coinGold;
            if (limit >= 10f) return theme.treasureGold;
            if (limit >= 5f) return theme.coinSilver;
            return theme.coinBronze;
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
            Debug.Log("[BBG][ProfileUI][Tap] Profile button tapped.");
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
            Transform uiRoot = GetUiRoot();

            if (profileButton == null)
            {
                var existingProfileButton = uiRoot != null ? uiRoot.Find("ProfileButton") : transform.Find("ProfileButton");
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
            bool isPrimary = objectName.Contains("StartHunting");
            var variant = isPrimary ? BBGButtonVariant.Primary : BBGButtonVariant.Secondary;
            var bbgBtn = BBGButton.Create(GetUiRoot(), label, variant, size);
            bbgBtn.gameObject.name = objectName;
            bbgBtn.RectTransform.anchoredPosition = anchoredPosition;
            return bbgBtn.UnityButton;
        }

        private GameObject BuildProfilePanel()
        {
            Color overlayColor = new Color(0.03f, 0.07f, 0.14f, 1f);
            Color cardColor = new Color(0.08f, 0.12f, 0.19f, 1f);
            Color sectionColor = new Color(0.11f, 0.17f, 0.26f, 0.98f);
            Color accentColor = new Color(0.91f, 0.74f, 0.24f, 1f);
            Color mutedTextColor = new Color(0.82f, 0.86f, 0.92f, 1f);

            var panelGo = new GameObject("ProfilePanel");
            panelGo.transform.SetParent(GetUiRoot(), false);

            var rect = panelGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var image = panelGo.AddComponent<Image>();
            image.color = overlayColor;

            var cardGo = CreateCenteredProfilePanel(
                panelGo.transform,
                "ProfileCard",
                new Vector2(0, -40),
                new Vector2(920, 1280),
                cardColor,
                accentColor
            );

            CreateProfileSectionPanel(
                cardGo.transform,
                "HeaderBand",
                new Vector2(0, -110),
                new Vector2(780, 120),
                new Color(0.12f, 0.18f, 0.28f, 1f),
                new Color(0.74f, 0.58f, 0.18f, 1f)
            );

            CreateLabel(cardGo.transform, "Title", "Captain Profile", new Vector2(0, -82), new Vector2(760, 64), 48, goldColor, TextAlignmentOptions.Center);
            CreateLabel(cardGo.transform, "Subtitle", "Identity, contact details, and your live player portrait.", new Vector2(0, -140), new Vector2(760, 46), 24, mutedTextColor, TextAlignmentOptions.Center);

            CreateProfileSectionPanel(cardGo.transform, "IdentitySection", new Vector2(0, -410), new Vector2(780, 620), sectionColor, new Color(0.42f, 0.33f, 0.12f, 0.9f));
            CreateProfileSectionPanel(cardGo.transform, "PhotoSection", new Vector2(0, -915), new Vector2(780, 250), sectionColor, new Color(0.42f, 0.33f, 0.12f, 0.9f));
            CreateProfileSectionPanel(cardGo.transform, "FooterSection", new Vector2(0, -1115), new Vector2(780, 150), new Color(0.1f, 0.15f, 0.23f, 1f), new Color(0.3f, 0.25f, 0.1f, 0.9f));

            CreateLabel(cardGo.transform, "IdentityHeader", "Captain Details", new Vector2(-250, -170), new Vector2(500, 46), 32, goldColor, TextAlignmentOptions.Left);
            CreateLabel(cardGo.transform, "IdentitySubheader", "These details appear across your player identity and account.", new Vector2(0, -210), new Vector2(700, 36), 20, mutedTextColor, TextAlignmentOptions.Center);

            CreateLabel(cardGo.transform, "DisplayNameLabel", "Display Name", new Vector2(-250, -275), new Vector2(500, 40), 26, Color.white, TextAlignmentOptions.Left);
            profileDisplayNameInput = CreateInputField(cardGo.transform, "DisplayNameInput", "How other pirates see you", TMP_InputField.ContentType.Standard, 20, new Vector2(0, -330), new Vector2(640, 76));

            CreateLabel(cardGo.transform, "AgeLabel", "Age", new Vector2(-250, -410), new Vector2(500, 40), 26, Color.white, TextAlignmentOptions.Left);
            profileAgeInput = CreateInputField(cardGo.transform, "AgeInput", "Required for play eligibility", TMP_InputField.ContentType.IntegerNumber, 3, new Vector2(0, -465), new Vector2(640, 76));

            CreateLabel(cardGo.transform, "EmailLabel", "Email Address", new Vector2(-250, -545), new Vector2(500, 40), 26, Color.white, TextAlignmentOptions.Left);
            profileEmailInput = CreateInputField(cardGo.transform, "EmailInput", "name@example.com", TMP_InputField.ContentType.EmailAddress, 80, new Vector2(0, -600), new Vector2(640, 76));

            CreateLabel(cardGo.transform, "PhoneLabel", "Phone Number", new Vector2(-250, -680), new Vector2(500, 40), 26, Color.white, TextAlignmentOptions.Left);
            profilePhoneInput = CreateInputField(cardGo.transform, "PhoneInput", "+14155552671", TMP_InputField.ContentType.Standard, 20, new Vector2(0, -735), new Vector2(640, 76));

            CreateLabel(cardGo.transform, "PhotoLabel", "Live Player Portrait", new Vector2(-250, -830), new Vector2(500, 40), 30, goldColor, TextAlignmentOptions.Left);
            profilePhotoPreview = CreatePhotoPreview(cardGo.transform, "ProfilePhotoPreview", new Vector2(-215, -930), new Vector2(170, 170));
            profileTakePhotoButton = CreatePanelButton(cardGo.transform, "TakePhotoButton", "Take Photo", new Vector2(65, -895), new Vector2(220, 66), 24);
            profilePickGalleryButton = CreatePanelButton(cardGo.transform, "PickGalleryButton", "Choose From Gallery", new Vector2(175, -970), new Vector2(440, 66), 24);
            profilePhotoStatusText = CreateLabel(cardGo.transform, "PhotoStatus", "Your chosen photo also powers the live dashboard player image.", new Vector2(125, -1040), new Vector2(520, 56), 20, mutedTextColor, TextAlignmentOptions.Left);
            profileValidationText = CreateLabel(cardGo.transform, "ProfileValidationText", "", new Vector2(0, -1185), new Vector2(720, 52), 24, warningColor, TextAlignmentOptions.Center);

            profileWalletHintText = CreateLabel(
                cardGo.transform,
                "WalletHint",
                "Wallet balances and transactions live in MY WALLET. This screen is focused on your identity and avatar.",
                new Vector2(0, -1115),
                new Vector2(700, 90),
                21,
                mutedTextColor,
                TextAlignmentOptions.Center
            );

            profileSaveButton = CreatePanelButton(cardGo.transform, "SaveProfileButton", "Save Profile", new Vector2(-180, -1235), new Vector2(250, 76), 30);
            profileCloseButton = CreatePanelButton(cardGo.transform, "CloseProfileButton", "Close", new Vector2(100, -1235), new Vector2(180, 76), 30);
            profileSkipButton = CreatePanelButton(cardGo.transform, "SkipProfileButton", "Skip For Now", new Vector2(330, -1235), new Vector2(230, 76), 24);

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

            StylePanelButton(profileTakePhotoButton, new Color(0.18f, 0.25f, 0.38f, 1f), Color.white);
            StylePanelButton(profilePickGalleryButton, new Color(0.18f, 0.25f, 0.38f, 1f), Color.white);
            StylePanelButton(profileSaveButton, new Color(0.73f, 0.57f, 0.2f, 1f), new Color(0.12f, 0.08f, 0.02f, 1f));
            StylePanelButton(profileCloseButton, new Color(0.19f, 0.23f, 0.31f, 1f), Color.white);
            StylePanelButton(profileSkipButton, new Color(0.12f, 0.16f, 0.23f, 1f), mutedTextColor);

            panelGo.SetActive(false);
            return panelGo;
        }

        private GameObject CreateProfileSectionPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color fillColor, Color outlineColor)
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
            image.color = fillColor;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -6f);

            return go;
        }

        private GameObject CreateCenteredProfilePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color fillColor, Color outlineColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = fillColor;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -6f);

            return go;
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
            var bbgInput = BBGInputField.Create(parent, placeholderValue, contentType, characterLimit, size);
            bbgInput.gameObject.name = name;
            bbgInput.RectTransform.anchorMin = new Vector2(0.5f, 1f);
            bbgInput.RectTransform.anchorMax = new Vector2(0.5f, 1f);
            bbgInput.RectTransform.pivot = new Vector2(0.5f, 0.5f);
            bbgInput.RectTransform.anchoredPosition = anchoredPosition;
            return bbgInput.InputField;
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
            image.color = new Color(0.09f, 0.13f, 0.2f, 1f);
            image.preserveAspect = true;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.66f, 0.22f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -4f);
            return image;
        }

        private Button CreatePanelButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            var bbgBtn = BBGButton.Create(parent, label, BBGButtonVariant.Secondary, size);
            bbgBtn.gameObject.name = name;
            bbgBtn.SetFontSize(fontSize);
            bbgBtn.RectTransform.anchorMin = new Vector2(0.5f, 1f);
            bbgBtn.RectTransform.anchorMax = new Vector2(0.5f, 1f);
            bbgBtn.RectTransform.pivot = new Vector2(0.5f, 0.5f);
            bbgBtn.RectTransform.anchoredPosition = anchoredPosition;
            return bbgBtn.UnityButton;
        }

        private void StylePanelButton(Button button, Color backgroundColor, Color textColor)
        {
            if (button == null) return;

            var bbg = button.GetComponent<BBGButton>();
            if (bbg != null)
            {
                bool isGold = backgroundColor.r > 0.5f && backgroundColor.g > 0.4f;
                bool isDark = backgroundColor.r < 0.15f && backgroundColor.g < 0.2f;
                if (isGold)
                    bbg.SetVariant(BBGButtonVariant.Primary);
                else if (isDark)
                    bbg.SetVariant(BBGButtonVariant.Ghost);
                else
                    bbg.SetVariant(BBGButtonVariant.Secondary);
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = backgroundColor;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = textColor;
                text.fontStyle = FontStyles.Bold;
            }
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
            bool hasPlayerData = PlayerData.Exists;
            bool hasCurrentUser = hasPlayerData && PlayerData.Instance.CurrentUser != null;
            Debug.Log($"[BBG][ProfileUI][Open] Requested open. onboarding={onboarding} panelReady={profilePanel != null} hasPlayerData={hasPlayerData} hasCurrentUser={hasCurrentUser}");

            if (profilePanel == null || !hasPlayerData || !hasCurrentUser)
            {
                Debug.LogWarning("[BBG][ProfileUI][Open] Aborted because required profile state is missing.");
                return;
            }

            profilePanel.SetActive(true);
            profilePanel.transform.SetAsLastSibling();
            UpdateProfileActionLayout(onboarding);

            var user = PlayerData.Instance.CurrentUser;
            profileDisplayNameInput.text = user.displayName ?? "";
            profileAgeInput.text = user.age > 0 ? user.age.ToString() : "";
            profileEmailInput.text = user.email ?? "";
            profilePhoneInput.text = user.phoneNumber ?? "";
            profileValidationText.text = "";
            pendingProfileTexture = null;
            LoadProfileImagePreview(user);
            Debug.Log($"[BBG][ProfileUI][Open] Panel active for userId={user.id} displayName='{user.displayName}'.");
        }

        private void CloseProfilePanel()
        {
            if (profilePanel != null)
            {
                profilePanel.SetActive(false);
                Debug.Log("[BBG][ProfileUI][Open] Panel closed.");
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
                    ? new Vector2(100, -1235)
                    : new Vector2(215, -1235);
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
