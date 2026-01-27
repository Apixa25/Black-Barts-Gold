// ============================================================================
// ProximityManager.cs
// Black Bart's Gold - Proximity Detection Manager
// Path: Assets/Scripts/Location/ProximityManager.cs
// ============================================================================
// Manages proximity detection to coins. Tracks nearest coin, triggers
// haptic feedback, and notifies systems when player enters/exits zones.
// Reference: BUILD-GUIDE.md Prompt 4.3, 4.4
// ============================================================================

using UnityEngine;
using System;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.AR;

namespace BlackBartsGold.Location
{
    /// <summary>
    /// Manages proximity detection and feedback for nearby coins.
    /// Coordinates between GPS, coins, and haptic feedback.
    /// </summary>
    public class ProximityManager : MonoBehaviour
    {
        #region Singleton
        
        private static ProximityManager _instance;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ProximityManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ProximityManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ProximityManager");
                        _instance = go.AddComponent<ProximityManager>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("How often to check proximity (seconds)")]
        private float updateInterval = 0.5f;
        
        [SerializeField]
        [Tooltip("Maximum distance to track coins (meters)")]
        private float maxTrackingDistance = 100f;
        
        [SerializeField]
        [Tooltip("Collection range (meters)")]
        private float collectionRange = 5f;
        
        [Header("Haptic Settings")]
        [SerializeField]
        private bool enableHaptics = false; // DISABLED BY DEFAULT - phone was vibrating off the table! 😅
        
        [SerializeField]
        [Tooltip("Only provide haptics for nearest coin")]
        private bool hapticForNearestOnly = true;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Nearest coin to player
        /// </summary>
        public Coin NearestCoin { get; private set; }
        
        /// <summary>
        /// Distance to nearest coin (meters)
        /// </summary>
        public float NearestCoinDistance { get; private set; } = float.MaxValue;
        
        /// <summary>
        /// Bearing to nearest coin (degrees)
        /// </summary>
        public float NearestCoinBearing { get; private set; } = 0f;
        
        /// <summary>
        /// Current proximity zone of nearest coin
        /// </summary>
        public ProximityZone CurrentZone { get; private set; } = ProximityZone.OutOfRange;
        
        /// <summary>
        /// Number of coins in collectible range
        /// </summary>
        public int CoinsInRange { get; private set; } = 0;
        
        /// <summary>
        /// Is player currently near any coin?
        /// </summary>
        public bool IsNearCoin => CurrentZone != ProximityZone.OutOfRange;
        
        /// <summary>
        /// Is player in collection range of any coin?
        /// </summary>
        public bool CanCollect => CurrentZone == ProximityZone.Collectible;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when nearest coin changes
        /// </summary>
        public event Action<Coin> OnNearestCoinChanged;
        
        /// <summary>
        /// Fired when proximity zone changes
        /// </summary>
        public event Action<ProximityZone, ProximityZone> OnZoneChanged;
        
        /// <summary>
        /// Fired when player enters collectible range of a coin
        /// </summary>
        public event Action<Coin> OnEnteredCollectionRange;
        
        /// <summary>
        /// Fired when player exits collectible range of a coin
        /// </summary>
        public event Action<Coin> OnExitedCollectionRange;
        
        /// <summary>
        /// Fired when distance to nearest coin updates
        /// </summary>
        public event Action<float, float> OnDistanceUpdated; // distance, bearing
        
        #endregion
        
        #region Private Fields
        
