// ============================================================================
// OfflineManager.cs
// Black Bart's Gold - Offline Support Manager
// Path: Assets/Scripts/Core/OfflineManager.cs
// ============================================================================
// Manages offline mode: caches data, queues actions for sync, and handles
// network status changes.
// Reference: BUILD-GUIDE.md Sprint 8, Prompt 8.2
// ============================================================================

using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Offline manager singleton.
    /// Handles caching, action queuing, and network status.
    /// </summary>
    public class OfflineManager : MonoBehaviour
    {
        #region Singleton
        
        private static OfflineManager _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static OfflineManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<OfflineManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("OfflineManager");
                        _instance = go.AddComponent<OfflineManager>();
                        Debug.Log("[OfflineManager] 📶 Created new OfflineManager instance");
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
        
        #region Constants
        
        /// <summary>
        /// Network check interval (seconds)
        /// </summary>
        private const float NETWORK_CHECK_INTERVAL = 5f;
        
        /// <summary>
        /// Max queued actions
        /// </summary>
        private const int MAX_QUEUED_ACTIONS = 100;
        
        /// <summary>
        /// Cache file name
        /// </summary>
        private const string CACHE_FILE = "offline_cache.json";
        
        /// <summary>
        /// Queue file name
        /// </summary>
        private const string QUEUE_FILE = "action_queue.json";
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is device online?
        /// </summary>
        public bool IsOnline => Application.internetReachability != NetworkReachability.NotReachable;
        
        /// <summary>
        /// Is device offline?
        /// </summary>
        public bool IsOffline => !IsOnline;
        
        /// <summary>
        /// Network reachability type
        /// </summary>
        public NetworkReachability NetworkType => Application.internetReachability;
        
        /// <summary>
        /// Is WiFi connected?
        /// </summary>
        public bool IsWifi => Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
        
        /// <summary>
        /// Is on mobile data?
        /// </summary>
        public bool IsMobileData => Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork;
        
        /// <summary>
        /// Number of queued actions
        /// </summary>
        public int QueuedActionCount => _actionQueue.Count;
        
        /// <summary>
        /// Is sync in progress?
        /// </summary>
        public bool IsSyncing { get; private set; } = false;
        
        /// <summary>
        /// Last sync time
        /// </summary>
        public DateTime LastSyncTime { get; private set; } = DateTime.MinValue;
        
        #endregion
        
        #region Private Fields
        
        private bool _wasOnline = true;
        private float _networkCheckTimer = 0f;
        private List<QueuedAction> _actionQueue = new List<QueuedAction>();
        private OfflineCache _cache = new OfflineCache();
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when network status changes
        /// </summary>
        public event Action<bool> OnNetworkStatusChanged;
        
        /// <summary>
        /// Fired when going online
        /// </summary>
        public event Action OnWentOnline;
        
        /// <summary>
        /// Fired when going offline
        /// </summary>
        public event Action OnWentOffline;
        
        /// <summary>
        /// Fired when sync starts
        /// </summary>
        public event Action OnSyncStarted;
        
        /// <summary>
        /// Fired when sync completes
        /// </summary>
        public event Action<int, int> OnSyncCompleted; // (success, failed)
        
        /// <summary>
        /// Fired when action is queued
        /// </summary>
        public event Action<QueuedAction> OnActionQueued;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[OfflineManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadCache();
            LoadQueue();
            
            _wasOnline = IsOnline;
            
            Debug.Log($"[OfflineManager] 📶 Initialized (Online: {IsOnline})");
        }
        
        private void Update()
        {
            // Periodic network check
            _networkCheckTimer += Time.deltaTime;
            if (_networkCheckTimer >= NETWORK_CHECK_INTERVAL)
            {
                _networkCheckTimer = 0f;
                CheckNetworkStatus();
            }
        }
        
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                // Save state when app goes to background
                SaveCache();
                SaveQueue();
            }
            else
            {
                // Check network when app resumes
                CheckNetworkStatus();
            }
        }
        
        private void OnDestroy()
        {
            SaveCache();
            SaveQueue();
        }
        
        #endregion
        
        #region Network Status
        
        /// <summary>
        /// Check current network status
        /// </summary>
        public void CheckNetworkStatus()
        {
            bool currentOnline = IsOnline;
            
            if (currentOnline != _wasOnline)
            {
                _wasOnline = currentOnline;
                
                Debug.Log($"[OfflineManager] Network status changed: {(currentOnline ? "ONLINE" : "OFFLINE")}");
                
                OnNetworkStatusChanged?.Invoke(currentOnline);
                
                if (currentOnline)
                {
                    OnWentOnline?.Invoke();
                    
                    // Auto-sync when coming online
                    _ = SyncQueuedActions();
                }
                else
                {
                    OnWentOffline?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Get network status string
        /// </summary>
        public string GetNetworkStatusString()
        {
            return NetworkType switch
            {
                NetworkReachability.NotReachable => "Offline",
                NetworkReachability.ReachableViaCarrierDataNetwork => "Mobile Data",
                NetworkReachability.ReachableViaLocalAreaNetwork => "WiFi",
                _ => "Unknown"
            };
        }
        
        #endregion
        
        #region Action Queue
        
        /// <summary>
        /// Queue an action for later sync
        /// </summary>
        public void QueueAction(QueuedActionType type, string data, string resourceId = null)
        {
            if (_actionQueue.Count >= MAX_QUEUED_ACTIONS)
            {
                Debug.LogWarning("[OfflineManager] Action queue is full!");
                return;
            }
            
            var action = new QueuedAction
            {
                id = Guid.NewGuid().ToString(),
                type = type,
                data = data,
                resourceId = resourceId,
                timestamp = DateTime.UtcNow.ToString("o"),
                retryCount = 0
            };
            
            _actionQueue.Add(action);
            SaveQueue();
            
            Debug.Log($"[OfflineManager] ⏳ Queued action: {type}");
            OnActionQueued?.Invoke(action);
        }
        
        /// <summary>
        /// Sync all queued actions
        /// </summary>
        public async Task SyncQueuedActions()
        {
            if (!IsOnline)
            {
                Debug.Log("[OfflineManager] Cannot sync - offline");
                return;
            }
            
            if (IsSyncing)
            {
                Debug.Log("[OfflineManager] Sync already in progress");
                return;
            }
            
            if (_actionQueue.Count == 0)
            {
                Debug.Log("[OfflineManager] No actions to sync");
                return;
            }
            
            IsSyncing = true;
            OnSyncStarted?.Invoke();
            
            Debug.Log($"[OfflineManager] 🔄 Starting sync of {_actionQueue.Count} actions...");
            
            int successCount = 0;
            int failCount = 0;
            var actionsToRemove = new List<QueuedAction>();
            
            foreach (var action in _actionQueue.ToArray())
            {
                try
                {
                    await ProcessQueuedAction(action);
                    actionsToRemove.Add(action);
                    successCount++;
                    Debug.Log($"[OfflineManager] ✅ Synced: {action.type}");
                }
                catch (Exception ex)
                {
                    action.retryCount++;
                    
                    if (action.retryCount >= 3)
                    {
                        // Give up after 3 retries
                        actionsToRemove.Add(action);
                        failCount++;
                        Debug.LogWarning($"[OfflineManager] ❌ Failed after retries: {action.type} - {ex.Message}");
                    }
                    else
                    {
                        Debug.LogWarning($"[OfflineManager] ⚠️ Retry {action.retryCount}/3: {action.type}");
                    }
                }
            }
            
            // Remove processed actions
            foreach (var action in actionsToRemove)
            {
                _actionQueue.Remove(action);
            }
            
            SaveQueue();
            IsSyncing = false;
            LastSyncTime = DateTime.UtcNow;
            
            Debug.Log($"[OfflineManager] 🔄 Sync complete: {successCount} success, {failCount} failed");
            OnSyncCompleted?.Invoke(successCount, failCount);
        }
        
        /// <summary>
        /// Process a single queued action
        /// </summary>
        private async Task ProcessQueuedAction(QueuedAction action)
        {
            switch (action.type)
            {
                case QueuedActionType.CollectCoin:
                    if (CoinApiService.Exists)
                    {
                        await CoinApiService.Instance.CollectCoin(action.resourceId);
                    }
                    break;
                    
                case QueuedActionType.HideCoin:
                    if (CoinApiService.Exists)
                    {
                        var request = JsonUtility.FromJson<HideCoinRequest>(action.data);
                        await CoinApiService.Instance.HideCoin(request);
                    }
                    break;
                    
                case QueuedActionType.UpdateWallet:
                    // Wallet sync handled separately
                    break;
                    
                case QueuedActionType.UpdateProfile:
                    // Profile sync handled separately
                    break;
                    
                default:
                    Debug.LogWarning($"[OfflineManager] Unknown action type: {action.type}");
                    break;
            }
        }
        
        /// <summary>
        /// Clear action queue
        /// </summary>
        public void ClearQueue()
        {
            _actionQueue.Clear();
            SaveQueue();
            Debug.Log("[OfflineManager] Queue cleared");
        }
        
        #endregion
        
        #region Cache
        
        /// <summary>
        /// Cache data for offline use
        /// </summary>
        public void CacheData<T>(string key, T data)
        {
            string json = JsonUtility.ToJson(data);
            _cache.entries[key] = new CacheEntry
            {
                key = key,
                data = json,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            SaveCache();
            
            Debug.Log($"[OfflineManager] 💾 Cached: {key}");
        }
        
        /// <summary>
        /// Get cached data
        /// </summary>
        public T GetCachedData<T>(string key) where T : class
        {
            if (_cache.entries.TryGetValue(key, out CacheEntry entry))
            {
                try
                {
                    return JsonUtility.FromJson<T>(entry.data);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Check if data is cached
        /// </summary>
        public bool HasCachedData(string key)
        {
            return _cache.entries.ContainsKey(key);
        }
        
        /// <summary>
        /// Clear specific cached data
        /// </summary>
        public void ClearCachedData(string key)
        {
            _cache.entries.Remove(key);
            SaveCache();
        }
        
        /// <summary>
        /// Clear all cached data
        /// </summary>
        public void ClearAllCache()
        {
            _cache.entries.Clear();
            SaveCache();
            Debug.Log("[OfflineManager] Cache cleared");
        }
        
        #endregion
        
        #region Persistence
        
        /// <summary>
        /// Get cache file path
        /// </summary>
        private string GetCachePath()
        {
            return Path.Combine(Application.persistentDataPath, CACHE_FILE);
        }
        
        /// <summary>
        /// Get queue file path
        /// </summary>
        private string GetQueuePath()
        {
            return Path.Combine(Application.persistentDataPath, QUEUE_FILE);
        }
        
        /// <summary>
        /// Load cache from disk
        /// </summary>
        private void LoadCache()
        {
            try
            {
                string path = GetCachePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    _cache = JsonUtility.FromJson<OfflineCache>(json) ?? new OfflineCache();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OfflineManager] Failed to load cache: {ex.Message}");
                _cache = new OfflineCache();
            }
        }
        
        /// <summary>
        /// Save cache to disk
        /// </summary>
        private void SaveCache()
        {
            try
            {
                string json = JsonUtility.ToJson(_cache);
                File.WriteAllText(GetCachePath(), json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OfflineManager] Failed to save cache: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load action queue from disk
        /// </summary>
        private void LoadQueue()
        {
            try
            {
                string path = GetQueuePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var wrapper = JsonUtility.FromJson<QueueWrapper>(json);
                    _actionQueue = wrapper?.actions ?? new List<QueuedAction>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OfflineManager] Failed to load queue: {ex.Message}");
                _actionQueue = new List<QueuedAction>();
            }
        }
        
        /// <summary>
        /// Save action queue to disk
        /// </summary>
        private void SaveQueue()
        {
            try
            {
                var wrapper = new QueueWrapper { actions = _actionQueue };
                string json = JsonUtility.ToJson(wrapper);
                File.WriteAllText(GetQueuePath(), json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OfflineManager] Failed to save queue: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Print status
        /// </summary>
        [ContextMenu("Debug: Print Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== Offline Manager Status ===");
            Debug.Log($"Online: {IsOnline}");
            Debug.Log($"Network: {GetNetworkStatusString()}");
            Debug.Log($"Queued Actions: {QueuedActionCount}");
            Debug.Log($"Cached Items: {_cache.entries.Count}");
            Debug.Log($"Is Syncing: {IsSyncing}");
            Debug.Log($"Last Sync: {LastSyncTime}");
            Debug.Log("==============================");
        }
        
        /// <summary>
        /// Debug: Force sync
        /// </summary>
        [ContextMenu("Debug: Force Sync")]
        public async void DebugForceSync()
        {
            await SyncQueuedActions();
        }
        
        /// <summary>
        /// Debug: Queue test action
        /// </summary>
        [ContextMenu("Debug: Queue Test Action")]
        public void DebugQueueTestAction()
        {
            QueueAction(QueuedActionType.CollectCoin, "{}", "test-coin-123");
        }
        
        #endregion
    }
    
    #region Data Types
    
    /// <summary>
    /// Types of actions that can be queued
    /// </summary>
    public enum QueuedActionType
    {
        CollectCoin,
        HideCoin,
        UpdateWallet,
        UpdateProfile,
        Custom
    }
    
    /// <summary>
    /// Queued action data
    /// </summary>
    [Serializable]
    public class QueuedAction
    {
        public string id;
        public QueuedActionType type;
        public string data;
        public string resourceId;
        public string timestamp;
        public int retryCount;
    }
    
    /// <summary>
    /// Queue wrapper for serialization
    /// </summary>
    [Serializable]
    public class QueueWrapper
    {
        public List<QueuedAction> actions = new List<QueuedAction>();
    }
    
    /// <summary>
    /// Offline cache data
    /// </summary>
    [Serializable]
    public class OfflineCache
    {
        public SerializableDictionary entries = new SerializableDictionary();
    }
    
    /// <summary>
    /// Cache entry
    /// </summary>
    [Serializable]
    public class CacheEntry
    {
        public string key;
        public string data;
        public string timestamp;
    }
    
    /// <summary>
    /// Serializable dictionary for cache
    /// </summary>
    [Serializable]
    public class SerializableDictionary : Dictionary<string, CacheEntry>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<string> _keys = new List<string>();
        
        [SerializeField]
        private List<CacheEntry> _values = new List<CacheEntry>();
        
        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            
            foreach (var kvp in this)
            {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }
        
        public void OnAfterDeserialize()
        {
            Clear();
            
            for (int i = 0; i < Math.Min(_keys.Count, _values.Count); i++)
            {
                this[_keys[i]] = _values[i];
            }
        }
    }
    
    #endregion
}
