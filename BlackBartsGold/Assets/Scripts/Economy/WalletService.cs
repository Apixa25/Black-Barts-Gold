// ============================================================================
// WalletService.cs
// Black Bart's Gold - Wallet Service
// Path: Assets/Scripts/Economy/WalletService.cs
// ============================================================================
// Service layer for wallet operations - balance management, park/unpark,
// transaction history. Works with PlayerData and Wallet model.
// Reference: BUILD-GUIDE.md Sprint 7, Prompt 7.1
// ============================================================================

using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Economy
{
    /// <summary>
    /// Wallet service singleton.
    /// Manages BBG balance operations, park/unpark, and transaction history.
    /// MVP: Uses local storage; later syncs with backend.
    /// </summary>
    public class WalletService : MonoBehaviour
    {
        #region Singleton
        
        private static WalletService _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static WalletService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<WalletService>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("WalletService");
                        _instance = go.AddComponent<WalletService>();
                        Debug.Log("[WalletService] 💰 Created new WalletService instance");
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
        /// Daily gas rate (BBG consumed per day)
        /// </summary>
        public const float DAILY_GAS_RATE = 0.33f;
        
        /// <summary>
        /// Days in a full tank ($10 worth)
        /// </summary>
        public const int FULL_TANK_DAYS = 30;
        
        /// <summary>
        /// Full tank value in BBG
        /// </summary>
        public const float FULL_TANK_VALUE = 10.00f;
        
        /// <summary>
        /// Pending confirmation time (hours)
        /// </summary>
        public const int PENDING_HOURS = 24;
        
        /// <summary>
        /// Simulated API delay
        /// </summary>
        private const int API_DELAY_MS = 300;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when balance changes
        /// </summary>
        public event Action<Wallet> OnBalanceChanged;
        
        /// <summary>
        /// Fired when gas changes
        /// </summary>
        public event Action<float> OnGasChanged;
        
        /// <summary>
        /// Fired when a transaction is added
        /// </summary>
        public event Action<Transaction> OnTransactionAdded;
        
        /// <summary>
        /// Fired when coins are parked
        /// </summary>
        public event Action<float> OnCoinsParked;
        
        /// <summary>
        /// Fired when coins are unparked
        /// </summary>
        public event Action<float> OnCoinsUnparked;
        
        /// <summary>
        /// Fired when pending coins are confirmed
        /// </summary>
        public event Action<float> OnPendingConfirmed;
        
        /// <summary>
        /// Fired on wallet error
        /// </summary>
        public event Action<string> OnWalletError;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[WalletService] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[WalletService] 💰 WalletService initialized");
        }
        
        private void Start()
        {
            // Subscribe to PlayerData events
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnWalletUpdated += HandleWalletUpdated;
            }
        }
        
        private void OnDestroy()
        {
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnWalletUpdated -= HandleWalletUpdated;
            }
        }
        
        #endregion
        
        #region Balance Methods
        
        /// <summary>
        /// Get current wallet balance
        /// </summary>
        public async Task<Wallet> GetBalance()
        {
            // Simulate API call
            await Task.Delay(API_DELAY_MS);
            
            if (!PlayerData.Exists || PlayerData.Instance.Wallet == null)
            {
                return Wallet.CreateEmptyWallet();
            }
            
            return PlayerData.Instance.Wallet;
        }
        
        /// <summary>
        /// Get balance synchronously (for UI updates)
        /// </summary>
        public Wallet GetBalanceSync()
        {
            if (!PlayerData.Exists || PlayerData.Instance.Wallet == null)
            {
                return Wallet.CreateEmptyWallet();
            }
            
            return PlayerData.Instance.Wallet;
        }
        
        /// <summary>
        /// Get total balance
        /// </summary>
        public float GetTotalBalance()
        {
            return GetBalanceSync().total;
        }
        
        /// <summary>
        /// Get gas tank balance
        /// </summary>
        public float GetGasTankBalance()
        {
            return GetBalanceSync().gasTank;
        }
        
        /// <summary>
        /// Get parked balance
        /// </summary>
        public float GetParkedBalance()
        {
            return GetBalanceSync().parked;
        }
        
        /// <summary>
        /// Get pending balance
        /// </summary>
        public float GetPendingBalance()
        {
            return GetBalanceSync().pending;
        }
        
        /// <summary>
        /// Get parkable balance (found coins in gas tank)
        /// </summary>
        public float GetParkableBalance()
        {
            return GetBalanceSync().GetParkableBalance();
        }
        
        #endregion
        
        #region Transaction Methods
        
        /// <summary>
        /// Get transaction history
        /// </summary>
        public async Task<List<Transaction>> GetTransactions(int limit = 50, int offset = 0)
        {
            // Simulate API call
            await Task.Delay(API_DELAY_MS);
            
            var wallet = GetBalanceSync();
            
            if (wallet.recentTransactions == null)
            {
                return new List<Transaction>();
            }
            
            // Apply pagination
            int start = Math.Min(offset, wallet.recentTransactions.Count);
            int count = Math.Min(limit, wallet.recentTransactions.Count - start);
            
            return wallet.recentTransactions.GetRange(start, count);
        }
        
        /// <summary>
        /// Get transactions synchronously
        /// </summary>
        public List<Transaction> GetTransactionsSync(int limit = 50)
        {
            var wallet = GetBalanceSync();
            
            if (wallet.recentTransactions == null)
            {
                return new List<Transaction>();
            }
            
            int count = Math.Min(limit, wallet.recentTransactions.Count);
            return wallet.recentTransactions.GetRange(0, count);
        }
        
        /// <summary>
        /// Add a transaction and notify listeners
        /// </summary>
        public void AddTransaction(Transaction tx)
        {
            var wallet = GetBalanceSync();
            wallet.AddTransaction(tx);
            
            // Save changes
            if (PlayerData.Exists)
            {
                PlayerData.Instance.SaveData();
            }
            
            OnTransactionAdded?.Invoke(tx);
        }
        
        #endregion
        
        #region Park/Unpark Methods
        
        /// <summary>
        /// Park coins (move from gas tank to parked)
        /// Only found coins can be parked!
        /// </summary>
        public async Task<WalletOperationResult> ParkCoins(float amount)
        {
            Debug.Log($"[WalletService] 🅿️ Parking ${amount:F2}...");
            
            // Validate
            if (amount <= 0)
            {
                return new WalletOperationResult(false, "Invalid amount");
            }
            
            var wallet = GetBalanceSync();
            
            if (amount > wallet.GetParkableBalance())
            {
                return new WalletOperationResult(false, "Cannot park purchased coins - only found coins can be parked");
            }
            
            // Simulate API call
            await Task.Delay(API_DELAY_MS);
            
            // Perform park
            if (wallet.Park(amount))
            {
                // Save changes
                if (PlayerData.Exists)
                {
                    PlayerData.Instance.SaveData();
                }
                
                Debug.Log($"[WalletService] ✅ Parked ${amount:F2}");
                
                OnCoinsParked?.Invoke(amount);
                OnBalanceChanged?.Invoke(wallet);
                OnGasChanged?.Invoke(wallet.gasRemainingDays);
                
                return new WalletOperationResult(true, "Coins parked successfully");
            }
            
            return new WalletOperationResult(false, "Failed to park coins");
        }
        
        /// <summary>
        /// Unpark coins (move from parked to gas tank)
        /// Charges one day's gas fee
        /// </summary>
        public async Task<WalletOperationResult> UnparkCoins(float amount)
        {
            Debug.Log($"[WalletService] 🚗 Unparking ${amount:F2}...");
            
            // Validate
            if (amount <= 0)
            {
                return new WalletOperationResult(false, "Invalid amount");
            }
            
            var wallet = GetBalanceSync();
            
            if (amount > wallet.parked)
            {
                return new WalletOperationResult(false, "Not enough parked coins");
            }
            
            if (amount <= DAILY_GAS_RATE)
            {
                return new WalletOperationResult(false, $"Minimum unpark amount is ${DAILY_GAS_RATE:F2} (covers fee)");
            }
            
            // Simulate API call
            await Task.Delay(API_DELAY_MS);
            
            // Perform unpark
            if (wallet.Unpark(amount))
            {
                // Save changes
                if (PlayerData.Exists)
                {
                    PlayerData.Instance.SaveData();
                }
                
                float netAmount = amount - DAILY_GAS_RATE;
                Debug.Log($"[WalletService] ✅ Unparked ${amount:F2} (net: ${netAmount:F2} after ${DAILY_GAS_RATE:F2} fee)");
                
                OnCoinsUnparked?.Invoke(netAmount);
                OnBalanceChanged?.Invoke(wallet);
                OnGasChanged?.Invoke(wallet.gasRemainingDays);
                
                return new WalletOperationResult(true, $"Coins unparked successfully (fee: ${DAILY_GAS_RATE:F2})");
            }
            
            return new WalletOperationResult(false, "Failed to unpark coins");
        }
        
        #endregion
        
        #region Pending Confirmation
        
        /// <summary>
        /// Add coins to pending (after finding a coin)
        /// </summary>
        public void AddPendingCoins(float amount, string coinId)
        {
            Debug.Log($"[WalletService] ⏳ Adding ${amount:F2} to pending...");
            
            if (PlayerData.Exists)
            {
                PlayerData.Instance.AddPendingCoins(amount, coinId);
            }
            else
            {
                var wallet = GetBalanceSync();
                wallet.AddToPending(amount, coinId);
            }
            
            OnBalanceChanged?.Invoke(GetBalanceSync());
        }
        
        /// <summary>
        /// Confirm pending coins (after 24 hour delay)
        /// </summary>
        public async Task ConfirmPendingCoins()
        {
            Debug.Log("[WalletService] ✅ Checking for pending coins to confirm...");
            
            var wallet = GetBalanceSync();
            var pendingTx = wallet.GetPendingTransactions();
            float totalConfirmed = 0f;
            
            foreach (var tx in pendingTx)
            {
                // Check if 24 hours have passed
                if (DateTime.TryParse(tx.timestamp, out DateTime txTime))
                {
                    if ((DateTime.UtcNow - txTime).TotalHours >= PENDING_HOURS)
                    {
                        // Confirm this transaction
                        tx.status = TransactionStatus.Confirmed;
                        totalConfirmed += tx.amount;
                        
                        Debug.Log($"[WalletService] ✅ Confirmed: ${tx.amount:F2}");
                    }
                }
            }
            
            if (totalConfirmed > 0)
            {
                // Move from pending to gas tank
                wallet.ConfirmPending(totalConfirmed);
                
                // Save changes
                if (PlayerData.Exists)
                {
                    PlayerData.Instance.SaveData();
                }
                
                OnPendingConfirmed?.Invoke(totalConfirmed);
                OnBalanceChanged?.Invoke(wallet);
                OnGasChanged?.Invoke(wallet.gasRemainingDays);
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Get time until next pending confirmation
        /// </summary>
        public TimeSpan? GetTimeUntilNextConfirmation()
        {
            var wallet = GetBalanceSync();
            var pendingTx = wallet.GetPendingTransactions();
            
            if (pendingTx.Count == 0) return null;
            
            TimeSpan? shortest = null;
            
            foreach (var tx in pendingTx)
            {
                if (DateTime.TryParse(tx.timestamp, out DateTime txTime))
                {
                    DateTime confirmTime = txTime.AddHours(PENDING_HOURS);
                    TimeSpan remaining = confirmTime - DateTime.UtcNow;
                    
                    if (remaining > TimeSpan.Zero)
                    {
                        if (!shortest.HasValue || remaining < shortest.Value)
                        {
                            shortest = remaining;
                        }
                    }
                }
            }
            
            return shortest;
        }
        
        #endregion
        
        #region Purchase Methods (Stubs)
        
        /// <summary>
        /// Add gas from purchase (stub for MVP)
        /// </summary>
        public async Task<WalletOperationResult> PurchaseGas(float amount)
        {
            Debug.Log($"[WalletService] 💳 Purchasing ${amount:F2} gas...");
            
            // Simulate purchase process
            await Task.Delay(API_DELAY_MS * 3);
            
            // For MVP, just add to wallet
            if (PlayerData.Exists)
            {
                PlayerData.Instance.AddGas(amount, CoinSource.Purchased);
            }
            else
            {
                var wallet = GetBalanceSync();
                wallet.AddToGasTank(amount, CoinSource.Purchased);
            }
            
            var currentWallet = GetBalanceSync();
            OnBalanceChanged?.Invoke(currentWallet);
            OnGasChanged?.Invoke(currentWallet.gasRemainingDays);
            
            Debug.Log($"[WalletService] ✅ Purchased ${amount:F2} gas");
            
            return new WalletOperationResult(true, $"Added ${amount:F2} to gas tank");
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Can player currently play? (has gas)
        /// </summary>
        public bool CanPlay()
        {
            return GetBalanceSync().CanPlay();
        }
        
        /// <summary>
        /// Get gas remaining in days
        /// </summary>
        public float GetGasDaysRemaining()
        {
            return GetBalanceSync().gasRemainingDays;
        }
        
        /// <summary>
        /// Get gas percentage (0-1)
        /// </summary>
        public float GetGasPercentage()
        {
            return GetBalanceSync().GetGasPercentage();
        }
        
        /// <summary>
        /// Format balance for display
        /// </summary>
        public string FormatBalance(float amount)
        {
            return $"${amount:F2}";
        }
        
        /// <summary>
        /// Format balance with BBG suffix
        /// </summary>
        public string FormatBalanceBBG(float amount)
        {
            return $"${amount:F2} BBG";
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleWalletUpdated(Wallet wallet)
        {
            OnBalanceChanged?.Invoke(wallet);
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Print wallet state
        /// </summary>
        [ContextMenu("Debug: Print Wallet")]
        public void DebugPrintWallet()
        {
            var wallet = GetBalanceSync();
            Debug.Log("=== Wallet State ===");
            Debug.Log($"Total: {FormatBalance(wallet.total)}");
            Debug.Log($"Gas Tank: {FormatBalance(wallet.gasTank)} ({wallet.GetGasDaysRemaining()} days)");
            Debug.Log($"Parked: {FormatBalance(wallet.parked)}");
            Debug.Log($"Pending: {FormatBalance(wallet.pending)}");
            Debug.Log($"Parkable: {FormatBalance(wallet.GetParkableBalance())}");
            Debug.Log($"Can Play: {CanPlay()}");
            Debug.Log($"Transactions: {wallet.recentTransactions?.Count ?? 0}");
            Debug.Log("====================");
        }
        
        /// <summary>
        /// Debug: Add test balance
        /// </summary>
        [ContextMenu("Debug: Add $10 Gas")]
        public async void DebugAddGas()
        {
            await PurchaseGas(10.00f);
        }
        
        #endregion
    }
    
    #region Operation Result
    
    /// <summary>
    /// Result of a wallet operation
    /// </summary>
    public class WalletOperationResult
    {
        public bool success;
        public string message;
        public float amount;
        public float fee;
        
        public WalletOperationResult(bool success, string message, float amount = 0, float fee = 0)
        {
            this.success = success;
            this.message = message;
            this.amount = amount;
            this.fee = fee;
        }
    }
    
    #endregion
}
