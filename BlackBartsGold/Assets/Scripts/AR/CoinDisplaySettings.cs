// ============================================================================
// CoinDisplaySettings.cs
// Black Bart's Gold - Coin Display Configuration (Pokemon GO Pattern)
// Path: Assets/Scripts/AR/CoinDisplaySettings.cs
// ============================================================================
// SIMPLIFIED for Pokemon GO materialization pattern.
// 
// Key distances:
//   - materializationDistance: When coin appears in AR (player navigates with direction indicator until then)
//   - collectionDistance: When player can collect the coin
//
// The old billboard/world-locked distinction is removed - coins are either
// HIDDEN (use direction indicator) or VISIBLE (materialized in AR).
// ============================================================================

using UnityEngine;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Configuration for Pokemon GO-style coin display.
    /// Simplified to focus on materialization pattern.
    /// </summary>
    [CreateAssetMenu(fileName = "CoinDisplaySettings", menuName = "Black Bart's Gold/Coin Display Settings")]
    public class CoinDisplaySettings : ScriptableObject
    {
        #region Distance Thresholds
        
        [Header("Distance Thresholds (meters)")]
        
        [Tooltip("GPS distance at which coin materializes in AR view. Player uses Direction Indicator to navigate until this distance.")]
        [Range(10f, 50f)]
        public float materializationDistance = 20f;
        
        [Tooltip("GPS distance at which coin can be collected.")]
        [Range(1f, 10f)]
        public float collectionDistance = 5f;
        
        [Tooltip("GPS distance at which coin disappears again if player walks away (should be > materializationDistance).")]
        [Range(20f, 100f)]
        public float hideDistance = 35f;
        
        #endregion
        
        #region Materialization Settings
        
        [Header("Materialization Settings")]
        
        [Tooltip("Distance in front of camera where coin materializes")]
        [Range(2f, 8f)]
        public float materializeViewingDistance = 4f;
        
        [Tooltip("Height above ground when materialized")]
        [Range(0.5f, 2f)]
        public float materializeHeight = 1.2f;
        
        [Tooltip("Duration of materialization animation in seconds")]
        [Range(0.3f, 2f)]
        public float materializeDuration = 0.8f;
        
        #endregion
        
        #region Animation Settings
        
        [Header("Animation Settings")]
        
        [Tooltip("Coin spin speed in degrees per second")]
        [Range(0f, 180f)]
        public float spinSpeed = 45f;
        
        [Tooltip("Bob animation amplitude (meters)")]
        [Range(0f, 0.2f)]
        public float bobAmplitude = 0.05f;
        
        [Tooltip("Bob animation frequency")]
        [Range(0.5f, 3f)]
        public float bobFrequency = 2f;
        
        #endregion
        
        #region Visual Settings
        
        [Header("Visual Settings")]
        
        [Tooltip("Gold color for coins")]
        public Color goldColor = new Color(1f, 0.84f, 0f);
        
        [Tooltip("Color when in collection range")]
        public Color inRangeColor = new Color(0.29f, 0.87f, 0.5f);
        
        [Tooltip("Color when locked (above find limit)")]
        public Color lockedColor = new Color(0.94f, 0.27f, 0.27f);
        
        #endregion
        
        #region Scale Settings
        
        [Header("Scale Settings")]
        
        [Tooltip("Base scale of coin when materialized")]
        [Range(0.1f, 1f)]
        public float baseScale = 0.3f;
        
        [Tooltip("Scale multiplier when in collection range (pulse effect)")]
        [Range(1f, 1.5f)]
        public float collectibleScaleMultiplier = 1.1f;
        
        #endregion
        
        #region Singleton Default
        
        private static CoinDisplaySettings _default;
        
        /// <summary>
        /// Get default settings (creates runtime defaults if no asset exists)
        /// </summary>
        public static CoinDisplaySettings Default
        {
            get
            {
                if (_default == null)
                {
                    // Try to load from Resources
                    _default = Resources.Load<CoinDisplaySettings>("CoinDisplaySettings");
                    
                    // Create runtime defaults if not found
                    if (_default == null)
                    {
                        _default = CreateInstance<CoinDisplaySettings>();
                        Debug.Log("[CoinDisplaySettings] Using runtime default settings");
                    }
                }
                return _default;
            }
        }
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            // Ensure hideDistance > materializationDistance
            if (hideDistance <= materializationDistance)
            {
                hideDistance = materializationDistance + 15f;
            }
            
            // Ensure collectionDistance < materializationDistance
            if (collectionDistance >= materializationDistance)
            {
                collectionDistance = materializationDistance * 0.25f;
            }
        }
        
        #endregion
    }
}
