// ============================================================================
// FullMapUI.cs
// Black Bart's Gold - Full Screen Map UI (Coin Selection)
// Path: Assets/Scripts/UI/FullMapUI.cs
// ============================================================================
// Full-screen map showing all known coins in the region.
// Player taps a coin to select it and enter hunting mode.
// This is the Pokémon GO-style "overworld map" pattern.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;
using BlackBartsGold.Location;
using BlackBartsGold.AR;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Full-screen map UI for viewing and selecting coins.
    /// Shows all known coins, player can tap to select target.
    /// </summary>
    public class FullMapUI : MonoBehaviour
    {
        #region Singleton
        
        private static FullMapUI _instance;
        
        public static FullMapUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<FullMapUI>();
                }
                return _instance;
            }
        }
        
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("UI Containers")]
        [SerializeField]
        [Tooltip("Main panel for the full map")]
        private GameObject mapPanel;
        
        [SerializeField]
        [Tooltip("Container for the map content (where coin markers go)")]
        private RectTransform mapContainer;
        
        [SerializeField]
        [Tooltip("Player position marker")]
        private RectTransform playerMarker;
        
        [Header("Coin Marker")]
        [SerializeField]
        [Tooltip("Prefab for coin markers on map")]
        private GameObject coinMarkerPrefab;
        
        [Header("Selection Panel")]
        [SerializeField]
        [Tooltip("Panel showing selected coin details")]
        private GameObject selectionPanel;
        
        [SerializeField]
        private TMP_Text selectedCoinValue;
        
        [SerializeField]
        private TMP_Text selectedCoinDistance;
        
        [SerializeField]
        private TMP_Text selectedCoinStatus;
        
        [SerializeField]
        private Button huntButton;
        
        [SerializeField]
        private Button cancelButton;
        
        [Header("Header Info")]
        [SerializeField]
        private TMP_Text coinCountText;
        
        [SerializeField]
        private TMP_Text totalValueText;
        
        [SerializeField]
        private Button closeButton;
        
        [Header("Map Settings")]
        [SerializeField]
        [Tooltip("Map range in meters")]
        private float mapRange = 200f;
        
        [SerializeField]
        [Tooltip("Map radius in pixels")]
        private float mapRadius = 300f;
        
        [Header("Marker Colors")]
        [SerializeField]
        private Color normalCoinColor = new Color(1f, 0.84f, 0f); // Gold
        
        [SerializeField]
        private Color lockedCoinColor = new Color(0.94f, 0.27f, 0.27f); // Red
        
        [SerializeField]
        private Color selectedCoinColor = new Color(0.29f, 0.87f, 0.5f); // Green
        
        [SerializeField]
        private Color poolCoinColor = new Color(0.5f, 0.8f, 1f); // Light blue
        
        [Header("Debug")]
        [SerializeField]
        private bool debugMode = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>Is the map currently visible?</summary>
        public bool IsVisible => mapPanel != null && mapPanel.activeSelf;
        
        /// <summary>Currently selected coin on map (before hunting)</summary>
        public Coin PreviewCoin { get; private set; }
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<string, RectTransform> coinMarkers = new Dictionary<string, RectTransform>();
        private Queue<RectTransform> markerPool = new Queue<RectTransform>();
        private string selectedMarkerId = null;
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when map is opened</summary>
        public event Action OnMapOpened;
        
        /// <summary>Fired when map is closed</summary>
        public event Action OnMapClosed;
        
        /// <summary>Fired when coin is selected for preview</summary>
        public event Action<Coin> OnCoinPreviewed;
        
        /// <summary>Fired when user confirms hunt on selected coin</summary>
        public event Action<Coin> OnHuntRequested;
        
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
            
            // Auto-find references if not assigned in Inspector
            AutoFindReferences();
        }
        
        private void Start()
        {
            // Subscribe to events
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnKnownCoinsUpdated += OnKnownCoinsUpdated;
                CoinManager.Instance.OnHuntModeChanged += OnHuntModeChanged;
            }
            
            // Setup buttons
            if (huntButton != null)
            {
                huntButton.onClick.AddListener(OnHuntButtonClicked);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            
            // Start hidden
            HideSelectionPanel();
            if (mapPanel != null) mapPanel.SetActive(false);
            
            // Create a floating "MAP" button as workaround for radar tap issues
            CreateMapButton();
        }
        
        /// <summary>
        /// Creates a floating MAP button that opens the full map.
        /// This is a workaround until radar tap is fixed.
        /// </summary>
        private void CreateMapButton()
        {
            // Find the canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            
            if (canvas == null)
            {
                Debug.LogError("[FullMapUI] No canvas found for MAP button");
                return;
            }
            
            // Create button GameObject
            GameObject btnObj = new GameObject("MapOpenButton");
            btnObj.transform.SetParent(canvas.transform, false);
            
            // Add RectTransform and position it (bottom-right, above where radar would be)
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-20, 200); // Above radar area
            rt.sizeDelta = new Vector2(100, 50);
            
            // Add Image for button background
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 0.9f, 0.9f); // Blue
            
            // Add Button component
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(Show);
            
            // Add text label
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            
            TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = "MAP";
            text.fontSize = 24;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;
            
            Debug.Log("[FullMapUI] Created MAP button as workaround");
        }
        
        /// <summary>
        /// Auto-find child references if not assigned in Inspector.
        /// This allows the UI to work without manual wiring.
        /// </summary>
        private void AutoFindReferences()
        {
            // Map panel is this gameObject
            if (mapPanel == null)
            {
                mapPanel = gameObject;
            }
            
            // Find Header children
            Transform header = transform.Find("Header");
            if (header != null)
            {
                if (closeButton == null)
                {
                    Transform closeBtn = header.Find("CloseButton");
                    if (closeBtn != null) closeButton = closeBtn.GetComponent<Button>();
                }
                
                if (coinCountText == null)
                {
                    Transform countText = header.Find("CoinCountText");
                    if (countText != null) coinCountText = countText.GetComponent<TMP_Text>();
                }
            }
            
            // Find MapContainer
            Transform mapCont = transform.Find("MapContainer");
            if (mapCont != null)
            {
                if (mapContainer == null)
                {
                    mapContainer = mapCont.GetComponent<RectTransform>();
                }
                
                if (playerMarker == null)
                {
                    Transform marker = mapCont.Find("PlayerMarker");
                    if (marker != null) playerMarker = marker.GetComponent<RectTransform>();
                }
            }
            
            // Find SelectionPanel children
            Transform selPanel = transform.Find("SelectionPanel");
            if (selPanel != null)
            {
                if (selectionPanel == null)
                {
                    selectionPanel = selPanel.gameObject;
                }
                
                if (selectedCoinValue == null)
                {
                    Transform valText = selPanel.Find("SelectedCoinValue");
                    if (valText != null) selectedCoinValue = valText.GetComponent<TMP_Text>();
                }
                
                if (selectedCoinDistance == null)
                {
                    Transform distText = selPanel.Find("SelectedCoinDistance");
                    if (distText != null) selectedCoinDistance = distText.GetComponent<TMP_Text>();
                }
                
                if (selectedCoinStatus == null)
                {
                    Transform statText = selPanel.Find("SelectedCoinStatus");
                    if (statText != null) selectedCoinStatus = statText.GetComponent<TMP_Text>();
                }
                
                if (huntButton == null)
                {
                    Transform huntBtn = selPanel.Find("HuntButton");
                    if (huntBtn != null) huntButton = huntBtn.GetComponent<Button>();
                }
                
                if (cancelButton == null)
                {
                    Transform cancelBtn = selPanel.Find("CancelButton");
                    if (cancelBtn != null) cancelButton = cancelBtn.GetComponent<Button>();
                }
            }
            
            Log($"AutoFindReferences complete - mapContainer:{mapContainer != null}, huntButton:{huntButton != null}");
        }
        
        private void OnDestroy()
        {
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnKnownCoinsUpdated -= OnKnownCoinsUpdated;
                CoinManager.Instance.OnHuntModeChanged -= OnHuntModeChanged;
            }
        }
        
        private void Update()
        {
            if (!IsVisible) return;
            
            // Update map periodically
            UpdateMapDisplay();
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnKnownCoinsUpdated(List<Coin> coins)
        {
            if (IsVisible)
            {
                RefreshMap();
            }
        }
        
        private void OnHuntModeChanged(HuntMode mode)
        {
            if (mode == HuntMode.Hunting || mode == HuntMode.Collecting)
            {
                // Auto-close map when hunting starts
                Hide();
            }
        }
        
        private void OnHuntButtonClicked()
        {
            if (PreviewCoin != null)
            {
                Log($"Hunt requested for: {PreviewCoin.id}");
                
                // Set target and start hunting
                if (CoinManager.Exists)
                {
                    CoinManager.Instance.SetTargetCoin(PreviewCoin);
                }
                
                OnHuntRequested?.Invoke(PreviewCoin);
                Hide();
            }
        }
        
        private void OnCancelButtonClicked()
        {
            ClearSelection();
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show the full map
        /// </summary>
        public void Show()
        {
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: Show() called");
            // Start coroutine to wait for GPS then show map
            StartCoroutine(ShowWithGPSWait());
        }
        
        /// <summary>
        /// Wait for GPS to be ready before showing the map.
        /// This fixes the "click twice to load" bug.
        /// </summary>
        private System.Collections.IEnumerator ShowWithGPSWait()
        {
            float startTime = Time.realtimeSinceStartup;
            Debug.Log($"[FullMapUI] T+{startTime:F2}s: ShowWithGPSWait coroutine STARTED");
            
            // Check initial GPS state
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: GPSManager.Exists={GPSManager.Exists}");
            if (GPSManager.Exists)
            {
                Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: GPSManager.Instance.IsTracking={GPSManager.Instance.IsTracking}");
                Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: GPSManager.Instance.ServiceState={GPSManager.Instance.ServiceState}");
            }
            
            LocationData playerLocation = GetPlayerLocation();
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: Initial GetPlayerLocation() = {(playerLocation != null ? $"valid ({playerLocation.latitude:F6}, {playerLocation.longitude:F6})" : "NULL")}");
            
            // If GPS not ready, wait up to 3 seconds
            float waitTime = 0f;
            float maxWait = 3f;
            
            while (playerLocation == null && waitTime < maxWait)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
                playerLocation = GetPlayerLocation();
                
                // Log every 0.5 seconds
                if (waitTime % 0.5f < 0.1f)
                {
                    Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: Waiting for GPS... ({waitTime:F1}s/{maxWait:F0}s), location={(playerLocation != null ? "READY" : "null")}");
                }
            }
            
            if (playerLocation == null)
            {
                Debug.LogWarning($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: GPS NOT READY after {waitTime:F1}s! Showing map anyway...");
                
                // Extra debugging - why is GPS not ready?
                if (GPSManager.Exists)
                {
                    Debug.LogWarning($"[FullMapUI]   - IsTracking: {GPSManager.Instance.IsTracking}");
                    Debug.LogWarning($"[FullMapUI]   - ServiceState: {GPSManager.Instance.ServiceState}");
                    Debug.LogWarning($"[FullMapUI]   - CurrentLocation: {(GPSManager.Instance.CurrentLocation != null ? "exists" : "NULL")}");
                    Debug.LogWarning($"[FullMapUI]   - Input.location.status: {Input.location.status}");
                }
            }
            else
            {
                Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: GPS READY after {waitTime:F1}s! Lat={playerLocation.latitude:F6}, Lng={playerLocation.longitude:F6}");
            }
            
            // NOW show the panel and load data
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: Activating map panel...");
            if (mapPanel != null)
            {
                mapPanel.SetActive(true);
                Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: Map panel activated");
            }
            else
            {
                Debug.LogError($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: mapPanel is NULL!");
            }
            
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: Calling ClearSelection()...");
            ClearSelection();
            
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: Calling RefreshMap()...");
            RefreshMap();
            
            float totalTime = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: Map opened! Total show time: {totalTime:F2}s");
            OnMapOpened?.Invoke();
        }
        
        /// <summary>
        /// Hide the full map
        /// </summary>
        public void Hide()
        {
            if (mapPanel != null)
            {
                mapPanel.SetActive(false);
            }
            
            ClearSelection();
            
            Log("Map closed");
            OnMapClosed?.Invoke();
        }
        
        /// <summary>
        /// Toggle map visibility
        /// </summary>
        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }
        
        #endregion
        
        #region Map Display
        
        /// <summary>
        /// Refresh the entire map
        /// </summary>
        public void RefreshMap()
        {
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: RefreshMap() called");
            
            if (!CoinManager.Exists)
            {
                Debug.LogWarning($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: RefreshMap - CoinManager does not exist!");
                return;
            }
            
            var knownCoins = CoinManager.Instance.KnownCoins;
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: RefreshMap - KnownCoins count: {knownCoins?.Count ?? 0}");
            
            LocationData playerLocation = GetPlayerLocation();
            
            if (playerLocation == null)
            {
                Debug.LogWarning($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: RefreshMap - playerLocation is NULL, cannot refresh!");
                
                // Extra debug info
                if (GPSManager.Exists)
                {
                    Debug.LogWarning($"[FullMapUI]   - GPSManager.IsTracking: {GPSManager.Instance.IsTracking}");
                    Debug.LogWarning($"[FullMapUI]   - GPSManager.CurrentLocation: {(GPSManager.Instance.CurrentLocation != null ? "exists" : "NULL")}");
                }
                
                return;
            }
            
            Debug.Log($"[FullMapUI] T+{Time.realtimeSinceStartup:F2}s: RefreshMap - playerLocation valid: {playerLocation.latitude:F6}, {playerLocation.longitude:F6}");
            
            // Update header info
            UpdateHeaderInfo(knownCoins);
            
            // Track which coins we've updated
            HashSet<string> updatedCoins = new HashSet<string>();
            
            foreach (var coin in knownCoins)
            {
                // Calculate distance and bearing
                float distance = GeoUtils.CalculateDistance(
                    playerLocation.latitude, playerLocation.longitude,
                    coin.latitude, coin.longitude
                );
                
                float bearing = GeoUtils.CalculateBearing(
                    playerLocation.latitude, playerLocation.longitude,
                    coin.latitude, coin.longitude
                );
                
                // Update or create marker
                UpdateCoinMarker(coin, distance, bearing);
                updatedCoins.Add(coin.id);
            }
            
            // Remove markers for coins no longer known
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
        }
        
        /// <summary>
        /// Update map display (called each frame when visible)
        /// </summary>
        private void UpdateMapDisplay()
        {
            // Could add animations, compass rotation, etc.
        }
        
        /// <summary>
        /// Update header with coin count and value
        /// </summary>
        private void UpdateHeaderInfo(List<Coin> coins)
        {
            if (coinCountText != null)
            {
                coinCountText.text = $"{coins.Count} Coins Nearby";
            }
            
            if (totalValueText != null)
            {
                float total = 0f;
                foreach (var coin in coins)
                {
                    total += coin.value;
                }
                totalValueText.text = $"Total: ${total:F2}";
            }
        }
        
        #endregion
        
        #region Coin Markers
        
        /// <summary>
        /// Update or create a coin marker
        /// </summary>
        private void UpdateCoinMarker(Coin coin, float distance, float bearing)
        {
            RectTransform marker;
            
            if (!coinMarkers.TryGetValue(coin.id, out marker))
            {
                // Create new marker
                marker = GetMarkerFromPool();
                marker.SetParent(mapContainer);
                marker.gameObject.SetActive(true);
                coinMarkers[coin.id] = marker;
                
                // Add click handler
                Button btn = marker.GetComponent<Button>();
                if (btn == null) btn = marker.gameObject.AddComponent<Button>();
                
                string coinId = coin.id; // Capture for closure
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnCoinMarkerClicked(coinId));
            }
            
            // Calculate position on map
            float bearingRad = bearing * Mathf.Deg2Rad;
            float normalizedDistance = Mathf.Clamp01(distance / mapRange);
            float pixelDistance = normalizedDistance * mapRadius;
            
            // Position (0,0 is center, up is north)
            float x = Mathf.Sin(bearingRad) * pixelDistance;
            float y = Mathf.Cos(bearingRad) * pixelDistance;
            
            marker.anchoredPosition = new Vector2(x, y);
            
            // Set color based on state
            Image markerImage = marker.GetComponent<Image>();
            if (markerImage != null)
            {
                if (coin.id == selectedMarkerId)
                {
                    markerImage.color = selectedCoinColor;
                }
                else if (coin.isLocked)
                {
                    markerImage.color = lockedCoinColor;
                }
                else if (coin.coinType == CoinType.Pool)
                {
                    markerImage.color = poolCoinColor;
                }
                else
                {
                    markerImage.color = normalCoinColor;
                }
            }
            
            // Scale based on value (bigger = more valuable)
            float scale = Mathf.Lerp(1f, 2f, Mathf.Clamp01(coin.value / 25f));
            marker.localScale = Vector3.one * scale;
            
            // Update value label if exists
            TMP_Text valueLabel = marker.GetComponentInChildren<TMP_Text>();
            if (valueLabel != null)
            {
                valueLabel.text = coin.GetDisplayValue();
            }
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
        /// Handle coin marker click
        /// </summary>
        private void OnCoinMarkerClicked(string coinId)
        {
            Log($"Coin marker clicked: {coinId}");
            
            Coin coin = CoinManager.Instance?.GetKnownCoinById(coinId);
            if (coin != null)
            {
                SelectCoin(coin);
            }
        }
        
        #endregion
        
        #region Selection
        
        /// <summary>
        /// Select a coin for preview
        /// </summary>
        public void SelectCoin(Coin coin)
        {
            PreviewCoin = coin;
            selectedMarkerId = coin.id;
            
            // Update selection panel
            ShowSelectionPanel(coin);
            
            // Refresh markers to update colors
            RefreshMap();
            
            Log($"Selected coin: {coin.id}, Value: {coin.GetDisplayValue()}");
            OnCoinPreviewed?.Invoke(coin);
        }
        
        /// <summary>
        /// Clear current selection
        /// </summary>
        public void ClearSelection()
        {
            PreviewCoin = null;
            selectedMarkerId = null;
            
            HideSelectionPanel();
            RefreshMap();
        }
        
        /// <summary>
        /// Show selection panel with coin details
        /// </summary>
        private void ShowSelectionPanel(Coin coin)
        {
            if (selectionPanel == null) return;
            
            selectionPanel.SetActive(true);
            
            // Get distance
            LocationData playerLocation = GetPlayerLocation();
            float distance = 0f;
            if (playerLocation != null)
            {
                distance = GeoUtils.CalculateDistance(
                    playerLocation.latitude, playerLocation.longitude,
                    coin.latitude, coin.longitude
                );
            }
            
            // Update UI
            if (selectedCoinValue != null)
            {
                selectedCoinValue.text = coin.GetDisplayValue();
                selectedCoinValue.color = coin.isLocked ? lockedCoinColor : normalCoinColor;
            }
            
            if (selectedCoinDistance != null)
            {
                selectedCoinDistance.text = GeoUtils.FormatDistance(distance);
            }
            
            if (selectedCoinStatus != null)
            {
                if (coin.isLocked)
                {
                    selectedCoinStatus.text = "🔒 LOCKED - Above your find limit";
                    selectedCoinStatus.color = lockedCoinColor;
                }
                else
                {
                    selectedCoinStatus.text = "🎯 Available to collect!";
                    selectedCoinStatus.color = selectedCoinColor;
                }
            }
            
            // Enable/disable hunt button based on locked status
            if (huntButton != null)
            {
                huntButton.interactable = !coin.isLocked;
            }
        }
        
        /// <summary>
        /// Hide selection panel
        /// </summary>
        private void HideSelectionPanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
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
                // Create default marker
                markerObj = new GameObject("CoinMarker");
                Image img = markerObj.AddComponent<Image>();
                img.color = normalCoinColor;
                
                RectTransform rt = markerObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(24, 24);
                
                // Add value label
                GameObject labelObj = new GameObject("ValueLabel");
                labelObj.transform.SetParent(markerObj.transform);
                TMP_Text label = labelObj.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 10;
                label.color = Color.white;
                
                RectTransform labelRt = labelObj.GetComponent<RectTransform>();
                labelRt.anchoredPosition = new Vector2(0, -20);
                labelRt.sizeDelta = new Vector2(60, 20);
            }
            
            return markerObj.GetComponent<RectTransform>();
        }
        
        private void ReturnMarkerToPool(RectTransform marker)
        {
            marker.gameObject.SetActive(false);
            markerPool.Enqueue(marker);
        }
        
        #endregion
        
        #region Helpers
        
        private LocationData GetPlayerLocation()
        {
            if (GPSManager.Exists && GPSManager.Instance.CurrentLocation != null)
            {
                return GPSManager.Instance.CurrentLocation;
            }
            return null;
        }
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[FullMapUI] {message}");
            }
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Toggle Map")]
        public void DebugToggle()
        {
            Toggle();
        }
        
        [ContextMenu("Debug: Select First Coin")]
        public void DebugSelectFirst()
        {
            if (CoinManager.Exists && CoinManager.Instance.KnownCoins.Count > 0)
            {
                SelectCoin(CoinManager.Instance.KnownCoins[0]);
            }
        }
        
        #endregion
    }
}
