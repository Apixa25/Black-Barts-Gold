// ============================================================================
// GasMeterUI.cs
// Black Bart's Gold - Gas Meter UI Component
// Path: Assets/Scripts/UI/GasMeterUI.cs
// ============================================================================
// Displays the player's gas tank level as a vertical gauge. Color changes
// based on remaining days and flashes when critically low.
// Reference: BUILD-GUIDE.md Prompt 5.1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using BlackBartsGold.Core;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Gas meter UI showing remaining fuel days.
    /// Vertical gauge with color-coded fill level.
    /// </summary>
    public class GasMeterUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Fill image for the gauge (Image type: Filled)")]
        private Image fillImage;
        
        [SerializeField]
        [Tooltip("Background image of the gauge")]
        private Image backgroundImage;
        
        [SerializeField]
        [Tooltip("Text showing days remaining")]
        private TMP_Text daysText;
        
        [SerializeField]
        [Tooltip("Gas icon")]
        private Image gasIcon;
        
        [SerializeField]
        [Tooltip("Container for the entire meter")]
        private RectTransform container;
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Maximum days (100% fill)")]
        private float maxDays = 30f;
        
        [SerializeField]
        [Tooltip("Low gas threshold (percentage)")]
        private float lowThreshold = 0.15f; // 15%
        
        [SerializeField]
        [Tooltip("Medium gas threshold (percentage)")]
        private float mediumThreshold = 0.50f; // 50%
        
        [Header("Colors")]
        [SerializeField]
        private Color fullColor = new Color(0.29f, 0.87f, 0.5f); // Green
        
        [SerializeField]
        private Color mediumColor = new Color(0.98f, 0.75f, 0.14f); // Yellow
        
        [SerializeField]
        private Color lowColor = new Color(0.94f, 0.27f, 0.27f); // Red
        
        [SerializeField]
        private Color emptyColor = new Color(0.5f, 0.5f, 0.5f); // Gray
        
        [Header("Animation")]
        [SerializeField]
        private bool flashWhenLow = true;
        
        [SerializeField]
        private float flashSpeed = 2f;
        
        [SerializeField]
        private bool animateFillChange = true;
        
        [SerializeField]
        private float fillAnimationSpeed = 2f;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Runtime Setup (Code-Only UI)
        
        /// <summary>
        /// Set gas meter references at runtime. Called by ARHuntSceneSetup when building UI from code.
        /// </summary>
        public void SetRuntimeReferences(Image fill, Image bg, TMP_Text days, Image icon, RectTransform cont)
        {
            fillImage = fill;
            backgroundImage = bg;
            daysText = days;
            gasIcon = icon;
            container = cont;
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current gas level (0-1)
        /// </summary>
        public float FillLevel { get; private set; } = 1f;
        
        /// <summary>
        /// Current days remaining
        /// </summary>
        public float DaysRemaining { get; private set; } = 30f;
        
        /// <summary>
        /// Current gas status
        /// </summary>
        public GasStatus Status { get; private set; } = GasStatus.Full;
        
        /// <summary>
        /// Is gas critically low?
        /// </summary>
        public bool IsLow => Status == GasStatus.Low || Status == GasStatus.Empty;
        
        #endregion
        
        #region Private Fields
        
        private float targetFillLevel = 1f;
        private bool isFlashing = false;
        private Coroutine flashCoroutine;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Initialize from PlayerData
            if (PlayerData.Exists)
            {
                UpdateGas(PlayerData.Instance.GasDays);
                PlayerData.Instance.OnGasChanged += UpdateGas;
            }
        }
        
        private void OnDestroy()
        {
            if (PlayerData.Exists)
            {
                PlayerData.Instance.OnGasChanged -= UpdateGas;
            }
        }
        
        private void Update()
        {
            // Animate fill level
            if (animateFillChange && Mathf.Abs(FillLevel - targetFillLevel) > 0.001f)
            {
                FillLevel = Mathf.Lerp(FillLevel, targetFillLevel, Time.deltaTime * fillAnimationSpeed);
                ApplyFillLevel();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Update gas display with days remaining
        /// </summary>
        public void UpdateGas(float daysRemaining)
        {
            DaysRemaining = Mathf.Max(0, daysRemaining);
            targetFillLevel = Mathf.Clamp01(DaysRemaining / maxDays);
            
            // Update status
            UpdateStatus();
            
            // Update text
            UpdateText();
            
            // Update color
            UpdateColor();
            
            // Handle flashing
            UpdateFlashing();
            
            // If not animating, apply immediately
            if (!animateFillChange)
            {
                FillLevel = targetFillLevel;
                ApplyFillLevel();
            }
            
            Log($"Gas updated: {daysRemaining:F1} days ({targetFillLevel * 100:F0}%)");
        }
        
        /// <summary>
        /// Set gas level directly (0-1)
        /// </summary>
        public void SetFillLevel(float level)
        {
            UpdateGas(level * maxDays);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Apply fill level to UI
        /// </summary>
        private void ApplyFillLevel()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = FillLevel;
            }
        }
        
        /// <summary>
        /// Update gas status
        /// </summary>
        private void UpdateStatus()
        {
            float percentage = targetFillLevel;
            
            if (percentage <= 0)
            {
                Status = GasStatus.Empty;
            }
            else if (percentage < lowThreshold)
            {
                Status = GasStatus.Low;
            }
            else if (percentage < mediumThreshold)
            {
                Status = GasStatus.Normal;
            }
            else
            {
                Status = GasStatus.Full;
            }
        }
        
        /// <summary>
        /// Update days text
        /// </summary>
        private void UpdateText()
        {
            if (daysText == null) return;
            
            if (DaysRemaining <= 0)
            {
                daysText.text = "EMPTY";
            }
            else if (DaysRemaining < 1)
            {
                // Show hours when less than 1 day
                float hours = DaysRemaining * 24;
                daysText.text = $"{hours:F0}h";
            }
            else
            {
                daysText.text = $"{DaysRemaining:F0}d";
            }
        }
        
        /// <summary>
        /// Update fill color based on status
        /// </summary>
        private void UpdateColor()
        {
            if (fillImage == null) return;
            
            Color targetColor = Status switch
            {
                GasStatus.Full => fullColor,
                GasStatus.Normal => mediumColor,
                GasStatus.Low => lowColor,
                GasStatus.Empty => emptyColor,
                _ => fullColor
            };
            
            fillImage.color = targetColor;
            
            // Update icon color too
            if (gasIcon != null)
            {
                gasIcon.color = targetColor;
            }
        }
        
        /// <summary>
        /// Handle low gas flashing
        /// </summary>
        private void UpdateFlashing()
        {
            bool shouldFlash = flashWhenLow && IsLow && DaysRemaining > 0;
            
            if (shouldFlash && !isFlashing)
            {
                StartFlashing();
            }
            else if (!shouldFlash && isFlashing)
            {
                StopFlashing();
            }
        }
        
        /// <summary>
        /// Start flashing animation
        /// </summary>
        private void StartFlashing()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashCoroutine());
            isFlashing = true;
        }
        
        /// <summary>
        /// Stop flashing animation
        /// </summary>
        private void StopFlashing()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            isFlashing = false;
            
            // Reset alpha
            if (fillImage != null)
            {
                Color c = fillImage.color;
                c.a = 1f;
                fillImage.color = c;
            }
        }
        
        /// <summary>
        /// Flash animation coroutine
        /// </summary>
        private IEnumerator FlashCoroutine()
        {
            while (true)
            {
                float alpha = (Mathf.Sin(Time.time * flashSpeed * Mathf.PI * 2) + 1f) / 2f;
                alpha = Mathf.Lerp(0.3f, 1f, alpha);
                
                if (fillImage != null)
                {
                    Color c = fillImage.color;
                    c.a = alpha;
                    fillImage.color = c;
                }
                
                if (gasIcon != null)
                {
                    Color c = gasIcon.color;
                    c.a = alpha;
                    gasIcon.color = c;
                }
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[GasMeterUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Set to full
        /// </summary>
        [ContextMenu("Debug: Set Full")]
        public void DebugSetFull()
        {
            UpdateGas(30f);
        }
        
        /// <summary>
        /// Debug: Set to medium
        /// </summary>
        [ContextMenu("Debug: Set Medium")]
        public void DebugSetMedium()
        {
            UpdateGas(10f);
        }
        
        /// <summary>
        /// Debug: Set to low
        /// </summary>
        [ContextMenu("Debug: Set Low")]
        public void DebugSetLow()
        {
            UpdateGas(3f);
        }
        
        /// <summary>
        /// Debug: Set to empty
        /// </summary>
        [ContextMenu("Debug: Set Empty")]
        public void DebugSetEmpty()
        {
            UpdateGas(0f);
        }
        
        /// <summary>
        /// Debug: Animate drain
        /// </summary>
        [ContextMenu("Debug: Animate Drain")]
        public void DebugAnimateDrain()
        {
            StartCoroutine(AnimateDrainCoroutine());
        }
        
        private IEnumerator AnimateDrainCoroutine()
        {
            for (float days = 30f; days >= 0; days -= 1f)
            {
                UpdateGas(days);
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        #endregion
    }
}
