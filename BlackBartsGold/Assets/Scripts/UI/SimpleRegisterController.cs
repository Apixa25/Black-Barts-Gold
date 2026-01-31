// ============================================================================
// SimpleRegisterController.cs
// Black Bart's Gold - Simple Register Screen Controller
// Path: Assets/Scripts/UI/SimpleRegisterController.cs
// ============================================================================
// SIMPLIFIED: Sets up UI visually and handles navigation back to Login.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Simple register controller that sets up and handles the register UI.
    /// </summary>
    public class SimpleRegisterController : MonoBehaviour
    {
        private Button registerButton;
        private Button backToLoginButton;
        private Canvas canvas;
        
        private void Awake()
        {
            Debug.Log("[SimpleRegisterController] Awake - Setting up UI...");
            SetupUI();
        }
        
        private void Start()
        {
            Debug.Log("[SimpleRegisterController] Start - Wiring buttons...");
            WireButtons();
            Debug.Log("[SimpleRegisterController] ✅ Ready!");
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
                // Create a canvas if none exists
                var canvasGO = new GameObject("RegisterCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // Ensure canvas is Screen Space - Overlay
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            // Setup Background
            SetupBackground();
            
            // Setup Title
            SetupTitle();
            
            // Setup instruction text
            SetupInstructions();
            
            // Setup Buttons
            SetupButtons();
            
            Debug.Log("[SimpleRegisterController] ✅ UI Setup complete");
        }
        
        private void SetupBackground()
        {
            var bg = canvas.transform.Find("Background");
            if (bg == null)
            {
                var bgGO = new GameObject("Background");
                bgGO.transform.SetParent(canvas.transform, false);
                bgGO.transform.SetAsFirstSibling();
                bg = bgGO.transform;
            }
            
            var rect = bg.GetComponent<RectTransform>();
            if (rect == null) rect = bg.gameObject.AddComponent<RectTransform>();
            
            // Stretch to fill canvas
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Add Image component with dark blue color
            var img = bg.GetComponent<Image>();
            if (img == null) img = bg.gameObject.AddComponent<Image>();
            img.color = new Color(0.12f, 0.18f, 0.28f, 1f); // Slightly different blue
        }
        
        private void SetupTitle()
        {
            var titleObj = canvas.transform.Find("TitleText");
            if (titleObj == null)
            {
                var titleGO = new GameObject("TitleText");
                titleGO.transform.SetParent(canvas.transform, false);
                titleObj = titleGO.transform;
            }
            
            var rect = titleObj.GetComponent<RectTransform>();
            if (rect == null) rect = titleObj.gameObject.AddComponent<RectTransform>();
            
            rect.anchorMin = new Vector2(0.5f, 0.85f);
            rect.anchorMax = new Vector2(0.5f, 0.85f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 80);
            rect.anchoredPosition = Vector2.zero;
            
            var tmp = titleObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = titleObj.gameObject.AddComponent<TextMeshProUGUI>();
            
            tmp.text = "Join the Crew!";
            tmp.fontSize = 48;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.84f, 0f, 1f); // Gold
        }
        
        private void SetupInstructions()
        {
            var instrObj = canvas.transform.Find("Instructions");
            if (instrObj == null)
            {
                var instrGO = new GameObject("Instructions");
                instrGO.transform.SetParent(canvas.transform, false);
                instrObj = instrGO.transform;
            }
            
            var rect = instrObj.GetComponent<RectTransform>();
            if (rect == null) rect = instrObj.gameObject.AddComponent<RectTransform>();
            
            rect.anchorMin = new Vector2(0.5f, 0.6f);
            rect.anchorMax = new Vector2(0.5f, 0.6f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500, 150);
            rect.anchoredPosition = Vector2.zero;
            
            var tmp = instrObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = instrObj.gameObject.AddComponent<TextMeshProUGUI>();
            
            tmp.text = "Registration coming soon!\n\nFor now, click below to\nreturn to Login.";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }
        
        private void SetupButtons()
        {
            // Create Register button (disabled/placeholder for now)
            CreateOrSetupButton("RegisterButton", "Register (Coming Soon)", 
                new Vector2(0.5f, 0.35f), new Color(0.4f, 0.4f, 0.4f, 1f));
            
            // Create Back to Login button
            CreateOrSetupButton("BackButton", "Back to Login", 
                new Vector2(0.5f, 0.2f), new Color(0.2f, 0.5f, 0.3f, 1f));
        }
        
        private void CreateOrSetupButton(string name, string text, Vector2 anchorPos, Color bgColor)
        {
            var btnObj = canvas.transform.Find(name);
            if (btnObj == null)
            {
                var btnGO = new GameObject(name);
                btnGO.transform.SetParent(canvas.transform, false);
                btnObj = btnGO.transform;
            }
            
            var rect = btnObj.GetComponent<RectTransform>();
            if (rect == null) rect = btnObj.gameObject.AddComponent<RectTransform>();
            
            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 60);
            rect.anchoredPosition = Vector2.zero;
            
            var img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.gameObject.AddComponent<Image>();
            img.color = bgColor;
            
            var btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            
            // Find or create text child
            var textObj = btnObj.Find("Text");
            if (textObj == null)
            {
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(btnObj, false);
                textObj = textGO.transform;
            }
            
            var textRect = textObj.GetComponent<RectTransform>();
            if (textRect == null) textRect = textObj.gameObject.AddComponent<RectTransform>();
            
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textObj.gameObject.AddComponent<TextMeshProUGUI>();
            
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            Debug.Log($"[SimpleRegisterController] Button '{text}' setup done");
        }
        
        private void WireButtons()
        {
            var backObj = canvas.transform.Find("BackButton");
            if (backObj != null)
            {
                backToLoginButton = backObj.GetComponent<Button>();
                if (backToLoginButton != null)
                {
                    backToLoginButton.onClick.AddListener(OnBackToLoginClicked);
                    Debug.Log("[SimpleRegisterController] ✅ Back button wired");
                }
            }
            
            // Register button is placeholder for now
            var regObj = canvas.transform.Find("RegisterButton");
            if (regObj != null)
            {
                registerButton = regObj.GetComponent<Button>();
                if (registerButton != null)
                {
                    registerButton.onClick.AddListener(OnRegisterClicked);
                    Debug.Log("[SimpleRegisterController] ✅ Register button wired (placeholder)");
                }
            }
        }
        
        private void OnBackToLoginClicked()
        {
            Debug.Log("[SimpleRegisterController] Back to Login clicked!");
            SceneManager.LoadScene("Login");
        }
        
        private void OnRegisterClicked()
        {
            Debug.Log("[SimpleRegisterController] Register clicked (not implemented yet)");
            // TODO: Implement actual registration
        }
        
        private void OnDestroy()
        {
            if (backToLoginButton != null) backToLoginButton.onClick.RemoveListener(OnBackToLoginClicked);
            if (registerButton != null) registerButton.onClick.RemoveListener(OnRegisterClicked);
        }
    }
}