        private float lastUpdateTime = 0f;
        private List<Coin> trackedCoins = new List<Coin>();
        private HashSet<string> coinsInCollectionRange = new HashSet<string>();
        private Coin previousNearestCoin;
        private ProximityZone previousZone = ProximityZone.OutOfRange;
        
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
        }
        
        private void Start()
        {
            // Subscribe to GPS updates
            if (GPSManager.Exists)
            {
                GPSManager.Instance.OnLocationUpdated += OnLocationUpdated;
            }
        }
        
        private void OnDestroy()
        {
            if (GPSManager.Exists)
            {
                GPSManager.Instance.OnLocationUpdated -= OnLocationUpdated;
            }
        }
        
        private void Update()
        {
            // Periodic proximity check
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateProximity();
            }
        }
        
        #endregion
        
        #region GPS Event Handler
        
        /// <summary>
        /// Handle GPS location update
        /// </summary>
        private void OnLocationUpdated(LocationData location)
        {
            UpdateProximity();
        }
        
        #endregion
        
        #region Proximity Updates
        
        /// <summary>
        /// Update proximity calculations
        /// </summary>
        public void UpdateProximity()
        {
            LocationData playerLocation = GetPlayerLocation();
            if (playerLocation == null) return;
            
            // Get coins from CoinManager
            UpdateTrackedCoins();
            
            if (trackedCoins.Count == 0)
            {
                ClearProximity();
                return;
            }
            
            // Update distances for all coins
            GeoUtils.UpdateCoinDistances(trackedCoins, playerLocation);
            
            // Find nearest coin
            Coin nearest = GeoUtils.GetNearestCoin(trackedCoins, playerLocation);
            
            if (nearest != null)
            {
                UpdateNearestCoin(nearest, playerLocation);
            }
            else
            {
                ClearProximity();
            }
            
            // Check collection range
            UpdateCollectionRange(playerLocation);
        }
        
        /// <summary>
        /// Update nearest coin tracking
        /// </summary>
        private void UpdateNearestCoin(Coin nearest, LocationData playerLocation)
        {
            // Calculate distance and bearing
            float distance = GeoUtils.CalculateDistance(playerLocation, 
                new LocationData(nearest.latitude, nearest.longitude));
            float bearing = GeoUtils.CalculateBearing(playerLocation,
                new LocationData(nearest.latitude, nearest.longitude));
            
            // Check if nearest changed
            bool nearestChanged = previousNearestCoin == null || 
                                  previousNearestCoin.id != nearest.id;
            
            // Update properties
            NearestCoin = nearest;
            NearestCoinDistance = distance;
            NearestCoinBearing = bearing;
            
            // Update zone
            ProximityZone newZone = GeoUtils.GetProximityZone(distance);
            bool zoneChanged = newZone != previousZone;
            
            CurrentZone = newZone;
            
            // Fire events
            if (nearestChanged)
            {
                Log($"Nearest coin changed: {nearest.GetDisplayValue()} at {distance:F0}m");
                previousNearestCoin = nearest;
                OnNearestCoinChanged?.Invoke(nearest);
            }
            
            if (zoneChanged)
            {
                Log($"Zone changed: {previousZone} → {newZone}");
                OnZoneChanged?.Invoke(previousZone, newZone);
                previousZone = newZone;
                
                // Update haptics
                if (enableHaptics && HapticService.Instance != null)
                {
                    HapticService.Instance.StartProximityFeedback(newZone);
                }
            }
            
            // Always fire distance update
            OnDistanceUpdated?.Invoke(distance, bearing);
        }
        
        /// <summary>
        /// Update collection range tracking
        /// </summary>
        private void UpdateCollectionRange(LocationData playerLocation)
        {
            HashSet<string> currentInRange = new HashSet<string>();
            int count = 0;
            
            foreach (var coin in trackedCoins)
            {
                if (coin.distanceFromPlayer <= collectionRange)
                {
                    currentInRange.Add(coin.id);
                    count++;
                    
                    // Check if just entered range
                    if (!coinsInCollectionRange.Contains(coin.id))
                    {
                        Log($"Entered collection range: {coin.GetDisplayValue()}");
                        OnEnteredCollectionRange?.Invoke(coin);
                    }
                }
            }
            
            // Check for coins that left range
            foreach (string coinId in coinsInCollectionRange)
            {
                if (!currentInRange.Contains(coinId))
                {
                    Coin coin = trackedCoins.Find(c => c.id == coinId);
                    if (coin != null)
                    {
                        Log($"Exited collection range: {coin.GetDisplayValue()}");
                        OnExitedCollectionRange?.Invoke(coin);
                    }
                }
            }
            
            coinsInCollectionRange = currentInRange;
            CoinsInRange = count;
        }
        
        /// <summary>
        /// Clear proximity tracking
        /// </summary>
        private void ClearProximity()
        {
            if (NearestCoin != null)
            {
                NearestCoin = null;
                previousNearestCoin = null;
                OnNearestCoinChanged?.Invoke(null);
            }
            
            NearestCoinDistance = float.MaxValue;
            NearestCoinBearing = 0f;
            
            if (CurrentZone != ProximityZone.OutOfRange)
            {
                ProximityZone oldZone = CurrentZone;
                CurrentZone = ProximityZone.OutOfRange;
                previousZone = ProximityZone.OutOfRange;
                OnZoneChanged?.Invoke(oldZone, ProximityZone.OutOfRange);
                
                if (enableHaptics && HapticService.Instance != null)
                {
                    HapticService.Instance.StopProximityFeedback();
                }
            }
            
            coinsInCollectionRange.Clear();
            CoinsInRange = 0;
        }
        
        #endregion
        
        #region Coin Tracking
        
        /// <summary>
        /// Update list of tracked coins from CoinManager
        /// </summary>
        private void UpdateTrackedCoins()
        {
            trackedCoins.Clear();
            
            if (CoinManager.Instance == null) return;
            
            // Get coins from CoinManager and convert to Coin data
            foreach (var controller in CoinManager.Instance.ActiveCoins)
            {
                if (controller.CoinData != null)
                {
                    trackedCoins.Add(controller.CoinData);
                }
            }
        }
        
        /// <summary>
        /// Manually set coins to track (for testing)
        /// </summary>
        public void SetTrackedCoins(List<Coin> coins)
        {
            trackedCoins = coins ?? new List<Coin>();
            UpdateProximity();
        }
        
        /// <summary>
        /// Add a coin to track
        /// </summary>
        public void AddTrackedCoin(Coin coin)
        {
            if (coin != null && !trackedCoins.Exists(c => c.id == coin.id))
            {
                trackedCoins.Add(coin);
            }
        }
        
        /// <summary>
        /// Remove a coin from tracking
        /// </summary>
        public void RemoveTrackedCoin(string coinId)
        {
            trackedCoins.RemoveAll(c => c.id == coinId);
            coinsInCollectionRange.Remove(coinId);
        }
        
        #endregion
        
        #region Helpers
        
        /// <summary>
        /// Get current player location
        /// </summary>
        private LocationData GetPlayerLocation()
        {
            if (GPSManager.Exists)
            {
                return GPSManager.Instance.GetBestLocation();
            }
            
            if (PlayerData.Exists)
            {
                return PlayerData.Instance.GetBestLocation();
            }
            
            return null;
        }
        
        /// <summary>
        /// Get direction text to nearest coin
        /// </summary>
        public string GetDirectionText()
        {
            if (NearestCoin == null) return "No coins nearby";
            
            string direction = GeoUtils.GetCardinalDirection(NearestCoinBearing);
            string distance = GeoUtils.FormatDistance(NearestCoinDistance);
            
            return $"{distance} {direction}";
        }
        
        /// <summary>
        /// Get proximity description
        /// </summary>
        public string GetProximityDescription()
        {
            return GeoUtils.GetProximityDescription(CurrentZone);
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[ProximityManager] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print proximity status
        /// </summary>
        [ContextMenu("Debug: Print Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== Proximity Status ===");
            Debug.Log($"Tracked Coins: {trackedCoins.Count}");
            Debug.Log($"Nearest: {NearestCoin?.GetDisplayValue() ?? "none"}");
            Debug.Log($"Distance: {NearestCoinDistance:F1}m");
            Debug.Log($"Bearing: {NearestCoinBearing:F0}° ({GeoUtils.GetCardinalDirection(NearestCoinBearing)})");
            Debug.Log($"Zone: {CurrentZone}");
            Debug.Log($"In Range: {CoinsInRange}");
            Debug.Log("========================");
        }
        
        /// <summary>
        /// Debug: Simulate approaching coin
        /// </summary>
        [ContextMenu("Debug: Simulate Approach")]
        public void DebugSimulateApproach()
        {
            StartCoroutine(SimulateApproachCoroutine());
        }
        
        private System.Collections.IEnumerator SimulateApproachCoroutine()
        {
            float[] distances = { 60f, 40f, 25f, 12f, 4f, 2f };
            
            foreach (float dist in distances)
            {
                // Simulate zone change
                ProximityZone zone = GeoUtils.GetProximityZone(dist);
                Log($"Simulating distance: {dist}m (zone: {zone})");
                
                if (enableHaptics && HapticService.Instance != null)
                {
                    HapticService.Instance.StartProximityFeedback(zone);
                }
                
                yield return new WaitForSeconds(2f);
            }
            
            if (HapticService.Instance != null)
            {
                HapticService.Instance.StopProximityFeedback();
            }
        }
        
        #endregion
    }
}
