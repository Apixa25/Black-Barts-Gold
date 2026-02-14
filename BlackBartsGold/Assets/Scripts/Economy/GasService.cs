// ============================================================================
// GasService.cs
// Black Bart's Gold - Gas System Service
// Path: Assets/Scripts/Economy/GasService.cs
// ============================================================================
// Manages the gas (fuel) system - daily consumption, warnings, and blocking
// gameplay when empty. Gas is the daily cost to play (~$0.33/day).
// Reference: BUILD-GUIDE.md Sprint 7, Prompt 7.2
// ============================================================================

using UnityEngine;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Economy
{
    /// <summary>
    /// Gas service singleton.
    /// Handles daily gas consumption, status checking, and warnings.
    /// $10 = 30 days of play, ~$0.33 consumed per day at midnight.
    /// </summary>
    public class GasService : MonoBehaviour
    {
        #region Singleton
        
        private static GasService _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static GasService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GasService>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GasService");
                        _instance = go.AddComponent<GasService>();
                        Debug.Log("[GasService] ⛽ Created new GasService instance");
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
        /// Daily gas consumption rate (BBG)
        /// </summary>
        public const float DAILY_RATE = 0.33f;
        
        /// <summary>
        /// Full tank value (30 days worth)
        /// </summary>
        public const float FULL_TANK = 10.00f;
        
        /// <summary>
        /// Full tank in days
        /// </summary>
        public const int FULL_TANK_DAYS = 30;
        
        /// <summary>
        /// Low gas threshold (percentage)
        /// </summary>
        public const float LOW_THRESHOLD = 0.15f; // 15%
        
        /// <summary>
        /// Normal/full threshold (percentage)
        /// </summary>
        public const float FULL_THRESHOLD = 0.50f; // 50%
        
        /// <summary>
        /// PlayerPrefs key for last charge date
        /// </summary>
        private const string LAST_CHARGE_KEY = "last_gas_charge";
        
        /// <summary>
        /// PlayerPrefs key for dismissed warning
        /// </summary>
        private const string WARNING_DISMISSED_KEY = "gas_warning_dismissed";
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Has gas been checked this session?
        /// </summary>
        public bool HasCheckedGasToday { get; private set; } = false;
        
        /// <summary>
        /// Was gas consumed on this check?
        /// </summary>
        public bool GasWasConsumedToday { get; private set; } = false;
        
        /// <summary>
        /// Has low gas warning been dismissed this session?
        /// </summary>
        public bool WarningDismissedThisSession { get; private set; } = false;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when gas is consumed
        /// </summary>
        public event Action<float> OnGasConsumed;
        
        /// <summary>
        /// Fired when gas status changes
        /// </summary>
        public event Action<GasStatusInfo> OnGasStatusChanged;
        
        /// <summary>
        /// Fired when gas is empty (can't play)
        /// </summary>
        public event Action OnGasEmpty;
        
        /// <summary>
        /// Fired when gas is low (warning)
        /// </summary>
        public event Action<GasStatusInfo> OnGasLow;
        
        /// <summary>
        /// Fired when gas is refilled
        /// </summary>
        public event Action<float> OnGasRefilled;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[GasService] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[GasService] ⛽ GasService initialized");
        }
        
        #endregion
        
        #region Gas Status
        
        /// <summary>
        /// Get current gas status
        /// </summary>
        public GasStatusInfo GetGasStatus()
        {
            var wallet = GetWallet();
            
            float remaining = wallet?.gasTank ?? 0f;
            float percentage = remaining / FULL_TANK;
            int daysLeft = (int)Math.Floor(remaining / DAILY_RATE);
            
            GasLevel level;
            string message;
            
            if (remaining <= 0)
            {
                level = GasLevel.Empty;
                message = "Ye've run aground, matey! No fuel left!";
            }
            else if (percentage < LOW_THRESHOLD)
            {
                level = GasLevel.Low;
                message = $"Low fuel! Only {daysLeft} day{(daysLeft != 1 ? "s" : "")} remaining!";
            }
            else if (percentage < FULL_THRESHOLD)
            {
                level = GasLevel.Normal;
                message = $"{daysLeft} days of fuel remaining";
            }
            else
            {
                level = GasLevel.Full;
                message = $"Full tank! {daysLeft} days of adventure ahead!";
            }
            
            return new GasStatusInfo
            {
                remaining = remaining,
                daysLeft = daysLeft,
                percentage = percentage,
                level = level,
                isEmpty = remaining <= 0,
                isLow = percentage < LOW_THRESHOLD && remaining > 0,
                message = message
            };
        }
        
        /// <summary>
        /// Get gas meter color based on level
        /// </summary>
        public Color GetGasMeterColor()
        {
            var status = GetGasStatus();
            return GetGasMeterColor(status.level);
        }
        
        /// <summary>
        /// Get gas meter color for a specific level
        /// </summary>
        public Color GetGasMeterColor(GasLevel level)
        {
            return level switch
            {
                GasLevel.Full => new Color(0.29f, 0.87f, 0.5f),    // Green
                GasLevel.Normal => new Color(0.98f, 0.75f, 0.14f), // Yellow/Gold
                GasLevel.Low => new Color(0.94f, 0.27f, 0.27f),    // Red
                GasLevel.Empty => new Color(0.5f, 0.5f, 0.5f),     // Gray
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Can player currently play?
        /// </summary>
        public bool CanPlay()
        {
            var wallet = GetWallet();
            return wallet?.CanPlay() ?? false;
        }
        
        #endregion
        
        #region Gas Consumption
        
        /// <summary>
        /// Check and consume gas on app launch
        /// Should be called once per day at app start
        /// </summary>
        public void CheckAndConsumeGas()
        {
            if (HasCheckedGasToday)
            {
                Debug.Log("[GasService] Gas already checked today");
                return;
            }
            
            HasCheckedGasToday = true;
            
            string lastChargeStr = PlayerPrefs.GetString(LAST_CHARGE_KEY, "");
            DateTime lastCharge;
            
            if (string.IsNullOrEmpty(lastChargeStr))
            {
                // First time - no charge yet
                lastCharge = DateTime.UtcNow.Date.AddDays(-1); // Pretend yesterday
            }
            else if (!DateTime.TryParse(lastChargeStr, out lastCharge))
            {
                lastCharge = DateTime.UtcNow.Date.AddDays(-1);
            }
            
            // Calculate days since last charge
            int daysSinceCharge = (int)(DateTime.UtcNow.Date - lastCharge.Date).TotalDays;
            
            if (daysSinceCharge > 0)
            {
                // Consume gas for each day missed
                float totalToConsume = DAILY_RATE * daysSinceCharge;
                
                Debug.Log($"[GasService] ⛽ Consuming {daysSinceCharge} day(s) of gas: ${totalToConsume:F2}");
                
                ConsumeGas(totalToConsume);
                GasWasConsumedToday = true;
                
                // Update last charge date
                SaveLastChargeDate();
            }
            else
            {
                Debug.Log("[GasService] No gas consumption needed today");
                GasWasConsumedToday = false;
            }
            
            // Check status after consumption
            var status = GetGasStatus();
            OnGasStatusChanged?.Invoke(status);
            
            if (status.isEmpty)
            {
                OnGasEmpty?.Invoke();
            }
            else if (status.isLow)
            {
                OnGasLow?.Invoke(status);
            }
        }
        
        /// <summary>
        /// Consume a specific amount of gas
        /// </summary>
        private void ConsumeGas(float amount)
        {
            if (PlayerData.Exists)
            {
                var wallet = PlayerData.Instance.Wallet;
                if (wallet != null)
                {
                    // Consume gas (may consume less if not enough)
                    float actualConsumed = Math.Min(wallet.gasTank, amount);
                    
                    // Use the wallet's ConsumeGas for each day's worth
                    int days = (int)Math.Ceiling(amount / DAILY_RATE);
                    for (int i = 0; i < days && wallet.gasTank > 0; i++)
                    {
                        wallet.ConsumeGas();
                    }
                    
                    PlayerData.Instance.SaveData();
                    
                    OnGasConsumed?.Invoke(actualConsumed);
                }
            }
        }
        
        /// <summary>
        /// Save last charge date
        /// </summary>
        private void SaveLastChargeDate()
        {
            PlayerPrefs.SetString(LAST_CHARGE_KEY, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Force consume one day's gas (for testing)
        /// </summary>
        public void ForceConsumeOneDay()
        {
            Debug.Log("[GasService] Force consuming one day's gas");
            ConsumeGas(DAILY_RATE);
            SaveLastChargeDate();
            
            var status = GetGasStatus();
            OnGasStatusChanged?.Invoke(status);
            
            if (status.isEmpty)
            {
                OnGasEmpty?.Invoke();
            }
            else if (status.isLow)
            {
                OnGasLow?.Invoke(status);
            }
        }
        
        #endregion
        
        #region Gas Refill
        
        /// <summary>
        /// Add gas (from purchase or unpark)
        /// </summary>
        public void AddGas(float amount, CoinSource source = CoinSource.Purchased)
        {
            Debug.Log($"[GasService] ⛽ Adding ${amount:F2} gas");
            
            if (PlayerData.Exists)
            {
                PlayerData.Instance.AddGas(amount, source);
            }
            
            OnGasRefilled?.Invoke(amount);
            OnGasStatusChanged?.Invoke(GetGasStatus());
        }
        
        #endregion
        
        #region Warning Management
        
        /// <summary>
        /// Dismiss the low gas warning for this session
        /// </summary>
        public void DismissWarning()
        {
            WarningDismissedThisSession = true;
            PlayerPrefs.SetString(WARNING_DISMISSED_KEY, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();
            Debug.Log("[GasService] Low gas warning dismissed");
        }
        
        /// <summary>
        /// Should show low gas warning?
        /// </summary>
        public bool ShouldShowLowGasWarning()
        {
            if (WarningDismissedThisSession) return false;
            
            var status = GetGasStatus();
            return status.isLow && !status.isEmpty;
        }
        
        /// <summary>
        /// Should show no gas overlay?
        /// </summary>
        public bool ShouldShowNoGasOverlay()
        {
            return GetGasStatus().isEmpty;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get wallet reference
        /// </summary>
        private Wallet GetWallet()
        {
            return PlayerData.Exists ? PlayerData.Instance.Wallet : null;
        }
        
        /// <summary>
        /// Calculate days from BBG amount
        /// </summary>
        public int CalculateDays(float bbgAmount)
        {
            return (int)Math.Floor(bbgAmount / DAILY_RATE);
        }
        
        /// <summary>
        /// Calculate BBG from days
        /// </summary>
        public float CalculateBBG(int days)
        {
            return days * DAILY_RATE;
        }
        
        /// <summary>
        /// Format days remaining string
        /// </summary>
        public string FormatDaysRemaining(int days)
        {
            if (days <= 0)
            {
                return "No fuel!";
            }
            else if (days == 1)
            {
                return "1 day left";
            }
            else
            {
                return $"{days} days left";
            }
        }
        
        /// <summary>
        /// Get pirate-themed status message
        /// </summary>
        public string GetPirateStatusMessage(GasLevel level, int daysLeft)
        {
            return level switch
            {
                GasLevel.Full => $"Full sails ahead! {daysLeft} days of adventure await!",
                GasLevel.Normal => $"Smooth sailing, {daysLeft} days of fuel remain!",
                GasLevel.Low => $"Low on fuel! Only {daysLeft} days left, matey!",
                GasLevel.Empty => "Ye've run aground! No fuel to hunt treasure!",
                _ => "Check yer fuel gauge, sailor!"
            };
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Print gas status
        /// </summary>
        [ContextMenu("Debug: Print Gas Status")]
        public void DebugPrintStatus()
        {
            var status = GetGasStatus();
            Debug.Log("=== Gas Status ===");
            Debug.Log($"Remaining: ${status.remaining:F2}");
            Debug.Log($"Days Left: {status.daysLeft}");
            Debug.Log($"Percentage: {status.percentage:P0}");
            Debug.Log($"Level: {status.level}");
            Debug.Log($"Is Empty: {status.isEmpty}");
            Debug.Log($"Is Low: {status.isLow}");
            Debug.Log($"Message: {status.message}");
            Debug.Log("==================");
        }
        
        /// <summary>
        /// Debug: Force consume gas
        /// </summary>
        [ContextMenu("Debug: Consume 1 Day Gas")]
        public void DebugConsumeGas()
        {
            ForceConsumeOneDay();
        }
        
        /// <summary>
        /// Debug: Add gas
        /// </summary>
        [ContextMenu("Debug: Add $5 Gas")]
        public void DebugAddGas()
        {
            AddGas(5.00f);
        }
        
        /// <summary>
        /// Debug: Reset gas check
        /// </summary>
        [ContextMenu("Debug: Reset Gas Check")]
        public void DebugResetGasCheck()
        {
            HasCheckedGasToday = false;
            GasWasConsumedToday = false;
            WarningDismissedThisSession = false;
            PlayerPrefs.DeleteKey(LAST_CHARGE_KEY);
            PlayerPrefs.DeleteKey(WARNING_DISMISSED_KEY);
            PlayerPrefs.Save();
            Debug.Log("[GasService] Gas check reset");
        }
        
        #endregion
    }
    
    #region Gas Status Info
    
    /// <summary>
    /// Gas status information
    /// </summary>
    public class GasStatusInfo
    {
        /// <summary>
        /// Remaining gas in BBG
        /// </summary>
        public float remaining;
        
        /// <summary>
        /// Days of fuel remaining
        /// </summary>
        public int daysLeft;
        
        /// <summary>
        /// Percentage of full tank (0-1)
        /// </summary>
        public float percentage;
        
        /// <summary>
        /// Gas level enum
        /// </summary>
        public GasLevel level;
        
        /// <summary>
        /// Is tank empty?
        /// </summary>
        public bool isEmpty;
        
        /// <summary>
        /// Is tank low (but not empty)?
        /// </summary>
        public bool isLow;
        
        /// <summary>
        /// Status message for display
        /// </summary>
        public string message;
    }
    
    /// <summary>
    /// Gas level enum
    /// </summary>
    public enum GasLevel
    {
        /// <summary>Tank empty, can't play</summary>
        Empty,
        
        /// <summary>Low fuel (less than 15%)</summary>
        Low,
        
        /// <summary>Normal level (15-50%)</summary>
        Normal,
        
        /// <summary>Full tank (more than 50%)</summary>
        Full
    }
    
    #endregion
}
