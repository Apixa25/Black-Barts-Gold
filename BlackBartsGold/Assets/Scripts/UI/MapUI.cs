// ============================================================================
// MapUI.cs
// Black Bart's Gold - Map Screen Controller
// Path: Assets/Scripts/UI/MapUI.cs
// ============================================================================
// Controls the 2D map screen showing coin locations. Displays player
// position and nearby coins with distance/direction info.
// Reference: BUILD-GUIDE.md Prompt 5.4
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;
using BlackBartsGold.AR;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Map screen controller showing coin locations.
    /// Radar-style view with player at center.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Map Display")]
        [SerializeField]
        private RectTransform mapContainer;
        
        [SerializeField]
        private RectTransform playerMarker;
        
        [SerializeField]
        private RectTransform playerDirectionArrow;
        
        [SerializeField]
        private GameObject coinMarkerPrefab;
        
        [SerializeField]
        private Image[] rangeCircles;
        
        [Header("Map Settings")]
        [SerializeField]
        private float mapRadius = 150f; // Pixels
        
        [SerializeField]
        private float mapRange = 500f; // Meters
        
        [SerializeField]
        private float updateInterval = 1f;
        
        [Header("Selected Coin Panel")]
        [SerializeField]
        private GameObject selectedCoinPanel;
        
        [SerializeField]
        private TMP_Text selectedCoinValue;
        
        [SerializeField]
        private TMP_Text selectedCoinDistance;
        
        [SerializeField]
        private TMP_Text selectedCoinDirection;
        
        [SerializeField]
        private Image selectedCoinIcon;
        
        [SerializeField]
        private Button navigateButton;
        
        [SerializeField]
        private Button cancelButton;
        
        [Header("Coin List")]
        [SerializeField]
        private Transform coinListContainer;
        
        [SerializeField]
        private GameObject coinListItemPrefab;
        
        [SerializeField]
        private TMP_Text coinCountText;
        
        [Header("Navigation")]
        [SerializeField]
        private Button backButton;
        
        [SerializeField]
        private Button zoomInButton;
        
        [SerializeField]
        private Button zoomOutButton;
        
        [SerializeField]
        private TMP_Text rangeText;
        
        [Header("Colors")]
        [SerializeField]
        private Color goldCoinColor = new Color(1f, 0.84f, 0f);
        
        [SerializeField]
        private Color poolCoinColor = new Color(0.75f, 0.75f, 0.75f);
        
        [SerializeField]
        private Color lockedCoinColor = new Color(0.94f, 0.27f, 0.27f);
        
        [SerializeField]
        private Color selectedColor = new Color(0.29f, 0.87f, 0.5f);
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Currently selected coin
        /// </summary>
        public Coin SelectedCoin { get; private set; }
        
        /// <summary>
        /// Number of coins on map
        /// </summary>
        public int CoinCount => coinMarkers.Count;
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<string, RectTransform> coinMarkers = new Dictionary<string, RectTransform>();
        private List<GameObject> coinListItems = new List<GameObject>();
        private Queue<RectTransform> markerPool = new Queue<RectTransform>();
        private float lastUpdateTime = 0f;
        private float currentHeading = 0f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            SetupButtons();
            HideSelectedCoinPanel();
            
            // Initialize DeviceCompass (New Input System replacement for legacy Input.compass)
            DeviceCompass.Initialize();
            
            // Subscribe to GPS updates
            if (GPSManager.Exists)
            {
                GPSManager.Instance.OnLocationUpdated += OnLocationUpdated;
            }
            
            // Initial update
            UpdateMap();
            UpdateRangeText();
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
            
            // Periodic map update
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateMap();
            }
        }
        
        #endregion
        
        #region Setup
        
        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupButtons()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            
            if (zoomInButton != null)
                zoomInButton.onClick.AddListener(OnZoomInClicked);
            
            if (zoomOutButton != null)
                zoomOutButton.onClick.AddListener(OnZoomOutClicked);
            
            if (navigateButton != null)
                navigateButton.onClick.AddListener(OnNavigateClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnLocationUpdated(LocationData location)
        {
            UpdateMap();
        }
        
        #endregion
        
        #region Map Updates
        
        /// <summary>
        /// Update the map display
        /// </summary>
        public void UpdateMap()
        {
            LocationData playerLocation = GetPlayerLocation();
            if (playerLocation == null) return;
            
            // Update player direction
            UpdatePlayerDirection();
            
            // Get coins
            List<Coin> coins = GetNearbyCoins(playerLocation);
            
            // Update coin count
            if (coinCountText != null)
            {
                coinCountText.text = $"{coins.Count} coins nearby";
            }
            
            // Track updated coins
            HashSet<string> updatedCoins = new HashSet<string>();
            
            // Update/create markers
            foreach (var coin in coins)
            {
                UpdateCoinMarker(coin, playerLocation);
                updatedCoins.Add(coin.id);
            }
            
            // Remove old markers
            List<string> toRemove = new List<string>();
            foreach (var id in coinMarkers.Keys)
            {
                if (!updatedCoins.Contains(id))
                {
                    toRemove.Add(id);
                }
            }
            foreach (var id in toRemove)
            {
                RemoveCoinMarker(id);
            }
            
            // Update coin list
            UpdateCoinList(coins, playerLocation);
            
            Log($"Map updated: {coins.Count} coins");
        }
        
        /// <summary>
        /// Update player direction arrow
        /// </summary>
        private void UpdateHeading()
        {
            // Uses DeviceCompass (New Input System) — legacy Input.compass broken on Android 16+
            if (DeviceCompass.IsAvailable)
            {
                currentHeading = DeviceCompass.Heading;
            }
            
            if (playerDirectionArrow != null)
            {
                playerDirectionArrow.localRotation = Quaternion.Euler(0, 0, -currentHeading);
            }
        }
        
        private void UpdatePlayerDirection()
        {
            // Player marker stays at center
            if (playerMarker != null)
            {
                playerMarker.anchoredPosition = Vector2.zero;
            }
        }
        
        /// <summary>
        /// Update or create a coin marker
        /// </summary>
        private void UpdateCoinMarker(Coin coin, LocationData playerLocation)
        {
            RectTransform marker;
            
            if (!coinMarkers.TryGetValue(coin.id, out marker))
            {
                // Create new marker
                marker = GetMarkerFromPool();
                marker.SetParent(mapContainer);
                marker.gameObject.SetActive(true);
                coinMarkers[coin.id] = marker;
                
                // Setup click handler
                Button btn = marker.GetComponent<Button>();
                if (btn != null)
                {
                    string coinId = coin.id;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnCoinMarkerClicked(coinId));
                }
            }
            
            // Calculate position
            float distance = GeoUtils.CalculateDistance(playerLocation, 
                new LocationData(coin.latitude, coin.longitude));
            float bearing = GeoUtils.CalculateBearing(playerLocation,
                new LocationData(coin.latitude, coin.longitude));
            
            // Adjust bearing for heading (map rotates with player)
            float adjustedBearing = bearing - currentHeading;
            float bearingRad = adjustedBearing * Mathf.Deg2Rad;
            
            // Calculate pixel position
            float normalizedDist = Mathf.Clamp01(distance / mapRange);
            float pixelDist = normalizedDist * mapRadius;
            
            float x = Mathf.Sin(bearingRad) * pixelDist;
            float y = Mathf.Cos(bearingRad) * pixelDist;
            
            marker.anchoredPosition = new Vector2(x, y);
            
            // Set color based on state
            Image markerImage = marker.GetComponent<Image>();
            if (markerImage != null)
            {
                if (coin.isLocked)
                {
                    markerImage.color = lockedCoinColor;
                }
                else if (coin.coinType == CoinType.Pool)
                {
                    markerImage.color = poolCoinColor;
                }
                else if (SelectedCoin != null && SelectedCoin.id == coin.id)
                {
                    markerImage.color = selectedColor;
                }
                else
                {
                    markerImage.color = goldCoinColor;
                }
            }
            
            // Scale based on value (bigger = more valuable)
            float scale = Mathf.Lerp(0.5f, 1.5f, Mathf.Clamp01(coin.value / 25f));
            marker.localScale = Vector3.one * scale;
        }
        
        /// <summary>
        /// Remove a coin marker
        /// </summary>
        private void RemoveCoinMarker(string coinId)
        {
            if (coinMarkers.TryGetValue(coinId, out RectTransform marker))
            {
                ReturnMarkerToPool(marker);
                coinMarkers.Remove(coinId);
            }
        }
        
        /// <summary>
        /// Update the coin list
        /// </summary>
        private void UpdateCoinList(List<Coin> coins, LocationData playerLocation)
        {
            // Clear existing items
            foreach (var item in coinListItems)
            {
                Destroy(item);
            }
            coinListItems.Clear();
            
            if (coinListContainer == null || coinListItemPrefab == null) return;
            
            // Sort by distance
            coins = GeoUtils.SortCoinsByDistance(coins, playerLocation);
            
            // Create list items (limit to 10)
            int count = Mathf.Min(coins.Count, 10);
            for (int i = 0; i < count; i++)
            {
                CreateCoinListItem(coins[i], playerLocation);
            }
        }
        
        /// <summary>
        /// Create a coin list item
        /// </summary>
        private void CreateCoinListItem(Coin coin, LocationData playerLocation)
        {
            GameObject item = Instantiate(coinListItemPrefab, coinListContainer);
            coinListItems.Add(item);
            
            // Find text components
            var texts = item.GetComponentsInChildren<TMP_Text>();
            
            float distance = GeoUtils.CalculateDistance(playerLocation,
                new LocationData(coin.latitude, coin.longitude));
            float bearing = GeoUtils.CalculateBearing(playerLocation,
                new LocationData(coin.latitude, coin.longitude));
            
            if (texts.Length >= 3)
            {
                texts[0].text = coin.GetDisplayValue();
                texts[1].text = GeoUtils.FormatDistance(distance);
                texts[2].text = GeoUtils.GetCardinalDirection(bearing);
            }
            
            // Setup click
            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                string coinId = coin.id;
                btn.onClick.AddListener(() => SelectCoin(coinId));
            }
        }
        
        #endregion
        
        #region Object Pool
        
        private RectTransform GetMarkerFromPool()
        {
            if (markerPool.Count > 0)
            {
                return markerPool.Dequeue();
            }
            
            // Create new marker
            GameObject markerObj;
            if (coinMarkerPrefab != null)
            {
                markerObj = Instantiate(coinMarkerPrefab);
            }
            else
            {
                markerObj = new GameObject("CoinMarker");
                Image img = markerObj.AddComponent<Image>();
                img.color = goldCoinColor;
                markerObj.AddComponent<Button>();
                
                RectTransform rt = markerObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(20, 20);
            }
            
            return markerObj.GetComponent<RectTransform>();
        }
        
        private void ReturnMarkerToPool(RectTransform marker)
        {
            marker.gameObject.SetActive(false);
            markerPool.Enqueue(marker);
        }
        
        #endregion
        
        #region Coin Selection
        
        /// <summary>
        /// Handle coin marker click
        /// </summary>
        private void OnCoinMarkerClicked(string coinId)
        {
            SelectCoin(coinId);
        }
        
        /// <summary>
        /// Select a coin
        /// </summary>
        public void SelectCoin(string coinId)
        {
            // Find the coin
            Coin coin = FindCoinById(coinId);
            if (coin == null) return;
            
            SelectedCoin = coin;
            ShowSelectedCoinPanel(coin);
            
            // Update marker color
            UpdateMap();
            
            Log($"Selected coin: {coin.GetDisplayValue()}");
        }
        
        /// <summary>
        /// Deselect current coin
        /// </summary>
        public void DeselectCoin()
        {
            SelectedCoin = null;
            HideSelectedCoinPanel();
            UpdateMap();
        }
        
        /// <summary>
        /// Show selected coin panel
        /// </summary>
        private void ShowSelectedCoinPanel(Coin coin)
        {
            if (selectedCoinPanel == null) return;
            
            selectedCoinPanel.SetActive(true);
            
            LocationData playerLocation = GetPlayerLocation();
            
            if (selectedCoinValue != null)
            {
                selectedCoinValue.text = coin.GetDisplayValue();
                selectedCoinValue.color = coin.isLocked ? lockedCoinColor : goldCoinColor;
            }
            
            if (playerLocation != null)
            {
                float distance = GeoUtils.CalculateDistance(playerLocation,
                    new LocationData(coin.latitude, coin.longitude));
                float bearing = GeoUtils.CalculateBearing(playerLocation,
                    new LocationData(coin.latitude, coin.longitude));
                
                if (selectedCoinDistance != null)
                {
                    selectedCoinDistance.text = GeoUtils.FormatDistance(distance);
                }
                
                if (selectedCoinDirection != null)
                {
                    selectedCoinDirection.text = $"{GeoUtils.GetCardinalDirection(bearing)} ({bearing:F0}°)";
                }
            }
            
            // Disable navigate for locked coins
            if (navigateButton != null)
            {
                navigateButton.interactable = !coin.isLocked;
            }
        }
        
        /// <summary>
        /// Hide selected coin panel
        /// </summary>
        private void HideSelectedCoinPanel()
        {
            if (selectedCoinPanel != null)
            {
                selectedCoinPanel.SetActive(false);
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnBackClicked()
        {
            SceneLoader.LoadScene(SceneNames.MainMenu);
        }
        
        private void OnZoomInClicked()
        {
            mapRange = Mathf.Max(50f, mapRange / 2f);
            UpdateRangeText();
            UpdateMap();
        }
        
        private void OnZoomOutClicked()
        {
            mapRange = Mathf.Min(2000f, mapRange * 2f);
            UpdateRangeText();
            UpdateMap();
        }
        
        private void OnNavigateClicked()
        {
            if (SelectedCoin == null) return;
            
            Log($"Navigating to coin: {SelectedCoin.GetDisplayValue()}");
            
            // Store selected coin for AR scene
            // TODO: Pass selected coin to AR scene
            
            SceneLoader.LoadScene(SceneNames.ARHunt);
        }
        
        private void OnCancelClicked()
        {
            DeselectCoin();
        }
        
        private void UpdateRangeText()
        {
            if (rangeText != null)
            {
                rangeText.text = $"{GeoUtils.FormatDistance(mapRange)} range";
            }
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
            
            // Return test location for development
            return LocationData.CreateTestLocation();
        }
        
        /// <summary>
        /// Get nearby coins
        /// </summary>
        private List<Coin> GetNearbyCoins(LocationData playerLocation)
        {
            List<Coin> coins = new List<Coin>();
            
            // Get from CoinManager if available
            if (CoinManager.Instance != null)
            {
                foreach (var controller in CoinManager.Instance.ActiveCoins)
                {
                    if (controller.CoinData != null)
                    {
                        coins.Add(controller.CoinData);
                    }
                }
            }
            
            // Filter by range
            return GeoUtils.FilterCoinsByDistance(coins, playerLocation, mapRange);
        }
        
        /// <summary>
        /// Find coin by ID
        /// </summary>
        private Coin FindCoinById(string coinId)
        {
            if (CoinManager.Instance != null)
            {
                var controller = CoinManager.Instance.GetCoinById(coinId);
                return controller?.CoinData;
            }
            return null;
        }
        
        #endregion
        
        #region Debug
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[MapUI] {message}");
            }
        }
        
        /// <summary>
        /// Debug: Print map state
        /// </summary>
        [ContextMenu("Debug: Print State")]
        public void DebugPrintState()
        {
            Debug.Log("=== Map State ===");
            Debug.Log($"Range: {mapRange}m");
            Debug.Log($"Coins: {CoinCount}");
            Debug.Log($"Selected: {SelectedCoin?.GetDisplayValue() ?? "none"}");
            Debug.Log($"Heading: {currentHeading:F0}°");
            Debug.Log("=================");
        }
        
        #endregion
    }
}
