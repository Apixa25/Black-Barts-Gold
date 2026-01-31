// ============================================================================
// PlayerLocationService.cs
// Black Bart's Gold - Player Location Tracking Service
// Path: Assets/Scripts/Core/PlayerLocationService.cs
// ============================================================================
// Sends player location updates to the admin dashboard for real-time tracking.
// Updates are throttled to every 5 seconds to balance accuracy and bandwidth.
// Reference: admin-dashboard/src/hooks/use-player-tracking.ts
// ============================================================================

using UnityEngine;
using System;
using System.Threading.Tasks;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Service that periodically sends player location to the admin dashboard.
    /// Enables real-time player tracking on the admin map.
    /// </summary>
    public class PlayerLocationService : MonoBehaviour
    {
        #region Singleton
        
        private static PlayerLocationService _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static PlayerLocationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayerLocationService>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PlayerLocationService");
                        _instance = go.AddComponent<PlayerLocationService>();
                        Debug.Log("[PlayerLocationService] 📍 Created new PlayerLocationService instance");
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
        
        #region Configuration
        
        /// <summary>
        /// Minimum interval between location updates (seconds)
        /// Matches admin-dashboard/src/components/maps/player-config.ts
        /// </summary>
        [SerializeField]
        private float updateIntervalSeconds = 5f;
        
        /// <summary>
        /// Maximum retries on network failure
        /// </summary>
        [SerializeField]
        private int maxRetries = 2;
        
        /// <summary>
        /// Enable/disable tracking (can be toggled by user settings)
        /// </summary>
        [SerializeField]
        private bool trackingEnabled = true;
        
        /// <summary>
        /// Minimum distance change (meters) to trigger an update
        /// Saves bandwidth when user is stationary
        /// </summary>
        [SerializeField]
        private float minDistanceChangeMeters = 2f;
        
        #endregion
        
        #region State
        
        private float _lastUpdateTime = 0f;
        private LocationData _lastSentLocation = null;
        private bool _isUpdating = false;
        private string _currentSessionId = null;
        private bool _isArActive = false;
        private bool _isSubscribed = false;
        
        /// <summary>
        /// Is tracking currently active?
        /// Checks if tracking is enabled and we have a valid user ID to send
        /// </summary>
        public bool IsTrackingActive
        {
            get
            {
                if (!trackingEnabled) return false;
                
                // Check if we have a valid user ID from any source
                string userId = GetUserId();
                return !string.IsNullOrEmpty(userId);
            }
        }
        
        /// <summary>
        /// Last successful update time
        /// </summary>
        public float LastUpdateTime => _lastUpdateTime;
        
        /// <summary>
        /// Number of updates sent this session
        /// </summary>
        public int UpdatesSentCount { get; private set; } = 0;
        
        /// <summary>
        /// Number of update failures
        /// </summary>
        public int UpdateFailuresCount { get; private set; } = 0;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when location update is sent successfully
        /// </summary>
        public event Action<LocationData> OnLocationSent;
        
        /// <summary>
        /// Fired when location update fails
        /// </summary>
        public event Action<string> OnLocationSendFailed;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Enforce singleton
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[PlayerLocationService] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Generate session ID for this play session
            _currentSessionId = Guid.NewGuid().ToString();
            
            Debug.Log("[PlayerLocationService] 📍 PlayerLocationService initialized");
        }
        
        private void Start()
        {
            // Subscribe to PlayerData location updates
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnLocationUpdated += HandleLocationUpdated;
                _isSubscribed = true;
                Debug.Log("[PlayerLocationService] ✅ Subscribed to PlayerData.OnLocationUpdated");
            }
            else
            {
                Debug.LogWarning("[PlayerLocationService] ⚠️ PlayerData not found, starting retry coroutine...");
                StartCoroutine(RetrySubscriptionCoroutine());
            }
        }
        
        /// <summary>
        /// Coroutine to retry subscribing to PlayerData until successful
        /// </summary>
        private System.Collections.IEnumerator RetrySubscriptionCoroutine()
        {
            int attempts = 0;
            const int maxAttempts = 30; // Try for up to 30 seconds
            
            while (!_isSubscribed && attempts < maxAttempts)
            {
                yield return new WaitForSeconds(1f);
                attempts++;
                
                if (PlayerData.Exists)
                {
                    PlayerData.Instance.OnLocationUpdated -= HandleLocationUpdated; // Prevent duplicates
                    PlayerData.Instance.OnLocationUpdated += HandleLocationUpdated;
                    _isSubscribed = true;
                    Debug.Log($"[PlayerLocationService] ✅ Subscription established after {attempts}s retry");
                    yield break;
                }
                
                if (attempts % 5 == 0)
                {
                    Debug.Log($"[PlayerLocationService] 🔄 Retry {attempts}/{maxAttempts}: Waiting for PlayerData...");
                }
            }
            
            if (!_isSubscribed)
            {
                Debug.LogError("[PlayerLocationService] ❌ Failed to subscribe after 30 attempts. Location tracking disabled.");
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnLocationUpdated -= HandleLocationUpdated;
            }
            
            // Send a final "going offline" signal
            SendOfflineSignal();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App going to background - can optionally stop tracking
                Debug.Log("[PlayerLocationService] ⏸️ App paused");
            }
            else
            {
                // App resuming - reset timer to allow immediate update
                _lastUpdateTime = Time.time - updateIntervalSeconds;
                Debug.Log("[PlayerLocationService] ▶️ App resumed");
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Enable or disable location tracking
        /// </summary>
        public void SetTrackingEnabled(bool enabled)
        {
            trackingEnabled = enabled;
            Debug.Log($"[PlayerLocationService] Tracking {(enabled ? "enabled" : "disabled")}");
            
            if (!enabled)
            {
                SendOfflineSignal();
            }
        }
        
        /// <summary>
        /// Set AR active state (for dashboard display)
        /// </summary>
        public void SetArActive(bool isActive)
        {
            _isArActive = isActive;
            
            // If AR just became active, send an immediate update
            if (isActive)
            {
                ForceUpdate();
            }
        }
        
        /// <summary>
        /// Force an immediate location update (ignores throttle)
        /// </summary>
        public void ForceUpdate()
        {
            if (PlayerData.Exists && PlayerData.Instance.CurrentLocation != null)
            {
                _ = SendLocationUpdateAsync(PlayerData.Instance.CurrentLocation, force: true);
            }
        }
        
        /// <summary>
        /// Start a new session (call on login)
        /// </summary>
        public void StartNewSession()
        {
            _currentSessionId = Guid.NewGuid().ToString();
            _lastSentLocation = null;
            _lastUpdateTime = 0f;
            UpdatesSentCount = 0;
            UpdateFailuresCount = 0;
            
            Debug.Log($"[PlayerLocationService] 🆕 New session started: {_currentSessionId}");
        }
        
        #endregion
        
        #region Location Handling
        
        /// <summary>
        /// Handle location updates from PlayerData
        /// </summary>
        private void HandleLocationUpdated(LocationData location)
        {
            Debug.Log($"[PlayerLocationService] 📍 HandleLocationUpdated called: ({location.latitude:F6}, {location.longitude:F6})");
            
            if (!IsTrackingActive)
            {
                string userId = GetUserId();
                Debug.Log($"[PlayerLocationService] ⏸️ SKIPPED: Tracking not active (trackingEnabled={trackingEnabled}, userId={(!string.IsNullOrEmpty(userId) ? userId : "NONE")})");
                return;
            }
            
            // Throttle updates
            float timeSinceLastUpdate = Time.time - _lastUpdateTime;
            if (timeSinceLastUpdate < updateIntervalSeconds)
            {
                Debug.Log($"[PlayerLocationService] ⏳ THROTTLED: {timeSinceLastUpdate:F1}s since last update (need {updateIntervalSeconds}s)");
                return;
            }
            
            // Check minimum distance change (if we have a previous location)
            if (_lastSentLocation != null)
            {
                float distance = location.DistanceTo(_lastSentLocation);
                if (distance < minDistanceChangeMeters)
                {
                    Debug.Log($"[PlayerLocationService] 📏 SKIPPED: Only moved {distance:F1}m (need {minDistanceChangeMeters}m)");
                    return;
                }
                Debug.Log($"[PlayerLocationService] 📏 Distance check passed: moved {distance:F1}m");
            }
            else
            {
                Debug.Log("[PlayerLocationService] 📏 First location update (no previous location)");
            }
            
            // Send update
            Debug.Log("[PlayerLocationService] 🚀 Initiating location send...");
            _ = SendLocationUpdateAsync(location, force: false);
        }
        
        /// <summary>
        /// Send location update to the server
        /// </summary>
        private async Task SendLocationUpdateAsync(LocationData location, bool force)
        {
            Debug.Log($"[PlayerLocationService] 📤 SendLocationUpdateAsync START (force={force}, isUpdating={_isUpdating})");
            
            if (_isUpdating && !force)
            {
                Debug.Log("[PlayerLocationService] ⏸️ SKIPPED: Already updating (use force=true to override)");
                return;
            }
            
            _isUpdating = true;
            
            try
            {
                // Get user ID
                string userId = GetUserId();
                Debug.Log($"[PlayerLocationService] 👤 User ID: {(string.IsNullOrEmpty(userId) ? "NULL/EMPTY" : userId)}");
                
                if (string.IsNullOrEmpty(userId))
                {
                    Debug.LogWarning("[PlayerLocationService] ⚠️ No user ID available, skipping update");
                    Debug.LogWarning($"[PlayerLocationService]   PlayerData.Exists={PlayerData.Exists}");
                    Debug.LogWarning($"[PlayerLocationService]   CurrentUser={(PlayerData.Exists && PlayerData.Instance.CurrentUser != null ? "exists" : "NULL")}");
                    Debug.LogWarning($"[PlayerLocationService]   PlayerPrefs user_id='{PlayerPrefs.GetString("user_id", "")}'");
                    return;
                }
                
                // Build request body
                var requestBody = new LocationUpdateRequest
                {
                    userId = userId,
                    latitude = location.latitude,
                    longitude = location.longitude,
                    altitude = location.altitude,
                    accuracyMeters = location.horizontalAccuracy,
                    heading = location.heading,
                    speedMps = location.speed,
                    deviceId = SystemInfo.deviceUniqueIdentifier,
                    deviceModel = SystemInfo.deviceModel,
                    appVersion = Application.version,
                    sessionId = _currentSessionId,
                    isArActive = _isArActive,
                    isMockLocation = IsMockLocationEnabled(),
                    clientTimestamp = location.timestamp
                };
                
                // Log the full request
                Debug.Log($"[PlayerLocationService] 📦 REQUEST BODY:");
                Debug.Log($"[PlayerLocationService]   userId: {requestBody.userId}");
                Debug.Log($"[PlayerLocationService]   coords: ({requestBody.latitude:F6}, {requestBody.longitude:F6})");
                Debug.Log($"[PlayerLocationService]   accuracy: {requestBody.accuracyMeters:F1}m");
                Debug.Log($"[PlayerLocationService]   heading: {requestBody.heading:F1}°, speed: {requestBody.speedMps:F2}m/s");
                Debug.Log($"[PlayerLocationService]   deviceId: {requestBody.deviceId}");
                Debug.Log($"[PlayerLocationService]   sessionId: {requestBody.sessionId}");
                Debug.Log($"[PlayerLocationService]   endpoint: {ApiConfig.Player.LOCATION}");
                
                // Send to API
                Debug.Log($"[PlayerLocationService] 🌐 Calling ApiClient.Post to {ApiConfig.Player.LOCATION}...");
                var response = await ApiClient.Instance.Post<LocationUpdateResponse>(
                    ApiConfig.Player.LOCATION, 
                    requestBody
                );
                
                Debug.Log($"[PlayerLocationService] 📥 Response received: {(response != null ? "not null" : "NULL")}");
                
                if (response != null && response.success)
                {
                    _lastUpdateTime = Time.time;
                    _lastSentLocation = location.Clone();
                    UpdatesSentCount++;
                    
                    Debug.Log($"[PlayerLocationService] ✅✅✅ SUCCESS! Location #{UpdatesSentCount} sent!");
                    Debug.Log($"[PlayerLocationService]   coords: ({location.latitude:F4}, {location.longitude:F4})");
                    Debug.Log($"[PlayerLocationService]   movementType: {response.movementType}");
                    Debug.Log($"[PlayerLocationService]   locationId: {response.locationId}");
                    Debug.Log($"[PlayerLocationService]   timestamp: {response.timestamp}");
                    
                    OnLocationSent?.Invoke(location);
                }
                else
                {
                    UpdateFailuresCount++;
                    Debug.LogWarning($"[PlayerLocationService] ⚠️ Location update FAILED (failure #{UpdateFailuresCount})");
                    Debug.LogWarning($"[PlayerLocationService]   response is null: {response == null}");
                    if (response != null)
                    {
                        Debug.LogWarning($"[PlayerLocationService]   response.success: {response.success}");
                    }
                    OnLocationSendFailed?.Invoke("Update failed");
                }
            }
            catch (NetworkException ex)
            {
                UpdateFailuresCount++;
                Debug.LogWarning($"[PlayerLocationService] 🌐 NETWORK ERROR (failure #{UpdateFailuresCount})");
                Debug.LogWarning($"[PlayerLocationService]   Message: {ex.Message}");
                Debug.LogWarning($"[PlayerLocationService]   Check internet connection!");
                OnLocationSendFailed?.Invoke(ex.Message);
            }
            catch (ApiException ex)
            {
                UpdateFailuresCount++;
                Debug.LogWarning($"[PlayerLocationService] ❌ API ERROR (failure #{UpdateFailuresCount})");
                Debug.LogWarning($"[PlayerLocationService]   Message: {ex.Message}");
                Debug.LogWarning($"[PlayerLocationService]   This may be a server-side issue.");
                OnLocationSendFailed?.Invoke(ex.Message);
            }
            catch (Exception ex)
            {
                UpdateFailuresCount++;
                Debug.LogError($"[PlayerLocationService] ❌ UNEXPECTED ERROR (failure #{UpdateFailuresCount})");
                Debug.LogError($"[PlayerLocationService]   Type: {ex.GetType().Name}");
                Debug.LogError($"[PlayerLocationService]   Message: {ex.Message}");
                Debug.LogError($"[PlayerLocationService]   StackTrace: {ex.StackTrace}");
                OnLocationSendFailed?.Invoke(ex.Message);
            }
            finally
            {
                _isUpdating = false;
                Debug.Log("[PlayerLocationService] 📤 SendLocationUpdateAsync END");
            }
        }
        
        /// <summary>
        /// Send signal that player is going offline
        /// </summary>
        private async void SendOfflineSignal()
        {
            if (!IsTrackingActive)
            {
                return;
            }
            
            try
            {
                string userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return;
                }
                
                // DELETE request to remove from live tracking
                await ApiClient.Instance.Delete(ApiConfig.Player.GetDeleteLocationUrl(userId));
                
                Debug.Log("[PlayerLocationService] 👋 Offline signal sent");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerLocationService] Failed to send offline signal: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get the current user's ID from multiple sources
        /// </summary>
        private string GetUserId()
        {
            // Log what sources are available
            Debug.Log($"[PlayerLocationService] 🔍 GetUserId checking sources...");
            Debug.Log($"[PlayerLocationService]   PlayerData.Exists={PlayerData.Exists}");
            Debug.Log($"[PlayerLocationService]   AuthService.Exists={AuthService.Exists}");
            
            // Try PlayerData first (if it exists and has a user)
            if (PlayerData.Exists && PlayerData.Instance.CurrentUser != null)
            {
                string id = PlayerData.Instance.CurrentUser.id;
                Debug.Log($"[PlayerLocationService]   PlayerData.CurrentUser.id='{id}'");
                if (!string.IsNullOrEmpty(id))
                {
                    Debug.Log($"[PlayerLocationService] ✅ Got userId from PlayerData: {id}");
                    return id;
                }
            }
            
            // Try AuthService (if it exists and has a user)
            if (AuthService.Exists && AuthService.Instance.CurrentUser != null)
            {
                string id = AuthService.Instance.CurrentUser.id;
                Debug.Log($"[PlayerLocationService]   AuthService.CurrentUser.id='{id}'");
                if (!string.IsNullOrEmpty(id))
                {
                    Debug.Log($"[PlayerLocationService] ✅ Got userId from AuthService: {id}");
                    return id;
                }
            }
            
            // Fallback to PlayerPrefs (where AuthService stores the user_id on login)
            string prefsUserId = PlayerPrefs.GetString("user_id", "");
            Debug.Log($"[PlayerLocationService]   PlayerPrefs 'user_id'='{prefsUserId}'");
            if (!string.IsNullOrEmpty(prefsUserId))
            {
                Debug.Log($"[PlayerLocationService] ✅ Got userId from PlayerPrefs: {prefsUserId}");
                return prefsUserId;
            }
            
            // Last resort: try to get from auth_user_id key (some implementations use this)
            string authUserId = PlayerPrefs.GetString("auth_user_id", "");
            Debug.Log($"[PlayerLocationService]   PlayerPrefs 'auth_user_id'='{authUserId}'");
            if (!string.IsNullOrEmpty(authUserId))
            {
                Debug.Log($"[PlayerLocationService] ✅ Got userId from PlayerPrefs auth_user_id: {authUserId}");
                return authUserId;
            }
            
            Debug.LogWarning("[PlayerLocationService] ❌ No userId found from any source!");
            return "";
        }
        
        /// <summary>
        /// Check if mock location is enabled on the device
        /// </summary>
        private bool IsMockLocationEnabled()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var settingsSecure = new AndroidJavaClass("android.provider.Settings$Secure"))
                using (var context = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity"))
                using (var contentResolver = context.Call<AndroidJavaObject>("getContentResolver"))
                {
                    // Check ALLOW_MOCK_LOCATION setting (deprecated but still used)
                    int mockEnabled = settingsSecure.CallStatic<int>(
                        "getInt", 
                        contentResolver, 
                        "mock_location",
                        0
                    );
                    return mockEnabled != 0;
                }
            }
            catch
            {
                return false;
            }
            #else
            return false;
            #endif
        }
        
        /// <summary>
        /// Ensure subscription to PlayerData
        /// Call this if PlayerData wasn't available at Start()
        /// </summary>
        public void EnsureSubscription()
        {
            if (PlayerData.Exists)
            {
                // Unsubscribe first to avoid duplicate subscriptions
                PlayerData.Instance.OnLocationUpdated -= HandleLocationUpdated;
                PlayerData.Instance.OnLocationUpdated += HandleLocationUpdated;
                _isSubscribed = true;
                Debug.Log("[PlayerLocationService] ✅ Subscription ensured");
            }
            else
            {
                Debug.LogWarning("[PlayerLocationService] ⚠️ EnsureSubscription called but PlayerData not available");
            }
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Print service state
        /// </summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== PlayerLocationService State ===");
            Debug.Log($"Tracking Enabled: {trackingEnabled}");
            Debug.Log($"Is Tracking Active: {IsTrackingActive}");
            Debug.Log($"Update Interval: {updateIntervalSeconds}s");
            Debug.Log($"Updates Sent: {UpdatesSentCount}");
            Debug.Log($"Update Failures: {UpdateFailuresCount}");
            Debug.Log($"Is AR Active: {_isArActive}");
            Debug.Log($"Session ID: {_currentSessionId}");
            Debug.Log($"Last Update: {_lastUpdateTime}s ago");
            Debug.Log($"Last Location: {_lastSentLocation}");
            Debug.Log("===================================");
        }
        
        /// <summary>
        /// Debug: Force send test location
        /// </summary>
        [ContextMenu("Debug: Send Test Location")]
        public void DebugSendTestLocation()
        {
            var testLocation = LocationData.CreateTestLocation();
            _ = SendLocationUpdateAsync(testLocation, force: true);
        }
        
        #endregion
    }
    
    #region Request/Response Types
    
    /// <summary>
    /// Location update request body
    /// </summary>
    [Serializable]
    public class LocationUpdateRequest
    {
        public string userId;
        public double latitude;
        public double longitude;
        public float altitude;
        public float accuracyMeters;
        public float heading;
        public float speedMps;
        public string deviceId;
        public string deviceModel;
        public string appVersion;
        public string sessionId;
        public bool isArActive;
        public bool isMockLocation;
        public string clientTimestamp;
    }
    
    /// <summary>
    /// Location update response
    /// </summary>
    [Serializable]
    public class LocationUpdateResponse
    {
        public bool success;
        public string locationId;
        public string movementType;
        public string timestamp;
    }
    
    #endregion
}
