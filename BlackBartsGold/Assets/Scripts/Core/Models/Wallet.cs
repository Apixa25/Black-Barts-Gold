// ============================================================================
// Wallet.cs
// Black Bart's Gold - Wallet Data Model
// Path: Assets/Scripts/Core/Models/Wallet.cs
// ============================================================================
// Represents a player's wallet with balance breakdown and transaction history.
// Reference: Docs/economy-and-currency.md
// ============================================================================

using System;
using System.Collections.Generic;

namespace BlackBartsGold.Core.Models
{
    /// <summary>
    /// Data model representing a player's wallet.
    /// 
    /// Balance breakdown:
    /// - Gas Tank: Active balance consumed for daily play (~$0.33/day)
    /// - Parked: Protected balance not subject to gas consumption
    /// - Pending: Recent finds waiting for confirmation (0-24 hours)
    /// 
    /// Reference: Docs/economy-and-currency.md
    /// </summary>
    [Serializable]
    public class Wallet
    {
        #region Balance
        
        /// <summary>
        /// Total balance (gasTank + parked + pending)
        /// </summary>
        public float total;
        
        /// <summary>
        /// Gas tank - active balance used for daily gas consumption
        /// Includes both purchased and found coins
        /// </summary>
        public float gasTank;
        
        /// <summary>
        /// Parked coins - protected from gas consumption
        /// Only found coins can be parked (not purchased)
        /// Note: Unparking charges a $0.33 (one day's gas) fee
        /// </summary>
        public float parked;
        
        /// <summary>
        /// Pending balance - collected but not yet confirmed
        /// Becomes confirmed after 24 hours
        /// </summary>
        public float pending;
        
        #endregion
        
        #region Coin Sources (for parking rules)
        
        /// <summary>
        /// Amount of gas tank balance from purchases (cannot be parked)
        /// </summary>
        public float purchasedBalance;
        
        /// <summary>
        /// Amount of gas tank balance from finds (can be parked)
        /// </summary>
        public float foundBalance;
        
        #endregion
        
        #region Gas Tracking
        
        /// <summary>
        /// Gas remaining in days
        /// Calculated from gasTank / $0.33
        /// </summary>
        public float gasRemainingDays;
        
        /// <summary>
        /// Last gas consumption timestamp
        /// </summary>
        public string lastGasChargeAt;
        
        /// <summary>
        /// Daily gas consumption rate (BBG)
        /// Default: $0.33 per day (~$10 for 30 days)
        /// </summary>
        public float dailyGasRate = 0.33f;
        
        #endregion
        
        #region Transaction History
        
        /// <summary>
        /// Recent transactions (limited to last N for memory)
        /// </summary>
        public List<Transaction> recentTransactions;
        
        /// <summary>
        /// Maximum transactions to keep in memory
        /// </summary>
        private const int MAX_RECENT_TRANSACTIONS = 100;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public Wallet()
        {
            recentTransactions = new List<Transaction>();
        }
        
        #endregion
        
        #region Balance Methods
        
        /// <summary>
        /// Calculate and update the total balance
        /// </summary>
        public void RecalculateTotal()
        {
            total = gasTank + parked + pending;
        }
        
        /// <summary>
        /// Add coins to gas tank (from purchase or found)
        /// </summary>
        public void AddToGasTank(float amount, CoinSource source)
        {
            gasTank += amount;
            
            if (source == CoinSource.Purchased)
            {
                purchasedBalance += amount;
            }
            else if (source == CoinSource.Found)
            {
                foundBalance += amount;
            }
            
            RecalculateTotal();
            UpdateGasDays();
        }
        
        /// <summary>
        /// Add coins to pending (after collecting a coin)
        /// </summary>
        public void AddToPending(float amount, string coinId)
        {
            pending += amount;
            RecalculateTotal();
            
            // Create pending transaction
            AddTransaction(new Transaction
            {
                id = Guid.NewGuid().ToString(),
                type = TransactionType.Found,
                amount = amount,
                coinId = coinId,
                status = TransactionStatus.Pending,
                timestamp = DateTime.UtcNow.ToString("o"),
                description = $"Found coin: ${amount:F2}"
            });
        }
        
        /// <summary>
        /// Confirm pending balance (after 24 hour delay)
        /// </summary>
        public void ConfirmPending(float amount)
        {
            if (amount > pending)
            {
                amount = pending;
            }
            
            pending -= amount;
            foundBalance += amount;
            gasTank += amount;
            RecalculateTotal();
            UpdateGasDays();
        }
        
        /// <summary>
        /// Park coins (move from gas tank to parked)
        /// Only found coins can be parked!
        /// </summary>
        public bool Park(float amount)
        {
            // Validate: can only park found coins
            if (amount > foundBalance)
            {
                return false; // Can't park purchased coins
            }
            
            if (amount > gasTank)
            {
                return false; // Not enough in gas tank
            }
            
            gasTank -= amount;
            foundBalance -= amount;
            parked += amount;
            RecalculateTotal();
            UpdateGasDays();
            
            AddTransaction(new Transaction
            {
                id = Guid.NewGuid().ToString(),
                type = TransactionType.Parked,
                amount = amount,
                status = TransactionStatus.Confirmed,
                timestamp = DateTime.UtcNow.ToString("o"),
                description = $"Parked ${amount:F2}"
            });
            
            return true;
        }
        
