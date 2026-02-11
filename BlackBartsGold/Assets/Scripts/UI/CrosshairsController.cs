// ============================================================================
// CrosshairsController.cs
// Black Bart's Gold - Crosshairs UI Controller
// Path: Assets/Scripts/UI/CrosshairsController.cs
// ============================================================================
// Controls the crosshairs UI element that helps players target coins.
// Changes color and animation based on targeting state.
// Reference: BUILD-GUIDE.md Prompt 5.1 (AR HUD)
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using BlackBartsGold.AR;
using BlackBartsGold.Core;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Controls crosshairs appearance based on targeting state.
    /// Placed at screen center, changes color when hovering/targeting coins.
    /// </summary>
    public class CrosshairsController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("References")]
        [SerializeField]
        [Tooltip("Main crosshairs image")]
        private Image crosshairsImage;
        
        [SerializeField]
        [Tooltip("Inner dot/circle (optional)")]
        private Image innerDot;
        
        [SerializeField]
        [Tooltip("Outer ring for animations (optional)")]
        private Image outerRing;
        
        [SerializeField]
        [Tooltip("Lock icon overlay for locked coins")]
        private GameObject lockOverlay;
        
        [SerializeField]
        [Tooltip("Optional circle around crosshairs showing how big the coin will look at collection range. Assign a child Image with a circle/ring sprite.")]
        private Image collectionSizeCircle;
        
        [SerializeField]
        [Tooltip("Show the collection-size circle when targeting a coin")]
        private bool showCollectionSizeCircle = true;
        
        [Header("Colors")]
        [SerializeField]
        private Color normalColor = Color.white;
        
        [SerializeField]
        private Color hoveringColor = new Color(1f, 0.84f, 0f); // Gold
        
        [SerializeField]
        private Color inRangeColor = new Color(0.29f, 0.87f, 0.5f); // Green
        
        [SerializeField]
        private Color lockedColor = new Color(0.94f, 0.27f, 0.27f); // Red
        
        [SerializeField]
        private Color noTrackingColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray
        
        [Header("Animation")]
        [SerializeField]
        [Tooltip("Pulse animation when hovering coin")]
        private bool enablePulseAnimation = true;
        
        [SerializeField]
        private float pulseSpeed = 2f;
        
        [SerializeField]
        private float pulseMinScale = 0.9f;
        
        [SerializeField]
        private float pulseMaxScale = 1.1f;
        
        [SerializeField]
        [Tooltip("Rotation animation for outer ring")]
        private bool enableRotation = true;
        
        [SerializeField]
        private float rotationSpeed = 30f;
        
        [Header("Scale")]
        [SerializeField]
        private float normalScale = 1f;
        
        [SerializeField]
        private float targetingScale = 1.2f;
        
        [SerializeField]
        private float scaleTransitionSpeed = 5f;
        
        #endregion
        
        #region State
        
        /// <summary>
        /// Current crosshairs state
        /// </summary>
        public CrosshairsState CurrentState { get; private set; } = CrosshairsState.Normal;
        
        /// <summary>
        /// Target color for smooth transition
        /// </summary>
        private Color targetColor;
        
        /// <summary>
        /// Target scale for smooth transition
        /// </summary>
        private float targetScale;
        
        /// <summary>
        /// Is a locked coin being targeted?
        /// </summary>
        private bool isTargetingLocked = false;
        
        /// <summary>
        /// Is coin in collection range?
        /// </summary>
        private bool isInRange = false;
        
        /// <summary>
        /// Whether we've computed and set the collection circle size (once per session)
        /// </summary>
        private bool collectionCircleSizeSet;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Initialize
            targetColor = normalColor;
            targetScale = normalScale;
            
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(false);
            }
            
            if (collectionSizeCircle != null)
            {
                collectionSizeCircle.gameObject.SetActive(false);
            }
            
            // Subscribe to events
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void Update()
        {
            // Smooth color transition
            UpdateColor();
            
            // Smooth scale transition
            UpdateScale();
            
            // Animations
            UpdateAnimations();
            
            // Collection-size circle: show when targeting, hide otherwise; size once from settings
            UpdateCollectionSizeCircle();
        }
        
        #endregion
        
        #region Event Subscriptions
        
        private void SubscribeToEvents()
        {
            // AR Raycast events
            if (ARRaycastController.Instance != null)
            {
                ARRaycastController.Instance.OnCoinHovered += OnCoinHovered;
                ARRaycastController.Instance.OnCoinUnhovered += OnCoinUnhovered;
            }
            
            // AR Session events
            if (ARSessionManager.Instance != null)
            {
                ARSessionManager.Instance.OnStateChanged += OnARStateChanged;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (ARRaycastController.Instance != null)
            {
                ARRaycastController.Instance.OnCoinHovered -= OnCoinHovered;
                ARRaycastController.Instance.OnCoinUnhovered -= OnCoinUnhovered;
            }
            
            if (ARSessionManager.Instance != null)
            {
                ARSessionManager.Instance.OnStateChanged -= OnARStateChanged;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnCoinHovered(GameObject coin)
        {
            // Check if coin is locked or in range
            // TODO: Get actual coin data from CoinController component
            // For now, we'll use placeholder logic
            
            isTargetingLocked = CheckIfCoinLocked(coin);
            isInRange = CheckIfCoinInRange(coin);
            
            if (isTargetingLocked)
            {
                SetState(CrosshairsState.TargetingLocked);
            }
            else if (isInRange)
            {
                SetState(CrosshairsState.InRange);
            }
            else
            {
                SetState(CrosshairsState.Hovering);
            }
        }
        
        private void OnCoinUnhovered()
        {
            isTargetingLocked = false;
            isInRange = false;
            SetState(CrosshairsState.Normal);
        }
        
        private void OnARStateChanged(UnityEngine.XR.ARFoundation.ARSessionState state)
        {
            if (state != UnityEngine.XR.ARFoundation.ARSessionState.SessionTracking)
            {
                SetState(CrosshairsState.NoTracking);
            }
            else if (CurrentState == CrosshairsState.NoTracking)
            {
                SetState(CrosshairsState.Normal);
            }
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Set crosshairs state
        /// </summary>
        public void SetState(CrosshairsState newState)
        {
            if (CurrentState == newState) return;
            
            CurrentState = newState;
            
            // Update targets based on state
            switch (newState)
            {
                case CrosshairsState.Normal:
                    targetColor = normalColor;
                    targetScale = normalScale;
                    SetLockOverlay(false);
                    break;
                    
                case CrosshairsState.Hovering:
                    targetColor = hoveringColor;
                    targetScale = targetingScale;
                    SetLockOverlay(false);
                    break;
                    
                case CrosshairsState.InRange:
                    targetColor = inRangeColor;
                    targetScale = targetingScale;
                    SetLockOverlay(false);
                    break;
                    
                case CrosshairsState.TargetingLocked:
                    targetColor = lockedColor;
                    targetScale = targetingScale;
                    SetLockOverlay(true);
                    break;
                    
                case CrosshairsState.NoTracking:
                    targetColor = noTrackingColor;
                    targetScale = normalScale;
                    SetLockOverlay(false);
                    break;
            }
        }
        
        /// <summary>
        /// Force immediate state update (no transition)
        /// </summary>
        public void SetStateImmediate(CrosshairsState newState)
        {
            SetState(newState);
            
            // Apply immediately
            if (crosshairsImage != null)
            {
                crosshairsImage.color = targetColor;
            }
            
            transform.localScale = Vector3.one * targetScale;
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateColor()
        {
            if (crosshairsImage == null) return;
            
            // Smooth color transition
            crosshairsImage.color = Color.Lerp(crosshairsImage.color, targetColor, Time.deltaTime * 10f);
            
            // Apply to inner dot if present
            if (innerDot != null)
            {
                innerDot.color = crosshairsImage.color;
            }
        }
        
        private void UpdateScale()
        {
            // Smooth scale transition
            float currentScale = transform.localScale.x;
            float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleTransitionSpeed);
            transform.localScale = Vector3.one * newScale;
        }
        
        private void UpdateAnimations()
        {
            // Pulse animation when targeting
            if (enablePulseAnimation && (CurrentState == CrosshairsState.Hovering || 
                                         CurrentState == CrosshairsState.InRange))
            {
                float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
                    (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
                
                if (innerDot != null)
                {
                    innerDot.transform.localScale = Vector3.one * pulse;
                }
            }
            else if (innerDot != null)
            {
                // Reset inner dot scale
                innerDot.transform.localScale = Vector3.one;
            }
            
            // Rotation animation for outer ring
            if (enableRotation && outerRing != null)
            {
                float rotationAmount = rotationSpeed * Time.deltaTime;
                
                // Faster rotation when targeting
                if (CurrentState == CrosshairsState.Hovering || CurrentState == CrosshairsState.InRange)
                {
                    rotationAmount *= 2f;
                }
                
                outerRing.transform.Rotate(0, 0, rotationAmount);
            }
        }
        
        private void SetLockOverlay(bool show)
        {
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(show);
            }
        }
        
        /// <summary>
        /// Show/hide and size the collection-size circle so it matches how big the coin will look at collection range.
        /// </summary>
        private void UpdateCollectionSizeCircle()
        {
            if (collectionSizeCircle == null || !showCollectionSizeCircle) return;
            
            bool targetingCoin = CurrentState == CrosshairsState.Hovering
                || CurrentState == CrosshairsState.InRange
                || CurrentState == CrosshairsState.TargetingLocked;
            
            if (targetingCoin)
            {
                if (!collectionCircleSizeSet)
                {
                    ComputeAndSetCollectionCircleSize();
                }
                if (!collectionSizeCircle.gameObject.activeSelf)
                {
                    collectionSizeCircle.gameObject.SetActive(true);
                }
            }
            else
            {
                if (collectionSizeCircle.gameObject.activeSelf)
                {
                    collectionSizeCircle.gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Size the collection circle to match the coin's apparent size when at collection distance (largest scale).
        /// Uses CoinDisplaySettings so it stays in sync with ARCoinRenderer.
        /// </summary>
        private void ComputeAndSetCollectionCircleSize()
        {
            if (collectionSizeCircle == null) return;
            
            CoinDisplaySettings settings = CoinDisplaySettings.Default;
            Camera cam = Camera.main;
            if (cam == null) return;
            
            float viewingDist = Mathf.Max(0.1f, settings.materializeViewingDistance);
            // World radius of coin at max scale (baseScale * scaleAtNear * scaleAtCollectionMultiplier; coin mesh ~1 unit)
            float worldRadius = settings.baseScale * settings.scaleAtNear * settings.scaleAtCollectionMultiplier * 0.5f;
            float angleRad = 2f * Mathf.Atan(worldRadius / viewingDist);
            float screenRadiusPixels = (angleRad * Mathf.Rad2Deg / cam.fieldOfView) * (Screen.height * 0.5f);
            
            Canvas canvas = collectionSizeCircle.GetComponentInParent<Canvas>();
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            float diameterUnits = 2f * screenRadiusPixels / scaleFactor;
            
            collectionSizeCircle.rectTransform.sizeDelta = new Vector2(diameterUnits, diameterUnits);
            collectionCircleSizeSet = true;
        }
        
        #endregion
        
        #region Coin Checks
        
        /// <summary>
        /// Check if coin is locked (above player's find limit)
        /// TODO: Replace with actual CoinController check
        /// </summary>
        private bool CheckIfCoinLocked(GameObject coin)
        {
            // Try to get CoinController component
            // var coinController = coin.GetComponent<CoinController>();
            // if (coinController != null)
            // {
            //     return coinController.IsLocked;
            // }
            
            // Placeholder: check for "Locked" tag
            return coin.CompareTag("LockedCoin");
        }
        
        /// <summary>
        /// Check if coin is within collection range
        /// TODO: Replace with actual distance check
        /// </summary>
        private bool CheckIfCoinInRange(GameObject coin)
        {
            // Try to get CoinController component
            // var coinController = coin.GetComponent<CoinController>();
            // if (coinController != null)
            // {
            //     return coinController.IsInRange;
            // }
            
            // Placeholder: check distance to camera
            if (Camera.main != null)
            {
                float distance = Vector3.Distance(Camera.main.transform.position, coin.transform.position);
                return distance <= 5f; // 5 meters is collection range
            }
            
            return false;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Update coin range status externally
        /// </summary>
        public void UpdateCoinStatus(bool isLocked, bool isInRange)
        {
            this.isTargetingLocked = isLocked;
            this.isInRange = isInRange;
            
            if (CurrentState != CrosshairsState.Normal && CurrentState != CrosshairsState.NoTracking)
            {
                if (isLocked)
                {
                    SetState(CrosshairsState.TargetingLocked);
                }
                else if (isInRange)
                {
                    SetState(CrosshairsState.InRange);
                }
                else
                {
                    SetState(CrosshairsState.Hovering);
                }
            }
        }
        
        /// <summary>
        /// Flash the crosshairs (for feedback)
        /// </summary>
        public void Flash(Color flashColor, float duration = 0.2f)
        {
            StartCoroutine(FlashCoroutine(flashColor, duration));
        }
        
        private System.Collections.IEnumerator FlashCoroutine(Color flashColor, float duration)
        {
            Color originalTarget = targetColor;
            
            if (crosshairsImage != null)
            {
                crosshairsImage.color = flashColor;
            }
            
            yield return new WaitForSeconds(duration);
            
            targetColor = originalTarget;
        }
        
        #endregion
    }
    
    #region Enums
    
    /// <summary>
    /// Crosshairs visual state
    /// </summary>
    public enum CrosshairsState
    {
        /// <summary>Normal state - white crosshairs</summary>
        Normal,
        
        /// <summary>Hovering over coin - gold</summary>
        Hovering,
        
        /// <summary>Coin in collection range - green</summary>
        InRange,
        
        /// <summary>Targeting locked coin - red</summary>
        TargetingLocked,
        
        /// <summary>AR not tracking - gray</summary>
        NoTracking
    }
    
    #endregion
}
