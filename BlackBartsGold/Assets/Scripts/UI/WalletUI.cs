// ============================================================================
// WalletUI.cs
// Black Bart's Gold - Wallet Screen Controller
// Path: Assets/Scripts/UI/WalletUI.cs
// ============================================================================
// Controls the wallet screen. Displays balance breakdown, transaction
// history, and provides park/unpark functionality.
// Reference: BUILD-GUIDE.md Prompt 5.3
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Wallet screen controller.
    /// Displays balance breakdown and transaction history.
    /// </summary>
    public class WalletUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Balance Display")]
        [SerializeField]
        private TMP_Text totalBalanceText;
        
        [SerializeField]
        private TMP_Text gasTankText;
        
        [SerializeField]
        private TMP_Text gasDaysText;
        
        [SerializeField]
        private Image gasTankFill;
        
        [SerializeField]
        private TMP_Text parkedText;
        
        [SerializeField]
        private TMP_Text pendingText;
        
        [SerializeField]
        private TMP_Text pendingCountdownText;
        
        [Header("Action Buttons")]
        [SerializeField]
        private Button parkButton;
        
        [SerializeField]
        private Button unparkButton;
        
        [SerializeField]
        private Button addGasButton;
        
        [SerializeField]
        private Button backButton;
        
        [Header("Transaction List")]
        [SerializeField]
        private Transform transactionContainer;
        
        [SerializeField]
        private GameObject transactionItemPrefab;
        
        [SerializeField]
        private TMP_Text noTransactionsText;
        
        [Header("Park Modal")]
        [SerializeField]
        private GameObject parkModal;
        
        [SerializeField]
        private TMP_InputField parkAmountInput;
        
        [SerializeField]
        private TMP_Text parkAvailableText;
        
        [SerializeField]
        private Button parkConfirmButton;
        
        [SerializeField]
        private Button parkCancelButton;
        
        [Header("Unpark Modal")]
        [SerializeField]
        private GameObject unparkModal;
        
        [SerializeField]
        private TMP_InputField unparkAmountInput;
        
        [SerializeField]
        private TMP_Text unparkAvailableText;
        
        [SerializeField]
        private Button unparkConfirmButton;
        
        [SerializeField]
        private Button unparkCancelButton;
        
        [Header("Colors")]
        [SerializeField]
        private Color positiveColor = new Color(0.29f, 0.87f, 0.5f);
        
        [SerializeField]
        private Color negativeColor = new Color(0.94f, 0.27f, 0.27f);
        
        [SerializeField]
        private Color pendingColor = new Color(0.98f, 0.75f, 0.14f);
        
        [SerializeField]
        private Color goldColor = new Color(1f, 0.84f, 0f);
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private List<GameObject> transactionItems = new List<GameObject>();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            Debug.Log($"[WalletUI] 🪙 Start - EventSystem.current={es?.name ?? "null"}");
            SetupButtons();
            HideModals();
            RefreshUI();
            Debug.Log($"[WalletUI] Start complete | backBtn={backButton != null} interactable={backButton?.interactable}");
        }
            
            // Subscribe to wallet changes
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnWalletUpdated += OnWalletUpdated;
            }
        }
        
        private void OnDestroy()
        {
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnWalletUpdated -= OnWalletUpdated;
            }
        }
        
        private void OnEnable()
        {
            Debug.Log($"[WalletUI] OnEnable - Wallet active | EventSystem.current={UnityEngine.EventSystems.EventSystem.current?.name ?? "null"}");
            RefreshUI();
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Resolve button references by name if not assigned in Inspector (e.g. after MCP-created buttons).
        /// </summary>
        private void ResolveButtonReferences()
        {
            if (backButton != null && parkButton != null && unparkButton != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (backButton == null) backButton = canvas.transform.Find("BackButton")?.GetComponent<Button>();
                if (parkButton == null) parkButton = canvas.transform.Find("ParkButton")?.GetComponent<Button>();
                if (unparkButton == null) unparkButton = canvas.transform.Find("UnparkButton")?.GetComponent<Button>();
            }
            if (backButton == null) backButton = GameObject.Find("BackButton")?.GetComponent<Button>();
            if (parkButton == null) parkButton = GameObject.Find("ParkButton")?.GetComponent<Button>();
            if (unparkButton == null) unparkButton = GameObject.Find("UnparkButton")?.GetComponent<Button>();
        }

        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupButtons()
        {
            ResolveButtonReferences();
            if (parkButton != null)
                parkButton.onClick.AddListener(ShowParkModal);
            
            if (unparkButton != null)
                unparkButton.onClick.AddListener(ShowUnparkModal);
            
            if (addGasButton != null)
                addGasButton.onClick.AddListener(OnAddGasClicked);
            
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            
            // Park modal
            if (parkConfirmButton != null)
                parkConfirmButton.onClick.AddListener(OnParkConfirm);
            
            if (parkCancelButton != null)
                parkCancelButton.onClick.AddListener(HideParkModal);
            
            // Unpark modal
            if (unparkConfirmButton != null)
                unparkConfirmButton.onClick.AddListener(OnUnparkConfirm);
            
            if (unparkCancelButton != null)
                unparkCancelButton.onClick.AddListener(HideUnparkModal);
            
            Debug.Log("[WalletUI] SetupButtons done - back=" + (backButton != null) + " park=" + (parkButton != null) + " unpark=" + (unparkButton != null));
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnWalletUpdated(Wallet wallet)
        {
            RefreshUI();
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// Refresh all UI elements
        /// </summary>
        public void RefreshUI()
        {
            if (!PlayerData.Exists) return;
            
            var wallet = PlayerData.Instance.Wallet;
            if (wallet == null) return;
            
            // Update balance displays
            UpdateBalanceDisplay(wallet);
            
            // Update transaction list
            RefreshTransactions(wallet);
            
            Log("Wallet UI refreshed");
        }
        
        /// <summary>
        /// Update balance display
        /// </summary>
        private void UpdateBalanceDisplay(Wallet wallet)
        {
            // Total balance
            if (totalBalanceText != null)
            {
                totalBalanceText.text = $"${wallet.total:F2} BBG";
                totalBalanceText.color = goldColor;
            }
            
            // Gas tank
            if (gasTankText != null)
            {
                gasTankText.text = $"${wallet.gasTank:F2}";
            }
            
            if (gasDaysText != null)
            {
                gasDaysText.text = $"{wallet.gasRemainingDays:F1} days";
            }
            
            if (gasTankFill != null)
            {
                gasTankFill.fillAmount = Mathf.Clamp01(wallet.gasRemainingDays / 30f);
            }
            
            // Parked
            if (parkedText != null)
            {
                parkedText.text = $"${wallet.parked:F2}";
            }
            
            // Pending
            if (pendingText != null)
            {
                pendingText.text = $"${wallet.pending:F2}";
                pendingText.color = wallet.pending > 0 ? pendingColor : Color.white;
            }
            
            if (pendingCountdownText != null)
            {
                if (wallet.pending > 0 && wallet.pendingConfirmationTime != null)
                {
                    // Calculate time remaining
                    if (DateTime.TryParse(wallet.pendingConfirmationTime, out DateTime confirmTime))
                    {
                        TimeSpan remaining = confirmTime - DateTime.UtcNow;
                        if (remaining.TotalHours > 0)
                        {
                            pendingCountdownText.text = $"Confirms in {remaining.Hours}h {remaining.Minutes}m";
                        }
                        else
                        {
                            pendingCountdownText.text = "Confirming soon...";
                        }
                    }
                }
                else
                {
                    pendingCountdownText.text = "";
                }
            }
            
            // Update button states
            UpdateButtonStates(wallet);
        }
        
        /// <summary>
        /// Update button interactability
        /// </summary>
        private void UpdateButtonStates(Wallet wallet)
        {
            // Can only park if there are found coins in gas tank
            if (parkButton != null)
            {
                parkButton.interactable = wallet.foundInGasTank > 0;
            }
            
            // Can only unpark if there are parked coins
            if (unparkButton != null)
            {
                unparkButton.interactable = wallet.parked > 0;
            }
        }
        
        /// <summary>
        /// Refresh transaction list
        /// </summary>
        private void RefreshTransactions(Wallet wallet)
        {
            // Clear existing items
            foreach (var item in transactionItems)
            {
                Destroy(item);
            }
            transactionItems.Clear();
            
            // Get transactions
            var transactions = wallet.recentTransactions;
            
            if (transactions == null || transactions.Count == 0)
            {
                if (noTransactionsText != null)
                {
                    noTransactionsText.gameObject.SetActive(true);
                }
                return;
            }
            
            if (noTransactionsText != null)
            {
                noTransactionsText.gameObject.SetActive(false);
            }
            
            // Create transaction items
            foreach (var tx in transactions)
            {
                CreateTransactionItem(tx);
            }
        }
        
        /// <summary>
        /// Create a transaction list item
        /// </summary>
        private void CreateTransactionItem(Transaction tx)
        {
            if (transactionItemPrefab == null || transactionContainer == null) return;
            
            GameObject item = Instantiate(transactionItemPrefab, transactionContainer);
            transactionItems.Add(item);
            
            // Setup the item
            var itemUI = item.GetComponent<TransactionItemUI>();
            if (itemUI != null)
            {
                itemUI.SetData(tx);
            }
            else
            {
                // Fallback: try to find text components directly
                SetupTransactionItemFallback(item, tx);
            }
        }
        
        /// <summary>
        /// Fallback setup for transaction item without TransactionItemUI
        /// </summary>
        private void SetupTransactionItemFallback(GameObject item, Transaction tx)
        {
            // Find text components
            var texts = item.GetComponentsInChildren<TMP_Text>();
            
            if (texts.Length >= 3)
            {
                // Icon/Type
                texts[0].text = GetTransactionIcon(tx.type);
                
                // Description
                texts[1].text = tx.description ?? GetTransactionDescription(tx.type);
                
                // Amount
                bool isPositive = tx.type == TransactionType.Found || 
                                  tx.type == TransactionType.Purchased ||
                                  tx.type == TransactionType.Bonus ||
                                  tx.type == TransactionType.Unparked;
                
                texts[2].text = (isPositive ? "+" : "-") + $"${Mathf.Abs(tx.amount):F2}";
                texts[2].color = isPositive ? positiveColor : negativeColor;
            }
        }
        
        /// <summary>
        /// Get icon for transaction type
        /// </summary>
        private string GetTransactionIcon(TransactionType type)
        {
            return type switch
            {
                TransactionType.Found => "🪙",
                TransactionType.Hidden => "📍",
                TransactionType.GasConsumed => "⛽",
                TransactionType.Purchased => "💳",
                TransactionType.Transfer => "↔️",
                TransactionType.Parked => "🅿️",
                TransactionType.Unparked => "🚗",
                TransactionType.Withdrawal => "📤",
                TransactionType.Bonus => "🎁",
                TransactionType.Refund => "↩️",
                _ => "•"
            };
        }
        
        /// <summary>
        /// Get description for transaction type
        /// </summary>
        private string GetTransactionDescription(TransactionType type)
        {
            return type switch
            {
                TransactionType.Found => "Coin found",
                TransactionType.Hidden => "Coin hidden",
                TransactionType.GasConsumed => "Daily gas",
                TransactionType.Purchased => "Gas purchased",
                TransactionType.Transfer => "Transfer",
                TransactionType.Parked => "Coins parked",
                TransactionType.Unparked => "Coins unparked",
                TransactionType.Withdrawal => "Withdrawal",
                TransactionType.Bonus => "Bonus",
                TransactionType.Refund => "Refund",
                _ => "Transaction"
            };
        }
        
        #endregion
        
        #region Park/Unpark
        
        /// <summary>
        /// Show park modal
        /// </summary>
        public void ShowParkModal()
        {
            if (parkModal == null) return;
            
            parkModal.SetActive(true);
            
            if (parkAmountInput != null)
            {
                parkAmountInput.text = "";
            }
            
            if (parkAvailableText != null && PlayerData.Exists)
            {
                float available = PlayerData.Instance.Wallet?.foundInGasTank ?? 0;
                parkAvailableText.text = $"Available: ${available:F2}";
            }
            
            Log("Park modal shown");
        }
        
        /// <summary>
        /// Hide park modal
        /// </summary>
        public void HideParkModal()
        {
            if (parkModal != null)
            {
                parkModal.SetActive(false);
            }
        }
        
        /// <summary>
        /// Show unpark modal
        /// </summary>
        public void ShowUnparkModal()
        {
            if (unparkModal == null) return;
            
            unparkModal.SetActive(true);
            
            if (unparkAmountInput != null)
            {
                unparkAmountInput.text = "";
            }
            
            if (unparkAvailableText != null && PlayerData.Exists)
            {
                float available = PlayerData.Instance.Wallet?.parked ?? 0;
                unparkAvailableText.text = $"Available: ${available:F2}";
            }
            
            Log("Unpark modal shown");
        }
        
        /// <summary>
        /// Hide unpark modal
        /// </summary>
        public void HideUnparkModal()
        {
            if (unparkModal != null)
            {
                unparkModal.SetActive(false);
            }
        }
        
        /// <summary>
        /// Hide all modals
        /// </summary>
        private void HideModals()
        {
            HideParkModal();
            HideUnparkModal();
        }
        
        /// <summary>
        /// Handle park confirm
        /// </summary>
        private void OnParkConfirm()
        {
            if (parkAmountInput == null || !PlayerData.Exists) return;
            
            if (float.TryParse(parkAmountInput.text, out float amount))
            {
                if (PlayerData.Instance.ParkCoins(amount))
                {
                    Log($"Parked ${amount:F2}");
                    HideParkModal();
                    RefreshUI();
                }
                else
                {
                    Log("Park failed - insufficient funds");
                }
            }
        }
        
        /// <summary>
        /// Handle unpark confirm
        /// </summary>
        private void OnUnparkConfirm()
        {
            if (unparkAmountInput == null || !PlayerData.Exists) return;
            
            if (float.TryParse(unparkAmountInput.text, out float amount))
            {
                if (PlayerData.Instance.UnparkCoins(amount))
                {
                    Log($"Unparked ${amount:F2}");
                    HideUnparkModal();
                    RefreshUI();
                }
                else
                {
                    Log("Unpark failed - insufficient funds");
                }
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        /// <summary>
        /// Handle Add Gas button
        /// </summary>
        private void OnAddGasClicked()
        {
            Log("Add Gas clicked");
            // TODO: Implement purchase flow
            // For now, just add test gas
            if (PlayerData.Exists)
            {
                PlayerData.Instance.AddGas(10f, CoinSource.Purchased);
                RefreshUI();
            }
        }
        
        /// <summary>
        /// Handle Back button
        /// </summary>
        private void OnBackClicked()
        {
            Debug.Log("[WalletUI] 🔙 BACK BUTTON CLICKED - loading MainMenu");
            Log("Back clicked");
            SceneLoader.LoadScene(SceneNames.MainMenu);
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[WalletUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Add test transaction
        /// </summary>
        [ContextMenu("Debug: Add Test Transaction")]
        public void DebugAddTestTransaction()
        {
            if (PlayerData.Exists && PlayerData.Instance.Wallet != null)
            {
                var tx = Transaction.CreateFoundTransaction(5.00f, "test-coin");
                PlayerData.Instance.Wallet.recentTransactions.Insert(0, tx);
                RefreshUI();
            }
        }
        
        /// <summary>
        /// Debug: Print wallet state
        /// </summary>
        [ContextMenu("Debug: Print Wallet State")]
        public void DebugPrintWalletState()
        {
            if (PlayerData.Exists && PlayerData.Instance.Wallet != null)
            {
                Debug.Log(PlayerData.Instance.Wallet.ToString());
            }
        }
        
        #endregion
    }
}
