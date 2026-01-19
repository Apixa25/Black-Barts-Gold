// ============================================================================
// RegisterUI.cs
// Black Bart's Gold - Registration Screen Controller
// Path: Assets/Scripts/UI/RegisterUI.cs
// ============================================================================
// Controls the registration screen UI. Handles new account creation with
// email, password, age validation, and terms acceptance.
// Reference: BUILD-GUIDE.md Sprint 6, Prompt 6.2
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Registration screen controller.
    /// Handles new user account creation with validation.
    /// </summary>
    public class RegisterUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Input Fields")]
        [SerializeField]
        private TMP_InputField emailInput;
        
        [SerializeField]
        private TMP_InputField displayNameInput;
        
        [SerializeField]
        private TMP_InputField passwordInput;
        
        [SerializeField]
        private TMP_InputField confirmPasswordInput;
        
        [SerializeField]
        private TMP_Dropdown ageDropdown;
        
        [Header("Toggles")]
        [SerializeField]
        private Toggle termsToggle;
        
        [SerializeField]
        private Toggle newsletterToggle;
        
        [Header("Buttons")]
        [SerializeField]
        private Button registerButton;
        
        [SerializeField]
        private Button backToLoginButton;
        
        [SerializeField]
        private Button viewTermsButton;
        
        [SerializeField]
        private Button viewPrivacyButton;
        
        [Header("Password Visibility")]
        [SerializeField]
        private Button togglePasswordButton;
        
        [SerializeField]
        private Button toggleConfirmPasswordButton;
        
        [Header("UI Elements")]
        [SerializeField]
        private TMP_Text titleText;
        
        [SerializeField]
        private TMP_Text errorText;
        
        [SerializeField]
        private TMP_Text successText;
        
        [SerializeField]
        private GameObject loadingOverlay;
        
        [SerializeField]
        private TMP_Text loadingText;
        
        [Header("Validation Icons")]
        [SerializeField]
        private Image emailValidationIcon;
        
        [SerializeField]
        private Image displayNameValidationIcon;
        
        [SerializeField]
        private Image passwordValidationIcon;
        
        [SerializeField]
        private Image confirmPasswordValidationIcon;
        
        [SerializeField]
        private Color validColor = new Color(0.29f, 0.87f, 0.5f);
        
        [SerializeField]
        private Color invalidColor = new Color(0.94f, 0.27f, 0.27f);
        
        [Header("Password Strength")]
        [SerializeField]
        private Slider passwordStrengthSlider;
        
        [SerializeField]
        private Image passwordStrengthFill;
        
        [SerializeField]
        private TMP_Text passwordStrengthText;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private bool isPasswordVisible = false;
        private bool isConfirmPasswordVisible = false;
        private bool isRegistering = false;
        private int selectedAge = 0;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            SetupUI();
            SetupListeners();
            HideLoading();
            ClearMessages();
        }
        
        private void OnDestroy()
        {
            if (AuthService.Exists)
            {
                AuthService.Instance.OnRegisterSuccess -= HandleRegisterSuccess;
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
                titleText.text = "🏴‍☠️ Join the Crew!";
            }
            
            // Configure password fields
            if (passwordInput != null)
            {
                passwordInput.contentType = TMP_InputField.ContentType.Password;
            }
            
            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
            }
            
            // Setup age dropdown
            SetupAgeDropdown();
            
            // Hide validation icons initially
            HideValidationIcons();
            
            // Hide password strength initially
            if (passwordStrengthSlider != null)
            {
                passwordStrengthSlider.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Setup age dropdown with valid ages
        /// </summary>
        private void SetupAgeDropdown()
        {
            if (ageDropdown == null) return;
            
            ageDropdown.ClearOptions();
            
            List<string> options = new List<string> { "Select Age" };
            
            for (int age = AuthService.MIN_AGE; age <= AuthService.MAX_AGE; age++)
            {
                options.Add(age.ToString());
            }
            
            ageDropdown.AddOptions(options);
            ageDropdown.value = 0;
        }
        
        /// <summary>
        /// Hide all validation icons
        /// </summary>
        private void HideValidationIcons()
        {
            if (emailValidationIcon != null) emailValidationIcon.gameObject.SetActive(false);
            if (displayNameValidationIcon != null) displayNameValidationIcon.gameObject.SetActive(false);
            if (passwordValidationIcon != null) passwordValidationIcon.gameObject.SetActive(false);
            if (confirmPasswordValidationIcon != null) confirmPasswordValidationIcon.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupListeners()
        {
            // Buttons
            if (registerButton != null)
            {
                registerButton.onClick.AddListener(OnRegisterClicked);
            }
            
            if (backToLoginButton != null)
            {
                backToLoginButton.onClick.AddListener(OnBackToLoginClicked);
            }
            
            if (viewTermsButton != null)
            {
                viewTermsButton.onClick.AddListener(OnViewTermsClicked);
            }
            
            if (viewPrivacyButton != null)
            {
                viewPrivacyButton.onClick.AddListener(OnViewPrivacyClicked);
            }
            
            if (togglePasswordButton != null)
            {
                togglePasswordButton.onClick.AddListener(OnTogglePasswordVisibility);
            }
            
            if (toggleConfirmPasswordButton != null)
            {
                toggleConfirmPasswordButton.onClick.AddListener(OnToggleConfirmPasswordVisibility);
            }
            
            // Input field validation
            if (emailInput != null)
            {
                emailInput.onValueChanged.AddListener(OnEmailChanged);
            }
            
            if (displayNameInput != null)
            {
                displayNameInput.onValueChanged.AddListener(OnDisplayNameChanged);
            }
            
            if (passwordInput != null)
            {
                passwordInput.onValueChanged.AddListener(OnPasswordChanged);
            }
            
            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.onValueChanged.AddListener(OnConfirmPasswordChanged);
            }
            
            if (ageDropdown != null)
            {
                ageDropdown.onValueChanged.AddListener(OnAgeChanged);
            }
            
            // Subscribe to AuthService events
            if (AuthService.Exists)
            {
                AuthService.Instance.OnRegisterSuccess += HandleRegisterSuccess;
                AuthService.Instance.OnAuthError += HandleAuthError;
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        /// <summary>
        /// Handle register button click
        /// </summary>
        private async void OnRegisterClicked()
        {
            if (isRegistering) return;
            
            Log("Register clicked");
            ClearMessages();
            
            // Get input values
            string email = emailInput?.text?.Trim() ?? "";
            string displayName = displayNameInput?.text?.Trim() ?? "";
            string password = passwordInput?.text ?? "";
            string confirmPassword = confirmPasswordInput?.text ?? "";
            
            // Get age from dropdown
            int age = selectedAge;
            
            // Validate terms accepted
            if (termsToggle != null && !termsToggle.isOn)
            {
                ShowError("You must accept the Terms of Service to continue");
                return;
            }
            
            // Validate with AuthService
            if (!AuthService.Exists)
            {
                ShowError("Authentication service unavailable");
                return;
            }
            
            var validation = AuthService.Instance.ValidateRegistration(email, password, confirmPassword, age);
            if (!validation.isValid)
            {
                ShowError(validation.error);
                return;
            }
            
            // Validate display name
            if (string.IsNullOrEmpty(displayName))
            {
                ShowError("Display name is required");
                return;
            }
            
            if (displayName.Length < 3)
            {
                ShowError("Display name must be at least 3 characters");
                return;
            }
            
            if (displayName.Length > 20)
            {
                ShowError("Display name must be 20 characters or less");
                return;
            }
            
            // Start registration
            isRegistering = true;
            ShowLoading("Creating your account...");
            
            try
            {
                var user = await AuthService.Instance.Register(email, password, displayName, age);
                
                if (user != null)
                {
                    Log($"Registration successful: {user.displayName}");
                    // Navigation handled by HandleRegisterSuccess
                }
            }
            catch (Exception e)
            {
                ShowError($"Registration failed: {e.Message}");
            }
            finally
            {
                isRegistering = false;
                HideLoading();
            }
        }
        
        /// <summary>
        /// Handle back to login button
        /// </summary>
        private void OnBackToLoginClicked()
        {
            Log("Back to login clicked");
            SceneLoader.LoadScene(SceneNames.Login);
        }
        
        /// <summary>
        /// View terms of service
        /// </summary>
        private void OnViewTermsClicked()
        {
            Log("View terms clicked");
            Application.OpenURL("https://blackbartsgold.com/terms");
        }
        
        /// <summary>
        /// View privacy policy
        /// </summary>
        private void OnViewPrivacyClicked()
        {
            Log("View privacy clicked");
            Application.OpenURL("https://blackbartsgold.com/privacy");
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
        }
        
        /// <summary>
        /// Toggle confirm password visibility
        /// </summary>
        private void OnToggleConfirmPasswordVisibility()
        {
            isConfirmPasswordVisible = !isConfirmPasswordVisible;
            
            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.contentType = isConfirmPasswordVisible 
                    ? TMP_InputField.ContentType.Standard 
                    : TMP_InputField.ContentType.Password;
                confirmPasswordInput.ForceLabelUpdate();
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
        /// Handle display name input change
        /// </summary>
        private void OnDisplayNameChanged(string value)
        {
            if (displayNameValidationIcon == null) return;
            
            if (string.IsNullOrEmpty(value))
            {
                displayNameValidationIcon.gameObject.SetActive(false);
                return;
            }
            
            bool isValid = value.Length >= 3 && value.Length <= 20;
            displayNameValidationIcon.gameObject.SetActive(true);
            displayNameValidationIcon.color = isValid ? validColor : invalidColor;
        }
        
        /// <summary>
        /// Handle password input change
        /// </summary>
        private void OnPasswordChanged(string value)
        {
            UpdatePasswordStrength(value);
            
            if (passwordValidationIcon == null) return;
            
            if (string.IsNullOrEmpty(value))
            {
                passwordValidationIcon.gameObject.SetActive(false);
                return;
            }
            
            bool isValid = value.Length >= AuthService.MIN_PASSWORD_LENGTH;
            passwordValidationIcon.gameObject.SetActive(true);
            passwordValidationIcon.color = isValid ? validColor : invalidColor;
            
            // Also update confirm password validation
            OnConfirmPasswordChanged(confirmPasswordInput?.text ?? "");
        }
        
        /// <summary>
        /// Handle confirm password input change
        /// </summary>
        private void OnConfirmPasswordChanged(string value)
        {
            if (confirmPasswordValidationIcon == null) return;
            
            if (string.IsNullOrEmpty(value))
            {
                confirmPasswordValidationIcon.gameObject.SetActive(false);
                return;
            }
            
            string password = passwordInput?.text ?? "";
            bool isValid = value == password && value.Length >= AuthService.MIN_PASSWORD_LENGTH;
            confirmPasswordValidationIcon.gameObject.SetActive(true);
            confirmPasswordValidationIcon.color = isValid ? validColor : invalidColor;
        }
        
        /// <summary>
        /// Handle age dropdown change
        /// </summary>
        private void OnAgeChanged(int index)
        {
            if (index == 0)
            {
                selectedAge = 0;
            }
            else
            {
                selectedAge = AuthService.MIN_AGE + (index - 1);
            }
            
            Log($"Age selected: {selectedAge}");
        }
        
        /// <summary>
        /// Update password strength indicator
        /// </summary>
        private void UpdatePasswordStrength(string password)
        {
            if (passwordStrengthSlider == null) return;
            
            if (string.IsNullOrEmpty(password))
            {
                passwordStrengthSlider.gameObject.SetActive(false);
                return;
            }
            
            passwordStrengthSlider.gameObject.SetActive(true);
            
            int strength = CalculatePasswordStrength(password);
            passwordStrengthSlider.value = strength / 100f;
            
            // Update color and text
            Color strengthColor;
            string strengthLabel;
            
            if (strength < 25)
            {
                strengthColor = invalidColor;
                strengthLabel = "Weak";
            }
            else if (strength < 50)
            {
                strengthColor = new Color(1f, 0.65f, 0f); // Orange
                strengthLabel = "Fair";
            }
            else if (strength < 75)
            {
                strengthColor = new Color(1f, 0.84f, 0f); // Gold
                strengthLabel = "Good";
            }
            else
            {
                strengthColor = validColor;
                strengthLabel = "Strong";
            }
            
            if (passwordStrengthFill != null)
            {
                passwordStrengthFill.color = strengthColor;
            }
            
            if (passwordStrengthText != null)
            {
                passwordStrengthText.text = strengthLabel;
                passwordStrengthText.color = strengthColor;
            }
        }
        
        /// <summary>
        /// Calculate password strength (0-100)
        /// </summary>
        private int CalculatePasswordStrength(string password)
        {
            int strength = 0;
            
            // Length
            if (password.Length >= 8) strength += 25;
            if (password.Length >= 12) strength += 15;
            if (password.Length >= 16) strength += 10;
            
            // Contains lowercase
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]"))
                strength += 10;
            
            // Contains uppercase
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]"))
                strength += 10;
            
            // Contains number
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]"))
                strength += 15;
            
            // Contains special character
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[^a-zA-Z0-9]"))
                strength += 15;
            
            return Mathf.Clamp(strength, 0, 100);
        }
        
        #endregion
        
        #region Auth Event Handlers
        
        /// <summary>
        /// Handle successful registration
        /// </summary>
        private void HandleRegisterSuccess(User user)
        {
            Log($"Registration success handler: {user.displayName}");
            HideLoading();
            
            // Mark session manager
            if (SessionManager.Exists)
            {
                SessionManager.Instance.MarkAppLaunched();
            }
            
            // Show success message briefly, then navigate
            ShowSuccess($"Welcome aboard, {user.displayName}! 🏴‍☠️");
            
            // Navigate to main menu after delay
            Invoke(nameof(NavigateToMainMenu), 1.5f);
        }
        
        /// <summary>
        /// Navigate to main menu
        /// </summary>
        private void NavigateToMainMenu()
        {
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
            
            if (successText != null)
            {
                successText.gameObject.SetActive(false);
            }
            
            Log($"Error shown: {message}");
        }
        
        /// <summary>
        /// Show success message
        /// </summary>
        private void ShowSuccess(string message)
        {
            if (successText != null)
            {
                successText.text = message;
                successText.gameObject.SetActive(true);
            }
            
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
            
            Log($"Success shown: {message}");
        }
        
        /// <summary>
        /// Clear all messages
        /// </summary>
        private void ClearMessages()
        {
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
            
            if (successText != null)
            {
                successText.text = "";
                successText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Set all buttons interactable state
        /// </summary>
        private void SetButtonsInteractable(bool interactable)
        {
            if (registerButton != null) registerButton.interactable = interactable;
            if (backToLoginButton != null) backToLoginButton.interactable = interactable;
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[RegisterUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Fill test registration data
        /// </summary>
        [ContextMenu("Debug: Fill Test Data")]
        public void DebugFillTestData()
        {
            if (emailInput != null) emailInput.text = "newpirate@blackbartsgold.com";
            if (displayNameInput != null) displayNameInput.text = "TestPirate";
            if (passwordInput != null) passwordInput.text = "treasure123";
            if (confirmPasswordInput != null) confirmPasswordInput.text = "treasure123";
            if (ageDropdown != null) ageDropdown.value = 13; // Age 25
            if (termsToggle != null) termsToggle.isOn = true;
        }
        
        /// <summary>
        /// Debug: Test registration
        /// </summary>
        [ContextMenu("Debug: Test Registration")]
        public void DebugTestRegistration()
        {
            DebugFillTestData();
            OnRegisterClicked();
        }
        
        #endregion
    }
}
