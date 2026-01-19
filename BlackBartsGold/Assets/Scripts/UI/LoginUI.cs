// ============================================================================
// LoginUI.cs
// Black Bart's Gold - Login Screen Controller
// Path: Assets/Scripts/UI/LoginUI.cs
// ============================================================================
// Controls the login screen UI. Handles email/password login, Google login,
// and navigation to registration.
// Reference: BUILD-GUIDE.md Sprint 6, Prompt 6.2
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Login screen controller.
    /// Handles user authentication via email/password or social login.
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Input Fields")]
        [SerializeField]
        private TMP_InputField emailInput;
        
        [SerializeField]
        private TMP_InputField passwordInput;
        
        [Header("Buttons")]
        [SerializeField]
        private Button loginButton;
        
        [SerializeField]
        private Button googleLoginButton;
        
        [SerializeField]
        private Button createAccountButton;
        
        [SerializeField]
        private Button forgotPasswordButton;
        
        [Header("UI Elements")]
        [SerializeField]
        private TMP_Text titleText;
        
        [SerializeField]
        private TMP_Text errorText;
        
        [SerializeField]
        private TMP_Text sessionExpiredText;
        
        [SerializeField]
        private GameObject loadingOverlay;
        
        [SerializeField]
        private TMP_Text loadingText;
        
        [Header("Password Visibility")]
        [SerializeField]
        private Button togglePasswordButton;
        
        [SerializeField]
        private Image passwordVisibilityIcon;
        
        [SerializeField]
        private Sprite eyeOpenSprite;
        
        [SerializeField]
        private Sprite eyeClosedSprite;
        
        [Header("Validation")]
        [SerializeField]
        private Image emailValidationIcon;
        
        [SerializeField]
        private Image passwordValidationIcon;
        
        [SerializeField]
        private Color validColor = new Color(0.29f, 0.87f, 0.5f); // Green
        
        [SerializeField]
        private Color invalidColor = new Color(0.94f, 0.27f, 0.27f); // Red
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private bool isPasswordVisible = false;
        private bool isLoggingIn = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            SetupUI();
            SetupListeners();
            CheckSessionExpiredMessage();
            HideLoading();
            ClearError();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (AuthService.Exists)
            {
                AuthService.Instance.OnLoginSuccess -= HandleLoginSuccess;
                AuthService.Instance.OnAuthError -= HandleAuthError;
            }
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Setup UI initial state
        /// </summary>
        private void SetupUI()
        {
            // Set title
            if (titleText != null)
            {
                titleText.text = "🏴‍☠️ Ahoy, Matey!";
            }
            
            // Configure password field
            if (passwordInput != null)
            {
                passwordInput.contentType = TMP_InputField.ContentType.Password;
            }
            
            // Hide validation icons initially
            if (emailValidationIcon != null)
            {
                emailValidationIcon.gameObject.SetActive(false);
            }
            
            if (passwordValidationIcon != null)
            {
                passwordValidationIcon.gameObject.SetActive(false);
            }
            
            // Hide session expired text
            if (sessionExpiredText != null)
            {
                sessionExpiredText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupListeners()
        {
            // Buttons
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginClicked);
            }
            
            if (googleLoginButton != null)
            {
                googleLoginButton.onClick.AddListener(OnGoogleLoginClicked);
            }
            
            if (createAccountButton != null)
            {
                createAccountButton.onClick.AddListener(OnCreateAccountClicked);
            }
            
            if (forgotPasswordButton != null)
            {
                forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);
            }
            
            if (togglePasswordButton != null)
            {
                togglePasswordButton.onClick.AddListener(OnTogglePasswordVisibility);
            }
            
            // Input field validation
            if (emailInput != null)
            {
                emailInput.onValueChanged.AddListener(OnEmailChanged);
                emailInput.onSubmit.AddListener(_ => OnLoginClicked());
            }
            
            if (passwordInput != null)
            {
                passwordInput.onValueChanged.AddListener(OnPasswordChanged);
                passwordInput.onSubmit.AddListener(_ => OnLoginClicked());
            }
            
            // Subscribe to AuthService events
            if (AuthService.Exists)
            {
                AuthService.Instance.OnLoginSuccess += HandleLoginSuccess;
                AuthService.Instance.OnAuthError += HandleAuthError;
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        /// <summary>
        /// Handle login button click
        /// </summary>
        private async void OnLoginClicked()
        {
            if (isLoggingIn) return;
            
            Log("Login clicked");
            
            // Clear previous error
            ClearError();
            
            // Get input values
            string email = emailInput?.text?.Trim() ?? "";
            string password = passwordInput?.text ?? "";
            
            // Validate
            if (!AuthService.Exists)
            {
                ShowError("Authentication service unavailable");
                return;
            }
            
            var validation = AuthService.Instance.ValidateLogin(email, password);
            if (!validation.isValid)
            {
                ShowError(validation.error);
                return;
            }
            
            // Start login
            isLoggingIn = true;
            ShowLoading("Logging in...");
            
            try
            {
                var user = await AuthService.Instance.Login(email, password);
                
                if (user != null)
                {
                    Log($"Login successful: {user.displayName}");
                    // Navigation handled by HandleLoginSuccess
                }
            }
            catch (Exception e)
            {
                ShowError($"Login failed: {e.Message}");
            }
            finally
            {
                isLoggingIn = false;
                HideLoading();
            }
        }
        
        /// <summary>
        /// Handle Google login button click
        /// </summary>
        private async void OnGoogleLoginClicked()
        {
            if (isLoggingIn) return;
            
            Log("Google login clicked");
            ClearError();
            
            if (!AuthService.Exists)
            {
                ShowError("Authentication service unavailable");
                return;
            }
            
            isLoggingIn = true;
            ShowLoading("Signing in with Google...");
            
            try
            {
                var user = await AuthService.Instance.LoginWithGoogle();
                
                if (user != null)
                {
                    Log($"Google login successful: {user.displayName}");
                }
            }
            catch (Exception e)
            {
                ShowError($"Google login failed: {e.Message}");
            }
            finally
            {
                isLoggingIn = false;
                HideLoading();
            }
        }
        
        /// <summary>
        /// Handle create account button click
        /// </summary>
        private void OnCreateAccountClicked()
        {
            Log("Create account clicked");
            SceneLoader.LoadScene(SceneNames.Register);
        }
        
        /// <summary>
        /// Handle forgot password button click
        /// </summary>
        private void OnForgotPasswordClicked()
        {
            Log("Forgot password clicked");
            // TODO: Implement password reset flow
            ShowError("Password reset coming soon! 🏴‍☠️");
        }
        
        /// <summary>
        /// Toggle password visibility
        /// </summary>
        private void OnTogglePasswordVisibility()
        {
            isPasswordVisible = !isPasswordVisible;
            
            if (passwordInput != null)
            {
                passwordInput.contentType = isPasswordVisible 
                    ? TMP_InputField.ContentType.Standard 
                    : TMP_InputField.ContentType.Password;
                passwordInput.ForceLabelUpdate();
            }
            
            if (passwordVisibilityIcon != null)
            {
                passwordVisibilityIcon.sprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
            }
        }
        
        #endregion
        
        #region Input Validation
        
        /// <summary>
        /// Handle email input change
        /// </summary>
        private void OnEmailChanged(string value)
        {
            if (emailValidationIcon == null) return;
            
            if (string.IsNullOrEmpty(value))
            {
                emailValidationIcon.gameObject.SetActive(false);
                return;
            }
            
            bool isValid = AuthService.Exists && AuthService.Instance.IsValidEmail(value);
            emailValidationIcon.gameObject.SetActive(true);
            emailValidationIcon.color = isValid ? validColor : invalidColor;
        }
        
        /// <summary>
        /// Handle password input change
        /// </summary>
        private void OnPasswordChanged(string value)
        {
            if (passwordValidationIcon == null) return;
            
            if (string.IsNullOrEmpty(value))
            {
                passwordValidationIcon.gameObject.SetActive(false);
                return;
            }
            
            bool isValid = value.Length >= AuthService.MIN_PASSWORD_LENGTH;
            passwordValidationIcon.gameObject.SetActive(true);
            passwordValidationIcon.color = isValid ? validColor : invalidColor;
        }
        
        #endregion
        
        #region Auth Event Handlers
        
        /// <summary>
        /// Handle successful login
        /// </summary>
        private void HandleLoginSuccess(User user)
        {
            Log($"Login success handler: {user.displayName}");
            HideLoading();
            
            // Mark session manager
            if (SessionManager.Exists)
            {
                SessionManager.Instance.MarkAppLaunched();
            }
            
            // Navigate to main menu
            SceneLoader.LoadScene(SceneNames.MainMenu);
        }
        
        /// <summary>
        /// Handle auth error
        /// </summary>
        private void HandleAuthError(string error)
        {
            Log($"Auth error: {error}");
            HideLoading();
            ShowError(error);
        }
        
        #endregion
        
        #region UI Helpers
        
        /// <summary>
        /// Show loading overlay
        /// </summary>
        private void ShowLoading(string message = "Loading...")
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }
            
            if (loadingText != null)
            {
                loadingText.text = message;
            }
            
            // Disable buttons while loading
            SetButtonsInteractable(false);
        }
        
        /// <summary>
        /// Hide loading overlay
        /// </summary>
        private void HideLoading()
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
            
            SetButtonsInteractable(true);
        }
        
        /// <summary>
        /// Show error message
        /// </summary>
        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
            
            Log($"Error shown: {message}");
        }
        
        /// <summary>
        /// Clear error message
        /// </summary>
        private void ClearError()
        {
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Check for session expired message
        /// </summary>
        private void CheckSessionExpiredMessage()
        {
            if (!SessionManager.Exists) return;
            
            string message = SessionManager.Instance.GetAndClearSessionExpiredMessage();
            
            if (!string.IsNullOrEmpty(message) && sessionExpiredText != null)
            {
                sessionExpiredText.text = message;
                sessionExpiredText.gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// Set all buttons interactable state
        /// </summary>
        private void SetButtonsInteractable(bool interactable)
        {
            if (loginButton != null) loginButton.interactable = interactable;
            if (googleLoginButton != null) googleLoginButton.interactable = interactable;
            if (createAccountButton != null) createAccountButton.interactable = interactable;
            if (forgotPasswordButton != null) forgotPasswordButton.interactable = interactable;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Pre-fill email field (e.g., after registration)
        /// </summary>
        public void SetEmail(string email)
        {
            if (emailInput != null)
            {
                emailInput.text = email;
            }
        }
        
        /// <summary>
        /// Show a custom message
        /// </summary>
        public void ShowMessage(string message)
        {
            if (sessionExpiredText != null)
            {
                sessionExpiredText.text = message;
                sessionExpiredText.gameObject.SetActive(true);
            }
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[LoginUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Auto-fill test credentials
        /// </summary>
        [ContextMenu("Debug: Fill Test Credentials")]
        public void DebugFillTestCredentials()
        {
            if (emailInput != null) emailInput.text = "pirate@blackbartsgold.com";
            if (passwordInput != null) passwordInput.text = "treasure123";
        }
        
        /// <summary>
        /// Debug: Trigger test login
        /// </summary>
        [ContextMenu("Debug: Test Login")]
        public void DebugTestLogin()
        {
            DebugFillTestCredentials();
            OnLoginClicked();
        }
        
        #endregion
    }
}
