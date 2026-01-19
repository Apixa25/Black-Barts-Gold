// ============================================================================
// CollectionService.cs
// Black Bart's Gold - Coin Collection Service
// Path: Assets/Scripts/Economy/CollectionService.cs
// ============================================================================
// Handles the complete coin collection flow - validation, value calculation,
// wallet updates, and feedback coordination.
// Reference: BUILD-GUIDE.md Sprint 7, Prompt 7.3
// ============================================================================

using UnityEngine;
using System;
using System.Threading.Tasks;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Economy
{
    /// <summary>
    /// Collection service singleton.
    /// Manages the complete coin collection process from validation to wallet update.
    /// </summary>
    public class CollectionService : MonoBehaviour
    {
        #region Singleton
        
        private static CollectionService _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static CollectionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CollectionService>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CollectionService");
                        _instance = go.AddComponent<CollectionService>();
                        Debug.Log("[CollectionService] 💎 Created new CollectionService instance");
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
        /// Collection range in meters
        /// </summary>
        public const float COLLECTION_RANGE = 5.0f;
        
        /// <summary>
        /// Pool coin minimum value
        /// </summary>
        public const float POOL_MIN_VALUE = 0.05f;
        
        /// <summary>
        /// Pool coin maximum multiplier
        /// </summary>
        public const float POOL_MAX_MULTIPLIER = 5.0f;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when collection starts
        /// </summary>
        public event Action<Coin> OnCollectionStarted;
        
        /// <summary>
        /// Fired when collection succeeds
        /// </summary>
        public event Action<CollectionResult> OnCollectionSuccess;
        
        /// <summary>
        /// Fired when collection fails
        /// </summary>
        public event Action<CollectionResult> OnCollectionFailed;
        
        /// <summary>
        /// Fired when coin is over limit (locked)
        /// </summary>
        public event Action<Coin, float> OnCoinOverLimit;
        
        /// <summary>
        /// Fired when player has no gas
        /// </summary>
        public event Action OnNoGasCollection;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[CollectionService] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[CollectionService] 💎 CollectionService initialized");
        }
        
        #endregion
        
        #region Pre-Collection Checks
        
        /// <summary>
        /// Check if a coin can be collected
        /// </summary>
        public CollectionCheck CanCollect(Coin coin, float distanceToPlayer = 0f)
        {
            // Check if coin is valid
            if (coin == null)
            {
                return new CollectionCheck(false, CollectionDenialReason.Invalid, "Invalid coin");
            }
            
            // Check if already collected
            if (coin.status == CoinStatus.Collected || coin.status == CoinStatus.Confirmed)
            {
                return new CollectionCheck(false, CollectionDenialReason.AlreadyCollected, "Coin already collected");
            }
            
            // Check gas
            if (GasService.Exists && !GasService.Instance.CanPlay())
            {
                return new CollectionCheck(false, CollectionDenialReason.NoGas, "No fuel! Add gas to continue hunting.");
            }
            
            // Check distance (if provided)
            if (distanceToPlayer > COLLECTION_RANGE)
            {
                return new CollectionCheck(false, CollectionDenialReason.TooFar, 
                    $"Too far away! Get within {COLLECTION_RANGE}m to collect.");
            }
            
            // Check find limit
            if (FindLimitService.Exists && FindLimitService.Instance.IsOverLimit(coin))
            {
                float playerLimit = FindLimitService.Instance.GetCurrentLimit();
                return new CollectionCheck(false, CollectionDenialReason.OverLimit, 
                    $"This treasure be above yer limit, matey! Your limit: ${playerLimit:F2}");
            }
            
            return new CollectionCheck(true, CollectionDenialReason.None, "Ready to collect!");
        }
        
        /// <summary>
        /// Quick check if coin is over player's limit
        /// </summary>
        public bool IsOverLimit(Coin coin)
        {
            if (FindLimitService.Exists)
            {
                return FindLimitService.Instance.IsOverLimit(coin);
            }
            
            // Fallback check
            float playerLimit = PlayerData.Exists ? PlayerData.Instance.FindLimit : 1.00f;
            return coin.GetEffectiveValue() > playerLimit;
        }
        
        #endregion
        
        #region Collection Process
        
        /// <summary>
        /// Collect a coin
        /// </summary>
        public async Task<CollectionResult> CollectCoin(Coin coin, float distanceToPlayer = 0f)
        {
            Debug.Log($"[CollectionService] 💎 Attempting to collect coin: {coin.id}");
            
            OnCollectionStarted?.Invoke(coin);
            
            // Pre-collection validation
            var check = CanCollect(coin, distanceToPlayer);
            if (!check.canCollect)
            {
                Debug.Log($"[CollectionService] ❌ Collection denied: {check.reason}");
                
                var failResult = new CollectionResult
                {
                    success = false,
                    coin = coin,
                    reason = check.reason,
                    message = check.message
                };
                
                // Fire specific events based on reason
                if (check.reason == CollectionDenialReason.OverLimit)
                {
                    OnCoinOverLimit?.Invoke(coin, GetPlayerLimit());
                }
                else if (check.reason == CollectionDenialReason.NoGas)
                {
                    OnNoGasCollection?.Invoke();
                }
                
                OnCollectionFailed?.Invoke(failResult);
                return failResult;
            }
            
            // Determine coin value
            float value = DetermineValue(coin);
            
            // Small delay for animation
            await Task.Delay(100);
            
            // Update coin status
            coin.status = CoinStatus.Collected;
            coin.collectedAt = DateTime.UtcNow.ToString("o");
            coin.collectedBy = GetPlayerId();
            
            // Add to wallet (pending)
            if (WalletService.Exists)
            {
                WalletService.Instance.AddPendingCoins(value, coin.id);
            }
            else if (PlayerData.Exists)
            {
                PlayerData.Instance.AddPendingCoins(value, coin.id);
            }
            
            // Update player stats
            UpdatePlayerStats(value);
            
            // Create result
            var result = new CollectionResult
            {
                success = true,
                coin = coin,
                value = value,
                message = GetCongratulationMessage(value),
                tier = coin.GetTier()
            };
            
            Debug.Log($"[CollectionService] ✅ Coin collected! Value: ${value:F2}");
            
            OnCollectionSuccess?.Invoke(result);
            
            return result;
        }
        
        #endregion
        
        #region Value Calculation
        
        /// <summary>
        /// Determine coin value (for pool coins, calculate dynamically)
        /// </summary>
        private float DetermineValue(Coin coin)
        {
            if (coin.type == CoinType.Fixed)
            {
                return coin.value;
            }
            else // Pool coin - slot machine algorithm
            {
                return CalculatePoolCoinValue(coin);
            }
        }
        
        /// <summary>
        /// Calculate pool coin value using slot machine algorithm
        /// Reference: Docs/dynamic-coin-distribution.md
        /// </summary>
        private float CalculatePoolCoinValue(Coin coin)
        {
            // Base value from pool contribution
            float baseValue = coin.poolContribution > 0 ? coin.poolContribution : coin.value;
            
            // Random multiplier (weighted towards lower values)
            float random = UnityEngine.Random.value;
            float multiplier;
            
            if (random < 0.50f)
            {
                // 50% chance: 0.2x - 0.8x (lower than contribution)
                multiplier = 0.2f + (random * 1.2f);
            }
            else if (random < 0.85f)
            {
                // 35% chance: 0.8x - 1.5x (around contribution)
                multiplier = 0.8f + ((random - 0.5f) * 2f);
            }
            else if (random < 0.98f)
            {
                // 13% chance: 1.5x - 3.0x (bonus!)
                multiplier = 1.5f + ((random - 0.85f) * 10f);
            }
            else
            {
                // 2% chance: 3.0x - 5.0x (jackpot!)
                multiplier = 3.0f + ((random - 0.98f) * 100f);
            }
            
            float calculatedValue = baseValue * multiplier;
            
            // Clamp to min/max
            calculatedValue = Mathf.Max(POOL_MIN_VALUE, calculatedValue);
            calculatedValue = Mathf.Min(baseValue * POOL_MAX_MULTIPLIER, calculatedValue);
            
            // Round to 2 decimal places
            calculatedValue = Mathf.Round(calculatedValue * 100f) / 100f;
            
            Debug.Log($"[CollectionService] Pool coin: base ${baseValue:F2} × {multiplier:F2} = ${calculatedValue:F2}");
            
            return calculatedValue;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get player's current find limit
        /// </summary>
        private float GetPlayerLimit()
        {
            if (FindLimitService.Exists)
            {
                return FindLimitService.Instance.GetCurrentLimit();
            }
            return PlayerData.Exists ? PlayerData.Instance.FindLimit : 1.00f;
        }
        
        /// <summary>
        /// Get current player ID
        /// </summary>
        private string GetPlayerId()
        {
            return PlayerData.Exists ? PlayerData.Instance.CurrentUser?.id : "unknown";
        }
        
        /// <summary>
        /// Update player statistics after collection
        /// </summary>
        private void UpdatePlayerStats(float value)
        {
            if (PlayerData.Exists && PlayerData.Instance.CurrentUser?.stats != null)
            {
                PlayerData.Instance.CurrentUser.stats.RecordFind(value);
            }
        }
        
        /// <summary>
        /// Get congratulation message based on value
        /// </summary>
        public string GetCongratulationMessage(float value)
        {
            if (value >= 50.00f)
            {
                return "LEGENDARY FIND! Ye've struck gold, Captain!";
            }
            else if (value >= 25.00f)
            {
                return "MASSIVE HAUL! The crew will sing of this!";
            }
            else if (value >= 10.00f)
            {
                return "EXCELLENT! A worthy treasure indeed!";
            }
            else if (value >= 5.00f)
            {
                return "GREAT FIND! Yer treasure grows!";
            }
            else if (value >= 1.00f)
            {
                return "Nice find, matey!";
            }
            else
            {
                return "Every coin counts on the high seas!";
            }
        }
        
        /// <summary>
        /// Get over-limit message
        /// </summary>
        public string GetOverLimitMessage(Coin coin)
        {
            float playerLimit = GetPlayerLimit();
            float coinValue = coin.GetEffectiveValue();
            float needed = coinValue;
            
            return $"This treasure be ${coinValue:F2}, but yer limit is only ${playerLimit:F2}!\n\n" +
                   $"Hide a ${needed:F2} coin to unlock finds up to ${needed:F2}!";
        }
        
        /// <summary>
        /// Get hint for unlocking higher finds
        /// </summary>
        public string GetUnlockHint(Coin coin)
        {
            float coinValue = coin.GetEffectiveValue();
            float hideNeeded = coinValue;
            
            return $"Hide ${hideNeeded:F2} to unlock!";
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Test collection with mock coin
        /// </summary>
        [ContextMenu("Debug: Test Collection")]
        public async void DebugTestCollection()
        {
            var testCoin = Coin.CreateTestCoin(CoinType.Fixed, 1.50f);
            var result = await CollectCoin(testCoin, 0f);
            Debug.Log($"[CollectionService] Test result: {result}");
        }
        
        /// <summary>
        /// Debug: Test pool coin value calculation
        /// </summary>
        [ContextMenu("Debug: Test Pool Values (10x)")]
        public void DebugTestPoolValues()
        {
            var testCoin = Coin.CreateTestCoin(CoinType.Pool, 5.00f);
            testCoin.poolContribution = 5.00f;
            
            Debug.Log("=== Pool Coin Value Test ===");
            for (int i = 0; i < 10; i++)
            {
                float value = CalculatePoolCoinValue(testCoin);
                Debug.Log($"Roll {i + 1}: ${value:F2}");
            }
            Debug.Log("============================");
        }
        
        #endregion
    }
    
    #region Collection Types
    
    /// <summary>
    /// Pre-collection check result
    /// </summary>
    public class CollectionCheck
    {
        public bool canCollect;
        public CollectionDenialReason reason;
        public string message;
        
        public CollectionCheck(bool canCollect, CollectionDenialReason reason, string message)
        {
            this.canCollect = canCollect;
            this.reason = reason;
            this.message = message;
        }
    }
    
    /// <summary>
    /// Collection result
    /// </summary>
    public class CollectionResult
    {
        public bool success;
        public Coin coin;
        public float value;
        public CollectionDenialReason reason;
        public string message;
        public CoinTier tier;
        
        public override string ToString()
        {
            if (success)
            {
                return $"SUCCESS: ${value:F2} - {message}";
            }
            else
            {
                return $"FAILED: {reason} - {message}";
            }
        }
    }
    
    /// <summary>
    /// Reasons collection can be denied
    /// </summary>
    public enum CollectionDenialReason
    {
        None,
        Invalid,
        TooFar,
        OverLimit,
        NoGas,
        AlreadyCollected,
        ServerError
    }
    
    #endregion
}
