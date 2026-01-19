// ============================================================================
// ApiException.cs
// Black Bart's Gold - API Exception Types
// Path: Assets/Scripts/Core/ApiException.cs
// ============================================================================
// Custom exception types for API errors. Includes NetworkException, 
// AuthException, and ServerException for specific error handling.
// Reference: BUILD-GUIDE.md Sprint 8, Prompt 8.1
// ============================================================================

using System;

namespace BlackBartsGold.Core
{
    #region Base API Exception
    
    /// <summary>
    /// Base exception for API errors
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// HTTP status code (0 for network errors)
        /// </summary>
        public int StatusCode { get; }
        
        /// <summary>
        /// Error code from API (if provided)
        /// </summary>
        public string ErrorCode { get; }
        
        /// <summary>
        /// Original error message from API
        /// </summary>
        public string ApiMessage { get; }
        
        /// <summary>
        /// Request URL that failed
        /// </summary>
        public string RequestUrl { get; }
        
        /// <summary>
        /// HTTP method used (GET, POST, etc.)
        /// </summary>
        public string HttpMethod { get; }
        
        /// <summary>
        /// Whether this error is retryable
        /// </summary>
        public virtual bool IsRetryable => false;
        
        /// <summary>
        /// User-friendly error message
        /// </summary>
        public virtual string UserMessage => "An error occurred. Please try again.";
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ApiException(string message, int statusCode = 0, string errorCode = null, 
            string requestUrl = null, string httpMethod = null)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            ApiMessage = message;
            RequestUrl = requestUrl;
            HttpMethod = httpMethod;
        }
        
        /// <summary>
        /// Constructor with inner exception
        /// </summary>
        public ApiException(string message, Exception innerException, int statusCode = 0)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ApiMessage = message;
        }
        
        /// <summary>
        /// Debug representation
        /// </summary>
        public override string ToString()
        {
            return $"[{GetType().Name}] {StatusCode}: {Message} (Code: {ErrorCode ?? "none"})";
        }
    }
    
    #endregion
    
    #region Network Exception
    
    /// <summary>
    /// Exception for network connectivity issues
    /// </summary>
    public class NetworkException : ApiException
    {
        /// <summary>
        /// Whether device has internet connection
        /// </summary>
        public bool HasConnection { get; }
        
        /// <summary>
        /// Network errors are retryable
        /// </summary>
        public override bool IsRetryable => true;
        
        /// <summary>
        /// User-friendly message
        /// </summary>
        public override string UserMessage => HasConnection 
            ? "Unable to reach server. Please try again."
            : "No internet connection. Please check your network.";
        
        /// <summary>
        /// Constructor
        /// </summary>
        public NetworkException(string message, bool hasConnection = false, string requestUrl = null)
            : base(message, 0, "NETWORK_ERROR", requestUrl)
        {
            HasConnection = hasConnection;
        }
        
        /// <summary>
        /// Constructor with inner exception
        /// </summary>
        public NetworkException(string message, Exception innerException, bool hasConnection = false)
            : base(message, innerException, 0)
        {
            HasConnection = hasConnection;
        }
        
        /// <summary>
        /// Create timeout exception
        /// </summary>
        public static NetworkException Timeout(string requestUrl, int timeoutSeconds)
        {
            return new NetworkException(
                $"Request timed out after {timeoutSeconds} seconds",
                hasConnection: true,
                requestUrl: requestUrl
            );
        }
        
        /// <summary>
        /// Create no connection exception
        /// </summary>
        public static NetworkException NoConnection()
        {
            return new NetworkException(
                "No internet connection available",
                hasConnection: false
            );
        }
    }
    
    #endregion
    
    #region Auth Exception
    
    /// <summary>
    /// Exception for authentication errors (401 Unauthorized, 403 Forbidden)
    /// </summary>
    public class AuthException : ApiException
    {
        /// <summary>
        /// Whether session should be cleared
        /// </summary>
        public bool ShouldClearSession { get; }
        
        /// <summary>
        /// Whether user should be redirected to login
        /// </summary>
        public bool ShouldRedirectToLogin { get; }
        
        /// <summary>
        /// Auth errors are not retryable without re-auth
        /// </summary>
        public override bool IsRetryable => false;
        
        /// <summary>
        /// User-friendly message
        /// </summary>
        public override string UserMessage => StatusCode switch
        {
            401 => "Session expired. Please log in again.",
            403 => "You don't have permission to do that.",
            _ => "Authentication failed. Please log in again."
        };
        
        /// <summary>
        /// Constructor
        /// </summary>
        public AuthException(string message, int statusCode = 401, string errorCode = null,
            bool shouldClearSession = true, bool shouldRedirectToLogin = true)
            : base(message, statusCode, errorCode ?? "AUTH_ERROR")
        {
            ShouldClearSession = shouldClearSession;
            ShouldRedirectToLogin = shouldRedirectToLogin;
        }
        
        /// <summary>
        /// Create session expired exception
        /// </summary>
        public static AuthException SessionExpired()
        {
            return new AuthException(
                "Session has expired",
                401,
                "SESSION_EXPIRED",
                shouldClearSession: true,
                shouldRedirectToLogin: true
            );
        }
        
        /// <summary>
        /// Create invalid credentials exception
        /// </summary>
        public static AuthException InvalidCredentials()
        {
            return new AuthException(
                "Invalid email or password",
                401,
                "INVALID_CREDENTIALS",
                shouldClearSession: false,
                shouldRedirectToLogin: false
            );
        }
        
        /// <summary>
        /// Create forbidden exception
        /// </summary>
        public static AuthException Forbidden(string message = "Access denied")
        {
            return new AuthException(
                message,
                403,
                "FORBIDDEN",
                shouldClearSession: false,
                shouldRedirectToLogin: false
            );
        }
    }
    
    #endregion
    
    #region Server Exception
    
    /// <summary>
    /// Exception for server errors (5xx status codes)
    /// </summary>
    public class ServerException : ApiException
    {
        /// <summary>
        /// Server errors are often retryable
        /// </summary>
        public override bool IsRetryable => true;
        
        /// <summary>
        /// User-friendly message
        /// </summary>
        public override string UserMessage => "Server error. Our crew is working on it!";
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ServerException(string message, int statusCode = 500, string errorCode = null,
            string requestUrl = null)
            : base(message, statusCode, errorCode ?? "SERVER_ERROR", requestUrl)
        {
        }
        
        /// <summary>
        /// Create internal server error
        /// </summary>
        public static ServerException InternalError(string requestUrl = null)
        {
            return new ServerException(
                "Internal server error",
                500,
                "INTERNAL_ERROR",
                requestUrl
            );
        }
        
        /// <summary>
        /// Create service unavailable exception
        /// </summary>
        public static ServerException ServiceUnavailable(string requestUrl = null)
        {
            return new ServerException(
                "Service temporarily unavailable",
                503,
                "SERVICE_UNAVAILABLE",
                requestUrl
            );
        }
    }
    
    #endregion
    
    #region Validation Exception
    
    /// <summary>
    /// Exception for validation errors (400 Bad Request)
    /// </summary>
    public class ValidationException : ApiException
    {
        /// <summary>
        /// Field that failed validation (if provided)
        /// </summary>
        public string Field { get; }
        
        /// <summary>
        /// Validation errors are not retryable without fixing input
        /// </summary>
        public override bool IsRetryable => false;
        
        /// <summary>
        /// User-friendly message
        /// </summary>
        public override string UserMessage => !string.IsNullOrEmpty(Field)
            ? $"Invalid {Field}. Please check and try again."
            : "Invalid request. Please check your input.";
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ValidationException(string message, string field = null, string errorCode = null)
            : base(message, 400, errorCode ?? "VALIDATION_ERROR")
        {
            Field = field;
        }
        
        /// <summary>
        /// Create required field exception
        /// </summary>
        public static ValidationException Required(string field)
        {
            return new ValidationException(
                $"{field} is required",
                field,
                "REQUIRED_FIELD"
            );
        }
        
        /// <summary>
        /// Create invalid format exception
        /// </summary>
        public static ValidationException InvalidFormat(string field, string expectedFormat = null)
        {
            string message = expectedFormat != null
                ? $"{field} must be in format: {expectedFormat}"
                : $"{field} has invalid format";
                
            return new ValidationException(message, field, "INVALID_FORMAT");
        }
    }
    
    #endregion
    
    #region Not Found Exception
    
    /// <summary>
    /// Exception for not found errors (404)
    /// </summary>
    public class NotFoundException : ApiException
    {
        /// <summary>
        /// Resource type that wasn't found
        /// </summary>
        public string ResourceType { get; }
        
        /// <summary>
        /// Resource ID that wasn't found
        /// </summary>
        public string ResourceId { get; }
        
        /// <summary>
        /// Not found errors are not retryable
        /// </summary>
        public override bool IsRetryable => false;
        
        /// <summary>
        /// User-friendly message
        /// </summary>
        public override string UserMessage => !string.IsNullOrEmpty(ResourceType)
            ? $"{ResourceType} not found."
            : "The requested item was not found.";
        
        /// <summary>
        /// Constructor
        /// </summary>
        public NotFoundException(string message, string resourceType = null, string resourceId = null)
            : base(message, 404, "NOT_FOUND")
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
        }
        
        /// <summary>
        /// Create coin not found exception
        /// </summary>
        public static NotFoundException CoinNotFound(string coinId)
        {
            return new NotFoundException(
                $"Coin {coinId} not found",
                "Coin",
                coinId
            );
        }
        
        /// <summary>
        /// Create user not found exception
        /// </summary>
        public static NotFoundException UserNotFound(string userId)
        {
            return new NotFoundException(
                $"User {userId} not found",
                "User",
                userId
            );
        }
    }
    
    #endregion
    
    #region Rate Limit Exception
    
    /// <summary>
    /// Exception for rate limiting (429 Too Many Requests)
    /// </summary>
    public class RateLimitException : ApiException
    {
        /// <summary>
        /// Seconds until retry is allowed
        /// </summary>
        public int RetryAfterSeconds { get; }
        
        /// <summary>
        /// Rate limit errors are retryable after delay
        /// </summary>
        public override bool IsRetryable => true;
        
        /// <summary>
        /// User-friendly message
        /// </summary>
        public override string UserMessage => RetryAfterSeconds > 0
            ? $"Too many requests. Please wait {RetryAfterSeconds} seconds."
            : "Too many requests. Please slow down, matey!";
        
        /// <summary>
        /// Constructor
        /// </summary>
        public RateLimitException(string message, int retryAfterSeconds = 60)
            : base(message, 429, "RATE_LIMITED")
        {
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
    
    #endregion
}
