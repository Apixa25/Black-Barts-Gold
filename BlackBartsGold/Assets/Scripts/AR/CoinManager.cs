// ============================================================================
// CoinManager.cs
// Black Bart's Gold - Coin Management System (Single-Target Architecture)
// Path: Assets/Scripts/AR/CoinManager.cs
// ============================================================================
// Manages coins using Pokémon GO-style single-target tracking.
// - KnownCoins: All coins from API (shown on full map)
// - TargetCoin: The ONE coin being actively hunted in AR
// Reference: Docs/AR-COIN-DISPLAY-SPEC.md
// ============================================================================

using UnityEngine;
using System;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Hunt mode state - determines UI and tracking behavior
    /// </summary>
    public enum HuntMode
    {
        /// <summary>Viewing the full map, no coin selected</summary>
        MapView,
        
        /// <summary>Actively hunting a single target coin in AR</summary>
        Hunting,
        
        /// <summary>Collecting the target coin</summary>
        Collecting
    }
    
    /// <summary>
    /// Manages coins using single-target architecture (like Pokémon GO).
    /// Only ONE coin is actively tracked in AR at a time.
    /// </summary>
    public class CoinManager : MonoBehaviour
    {
        #region Singleton
        
        private static CoinManager _instance;
        
        public static CoinManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CoinManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CoinManager");
                        _instance = go.AddComponent<CoinManager>();
                    }
                }
                return _instance;
            }
        }
        
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Prefab")]
        [SerializeField]
        [Tooltip("Coin prefab to instantiate (optional - creates default if null)")]
        private GameObject coinPrefab;
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Coin display settings (uses default if null)")]
        private CoinDisplaySettings displaySettings;
        
        [SerializeField]
        [Tooltip("Parent transform for spawned coins")]
        private Transform coinsParent;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties - Single Target Architecture
        
        /// <summary>
        /// Current hunt mode (MapView or Hunting)
        /// </summary>
        public HuntMode CurrentMode { get; private set; } = HuntMode.MapView;
        
        /// <summary>
        /// All coins we know about from the server (for map display).
        /// These are NOT spawned as AR objects until selected.
        /// </summary>
        public List<Coin> KnownCoins { get; private set; } = new List<Coin>();
        
        /// <summary>
        /// The ONE coin being actively hunted in AR.
        /// Only this coin has an active CoinController/ARCoinPositioner.
        /// </summary>
        public CoinController TargetCoin { get; private set; }
        
        /// <summary>
        /// The data for the target coin (available even before spawning)
        /// </summary>
        public Coin TargetCoinData { get; private set; }
        
        /// <summary>
        /// Is there an active target coin?
        /// </summary>
        public bool HasTarget => TargetCoin != null && TargetCoinData != null;
        
        /// <summary>
        /// Number of coins known (for map display)
        /// </summary>
        public int KnownCoinCount => KnownCoins.Count;
        
        /// <summary>
        /// Total value of all known coins
        /// </summary>
        public float TotalKnownValue { get; private set; } = 0f;
        
        /// <summary>Display settings in use</summary>
        public CoinDisplaySettings DisplaySettings => displaySettings ?? CoinDisplaySettings.Default;
        
        // Legacy compatibility properties
        /// <summary>All currently active coins (legacy - returns target if exists)</summary>
        public List<CoinController> ActiveCoins 
        { 
            get 
            {
                var list = new List<CoinController>();
                if (TargetCoin != null) list.Add(TargetCoin);
                return list;
            }
        }
        
        /// <summary>Number of active coins (legacy - 0 or 1)</summary>
        public int ActiveCoinCount => TargetCoin != null ? 1 : 0;
        
        /// <summary>Currently selected coin (legacy - same as TargetCoin)</summary>
        public CoinController SelectedCoin => TargetCoin;
        
        /// <summary>Nearest coin to player (legacy - same as TargetCoin)</summary>
        public CoinController NearestCoin => TargetCoin;
        
        /// <summary>Total value of all active coins (legacy)</summary>
        public float TotalActiveValue => TargetCoinData?.value ?? 0f;
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when hunt mode changes</summary>
        public event Action<HuntMode> OnHuntModeChanged;
        
        /// <summary>Fired when target coin is set</summary>
        public event Action<Coin> OnTargetSet;
        
        /// <summary>Fired when target coin is cleared</summary>
        public event Action OnTargetCleared;
        
        /// <summary>Fired when known coins list is updated</summary>
        public event Action<List<Coin>> OnKnownCoinsUpdated;
        
        /// <summary>Fired when target coin is collected</summary>
        public event Action<Coin, float> OnTargetCollected;
        
        /// <summary>Fired when target coin materializes in AR view (Pokemon GO pattern)</summary>
        public event Action<CoinController> OnCoinMaterialized;
        
        // Legacy events
        public event Action<CoinController> OnCoinSpawned;
        public event Action<CoinController> OnCoinDespawned;
        public event Action<CoinController, float> OnCoinCollected;
        public event Action<CoinController> OnCoinSelectionChanged;
        public event Action<CoinController> OnNearestCoinChanged;
        
        #endregion
        
        #region Private Fields
        
        private Queue<CoinController> coinPool = new Queue<CoinController>();
        
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
            
            // Create coins parent
            if (coinsParent == null)
            {
                GameObject parent = new GameObject("Coins");
                coinsParent = parent.transform;
            }
            
            Log("CoinManager initialized with SINGLE-TARGET architecture");
        }
        
        private void Start()
        {
            // Initialize compass heading for positioning
            ARCoinPositioner.CaptureInitialCompassHeading();
            
            // Start in map view mode
            SetHuntMode(HuntMode.MapView);
        }
        
        #endregion
        
        #region Hunt Mode Management
        
        /// <summary>
        /// Set the current hunt mode
        /// </summary>
        public void SetHuntMode(HuntMode mode)
        {
            if (CurrentMode == mode) return;
            
            HuntMode previousMode = CurrentMode;
            CurrentMode = mode;
            
            Log($"Hunt mode changed: {previousMode} → {mode}");
            
            OnHuntModeChanged?.Invoke(mode);
        }
        
        /// <summary>
        /// Enter map view mode (clears target, shows full map)
        /// </summary>
        public void EnterMapView()
        {
            ClearTarget();
            SetHuntMode(HuntMode.MapView);
        }
        
        /// <summary>
        /// Enter hunting mode (requires a target to be set)
        /// </summary>
        public void EnterHuntingMode()
        {
            if (!HasTarget)
            {
                Log("Cannot enter hunting mode without a target");
                return;
            }
            SetHuntMode(HuntMode.Hunting);
        }
        
        #endregion
        
        #region Known Coins (Map Data)
        
        /// <summary>
        /// Set the list of known coins from server/API.
        /// These are NOT spawned as AR objects - just stored for map display.
        /// </summary>
        public void SetKnownCoins(List<Coin> coins)
        {
            Log($"SetKnownCoins called with {coins?.Count ?? 0} coins");
            
            KnownCoins.Clear();
            
            if (coins != null)
            {
                // Mark locked status based on player's find limit
                foreach (var coin in coins)
                {
                    coin.isLocked = CheckIfLocked(coin);
                    KnownCoins.Add(coin);
                }
            }
            
            UpdateTotalKnownValue();
            
            Log($"Known coins updated: {KnownCoinCount} coins, total value ${TotalKnownValue:F2}");
            
            OnKnownCoinsUpdated?.Invoke(KnownCoins);
        }
        
        /// <summary>
        /// Legacy compatibility - calls SetKnownCoins
        /// </summary>
        public void SetNearbyCoins(List<Coin> coins)
        {
            SetKnownCoins(coins);
        }
        
        /// <summary>
        /// Get a known coin by ID
        /// </summary>
        public Coin GetKnownCoinById(string coinId)
        {
            return KnownCoins.Find(c => c.id == coinId);
        }
        
        /// <summary>
        /// Remove a coin from known coins (e.g., after collection)
        /// </summary>
        public void RemoveKnownCoin(string coinId)
        {
            KnownCoins.RemoveAll(c => c.id == coinId);
            UpdateTotalKnownValue();
            OnKnownCoinsUpdated?.Invoke(KnownCoins);
        }
        
        private void UpdateTotalKnownValue()
        {
            TotalKnownValue = 0f;
            foreach (var coin in KnownCoins)
            {
                TotalKnownValue += coin.value;
            }
        }
        
        #endregion
        
        #region Target Coin (Single AR Target)
        
        /// <summary>
        /// Set the target coin to hunt. This spawns the coin in AR.
        /// Only ONE coin can be targeted at a time.
        /// </summary>
        public bool SetTargetCoin(Coin coinData)
        {
            if (coinData == null)
            {
                Debug.LogWarning("[CoinManager] Cannot set null target");
                return false;
            }
            
            // Clear existing target first
            if (HasTarget)
            {
                ClearTarget();
            }
            
            Log($"Setting target coin: {coinData.id}, Value: {coinData.GetDisplayValue()}");
            
            // Store target data
            TargetCoinData = coinData;
            
            // Spawn the coin in AR
            TargetCoin = SpawnTargetCoin(coinData);
            
            if (TargetCoin == null)
            {
                Debug.LogError("[CoinManager] Failed to spawn target coin");
                TargetCoinData = null;
                return false;
            }
            
            // Enter hunting mode
            SetHuntMode(HuntMode.Hunting);
            
            // Notify
            OnTargetSet?.Invoke(coinData);
            OnCoinSelectionChanged?.Invoke(TargetCoin);
            OnNearestCoinChanged?.Invoke(TargetCoin);
            
            return true;
        }
        
        /// <summary>
        /// Set target by coin ID (looks up from known coins)
        /// </summary>
        public bool SetTargetCoinById(string coinId)
        {
            Coin coin = GetKnownCoinById(coinId);
            if (coin == null)
            {
                Debug.LogWarning($"[CoinManager] Coin not found: {coinId}");
                return false;
            }
            return SetTargetCoin(coin);
        }
        
        /// <summary>
        /// Clear the current target (despawn AR coin)
        /// </summary>
        public void ClearTarget()
        {
            if (TargetCoin != null)
            {
                DespawnTargetCoin();
            }
            
            TargetCoinData = null;
            
            Log("Target cleared");
            
            OnTargetCleared?.Invoke();
            OnCoinSelectionChanged?.Invoke(null);
            OnNearestCoinChanged?.Invoke(null);
        }
        
        /// <summary>
        /// Spawn the target coin as an AR object
        /// </summary>
        private CoinController SpawnTargetCoin(Coin coinData)
        {
            // Get or create coin object
            CoinController coin = GetCoinFromPool();
            if (coin == null)
            {
                Debug.LogError("[CoinManager] Failed to get coin from pool");
                return null;
            }
            
            // Configure
            coin.transform.SetParent(coinsParent);
            coin.gameObject.SetActive(true);
            
            // Initialize with data
            bool isLocked = CheckIfLocked(coinData);
            coin.Initialize(coinData, isLocked);
            
            // Subscribe to events
            coin.OnCollected += HandleTargetCollected;
            coin.OnLockedTap += HandleLockedCoinTap;
            coin.OnOutOfRangeTap += HandleOutOfRangeTap;
            coin.OnEnteredRange += HandleCoinEnteredRange;
            coin.OnMaterialized += HandleCoinMaterialized;
            
            Log($"Spawned TARGET coin: {coinData.id}, Value: {coinData.GetDisplayValue()}");
            
            OnCoinSpawned?.Invoke(coin);
            
            return coin;
        }
        
        /// <summary>
        /// Despawn the target coin
        /// </summary>
        private void DespawnTargetCoin()
        {
            if (TargetCoin == null) return;
            
            string coinId = TargetCoin.CoinId;
            
            // Unsubscribe
            TargetCoin.OnCollected -= HandleTargetCollected;
            TargetCoin.OnLockedTap -= HandleLockedCoinTap;
            TargetCoin.OnOutOfRangeTap -= HandleOutOfRangeTap;
            TargetCoin.OnEnteredRange -= HandleCoinEnteredRange;
            TargetCoin.OnMaterialized -= HandleCoinMaterialized;
            
            // Return to pool
            ReturnCoinToPool(TargetCoin);
            
            Log($"Despawned target coin: {coinId}");
            
            OnCoinDespawned?.Invoke(TargetCoin);
            
            TargetCoin = null;
        }
        
        #endregion
        
        #region Legacy Compatibility Methods
        
        /// <summary>
        /// Legacy: Spawn a coin (now sets as target)
        /// </summary>
        public CoinController SpawnCoin(Coin coinData)
        {
            if (SetTargetCoin(coinData))
            {
                return TargetCoin;
            }
            return null;
        }
        
        /// <summary>
        /// Legacy: Despawn coin by ID
        /// </summary>
        public void DespawnCoin(string coinId)
        {
            if (TargetCoin != null && TargetCoin.CoinId == coinId)
            {
                ClearTarget();
            }
        }
        
        /// <summary>
        /// Legacy: Despawn a coin controller
        /// </summary>
        public void DespawnCoin(CoinController coin)
        {
            if (coin == TargetCoin)
            {
                ClearTarget();
            }
        }
        
        /// <summary>
        /// Legacy: Despawn all coins
        /// </summary>
        public void DespawnAllCoins()
        {
            ClearTarget();
            Log("All coins despawned");
        }
        
        /// <summary>
        /// Legacy: Select a coin (now sets as target)
        /// </summary>
        public void SelectCoin(CoinController coin)
        {
            if (coin?.CoinData != null)
            {
                SetTargetCoin(coin.CoinData);
            }
        }
        
        /// <summary>
        /// Legacy: Clear selection
        /// </summary>
        public void ClearSelection()
        {
            ClearTarget();
        }
        
        #endregion
        
        #region Object Pool
        
        private CoinController GetCoinFromPool()
        {
            CoinController coin;
            
            if (coinPool.Count > 0)
            {
                coin = coinPool.Dequeue();
            }
            else
            {
                // Create new
                if (coinPrefab != null)
                {
                    GameObject coinObj = Instantiate(coinPrefab);
                    coin = coinObj.GetComponent<CoinController>();
                    
                    if (coin == null)
                    {
                        coin = coinObj.AddComponent<CoinController>();
                    }
                }
                else
                {
                    // Create default coin
                    GameObject coinObj = CreateDefaultCoinObject();
                    coin = coinObj.GetComponent<CoinController>();
                }
            }
            
            return coin;
        }
        
        private void ReturnCoinToPool(CoinController coin)
        {
            coin.gameObject.SetActive(false);
            coin.transform.SetParent(coinsParent);
            coinPool.Enqueue(coin);
        }
        
        /// <summary>
        /// Create a default coin object with new components
        /// </summary>
        private GameObject CreateDefaultCoinObject()
        {
            GameObject coin = new GameObject("Coin");
            
            // Create visual - gold sphere
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "CoinModel";
            visual.transform.SetParent(coin.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); // 30cm diameter
            
            // Gold unlit material (mobile compatible)
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Standard");
                
                renderer.material = new Material(shader);
                renderer.material.color = DisplaySettings.goldColor;
            }
            
            // Remove visual's collider
            Collider visualCol = visual.GetComponent<Collider>();
            if (visualCol != null) Destroy(visualCol);
            
            // Add collider to parent
            SphereCollider col = coin.AddComponent<SphereCollider>();
            col.radius = 0.2f;
            
            // Add new components (CoinController will add required components in Awake)
            coin.AddComponent<CoinController>();
            
            // Tag
            coin.tag = "Coin";
            
            Log("Created default coin with new architecture");
            
            return coin;
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle target coin collected
        /// </summary>
        private void HandleTargetCollected(CoinController coin)
        {
            float value = coin.CoinData?.value ?? 0f;
            string coinId = coin.CoinId;
            
            Log($"TARGET COIN COLLECTED! Value: ${value:F2}");
            
            // Set collecting mode
            SetHuntMode(HuntMode.Collecting);
            
            // Add to wallet
            if (PlayerData.Exists)
            {
                PlayerData.Instance.AddPendingCoins(value, coinId);
            }
            
            // Remove from known coins
            RemoveKnownCoin(coinId);
            
            // Notify
            OnTargetCollected?.Invoke(TargetCoinData, value);
            OnCoinCollected?.Invoke(coin, value);
            
            // Return to pool after delay, then go back to map view
            StartCoroutine(HandleCollectionComplete(coin, 1.5f));
        }
        
        private System.Collections.IEnumerator HandleCollectionComplete(CoinController coin, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Clean up target
            if (TargetCoin == coin)
            {
                ReturnCoinToPool(coin);
                TargetCoin = null;
                TargetCoinData = null;
            }
            
            // Return to map view
            SetHuntMode(HuntMode.MapView);
            
            OnTargetCleared?.Invoke();
        }
        
        private void HandleLockedCoinTap(CoinController coin)
        {
            Log($"Locked coin tapped: {coin.CoinData?.GetDisplayValue()}");
            // TODO: Show locked popup via UIManager
        }
        
        private void HandleOutOfRangeTap(CoinController coin)
        {
            Log($"Out of range tap: {coin.DistanceFromPlayer:F1}m away");
            // TODO: Show distance message via UIManager
        }
        
        private void HandleCoinEnteredRange(CoinController coin)
        {
            Log($"Target coin entered range: {coin.CoinId}");
            // Trigger haptic feedback when coin enters collection range
            if (HapticService.Instance != null)
            {
                HapticService.Instance.TriggerCollectionFeedback();
            }
        }
        
        /// <summary>
        /// Handle coin materialization (Pokemon GO pattern).
        /// Called when player gets close enough and coin appears in AR.
        /// </summary>
        private void HandleCoinMaterialized(CoinController coin)
        {
            Log($"Target coin MATERIALIZED! {coin.CoinId}");
            
            // Trigger haptic feedback for materialization
            if (HapticService.Instance != null)
            {
                HapticService.Instance.StartProximityFeedback(ProximityZone.Near);
            }
            
            // Notify UI
            OnCoinMaterialized?.Invoke(coin);
        }
        
        #endregion
        
        #region Coin Lookup (Legacy + New)
        
        /// <summary>Get active coin by ID (legacy - returns target if matches)</summary>
        public CoinController GetCoinById(string coinId)
        {
            if (TargetCoin != null && TargetCoin.CoinId == coinId)
            {
                return TargetCoin;
            }
            return null;
        }
        
        /// <summary>Get coins within distance (legacy - returns target if in range)</summary>
        public List<CoinController> GetCoinsWithinDistance(float maxDistance)
        {
            List<CoinController> result = new List<CoinController>();
            if (TargetCoin != null && TargetCoin.DistanceFromPlayer <= maxDistance)
            {
                result.Add(TargetCoin);
            }
            return result;
        }
        
        /// <summary>Get collectible coins (legacy - returns target if collectible)</summary>
        public List<CoinController> GetCollectibleCoins()
        {
            List<CoinController> result = new List<CoinController>();
            if (TargetCoin != null && TargetCoin.IsInRange && !TargetCoin.IsLocked && !TargetCoin.IsCollected)
            {
                result.Add(TargetCoin);
            }
            return result;
        }
        
        /// <summary>
        /// Get known coins within a distance (for map display)
        /// </summary>
        public List<Coin> GetKnownCoinsWithinDistance(float maxDistance, double playerLat, double playerLng)
        {
            List<Coin> result = new List<Coin>();
            foreach (var coin in KnownCoins)
            {
                float distance = (float)GeoUtils.CalculateDistance(playerLat, playerLng, coin.latitude, coin.longitude);
                if (distance <= maxDistance)
                {
                    result.Add(coin);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Get the nearest known coin (for quick select)
        /// </summary>
        public Coin GetNearestKnownCoin(double playerLat, double playerLng)
        {
            Coin nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var coin in KnownCoins)
            {
                float distance = (float)GeoUtils.CalculateDistance(playerLat, playerLng, coin.latitude, coin.longitude);
                if (distance < nearestDist)
                {
                    nearestDist = distance;
                    nearest = coin;
                }
            }
            
            return nearest;
        }
        
        #endregion
        
        #region Utility
        
        private bool CheckIfLocked(Coin coinData)
        {
            if (coinData == null) return false;
            float playerLimit = PlayerData.Exists ? PlayerData.Instance.FindLimit : 1.00f;
            return coinData.value > playerLimit;
        }
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[CoinManager] {message}");
            }
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Print Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== CoinManager Status (SINGLE-TARGET) ===");
            Debug.Log($"Hunt Mode: {CurrentMode}");
            Debug.Log($"Known Coins: {KnownCoinCount}");
            Debug.Log($"Total Known Value: ${TotalKnownValue:F2}");
            Debug.Log($"---");
            Debug.Log($"Has Target: {HasTarget}");
            if (HasTarget)
            {
                Debug.Log($"Target ID: {TargetCoinData.id}");
                Debug.Log($"Target Value: {TargetCoinData.GetDisplayValue()}");
                Debug.Log($"Target Distance: {TargetCoin?.DistanceFromPlayer:F1}m");
                Debug.Log($"Target Mode: {TargetCoin?.DisplayMode}");
                Debug.Log($"Target InRange: {TargetCoin?.IsInRange}");
            }
            Debug.Log("==========================================");
        }
        
        [ContextMenu("Debug: Print Known Coins")]
        public void DebugPrintKnownCoins()
        {
            Debug.Log($"=== Known Coins ({KnownCoinCount}) ===");
            foreach (var coin in KnownCoins)
            {
                string lockStatus = coin.isLocked ? "🔒" : "🔓";
                Debug.Log($"  {lockStatus} {coin.id}: {coin.GetDisplayValue()} @ ({coin.latitude:F4}, {coin.longitude:F4})");
            }
            Debug.Log("=====================================");
        }
        
        [ContextMenu("Debug: Add Test Known Coins")]
        public void DebugAddTestKnownCoins()
        {
            // Get player location
            double playerLat = 37.7749;
            double playerLng = -122.4194;
            
            if (GPSManager.Exists && GPSManager.Instance.CurrentLocation != null)
            {
                playerLat = GPSManager.Instance.CurrentLocation.latitude;
                playerLng = GPSManager.Instance.CurrentLocation.longitude;
            }
            
            // Create test coins at various distances and directions
            List<Coin> testCoins = new List<Coin>();
            float[] distances = { 15f, 30f, 50f, 80f, 120f };
            float[] bearings = { 0f, 72f, 144f, 216f, 288f };
            float[] values = { 1f, 2f, 5f, 10f, 25f };
            
            for (int i = 0; i < 5; i++)
            {
                // Calculate offset lat/lng
                double latOffset = distances[i] * Math.Cos(bearings[i] * Math.PI / 180) / 111320.0;
                double lngOffset = distances[i] * Math.Sin(bearings[i] * Math.PI / 180) / (111320.0 * Math.Cos(playerLat * Math.PI / 180));
                
                Coin testCoin = Coin.CreateTestCoin(values[i]);
                testCoin.latitude = playerLat + latOffset;
                testCoin.longitude = playerLng + lngOffset;
                testCoin.id = $"test-{i}-{DateTime.Now.Ticks}";
                
                testCoins.Add(testCoin);
            }
            
            SetKnownCoins(testCoins);
            Debug.Log($"[CoinManager] Added {testCoins.Count} test known coins around player");
        }
        
        [ContextMenu("Debug: Set First Known As Target")]
        public void DebugSetFirstAsTarget()
        {
            if (KnownCoins.Count > 0)
            {
                SetTargetCoin(KnownCoins[0]);
            }
            else
            {
                Debug.LogWarning("[CoinManager] No known coins to set as target");
            }
        }
        
        [ContextMenu("Debug: Clear Target")]
        public void DebugClearTarget()
        {
            ClearTarget();
        }
        
        #endregion
    }
}
