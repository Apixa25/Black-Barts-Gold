// ============================================================================
// RegisterButtonHandler.cs
// Register scene controller - creates registration form and calls AuthService
// Path: Assets/Scripts/UI/RegisterButtonHandler.cs
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using BlackBartsGold.Core;

namespace BlackBartsGold.UI
{
    public class RegisterButtonHandler : MonoBehaviour
    {
        private Button backButton;
        private Button registerButton;
        private TMP_InputField emailInput;
        private TMP_InputField passwordInput;
        private TMP_InputField confirmPasswordInput;
        private TMP_InputField displayNameInput;
        private TMP_InputField ageInput;
        private TMP_Text messageText;
        private bool isRegistering;
        
        private IEnumerator Start()
        {
            Debug.Log("[RegisterButtonHandler] Start - waiting a frame...");
            
            yield return null;
            
            Debug.Log("[RegisterButtonHandler] Creating registration UI...");
            CreateUI();
        }
        
        private void CreateUI()
        {
            try
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                
                if (canvas == null)
                {
                    var canvasGO = new GameObject("RegisterCanvas");
                    canvas = canvasGO.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    var scaler = canvasGO.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080f, 1920f);
                    scaler.matchWidthOrHeight = 0.5f;
                    canvasGO.AddComponent<GraphicRaycaster>();
                }
                else
                {
                    var scaler = canvas.GetComponent<CanvasScaler>();
                    if (scaler != null)
                    {
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1080f, 1920f);
                        scaler.matchWidthOrHeight = 0.5f;
                    }
                }
                
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                // Clear existing children so we build fresh
                foreach (Transform child in canvas.transform)
                {
                    Destroy(child.gameObject);
                }
                
