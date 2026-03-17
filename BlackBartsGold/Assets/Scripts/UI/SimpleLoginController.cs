// ============================================================================
// SimpleLoginController.cs
// Black Bart's Gold - Simple Login Screen Controller
// Path: Assets/Scripts/UI/SimpleLoginController.cs
// ============================================================================
// Sets up login UI: shows "Login" / "Join the Crew" buttons. When Login clicked,
// shows inline email/password form and calls AuthService. No UI Toolkit dependency.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using BlackBartsGold.Core;
namespace BlackBartsGold.UI
{
    /// <summary>
    /// Login controller: two-button home screen, then inline email/password form.
    /// </summary>
    public class SimpleLoginController : MonoBehaviour
    {
        private Button loginButton;
        private Button createAccountButton;
        private Canvas canvas;
        private GameObject loginFormContainer;
        private TMP_InputField emailInput;
        private TMP_InputField passwordInput;
        private TMP_Text messageText;
        private Button signInButton;
        private Button backButton;
        private bool isLoggingIn;
        
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
            
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            
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
            
            var img = bg.GetComponent<Image>();
            if (img == null) img = bg.gameObject.AddComponent<Image>();
            img.sprite = BBGSprites.PanelWood;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.2f, 0.15f, 1f);
            
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
            rect.sizeDelta = new Vector2(800, 120);
            rect.anchoredPosition = Vector2.zero;
            
            // Add/configure TextMeshProUGUI
            var tmp = titleObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = titleObj.gameObject.AddComponent<TextMeshProUGUI>();
            
            tmp.text = "There's treasure everywhere! Get ready to find it!";
            tmp.fontSize = 48;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = BBGThemeProvider.Gold;
            
