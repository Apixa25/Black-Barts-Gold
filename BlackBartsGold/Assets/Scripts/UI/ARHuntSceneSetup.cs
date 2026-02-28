// ============================================================================
// ARHuntSceneSetup.cs
// Black Bart's Gold - ARHunt Scene Setup
// Path: Assets/Scripts/UI/ARHuntSceneSetup.cs
// Last Modified: 2026-01-27 20:30 - Added radar setup and diagnostics
// ============================================================================
// Sets up the AR Hunt scene HUD overlay at runtime.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TMPro;
using System.Collections;
using System.Text;
using System.Reflection;
using BlackBartsGold.Location;
using BlackBartsGold.Core;
using BlackBartsGold.Utils;
using BlackBartsGold.AR;

namespace BlackBartsGold.UI
{
    [DefaultExecutionOrder(-100)] // Run before ARHUD so panels exist when InitializeRuntimeReferences is called
    public class ARHuntSceneSetup : MonoBehaviour
    {
        private readonly Color GoldColor = new Color(1f, 0.84f, 0f);
        private readonly Color SemiTransparentBlack = new Color(0, 0, 0, 0.5f);

        private TextMeshProUGUI _debugDiagnosticsText;
        private TextMeshProUGUI _debugTitleText;
        private RawImage _radarMapTileImage;
        private float _radarMapLastUpdate;
        private double _radarMapLastLat, _radarMapLastLng;
        private bool _radarMapUpdatePending;
        private Texture2D _radarMapCurrentTile;
        private bool _radarMapTileIsOurCopy;
        private const float _radarControlsRefreshInterval = 0.25f;
        private const float _sensorConsoleRefreshInterval = 0.5f;
        private int _radarZoom = 19; // 19 = default (3 levels closer); 21 = zoomed in when hunting
        private int _lastLoggedRadarTileZoom = -1;
        private Sprite _cachedMapCoinIconSprite;
        private bool _mapCoinIconLoadLogged = false;
        private Sprite _directionArrowSprite;
        private const float _miniMapUiScale = 2f; // Single tuning point for AR mini-map sizing
        private const float _radarBaseSize = 360f;
        private float _lastRadarControlsRefresh;
        private RadarUI _radarZoomRadarUI;
        private Button _radarMinusButton;
        private Button _radarPlusButton;
        private Button _radarAutoButton;
        private TextMeshProUGUI _radarRangeText;
        private AttitudeSensor _attitudeSensor;
        private Accelerometer _accelerometer;
        private GravitySensor _gravitySensor;
        private UnityEngine.InputSystem.Gyroscope _gyroscope;
        private MagneticFieldSensor _magneticFieldSensor;
        private Vector3 _lastCameraPosition;
        private float _cameraMovementSinceStart;
        private string _lastArState;
        private string _lastArStateChangeInfo;
        private float _lastSensorConsoleRefresh;
        private float _lastVerboseLifecycleLog;
        private float _lastAdbSensorSnapshotLog;
        private float _lastHardKillPassTime;
        private int _hardKillPassIteration;
        private float _lastUiTracerLogTime;
        private int _uiTracerSamplesTaken;
        private const float _verboseLifecycleInterval = 3f;
        private const float _adbSensorSnapshotInterval = 2f;
        private const float _hardKillPassInterval = 1f;
        private const float _uiTracerInterval = 2f;
        private const int _uiTracerMaxSamples = 8;
        private TextMeshProUGUI _sensorStatusText;
        private Transform _sensorStatusPanel;
        private const bool EnableRuntimeDevConsole = false;
        
        private void Start()
        {
            DiagnosticLog.Log("Setup", $"AR SCENE START T+{Time.realtimeSinceStartup:F2}s");
            DiagnosticLog.Log("Setup", $"GameObject: {gameObject.name}");
            DiagnosticLog.Log("Setup", $"Scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} children={transform.childCount} screen={Screen.width}x{Screen.height}");
            
            // Single source of truth: remove scene-based FullMapPanel - we use UIManager's code-based map only
            var fullMap = transform.Find("FullMapPanel");
            if (fullMap != null)
            {
                Destroy(fullMap.gameObject);
                DiagnosticLog.Log("Setup", "Removed scene-based FullMapPanel - using code-based map only");
            }
            
            SetupCanvas();
            HardGlobalKillPass("start");
            if (EnableRuntimeDevConsole)
            {
                InitializeDevelopmentConsoleSensors();
            }
            DisableForeignDebugPanels();
            CleanupStrayCenteredImages(); // Remove white square from orphan CompassArrowPanel etc.
            SetupBackButton();
            SetupCrosshairs();
            SetupRadarPanel();
            RemoveDevelopmentConsolePanel();
            if (EnableRuntimeDevConsole)
            {
                SetupSensorStatusPanel();
            }
            SetupMessagePanel();
            SetupLockedPopup();
            SetupCollectionPopup();
            SetupCoinInfoPanel();
            SetupCompassPanel();
            SetupGasMeterPanel();
            SetupFindLimitPanel();
            SetupDirectionIndicatorPanel();
            SetupLightship(); // Pokemon GO technology!
            HardGlobalKillPass("post-setup");
            
            // Subscribe to hunt mode - zoom radar in when coin selected
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnHuntModeChanged += OnHuntModeChanged;
            }

            // Wire ARHUD to code-created panels (must run after all Setup* methods)
            var arhud = GetComponentInChildren<ARHUD>(true);
            if (arhud != null)
            {
                arhud.InitializeRuntimeReferences(transform);
                DiagnosticLog.Log("Setup", "ARHUD runtime references initialized");
            }
            else
            {
                DiagnosticLog.Warn("Setup", "ARHUD not found - panels may not work");
            }
            
