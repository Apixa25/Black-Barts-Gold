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
        }
        
        /// <summary>
        /// Build the diagnostics string with current system state
        /// </summary>
        private string BuildDiagnosticsString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
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
                if (CoinManager.Instance.ActiveCoinCount > 0 && cam != null)
                {
                    var firstCoin = CoinManager.Instance.ActiveCoins[0];
                    if (firstCoin != null)
                    {
                        Vector3 coinWorldPos = firstCoin.transform.position;
                        sb.AppendLine($"<b>Coin1 World:</b> ({coinWorldPos.x:F1}, {coinWorldPos.y:F1}, {coinWorldPos.z:F1})");
                        sb.AppendLine($"Coin1 Dist: {firstCoin.DistanceFromPlayer:F1}m");
                        
                        // ================================================================
                        // CAMERA-RELATIVE direction (Pokémon GO style)
                        // Transform coin position into camera's local space
                        // ================================================================
                        Vector3 directionToCoin = coinWorldPos - cam.transform.position;
                        Vector3 localDir = cam.transform.InverseTransformDirection(directionToCoin);
                        
                        // In local space: +X is right, +Y is up, +Z is forward
                        // Using 0 threshold so direction is always clear
                        string xDir = localDir.x > 0 ? "RIGHT" : "LEFT";
                        string zDir = localDir.z > 0 ? "FRONT" : "BEHIND";
                        string yDir = localDir.y > 0.5f ? "UP" : (localDir.y < -0.5f ? "DOWN" : "");
                        string direction = $"{zDir} {xDir} {yDir}".Trim();
                        
                        if (string.IsNullOrEmpty(direction))
                        {
                            direction = "NEAR"; // Coin is very close in all directions
                        }
                        sb.AppendLine($"<b>Look:</b> {direction}");
                        
                        // Show local direction values for debugging
                        sb.AppendLine($"<b>Local:</b> ({localDir.x:F1}, {localDir.y:F1}, {localDir.z:F1})");
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
            
            // Instructions (bottom of screen)
            var instructions = CreateText(panel.transform, "Instructions", 
                "Point camera at ground to find treasure!", 
                new Vector2(0, -400), 24, Color.white, FontStyles.Normal);
            
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
            // Container in top-right corner
            var mapContainer = new GameObject("MiniMapContainer");
            mapContainer.transform.SetParent(parent, false);
            
            var containerRect = mapContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 1); // Top-right
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(1, 1);
            containerRect.anchoredPosition = new Vector2(-20, -20);
            containerRect.sizeDelta = new Vector2(220, 220);
            
            _miniMapContainer = containerRect;
            
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
            ringRect.sizeDelta = new Vector2(200, 200);
            var ringImage = rangeRing.AddComponent<Image>();
            ringImage.color = new Color(1, 1, 1, 0.2f);
            ringImage.raycastTarget = false;
            
            // Inner range ring (25m)
            var innerRing = new GameObject("InnerRing");
            innerRing.transform.SetParent(mapContainer.transform, false);
            var innerRect = innerRing.AddComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.5f, 0.5f);
            innerRect.anchorMax = new Vector2(0.5f, 0.5f);
            innerRect.sizeDelta = new Vector2(100, 100);
            var innerImage = innerRing.AddComponent<Image>();
            innerImage.color = new Color(1, 1, 1, 0.15f);
            innerImage.raycastTarget = false;
            
            // Player dot (center, blue)
            var playerDot = new GameObject("PlayerDot");
            playerDot.transform.SetParent(mapContainer.transform, false);
            var playerRect = playerDot.AddComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(0.5f, 0.5f);
            playerRect.anchorMax = new Vector2(0.5f, 0.5f);
            playerRect.sizeDelta = new Vector2(16, 16);
            var playerImage = playerDot.AddComponent<Image>();
            playerImage.color = new Color(0.2f, 0.6f, 1f); // Blue
            playerImage.raycastTarget = false;
            _playerDot = playerRect;
            
            // Direction indicator (triangle showing where you're facing)
            var dirIndicator = new GameObject("DirectionIndicator");
            dirIndicator.transform.SetParent(playerDot.transform, false);
            var dirRect = dirIndicator.AddComponent<RectTransform>();
            dirRect.anchoredPosition = new Vector2(0, 12);
            dirRect.sizeDelta = new Vector2(10, 10);
            var dirImage = dirIndicator.AddComponent<Image>();
            dirImage.color = new Color(0.2f, 0.6f, 1f);
            dirImage.raycastTarget = false;
            
            // Title
            var title = CreateText(mapContainer.transform, "MapTitle", "RADAR 50m", 
                Vector2.zero, 18, Color.white, FontStyles.Bold);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -5);
            
            // North indicator
            var northLabel = CreateText(mapContainer.transform, "NorthLabel", "N", 
                Vector2.zero, 16, Color.white, FontStyles.Bold);
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
            
            float currentHeading = Input.compass.enabled ? Input.compass.trueHeading : 0f;
            
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
                
                // Adjust bearing for compass heading (so "up" on map = direction you're facing)
                float adjustedBearing = bearing - currentHeading;
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
                    dot.sizeDelta = new Vector2(14, 14);
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
