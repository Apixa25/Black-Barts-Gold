// ============================================================================
// UIManager.cs
// Black Bart's Gold - Market Standard UI Management
// Path: Assets/Scripts/Core/UIManager.cs
// ============================================================================
// MARKET STANDARD PATTERN: Single persistent UI manager that handles all 
// navigation through showing/hiding panels rather than loading scenes.
// This is how Pokemon Go, Clash of Clans, and other top mobile games work.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using BlackBartsGold.Location;
using BlackBartsGold.AR;
using BlackBartsGold.Core.Models;
using BlackBartsGold.UI;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Persistent UI Manager - survives scene changes.
    /// Handles all menu navigation through panel visibility.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        
        private static UIManager _instance;
        public static UIManager Instance => _instance;
        
        #endregion
        
        #region UI Panels
        
        [Header("UI Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject walletPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject arHudPanel;
        
        #endregion
        
        #region Colors (Pirate Theme)
        
        public static readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        public static readonly Color DeepSeaBlue = new Color(0.102f, 0.212f, 0.365f);
        public static readonly Color Parchment = new Color(0.961f, 0.902f, 0.827f);
        public static readonly Color DarkBrown = new Color(0.239f, 0.161f, 0.078f);
        public static readonly Color SemiTransparent = new Color(0, 0, 0, 0.7f);
        
        #endregion
        
        #region State
        
        private GameObject currentPanel;
        private bool isInARMode = false;
        
        public bool IsInARMode => isInARMode;
        
        #endregion
        
        #region Debug Diagnostic Panel
        
        // References for the AR diagnostic panel
        private TextMeshProUGUI _coinCounterText;
        private TextMeshProUGUI _debugDiagnosticsText;
        private float _diagnosticUpdateInterval = 0.5f;
        private float _lastDiagnosticUpdate = 0f;
        
        // Mini-map references
        private RectTransform _miniMapContainer;
        private RectTransform _playerDot;
        private Dictionary<string, RectTransform> _coinDots = new Dictionary<string, RectTransform>();
        private float _miniMapRange = 50f; // meters
        private float _miniMapRadius = 100f; // pixels
        
        // Mapbox real map integration
        private RawImage _miniMapImage;
        private Texture2D _currentMapTile;
        private float _lastMapUpdateTime = 0f;
        private float _mapUpdateInterval = 2f; // Update map every 2 seconds
        private double _lastMapLat = 0;
        private double _lastMapLng = 0;
        private float _lastMapBearing = 0f;
        private bool _mapUpdatePending = false;
        
        // Compass smoothing for mini-map (reduces jitter)
        private float _smoothedCompassHeading = 0f;
        private float _compassVelocity = 0f;
        private float _compassSmoothTime = 0.2f; // Smoothing factor
        
        // Reveal Coin button (proper AR anchor system)
        private GameObject _placeCoinButton;
        private TextMeshProUGUI _placeCoinButtonText;
        private Coin _coinToPlace; // The coin ready to be placed in AR
        
        #endregion
        
        #region Unity Lifecycle
        
        private Canvas _ourCanvas;
        
        private void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Debug.Log("[UIManager] Duplicate found, destroying...");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[UIManager] Initialized - Market Standard UI Pattern");
            
            // Subscribe to scene loaded to clean up scene-based UI
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Build UI if not already set up
            if (loginPanel == null)
            {
                BuildUI();
            }
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void Start()
        {
            // Show login by default
            ShowLogin();
        }
        
        private void Update()
        {
            // Update diagnostic panel when in AR mode
            if (isInARMode && Time.time - _lastDiagnosticUpdate >= _diagnosticUpdateInterval)
            {
                _lastDiagnosticUpdate = Time.time;
                UpdateDiagnosticsPanel();
            }
        }
        
        /// <summary>
        /// Update the AR diagnostics panel with real-time info
        /// </summary>
        private void UpdateDiagnosticsPanel()
        {
            // Update coin counter
            if (_coinCounterText != null && CoinManager.Instance != null)
            {
                int activeCoins = CoinManager.Instance.ActiveCoinCount;
                _coinCounterText.text = $"Coins: {activeCoins}";
            }
            
            // Update debug diagnostics
            if (_debugDiagnosticsText != null)
            {
                string diagnostics = BuildDiagnosticsString();
                _debugDiagnosticsText.text = diagnostics;
            }
            
            // Update mini-map
            UpdateMiniMap();
            
            // Update Place Coin button (proper AR anchor system)
            UpdatePlaceCoinButton();
        }
        
        /// <summary>
        /// Build the diagnostics string with current system state
        /// </summary>
        private string BuildDiagnosticsString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // ================================================================
            // AR TRACKING STATUS - This is the critical issue!
            // ARCore must be tracking before planes can be detected
            // ================================================================
            var arSession = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
            if (arSession != null)
            {
                var state = UnityEngine.XR.ARFoundation.ARSession.state;
                string stateColor = state == UnityEngine.XR.ARFoundation.ARSessionState.SessionTracking 
                    ? "green" : "red";
                sb.AppendLine($"<b>AR:</b> <color={stateColor}>{state}</color>");
            }
            else
            {
                sb.AppendLine("<b>AR:</b> <color=red>NO SESSION</color>");
            }
            
            // Check plane detection - needed for "Reveal Coin"
            var planeManager = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARPlaneManager>();
            if (planeManager != null)
            {
                int planeCount = planeManager.trackables.count;
                if (planeCount > 0)
                {
                    sb.AppendLine($"<b>Planes:</b> <color=green>{planeCount} detected</color>");
                }
                else
                {
                    sb.AppendLine($"<b>Planes:</b> <color=yellow>Scanning...</color>");
                }
            }
            else
            {
                sb.AppendLine("<b>Planes:</b> <color=red>No PlaneManager</color>");
            }
            
            // API Config
            sb.AppendLine($"<b>API:</b> {ApiConfig.CurrentEnvironment}");
            sb.AppendLine($"Mock: {ApiConfig.UseMockApi}");
            
            // GPS Status
            if (GPSManager.Instance != null)
            {
                var loc = GPSManager.Instance.CurrentLocation;
                if (loc != null)
                {
                    sb.AppendLine($"<b>GPS:</b> {loc.latitude:F5}, {loc.longitude:F5}");
                    sb.AppendLine($"Accuracy: {loc.horizontalAccuracy:F0}m");
                }
                else
                {
                    sb.AppendLine("<b>GPS:</b> <color=red>No location</color>");
                }
                sb.AppendLine($"Tracking: {GPSManager.Instance.IsTracking}");
            }
            else
            {
                sb.AppendLine("<b>GPS:</b> <color=red>Not initialized</color>");
            }
            
            // Compass heading (for debugging AR alignment)
            if (Input.compass.enabled)
            {
                sb.AppendLine($"<b>Compass:</b> {Input.compass.trueHeading:F0}°");
            }
            
            // Camera reference (needed for direction calculations)
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindFirstObjectByType<Camera>();
            }
            
            // Coin Manager with position info
            if (CoinManager.Instance != null)
            {
                sb.AppendLine($"<b>Active Coins:</b> {CoinManager.Instance.ActiveCoinCount}");
                
                // Show first coin's position for debugging
                if (CoinManager.Instance.ActiveCoinCount > 0)
                {
                    var firstCoin = CoinManager.Instance.ActiveCoins[0];
                    if (firstCoin != null && firstCoin.CoinData != null && GPSManager.Instance != null)
                    {
                        var playerLoc = GPSManager.Instance.CurrentLocation;
                        var coinData = firstCoin.CoinData;
                        
                        sb.AppendLine($"Coin1 Dist: {firstCoin.DistanceFromPlayer:F1}m");
                        
                        // ================================================================
                        // COMPASS-RELATIVE direction (since AR tracking isn't working)
                        // Use GPS bearing adjusted by compass heading - same as mini-map
                        // ================================================================
                        if (playerLoc != null)
                        {
                            // Calculate GPS bearing from player to coin
                            var coinLoc = new LocationData(coinData.latitude, coinData.longitude);
                            float gpsBearing = (float)playerLoc.BearingTo(coinLoc);
                            
                            // Adjust by compass heading (so "front" = direction you're facing)
                            float compassHeading = Input.compass.enabled ? Input.compass.trueHeading : 0f;
                            float relativeBearing = gpsBearing - compassHeading;
                            
                            // Normalize to -180 to 180
                            while (relativeBearing > 180) relativeBearing -= 360;
                            while (relativeBearing < -180) relativeBearing += 360;
                            
                            // Convert to direction words
                            // 0° = directly ahead, 90° = right, 180° = behind, -90° = left
                            string direction;
                            if (relativeBearing >= -45 && relativeBearing <= 45)
                                direction = "FRONT";
                            else if (relativeBearing > 45 && relativeBearing < 135)
                                direction = "RIGHT";
                            else if (relativeBearing >= 135 || relativeBearing <= -135)
                                direction = "BEHIND";
                            else
                                direction = "LEFT";
                            
                            sb.AppendLine($"<b>Look:</b> {direction}");
                            sb.AppendLine($"<b>Bearing:</b> {relativeBearing:F0}°");
                        }
                    }
                }
            }
            
            // Camera position and rotation for debugging AR tracking
            if (cam != null)
            {
                Vector3 camPos = cam.transform.position;
                Vector3 camRot = cam.transform.eulerAngles;
                sb.AppendLine($"<b>Cam:</b> {cam.name}"); // WHICH camera are we using?
                sb.AppendLine($"<b>Cam Pos:</b> ({camPos.x:F1}, {camPos.y:F1}, {camPos.z:F1})");
                sb.AppendLine($"<b>Cam RotY:</b> {camRot.y:F0}°"); // Just Y rotation (should change when turning!)
            }
            else
            {
                sb.AppendLine("<b>Cam:</b> <color=red>NOT FOUND!</color>");
            }
            
            // Coin API Cache
            if (CoinApiService.Exists)
            {
                var cached = CoinApiService.Instance.GetCachedCoins();
                sb.AppendLine($"<b>Cached:</b> {cached.Count}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Clean up any scene-based UI after scene loads.
        /// We only want OUR Canvas to be active.
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[UIManager] Scene loaded: {scene.name} - cleaning up scene UI");
            
            // Find all Canvas objects in the scene
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            
            foreach (var canvas in allCanvases)
            {
                // Skip our own canvas
                if (canvas == _ourCanvas)
                {
                    continue;
                }
                
                // Skip if it's a child of our UIManager (our canvas)
                if (canvas.transform.IsChildOf(transform))
                {
                    continue;
                }
                
                // This is a scene-based Canvas - destroy it!
                Debug.Log($"[UIManager] Destroying scene-based Canvas: {canvas.gameObject.name}");
                Destroy(canvas.gameObject);
            }
            
            // Show appropriate UI based on which scene loaded
            if (scene.name == "ARHunt")
            {
                // Show AR HUD
                Debug.Log("[UIManager] ARHunt scene - showing AR HUD");
                if (arHudPanel != null) arHudPanel.SetActive(true);
                
                // Fetch real coins from API after AR initializes
                StartCoroutine(FetchCoinsFromAPI());
            }
            else if (isInARMode == false)
            {
                // Not in AR mode and scene loaded - make sure main menu is visible
                // (This handles returning from AR)
            }
        }
        
        /// <summary>
        /// Fetch real coins from the API after AR scene loads.
        /// Waits for GPS to be ready, then calls the coin API.
        /// </summary>
        private IEnumerator FetchCoinsFromAPI()
        {
            Debug.Log("[UIManager] 🗺️ Starting coin fetch from API...");
            
            // DIAGNOSTIC: Log current API configuration
            Debug.Log("═══════════════════════════════════════════════════");
            Debug.Log("[DIAGNOSTIC] API Configuration:");
            Debug.Log($"  Environment: {ApiConfig.CurrentEnvironment}");
            Debug.Log($"  Use Mock API: {ApiConfig.UseMockApi}");
            Debug.Log($"  Base URL: {ApiConfig.GetBaseUrl()}");
            Debug.Log($"  Is Production: {ApiConfig.IsProduction()}");
            Debug.Log("═══════════════════════════════════════════════════");
            
            // Wait for AR to initialize
            yield return new WaitForSeconds(1.5f);
            
            // START THE GPS SERVICE! (This was missing before)
            Debug.Log("[UIManager] 📍 Starting GPS service...");
            GPSManager.Instance.StartLocationService();
            
            // Wait for GPS to be ready (max 30 seconds)
            float gpsTimeout = 30f;
            float elapsed = 0f;
            
            Debug.Log("[UIManager] 📍 Waiting for GPS to get a fix...");
            
            while (!GPSManager.Instance.IsTracking && elapsed < gpsTimeout)
            {
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
                
                if (elapsed % 5f < 0.5f) // Log every 5 seconds
                {
                    Debug.Log($"[UIManager] Still waiting for GPS... ({elapsed:F0}s)");
                }
            }
            
            if (!GPSManager.Instance.IsTracking)
            {
                Debug.LogWarning("[UIManager] ⚠️ GPS timeout! Using last known location or mock data.");
            }
            
            // Get current location
            LocationData location = GPSManager.Instance.CurrentLocation ?? GPSManager.Instance.LastKnownLocation;
            
            if (location == null)
            {
                Debug.LogWarning("[UIManager] ⚠️ No GPS location available. Cannot fetch coins.");
                // Could show user a message here
                yield break;
            }
            
            Debug.Log($"[UIManager] 📍 GPS ready! Location: ({location.latitude:F6}, {location.longitude:F6})");
            
            // Fetch coins from API
            yield return FetchAndSpawnCoins(location.latitude, location.longitude);
            
            // Start periodic refresh coroutine
            StartCoroutine(PeriodicCoinRefresh());
        }
        
        /// <summary>
        /// Fetch coins from the API and spawn them via CoinManager.
        /// This is an async operation wrapped in a coroutine.
        /// </summary>
        private IEnumerator FetchAndSpawnCoins(double latitude, double longitude)
        {
            Debug.Log($"[UIManager] 🪙 Fetching coins near ({latitude:F6}, {longitude:F6})...");
            
            List<Coin> coins = null;
            System.Exception caughtException = null;
            bool fetchComplete = false;
            
            // Call the async API method
            var fetchTask = CoinApiService.Instance.GetNearbyCoins(latitude, longitude, 500f);
            
            // Poll for task completion on the main thread (Unity-safe approach)
            float timeout = 15f;
            float waitTime = 0f;
            
            while (!fetchTask.IsCompleted && waitTime < timeout)
            {
                waitTime += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
            
            // Now check result on main thread
            if (fetchTask.IsFaulted)
            {
                caughtException = fetchTask.Exception?.InnerException ?? fetchTask.Exception;
                Debug.LogError($"[UIManager] ❌ API Error: {caughtException?.Message}");
                Debug.LogError($"[UIManager] Stack trace: {caughtException?.StackTrace}");
                yield break;
            }
            else if (fetchTask.IsCompleted && !fetchTask.IsCanceled)
            {
                coins = fetchTask.Result;
                fetchComplete = true;
                Debug.Log($"[UIManager] ✅ Received {coins?.Count ?? 0} coins from API");
            }
            
            if (!fetchComplete)
            {
                Debug.LogError("[UIManager] ❌ API request timed out or was cancelled!");
                yield break;
            }
            
            if (coins != null && coins.Count > 0)
            {
                Debug.Log($"[UIManager] 🎯 Passing {coins.Count} coins to CoinManager");
                
                // Log each coin for debugging
                foreach (var coin in coins)
                {
                    Debug.Log($"[UIManager] Coin to spawn: ID={coin.id}, Value=${coin.value:F2}, Status={coin.status}, Lat={coin.latitude:F6}, Lng={coin.longitude:F6}");
                }
                
                // Pass coins to CoinManager for AR spawning
                // NEW ARCHITECTURE: CoinManager uses ARCoinPositioner for GPS-to-AR conversion
                if (CoinManager.Instance != null)
                {
                    CoinManager.Instance.SetNearbyCoins(coins);
                    Debug.Log("[UIManager] ✅ Coins passed to CoinManager (new architecture)");
                }
                else
                {
                    Debug.LogError("[UIManager] ❌ CoinManager not found!");
                }
            }
            else
            {
                Debug.LogWarning("[UIManager] ⚠️ No coins found nearby! Make sure you've placed coins in the admin dashboard near your current GPS location.");
            }
        }
        
        /// <summary>
        /// Periodically refresh coins when the player moves significantly.
        /// </summary>
        private IEnumerator PeriodicCoinRefresh()
        {
            Debug.Log("[UIManager] 🔄 Starting periodic coin refresh...");
            
            float refreshInterval = 30f; // Refresh every 30 seconds
            float lastRefreshLat = 0;
            float lastRefreshLng = 0;
            float minMovementForRefresh = 50f; // Meters
            
            while (isInARMode)
            {
                yield return new WaitForSeconds(refreshInterval);
                
                if (!GPSManager.Instance.IsTracking) continue;
                
                var location = GPSManager.Instance.CurrentLocation;
                if (location == null) continue;
                
                // Calculate distance from last refresh
                float distance = CalculateDistance(
                    lastRefreshLat, lastRefreshLng,
                    (float)location.latitude, (float)location.longitude
                );
                
                // Only refresh if we've moved enough
                if (distance > minMovementForRefresh || lastRefreshLat == 0)
                {
                    Debug.Log($"[UIManager] 🔄 Refreshing coins (moved {distance:F0}m)");
                    lastRefreshLat = (float)location.latitude;
                    lastRefreshLng = (float)location.longitude;
                    
                    yield return FetchAndSpawnCoins(location.latitude, location.longitude);
                }
            }
            
            Debug.Log("[UIManager] 🔄 Stopped periodic coin refresh (left AR mode)");
        }
        
        /// <summary>
        /// Calculate approximate distance between two GPS points in meters.
        /// Uses Haversine formula.
        /// </summary>
        private float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
        {
            const float R = 6371000; // Earth's radius in meters
            
            float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
            float dLon = (lon2 - lon1) * Mathf.Deg2Rad;
            
            float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                      Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad) *
                      Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
            
            float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
            
            return R * c;
        }
        
        /// <summary>
        /// [DEBUG ONLY] Spawn test coins in front of camera (3-6 feet away).
        /// This is only used when API is unavailable or for testing.
        /// </summary>
        private void SpawnDebugTestCoins()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[UIManager] No camera found for coin spawning");
                return;
            }
            
            // Spawn 3 coins at different distances (in meters)
            // 3 feet ≈ 1m, 4.5 feet ≈ 1.4m, 6 feet ≈ 1.8m
            SpawnCoin(cam, 1.0f, -15f, 1.00f);   // Left, close
            SpawnCoin(cam, 1.4f, 0f, 5.00f);     // Center, medium
            SpawnCoin(cam, 1.8f, 15f, 10.00f);   // Right, far
            
            Debug.Log("[UIManager] Test coins spawned!");
        }
        
        /// <summary>
        /// Spawn a single gold coin
        /// </summary>
        private void SpawnCoin(Camera cam, float distance, float angleOffset, float value)
        {
            // Calculate position relative to camera
            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;
            
            // Apply horizontal angle offset
            Vector3 direction = Quaternion.AngleAxis(angleOffset, Vector3.up) * forward;
            Vector3 position = cam.transform.position + direction * distance;
            
            // Place at waist height (below camera)
            position.y = cam.transform.position.y - 0.5f;
            
            // Create coin object
            GameObject coinObj = new GameObject($"TestCoin_${value}");
            coinObj.transform.position = position;
            
            // Create visual (gold cylinder)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "CoinVisual";
            visual.transform.SetParent(coinObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f); // Flat coin shape
            
            // Gold material
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material goldMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (goldMat != null)
                {
                    goldMat.color = GoldColor;
                    goldMat.SetFloat("_Smoothness", 0.9f);
                    goldMat.SetFloat("_Metallic", 1f);
                    renderer.material = goldMat;
                }
            }
            
            // Remove collider
            var col = visual.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            // Add spin animation
            coinObj.AddComponent<CoinSpin>();
            
            Debug.Log($"[UIManager] Spawned ${value} coin at {position}");
        }
        
        #endregion
        
        #region Panel Navigation (The Simple Way!)
        
        /// <summary>
        /// Show the Login panel
        /// </summary>
        public void ShowLogin()
        {
            Debug.Log("[UIManager] Showing Login");
            HideAllPanels();
            if (loginPanel != null) loginPanel.SetActive(true);
            currentPanel = loginPanel;
        }
        
        /// <summary>
        /// Show the Register panel
        /// </summary>
        public void ShowRegister()
        {
            Debug.Log("[UIManager] Showing Register");
            HideAllPanels();
            if (registerPanel != null) registerPanel.SetActive(true);
            currentPanel = registerPanel;
        }
        
        /// <summary>
        /// Show the Main Menu panel
        /// </summary>
        public void ShowMainMenu()
        {
            Debug.Log("[UIManager] Showing MainMenu");
            HideAllPanels();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            currentPanel = mainMenuPanel;
        }
        
        /// <summary>
        /// Show the Wallet panel
        /// </summary>
        public void ShowWallet()
        {
            Debug.Log("[UIManager] Showing Wallet");
            HideAllPanels();
            if (walletPanel != null) walletPanel.SetActive(true);
            currentPanel = walletPanel;
        }
        
        /// <summary>
        /// Show the Settings panel
        /// </summary>
        public void ShowSettings()
        {
            Debug.Log("[UIManager] Showing Settings");
            HideAllPanels();
            if (settingsPanel != null) settingsPanel.SetActive(true);
            currentPanel = settingsPanel;
        }
        
        /// <summary>
        /// Enter AR Hunt mode - this is the only time we load a different scene
        /// </summary>
        public void StartARHunt()
        {
            Debug.Log("[UIManager] Starting AR Hunt");
            isInARMode = true;
            HideAllPanels();
            
            // Load AR scene - HUD will be shown after scene loads
            SceneManager.LoadScene("ARHunt", LoadSceneMode.Single);
        }
        
        /// <summary>
        /// Exit AR mode and return to main menu
        /// </summary>
        public void ExitARHunt()
        {
            Debug.Log("[UIManager] Exiting AR Hunt");
            isInARMode = false;
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            
            // After scene loads, show main menu
            StartCoroutine(ShowMainMenuAfterLoad());
        }
        
        private IEnumerator ShowMainMenuAfterLoad()
        {
            yield return null; // Wait one frame
            ShowMainMenu();
        }
        
        /// <summary>
        /// Called when mini-map is clicked - opens full map view
        /// </summary>
        public void OnMiniMapClicked()
        {
            Debug.Log("[UIManager] 🗺️ Opening FULL MAP!");
            
            // Try to use existing FullMapUI if it exists
            if (FullMapUI.Exists)
            {
                Debug.Log("[UIManager] Using FullMapUI.Show()");
                FullMapUI.Instance.Show();
                return;
            }
            
            // Otherwise create a simple full-screen map overlay
            Debug.Log("[UIManager] Creating simple full map overlay");
            ShowSimpleFullMap();
        }
        
        private GameObject _simpleFullMapPanel;
        
        /// <summary>
        /// Show a simple full-screen map overlay
        /// </summary>
        private void ShowSimpleFullMap()
        {
            // If already showing, hide it
            if (_simpleFullMapPanel != null && _simpleFullMapPanel.activeSelf)
            {
                Debug.Log("[UIManager] Hiding full map");
                _simpleFullMapPanel.SetActive(false);
                return;
            }
            
            // Create if doesn't exist
            if (_simpleFullMapPanel == null)
            {
                _simpleFullMapPanel = CreateSimpleFullMapPanel();
            }
            else
            {
                // Refresh map and markers when reopening
                StartCoroutine(LoadFullMapTile());
            }
            
            _simpleFullMapPanel.SetActive(true);
            Debug.Log("[UIManager] Full map shown!");
        }
        
        private RawImage _fullMapImage;
        private Texture2D _fullMapTile;
        private RectTransform _fullMapContainer;
        private Transform _coinMarkersContainer;
        private Dictionary<string, GameObject> _fullMapCoinMarkers = new Dictionary<string, GameObject>();
        private double _fullMapCenterLat;
        private double _fullMapCenterLng;
        private int _fullMapZoom = 17; // Start zoomed in more
        private const int MIN_ZOOM = 14;
        private const int MAX_ZOOM = 19;
        private float _fullMapMetersPerPixel;
        private float _lastPinchDistance = 0f;
        private bool _isPinching = false;
        private TMP_Text _zoomLevelText;
        private bool _mapLoadPending = false;
        
        private GameObject CreateSimpleFullMapPanel()
        {
            var panel = new GameObject("SimpleFullMapPanel");
            panel.transform.SetParent(_ourCanvas.transform, false);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Dark background
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.1f, 0.15f, 1f);
            bg.raycastTarget = true;
            
            // === REAL MAP AREA ===
            var mapArea = new GameObject("MapArea");
            mapArea.transform.SetParent(panel.transform, false);
            var mapAreaRect = mapArea.AddComponent<RectTransform>();
            mapAreaRect.anchorMin = new Vector2(0.03f, 0.12f);
            mapAreaRect.anchorMax = new Vector2(0.97f, 0.88f);
            mapAreaRect.offsetMin = Vector2.zero;
            mapAreaRect.offsetMax = Vector2.zero;
            
            // Map border
            var mapBorder = mapArea.AddComponent<Image>();
            mapBorder.color = new Color(0.7f, 0.6f, 0.3f, 1f); // Gold border
            
            // Map container (inside border)
            var mapContainer = new GameObject("MapContainer");
            mapContainer.transform.SetParent(mapArea.transform, false);
            var mapContainerRect = mapContainer.AddComponent<RectTransform>();
            mapContainerRect.anchorMin = Vector2.zero;
            mapContainerRect.anchorMax = Vector2.one;
            mapContainerRect.offsetMin = new Vector2(4, 4);
            mapContainerRect.offsetMax = new Vector2(-4, -4);
            _fullMapContainer = mapContainerRect;
            
            // Map background (shows while loading)
            var mapBg = mapContainer.AddComponent<Image>();
            mapBg.color = new Color(0.15f, 0.2f, 0.25f, 1f);
            
            // Real map image
            var mapImageObj = new GameObject("MapImage");
            mapImageObj.transform.SetParent(mapContainer.transform, false);
            var mapImageRect = mapImageObj.AddComponent<RectTransform>();
            mapImageRect.anchorMin = Vector2.zero;
            mapImageRect.anchorMax = Vector2.one;
            mapImageRect.offsetMin = Vector2.zero;
            mapImageRect.offsetMax = Vector2.zero;
            
            _fullMapImage = mapImageObj.AddComponent<RawImage>();
            _fullMapImage.color = Color.white;
            
            // Coin markers container (overlay on map)
            var coinMarkers = new GameObject("CoinMarkers");
            coinMarkers.transform.SetParent(mapContainer.transform, false);
            var markersRect = coinMarkers.AddComponent<RectTransform>();
            markersRect.anchorMin = Vector2.zero;
            markersRect.anchorMax = Vector2.one;
            markersRect.offsetMin = Vector2.zero;
            markersRect.offsetMax = Vector2.zero;
            _coinMarkersContainer = coinMarkers.transform;
            
            // Player marker on map
            CreatePlayerMarkerOnFullMap(coinMarkers.transform);
            
            // "Loading..." text
            var loadingText = CreateText(mapContainer.transform, "LoadingText", "Loading map...", 
                Vector2.zero, 24, Color.gray, FontStyles.Italic);
            var loadingRect = loadingText.GetComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
            loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            
            // Title bar at top
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(panel.transform, false);
            var titleBarRect = titleBar.AddComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0, 0.88f);
            titleBarRect.anchorMax = new Vector2(1, 1);
            titleBarRect.offsetMin = Vector2.zero;
            titleBarRect.offsetMax = Vector2.zero;
            var titleBarBg = titleBar.AddComponent<Image>();
            titleBarBg.color = new Color(0.1f, 0.15f, 0.2f, 0.95f);
            
            // Title
            var title = CreateText(titleBar.transform, "Title", "TREASURE MAP", 
                Vector2.zero, 32, GoldColor, FontStyles.Bold);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            
            // Close button (X in top right)
            var closeBtn = CreateButton(titleBar.transform, "CloseButton", "✕", 
                Vector2.zero, new Vector2(50, 50), new Color(0.8f, 0.2f, 0.2f),
                () => {
                    Debug.Log("[UIManager] Closing full map");
                    _simpleFullMapPanel.SetActive(false);
                });
            var closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1, 0.5f);
            closeBtnRect.pivot = new Vector2(1, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-10, 0);
            
            // Bottom info bar with zoom controls
            var bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(panel.transform, false);
            var bottomRect = bottomBar.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0.11f);
            bottomRect.offsetMin = Vector2.zero;
            bottomRect.offsetMax = Vector2.zero;
            var bottomBg = bottomBar.AddComponent<Image>();
            bottomBg.color = new Color(0.1f, 0.15f, 0.2f, 0.95f);
            
            // Instructions (left side)
            var instructions = CreateText(bottomBar.transform, "Instructions", 
                "TAP COIN TO SELECT  •  PINCH TO ZOOM", 
                Vector2.zero, 14, Color.white, FontStyles.Normal);
            var instrRect = instructions.GetComponent<RectTransform>();
            instrRect.anchorMin = new Vector2(0, 0.5f);
            instrRect.anchorMax = new Vector2(0, 0.5f);
            instrRect.pivot = new Vector2(0, 0.5f);
            instrRect.anchoredPosition = new Vector2(15, 0);
            
            // Zoom controls (right side)
            var zoomControls = new GameObject("ZoomControls");
            zoomControls.transform.SetParent(bottomBar.transform, false);
            var zoomRect = zoomControls.AddComponent<RectTransform>();
            zoomRect.anchorMin = new Vector2(1, 0.5f);
            zoomRect.anchorMax = new Vector2(1, 0.5f);
            zoomRect.pivot = new Vector2(1, 0.5f);
            zoomRect.anchoredPosition = new Vector2(-15, 0);
            zoomRect.sizeDelta = new Vector2(180, 60);
            
            // Zoom OUT button (-)
            var zoomOutBtn = CreateButton(zoomControls.transform, "ZoomOut", "−", 
                Vector2.zero, new Vector2(50, 50), new Color(0.3f, 0.3f, 0.4f),
                () => ChangeMapZoom(-1));
            var zoomOutRect = zoomOutBtn.GetComponent<RectTransform>();
            zoomOutRect.anchorMin = new Vector2(0, 0.5f);
            zoomOutRect.anchorMax = new Vector2(0, 0.5f);
            zoomOutRect.pivot = new Vector2(0, 0.5f);
            zoomOutRect.anchoredPosition = new Vector2(0, 0);
            
            // Zoom level display
            _zoomLevelText = CreateText(zoomControls.transform, "ZoomLevel", $"{_fullMapZoom}x", 
                Vector2.zero, 18, GoldColor, FontStyles.Bold);
            var zoomLevelRect = _zoomLevelText.GetComponent<RectTransform>();
            zoomLevelRect.anchorMin = new Vector2(0.5f, 0.5f);
            zoomLevelRect.anchorMax = new Vector2(0.5f, 0.5f);
            zoomLevelRect.sizeDelta = new Vector2(50, 30);
            
            // Zoom IN button (+)
            var zoomInBtn = CreateButton(zoomControls.transform, "ZoomIn", "+", 
                Vector2.zero, new Vector2(50, 50), new Color(0.3f, 0.3f, 0.4f),
                () => ChangeMapZoom(1));
            var zoomInRect = zoomInBtn.GetComponent<RectTransform>();
            zoomInRect.anchorMin = new Vector2(1, 0.5f);
            zoomInRect.anchorMax = new Vector2(1, 0.5f);
            zoomInRect.pivot = new Vector2(1, 0.5f);
            zoomInRect.anchoredPosition = new Vector2(0, 0);
            
            // Add pinch zoom handler
            var pinchHandler = panel.AddComponent<FullMapPinchZoom>();
            pinchHandler.Initialize(this);
            
            Debug.Log("[UIManager] Full map panel created with REAL MAP, coin markers, and ZOOM!");
            
            return panel;
        }
        
        /// <summary>
        /// Change the full map zoom level
        /// </summary>
        public void ChangeMapZoom(int delta)
        {
            int newZoom = Mathf.Clamp(_fullMapZoom + delta, MIN_ZOOM, MAX_ZOOM);
            if (newZoom == _fullMapZoom) return; // No change
            
            _fullMapZoom = newZoom;
            Debug.Log($"[UIManager] Map zoom changed to {_fullMapZoom}");
            
            // Update zoom level display
            if (_zoomLevelText != null)
            {
                _zoomLevelText.text = $"{_fullMapZoom}x";
            }
            
            // Reload the map at new zoom level
            if (!_mapLoadPending)
            {
                StartCoroutine(LoadFullMapTile());
            }
        }
        
        private GameObject _playerMarkerFullMap;
        
        private void CreatePlayerMarkerOnFullMap(Transform parent)
        {
            var marker = new GameObject("PlayerMarker");
            marker.transform.SetParent(parent, false);
            var markerRect = marker.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(30, 30);
            
            // Glow
            var glow = new GameObject("Glow");
            glow.transform.SetParent(marker.transform, false);
            var glowRect = glow.AddComponent<RectTransform>();
            glowRect.sizeDelta = new Vector2(50, 50);
            var glowImg = glow.AddComponent<Image>();
            glowImg.color = new Color(0.2f, 0.6f, 1f, 0.4f);
            
            // Main dot
            var dot = new GameObject("Dot");
            dot.transform.SetParent(marker.transform, false);
            var dotRect = dot.AddComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(24, 24);
            var dotImg = dot.AddComponent<Image>();
            dotImg.color = new Color(0.2f, 0.6f, 1f);
            
            // Direction arrow
            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(marker.transform, false);
            var arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchoredPosition = new Vector2(0, 20);
            arrowRect.sizeDelta = new Vector2(10, 10);
            var arrowImg = arrow.AddComponent<Image>();
            arrowImg.color = new Color(0.2f, 0.6f, 1f);
            
            _playerMarkerFullMap = marker;
        }
        
        private IEnumerator LoadFullMapTile()
        {
            _mapLoadPending = true;
            yield return null; // Wait a frame
            
            if (!MapboxService.Exists)
            {
                EnsureMapboxService();
                yield return new WaitForSeconds(0.5f);
            }
            
            if (GPSManager.Instance == null || !GPSManager.Instance.IsTracking)
            {
                Debug.Log("[UIManager] Waiting for GPS for full map...");
                yield return new WaitForSeconds(1f);
            }
            
            var loc = GPSManager.Instance?.CurrentLocation;
            if (loc != null && MapboxService.Exists)
            {
                // Store center for coin positioning
                _fullMapCenterLat = loc.latitude;
                _fullMapCenterLng = loc.longitude;
                // Keep current zoom level (don't override!)
                
                // Calculate meters per pixel at this zoom level
                // Formula: meters/pixel = 156543.03392 * cos(lat) / 2^zoom
                _fullMapMetersPerPixel = (float)(156543.03392 * Mathf.Cos((float)loc.latitude * Mathf.Deg2Rad) / Mathf.Pow(2, _fullMapZoom));
                
                Debug.Log($"[UIManager] Loading full map tile at {loc.latitude}, {loc.longitude}, zoom {_fullMapZoom}");
                Debug.Log($"[UIManager] Meters per pixel: {_fullMapMetersPerPixel}");
                
                MapboxService.Instance.GetFullMapTile(loc.latitude, loc.longitude, _fullMapZoom, (texture) => {
                    if (texture != null && _fullMapImage != null)
                    {
                        if (_fullMapTile != null)
                        {
                            Destroy(_fullMapTile);
                        }
                        _fullMapTile = texture;
                        _fullMapImage.texture = texture;
                        
                        // Hide loading text
                        var loadingText = _fullMapImage.transform.parent.Find("LoadingText");
                        if (loadingText != null)
                        {
                            loadingText.gameObject.SetActive(false);
                        }
                        
                        // Add coin markers on the map
                        PopulateCoinMarkersOnMap();
                        
                        // Update zoom text
                        if (_zoomLevelText != null)
                        {
                            _zoomLevelText.text = $"{_fullMapZoom}x";
                        }
                        
                        _mapLoadPending = false;
                        Debug.Log($"[UIManager] Full map tile loaded at zoom {_fullMapZoom}!");
                    }
                    else
                    {
                        _mapLoadPending = false;
                    }
                });
            }
            else
            {
                _mapLoadPending = false;
            }
        }
        
        /// <summary>
        /// Add clickable coin markers on the full map
        /// </summary>
        private void PopulateCoinMarkersOnMap()
        {
            if (_coinMarkersContainer == null) return;
            if (CoinManager.Instance == null) return;
            if (_fullMapContainer == null) return;
            
            // Clear existing markers
            foreach (var marker in _fullMapCoinMarkers.Values)
            {
                if (marker != null) Destroy(marker);
            }
            _fullMapCoinMarkers.Clear();
            
            var playerLoc = GPSManager.Instance?.CurrentLocation;
            if (playerLoc == null) return;
            
            // Get map dimensions
            float mapWidth = _fullMapContainer.rect.width;
            float mapHeight = _fullMapContainer.rect.height;
            
            Debug.Log($"[UIManager] Populating coin markers, map size: {mapWidth}x{mapHeight}");
            
            foreach (var coin in CoinManager.Instance.KnownCoins)
            {
                if (coin == null) continue;
                
                // Calculate distance from map center
                float distanceMeters = (float)new LocationData(_fullMapCenterLat, _fullMapCenterLng)
                    .DistanceTo(new LocationData(coin.latitude, coin.longitude));
                float bearingDeg = (float)new LocationData(_fullMapCenterLat, _fullMapCenterLng)
                    .BearingTo(new LocationData(coin.latitude, coin.longitude));
                
                // Convert to pixels
                float distancePixels = distanceMeters / _fullMapMetersPerPixel;
                
                // Check if coin is within map bounds (with margin)
                float maxPixelDist = Mathf.Min(mapWidth, mapHeight) * 0.45f;
                if (distancePixels > maxPixelDist)
                {
                    // Coin is off-map, show at edge
                    distancePixels = maxPixelDist;
                }
                
                // Convert bearing to position (bearing is clockwise from north)
                float bearingRad = bearingDeg * Mathf.Deg2Rad;
                float x = Mathf.Sin(bearingRad) * distancePixels;
                float y = Mathf.Cos(bearingRad) * distancePixels;
                
                // Create marker
                CreateCoinMarkerOnMap(coin, new Vector2(x, y));
            }
            
            Debug.Log($"[UIManager] Created {_fullMapCoinMarkers.Count} coin markers on map");
        }
        
        private void CreateCoinMarkerOnMap(Coin coin, Vector2 position)
        {
            var marker = new GameObject($"CoinMarker_{coin.id}");
            marker.transform.SetParent(_coinMarkersContainer, false);
            
            var rect = marker.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(60, 70); // Clickable area
            
            // Calculate distance for display
            float distance = 0f;
            if (GPSManager.Instance?.CurrentLocation != null)
            {
                distance = (float)GPSManager.Instance.CurrentLocation
                    .DistanceTo(new LocationData(coin.latitude, coin.longitude));
            }
            
            // Marker background (touch area)
            var touchArea = marker.AddComponent<Image>();
            touchArea.color = new Color(0, 0, 0, 0.01f); // Nearly invisible but raycastable
            touchArea.raycastTarget = true;
            
            // Gold coin icon
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(marker.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0, 10);
            iconRect.sizeDelta = new Vector2(40, 40);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.color = GoldColor;
            iconImg.raycastTarget = false;
            
            // Glow effect
            var glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(iconObj.transform, false);
            glowObj.transform.SetAsFirstSibling();
            var glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.sizeDelta = new Vector2(55, 55);
            var glowImg = glowObj.AddComponent<Image>();
            glowImg.color = new Color(1f, 0.9f, 0.3f, 0.4f);
            glowImg.raycastTarget = false;
            
            // Value text
            var valueText = CreateText(marker.transform, "Value", $"${coin.value:F0}", 
                Vector2.zero, 14, Color.white, FontStyles.Bold);
            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchoredPosition = new Vector2(0, -20);
            
            // Distance text
            var distText = CreateText(marker.transform, "Distance", $"{distance:F0}m", 
                Vector2.zero, 11, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);
            var distRect = distText.GetComponent<RectTransform>();
            distRect.anchoredPosition = new Vector2(0, -32);
            
            // Make it a button
            var button = marker.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = iconImg;
            
            var colors = button.colors;
            colors.normalColor = GoldColor;
            colors.highlightedColor = new Color(1f, 0.95f, 0.5f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.2f);
            button.colors = colors;
            
            // Capture coin reference
            var capturedCoin = coin;
            button.onClick.AddListener(() => {
                Debug.Log($"[UIManager] Map coin tapped: {capturedCoin.id} (${capturedCoin.value})");
                SelectCoinAsTarget(capturedCoin);
            });
            
            // Pulse animation for nearby coins
            if (distance < 50f)
            {
                StartCoroutine(PulseCoinMarker(iconRect));
            }
            
            _fullMapCoinMarkers[coin.id] = marker;
        }
        
        private IEnumerator PulseCoinMarker(RectTransform rect)
        {
            while (rect != null && rect.gameObject.activeInHierarchy)
            {
                float scale = 1f + Mathf.Sin(Time.time * 3f) * 0.1f;
                rect.localScale = Vector3.one * scale;
                yield return null;
            }
        }
        
        private void CreateCoinListInFullMap(Transform parent)
        {
            // Create a scrollable area with coin entries
            var listContainer = new GameObject("CoinList");
            listContainer.transform.SetParent(parent, false);
            
            var listRect = listContainer.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.1f, 0.3f);
            listRect.anchorMax = new Vector2(0.9f, 0.7f);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = Vector2.zero;
            
            // Background
            var listBg = listContainer.AddComponent<Image>();
            listBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Header
            var header = CreateText(listContainer.transform, "Header", "NEARBY COINS:", 
                Vector2.zero, 28, GoldColor, FontStyles.Bold);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -10);
            headerRect.sizeDelta = new Vector2(0, 40);
            
            // Populate with coins (will update when map is shown)
            StartCoroutine(PopulateCoinList(listContainer.transform));
        }
        
        private IEnumerator PopulateCoinList(Transform container)
        {
            yield return null; // Wait a frame for layout
            
            if (CoinManager.Instance == null) yield break;
            
            var coins = CoinManager.Instance.KnownCoins;
            if (coins == null || coins.Count == 0)
            {
                CreateText(container, "NoCoins", "No coins nearby. Keep exploring!", 
                    new Vector2(0, -60), 20, Color.gray, FontStyles.Italic);
                yield break;
            }
            
            float yOffset = -60;
            int count = 0;
            
            foreach (var coin in coins)
            {
                if (count >= 5) break; // Max 5 coins shown
                
                // Calculate distance if GPS available
                string distText = "";
                if (GPSManager.Instance != null && GPSManager.Instance.IsTracking)
                {
                    var playerLoc = GPSManager.Instance.CurrentLocation;
                    if (playerLoc != null)
                    {
                        float dist = (float)playerLoc.DistanceTo(new LocationData(coin.latitude, coin.longitude));
                        distText = $" ({dist:F0}m away)";
                    }
                }
                
                var coinText = $"💰 ${coin.value:F2}{distText}";
                
                // Capture coin reference for closure
                var capturedCoin = coin;
                
                // Create coin button
                var coinBtn = CreateButton(container, $"Coin_{coin.id}", coinText,
                    new Vector2(0, yOffset), new Vector2(400, 50), GoldColor,
                    () => {
                        Debug.Log($"[UIManager] Selected coin: {capturedCoin.id}");
                        SelectCoinAsTarget(capturedCoin);
                    });
                
                var btnRect = coinBtn.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.5f, 1);
                btnRect.anchorMax = new Vector2(0.5f, 1);
                btnRect.pivot = new Vector2(0.5f, 1);
                
                yOffset -= 60;
                count++;
            }
        }
        
        private void SelectCoinAsTarget(Coin coin)
        {
            Debug.Log($"[UIManager] 🎯 Setting target coin: {coin.id} (${coin.value})");
            
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.SetTargetCoin(coin);
            }
            
            // Close the map
            if (_simpleFullMapPanel != null)
            {
                _simpleFullMapPanel.SetActive(false);
            }
            
            Debug.Log("[UIManager] Target set! Hunt it down in AR!");
        }
        
        private void HideAllPanels()
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (walletPanel != null) walletPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (arHudPanel != null) arHudPanel.SetActive(false);
        }
        
        #endregion
        
        #region Dynamic UI Building
        
        /// <summary>
        /// Build all UI panels programmatically (market standard for mobile)
        /// </summary>
        private void BuildUI()
        {
            Debug.Log("[UIManager] Building UI panels...");
            
            // Create main canvas
            _ourCanvas = CreateCanvas();
            
            // Create each panel
            loginPanel = CreateLoginPanel(_ourCanvas.transform);
            registerPanel = CreateRegisterPanel(_ourCanvas.transform);
            mainMenuPanel = CreateMainMenuPanel(_ourCanvas.transform);
            walletPanel = CreateWalletPanel(_ourCanvas.transform);
            settingsPanel = CreateSettingsPanel(_ourCanvas.transform);
            arHudPanel = CreateARHudPanel(_ourCanvas.transform);
            
            // Hide all initially
            HideAllPanels();
            
            Debug.Log("[UIManager] UI panels built successfully!");
        }
        
        private Canvas CreateCanvas()
        {
            var canvasGO = new GameObject("UICanvas");
            canvasGO.transform.SetParent(transform);
            
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            return canvas;
        }
        
        #endregion
        
        #region Panel Builders
        
        private GameObject CreateLoginPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "LoginPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "🏴‍☠️ Ahoy, Matey!", 
                new Vector2(0, 300), 52, GoldColor, FontStyles.Bold);
            
            // Login Button
            CreateButton(panel.transform, "LoginButton", "⚓ SET SAIL", 
                new Vector2(0, -100), new Vector2(500, 80), GoldColor,
                () => ShowMainMenu());
            
            // Register Button
            CreateButton(panel.transform, "RegisterButton", "🏴‍☠️ Join the Crew", 
                new Vector2(0, -200), new Vector2(400, 60), Parchment,
                () => ShowRegister());
            
            return panel;
        }
        
        private GameObject CreateRegisterPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "RegisterPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "🏴‍☠️ Join the Crew!", 
                new Vector2(0, 300), 48, GoldColor, FontStyles.Bold);
            
            // Subtitle
            CreateText(panel.transform, "Subtitle", "Sign up to start hunting treasure", 
                new Vector2(0, 220), 24, Parchment, FontStyles.Normal);
            
            // Register Button (for now, just goes to main menu)
            CreateButton(panel.transform, "CreateButton", "⚓ Create Account", 
                new Vector2(0, -100), new Vector2(500, 80), GoldColor,
                () => ShowMainMenu());
            
            // Back Button
            CreateButton(panel.transform, "BackButton", "← Back to Login", 
                new Vector2(0, -200), new Vector2(300, 50), Parchment,
                () => ShowLogin());
            
            return panel;
        }
        
        private GameObject CreateMainMenuPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MainMenuPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "⚓ Black Bart's Gold", 
                new Vector2(0, 350), 48, GoldColor, FontStyles.Bold);
            
            // Start Hunt Button
            CreateButton(panel.transform, "StartHuntButton", "🔍 Start Hunting!", 
                new Vector2(0, 50), new Vector2(500, 100), GoldColor,
                () => StartARHunt());
            
            // Wallet Button
            CreateButton(panel.transform, "WalletButton", "💰 Wallet", 
                new Vector2(0, -80), new Vector2(400, 70), Parchment,
                () => ShowWallet());
            
            // Settings Button
            CreateButton(panel.transform, "SettingsButton", "⚙️ Settings", 
                new Vector2(0, -170), new Vector2(400, 70), Parchment,
                () => ShowSettings());
            
            return panel;
        }
        
        private GameObject CreateWalletPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "WalletPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "💰 Treasure Chest", 
                new Vector2(0, 350), 48, GoldColor, FontStyles.Bold);
            
            // Balance
            CreateText(panel.transform, "Balance", "0 Doubloons", 
                new Vector2(0, 200), 36, Parchment, FontStyles.Bold);
            
            // Back Button
            CreateButton(panel.transform, "BackButton", "← Back", 
                new Vector2(0, -300), new Vector2(300, 60), Parchment,
                () => ShowMainMenu());
            
            return panel;
        }
        
        private GameObject CreateSettingsPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "SettingsPanel", DeepSeaBlue);
            
            // Title
            CreateText(panel.transform, "Title", "⚙️ Settings", 
                new Vector2(0, 350), 48, GoldColor, FontStyles.Bold);
            
            // Back Button
            CreateButton(panel.transform, "BackButton", "← Back", 
                new Vector2(0, -300), new Vector2(300, 60), Parchment,
                () => ShowMainMenu());
            
            // Logout Button
            CreateButton(panel.transform, "LogoutButton", "🚪 Logout", 
                new Vector2(0, -200), new Vector2(300, 60), new Color(0.8f, 0.2f, 0.2f),
                () => ShowLogin());
            
            return panel;
        }
        
        private GameObject CreateARHudPanel(Transform parent)
        {
            // AR HUD is transparent - just overlay elements, no background!
            var panel = new GameObject("ARHudPanel");
            panel.transform.SetParent(parent, false);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Back Button (top-left corner)
            var backButton = CreateButton(panel.transform, "BackButton", "< Back", 
                Vector2.zero, new Vector2(150, 60), SemiTransparent,
                () => ExitARHunt());
            
            // Position back button in top-left
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 1);
            backRect.anchorMax = new Vector2(0, 1);
            backRect.pivot = new Vector2(0, 1);
            backRect.anchoredPosition = new Vector2(20, -40);
            
            // Make button text white for visibility on camera
            var backText = backButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backText != null) backText.color = Color.white;
            
            // Crosshairs - using simple + that all fonts support
            CreateCrosshairs(panel.transform);
            
            // Coin Counter (top-right) - using text that renders
            var coinCounter = CreateText(panel.transform, "CoinCounter", "Coins: 0", 
                Vector2.zero, 28, GoldColor, FontStyles.Bold);
            var coinRect = coinCounter.GetComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(1, 1);
            coinRect.anchorMax = new Vector2(1, 1);
            coinRect.pivot = new Vector2(1, 1);
            coinRect.anchoredPosition = new Vector2(-20, -50);
            coinRect.sizeDelta = new Vector2(200, 50);
            
            // Store reference for dynamic updates
            _coinCounterText = coinCounter.GetComponent<TextMeshProUGUI>();
            
            // ================================================================
            // DEBUG DIAGNOSTIC PANEL (bottom-left)
            // Shows GPS, API status, and coin count to help debug connection issues
            // ================================================================
            CreateDebugDiagnosticsPanel(panel.transform);
            
            // ================================================================
            // MINI-MAP (top-right corner) - Pokémon GO style radar
            // Shows nearby coins as dots relative to player position
            // ================================================================
            CreateMiniMap(panel.transform);
            
            // ================================================================
            // PLACE COIN BUTTON (bottom center) - Pokémon GO style
            // Appears when player is close to a coin, places it on AR plane
            // ================================================================
            CreatePlaceCoinButton(panel.transform);
            
            // Instructions (bottom of screen)
            var instructions = CreateText(panel.transform, "Instructions", 
                "Use mini-map to navigate to coins!", 
                new Vector2(0, -450), 24, Color.white, FontStyles.Normal);
            
            return panel;
        }
        
        /// <summary>
        /// Create debug diagnostics panel showing GPS, API, and coin status
        /// </summary>
        private void CreateDebugDiagnosticsPanel(Transform parent)
        {
            // Container panel with semi-transparent background
            var debugPanel = new GameObject("DebugDiagnosticsPanel");
            debugPanel.transform.SetParent(parent, false);
            
            var panelRect = debugPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = new Vector2(20, 20);
            panelRect.sizeDelta = new Vector2(560, 450); // DOUBLED SIZE for readability
            
            // Semi-transparent black background
            var bgImage = debugPanel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f); // Slightly more opaque
            bgImage.raycastTarget = false;
            
            // Title - DOUBLED font size
            var title = CreateText(debugPanel.transform, "DebugTitle", "🔧 DEBUG INFO", 
                Vector2.zero, 32, GoldColor, FontStyles.Bold);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 45);
            
            // Diagnostic text (dynamically updated) - DOUBLED font size
            var diagText = CreateText(debugPanel.transform, "DiagnosticsText", 
                "Loading...", Vector2.zero, 26, Color.white, FontStyles.Normal);
            var diagRect = diagText.GetComponent<RectTransform>();
            diagRect.anchorMin = new Vector2(0, 0);
            diagRect.anchorMax = new Vector2(1, 1);
            diagRect.pivot = new Vector2(0, 1);
            diagRect.anchoredPosition = new Vector2(15, -55);
            diagRect.sizeDelta = new Vector2(-30, -70);
            
            // Configure text settings
            var tmpText = diagText.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.alignment = TextAlignmentOptions.TopLeft;
                tmpText.enableWordWrapping = true;
                tmpText.richText = true;
            }
            
            // Store reference for updates
            _debugDiagnosticsText = tmpText;
        }
        
        /// <summary>
        /// Create mini-map showing nearby coins (Pokémon GO style with REAL MAP!)
        /// </summary>
        private void CreateMiniMap(Transform parent)
        {
            // Container in top-right corner
            var mapContainer = new GameObject("MiniMapContainer");
            mapContainer.transform.SetParent(parent, false);
            
            var containerRect = mapContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 1); // Top-right
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(1, 1);
            containerRect.anchoredPosition = new Vector2(-20, -20);
            containerRect.sizeDelta = new Vector2(300, 300); // Slightly smaller for cleaner look
            
            _miniMapContainer = containerRect;
            _miniMapRadius = 130f; // Adjusted for new size
            
            // Dark background (shows while map loads)
            var bgImage = mapContainer.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.15f, 0.2f, 0.95f);
            bgImage.raycastTarget = true; // ENABLED for click detection!
            
            // === REAL MAP TILE (Mapbox) ===
            var mapTileObj = new GameObject("MapTile");
            mapTileObj.transform.SetParent(mapContainer.transform, false);
            var mapTileRect = mapTileObj.AddComponent<RectTransform>();
            mapTileRect.anchorMin = Vector2.zero;
            mapTileRect.anchorMax = Vector2.one;
            mapTileRect.offsetMin = new Vector2(5, 5); // Small padding
            mapTileRect.offsetMax = new Vector2(-5, -5);
            
            _miniMapImage = mapTileObj.AddComponent<RawImage>();
            _miniMapImage.color = Color.white;
            _miniMapImage.raycastTarget = false;
            
            // Circular mask for the map
            var maskObj = new GameObject("MapMask");
            maskObj.transform.SetParent(mapContainer.transform, false);
            maskObj.transform.SetSiblingIndex(0); // Behind map
            var maskRect = maskObj.AddComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
            var maskImage = maskObj.AddComponent<Image>();
            maskImage.color = Color.white;
            maskImage.raycastTarget = false;
            var mask = maskObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            // Move map tile under mask
            mapTileObj.transform.SetParent(maskObj.transform, false);
            mapTileRect.anchorMin = Vector2.zero;
            mapTileRect.anchorMax = Vector2.one;
            mapTileRect.offsetMin = Vector2.zero;
            mapTileRect.offsetMax = Vector2.zero;
            
            // Semi-transparent overlay for better visibility of markers
            var overlay = new GameObject("MapOverlay");
            overlay.transform.SetParent(maskObj.transform, false);
            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.2f); // Slight darkening
            overlayImage.raycastTarget = false;
            
            // Add Button component to make mini-map clickable
            var miniMapButton = mapContainer.AddComponent<Button>();
            miniMapButton.transition = Selectable.Transition.None;
            miniMapButton.onClick.AddListener(() => {
                Debug.Log("[UIManager] 🗺️ Mini-map CLICKED! Opening full map...");
                OnMiniMapClicked();
            });
            
            Debug.Log("[UIManager] 🗺️ Mini-map click handler registered!");
            
            // Range ring (50m indicator) - now on top of map
            var rangeRing = new GameObject("RangeRing");
            rangeRing.transform.SetParent(mapContainer.transform, false);
            var ringRect = rangeRing.AddComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(260, 260);
            var ringImage = rangeRing.AddComponent<Image>();
            ringImage.color = new Color(1, 1, 1, 0.3f);
            ringImage.raycastTarget = false;
            // Make it a ring (just outline)
            ringImage.fillCenter = false;
            ringImage.type = Image.Type.Sliced;
            
            // Inner range ring (25m)
            var innerRing = new GameObject("InnerRing");
            innerRing.transform.SetParent(mapContainer.transform, false);
            var innerRect = innerRing.AddComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.5f, 0.5f);
            innerRect.anchorMax = new Vector2(0.5f, 0.5f);
            innerRect.sizeDelta = new Vector2(130, 130);
            var innerImage = innerRing.AddComponent<Image>();
            innerImage.color = new Color(1, 1, 1, 0.2f);
            innerImage.raycastTarget = false;
            
            // Player dot (center, blue with glow effect)
            var playerDot = new GameObject("PlayerDot");
            playerDot.transform.SetParent(mapContainer.transform, false);
            var playerRect = playerDot.AddComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(0.5f, 0.5f);
            playerRect.anchorMax = new Vector2(0.5f, 0.5f);
            playerRect.sizeDelta = new Vector2(24, 24);
            var playerImage = playerDot.AddComponent<Image>();
            playerImage.color = new Color(0.2f, 0.6f, 1f); // Blue
            playerImage.raycastTarget = false;
            _playerDot = playerRect;
            
            // Player glow/pulse effect
            var playerGlow = new GameObject("PlayerGlow");
            playerGlow.transform.SetParent(playerDot.transform, false);
            var glowRect = playerGlow.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(40, 40);
            var glowImage = playerGlow.AddComponent<Image>();
            glowImage.color = new Color(0.2f, 0.6f, 1f, 0.3f);
            glowImage.raycastTarget = false;
            glowRect.SetAsFirstSibling(); // Behind player dot
            
            // Direction indicator (arrow showing where you're facing)
            var dirIndicator = new GameObject("DirectionIndicator");
            dirIndicator.transform.SetParent(playerDot.transform, false);
            var dirRect = dirIndicator.AddComponent<RectTransform>();
            dirRect.anchoredPosition = new Vector2(0, 18);
            dirRect.sizeDelta = new Vector2(12, 12);
            var dirImage = dirIndicator.AddComponent<Image>();
            dirImage.color = new Color(0.2f, 0.6f, 1f);
            dirImage.raycastTarget = false;
            
            // Border/frame around map
            var border = new GameObject("MapBorder");
            border.transform.SetParent(mapContainer.transform, false);
            var borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(0.8f, 0.7f, 0.4f, 0.8f); // Gold border
            borderImage.raycastTarget = false;
            borderImage.fillCenter = false;
            borderImage.type = Image.Type.Sliced;
            border.transform.SetAsLastSibling(); // On top
            
            // Title with background
            var titleBg = new GameObject("TitleBg");
            titleBg.transform.SetParent(mapContainer.transform, false);
            var titleBgRect = titleBg.AddComponent<RectTransform>();
            titleBgRect.anchorMin = new Vector2(0.5f, 1);
            titleBgRect.anchorMax = new Vector2(0.5f, 1);
            titleBgRect.pivot = new Vector2(0.5f, 1);
            titleBgRect.anchoredPosition = new Vector2(0, 5);
            titleBgRect.sizeDelta = new Vector2(120, 30);
            var titleBgImage = titleBg.AddComponent<Image>();
            titleBgImage.color = new Color(0, 0, 0, 0.7f);
            titleBgImage.raycastTarget = false;
            
            var title = CreateText(titleBg.transform, "MapTitle", "MAP", 
                Vector2.zero, 18, GoldColor, FontStyles.Bold);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            // "Tap to expand" hint
            var hint = CreateText(mapContainer.transform, "TapHint", "Tap to select coin", 
                Vector2.zero, 12, new Color(1, 1, 1, 0.7f), FontStyles.Italic);
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0);
            hintRect.anchorMax = new Vector2(0.5f, 0);
            hintRect.pivot = new Vector2(0.5f, 0);
            hintRect.anchoredPosition = new Vector2(0, 8);
            
            Debug.Log("[UIManager] 🗺️ Mini-map with REAL MAP created!");
            
            // Initialize MapboxService if needed
            EnsureMapboxService();
        }
        
        /// <summary>
        /// Ensure MapboxService exists
        /// </summary>
        private void EnsureMapboxService()
        {
            if (!MapboxService.Exists)
            {
                var mapboxGO = new GameObject("MapboxService");
                mapboxGO.AddComponent<MapboxService>();
                DontDestroyOnLoad(mapboxGO);
                Debug.Log("[UIManager] Created MapboxService");
            }
        }
        
        /// <summary>
        /// Update the mini-map tile from Mapbox
        /// </summary>
        private void UpdateMiniMapTile()
        {
            if (_miniMapImage == null) return;
            if (!MapboxService.Exists) return;
            if (GPSManager.Instance == null || !GPSManager.Instance.IsTracking) return;
            
            var loc = GPSManager.Instance.CurrentLocation;
            if (loc == null) return;
            
            // Check if we need to update (moved enough or time passed)
            float timeSinceUpdate = Time.time - _lastMapUpdateTime;
            double latDiff = System.Math.Abs(loc.latitude - _lastMapLat);
            double lngDiff = System.Math.Abs(loc.longitude - _lastMapLng);
            float bearingDiff = Mathf.Abs(_smoothedCompassHeading - _lastMapBearing);
            
            bool needsUpdate = timeSinceUpdate > _mapUpdateInterval && 
                              (latDiff > 0.0001 || lngDiff > 0.0001 || bearingDiff > 15f);
            
            if (needsUpdate && !_mapUpdatePending)
            {
                _mapUpdatePending = true;
                _lastMapUpdateTime = Time.time;
                _lastMapLat = loc.latitude;
                _lastMapLng = loc.longitude;
                _lastMapBearing = _smoothedCompassHeading;
                
                // Request map tile from Mapbox (north-up, bearing=0 for mini-map)
                MapboxService.Instance.GetMiniMapTile(loc.latitude, loc.longitude, 0f, (texture) => {
                    _mapUpdatePending = false;
                    if (texture != null && _miniMapImage != null)
                    {
                        // Clean up old texture
                        if (_currentMapTile != null)
                        {
                            Destroy(_currentMapTile);
                        }
                        _currentMapTile = texture;
                        _miniMapImage.texture = texture;
                        Debug.Log("[UIManager] 🗺️ Mini-map tile updated!");
                    }
                });
            }
        }
        
        /// <summary>
        /// Update mini-map - only shows SELECTED target coin!
        /// User must tap mini-map and select a coin from full map first.
        /// </summary>
        private void UpdateMiniMap()
        {
            if (_miniMapContainer == null) return;
            if (GPSManager.Instance == null || !GPSManager.Instance.IsTracking) return;
            if (CoinManager.Instance == null) return;
            
            var playerLoc = GPSManager.Instance.CurrentLocation;
            if (playerLoc == null) return;
            
            // Get raw compass heading and apply smoothing to reduce jitter
            float rawHeading = Input.compass.enabled ? Input.compass.trueHeading : 0f;
            _smoothedCompassHeading = Mathf.SmoothDampAngle(_smoothedCompassHeading, rawHeading, ref _compassVelocity, _compassSmoothTime);
            
            // Update the real map tile from Mapbox
            UpdateMiniMapTile();
            
            // Clear all existing dots first
            foreach (var kvp in _coinDots)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.gameObject.SetActive(false);
                }
            }
            
            // ONLY show the selected target coin on the mini-map
            var targetCoin = CoinManager.Instance.TargetCoinData;
            if (targetCoin == null)
            {
                // No coin selected - mini-map shows nothing (user should tap to select one)
                return;
            }
            
            // Calculate distance and bearing to target
            float distance = (float)playerLoc.DistanceTo(new LocationData(targetCoin.latitude, targetCoin.longitude));
            float bearing = (float)playerLoc.BearingTo(new LocationData(targetCoin.latitude, targetCoin.longitude));
            
            // Adjust bearing for SMOOTHED compass heading (reduces jitter)
            float adjustedBearing = bearing - _smoothedCompassHeading;
            float bearingRad = adjustedBearing * Mathf.Deg2Rad;
            
            // Calculate position on mini-map (extended range for visibility)
            float effectiveRange = _miniMapRange * 2; // 100m display range
            float normalizedDist = Mathf.Clamp01(distance / effectiveRange);
            float pixelDist = normalizedDist * _miniMapRadius;
            float x = Mathf.Sin(bearingRad) * pixelDist;
            float y = Mathf.Cos(bearingRad) * pixelDist;
            
            // Get or create dot for target
            RectTransform dot;
            if (!_coinDots.TryGetValue(targetCoin.id, out dot) || dot == null)
            {
                var dotObj = new GameObject($"CoinDot_{targetCoin.id}");
                dotObj.transform.SetParent(_miniMapContainer, false);
                dot = dotObj.AddComponent<RectTransform>();
                dot.anchorMin = new Vector2(0.5f, 0.5f);
                dot.anchorMax = new Vector2(0.5f, 0.5f);
                dot.sizeDelta = new Vector2(24, 24);
                var dotImage = dotObj.AddComponent<Image>();
                dotImage.color = GoldColor;
                dotImage.raycastTarget = false;
                _coinDots[targetCoin.id] = dot;
                
                Debug.Log($"[UIManager] Created mini-map dot for TARGET coin {targetCoin.id}");
            }
            
            // Update position
            dot.anchoredPosition = new Vector2(x, y);
            dot.gameObject.SetActive(true);
            
            // Determine if in collection range
            bool isInRange = distance <= 25f;
            
            // Color based on range
            var img = dot.GetComponent<Image>();
            if (img != null)
            {
                if (isInRange)
                    img.color = new Color(0.29f, 0.87f, 0.5f); // Bright green - collectible!
                else
                    img.color = GoldColor; // Gold - tracking
            }
            
            // Pulse animation
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.2f;
            float baseScale = isInRange ? 1.4f : 1.2f;
            dot.localScale = Vector3.one * baseScale * pulse;
        }
        
        /// <summary>
        /// Remove a coin dot from mini-map
        /// </summary>
        private void RemoveMiniMapDot(string coinId)
        {
            if (_coinDots.TryGetValue(coinId, out var dot))
            {
                if (dot != null)
                    Destroy(dot.gameObject);
                _coinDots.Remove(coinId);
            }
        }
        
        /// <summary>
        /// Create the "Place Coin in AR" button (Pokémon GO style)
        /// </summary>
        private void CreatePlaceCoinButton(Transform parent)
        {
            // Button container at bottom center
            var buttonObj = new GameObject("PlaceCoinButton");
            buttonObj.transform.SetParent(parent, false);
            
            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.pivot = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(0, 520); // Above instructions
            buttonRect.sizeDelta = new Vector2(350, 80);
            
            // Button background
            var bgImage = buttonObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.7f, 0.3f, 0.9f); // Green
            
            // Make it clickable
            var button = buttonObj.AddComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(OnPlaceCoinButtonClicked);
            
            // Button text
            var textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(buttonObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            var tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "REVEAL COIN";
            tmpText.fontSize = 32;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontStyle = FontStyles.Bold;
            
            _placeCoinButton = buttonObj;
            _placeCoinButtonText = tmpText;
            
            // Start hidden
            buttonObj.SetActive(false);
            
            Debug.Log("[UIManager] Reveal Coin button created");
        }
        
        /// <summary>
        /// Handle Reveal Coin button click.
        /// NEW ARCHITECTURE: Coins are ALWAYS visible via ARCoinRenderer.
        /// This button now just highlights the coin for collection.
        /// </summary>
        private void OnPlaceCoinButtonClicked()
        {
            if (_coinToPlace == null)
            {
                Debug.LogWarning("[UIManager] No coin to reveal!");
                return;
            }
            
            Debug.Log($"[UIManager] ═══════════════════════════════════════════════════");
            Debug.Log($"[UIManager] 🪙 REVEAL COIN BUTTON CLICKED!");
            Debug.Log($"[UIManager] Coin to reveal: {_coinToPlace.id}");
            
            // NEW ARCHITECTURE: Coins are always visible through ARCoinRenderer
            // The "Reveal" button now focuses/selects the coin for collection
            if (CoinManager.Instance != null)
            {
                var controller = CoinManager.Instance.GetCoinById(_coinToPlace.id);
                if (controller != null)
                {
                    // Select the coin
                    CoinManager.Instance.SelectCoin(controller);
                    
                    // If in range, try to collect
                    if (controller.IsInRange && !controller.IsLocked)
                    {
                        Debug.Log($"[UIManager] 🪙 Attempting to collect coin: {_coinToPlace.id}");
                        controller.TryCollect();
                    }
                    else
                    {
                        Debug.Log($"[UIManager] 🪙 Coin selected, but not in range or locked");
                        Debug.Log($"[UIManager]    Distance: {controller.DistanceFromPlayer:F1}m");
                        Debug.Log($"[UIManager]    InRange: {controller.IsInRange}");
                        Debug.Log($"[UIManager]    Locked: {controller.IsLocked}");
                    }
                    
                    // Hide button after action
                    _placeCoinButton?.SetActive(false);
                    _coinToPlace = null;
                }
                else
                {
                    Debug.LogWarning("[UIManager] ⚠️ Coin controller not found in CoinManager");
                }
            }
            
            Debug.Log($"[UIManager] ═══════════════════════════════════════════════════");
        }
        
        /// <summary>
        /// DIAGNOSTIC: Spawn a simple bright sphere at a FIXED WORLD POSITION
        /// This bypasses ALL coin systems to verify basic 3D rendering works
        /// </summary>
        private void SpawnDiagnosticSphere()
        {
            Debug.Log("[UIManager] ═══════════════════════════════════════════════════");
            Debug.Log("[UIManager] 🔴 SPAWNING DIAGNOSTIC SPHERE...");
            
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindFirstObjectByType<Camera>();
            }
            
            if (cam == null)
            {
                Debug.LogError("[UIManager] ❌ No camera found for diagnostic sphere!");
                return;
            }
            
            // ================================================================
            // DIAGNOSTIC: Log camera settings AND parent hierarchy
            // ================================================================
            Debug.Log($"[UIManager] 📷 CAMERA DIAGNOSTICS:");
            Debug.Log($"[UIManager]    Name: {cam.name}");
            Debug.Log($"[UIManager]    LOCAL Position: {cam.transform.localPosition}");
            Debug.Log($"[UIManager]    WORLD Position: {cam.transform.position}");
            Debug.Log($"[UIManager]    Forward: {cam.transform.forward}");
            Debug.Log($"[UIManager]    Parent: {(cam.transform.parent != null ? cam.transform.parent.name : "NULL (root)")}");
            if (cam.transform.parent != null)
            {
                Debug.Log($"[UIManager]    Parent WORLD pos: {cam.transform.parent.position}");
            }
            Debug.Log($"[UIManager]    Near Clip: {cam.nearClipPlane}");
            Debug.Log($"[UIManager]    Far Clip: {cam.farClipPlane}");
            Debug.Log($"[UIManager]    Culling Mask: {cam.cullingMask} (Everything = -1)");
            
            // ================================================================
            // TEST: Place sphere at FIXED ABSOLUTE world position
            // This should NOT follow the camera at all!
            // ================================================================
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "DIAGNOSTIC_SPHERE_FIXED";
            sphere.layer = 0; // Default layer
            
            // FIXED WORLD POSITION - 2m ahead in Z, 1m high
            // This is an ABSOLUTE position, NOT relative to camera!
            Vector3 fixedWorldPos = new Vector3(0f, 1f, 2f);
            sphere.transform.position = fixedWorldPos;
            sphere.transform.SetParent(null); // Explicitly no parent - world space!
            sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            Debug.Log($"[UIManager] 🔴 Sphere placed at FIXED world position: {fixedWorldPos}");
            Debug.Log($"[UIManager]    Sphere parent: {(sphere.transform.parent != null ? sphere.transform.parent.name : "NULL (world space)")}");
            Debug.Log($"[UIManager]    Sphere actual position: {sphere.transform.position}");
            
            // ================================================================
            // BRIGHT RED unlit material for maximum visibility
            // Try multiple shader options for compatibility
            // ================================================================
            MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // Try shaders in order of preference for mobile AR
                Shader shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Mobile/Diffuse");
                if (shader == null) shader = Shader.Find("Standard");
                
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    mat.color = Color.red; // BRIGHT RED
                    
                    // For URP Unlit, we need to set different property
                    if (shader.name.Contains("Universal"))
                    {
                        mat.SetColor("_BaseColor", Color.red);
                    }
                    
                    renderer.material = mat;
                    Debug.Log($"[UIManager] 🔴 Diagnostic sphere shader: {shader.name}");
                }
                else
                {
                    Debug.LogError("[UIManager] ❌ No shader found!");
                }
            }
            
            // Remove collider (not needed for visual test)
            Collider col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            Debug.Log($"[UIManager] ✅ DIAGNOSTIC SPHERE spawned at {fixedWorldPos}");
            Debug.Log($"[UIManager]    Distance from camera: {Vector3.Distance(cam.transform.position, fixedWorldPos)}m");
            Debug.Log($"[UIManager] ═══════════════════════════════════════════════════");
            Debug.Log($"[UIManager] 🔴🔴🔴 IF YOU DON'T SEE A RED SPHERE, CHECK:");
            Debug.Log($"[UIManager]    1. Camera culling mask includes layer 0 (Default)");
            Debug.Log($"[UIManager]    2. Near clip plane < 2m (current: {cam.nearClipPlane})");
            Debug.Log($"[UIManager]    3. Far clip plane > 2m (current: {cam.farClipPlane})");
            Debug.Log($"[UIManager]    4. Camera is actually rendering (not overridden)");
            Debug.Log($"[UIManager] ═══════════════════════════════════════════════════");
            
            // Destroy after 30 seconds
            Destroy(sphere, 30f);
        }
        
        /// <summary>
        /// Update the collect button visibility based on proximity.
        /// NEW ARCHITECTURE: Shows when coin is in collection range.
        /// </summary>
        private void UpdatePlaceCoinButton()
        {
            if (_placeCoinButton == null) return;
            if (CoinManager.Instance == null) return;
            
            // Find closest coin in collection range
            CoinController closestController = null;
            float closestDist = float.MaxValue;
            
            foreach (var controller in CoinManager.Instance.ActiveCoins)
            {
                if (controller == null || controller.IsCollected) continue;
                if (!controller.IsVisible) continue;
                
                float distance = controller.DistanceFromPlayer;
                
                if (distance < closestDist)
                {
                    closestDist = distance;
                    closestController = controller;
                }
            }
            
            // Show button when near a coin (within 20m)
            if (closestController != null && closestDist < 20f)
            {
                _coinToPlace = closestController.CoinData;
                _placeCoinButton.SetActive(true);
                
                // Update button text and color based on collection readiness
                var bgImage = _placeCoinButton.GetComponent<Image>();
                
                if (closestController.IsInRange && !closestController.IsLocked)
                {
                    // Can collect!
                    _placeCoinButtonText.text = $"COLLECT! ({closestDist:F0}m)";
                    bgImage.color = new Color(0.2f, 0.8f, 0.3f, 0.9f); // Bright green
                }
                else if (closestController.IsLocked)
                {
                    // Locked
                    _placeCoinButtonText.text = $"LOCKED ({closestDist:F0}m)";
                    bgImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // Red
                }
                else
                {
                    // Getting closer
                    _placeCoinButtonText.text = $"GET CLOSER ({closestDist:F0}m)";
                    bgImage.color = new Color(0.8f, 0.6f, 0.2f, 0.9f); // Orange
                }
            }
            else
            {
                _placeCoinButton.SetActive(false);
                _coinToPlace = null;
            }
        }
        
        /// <summary>
        /// Create proper crosshairs using UI lines instead of text
        /// </summary>
        private void CreateCrosshairs(Transform parent)
        {
            var crosshairContainer = new GameObject("Crosshairs");
            crosshairContainer.transform.SetParent(parent, false);
            
            var containerRect = crosshairContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(100, 100);
            
            // Horizontal line
            CreateCrosshairLine(crosshairContainer.transform, "HorizontalLine", 
                new Vector2(60, 4), Vector2.zero);
            
            // Vertical line
            CreateCrosshairLine(crosshairContainer.transform, "VerticalLine", 
                new Vector2(4, 60), Vector2.zero);
            
            // Center dot
            CreateCrosshairLine(crosshairContainer.transform, "CenterDot", 
                new Vector2(10, 10), Vector2.zero);
        }
        
        private void CreateCrosshairLine(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var line = new GameObject(name);
            line.transform.SetParent(parent, false);
            
            var rect = line.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            var image = line.AddComponent<Image>();
            image.color = GoldColor;
            image.raycastTarget = false;
        }
        
        #endregion
        
        #region UI Helper Methods
        
        private GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var image = panel.AddComponent<Image>();
            image.color = bgColor;
            image.raycastTarget = false; // Don't block button clicks!
            
            return panel;
        }
        
        private TextMeshProUGUI CreateText(Transform parent, string name, string content,
            Vector2 position, float fontSize, Color color, FontStyles style)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            
            var rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900, 100);
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false; // Don't block button clicks!
            
            return text;
        }
        
        private Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);
            
            var rect = buttonGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            var image = buttonGO.AddComponent<Image>();
            image.color = bgColor;
            image.raycastTarget = true; // Buttons MUST receive raycasts
            
            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            
            // Button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = size.y * 0.4f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = DarkBrown;
            text.raycastTarget = false;
            
            return button;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Simple spin animation for coins
    /// </summary>
    public class CoinSpin : MonoBehaviour
    {
        private float rotationSpeed = 90f;
        private float bobSpeed = 2f;
        private float bobAmount = 0.03f;
        private Vector3 startPos;
        
        private void Start()
        {
            startPos = transform.position;
        }
        
        private void Update()
        {
            // Spin around Y axis
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            // Gentle bob up and down
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            Vector3 pos = transform.position;
            pos.y = newY;
            transform.position = pos;
        }
    }
}
