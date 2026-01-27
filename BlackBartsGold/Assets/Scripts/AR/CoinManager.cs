// ============================================================================
// CoinManager.cs
// Black Bart's Gold - Coin Management System (New Architecture)
// Path: Assets/Scripts/AR/CoinManager.cs
// ============================================================================
// Manages all active coins in the scene. Uses new ARCoinRenderer and
// ARCoinPositioner components for distance-adaptive display.
// Reference: Docs/AR-COIN-DISPLAY-SPEC.md
// ============================================================================

using UnityEngine;
using System;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Manages all active coins in the AR scene.
    /// Singleton that handles spawning, tracking, and cleanup.
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
        [Tooltip("Maximum coins to render at once")]
        private int maxActiveCoins = 20;
        
        [SerializeField]
        [Tooltip("Parent transform for spawned coins")]
        private Transform coinsParent;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>All currently active coins</summary>
        public List<CoinController> ActiveCoins { get; private set; } = new List<CoinController>();
        
        /// <summary>Number of active coins</summary>
        public int ActiveCoinCount => ActiveCoins.Count;
        
        /// <summary>Currently selected coin</summary>
        public CoinController SelectedCoin { get; private set; }
        
        /// <summary>Nearest coin to player</summary>
        public CoinController NearestCoin { get; private set; }
        
        /// <summary>Total value of all active coins</summary>
        public float TotalActiveValue { get; private set; } = 0f;
        
        /// <summary>Display settings in use</summary>
        public CoinDisplaySettings DisplaySettings => displaySettings ?? CoinDisplaySettings.Default;
        
        #endregion
        
        #region Events
        
        public event Action<CoinController> OnCoinSpawned;
        public event Action<CoinController> OnCoinDespawned;
        public event Action<CoinController, float> OnCoinCollected;
        public event Action<CoinController> OnCoinSelectionChanged;
        public event Action<CoinController> OnNearestCoinChanged;
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<string, CoinController> coinLookup = new Dictionary<string, CoinController>();
        private Queue<CoinController> coinPool = new Queue<CoinController>();
        private float lastNearestUpdate = 0f;
        private const float NEAREST_UPDATE_INTERVAL = 0.5f;
        
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
            
            Log("CoinManager initialized with new architecture");
        }
        
        private void Start()
        {
            // Initialize compass heading for positioning
            ARCoinPositioner.CaptureInitialCompassHeading();
        }
        
        private void Update()
        {
            // Periodic nearest coin update
            if (Time.time - lastNearestUpdate >= NEAREST_UPDATE_INTERVAL)
            {
                lastNearestUpdate = Time.time;
                UpdateNearestCoin();
            }
        }
        
        #endregion
        
        #region Coin Spawning
        
        /// <summary>
        /// Set nearby coins from server/API data
        /// </summary>
        public void SetNearbyCoins(List<Coin> coins)
        {
            Log($"SetNearbyCoins called with {coins?.Count ?? 0} coins");
            
            if (coins == null || coins.Count == 0)
            {
                Log("No coins to spawn");
                return;
            }
            
            // Find coins to despawn
            List<string> toRemove = new List<string>();
            foreach (var kvp in coinLookup)
            {
                if (!coins.Exists(c => c.id == kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            // Despawn removed
            foreach (string id in toRemove)
            {
                DespawnCoin(id);
            }
            
            // Spawn new
            int spawnedCount = 0;
            foreach (var coin in coins)
            {
                if (!coinLookup.ContainsKey(coin.id))
                {
                    var spawned = SpawnCoin(coin);
                    if (spawned != null) spawnedCount++;
                }
            }
            
            Log($"SetNearbyCoins complete: {spawnedCount} new, {ActiveCoinCount} total");
        }
        
        /// <summary>
        /// Spawn a coin with the new architecture
        /// </summary>
        public CoinController SpawnCoin(Coin coinData)
        {
            if (coinData == null)
            {
                Debug.LogWarning("[CoinManager] Cannot spawn null coin");
                return null;
            }
            
            // Check if exists
            if (coinLookup.ContainsKey(coinData.id))
            {
                Log($"Coin {coinData.id} already exists");
                return coinLookup[coinData.id];
            }
            
            // Check limit
            if (ActiveCoins.Count >= maxActiveCoins)
            {
                Log($"Max coins reached ({maxActiveCoins})");
                return null;
            }
            
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
            coin.OnCollected += HandleCoinCollected;
            coin.OnLockedTap += HandleLockedCoinTap;
            coin.OnOutOfRangeTap += HandleOutOfRangeTap;
            coin.OnEnteredRange += HandleCoinEnteredRange;
            
            // Track
            ActiveCoins.Add(coin);
            coinLookup[coinData.id] = coin;
            UpdateTotalValue();
            
            Log($"Spawned coin: {coinData.id}, Value: {coinData.GetDisplayValue()}");
            
            OnCoinSpawned?.Invoke(coin);
            
            return coin;
        }
        
        /// <summary>
        /// Despawn a coin by ID
        /// </summary>
        public void DespawnCoin(string coinId)
        {
            if (!coinLookup.TryGetValue(coinId, out CoinController coin))
            {
                return;
            }
            DespawnCoin(coin);
        }
        
        /// <summary>
        /// Despawn a coin
        /// </summary>
        public void DespawnCoin(CoinController coin)
        {
            if (coin == null) return;
            
            string coinId = coin.CoinId;
            
            // Unsubscribe
            coin.OnCollected -= HandleCoinCollected;
            coin.OnLockedTap -= HandleLockedCoinTap;
            coin.OnOutOfRangeTap -= HandleOutOfRangeTap;
            coin.OnEnteredRange -= HandleCoinEnteredRange;
            
            // Remove from tracking
            ActiveCoins.Remove(coin);
            coinLookup.Remove(coinId);
            
            // Return to pool
            ReturnCoinToPool(coin);
            
            UpdateTotalValue();
            Log($"Despawned coin: {coinId}");
            
            OnCoinDespawned?.Invoke(coin);
        }
        
        /// <summary>
        /// Despawn all coins
        /// </summary>
        public void DespawnAllCoins()
        {
            List<CoinController> toRemove = new List<CoinController>(ActiveCoins);
            foreach (var coin in toRemove)
            {
                DespawnCoin(coin);
            }
            Log("All coins despawned");
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
        
        #region Coin Lookup
        
        /// <summary>Get coin by ID</summary>
        public CoinController GetCoinById(string coinId)
        {
            coinLookup.TryGetValue(coinId, out CoinController coin);
            return coin;
        }
        
        /// <summary>Get coins within distance</summary>
        public List<CoinController> GetCoinsWithinDistance(float maxDistance)
        {
            List<CoinController> result = new List<CoinController>();
            foreach (var coin in ActiveCoins)
            {
                if (coin.DistanceFromPlayer <= maxDistance)
                {
                    result.Add(coin);
                }
            }
            return result;
        }
        
        /// <summary>Get collectible coins</summary>
        public List<CoinController> GetCollectibleCoins()
        {
            List<CoinController> result = new List<CoinController>();
            foreach (var coin in ActiveCoins)
            {
                if (coin.IsInRange && !coin.IsLocked && !coin.IsCollected)
                {
                    result.Add(coin);
                }
            }
            return result;
        }
        
        #endregion
        
        #region Selection
        
        /// <summary>Select a coin</summary>
        public void SelectCoin(CoinController coin)
        {
            if (SelectedCoin != coin)
            {
                SelectedCoin = coin;
                OnCoinSelectionChanged?.Invoke(coin);
            }
        }
        
        /// <summary>Clear selection</summary>
        public void ClearSelection()
        {
            if (SelectedCoin != null)
            {
                SelectedCoin = null;
                OnCoinSelectionChanged?.Invoke(null);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleCoinCollected(CoinController coin)
        {
            float value = coin.CoinData?.value ?? 0f;
            Log($"Coin collected! Value: ${value:F2}");
            
            // Add to wallet
            if (PlayerData.Exists)
            {
                PlayerData.Instance.AddPendingCoins(value, coin.CoinId);
            }
            
            OnCoinCollected?.Invoke(coin, value);
            
            // Remove from tracking
            ActiveCoins.Remove(coin);
            coinLookup.Remove(coin.CoinId);
            UpdateTotalValue();
            
            // Return to pool after delay
            StartCoroutine(ReturnToPoolDelayed(coin, 1f));
        }
        
        private System.Collections.IEnumerator ReturnToPoolDelayed(CoinController coin, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnCoinToPool(coin);
        }
        
        private void HandleLockedCoinTap(CoinController coin)
        {
            Log($"Locked coin tapped: {coin.CoinData?.GetDisplayValue()}");
            // TODO: Show locked popup
        }
        
        private void HandleOutOfRangeTap(CoinController coin)
        {
            Log($"Out of range tap: {coin.DistanceFromPlayer:F1}m away");
            // TODO: Show distance message
        }
        
        private void HandleCoinEnteredRange(CoinController coin)
        {
            Log($"Coin entered range: {coin.CoinId}");
            // Could trigger haptic feedback here
        }
        
        #endregion
        
        #region Updates
        
        private void UpdateNearestCoin()
        {
            CoinController nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var coin in ActiveCoins)
            {
                if (coin.IsCollected) continue;
                if (!coin.IsVisible) continue;
                
                if (coin.DistanceFromPlayer < nearestDist)
                {
                    nearestDist = coin.DistanceFromPlayer;
                    nearest = coin;
                }
            }
            
            if (nearest != NearestCoin)
            {
                NearestCoin = nearest;
                OnNearestCoinChanged?.Invoke(nearest);
            }
        }
        
        #endregion
        
        #region Utility
        
        private bool CheckIfLocked(Coin coinData)
        {
            if (coinData == null) return false;
            float playerLimit = PlayerData.Exists ? PlayerData.Instance.FindLimit : 1.00f;
            return coinData.value > playerLimit;
        }
        
        private void UpdateTotalValue()
        {
            float total = 0f;
            foreach (var coin in ActiveCoins)
            {
                if (coin.CoinData != null)
                {
                    total += coin.CoinData.value;
                }
            }
            TotalActiveValue = total;
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
        
        [ContextMenu("Debug: Print Active Coins")]
        public void DebugPrintActiveCoins()
        {
            Debug.Log($"=== Active Coins ({ActiveCoinCount}) ===");
            foreach (var coin in ActiveCoins)
            {
                Debug.Log($"  - {coin.CoinId}: {coin.CoinData?.GetDisplayValue()} @ {coin.DistanceFromPlayer:F1}m ({coin.DisplayMode})");
            }
            Debug.Log($"Total Value: ${TotalActiveValue:F2}");
            Debug.Log("================================");
        }
        
        [ContextMenu("Debug: Spawn Test Coins")]
        public void DebugSpawnTestCoins()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindFirstObjectByType<Camera>();
            }
            if (cam == null)
            {
                Debug.LogWarning("No camera found");
                return;
            }
            
            // Spawn 5 test coins at various distances
            float[] distances = { 5f, 10f, 20f, 35f, 50f };
            float[] values = { 1f, 2f, 5f, 10f, 25f };
            
            for (int i = 0; i < 5; i++)
            {
                Coin testCoin = Coin.CreateTestCoin(values[i], distances[i]);
                
                // Position in front of camera
                Vector3 dir = cam.transform.forward;
                dir.y = 0;
                dir.Normalize();
                
                Vector3 offset = Quaternion.Euler(0, (i - 2) * 20f, 0) * dir * distances[i];
                Vector3 pos = cam.transform.position + offset;
                pos.y = 1f;
                
                var controller = SpawnCoin(testCoin);
                if (controller != null)
                {
                    controller.transform.position = pos;
                }
            }
            
            Debug.Log("[CoinManager] Spawned 5 test coins at 5m, 10m, 20m, 35m, 50m");
        }
        
        #endregion
    }
}
