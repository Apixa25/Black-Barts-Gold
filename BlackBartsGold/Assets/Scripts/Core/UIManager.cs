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
                if (CoinManager.Instance != null)
                {
                    CoinManager.Instance.SetNearbyCoins(coins);
                    
                    // Immediately trigger position recalculation so coins appear at correct GPS positions
                    if (CoinSpawner.Instance != null)
                    {
                        // CRITICAL: Set CoinSpawner's location from GPSManager before recalculating!
                        // CoinSpawner has its own GPS tracking that may not be ready yet
                        var gpsLocation = GPSManager.Instance.CurrentLocation ?? GPSManager.Instance.LastKnownLocation;
                        if (gpsLocation != null)
                        {
                            Debug.Log($"[UIManager] 📍 Setting CoinSpawner location from GPSManager: ({gpsLocation.latitude:F6}, {gpsLocation.longitude:F6})");
                            CoinSpawner.Instance.SetPlayerLocationManually(gpsLocation.latitude, gpsLocation.longitude);
                        }
                        else
                        {
                            Debug.LogWarning("[UIManager] ⚠️ No GPS location available for CoinSpawner!");
                        }
                        
                        Debug.Log("[UIManager] 📍 Triggering immediate position recalculation");
                        CoinSpawner.Instance.RecalculateAllCoinPositions();
                    }
                    else
                    {
                        Debug.LogWarning("[UIManager] ⚠️ CoinSpawner not found - coins may not be positioned correctly");
                    }
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
        /// Create mini-map showing nearby coins (Pokémon GO style radar)
        /// </summary>
        private void CreateMiniMap(Transform parent)
        {
            // Container in top-right corner - DOUBLED SIZE for visibility
            var mapContainer = new GameObject("MiniMapContainer");
            mapContainer.transform.SetParent(parent, false);
            
            var containerRect = mapContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 1); // Top-right
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(1, 1);
            containerRect.anchoredPosition = new Vector2(-20, -20);
            containerRect.sizeDelta = new Vector2(400, 400); // DOUBLED from 220
            
            _miniMapContainer = containerRect;
            _miniMapRadius = 180f; // DOUBLED from 100
            
            // Circular background
            var bgImage = mapContainer.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            bgImage.raycastTarget = false;
            
            // Range ring (50m indicator)
            var rangeRing = new GameObject("RangeRing");
            rangeRing.transform.SetParent(mapContainer.transform, false);
            var ringRect = rangeRing.AddComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(360, 360); // DOUBLED from 200
            var ringImage = rangeRing.AddComponent<Image>();
            ringImage.color = new Color(1, 1, 1, 0.2f);
            ringImage.raycastTarget = false;
            
            // Inner range ring (25m)
            var innerRing = new GameObject("InnerRing");
            innerRing.transform.SetParent(mapContainer.transform, false);
            var innerRect = innerRing.AddComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.5f, 0.5f);
            innerRect.anchorMax = new Vector2(0.5f, 0.5f);
            innerRect.sizeDelta = new Vector2(180, 180); // DOUBLED from 100
            var innerImage = innerRing.AddComponent<Image>();
            innerImage.color = new Color(1, 1, 1, 0.15f);
            innerImage.raycastTarget = false;
            
            // Player dot (center, blue) - DOUBLED SIZE
            var playerDot = new GameObject("PlayerDot");
            playerDot.transform.SetParent(mapContainer.transform, false);
            var playerRect = playerDot.AddComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(0.5f, 0.5f);
            playerRect.anchorMax = new Vector2(0.5f, 0.5f);
            playerRect.sizeDelta = new Vector2(28, 28); // DOUBLED from 16
            var playerImage = playerDot.AddComponent<Image>();
            playerImage.color = new Color(0.2f, 0.6f, 1f); // Blue
            playerImage.raycastTarget = false;
            _playerDot = playerRect;
            
            // Direction indicator (triangle showing where you're facing) - DOUBLED
            var dirIndicator = new GameObject("DirectionIndicator");
            dirIndicator.transform.SetParent(playerDot.transform, false);
            var dirRect = dirIndicator.AddComponent<RectTransform>();
            dirRect.anchoredPosition = new Vector2(0, 20); // DOUBLED from 12
            dirRect.sizeDelta = new Vector2(16, 16); // DOUBLED from 10
            var dirImage = dirIndicator.AddComponent<Image>();
            dirImage.color = new Color(0.2f, 0.6f, 1f);
            dirImage.raycastTarget = false;
            
            // Title - BIGGER
            var title = CreateText(mapContainer.transform, "MapTitle", "RADAR 50m", 
                Vector2.zero, 28, Color.white, FontStyles.Bold); // BIGGER font
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -8);
            
            // North indicator - BIGGER
            var northLabel = CreateText(mapContainer.transform, "NorthLabel", "N", 
                Vector2.zero, 24, Color.white, FontStyles.Bold); // BIGGER font
            var northRect = northLabel.GetComponent<RectTransform>();
            northRect.anchorMin = new Vector2(0.5f, 1);
            northRect.anchorMax = new Vector2(0.5f, 1);
            northRect.pivot = new Vector2(0.5f, 0.5f);
            northRect.anchoredPosition = new Vector2(0, -25);
            
            Debug.Log("[UIManager] 🗺️ Mini-map created!");
        }
        
        /// <summary>
        /// Update mini-map with coin positions
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
            
            // Track which coins we've updated
            HashSet<string> updatedCoins = new HashSet<string>();
            
            foreach (var controller in CoinManager.Instance.ActiveCoins)
            {
                if (controller?.CoinData == null) continue;
                
                var coin = controller.CoinData;
                
                // Calculate distance and bearing
                float distance = (float)playerLoc.DistanceTo(new LocationData(coin.latitude, coin.longitude));
                float bearing = (float)playerLoc.BearingTo(new LocationData(coin.latitude, coin.longitude));
                
                // Skip if out of range
                if (distance > _miniMapRange)
                {
                    RemoveMiniMapDot(coin.id);
                    continue;
                }
                
                // Adjust bearing for SMOOTHED compass heading (reduces jitter)
                float adjustedBearing = bearing - _smoothedCompassHeading;
                float bearingRad = adjustedBearing * Mathf.Deg2Rad;
                
                // Calculate position on mini-map
                float normalizedDist = distance / _miniMapRange;
                float pixelDist = normalizedDist * _miniMapRadius;
                float x = Mathf.Sin(bearingRad) * pixelDist;
                float y = Mathf.Cos(bearingRad) * pixelDist;
                
                // Get or create dot
                RectTransform dot;
                if (!_coinDots.TryGetValue(coin.id, out dot))
                {
                    var dotObj = new GameObject($"CoinDot_{coin.id}");
                    dotObj.transform.SetParent(_miniMapContainer, false);
                    dot = dotObj.AddComponent<RectTransform>();
                    dot.anchorMin = new Vector2(0.5f, 0.5f);
                    dot.anchorMax = new Vector2(0.5f, 0.5f);
                    dot.sizeDelta = new Vector2(24, 24); // BIGGER dots
                    var dotImage = dotObj.AddComponent<Image>();
                    dotImage.color = GoldColor;
                    dotImage.raycastTarget = false;
                    _coinDots[coin.id] = dot;
                }
                
                // Update position
                dot.anchoredPosition = new Vector2(x, y);
                dot.gameObject.SetActive(true);
                
                // Color based on state
                var img = dot.GetComponent<Image>();
                if (img != null)
                {
                    if (controller.IsInRange)
                        img.color = new Color(0.29f, 0.87f, 0.5f); // Green - in range!
                    else
                        img.color = GoldColor; // Gold
                }
                
                // Scale based on distance (closer = bigger)
                float scale = Mathf.Lerp(1.5f, 0.7f, normalizedDist);
                dot.localScale = Vector3.one * scale;
                
                updatedCoins.Add(coin.id);
            }
            
            // Remove dots for coins no longer active
            var toRemove = new List<string>();
            foreach (var id in _coinDots.Keys)
            {
                if (!updatedCoins.Contains(id))
                    toRemove.Add(id);
            }
            foreach (var id in toRemove)
            {
                RemoveMiniMapDot(id);
            }
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
        /// Handle Reveal Coin button click - anchors coin to AR plane
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
            
            // ================================================================
            // DIAGNOSTIC: Also spawn a simple test sphere directly in front of camera
            // This helps us verify that 3D rendering works at all!
            // ================================================================
            SpawnDiagnosticSphere();
            
            // Use proper AR anchor system
            var placer = ARCoinPlacer.Instance;
            if (placer != null)
            {
                Debug.Log($"[UIManager] 📍 ARCoinPlacer found, calling PlaceCoinInAR...");
                var placed = placer.PlaceCoinInAR(_coinToPlace);
                if (placed != null)
                {
                    Debug.Log($"[UIManager] ✅ Coin revealed successfully!");
                    Debug.Log($"[UIManager]    Placed at: {placed.CoinObject?.transform.position}");
                    // Hide button after placing
                    _placeCoinButton?.SetActive(false);
                    _coinToPlace = null;
                }
                else
                {
                    Debug.LogWarning("[UIManager] ⚠️ Failed to reveal coin - PlaceCoinInAR returned null");
                    _placeCoinButtonText.text = "SCAN GROUND FIRST!";
                }
            }
            else
            {
                Debug.LogError("[UIManager] ❌ ARCoinPlacer not found!");
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
        /// Update the Place Coin button visibility based on proximity
        /// </summary>
        private void UpdatePlaceCoinButton()
        {
            if (_placeCoinButton == null) return;
            if (GPSManager.Instance == null || CoinManager.Instance == null) return;
            
            var playerLoc = GPSManager.Instance.CurrentLocation;
            if (playerLoc == null)
            {
                _placeCoinButton.SetActive(false);
                return;
            }
            
            // Find closest coin within placement range
            Coin closestCoin = null;
            float closestDist = 20f; // Max placement distance
            
            foreach (var controller in CoinManager.Instance.ActiveCoins)
            {
                if (controller?.CoinData == null) continue;
                
                var coin = controller.CoinData;
                
                // Skip if already placed in AR
                if (ARCoinPlacer.Instance != null && ARCoinPlacer.Instance.IsCoinPlaced(coin.id))
                    continue;
                
                float distance = (float)playerLoc.DistanceTo(new LocationData(coin.latitude, coin.longitude));
                
                if (distance < closestDist)
                {
                    closestDist = distance;
                    closestCoin = coin;
                }
            }
            
            // Show/hide button based on proximity
            if (closestCoin != null)
            {
                _coinToPlace = closestCoin;
                _placeCoinButton.SetActive(true);
                _placeCoinButtonText.text = $"REVEAL COIN ({closestDist:F0}m)";
                
                // Change color based on distance
                var bgImage = _placeCoinButton.GetComponent<Image>();
                if (closestDist < 10f)
                    bgImage.color = new Color(0.2f, 0.8f, 0.3f, 0.9f); // Bright green - close!
                else
                    bgImage.color = new Color(0.8f, 0.6f, 0.2f, 0.9f); // Orange - getting closer
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
