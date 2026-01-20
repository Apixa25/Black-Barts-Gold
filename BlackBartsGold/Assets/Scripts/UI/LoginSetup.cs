// ============================================================================
// LoginSetup.cs
// Black Bart's Gold - Login Scene UI Auto-Setup
// Path: Assets/Scripts/UI/LoginSetup.cs
// ============================================================================
// Automatically configures all UI elements for the login screen at runtime.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Auto-configures the login UI elements at runtime.
    /// Attach this to the LoginCanvas.
    /// </summary>
    public class LoginSetup : MonoBehaviour
    {
        #region Color Constants
        
        private static readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        private static readonly Color DeepSeaBlue = new Color(0.102f, 0.212f, 0.365f);
        private static readonly Color Parchment = new Color(0.961f, 0.902f, 0.827f);
        private static readonly Color DarkBrown = new Color(0.239f, 0.161f, 0.078f);
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            SetupCanvas();
            SetupBackground();
            SetupTitle();
            SetupInputFields();
            SetupButtons();
            
            Debug.Log("[LoginSetup] UI setup complete!");
        }
        
        #endregion
        
        #region Setup Methods
        
        private void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            
            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }
        
        private void SetupBackground()
        {
            var bg = transform.Find("Background");
            if (bg == null) return;
            
            var rect = bg.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            
            var image = bg.GetComponent<Image>();
            if (image != null)
            {
                image.color = DeepSeaBlue;
            }
        }
        
        private void SetupTitle()
        {
            var title = transform.Find("TitleText");
            if (title == null) return;
            
            var rect = title.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0, -150);
                rect.sizeDelta = new Vector2(800, 120);
            }
            
            var text = title.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = "🏴‍☠️ Ahoy, Matey!";
                text.fontSize = 52;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = GoldColor;
            }
        }
        
        private void SetupInputFields()
        {
            SetupInputField("EmailInput", "📧 Email Address", 0, 100);
            SetupInputField("PasswordInput", "🔒 Password", 0, -20);
        }
        
        private void SetupInputField(string name, string placeholder, float posX, float posY)
        {
            var field = transform.Find(name);
            if (field == null) return;
            
            // Add RectTransform if missing
            var rect = field.GetComponent<RectTransform>();
            if (rect == null) rect = field.gameObject.AddComponent<RectTransform>();
            
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(posX, posY);
            rect.sizeDelta = new Vector2(600, 80);
            
            // Add Image if missing
            var image = field.GetComponent<Image>();
            if (image == null) image = field.gameObject.AddComponent<Image>();
            image.color = Parchment;
            
            // Add TMP_InputField if missing
            var input = field.GetComponent<TMP_InputField>();
            if (input == null) input = field.gameObject.AddComponent<TMP_InputField>();
            
            // Create text area
            var textArea = field.Find("TextArea");
            if (textArea == null)
            {
                var textAreaGO = new GameObject("TextArea");
                textAreaGO.transform.SetParent(field, false);
                textArea = textAreaGO.transform;
                
                var textRect = textAreaGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(20, 10);
                textRect.offsetMax = new Vector2(-20, -10);
            }
            
            // Create placeholder text
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textArea, false);
            var placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            
            var placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 24;
            placeholderText.color = new Color(DarkBrown.r, DarkBrown.g, DarkBrown.b, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Create input text
            var inputTextGO = new GameObject("Text");
            inputTextGO.transform.SetParent(textArea, false);
            var inputRect = inputTextGO.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            
            var inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 24;
            inputText.color = DarkBrown;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Configure input field
            input.textViewport = textArea.GetComponent<RectTransform>();
            input.textComponent = inputText;
            input.placeholder = placeholderText;
            
            if (name.Contains("Password"))
            {
                input.contentType = TMP_InputField.ContentType.Password;
            }
        }
        
        private void SetupButtons()
        {
            SetupButton("LoginButton", "⚓ SET SAIL (Login)", 0, -150, 500, 80, GoldColor, true);
            SetupButton("CreateAccountButton", "🏴‍☠️ Join the Crew", 0, -260, 400, 60, Parchment, false);
        }
        
        private void SetupButton(string name, string label, float posX, float posY, 
            float width, float height, Color bgColor, bool isPrimary)
        {
            var btn = transform.Find(name);
            if (btn == null) return;
            
            var rect = btn.GetComponent<RectTransform>();
            if (rect == null) rect = btn.gameObject.AddComponent<RectTransform>();
            
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(posX, posY);
            rect.sizeDelta = new Vector2(width, height);
            
            var image = btn.GetComponent<Image>();
            if (image == null) image = btn.gameObject.AddComponent<Image>();
            image.color = bgColor;
            
            var button = btn.GetComponent<Button>();
            if (button == null) button = btn.gameObject.AddComponent<Button>();
            
            // Create button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btn, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = isPrimary ? 28 : 22;
            text.fontStyle = isPrimary ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.color = DarkBrown;
        }
        
        #endregion
        
        [ContextMenu("Force Setup UI")]
        public void ForceSetupUI()
        {
            SetupCanvas();
            SetupBackground();
            SetupTitle();
            SetupInputFields();
            SetupButtons();
        }
    }
}
