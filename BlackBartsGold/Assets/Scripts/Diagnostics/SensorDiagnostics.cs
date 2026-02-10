// ============================================================================
// SensorDiagnostics.cs
// Black Bart's Gold - Comprehensive Sensor and AR Diagnostics
// Path: Assets/Scripts/Diagnostics/SensorDiagnostics.cs
// ============================================================================
// Logs ALL sensor states, AR states, and timing information to help debug
// AR positioning issues. Add this to your AR scene for detailed diagnostics.
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Text;
using BlackBartsGold.Location;
using BlackBartsGold.AR;

namespace BlackBartsGold.Diagnostics
{
    /// <summary>
    /// Comprehensive sensor diagnostics for debugging AR issues.
    /// Logs all sensor states, timing, and AR session information.
    /// </summary>
    public class SensorDiagnostics : MonoBehaviour
    {
        [Header("Logging Settings")]
        [SerializeField] private float logInterval = 2f; // Log every N seconds
        [SerializeField] private bool logGPS = true;
        [SerializeField] private bool logCompass = true;
        [SerializeField] private bool logGyroscope = true;
        [SerializeField] private bool logAccelerometer = true;
        [SerializeField] private bool logARState = true;
        [SerializeField] private bool logCamera = true;
        [SerializeField] private bool logCoinState = true;
        
        // Timing
        private float appStartTime;
        private float lastLogTime;
        private float gpsFirstFixTime = -1f;
        private float compassFirstReadTime = -1f;
        private float gyroFirstReadTime = -1f;
        private float arTrackingStartTime = -1f;
        
        // Previous values for change detection
        private Vector3 lastAccel;
        private float lastCompassHeading;
        private Quaternion lastGyroAttitude;
        private Vector3 lastCameraPos;
        private string lastARState;
        
        // State tracking
        private bool gpsEverReady = false;
        private bool compassEverWorked = false;
        private bool gyroEverWorked = false;
        private bool arEverTracked = false;
        private bool newAttitudeEverWorked = false;
        private bool newAccelEverWorked = false;
        private float newAttitudeFirstTime = -1f;
        private float newAccelFirstTime = -1f;
        
        // NEW Input System sensors
        private AttitudeSensor attitudeSensor;
        private Accelerometer accelSensor;
        private GravitySensor gravitySensor;
        private UnityEngine.InputSystem.Gyroscope gyroSensor;
        
        private void Awake()
        {
            appStartTime = Time.realtimeSinceStartup;
            Debug.Log($"[DIAG] ====== SENSOR DIAGNOSTICS STARTED at {DateTime.Now:HH:mm:ss.fff} ======");
            Debug.Log($"[DIAG] Device: {SystemInfo.deviceModel}, OS: {SystemInfo.operatingSystem}");
            Debug.Log($"[DIAG] Supports Gyro: {SystemInfo.supportsGyroscope}, Supports Accel: {SystemInfo.supportsAccelerometer}");
            Debug.Log($"[DIAG] Supports Location: {SystemInfo.supportsLocationService}");
        }
        
