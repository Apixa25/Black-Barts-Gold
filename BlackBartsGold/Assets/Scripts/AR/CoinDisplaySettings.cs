// ============================================================================
// CoinDisplaySettings.cs
// Black Bart's Gold - Coin Display Configuration
// Path: Assets/Scripts/AR/CoinDisplaySettings.cs
// ============================================================================
// Configuration settings for the distance-adaptive AR coin display system.
// Reference: Docs/AR-COIN-DISPLAY-SPEC.md
// ============================================================================

using UnityEngine;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Configuration settings for coin display behavior.
    /// Based on ViroReact billboard patterns and Minecraft Earth scaling.
    /// </summary>
    [CreateAssetMenu(fileName = "CoinDisplaySettings", menuName = "Black Bart's Gold/Coin Display Settings")]
    public class CoinDisplaySettings : ScriptableObject
    {
        #region Distance Thresholds
        
        [Header("Distance Thresholds (meters)")]
        
        [Tooltip("Beyond this distance, coin is hidden (not rendered)")]
        [Range(50f, 200f)]
        public float hideDistance = 100f;
        
        [Tooltip("Distance at which coin 'materializes' in AR view (Pokemon GO pattern)")]
        [Range(15f, 50f)]
        public float materializationDistance = 20f;
        
        [Tooltip("Beyond this distance, coin uses billboard mode. Below this = world-locked")]
        [Range(10f, 50f)]
        public float billboardDistance = 15f;
        
        [Tooltip("Within this distance, coin can be collected")]
        [Range(1f, 10f)]
        public float collectionDistance = 5f;
        
        #endregion
        
        #region Screen Size Settings
        
        [Header("Screen Size Settings (Billboard Mode)")]
        
        [Tooltip("Minimum screen size in pixels when in billboard mode (same as crosshairs)")]
        [Range(30f, 120f)]
        public float minScreenSizePixels = 60f;
        
        [Tooltip("Maximum screen size in pixels to prevent coins from getting too large")]
        [Range(100f, 400f)]
        public float maxScreenSizePixels = 200f;
        
        #endregion
        
        #region World Scale Settings
        
        [Header("World Scale Settings (World-Locked Mode)")]
        
        [Tooltip("Base world scale of coin (meters). 0.3 = 30cm diameter")]
        [Range(0.1f, 1f)]
        public float baseWorldScale = 0.3f;
        
        [Tooltip("Maximum world scale when very close")]
        [Range(0.3f, 1.5f)]
        public float maxWorldScale = 0.5f;
        
        #endregion
        
        #region Transition Settings
        
        [Header("Transition Settings")]
        
        [Tooltip("Time to smoothly transition between modes (seconds)")]
        [Range(0.1f, 1f)]
        public float transitionSmoothTime = 0.3f;
        
        [Tooltip("Hysteresis distance to prevent mode flickering (meters)")]
        [Range(1f, 5f)]
        public float hysteresisDistance = 2f;
        
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
        
        [Tooltip("Color when locked")]
        public Color lockedColor = new Color(0.94f, 0.27f, 0.27f);
        
        #endregion
        
        #region Performance Settings
        
        [Header("Performance Settings")]
        
        [Tooltip("Update rate for billboard mode (Hz). Lower = better performance")]
        [Range(5f, 30f)]
        public float billboardUpdateRate = 10f;
        
        [Tooltip("Update rate for world-locked mode (Hz)")]
        [Range(15f, 60f)]
        public float worldLockedUpdateRate = 30f;
        
        #endregion
        
        #region Height Settings
        
        [Header("Height Settings")]
        
        [Tooltip("Default height above ground for coins (meters)")]
        [Range(0.5f, 2f)]
        public float defaultHeight = 1.0f;
        
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
        
        #region Helper Methods
        
        /// <summary>
        /// Get the update interval for a given display mode
        /// </summary>
        public float GetUpdateInterval(CoinDisplayMode mode)
        {
            return mode switch
            {
                CoinDisplayMode.Billboard => 1f / billboardUpdateRate,
                CoinDisplayMode.WorldLocked => 1f / worldLockedUpdateRate,
                _ => 1f / 10f // Default 10 Hz
            };
        }
        
        /// <summary>
        /// Get the appropriate display mode for a given distance
        /// </summary>
        public CoinDisplayMode GetModeForDistance(float distance, CoinDisplayMode currentMode)
        {
            // Apply hysteresis to prevent flickering
            float effectiveBillboardDistance = billboardDistance;
            
            if (currentMode == CoinDisplayMode.WorldLocked)
            {
                // Add hysteresis when transitioning back to billboard
                effectiveBillboardDistance += hysteresisDistance;
            }
            
            if (distance > hideDistance)
                return CoinDisplayMode.Hidden;
            else if (distance > effectiveBillboardDistance)
                return CoinDisplayMode.Billboard;
            else
                return CoinDisplayMode.WorldLocked;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Display mode for AR coins.
    /// Based on ViroReact transformBehaviors patterns.
    /// </summary>
    public enum CoinDisplayMode
    {
        /// <summary>Coin is not rendered (out of range)</summary>
        Hidden,
        
        /// <summary>Billboard mode - always faces camera, constant screen size</summary>
        Billboard,
        
        /// <summary>World-locked mode - fixed in AR space, natural perspective</summary>
        WorldLocked,
        
        /// <summary>Collection state - being collected</summary>
        Collected
    }
}
