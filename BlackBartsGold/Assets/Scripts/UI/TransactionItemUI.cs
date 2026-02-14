// ============================================================================
// TransactionItemUI.cs
// Black Bart's Gold - Transaction List Item Component
// Path: Assets/Scripts/UI/TransactionItemUI.cs
// ============================================================================
// UI component for displaying a single transaction in a list.
// Shows icon, description, amount, and time.
// Reference: BUILD-GUIDE.md Prompt 5.3
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// UI component for a single transaction list item.
    /// </summary>
    public class TransactionItemUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField]
        private TMP_Text iconText;
        
        [SerializeField]
        private Image iconImage;
        
        [SerializeField]
        private TMP_Text descriptionText;
        
        [SerializeField]
        private TMP_Text amountText;
        
        [SerializeField]
        private TMP_Text timeText;
        
        [SerializeField]
        private Image statusBadge;
        
        [SerializeField]
        private TMP_Text statusText;
        
        [SerializeField]
        private Image backgroundImage;
        
        [Header("Colors")]
        [SerializeField]
        private Color positiveColor = new Color(0.29f, 0.87f, 0.5f);
        
        [SerializeField]
        private Color negativeColor = new Color(0.94f, 0.27f, 0.27f);
        
        [SerializeField]
        private Color pendingColor = new Color(0.98f, 0.75f, 0.14f);
        
        [SerializeField]
        private Color confirmedColor = new Color(0.29f, 0.87f, 0.5f);
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// The transaction this item displays
        /// </summary>
        public Transaction Transaction { get; private set; }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set transaction data
        /// </summary>
        public void SetData(Transaction tx)
        {
            Transaction = tx;
            
            if (tx == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            gameObject.SetActive(true);
            
            // Set icon
            SetIcon(tx.type);
            
            // Set description
            if (descriptionText != null)
            {
                descriptionText.text = tx.description ?? GetDefaultDescription(tx.type);
            }
            
            // Set amount
            SetAmount(tx.amount, tx.type);
            
            // Set time
            if (timeText != null)
            {
                timeText.text = FormatRelativeTime(tx.timestamp);
            }
            
            // Set status badge
            SetStatus(tx.status);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Set icon based on transaction type
        /// </summary>
        private void SetIcon(TransactionType type)
        {
            string icon = type switch
            {
                TransactionType.Found => "[F]",
                TransactionType.Hidden => "[H]",
                TransactionType.GasConsumed => "[G]",
                TransactionType.Purchased => "[$]",
                TransactionType.Transfer => "[<>]",
                TransactionType.Parked => "[P]",
                TransactionType.Unparked => "[U]",
                TransactionType.Withdrawal => "[W]",
                TransactionType.Bonus => "[B]",
                TransactionType.Refund => "[R]",
                _ => "."
            };
            
            if (iconText != null)
            {
                iconText.text = icon;
            }
        }
        
        /// <summary>
        /// Get default description for type
        /// </summary>
        private string GetDefaultDescription(TransactionType type)
        {
            return type switch
            {
                TransactionType.Found => "Treasure found!",
                TransactionType.Hidden => "Treasure hidden",
                TransactionType.GasConsumed => "Daily gas consumption",
                TransactionType.Purchased => "Gas purchased",
                TransactionType.Transfer => "Transfer",
                TransactionType.Parked => "Coins parked",
                TransactionType.Unparked => "Coins unparked",
                TransactionType.Withdrawal => "Withdrawal",
                TransactionType.Bonus => "Bonus reward",
                TransactionType.Refund => "Refund",
                _ => "Transaction"
            };
        }
        
        /// <summary>
        /// Set amount with appropriate color
        /// </summary>
        private void SetAmount(float amount, TransactionType type)
        {
            if (amountText == null) return;
            
            bool isPositive = IsPositiveTransaction(type);
            
            string prefix = isPositive ? "+" : "-";
            amountText.text = $"{prefix}${Mathf.Abs(amount):F2}";
            amountText.color = isPositive ? positiveColor : negativeColor;
        }
        
        /// <summary>
        /// Check if transaction type is positive (adds to balance)
        /// </summary>
        private bool IsPositiveTransaction(TransactionType type)
        {
            return type == TransactionType.Found ||
                   type == TransactionType.Purchased ||
                   type == TransactionType.Bonus ||
                   type == TransactionType.Unparked ||
                   type == TransactionType.Refund;
        }
        
        /// <summary>
        /// Set status badge
        /// </summary>
        private void SetStatus(TransactionStatus status)
        {
            if (statusBadge == null && statusText == null) return;
            
            bool showBadge = status == TransactionStatus.Pending;
            
            if (statusBadge != null)
            {
                statusBadge.gameObject.SetActive(showBadge);
                statusBadge.color = status == TransactionStatus.Pending ? pendingColor : confirmedColor;
            }
            
            if (statusText != null)
            {
                statusText.gameObject.SetActive(showBadge);
                statusText.text = status switch
                {
                    TransactionStatus.Pending => "PENDING",
                    TransactionStatus.Confirmed => "✓",
                    TransactionStatus.Failed => "FAILED",
                    TransactionStatus.Cancelled => "CANCELLED",
                    _ => ""
                };
            }
        }
        
        /// <summary>
        /// Format timestamp as relative time
        /// </summary>
        private string FormatRelativeTime(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp)) return "";
            
            if (!DateTime.TryParse(timestamp, out DateTime time))
            {
                return timestamp;
            }
            
            TimeSpan elapsed = DateTime.UtcNow - time;
            
            if (elapsed.TotalMinutes < 1)
            {
                return "Just now";
            }
            if (elapsed.TotalMinutes < 60)
            {
                int mins = (int)elapsed.TotalMinutes;
                return $"{mins}m ago";
            }
            if (elapsed.TotalHours < 24)
            {
                int hours = (int)elapsed.TotalHours;
                return $"{hours}h ago";
            }
            if (elapsed.TotalDays < 7)
            {
                int days = (int)elapsed.TotalDays;
                return $"{days}d ago";
            }
            
            return time.ToString("MMM d");
        }
        
        #endregion
    }
}
