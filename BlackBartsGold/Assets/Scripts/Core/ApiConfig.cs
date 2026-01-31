// ============================================================================
// ApiConfig.cs
// Black Bart's Gold - API Configuration
// Path: Assets/Scripts/Core/ApiConfig.cs
// ============================================================================
// Centralized API configuration. Allows toggling between mock and real API,
// and environment-based URL selection.
// Reference: BUILD-GUIDE.md Sprint 8, Prompt 8.2
// ============================================================================

using UnityEngine;
using System;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// API environment mode
    /// </summary>
    public enum ApiEnvironment
    {
        /// <summary>Mock responses - no real server</summary>
        Mock,
        
        /// <summary>Local development server</summary>
        Development,
        
        /// <summary>Staging/testing server</summary>
        Staging,
        
        /// <summary>Production server</summary>
        Production
    }
    
    /// <summary>
    /// API configuration static class.
    /// Manages environment selection and API settings.
    /// </summary>
    public static class ApiConfig
    {
        #region Constants
        
        /// <summary>
        /// PlayerPrefs key for environment override
        /// </summary>
        private const string ENVIRONMENT_KEY = "api_environment";
        
        /// <summary>
        /// Default request timeout in seconds
        /// </summary>
        public const int DEFAULT_TIMEOUT = 30;
        
        /// <summary>
        /// Maximum retry attempts
        /// </summary>
        public const int MAX_RETRIES = 3;
        
        /// <summary>
        /// Delay between retries in milliseconds
        /// </summary>
        public const int RETRY_DELAY_MS = 1000;
        
        /// <summary>
        /// API version
        /// </summary>
        public const string API_VERSION = "v1";
        
        #endregion
        
        #region URLs
        
        /// <summary>
        /// Development server URL (localhost - for Unity Editor only)
        /// For Android device testing, use SetDevServerIP() with your computer's local IP
        /// </summary>
        public const string DEV_BASE_URL = "http://localhost:3000/api/v1";
        
        /// <summary>
        /// Custom development server URL (for Android device testing)
        /// Set via SetDevServerIP() with your computer's local IP address
        /// Example: 192.168.1.100
        /// </summary>
        private static string _customDevUrl = null;
        
        /// <summary>
        /// Staging server URL (Vercel preview deployment)
        /// Used for testing before pushing to production
        /// </summary>
        public const string STAGING_BASE_URL = "https://black-barts-gold-admin.vercel.app/api/v1";
        
        /// <summary>
        /// Production server URL
        /// Custom domain pointing to Vercel-deployed admin dashboard
        /// Update this if using a different domain name
        /// </summary>
        public const string PROD_BASE_URL = "https://admin.blackbartsgold.com/api/v1";
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current API environment
        /// </summary>
        private static ApiEnvironment _currentEnvironment = ApiEnvironment.Mock;
        
        /// <summary>
        /// Get/set current environment
        /// </summary>
        public static ApiEnvironment CurrentEnvironment
        {
            get => _currentEnvironment;
            set
            {
                _currentEnvironment = value;
                PlayerPrefs.SetInt(ENVIRONMENT_KEY, (int)value);
                PlayerPrefs.Save();
                Debug.Log($"[ApiConfig] Environment set to: {value}");
                OnEnvironmentChanged?.Invoke(value);
            }
        }
        
        /// <summary>
        /// Is using mock API?
        /// </summary>
        public static bool UseMockApi => _currentEnvironment == ApiEnvironment.Mock;
        
        /// <summary>
        /// Is debug logging enabled?
        /// </summary>
        public static bool DebugLogging { get; set; } = true;
        
        /// <summary>
        /// Should log request/response bodies?
        /// </summary>
        public static bool LogRequestBodies { get; set; } = true;
        
        /// <summary>
        /// Current request timeout
        /// </summary>
        public static int TimeoutSeconds { get; set; } = DEFAULT_TIMEOUT;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when environment changes
        /// </summary>
        public static event Action<ApiEnvironment> OnEnvironmentChanged;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Static constructor - load saved settings
        /// </summary>
        static ApiConfig()
        {
            LoadSavedEnvironment();
        }
        
        /// <summary>
        /// Load saved environment from PlayerPrefs
        /// </summary>
        private static void LoadSavedEnvironment()
        {
            // ================================================================
            // CRITICAL FIX: Device builds ALWAYS use Production mode
            // This ensures the Prize-Finder APK connects to the real admin 
            // dashboard at https://admin.blackbartsgold.com/api/v1
            // ================================================================
            
            #if UNITY_EDITOR
            // In Editor: Allow PlayerPrefs override for testing different environments
            if (PlayerPrefs.HasKey(ENVIRONMENT_KEY))
            {
                _currentEnvironment = (ApiEnvironment)PlayerPrefs.GetInt(ENVIRONMENT_KEY);
            }
            else
            {
                _currentEnvironment = ApiEnvironment.Mock; // Mock in Editor for testing
            }
            #else
            // On Device: ALWAYS use Production mode - ignore any saved PlayerPrefs
            // This fixes the bug where Mock mode could get "stuck" on device builds
            _currentEnvironment = ApiEnvironment.Production;
            
            // Clear any stale environment override from previous test builds
            if (PlayerPrefs.HasKey(ENVIRONMENT_KEY))
            {
                PlayerPrefs.DeleteKey(ENVIRONMENT_KEY);
                PlayerPrefs.Save();
            }
            #endif
            
            // Load custom dev server URL if saved (only relevant for Development mode)
            if (PlayerPrefs.HasKey("dev_server_url"))
            {
                _customDevUrl = PlayerPrefs.GetString("dev_server_url");
                Debug.Log($"[ApiConfig] Loaded custom dev URL: {_customDevUrl}");
            }
            
            // Log the configuration prominently at startup
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log($"[ApiConfig] 🌐 API CONFIGURATION");
            Debug.Log($"[ApiConfig]   Environment: {_currentEnvironment}");
            Debug.Log($"[ApiConfig]   Use Mock API: {UseMockApi}");
            Debug.Log($"[ApiConfig]   Base URL: {GetBaseUrl()}");
            Debug.Log($"[ApiConfig]   Platform: {Application.platform}");
            Debug.Log("═══════════════════════════════════════════════════════════════");
        }
        
        #endregion
        
        #region URL Methods
        
        /// <summary>
        /// Set custom development server IP for Android device testing.
        /// When testing on a physical Android device, localhost won't work.
        /// Use your computer's local IP address (e.g., 192.168.1.100).
        /// 
        /// Find your IP:
        /// - Windows: ipconfig in Command Prompt, look for IPv4 Address
        /// - Mac/Linux: ifconfig, look for inet address
        /// </summary>
        /// <param name="ipAddress">Your computer's local IP address</param>
        /// <param name="port">Server port (default 3000 for Next.js)</param>
        public static void SetDevServerIP(string ipAddress, int port = 3000)
        {
            _customDevUrl = $"http://{ipAddress}:{port}/api/v1";
            Debug.Log($"[ApiConfig] Development server set to: {_customDevUrl}");
            
            // Save to PlayerPrefs so it persists
            PlayerPrefs.SetString("dev_server_url", _customDevUrl);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Clear custom development URL (revert to localhost)
        /// </summary>
        public static void ClearDevServerIP()
        {
            _customDevUrl = null;
            PlayerPrefs.DeleteKey("dev_server_url");
            Debug.Log("[ApiConfig] Custom dev server cleared, using localhost");
        }
        
        /// <summary>
        /// Get base URL for current environment
        /// </summary>
        public static string GetBaseUrl()
        {
            return _currentEnvironment switch
            {
                ApiEnvironment.Mock => "mock://",
                ApiEnvironment.Development => _customDevUrl ?? DEV_BASE_URL,
                ApiEnvironment.Staging => STAGING_BASE_URL,
                ApiEnvironment.Production => PROD_BASE_URL,
                _ => DEV_BASE_URL
            };
        }
        
        /// <summary>
        /// Build full URL for an endpoint
        /// </summary>
        public static string BuildUrl(string endpoint)
        {
            if (UseMockApi)
            {
                return $"mock://{endpoint}";
            }
            
            string baseUrl = GetBaseUrl();
            
            // Ensure endpoint starts with /
            if (!endpoint.StartsWith("/"))
            {
                endpoint = "/" + endpoint;
            }
            
            return baseUrl + endpoint;
        }
        
        #endregion
        
        #region Endpoint Definitions
        
        /// <summary>
        /// Auth endpoints
        /// </summary>
        public static class Auth
        {
            public const string REGISTER = "/auth/register";
            public const string LOGIN = "/auth/login";
            public const string GOOGLE = "/auth/google";
            public const string LOGOUT = "/auth/logout";
            public const string ME = "/auth/me";
            public const string REFRESH = "/auth/refresh";
        }
        
        /// <summary>
        /// Wallet endpoints
        /// </summary>
        public static class Wallet
        {
            public const string GET = "/wallet";
            public const string TRANSACTIONS = "/wallet/transactions";
            public const string PARK = "/wallet/park";
            public const string UNPARK = "/wallet/unpark";
            public const string CONSUME_GAS = "/wallet/consume-gas";
            public const string PURCHASE = "/wallet/purchase";
        }
        
        /// <summary>
        /// Coin endpoints
        /// </summary>
        public static class Coins
        {
            public const string NEARBY = "/coins/nearby";
            public const string HIDE = "/coins/hide";
            public const string COLLECT = "/coins/{id}/collect";
            public const string DELETE = "/coins/{id}";
            public const string GET = "/coins/{id}";
            
            /// <summary>
            /// Build nearby URL with params
            /// </summary>
            public static string GetNearbyUrl(double lat, double lng, float radius)
            {
                return $"{NEARBY}?lat={lat}&lng={lng}&radius={radius}";
            }
            
            /// <summary>
            /// Build coin-specific URL
            /// </summary>
            public static string GetCoinUrl(string endpoint, string coinId)
            {
                return endpoint.Replace("{id}", coinId);
            }
        }
        
        /// <summary>
        /// User endpoints
        /// </summary>
        public static class User
        {
            public const string PROFILE = "/user/profile";
            public const string STATS = "/user/stats";
            public const string SETTINGS = "/user/settings";
            public const string FIND_LIMIT = "/user/find-limit";
        }
        
        /// <summary>
        /// Player tracking endpoints
        /// Used by PlayerLocationService for real-time location updates
        /// </summary>
        public static class Player
        {
            /// <summary>
            /// Update player location (POST) or remove from tracking (DELETE)
            /// </summary>
            public const string LOCATION = "/player/location";
            
            /// <summary>
            /// Build DELETE URL with userId parameter
            /// </summary>
            public static string GetDeleteLocationUrl(string userId)
            {
                return $"{LOCATION}?userId={userId}";
            }
        }
        
        #endregion
        
        #region Header Keys
        
        /// <summary>
        /// HTTP header key constants
        /// </summary>
        public static class Headers
        {
            public const string AUTHORIZATION = "Authorization";
            public const string CONTENT_TYPE = "Content-Type";
            public const string ACCEPT = "Accept";
            public const string CLIENT_VERSION = "X-Client-Version";
            public const string PLATFORM = "X-Platform";
            public const string DEVICE_ID = "X-Device-ID";
            
            public const string JSON_CONTENT = "application/json";
            public const string BEARER_PREFIX = "Bearer ";
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get current client version
        /// </summary>
        public static string GetClientVersion()
        {
            return Application.version;
        }
        
        /// <summary>
        /// Get current platform string
        /// </summary>
        public static string GetPlatform()
        {
            return Application.platform switch
            {
                RuntimePlatform.Android => "android",
                RuntimePlatform.IPhonePlayer => "ios",
                RuntimePlatform.WindowsPlayer => "windows",
                RuntimePlatform.OSXPlayer => "macos",
                RuntimePlatform.LinuxPlayer => "linux",
                RuntimePlatform.WebGLPlayer => "webgl",
                _ => "unity-editor"
            };
        }
        
        /// <summary>
        /// Get unique device ID
        /// </summary>
        public static string GetDeviceId()
        {
            return SystemInfo.deviceUniqueIdentifier;
        }
        
        /// <summary>
        /// Check if current environment is production
        /// </summary>
        public static bool IsProduction()
        {
            return _currentEnvironment == ApiEnvironment.Production;
        }
        
        /// <summary>
        /// Set to development mode (for testing)
        /// </summary>
        public static void SetDevelopmentMode()
        {
            CurrentEnvironment = ApiEnvironment.Development;
            DebugLogging = true;
            LogRequestBodies = true;
        }
        
        /// <summary>
        /// Set to production mode
        /// </summary>
        public static void SetProductionMode()
        {
            CurrentEnvironment = ApiEnvironment.Production;
            DebugLogging = false;
            LogRequestBodies = false;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Log current configuration
        /// </summary>
        public static void LogConfig()
        {
            Debug.Log("=== API Configuration ===");
            Debug.Log($"Environment: {_currentEnvironment}");
            Debug.Log($"Base URL: {GetBaseUrl()}");
            Debug.Log($"Use Mock: {UseMockApi}");
            Debug.Log($"Timeout: {TimeoutSeconds}s");
            Debug.Log($"Debug Logging: {DebugLogging}");
            Debug.Log($"Client Version: {GetClientVersion()}");
            Debug.Log($"Platform: {GetPlatform()}");
            Debug.Log("=========================");
        }
        
        #endregion
    }
}