        /// <summary>
        /// Unpark coins (move from parked to gas tank)
        /// Charges one day's gas fee ($0.33)
        /// </summary>
        public bool Unpark(float amount)
        {
            if (amount > parked)
            {
                return false; // Not enough parked
            }
            
            // Charge unparking fee (one day's gas)
            float fee = dailyGasRate;
            float netAmount = amount - fee;
            
            if (netAmount <= 0)
            {
                return false; // Amount too small to cover fee
            }
            
            parked -= amount;
            gasTank += netAmount;
            foundBalance += netAmount; // Goes back to found category
            RecalculateTotal();
            UpdateGasDays();
            
            AddTransaction(new Transaction
            {
                id = Guid.NewGuid().ToString(),
                type = TransactionType.Unparked,
                amount = netAmount,
                fee = fee,
                status = TransactionStatus.Confirmed,
                timestamp = DateTime.UtcNow.ToString("o"),
                description = $"Unparked ${amount:F2} (fee: ${fee:F2})"
            });
            
            return true;
        }
        
        /// <summary>
        /// Consume daily gas
        /// Returns remaining gas days
        /// </summary>
        public float ConsumeGas()
        {
            float consumed = Math.Min(gasTank, dailyGasRate);
            
            gasTank -= consumed;
            
            // Deduct from appropriate source
            if (purchasedBalance >= consumed)
            {
                purchasedBalance -= consumed;
            }
            else
            {
                float remaining = consumed - purchasedBalance;
                purchasedBalance = 0;
                foundBalance = Math.Max(0, foundBalance - remaining);
            }
            
            RecalculateTotal();
            UpdateGasDays();
            lastGasChargeAt = DateTime.UtcNow.ToString("o");
            
            if (consumed > 0)
            {
                AddTransaction(new Transaction
                {
                    id = Guid.NewGuid().ToString(),
                    type = TransactionType.GasConsumed,
                    amount = -consumed,
                    status = TransactionStatus.Confirmed,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    description = $"Daily gas: -${consumed:F2}"
                });
            }
            
            return gasRemainingDays;
        }
        
        /// <summary>
        /// Update gas remaining days calculation
        /// </summary>
        private void UpdateGasDays()
        {
            gasRemainingDays = dailyGasRate > 0 ? gasTank / dailyGasRate : 0;
        }
        
        #endregion
        
        #region Gas Status
        
        /// <summary>
        /// Get integer days remaining
        /// </summary>
        public int GetGasDaysRemaining()
        {
            return (int)Math.Floor(gasRemainingDays);
        }
        
        /// <summary>
        /// Get gas status enum
        /// </summary>
        public GasStatus GetGasStatus()
        {
            if (gasTank <= 0) return GasStatus.Empty;
            
            // Full tank = 30 days = $10
            float fullTank = dailyGasRate * 30;
            float percentage = gasTank / fullTank;
            
            if (percentage < 0.15f) return GasStatus.Low;
            if (percentage < 0.50f) return GasStatus.Normal;
            return GasStatus.Full;
        }
        
        /// <summary>
        /// Get gas percentage (0-1)
        /// </summary>
        public float GetGasPercentage()
        {
            float fullTank = dailyGasRate * 30;
            return Math.Min(1f, gasTank / fullTank);
        }
        
        /// <summary>
        /// Can the player play?
        /// </summary>
        public bool CanPlay()
        {
            return gasTank > 0;
        }
        
        #endregion
        
        #region Transaction Management
        
        /// <summary>
        /// Add a transaction to history
        /// </summary>
        public void AddTransaction(Transaction tx)
        {
            recentTransactions.Insert(0, tx); // Add to front
            
            // Trim if over limit
            while (recentTransactions.Count > MAX_RECENT_TRANSACTIONS)
            {
                recentTransactions.RemoveAt(recentTransactions.Count - 1);
            }
        }
        
        /// <summary>
        /// Get transactions of a specific type
        /// </summary>
        public List<Transaction> GetTransactionsByType(TransactionType type)
        {
            return recentTransactions.FindAll(t => t.type == type);
        }
        
        /// <summary>
        /// Get pending transactions
        /// </summary>
        public List<Transaction> GetPendingTransactions()
        {
            return recentTransactions.FindAll(t => t.status == TransactionStatus.Pending);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get parkable balance (found coins in gas tank)
        /// </summary>
        public float GetParkableBalance()
        {
            return foundBalance;
        }
        
        /// <summary>
        /// Debug string representation
        /// </summary>
        public override string ToString()
        {
            return $"Wallet: ${total:F2} total (Gas: ${gasTank:F2}, Parked: ${parked:F2}, Pending: ${pending:F2}) - {GetGasDaysRemaining()} days";
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create a test wallet for development
        /// </summary>
        public static Wallet CreateTestWallet()
        {
            return new Wallet
            {
                gasTank = 5.00f,
                parked = 10.00f,
                pending = 2.50f,
                purchasedBalance = 3.00f,
                foundBalance = 2.00f,
                total = 17.50f,
                gasRemainingDays = 15.15f, // 5.00 / 0.33
                dailyGasRate = 0.33f,
                lastGasChargeAt = DateTime.UtcNow.AddDays(-1).ToString("o"),
                recentTransactions = new List<Transaction>
                {
                    Transaction.CreateTestTransaction(TransactionType.Found, 2.50f),
                    Transaction.CreateTestTransaction(TransactionType.GasConsumed, -0.33f),
                    Transaction.CreateTestTransaction(TransactionType.Found, 1.00f)
                }
            };
        }
        
        /// <summary>
        /// Create an empty wallet for new users
        /// </summary>
        public static Wallet CreateEmptyWallet()
        {
            return new Wallet
            {
                gasTank = 0f,
                parked = 0f,
                pending = 0f,
                purchasedBalance = 0f,
                foundBalance = 0f,
                total = 0f,
                gasRemainingDays = 0f,
                dailyGasRate = 0.33f,
                recentTransactions = new List<Transaction>()
            };
        }
        
        #endregion
    }
}
