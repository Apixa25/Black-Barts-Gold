// ============================================================================
// GPSManager.cs
// Black Bart's Gold - GPS Location Service Manager
// Path: Assets/Scripts/Location/GPSManager.cs
// ============================================================================
// Singleton that manages GPS location tracking. Handles permissions, 
// location updates, accuracy filtering, and battery-efficient updates.
// Reference: BUILD-GUIDE.md Prompt 4.1
// ============================================================================

using UnityEngine;
using System;
using System.Collections;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Location
{
    /// <summary>
    /// Manages GPS location tracking for the game.
    /// Singleton that provides location updates and handles permissions.
    /// </summary>
    public class GPSManager : MonoBehaviour
    {
        #region Singleton
        
        private static GPSManager _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static GPSManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GPSManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GPSManager");
                        _instance = go.AddComponent<GPSManager>();
                        DontDestroyOnLoad(go);
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
        
        #region Inspector Fields
        
        [Header("Location Settings")]
        [SerializeField]
        [Tooltip("Desired accuracy in meters (lower = more accurate, more battery)")]
        private float desiredAccuracy = 5f;
        
        [SerializeField]
        [Tooltip("Minimum distance moved before update (meters)")]
        private float updateDistance = 2f;
        
        [SerializeField]
        [Tooltip("How often to poll for location (seconds)")]
        private float pollInterval = 1f;
        
        [SerializeField]
        [Tooltip("Maximum age of location before considered stale (seconds)")]
        private float maxLocationAge = 30f;
        
        [SerializeField]
        [Tooltip("Minimum accuracy to accept for displaying coins (meters) - higher values are more lenient")]
        private float minAcceptableAccuracy = 200f; // Lenient for display; collection requires better accuracy (see CoinController)
        
        [Header("Timeout Settings")]
        [SerializeField]
        [Tooltip("Timeout waiting for GPS initialization (seconds)")]
        private float initializationTimeout = 30f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        [SerializeField]
        [Tooltip("Use simulated location in editor")]
        private bool useSimulatedLocation = true;
        
        [SerializeField]
        [Tooltip("Simulated location for editor testing")]
        private Vector2 simulatedLocation = new Vector2(37.7749f, -122.4194f); // San Francisco
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current GPS service state
        /// </summary>
        public GPSServiceState ServiceState { get; private set; } = GPSServiceState.Stopped;
        
        /// <summary>
        /// Current location data
        /// </summary>
        public LocationData CurrentLocation { get; private set; }
        
        /// <summary>
        /// Last known good location (may be stale)
        /// </summary>
        public LocationData LastKnownLocation { get; private set; }
        
        /// <summary>
        /// Is GPS currently tracking?
        /// </summary>
        public bool IsTracking => ServiceState == GPSServiceState.Running;
        
        /// <summary>
        /// Is location permission granted?
        /// </summary>
        public bool HasPermission { get; private set; } = false;
        
        /// <summary>
        /// Is GPS enabled on device?
        /// </summary>
        public bool IsGPSEnabled => Input.location.isEnabledByUser;
        
        /// <summary>
        /// Current accuracy level
        /// </summary>
        public GPSAccuracy AccuracyLevel
        {
            get
            {
                if (CurrentLocation == null) return GPSAccuracy.None;
                return CurrentLocation.GetAccuracyLevel();
            }
        }
        
        /// <summary>
        /// Error message if service failed
        /// </summary>
        public string ErrorMessage { get; private set; }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when location updates
        /// </summary>
        public event Action<LocationData> OnLocationUpdated;
        
        /// <summary>
        /// Fired when location error occurs
        /// </summary>
        public event Action<string> OnLocationError;
        
        /// <summary>
        /// Fired when service state changes
        /// </summary>
        public event Action<GPSServiceState> OnServiceStateChanged;
        
        /// <summary>
        /// Fired when permission is granted
        /// </summary>
        public event Action OnPermissionGranted;
        
        /// <summary>
        /// Fired when permission is denied
        /// </summary>
        public event Action OnPermissionDenied;
        
        /// <summary>
        /// Fired when GPS is disabled on device
        /// </summary>
        public event Action OnGPSDisabled;
        
        /// <summary>
        /// Fired when accuracy changes significantly
        /// </summary>
        public event Action<GPSAccuracy> OnAccuracyChanged;
        
        #endregion
        
        #region Private Fields
        
        private Coroutine locationCoroutine;
        private GPSAccuracy lastAccuracyLevel = GPSAccuracy.None;
        private float lastUpdateTime = 0f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Log("GPSManager initialized");
        }
        
        private void Start()
        {
            // In Editor with simulation enabled, auto-start with simulated location
            #if UNITY_EDITOR
            if (useSimulatedLocation)
            {
                Log("Editor mode - using simulated location");
                StartCoroutine(EditorSimulationCoroutine());
            }
            #endif
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Coroutine that simulates location updates in the Editor
        /// </summary>
        private IEnumerator EditorSimulationCoroutine()
        {
            // Wait a frame for everything to initialize
            yield return null;
            
            SetState(GPSServiceState.Running);
            HasPermission = true;
            
            // Send initial location
            SetSimulatedLocation(simulatedLocation.x, simulatedLocation.y);
            Log($"Editor simulation started at {simulatedLocation.x:F6}, {simulatedLocation.y:F6}");
            
            // Keep state running (location updates come from SetSimulatedLocation or LocationSimulator)
            while (useSimulatedLocation)
            {
                yield return new WaitForSeconds(1f);
            }
        }
        #endif
        
        private void OnDestroy()
        {
            StopLocationService();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App going to background - stop GPS to save battery
                if (IsTracking)
                {
                    Log("App paused - stopping GPS");
                    StopLocationService();
                }
            }
            else
            {
                // App resuming - restart GPS if we were tracking
                if (ServiceState == GPSServiceState.Paused)
                {
                    Log("App resumed - restarting GPS");
                    StartLocationService();
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Start the location service
        /// </summary>
        public void StartLocationService()
        {
            if (IsTracking)
            {
                Log("Location service already running");
                return;
            }
            
            Log("Starting location service...");
            
            // Start the location coroutine (it will handle permission requests)
            if (locationCoroutine != null)
            {
                StopCoroutine(locationCoroutine);
            }
            locationCoroutine = StartCoroutine(LocationServiceCoroutine());
        }
        
        /// <summary>
        /// Stop the location service
        /// </summary>
        public void StopLocationService()
        {
            if (locationCoroutine != null)
            {
                StopCoroutine(locationCoroutine);
                locationCoroutine = null;
            }
            
            if (Input.location.status == LocationServiceStatus.Running)
            {
                Input.location.Stop();
            }
            
            SetState(GPSServiceState.Stopped);
            Log("Location service stopped");
        }
        
        /// <summary>
        /// Get the current location (may be null)
        /// </summary>
        public LocationData GetCurrentLocation()
        {
            // In editor, return simulated location
            #if UNITY_EDITOR
            if (useSimulatedLocation)
            {
                return new LocationData(simulatedLocation.x, simulatedLocation.y)
                {
                    horizontalAccuracy = 5f,
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            #endif
            
            return CurrentLocation;
        }
        
        /// <summary>
        /// Get best available location (current or last known)
        /// </summary>
        public LocationData GetBestLocation()
        {
            if (CurrentLocation != null && CurrentLocation.IsFresh((int)maxLocationAge))
            {
                return CurrentLocation;
            }
            return LastKnownLocation;
        }
        
        /// <summary>
        /// Check if we have a usable location
        /// </summary>
        public bool HasUsableLocation()
        {
            var location = GetBestLocation();
            return location != null && location.IsValid();
        }
        
        /// <summary>
        /// Set simulated location (for testing)
        /// </summary>
        public void SetSimulatedLocation(double latitude, double longitude)
        {
            simulatedLocation = new Vector2((float)latitude, (float)longitude);
            
            #if UNITY_EDITOR
            if (useSimulatedLocation)
            {
                CurrentLocation = new LocationData(latitude, longitude)
                {
                    horizontalAccuracy = 5f,
                    timestamp = DateTime.UtcNow.ToString("o")
                };
                LastKnownLocation = CurrentLocation.Clone();
                OnLocationUpdated?.Invoke(CurrentLocation);
                Log($"Simulated location set: {latitude}, {longitude}");
            }
            #endif
        }
        
        /// <summary>
        /// Request location permission (Android)
        /// </summary>
        public void RequestPermission()
        {
            #if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                UnityEngine.Android.Permission.FineLocation))
            {
                UnityEngine.Android.Permission.RequestUserPermission(
                    UnityEngine.Android.Permission.FineLocation);
            }
            #endif
        }
        
        #endregion
        
        #region Location Coroutine
        
        /// <summary>
        /// Main location service coroutine
        /// </summary>
        private IEnumerator LocationServiceCoroutine()
        {
            float startTime = Time.realtimeSinceStartup;
            Debug.Log($"[GPSManager] T+{startTime:F2}s: LocationServiceCoroutine STARTED");
            
            SetState(GPSServiceState.Initializing);
            
            // Check permission on Android
            #if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                UnityEngine.Android.Permission.FineLocation))
            {
                Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: Requesting location permission...");
                UnityEngine.Android.Permission.RequestUserPermission(
                    UnityEngine.Android.Permission.FineLocation);
                
                // Wait a moment for permission dialog
                yield return new WaitForSeconds(0.5f);
                
                // Check again
                float permissionWaitTime = 0f;
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                    UnityEngine.Android.Permission.FineLocation) && permissionWaitTime < 30f)
                {
                    yield return new WaitForSeconds(0.5f);
                    permissionWaitTime += 0.5f;
                    Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: Waiting for permission... ({permissionWaitTime:F1}s)");
                }
                
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                    UnityEngine.Android.Permission.FineLocation))
                {
                    HasPermission = false;
                    SetState(GPSServiceState.PermissionDenied);
                    ErrorMessage = "Location permission denied";
                    Debug.LogError($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: PERMISSION DENIED after {permissionWaitTime:F1}s");
                    OnPermissionDenied?.Invoke();
                    OnLocationError?.Invoke(ErrorMessage);
                    yield break;
                }
            }
            HasPermission = true;
            Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: Permission GRANTED");
            OnPermissionGranted?.Invoke();
            #else
            HasPermission = true;
            Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: Non-Android platform, permission auto-granted");
            #endif
            
            // Start Unity's location service
            Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: Calling Input.location.Start(accuracy={desiredAccuracy}m, distance={updateDistance}m)");
            Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: Input.location.status BEFORE Start: {Input.location.status}");
            Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: Input.location.isEnabledByUser: {Input.location.isEnabledByUser}");
            
            Input.location.Start(desiredAccuracy, updateDistance);
            
            Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: Input.location.status AFTER Start: {Input.location.status}");
            
            // Wait for initialization
            float waitTime = 0f;
            while (Input.location.status == LocationServiceStatus.Initializing && 
                   waitTime < initializationTimeout)
            {
                yield return new WaitForSeconds(1f);
                waitTime += 1f;
                Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: GPS initializing... ({waitTime:F0}s/{initializationTimeout:F0}s), status={Input.location.status}");
            }
            
            // Check result
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                SetState(GPSServiceState.Failed);
                ErrorMessage = "Unable to determine device location";
                Debug.LogError($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: GPS FAILED after {waitTime:F1}s");
                OnLocationError?.Invoke(ErrorMessage);
                yield break;
            }
            
            if (waitTime >= initializationTimeout)
            {
                SetState(GPSServiceState.Timeout);
                ErrorMessage = "GPS initialization timed out";
                Debug.LogError($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: GPS TIMEOUT after {waitTime:F1}s");
                OnLocationError?.Invoke(ErrorMessage);
                yield break;
            }
            
            // GPS is running!
            SetState(GPSServiceState.Running);
            float totalInitTime = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: *** GPS SERVICE RUNNING! *** Total init time: {totalInitTime:F2}s");
            
            // Initialize PlayerLocationService to send location updates to admin dashboard
            if (PlayerLocationService.Exists || PlayerLocationService.Instance != null)
            {
                PlayerLocationService.Instance.EnsureSubscription();
                Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: PlayerLocationService initialized for real-time tracking");
            }
            
            // Log first location data
            if (Input.location.status == LocationServiceStatus.Running)
            {
                var data = Input.location.lastData;
                Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: FIRST FIX: Lat={data.latitude:F6}, Lng={data.longitude:F6}, Acc={data.horizontalAccuracy:F1}m");
            }
            
            // Main location update loop
            int updateCount = 0;
            while (ServiceState == GPSServiceState.Running)
            {
                UpdateLocation();
                updateCount++;
                if (updateCount % 10 == 0) // Log every 10th update
                {
                    Debug.Log($"[GPSManager] T+{Time.realtimeSinceStartup:F2}s: Update loop #{updateCount}, CurrentLocation={(CurrentLocation != null ? $"valid ({CurrentLocation.horizontalAccuracy:F1}m)" : "NULL")}");
                }
                yield return new WaitForSeconds(pollInterval);
            }
        }
        
        /// <summary>
        /// Update current location from GPS
        /// </summary>
        private void UpdateLocation()
        {
            if (Input.location.status != LocationServiceStatus.Running)
            {
                return;
            }
            
            LocationInfo info = Input.location.lastData;
            
            // Check accuracy
            if (info.horizontalAccuracy > minAcceptableAccuracy)
            {
                Log($"Location accuracy too low: {info.horizontalAccuracy}m (need < {minAcceptableAccuracy}m)");
                return;
            }
            
            // Create location data
            LocationData newLocation = new LocationData
            {
                latitude = info.latitude,
                longitude = info.longitude,
                altitude = info.altitude,
                horizontalAccuracy = info.horizontalAccuracy,
                verticalAccuracy = info.verticalAccuracy,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            
            // Check if location actually changed
            if (CurrentLocation != null)
            {
                float distance = CurrentLocation.DistanceTo(newLocation);
                if (distance < updateDistance * 0.5f)
                {
                    // Hasn't moved enough, skip update
                    return;
                }
            }
            
            // Update locations
            CurrentLocation = newLocation;
            LastKnownLocation = newLocation.Clone();
            lastUpdateTime = Time.time;
            
            // Check accuracy level change
            GPSAccuracy newAccuracy = newLocation.GetAccuracyLevel();
            if (newAccuracy != lastAccuracyLevel)
            {
                lastAccuracyLevel = newAccuracy;
                OnAccuracyChanged?.Invoke(newAccuracy);
            }
            
            // Update PlayerData
            if (PlayerData.Exists)
            {
                PlayerData.Instance.UpdateLocation(newLocation);
            }
            
            // Notify listeners
            OnLocationUpdated?.Invoke(newLocation);
            
            Log($"Location updated: {newLocation.ToCoordinateString()} (±{newLocation.horizontalAccuracy:F0}m)");
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Set service state and fire event
        /// </summary>
        private void SetState(GPSServiceState newState)
        {
            if (ServiceState == newState) return;
            
            GPSServiceState oldState = ServiceState;
            ServiceState = newState;
            
            Log($"GPS state: {oldState} → {newState}");
            OnServiceStateChanged?.Invoke(newState);
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[GPSManager] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print GPS status
        /// </summary>
        [ContextMenu("Debug: Print GPS Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== GPS Status ===");
            Debug.Log($"Service State: {ServiceState}");
            Debug.Log($"GPS Enabled: {IsGPSEnabled}");
            Debug.Log($"Has Permission: {HasPermission}");
            Debug.Log($"Is Tracking: {IsTracking}");
            Debug.Log($"Current Location: {CurrentLocation?.ToCoordinateString() ?? "null"}");
            Debug.Log($"Accuracy: {AccuracyLevel} ({CurrentLocation?.horizontalAccuracy ?? 0}m)");
            Debug.Log($"Last Update: {(Time.time - lastUpdateTime):F1}s ago");
            Debug.Log($"Error: {ErrorMessage ?? "none"}");
            Debug.Log("==================");
        }
        
        /// <summary>
        /// Debug: Simulate location update
        /// </summary>
        [ContextMenu("Debug: Simulate Location Update")]
        public void DebugSimulateUpdate()
        {
            SetSimulatedLocation(
                simulatedLocation.x + UnityEngine.Random.Range(-0.0001f, 0.0001f),
                simulatedLocation.y + UnityEngine.Random.Range(-0.0001f, 0.0001f)
            );
        }
        
        #endregion
    }
    
    #region Enums
    
    /// <summary>
    /// GPS service state
    /// </summary>
    public enum GPSServiceState
    {
        /// <summary>Service not started</summary>
        Stopped,
        
        /// <summary>Waiting for GPS to initialize</summary>
        Initializing,
        
        /// <summary>GPS running and tracking</summary>
        Running,
        
        /// <summary>GPS paused (app in background)</summary>
        Paused,
        
        /// <summary>GPS disabled on device</summary>
        Disabled,
        
        /// <summary>Permission denied by user</summary>
        PermissionDenied,
        
        /// <summary>GPS failed to start</summary>
        Failed,
        
        /// <summary>Initialization timed out</summary>
        Timeout
    }
    
    #endregion
}