                // Background panel
                var panel = new GameObject("Panel");
                panel.transform.SetParent(canvas.transform, false);
                var panelRect = panel.AddComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                panel.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 1f);
                
                // Title - large and readable on mobile
                var title = CreateText(canvas.transform, "Title", "Join the Crew!", 0.88f, 56, new Color(1f, 0.84f, 0f, 1f));
                
                // Create scroll container for form fields
                float y = 0.78f;
                
                // Email
                var emailRow = CreateInputRow(canvas.transform, "Email", "Email", ref y);
                emailInput = emailRow.inputField;
                emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
                
                // Display name
                var nameRow = CreateInputRow(canvas.transform, "DisplayName", "Display Name", ref y);
                displayNameInput = nameRow.inputField;
                
                // Password
                var passRow = CreateInputRow(canvas.transform, "Password", "Password (min 8 chars)", ref y);
                passwordInput = passRow.inputField;
                passwordInput.contentType = TMP_InputField.ContentType.Password;
                
                // Confirm password
                var confirmRow = CreateInputRow(canvas.transform, "ConfirmPassword", "Confirm Password", ref y);
                confirmPasswordInput = confirmRow.inputField;
                confirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
                
                // Age (simple input, must be 13+)
                var ageRow = CreateInputRow(canvas.transform, "Age", "Age (13+)", ref y);
                ageInput = ageRow.inputField;
                ageInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                
                // Message label (errors/success)
                var msgGO = new GameObject("Message");
                msgGO.transform.SetParent(canvas.transform, false);
                var msgRect = msgGO.AddComponent<RectTransform>();
                msgRect.anchorMin = new Vector2(0.5f, y - 0.05f);
                msgRect.anchorMax = new Vector2(0.5f, y - 0.05f);
                msgRect.sizeDelta = new Vector2(500, 60);
                msgRect.pivot = new Vector2(0.5f, 0.5f);
                messageText = msgGO.AddComponent<TextMeshProUGUI>();
                messageText.text = "";
                messageText.fontSize = 36;
                messageText.alignment = TextAlignmentOptions.Center;
                messageText.color = new Color(1f, 0.4f, 0.4f, 1f);
                y -= 0.1f;
                
                // Register button
                registerButton = CreateButton(canvas.transform, "Register", "Create Account", y, new Color(0.6f, 0.3f, 0.1f, 1f));
                registerButton.onClick.AddListener(OnRegisterClicked);
                y -= 0.1f;
                
                // Back button
                backButton = CreateButton(canvas.transform, "BackButton", "Back to Login", y, new Color(0.2f, 0.5f, 0.3f, 1f));
                backButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneNames.Login));
                
                Debug.Log("[RegisterButtonHandler] ✅ Registration form created and wired!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RegisterButtonHandler] Error: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private (GameObject go, TMP_InputField inputField) CreateInputRow(Transform parent, string name, string label, ref float y)
        {
            y -= 0.07f;
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, y);
            rowRect.anchorMax = new Vector2(0.5f, y);
            rowRect.sizeDelta = new Vector2(500, 80);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            
            // Background on ROW (only one Graphic per object - Image here)
            var bgImage = row.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            
            // Input field - proper TMP_InputField hierarchy
            var inputGO = new GameObject("Input");
            inputGO.transform.SetParent(row.transform, false);
            var inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(12, 8);
            inputRect.offsetMax = new Vector2(-12, -8);
            
            var input = inputGO.AddComponent<TMP_InputField>();
            
            // Text Area (viewport)
            var textAreaGO = new GameObject("Text Area");
            textAreaGO.transform.SetParent(inputGO.transform, false);
            var textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;
            textAreaGO.AddComponent<RectMask2D>();
            
            // Text (what user types)
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.5f);
            textRect.anchorMax = new Vector2(1, 0.5f);
            textRect.offsetMin = new Vector2(0, -20);
            textRect.offsetMax = new Vector2(0, 20);
            var inputText = textGO.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 36;
            inputText.color = Color.white;
            
            // Placeholder
            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(textAreaGO.transform, false);
            var phRect = phGO.AddComponent<RectTransform>();
            phRect.anchorMin = new Vector2(0, 0.5f);
            phRect.anchorMax = new Vector2(1, 0.5f);
            phRect.offsetMin = new Vector2(0, -20);
            phRect.offsetMax = new Vector2(0, 20);
            var placeholder = phGO.AddComponent<TextMeshProUGUI>();
            placeholder.text = label;
            placeholder.fontSize = 36;
            placeholder.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            
            input.textComponent = inputText;
            input.textViewport = textAreaRect;
            input.placeholder = placeholder;
            input.targetGraphic = bgImage;
            
            return (row, input);
        }
        
        private GameObject CreateText(Transform parent, string name, string text, float y, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, y);
            rect.anchorMax = new Vector2(0.5f, y);
            rect.sizeDelta = new Vector2(600, 80);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return go;
        }
        
        private Button CreateButton(Transform parent, string name, string text, float y, Color bgColor)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, y);
            btnRect.anchorMax = new Vector2(0.5f, y);
            btnRect.sizeDelta = new Vector2(420, 90);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            var btnImage = btnGO.AddComponent<Image>();
            btnImage.color = bgColor;
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRect = txtGO.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 42;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            
            return btn;
        }
        
        private async void OnRegisterClicked()
        {
            if (isRegistering) return;
            
            string email = emailInput?.text?.Trim() ?? "";
            string displayName = displayNameInput?.text?.Trim() ?? "";
            string password = passwordInput?.text ?? "";
            string confirmPassword = confirmPasswordInput?.text ?? "";
            int age = 0;
            int.TryParse(ageInput?.text?.Trim() ?? "0", out age);
            
            if (messageText != null) messageText.text = "";
            
            if (!AuthService.Exists)
            {
                SetMessage("Authentication service unavailable", true);
                return;
            }
            
            var validation = AuthService.Instance.ValidateRegistration(email, password, confirmPassword, age);
            if (!validation.isValid)
            {
                SetMessage(validation.error, true);
                return;
            }
            
            if (string.IsNullOrEmpty(displayName))
            {
                SetMessage("Display name is required", true);
                return;
            }
            
            if (displayName.Length < 3 || displayName.Length > 20)
            {
                SetMessage("Display name must be 3–20 characters", true);
                return;
            }
            
            isRegistering = true;
            if (registerButton != null) registerButton.interactable = false;
            if (backButton != null) backButton.interactable = false;
            SetMessage("Creating account...", false);
            
            try
            {
                var user = await AuthService.Instance.Register(email, password, displayName, age);
                
                if (user != null)
                {
                    SetMessage($"Welcome aboard, {user.displayName}!", false);
                    messageText.color = new Color(0.3f, 1f, 0.5f, 1f);
                    await System.Threading.Tasks.Task.Delay(1500);
                    SceneLoader.LoadScene(SceneNames.MainMenu);
                }
                else
                {
                    SetMessage(AuthService.Instance.LastError ?? "Registration failed", true);
                }
            }
            catch (System.Exception e)
            {
                SetMessage(e.Message, true);
            }
            finally
            {
                isRegistering = false;
                if (registerButton != null) registerButton.interactable = true;
                if (backButton != null) backButton.interactable = true;
            }
        }
        
        private void SetMessage(string text, bool isError)
        {
            if (messageText == null) return;
            messageText.text = text;
            messageText.color = isError ? new Color(1f, 0.4f, 0.4f, 1f) : new Color(0.3f, 1f, 0.5f, 1f);
        }
    }
}
