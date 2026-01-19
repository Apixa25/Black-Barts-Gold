// ============================================================================
// FindLimitService.cs
// Black Bart's Gold - Find Limit Service
// Path: Assets/Scripts/Economy/FindLimitService.cs
// ============================================================================
// Enforces the find limit system - players can only collect coins up to their
// limit, which equals the highest value coin they've ever hidden.
// Reference: BUILD-GUIDE.md Sprint 7, Prompt 7.4
// ============================================================================

using UnityEngine;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Economy
{
    /// <summary>
    /// Find limit service singleton.
    /// Manages the find limit system - your limit equals your highest hidden coin.
    /// Default limit: $1.00 (for players who haven't hidden any coins)
    /// </summary>
    public class FindLimitService : MonoBehaviour
    {
        #region Singleton
        
        private static FindLimitService _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static FindLimitService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<FindLimitService>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("FindLimitService");
                        _instance = go.AddComponent<FindLimitService>();
                        Debug.Log("[FindLimitService] 🔒 Created new FindLimitService instance");
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
        /// Default find limit for new players
        /// </summary>
        public const float DEFAULT_LIMIT = 1.00f;
        
        #endregion
        
        #region Tier Definitions
        
        /// <summary>
        /// Tier thresholds and names
        /// </summary>
        public static readonly TierInfo[] Tiers = new TierInfo[]
        {
            new TierInfo(FindLimitTier.CabinBoy, 1.00f, "Cabin Boy", new Color(0.8f, 0.5f, 0.2f)),      // Bronze
            new TierInfo(FindLimitTier.DeckHand, 5.00f, "Deck Hand", new Color(0.75f, 0.75f, 0.75f)),   // Silver
            new TierInfo(FindLimitTier.TreasureHunter, 10.00f, "Treasure Hunter", new Color(1f, 0.84f, 0f)), // Gold
            new TierInfo(FindLimitTier.Captain, 25.00f, "Captain", new Color(0.9f, 0.9f, 1f)),          // Platinum
            new TierInfo(FindLimitTier.PirateLegend, 50.00f, "Pirate Legend", new Color(0.7f, 0.9f, 1f)), // Diamond
            new TierInfo(FindLimitTier.KingOfPirates, 100.00f, "King of Pirates", new Color(1f, 0.5f, 0.8f)) // Legendary
        };
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when limit increases
        /// </summary>
        public event Action<float, float> OnLimitIncreased; // (oldLimit, newLimit)
        
        /// <summary>
        /// Fired when tier changes
        /// </summary>
        public event Action<TierInfo, TierInfo> OnTierChanged; // (oldTier, newTier)
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[FindLimitService] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[FindLimitService] 🔒 FindLimitService initialized");
        }
        
        #endregion
        
        #region Limit Methods
        
        /// <summary>
        /// Get current find limit
        /// </summary>
        public float GetCurrentLimit()
        {
            if (PlayerData.Exists && PlayerData.Instance.CurrentUser != null)
            {
                return PlayerData.Instance.CurrentUser.findLimit;
            }
            return DEFAULT_LIMIT;
        }
        
        /// <summary>
        /// Check if a coin is over the player's limit
        /// </summary>
        public bool IsOverLimit(Coin coin)
        {
            if (coin == null) return false;
            
            float coinValue = coin.GetEffectiveValue();
            float playerLimit = GetCurrentLimit();
            
            return coinValue > playerLimit;
        }
        
        /// <summary>
        /// Check if a value is over the player's limit
        /// </summary>
        public bool IsValueOverLimit(float value)
        {
            return value > GetCurrentLimit();
        }
        
        /// <summary>
        /// Update limit after hiding a coin
        /// Returns true if limit increased
        /// </summary>
        public bool UpdateLimitAfterHide(float hiddenValue)
        {
            float currentLimit = GetCurrentLimit();
            
            if (hiddenValue > currentLimit)
            {
                float oldLimit = currentLimit;
                TierInfo oldTier = GetTierInfo();
                
                // Update limit
                if (PlayerData.Exists)
                {
                    PlayerData.Instance.UpdateFindLimit(hiddenValue);
                }
                
                TierInfo newTier = GetTierInfo();
                
                Debug.Log($"[FindLimitService] 📈 Limit increased: ${oldLimit:F2} → ${hiddenValue:F2}");
                
                OnLimitIncreased?.Invoke(oldLimit, hiddenValue);
                
                if (oldTier.tier != newTier.tier)
                {
                    Debug.Log($"[FindLimitService] 🎖️ Tier up! {oldTier.name} → {newTier.name}");
                    OnTierChanged?.Invoke(oldTier, newTier);
                }
                
                return true;
            }
            
            return false;
        }
        
        #endregion
        
        #region Tier Methods
        
        /// <summary>
        /// Get current tier info
        /// </summary>
        public TierInfo GetTierInfo()
        {
            float limit = GetCurrentLimit();
            return GetTierForLimit(limit);
        }
        
        /// <summary>
        /// Get tier info for a specific limit
        /// </summary>
        public TierInfo GetTierForLimit(float limit)
        {
            // Find highest matching tier
            TierInfo result = Tiers[0];
            
            for (int i = Tiers.Length - 1; i >= 0; i--)
            {
                if (limit >= Tiers[i].threshold)
                {
                    result = Tiers[i];
                    break;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get tier by enum
        /// </summary>
        public TierInfo GetTier(FindLimitTier tier)
        {
            foreach (var t in Tiers)
            {
                if (t.tier == tier) return t;
            }
            return Tiers[0];
        }
        
        /// <summary>
        /// Get next tier info (or null if at max)
        /// </summary>
        public TierInfo GetNextTier()
        {
            float limit = GetCurrentLimit();
            
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (Tiers[i].threshold > limit)
                {
                    return Tiers[i];
                }
            }
            
            return null; // Already at max tier
        }
        
        /// <summary>
        /// Get amount needed to unlock next tier
        /// </summary>
        public float GetAmountToNextTier()
        {
            var nextTier = GetNextTier();
            if (nextTier == null) return 0f;
            
            return nextTier.threshold;
        }
        
        /// <summary>
        /// Get progress to next tier (0-1)
        /// </summary>
        public float GetProgressToNextTier()
        {
            float limit = GetCurrentLimit();
            var currentTier = GetTierInfo();
            var nextTier = GetNextTier();
            
            if (nextTier == null) return 1f;
            
            float range = nextTier.threshold - currentTier.threshold;
            float progress = limit - currentTier.threshold;
            
            return range > 0 ? progress / range : 1f;
        }
        
        /// <summary>
        /// Get tier color for a coin value
        /// </summary>
        public Color GetColorForValue(float value)
        {
            return GetTierForLimit(value).color;
        }
        
        #endregion
        
        #region Display Methods
        
        /// <summary>
        /// Format limit for display
        /// </summary>
        public string FormatLimit(float limit)
        {
            return $"${limit:F2}";
        }
        
        /// <summary>
        /// Get display string for current limit
        /// </summary>
        public string GetLimitDisplayString()
        {
            return FormatLimit(GetCurrentLimit());
        }
        
        /// <summary>
        /// Get "Find: $X.XX" string
        /// </summary>
        public string GetFindDisplayString()
        {
            return $"Find: {GetLimitDisplayString()}";
        }
        
        /// <summary>
        /// Get message when coin is over limit
        /// </summary>
        public string GetOverLimitMessage(Coin coin)
        {
            float limit = GetCurrentLimit();
            float coinValue = coin.GetEffectiveValue();
            
            return $"This treasure be above yer limit, matey!\n" +
                   $"Coin Value: ${coinValue:F2}\n" +
                   $"Your Limit: ${limit:F2}";
        }
        
        /// <summary>
        /// Get hint for unlocking a coin
        /// </summary>
        public string GetUnlockHint(Coin coin)
        {
            float coinValue = coin.GetEffectiveValue();
            return $"Hide ${coinValue:F2} to unlock!";
        }
        
        /// <summary>
        /// Get tier up celebration message
        /// </summary>
        public string GetTierUpMessage(TierInfo newTier)
        {
            return newTier.tier switch
            {
                FindLimitTier.DeckHand => "🎖️ Ye've been promoted to Deck Hand! Finds up to $5!",
                FindLimitTier.TreasureHunter => "🎖️ Arrr! Ye be a Treasure Hunter now! Finds up to $10!",
                FindLimitTier.Captain => "🎖️ All hands, salute the Captain! Finds up to $25!",
                FindLimitTier.PirateLegend => "🎖️ A Pirate Legend walks among us! Finds up to $50!",
                FindLimitTier.KingOfPirates => "👑 BOW YE SCALLYWAGS! The King of Pirates has arrived! UNLIMITED POWER!",
                _ => $"🎖️ Tier up! Now a {newTier.name}!"
            };
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Print limit status
        /// </summary>
        [ContextMenu("Debug: Print Limit Status")]
        public void DebugPrintStatus()
        {
            var tier = GetTierInfo();
            var nextTier = GetNextTier();
            
            Debug.Log("=== Find Limit Status ===");
            Debug.Log($"Current Limit: {GetLimitDisplayString()}");
            Debug.Log($"Tier: {tier.name} ({tier.tier})");
            Debug.Log($"Tier Color: {tier.color}");
            
            if (nextTier != null)
            {
                Debug.Log($"Next Tier: {nextTier.name} at ${nextTier.threshold:F2}");
                Debug.Log($"Progress: {GetProgressToNextTier():P0}");
            }
            else
            {
                Debug.Log("At maximum tier!");
            }
            Debug.Log("=========================");
        }
        
        /// <summary>
        /// Debug: Simulate hiding a $5 coin
        /// </summary>
        [ContextMenu("Debug: Hide $5 Coin")]
        public void DebugHide5()
        {
            UpdateLimitAfterHide(5.00f);
        }
        
        /// <summary>
        /// Debug: Simulate hiding a $25 coin
        /// </summary>
        [ContextMenu("Debug: Hide $25 Coin")]
        public void DebugHide25()
        {
            UpdateLimitAfterHide(25.00f);
        }
        
        #endregion
    }
    
    #region Tier Info Class
    
    /// <summary>
    /// Information about a find limit tier
    /// </summary>
    [Serializable]
    public class TierInfo
    {
        public FindLimitTier tier;
        public float threshold;
        public string name;
        public Color color;
        
        public TierInfo(FindLimitTier tier, float threshold, string name, Color color)
        {
            this.tier = tier;
            this.threshold = threshold;
            this.name = name;
            this.color = color;
        }
    }
    
    #endregion
}