        private void Start()
        {
            LogTimestamp("SensorDiagnostics.Start()");
            
            // Initialize DeviceCompass (centralized New Input System compass)
            DeviceCompass.Initialize();
            Debug.Log($"[DIAG] DeviceCompass initialized: Available={DeviceCompass.IsAvailable}, Method={DeviceCompass.ActiveMethod}");
            
            // ================================================================
            // ENABLE NEW INPUT SYSTEM SENSORS
            // ================================================================
            Debug.Log($"[DIAG] Enabling NEW Input System sensors...");
            
            attitudeSensor = AttitudeSensor.current;
            if (attitudeSensor != null)
            {
                InputSystem.EnableDevice(attitudeSensor);
                Debug.Log($"[DIAG] AttitudeSensor: ENABLED={attitudeSensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[DIAG] AttitudeSensor: NOT AVAILABLE");
            }
            
            accelSensor = Accelerometer.current;
            if (accelSensor != null)
            {
                InputSystem.EnableDevice(accelSensor);
                Debug.Log($"[DIAG] Accelerometer: ENABLED={accelSensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[DIAG] Accelerometer: NOT AVAILABLE");
            }
            
            gravitySensor = GravitySensor.current;
            if (gravitySensor != null)
            {
                InputSystem.EnableDevice(gravitySensor);
                Debug.Log($"[DIAG] GravitySensor: ENABLED={gravitySensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[DIAG] GravitySensor: NOT AVAILABLE");
            }
            
            gyroSensor = UnityEngine.InputSystem.Gyroscope.current;
            if (gyroSensor != null)
            {
                InputSystem.EnableDevice(gyroSensor);
                Debug.Log($"[DIAG] Gyroscope(NEW): ENABLED={gyroSensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[DIAG] Gyroscope(NEW): NOT AVAILABLE");
            }
            
            // Initial state dump
            LogAllSensorStates("INITIAL STATE");
            
            // Start periodic logging
            InvokeRepeating(nameof(PeriodicDiagnosticLog), 1f, logInterval);
        }
        
        private void Update()
        {
            // Track first-time events
            TrackFirstTimeEvents();
        }
        
        private void TrackFirstTimeEvents()
        {
            float elapsed = Time.realtimeSinceStartup - appStartTime;
            
            // GPS first fix
            if (!gpsEverReady && GPSManager.Exists && GPSManager.Instance.IsTracking)
            {
                var loc = GPSManager.Instance.CurrentLocation;
                if (loc != null && loc.horizontalAccuracy < 100f)
                {
                    gpsEverReady = true;
                    gpsFirstFixTime = elapsed;
                    Debug.Log($"[DIAG] *** GPS FIRST FIX at T+{elapsed:F2}s! Accuracy: {loc.horizontalAccuracy:F1}m, Lat: {loc.latitude:F6}, Lng: {loc.longitude:F6}");
                }
            }
            
            // NEW Input System - AttitudeSensor first read
            if (!newAttitudeEverWorked && attitudeSensor != null && attitudeSensor.enabled)
            {
                Quaternion att = attitudeSensor.attitude.ReadValue();
                if (att.x != 0 || att.y != 0 || att.z != 0)
                {
                    newAttitudeEverWorked = true;
                    newAttitudeFirstTime = elapsed;
                    Debug.Log($"[DIAG] *** NEW ATTITUDE SENSOR FIRST READ at T+{elapsed:F2}s! Attitude: {att.eulerAngles}");
                }
            }
            
            // NEW Input System - Accelerometer first read
            if (!newAccelEverWorked && accelSensor != null && accelSensor.enabled)
            {
                Vector3 acc = accelSensor.acceleration.ReadValue();
                if (acc.sqrMagnitude > 0.01f)
                {
                    newAccelEverWorked = true;
                    newAccelFirstTime = elapsed;
                    Debug.Log($"[DIAG] *** NEW ACCELEROMETER FIRST READ at T+{elapsed:F2}s! Accel: {acc}");
                }
            }
            
            // LEGACY Compass first read
            if (!compassEverWorked && Input.compass.enabled)
            {
                float heading = Input.compass.trueHeading;
                if (heading != 0 && !float.IsNaN(heading))
                {
                    compassEverWorked = true;
                    compassFirstReadTime = elapsed;
                    Debug.Log($"[DIAG] *** LEGACY COMPASS FIRST READ at T+{elapsed:F2}s! Heading: {heading:F1}°");
                }
            }
            
            // LEGACY Gyro first read
            if (!gyroEverWorked && Input.gyro.enabled)
            {
                Quaternion att = Input.gyro.attitude;
                if (att.x != 0 || att.y != 0 || att.z != 0)
                {
                    gyroEverWorked = true;
                    gyroFirstReadTime = elapsed;
                    Debug.Log($"[DIAG] *** LEGACY GYRO FIRST READ at T+{elapsed:F2}s! Attitude: {att.eulerAngles}");
                }
            }
            
            // AR tracking start
            if (!arEverTracked)
            {
                var arState = UnityEngine.XR.ARFoundation.ARSession.state;
                if (arState == UnityEngine.XR.ARFoundation.ARSessionState.SessionTracking)
                {
                    arEverTracked = true;
                    arTrackingStartTime = elapsed;
                    Debug.Log($"[DIAG] *** AR TRACKING STARTED at T+{elapsed:F2}s!");
                }
            }
        }
        
