// ============================================================================
// GyroscopeCoinPositioner.cs
// Black Bart's Gold - Gyroscope-Based AR Coin Positioning
// Path: Assets/Scripts/AR/GyroscopeCoinPositioner.cs
// ============================================================================
// SOLUTION: When ARCore tracking fails (camera position never changes),
// we use gyroscope + compass to position the coin in world space.
//
// This is how Pokemon GO's "basic AR mode" works:
// 1. Calculate the COMPASS BEARING to the target (GPS)
// 2. Use GYROSCOPE to know which direction the phone is pointing
// 3. When phone points at target bearing, coin appears in front
// 4. Rotate phone away, coin moves off screen
//
// This approach works WITHOUT ARCore tracking!
//
// IMPORTANT: Uses NEW Input System sensors (Unity 6) - NOT the legacy Input class!
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using BlackBartsGold.Location;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Positions the AR coin using gyroscope and compass when AR tracking isn't available.
    /// This is the Pokemon GO "basic AR" approach.
    /// Uses the NEW Input System sensors for Unity 6 compatibility.
    /// </summary>
    public class GyroscopeCoinPositioner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float displayDistance = 4f;
        [SerializeField] private float displayHeight = 1.2f;
        [SerializeField] private float smoothSpeed = 5f;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = true;
        
        // Target coin data
        private double targetLatitude;
        private double targetLongitude;
        private bool hasTarget = false;
        
        // Legacy Gyroscope (for fallback)
        private UnityEngine.Gyroscope gyro;
        private bool gyroEnabled = false;
        private Quaternion gyroRotation;
        
        // NEW Input System sensors
        private UnityEngine.InputSystem.Gyroscope gyroSensor;
        private Accelerometer accelSensor;
        private AttitudeSensor attitudeSensor;
        private GravitySensor gravitySensor;
        
        // Camera reference
        private Camera arCamera;
        private Transform cameraTransform;
        
        // Calculated values
        private float bearingToTarget = 0f;
        private float gpsDistance = 0f;
        
        // Smoothed heading to reduce jitter from accelerometer
        private float smoothedHeading = 0f;
        private float headingSmoothVelocity = 0f;
        private const float HEADING_SMOOTH_TIME = 0.3f; // Smooth over 0.3 seconds
        
        private void Start()
        {
            Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Start() BEGIN");
            
            // Enable gyroscope
            EnableGyroscope();
            
            // Enable compass - IMPORTANT: Start location service first on Android
            StartCoroutine(EnableCompassCoroutine());
            
            // Find camera
            arCamera = Camera.main;
            if (arCamera != null)
            {
                cameraTransform = arCamera.transform;
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Camera found: {arCamera.name}");
            }
            else
            {
                Debug.LogWarning($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Camera.main is NULL!");
            }
            
            // Subscribe to target changes
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet += OnTargetSet;
                CoinManager.Instance.OnTargetCleared += OnTargetCleared;
                
                // Check if we already have a target
                if (CoinManager.Instance.HasTarget && CoinManager.Instance.TargetCoinData != null)
                {
                    var coin = CoinManager.Instance.TargetCoinData;
                    SetTarget(coin.latitude, coin.longitude);
                    Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Already had target: {coin.latitude:F6}, {coin.longitude:F6}");
                }
                else
                {
                    Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: No existing target");
                }
            }
            else
            {
                Debug.LogWarning($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: CoinManager does NOT exist!");
            }
            
            Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Start() END. Gyro={gyroEnabled}, Camera={(arCamera != null ? "OK" : "NULL")}");
        }
        
        private System.Collections.IEnumerator EnableCompassCoroutine()
        {
            float startTime = Time.realtimeSinceStartup;
            Debug.Log($"[GyroscopeCoinPositioner] T+{startTime:F2}s: EnableCompassCoroutine BEGIN");
            
            // Check current location service state
            Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Input.location.isEnabledByUser={Input.location.isEnabledByUser}");
            Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Input.location.status={Input.location.status}");
            
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Location NOT enabled by user!");
            }
            
            if (Input.location.status == LocationServiceStatus.Stopped)
            {
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Location stopped, calling Input.location.Start()...");
                Input.location.Start(1f, 0.1f);
                
                // Wait for location to initialize
                int maxWait = 15;
                int waitCount = 0;
                while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
                {
                    yield return new WaitForSeconds(1);
                    maxWait--;
                    waitCount++;
                    Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Waiting for location... ({waitCount}s), status={Input.location.status}");
                }
                
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Location init done after {waitCount}s, status={Input.location.status}");
            }
            else
            {
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Location already running, status={Input.location.status}");
            }
            
            // Now enable compass
            Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Enabling compass...");
            Input.compass.enabled = true;
            
            // Wait a moment for compass to warm up
            yield return new WaitForSeconds(0.5f);
            
            float elapsed = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Compass status after {elapsed:F1}s:");
            Debug.Log($"[GyroscopeCoinPositioner]   - enabled: {Input.compass.enabled}");
            Debug.Log($"[GyroscopeCoinPositioner]   - trueHeading: {Input.compass.trueHeading:F1}°");
            Debug.Log($"[GyroscopeCoinPositioner]   - magneticHeading: {Input.compass.magneticHeading:F1}°");
            Debug.Log($"[GyroscopeCoinPositioner]   - rawVector: {Input.compass.rawVector}");
            Debug.Log($"[GyroscopeCoinPositioner]   - timestamp: {Input.compass.timestamp}");
            Debug.Log($"[GyroscopeCoinPositioner]   - headingAccuracy: {Input.compass.headingAccuracy}");
            
            bool compassWorking = Input.compass.trueHeading != 0 || Input.compass.magneticHeading != 0;
            Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Compass WORKING: {compassWorking}");
        }
        
        private void EnableGyroscope()
        {
            Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: EnableGyroscope() - Enabling NEW INPUT SYSTEM sensors...");
            
            // ================================================================
            // ENABLE NEW INPUT SYSTEM SENSORS
            // The legacy Input.compass, Input.gyro, Input.acceleration don't work
            // when Active Input Handling is set to "Input System Package (New)"
            // ================================================================
            
            // Get and enable Attitude Sensor (device orientation) - MOST IMPORTANT
            attitudeSensor = AttitudeSensor.current;
            if (attitudeSensor != null)
            {
                InputSystem.EnableDevice(attitudeSensor);
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: AttitudeSensor ENABLED: {attitudeSensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: AttitudeSensor NOT AVAILABLE");
            }
            
            // Get and enable Gyroscope (new input system)
            gyroSensor = UnityEngine.InputSystem.Gyroscope.current;
            if (gyroSensor != null)
            {
                InputSystem.EnableDevice(gyroSensor);
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Gyroscope(NEW) ENABLED: {gyroSensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Gyroscope(NEW) NOT AVAILABLE");
            }
            
            // Get and enable Accelerometer
            accelSensor = Accelerometer.current;
            if (accelSensor != null)
            {
                InputSystem.EnableDevice(accelSensor);
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Accelerometer ENABLED: {accelSensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Accelerometer NOT AVAILABLE");
            }
            
            // Get and enable Gravity Sensor
            gravitySensor = GravitySensor.current;
            if (gravitySensor != null)
            {
                InputSystem.EnableDevice(gravitySensor);
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: GravitySensor ENABLED: {gravitySensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: GravitySensor NOT AVAILABLE");
            }
            
            // Also enable legacy gyroscope (may work on some devices)
            if (SystemInfo.supportsGyroscope)
            {
                gyro = Input.gyro;
                gyro.enabled = true;
                gyroEnabled = true;
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Legacy gyro enabled={gyro.enabled}");
            }
            
            Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: Sensor initialization complete");
        }
        
        private void OnDestroy()
        {
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet -= OnTargetSet;
                CoinManager.Instance.OnTargetCleared -= OnTargetCleared;
            }
        }
        
        private void OnTargetSet(Coin coin)
        {
            SetTarget(coin.latitude, coin.longitude);
        }
        
        private void OnTargetCleared()
        {
            hasTarget = false;
            Debug.Log("[GyroscopeCoinPositioner] Target cleared");
        }
        
        public void SetTarget(double latitude, double longitude)
        {
            targetLatitude = latitude;
            targetLongitude = longitude;
            hasTarget = true;
            Debug.Log($"[GyroscopeCoinPositioner] Target set: {latitude}, {longitude}");
        }
        
        private void Update()
        {
            if (!hasTarget) return;
            if (cameraTransform == null) return;
            
            // Get player location
            LocationData playerLoc = null;
            if (GPSManager.Exists)
            {
                playerLoc = GPSManager.Instance.GetCurrentLocation();
            }
            if (playerLoc == null) return;
            
            // Calculate bearing and distance to target
            bearingToTarget = (float)GeoUtils.CalculateBearing(
                playerLoc.latitude, playerLoc.longitude,
                targetLatitude, targetLongitude
            );
            
            gpsDistance = (float)GeoUtils.CalculateDistance(
                playerLoc.latitude, playerLoc.longitude,
                targetLatitude, targetLongitude
            );
            
            // Get device heading (which direction phone is pointing)
            float deviceHeading = GetDeviceHeading();
            
            // Calculate relative angle: how much to rotate from current view to see target
            // If relativeBearing = 0, target is straight ahead
            // If relativeBearing = 90, target is to the right
            float relativeBearing = bearingToTarget - deviceHeading;
            
            // Normalize to -180 to 180
            while (relativeBearing > 180) relativeBearing -= 360;
            while (relativeBearing < -180) relativeBearing += 360;
            
            // Position the coin based on relative bearing
            PositionCoin(relativeBearing);
            
            // Debug logging
            if (debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[GyroscopeCoinPositioner] Bearing={bearingToTarget:F0}°, Heading={deviceHeading:F0}°, Relative={relativeBearing:F0}°, GPS={gpsDistance:F1}m");
            }
        }
        
        /// <summary>
        /// Get device heading using compass, gyroscope, or accelerometer.
        /// Returns a SMOOTHED heading to reduce jitter.
        /// </summary>
        private float GetDeviceHeading()
        {
            float rawHeading = GetRawDeviceHeading();
            
            // Apply smoothing using SmoothDampAngle (handles angle wrapping)
            smoothedHeading = Mathf.SmoothDampAngle(smoothedHeading, rawHeading, ref headingSmoothVelocity, HEADING_SMOOTH_TIME);
            
            return smoothedHeading;
        }
        
        /// <summary>
        /// Get raw device heading without smoothing.
        /// Uses NEW Input System sensors for Unity 6 compatibility.
        /// </summary>
        private float GetRawDeviceHeading()
        {
            string methodUsed = "none";
            float result = 0f;
            
            // ================================================================
            // Method 1: NEW INPUT SYSTEM - AttitudeSensor (device orientation)
            // This is the most reliable for device heading in Unity 6
            // ================================================================
            if (attitudeSensor != null && attitudeSensor.enabled)
            {
                Quaternion attitude = attitudeSensor.attitude.ReadValue();
                
                // Check if we're getting real data
                if (attitude.x != 0 || attitude.y != 0 || attitude.z != 0)
                {
                    // Convert device attitude to heading
                    // Apply rotation fix for Android coordinate system
                    Quaternion rotFix = Quaternion.Euler(90f, 0f, 0f);
                    Quaternion deviceRot = rotFix * new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
                    result = deviceRot.eulerAngles.y;
                    methodUsed = "attitude_sensor";
                }
            }
            
            // ================================================================
            // Method 2: NEW INPUT SYSTEM - Accelerometer
            // Use gravity direction to estimate device tilt/rotation
            // ================================================================
            if (methodUsed == "none" && accelSensor != null && accelSensor.enabled)
            {
                Vector3 accel = accelSensor.acceleration.ReadValue();
                if (accel.sqrMagnitude > 0.01f)
                {
                    // Get tilt angle from accelerometer
                    float tiltAngle = Mathf.Atan2(accel.x, -accel.z) * Mathf.Rad2Deg;
                    result = tiltAngle;
                    methodUsed = "new_accelerometer";
                }
            }
            
            // ================================================================
            // Method 3: NEW INPUT SYSTEM - GravitySensor
            // Similar to accelerometer but filtered
            // ================================================================
            if (methodUsed == "none" && gravitySensor != null && gravitySensor.enabled)
            {
                Vector3 gravity = gravitySensor.gravity.ReadValue();
                if (gravity.sqrMagnitude > 0.01f)
                {
                    float tiltAngle = Mathf.Atan2(gravity.x, -gravity.z) * Mathf.Rad2Deg;
                    result = tiltAngle;
                    methodUsed = "gravity_sensor";
                }
            }
            
            // ================================================================
            // Method 4: LEGACY - Try old Input.compass
            // ================================================================
            if (methodUsed == "none" && Input.compass.enabled)
            {
                float heading = Input.compass.trueHeading;
                if (heading == 0 || float.IsNaN(heading))
                {
                    heading = Input.compass.magneticHeading;
                }
                
                if (heading != 0 && !float.IsNaN(heading))
                {
                    result = heading;
                    methodUsed = "legacy_compass";
                }
            }
            
            // ================================================================
            // Method 5: LEGACY - Try old Input.acceleration
            // ================================================================
            if (methodUsed == "none")
            {
                Vector3 accel = Input.acceleration;
                if (accel.sqrMagnitude > 0.01f)
                {
                    float tiltAngle = Mathf.Atan2(accel.x, -accel.z) * Mathf.Rad2Deg;
                    result = tiltAngle;
                    methodUsed = "legacy_accelerometer";
                }
            }
            
            // ================================================================
            // Method 6: LEGACY - Try old Input.gyro
            // ================================================================
            if (methodUsed == "none" && gyroEnabled && gyro != null)
            {
                Quaternion gyroAttitude = gyro.attitude;
                
                if (gyroAttitude.x != 0 || gyroAttitude.y != 0 || gyroAttitude.z != 0)
                {
                    Quaternion rotFix = Quaternion.Euler(90f, 0f, 0f);
                    Quaternion deviceRot = rotFix * new Quaternion(gyroAttitude.x, gyroAttitude.y, -gyroAttitude.z, -gyroAttitude.w);
                    Vector3 forward = deviceRot * Vector3.forward;
                    float heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                    if (heading < 0) heading += 360f;
                    
                    result = heading;
                    methodUsed = "legacy_gyroscope";
                }
            }
            
            // ================================================================
            // Method 7: Camera Y rotation (last resort)
            // ================================================================
            if (methodUsed == "none" && arCamera != null)
            {
                result = arCamera.transform.eulerAngles.y;
                methodUsed = "camera";
            }
            
            // Log detailed sensor state every 5 seconds
            if (debugMode && Time.frameCount % 300 == 0)
            {
                Debug.Log($"[GyroscopeCoinPositioner] T+{Time.realtimeSinceStartup:F2}s: === SENSOR STATUS ===");
                Debug.Log($"[GyroscopeCoinPositioner]   Method used: {methodUsed}, RawResult: {result:F1}°, SmoothedHeading: {smoothedHeading:F1}°");
                
                // New Input System sensors
                if (attitudeSensor != null)
                {
                    var att = attitudeSensor.attitude.ReadValue();
                    Debug.Log($"[GyroscopeCoinPositioner]   AttitudeSensor: enabled={attitudeSensor.enabled}, attitude={att.eulerAngles}");
                }
                if (accelSensor != null)
                {
                    var acc = accelSensor.acceleration.ReadValue();
                    Debug.Log($"[GyroscopeCoinPositioner]   Accelerometer(NEW): enabled={accelSensor.enabled}, value={acc}, mag={acc.magnitude:F3}");
                }
                if (gravitySensor != null)
                {
                    var grav = gravitySensor.gravity.ReadValue();
                    Debug.Log($"[GyroscopeCoinPositioner]   GravitySensor: enabled={gravitySensor.enabled}, value={grav}");
                }
                
                // Legacy sensors (for comparison)
                Debug.Log($"[GyroscopeCoinPositioner]   LEGACY compass: enabled={Input.compass.enabled}, true={Input.compass.trueHeading:F1}°, mag={Input.compass.magneticHeading:F1}°");
                Debug.Log($"[GyroscopeCoinPositioner]   LEGACY accel: {Input.acceleration}, mag={Input.acceleration.magnitude:F3}");
                Debug.Log($"[GyroscopeCoinPositioner]   LEGACY gyro: enabled={gyroEnabled}, attitude={(gyro != null ? gyro.attitude.eulerAngles.ToString() : "null")}");
                Debug.Log($"[GyroscopeCoinPositioner]   Camera: rot={(arCamera != null ? arCamera.transform.eulerAngles.ToString() : "null")}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Position the coin based on relative bearing.
        /// Coin appears in front of camera when pointing at target.
        /// </summary>
        private void PositionCoin(float relativeBearing)
        {
            if (cameraTransform == null) return;
            
            // Use gyroscope rotation directly to determine where coin should appear
            if (gyroEnabled && gyro != null)
            {
                // Get device rotation
                Quaternion gyroAttitude = gyro.attitude;
                Quaternion rotFix = Quaternion.Euler(90f, 0f, 0f);
                Quaternion deviceRot = rotFix * new Quaternion(gyroAttitude.x, gyroAttitude.y, -gyroAttitude.z, -gyroAttitude.w);
                
                // The coin should be at bearingToTarget degrees from north
                // The phone is currently pointing at deviceHeading degrees from north
                // So the coin should appear at (bearingToTarget - deviceHeading) relative to phone forward
                
                // Convert relative bearing to a direction
                float radians = relativeBearing * Mathf.Deg2Rad;
                
                // Create a rotation from the bearing
                Quaternion bearingRot = Quaternion.Euler(0, relativeBearing, 0);
                
                // Get the direction where the coin should appear (relative to phone)
                Vector3 coinDirection = bearingRot * Vector3.forward;
                
                // Position in world space relative to camera
                Vector3 targetPos = cameraTransform.position + coinDirection * displayDistance;
                targetPos.y = cameraTransform.position.y + displayHeight;
                
                // Smooth movement
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
            }
            else
            {
                // Fallback: use simple bearing calculation
                float radians = relativeBearing * Mathf.Deg2Rad;
                float x = Mathf.Sin(radians) * displayDistance;
                float z = Mathf.Cos(radians) * displayDistance;
                
                Vector3 targetPos = cameraTransform.position + new Vector3(x, displayHeight, z);
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
            }
            
            // Face camera (billboard)
            Vector3 lookDir = cameraTransform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(-lookDir, Vector3.up);
            }
        }
        
        /// <summary>
        /// Get the GPS distance to target
        /// </summary>
        public float GPSDistance => gpsDistance;
        
        /// <summary>
        /// Get the bearing to target
        /// </summary>
        public float BearingToTarget => bearingToTarget;
        
        /// <summary>
        /// Is the target in front of the camera? (within ~60 degrees)
        /// </summary>
        public bool IsTargetInView
        {
            get
            {
                if (!hasTarget) return false;
                
                float deviceHeading = GetDeviceHeading();
                float relative = bearingToTarget - deviceHeading;
                while (relative > 180) relative -= 360;
                while (relative < -180) relative += 360;
                
                return Mathf.Abs(relative) < 60f;
            }
        }
    }
}
