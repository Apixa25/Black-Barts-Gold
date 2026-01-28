// ============================================================================
// CoinCollectionEffect.cs
// Black Bart's Gold - Coin Collection Visual Effects
// Path: Assets/Scripts/AR/CoinCollectionEffect.cs
// ============================================================================
// Handles the visual feedback when a coin is collected: particles, sound,
// screen flash, and celebration animation.
// Reference: BUILD-GUIDE.md Prompt 3.4
// ============================================================================

using UnityEngine;
using System.Collections;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Manages coin collection visual and audio effects.
    /// Can be attached to a coin prefab or used as a standalone manager.
    /// </summary>
    public class CoinCollectionEffect : MonoBehaviour
    {
        #region Singleton (Optional)
        
        private static CoinCollectionEffect _instance;
        
        /// <summary>
        /// Optional singleton for standalone manager mode
        /// </summary>
        public static CoinCollectionEffect Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CoinCollectionEffect>();
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Particle Prefabs")]
        [SerializeField]
        [Tooltip("Explosion of coins/sparkles on collection")]
        private GameObject collectParticlePrefab;
        
        [SerializeField]
        [Tooltip("Coin trail flying to UI wallet")]
        private GameObject coinTrailPrefab;
        
        [Header("Audio")]
        [SerializeField]
        private AudioSource audioSource;
        
        [SerializeField]
        private AudioClip collectSoundSmall;  // < $1
        
        [SerializeField]
        private AudioClip collectSoundMedium; // $1 - $5
        
        [SerializeField]
        private AudioClip collectSoundLarge;  // $5 - $25
        
        [SerializeField]
        private AudioClip collectSoundJackpot; // > $25
        
        [SerializeField]
        private AudioClip collectSoundLocked;  // Locked coin tap
        
        [Header("Screen Flash")]
        [SerializeField]
        private CanvasGroup screenFlashOverlay;
        
        [SerializeField]
        private float flashDuration = 0.3f;
        
        [SerializeField]
        private Color flashColorSmall = new Color(1f, 0.84f, 0f, 0.3f);  // Gold
        
        [SerializeField]
        private Color flashColorLarge = new Color(1f, 1f, 1f, 0.5f);     // White
        
        [Header("Haptics")]
        [SerializeField]
        private bool useHaptics = true;
        
        [SerializeField]
        [Range(0.01f, 1f)]
        private float hapticIntensity = 0.5f;
        
        [Header("Animation")]
        [SerializeField]
        private float coinFlyDuration = 0.8f;
        
        [SerializeField]
        private Transform walletUITarget; // UI position coins fly to
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Private Fields
        
        private Camera mainCamera;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            
            mainCamera = Camera.main;
            
            // Create audio source if needed
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        
        private void Start()
        {
            // Subscribe to CoinManager events
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.OnCoinCollected += OnCoinCollected;
            }
        }
        
        private void OnDestroy()
        {
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.OnCoinCollected -= OnCoinCollected;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle coin collection from CoinManager
        /// </summary>
        private void OnCoinCollected(CoinController coin, float value)
        {
            PlayCollectionEffect(coin.transform.position, value, coin.CoinData?.currentTier ?? CoinTier.Bronze);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Play the full collection effect at a position
        /// </summary>
        public void PlayCollectionEffect(Vector3 worldPosition, float value, CoinTier tier)
        {
            Log($"Playing collection effect: ${value:F2} ({tier})");
            
            // 1. Particle burst at coin position
            SpawnCollectParticles(worldPosition, tier);
            
            // 2. Play sound based on value
            PlayCollectSound(value);
            
            // 3. Screen flash
            StartCoroutine(PlayScreenFlash(value));
            
            // 4. Haptic feedback
            PlayHaptic(value);
            
            // 5. Coin trail to wallet UI (if target set)
            if (walletUITarget != null)
            {
                StartCoroutine(AnimateCoinToWallet(worldPosition, value));
            }
        }
        
        /// <summary>
        /// Play locked coin feedback (denied collection)
        /// </summary>
        public void PlayLockedFeedback(Vector3 worldPosition)
        {
            Log("Playing locked feedback");
            
            // Play denied sound
            if (collectSoundLocked != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSoundLocked);
            }
            
            // Short haptic
            if (useHaptics)
            {
                TryVibrate();
            }
            
            // Red flash
            if (screenFlashOverlay != null)
            {
                StartCoroutine(PlayFlash(new Color(1f, 0.2f, 0.2f, 0.3f)));
            }
        }
        
        /// <summary>
        /// Play out-of-range feedback
        /// </summary>
        public void PlayOutOfRangeFeedback()
        {
            Log("Playing out-of-range feedback");
            
            // Light haptic
            if (useHaptics)
            {
                // Light vibration (platform dependent)
                #if UNITY_ANDROID
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                        {
                            vibrator.Call("vibrate", 50L); // 50ms
                        }
                    }
                }
                #else
                TryVibrate();
                #endif
            }
        }
        
        #endregion
        
        #region Particles
        
        /// <summary>
        /// Spawn collection particles
        /// </summary>
        private void SpawnCollectParticles(Vector3 position, CoinTier tier)
        {
            if (collectParticlePrefab == null) return;
            
            GameObject particles = Instantiate(collectParticlePrefab, position, Quaternion.identity);
            
            // Customize based on tier
            ParticleSystem ps = particles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = GetColorForTier(tier);
                
                // More particles for higher value
                var emission = ps.emission;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0f, GetParticleCountForTier(tier))
                });
            }
            
            // Auto-destroy
            Destroy(particles, 3f);
        }
        
        /// <summary>
        /// Get particle color for tier
        /// </summary>
        private Color GetColorForTier(CoinTier tier)
        {
            return tier switch
            {
                CoinTier.Bronze => new Color(0.8f, 0.5f, 0.2f),
                CoinTier.Silver => new Color(0.75f, 0.75f, 0.75f),
                CoinTier.Gold => new Color(1f, 0.84f, 0f),
                CoinTier.Platinum => new Color(0.9f, 0.9f, 0.95f),
                CoinTier.Diamond => new Color(0.5f, 0.8f, 1f),
                _ => Color.yellow
            };
        }
        
        /// <summary>
        /// Get particle count for tier
        /// </summary>
        private short GetParticleCountForTier(CoinTier tier)
        {
            return tier switch
            {
                CoinTier.Bronze => 10,
                CoinTier.Silver => 20,
                CoinTier.Gold => 30,
                CoinTier.Platinum => 50,
                CoinTier.Diamond => 100,
                _ => 15
            };
        }
        
        #endregion
        
        #region Audio
        
        /// <summary>
        /// Play collection sound based on value
        /// </summary>
        private void PlayCollectSound(float value)
        {
            if (audioSource == null) return;
            
            AudioClip clip = null;
            
            if (value >= 25f && collectSoundJackpot != null)
            {
                clip = collectSoundJackpot;
            }
            else if (value >= 5f && collectSoundLarge != null)
            {
                clip = collectSoundLarge;
            }
            else if (value >= 1f && collectSoundMedium != null)
            {
                clip = collectSoundMedium;
            }
            else if (collectSoundSmall != null)
            {
                clip = collectSoundSmall;
            }
            
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        #endregion
        
        #region Screen Flash
        
        /// <summary>
        /// Play screen flash coroutine
        /// </summary>
        private IEnumerator PlayScreenFlash(float value)
        {
            if (screenFlashOverlay == null) yield break;
            
            Color flashColor = value >= 10f ? flashColorLarge : flashColorSmall;
            yield return PlayFlash(flashColor);
        }
        
        /// <summary>
        /// Generic flash coroutine
        /// </summary>
        private IEnumerator PlayFlash(Color color)
        {
            if (screenFlashOverlay == null) yield break;
            
            // Get or add image for color
            var image = screenFlashOverlay.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = color;
            }
            
            // Fade in
            float timer = 0f;
            float halfDuration = flashDuration / 2f;
            
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                screenFlashOverlay.alpha = Mathf.Lerp(0, 1, timer / halfDuration);
                yield return null;
            }
            
            // Fade out
            timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                screenFlashOverlay.alpha = Mathf.Lerp(1, 0, timer / halfDuration);
                yield return null;
            }
            
            screenFlashOverlay.alpha = 0;
        }
        
        #endregion
        
        #region Haptics
        
        /// <summary>
        /// Play haptic feedback based on value
        /// </summary>
        private void PlayHaptic(float value)
        {
            if (!useHaptics) return;
            
            #if UNITY_ANDROID
            // Android: Variable vibration
            long duration = value >= 25f ? 200L :
                           value >= 5f ? 100L :
                           value >= 1f ? 50L : 25L;
            
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                        {
                            if (value >= 25f)
                            {
                                // Pattern for jackpot: buzz-pause-buzz
                                vibrator.Call("vibrate", new long[] { 0, 100, 50, 150 }, -1);
                            }
                            else
                            {
                                vibrator.Call("vibrate", duration);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CoinCollectionEffect] Haptic error: {e.Message}");
            }
            #elif UNITY_IOS
            // iOS: Use vibration API
            TryVibrate();
            #endif
        }
        
        /// <summary>
        /// Cross-platform vibration that handles Unity 6 API changes
        /// </summary>
        private void TryVibrate()
        {
            #if UNITY_ANDROID
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                        {
                            vibrator.Call("vibrate", 100L); // 100ms default vibration
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CoinCollectionEffect] Vibration error: {e.Message}");
            }
            #elif UNITY_IOS
            // iOS vibration - using AudioServices as fallback
            // Note: Handheld.Vibrate() was removed in Unity 6
            Debug.Log("[CoinCollectionEffect] iOS vibration requested");
            #else
            // Editor/other platforms - just log
            Debug.Log("[CoinCollectionEffect] Vibration requested (not supported on this platform)");
            #endif
        }
        
        #endregion
        
        #region Coin Animation
        
        /// <summary>
        /// Animate coin icon flying to wallet UI
        /// </summary>
        private IEnumerator AnimateCoinToWallet(Vector3 startWorldPos, float value)
        {
            if (coinTrailPrefab == null || walletUITarget == null || mainCamera == null)
            {
                yield break;
            }
            
            // Convert world position to screen position
            Vector3 startScreenPos = mainCamera.WorldToScreenPoint(startWorldPos);
            Vector3 endScreenPos = walletUITarget.position;
            
            // Create coin trail
            GameObject trail = Instantiate(coinTrailPrefab);
            RectTransform rectTransform = trail.GetComponent<RectTransform>();
            
            if (rectTransform == null)
            {
                Destroy(trail);
                yield break;
            }
            
            // Animate
            float timer = 0f;
            Vector3 controlPoint = (startScreenPos + endScreenPos) / 2f;
            controlPoint.y += 200f; // Arc upward
            
            while (timer < coinFlyDuration)
            {
                timer += Time.deltaTime;
                float t = timer / coinFlyDuration;
                
                // Ease out
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                
                // Quadratic bezier curve
                Vector3 pos = QuadraticBezier(startScreenPos, controlPoint, endScreenPos, easedT);
                rectTransform.position = pos;
                
                // Scale down as approaching target
                float scale = Mathf.Lerp(1f, 0.3f, easedT);
                rectTransform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            // Destroy trail
            Destroy(trail);
        }
        
        /// <summary>
        /// Quadratic bezier curve calculation
        /// </summary>
        private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[CoinCollectionEffect] {message}");
            }
        }
        
        /// <summary>
        /// Test collection effect
        /// </summary>
        [ContextMenu("Test: Small Collection")]
        public void TestSmallCollection()
        {
            PlayCollectionEffect(transform.position + Vector3.forward * 2f, 0.50f, CoinTier.Bronze);
        }
        
        /// <summary>
        /// Test medium collection effect
        /// </summary>
        [ContextMenu("Test: Medium Collection")]
        public void TestMediumCollection()
        {
            PlayCollectionEffect(transform.position + Vector3.forward * 2f, 5.00f, CoinTier.Gold);
        }
        
        /// <summary>
        /// Test jackpot collection effect
        /// </summary>
        [ContextMenu("Test: Jackpot Collection")]
        public void TestJackpotCollection()
        {
            PlayCollectionEffect(transform.position + Vector3.forward * 2f, 50.00f, CoinTier.Diamond);
        }
        
        /// <summary>
        /// Test locked feedback
        /// </summary>
        [ContextMenu("Test: Locked Feedback")]
        public void TestLockedFeedback()
        {
            PlayLockedFeedback(transform.position + Vector3.forward * 2f);
        }
        
        #endregion
    }
}
