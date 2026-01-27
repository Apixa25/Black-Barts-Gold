// ============================================================================
// HapticService.cs
// Black Bart's Gold - Haptic Feedback Service
// Path: Assets/Scripts/Location/HapticService.cs
// ============================================================================
// Manages haptic (vibration) feedback based on proximity to coins.
// Different vibration patterns for different distance zones.
// Reference: BUILD-GUIDE.md Prompt 4.3
// ============================================================================

using UnityEngine;
using System.Collections;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Location
{
    /// <summary>
    /// Manages haptic feedback for proximity to coins.
    /// Vibration intensity increases as player gets closer.
    /// </summary>
    public class HapticService : MonoBehaviour
    {
        #region Singleton
        
        private static HapticService _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static HapticService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<HapticService>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("HapticService");
                        _instance = go.AddComponent<HapticService>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Is haptic feedback enabled? (Disabled by default during development)")]
        private bool isEnabled = false; // DISABLED BY DEFAULT - enable in Settings when ready!
        
        [SerializeField]
        [Tooltip("Minimum time between vibrations (seconds)")]
        private float minVibrationInterval = 0.1f;
        
        [Header("Proximity Patterns")]
        [SerializeField]
        [Tooltip("Far zone (30-50m): pulse interval in seconds")]
        private float farPulseInterval = 2.0f;
        
        [SerializeField]
        [Tooltip("Medium zone (15-30m): pulse interval in seconds")]
        private float mediumPulseInterval = 1.0f;
        
        [SerializeField]
        [Tooltip("Near zone (5-15m): pulse interval in seconds")]
        private float nearPulseInterval = 0.5f;
        
        [SerializeField]
        [Tooltip("Collectible zone (<5m): continuous buzz interval")]
        private float collectiblePulseInterval = 0.2f;
        
        [Header("Vibration Durations (ms)")]
        [SerializeField]
        private long farVibrationDuration = 50;
        
        [SerializeField]
        private long mediumVibrationDuration = 100;
        
        [SerializeField]
        private long nearVibrationDuration = 150;
        
        [SerializeField]
        private long collectibleVibrationDuration = 200;
        
        [SerializeField]
        private long collectionSuccessDuration = 300;
        
        [SerializeField]
        private long lockedDeniedDuration = 100;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is haptic feedback currently enabled?
        /// </summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }
        
        /// <summary>
        /// Current proximity zone being tracked
        /// </summary>
        public ProximityZone CurrentZone { get; private set; } = ProximityZone.OutOfRange;
        
        /// <summary>
        /// Is proximity feedback currently active?
        /// </summary>
        public bool IsProximityFeedbackActive { get; private set; } = false;
        
        #endregion
        
        #region Private Fields
        
        private Coroutine proximityCoroutine;
        private float lastVibrationTime = 0f;
        
        #if UNITY_ANDROID
        private AndroidJavaObject vibrator;
        private bool hasVibrator = false;
        #endif
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeVibrator();
        }
        
        private void OnDestroy()
        {
            StopProximityFeedback();
            
            #if UNITY_ANDROID
            vibrator?.Dispose();
            #endif
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize platform-specific vibrator
        /// </summary>
        private void InitializeVibrator()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                        hasVibrator = vibrator != null && vibrator.Call<bool>("hasVibrator");
                        
                        if (debugMode)
                        {
                            Debug.Log($"[HapticService] Android vibrator initialized: {hasVibrator}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticService] Failed to initialize vibrator: {e.Message}");
                hasVibrator = false;
            }
            #endif
        }
        
        #endregion
        
        #region Proximity Feedback
        
        /// <summary>
        /// Start proximity feedback for a distance
        /// </summary>
        public void StartProximityFeedback(float distanceMeters)
        {
            ProximityZone zone = GeoUtils.GetProximityZone(distanceMeters);
            StartProximityFeedback(zone);
        }
        
        /// <summary>
        /// Start proximity feedback for a zone
        /// </summary>
        public void StartProximityFeedback(ProximityZone zone)
        {
            if (!isEnabled) return;
            
            // Don't restart if same zone
            if (IsProximityFeedbackActive && CurrentZone == zone)
            {
                return;
            }
            
            // Stop existing feedback
            StopProximityFeedback();
            
            CurrentZone = zone;
            
            // No feedback for out of range
            if (zone == ProximityZone.OutOfRange)
            {
                return;
            }
            
            // Start new feedback coroutine
            IsProximityFeedbackActive = true;
            proximityCoroutine = StartCoroutine(ProximityFeedbackCoroutine(zone));
            
            Log($"Started proximity feedback: {zone}");
        }
        
        /// <summary>
        /// Update proximity feedback with new distance
        /// </summary>
        public void UpdateProximityFeedback(float distanceMeters)
        {
            ProximityZone newZone = GeoUtils.GetProximityZone(distanceMeters);
            
            if (newZone != CurrentZone)
            {
                StartProximityFeedback(newZone);
            }
        }
        
        /// <summary>
        /// Stop proximity feedback
        /// </summary>
        public void StopProximityFeedback()
        {
            if (proximityCoroutine != null)
            {
                StopCoroutine(proximityCoroutine);
                proximityCoroutine = null;
            }
            
            IsProximityFeedbackActive = false;
            CurrentZone = ProximityZone.OutOfRange;
            
            Log("Stopped proximity feedback");
        }
        
        /// <summary>
        /// Proximity feedback coroutine
        /// </summary>
        private IEnumerator ProximityFeedbackCoroutine(ProximityZone zone)
        {
            float interval;
            long duration;
            
            // Get settings for zone
            switch (zone)
            {
                case ProximityZone.Far:
                    interval = farPulseInterval;
                    duration = farVibrationDuration;
                    break;
                    
                case ProximityZone.Medium:
                    interval = mediumPulseInterval;
                    duration = mediumVibrationDuration;
                    break;
                    
                case ProximityZone.Near:
                    interval = nearPulseInterval;
                    duration = nearVibrationDuration;
                    break;
                    
                case ProximityZone.Collectible:
                    interval = collectiblePulseInterval;
                    duration = collectibleVibrationDuration;
                    break;
                    
                default:
                    yield break;
            }
            
            // Pulse loop
            while (IsProximityFeedbackActive)
            {
                Vibrate(duration);
                yield return new WaitForSeconds(interval);
            }
        }
        
        #endregion
        
        #region Special Feedback
        
        /// <summary>
        /// Trigger collection success feedback
        /// </summary>
        public void TriggerCollectionFeedback()
        {
            if (!isEnabled) return;
            
            Log("Collection feedback triggered");
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Pattern: short-pause-long
            long[] pattern = { 0, 100, 50, 200 };
            VibratePattern(pattern);
            #else
            Vibrate(collectionSuccessDuration);
            #endif
        }
        
        /// <summary>
        /// Trigger locked/denied feedback
        /// </summary>
        public void TriggerLockedFeedback()
        {
            if (!isEnabled) return;
            
            Log("Locked feedback triggered");
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Pattern: two quick pulses
            long[] pattern = { 0, 50, 50, 50 };
            VibratePattern(pattern);
            #else
            Vibrate(lockedDeniedDuration);
            #endif
        }
        
        /// <summary>
        /// Trigger out-of-range feedback
        /// </summary>
        public void TriggerOutOfRangeFeedback()
        {
            if (!isEnabled) return;
            
            Log("Out of range feedback triggered");
            Vibrate(50);
        }
        
        /// <summary>
        /// Trigger error feedback
        /// </summary>
        public void TriggerErrorFeedback()
        {
            if (!isEnabled) return;
            
            Log("Error feedback triggered");
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Pattern: three quick pulses
            long[] pattern = { 0, 50, 30, 50, 30, 50 };
            VibratePattern(pattern);
            #else
            Vibrate(100);
            #endif
        }
        
        #endregion
        
        #region Core Vibration
        
        /// <summary>
        /// Vibrate for a duration (milliseconds)
        /// </summary>
        public void Vibrate(long durationMs)
        {
            if (!isEnabled) return;
            
            // Rate limiting
            if (Time.time - lastVibrationTime < minVibrationInterval)
            {
                return;
            }
            lastVibrationTime = Time.time;
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (hasVibrator && vibrator != null)
            {
                try
                {
                    vibrator.Call("vibrate", durationMs);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[HapticService] Vibration failed: {e.Message}");
                }
            }
            #elif UNITY_IOS && !UNITY_EDITOR
            // iOS doesn't support duration, just vibrate
            Handheld.Vibrate();
            #else
            // Editor - just log
            if (debugMode)
            {
                Debug.Log($"[HapticService] Vibrate: {durationMs}ms");
            }
            #endif
        }
        
        /// <summary>
        /// Vibrate with a pattern (Android only)
        /// </summary>
        public void VibratePattern(long[] pattern)
        {
            if (!isEnabled) return;
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (hasVibrator && vibrator != null)
            {
                try
                {
                    // -1 means don't repeat
                    vibrator.Call("vibrate", pattern, -1);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[HapticService] Pattern vibration failed: {e.Message}");
                }
            }
            #else
            // Fallback to simple vibration
            Vibrate(100);
            #endif
        }
        
        /// <summary>
        /// Cancel any ongoing vibration
        /// </summary>
        public void CancelVibration()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (hasVibrator && vibrator != null)
            {
                try
                {
                    vibrator.Call("cancel");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[HapticService] Cancel vibration failed: {e.Message}");
                }
            }
            #endif
        }
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// Enable or disable haptic feedback
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            
            if (!enabled)
            {
                StopProximityFeedback();
                CancelVibration();
            }
            
            Log($"Haptic feedback {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Toggle haptic feedback
        /// </summary>
        public void ToggleEnabled()
        {
            SetEnabled(!isEnabled);
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[HapticService] {message}");
            }
        }
        
        /// <summary>
        /// Test vibration
        /// </summary>
        [ContextMenu("Test: Short Vibration")]
        public void TestShortVibration()
        {
            Vibrate(50);
        }
        
        /// <summary>
        /// Test medium vibration
        /// </summary>
        [ContextMenu("Test: Medium Vibration")]
        public void TestMediumVibration()
        {
            Vibrate(150);
        }
        
        /// <summary>
        /// Test collection feedback
        /// </summary>
        [ContextMenu("Test: Collection Feedback")]
        public void TestCollectionFeedback()
        {
            TriggerCollectionFeedback();
        }
        
        /// <summary>
        /// Test locked feedback
        /// </summary>
        [ContextMenu("Test: Locked Feedback")]
        public void TestLockedFeedback()
        {
            TriggerLockedFeedback();
        }
        
        /// <summary>
        /// Test all proximity zones
        /// </summary>
        [ContextMenu("Test: Cycle Proximity Zones")]
        public void TestCycleProximityZones()
        {
            StartCoroutine(CycleZonesCoroutine());
        }
        
        private IEnumerator CycleZonesCoroutine()
        {
            ProximityZone[] zones = { 
                ProximityZone.Far, 
                ProximityZone.Medium, 
                ProximityZone.Near, 
                ProximityZone.Collectible 
            };
            
            foreach (var zone in zones)
            {
                Debug.Log($"[HapticService] Testing zone: {zone}");
                StartProximityFeedback(zone);
                yield return new WaitForSeconds(3f);
            }
            
            StopProximityFeedback();
            Debug.Log("[HapticService] Zone test complete");
        }
        
        #endregion
    }
}