            DiagnosticLog.Log("Setup", "AR HUD setup COMPLETE");
        }
        
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }
        
        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void OnDestroy()
        {
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnHuntModeChanged -= OnHuntModeChanged;
            }
            if (_radarMapCurrentTile != null && _radarMapTileIsOurCopy)
                Destroy(_radarMapCurrentTile);
        }
        
        private void OnHuntModeChanged(HuntMode mode)
        {
            _radarZoom = ComputeMiniMapTileZoom();
            _radarMapUpdatePending = false;
            _radarMapLastUpdate = -999f; // Force immediate refresh on next Update
        }
        
        private void Update()
        {
            // Update radar map tile from Mapbox
            UpdateRadarMapTile();

            if (EnableRuntimeDevConsole && _sensorStatusText != null && Time.time - _lastSensorConsoleRefresh >= _sensorConsoleRefreshInterval)
            {
                _lastSensorConsoleRefresh = Time.time;
                _sensorStatusText.text = BuildSensorStatusString();
                EnsureSensorStatusPanelVisible();
            }

            if (_radarZoomRadarUI != null && Time.time - _lastRadarControlsRefresh >= _radarControlsRefreshInterval)
            {
                _lastRadarControlsRefresh = Time.time;
                RefreshRadarZoomControlsUi();
            }

            if (Time.time - _lastHardKillPassTime >= _hardKillPassInterval)
            {
                _lastHardKillPassTime = Time.time;
                HardGlobalKillPass("heartbeat");
            }

            if (_uiTracerSamplesTaken < _uiTracerMaxSamples && Time.time - _lastUiTracerLogTime >= _uiTracerInterval)
            {
                _lastUiTracerLogTime = Time.time;
                _uiTracerSamplesTaken++;
                TraceHudArtifactCandidates();
                TraceBottomLeftAcrossCanvases();
                TraceNoFilterAcrossCanvases();
            }

            if (Time.time - _lastVerboseLifecycleLog >= _verboseLifecycleInterval)
            {
                _lastVerboseLifecycleLog = Time.time;
                RemoveDevelopmentConsolePanel();
                CleanupDirectionIndicatorArtifacts();
                EnsureRadarPanelOperational();
                bool debugPanelPresent = HasDevelopmentOverlayVisible();
                DiagnosticLog.Log("Setup", $"Heartbeat t={Time.time:F1}s devConsole={debugPanelPresent} radarUI={(_radarZoomRadarUI != null)} mapTile={(_radarMapTileImage != null)}");
            }

            if (EnableRuntimeDevConsole && Time.time - _lastAdbSensorSnapshotLog >= _adbSensorSnapshotInterval)
            {
                _lastAdbSensorSnapshotLog = Time.time;
                EmitPeriodicSensorSnapshotLog();
            }
        }

        private void InitializeDevelopmentConsoleSensors()
        {
            // Ensure all sensor devices used by AR/nav pipelines are enabled for diagnostics and runtime behavior.
            DeviceCompass.Initialize();
            DiagnosticLog.Log("Sensors", $"DeviceCompass available={DeviceCompass.IsAvailable} heading={DeviceCompass.Heading:F1} method={DeviceCompass.ActiveMethod}");

            _attitudeSensor = AttitudeSensor.current;
            if (_attitudeSensor != null) InputSystem.EnableDevice(_attitudeSensor);
            DiagnosticLog.Log("Sensors", $"AttitudeSensor present={_attitudeSensor != null} enabled={(_attitudeSensor != null && _attitudeSensor.enabled)}");

            _accelerometer = Accelerometer.current;
            if (_accelerometer != null) InputSystem.EnableDevice(_accelerometer);
            DiagnosticLog.Log("Sensors", $"Accelerometer present={_accelerometer != null} enabled={(_accelerometer != null && _accelerometer.enabled)}");

            _gravitySensor = GravitySensor.current;
            if (_gravitySensor != null) InputSystem.EnableDevice(_gravitySensor);
            DiagnosticLog.Log("Sensors", $"GravitySensor present={_gravitySensor != null} enabled={(_gravitySensor != null && _gravitySensor.enabled)}");

            _gyroscope = UnityEngine.InputSystem.Gyroscope.current;
            if (_gyroscope != null) InputSystem.EnableDevice(_gyroscope);
            DiagnosticLog.Log("Sensors", $"InputSystem Gyroscope present={_gyroscope != null} enabled={(_gyroscope != null && _gyroscope.enabled)}");

            _magneticFieldSensor = MagneticFieldSensor.current;
            if (_magneticFieldSensor != null) InputSystem.EnableDevice(_magneticFieldSensor);
            DiagnosticLog.Log("Sensors", $"MagneticFieldSensor present={_magneticFieldSensor != null} enabled={(_magneticFieldSensor != null && _magneticFieldSensor.enabled)}");

            Input.compass.enabled = true;
            DiagnosticLog.Log("Sensors", $"Legacy compass enabled={Input.compass.enabled} rawHeading={Input.compass.trueHeading:F1}");
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
            }
            DiagnosticLog.Log("Sensors", $"Legacy gyro supported={SystemInfo.supportsGyroscope} enabled={Input.gyro.enabled}");

            var cam = Camera.main;
            _lastCameraPosition = cam != null ? cam.transform.position : Vector3.zero;
            _cameraMovementSinceStart = 0f;
            _lastArState = UnityEngine.XR.ARFoundation.ARSession.state.ToString();
            _lastArStateChangeInfo = "AR state stable";
            DiagnosticLog.Log("Sensors", $"Initial AR state={_lastArState} cameraFound={cam != null}");
        }

        private string BuildSensorStatusString()
        {
            try
            {
                var sb = new StringBuilder(700);
                string arState = UnityEngine.XR.ARFoundation.ARSession.state.ToString();
                var gpsManager = GPSManager.Instance;
                var coinManager = CoinManager.Exists ? CoinManager.Instance : null;

                bool gpsOn = Input.location.status == LocationServiceStatus.Running;
                bool gpsWorking = gpsManager != null && gpsManager.CurrentLocation != null;
                float gpsAcc = gpsWorking ? gpsManager.CurrentLocation.horizontalAccuracy : -1f;

                bool compassOn = DeviceCompass.IsAvailable || Input.compass.enabled;
                bool compassWorking = DeviceCompass.IsAvailable && DeviceCompass.ActiveMethod != "none";

                bool gyroOn = (_gyroscope != null && _gyroscope.enabled) || Input.gyro.enabled;
                bool gyroWorking = false;
                if (_gyroscope != null && _gyroscope.enabled)
                {
                    Vector3 rate = _gyroscope.angularVelocity.ReadValue();
                    gyroWorking = rate.sqrMagnitude > 0.0001f;
                }

                bool accelOn = _accelerometer != null && _accelerometer.enabled;
                bool accelWorking = false;
                if (accelOn)
                {
                    Vector3 accel = _accelerometer.acceleration.ReadValue();
                    accelWorking = accel.sqrMagnitude > 0.01f;
                }

                bool gravityOn = _gravitySensor != null && _gravitySensor.enabled;
                bool gravityWorking = false;
                if (gravityOn)
                {
                    Vector3 grav = _gravitySensor.gravity.ReadValue();
                    gravityWorking = grav.sqrMagnitude > 0.01f;
                }

                bool attitudeOn = _attitudeSensor != null && _attitudeSensor.enabled;
                bool attitudeWorking = false;
                if (attitudeOn)
                {
                    Quaternion att = _attitudeSensor.attitude.ReadValue();
                    attitudeWorking = att.x != 0f || att.y != 0f || att.z != 0f;
                }

                bool magOn = _magneticFieldSensor != null && _magneticFieldSensor.enabled;
                bool magWorking = false;
                if (magOn)
                {
                    Vector3 mag = _magneticFieldSensor.magneticField.ReadValue();
                    magWorking = mag.sqrMagnitude > 1f;
                }

                string metersToCoin = "n/a";
                if (coinManager != null && coinManager.HasTarget && coinManager.TargetCoin != null)
                {
                    var renderer = coinManager.TargetCoin.GetComponent<ARCoinRenderer>();
                    if (renderer != null)
                    {
                        metersToCoin = $"{renderer.GPSDistance:F1}m";
                    }
                }

                sb.AppendLine("<b>SENSOR STATUS</b>");
                sb.AppendLine($"AR: {arState}");
                sb.AppendLine($"Compass: {(compassOn ? "ON" : "OFF")}/{(compassWorking ? "YES" : "NO")}  {DeviceCompass.Heading:F1} deg");
                sb.AppendLine($"Coin distance: {metersToCoin}");
                sb.AppendLine($"GPS: {(gpsOn ? "ON" : "OFF")}/{(gpsWorking ? "YES" : "NO")}  acc={(gpsAcc >= 0f ? $"{gpsAcc:F1}m" : "n/a")}");
                sb.AppendLine($"Gyro: {(gyroOn ? "ON" : "OFF")}/{(gyroWorking ? "YES" : "NO")}");
                sb.AppendLine($"Accel: {(accelOn ? "ON" : "OFF")}/{(accelWorking ? "YES" : "NO")}");
                sb.AppendLine($"Gravity: {(gravityOn ? "ON" : "OFF")}/{(gravityWorking ? "YES" : "NO")}");
                sb.AppendLine($"Attitude: {(attitudeOn ? "ON" : "OFF")}/{(attitudeWorking ? "YES" : "NO")}");
                sb.AppendLine($"MagField: {(magOn ? "ON" : "OFF")}/{(magWorking ? "YES" : "NO")}");

                return sb.ToString();
            }
            catch (System.Exception ex)
            {
                DiagnosticLog.Error("Setup", $"BuildSensorStatusString exception: {ex.GetType().Name}: {ex.Message}");
                return "<b>SENSOR STATUS</b>\nUnavailable (build error)";
            }
        }

        private void EmitPeriodicSensorSnapshotLog()
        {
            bool gpsOn = Input.location.status == LocationServiceStatus.Running;
            bool gpsWorking = GPSManager.Instance != null && GPSManager.Instance.CurrentLocation != null;
            string gpsSummary = gpsWorking
                ? $"YES acc={GPSManager.Instance.CurrentLocation.horizontalAccuracy:F1}m"
                : $"NO status={Input.location.status}";

            bool compassOn = DeviceCompass.IsAvailable || Input.compass.enabled;
            bool compassWorking = DeviceCompass.IsAvailable && DeviceCompass.ActiveMethod != "none";

            bool gyroOn = (_gyroscope != null && _gyroscope.enabled) || Input.gyro.enabled;
            Vector3 gyroRate = Vector3.zero;
            bool gyroWorking = false;
            if (_gyroscope != null && _gyroscope.enabled)
            {
                gyroRate = _gyroscope.angularVelocity.ReadValue();
                gyroWorking = gyroRate.sqrMagnitude > 0.0001f;
            }

            bool accelOn = _accelerometer != null && _accelerometer.enabled;
            Vector3 accel = accelOn ? _accelerometer.acceleration.ReadValue() : Input.acceleration;
            bool accelWorking = accel.sqrMagnitude > 0.01f;

            bool gravityOn = _gravitySensor != null && _gravitySensor.enabled;
            Vector3 grav = gravityOn ? _gravitySensor.gravity.ReadValue() : Vector3.zero;
            bool gravityWorking = gravityOn && grav.sqrMagnitude > 0.01f;

            bool attitudeOn = _attitudeSensor != null && _attitudeSensor.enabled;
            Quaternion attitude = attitudeOn ? _attitudeSensor.attitude.ReadValue() : Quaternion.identity;
            bool attitudeWorking = attitudeOn && (attitude.x != 0f || attitude.y != 0f || attitude.z != 0f);

            var arState = UnityEngine.XR.ARFoundation.ARSession.state.ToString();
            int trackedPlanes = 0;
            var planeManager = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARPlaneManager>();
            if (planeManager != null) trackedPlanes = planeManager.trackables.count;

            string targetSummary = "target=n/a";
            if (CoinManager.Exists && CoinManager.Instance != null && CoinManager.Instance.HasTarget && CoinManager.Instance.TargetCoin != null)
            {
                var targetRenderer = CoinManager.Instance.TargetCoin.GetComponent<ARCoinRenderer>();
                if (targetRenderer != null)
                {
                    targetSummary = $"targetDist={targetRenderer.GPSDistance:F1}m mode={CoinManager.Instance.CurrentMode}";
                }
                else
                {
                    targetSummary = $"target=YES mode={CoinManager.Instance.CurrentMode}";
                }
            }

            DiagnosticLog.Log(
                "Sensors",
                $"SNAPSHOT t={Time.realtimeSinceStartup:F1}s | AR={arState} planes={trackedPlanes} | GPS={gpsSummary} | " +
                $"Compass {(compassOn ? "ON" : "OFF")}/{(compassWorking ? "YES" : "NO")} heading={DeviceCompass.Heading:F1}({DeviceCompass.ActiveMethod}) | " +
                $"Gyro {(gyroOn ? "ON" : "OFF")}/{(gyroWorking ? "YES" : "NO")} rate=({gyroRate.x:F2},{gyroRate.y:F2},{gyroRate.z:F2}) | " +
                $"Accel {(accelOn ? "ON" : "OFF")}/{(accelWorking ? "YES" : "NO")} xyz=({accel.x:F2},{accel.y:F2},{accel.z:F2}) | " +
                $"Gravity {(gravityOn ? "ON" : "OFF")}/{(gravityWorking ? "YES" : "NO")} xyz=({grav.x:F2},{grav.y:F2},{grav.z:F2}) | " +
                $"Attitude {(attitudeOn ? "ON" : "OFF")}/{(attitudeWorking ? "YES" : "NO")} q=({attitude.x:F2},{attitude.y:F2},{attitude.z:F2},{attitude.w:F2}) | " +
                targetSummary
            );
        }

        private void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Render on top of AR
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        /// <summary>
        /// Remove stray scene-baked UI that causes a white square at screen center.
        /// CompassArrowPanel has an Image with no sprite (renders white). Not used by code-based CompassPanel.
        /// </summary>
        private void CleanupStrayCenteredImages()
        {
            var compassArrow = transform.Find("CompassArrowPanel");
            if (compassArrow != null)
            {
                var img = compassArrow.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = false;
                    Destroy(img);
                    DiagnosticLog.Log("Setup", "Disabled orphan CompassArrowPanel Image (was causing white square)");
                }
                // Deactivate entire panel - it's unused (CompassPanel is created in code)
                compassArrow.gameObject.SetActive(false);
            }

            // Generic safety net: disable centered, pure-white, no-sprite images that render as opaque squares.
            var images = GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image == null) continue;
                var rect = image.rectTransform;
                if (rect == null) continue;

                bool centeredAnchor = Mathf.Abs(rect.anchorMin.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMin.y - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.y - 0.5f) < 0.01f;
                bool centeredPosition = rect.anchoredPosition.sqrMagnitude < 9f;
                bool looksLikeWhiteSquare = image.color.a > 0.95f
                    && image.color.r > 0.95f
                    && image.color.g > 0.95f
                    && image.color.b > 0.95f;
                bool hasNoVisualSource = image.sprite == null;
                string lowerName = image.gameObject.name.ToLowerInvariant();
                bool explicitArtifactName = lowerName.Contains("crosshair")
                    || lowerName.Contains("compassarrow")
                    || lowerName.Contains("center")
                    || lowerName.Contains("reticle");

                // Be intentionally aggressive: any centered, pure-white, sprite-less image (or explicitly named artifact)
                // is treated as a stray artifact and disabled, regardless of its exact size.
                if (centeredAnchor && centeredPosition && ((looksLikeWhiteSquare && hasNoVisualSource) || explicitArtifactName))
                {
                    image.enabled = false;
                    DiagnosticLog.Log("Setup", $"Disabled centered Image artifact: {image.gameObject.name} size={rect.sizeDelta} sprite={(image.sprite != null)}");
                }
            }

            var rawImages = GetComponentsInChildren<RawImage>(true);
            foreach (var rawImage in rawImages)
            {
                if (rawImage == null || rawImage.texture != null) continue;
                var rect = rawImage.rectTransform;
                if (rect == null) continue;

                bool centeredAnchor = Mathf.Abs(rect.anchorMin.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMin.y - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.y - 0.5f) < 0.01f;
                bool centeredPosition = rect.anchoredPosition.sqrMagnitude < 9f;
                bool looksLikeWhiteSquare = rawImage.color.a > 0.95f
                    && rawImage.color.r > 0.95f
                    && rawImage.color.g > 0.95f
                    && rawImage.color.b > 0.95f;

                // RawImage artifacts: if they're centered, pure white, and have no texture, they are almost certainly the white box.
                if (centeredAnchor && centeredPosition && looksLikeWhiteSquare)
                {
                    rawImage.enabled = false;
                    DiagnosticLog.Log("Setup", $"Disabled centered white no-texture RawImage artifact: {rawImage.gameObject.name}");
                }
            }
        }

        private void CleanupDirectionIndicatorArtifacts()
        {
            var panel = transform.Find("DirectionIndicatorPanel");
            if (panel == null) return;

            var images = panel.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image == null) continue;
                var rect = image.rectTransform;
                if (rect == null) continue;

                bool centeredAnchor = Mathf.Abs(rect.anchorMin.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMin.y - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.y - 0.5f) < 0.01f;
                bool centeredPosition = rect.anchoredPosition.sqrMagnitude < 9f;
                bool hasNoSprite = image.sprite == null;
                bool opaqueWhite = image.color.a > 0.95f
                    && image.color.r > 0.95f
                    && image.color.g > 0.95f
                    && image.color.b > 0.95f;

                if (centeredAnchor && centeredPosition && hasNoSprite && opaqueWhite)
                {
                    image.enabled = false;
                    DiagnosticLog.Log("Setup", $"Disabled DirectionIndicatorPanel white-square Image artifact: {image.gameObject.name}");
                }
            }

            var rawImages = panel.GetComponentsInChildren<RawImage>(true);
            foreach (var rawImage in rawImages)
            {
                if (rawImage == null || rawImage.texture != null) continue;
                var rect = rawImage.rectTransform;
                if (rect == null) continue;

                bool centeredAnchor = Mathf.Abs(rect.anchorMin.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMin.y - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.x - 0.5f) < 0.01f
                    && Mathf.Abs(rect.anchorMax.y - 0.5f) < 0.01f;
                bool centeredPosition = rect.anchoredPosition.sqrMagnitude < 9f;
                bool opaqueWhite = rawImage.color.a > 0.95f
                    && rawImage.color.r > 0.95f
                    && rawImage.color.g > 0.95f
                    && rawImage.color.b > 0.95f;

                if (centeredAnchor && centeredPosition && opaqueWhite)
                {
                    rawImage.enabled = false;
                    DiagnosticLog.Log("Setup", $"Disabled DirectionIndicatorPanel white-square RawImage artifact: {rawImage.gameObject.name}");
                }
            }
        }

        private void DisableForeignDebugPanels()
        {
            RemoveDevelopmentConsolePanel();

            // Prevent tiny legacy/persistent debug panels from showing in AR instead of this scene's console.
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj == null || obj.name != "DebugDiagnosticsPanel") continue;
                obj.SetActive(false);
                DiagnosticLog.Log("Setup", $"Disabled foreign DebugDiagnosticsPanel: {obj.name}");
            }

            // Disable legacy OnGUI emergency overlay if present.
            var emergencyBtn = FindFirstObjectByType<EmergencyMapButton>();
            if (emergencyBtn != null)
            {
                emergencyBtn.showButton = false;
                emergencyBtn.showDebugInfo = false;
                emergencyBtn.enabled = false;
                DiagnosticLog.Log("Setup", "EmergencyMapButton overlay disabled");
            }

            DisableOnGuiOverlays("foreign-panel-sweep");
        }

        private void DisableRuntimeDiagnosticsForProduction()
        {
            // Keep AR HUD production-clean: disable debug scripts that consume cycles and render diagnostics.
            var startupLoggers = FindObjectsByType<BlackBartsGold.Diagnostics.StartupLogger>(FindObjectsSortMode.None);
            foreach (var logger in startupLoggers)
            {
                if (logger == null) continue;
                logger.enabled = false;
                Destroy(logger);
            }

            var sensorDiagnostics = FindObjectsByType<BlackBartsGold.Diagnostics.SensorDiagnostics>(FindObjectsSortMode.None);
            foreach (var diag in sensorDiagnostics)
            {
                if (diag == null) continue;
                diag.enabled = false;
                Destroy(diag);
            }

            var arTrackingDebug = FindObjectsByType<BlackBartsGold.AR.ARTrackingDebug>(FindObjectsSortMode.None);
            foreach (var debug in arTrackingDebug)
            {
                if (debug == null) continue;
                debug.enabled = false;
                Destroy(debug);
            }
        }

        private void HardGlobalKillPass(string reason)
        {
            _hardKillPassIteration++;
            int removedComponents = 0;
            int removedObjects = 0;

            // Kill known legacy debug MonoBehaviours that continue logging/updating every frame.
            var startupLoggers = FindObjectsByType<BlackBartsGold.Diagnostics.StartupLogger>(FindObjectsSortMode.None);
            foreach (var logger in startupLoggers)
            {
                if (logger == null) continue;
                logger.enabled = false;
                Destroy(logger);
                removedComponents++;
            }

            var sensorDiagnostics = FindObjectsByType<BlackBartsGold.Diagnostics.SensorDiagnostics>(FindObjectsSortMode.None);
            foreach (var diag in sensorDiagnostics)
            {
                if (diag == null) continue;
                diag.enabled = false;
                Destroy(diag);
                removedComponents++;
            }

            var arTrackingDebug = FindObjectsByType<BlackBartsGold.AR.ARTrackingDebug>(FindObjectsSortMode.None);
            foreach (var debug in arTrackingDebug)
            {
                if (debug == null) continue;
                debug.enabled = false;
                Destroy(debug);
                removedComponents++;
            }

            var simpleArrows = FindObjectsByType<SimpleDirectionArrow>(FindObjectsSortMode.None);
            foreach (var arrow in simpleArrows)
            {
                if (arrow == null) continue;
                arrow.enabled = false;
                Destroy(arrow);
                removedComponents++;
            }

            // Remove common debug overlay roots by name regardless of source scene/prefab.
            string[] bannedNames =
            {
                "DebugDiagnosticsPanel",
                "DiagnosticsText",
                "DiagnosticsTitle",
                "DevelopmentConsolePanel",
                "SensorStatusPanel",
                "SensorText"
            };

            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                if (obj == gameObject) continue;
                if (obj.name == "RadarPanel" || obj.name == "MapTile" || obj.name == "Crosshairs" || obj.name == "BackButton") continue;

                bool isBanned = false;
                for (int i = 0; i < bannedNames.Length; i++)
                {
                    if (obj.name == bannedNames[i])
                    {
                        isBanned = true;
                        break;
                    }
                }
                if (!isBanned) continue;

                obj.SetActive(false);
                Destroy(obj);
                removedObjects++;
            }

            // Keep existing broad text-pattern cleanup too.
            RemoveDevelopmentConsolePanel();
            removedObjects += RemoveBottomLeftDiagnosticCandidates();
            removedComponents += DisableOnGuiOverlays($"kill-pass:{reason}");

            if (removedComponents > 0 || removedObjects > 0 || _hardKillPassIteration <= 3)
            {
                DiagnosticLog.Log("KillPass", $"reason={reason} iteration={_hardKillPassIteration} removedComponents={removedComponents} removedObjects={removedObjects}");
            }
        }

        private int DisableOnGuiOverlays(string reason)
        {
            int disabled = 0;
            int traced = 0;
            const int maxDetailedLogs = 8;

            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || !behaviour.enabled) continue;
                if (behaviour == this) continue;

                var type = behaviour.GetType();
                var onGuiMethod = type.GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (onGuiMethod == null) continue;

                if (behaviour is EmergencyMapButton emergencyMapButton)
                {
                    emergencyMapButton.showButton = false;
                    emergencyMapButton.showDebugInfo = false;
                }

                behaviour.enabled = false;
                disabled++;

                var owner = behaviour.gameObject;
                if (owner != null)
                {
                    string lowerName = owner.name.ToLowerInvariant();
                    bool looksLikeOverlayRoot = lowerName.Contains("debug")
                        || lowerName.Contains("diagnostic")
                        || lowerName.Contains("console")
                        || lowerName.Contains("emergency");
                    if (looksLikeOverlayRoot && owner.activeSelf)
                    {
                        owner.SetActive(false);
                    }
                }

                if (traced < maxDetailedLogs)
                {
                    traced++;
                    DiagnosticLog.Log("OnGUIKill", $"reason={reason} disabled {type.FullName} on {owner?.name}");
                }
            }

            if (disabled > 0)
            {
                DiagnosticLog.Log("OnGUIKill", $"reason={reason} disabledTotal={disabled}");
            }

            return disabled;
        }

        private int RemoveBottomLeftDiagnosticCandidates()
        {
            int removed = 0;
            var rects = GetComponentsInChildren<RectTransform>(true);
            foreach (var rect in rects)
            {
                if (rect == null || rect == transform) continue;
                var go = rect.gameObject;
                if (go == null || !go.activeInHierarchy) continue;
                if (go.name == "BackButton" || go.name == "RadarPanel" || go.name == "MessagePanel") continue;

                bool anchorBottomLeft = rect.anchorMin.x <= 0.05f && rect.anchorMin.y <= 0.05f
                    && rect.anchorMax.x <= 0.05f && rect.anchorMax.y <= 0.05f;
                bool nearBottomLeft = rect.anchoredPosition.x < 420f && rect.anchoredPosition.y < 420f;
                if (!(anchorBottomLeft && nearBottomLeft)) continue;

                string lowerName = go.name.ToLowerInvariant();
                bool suspiciousName = lowerName.Contains("debug")
                    || lowerName.Contains("diagnostic")
                    || lowerName.Contains("console")
                    || lowerName.Contains("sensor");

                string textBlob = string.Empty;
                var tmpTexts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < tmpTexts.Length; i++)
                {
                    if (tmpTexts[i] == null) continue;
                    textBlob += " " + (tmpTexts[i].text ?? string.Empty);
                }
                var legacyTexts = go.GetComponentsInChildren<Text>(true);
                for (int i = 0; i < legacyTexts.Length; i++)
                {
                    if (legacyTexts[i] == null) continue;
                    textBlob += " " + (legacyTexts[i].text ?? string.Empty);
                }

                bool looksLikeDiagnostics = textBlob.Contains("AR:")
                    || textBlob.Contains("Planes:")
                    || textBlob.Contains("API:")
                    || textBlob.Contains("Compass")
                    || textBlob.Contains("Gyro")
                    || textBlob.Contains("Accel")
                    || textBlob.Contains("GPS");

                if (!suspiciousName && !looksLikeDiagnostics) continue;

                var owner = GetDirectCanvasChild(rect);
                var killTarget = owner != null ? owner.gameObject : go;
                if (killTarget == null || killTarget == gameObject) continue;
                if (killTarget.name == "BackButton" || killTarget.name == "RadarPanel" || killTarget.name == "MessagePanel") continue;

                killTarget.SetActive(false);
                Destroy(killTarget);
                removed++;
            }

            return removed;
        }

        private void TraceHudArtifactCandidates()
        {
            int logged = 0;
            const int maxLogged = 14;

            var rects = GetComponentsInChildren<RectTransform>(true);
            foreach (var rect in rects)
            {
                if (rect == null || rect == transform) continue;
                var go = rect.gameObject;
                if (go == null || !go.activeInHierarchy) continue;

                bool anchorCenter = Mathf.Abs(rect.anchorMin.x - 0.5f) < 0.02f
                    && Mathf.Abs(rect.anchorMin.y - 0.5f) < 0.02f
                    && Mathf.Abs(rect.anchorMax.x - 0.5f) < 0.02f
                    && Mathf.Abs(rect.anchorMax.y - 0.5f) < 0.02f;
                bool anchorBottomLeft = rect.anchorMin.x < 0.05f && rect.anchorMin.y < 0.05f
                    && rect.anchorMax.x < 0.05f && rect.anchorMax.y < 0.05f;
                bool centerByPosition = rect.anchoredPosition.sqrMagnitude < 400f; // ~20px radius
                bool bottomLeftByPosition = rect.anchoredPosition.x < 260f && rect.anchoredPosition.y < 260f;

                bool candidate = (anchorCenter && centerByPosition) || (anchorBottomLeft && bottomLeftByPosition);
                if (!candidate) continue;

                var img = go.GetComponent<Image>();
                var raw = go.GetComponent<RawImage>();
                var tmp = go.GetComponent<TextMeshProUGUI>();

                string textPreview = string.Empty;
                if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                {
                    string compact = tmp.text.Replace('\n', ' ').Replace('\r', ' ');
                    textPreview = compact.Length > 80 ? compact.Substring(0, 80) : compact;
                }

                var monoBehaviours = go.GetComponents<MonoBehaviour>();
                var sb = new StringBuilder(200);
                for (int i = 0; i < monoBehaviours.Length; i++)
                {
                    var mb = monoBehaviours[i];
                    if (mb == null) continue;
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(mb.GetType().Name);
                }

                DiagnosticLog.Log(
                    "TraceUI",
                    $"sample={_uiTracerSamplesTaken}/{_uiTracerMaxSamples} name={go.name} pos={rect.anchoredPosition} size={rect.sizeDelta} " +
                    $"anchors=({rect.anchorMin.x:F2},{rect.anchorMin.y:F2})-({rect.anchorMax.x:F2},{rect.anchorMax.y:F2}) " +
                    $"image={(img != null)} imageEnabled={(img != null && img.enabled)} sprite={(img != null && img.sprite != null)} " +
                    $"raw={(raw != null)} rawEnabled={(raw != null && raw.enabled)} texture={(raw != null && raw.texture != null)} " +
                    $"tmp={(tmp != null)} text='{textPreview}' mbs=[{sb}]");

                logged++;
                if (logged >= maxLogged) break;
            }

            if (logged == 0)
            {
                DiagnosticLog.Log("TraceUI", $"sample={_uiTracerSamplesTaken}/{_uiTracerMaxSamples} no center/bottom-left candidates");
            }
        }

        private void TraceBottomLeftAcrossCanvases()
        {
            int logged = 0;
            const int maxLogged = 24;

            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas == null || !canvas.isActiveAndEnabled) continue;
                var root = canvas.transform;
                if (root == null) continue;

                var rects = root.GetComponentsInChildren<RectTransform>(true);
                foreach (var rect in rects)
                {
                    if (rect == null || rect == root) continue;
                    var go = rect.gameObject;
                    if (go == null || !go.activeInHierarchy) continue;

                    bool anchoredToBottomLeft = rect.anchorMin.x <= 0.1f && rect.anchorMin.y <= 0.1f
                        && rect.anchorMax.x <= 0.25f && rect.anchorMax.y <= 0.25f;
                    bool nearBottomLeft = rect.anchoredPosition.x <= 520f && rect.anchoredPosition.y <= 520f;
                    bool largeEnough = rect.sizeDelta.x >= 120f && rect.sizeDelta.y >= 60f;
                    if (!(anchoredToBottomLeft && nearBottomLeft && largeEnough)) continue;

                    var img = go.GetComponent<Image>();
                    var raw = go.GetComponent<RawImage>();
                    var tmp = go.GetComponent<TextMeshProUGUI>();
                    var txt = go.GetComponent<Text>();

                    string preview = string.Empty;
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                    {
                        preview = tmp.text.Replace('\n', ' ').Replace('\r', ' ');
                    }
                    else if (txt != null && !string.IsNullOrEmpty(txt.text))
                    {
                        preview = txt.text.Replace('\n', ' ').Replace('\r', ' ');
                    }
                    if (preview.Length > 100) preview = preview.Substring(0, 100);

                    var mbs = go.GetComponents<MonoBehaviour>();
                    var sb = new StringBuilder(220);
                    for (int i = 0; i < mbs.Length; i++)
                    {
                        var mb = mbs[i];
                        if (mb == null) continue;
                        if (sb.Length > 0) sb.Append(",");
                        sb.Append(mb.GetType().Name);
                    }

                    DiagnosticLog.Log(
                        "TraceUIBroad",
                        $"sample={_uiTracerSamplesTaken}/{_uiTracerMaxSamples} canvas={canvas.name} go={go.name} " +
                        $"pos={rect.anchoredPosition} size={rect.sizeDelta} anchors=({rect.anchorMin.x:F2},{rect.anchorMin.y:F2})-({rect.anchorMax.x:F2},{rect.anchorMax.y:F2}) " +
                        $"img={(img != null)} imgOn={(img != null && img.enabled)} raw={(raw != null)} rawOn={(raw != null && raw.enabled)} " +
                        $"tmp={(tmp != null)} text='{preview}' mbs=[{sb}]");

                    logged++;
                    if (logged >= maxLogged) break;
                }

                if (logged >= maxLogged) break;
            }

            if (logged == 0)
            {
                DiagnosticLog.Log("TraceUIBroad", $"sample={_uiTracerSamplesTaken}/{_uiTracerMaxSamples} no bottom-left candidates across canvases");
            }
        }

        private void TraceNoFilterAcrossCanvases()
        {
            int logged = 0;
            const int maxLogged = 40;

            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas == null || !canvas.isActiveAndEnabled) continue;
                var root = canvas.transform;
                if (root == null) continue;

                var rects = root.GetComponentsInChildren<RectTransform>(true);
                foreach (var rect in rects)
                {
                    if (rect == null) continue;
                    var go = rect.gameObject;
                    if (go == null || !go.activeInHierarchy) continue;

                    var tmp = go.GetComponent<TextMeshProUGUI>();
                    var txt = go.GetComponent<Text>();
                    var img = go.GetComponent<Image>();
                    var raw = go.GetComponent<RawImage>();
                    if (tmp == null && txt == null && img == null && raw == null) continue;

                    string preview = string.Empty;
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                    {
                        preview = tmp.text.Replace('\n', ' ').Replace('\r', ' ');
                    }
                    else if (txt != null && !string.IsNullOrEmpty(txt.text))
                    {
                        preview = txt.text.Replace('\n', ' ').Replace('\r', ' ');
                    }
                    if (preview.Length > 100) preview = preview.Substring(0, 100);

                    var mbs = go.GetComponents<MonoBehaviour>();
                    var sb = new StringBuilder(220);
                    for (int i = 0; i < mbs.Length; i++)
                    {
                        var mb = mbs[i];
                        if (mb == null) continue;
                        if (sb.Length > 0) sb.Append(",");
                        sb.Append(mb.GetType().Name);
                    }

                    DiagnosticLog.Log(
                        "TraceUINoFilter",
                        $"sample={_uiTracerSamplesTaken}/{_uiTracerMaxSamples} canvas={canvas.name} go={go.name} " +
                        $"pos={rect.anchoredPosition} size={rect.sizeDelta} anchors=({rect.anchorMin.x:F2},{rect.anchorMin.y:F2})-({rect.anchorMax.x:F2},{rect.anchorMax.y:F2}) " +
                        $"img={(img != null)} imgOn={(img != null && img.enabled)} raw={(raw != null)} rawOn={(raw != null && raw.enabled)} " +
                        $"tmp={(tmp != null)} txt={(txt != null)} text='{preview}' mbs=[{sb}]");

                    logged++;
                    if (logged >= maxLogged) break;
                }

                if (logged >= maxLogged) break;
            }

            if (logged == 0)
            {
                DiagnosticLog.Log("TraceUINoFilter", $"sample={_uiTracerSamplesTaken}/{_uiTracerMaxSamples} no active UI candidates");
            }
        }

        private void RemoveDevelopmentConsolePanel()
        {
            // Remove legacy diagnostics overlays globally, not just this canvas hierarchy.
            string[] debugOverlayNames =
            {
                "DebugDiagnosticsPanel",
                "DiagnosticsText",
                "DiagnosticsTitle",
                "DevelopmentConsolePanel",
                "SensorStatusPanel",
                "SensorText"
            };

            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int removedCount = 0;

            foreach (var obj in allObjects)
            {
                if (obj == null) continue;

                bool isDebugOverlay = false;
                for (int i = 0; i < debugOverlayNames.Length; i++)
                {
                    if (obj.name == debugOverlayNames[i])
                    {
                        isDebugOverlay = true;
                        break;
                    }
                }

                if (!isDebugOverlay) continue;

                obj.SetActive(false);
                Destroy(obj);
                removedCount++;
            }

            if (removedCount > 0)
            {
                DiagnosticLog.Log("Setup", $"Removed development overlays: {removedCount}");
            }

            // Some legacy diagnostics are text-driven and may not use known object names.
            var tmpTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in tmpTexts)
            {
                if (tmp == null) continue;

                string text = tmp.text ?? string.Empty;
                bool looksLikeLegacyDiagnostics =
                    text.Contains("<b>Planes:</b>") ||
                    text.Contains("<b>API:</b>") ||
                    text.Contains("<b>AR:</b> <color=") ||
                    text.Contains("Mock: ");

                if (!looksLikeLegacyDiagnostics) continue;

                var owner = GetDirectCanvasChild(tmp.transform);
                if (owner != null)
                {
                    owner.gameObject.SetActive(false);
                    Destroy(owner.gameObject);
                }
                else
                {
                    tmp.gameObject.SetActive(false);
                    Destroy(tmp.gameObject);
                }
            }

            // Legacy overlays may use UnityEngine.UI.Text instead of TMP.
            var legacyTexts = FindObjectsByType<Text>(FindObjectsSortMode.None);
            foreach (var txt in legacyTexts)
            {
                if (txt == null) continue;
                string text = txt.text ?? string.Empty;
                bool looksLikeLegacyDiagnostics =
                    text.Contains("AR:") ||
                    text.Contains("Planes:") ||
                    text.Contains("API:") ||
                    text.Contains("Mock:");
                if (!looksLikeLegacyDiagnostics) continue;

                var owner = GetDirectCanvasChild(txt.transform);
                if (owner != null)
                {
                    owner.gameObject.SetActive(false);
                    Destroy(owner.gameObject);
                }
                else
                {
                    txt.gameObject.SetActive(false);
                    Destroy(txt.gameObject);
                }
            }
        }

        private Transform GetDirectCanvasChild(Transform t)
        {
            if (t == null) return null;
            Transform current = t;
            while (current != null && current.parent != null)
            {
                if (current.parent == transform) return current;
                current = current.parent;
            }
            return null;
        }

        private bool HasDevelopmentOverlayVisible()
        {
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj == null || !obj.activeInHierarchy) continue;
                string n = obj.name.ToLowerInvariant();
                if (n.Contains("debugdiagnosticspanel") || n.Contains("developmentconsole") || n == "diagnosticstext")
                {
                    return true;
                }
            }

            var tmpTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in tmpTexts)
            {
                if (tmp == null || !tmp.gameObject.activeInHierarchy) continue;
                string text = tmp.text ?? string.Empty;
                if (text.Contains("<b>Planes:</b>") || text.Contains("<b>API:</b>") || text.Contains("<b>AR:</b> <color="))
                {
                    return true;
                }
            }

            var legacyTexts = FindObjectsByType<Text>(FindObjectsSortMode.None);
            foreach (var txt in legacyTexts)
            {
                if (txt == null || !txt.gameObject.activeInHierarchy) continue;
                string text = txt.text ?? string.Empty;
                if (text.Contains("AR:") || text.Contains("Planes:") || text.Contains("API:") || text.Contains("Mock:"))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetupBackButton()
        {
            var btn = transform.Find("BackButton");
            if (btn != null && !btn) btn = null;

            // Rebuild malformed scene leftovers: UI button roots must be RectTransform-based.
            if (btn != null && btn.GetComponent<RectTransform>() == null)
            {
                DiagnosticLog.Warn("Setup", "BackButton exists without RectTransform - rebuilding");
                Destroy(btn.gameObject);
                btn = null;
            }

            if (btn == null)
            {
                // Create BackButton from code for fully code-based setup.
                var btnGO = new GameObject(
                    "BackButton",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );
                btnGO.transform.SetParent(transform, false);
                btn = btnGO.transform;
                DiagnosticLog.Log("Setup", "Created BackButton from code");
            }

            var rect = btn.GetComponent<RectTransform>();
            if (rect == null)
            {
                DiagnosticLog.Error("Setup", "SetupBackButton aborted: RectTransform missing after create");
                return;
            }
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(30, -50);
            rect.sizeDelta = new Vector2(120, 60);

            var image = btn.GetComponent<Image>();
            if (image == null) image = btn.gameObject.AddComponent<Image>();
            if (image != null)
            {
                image.color = SemiTransparentBlack;
            }

            // Add text
            var textTransform = btn.Find("Text");
            if (textTransform == null)
            {
                var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(btn, false);
                textTransform = textGO.transform;
            }
            else if (textTransform.GetComponent<RectTransform>() == null)
            {
                // Replace malformed text child rather than mutating non-UI transforms.
                Destroy(textTransform.gameObject);
                var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(btn, false);
                textTransform = textGO.transform;
            }

            var textRect = textTransform.GetComponent<RectTransform>();
            if (textRect == null)
            {
                DiagnosticLog.Error("Setup", "SetupBackButton aborted: BackButton text RectTransform missing");
                return;
            }
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmpText = textTransform.GetComponent<TextMeshProUGUI>();
            if (tmpText == null) tmpText = textTransform.gameObject.AddComponent<TextMeshProUGUI>();
            if (tmpText == null)
            {
                DiagnosticLog.Error("Setup", "SetupBackButton aborted: TextMeshProUGUI missing on BackButton text");
                return;
            }
            tmpText.text = "< Back";
            tmpText.fontSize = 24;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;

            // Wire Back button to exit AR and return to MainMenu
            var button = btn.GetComponent<Button>();
            if (button == null) button = btn.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                DiagnosticLog.Log("BackButton", "Tapped - exiting AR");
                if (Core.UIManager.Instance != null)
                    Core.UIManager.Instance.ExitARHunt();
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            });
            DiagnosticLog.Log("Setup", "BackButton wired");
        }

        /// <summary>
        /// CODE-ONLY setup for crosshairs and collection size ring.
        /// Loads sprites from Resources/UI (crosshairs.jpg, gold ring.png).
        /// No Unity Editor wiring required - everything built at runtime.
        /// </summary>
        private void SetupCrosshairs()
        {
            // Find or create Crosshairs container
            var crosshairs = transform.Find("Crosshairs");
            if (crosshairs == null)
            {
                crosshairs = new GameObject("Crosshairs").transform;
                crosshairs.SetParent(transform, false);
                crosshairs.gameObject.AddComponent<RectTransform>();
                crosshairs.gameObject.AddComponent<CrosshairsController>();
                Debug.Log("[ARHuntSceneSetup] Created Crosshairs from code");
            }

            var rect = crosshairs.GetComponent<RectTransform>();
            if (rect == null)
            {
                Debug.LogError("[ARHuntSceneSetup] Crosshairs has no RectTransform!");
                return;
            }

            // Center of screen
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(120, 120);

            // Load crosshairs sprite from Resources/UI
            // Expected: Assets/Resources/UI/crosshairs.jpg and Assets/Resources/UI/gold ring.png
            var crosshairsSprite = Resources.Load<Sprite>("UI/crosshairs");
            if (crosshairsSprite == null) crosshairsSprite = Resources.Load<Sprite>("crosshairs");
            if (crosshairsSprite == null)
            {
                Debug.LogWarning("[ARHuntSceneSetup] crosshairs.jpg not in Resources/UI - using programmatic fallback");
            }

            // Main crosshairs Image - REMOVED: Crosshairs cover the coin - we want players to see the beautiful coin!
            // Gold ring (CollectionSizeCircle) still shows when in range - that stays visible.
            var image = crosshairs.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = false;  // Hide immediately (prevents white square from broken/missing sprite)
                Destroy(image);
                image = null;
            }

            // Extra cleanup for legacy scene-authored children that can appear as a centered square.
            var legacyCrosshairImage = crosshairs.Find("CrosshairImage");
            if (legacyCrosshairImage != null)
            {
                var legacyImage = legacyCrosshairImage.GetComponent<Image>();
                if (legacyImage != null)
                {
                    legacyImage.enabled = false;
                }
                legacyCrosshairImage.gameObject.SetActive(false);
            }
            // Don't add Image - we intentionally have no crosshairs visual

            // Remove old CrosshairText (font doesn't support ⊕) - we use sprite now
            var oldText = crosshairs.Find("CrosshairText");
            if (oldText != null)
            {
                Destroy(oldText.gameObject);
            }

            // Create CollectionSizeCircle (gold ring) - shows when targeting coin
            var collectionCircle = crosshairs.Find("CollectionSizeCircle");
            if (collectionCircle == null)
            {
                var circleGO = new GameObject("CollectionSizeCircle");
                circleGO.transform.SetParent(crosshairs, false);
                collectionCircle = circleGO.transform;

                var circleRect = circleGO.AddComponent<RectTransform>();
                circleRect.anchorMin = new Vector2(0.5f, 0.5f);
                circleRect.anchorMax = new Vector2(0.5f, 0.5f);
                circleRect.pivot = new Vector2(0.5f, 0.5f);
                circleRect.anchoredPosition = Vector2.zero;
                circleRect.sizeDelta = new Vector2(80, 80);

                var circleImage = circleGO.AddComponent<Image>();
                var goldRingSprite = Resources.Load<Sprite>("UI/gold ring");
                if (goldRingSprite == null) goldRingSprite = Resources.Load<Sprite>("gold ring");
                circleImage.sprite = goldRingSprite;
                circleImage.color = new Color(1f, 0.84f, 0f, 0.7f);
                circleImage.raycastTarget = false;
                circleImage.preserveAspect = true;
                if (circleImage.sprite == null)
                {
                    circleImage.enabled = false;
                    DiagnosticLog.Warn("Setup", "CollectionSizeCircle sprite missing - disabled to prevent square artifact");
                }
                Debug.Log("[ARHuntSceneSetup] Created CollectionSizeCircle from code");
            }
            else
            {
                // Ensure existing circle has sprite and settings
                var circleImage = collectionCircle.GetComponent<Image>();
                if (circleImage != null)
                {
                    if (circleImage.sprite == null)
                    {
                        var goldRingSprite = Resources.Load<Sprite>("UI/gold ring");
                        if (goldRingSprite == null) goldRingSprite = Resources.Load<Sprite>("gold ring");
                        circleImage.sprite = goldRingSprite;
                    }
                    circleImage.raycastTarget = false;
                    if (circleImage.sprite == null)
                    {
                        circleImage.enabled = false;
                        DiagnosticLog.Warn("Setup", "CollectionSizeCircle sprite still missing - disabled to prevent square artifact");
                    }
                }
            }

            // Wire CrosshairsController references at runtime
            var controller = crosshairs.GetComponent<CrosshairsController>();
            if (controller != null)
            {
                var circleImg = collectionCircle.GetComponent<Image>();
                controller.SetRuntimeReferences(image, circleImg);
            }
        }
        
        /// <summary>
        /// Setup RadarPanel for click detection.
        /// This ensures the radar can be tapped to open the full map.
        /// </summary>
        private void SetupRadarPanel()
        {
            DiagnosticLog.Log("Setup", "Setting up RadarPanel...");
            
            var radar = transform.Find("RadarPanel");
            if (radar == null)
            {
                // Create RadarPanel from code for fully code-based setup
                var radarGO = new GameObject("RadarPanel");
                radarGO.transform.SetParent(transform, false);
                radar = radarGO.transform;
                radarGO.AddComponent<RectTransform>();
                radarGO.AddComponent<RadarUI>();
                DiagnosticLog.Log("Setup", "Created RadarPanel from code");
            }
            else
            {
                DiagnosticLog.Log("Setup", $"Found RadarPanel: {radar.name}");
            }
            
            // Get or add RectTransform
            var rect = radar.GetComponent<RectTransform>();
            if (rect != null)
            {
                float radarSize = _radarBaseSize * _miniMapUiScale;
                // Position in top-right corner with safe margin (below status bar)
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-20, -20);
                rect.sizeDelta = new Vector2(radarSize, radarSize);
                Debug.Log($"[ARHuntSceneSetup] RadarPanel positioned: anchor TR, pos (-20, -20), size {radarSize}x{radarSize}");
            }
            
            // CRITICAL: Ensure there's an Image with raycastTarget = true
            var image = radar.GetComponent<Image>();
            if (image == null)
            {
                image = radar.gameObject.AddComponent<Image>();
                Debug.Log("[ARHuntSceneSetup] Added Image to RadarPanel");
            }
            image.raycastTarget = true;
            image.color = new Color(1f, 1f, 1f, 0.01f); // Nearly invisible so map tile shows through
            Debug.Log($"[ARHuntSceneSetup] RadarPanel Image raycastTarget: {image.raycastTarget}");
            
            // CRITICAL: Ensure there's a Button component
            var button = radar.GetComponent<Button>();
            if (button == null)
            {
                button = radar.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                Debug.Log("[ARHuntSceneSetup] Added Button to RadarPanel");
            }
            
            // Radar click ownership lives in RadarUI.SetupRadarTap() to avoid duplicate handlers.
            button.onClick.RemoveAllListeners();
            
            // Wire RadarUI and create radar content (player dot, coin sprite) - code-only
            var radarUI = radar.GetComponent<RadarUI>();
            if (radarUI != null)
            {
                // Keep the primary radar reference even if optional zoom controls fail.
                _radarZoomRadarUI = radarUI;
                SetupRadarContent(radar, radarUI);
                radarUI.SetMiniMapScale(_miniMapUiScale);
                radarUI.SetOrientationMode(RadarUI.MiniMapOrientationMode.NorthUp);
                SetupRadarZoomControls(radarUI);
                radar.gameObject.SetActive(true);
                radarUI.Show();
                Debug.Log("[ARHuntSceneSetup] RadarUI wired with code-based setup");
                DiagnosticLog.Log("Radar", $"RadarPanel ready size={rect?.sizeDelta} scale={_miniMapUiScale} zoom={_radarZoom}");
            }
            else
            {
                Debug.LogWarning("[ARHuntSceneSetup] RadarUI component NOT found on RadarPanel!");
                DiagnosticLog.Warn("Radar", "RadarUI missing on RadarPanel after setup");
            }
        }

        /// <summary>
        /// Create radar content (map tile, player dot, sweep line) and wire RadarUI at runtime.
        /// Uses player.png and map-coin-icon.png from Resources/UI.
        /// Map tile from Mapbox shows real streets behind the radar overlay.
        /// </summary>
        private void SetupRadarContent(Transform radar, RadarUI radarUI)
        {
            var rect = radar.GetComponent<RectTransform>();
            if (rect == null)
            {
                Debug.LogError("[ARHuntSceneSetup] Radar has no RectTransform!");
                return;
            }

            RectTransform playerRect = null;
            RectTransform sweepRect = null;
            RectTransform northRect = null;
            float localScale = _miniMapUiScale;

            // === MAP TILE (Mapbox) - First child, behind everything ===
            var mapTile = radar.Find("MapTile");
            RawImage mapTileImage = null;
            if (mapTile == null || !mapTile.gameObject)
            {
                var mapTileGO = new GameObject("MapTile");
                mapTileGO.transform.SetParent(radar, false);
                mapTileGO.transform.SetAsFirstSibling(); // Behind player, sweep, north
                var mapTileRect = mapTileGO.AddComponent<RectTransform>();
                mapTileRect.anchorMin = Vector2.zero;
                mapTileRect.anchorMax = Vector2.one;
                mapTileRect.offsetMin = Vector2.zero;
                mapTileRect.offsetMax = Vector2.zero;
                mapTileImage = mapTileGO.AddComponent<RawImage>();
                mapTileImage.color = new Color(0.15f, 0.2f, 0.25f, 0.95f); // Dark placeholder while loading
                mapTileImage.raycastTarget = false;
                Debug.Log("[ARHuntSceneSetup] Created MapTile RawImage for radar");
            }
            else
            {
                mapTileImage = mapTile.GetComponent<RawImage>();
                if (mapTileImage == null) mapTileImage = mapTile.gameObject.AddComponent<RawImage>();
            }

            _radarMapTileImage = mapTileImage;
            EnsureMapboxService();
            Debug.Log("[ARHuntSceneSetup] Map tile wired - will fetch from Mapbox");

            // Player dot at center
            var playerDot = radar.Find("PlayerDot");
            if (playerDot == null || !playerDot.gameObject)
            {
                var playerGO = new GameObject("PlayerDot");
                playerGO.transform.SetParent(radar, false);
                playerRect = playerGO.AddComponent<RectTransform>();
                playerRect.anchorMin = new Vector2(0.5f, 0.5f);
                playerRect.anchorMax = new Vector2(0.5f, 0.5f);
                playerRect.pivot = new Vector2(0.5f, 0.5f);
                playerRect.anchoredPosition = Vector2.zero;
                playerRect.sizeDelta = new Vector2(24f * localScale, 24f * localScale);
                var playerImg = playerGO.AddComponent<Image>();
                var playerSprite = Resources.Load<Sprite>("UI/player");
                if (playerSprite == null) playerSprite = Resources.Load<Sprite>("player");
                playerImg.sprite = playerSprite;
                playerImg.color = Color.white;
                playerImg.raycastTarget = false;
                playerImg.preserveAspect = true;
            }
            else
            {
                playerRect = playerDot.GetComponent<RectTransform>();
                if (playerRect == null) playerRect = playerDot.gameObject.AddComponent<RectTransform>();
            }
            playerRect.sizeDelta = new Vector2(24f * localScale, 24f * localScale);

            // Sweep line (optional - thin rotating line)
            var sweepLine = radar.Find("SweepLine");
            if (sweepLine == null || !sweepLine.gameObject)
            {
                var sweepGO = new GameObject("SweepLine");
                sweepGO.transform.SetParent(radar, false);
                sweepRect = sweepGO.AddComponent<RectTransform>();
                sweepRect.anchorMin = new Vector2(0.5f, 0.5f);
                sweepRect.anchorMax = new Vector2(0.5f, 0.5f);
                sweepRect.pivot = new Vector2(0.5f, 0f);
                sweepRect.anchoredPosition = Vector2.zero;
                sweepRect.sizeDelta = new Vector2(4f * localScale, 70f * localScale);
                var sweepImg = sweepGO.AddComponent<Image>();
                sweepImg.color = new Color(1f, 0.84f, 0f, 0.4f);
                sweepImg.raycastTarget = false;
            }
            else
            {
                sweepRect = sweepLine.GetComponent<RectTransform>();
                if (sweepRect == null) sweepRect = sweepLine.gameObject.AddComponent<RectTransform>();
            }
            sweepRect.sizeDelta = new Vector2(4f * localScale, 70f * localScale);

            // North indicator (optional)
            var northIndicator = radar.Find("NorthIndicator");
            if (northIndicator == null || !northIndicator.gameObject)
            {
                var northGO = new GameObject("NorthIndicator");
                northGO.transform.SetParent(radar, false);
                northRect = northGO.AddComponent<RectTransform>();
                northRect.anchorMin = new Vector2(0.5f, 1f);
                northRect.anchorMax = new Vector2(0.5f, 1f);
                northRect.pivot = new Vector2(0.5f, 1f);
                northRect.anchoredPosition = new Vector2(0, -10f * localScale);
                northRect.sizeDelta = new Vector2(8f * localScale, 12f * localScale);
                var northImg = northGO.AddComponent<Image>();
                northImg.color = new Color(1f, 0.3f, 0.3f, 0.8f);
                northImg.raycastTarget = false;
            }
            else
            {
                northRect = northIndicator.GetComponent<RectTransform>();
                if (northRect == null) northRect = northIndicator.gameObject.AddComponent<RectTransform>();
            }
            northRect.anchoredPosition = new Vector2(0, -10f * localScale);
            northRect.sizeDelta = new Vector2(8f * localScale, 12f * localScale);

            if (playerRect == null)
            {
                Debug.LogError("[ARHuntSceneSetup] Failed to get player dot RectTransform!");
                return;
            }

            var coinSprite = GetMapCoinIconSprite();
            radarUI.SetRuntimeReferences(rect, playerRect, sweepRect, northRect, coinSprite);
            Debug.Log("[ARHuntSceneSetup] Radar content wired successfully");
        }

        private void SetupRadarZoomControls(RadarUI radarUI)
        {
            try
            {
                if (radarUI == null)
                {
                    DiagnosticLog.Warn("Radar", "SetupRadarZoomControls skipped: radarUI null");
                    return;
                }

                var controls = transform.Find("RadarZoomControls");
                if (controls != null && controls.GetComponent<RectTransform>() == null)
                {
                    Destroy(controls.gameObject);
                    controls = null;
                }

                if (controls == null)
                {
                    var controlsGO = new GameObject("RadarZoomControls", typeof(RectTransform));
                    controlsGO.transform.SetParent(transform, false);
                    controls = controlsGO.transform;
                    DiagnosticLog.Log("Radar", "Created RadarZoomControls");
                }

                if (controls == null || controls.gameObject == null)
                {
                    DiagnosticLog.Warn("Radar", "SetupRadarZoomControls recovering from invalid controls transform");
                    var controlsGO = new GameObject("RadarZoomControls", typeof(RectTransform));
                    controlsGO.transform.SetParent(transform, false);
                    controls = controlsGO.transform;
                }

                var controlsRect = controls.GetComponent<RectTransform>();
                if (controlsRect == null)
                {
                    var controlsGO = new GameObject("RadarZoomControls", typeof(RectTransform));
                    controlsGO.transform.SetParent(transform, false);
                    controls = controlsGO.transform;
                    controlsRect = controls.GetComponent<RectTransform>();
                }
                controlsRect.anchorMin = new Vector2(1f, 1f);
                controlsRect.anchorMax = new Vector2(1f, 1f);
                controlsRect.pivot = new Vector2(1f, 1f);
                controlsRect.anchoredPosition = new Vector2(-20f, -(_radarBaseSize * _miniMapUiScale + 40f));
                controlsRect.sizeDelta = new Vector2(520f, 120f);
                controls.SetAsLastSibling();

                var controlsBg = controls.GetComponent<Image>();
                if (controlsBg == null)
                {
                    controlsBg = controls.gameObject.AddComponent<Image>();
                }
                // Keep parent container non-rendering so it never appears as a white center square.
                controlsBg.enabled = false;
                controlsBg.color = new Color(0f, 0f, 0f, 0f);
                controlsBg.raycastTarget = false;

                var minusButton = EnsureRadarZoomButton(controls, "MinusButton", "-", new Vector2(56f, -60f), new Vector2(112f, 112f), 52f);
                var plusButton = EnsureRadarZoomButton(controls, "PlusButton", "+", new Vector2(188f, -60f), new Vector2(112f, 112f), 52f);
                var rangeText = EnsureRadarZoomLabel(controls, "RangeText", new Vector2(328f, -60f));
                var autoButton = EnsureRadarZoomButton(controls, "AutoButton", "AUTO", new Vector2(452f, -60f), new Vector2(140f, 112f), 30f);
                DiagnosticLog.Log("Radar", $"Zoom controls refs: minus={minusButton != null} plus={plusButton != null} range={rangeText != null} auto={autoButton != null}");

                _radarZoomRadarUI = radarUI;
                _radarMinusButton = minusButton;
                _radarPlusButton = plusButton;
                _radarAutoButton = autoButton;
                _radarRangeText = rangeText;
                _lastRadarControlsRefresh = -999f;

                if (minusButton == null || plusButton == null || autoButton == null)
                {
                    DiagnosticLog.Warn("Radar", "SetupRadarZoomControls aborted: required zoom buttons missing");
                    return;
                }

                if (rangeText == null)
                {
                    DiagnosticLog.Warn("Radar", "SetupRadarZoomControls continuing without RangeText label");
                }

                minusButton.onClick.RemoveAllListeners();
                minusButton.onClick.AddListener(() =>
                {
                    float before = radarUI.Range;
                    bool autoBefore = radarUI.AutoZoomEnabled;
                    DiagnosticLog.Log("RadarZoom", "MinusButton pressed");
                    radarUI.ZoomOut();
                    TriggerZoomHaptic();
                    RefreshRadarZoomControlsUi();
                    _radarMapUpdatePending = false;
                    _radarMapLastUpdate = -999f;
                    DiagnosticLog.Log("RadarZoom", $"MinusButton applied range {before:F1}m -> {radarUI.Range:F1}m auto {autoBefore}->{radarUI.AutoZoomEnabled}");
                });

                plusButton.onClick.RemoveAllListeners();
                plusButton.onClick.AddListener(() =>
                {
                    float before = radarUI.Range;
                    bool autoBefore = radarUI.AutoZoomEnabled;
                    DiagnosticLog.Log("RadarZoom", "PlusButton pressed");
                    radarUI.ZoomIn();
                    TriggerZoomHaptic();
                    RefreshRadarZoomControlsUi();
                    _radarMapUpdatePending = false;
                    _radarMapLastUpdate = -999f;
                    DiagnosticLog.Log("RadarZoom", $"PlusButton applied range {before:F1}m -> {radarUI.Range:F1}m auto {autoBefore}->{radarUI.AutoZoomEnabled}");
                });

                autoButton.onClick.RemoveAllListeners();
                autoButton.onClick.AddListener(() =>
                {
                    float before = radarUI.Range;
                    bool autoBefore = radarUI.AutoZoomEnabled;
                    DiagnosticLog.Log("RadarZoom", "AutoButton pressed");
                    radarUI.ToggleAutoZoom();
                    TriggerZoomHaptic();
                    RefreshRadarZoomControlsUi();
                    _radarMapUpdatePending = false;
                    _radarMapLastUpdate = -999f;
                    DiagnosticLog.Log("RadarZoom", $"AutoButton applied range {before:F1}m -> {radarUI.Range:F1}m auto {autoBefore}->{radarUI.AutoZoomEnabled}");
                });

                RefreshRadarZoomControlsUi();
                DiagnosticLog.Log("Radar", "Zoom controls wired");
            }
            catch (System.Exception ex)
            {
                DiagnosticLog.Error("Radar", $"SetupRadarZoomControls exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private Button EnsureRadarZoomButton(Transform parent, string buttonName, string label, Vector2 anchoredPosition, Vector2? sizeOverride = null, float fontSize = 32f)
        {
            try
            {
                if (parent == null || !parent)
                {
                    DiagnosticLog.Warn("Radar", $"EnsureRadarZoomButton failed: parent invalid for {buttonName}");
                    return null;
                }

                var buttonTransform = parent.Find(buttonName);
                if (buttonTransform == null)
                {
                    var buttonGO = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                    buttonGO.transform.SetParent(parent, false);
                    buttonTransform = buttonGO.transform;
                    DiagnosticLog.Log("Radar", $"Created zoom button {buttonName}");
                }

                if (buttonTransform == null || !buttonTransform) return null;

                var rect = buttonTransform.GetComponent<RectTransform>() ?? buttonTransform.gameObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeOverride ?? new Vector2(56f, 56f);

                var image = buttonTransform.GetComponent<Image>() ?? buttonTransform.gameObject.AddComponent<Image>();
                image.color = new Color(0.14f, 0.14f, 0.14f, 0.9f);
                image.raycastTarget = true;

                var button = buttonTransform.GetComponent<Button>() ?? buttonTransform.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;

                var labelTransform = buttonTransform.Find("Text");
                if (labelTransform == null)
                {
                    var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGO.transform.SetParent(buttonTransform, false);
                    labelTransform = textGO.transform;
                }

                if (labelTransform == null || !labelTransform) return button;

                var labelRect = labelTransform.GetComponent<RectTransform>() ?? labelTransform.gameObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var labelText = labelTransform.GetComponent<TextMeshProUGUI>() ?? labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = GoldColor;
                labelText.text = label;
                labelText.fontSize = fontSize;
                labelText.fontStyle = FontStyles.Bold;
                labelText.enableWordWrapping = false;

                // Use a UI Outline effect instead of TMP material outline to avoid runtime material null issues.
                var outline = labelTransform.GetComponent<UnityEngine.UI.Outline>();
                if (outline == null) outline = labelTransform.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = true;

                DiagnosticLog.Log("Radar", $"Prepared zoom button {buttonName} label={label}");
                return button;
            }
            catch (System.Exception ex)
            {
                DiagnosticLog.Error("Radar", $"EnsureRadarZoomButton exception ({buttonName}): {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private TextMeshProUGUI EnsureRadarZoomLabel(Transform parent, string name, Vector2 anchoredPosition)
        {
            try
            {
                if (parent == null || !parent)
                {
                    DiagnosticLog.Warn("Radar", $"EnsureRadarZoomLabel failed: parent invalid for {name}");
                    return null;
                }

                var textTransform = parent.Find(name);
                if (textTransform == null)
                {
                    var textGO = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGO.transform.SetParent(parent, false);
                    textTransform = textGO.transform;
                }

                if (textTransform == null || !textTransform)
                {
                    return null;
                }

                var rect = textTransform.GetComponent<RectTransform>() ?? textTransform.gameObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = new Vector2(110f, 70f);

                var tmpText = textTransform.GetComponent<TextMeshProUGUI>() ?? textTransform.gameObject.AddComponent<TextMeshProUGUI>();
                if (tmpText == null) return null;
                tmpText.fontSize = 28f;
                tmpText.fontStyle = FontStyles.Bold;
                tmpText.color = GoldColor;
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.enableWordWrapping = false;
                tmpText.raycastTarget = false;
                tmpText.text = _radarZoomRadarUI != null ? $"{Mathf.RoundToInt(_radarZoomRadarUI.Range)}m" : "50m";
                DiagnosticLog.Log("Radar", $"Prepared zoom label {name} at {anchoredPosition} size={rect.sizeDelta}");

                return tmpText;
            }
            catch (System.Exception ex)
            {
                DiagnosticLog.Error("Radar", $"EnsureRadarZoomLabel exception ({name}): {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private void RefreshRadarZoomControlsUi()
        {
            if (_radarZoomRadarUI == null) return;

            if (_radarRangeText != null)
            {
                _radarRangeText.text = _radarZoomRadarUI.AutoZoomEnabled
                    ? $"A {Mathf.RoundToInt(_radarZoomRadarUI.Range)}m"
                    : $"{Mathf.RoundToInt(_radarZoomRadarUI.Range)}m";
            }

            if (_radarMinusButton != null)
                _radarMinusButton.interactable = !_radarZoomRadarUI.AutoZoomEnabled;

            if (_radarPlusButton != null)
                _radarPlusButton.interactable = !_radarZoomRadarUI.AutoZoomEnabled;

            if (_radarAutoButton != null)
            {
                var autoImage = _radarAutoButton.GetComponent<Image>();
                if (autoImage != null)
                {
                    autoImage.color = _radarZoomRadarUI.AutoZoomEnabled
                        ? new Color(0.2f, 0.45f, 0.2f, 0.95f)
                        : new Color(0.1f, 0.1f, 0.1f, 0.85f);
                }
            }
        }

        private static void TriggerZoomHaptic()
        {
            if (HapticService.Instance != null)
            {
                HapticService.Instance.Vibrate(50);
            }
        }
        
        /// <summary>
        /// Resolve map-coin-icon from Resources with a Texture2D fallback.
        /// Handles projects where import mode is Texture2D instead of Sprite.
        /// </summary>
        private Sprite GetMapCoinIconSprite()
        {
            if (_cachedMapCoinIconSprite != null)
                return _cachedMapCoinIconSprite;
            
            var sprite = Resources.Load<Sprite>("UI/map-coin-icon") ?? Resources.Load<Sprite>("map-coin-icon");
            if (sprite != null)
            {
                _cachedMapCoinIconSprite = sprite;
                if (!_mapCoinIconLoadLogged)
                {
                    _mapCoinIconLoadLogged = true;
                    Debug.Log($"[ARHuntSceneSetup][MapIcon] Loaded as Sprite: {_cachedMapCoinIconSprite.texture.width}x{_cachedMapCoinIconSprite.texture.height}");
                }
                return _cachedMapCoinIconSprite;
            }
            
            var tex = Resources.Load<Texture2D>("UI/map-coin-icon") ?? Resources.Load<Texture2D>("map-coin-icon");
            if (tex != null)
            {
                _cachedMapCoinIconSprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                if (!_mapCoinIconLoadLogged)
                {
                    _mapCoinIconLoadLogged = true;
                    Debug.Log($"[ARHuntSceneSetup][MapIcon] Loaded via Texture2D fallback: {tex.width}x{tex.height}");
                }
                return _cachedMapCoinIconSprite;
            }
            
            if (!_mapCoinIconLoadLogged)
            {
                _mapCoinIconLoadLogged = true;
                Debug.LogWarning("[ARHuntSceneSetup][MapIcon] map-coin-icon not found in Resources (Sprite/Texture2D)");
            }
            return null;
        }

        /// <summary>
        /// Create a compact AR-only sensor panel in the bottom-left.
        /// This replaces the old development console.
        /// </summary>
        private void SetupSensorStatusPanel()
        {
            try
            {
                Transform panel = transform.Find("SensorStatusPanel");
                if (panel == null || !panel)
                {
                    var panelGO = new GameObject("SensorStatusPanel");
                    panelGO.transform.SetParent(transform, false);
                    panel = panelGO.transform;
                }

                if (panel == null || panel.gameObject == null)
                {
                    DiagnosticLog.Warn("Setup", "SensorStatusPanel setup aborted: panel transform invalid");
                    return;
                }

                var panelRect = panel.GetComponent<RectTransform>() ?? panel.gameObject.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0, 0);
                panelRect.anchorMax = new Vector2(0, 0);
                panelRect.pivot = new Vector2(0, 0);
                panelRect.anchoredPosition = new Vector2(20, 20);
                panelRect.sizeDelta = new Vector2(520, 320);

                var bgImage = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
                bgImage.color = new Color(0, 0, 0, 0.7f);
                bgImage.raycastTarget = false;

                Transform textTransform = panel.Find("SensorText");
                if (textTransform == null || !textTransform)
                {
                    var textGO = new GameObject("SensorText");
                    textGO.transform.SetParent(panel, false);
                    textTransform = textGO.transform;
                }

                if (textTransform == null || textTransform.gameObject == null)
                {
                    DiagnosticLog.Warn("Setup", "SensorStatusPanel setup aborted: SensorText invalid");
                    return;
                }

                var textRect = textTransform.GetComponent<RectTransform>() ?? textTransform.gameObject.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = new Vector2(12, 12);
                textRect.offsetMax = new Vector2(-12, -12);

                _sensorStatusText = textTransform.GetComponent<TextMeshProUGUI>() ?? textTransform.gameObject.AddComponent<TextMeshProUGUI>();
                _sensorStatusText.fontSize = 18;
                _sensorStatusText.color = Color.white;
                _sensorStatusText.alignment = TextAlignmentOptions.TopLeft;
                _sensorStatusText.enableWordWrapping = true;
                _sensorStatusText.richText = true;
                _sensorStatusText.raycastTarget = false;
                _sensorStatusText.text = BuildSensorStatusString();

                panel.gameObject.SetActive(true);
                _sensorStatusPanel = panel;
                panel.SetAsLastSibling();
                DiagnosticLog.Log("Setup", "SensorStatusPanel created");
            }
            catch (System.Exception ex)
            {
                DiagnosticLog.Error("Setup", $"SetupSensorStatusPanel exception: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void EnsureSensorStatusPanelVisible()
        {
            if (_sensorStatusPanel == null || !_sensorStatusPanel)
            {
                _sensorStatusPanel = transform.Find("SensorStatusPanel");
            }

            if (_sensorStatusPanel == null || !_sensorStatusPanel) return;

            if (!_sensorStatusPanel.gameObject.activeSelf)
            {
                _sensorStatusPanel.gameObject.SetActive(true);
                DiagnosticLog.Warn("Setup", "SensorStatusPanel was hidden and has been re-enabled");
            }
        }

        private void EnsureRadarPanelOperational()
        {
            if (_radarZoomRadarUI == null || !_radarZoomRadarUI)
            {
                var radarPanel = transform.Find("RadarPanel");
                if (radarPanel != null)
                {
                    _radarZoomRadarUI = radarPanel.GetComponent<RadarUI>();
                }
            }

            if (_radarZoomRadarUI != null)
            {
                if (!_radarZoomRadarUI.gameObject.activeSelf)
                {
                    _radarZoomRadarUI.gameObject.SetActive(true);
                }
                _radarZoomRadarUI.Show();
                _radarZoomRadarUI.transform.SetAsLastSibling();
            }

            if (_radarMapTileImage == null || !_radarMapTileImage)
            {
                var mapTile = transform.Find("RadarPanel/MapTile");
                if (mapTile != null)
                {
                    _radarMapTileImage = mapTile.GetComponent<RawImage>();
                }
            }

            // If radar references are still incomplete, rebuild runtime wiring.
            if (_radarZoomRadarUI == null || _radarMapTileImage == null)
            {
                SetupRadarPanel();
            }
        }

        /// <summary>
        /// Create MessagePanel for ARHUD.ShowMessage() - code-only, no Inspector wiring.
        /// Center-bottom placement. ARHUD finds via transform.Find("MessagePanel").
        /// </summary>
        private void SetupMessagePanel()
        {
            var existing = transform.Find("MessagePanel");
            if (existing != null) return;

            var panel = new GameObject("MessagePanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 120);
            panelRect.sizeDelta = new Vector2(600, 80);

            var cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            bgImage.raycastTarget = false;

            var textGO = new GameObject("MessageText");
            textGO.transform.SetParent(panel.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 10);
            textRect.offsetMax = new Vector2(-15, -10);
            var tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.text = "";
            tmpText.fontSize = 28;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.enableWordWrapping = true;

            panel.SetActive(true);
            DiagnosticLog.Log("Setup", "MessagePanel created");
        }

        /// <summary>
        /// Create LockedPopup for ARHUD.ShowLockedPopup() - code-only, no Inspector wiring.
        /// Center of screen. ARHUD finds via transform.Find("LockedPopup").
        /// </summary>
        private void SetupLockedPopup()
        {
            var existing = transform.Find("LockedPopup");
            if (existing != null) return;

            var panel = new GameObject("LockedPopup");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 200);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            bgImage.raycastTarget = true;

            var valueGO = new GameObject("LockedValueText");
            valueGO.transform.SetParent(panel.transform, false);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0.7f);
            valueRect.anchorMax = new Vector2(0.5f, 0.7f);
            valueRect.pivot = new Vector2(0.5f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = new Vector2(360, 50);
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "$0.00";
            valueText.fontSize = 36;
            valueText.color = GoldColor;
            valueText.alignment = TextAlignmentOptions.Center;

            var msgGO = new GameObject("LockedMessageText");
            msgGO.transform.SetParent(panel.transform, false);
            var msgRect = msgGO.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.5f, 0.3f);
            msgRect.anchorMax = new Vector2(0.5f, 0.3f);
            msgRect.pivot = new Vector2(0.5f, 0.5f);
            msgRect.anchoredPosition = Vector2.zero;
            msgRect.sizeDelta = new Vector2(360, 80);
            var msgText = msgGO.AddComponent<TextMeshProUGUI>();
            msgText.text = "";
            msgText.fontSize = 24;
            msgText.color = Color.white;
            msgText.alignment = TextAlignmentOptions.Center;
            msgText.enableWordWrapping = true;

            panel.SetActive(false);
            DiagnosticLog.Log("Setup", "LockedPopup created");
        }

        /// <summary>
        /// Create CollectionPopup for ARHUD.ShowCollectionPopup() - code-only, no Inspector wiring.
        /// Center of screen. ARHUD finds via transform.Find("CollectionPopup").
        /// </summary>
        private void SetupCollectionPopup()
        {
            var existing = transform.Find("CollectionPopup");
            if (existing != null) return;

            var panel = new GameObject("CollectionPopup");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(350, 150);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.2f, 0.1f, 0.95f);
            bgImage.raycastTarget = false;

            var valueGO = new GameObject("CollectionValueText");
            valueGO.transform.SetParent(panel.transform, false);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0.65f);
            valueRect.anchorMax = new Vector2(0.5f, 0.65f);
            valueRect.pivot = new Vector2(0.5f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = new Vector2(320, 50);
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "+$0.00";
            valueText.fontSize = 40;
            valueText.color = GoldColor;
            valueText.alignment = TextAlignmentOptions.Center;

            var msgGO = new GameObject("CollectionMessageText");
            msgGO.transform.SetParent(panel.transform, false);
            var msgRect = msgGO.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.5f, 0.3f);
            msgRect.anchorMax = new Vector2(0.5f, 0.3f);
            msgRect.pivot = new Vector2(0.5f, 0.5f);
            msgRect.anchoredPosition = Vector2.zero;
            msgRect.sizeDelta = new Vector2(320, 40);
            var msgText = msgGO.AddComponent<TextMeshProUGUI>();
            msgText.text = "Treasure collected!";
            msgText.fontSize = 24;
            msgText.color = Color.white;
            msgText.alignment = TextAlignmentOptions.Center;

            panel.SetActive(false);
            DiagnosticLog.Log("Setup", "CollectionPopup created");
        }

        /// <summary>
        /// Create CoinInfoPanel for ARHUD.ShowCoinInfo() - code-only, no Inspector wiring.
        /// Top-center, below radar. ARHUD finds via transform.Find("CoinInfoPanel").
        /// </summary>
        private void SetupCoinInfoPanel()
        {
            var existing = transform.Find("CoinInfoPanel");
            if (existing != null) return;

            var panel = new GameObject("CoinInfoPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1);
            panelRect.anchorMax = new Vector2(0.5f, 1);
            panelRect.pivot = new Vector2(0.5f, 1);
            panelRect.anchoredPosition = new Vector2(0, -420);
            panelRect.sizeDelta = new Vector2(320, 100);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.75f);
            bgImage.raycastTarget = false;

            var valueGO = new GameObject("CoinValueText");
            valueGO.transform.SetParent(panel.transform, false);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 0.6f);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.offsetMin = new Vector2(10, 5);
            valueRect.offsetMax = new Vector2(-10, -5);
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "$0.00";
            valueText.fontSize = 32;
            valueText.color = GoldColor;
            valueText.alignment = TextAlignmentOptions.Center;

            var distGO = new GameObject("CoinDistanceText");
            distGO.transform.SetParent(panel.transform, false);
            var distRect = distGO.AddComponent<RectTransform>();
            distRect.anchorMin = new Vector2(0, 0.25f);
            distRect.anchorMax = new Vector2(1, 0.6f);
            distRect.offsetMin = new Vector2(10, 2);
            distRect.offsetMax = new Vector2(-10, -2);
            var distText = distGO.AddComponent<TextMeshProUGUI>();
            distText.text = "0m";
            distText.fontSize = 24;
            distText.color = Color.white;
            distText.alignment = TextAlignmentOptions.Center;

            var statusGO = new GameObject("CoinStatusText");
            statusGO.transform.SetParent(panel.transform, false);
            var statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0);
            statusRect.anchorMax = new Vector2(1, 0.25f);
            statusRect.offsetMin = new Vector2(10, 2);
            statusRect.offsetMax = new Vector2(-10, -2);
            var statusText = statusGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.fontSize = 20;
            statusText.color = Color.white;
            statusText.alignment = TextAlignmentOptions.Center;

            var iconGO = new GameObject("CoinTierIcon");
            iconGO.transform.SetParent(panel.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1, 0.5f);
            iconRect.anchorMax = new Vector2(1, 0.5f);
            iconRect.pivot = new Vector2(1, 0.5f);
            iconRect.anchoredPosition = new Vector2(-10, 0);
            iconRect.sizeDelta = new Vector2(40, 40);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = new Color(1, 1, 1, 0.5f);
            iconImage.raycastTarget = false;

            panel.SetActive(false);
            DiagnosticLog.Log("Setup", "CoinInfoPanel created");
        }

        /// <summary>
        /// Create CompassPanel for ARHUD - code-only. CompassUI shows direction to target coin.
        /// </summary>
        private void SetupCompassPanel()
        {
            var existing = transform.Find("CompassPanel");
            if (existing != null) return;

            var panel = new GameObject("CompassPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1);
            panelRect.anchorMax = new Vector2(0.5f, 1);
            panelRect.pivot = new Vector2(0.5f, 1);
            panelRect.anchoredPosition = new Vector2(0, -120);
            panelRect.sizeDelta = new Vector2(200, 80);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.21f, 0.36f, 0.8f);
            bgImage.raycastTarget = false;

            var arrowGO = new GameObject("ArrowImage");
            arrowGO.transform.SetParent(panel.transform, false);
            var arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-60, 0);
            arrowRect.sizeDelta = new Vector2(40, 40);
            var arrowImg = arrowGO.AddComponent<Image>();
            arrowImg.color = Color.white;
            arrowImg.raycastTarget = false;

            var distGO = new GameObject("DistanceText");
            distGO.transform.SetParent(panel.transform, false);
            var distRect = distGO.AddComponent<RectTransform>();
            distRect.anchorMin = new Vector2(0.5f, 0.5f);
            distRect.anchorMax = new Vector2(0.5f, 0.5f);
            distRect.pivot = new Vector2(0.5f, 0.5f);
            distRect.anchoredPosition = new Vector2(0, 0);
            distRect.sizeDelta = new Vector2(80, 30);
            var distText = distGO.AddComponent<TextMeshProUGUI>();
            distText.text = "0m";
            distText.fontSize = 20;
            distText.color = Color.white;
            distText.alignment = TextAlignmentOptions.Center;

            var dirGO = new GameObject("DirectionText");
            dirGO.transform.SetParent(panel.transform, false);
            var dirRect = dirGO.AddComponent<RectTransform>();
            dirRect.anchorMin = new Vector2(0.5f, 0);
            dirRect.anchorMax = new Vector2(0.5f, 0.5f);
            dirRect.pivot = new Vector2(0.5f, 0.5f);
            dirRect.anchoredPosition = new Vector2(20, 5);
            dirRect.sizeDelta = new Vector2(50, 25);
            var dirText = dirGO.AddComponent<TextMeshProUGUI>();
            dirText.text = "N";
            dirText.fontSize = 18;
            dirText.color = Color.white;
            dirText.alignment = TextAlignmentOptions.Center;

            var valGO = new GameObject("ValueText");
            valGO.transform.SetParent(panel.transform, false);
            var valRect = valGO.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.5f, 0.5f);
            valRect.anchorMax = new Vector2(0.5f, 1);
            valRect.pivot = new Vector2(0.5f, 0.5f);
            valRect.anchoredPosition = new Vector2(20, -5);
            valRect.sizeDelta = new Vector2(80, 25);
            var valText = valGO.AddComponent<TextMeshProUGUI>();
            valText.text = "$0.00";
            valText.fontSize = 18;
            valText.color = GoldColor;
            valText.alignment = TextAlignmentOptions.Center;

            var compassUI = panel.AddComponent<CompassUI>();
            compassUI.SetRuntimeReferences(arrowRect, distText, dirText, valText, panel, bgImage);
            panel.SetActive(false);
            DiagnosticLog.Log("Setup", "CompassPanel created");
        }

        /// <summary>
        /// Create GasMeterPanel for ARHUD - code-only. Vertical gauge showing fuel days.
        /// </summary>
        private void SetupGasMeterPanel()
        {
            var existing = transform.Find("GasMeterPanel");
            if (existing != null) return;

            var panel = new GameObject("GasMeterPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(0, 0.5f);
            panelRect.pivot = new Vector2(0, 0.5f);
            panelRect.anchoredPosition = new Vector2(20, 0);
            panelRect.sizeDelta = new Vector2(60, 120);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            bgImage.raycastTarget = false;

            var fillGO = new GameObject("FillImage");
            fillGO.transform.SetParent(panel.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.offsetMin = new Vector2(4, 4);
            fillRect.offsetMax = new Vector2(-4, -4);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Vertical;
            fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
            fillImg.fillAmount = 1f;
            fillImg.color = new Color(0.29f, 0.87f, 0.5f);
            fillImg.raycastTarget = false;

            var daysGO = new GameObject("DaysText");
            daysGO.transform.SetParent(panel.transform, false);
            var daysRect = daysGO.AddComponent<RectTransform>();
            daysRect.anchorMin = Vector2.zero;
            daysRect.anchorMax = Vector2.one;
            daysRect.offsetMin = Vector2.zero;
            daysRect.offsetMax = Vector2.zero;
            var daysText = daysGO.AddComponent<TextMeshProUGUI>();
            daysText.text = "30d";
            daysText.fontSize = 18;
            daysText.color = Color.white;
            daysText.alignment = TextAlignmentOptions.Center;

            var iconGO = new GameObject("GasIcon");
            iconGO.transform.SetParent(panel.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1);
            iconRect.anchorMax = new Vector2(0.5f, 1);
            iconRect.pivot = new Vector2(0.5f, 1);
            iconRect.anchoredPosition = new Vector2(0, 5);
            iconRect.sizeDelta = new Vector2(24, 24);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.color = new Color(0.29f, 0.87f, 0.5f);
            iconImg.raycastTarget = false;

            var gasMeterUI = panel.AddComponent<GasMeterUI>();
            gasMeterUI.SetRuntimeReferences(fillImg, bgImage, daysText, iconImg, panelRect);
            DiagnosticLog.Log("Setup", "GasMeterPanel created");
        }

        /// <summary>
        /// Create FindLimitPanel for ARHUD - code-only. Shows player's find limit tier.
        /// </summary>
        private void SetupFindLimitPanel()
        {
            var existing = transform.Find("FindLimitPanel");
            if (existing != null) return;

            var panel = new GameObject("FindLimitPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 0.5f);
            panelRect.anchorMax = new Vector2(1, 0.5f);
            panelRect.pivot = new Vector2(1, 0.5f);
            panelRect.anchoredPosition = new Vector2(-20, 0);
            panelRect.sizeDelta = new Vector2(140, 60);

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.8f, 0.5f, 0.2f, 0.2f);
            bgImage.raycastTarget = false;

            var limitGO = new GameObject("LimitText");
            limitGO.transform.SetParent(panel.transform, false);
            var limitRect = limitGO.AddComponent<RectTransform>();
            limitRect.anchorMin = new Vector2(0, 0.5f);
            limitRect.anchorMax = new Vector2(1, 0.5f);
            limitRect.pivot = new Vector2(0.5f, 0.5f);
            limitRect.anchoredPosition = Vector2.zero;
            limitRect.sizeDelta = new Vector2(-50, 30);
            var limitText = limitGO.AddComponent<TextMeshProUGUI>();
            limitText.text = "Find: $1.00";
            limitText.fontSize = 20;
            limitText.color = new Color(0.8f, 0.5f, 0.2f);
            limitText.alignment = TextAlignmentOptions.Center;

            var tierGO = new GameObject("TierText");
            tierGO.transform.SetParent(panel.transform, false);
            var tierRect = tierGO.AddComponent<RectTransform>();
            tierRect.anchorMin = new Vector2(0, 0);
            tierRect.anchorMax = new Vector2(1, 0.5f);
            tierRect.pivot = new Vector2(0.5f, 0.5f);
            tierRect.anchoredPosition = Vector2.zero;
            tierRect.sizeDelta = new Vector2(-50, 25);
            var tierText = tierGO.AddComponent<TextMeshProUGUI>();
            tierText.text = "Cabin Boy";
            tierText.fontSize = 14;
            tierText.color = new Color(0.8f, 0.5f, 0.2f);
            tierText.alignment = TextAlignmentOptions.Center;

            var iconGO = new GameObject("TierIcon");
            iconGO.transform.SetParent(panel.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1, 0.5f);
            iconRect.anchorMax = new Vector2(1, 0.5f);
            iconRect.pivot = new Vector2(1, 0.5f);
            iconRect.anchoredPosition = new Vector2(-5, 0);
            iconRect.sizeDelta = new Vector2(36, 36);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.color = new Color(0.8f, 0.5f, 0.2f);
            iconImg.raycastTarget = false;

            var findLimitUI = panel.AddComponent<FindLimitUI>();
            findLimitUI.SetRuntimeReferences(limitText, tierText, bgImage, iconImg, panelRect);
            DiagnosticLog.Log("Setup", "FindLimitPanel created");
        }

        /// <summary>
        /// Create DirectionIndicatorPanel for ARHUD - code-only. Large arrow pointing to target coin.
        /// </summary>
        private void SetupDirectionIndicatorPanel()
        {
            var existing = transform.Find("DirectionIndicatorPanel");
            if (existing != null)
            {
                // Keep scene-authored panel, but enforce a single controller.
                var legacyArrow = existing.GetComponent<SimpleDirectionArrow>();
                if (legacyArrow != null)
                {
                    legacyArrow.enabled = false;
                    Destroy(legacyArrow);
                    DiagnosticLog.Log("Setup", "Removed legacy SimpleDirectionArrow from DirectionIndicatorPanel");
                }
                
                var panelRectExisting = existing.GetComponent<RectTransform>();
                var bgPanelExisting = existing.GetComponent<Image>();
                var arrowRectExisting = existing.Find("ArrowTransform")?.GetComponent<RectTransform>();
                var distTextExisting = existing.Find("DistanceText")?.GetComponent<TextMeshProUGUI>();
                var valTextExisting = existing.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
                var statTextExisting = existing.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
                var arrowImgExisting = arrowRectExisting != null ? arrowRectExisting.GetComponent<Image>() : null;
                
                var dirIndicatorExisting = existing.GetComponent<CoinDirectionIndicator>();
                if (dirIndicatorExisting == null)
                {
                    dirIndicatorExisting = existing.gameObject.AddComponent<CoinDirectionIndicator>();
                }
                
                if (panelRectExisting != null && bgPanelExisting != null &&
                    arrowRectExisting != null && distTextExisting != null &&
                    valTextExisting != null && statTextExisting != null && arrowImgExisting != null)
                {
                    if (bgPanelExisting.sprite == null)
                    {
                        bgPanelExisting.enabled = false;
                        bgPanelExisting.raycastTarget = false;
                    }
                    if (arrowImgExisting.sprite == null)
                    {
                        arrowImgExisting.sprite = GetOrCreateDirectionArrowSprite();
                        arrowImgExisting.preserveAspect = true;
                    }
                    // Keep the panel active in hierarchy. CoinDirectionIndicator controls visibility via CanvasGroup.
                    existing.gameObject.SetActive(true);
                    dirIndicatorExisting.SetRuntimeReferences(
                        arrowRectExisting,
                        distTextExisting,
                        valTextExisting,
                        statTextExisting,
                        panelRectExisting,
                        bgPanelExisting,
                        arrowImgExisting
                    );
                    DiagnosticLog.Log("Setup", "DirectionIndicatorPanel found and sanitized");
                    return;
                }
                
                DiagnosticLog.Warn("Setup", "Existing DirectionIndicatorPanel missing references - rebuilding");
                Destroy(existing.gameObject);
            }

            var panel = new GameObject("DirectionIndicatorPanel");
            panel.transform.SetParent(transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(200, 120);

            var bgPanel = panel.AddComponent<Image>();
            bgPanel.color = new Color(0, 0, 0, 0.6f);
            bgPanel.raycastTarget = false;

            var arrowGO = new GameObject("ArrowTransform");
            arrowGO.transform.SetParent(panel.transform, false);
            var arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = Vector2.zero;
            arrowRect.sizeDelta = new Vector2(60, 60);
            var arrowImg = arrowGO.AddComponent<Image>();
            arrowImg.sprite = GetOrCreateDirectionArrowSprite();
            arrowImg.preserveAspect = true;
            arrowImg.color = new Color(1f, 0.84f, 0f, 0.9f);
            arrowImg.raycastTarget = false;

            var distGO = new GameObject("DistanceText");
            distGO.transform.SetParent(panel.transform, false);
            var distRect = distGO.AddComponent<RectTransform>();
            distRect.anchorMin = new Vector2(0.5f, 0.7f);
            distRect.anchorMax = new Vector2(0.5f, 0.7f);
            distRect.pivot = new Vector2(0.5f, 0.5f);
            distRect.anchoredPosition = Vector2.zero;
            distRect.sizeDelta = new Vector2(120, 30);
            var distText = distGO.AddComponent<TextMeshProUGUI>();
            distText.text = "47m";
            distText.fontSize = 24;
            distText.color = Color.white;
            distText.alignment = TextAlignmentOptions.Center;

            var valGO = new GameObject("ValueText");
            valGO.transform.SetParent(panel.transform, false);
            var valRect = valGO.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.5f, 0.5f);
            valRect.anchorMax = new Vector2(0.5f, 0.5f);
            valRect.pivot = new Vector2(0.5f, 0.5f);
            valRect.anchoredPosition = Vector2.zero;
            valRect.sizeDelta = new Vector2(120, 25);
            var valText = valGO.AddComponent<TextMeshProUGUI>();
            valText.text = "$5.00";
            valText.fontSize = 20;
            valText.color = GoldColor;
            valText.alignment = TextAlignmentOptions.Center;

            var statGO = new GameObject("StatusText");
            statGO.transform.SetParent(panel.transform, false);
            var statRect = statGO.AddComponent<RectTransform>();
            statRect.anchorMin = new Vector2(0.5f, 0.25f);
            statRect.anchorMax = new Vector2(0.5f, 0.25f);
            statRect.pivot = new Vector2(0.5f, 0.5f);
            statRect.anchoredPosition = Vector2.zero;
            statRect.sizeDelta = new Vector2(180, 30);
            var statText = statGO.AddComponent<TextMeshProUGUI>();
            statText.text = "Walk toward the treasure!";
            statText.fontSize = 18;
            statText.color = Color.white;
            statText.alignment = TextAlignmentOptions.Center;
            statText.enableWordWrapping = true;

            var dirIndicator = panel.AddComponent<CoinDirectionIndicator>();
            dirIndicator.SetRuntimeReferences(arrowRect, distText, valText, statText, panelRect, bgPanel, arrowImg);
            // Keep active so lifecycle/event subscriptions run. Visibility is handled by CanvasGroup alpha.
            panel.SetActive(true);
            dirIndicator.Hide();
            DiagnosticLog.Log("Setup", "DirectionIndicatorPanel created");
        }

        private Sprite GetOrCreateDirectionArrowSprite()
        {
            if (_directionArrowSprite != null)
                return _directionArrowSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color32(0, 0, 0, 0);
            var fill = new Color32(255, 255, 255, 255);
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // Draw a simple upward-pointing triangle on transparent background.
            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                int halfWidth = Mathf.RoundToInt(Mathf.Lerp(size / 2f, 2f, t));
                int centerX = size / 2;
                int minX = Mathf.Clamp(centerX - halfWidth, 0, size - 1);
                int maxX = Mathf.Clamp(centerX + halfWidth, 0, size - 1);
                for (int x = minX; x <= maxX; x++)
                {
                    pixels[y * size + x] = fill;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            _directionArrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.1f), 100f);
            DiagnosticLog.Log("Setup", "Generated runtime direction arrow sprite");
            return _directionArrowSprite;
        }
        
        /// <summary>
        /// Fetch and apply Mapbox map tile to radar. Updates when location changes.
        /// </summary>
        private void UpdateRadarMapTile()
        {
            if (_radarMapTileImage == null) return;
            if (!MapboxService.Exists) return;
            if (GPSManager.Instance == null || !GPSManager.Instance.IsTracking) return;

            var loc = GPSManager.Instance.CurrentLocation;
            if (loc == null) return;

            float timeSince = Time.time - _radarMapLastUpdate;
            double latDiff = System.Math.Abs(loc.latitude - _radarMapLastLat);
            double lngDiff = System.Math.Abs(loc.longitude - _radarMapLastLng);
            int desiredZoom = ComputeMiniMapTileZoom();
            bool zoomChanged = desiredZoom != _radarZoom;
            if (zoomChanged)
            {
                _radarZoom = desiredZoom;
            }

            bool needsUpdate = timeSince >= 2f &&
                (latDiff > 0.0001 || lngDiff > 0.0001 || zoomChanged);

            if (needsUpdate && !_radarMapUpdatePending)
            {
                _radarMapUpdatePending = true;
                _radarMapLastUpdate = Time.time;
                _radarMapLastLat = loc.latitude;
                _radarMapLastLng = loc.longitude;

                if (_lastLoggedRadarTileZoom != _radarZoom || zoomChanged)
                {
                    _lastLoggedRadarTileZoom = _radarZoom;
                    float range = _radarZoomRadarUI != null ? _radarZoomRadarUI.Range : -1f;
                    bool auto = _radarZoomRadarUI != null && _radarZoomRadarUI.AutoZoomEnabled;
                    DiagnosticLog.Log("RadarTileZoom", $"Request tile zoom={_radarZoom} from range={range:F1}m auto={auto} lat={loc.latitude:F6} lng={loc.longitude:F6}");
                }

                MapboxService.Instance.GetMiniMapTile(loc.latitude, loc.longitude, _radarZoom, 0f, OnRadarMapTileReceived);
            }
        }

        private int ComputeMiniMapTileZoom()
        {
            // Match map tile zoom to radar range so + / - / AUTO visibly change the mini-map image scale.
            float range = _radarZoomRadarUI != null ? _radarZoomRadarUI.Range : 50f;

            if (range <= 35f) return 21;
            if (range <= 50f) return 20;
            if (range <= 80f) return 19;
            if (range <= 120f) return 18;
            if (range <= 180f) return 17;
            return 16;
        }

        private void OnRadarMapTileReceived(Texture2D texture)
        {
            if (texture == null || _radarMapTileImage == null)
            {
                _radarMapUpdatePending = false;
                return;
            }
            StartCoroutine(ApplyRadarMapTileNextFrame(texture));
        }

        private IEnumerator ApplyRadarMapTileNextFrame(Texture2D texture)
        {
            yield return null;
            if (texture == null || _radarMapTileImage == null)
            {
                _radarMapUpdatePending = false;
                yield break;
            }
            bool useCopy = Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
            Texture2D displayTex = useCopy ? EnsureRadarTextureForUI(texture) : texture;
            if (displayTex == null)
            {
                _radarMapUpdatePending = false;
                yield break;
            }
            if (_radarMapCurrentTile != null && _radarMapTileIsOurCopy)
                Destroy(_radarMapCurrentTile);
            _radarMapCurrentTile = displayTex;
            _radarMapTileIsOurCopy = useCopy;
            _radarMapTileImage.texture = _radarMapCurrentTile;
            _radarMapTileImage.enabled = true;
            _radarMapTileImage.color = Color.white;
            Canvas.ForceUpdateCanvases();
            _radarMapUpdatePending = false;
            Debug.Log("[ARHuntSceneSetup] Map tile applied to radar");
        }

        private static Texture2D EnsureRadarTextureForUI(Texture2D source)
        {
            if (source == null) return null;
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, rt);
                var copy = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
                copy.filterMode = FilterMode.Bilinear;
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply(false, false);
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                return copy;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ARHuntSceneSetup] EnsureRadarTextureForUI failed: {e.Message}");
                return source;
            }
#else
            return source;
#endif
        }

        private static void EnsureMapboxService()
        {
            if (!MapboxService.Exists)
            {
                var go = new GameObject("MapboxService");
                go.AddComponent<MapboxService>();
                DontDestroyOnLoad(go);
                Debug.Log("[ARHuntSceneSetup] Created MapboxService");
            }
        }

        /// <summary>
        /// Setup Niantic Lightship for Pokemon GO-style AR features.
        /// Enables occlusion, meshing, semantics, and depth.
        /// </summary>
        private void SetupLightship()
        {
            Debug.Log("[ARHuntSceneSetup] Setting up Niantic Lightship (Pokemon GO technology)...");
            
            // Check if LightshipManager already exists
            var existing = FindFirstObjectByType<BlackBartsGold.AR.LightshipManager>();
            if (existing != null)
            {
                Debug.Log("[ARHuntSceneSetup] LightshipManager already exists");
                return;
            }
            
            // Create LightshipManager
            var lightshipGO = new GameObject("LightshipManager");
            lightshipGO.AddComponent<BlackBartsGold.AR.LightshipManager>();
            
            Debug.Log("[ARHuntSceneSetup] LightshipManager created - Pokemon GO features enabled!");
            Debug.Log("  - Occlusion: Coins hide behind real objects");
            Debug.Log("  - Meshing: Coins sit on real surfaces");
            Debug.Log("  - Semantics: Sky/ground detection");
            Debug.Log("  - Depth: Better AR placement");
        }
        
        // EventSystem ownership is centralized in AppBootstrap.
    }
}
