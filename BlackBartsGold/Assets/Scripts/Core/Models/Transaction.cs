// ============================================================================
// Transaction.cs
// Black Bart's Gold - Transaction Data Model
// Path: Assets/Scripts/Core/Models/Transaction.cs
// ============================================================================
// Represents a wallet transaction (find, hide, gas, purchase, etc.)
// Reference: Docs/economy-and-currency.md
// ============================================================================

using System;

namespace BlackBartsGold.Core.Models
{
    /// <summary>
    /// Data model representing a wallet transaction.
    /// Serializable for JSON persistence.
    /// </summary>
    [Serializable]
    public class Transaction
    {
        #region Identity
        
        /// <summary>
        /// Unique transaction identifier
        /// </summary>
        public string id;
        
        /// <summary>
        /// Type of transaction
        /// </summary>
        public TransactionType type;
        
        /// <summary>
        /// Current status
        /// </summary>
        public TransactionStatus status;
        
        #endregion
        
        #region Amount
        
        /// <summary>
        /// Transaction amount (positive = credit, negative = debit)
        /// </summary>
        public float amount;
        
        /// <summary>
        /// Fee charged (if any)
        /// </summary>
        public float fee;
        
        /// <summary>
        /// Net amount after fees
        /// </summary>
        public float NetAmount => amount - fee;
        
        #endregion
        
        #region References
        
        /// <summary>
        /// Related coin ID (for found/hidden transactions)
        /// </summary>
        public string coinId;
        
        /// <summary>
        /// Related user ID (for transfers)
        /// </summary>
        public string relatedUserId;
        
        /// <summary>
        /// Related user display name
        /// </summary>
        public string relatedUserName;
        
        #endregion
        
        #region Metadata
        
        /// <summary>
        /// Human readable description
        /// </summary>
        public string description;
        
        /// <summary>
        /// Timestamp (ISO 8601)
        /// </summary>
        public string timestamp;
        
        /// <summary>
        /// When transaction will be confirmed (for pending)
        /// </summary>
        public string confirmsAt;
        
        /// <summary>
        /// Additional notes/metadata
        /// </summary>
        public string notes;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public Transaction() { }
        
        /// <summary>
        /// Create transaction with basic info
        /// </summary>
        public Transaction(TransactionType type, float amount, string description = "")
        {
            this.id = Guid.NewGuid().ToString();
            this.type = type;
            this.amount = amount;
            this.description = description;
            this.status = TransactionStatus.Confirmed;
            this.timestamp = DateTime.UtcNow.ToString("o");
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get timestamp as DateTime
        /// </summary>
        public DateTime GetTimestamp()
        {
            if (DateTime.TryParse(timestamp, out DateTime dt))
            {
                return dt;
            }
            return DateTime.MinValue;
        }
        
        /// <summary>
        /// Get confirms at as DateTime
        /// </summary>
        public DateTime GetConfirmsAt()
        {
            if (DateTime.TryParse(confirmsAt, out DateTime dt))
            {
                return dt;
            }
            return DateTime.MinValue;
        }
        
        /// <summary>
        /// Get time until confirmation (for pending)
        /// </summary>
        public TimeSpan GetTimeUntilConfirmation()
        {
            if (status != TransactionStatus.Pending)
            {
                return TimeSpan.Zero;
            }
            
            DateTime confirms = GetConfirmsAt();
            if (confirms == DateTime.MinValue)
            {
                // Default: 24 hours from timestamp
                confirms = GetTimestamp().AddHours(24);
            }
            
            TimeSpan remaining = confirms - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        
        /// <summary>
        /// Is this a credit (positive) transaction?
        /// </summary>
        public bool IsCredit()
        {
            return amount > 0;
        }
        
        /// <summary>
        /// Is this a debit (negative) transaction?
        /// </summary>
        public bool IsDebit()
        {
            return amount < 0;
        }
        
        /// <summary>
        /// Get formatted amount string with +/- sign
        /// </summary>
        public string GetFormattedAmount()
        {
            if (amount >= 0)
            {
                return $"+${amount:F2}";
            }
            return $"-${Math.Abs(amount):F2}";
        }
        
        /// <summary>
        /// Get relative time string (e.g., "2 hours ago")
        /// </summary>
        public string GetRelativeTimeString()
        {
            TimeSpan elapsed = DateTime.UtcNow - GetTimestamp();
            
            if (elapsed.TotalMinutes < 1)
                return "Just now";
            if (elapsed.TotalMinutes < 60)
                return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24)
                return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 7)
                return $"{(int)elapsed.TotalDays}d ago";
            if (elapsed.TotalDays < 30)
                return $"{(int)(elapsed.TotalDays / 7)}w ago";
            
            return GetTimestamp().ToString("MMM d");
        }
        
