// ============================================================================
// MainMenuSetup.cs
// Black Bart's Gold - Main Menu UI Auto-Setup
// Path: Assets/Scripts/UI/MainMenuSetup.cs
// ============================================================================
// Automatically configures all UI elements for the main menu at runtime.
// This handles positioning, sizing, colors, and text setup.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Auto-configures the main menu UI elements at runtime.
    /// Attach this to the MainMenuCanvas.
    /// </summary>
    public class MainMenuSetup : MonoBehaviour
    {
        #region Color Constants
        
        // From project-vision.md color palette
        private static readonly Color GoldColor = new Color(1f, 0.84f, 0f);           // #FFD700
        private static readonly Color DeepSeaBlue = new Color(0.102f, 0.212f, 0.365f); // #1A365D
        private static readonly Color PirateRed = new Color(0.545f, 0f, 0f);           // #8B0000
        private static readonly Color Parchment = new Color(0.961f, 0.902f, 0.827f);   // #F5E6D3
        private static readonly Color DarkBrown = new Color(0.239f, 0.161f, 0.078f);   // #3D2914
        
        #endregion
        
        #region UI References (Auto-Found)
        
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private RectTransform backgroundRect;
        private Image backgroundImage;
        private RectTransform titleRect;
        private TMP_Text titleText;
        private RectTransform startHuntRect;
        private Button startHuntButton;
        private Image startHuntImage;
        private RectTransform walletRect;
        private Button walletButton;
        private Image walletImage;
        private RectTransform settingsRect;
        private Button settingsButton;
        private Image settingsImage;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            FindUIElements();
            SetupCanvas();
            SetupBackground();
            SetupTitle();
            SetupButtons();
            
            Debug.Log("[MainMenuSetup] UI setup complete!");
        }
        
        #endregion
        
        #region Find Elements
        
        private void FindUIElements()
        {
            canvas = GetComponent<Canvas>();
            canvasScaler = GetComponent<CanvasScaler>();
            
            // Find background
            var bg = transform.Find("BackgroundPanel");
            if (bg != null)
            {
                backgroundRect = bg.GetComponent<RectTransform>();
                backgroundImage = bg.GetComponent<Image>();
            }
            
            // Find title
            var title = transform.Find("TitleText");
            if (title != null)
            {
                titleRect = title.GetComponent<RectTransform>();
                titleText = title.GetComponent<TMP_Text>();
            }
            
            // Find buttons
            var startBtn = transform.Find("StartHuntButton");
            if (startBtn != null)
            {
                startHuntRect = startBtn.GetComponent<RectTransform>();
                startHuntButton = startBtn.GetComponent<Button>();
                startHuntImage = startBtn.GetComponent<Image>();
            }
            
            var walletBtn = transform.Find("WalletButton");
            if (walletBtn != null)
            {
                walletRect = walletBtn.GetComponent<RectTransform>();
                walletButton = walletBtn.GetComponent<Button>();
                walletImage = walletBtn.GetComponent<Image>();
            }
            
            var settingsBtn = transform.Find("SettingsButton");
            if (settingsBtn != null)
            {
                settingsRect = settingsBtn.GetComponent<RectTransform>();
                settingsButton = settingsBtn.GetComponent<Button>();
                settingsImage = settingsBtn.GetComponent<Image>();
            }
        }
        
        #endregion
        
        #region Setup Methods
        
        private void SetupCanvas()
        {
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;
            }
            
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1080, 1920);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
            }
        }
        
        private void SetupBackground()
        {
            if (backgroundRect == null) return;
            
            // Stretch to fill screen
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            
            // Deep sea blue background
            if (backgroundImage != null)
            {
                backgroundImage.color = DeepSeaBlue;
            }
        }
        
        private void SetupTitle()
        {
            if (titleRect == null) return;
            
            // Position at top center
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -100);
            titleRect.sizeDelta = new Vector2(800, 150);
            
            // Style text
            if (titleText != null)
            {
                titleText.text = "🏴‍☠️ Black Bart's Gold 🏴‍☠️";
                titleText.fontSize = 56;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = GoldColor;
                titleText.enableWordWrapping = false;
            }
        }
        
        private void SetupButtons()
        {
            // Start Hunt Button - Main button, larger
            SetupButton(startHuntRect, startHuntImage, startHuntButton, 
                "StartHuntButton", "🏴‍☠️ START HUNTING", 
                0, -100, 600, 120, GoldColor, DarkBrown, true);
            
            // Wallet Button
            SetupButton(walletRect, walletImage, walletButton,
                "WalletButton", "👛 MY WALLET",
                0, -250, 500, 100, Parchment, DarkBrown, false);
            
            // Settings Button
            SetupButton(settingsRect, settingsImage, settingsButton,
                "SettingsButton", "⚙️ SETTINGS",
                0, -380, 500, 100, Parchment, DarkBrown, false);
        }
        
        private void SetupButton(RectTransform rect, Image image, Button button,
            string name, string labelText, float posX, float posY, 
            float width, float height, Color bgColor, Color textColor, bool isPrimary)
        {
            if (rect == null) return;
            
            // Position from center
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(posX, posY);
            rect.sizeDelta = new Vector2(width, height);
            
            // Style image
            if (image != null)
            {
                image.color = bgColor;
            }
            
            // Style button
            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
                colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
                colors.selectedColor = Color.white;
                button.colors = colors;
            }
            
            // Setup button text
            var textTransform = rect.Find("ButtonText");
            if (textTransform != null)
            {
                var textRect = textTransform.GetComponent<RectTransform>();
                if (textRect == null)
                {
                    textRect = textTransform.gameObject.AddComponent<RectTransform>();
                }
                
                // Stretch text to fill button
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(20, 10);
                textRect.offsetMax = new Vector2(-20, -10);
                
                // Get or add TMP_Text
                var tmpText = textTransform.GetComponent<TMP_Text>();
                if (tmpText == null)
                {
                    tmpText = textTransform.gameObject.AddComponent<TextMeshProUGUI>();
                }
                
                tmpText.text = labelText;
                tmpText.fontSize = isPrimary ? 36 : 28;
                tmpText.fontStyle = isPrimary ? FontStyles.Bold : FontStyles.Normal;
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.color = textColor;
            }
        }
        
        #endregion
        
        #region Editor Helper
        
        /// <summary>
        /// Editor: Force setup UI
        /// </summary>
        [ContextMenu("Force Setup UI")]
        public void ForceSetupUI()
        {
            FindUIElements();
            SetupCanvas();
            SetupBackground();
            SetupTitle();
            SetupButtons();
            Debug.Log("[MainMenuSetup] Forced UI setup complete!");
        }
        
        #endregion
    }
}
