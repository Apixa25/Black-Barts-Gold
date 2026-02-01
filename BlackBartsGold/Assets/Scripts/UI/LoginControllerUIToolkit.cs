// ============================================================================
// LoginControllerUIToolkit.cs
// Black Bart's Gold - UI Toolkit Login Screen Controller
// Path: Assets/Scripts/UI/LoginControllerUIToolkit.cs
// ============================================================================
// Drives the UI Toolkit login form: email/password fields, Sign In button,
// and Create Account. Calls AuthService (dashboard API) and navigates on success.
// Reference: project-vision.md, user-accounts-security.md
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using BlackBartsGold.Core;
using System;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Login screen controller for UI Toolkit.
    /// Binds UXML form to AuthService and scene navigation.
    /// Adds UIDocument at runtime if missing.
    /// </summary>
    public class LoginControllerUIToolkit : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;
        private TextField _emailField;
        private TextField _passwordField;
        private Button _loginButton;
        private Button _registerButton;
        private Label _messageLabel;
        private bool _isLoggingIn;

        [Tooltip("Optional: assign in Inspector. If null, loads from Resources/UI Toolkit/Login/LoginScreen.")]
        [SerializeField] private VisualTreeAsset loginUXML;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = gameObject.AddComponent<UIDocument>();
                Debug.Log("[LoginControllerUIToolkit] Added UIDocument at runtime.");
            }
            if (_uiDocument == null)
            {
                Debug.LogError("[LoginControllerUIToolkit] UIDocument could not be added.");
                return;
            }

            // Load UXML from Resources if not assigned in Inspector
            if (_uiDocument.visualTreeAsset == null && loginUXML == null)
            {
                var tree = Resources.Load<VisualTreeAsset>("UI Toolkit/Login/LoginScreen");
                if (tree != null)
                {
                    _uiDocument.visualTreeAsset = tree;
                    Debug.Log("[LoginControllerUIToolkit] Loaded login UXML from Resources.");
                }
                else
                    Debug.LogError("[LoginControllerUIToolkit] No login UXML assigned and Resources/UI Toolkit/Login/LoginScreen not found.");
            }
            else if (loginUXML != null && _uiDocument.visualTreeAsset == null)
                _uiDocument.visualTreeAsset = loginUXML;
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[LoginControllerUIToolkit] rootVisualElement is null. Check UXML is assigned.");
                return;
            }

            _emailField = _root.Q<TextField>("email");
            _passwordField = _root.Q<TextField>("password");
            _loginButton = _root.Q<Button>("login");
            _registerButton = _root.Q<Button>("register");
            _messageLabel = _root.Q<Label>("message");

            if (_emailField == null || _passwordField == null || _loginButton == null || _messageLabel == null)
            {
                Debug.LogError("[LoginControllerUIToolkit] Missing required elements (email, password, login, message). Check UXML names.");
                return;
            }

            // Apply stylesheet if loaded from Resources (UXML Style src may not resolve)
            var sheet = Resources.Load<StyleSheet>("UI Toolkit/Login/LoginScreen");
            if (sheet != null)
                _root.styleSheets.Add(sheet);

            // Mask password input
            _passwordField.isPasswordField = true;

            _loginButton.clicked += OnLoginClicked;
            if (_registerButton != null)
                _registerButton.clicked += OnRegisterClicked;

            AuthService.Instance.OnLoginSuccess += OnLoginSuccess;
            AuthService.Instance.OnAuthError += OnAuthError;

            ClearMessage();

            // Hide the old uGUI login canvas so only UI Toolkit is visible
            var oldCanvas = GameObject.Find("LoginCanvas");
            if (oldCanvas != null)
            {
                oldCanvas.SetActive(false);
                Debug.Log("[LoginControllerUIToolkit] Hid legacy LoginCanvas.");
            }

            Debug.Log("[LoginControllerUIToolkit] ✅ UI Toolkit login bound.");
        }

        private void OnDisable()
        {
            if (_loginButton != null)
                _loginButton.clicked -= OnLoginClicked;
            if (_registerButton != null)
                _registerButton.clicked -= OnRegisterClicked;

            if (AuthService.Exists)
            {
                AuthService.Instance.OnLoginSuccess -= OnLoginSuccess;
                AuthService.Instance.OnAuthError -= OnAuthError;
            }
        }

        private void OnLoginClicked()
        {
            if (_isLoggingIn) return;

            string email = _emailField?.value?.Trim() ?? "";
            string password = _passwordField?.value ?? "";

            var validation = AuthService.Instance.ValidateLogin(email, password);
            if (!validation.isValid)
            {
                ShowMessage(validation.error, isError: true);
                return;
            }

            _isLoggingIn = true;
            SetInteractive(false);
            ShowMessage("Signing in…");

            _ = DoLoginAsync(email, password);
        }

        private async System.Threading.Tasks.Task DoLoginAsync(string email, string password)
        {
            try
            {
                var user = await AuthService.Instance.Login(email, password);
                if (user != null)
                {
                    ShowMessage("Welcome back!");
                    // OnLoginSuccess will fire; we navigate there to avoid double navigation
                }
                else
                {
                    ShowMessage(AuthService.Instance.LastError ?? "Login failed.", isError: true);
                    SetInteractive(true);
                }
            }
            catch (Exception e)
            {
                ShowMessage(e.Message, isError: true);
                SetInteractive(true);
            }
            finally
            {
                _isLoggingIn = false;
            }
        }

        private void OnLoginSuccess(Core.Models.User user)
        {
            SetInteractive(true);
            SceneLoader.LoadScene(SceneNames.MainMenu);
        }

        private void OnAuthError(string message)
        {
            _isLoggingIn = false;
            SetInteractive(true);
            ShowMessage(message ?? "Authentication error.", isError: true);
        }

        private void OnRegisterClicked()
        {
            SceneLoader.LoadScene(SceneNames.Register);
        }

        private void ShowMessage(string text, bool isError = false)
        {
            if (_messageLabel == null) return;
            _messageLabel.text = text;
            _messageLabel.RemoveFromClassList("error");
            if (isError)
                _messageLabel.AddToClassList("error");
        }

        private void ClearMessage()
        {
            ShowMessage("");
        }

        private void SetInteractive(bool interactive)
        {
            if (_loginButton != null)
                _loginButton.SetEnabled(interactive);
            if (_registerButton != null)
                _registerButton.SetEnabled(interactive);
            if (_emailField != null)
                _emailField.SetEnabled(interactive);
            if (_passwordField != null)
                _passwordField.SetEnabled(interactive);
        }
    }
}