        /// <summary>
        /// Get icon/emoji for transaction type
        /// </summary>
        public string GetIcon()
        {
            return type switch
            {
                TransactionType.Found => "💰",
                TransactionType.Hidden => "🏴‍☠️",
                TransactionType.GasConsumed => "⛽",
                TransactionType.Purchased => "💳",
                TransactionType.Transfer => "↔️",
                TransactionType.Parked => "🅿️",
                TransactionType.Unparked => "🚗",
                TransactionType.Withdrawal => "📤",
                TransactionType.Bonus => "🎁",
                TransactionType.Refund => "↩️",
                _ => "📝"
            };
        }
        
        /// <summary>
        /// Get default description based on type
        /// </summary>
        public string GetDefaultDescription()
        {
            return type switch
            {
                TransactionType.Found => $"Found treasure: {GetFormattedAmount()}",
                TransactionType.Hidden => $"Hid treasure: {GetFormattedAmount()}",
                TransactionType.GasConsumed => $"Daily gas consumed: {GetFormattedAmount()}",
                TransactionType.Purchased => $"Purchased BBG: {GetFormattedAmount()}",
                TransactionType.Transfer => relatedUserName != null 
                    ? $"Transfer {(IsCredit() ? "from" : "to")} {relatedUserName}: {GetFormattedAmount()}"
                    : $"Transfer: {GetFormattedAmount()}",
                TransactionType.Parked => $"Parked coins: {GetFormattedAmount()}",
                TransactionType.Unparked => $"Unparked coins: {GetFormattedAmount()}",
                TransactionType.Withdrawal => $"Withdrawal: {GetFormattedAmount()}",
                TransactionType.Bonus => $"Bonus: {GetFormattedAmount()}",
                TransactionType.Refund => $"Refund: {GetFormattedAmount()}",
                _ => description ?? "Transaction"
            };
        }
        
        /// <summary>
        /// Debug string representation
        /// </summary>
        public override string ToString()
        {
            return $"TX[{id.Substring(0, 8)}]: {type} {GetFormattedAmount()} - {status} - {GetRelativeTimeString()}";
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create a coin found transaction
        /// </summary>
        public static Transaction CreateFoundTransaction(float value, string coinId)
        {
            var tx = new Transaction
            {
                id = Guid.NewGuid().ToString(),
                type = TransactionType.Found,
                amount = value,
                coinId = coinId,
                status = TransactionStatus.Pending,
                timestamp = DateTime.UtcNow.ToString("o"),
                confirmsAt = DateTime.UtcNow.AddHours(24).ToString("o"),
                description = $"Found treasure: +${value:F2}"
            };
            return tx;
        }
        
        /// <summary>
        /// Create a gas consumed transaction
        /// </summary>
        public static Transaction CreateGasTransaction(float amount)
        {
            return new Transaction
            {
                id = Guid.NewGuid().ToString(),
                type = TransactionType.GasConsumed,
                amount = -Math.Abs(amount), // Always negative
                status = TransactionStatus.Confirmed,
                timestamp = DateTime.UtcNow.ToString("o"),
                description = $"Daily gas: -${Math.Abs(amount):F2}"
            };
        }
        
        /// <summary>
        /// Create a purchase transaction
        /// </summary>
        public static Transaction CreatePurchaseTransaction(float amount)
        {
            return new Transaction
            {
                id = Guid.NewGuid().ToString(),
                type = TransactionType.Purchased,
                amount = amount,
                status = TransactionStatus.Confirmed,
                timestamp = DateTime.UtcNow.ToString("o"),
                description = $"Purchased: +${amount:F2} BBG"
            };
        }
        
        /// <summary>
        /// Create a test transaction
        /// </summary>
        public static Transaction CreateTestTransaction(TransactionType type, float amount)
        {
            return new Transaction
            {
                id = Guid.NewGuid().ToString(),
                type = type,
                amount = amount,
                status = TransactionStatus.Confirmed,
                timestamp = DateTime.UtcNow.AddHours(-1).ToString("o"),
                description = $"Test: {type}"
            };
        }
        
        #endregion
    }
}
