// ============================================================================
// SimpleLoginController.cs
// Black Bart's Gold - Simple Login Screen Controller
// Path: Assets/Scripts/UI/SimpleLoginController.cs
// ============================================================================
// SIMPLIFIED: Sets up UI visually and handles navigation.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Simple login controller that sets up and handles the login UI.
    /// </summary>
    public class SimpleLoginController : MonoBehaviour
    {
        private Button loginButton;
        private Button createAccountButton;
        private Canvas canvas;
        
        private void Awake()
        {
            Debug.Log("[SimpleLoginController] Awake - Setting up UI...");
            SetupUI();
        }
        
        private void Start()
        {
            Debug.Log("[SimpleLoginController] Start - Wiring buttons...");
            WireButtons();
            Debug.Log("[SimpleLoginController] ✅ Ready!");
        }
        
        private void SetupUI()
        {
            // Find or get the canvas
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }
            
            if (canvas == null)
            {
                Debug.LogError("[SimpleLoginController] No Canvas found!");
                return;
            }
            
            // Ensure canvas is Screen Space - Overlay
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // On top of everything
            
            // Setup Background (stretch to fill)
            SetupBackground();
            
            // Setup Title
            SetupTitle();
            
            // Setup Buttons with text
            SetupButtons();
            
            Debug.Log("[SimpleLoginController] ✅ UI Setup complete");
        }
        
        private void SetupBackground()
        {
            var bg = canvas.transform.Find("Background");
            if (bg == null)
            {
                Debug.Log("[SimpleLoginController] Creating Background...");
                var bgGO = new GameObject("Background");
                bgGO.transform.SetParent(canvas.transform, false);
                bgGO.transform.SetAsFirstSibling(); // Behind everything
                bg = bgGO.transform;
            }
            
            var rect = bg.GetComponent<RectTransform>();
            if (rect == null) rect = bg.gameObject.AddComponent<RectTransform>();
            
            // Stretch to fill canvas
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Add Image component with a nice dark blue color
            var img = bg.GetComponent<Image>();
            if (img == null) img = bg.gameObject.AddComponent<Image>();
            img.color = new Color(0.1f, 0.15f, 0.25f, 1f); // Dark pirate blue
            
            Debug.Log("[SimpleLoginController] Background setup done");
        }
        
        private void SetupTitle()
        {
            var titleObj = canvas.transform.Find("TitleText");
            if (titleObj == null)
            {
                Debug.Log("[SimpleLoginController] Creating Title...");
                var titleGO = new GameObject("TitleText");
                titleGO.transform.SetParent(canvas.transform, false);
                titleObj = titleGO.transform;
            }
            
            var rect = titleObj.GetComponent<RectTransform>();
            if (rect == null) rect = titleObj.gameObject.AddComponent<RectTransform>();
            
            // Position at top center
            rect.anchorMin = new Vector2(0.5f, 0.8f);
            rect.anchorMax = new Vector2(0.5f, 0.8f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 100);
            rect.anchoredPosition = Vector2.zero;
            
            // Add/configure TextMeshProUGUI
            var tmp = titleObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = titleObj.gameObject.AddComponent<TextMeshProUGUI>();
            
            tmp.text = "🏴‍☠️ Ahoy Matey! 🏴‍☠️";
            tmp.fontSize = 48;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.84f, 0f, 1f); // Gold color
            
            Debug.Log("[SimpleLoginController] Title setup done");
        }
        
        private void SetupButtons()
        {
            // Login Button - upper button
            var loginObj = canvas.transform.Find("LoginButton");
            if (loginObj != null)
            {
                SetupButton(loginObj.gameObject, "⚓ Log In", new Vector2(0.5f, 0.45f), 
                    new Color(0.2f, 0.5f, 0.3f, 1f)); // Green
            }
            
            // Create Account Button - lower button
            var createObj = canvas.transform.Find("CreateAccountButton");
            if (createObj != null)
            {
                SetupButton(createObj.gameObject, "🏴‍☠️ Join the Crew!", new Vector2(0.5f, 0.3f), 
                    new Color(0.6f, 0.3f, 0.1f, 1f)); // Brown/Orange
            }
        }
        
        private void SetupButton(GameObject btnObj, string text, Vector2 anchorPos, Color bgColor)
        {
            var rect = btnObj.GetComponent<RectTransform>();
            if (rect == null) rect = btnObj.AddComponent<RectTransform>();
            
            // Position
            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 60);
            rect.anchoredPosition = Vector2.zero;
            
            // Button Image
            var img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            
            // Ensure Button component exists
            var btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            
            // Find or create text child
            var textObj = btnObj.transform.Find("Text");
            if (textObj == null)
            {
                textObj = btnObj.transform.Find("Text (TMP)");
            }
            if (textObj == null)
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(btnObj.transform, false);
                textObj = textGO.transform;
            }
            
            var textRect = textObj.GetComponent<RectTransform>();
            if (textRect == null) textRect = textObj.gameObject.AddComponent<RectTransform>();
            
            // Stretch text to fill button
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            // Configure text
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textObj.gameObject.AddComponent<TextMeshProUGUI>();
            
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            Debug.Log($"[SimpleLoginController] Button '{text}' setup done");
        }
        
        private void WireButtons()
        {
            // Find buttons
            var loginObj = canvas.transform.Find("LoginButton");
            if (loginObj != null)
            {
                loginButton = loginObj.GetComponent<Button>();
                if (loginButton != null)
                {
                    loginButton.onClick.AddListener(OnLoginClicked);
                    Debug.Log("[SimpleLoginController] ✅ Login button wired");
                }
            }
            
            var createObj = canvas.transform.Find("CreateAccountButton");
            if (createObj != null)
            {
                createAccountButton = createObj.GetComponent<Button>();
                if (createAccountButton != null)
                {
                    createAccountButton.onClick.AddListener(OnCreateAccountClicked);
                    Debug.Log("[SimpleLoginController] ✅ Create Account button wired");
                }
            }
        }
        
        private void OnLoginClicked()
        {
            Debug.Log("[SimpleLoginController] 🔑 LOGIN CLICKED!");
            SceneManager.LoadScene("MainMenu");
        }
        
        private void OnCreateAccountClicked()
        {
            Debug.Log("[SimpleLoginController] 📝 CREATE ACCOUNT CLICKED!");
            SceneManager.LoadScene("Register");
        }
        
        private void OnDestroy()
        {
            if (loginButton != null) loginButton.onClick.RemoveListener(OnLoginClicked);
            if (createAccountButton != null) createAccountButton.onClick.RemoveListener(OnCreateAccountClicked);
        }
    }
}
