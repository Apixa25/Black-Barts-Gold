// ============================================================================
// SettingsUI.cs
// Black Bart's Gold - Settings Screen Controller
// Path: Assets/Scripts/UI/SettingsUI.cs
// ============================================================================
// Controls the settings screen. Manages audio, haptics, and account settings.
// Reference: BUILD-GUIDE.md Prompt 5.4
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Settings screen controller.
    /// Manages game settings and account options.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Audio Settings")]
        [SerializeField]
        private Toggle soundEffectsToggle;
        
        [SerializeField]
        private Slider soundVolumeSlider;
        
        [SerializeField]
        private TMP_Text soundVolumeText;
        
        [SerializeField]
        private Toggle musicToggle;
        
        [SerializeField]
        private Slider musicVolumeSlider;
        
        [SerializeField]
        private TMP_Text musicVolumeText;
        
        [Header("Haptic Settings")]
        [SerializeField]
        private Toggle hapticToggle;
        
        [SerializeField]
        private Toggle proximityHapticToggle;
        
        [Header("Display Settings")]
        [SerializeField]
        private Toggle showCompassToggle;
        
        [SerializeField]
        private Toggle showRadarToggle;
        
        [SerializeField]
        private Toggle showGasMeterToggle;
        
        [SerializeField]
        private Slider radarRangeSlider;
        
        [SerializeField]
        private TMP_Text radarRangeText;
        
        [Header("Account Section")]
        [SerializeField]
        private TMP_Text usernameText;
        
        [SerializeField]
        private TMP_Text emailText;
        
        [SerializeField]
        private TMP_Text tierText;
        
        [SerializeField]
        private Button editProfileButton;
        
        [SerializeField]
        private Button changePasswordButton;
        
        [SerializeField]
        private Button logoutButton;
        
        [Header("Support Section")]
        [SerializeField]
        private Button helpButton;
        
        [SerializeField]
        private Button feedbackButton;
        
        [SerializeField]
        private Button privacyPolicyButton;
        
        [SerializeField]
        private Button termsOfServiceButton;
        
        [Header("Danger Zone")]
        [SerializeField]
        private Button deleteAccountButton;
        
        [SerializeField]
        private Button resetDataButton;
        
        [Header("Navigation")]
        [SerializeField]
        private Button backButton;
        
        [Header("Version Info")]
        [SerializeField]
        private TMP_Text versionText;
        
        [Header("Confirmation Modal")]
        [SerializeField]
        private GameObject confirmModal;
        
        [SerializeField]
        private TMP_Text confirmTitleText;
        
        [SerializeField]
        private TMP_Text confirmMessageText;
        
        [SerializeField]
        private Button confirmYesButton;
        
        [SerializeField]
        private Button confirmNoButton;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private Action pendingConfirmAction;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            SetupListeners();
            LoadSettings();
            UpdateAccountInfo();
            HideConfirmModal();
            
            // Set version
            if (versionText != null)
            {
                versionText.text = $"Version {Application.version}";
            }
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Setup all UI listeners
        /// </summary>
        private void SetupListeners()
        {
            // Audio
            if (soundEffectsToggle != null)
                soundEffectsToggle.onValueChanged.AddListener(OnSoundEffectsChanged);
            
            if (soundVolumeSlider != null)
                soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            
            if (musicToggle != null)
                musicToggle.onValueChanged.AddListener(OnMusicChanged);
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
            // Haptics
            if (hapticToggle != null)
                hapticToggle.onValueChanged.AddListener(OnHapticChanged);
            
            if (proximityHapticToggle != null)
                proximityHapticToggle.onValueChanged.AddListener(OnProximityHapticChanged);
            
            // Display
            if (showCompassToggle != null)
                showCompassToggle.onValueChanged.AddListener(OnShowCompassChanged);
            
            if (showRadarToggle != null)
                showRadarToggle.onValueChanged.AddListener(OnShowRadarChanged);
            
            if (showGasMeterToggle != null)
                showGasMeterToggle.onValueChanged.AddListener(OnShowGasMeterChanged);
            
            if (radarRangeSlider != null)
                radarRangeSlider.onValueChanged.AddListener(OnRadarRangeChanged);
            
            // Account buttons
            if (editProfileButton != null)
                editProfileButton.onClick.AddListener(OnEditProfileClicked);
            
            if (changePasswordButton != null)
                changePasswordButton.onClick.AddListener(OnChangePasswordClicked);
            
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
            
            // Support buttons
            if (helpButton != null)
                helpButton.onClick.AddListener(OnHelpClicked);
            
            if (feedbackButton != null)
                feedbackButton.onClick.AddListener(OnFeedbackClicked);
            
            if (privacyPolicyButton != null)
                privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyClicked);
            
            if (termsOfServiceButton != null)
                termsOfServiceButton.onClick.AddListener(OnTermsOfServiceClicked);
            
            // Danger zone
            if (deleteAccountButton != null)
                deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);
            
            if (resetDataButton != null)
                resetDataButton.onClick.AddListener(OnResetDataClicked);
            
            // Navigation
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            
            // Confirm modal
            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmNo);
        }
        
        #endregion
        
        #region Load/Save Settings
        
        /// <summary>
        /// Load settings from PlayerPrefs
        /// </summary>
        private void LoadSettings()
        {
            // Audio
            if (soundEffectsToggle != null)
                soundEffectsToggle.isOn = PlayerPrefs.GetInt("SoundEffects", 1) == 1;
            
            if (soundVolumeSlider != null)
                soundVolumeSlider.value = PlayerPrefs.GetFloat("SoundVolume", 1f);
            
            if (musicToggle != null)
                musicToggle.isOn = PlayerPrefs.GetInt("Music", 1) == 1;
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            
            // Haptics
            if (hapticToggle != null)
                hapticToggle.isOn = PlayerPrefs.GetInt("Haptics", 1) == 1;
            
            if (proximityHapticToggle != null)
                proximityHapticToggle.isOn = PlayerPrefs.GetInt("ProximityHaptics", 1) == 1;
            
            // Display
            if (showCompassToggle != null)
                showCompassToggle.isOn = PlayerPrefs.GetInt("ShowCompass", 1) == 1;
            
            if (showRadarToggle != null)
                showRadarToggle.isOn = PlayerPrefs.GetInt("ShowRadar", 1) == 1;
            
            if (showGasMeterToggle != null)
                showGasMeterToggle.isOn = PlayerPrefs.GetInt("ShowGasMeter", 1) == 1;
            
            if (radarRangeSlider != null)
                radarRangeSlider.value = PlayerPrefs.GetFloat("RadarRange", 50f);
            
            Log("Settings loaded");
        }
        
        /// <summary>
        /// Save a setting
        /// </summary>
        private void SaveSetting(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }
        
        private void SaveSetting(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
        
        #endregion
        
        #region Account Info
        
        /// <summary>
        /// Update account info display
        /// </summary>
        private void UpdateAccountInfo()
        {
            if (!PlayerData.Exists) return;
            
            var user = PlayerData.Instance.CurrentUser;
            
            if (usernameText != null)
            {
                usernameText.text = user?.displayName ?? "Pirate";
            }
            
            if (emailText != null)
            {
                emailText.text = user?.email ?? "Not logged in";
            }
            
            if (tierText != null)
            {
                tierText.text = PlayerData.Instance.TierName;
            }
        }
        
        #endregion
        
        #region Setting Change Handlers
        
        private void OnSoundEffectsChanged(bool value)
        {
            SaveSetting("SoundEffects", value ? 1 : 0);
            Log($"Sound effects: {value}");
        }
        
        private void OnSoundVolumeChanged(float value)
        {
            SaveSetting("SoundVolume", value);
            if (soundVolumeText != null)
            {
                soundVolumeText.text = $"{(int)(value * 100)}%";
            }
            AudioListener.volume = value;
        }
        
        private void OnMusicChanged(bool value)
        {
            SaveSetting("Music", value ? 1 : 0);
            Log($"Music: {value}");
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            SaveSetting("MusicVolume", value);
            if (musicVolumeText != null)
            {
                musicVolumeText.text = $"{(int)(value * 100)}%";
            }
        }
        
        private void OnHapticChanged(bool value)
        {
            SaveSetting("Haptics", value ? 1 : 0);
            
            if (HapticService.Instance != null)
            {
                HapticService.Instance.SetEnabled(value);
            }
            
            Log($"Haptics: {value}");
        }
        
        private void OnProximityHapticChanged(bool value)
        {
            SaveSetting("ProximityHaptics", value ? 1 : 0);
            Log($"Proximity haptics: {value}");
        }
        
        private void OnShowCompassChanged(bool value)
        {
            SaveSetting("ShowCompass", value ? 1 : 0);
            
            if (ARHUD.Instance != null)
            {
                ARHUD.Instance.SetCompassVisible(value);
            }
        }
        
        private void OnShowRadarChanged(bool value)
        {
            SaveSetting("ShowRadar", value ? 1 : 0);
            
            if (ARHUD.Instance != null)
            {
                ARHUD.Instance.SetRadarVisible(value);
            }
        }
        
        private void OnShowGasMeterChanged(bool value)
        {
            SaveSetting("ShowGasMeter", value ? 1 : 0);
            
            if (ARHUD.Instance != null)
            {
                ARHUD.Instance.SetGasMeterVisible(value);
            }
        }
        
        private void OnRadarRangeChanged(float value)
        {
            SaveSetting("RadarRange", value);
            
            if (radarRangeText != null)
            {
                radarRangeText.text = $"{(int)value}m";
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnEditProfileClicked()
        {
            Log("Edit profile clicked");
            // TODO: Implement profile editing
        }
        
        private void OnChangePasswordClicked()
        {
            Log("Change password clicked");
            // TODO: Implement password change
        }
        
        private void OnLogoutClicked()
        {
            ShowConfirmation(
                "Logout",
                "Are ye sure ye want to leave the ship, matey?",
                async () =>
                {
                    Log("Logging out via AuthService");
                    
                    // Use AuthService for proper logout
                    if (AuthService.Exists)
                    {
                        await AuthService.Instance.Logout();
                    }
                    else
                    {
                        // Fallback: clear data manually
                        if (PlayerData.Exists)
                        {
                            PlayerData.Instance.ClearData();
                        }
                        
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.SetAuthenticated(false);
                        }
                    }
                    
                    SceneLoader.LoadScene(SceneNames.Login);
                }
            );
        }
        
        private void OnHelpClicked()
        {
            Log("Help clicked");
            Application.OpenURL("https://blackbartsgold.com/help");
        }
        
        private void OnFeedbackClicked()
        {
            Log("Feedback clicked");
            Application.OpenURL("https://blackbartsgold.com/feedback");
        }
        
        private void OnPrivacyPolicyClicked()
        {
            Log("Privacy policy clicked");
            Application.OpenURL("https://blackbartsgold.com/privacy");
        }
        
        private void OnTermsOfServiceClicked()
        {
            Log("Terms of service clicked");
            Application.OpenURL("https://blackbartsgold.com/terms");
        }
        
        private void OnDeleteAccountClicked()
        {
            ShowConfirmation(
                "Delete Account",
                "⚠️ This will permanently delete your account and all your treasure! This cannot be undone!",
                () =>
                {
                    Log("Deleting account");
                    // TODO: Implement account deletion
                }
            );
        }
        
        private void OnResetDataClicked()
        {
            ShowConfirmation(
                "Reset Data",
                "This will reset all local data. Your account will not be affected.",
                () =>
                {
                    Log("Resetting data");
                    PlayerPrefs.DeleteAll();
                    if (PlayerData.Exists)
                    {
                        PlayerData.Instance.ClearData();
                    }
                    SceneLoader.LoadScene(SceneNames.MainMenu);
                }
            );
        }
        
        private void OnBackClicked()
        {
            SceneLoader.LoadScene(SceneNames.MainMenu);
        }
        
        #endregion
        
        #region Confirmation Modal
        
        /// <summary>
        /// Show confirmation dialog
        /// </summary>
        private void ShowConfirmation(string title, string message, Action onConfirm)
        {
            if (confirmModal == null) return;
            
            pendingConfirmAction = onConfirm;
            
            if (confirmTitleText != null)
            {
                confirmTitleText.text = title;
            }
            
            if (confirmMessageText != null)
            {
                confirmMessageText.text = message;
            }
            
            confirmModal.SetActive(true);
        }
        
        /// <summary>
        /// Hide confirmation modal
        /// </summary>
        private void HideConfirmModal()
        {
            if (confirmModal != null)
            {
                confirmModal.SetActive(false);
            }
            pendingConfirmAction = null;
        }
        
        private void OnConfirmYes()
        {
            pendingConfirmAction?.Invoke();
            HideConfirmModal();
        }
        
        private void OnConfirmNo()
        {
            HideConfirmModal();
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[SettingsUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print all settings
        /// </summary>
        [ContextMenu("Debug: Print Settings")]
        public void DebugPrintSettings()
        {
            Debug.Log("=== Settings ===");
            Debug.Log($"Sound Effects: {PlayerPrefs.GetInt("SoundEffects", 1)}");
            Debug.Log($"Sound Volume: {PlayerPrefs.GetFloat("SoundVolume", 1f)}");
            Debug.Log($"Music: {PlayerPrefs.GetInt("Music", 1)}");
            Debug.Log($"Music Volume: {PlayerPrefs.GetFloat("MusicVolume", 0.5f)}");
            Debug.Log($"Haptics: {PlayerPrefs.GetInt("Haptics", 1)}");
            Debug.Log($"Proximity Haptics: {PlayerPrefs.GetInt("ProximityHaptics", 1)}");
            Debug.Log($"Show Compass: {PlayerPrefs.GetInt("ShowCompass", 1)}");
            Debug.Log($"Show Radar: {PlayerPrefs.GetInt("ShowRadar", 1)}");
            Debug.Log($"Show Gas Meter: {PlayerPrefs.GetInt("ShowGasMeter", 1)}");
            Debug.Log($"Radar Range: {PlayerPrefs.GetFloat("RadarRange", 50f)}");
            Debug.Log("================");
        }
        
        #endregion
    }
}
