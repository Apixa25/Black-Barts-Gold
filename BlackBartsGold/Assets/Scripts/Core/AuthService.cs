// ============================================================================
// AuthService.cs
// Black Bart's Gold - Authentication Service
// Path: Assets/Scripts/Core/AuthService.cs
// ============================================================================
// Singleton service handling user authentication - login, registration,
// logout, and session management. Uses mock responses for MVP.
// Reference: BUILD-GUIDE.md Sprint 6, Prompt 6.1
// ============================================================================

using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Authentication service singleton.
    /// Handles login, registration, session management.
    /// MVP: Uses mock responses; later connects to real backend.
    /// </summary>
    public class AuthService : MonoBehaviour
    {
        #region Singleton
        
        private static AuthService _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static AuthService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AuthService>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AuthService");
                        _instance = go.AddComponent<AuthService>();
                        Debug.Log("[AuthService] 🔐 Created new AuthService instance");
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Check if instance exists without creating one
        /// </summary>
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Constants
        
        /// <summary>
        /// Minimum password length
        /// </summary>
        public const int MIN_PASSWORD_LENGTH = 8;
        
        /// <summary>
        /// Minimum age to register
        /// </summary>
        public const int MIN_AGE = 13;
        
        /// <summary>
        /// Maximum age to register
        /// </summary>
        public const int MAX_AGE = 99;
        
        /// <summary>
        /// Mock auth delay (simulates network)
        /// </summary>
        private const int MOCK_DELAY_MS = 800;
        
        /// <summary>
        /// PlayerPrefs key for auth token
        /// </summary>
        private const string AUTH_TOKEN_KEY = "auth_token";
        
        /// <summary>
        /// PlayerPrefs key for user ID
        /// </summary>
        private const string USER_ID_KEY = "user_id";
        
        /// <summary>
        /// PlayerPrefs key for last login time
        /// </summary>
        private const string LAST_LOGIN_KEY = "last_login";
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current authentication state
        /// </summary>
        public AuthState CurrentState { get; private set; } = AuthState.Unknown;
        
        /// <summary>
        /// Currently authenticated user
        /// </summary>
        public User CurrentUser { get; private set; }
        
        /// <summary>
        /// Current auth token
        /// </summary>
        public string AuthToken { get; private set; }
        
        /// <summary>
        /// Is user logged in?
        /// </summary>
        public bool IsLoggedIn => CurrentState == AuthState.Authenticated && CurrentUser != null;
        
        /// <summary>
        /// Is auth operation in progress?
        /// </summary>
        public bool IsLoading { get; private set; } = false;
        
        /// <summary>
        /// Last error message
        /// </summary>
        public string LastError { get; private set; }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when login succeeds
        /// </summary>
        public event Action<User> OnLoginSuccess;
        
        /// <summary>
        /// Fired when registration succeeds
        /// </summary>
        public event Action<User> OnRegisterSuccess;
        
        /// <summary>
        /// Fired when logout completes
        /// </summary>
        public event Action OnLogoutSuccess;
        
        /// <summary>
        /// Fired on auth error
        /// </summary>
        public event Action<string> OnAuthError;
        
        /// <summary>
        /// Fired when auth state changes
        /// </summary>
        public event Action<AuthState> OnAuthStateChanged;
        
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
                Debug.LogWarning("[AuthService] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[AuthService] 🔐 AuthService initialized");
        }
        
        #endregion
        
        #region Public Methods - Registration
        
        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="password">Password (min 8 chars)</param>
        /// <param name="displayName">Display name</param>
        /// <param name="age">User age (must be 13+)</param>
        /// <returns>Registered user or null on failure</returns>
        public async Task<User> Register(string email, string password, string displayName, int age)
        {
            Debug.Log($"[AuthService] 📝 Registering new user: {email}");
            
            // Validate input
            var validation = ValidateRegistration(email, password, password, age);
            if (!validation.isValid)
            {
                SetError(validation.error);
                return null;
            }
            
            SetState(AuthState.Authenticating);
            IsLoading = true;
            
            try
            {
                // Simulate network delay
                await Task.Delay(MOCK_DELAY_MS);
                
                // --- MVP: Mock registration ---
                // In production, this calls the real API
                User newUser = CreateMockUser(email, displayName, age);
                string token = GenerateMockToken(newUser.id);
                
                // Save session
                SaveSession(token, newUser.id);
                
                // Set current user
                CurrentUser = newUser;
                AuthToken = token;
                
                // Update PlayerData
                if (PlayerData.Exists)
                {
                    PlayerData.Instance.SetUser(newUser, token);
                }
                
                SetState(AuthState.Authenticated);
                
                Debug.Log($"[AuthService] ✅ Registration successful: {newUser.displayName}");
                OnRegisterSuccess?.Invoke(newUser);
                
                return newUser;
            }
            catch (Exception e)
            {
                SetError($"Registration failed: {e.Message}");
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        #endregion
        
        #region Public Methods - Login
        
        /// <summary>
        /// Login with email and password
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="password">Password</param>
        /// <returns>Logged in user or null on failure</returns>
        public async Task<User> Login(string email, string password)
        {
            Debug.Log($"[AuthService] 🔑 Logging in: {email}");
            
            // Validate input
            if (!IsValidEmail(email))
            {
                SetError("Invalid email format");
                return null;
            }
            
            if (string.IsNullOrEmpty(password))
            {
                SetError("Password is required");
                return null;
            }
            
            SetState(AuthState.Authenticating);
            IsLoading = true;
            
            try
            {
                // Simulate network delay
                await Task.Delay(MOCK_DELAY_MS);
                
                // --- MVP: Mock login ---
                // Any email/password works; returns a test user
                // In production, this validates against real API
                
                User user = CreateMockUser(email, GetDisplayNameFromEmail(email), 25);
                string token = GenerateMockToken(user.id);
                
                // Save session
                SaveSession(token, user.id);
                
                // Set current user
                CurrentUser = user;
                AuthToken = token;
                
                // Update PlayerData
                if (PlayerData.Exists)
                {
                    PlayerData.Instance.SetUser(user, token);
                }
                
                // Update GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetAuthenticated(true);
                }
                
                SetState(AuthState.Authenticated);
                
                Debug.Log($"[AuthService] ✅ Login successful: {user.displayName}");
                OnLoginSuccess?.Invoke(user);
                
                return user;
            }
            catch (Exception e)
            {
                SetError($"Login failed: {e.Message}");
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// Login with Google OAuth (stub for MVP)
        /// </summary>
        /// <returns>Logged in user or null</returns>
        public async Task<User> LoginWithGoogle()
        {
            Debug.Log("[AuthService] 🔑 Google login requested");
            
            SetState(AuthState.Authenticating);
            IsLoading = true;
            
            try
            {
                // Simulate network delay
                await Task.Delay(MOCK_DELAY_MS);
                
                // --- MVP: Stub ---
                // In production, this triggers Google OAuth flow
                
                User user = CreateMockUser("pirate@gmail.com", "Google Pirate", 25);
                user.authMethod = AuthMethod.Google;
                user.emailVerified = true;
                
                string token = GenerateMockToken(user.id);
                
                SaveSession(token, user.id);
                CurrentUser = user;
                AuthToken = token;
                
                if (PlayerData.Exists)
                {
                    PlayerData.Instance.SetUser(user, token);
                }
                
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetAuthenticated(true);
                }
                
                SetState(AuthState.Authenticated);
                
                Debug.Log($"[AuthService] ✅ Google login successful: {user.displayName}");
                OnLoginSuccess?.Invoke(user);
                
                return user;
            }
            catch (Exception e)
            {
                SetError($"Google login failed: {e.Message}");
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        #endregion
        
        #region Public Methods - Logout
        
        /// <summary>
        /// Logout current user
        /// </summary>
        public async Task Logout()
        {
            Debug.Log("[AuthService] 👋 Logging out...");
            
            IsLoading = true;
            
            try
            {
                // Simulate network delay (e.g., invalidating token on server)
                await Task.Delay(300);
                
                // Clear local session
                ClearSession();
                
                // Clear player data
                if (PlayerData.Exists)
                {
                    PlayerData.Instance.ClearData();
                }
                
                // Update GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetAuthenticated(false);
                }
                
                CurrentUser = null;
                AuthToken = null;
                
                SetState(AuthState.NotAuthenticated);
                
                Debug.Log("[AuthService] ✅ Logout successful");
                OnLogoutSuccess?.Invoke();
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        #endregion
        
        #region Public Methods - Session
        
        /// <summary>
        /// Get current user (checks session if needed)
        /// </summary>
        /// <returns>Current user or null</returns>
        public async Task<User> GetCurrentUser()
        {
            if (CurrentUser != null && CurrentState == AuthState.Authenticated)
            {
                return CurrentUser;
            }
            
            // Try to restore from saved session
            return await TryAutoLogin();
        }
        
        /// <summary>
        /// Try to auto-login from saved session
        /// </summary>
        /// <returns>User if session valid, null otherwise</returns>
        public async Task<User> TryAutoLogin()
        {
            Debug.Log("[AuthService] 🔄 Attempting auto-login...");
            
            string savedToken = PlayerPrefs.GetString(AUTH_TOKEN_KEY, "");
            string savedUserId = PlayerPrefs.GetString(USER_ID_KEY, "");
            
            if (string.IsNullOrEmpty(savedToken) || string.IsNullOrEmpty(savedUserId))
            {
                Debug.Log("[AuthService] No saved session found");
                SetState(AuthState.NotAuthenticated);
                return null;
            }
            
            SetState(AuthState.Authenticating);
            IsLoading = true;
            
            try
            {
                // Simulate token validation
                await Task.Delay(500);
                
                // --- MVP: Mock token validation ---
                // In production, validate token with server
                bool isValid = ValidateMockToken(savedToken);
                
                if (!isValid)
                {
                    Debug.Log("[AuthService] Session expired");
                    ClearSession();
                    SetState(AuthState.SessionExpired);
                    OnSessionExpired?.Invoke();
                    return null;
                }
                
                // Restore user from PlayerData or create mock
                User user = null;
                
                if (PlayerData.Exists && PlayerData.Instance.CurrentUser != null)
                {
                    user = PlayerData.Instance.CurrentUser;
                }
                else
                {
                    // Create mock user from saved info
                    user = User.CreateTestUser();
                    user.id = savedUserId;
                    
                    if (PlayerData.Exists)
                    {
                        PlayerData.Instance.SetUser(user, savedToken);
                    }
                }
                
                CurrentUser = user;
                AuthToken = savedToken;
                
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetAuthenticated(true);
                }
                
                SetState(AuthState.Authenticated);
                
                Debug.Log($"[AuthService] ✅ Auto-login successful: {user.displayName}");
                OnLoginSuccess?.Invoke(user);
                
                return user;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthService] Auto-login failed: {e.Message}");
                SetState(AuthState.NotAuthenticated);
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// Check if there's a saved session
        /// </summary>
        public bool HasSavedSession()
        {
            string token = PlayerPrefs.GetString(AUTH_TOKEN_KEY, "");
            return !string.IsNullOrEmpty(token);
        }
        
        /// <summary>
        /// Check if session is valid (quick check)
        /// </summary>
        public bool HasValidSession()
        {
            if (string.IsNullOrEmpty(AuthToken))
            {
                AuthToken = PlayerPrefs.GetString(AUTH_TOKEN_KEY, "");
            }
            
            return !string.IsNullOrEmpty(AuthToken) && ValidateMockToken(AuthToken);
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate registration form data
        /// </summary>
        public (bool isValid, string error) ValidateRegistration(string email, string password, string confirmPassword, int age)
        {
            // Email
            if (string.IsNullOrEmpty(email))
            {
                return (false, "Email is required");
            }
            
            if (!IsValidEmail(email))
            {
                return (false, "Invalid email format");
            }
            
            // Password
            if (string.IsNullOrEmpty(password))
            {
                return (false, "Password is required");
            }
            
            if (password.Length < MIN_PASSWORD_LENGTH)
            {
                return (false, $"Password must be at least {MIN_PASSWORD_LENGTH} characters");
            }
            
            // Confirm password
            if (password != confirmPassword)
            {
                return (false, "Passwords do not match");
            }
            
            // Age
            if (age < MIN_AGE)
            {
                return (false, $"You must be at least {MIN_AGE} years old to register");
            }
            
            if (age > MAX_AGE)
            {
                return (false, "Please enter a valid age");
            }
            
            return (true, null);
        }
        
        /// <summary>
        /// Validate login form data
        /// </summary>
        public (bool isValid, string error) ValidateLogin(string email, string password)
        {
            if (string.IsNullOrEmpty(email))
            {
                return (false, "Email is required");
            }
            
            if (!IsValidEmail(email))
            {
                return (false, "Invalid email format");
            }
            
            if (string.IsNullOrEmpty(password))
            {
                return (false, "Password is required");
            }
            
            return (true, null);
        }
        
        /// <summary>
        /// Check if email format is valid
        /// </summary>
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            
            // Simple regex for email validation
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Set auth state and fire event
        /// </summary>
        private void SetState(AuthState newState)
        {
            if (CurrentState == newState) return;
            
            AuthState oldState = CurrentState;
            CurrentState = newState;
            
            Debug.Log($"[AuthService] State: {oldState} → {newState}");
            OnAuthStateChanged?.Invoke(newState);
        }
        
        /// <summary>
        /// Set error and fire event
        /// </summary>
        private void SetError(string error)
        {
            LastError = error;
            Debug.LogWarning($"[AuthService] ❌ Error: {error}");
            SetState(AuthState.Error);
            OnAuthError?.Invoke(error);
        }
        
        /// <summary>
        /// Save session to PlayerPrefs
        /// </summary>
        private void SaveSession(string token, string userId)
        {
            PlayerPrefs.SetString(AUTH_TOKEN_KEY, token);
            PlayerPrefs.SetString(USER_ID_KEY, userId);
            PlayerPrefs.SetString(LAST_LOGIN_KEY, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
            
            Debug.Log("[AuthService] 💾 Session saved");
        }
        
        /// <summary>
        /// Clear saved session
        /// </summary>
        private void ClearSession()
        {
            PlayerPrefs.DeleteKey(AUTH_TOKEN_KEY);
            PlayerPrefs.DeleteKey(USER_ID_KEY);
            PlayerPrefs.DeleteKey(LAST_LOGIN_KEY);
            PlayerPrefs.Save();
            
            Debug.Log("[AuthService] 🗑️ Session cleared");
        }
        
        #endregion
        
        #region Mock Helpers (MVP Only)
        
        /// <summary>
        /// Create a mock user for MVP testing
        /// </summary>
        private User CreateMockUser(string email, string displayName, int age)
        {
            var user = new User
            {
                id = Guid.NewGuid().ToString(),
                email = email,
                displayName = displayName,
                age = age,
                bbgBalance = 15.00f, // Starter balance
                gasRemaining = 10f,  // 10 days free trial
                findLimit = 1.00f,
                highestHiddenValue = 0f,
                currentTier = FindLimitTier.CabinBoy,
                stats = new UserStats(),
                createdAt = DateTime.UtcNow.ToString("o"),
                lastLoginAt = DateTime.UtcNow.ToString("o"),
                accountStatus = AccountStatus.Active,
                authMethod = AuthMethod.Email,
                emailVerified = false
            };
            
            return user;
        }
        
        /// <summary>
        /// Generate a mock auth token
        /// </summary>
        private string GenerateMockToken(string userId)
        {
            // Simple mock token: base64(userId + timestamp)
            string payload = $"{userId}|{DateTime.UtcNow.Ticks}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        }
        
        /// <summary>
        /// Validate a mock token (check if not expired)
        /// </summary>
        private bool ValidateMockToken(string token)
        {
            try
            {
                string payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                string[] parts = payload.Split('|');
                
                if (parts.Length != 2) return false;
                
                long ticks = long.Parse(parts[1]);
                DateTime tokenTime = new DateTime(ticks);
                
                // Token valid for 30 days
                return (DateTime.UtcNow - tokenTime).TotalDays < 30;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Extract display name from email
        /// </summary>
        private string GetDisplayNameFromEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "Pirate";
            
            int atIndex = email.IndexOf('@');
            if (atIndex > 0)
            {
                string localPart = email.Substring(0, atIndex);
                // Capitalize first letter
                return char.ToUpper(localPart[0]) + localPart.Substring(1);
            }
            
            return "Pirate";
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Print current auth state
        /// </summary>
        [ContextMenu("Debug: Print Auth State")]
        public void DebugPrintState()
        {
            Debug.Log("=== AuthService State ===");
            Debug.Log($"State: {CurrentState}");
            Debug.Log($"Is Logged In: {IsLoggedIn}");
            Debug.Log($"Is Loading: {IsLoading}");
            Debug.Log($"User: {CurrentUser?.displayName ?? "None"}");
            Debug.Log($"Has Token: {!string.IsNullOrEmpty(AuthToken)}");
            Debug.Log($"Has Saved Session: {HasSavedSession()}");
            Debug.Log($"Last Error: {LastError ?? "None"}");
            Debug.Log("=========================");
        }
        
        /// <summary>
        /// Debug: Force login with test user
        /// </summary>
        [ContextMenu("Debug: Login Test User")]
        public async void DebugLoginTestUser()
        {
            await Login("test@blackbartsgold.com", "password123");
        }
        
        /// <summary>
        /// Debug: Force logout
        /// </summary>
        [ContextMenu("Debug: Logout")]
        public async void DebugLogout()
        {
            await Logout();
        }
        
        #endregion
    }
    
    #region Auth State Enum
    
    /// <summary>
    /// Authentication state machine states
    /// </summary>
    public enum AuthState
    {
        /// <summary>Initial state, unknown auth status</summary>
        Unknown,
        
        /// <summary>Not authenticated, needs login</summary>
        NotAuthenticated,
        
        /// <summary>Currently authenticating (login/register in progress)</summary>
        Authenticating,
        
        /// <summary>Successfully authenticated</summary>
        Authenticated,
        
        /// <summary>Session expired, needs re-login</summary>
        SessionExpired,
        
        /// <summary>Authentication error occurred</summary>
        Error
    }
    
    #endregion
}
