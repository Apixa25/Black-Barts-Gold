// ============================================================================
// CoinApiService.cs
// Black Bart's Gold - Coin API Service
// Path: Assets/Scripts/Core/CoinApiService.cs
// ============================================================================
// Service for coin-related API operations: fetching nearby coins, hiding,
// collecting, and syncing coin state with the server.
// Reference: BUILD-GUIDE.md Sprint 8, Prompt 8.2
// ============================================================================

using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Coin API service singleton.
    /// Handles all coin-related API communication.
    /// </summary>
    public class CoinApiService : MonoBehaviour
    {
        #region Singleton
        
        private static CoinApiService _instance;
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static CoinApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CoinApiService>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CoinApiService");
                        _instance = go.AddComponent<CoinApiService>();
                        Debug.Log("[CoinApiService] 🪙 Created new CoinApiService instance");
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Check if instance exists
        /// </summary>
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Cache
        
        /// <summary>
        /// Cached nearby coins
        /// </summary>
        private List<Coin> _cachedCoins = new List<Coin>();
        
        /// <summary>
        /// Last cache update time
        /// </summary>
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        
        /// <summary>
        /// Cache validity duration (seconds)
        /// </summary>
        private const int CACHE_DURATION_SECONDS = 60;
        
        /// <summary>
        /// Last fetched location
        /// </summary>
        private double _lastLat = 0;
        private double _lastLng = 0;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when nearby coins are updated
        /// </summary>
        public event Action<List<Coin>> OnNearbyCoinsUpdated;
        
        /// <summary>
        /// Fired when a coin is collected
        /// </summary>
        public event Action<Coin, float> OnCoinCollected;
        
        /// <summary>
        /// Fired when a coin is hidden
        /// </summary>
        public event Action<Coin> OnCoinHidden;
        
        /// <summary>
        /// Fired on API error
        /// </summary>
        public event Action<string> OnApiError;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[CoinApiService] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[CoinApiService] 🪙 CoinApiService initialized");
        }
        
        #endregion
        
        #region Nearby Coins
        
        /// <summary>
        /// Get coins near a location
        /// </summary>
        public async Task<List<Coin>> GetNearbyCoins(double latitude, double longitude, float radiusMeters = 500f)
        {
            Debug.Log($"[CoinApiService] 📍 Fetching coins near ({latitude:F6}, {longitude:F6}) r={radiusMeters}m");
            Debug.Log($"[CoinApiService] Current API mode: {ApiConfig.CurrentEnvironment}, UseMock: {ApiConfig.UseMockApi}");
            
            // Check cache validity
            if (IsCacheValid(latitude, longitude))
            {
                Debug.Log("[CoinApiService] Using cached coins");
                return _cachedCoins;
            }
            
            try
            {
                if (ApiConfig.UseMockApi)
                {
                    Debug.Log("[CoinApiService] 🎭 Using MOCK API - returning fake coins");
                    return await GetMockNearbyCoins(latitude, longitude, radiusMeters);
                }
                
                string endpoint = ApiConfig.Coins.GetNearbyUrl(latitude, longitude, radiusMeters);
                string fullUrl = ApiConfig.BuildUrl(endpoint);
                Debug.Log($"[CoinApiService] 🌐 Making REAL API call to: {fullUrl}");
                
                var response = await ApiClient.Instance.Get<NearbyCoinsResponse>(endpoint);
                
                Debug.Log($"[CoinApiService] 📦 API Response received: success={response?.success}, coins count={response?.coins?.Count ?? 0}");
                
                if (response?.coins != null)
                {
                    Debug.Log($"[CoinApiService] ✅ Found {response.coins.Count} coins from server!");
                    foreach (var coin in response.coins)
                    {
                        Debug.Log($"  - Coin: {coin.id}, Value: ${coin.value:F2}, Status: {coin.status}, Lat: {coin.latitude:F6}, Lng: {coin.longitude:F6}");
                    }
                    UpdateCache(response.coins, latitude, longitude);
                    OnNearbyCoinsUpdated?.Invoke(response.coins);
                    return response.coins;
                }
                
                Debug.Log("[CoinApiService] ⚠️ No coins in response");
                return new List<Coin>();
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[CoinApiService] Error fetching nearby coins: {ex.Message}");
                OnApiError?.Invoke(ex.UserMessage);
                
                // Return cached if available
                if (_cachedCoins.Count > 0)
                {
                    return _cachedCoins;
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// Force refresh nearby coins (ignore cache)
        /// </summary>
        public async Task<List<Coin>> RefreshNearbyCoins(double latitude, double longitude, float radiusMeters = 500f)
        {
            InvalidateCache();
            return await GetNearbyCoins(latitude, longitude, radiusMeters);
        }
        
        /// <summary>
        /// Get mock nearby coins
        /// </summary>
        private async Task<List<Coin>> GetMockNearbyCoins(double latitude, double longitude, float radiusMeters)
        {
            await Task.Delay(UnityEngine.Random.Range(200, 500));
            
            var coins = new List<Coin>();
            int coinCount = UnityEngine.Random.Range(3, 8);
            
            for (int i = 0; i < coinCount; i++)
            {
                // Generate random offset (within radius)
                float offsetLat = UnityEngine.Random.Range(-0.001f, 0.001f);
                float offsetLng = UnityEngine.Random.Range(-0.001f, 0.001f);
                
                // Random coin type and value
                CoinType type = UnityEngine.Random.value > 0.7f ? CoinType.Pool : CoinType.Fixed;
                float value = GetRandomCoinValue();
                
                var coin = Coin.CreateTestCoin(type, value);
                coin.latitude = latitude + offsetLat;
                coin.longitude = longitude + offsetLng;
                coin.status = CoinStatus.Active;
                
                coins.Add(coin);
            }
            
            UpdateCache(coins, latitude, longitude);
            OnNearbyCoinsUpdated?.Invoke(coins);
            
            Debug.Log($"[CoinApiService] [MOCK] Generated {coins.Count} nearby coins");
            return coins;
        }
        
        /// <summary>
        /// Get random coin value based on distribution
        /// </summary>
        private float GetRandomCoinValue()
        {
            float roll = UnityEngine.Random.value;
            
            if (roll < 0.40f) return 0.05f;      // 40% - 5¢
            if (roll < 0.65f) return 0.10f;      // 25% - 10¢
            if (roll < 0.80f) return 0.25f;      // 15% - 25¢
            if (roll < 0.90f) return 0.50f;      // 10% - 50¢
            if (roll < 0.96f) return 1.00f;      // 6% - $1
            if (roll < 0.99f) return 5.00f;      // 3% - $5
            return 10.00f;                        // 1% - $10
        }
        
        #endregion
        
        #region Collect Coin
        
        /// <summary>
        /// Collect a coin
        /// </summary>
        public async Task<CoinCollectionResponse> CollectCoin(string coinId)
        {
            Debug.Log($"[CoinApiService] 💰 Collecting coin: {coinId}");
            
            try
            {
                if (ApiConfig.UseMockApi)
                {
                    return await MockCollectCoin(coinId);
                }
                
                string endpoint = ApiConfig.Coins.GetCoinUrl(ApiConfig.Coins.COLLECT, coinId);
                var response = await ApiClient.Instance.Post<CoinCollectionResponse>(endpoint, new { coinId });
                
                if (response != null && response.success)
                {
                    // Remove from cache
                    RemoveFromCache(coinId);
                    OnCoinCollected?.Invoke(response.coin, response.value);
                }
                
                return response;
            }
            catch (NotFoundException)
            {
                Debug.LogWarning($"[CoinApiService] Coin {coinId} not found - may have been collected");
                RemoveFromCache(coinId);
                throw;
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[CoinApiService] Error collecting coin: {ex.Message}");
                OnApiError?.Invoke(ex.UserMessage);
                throw;
            }
        }
        
        /// <summary>
        /// Mock collect coin
        /// </summary>
        private async Task<CoinCollectionResponse> MockCollectCoin(string coinId)
        {
            await Task.Delay(UnityEngine.Random.Range(200, 400));
            
            // Find coin in cache
            var coin = _cachedCoins.Find(c => c.id == coinId);
            if (coin == null)
            {
                throw NotFoundException.CoinNotFound(coinId);
            }
            
            // Calculate value (for pool coins, use slot machine)
            float value = coin.type == CoinType.Pool 
                ? CalculatePoolValue(coin.value)
                : coin.value;
            
            coin.status = CoinStatus.Collected;
            RemoveFromCache(coinId);
            
            var response = new CoinCollectionResponse
            {
                success = true,
                coin = coin,
                value = value,
                message = "Treasure collected!"
            };
            
            OnCoinCollected?.Invoke(coin, value);
            Debug.Log($"[CoinApiService] [MOCK] Collected coin: ${value:F2}");
            
            return response;
        }
        
        /// <summary>
        /// Calculate pool coin value (slot machine)
        /// </summary>
        private float CalculatePoolValue(float baseValue)
        {
            float roll = UnityEngine.Random.value;
            float multiplier;
            
            if (roll < 0.50f) multiplier = UnityEngine.Random.Range(0.2f, 0.8f);
            else if (roll < 0.85f) multiplier = UnityEngine.Random.Range(0.8f, 1.5f);
            else if (roll < 0.98f) multiplier = UnityEngine.Random.Range(1.5f, 3.0f);
            else multiplier = UnityEngine.Random.Range(3.0f, 5.0f);
            
            return Mathf.Round(baseValue * multiplier * 100f) / 100f;
        }
        
        #endregion
        
        #region Hide Coin
        
        /// <summary>
        /// Hide a coin at location
        /// </summary>
        public async Task<HideCoinResponse> HideCoin(HideCoinRequest request)
        {
            Debug.Log($"[CoinApiService] 🏴‍☠️ Hiding coin: ${request.value:F2} at ({request.latitude:F6}, {request.longitude:F6})");
            
            try
            {
                if (ApiConfig.UseMockApi)
                {
                    return await MockHideCoin(request);
                }
                
                var response = await ApiClient.Instance.Post<HideCoinResponse>(ApiConfig.Coins.HIDE, request);
                
                if (response != null && response.success)
                {
                    OnCoinHidden?.Invoke(response.coin);
                }
                
                return response;
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[CoinApiService] Error hiding coin: {ex.Message}");
                OnApiError?.Invoke(ex.UserMessage);
                throw;
            }
        }
        
        /// <summary>
        /// Mock hide coin
        /// </summary>
        private async Task<HideCoinResponse> MockHideCoin(HideCoinRequest request)
        {
            await Task.Delay(UnityEngine.Random.Range(300, 600));
            
            var coin = new Coin
            {
                id = Guid.NewGuid().ToString(),
                type = request.type,
                value = request.value,
                latitude = request.latitude,
                longitude = request.longitude,
                status = CoinStatus.Active,
                hiddenBy = PlayerData.Exists ? PlayerData.Instance.CurrentUser?.id : "mock-user",
                hiddenAt = DateTime.UtcNow.ToString("o"),
                poolContribution = request.type == CoinType.Pool ? request.value : 0
            };
            
            var response = new HideCoinResponse
            {
                success = true,
                coin = coin,
                message = "Treasure hidden successfully!"
            };
            
            OnCoinHidden?.Invoke(coin);
            Debug.Log($"[CoinApiService] [MOCK] Hidden coin: ${request.value:F2}");
            
            return response;
        }
        
        #endregion
        
        #region Delete Coin
        
        /// <summary>
        /// Delete a coin (owner only)
        /// </summary>
        public async Task<bool> DeleteCoin(string coinId)
        {
            Debug.Log($"[CoinApiService] 🗑️ Deleting coin: {coinId}");
            
            try
            {
                if (ApiConfig.UseMockApi)
                {
                    await Task.Delay(200);
                    RemoveFromCache(coinId);
                    return true;
                }
                
                string endpoint = ApiConfig.Coins.GetCoinUrl(ApiConfig.Coins.DELETE, coinId);
                await ApiClient.Instance.Delete(endpoint);
                
                RemoveFromCache(coinId);
                return true;
            }
            catch (ApiException ex)
            {
                Debug.LogError($"[CoinApiService] Error deleting coin: {ex.Message}");
                OnApiError?.Invoke(ex.UserMessage);
                return false;
            }
        }
        
        #endregion
        
        #region Cache Management
        
        /// <summary>
        /// Check if cache is valid
        /// </summary>
        private bool IsCacheValid(double latitude, double longitude)
        {
            if (_cachedCoins.Count == 0) return false;
            
            // Check time validity
            if ((DateTime.UtcNow - _lastCacheUpdate).TotalSeconds > CACHE_DURATION_SECONDS)
            {
                return false;
            }
            
            // Check location validity (moved more than 50m)
            double distanceMoved = CalculateDistance(_lastLat, _lastLng, latitude, longitude);
            if (distanceMoved > 50)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Update cache
        /// </summary>
        private void UpdateCache(List<Coin> coins, double latitude, double longitude)
        {
            _cachedCoins = coins ?? new List<Coin>();
            _lastCacheUpdate = DateTime.UtcNow;
            _lastLat = latitude;
            _lastLng = longitude;
        }
        
        /// <summary>
        /// Remove coin from cache
        /// </summary>
        private void RemoveFromCache(string coinId)
        {
            _cachedCoins.RemoveAll(c => c.id == coinId);
        }
        
        /// <summary>
        /// Invalidate cache
        /// </summary>
        public void InvalidateCache()
        {
            _cachedCoins.Clear();
            _lastCacheUpdate = DateTime.MinValue;
        }
        
        /// <summary>
        /// Get cached coins
        /// </summary>
        public List<Coin> GetCachedCoins()
        {
            return new List<Coin>(_cachedCoins);
        }
        
        /// <summary>
        /// Calculate distance between two points (meters)
        /// </summary>
        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000; // Earth radius in meters
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLng = (lng2 - lng1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                      Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Debug: Fetch mock coins
        /// </summary>
        [ContextMenu("Debug: Fetch Mock Coins")]
        public async void DebugFetchCoins()
        {
            var coins = await GetNearbyCoins(37.7749, -122.4194, 500f);
            Debug.Log($"[CoinApiService] Fetched {coins.Count} coins");
        }
        
        /// <summary>
        /// Debug: Print cache status
        /// </summary>
        [ContextMenu("Debug: Print Cache")]
        public void DebugPrintCache()
        {
            Debug.Log($"=== Coin Cache ===");
            Debug.Log($"Coins: {_cachedCoins.Count}");
            Debug.Log($"Last Update: {_lastCacheUpdate}");
            Debug.Log($"Location: ({_lastLat:F6}, {_lastLng:F6})");
            foreach (var coin in _cachedCoins)
            {
                Debug.Log($"  - {coin.id}: ${coin.value:F2} ({coin.type})");
            }
            Debug.Log($"==================");
        }
        
        #endregion
    }
    
    #region Request/Response Types
    
    /// <summary>
    /// Nearby coins API response
    /// </summary>
    [Serializable]
    public class NearbyCoinsResponse
    {
        public bool success;
        public List<Coin> coins;
        public int totalCount;
    }
    
    /// <summary>
    /// Coin collection API response
    /// </summary>
    [Serializable]
    public class CoinCollectionResponse
    {
        public bool success;
        public Coin coin;
        public float value;
        public string message;
    }
    
    /// <summary>
    /// Hide coin request
    /// </summary>
    [Serializable]
    public class HideCoinRequest
    {
        public CoinType type;
        public float value;
        public double latitude;
        public double longitude;
        public string message;
    }
    
    /// <summary>
    /// Hide coin response
    /// </summary>
    [Serializable]
    public class HideCoinResponse
    {
        public bool success;
        public Coin coin;
        public string message;
    }
    
    #endregion
}
