// ============================================================================
// RadarUI.cs
// Black Bart's Gold - Mini Radar/Map UI Component
// Path: Assets/Scripts/UI/RadarUI.cs
// ============================================================================
// Displays a radar-style mini-map showing nearby coins as dots around
// the player's position. Updates based on GPS and coin positions.
// Reference: BUILD-GUIDE.md Prompt 5.1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;
using BlackBartsGold.AR;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Mini radar display showing nearby coins.
    /// Player is at center, coins appear as dots.
    /// </summary>
    public class RadarUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField]
        [Tooltip("Container for the radar")]
        private RectTransform radarContainer;
        
        [SerializeField]
        [Tooltip("Player dot at center")]
        private RectTransform playerDot;
        
        [SerializeField]
        [Tooltip("Prefab for coin dots")]
        private GameObject coinDotPrefab;
        
        [SerializeField]
        [Tooltip("Sweep line that rotates")]
        private RectTransform sweepLine;
        
        [SerializeField]
        [Tooltip("Range rings")]
        private Image[] rangeRings;
        
        [SerializeField]
        [Tooltip("North indicator")]
        private RectTransform northIndicator;
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Radar range in meters")]
        private float radarRange = 50f;
        
        [SerializeField]
        [Tooltip("Radar radius in pixels")]
        private float radarRadius = 60f;
        
        [SerializeField]
        [Tooltip("Rotate radar with device heading")]
        private bool rotateWithHeading = true;
        
        [SerializeField]
        [Tooltip("Sweep animation speed (degrees/second)")]
        private float sweepSpeed = 90f;
        
        [SerializeField]
        [Tooltip("Update interval (seconds)")]
        private float updateInterval = 0.5f;
        
        [Header("Dot Colors")]
        [SerializeField]
        private Color normalCoinColor = new Color(1f, 0.84f, 0f); // Gold
        
        [SerializeField]
        private Color lockedCoinColor = new Color(0.94f, 0.27f, 0.27f); // Red
        
        [SerializeField]
        private Color inRangeCoinColor = new Color(0.29f, 0.87f, 0.5f); // Green
        
        [SerializeField]
        private Color poolCoinColor = new Color(0.5f, 0.8f, 1f); // Light blue
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Is radar visible?
        /// </summary>
        public bool IsVisible { get; private set; } = true;
        
        /// <summary>
        /// Current radar range
        /// </summary>
        public float Range => radarRange;
        
        /// <summary>
        /// Number of coins on radar
        /// </summary>
        public int CoinCount => activeDots.Count;
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<string, RectTransform> activeDots = new Dictionary<string, RectTransform>();
        private Queue<RectTransform> dotPool = new Queue<RectTransform>();
        private float lastUpdateTime = 0f;
        private float currentHeading = 0f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            // Enable compass
            Input.compass.enabled = true;
            
            // Subscribe to events
            if (GPSManager.Exists)
            {
                GPSManager.Instance.OnLocationUpdated += OnLocationUpdated;
            }
            
            // Initial update
            UpdateRadar();
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
            // Update heading
            UpdateHeading();
            
            // Animate sweep
            AnimateSweep();
            
            // Periodic radar update
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateRadar();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle GPS location update
        /// </summary>
        private void OnLocationUpdated(LocationData location)
        {
            UpdateRadar();
        }
        
        #endregion
        
        #region Radar Updates
        
        /// <summary>
        /// Update radar display
        /// </summary>
        public void UpdateRadar()
        {
            if (!IsVisible) return;
            
            LocationData playerLocation = GetPlayerLocation();
            if (playerLocation == null) return;
            
            // Get coins from CoinManager
            if (CoinManager.Instance == null) return;
            
            // Track which coins we've updated
            HashSet<string> updatedCoins = new HashSet<string>();
            
            foreach (var controller in CoinManager.Instance.ActiveCoins)
            {
                if (controller.CoinData == null) continue;
                
                Coin coin = controller.CoinData;
                
                // Calculate distance
                float distance = GeoUtils.CalculateDistance(
                    playerLocation.latitude, playerLocation.longitude,
                    coin.latitude, coin.longitude
                );
                
                // Skip if out of radar range
                if (distance > radarRange)
                {
                    RemoveCoinDot(coin.id);
                    continue;
                }
                
                // Calculate bearing
                float bearing = GeoUtils.CalculateBearing(
                    playerLocation.latitude, playerLocation.longitude,
                    coin.latitude, coin.longitude
                );
                
                // Update or create dot
                UpdateCoinDot(coin, distance, bearing, controller.IsLocked, controller.IsInRange);
                updatedCoins.Add(coin.id);
            }
            
            // Remove dots for coins no longer tracked
            List<string> toRemove = new List<string>();
            foreach (var id in activeDots.Keys)
            {
                if (!updatedCoins.Contains(id))
                {
                    toRemove.Add(id);
                }
            }
            foreach (var id in toRemove)
            {
                RemoveCoinDot(id);
            }
        }
        
        /// <summary>
        /// Update or create a coin dot
        /// </summary>
        private void UpdateCoinDot(Coin coin, float distance, float bearing, bool isLocked, bool isInRange)
        {
            RectTransform dot;
            
            if (!activeDots.TryGetValue(coin.id, out dot))
            {
                // Create new dot
                dot = GetDotFromPool();
                dot.SetParent(radarContainer);
                dot.gameObject.SetActive(true);
                activeDots[coin.id] = dot;
            }
            
            // Calculate position on radar
            // Adjust bearing for heading if rotating
            float adjustedBearing = bearing;
            if (rotateWithHeading)
            {
                adjustedBearing = bearing - currentHeading;
            }
            
            float bearingRad = adjustedBearing * Mathf.Deg2Rad;
            float normalizedDistance = distance / radarRange;
            float pixelDistance = normalizedDistance * radarRadius;
            
            // Position (0,0 is center, up is north/forward)
            float x = Mathf.Sin(bearingRad) * pixelDistance;
            float y = Mathf.Cos(bearingRad) * pixelDistance;
            
            dot.anchoredPosition = new Vector2(x, y);
            
            // Set color based on state
            Image dotImage = dot.GetComponent<Image>();
            if (dotImage != null)
            {
                if (isLocked)
                {
                    dotImage.color = lockedCoinColor;
                }
                else if (isInRange)
                {
                    dotImage.color = inRangeCoinColor;
                }
                else if (coin.coinType == CoinType.Pool)
                {
                    dotImage.color = poolCoinColor;
                }
                else
                {
                    dotImage.color = normalCoinColor;
                }
            }
            
            // Scale based on distance (closer = bigger)
            float scale = Mathf.Lerp(1.5f, 0.5f, normalizedDistance);
            dot.localScale = Vector3.one * scale;
        }
        
        /// <summary>
        /// Remove a coin dot
        /// </summary>
        private void RemoveCoinDot(string coinId)
        {
            if (activeDots.TryGetValue(coinId, out RectTransform dot))
            {
                ReturnDotToPool(dot);
                activeDots.Remove(coinId);
            }
        }
        
        /// <summary>
        /// Clear all coin dots
        /// </summary>
        public void ClearAllDots()
        {
            foreach (var dot in activeDots.Values)
            {
                ReturnDotToPool(dot);
            }
            activeDots.Clear();
        }
        
        #endregion
        
        #region Object Pool
        
        /// <summary>
        /// Get a dot from the pool or create new
        /// </summary>
        private RectTransform GetDotFromPool()
        {
            if (dotPool.Count > 0)
            {
                return dotPool.Dequeue();
            }
            
            // Create new dot
            GameObject dotObj;
            if (coinDotPrefab != null)
            {
                dotObj = Instantiate(coinDotPrefab);
            }
            else
            {
                // Create default dot
                dotObj = new GameObject("CoinDot");
                Image img = dotObj.AddComponent<Image>();
                img.color = normalCoinColor;
                
                RectTransform rt = dotObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(8, 8);
            }
            
            return dotObj.GetComponent<RectTransform>();
        }
        
        /// <summary>
        /// Return a dot to the pool
        /// </summary>
        private void ReturnDotToPool(RectTransform dot)
        {
            dot.gameObject.SetActive(false);
            dotPool.Enqueue(dot);
        }
        
        #endregion
        
        #region Heading & Animation
        
        /// <summary>
        /// Update device heading
        /// </summary>
        private void UpdateHeading()
        {
            if (Input.compass.enabled)
            {
                currentHeading = Input.compass.trueHeading;
                if (currentHeading == 0)
                {
                    currentHeading = Input.compass.magneticHeading;
                }
            }
            
            // Rotate north indicator
            if (northIndicator != null && rotateWithHeading)
            {
                northIndicator.localRotation = Quaternion.Euler(0, 0, currentHeading);
            }
        }
        
        /// <summary>
        /// Animate the sweep line
        /// </summary>
        private void AnimateSweep()
        {
            if (sweepLine == null) return;
            
            sweepLine.Rotate(0, 0, -sweepSpeed * Time.deltaTime);
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the radar
        /// </summary>
        public void Show()
        {
            if (radarContainer != null)
            {
                radarContainer.gameObject.SetActive(true);
            }
            IsVisible = true;
            UpdateRadar();
        }
        
        /// <summary>
        /// Hide the radar
        /// </summary>
        public void Hide()
        {
            if (radarContainer != null)
            {
                radarContainer.gameObject.SetActive(false);
            }
            IsVisible = false;
        }
        
        /// <summary>
        /// Toggle visibility
        /// </summary>
        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// Set radar range
        /// </summary>
        public void SetRange(float meters)
        {
            radarRange = Mathf.Max(10f, meters);
            UpdateRadar();
        }
        
        /// <summary>
        /// Zoom in (decrease range)
        /// </summary>
        public void ZoomIn()
        {
            SetRange(radarRange * 0.5f);
        }
        
        /// <summary>
        /// Zoom out (increase range)
        /// </summary>
        public void ZoomOut()
        {
            SetRange(radarRange * 2f);
        }
        
        #endregion
        
        #region Helpers
        
        /// <summary>
        /// Get player location
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
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[RadarUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print radar state
        /// </summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== Radar State ===");
            Debug.Log($"Visible: {IsVisible}");
            Debug.Log($"Range: {radarRange}m");
            Debug.Log($"Coins: {CoinCount}");
            Debug.Log($"Heading: {currentHeading:F0}°");
            Debug.Log("===================");
        }
        
        /// <summary>
        /// Debug: Add test dots
        /// </summary>
        [ContextMenu("Debug: Add Test Dots")]
        public void DebugAddTestDots()
        {
            // Create test coins at various bearings
            float[] bearings = { 0, 45, 90, 135, 180, 225, 270, 315 };
            float[] distances = { 10, 20, 30, 40, 25, 35, 15, 45 };
            
            for (int i = 0; i < bearings.Length; i++)
            {
                Coin testCoin = Coin.CreateTestCoin((i + 1) * 1.00f);
                testCoin.id = $"test-{i}";
                
                UpdateCoinDot(testCoin, distances[i], bearings[i], i == 3, i == 0);
            }
        }
        
        #endregion
    }
}
