// ============================================================================
// UIManager.cs
// Black Bart's Gold - Market Standard UI Management
// Path: Assets/Scripts/Core/UIManager.cs
// ============================================================================
// MARKET STANDARD PATTERN: Single persistent UI manager that handles all 
// navigation through showing/hiding panels rather than loading scenes.
// This is how Pokemon Go, Clash of Clans, and other top mobile games work.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Persistent UI Manager - survives scene changes.
    /// Handles all menu navigation through panel visibility.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        
        private static UIManager _instance;
        public static UIManager Instance => _instance;
        
        #endregion
        
        #region UI Panels
        
        [Header("UI Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject walletPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject arHudPanel;
        
        #endregion
        
        #region Colors (Pirate Theme)
        
        public static readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        public static readonly Color DeepSeaBlue = new Color(0.102f, 0.212f, 0.365f);
        public static readonly Color Parchment = new Color(0.961f, 0.902f, 0.827f);
        public static readonly Color DarkBrown = new Color(0.239f, 0.161f, 0.078f);
        public static readonly Color SemiTransparent = new Color(0, 0, 0, 0.7f);
        
        #endregion
        
        #region State
        
        private GameObject currentPanel;
        private bool isInARMode = false;
        
        public bool IsInARMode => isInARMode;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Debug.Log("[UIManager] Duplicate found, destroying...");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[UIManager] Initialized - Market Standard UI Pattern");
            
            // Build UI if not already set up
            if (loginPanel == null)
            {
                BuildUI();
            }
        }
        
        private void Start()
        {
            // Show login by default
            ShowLogin();
        }
        
        #endregion
        
        #region Panel Navigation (The Simple Way!)
        
        /// <summary>
        /// Show the Login panel
        /// </summary>
        public void ShowLogin()
        {
            Debug.Log("[UIManager] Showing Login");
            HideAllPanels();
            if (loginPanel != null) loginPanel.SetActive(true);
            currentPanel = loginPanel;
        }
        
        /// <summary>
        /// Show the Register panel
        /// </summary>
        public void ShowRegister()
        {
            Debug.Log("[UIManager] Showing Register");
            HideAllPanels();
            if (registerPanel != null) registerPanel.SetActive(true);
            currentPanel = registerPanel;
        }
        
        /// <summary>
        /// Show the Main Menu panel
        /// </summary>
        public void ShowMainMenu()
        {
            Debug.Log("[UIManager] Showing MainMenu");
            HideAllPanels();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            currentPanel = mainMenuPanel;
        }
        
        /// <summary>
        /// Show the Wallet panel
        /// </summary>
        public void ShowWallet()
        {
            Debug.Log("[UIManager] Showing Wallet");
            HideAllPanels();
            if (walletPanel != null) walletPanel.SetActive(true);
            currentPanel = walletPanel;
        }
        
        /// <summary>
        /// Show the Settings panel
        /// </summary>
        public void ShowSettings()
        {
            Debug.Log("[UIManager] Showing Settings");
            HideAllPanels();
            if (settingsPanel != null) settingsPanel.SetActive(true);
            currentPanel = settingsPanel;
        }
        
        /// <summary>
        /// Enter AR Hunt mode - this is the only time we load a different scene
        /// </summary>
        public void StartARHunt()
        {
            Debug.Log("[UIManager] Starting AR Hunt");
            isInARMode = true;
            HideAllPanels();
            if (arHudPanel != null) arHudPanel.SetActive(true);
            
            // Load AR scene additively (keeps UIManager alive)
            SceneManager.LoadScene("ARHunt", LoadSceneMode.Single);
        }
        
        /// <summary>
        /// Exit AR mode and return to main menu
        /// </summary>
        public void ExitARHunt()
        {
            Debug.Log("[UIManager] Exiting AR Hunt");
            isInARMode = false;
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            
            // After scene loads, show main menu
            StartCoroutine(ShowMainMenuAfterLoad());
        }
        
        private IEnumerator ShowMainMenuAfterLoad()
        {
            yield return null; // Wait one frame
            ShowMainMenu();
        }
        
        private void HideAllPanels()
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (walletPanel != null) walletPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (arHudPanel != null) arHudPanel.SetActive(false);
        }
        
        #endregion
        
        #region Dynamic UI Building
        
        /// <summary>
        /// Build all UI panels programmatically (market standard for mobile)
        /// </summary>
        private void BuildUI()
        {
            Debug.Log("[UIManager] Building UI panels...");
            
            // Create main canvas
            var canvas = CreateCanvas();
            
            // Create each panel
            loginPanel = CreateLoginPanel(canvas.transform);
            registerPanel = CreateRegisterPanel(canvas.transform);
            mainMenuPanel = CreateMainMenuPanel(canvas.transform);
            walletPanel = CreateWalletPanel(canvas.transform);
            settingsPanel = CreateSettingsPanel(canvas.transform);
            
            // Hide all initially
            HideAllPanels();
            
            Debug.Log("[UIManager] UI panels built successfully!");
        }
        
        private Canvas CreateCanvas()
        {
            var canvasGO = new GameObject("UICanvas");
            canvasGO.transform.SetParent(transform);
            
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            return canvas;
        }
        
        #endregion
        
        #region Panel Builders
        
        private GameObject CreateLoginPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "LoginPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "🏴‍☠️ Ahoy, Matey!", 
                new Vector2(0, 300), 52, GoldColor, FontStyles.Bold);
            
            // Login Button
            CreateButton(panel.transform, "LoginButton", "⚓ SET SAIL", 
                new Vector2(0, -100), new Vector2(500, 80), GoldColor,
                () => ShowMainMenu());
            
            // Register Button
            CreateButton(panel.transform, "RegisterButton", "🏴‍☠️ Join the Crew", 
                new Vector2(0, -200), new Vector2(400, 60), Parchment,
                () => ShowRegister());
            
            return panel;
        }
        
        private GameObject CreateRegisterPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "RegisterPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "🏴‍☠️ Join the Crew!", 
                new Vector2(0, 300), 48, GoldColor, FontStyles.Bold);
            
            // Subtitle
            CreateText(panel.transform, "Subtitle", "Sign up to start hunting treasure", 
                new Vector2(0, 220), 24, Parchment, FontStyles.Normal);
            
            // Register Button (for now, just goes to main menu)
            CreateButton(panel.transform, "CreateButton", "⚓ Create Account", 
                new Vector2(0, -100), new Vector2(500, 80), GoldColor,
                () => ShowMainMenu());
            
            // Back Button
            CreateButton(panel.transform, "BackButton", "← Back to Login", 
                new Vector2(0, -200), new Vector2(300, 50), Parchment,
                () => ShowLogin());
            
            return panel;
        }
        
        private GameObject CreateMainMenuPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MainMenuPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "⚓ Black Bart's Gold", 
                new Vector2(0, 350), 48, GoldColor, FontStyles.Bold);
            
            // Start Hunt Button
            CreateButton(panel.transform, "StartHuntButton", "🔍 Start Hunting!", 
                new Vector2(0, 50), new Vector2(500, 100), GoldColor,
                () => StartARHunt());
            
            // Wallet Button
            CreateButton(panel.transform, "WalletButton", "💰 Wallet", 
                new Vector2(0, -80), new Vector2(400, 70), Parchment,
                () => ShowWallet());
            
            // Settings Button
            CreateButton(panel.transform, "SettingsButton", "⚙️ Settings", 
                new Vector2(0, -170), new Vector2(400, 70), Parchment,
                () => ShowSettings());
            
            return panel;
        }
        
        private GameObject CreateWalletPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "WalletPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "💰 Treasure Chest", 
                new Vector2(0, 350), 48, GoldColor, FontStyles.Bold);
            
            // Balance
            CreateText(panel.transform, "Balance", "0 Doubloons", 
                new Vector2(0, 200), 36, Parchment, FontStyles.Bold);
            
            // Back Button
            CreateButton(panel.transform, "BackButton", "← Back", 
                new Vector2(0, -300), new Vector2(300, 60), Parchment,
                () => ShowMainMenu());
            
            return panel;
        }
        
        private GameObject CreateSettingsPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "SettingsPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "⚙️ Settings", 
                new Vector2(0, 350), 48, GoldColor, FontStyles.Bold);
            
            // Back Button
            CreateButton(panel.transform, "BackButton", "← Back", 
                new Vector2(0, -300), new Vector2(300, 60), Parchment,
                () => ShowMainMenu());
            
            // Logout Button
            CreateButton(panel.transform, "LogoutButton", "🚪 Logout", 
                new Vector2(0, -200), new Vector2(300, 60), new Color(0.8f, 0.2f, 0.2f),
                () => ShowLogin());
            
            return panel;
        }
        
        #endregion
        
        #region UI Helper Methods
        
        private GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var image = panel.AddComponent<Image>();
            image.color = bgColor;
            image.raycastTarget = false; // Don't block button clicks!
            
            return panel;
        }
        
        private TextMeshProUGUI CreateText(Transform parent, string name, string content,
            Vector2 position, float fontSize, Color color, FontStyles style)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            
            var rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900, 100);
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false; // Don't block button clicks!
            
            return text;
        }
        
        private Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);
            
            var rect = buttonGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            var image = buttonGO.AddComponent<Image>();
            image.color = bgColor;
            image.raycastTarget = true; // Buttons MUST receive raycasts
            
            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            
            // Button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = size.y * 0.4f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = DarkBrown;
            text.raycastTarget = false;
            
            return button;
        }
        
        #endregion
    }
}
