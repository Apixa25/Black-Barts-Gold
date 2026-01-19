// ============================================================================
// CoinManager.cs
// Black Bart's Gold - Coin Management System
// Path: Assets/Scripts/AR/CoinManager.cs
// ============================================================================
// Singleton that manages all active coins in the scene. Handles spawning,
// despawning, tracking, and coordination with the targeting system.
// Reference: BUILD-GUIDE.md Prompt 3.3
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
        
        /// <summary>
        /// Singleton instance
        /// </summary>
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
        
        [Header("Prefabs")]
        [SerializeField]
        [Tooltip("Coin prefab to instantiate")]
        private GameObject coinPrefab;
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Maximum coins to render at once")]
        private int maxActiveCoins = 20;
        
        [SerializeField]
        [Tooltip("Maximum render distance in meters")]
        private float maxRenderDistance = 100f;
        
        [SerializeField]
        [Tooltip("Update coin positions every X seconds")]
        private float positionUpdateInterval = 1f;
        
        [SerializeField]
        [Tooltip("Parent transform for spawned coins")]
        private Transform coinsParent;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// All currently active coins
        /// </summary>
        public List<CoinController> ActiveCoins { get; private set; } = new List<CoinController>();
        
        /// <summary>
        /// Number of active coins
        /// </summary>
        public int ActiveCoinCount => ActiveCoins.Count;
        
        /// <summary>
        /// Currently selected/hovered coin
        /// </summary>
        public CoinController SelectedCoin { get; private set; }
        
        /// <summary>
        /// Nearest coin to player
        /// </summary>
        public CoinController NearestCoin { get; private set; }
        
        /// <summary>
        /// Total value of all active coins
        /// </summary>
        public float TotalActiveValue { get; private set; } = 0f;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when a coin is spawned
        /// </summary>
        public event Action<CoinController> OnCoinSpawned;
        
        /// <summary>
        /// Fired when a coin is despawned
        /// </summary>
        public event Action<CoinController> OnCoinDespawned;
        
        /// <summary>
        /// Fired when a coin is collected
        /// </summary>
        public event Action<CoinController, float> OnCoinCollected;
        
        /// <summary>
        /// Fired when a coin selection changes
        /// </summary>
        public event Action<CoinController> OnCoinSelectionChanged;
        
        /// <summary>
        /// Fired when nearest coin changes
        /// </summary>
        public event Action<CoinController> OnNearestCoinChanged;
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<string, CoinController> coinLookup = new Dictionary<string, CoinController>();
        private float lastPositionUpdate = 0f;
        private List<Coin> pendingCoins = new List<Coin>(); // Coins waiting to be spawned
        private Queue<CoinController> coinPool = new Queue<CoinController>(); // Object pool
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // Create coins parent if needed
            if (coinsParent == null)
            {
                GameObject parent = new GameObject("Coins");
                coinsParent = parent.transform;
            }
        }
        
        private void Start()
        {
            // Subscribe to raycast events for selection
            if (ARRaycastController.Instance != null)
            {
                ARRaycastController.Instance.OnCoinHovered += OnCoinHovered;
                ARRaycastController.Instance.OnCoinUnhovered += OnCoinUnhovered;
                ARRaycastController.Instance.OnCoinSelected += OnCoinSelectedByRaycast;
            }
        }
        
        private void OnDestroy()
        {
            if (ARRaycastController.Instance != null)
            {
                ARRaycastController.Instance.OnCoinHovered -= OnCoinHovered;
                ARRaycastController.Instance.OnCoinUnhovered -= OnCoinUnhovered;
                ARRaycastController.Instance.OnCoinSelected -= OnCoinSelectedByRaycast;
            }
        }
        
        private void Update()
        {
            // Periodic position updates
            if (Time.time - lastPositionUpdate >= positionUpdateInterval)
            {
                lastPositionUpdate = Time.time;
                UpdateCoinDistances();
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
            if (coins == null) return;
            
            // Find coins to despawn (no longer in list)
            List<string> toRemove = new List<string>();
            foreach (var kvp in coinLookup)
            {
                if (!coins.Exists(c => c.id == kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            // Despawn removed coins
            foreach (string id in toRemove)
            {
                DespawnCoin(id);
            }
            
            // Spawn new coins
            foreach (var coin in coins)
            {
                if (!coinLookup.ContainsKey(coin.id))
                {
                    SpawnCoin(coin);
                }
            }
            
            Log($"SetNearbyCoins: {coins.Count} coins, {ActiveCoinCount} active");
        }
        
        /// <summary>
        /// Spawn a coin at its world position
        /// </summary>
        public CoinController SpawnCoin(Coin coinData)
        {
            if (coinData == null)
            {
                Debug.LogWarning("[CoinManager] Cannot spawn null coin data");
                return null;
            }
            
            // Check if already exists
            if (coinLookup.ContainsKey(coinData.id))
            {
                Log($"Coin {coinData.id} already exists");
                return coinLookup[coinData.id];
            }
            
            // Check max coins limit
            if (ActiveCoins.Count >= maxActiveCoins)
            {
                Log($"Max coins reached ({maxActiveCoins}), cannot spawn more");
                return null;
            }
            
            // Get or create coin object
            CoinController coin = GetCoinFromPool();
            
            if (coin == null)
            {
                Debug.LogError("[CoinManager] Failed to get coin from pool");
                return null;
            }
            
            // Position will be set by CoinSpawner based on GPS
            // For now, use a placeholder
            coin.transform.SetParent(coinsParent);
            coin.gameObject.SetActive(true);
            
            // Initialize with data
            bool isLocked = CheckIfLocked(coinData);
            bool isInRange = false; // Will be updated by distance check
            coin.Initialize(coinData, isLocked, isInRange);
            
            // Play appear animation
            coin.PlayAppearAnimation();
            
            // Subscribe to coin events
            coin.OnCollected += HandleCoinCollected;
            coin.OnLockedTap += HandleLockedCoinTap;
            coin.OnOutOfRangeTap += HandleOutOfRangeTap;
            
            // Track
            ActiveCoins.Add(coin);
            coinLookup[coinData.id] = coin;
            UpdateTotalValue();
            
            Log($"Spawned coin: {coinData.id}, Value: {coinData.GetDisplayValue()}");
            
            OnCoinSpawned?.Invoke(coin);
            
            return coin;
        }
        
        /// <summary>
        /// Spawn a coin at a specific AR position (for testing)
        /// </summary>
        public CoinController SpawnCoinAtPosition(Coin coinData, Vector3 arPosition)
        {
            CoinController coin = SpawnCoin(coinData);
            
            if (coin != null)
            {
                coin.transform.position = arPosition;
            }
            
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
        
        /// <summary>
        /// Get a coin from the pool or create new
        /// </summary>
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
                if (coinPrefab == null)
                {
                    // Create primitive if no prefab
                    GameObject coinObj = CreateDefaultCoinObject();
                    coin = coinObj.GetComponent<CoinController>();
                }
                else
                {
                    GameObject coinObj = Instantiate(coinPrefab);
                    coin = coinObj.GetComponent<CoinController>();
                    
                    if (coin == null)
                    {
                        coin = coinObj.AddComponent<CoinController>();
                    }
                }
            }
            
            return coin;
        }
        
        /// <summary>
        /// Return a coin to the pool
        /// </summary>
        private void ReturnCoinToPool(CoinController coin)
        {
            coin.gameObject.SetActive(false);
            coin.transform.SetParent(coinsParent);
            coinPool.Enqueue(coin);
        }
        
        /// <summary>
        /// Create a default coin object (no prefab)
        /// </summary>
        private GameObject CreateDefaultCoinObject()
        {
            GameObject coin = new GameObject("Coin");
            
            // Create visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "CoinModel";
            visual.transform.SetParent(coin.transform);
            visual.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            visual.transform.localRotation = Quaternion.Euler(90, 0, 0);
            
            // Gold material
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(1f, 0.84f, 0f); // Gold
                renderer.material.SetFloat("_Metallic", 0.8f);
                renderer.material.SetFloat("_Smoothness", 0.7f);
            }
            
            // Remove collider from visual
            Collider visualCol = visual.GetComponent<Collider>();
            if (visualCol != null) Destroy(visualCol);
            
            // Add sphere collider to parent
            SphereCollider col = coin.AddComponent<SphereCollider>();
            col.radius = 0.2f;
            
            // Add CoinController
            coin.AddComponent<CoinController>();
            
            return coin;
        }
        
        #endregion
        
        #region Coin Lookup
        
        /// <summary>
        /// Get coin by ID
        /// </summary>
        public CoinController GetCoinById(string coinId)
        {
            coinLookup.TryGetValue(coinId, out CoinController coin);
            return coin;
        }
        
        /// <summary>
        /// Get coins within a certain distance
        /// </summary>
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
        
        /// <summary>
        /// Get collectible coins (in range and not locked)
        /// </summary>
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
        
        /// <summary>
        /// Handle coin hover from raycast system
        /// </summary>
        private void OnCoinHovered(GameObject coinObj)
        {
            CoinController coin = coinObj.GetComponent<CoinController>();
            if (coin == null) return;
            
            if (SelectedCoin != coin)
            {
                // Unhover previous
                if (SelectedCoin != null)
                {
                    SelectedCoin.OnUnhover();
                }
                
                // Hover new
                SelectedCoin = coin;
                coin.OnHover();
                
                OnCoinSelectionChanged?.Invoke(coin);
            }
        }
        
        /// <summary>
        /// Handle coin unhover
        /// </summary>
        private void OnCoinUnhovered()
        {
            if (SelectedCoin != null)
            {
                SelectedCoin.OnUnhover();
                SelectedCoin = null;
                
                OnCoinSelectionChanged?.Invoke(null);
            }
        }
        
        /// <summary>
        /// Handle coin selection (tap)
        /// </summary>
        private void OnCoinSelectedByRaycast(GameObject coinObj)
        {
            CoinController coin = coinObj.GetComponent<CoinController>();
            if (coin == null) return;
            
            // Try to collect
            coin.TryCollect();
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle coin collected
        /// </summary>
        private void HandleCoinCollected(CoinController coin)
        {
            float value = coin.CoinData?.value ?? 0f;
            
            Log($"Coin collected! Value: ${value:F2}");
            
            // Add to player wallet (pending)
            if (PlayerData.Exists)
            {
                PlayerData.Instance.AddPendingCoins(value, coin.CoinId);
            }
            
            // Notify listeners
            OnCoinCollected?.Invoke(coin, value);
            
            // Remove from tracking (coin will be deactivated by controller)
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
        
        /// <summary>
        /// Handle locked coin tap
        /// </summary>
        private void HandleLockedCoinTap(CoinController coin)
        {
            Log($"Locked coin tapped: {coin.CoinData?.GetDisplayValue()}");
            
            // TODO: Show locked coin popup
            // UIManager.Instance.ShowLockedCoinPopup(coin);
        }
        
        /// <summary>
        /// Handle out of range tap
        /// </summary>
        private void HandleOutOfRangeTap(CoinController coin)
        {
            Log($"Out of range tap: {coin.DistanceFromPlayer:F1}m away");
            
            // TODO: Show distance message
            // UIManager.Instance.ShowMessage($"Get closer! {coin.DistanceFromPlayer:F0}m away");
        }
        
        #endregion
        
        #region Distance Updates
        
        /// <summary>
        /// Update distances for all coins
        /// </summary>
        private void UpdateCoinDistances()
        {
            foreach (var coin in ActiveCoins)
            {
                // Distance is updated in CoinController.Update()
                // Here we can do batch operations if needed
            }
        }
        
        /// <summary>
        /// Find and update nearest coin
        /// </summary>
        private void UpdateNearestCoin()
        {
            CoinController nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var coin in ActiveCoins)
            {
                if (coin.IsCollected) continue;
                
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
        
        /// <summary>
        /// Check if coin should be locked for current player
        /// </summary>
        private bool CheckIfLocked(Coin coinData)
        {
            if (coinData == null) return false;
            
            float playerLimit = PlayerData.Exists ? PlayerData.Instance.FindLimit : 1.00f;
            return coinData.value > playerLimit;
        }
        
        /// <summary>
        /// Update total value of active coins
        /// </summary>
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
        
        /// <summary>
        /// Debug: Print all active coins
        /// </summary>
        [ContextMenu("Debug: Print Active Coins")]
        public void DebugPrintActiveCoins()
        {
            Debug.Log($"=== Active Coins ({ActiveCoinCount}) ===");
            foreach (var coin in ActiveCoins)
            {
                Debug.Log($"  - {coin.CoinId}: {coin.CoinData?.GetDisplayValue()} @ {coin.DistanceFromPlayer:F1}m");
            }
            Debug.Log($"Total Value: ${TotalActiveValue:F2}");
            Debug.Log("================================");
        }
        
        /// <summary>
        /// Debug: Spawn test coins
        /// </summary>
        [ContextMenu("Debug: Spawn Test Coins")]
        public void DebugSpawnTestCoins()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            
            // Spawn 5 test coins in front of camera
            for (int i = 0; i < 5; i++)
            {
                Coin testCoin = Coin.CreateTestCoin((i + 1) * 1.00f);
                
                Vector3 pos = cam.transform.position + cam.transform.forward * (3 + i);
                pos += cam.transform.right * (i - 2) * 1.5f; // Spread horizontally
                pos.y = cam.transform.position.y;
                
                SpawnCoinAtPosition(testCoin, pos);
            }
            
            Debug.Log("[CoinManager] Spawned 5 test coins");
        }
        
        #endregion
    }
}
