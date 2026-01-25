// ============================================================================
// ApiClient.cs
// Black Bart's Gold - API Client
// Path: Assets/Scripts/Core/ApiClient.cs
// ============================================================================
// Singleton HTTP client for backend communication. Uses UnityWebRequest,
// handles auth headers, retries, and error handling.
// Reference: BUILD-GUIDE.md Sprint 8, Prompt 8.1
// ============================================================================

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using BlackBartsGold.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// API Client singleton.
    /// Handles all HTTP communication with the backend server.
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        #region Singleton
        
        private static ApiClient _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static ApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ApiClient>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ApiClient");
                        _instance = go.AddComponent<ApiClient>();
                        Debug.Log("[ApiClient] 🌐 Created new ApiClient instance");
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
        
        #region JSON Settings
        
        /// <summary>
        /// JSON serialization settings for Newtonsoft.Json
        /// Configured for proper enum handling and camelCase
        /// </summary>
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            // Convert enums to/from strings (e.g., "fixed" <-> CoinType.Fixed)
            Converters = { new StringEnumConverter() },
            // Use camelCase for JSON properties (matches API)
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            // Ignore null values when serializing
            NullValueHandling = NullValueHandling.Ignore,
            // Don't fail on missing properties
            MissingMemberHandling = MissingMemberHandling.Ignore,
            // Format dates in ISO 8601
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired before each request
        /// </summary>
        public event Action<string, string> OnRequestStarted; // (method, url)
        
        /// <summary>
        /// Fired after each request completes
        /// </summary>
        public event Action<string, string, int, long> OnRequestCompleted; // (method, url, statusCode, durationMs)
        
        /// <summary>
        /// Fired on request error
        /// </summary>
        public event Action<string, ApiException> OnRequestError;
        
        /// <summary>
        /// Fired when auth token expires
        /// </summary>
        public event Action OnAuthExpired;
        
        /// <summary>
        /// Fired when network status changes
        /// </summary>
        public event Action<bool> OnNetworkStatusChanged;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is network available?
        /// </summary>
        public bool IsNetworkAvailable => Application.internetReachability != NetworkReachability.NotReachable;
        
        /// <summary>
        /// Pending request count
        /// </summary>
        public int PendingRequestCount { get; private set; } = 0;
        
        /// <summary>
        /// Is any request in progress?
        /// </summary>
        public bool IsLoading => PendingRequestCount > 0;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[ApiClient] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[ApiClient] 🌐 ApiClient initialized");
        }
        
        #endregion
        
        #region HTTP Methods
        
        /// <summary>
        /// GET request
        /// </summary>
        public async Task<T> Get<T>(string endpoint)
        {
            return await SendRequest<T>("GET", endpoint, null);
        }
        
        /// <summary>
        /// POST request
        /// </summary>
        public async Task<T> Post<T>(string endpoint, object body)
        {
            return await SendRequest<T>("POST", endpoint, body);
        }
        
        /// <summary>
        /// PUT request
        /// </summary>
        public async Task<T> Put<T>(string endpoint, object body)
        {
            return await SendRequest<T>("PUT", endpoint, body);
        }
        
        /// <summary>
        /// DELETE request
        /// </summary>
        public async Task Delete(string endpoint)
        {
            await SendRequest<object>("DELETE", endpoint, null);
        }
        
        /// <summary>
        /// PATCH request
        /// </summary>
        public async Task<T> Patch<T>(string endpoint, object body)
        {
            return await SendRequest<T>("PATCH", endpoint, body);
        }
        
        #endregion
        
        #region Core Request Handler
        
        /// <summary>
        /// Send HTTP request with retry logic
        /// </summary>
        private async Task<T> SendRequest<T>(string method, string endpoint, object body)
        {
            // Check network
            if (!IsNetworkAvailable)
            {
                throw NetworkException.NoConnection();
            }
            
            // Handle mock mode
            if (ApiConfig.UseMockApi)
            {
                return await HandleMockRequest<T>(method, endpoint, body);
            }
            
            string url = ApiConfig.BuildUrl(endpoint);
            int attempts = 0;
            Exception lastException = null;
            
            while (attempts < ApiConfig.MAX_RETRIES)
            {
                attempts++;
                
                try
                {
                    return await ExecuteRequest<T>(method, url, body);
                }
                catch (NetworkException ex)
                {
                    lastException = ex;
                    if (ex.IsRetryable && attempts < ApiConfig.MAX_RETRIES)
                    {
                        Log($"Retry {attempts}/{ApiConfig.MAX_RETRIES} for {method} {endpoint}");
                        await Task.Delay(ApiConfig.RETRY_DELAY_MS * attempts);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (ServerException ex)
                {
                    lastException = ex;
                    if (ex.IsRetryable && attempts < ApiConfig.MAX_RETRIES)
                    {
                        Log($"Retry {attempts}/{ApiConfig.MAX_RETRIES} for {method} {endpoint}");
                        await Task.Delay(ApiConfig.RETRY_DELAY_MS * attempts);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (ApiException)
                {
                    throw; // Non-retryable errors
                }
            }
            
            throw lastException ?? new ApiException("Request failed after retries");
        }
        
        /// <summary>
        /// Execute a single HTTP request
        /// </summary>
        private async Task<T> ExecuteRequest<T>(string method, string url, object body)
        {
            PendingRequestCount++;
            var startTime = DateTime.UtcNow;
            
            OnRequestStarted?.Invoke(method, url);
            Log($"→ {method} {url}");
            
            try
            {
                using (UnityWebRequest request = CreateRequest(method, url, body))
                {
                    // Add headers
                    AddHeaders(request);
                    
                    // Send request
                    var operation = request.SendWebRequest();
                    
                    // Wait for completion
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    long durationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    int statusCode = (int)request.responseCode;
                    
                    Log($"← {statusCode} {method} {url} ({durationMs}ms)");
                    OnRequestCompleted?.Invoke(method, url, statusCode, durationMs);
                    
                    // Handle response
                    return HandleResponse<T>(request);
                }
            }
            catch (ApiException ex)
            {
                OnRequestError?.Invoke(url, ex);
                throw;
            }
            finally
            {
                PendingRequestCount--;
            }
        }
        
        /// <summary>
        /// Create UnityWebRequest for the given method
        /// </summary>
        private UnityWebRequest CreateRequest(string method, string url, object body)
        {
            UnityWebRequest request;
            
            switch (method.ToUpper())
            {
                case "GET":
                    request = UnityWebRequest.Get(url);
                    break;
                    
                case "POST":
                case "PUT":
                case "PATCH":
                    string jsonBody = body != null ? JsonConvert.SerializeObject(body, _jsonSettings) : "{}";
                    byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                    
                    request = new UnityWebRequest(url, method);
                    request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    
                    if (ApiConfig.LogRequestBodies && ApiConfig.DebugLogging)
                    {
                        Log($"Body: {jsonBody}");
                    }
                    break;
                    
                case "DELETE":
                    request = UnityWebRequest.Delete(url);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    break;
                    
                default:
                    throw new ArgumentException($"Unsupported HTTP method: {method}");
            }
            
            request.timeout = ApiConfig.TimeoutSeconds;
            return request;
        }
        
        /// <summary>
        /// Add standard headers to request
        /// </summary>
        private void AddHeaders(UnityWebRequest request)
        {
            // Content type
            request.SetRequestHeader(ApiConfig.Headers.CONTENT_TYPE, ApiConfig.Headers.JSON_CONTENT);
            request.SetRequestHeader(ApiConfig.Headers.ACCEPT, ApiConfig.Headers.JSON_CONTENT);
            
            // Client info
            request.SetRequestHeader(ApiConfig.Headers.CLIENT_VERSION, ApiConfig.GetClientVersion());
            request.SetRequestHeader(ApiConfig.Headers.PLATFORM, ApiConfig.GetPlatform());
            request.SetRequestHeader(ApiConfig.Headers.DEVICE_ID, ApiConfig.GetDeviceId());
            
            // Auth token
            string token = SessionManager.GetAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader(ApiConfig.Headers.AUTHORIZATION, 
                    ApiConfig.Headers.BEARER_PREFIX + token);
            }
        }
        
        /// <summary>
        /// Handle response and parse JSON
        /// </summary>
        private T HandleResponse<T>(UnityWebRequest request)
        {
            // Check for network error
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                throw new NetworkException(request.error, hasConnection: false, requestUrl: request.url);
            }
            
            // Check for protocol error (HTTP errors)
            if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                int statusCode = (int)request.responseCode;
                string responseBody = request.downloadHandler?.text ?? "";
                
                // Try to parse error message from response
                string errorMessage = ParseErrorMessage(responseBody) ?? request.error;
                string errorCode = ParseErrorCode(responseBody);
                
                // Throw specific exception based on status code
                throw statusCode switch
                {
                    400 => new ValidationException(errorMessage, errorCode: errorCode),
                    401 => HandleAuthError(errorMessage, errorCode),
                    403 => AuthException.Forbidden(errorMessage),
                    404 => new NotFoundException(errorMessage),
                    429 => new RateLimitException(errorMessage, ParseRetryAfter(request)),
                    >= 500 => new ServerException(errorMessage, statusCode, errorCode, request.url),
                    _ => new ApiException(errorMessage, statusCode, errorCode, request.url)
                };
            }
            
            // Success - parse response
            string json = request.downloadHandler?.text;
            
            if (ApiConfig.LogRequestBodies && ApiConfig.DebugLogging && !string.IsNullOrEmpty(json))
            {
                Log($"Response: {json.Substring(0, Math.Min(500, json.Length))}...");
            }
            
            if (string.IsNullOrEmpty(json) || typeof(T) == typeof(object))
            {
                return default;
            }
            
            try
            {
                return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ApiClient] JSON parse error: {ex.Message}\nJSON: {json}");
                throw new ApiException($"Failed to parse response: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handle 401 auth error
        /// </summary>
        private AuthException HandleAuthError(string message, string errorCode)
        {
            var exception = AuthException.SessionExpired();
            
            // Clear session and notify
            SessionManager.ClearSession();
            OnAuthExpired?.Invoke();
            
            return exception;
        }
        
        #endregion
        
        #region Mock Handler
        
        /// <summary>
        /// Handle mock requests (for MVP without backend)
        /// </summary>
        private async Task<T> HandleMockRequest<T>(string method, string endpoint, object body)
        {
            Log($"[MOCK] {method} {endpoint}");
            
            // Simulate network delay
            await Task.Delay(UnityEngine.Random.Range(100, 500));
            
            // Return mock data based on endpoint and type
            return GetMockResponse<T>(endpoint, body);
        }
        
        /// <summary>
        /// Get mock response for endpoint
        /// </summary>
        private T GetMockResponse<T>(string endpoint, object body)
        {
            // Handle specific types
            if (typeof(T) == typeof(User))
            {
                return (T)(object)User.CreateTestUser();
            }
            
            if (typeof(T) == typeof(Wallet))
            {
                return (T)(object)Wallet.CreateTestWallet();
            }
            
            if (typeof(T) == typeof(List<Coin>))
            {
                var coins = new List<Coin>
                {
                    Coin.CreateTestCoin(CoinType.Fixed, 0.50f),
                    Coin.CreateTestCoin(CoinType.Fixed, 1.00f),
                    Coin.CreateTestCoin(CoinType.Pool, 5.00f)
                };
                return (T)(object)coins;
            }
            
            if (typeof(T) == typeof(List<Transaction>))
            {
                var transactions = new List<Transaction>
                {
                    Transaction.CreateTestTransaction(TransactionType.Found, 1.50f),
                    Transaction.CreateTestTransaction(TransactionType.GasConsumed, -0.33f),
                    Transaction.CreateTestTransaction(TransactionType.Found, 0.75f)
                };
                return (T)(object)transactions;
            }
            
            // Default: return default
            return default;
        }
        
        #endregion
        
        #region Parse Helpers
        
        /// <summary>
        /// Parse error message from JSON response
        /// </summary>
        private string ParseErrorMessage(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            
            try
            {
                var error = JsonUtility.FromJson<ApiErrorResponse>(json);
                return error?.message ?? error?.error;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Parse error code from JSON response
        /// </summary>
        private string ParseErrorCode(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            
            try
            {
                var error = JsonUtility.FromJson<ApiErrorResponse>(json);
                return error?.code;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Parse Retry-After header
        /// </summary>
        private int ParseRetryAfter(UnityWebRequest request)
        {
            string retryAfter = request.GetResponseHeader("Retry-After");
            if (int.TryParse(retryAfter, out int seconds))
            {
                return seconds;
            }
            return 60; // Default
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Log message if debug enabled
        /// </summary>
        private void Log(string message)
        {
            if (ApiConfig.DebugLogging)
            {
                Debug.Log($"[ApiClient] {message}");
            }
        }
        
        /// <summary>
        /// Check network connectivity
        /// </summary>
        public void CheckNetworkStatus()
        {
            bool isAvailable = IsNetworkAvailable;
            OnNetworkStatusChanged?.Invoke(isAvailable);
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Test GET request
        /// </summary>
        [ContextMenu("Debug: Test Health Check")]
        public async void DebugTestHealth()
        {
            try
            {
                await Get<object>("/health");
                Debug.Log("[ApiClient] Health check passed!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ApiClient] Health check failed: {ex.Message}");
            }
        }
        
        #endregion
    }
    
    #region Response Types
    
    /// <summary>
    /// Standard API error response
    /// </summary>
    [Serializable]
    public class ApiErrorResponse
    {
        public string message;
        public string error;
        public string code;
        public int statusCode;
    }
    
    /// <summary>
    /// Standard API success response wrapper
    /// </summary>
    [Serializable]
    public class ApiResponse<T>
    {
        public bool success;
        public T data;
        public string message;
    }
    
    #endregion
}