        private void PeriodicDiagnosticLog()
        {
            LogAllSensorStates("PERIODIC");
        }
        
        private void LogAllSensorStates(string context)
        {
            float elapsed = Time.realtimeSinceStartup - appStartTime;
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine($"[DIAG] ====== {context} at T+{elapsed:F2}s ======");
            
            // GPS State
            if (logGPS)
            {
                sb.AppendLine(GetGPSState());
            }
            
            // Compass State
            if (logCompass)
            {
                sb.AppendLine(GetCompassState());
            }
            
            // Gyroscope State
            if (logGyroscope)
            {
                sb.AppendLine(GetGyroscopeState());
            }
            
            // Accelerometer State
            if (logAccelerometer)
            {
                sb.AppendLine(GetAccelerometerState());
            }
            
            // AR State
            if (logARState)
            {
                sb.AppendLine(GetARState());
            }
            
            // Camera State
            if (logCamera)
            {
                sb.AppendLine(GetCameraState());
            }
            
            // Coin State
            if (logCoinState)
            {
                sb.AppendLine(GetCoinState());
            }
            
            // Timing Summary
            sb.AppendLine(GetTimingSummary());
            
            Debug.Log(sb.ToString());
        }
        
        private string GetGPSState()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[GPS] ");
            
            // Unity location service status
            sb.Append($"ServiceStatus={Input.location.status}, ");
            sb.Append($"EnabledByUser={Input.location.isEnabledByUser}, ");
            
            if (Input.location.status == LocationServiceStatus.Running)
            {
                var data = Input.location.lastData;
                sb.Append($"Lat={data.latitude:F6}, Lng={data.longitude:F6}, ");
                sb.Append($"Accuracy={data.horizontalAccuracy:F1}m, ");
                sb.Append($"Timestamp={data.timestamp:F1}");
            }
            
            // GPSManager state
            if (GPSManager.Exists)
            {
                var gps = GPSManager.Instance;
                sb.Append($" | GPSMgr: IsTracking={gps.IsTracking}");
                
                var loc = gps.CurrentLocation;
                if (loc != null)
                {
                    sb.Append($", Acc={loc.horizontalAccuracy:F1}m");
                }
                else
                {
                    sb.Append(", CurrentLocation=NULL");
                }
            }
            else
            {
                sb.Append(" | GPSManager NOT EXISTS");
            }
            
            return sb.ToString();
        }
        
        private string GetCompassState()
        {
            StringBuilder sb = new StringBuilder();
            
            // DeviceCompass (New Input System — primary compass source)
            sb.Append("[DEVICE_COMPASS] ");
            sb.Append($"Available={DeviceCompass.IsAvailable}, ");
            sb.Append($"Method={DeviceCompass.ActiveMethod}, ");
            sb.Append($"Heading={DeviceCompass.Heading:F1}°, ");
            sb.Append($"RawHeading={DeviceCompass.RawHeading:F1}°, ");
            sb.Append($"IsTrueNorth={DeviceCompass.IsTrueNorth}");
            sb.AppendLine();
            
            // Legacy compass (kept for comparison — often broken on Android 16+)
            sb.Append("[LEGACY_COMPASS] ");
            sb.Append($"Enabled={Input.compass.enabled}, ");
            sb.Append($"TrueHeading={Input.compass.trueHeading:F1}°, ");
            sb.Append($"MagneticHeading={Input.compass.magneticHeading:F1}°, ");
            sb.Append($"RawVector={Input.compass.rawVector}, ");
            sb.Append($"Timestamp={Input.compass.timestamp:F1}, ");
            sb.Append($"HeadingAccuracy={Input.compass.headingAccuracy:F1}");
            
            // Check if legacy compass is actually returning data
            bool legacyWorking = Input.compass.trueHeading != 0 || Input.compass.magneticHeading != 0;
            sb.Append($" | Working={legacyWorking}");
            
            return sb.ToString();
        }
        
