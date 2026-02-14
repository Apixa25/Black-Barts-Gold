// ============================================================================
// FindLimitUI.cs
// Black Bart's Gold - Find Limit Display Component
// Path: Assets/Scripts/UI/FindLimitUI.cs
// ============================================================================
// Displays the player's current find limit with tier-based styling.
// Shows the maximum coin value the player can collect.
// Reference: BUILD-GUIDE.md Prompt 5.1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlackBartsGold.Core;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Find limit display showing player's max collectible coin value.
    /// Color-coded by tier with optional tier name display.
    /// </summary>
    public class FindLimitUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Text showing 'Find: $X.XX'")]
        private TMP_Text limitText;
        
        [SerializeField]
        [Tooltip("Text showing tier name (optional)")]
        private TMP_Text tierText;
        
        [SerializeField]
        [Tooltip("Background image for color")]
        private Image backgroundImage;
        
        [SerializeField]
        [Tooltip("Tier icon/badge")]
        private Image tierIcon;
        
        [SerializeField]
        [Tooltip("Container for the display")]
        private RectTransform container;
        
        [Header("Display Format")]
        [SerializeField]
        private string prefix = "Find: ";
        
        [SerializeField]
        private bool showTierName = true;
        
        [SerializeField]
        private bool showTierIcon = true;
        
        [Header("Tier Colors")]
        [SerializeField]
        private Color cabinBoyColor = new Color(0.8f, 0.5f, 0.2f); // Bronze
        
        [SerializeField]
        private Color deckHandColor = new Color(0.75f, 0.75f, 0.75f); // Silver
        
        [SerializeField]
        private Color treasureHunterColor = new Color(1f, 0.84f, 0f); // Gold
        
        [SerializeField]
        private Color captainColor = new Color(0.9f, 0.9f, 0.95f); // Platinum
        
        [SerializeField]
        private Color pirateLegendColor = new Color(0.5f, 0.8f, 1f); // Diamond
        
        [SerializeField]
        private Color kingOfPiratesColor = new Color(1f, 0.4f, 0.7f); // Pink/Special
        
        [Header("Animation")]
        [SerializeField]
        private bool animateOnChange = true;
        
        [SerializeField]
        private float punchScale = 1.2f;
        
        [SerializeField]
        private float animationDuration = 0.3f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Runtime Setup (Code-Only UI)
        
        /// <summary>
        /// Set find limit references at runtime. Called by ARHuntSceneSetup when building UI from code.
        /// </summary>
        public void SetRuntimeReferences(TMP_Text limit, TMP_Text tier, Image bg, Image icon, RectTransform cont)
        {
            limitText = limit;
            tierText = tier;
            backgroundImage = bg;
            tierIcon = icon;
            container = cont;
            if (container != null) originalScale = container.localScale;
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current find limit value
        /// </summary>
        public float CurrentLimit { get; private set; } = 1.00f;
        
        /// <summary>
        /// Current tier
        /// </summary>
        public FindLimitTier CurrentTier { get; private set; } = FindLimitTier.CabinBoy;
        
        #endregion
        
        #region Private Fields
        
        private Vector3 originalScale;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (container != null)
            {
                originalScale = container.localScale;
            }
            else
            {
                originalScale = Vector3.one;
            }
        }
        
        private void Start()
        {
            // Initialize from PlayerData
            if (PlayerData.Exists)
            {
                UpdateFindLimit(PlayerData.Instance.FindLimit);
                PlayerData.Instance.OnFindLimitChanged += OnFindLimitChanged;
            }
        }
        
        private void OnDestroy()
        {
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnFindLimitChanged -= OnFindLimitChanged;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnFindLimitChanged(float newLimit)
        {
            float oldLimit = CurrentLimit;
            UpdateFindLimit(newLimit);
            
            // Animate if increased
            if (animateOnChange && newLimit > oldLimit)
            {
                PlayUpgradeAnimation();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Update find limit display
        /// </summary>
        public void UpdateFindLimit(float limit)
        {
            CurrentLimit = limit;
            CurrentTier = GetTierForLimit(limit);
            
            // Update limit text
            if (limitText != null)
            {
                limitText.text = $"{prefix}${limit:F2}";
            }
            
            // Update tier text
            if (tierText != null && showTierName)
            {
                tierText.text = GetTierName(CurrentTier);
            }
            
            // Update colors
            UpdateColors();
            
            Log($"Find limit updated: ${limit:F2} ({CurrentTier})");
        }
        
        /// <summary>
        /// Set tier directly (for testing)
        /// </summary>
        public void SetTier(FindLimitTier tier)
        {
            float limit = GetLimitForTier(tier);
            UpdateFindLimit(limit);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Get tier for a limit value
        /// </summary>
        private FindLimitTier GetTierForLimit(float limit)
        {
            if (limit >= 100f) return FindLimitTier.KingOfPirates;
            if (limit >= 50f) return FindLimitTier.PirateLegend;
            if (limit >= 25f) return FindLimitTier.Captain;
            if (limit >= 10f) return FindLimitTier.TreasureHunter;
            if (limit >= 5f) return FindLimitTier.DeckHand;
            return FindLimitTier.CabinBoy;
        }
        
        /// <summary>
        /// Get limit for a tier
        /// </summary>
        private float GetLimitForTier(FindLimitTier tier)
        {
            return tier switch
            {
                FindLimitTier.CabinBoy => 1.00f,
                FindLimitTier.DeckHand => 5.00f,
                FindLimitTier.TreasureHunter => 10.00f,
                FindLimitTier.Captain => 25.00f,
                FindLimitTier.PirateLegend => 50.00f,
                FindLimitTier.KingOfPirates => 100.00f,
                _ => 1.00f
            };
        }
        
        /// <summary>
        /// Get tier display name
        /// </summary>
        private string GetTierName(FindLimitTier tier)
        {
            return tier switch
            {
                FindLimitTier.CabinBoy => "Cabin Boy",
                FindLimitTier.DeckHand => "Deck Hand",
                FindLimitTier.TreasureHunter => "Treasure Hunter",
                FindLimitTier.Captain => "Captain",
                FindLimitTier.PirateLegend => "Pirate Legend",
                FindLimitTier.KingOfPirates => "King of Pirates",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Get color for tier
        /// </summary>
        private Color GetColorForTier(FindLimitTier tier)
        {
            return tier switch
            {
                FindLimitTier.CabinBoy => cabinBoyColor,
                FindLimitTier.DeckHand => deckHandColor,
                FindLimitTier.TreasureHunter => treasureHunterColor,
                FindLimitTier.Captain => captainColor,
                FindLimitTier.PirateLegend => pirateLegendColor,
                FindLimitTier.KingOfPirates => kingOfPiratesColor,
                _ => cabinBoyColor
            };
        }
        
        /// <summary>
        /// Update UI colors based on tier
        /// </summary>
        private void UpdateColors()
        {
            Color tierColor = GetColorForTier(CurrentTier);
            
            if (limitText != null)
            {
                limitText.color = tierColor;
            }
            
            if (tierText != null)
            {
                tierText.color = tierColor;
            }
            
            if (tierIcon != null && showTierIcon)
            {
                tierIcon.color = tierColor;
            }
            
            if (backgroundImage != null)
            {
                // Slightly transparent version for background
                Color bgColor = tierColor;
                bgColor.a = 0.2f;
                backgroundImage.color = bgColor;
            }
        }
        
        /// <summary>
        /// Play upgrade animation
        /// </summary>
        private void PlayUpgradeAnimation()
        {
            if (container == null) return;
            
            StartCoroutine(UpgradeAnimationCoroutine());
        }
        
        private System.Collections.IEnumerator UpgradeAnimationCoroutine()
        {
            float timer = 0f;
            float halfDuration = animationDuration / 2f;
            
            // Scale up
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float t = timer / halfDuration;
                float scale = Mathf.Lerp(1f, punchScale, t);
                container.localScale = originalScale * scale;
                yield return null;
            }
            
            // Scale down
            timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float t = timer / halfDuration;
                float scale = Mathf.Lerp(punchScale, 1f, t);
                container.localScale = originalScale * scale;
                yield return null;
            }
            
            container.localScale = originalScale;
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[FindLimitUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Cycle through tiers
        /// </summary>
        [ContextMenu("Debug: Cycle Tiers")]
        public void DebugCycleTiers()
        {
            StartCoroutine(CycleTiersCoroutine());
        }
        
        private System.Collections.IEnumerator CycleTiersCoroutine()
        {
            FindLimitTier[] tiers = {
                FindLimitTier.CabinBoy,
                FindLimitTier.DeckHand,
                FindLimitTier.TreasureHunter,
                FindLimitTier.Captain,
                FindLimitTier.PirateLegend,
                FindLimitTier.KingOfPirates
            };
            
            foreach (var tier in tiers)
            {
                SetTier(tier);
                yield return new WaitForSeconds(1f);
            }
        }
        
        /// <summary>
        /// Debug: Set to Cabin Boy
        /// </summary>
        [ContextMenu("Debug: Set Cabin Boy")]
        public void DebugSetCabinBoy()
        {
            UpdateFindLimit(1.00f);
        }
        
        /// <summary>
        /// Debug: Set to Captain
        /// </summary>
        [ContextMenu("Debug: Set Captain")]
        public void DebugSetCaptain()
        {
            UpdateFindLimit(25.00f);
        }
        
        /// <summary>
        /// Debug: Set to King
        /// </summary>
        [ContextMenu("Debug: Set King of Pirates")]
        public void DebugSetKing()
        {
            UpdateFindLimit(100.00f);
        }
        
        #endregion
    }
}
