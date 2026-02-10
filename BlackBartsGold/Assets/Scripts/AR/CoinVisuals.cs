// ============================================================================
// CoinVisuals.cs
// Black Bart's Gold - Coin Visual Effects Component
// Path: Assets/Scripts/AR/CoinVisuals.cs
// ============================================================================
// Handles coin visual effects: materials, glow, sparkles, and state transitions.
// Attach to coin prefab alongside CoinController.
// Reference: BUILD-GUIDE.md Prompt 3.1
// ============================================================================

using UnityEngine;
using System.Collections;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Manages visual appearance of a coin.
    /// Works with CoinController to update visuals based on state.
    /// </summary>
    public class CoinVisuals : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Mesh References")]
        [SerializeField]
        [Tooltip("The main coin mesh renderer")]
        private MeshRenderer coinRenderer;
        
        [SerializeField]
        [Tooltip("Optional secondary mesh (e.g., inner ring)")]
        private MeshRenderer innerRenderer;
        
        [Header("Tier Materials")]
        [SerializeField]
        private Material bronzeMaterial;
        
        [SerializeField]
        private Material silverMaterial;
        
        [SerializeField]
        private Material goldMaterial;
        
        [SerializeField]
        private Material platinumMaterial;
        
        [SerializeField]
        private Material diamondMaterial;
        
        [SerializeField]
        private Material unknownMaterial; // For pool coins
        
        [SerializeField]
        private Material lockedMaterial;
        
        [Header("Glow Settings")]
        [SerializeField]
        private Light pointLight;
        
        [SerializeField]
        private float glowIntensityNormal = 1f;
        
        [SerializeField]
        private float glowIntensityHover = 2f;
        
        [SerializeField]
        private float glowIntensityInRange = 3f;
        
        [SerializeField]
        private bool pulseGlow = true;
        
        [SerializeField]
        private float glowPulseSpeed = 2f;
        
        [Header("Particle Systems")]
        [SerializeField]
        private ParticleSystem idleSparkles;
        
        [SerializeField]
        private ParticleSystem hoverSparkles;
        
        [SerializeField]
        private ParticleSystem inRangeAura;
        
        [SerializeField]
        private ParticleSystem lockedPulse;
        
        [Header("Animation")]
        [SerializeField]
        private float hoverScaleMultiplier = 1.2f;
        
        [SerializeField]
        private float inRangeScaleMultiplier = 1.3f;
        
        [SerializeField]
        private float lockedScaleMultiplier = 0.9f;
        
        [SerializeField]
        private float scaleTransitionSpeed = 5f;
        
        [Header("Colors")]
        [SerializeField]
        private Color glowColorBronze = new Color(0.8f, 0.5f, 0.2f);
        
        [SerializeField]
        private Color glowColorSilver = new Color(0.75f, 0.75f, 0.75f);
        
        [SerializeField]
        private Color glowColorGold = new Color(1f, 0.84f, 0f);
        
        [SerializeField]
        private Color glowColorPlatinum = new Color(0.9f, 0.9f, 0.95f);
        
        [SerializeField]
        private Color glowColorDiamond = new Color(0.5f, 0.8f, 1f);
        
        [SerializeField]
        private Color glowColorLocked = new Color(0.94f, 0.27f, 0.27f);
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current visual state
        /// </summary>
        public CoinVisualState CurrentState { get; private set; } = CoinVisualState.Normal;
        
        /// <summary>
        /// Current tier
        /// </summary>
        public CoinTier CurrentTier { get; private set; } = CoinTier.Gold;
        
        /// <summary>
        /// Is the coin locked?
        /// </summary>
        public bool IsLocked { get; private set; } = false;
        
        #endregion
        
        #region Private Fields
        
        private Vector3 baseScale;
        private float targetScale = 1f;
        private float targetGlowIntensity;
        private Color targetGlowColor;
        private Material currentMaterial;
        private CoinController controller;
        
        // When ARCoinRenderer is present, it owns scale — we yield to avoid conflicts
        private ARCoinRenderer arCoinRenderer;
        private bool arRendererOwnsScale = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            baseScale = transform.localScale;
            controller = GetComponent<CoinController>();
            
            // Check if ARCoinRenderer is present — it owns distance-based scale
            arCoinRenderer = GetComponent<ARCoinRenderer>();
            arRendererOwnsScale = (arCoinRenderer != null);
            if (arRendererOwnsScale)
            {
                Debug.Log("[CoinVisuals] ARCoinRenderer detected — yielding scale control to distance-based system");
            }
            
            // Auto-find mesh renderer if not set
            if (coinRenderer == null)
            {
                coinRenderer = GetComponentInChildren<MeshRenderer>();
            }
            
            // Auto-find point light if not set
            if (pointLight == null)
            {
                pointLight = GetComponentInChildren<Light>();
            }
            
            // Set initial glow
            targetGlowIntensity = glowIntensityNormal;
            targetGlowColor = glowColorGold;
        }
        
        private void Start()
        {
            // Initialize effects
            if (idleSparkles != null)
            {
                idleSparkles.Play();
            }
        }
        
        private void Update()
        {
            UpdateScale();
            UpdateGlow();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize visuals from coin data
        /// </summary>
        public void Initialize(Coin coinData, bool locked)
        {
            if (coinData == null) return;
            
            CurrentTier = coinData.currentTier;
            IsLocked = locked;
            
            // Apply tier material
            ApplyTierMaterial(CurrentTier, coinData.coinType == CoinType.Pool);
            
            // Apply locked state
            if (locked)
            {
                SetState(CoinVisualState.Locked);
            }
            else
            {
                SetState(CoinVisualState.Normal);
            }
        }
        
        #endregion
        
        #region Material Management
        
        /// <summary>
        /// Apply material based on tier
        /// </summary>
        public void ApplyTierMaterial(CoinTier tier, bool isPoolCoin = false)
        {
            CurrentTier = tier;
            
            // Get appropriate material
            Material targetMaterial;
            Color glowColor;
            
            if (IsLocked && lockedMaterial != null)
            {
                targetMaterial = lockedMaterial;
                glowColor = glowColorLocked;
            }
            else if (isPoolCoin && unknownMaterial != null)
            {
                targetMaterial = unknownMaterial;
                glowColor = glowColorSilver;
            }
            else
            {
                (targetMaterial, glowColor) = GetMaterialAndColorForTier(tier);
            }
            
            // Apply material
            if (coinRenderer != null && targetMaterial != null)
            {
                coinRenderer.material = targetMaterial;
                currentMaterial = targetMaterial;
            }
            
            // Update glow color
            targetGlowColor = glowColor;
            if (pointLight != null)
            {
                pointLight.color = glowColor;
            }
            
            // Update particle colors
            UpdateParticleColors(glowColor);
        }
        
        /// <summary>
        /// Get material and glow color for tier
        /// </summary>
        private (Material, Color) GetMaterialAndColorForTier(CoinTier tier)
        {
            return tier switch
            {
                CoinTier.Bronze => (bronzeMaterial ?? goldMaterial, glowColorBronze),
                CoinTier.Silver => (silverMaterial ?? goldMaterial, glowColorSilver),
                CoinTier.Gold => (goldMaterial, glowColorGold),
                CoinTier.Platinum => (platinumMaterial ?? goldMaterial, glowColorPlatinum),
                CoinTier.Diamond => (diamondMaterial ?? goldMaterial, glowColorDiamond),
                _ => (goldMaterial, glowColorGold)
            };
        }
        
        /// <summary>
        /// Update particle system colors
        /// </summary>
        private void UpdateParticleColors(Color color)
        {
            UpdateParticleColor(idleSparkles, color);
            UpdateParticleColor(hoverSparkles, color);
            UpdateParticleColor(inRangeAura, color);
        }
        
        private void UpdateParticleColor(ParticleSystem ps, Color color)
        {
            if (ps == null) return;
            
            var main = ps.main;
            main.startColor = color;
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Set visual state
        /// </summary>
        public void SetState(CoinVisualState state)
        {
            if (CurrentState == state) return;
            
            CoinVisualState previousState = CurrentState;
            CurrentState = state;
            
            // Update targets based on state
            switch (state)
            {
                case CoinVisualState.Normal:
                    targetScale = 1f;
                    targetGlowIntensity = glowIntensityNormal;
                    SetParticleState(idle: true, hover: false, inRange: false, locked: false);
                    break;
                    
                case CoinVisualState.Hovering:
                    targetScale = hoverScaleMultiplier;
                    targetGlowIntensity = glowIntensityHover;
                    SetParticleState(idle: true, hover: true, inRange: false, locked: false);
                    break;
                    
                case CoinVisualState.InRange:
                    targetScale = inRangeScaleMultiplier;
                    targetGlowIntensity = glowIntensityInRange;
                    SetParticleState(idle: true, hover: true, inRange: true, locked: false);
                    break;
                    
                case CoinVisualState.Locked:
                    targetScale = lockedScaleMultiplier;
                    targetGlowIntensity = glowIntensityNormal * 0.5f;
                    targetGlowColor = glowColorLocked;
                    SetParticleState(idle: false, hover: false, inRange: false, locked: true);
                    
                    // Apply locked material
                    if (lockedMaterial != null && coinRenderer != null)
                    {
                        coinRenderer.material = lockedMaterial;
                    }
                    break;
                    
                case CoinVisualState.Collecting:
                    // Collecting state is handled by CoinController
                    SetParticleState(idle: false, hover: false, inRange: false, locked: false);
                    break;
            }
        }
        
        /// <summary>
        /// Set locked state
        /// </summary>
        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            
            if (locked)
            {
                SetState(CoinVisualState.Locked);
            }
            else
            {
                // Restore tier material
                ApplyTierMaterial(CurrentTier);
                SetState(CoinVisualState.Normal);
            }
        }
        
        /// <summary>
        /// Control particle system states
        /// </summary>
        private void SetParticleState(bool idle, bool hover, bool inRange, bool locked)
        {
            SetParticleEnabled(idleSparkles, idle);
            SetParticleEnabled(hoverSparkles, hover);
            SetParticleEnabled(inRangeAura, inRange);
            SetParticleEnabled(lockedPulse, locked);
        }
        
        private void SetParticleEnabled(ParticleSystem ps, bool enabled)
        {
            if (ps == null) return;
            
            if (enabled && !ps.isPlaying)
            {
                ps.Play();
            }
            else if (!enabled && ps.isPlaying)
            {
                ps.Stop();
            }
        }
        
        #endregion
        
        #region Animation Updates
        
        /// <summary>
        /// Smoothly update scale.
        /// Skipped when ARCoinRenderer is present — it owns distance-based scaling.
        /// </summary>
        private void UpdateScale()
        {
            // ARCoinRenderer handles distance-based scale — don't fight it
            if (arRendererOwnsScale) return;
            
            float currentScale = transform.localScale.x / baseScale.x;
            float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleTransitionSpeed);
            transform.localScale = baseScale * newScale;
        }
        
        /// <summary>
        /// Update glow effect
        /// </summary>
        private void UpdateGlow()
        {
            if (pointLight == null) return;
            
            float intensity = targetGlowIntensity;
            
            // Pulse effect
            if (pulseGlow && CurrentState != CoinVisualState.Locked)
            {
                float pulse = Mathf.Sin(Time.time * glowPulseSpeed * Mathf.PI) * 0.3f + 0.7f;
                intensity *= pulse;
            }
            
            // Smooth transition
            pointLight.intensity = Mathf.Lerp(pointLight.intensity, intensity, Time.deltaTime * 5f);
            pointLight.color = Color.Lerp(pointLight.color, targetGlowColor, Time.deltaTime * 5f);
        }
        
        #endregion
        
        #region Special Effects
        
        /// <summary>
        /// Play sparkle burst effect
        /// </summary>
        public void PlaySparkleBurst()
        {
            if (hoverSparkles == null) return;
            
            var emission = hoverSparkles.emission;
            var burst = new ParticleSystem.Burst(0f, 20);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });
            hoverSparkles.Play();
        }
        
        /// <summary>
        /// Flash the coin (e.g., when discovered)
        /// </summary>
        public void PlayFlash(float duration = 0.3f)
        {
            StartCoroutine(FlashCoroutine(duration));
        }
        
        private IEnumerator FlashCoroutine(float duration)
        {
            if (coinRenderer == null) yield break;
            
            // Store original color
            Color originalColor = coinRenderer.material.GetColor("_Color");
            
            // Flash to white
            coinRenderer.material.SetColor("_Color", Color.white);
            
            // Wait
            yield return new WaitForSeconds(duration);
            
            // Restore
            coinRenderer.material.SetColor("_Color", originalColor);
        }
        
        /// <summary>
        /// Fade out effect (for despawning)
        /// </summary>
        public void FadeOut(float duration = 0.5f)
        {
            StartCoroutine(FadeOutCoroutine(duration));
        }
        
        private IEnumerator FadeOutCoroutine(float duration)
        {
            float timer = 0f;
            Vector3 startScale = transform.localScale;
            float startAlpha = 1f;
            
            // Get material alpha if supported
            if (coinRenderer != null && coinRenderer.material.HasProperty("_Color"))
            {
                startAlpha = coinRenderer.material.GetColor("_Color").a;
            }
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                
                // Scale down
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                
                // Fade alpha
                if (coinRenderer != null && coinRenderer.material.HasProperty("_Color"))
                {
                    Color color = coinRenderer.material.GetColor("_Color");
                    color.a = Mathf.Lerp(startAlpha, 0f, t);
                    coinRenderer.material.SetColor("_Color", color);
                }
                
                // Fade glow
                if (pointLight != null)
                {
                    pointLight.intensity = Mathf.Lerp(targetGlowIntensity, 0f, t);
                }
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Cycle through tiers
        /// </summary>
        [ContextMenu("Debug: Cycle Tiers")]
        public void DebugCycleTiers()
        {
            CoinTier nextTier = (CoinTier)(((int)CurrentTier + 1) % 5);
            ApplyTierMaterial(nextTier);
            Debug.Log($"[CoinVisuals] Tier: {nextTier}");
        }
        
        /// <summary>
        /// Debug: Toggle locked state
        /// </summary>
        [ContextMenu("Debug: Toggle Locked")]
        public void DebugToggleLocked()
        {
            SetLocked(!IsLocked);
            Debug.Log($"[CoinVisuals] Locked: {IsLocked}");
        }
        
        /// <summary>
        /// Debug: Cycle states
        /// </summary>
        [ContextMenu("Debug: Cycle States")]
        public void DebugCycleStates()
        {
            CoinVisualState nextState = (CoinVisualState)(((int)CurrentState + 1) % 5);
            SetState(nextState);
            Debug.Log($"[CoinVisuals] State: {nextState}");
        }
        
        #endregion
    }
}
