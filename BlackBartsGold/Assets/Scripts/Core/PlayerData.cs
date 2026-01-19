// ============================================================================
// PlayerData.cs
// Black Bart's Gold - Runtime Player Data Manager
// Path: Assets/Scripts/Core/PlayerData.cs
// ============================================================================
// Singleton that stores all runtime player data. Persists across scenes and
// provides centralized access to user, wallet, location, and settings.
// Auto-saves on critical changes.
// ============================================================================

using UnityEngine;
using System;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Singleton manager for all runtime player data.
    /// Persists across scenes with DontDestroyOnLoad.
    /// Provides events for data changes to update UI.
    /// </summary>
    public class PlayerData : MonoBehaviour
    {
        #region Singleton
        
        private static PlayerData _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static PlayerData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayerData>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PlayerData");
                        _instance = go.AddComponent<PlayerData>();
                        Debug.Log("[PlayerData] 💾 Created new PlayerData instance");
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
        
        #region Data Properties
        
        /// <summary>
        /// Current user profile
        /// </summary>
        public User CurrentUser { get; private set; }
        
        /// <summary>
        /// Current wallet data
        /// </summary>
        public Wallet Wallet { get; private set; }
        
        /// <summary>
        /// Current location
        /// </summary>
        public LocationData CurrentLocation { get; private set; }
        
        /// <summary>
        /// Last known good location
        /// </summary>
        public LocationData LastKnownLocation { get; private set; }
        
        /// <summary>
        /// Authentication token
        /// </summary>
        public string AuthToken { get; private set; }
        
        /// <summary>
        /// Is data loaded and valid?
        /// </summary>
        public bool IsDataLoaded { get; private set; } = false;
        
        #endregion
        
        #region Quick Accessors
        
        /// <summary>
        /// Total BBG balance
        /// </summary>
        public float Balance => Wallet?.total ?? 0f;
        
        /// <summary>
        /// Gas tank balance
        /// </summary>
        public float GasBalance => Wallet?.gasTank ?? 0f;
        
        /// <summary>
        /// Gas remaining in days
        /// </summary>
        public float GasDays => Wallet?.gasRemainingDays ?? 0f;
        
        /// <summary>
        /// Current find limit
        /// </summary>
        public float FindLimit => CurrentUser?.findLimit ?? 1.00f;
        
        /// <summary>
        /// Player display name
        /// </summary>
        public string DisplayName => CurrentUser?.displayName ?? "Pirate";
        
        /// <summary>
        /// Player tier name
        /// </summary>
        public string TierName => CurrentUser?.GetTierName() ?? "Cabin Boy";
        
        /// <summary>
        /// Can player currently play? (has gas)
        /// </summary>
        public bool CanPlay => Wallet?.CanPlay() ?? false;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when any data changes (general update)
        /// </summary>
        public event Action OnDataChanged;
        
        /// <summary>
        /// Fired when balance changes
        /// </summary>
        public event Action<float> OnBalanceChanged;
        
        /// <summary>
        /// Fired when gas changes
        /// </summary>
        public event Action<float> OnGasChanged;
        
        /// <summary>
        /// Fired when find limit changes
        /// </summary>
        public event Action<float> OnFindLimitChanged;
        
        /// <summary>
        /// Fired when location updates
        /// </summary>
        public event Action<LocationData> OnLocationUpdated;
        
        /// <summary>
        /// Fired when user profile updates
        /// </summary>
        public event Action<User> OnUserUpdated;
        
        /// <summary>
        /// Fired when wallet updates
        /// </summary>
        public event Action<Wallet> OnWalletUpdated;
        
        /// <summary>
        /// Fired when data is loaded
        /// </summary>
        public event Action OnDataLoaded;
        
        /// <summary>
        /// Fired when data is cleared (logout)
        /// </summary>
        public event Action OnDataCleared;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Enforce singleton
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[PlayerData] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[PlayerData] 💾 PlayerData initialized");
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && IsDataLoaded)
            {
                // App going to background - save data
                SaveData();
            }
        }
        
        private void OnApplicationQuit()
        {
            if (IsDataLoaded)
            {
                SaveData();
            }
        }
        
        #endregion
        
        #region Data Loading
        
        /// <summary>
        /// Load player data from storage
        /// </summary>
        public void LoadData()
        {
            Debug.Log("[PlayerData] 📂 Loading player data...");
            
            var saveData = SaveSystem.LoadPlayerData();
            
            if (saveData != null)
            {
                CurrentUser = saveData.user ?? new User();
                Wallet = saveData.wallet ?? Wallet.CreateEmptyWallet();
                LastKnownLocation = saveData.lastLocation;
                AuthToken = saveData.authToken;
                
                IsDataLoaded = true;
                Debug.Log($"[PlayerData] ✅ Data loaded for: {CurrentUser.displayName}");
            }
            else
            {
                // No saved data - create empty
                CurrentUser = null;
                Wallet = Wallet.CreateEmptyWallet();
                IsDataLoaded = false;
                Debug.Log("[PlayerData] ℹ️ No saved data found");
            }
            
            OnDataLoaded?.Invoke();
            OnDataChanged?.Invoke();
        }
        
        /// <summary>
        /// Save player data to storage
        /// </summary>
        public void SaveData()
        {
            if (CurrentUser == null)
            {
                Debug.Log("[PlayerData] ⚠️ No user data to save");
                return;
            }
            
            Debug.Log("[PlayerData] 💾 Saving player data...");
            
            var saveData = new PlayerSaveData
            {
                user = CurrentUser,
                wallet = Wallet,
                lastLocation = LastKnownLocation,
                authToken = AuthToken,
                savedAt = DateTime.UtcNow.ToString("o")
            };
            
            SaveSystem.SavePlayerData(saveData);
            Debug.Log("[PlayerData] ✅ Data saved");
        }
        
        /// <summary>
        /// Clear all data (for logout)
        /// </summary>
        public void ClearData()
        {
            Debug.Log("[PlayerData] 🗑️ Clearing player data...");
            
            CurrentUser = null;
            Wallet = Wallet.CreateEmptyWallet();
            CurrentLocation = null;
            LastKnownLocation = null;
            AuthToken = null;
            IsDataLoaded = false;
            
            SaveSystem.DeletePlayerData();
            
            OnDataCleared?.Invoke();
            OnDataChanged?.Invoke();
        }
        
        #endregion
        
        #region User Management
        
        /// <summary>
        /// Set user after login/registration
        /// </summary>
        public void SetUser(User user, string token)
        {
            CurrentUser = user;
            AuthToken = token;
            IsDataLoaded = true;
            
            Debug.Log($"[PlayerData] 👤 User set: {user.displayName}");
            
            OnUserUpdated?.Invoke(user);
            OnDataChanged?.Invoke();
            
            SaveData();
        }
        
        /// <summary>
        /// Update user profile
        /// </summary>
        public void UpdateUser(User user)
        {
            CurrentUser = user;
            
            OnUserUpdated?.Invoke(user);
            OnFindLimitChanged?.Invoke(user.findLimit);
            OnDataChanged?.Invoke();
            
            SaveData();
        }
        
        /// <summary>
        /// Update find limit after hiding a coin
        /// </summary>
        public void UpdateFindLimit(float hiddenValue)
        {
            if (CurrentUser == null) return;
            
            float oldLimit = CurrentUser.findLimit;
            CurrentUser.UpdateFindLimit(hiddenValue);
            
            if (CurrentUser.findLimit > oldLimit)
            {
                Debug.Log($"[PlayerData] 📈 Find limit increased: ${oldLimit:F2} → ${CurrentUser.findLimit:F2}");
                OnFindLimitChanged?.Invoke(CurrentUser.findLimit);
                OnDataChanged?.Invoke();
                SaveData();
            }
        }
        
        #endregion
        
        #region Wallet Management
        
        /// <summary>
        /// Set wallet data
        /// </summary>
        public void SetWallet(Wallet wallet)
        {
            float oldBalance = Wallet?.total ?? 0;
            float oldGas = Wallet?.gasTank ?? 0;
            
            Wallet = wallet;
            
            OnWalletUpdated?.Invoke(wallet);
            
            if (Math.Abs(wallet.total - oldBalance) > 0.001f)
            {
                OnBalanceChanged?.Invoke(wallet.total);
            }
            
            if (Math.Abs(wallet.gasTank - oldGas) > 0.001f)
            {
                OnGasChanged?.Invoke(wallet.gasRemainingDays);
            }
            
            OnDataChanged?.Invoke();
        }
        
        /// <summary>
        /// Add coins to pending (after finding a coin)
        /// </summary>
        public void AddPendingCoins(float amount, string coinId)
        {
            if (Wallet == null) return;
            
            Wallet.AddToPending(amount, coinId);
            CurrentUser?.stats?.RecordFind(amount);
            
            Debug.Log($"[PlayerData] 💰 Pending coins added: +${amount:F2}");
            
            OnBalanceChanged?.Invoke(Wallet.total);
            OnWalletUpdated?.Invoke(Wallet);
            OnDataChanged?.Invoke();
            
            SaveData();
        }
        
        /// <summary>
        /// Consume daily gas
        /// Returns remaining days
        /// </summary>
        public float ConsumeGas()
        {
            if (Wallet == null) return 0;
            
            float remaining = Wallet.ConsumeGas();
            CurrentUser?.ConsumeGas(1f);
            
            Debug.Log($"[PlayerData] ⛽ Gas consumed, {remaining:F1} days remaining");
            
            OnGasChanged?.Invoke(remaining);
            OnWalletUpdated?.Invoke(Wallet);
            OnDataChanged?.Invoke();
            
            SaveData();
            
            return remaining;
        }
        
        /// <summary>
        /// Add gas (from purchase or unpark)
        /// </summary>
        public void AddGas(float bbgAmount, CoinSource source)
        {
            if (Wallet == null) return;
            
            Wallet.AddToGasTank(bbgAmount, source);
            CurrentUser?.AddGas(bbgAmount / 0.33f); // Convert to days
            
            Debug.Log($"[PlayerData] ⛽ Gas added: +${bbgAmount:F2} ({bbgAmount / 0.33f:F1} days)");
            
            OnGasChanged?.Invoke(Wallet.gasRemainingDays);
            OnBalanceChanged?.Invoke(Wallet.total);
            OnWalletUpdated?.Invoke(Wallet);
            OnDataChanged?.Invoke();
            
            SaveData();
        }
        
        /// <summary>
        /// Park coins
        /// </summary>
        public bool ParkCoins(float amount)
        {
            if (Wallet == null) return false;
            
            if (Wallet.Park(amount))
            {
                Debug.Log($"[PlayerData] 🅿️ Parked: ${amount:F2}");
                
                OnWalletUpdated?.Invoke(Wallet);
                OnDataChanged?.Invoke();
                SaveData();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Unpark coins
        /// </summary>
        public bool UnparkCoins(float amount)
        {
            if (Wallet == null) return false;
            
            if (Wallet.Unpark(amount))
            {
                Debug.Log($"[PlayerData] 🚗 Unparked: ${amount:F2}");
                
                OnGasChanged?.Invoke(Wallet.gasRemainingDays);
                OnWalletUpdated?.Invoke(Wallet);
                OnDataChanged?.Invoke();
                SaveData();
                return true;
            }
            
            return false;
        }
        
        #endregion
        
        #region Location Management
        
        /// <summary>
        /// Update current location
        /// </summary>
        public void UpdateLocation(LocationData location)
        {
            CurrentLocation = location;
            
            // Store as last known if accuracy is acceptable
            if (location.IsAccuracyAcceptable())
            {
                LastKnownLocation = location.Clone();
            }
            
            OnLocationUpdated?.Invoke(location);
        }
        
        /// <summary>
        /// Get best available location
        /// </summary>
        public LocationData GetBestLocation()
        {
            if (CurrentLocation != null && CurrentLocation.IsFresh())
            {
                return CurrentLocation;
            }
            return LastKnownLocation;
        }
        
        #endregion
        
        #region Development Helpers
        
        /// <summary>
        /// Initialize with test data (for development)
        /// </summary>
        [ContextMenu("Initialize Test Data")]
        public void InitializeTestData()
        {
            Debug.Log("[PlayerData] 🧪 Initializing test data...");
            
            CurrentUser = User.CreateTestUser();
            Wallet = Wallet.CreateTestWallet();
            LastKnownLocation = LocationData.CreateTestLocation();
            AuthToken = "test-token-12345";
            IsDataLoaded = true;
            
            OnUserUpdated?.Invoke(CurrentUser);
            OnWalletUpdated?.Invoke(Wallet);
            OnDataLoaded?.Invoke();
            OnDataChanged?.Invoke();
            
            Debug.Log($"[PlayerData] ✅ Test data loaded: {CurrentUser}");
        }
        
        /// <summary>
        /// Debug print current state
        /// </summary>
        [ContextMenu("Debug Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== PlayerData State ===");
            Debug.Log($"User: {CurrentUser}");
            Debug.Log($"Wallet: {Wallet}");
            Debug.Log($"Location: {CurrentLocation}");
            Debug.Log($"Auth Token: {(string.IsNullOrEmpty(AuthToken) ? "None" : "Set")}");
            Debug.Log($"Data Loaded: {IsDataLoaded}");
            Debug.Log("========================");
        }
        
        #endregion
    }
    
    #region Save Data Structure
    
    /// <summary>
    /// Container for all saveable player data
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        public User user;
        public Wallet wallet;
        public LocationData lastLocation;
        public string authToken;
        public string savedAt;
    }
    
    #endregion
}
