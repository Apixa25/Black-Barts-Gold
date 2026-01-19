// ============================================================================
// TestCoinSpawner.cs
// Black Bart's Gold - Development Test Spawner
// Path: Assets/Scripts/AR/TestCoinSpawner.cs
// ============================================================================
// Spawns test coins at fixed AR positions for development and testing.
// Does not require GPS - coins appear relative to camera position.
// Reference: BUILD-GUIDE.md Prompt 3.4
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Spawns test coins for development without requiring GPS.
    /// Coins are positioned relative to the camera.
    /// </summary>
    public class TestCoinSpawner : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Auto Spawn Settings")]
        [SerializeField]
        [Tooltip("Automatically spawn test coins on start")]
        private bool autoSpawnOnStart = true;
        
        [SerializeField]
        [Tooltip("Delay before auto-spawning (seconds)")]
        private float autoSpawnDelay = 2f;
        
        [Header("Test Coin Configuration")]
        [SerializeField]
        [Tooltip("Predefined test coins to spawn")]
        private List<TestCoinConfig> testCoins = new List<TestCoinConfig>
        {
            new TestCoinConfig { value = 1.00f, position = new Vector3(0, 1.5f, 3), locked = false },
            new TestCoinConfig { value = 5.00f, position = new Vector3(-2, 1.5f, 4), locked = false },
            new TestCoinConfig { value = 2.50f, position = new Vector3(2, 1.5f, 4), locked = false, isPool = true },
            new TestCoinConfig { value = 10.00f, position = new Vector3(0, 1.5f, 6), locked = false },
            new TestCoinConfig { value = 25.00f, position = new Vector3(-1, 2f, 3), locked = true }
        };
        
        [Header("References")]
        [SerializeField]
        private Camera arCamera;
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Has test spawning occurred?
        /// </summary>
        public bool HasSpawned { get; private set; } = false;
        
        /// <summary>
        /// Spawned test coin controllers
        /// </summary>
        public List<CoinController> SpawnedCoins { get; private set; } = new List<CoinController>();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Find camera
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
            
            // Auto spawn if enabled
            if (autoSpawnOnStart)
            {
                Invoke(nameof(SpawnTestCoins), autoSpawnDelay);
            }
        }
        
        #endregion
        
        #region Spawn Methods
        
        /// <summary>
        /// Spawn all configured test coins
        /// </summary>
        [ContextMenu("Spawn Test Coins")]
        public void SpawnTestCoins()
        {
            if (CoinManager.Instance == null)
            {
                Debug.LogError("[TestCoinSpawner] CoinManager not found!");
                return;
            }
            
            // Clear any existing test coins
            ClearTestCoins();
            
            Log($"Spawning {testCoins.Count} test coins...");
            
            foreach (var config in testCoins)
            {
                SpawnTestCoin(config);
            }
            
            HasSpawned = true;
            Log($"Spawned {SpawnedCoins.Count} test coins");
        }
        
        /// <summary>
        /// Spawn a single test coin from config
        /// </summary>
        private void SpawnTestCoin(TestCoinConfig config)
        {
            // Create coin data
            Coin coinData = new Coin
            {
                id = $"test-{System.Guid.NewGuid().ToString().Substring(0, 8)}",
                coinType = config.isPool ? CoinType.Pool : CoinType.Fixed,
                value = config.value,
                contribution = config.value * 1.1f,
                currentTier = Coin.CalculateTier(config.value),
                status = CoinStatus.Visible,
                huntType = HuntType.Standard,
                multiFind = false,
                findsRemaining = 1,
                maxFinds = 1,
                hiderId = "test-spawner",
                hiderName = "Test Spawner",
                hiddenAt = System.DateTime.UtcNow.ToString("o")
            };
            
            // Calculate world position relative to camera
            Vector3 worldPosition = GetWorldPosition(config.position);
            
            // Spawn through CoinManager
            CoinController coin = CoinManager.Instance.SpawnCoinAtPosition(coinData, worldPosition);
            
            if (coin != null)
            {
                // Apply locked state
                if (config.locked)
                {
                    coin.SetLocked(true);
                }
                
                SpawnedCoins.Add(coin);
                
                Log($"Spawned: ${config.value:F2} at {worldPosition} (locked: {config.locked})");
            }
        }
        
        /// <summary>
        /// Convert local position to world position (relative to camera)
        /// </summary>
        private Vector3 GetWorldPosition(Vector3 localPosition)
        {
            if (arCamera == null)
            {
                return localPosition;
            }
            
            // Position is relative to camera's forward direction
            return arCamera.transform.TransformPoint(localPosition);
        }
        
        /// <summary>
        /// Clear all spawned test coins
        /// </summary>
        [ContextMenu("Clear Test Coins")]
        public void ClearTestCoins()
        {
            if (CoinManager.Instance != null)
            {
                foreach (var coin in SpawnedCoins)
                {
                    if (coin != null)
                    {
                        CoinManager.Instance.DespawnCoin(coin);
                    }
                }
            }
            
            SpawnedCoins.Clear();
            HasSpawned = false;
            
            Log("Test coins cleared");
        }
        
        #endregion
        
        #region Quick Spawn Methods
        
        /// <summary>
        /// Spawn a single test coin in front of player
        /// </summary>
        public CoinController SpawnCoinInFront(float value, float distance = 3f, bool locked = false)
        {
            if (arCamera == null || CoinManager.Instance == null) return null;
            
            Vector3 position = arCamera.transform.position + arCamera.transform.forward * distance;
            position.y = arCamera.transform.position.y; // Same height as camera
            
            Coin coinData = Coin.CreateTestCoin(value, distance);
            CoinController coin = CoinManager.Instance.SpawnCoinAtPosition(coinData, position);
            
            if (coin != null && locked)
            {
                coin.SetLocked(true);
            }
            
            if (coin != null)
            {
                SpawnedCoins.Add(coin);
            }
            
            return coin;
        }
        
        /// <summary>
        /// Spawn coins in a line in front of camera
        /// </summary>
        public void SpawnCoinLine(int count, float startDistance, float spacing, float[] values = null)
        {
            if (arCamera == null || CoinManager.Instance == null) return;
            
            for (int i = 0; i < count; i++)
            {
                float distance = startDistance + (i * spacing);
                float value = values != null && i < values.Length ? values[i] : (i + 1) * 1.00f;
                
                Vector3 position = arCamera.transform.position + arCamera.transform.forward * distance;
                position.y = arCamera.transform.position.y;
                
                Coin coinData = Coin.CreateTestCoin(value, distance);
                CoinController coin = CoinManager.Instance.SpawnCoinAtPosition(coinData, position);
                
                if (coin != null)
                {
                    SpawnedCoins.Add(coin);
                }
            }
            
            Log($"Spawned line of {count} coins");
        }
        
        /// <summary>
        /// Spawn coins in a circle around camera
        /// </summary>
        public void SpawnCoinCircle(int count, float radius, float[] values = null)
        {
            if (arCamera == null || CoinManager.Instance == null) return;
            
            float angleStep = 360f / count;
            
            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float value = values != null && i < values.Length ? values[i] : (i + 1) * 1.00f;
                
                Vector3 offset = new Vector3(
                    Mathf.Sin(angle) * radius,
                    0,
                    Mathf.Cos(angle) * radius
                );
                
                Vector3 position = arCamera.transform.position + offset;
                position.y = arCamera.transform.position.y;
                
                Coin coinData = Coin.CreateTestCoin(value, radius);
                CoinController coin = CoinManager.Instance.SpawnCoinAtPosition(coinData, position);
                
                if (coin != null)
                {
                    SpawnedCoins.Add(coin);
                }
            }
            
            Log($"Spawned circle of {count} coins");
        }
        
        /// <summary>
        /// Spawn one coin of each tier for testing visuals
        /// </summary>
        [ContextMenu("Spawn All Tiers")]
        public void SpawnAllTiers()
        {
            float[] values = { 0.50f, 2.00f, 10.00f, 50.00f, 150.00f };
            SpawnCoinCircle(5, 4f, values);
        }
        
        /// <summary>
        /// Spawn mixed locked/unlocked coins
        /// </summary>
        [ContextMenu("Spawn Mixed (Locked + Unlocked)")]
        public void SpawnMixedCoins()
        {
            if (arCamera == null || CoinManager.Instance == null) return;
            
            // Unlocked coins (within default $1 limit)
            SpawnCoinInFront(0.50f, 3f, false);
            SpawnCoinInFront(1.00f, 4f, false);
            
            // Locked coins (above $1 limit)
            SpawnCoinInFront(5.00f, 5f, true);
            SpawnCoinInFront(10.00f, 6f, true);
            
            Log("Spawned 4 mixed coins (2 unlocked, 2 locked)");
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[TestCoinSpawner] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print spawned coins
        /// </summary>
        [ContextMenu("Debug: Print Spawned Coins")]
        public void DebugPrintSpawnedCoins()
        {
            Debug.Log($"=== Test Coins ({SpawnedCoins.Count}) ===");
            foreach (var coin in SpawnedCoins)
            {
                if (coin != null)
                {
                    Debug.Log($"  - {coin.CoinData?.GetDisplayValue() ?? "null"} at {coin.transform.position}");
                }
            }
            Debug.Log("================================");
        }
        
        #endregion
    }
    
    #region Test Coin Configuration
    
    /// <summary>
    /// Configuration for a test coin
    /// </summary>
    [System.Serializable]
    public class TestCoinConfig
    {
        [Tooltip("Value in BBG")]
        public float value = 1.00f;
        
        [Tooltip("Position relative to camera")]
        public Vector3 position = new Vector3(0, 1.5f, 3);
        
        [Tooltip("Is this coin locked?")]
        public bool locked = false;
        
        [Tooltip("Is this a pool coin (shows '?')?")]
        public bool isPool = false;
    }
    
    #endregion
}