        private string GetGyroscopeState()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[GYRO] ");
            
            // NEW Input System sensors
            sb.Append("NEW: ");
            if (attitudeSensor != null)
            {
                var att = attitudeSensor.attitude.ReadValue();
                sb.Append($"Attitude={att.eulerAngles}, ");
                bool working = att.x != 0 || att.y != 0 || att.z != 0;
                sb.Append($"Working={working} | ");
            }
            else
            {
                sb.Append("AttitudeSensor=NULL | ");
            }
            
            if (gyroSensor != null)
            {
                var rate = gyroSensor.angularVelocity.ReadValue();
                sb.Append($"GyroRate={rate} | ");
            }
            
            // LEGACY sensors
            sb.Append("LEGACY: ");
            sb.Append($"Supported={SystemInfo.supportsGyroscope}, ");
            sb.Append($"Enabled={Input.gyro.enabled}, ");
            
            if (Input.gyro.enabled)
            {
                var att = Input.gyro.attitude;
                sb.Append($"Attitude={att.eulerAngles}");
                bool working = att.x != 0 || att.y != 0 || att.z != 0 || att.w != 1;
                sb.Append($", Working={working}");
            }
            
            return sb.ToString();
        }
        
        private string GetAccelerometerState()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[ACCEL] ");
            
            // NEW Input System accelerometer
            sb.Append("NEW: ");
            if (accelSensor != null)
            {
                Vector3 newAccel = accelSensor.acceleration.ReadValue();
                sb.Append($"Value={newAccel}, Mag={newAccel.magnitude:F3}, ");
                float tiltNew = Mathf.Atan2(newAccel.x, -newAccel.z) * Mathf.Rad2Deg;
                sb.Append($"TiltAngle={tiltNew:F1}°, ");
                bool working = newAccel.sqrMagnitude > 0.01f;
                sb.Append($"Working={working} | ");
            }
            else
            {
                sb.Append("Accelerometer=NULL | ");
            }
            
            if (gravitySensor != null)
            {
                Vector3 grav = gravitySensor.gravity.ReadValue();
                sb.Append($"Gravity={grav} | ");
            }
            
            // LEGACY accelerometer
            sb.Append("LEGACY: ");
            sb.Append($"Supported={SystemInfo.supportsAccelerometer}, ");
            
            Vector3 accel = Input.acceleration;
            sb.Append($"Value={accel}, Mag={accel.magnitude:F3}");
            
            // Calculate tilt angle (what we use for heading)
            float tiltAngle = Mathf.Atan2(accel.x, -accel.z) * Mathf.Rad2Deg;
            sb.Append($", TiltAngle={tiltAngle:F1}°");
            
            lastAccel = accel;
            
            return sb.ToString();
        }
        
        private string GetARState()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[AR] ");
            
            var state = UnityEngine.XR.ARFoundation.ARSession.state;
            sb.Append($"State={state}");
            
            // Track state changes
            string stateStr = state.ToString();
            if (stateStr != lastARState)
            {
                sb.Append($" (CHANGED from {lastARState ?? "null"})");
                lastARState = stateStr;
            }
            
            // XR subsystem status
            try
            {
                var xrSettings = UnityEngine.XR.XRSettings.enabled;
                sb.Append($", XREnabled={xrSettings}");
                sb.Append($", XRDevice={UnityEngine.XR.XRSettings.loadedDeviceName}");
            }
            catch { }
            
            return sb.ToString();
        }
        
        private string GetCameraState()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[CAMERA] ");
            
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 pos = cam.transform.position;
                Vector3 rot = cam.transform.eulerAngles;
                
                sb.Append($"Pos=({pos.x:F2},{pos.y:F2},{pos.z:F2}), ");
                sb.Append($"Rot=({rot.x:F1},{rot.y:F1},{rot.z:F1}), ");
                
                // Check if camera is moving
                float movement = Vector3.Distance(pos, lastCameraPos);
                sb.Append($"Movement={movement:F4}m");
                
                if (movement < 0.0001f)
                {
                    sb.Append(" (STATIONARY)");
                }
                else
                {
                    sb.Append(" (MOVING)");
                }
                
                lastCameraPos = pos;
            }
            else
            {
                sb.Append("Camera.main is NULL!");
            }
            
            return sb.ToString();
        }
        
        private string GetCoinState()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[COIN] ");
            
            if (CoinManager.Exists)
            {
                var mgr = CoinManager.Instance;
                sb.Append($"HasTarget={mgr.HasTarget}, ");
                sb.Append($"HuntMode={mgr.CurrentMode}, ");
                sb.Append($"KnownCoins={mgr.KnownCoins?.Count ?? 0}");
                
                if (mgr.HasTarget && mgr.TargetCoin != null)
                {
                    var coin = mgr.TargetCoin;
                    var renderer = coin.GetComponent<ARCoinRenderer>();
                    
                    sb.Append($", TargetID={coin.CoinId?.Substring(0, 8) ?? "null"}");
                    
                    if (renderer != null)
                    {
                        sb.Append($", Mode={renderer.CurrentMode}");
                        sb.Append($", GPSDist={renderer.GPSDistance:F1}m");
                        sb.Append($", ARDist={renderer.ARDistance:F1}m");
                    }
                    
                    // Coin world position
                    sb.Append($", CoinPos={coin.transform.position}");
                }
            }
            else
            {
                sb.Append("CoinManager NOT EXISTS");
            }
            
            return sb.ToString();
        }
        
        private string GetTimingSummary()
        {
            StringBuilder sb = new StringBuilder();
            float elapsed = Time.realtimeSinceStartup - appStartTime;
            
            sb.Append("[TIMING] ");
            sb.Append($"AppRuntime={elapsed:F1}s, ");
            sb.Append($"GPSFirstFix={(gpsFirstFixTime >= 0 ? $"{gpsFirstFixTime:F1}s" : "NEVER")}, ");
            sb.Append($"NEW_AttitudeFirst={(newAttitudeFirstTime >= 0 ? $"{newAttitudeFirstTime:F1}s" : "NEVER")}, ");
            sb.Append($"NEW_AccelFirst={(newAccelFirstTime >= 0 ? $"{newAccelFirstTime:F1}s" : "NEVER")}, ");
            sb.Append($"LEGACY_CompassFirst={(compassFirstReadTime >= 0 ? $"{compassFirstReadTime:F1}s" : "NEVER")}, ");
            sb.Append($"LEGACY_GyroFirst={(gyroFirstReadTime >= 0 ? $"{gyroFirstReadTime:F1}s" : "NEVER")}, ");
            sb.Append($"ARTrackingStart={(arTrackingStartTime >= 0 ? $"{arTrackingStartTime:F1}s" : "NEVER")}");
            
            return sb.ToString();
        }
        
        private void LogTimestamp(string eventName)
        {
            float elapsed = Time.realtimeSinceStartup - appStartTime;
            Debug.Log($"[DIAG] T+{elapsed:F3}s: {eventName}");
        }
        
        /// <summary>
        /// Call this to log a custom event with timestamp
        /// </summary>
        public void LogEvent(string eventName, string details = "")
        {
            float elapsed = Time.realtimeSinceStartup - appStartTime;
            Debug.Log($"[DIAG] T+{elapsed:F3}s: {eventName} {details}");
        }
        
        /// <summary>
        /// Force an immediate diagnostic dump
        /// </summary>
        [ContextMenu("Force Diagnostic Dump")]
        public void ForceDiagnosticDump()
        {
            LogAllSensorStates("MANUAL DUMP");
        }
    }
}