            Debug.Log("[SimpleLoginController] Title setup done");
        }
        
        private void SetupButtons()
        {
            // Login Button - upper button
            var loginObj = canvas.transform.Find("LoginButton");
            if (loginObj != null)
            {
                SetupButton(loginObj.gameObject, "Log In", new Vector2(0.5f, 0.45f), 
                    new Color(0.2f, 0.5f, 0.3f, 1f)); // Green
            }
            
            // Create Account Button - lower button
            var createObj = canvas.transform.Find("CreateAccountButton");
            if (createObj != null)
            {
                SetupButton(createObj.gameObject, "Join the Crew!", new Vector2(0.5f, 0.3f), 
                    new Color(0.6f, 0.3f, 0.1f, 1f)); // Brown/Orange
            }
        }
        
        private void SetupButton(GameObject btnObj, string text, Vector2 anchorPos, Color bgColor)
        {
            var rect = btnObj.GetComponent<RectTransform>();
            if (rect == null) rect = btnObj.AddComponent<RectTransform>();

            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420, 90);
            rect.anchoredPosition = Vector2.zero;

            bool isPrimary = text.Contains("Log In");
            var variant = isPrimary ? BBGButtonVariant.Primary : BBGButtonVariant.Secondary;
            BBGButton.Upgrade(btnObj, variant);

            var bbg = btnObj.GetComponent<BBGButton>();
            if (bbg != null) bbg.SetText(text);

            Debug.Log($"[SimpleLoginController] Button '{text}' themed with BBGButton ({variant})");
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
            Debug.Log("[SimpleLoginController] 🔑 LOGIN CLICKED - Showing email/password form...");
            
            if (loginFormContainer != null) return;
            
            // Hide main buttons
            if (loginButton != null) loginButton.gameObject.SetActive(false);
            if (createAccountButton != null) createAccountButton.gameObject.SetActive(false);
            
            // Build inline login form (uGUI - works on device, no UI Toolkit)
            ShowLoginForm();
        }
        
        private void ShowLoginForm()
        {
            loginFormContainer = new GameObject("LoginFormContainer");
            loginFormContainer.transform.SetParent(canvas.transform, false);
            var rect = loginFormContainer.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            float y = 0.7f;
            var emailRow = CreateLoginInputRow(loginFormContainer.transform, "Email", "Email", ref y);
            emailInput = emailRow;
            emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
            
            var passRow = CreateLoginInputRow(loginFormContainer.transform, "Password", "Password", ref y);
            passwordInput = passRow;
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            
            var msgGO = new GameObject("Message");
            msgGO.transform.SetParent(loginFormContainer.transform, false);
            var msgRect = msgGO.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.5f, y - 0.05f);
            msgRect.anchorMax = new Vector2(0.5f, y - 0.05f);
            msgRect.sizeDelta = new Vector2(500, 50);
            msgRect.pivot = new Vector2(0.5f, 0.5f);
            messageText = msgGO.AddComponent<TextMeshProUGUI>();
            messageText.text = "";
            messageText.fontSize = 32;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = new Color(1f, 0.4f, 0.4f, 1f);
            y -= 0.12f;
            
            signInButton = CreateLoginButton(loginFormContainer.transform, "Sign In", y, new Color(0.2f, 0.5f, 0.3f, 1f));
            signInButton.onClick.AddListener(OnSignInClicked);
            y -= 0.12f;
            
            backButton = CreateLoginButton(loginFormContainer.transform, "Back", y, new Color(0.4f, 0.4f, 0.4f, 1f));
            backButton.onClick.AddListener(HideLoginForm);
            
            Debug.Log("[SimpleLoginController] Login form shown.");
        }
        
        private TMP_InputField CreateLoginInputRow(Transform parent, string name, string label, ref float y)
        {
            y -= 0.12f;
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, y);
            rowRect.anchorMax = new Vector2(0.5f, y);
            rowRect.sizeDelta = new Vector2(450, 75);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            row.AddComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 1f);
            
            var inputGO = new GameObject("Input");
            inputGO.transform.SetParent(row.transform, false);
            var inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(12, 8);
            inputRect.offsetMax = new Vector2(-12, -8);
            var input = inputGO.AddComponent<TMP_InputField>();
            
            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputGO.transform, false);
            var taRect = textArea.AddComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = Vector2.zero;
            taRect.offsetMax = Vector2.zero;
            textArea.AddComponent<RectMask2D>();
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textArea.transform, false);
            var tr = textGO.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 0.5f);
            tr.anchorMax = new Vector2(1, 0.5f);
            tr.offsetMin = new Vector2(0, -18);
            tr.offsetMax = new Vector2(0, 18);
            var it = textGO.AddComponent<TextMeshProUGUI>();
            it.fontSize = 32;
            it.color = Color.white;
            
            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(textArea.transform, false);
            var phR = phGO.AddComponent<RectTransform>();
            phR.anchorMin = new Vector2(0, 0.5f);
            phR.anchorMax = new Vector2(1, 0.5f);
            phR.offsetMin = new Vector2(0, -18);
            phR.offsetMax = new Vector2(0, 18);
            var ph = phGO.AddComponent<TextMeshProUGUI>();
            ph.text = label;
            ph.fontSize = 32;
            ph.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            
            input.textComponent = it;
            input.textViewport = taRect;
            input.placeholder = ph;
            input.targetGraphic = row.GetComponent<Image>();
            return input;
        }
        
        private Button CreateLoginButton(Transform parent, string text, float y, Color bgColor)
        {
            bool isPrimary = text.Contains("Sign In");
            var variant = isPrimary ? BBGButtonVariant.Primary : BBGButtonVariant.Ghost;
            var bbgBtn = BBGButton.Create(parent, text, variant, new Vector2(420, 90));
            bbgBtn.RectTransform.anchorMin = new Vector2(0.5f, y);
            bbgBtn.RectTransform.anchorMax = new Vector2(0.5f, y);
            bbgBtn.RectTransform.pivot = new Vector2(0.5f, 0.5f);
            bbgBtn.RectTransform.anchoredPosition = Vector2.zero;
            return bbgBtn.UnityButton;
        }
        
        private async void OnSignInClicked()
        {
            if (isLoggingIn) return;
            string email = emailInput?.text?.Trim() ?? "";
            string password = passwordInput?.text ?? "";
            
            if (messageText != null) messageText.text = "";
            if (!AuthService.Exists) { SetLoginMessage("Auth unavailable"); return; }
            var v = AuthService.Instance.ValidateLogin(email, password);
            if (!v.isValid) { SetLoginMessage(v.error); return; }
            
            isLoggingIn = true;
            signInButton.interactable = false;
            backButton.interactable = false;
            SetLoginMessage("Signing in...");
            
            try
            {
                var user = await AuthService.Instance.Login(email, password);
                if (user != null)
                {
                    SceneLoader.LoadScene(SceneNames.MainMenu);
                }
                else
                {
                    SetLoginMessage(AuthService.Instance.LastError ?? "Login failed");
                }
            }
            catch (System.Exception e)
            {
                SetLoginMessage(e.Message);
            }
            finally
            {
                isLoggingIn = false;
                signInButton.interactable = true;
                backButton.interactable = true;
            }
        }
        
        private void SetLoginMessage(string msg)
        {
            if (messageText != null) messageText.text = msg;
        }
        
        private void HideLoginForm()
        {
            if (loginFormContainer != null)
            {
                Destroy(loginFormContainer);
                loginFormContainer = null;
            }
            if (loginButton != null) loginButton.gameObject.SetActive(true);
            if (createAccountButton != null) createAccountButton.gameObject.SetActive(true);
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
