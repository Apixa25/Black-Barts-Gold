// ============================================================================
// CollectButtonController.cs
// Black Bart's Gold - Collect Button UI Controller
// Path: Assets/Scripts/UI/CollectButtonController.cs
// ============================================================================
// Shows/hides the collect button based on player proximity to target coin.
// Button appears when player is within collection range.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlackBartsGold.AR;
using BlackBartsGold.Location;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
namespace BlackBartsGold.UI
{
    /// <summary>
    /// Controls the visibility and behavior of the collect button.
    /// Shows when player is within collection range of target coin.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CollectButtonController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField]
        [Tooltip("The button component")]
        private Button collectButton;
        
        [SerializeField]
        [Tooltip("Button text to update")]
        private TMP_Text buttonText;
        
        [Header("Messages")]
        [SerializeField]
        private string collectMessage = "COLLECT TREASURE!";
        
        [SerializeField]
        private string lockedMessage = "LOCKED - GET CLOSER";
        
        [SerializeField]
        private string tooFarMessage = "GET CLOSER";
        
        [Header("Colors")]
        [SerializeField]
        private Color normalColor = new Color(0.29f, 0.87f, 0.5f, 1f); // Green
        
        [SerializeField]
        private Color lockedColor = new Color(0.94f, 0.27f, 0.27f, 1f); // Red
        
        [SerializeField]
        private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Distance at which collection is allowed (meters)")]
        private float collectionDistance = 5f;
        
        [SerializeField]
        [Tooltip("Show button when target is set, regardless of distance")]
        private bool alwaysShowWhenHunting = false;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private Image buttonImage;
        private bool isInRange = false;
        private bool isLocked = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (collectButton == null)
            {
                collectButton = GetComponent<Button>();
            }
            
            buttonImage = GetComponent<Image>();
            
            // Start hidden
            gameObject.SetActive(false);
        }
        
        private void Start()
        {
            // Subscribe to events
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet += OnTargetSet;
                CoinManager.Instance.OnTargetCleared += OnTargetCleared;
                CoinManager.Instance.OnTargetCollected += OnTargetCollected;
            }
            
            if (ProximityManager.Instance != null)
            {
                ProximityManager.Instance.OnZoneChanged += OnZoneChanged;
                ProximityManager.Instance.OnEnteredCollectionRange += OnEnteredCollectionRange;
                ProximityManager.Instance.OnExitedCollectionRange += OnExitedCollectionRange;
            }
            
            // Wire up button click
            if (collectButton != null)
            {
                collectButton.onClick.AddListener(OnCollectClicked);
            }
            
            Log("CollectButtonController initialized");
        }
        
        private void OnDestroy()
        {
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet -= OnTargetSet;
                CoinManager.Instance.OnTargetCleared -= OnTargetCleared;
                CoinManager.Instance.OnTargetCollected -= OnTargetCollected;
            }
            
            if (ProximityManager.Instance != null)
            {
                ProximityManager.Instance.OnZoneChanged -= OnZoneChanged;
                ProximityManager.Instance.OnEnteredCollectionRange -= OnEnteredCollectionRange;
                ProximityManager.Instance.OnExitedCollectionRange -= OnExitedCollectionRange;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnTargetSet(Coin coin)
        {
            Log($"Target set: {coin.GetDisplayValue()}");
            isLocked = coin.isLocked;
            
            if (alwaysShowWhenHunting)
            {
                Show();
                UpdateButtonState();
            }
        }
        
        private void OnTargetCleared()
        {
            Log("Target cleared");
            Hide();
        }
        
        private void OnTargetCollected(Coin coin, float value)
        {
            Log($"Target collected: ${value:F2}");
            Hide();
        }
        
        private void OnZoneChanged(ProximityZone oldZone, ProximityZone newZone)
        {
            Log($"Zone changed: {oldZone} → {newZone}");
            
            isInRange = (newZone == ProximityZone.Collectible);
            
            if (CoinManager.Exists && CoinManager.Instance.HasTarget)
            {
                if (isInRange || alwaysShowWhenHunting)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
                UpdateButtonState();
            }
        }
        
        private void OnEnteredCollectionRange(Coin coin)
        {
            Log($"Entered collection range: {coin.GetDisplayValue()}");
            isInRange = true;
            isLocked = coin.isLocked;
            Show();
            UpdateButtonState();
        }
        
        private void OnExitedCollectionRange(Coin coin)
        {
            Log($"Exited collection range");
            isInRange = false;
            
            if (!alwaysShowWhenHunting)
            {
                Hide();
            }
            else
            {
                UpdateButtonState();
            }
        }
        
        #endregion
        
        #region Button Actions
        
        private void OnCollectClicked()
        {
            Log("Collect button clicked");
            
            if (!CoinManager.Exists || !CoinManager.Instance.HasTarget)
            {
                Log("No target to collect");
                return;
            }
            
            if (!isInRange)
            {
                Log("Not in range");
                if (ARHUD.Instance != null)
                {
                    ARHUD.Instance.ShowMessage("Get closer to collect!");
                }
                return;
            }
            
            if (isLocked)
            {
                Log("Coin is locked");
                if (ARHUD.Instance != null)
                {
                    var coin = CoinManager.Instance.TargetCoinData;
                    ARHUD.Instance.ShowLockedPopup(coin.value, PlayerData.Instance?.FindLimit ?? 0);
                }
                return;
            }
            
            // Attempt collection!
            Log("Attempting collection...");
            if (CoinManager.Instance.TargetCoin != null)
            {
                CoinManager.Instance.TargetCoin.TryCollect();
            }
        }
        
        #endregion
        
        #region UI Updates
        
        private void UpdateButtonState()
        {
            if (buttonImage == null) return;
            
            if (isLocked)
            {
                buttonImage.color = lockedColor;
                if (buttonText != null) buttonText.text = lockedMessage;
                if (collectButton != null) collectButton.interactable = false;
            }
            else if (isInRange)
            {
                buttonImage.color = normalColor;
                if (buttonText != null) buttonText.text = collectMessage;
                if (collectButton != null) collectButton.interactable = true;
            }
            else
            {
                buttonImage.color = disabledColor;
                if (buttonText != null) buttonText.text = tooFarMessage;
                if (collectButton != null) collectButton.interactable = false;
            }
        }
        
        #endregion
        
        #region Show/Hide
        
        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                Log("Button shown");
            }
        }
        
        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                Log("Button hidden");
            }
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            DiagnosticLog.Log("Collect", message);
        }
        
        [ContextMenu("Debug: Show Button")]
        public void DebugShow()
        {
            Show();
            isInRange = true;
            UpdateButtonState();
        }
        
        [ContextMenu("Debug: Show Locked")]
        public void DebugShowLocked()
        {
            Show();
            isLocked = true;
            isInRange = true;
            UpdateButtonState();
        }
        
        #endregion
    }
}
