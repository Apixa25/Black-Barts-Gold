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
            Debug.Log("╔════════════════════════════════════════════════════╗");
            Debug.Log("║ [LoginSetup] AWAKE() - Setting up Login UI         ║");
            Debug.Log("╚════════════════════════════════════════════════════╝");
            
            Debug.Log("[LoginSetup] Setting up canvas...");
            SetupCanvas();
            
            Debug.Log("[LoginSetup] Setting up background...");
            SetupBackground();
            
            Debug.Log("[LoginSetup] Setting up title...");
            SetupTitle();
            
            Debug.Log("[LoginSetup] Setting up input fields...");
            SetupInputFields();
            
            Debug.Log("[LoginSetup] Setting up buttons...");
            SetupButtons();
            
            Debug.Log("[LoginSetup] ✅ UI setup complete!");
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
            // TEMPORARILY DISABLED - dynamic TMP_InputField creation causes conflicts
            // The scene should have properly configured TMP_InputField components
            // For now, just position the placeholder objects
            Debug.Log("[LoginSetup] Input field setup temporarily simplified");
            
            PositionInputField("EmailInput", 0, 100);
            PositionInputField("PasswordInput", 0, -20);
        }
        
        private void PositionInputField(string name, float posX, float posY)
        {
            var field = transform.Find(name);
            if (field == null)
            {
                Debug.LogWarning($"[LoginSetup] Could not find '{name}'");
                return;
            }
            
            var rect = field.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(posX, posY);
                rect.sizeDelta = new Vector2(600, 80);
            }
            
            var image = field.GetComponent<Image>();
            if (image != null)
            {
                image.color = Parchment;
            }
            
            Debug.Log($"[LoginSetup] Positioned '{name}' at ({posX}, {posY})");
        }
        
        private void SetupInputField(string name, string placeholder, float posX, float posY)
        {
            Debug.Log($"[LoginSetup] SetupInputField('{name}', '{placeholder}')");
            
            try
            {
                var field = transform.Find(name);
                if (field == null)
                {
                    Debug.LogError($"[LoginSetup] ❌ Could not find '{name}'!");
                    return;
                }
                Debug.Log($"[LoginSetup] ✅ Found '{name}'");
                
                // Add RectTransform if missing
                var rect = field.GetComponent<RectTransform>();
                if (rect == null)
                {
                    rect = field.gameObject.AddComponent<RectTransform>();
                    Debug.Log($"[LoginSetup] Added RectTransform to '{name}'");
                }
                
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(posX, posY);
                rect.sizeDelta = new Vector2(600, 80);
                
                // Add Image if missing
                var image = field.GetComponent<Image>();
                if (image == null)
                {
                    image = field.gameObject.AddComponent<Image>();
                    Debug.Log($"[LoginSetup] Added Image to '{name}'");
                }
                image.color = Parchment;
                
                // Add TMP_InputField if missing
                var input = field.GetComponent<TMP_InputField>();
                if (input == null)
                {
                    input = field.gameObject.AddComponent<TMP_InputField>();
                    Debug.Log($"[LoginSetup] ➕ Added TMP_InputField to '{name}'");
                }
                else
                {
                    Debug.Log($"[LoginSetup] TMP_InputField already exists on '{name}'");
                }
                
                // Create or find text area
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
                    Debug.Log($"[LoginSetup] Created TextArea for '{name}'");
                }
                
                // Find or create placeholder text (don't create duplicates)
                var existingPlaceholder = textArea.Find("Placeholder");
                TMP_Text placeholderText;
                if (existingPlaceholder == null)
                {
                    var placeholderGO = new GameObject("Placeholder");
                    placeholderGO.transform.SetParent(textArea, false);
                    var placeholderRect = placeholderGO.AddComponent<RectTransform>();
                    placeholderRect.anchorMin = Vector2.zero;
                    placeholderRect.anchorMax = Vector2.one;
                    placeholderRect.offsetMin = Vector2.zero;
                    placeholderRect.offsetMax = Vector2.zero;
                    
                    placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
                    Debug.Log($"[LoginSetup] Created Placeholder for '{name}'");
                }
                else
                {
                    placeholderText = existingPlaceholder.GetComponent<TMP_Text>();
                    Debug.Log($"[LoginSetup] Using existing Placeholder for '{name}'");
                }
                
                if (placeholderText != null)
                {
                    placeholderText.text = placeholder;
                    placeholderText.fontSize = 24;
                    placeholderText.color = new Color(DarkBrown.r, DarkBrown.g, DarkBrown.b, 0.5f);
                    placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
                }
                
                // Find or create input text (don't create duplicates)
                var existingText = textArea.Find("Text");
                TMP_Text inputText;
                if (existingText == null)
                {
                    var inputTextGO = new GameObject("Text");
                    inputTextGO.transform.SetParent(textArea, false);
                    var inputRect = inputTextGO.AddComponent<RectTransform>();
                    inputRect.anchorMin = Vector2.zero;
                    inputRect.anchorMax = Vector2.one;
                    inputRect.offsetMin = Vector2.zero;
                    inputRect.offsetMax = Vector2.zero;
                    
                    inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
                    Debug.Log($"[LoginSetup] Created Text for '{name}'");
                }
                else
                {
                    inputText = existingText.GetComponent<TMP_Text>();
                    Debug.Log($"[LoginSetup] Using existing Text for '{name}'");
                }
                
                if (inputText != null)
                {
                    inputText.fontSize = 24;
                    inputText.color = DarkBrown;
                    inputText.alignment = TextAlignmentOptions.MidlineLeft;
                }
                
                // Configure input field
                if (input != null && textArea != null)
                {
                    input.textViewport = textArea.GetComponent<RectTransform>();
                    input.textComponent = inputText as TMP_Text;
                    input.placeholder = placeholderText;
                    
                    if (name.Contains("Password"))
                    {
                        input.contentType = TMP_InputField.ContentType.Password;
                    }
                    Debug.Log($"[LoginSetup] ✅ Configured TMP_InputField for '{name}'");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoginSetup] ❌ Exception setting up '{name}': {e.Message}");
            }
        }
        
        private void SetupButtons()
        {
            Debug.Log("[LoginSetup] Setting up buttons...");
            // Login button for existing users
            SetupButton("LoginButton", "⚓ Log In", 0, -150, 500, 80, GoldColor, true);
            // Create account button for new users
            SetupButton("CreateAccountButton", "🏴‍☠️ Join the Crew (New User)", 0, -260, 400, 60, Parchment, false);
        }
        
        private void SetupButton(string name, string label, float posX, float posY, 
            float width, float height, Color bgColor, bool isPrimary)
        {
            Debug.Log($"[LoginSetup] SetupButton('{name}', '{label}')");
            
            var btn = transform.Find(name);
            if (btn == null)
            {
                Debug.LogError($"[LoginSetup] ❌ Could not find button '{name}'!");
                return;
            }
            Debug.Log($"[LoginSetup] ✅ Found button '{name}'");
            
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
            
            // Log how many listeners the button has
            int listenerCount = button.onClick.GetPersistentEventCount();
            Debug.Log($"[LoginSetup] Button '{name}' has {listenerCount} persistent listeners");
            
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
