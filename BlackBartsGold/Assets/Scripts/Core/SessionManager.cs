// ============================================================================
// SessionManager.cs
// Black Bart's Gold - Session Management
// Path: Assets/Scripts/Core/SessionManager.cs
// ============================================================================
// Manages user sessions including auto-login, session validation, and
// protected scene access. Works with AuthService for authentication.
// Reference: BUILD-GUIDE.md Sprint 6, Prompt 6.3
// ============================================================================

using UnityEngine;
using System;
using System.Threading.Tasks;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Session manager singleton.
    /// Handles session validation, auto-login, and protected scene access.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        #region Singleton
        
        private static SessionManager _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static SessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SessionManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SessionManager");
                        _instance = go.AddComponent<SessionManager>();
                        Debug.Log("[SessionManager] 🔒 Created new SessionManager instance");
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Check if instance exists
        /// </summary>
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is user currently logged in?
        /// </summary>
        public bool IsLoggedIn
        {
            get
            {
                if (AuthService.Exists && AuthService.Instance.IsLoggedIn)
                {
                    return true;
                }
                
                // Check stored session
                return HasStoredSession();
            }
        }
        
        /// <summary>
        /// Has session been validated this session?
        /// </summary>
        public bool IsSessionValidated { get; private set; } = false;
        
        /// <summary>
        /// Is startup check complete?
        /// </summary>
        public bool IsStartupCheckComplete { get; private set; } = false;
        
        /// <summary>
        /// Session check result
        /// </summary>
        public SessionCheckResult LastCheckResult { get; private set; } = SessionCheckResult.Unknown;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when session check completes
        /// </summary>
        public event Action<SessionCheckResult> OnSessionCheckComplete;
        
        /// <summary>
        /// Fired when session is validated successfully
        /// </summary>
        public event Action OnSessionValidated;
        
        /// <summary>
        /// Fired when session is invalid
        /// </summary>
        public event Action OnSessionInvalid;
        
        /// <summary>
        /// Fired when session expires
        /// </summary>
        public event Action OnSessionExpired;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Enforce singleton
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[SessionManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[SessionManager] 🔒 SessionManager initialized");
        }
        
        private void Start()
        {
            // Subscribe to AuthService events
            if (AuthService.Exists)
            {
                AuthService.Instance.OnSessionExpired += HandleSessionExpired;
                AuthService.Instance.OnLogoutSuccess += HandleLogout;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe
            if (AuthService.Exists)
            {
                AuthService.Instance.OnSessionExpired -= HandleSessionExpired;
                AuthService.Instance.OnLogoutSuccess -= HandleLogout;
            }
        }
        
        #endregion
        
        #region Session Check
        
        /// <summary>
        /// Check session on app startup
        /// Determines where to navigate (Main Menu or Login)
        /// </summary>
        public async Task<SessionCheckResult> CheckSessionOnStartup()
        {
            Debug.Log("[SessionManager] 🔄 Checking session on startup...");
            
            if (!HasStoredSession())
            {
                Debug.Log("[SessionManager] No stored session found");
                LastCheckResult = SessionCheckResult.NoSession;
                IsStartupCheckComplete = true;
                OnSessionCheckComplete?.Invoke(LastCheckResult);
                return LastCheckResult;
            }
            
            // Try auto-login
            if (AuthService.Exists)
            {
                var user = await AuthService.Instance.TryAutoLogin();
                
                if (user != null)
                {
                    Debug.Log($"[SessionManager] ✅ Session valid for: {user.displayName}");
                    LastCheckResult = SessionCheckResult.ValidSession;
                    IsSessionValidated = true;
                    IsStartupCheckComplete = true;
                    OnSessionValidated?.Invoke();
                    OnSessionCheckComplete?.Invoke(LastCheckResult);
                    return LastCheckResult;
                }
            }
            
            // Session invalid or expired
            Debug.Log("[SessionManager] ⚠️ Session invalid or expired");
            LastCheckResult = SessionCheckResult.ExpiredSession;
            IsStartupCheckComplete = true;
            OnSessionInvalid?.Invoke();
            OnSessionCheckComplete?.Invoke(LastCheckResult);
            return LastCheckResult;
        }
        
        /// <summary>
        /// Quick synchronous check if logged in
        /// </summary>
        public bool QuickSessionCheck()
        {
            // First check AuthService
            if (AuthService.Exists && AuthService.Instance.IsLoggedIn)
            {
                return true;
            }
            
            // Check stored session
            if (!HasStoredSession())
            {
                return false;
            }
            
            // If we have a stored session but haven't validated, assume valid
            // (full validation happens async)
            return AuthService.Exists && AuthService.Instance.HasValidSession();
        }
        
        #endregion
        
        #region Protected Scenes
        
        /// <summary>
        /// Check if scene requires authentication
        /// </summary>
        public bool IsProtectedScene(SceneNames scene)
        {
            return scene switch
            {
                SceneNames.MainMenu => true,
                SceneNames.ARHunt => true,
                SceneNames.Map => true,
                SceneNames.Wallet => true,
                SceneNames.Settings => true,
                SceneNames.Login => false,
                SceneNames.Register => false,
                SceneNames.Onboarding => false,
                SceneNames.ARTest => false, // Dev scene
                _ => true // Default to protected
            };
        }
        
        /// <summary>
        /// Try to load a protected scene
        /// Redirects to login if not authenticated
        /// </summary>
        public async void LoadProtectedScene(SceneNames scene)
        {
            Debug.Log($"[SessionManager] Loading protected scene: {scene}");
            
            if (!IsProtectedScene(scene))
            {
                SceneLoader.LoadScene(scene);
                return;
            }
            
            // Check session
            if (QuickSessionCheck())
            {
                SceneLoader.LoadScene(scene);
                return;
            }
            
            // Try to restore session
            var result = await CheckSessionOnStartup();
            
            if (result == SessionCheckResult.ValidSession)
            {
                SceneLoader.LoadScene(scene);
            }
            else
            {
                Debug.Log($"[SessionManager] ⚠️ Not authenticated, redirecting to login");
                ShowSessionExpiredMessage(scene);
                SceneLoader.LoadScene(SceneNames.Login);
            }
        }
        
        /// <summary>
        /// Navigate based on current auth state
        /// </summary>
        public void NavigateBasedOnAuthState()
        {
            if (IsLoggedIn)
            {
                SceneLoader.LoadScene(SceneNames.MainMenu);
            }
            else if (HasStoredSession())
            {
                // Has session but not validated - show loading/login
                SceneLoader.LoadScene(SceneNames.Login);
            }
            else
            {
                // No session - show onboarding
                SceneLoader.LoadScene(SceneNames.Onboarding);
            }
        }
        
        /// <summary>
        /// Get the appropriate start scene based on auth state
        /// </summary>
        public SceneNames GetStartScene()
        {
            if (QuickSessionCheck())
            {
                return SceneNames.MainMenu;
            }
            else if (IsFirstLaunch())
            {
                return SceneNames.Onboarding;
            }
            else
            {
                return SceneNames.Login;
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Check if there's a stored session token
        /// </summary>
        private bool HasStoredSession()
        {
            string token = PlayerPrefs.GetString("auth_token", "");
            return !string.IsNullOrEmpty(token);
        }
        
        /// <summary>
        /// Check if this is the first app launch
        /// </summary>
        public bool IsFirstLaunch()
        {
            return PlayerPrefs.GetInt("has_launched_before", 0) == 0;
        }
        
        /// <summary>
        /// Mark that the app has been launched
        /// </summary>
        public void MarkAppLaunched()
        {
            PlayerPrefs.SetInt("has_launched_before", 1);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Show session expired message (store for display on login screen)
        /// </summary>
        private void ShowSessionExpiredMessage(SceneNames attemptedScene)
        {
            PlayerPrefs.SetString("session_expired_message", "Session expired. Please log in again.");
            PlayerPrefs.SetString("session_expired_return_scene", attemptedScene.ToString());
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Get and clear session expired message
        /// </summary>
        public string GetAndClearSessionExpiredMessage()
        {
            string message = PlayerPrefs.GetString("session_expired_message", "");
            PlayerPrefs.DeleteKey("session_expired_message");
            PlayerPrefs.DeleteKey("session_expired_return_scene");
            PlayerPrefs.Save();
            return message;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleSessionExpired()
        {
            Debug.Log("[SessionManager] ⚠️ Session expired");
            IsSessionValidated = false;
            LastCheckResult = SessionCheckResult.ExpiredSession;
            OnSessionExpired?.Invoke();
        }
        
        private void HandleLogout()
        {
            Debug.Log("[SessionManager] 👋 User logged out");
            IsSessionValidated = false;
            LastCheckResult = SessionCheckResult.NoSession;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Print session state
        /// </summary>
        [ContextMenu("Debug: Print Session State")]
        public void DebugPrintState()
        {
            Debug.Log("=== SessionManager State ===");
            Debug.Log($"Is Logged In: {IsLoggedIn}");
            Debug.Log($"Is Session Validated: {IsSessionValidated}");
            Debug.Log($"Is Startup Check Complete: {IsStartupCheckComplete}");
            Debug.Log($"Last Check Result: {LastCheckResult}");
            Debug.Log($"Has Stored Session: {HasStoredSession()}");
            Debug.Log($"Is First Launch: {IsFirstLaunch()}");
            Debug.Log("============================");
        }
        
        /// <summary>
        /// Debug: Clear all session data
        /// </summary>
        [ContextMenu("Debug: Clear Session")]
        public void DebugClearSession()
        {
            PlayerPrefs.DeleteKey("auth_token");
            PlayerPrefs.DeleteKey("user_id");
            PlayerPrefs.DeleteKey("last_login");
            PlayerPrefs.DeleteKey("has_launched_before");
            PlayerPrefs.Save();
            IsSessionValidated = false;
            IsStartupCheckComplete = false;
            LastCheckResult = SessionCheckResult.Unknown;
            Debug.Log("[SessionManager] 🗑️ Session cleared");
        }
        
        #endregion
    }
    
    #region Session Check Result Enum
    
    /// <summary>
    /// Result of session check
    /// </summary>
    public enum SessionCheckResult
    {
        /// <summary>Session check not performed yet</summary>
        Unknown,
        
        /// <summary>No stored session found</summary>
        NoSession,
        
        /// <summary>Session valid and restored</summary>
        ValidSession,
        
        /// <summary>Session expired or invalid</summary>
        ExpiredSession,
        
        /// <summary>Error checking session</summary>
        Error
    }
    
    #endregion
}
