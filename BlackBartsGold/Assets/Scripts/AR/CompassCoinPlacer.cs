// ============================================================================
// CompassCoinPlacer.cs
// Black Bart's Gold - Simple Compass-Based AR Coin Placement
// Path: Assets/Scripts/AR/CompassCoinPlacer.cs
// ============================================================================
// SIMPLE APPROACH: Place coin based on compass direction.
// When you point your phone at the coin's GPS bearing, it appears in front.
// When you turn away, it moves off screen.
// 
// This does NOT rely on AR tracking at all - just compass + gyroscope.
// 
// IMPORTANT: Uses NEW Input System sensors (Unity 6) - NOT the legacy Input class!
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using BlackBartsGold.Location;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.AR
{
    /// <summary>
    /// Simple compass-based coin placer. Places coin based on where phone is pointing.
    /// Uses the NEW Input System sensors for Unity 6 compatibility.
    /// </summary>
    public class CompassCoinPlacer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float displayDistance = 4f;
        [SerializeField] private float displayHeight = 0f; // Relative to camera
        [SerializeField] private float smoothSpeed = 8f;
        
        [Header("Debug")]
        [SerializeField] private bool logEnabled = true;
        
        // References
        private Camera arCamera;
        private Transform coinTransform;
        private CoinController coinController;
        
        // NEW Input System sensors
        private UnityEngine.InputSystem.Gyroscope gyroSensor;
        private Accelerometer accelSensor;
        private AttitudeSensor attitudeSensor;
        private GravitySensor gravitySensor;
        
        // State
        private bool hasTarget = false;
        private double targetLat;
        private double targetLon;
        private float lastLogTime;
        private float lastRotationLogTime = -999f;
        
        // Smoothed heading to reduce jitter
        private float smoothedHeading = 0f;
        private float headingSmoothVelocity = 0f;
        private const float HEADING_SMOOTH_TIME = 0.3f;
        
        private void Awake()
        {
            Debug.Log("[CompassCoinPlacer] AWAKE");
        }
        
        private void Start()
        {
            Debug.Log($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: START - Enabling NEW INPUT SYSTEM sensors...");
            
            // ================================================================
            // ENABLE NEW INPUT SYSTEM SENSORS
            // The legacy Input.compass, Input.gyro, Input.acceleration don't work
            // when Active Input Handling is set to "Input System Package (New)"
            // ================================================================
            
            // Get and enable Attitude Sensor (device orientation)
            attitudeSensor = AttitudeSensor.current;
            if (attitudeSensor != null)
            {
                InputSystem.EnableDevice(attitudeSensor);
                Debug.Log($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: AttitudeSensor ENABLED: {attitudeSensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: AttitudeSensor NOT AVAILABLE");
            }
            
            // Get and enable Gyroscope
            gyroSensor = UnityEngine.InputSystem.Gyroscope.current;
            if (gyroSensor != null)
            {
                InputSystem.EnableDevice(gyroSensor);
                Debug.Log($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: Gyroscope ENABLED: {gyroSensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: Gyroscope NOT AVAILABLE");
            }
            
            // Get and enable Accelerometer
            accelSensor = Accelerometer.current;
            if (accelSensor != null)
            {
                InputSystem.EnableDevice(accelSensor);
                Debug.Log($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: Accelerometer ENABLED: {accelSensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: Accelerometer NOT AVAILABLE");
            }
            
            // Get and enable Gravity Sensor
            gravitySensor = GravitySensor.current;
            if (gravitySensor != null)
            {
                InputSystem.EnableDevice(gravitySensor);
                Debug.Log($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: GravitySensor ENABLED: {gravitySensor.enabled}");
            }
            else
            {
                Debug.LogWarning($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: GravitySensor NOT AVAILABLE");
            }
            
            // Also try legacy APIs (may work on some devices)
            Input.compass.enabled = true;
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
            }
            
            Debug.Log($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: Sensor initialization complete");
            
            // Find camera
            arCamera = Camera.main;
            if (arCamera == null)
            {
                arCamera = FindFirstObjectByType<Camera>();
            }
            
            // Get our coin controller
            coinController = GetComponent<CoinController>();
            coinTransform = transform;
            
            // Subscribe to target events
            if (CoinManager.Exists)
            {
                CoinManager.Instance.OnTargetSet += OnTargetSet;
                CoinManager.Instance.OnTargetCleared += OnTargetCleared;
                
                // Check if already have target
                if (CoinManager.Instance.HasTarget && CoinManager.Instance.TargetCoinData != null)
                {
                    var coin = CoinManager.Instance.TargetCoinData;
                    targetLat = coin.latitude;
                    targetLon = coin.longitude;
                    hasTarget = true;
                    Debug.Log($"[CompassCoinPlacer] Already has target at {targetLat}, {targetLon}");
                }
            }
            
            Debug.Log($"[CompassCoinPlacer] Started. Camera={arCamera != null}, Compass={Input.compass.enabled}");
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
            targetLat = coin.latitude;
            targetLon = coin.longitude;
            hasTarget = true;
            Debug.Log($"[CompassCoinPlacer] Target set: {targetLat}, {targetLon}");
        }
        
        private void OnTargetCleared()
        {
            hasTarget = false;
            Debug.Log("[CompassCoinPlacer] Target cleared");
        }
        
        private void Update()
        {
            if (!hasTarget || arCamera == null) return;
            
            // When GyroscopeCoinPositioner is active, it owns position/rotation - don't fight it
            var gyroPlacer = GetComponent<GyroscopeCoinPositioner>();
            if (gyroPlacer != null && gyroPlacer.enabled) return;
            
            // Get player location
            LocationData playerLoc = null;
            if (GPSManager.Exists)
            {
                playerLoc = GPSManager.Instance.GetCurrentLocation();
            }
            if (playerLoc == null) return;
            
            // Calculate bearing to target (0° = North, 90° = East)
            float bearingToTarget = (float)GeoUtils.CalculateBearing(
                playerLoc.latitude, playerLoc.longitude,
                targetLat, targetLon
            );
            
            // Get device heading - TRY MULTIPLE METHODS
            float deviceHeading = GetDeviceHeading();
            
            // Calculate relative bearing
            // 0 = target straight ahead, 90 = target to right, -90 = target to left
            float relativeBearing = bearingToTarget - deviceHeading;
            
            // Normalize to -180 to 180
            while (relativeBearing > 180) relativeBearing -= 360;
            while (relativeBearing < -180) relativeBearing += 360;
            
            // Position coin based on relative bearing
            PositionCoin(relativeBearing);
            
            // Log periodically
            if (logEnabled && Time.time - lastLogTime > 3f)
            {
                lastLogTime = Time.time;
                float gpsDistance = (float)GeoUtils.CalculateDistance(
                    playerLoc.latitude, playerLoc.longitude, targetLat, targetLon);
                Debug.Log($"[CompassCoinPlacer] Bearing={bearingToTarget:F0}°, DeviceHeading={deviceHeading:F0}°, Relative={relativeBearing:F0}°, GPS={gpsDistance:F1}m");
            }
        }
        
        /// <summary>
        /// Get device heading using multiple fallback methods.
        /// Returns SMOOTHED heading to reduce jitter.
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
            // Method 4: LEGACY - Try old Input.compass (may work on some devices)
            // ================================================================
            if (methodUsed == "none")
            {
                float compassHeading = Input.compass.trueHeading;
                if (compassHeading == 0 || float.IsNaN(compassHeading))
                {
                    compassHeading = Input.compass.magneticHeading;
                }
                
                if (compassHeading != 0 && !float.IsNaN(compassHeading))
                {
                    result = compassHeading;
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
            // Method 6: Camera Y rotation (last resort)
            // ================================================================
            if (methodUsed == "none" && arCamera != null)
            {
                result = arCamera.transform.eulerAngles.y;
                methodUsed = "camera";
            }
            
            // Log detailed sensor state every 5 seconds
            if (logEnabled && Time.frameCount % 300 == 0)
            {
                Debug.Log($"[CompassCoinPlacer] T+{Time.realtimeSinceStartup:F2}s: === SENSOR STATUS ===");
                Debug.Log($"[CompassCoinPlacer]   Method used: {methodUsed}, Result: {result:F1}°");
                
                // New Input System sensors
                if (attitudeSensor != null)
                {
                    var att = attitudeSensor.attitude.ReadValue();
                    Debug.Log($"[CompassCoinPlacer]   AttitudeSensor: enabled={attitudeSensor.enabled}, attitude={att.eulerAngles}");
                }
                if (accelSensor != null)
                {
                    var acc = accelSensor.acceleration.ReadValue();
                    Debug.Log($"[CompassCoinPlacer]   Accelerometer(NEW): enabled={accelSensor.enabled}, value={acc}, mag={acc.magnitude:F3}");
                }
                if (gravitySensor != null)
                {
                    var grav = gravitySensor.gravity.ReadValue();
                    Debug.Log($"[CompassCoinPlacer]   GravitySensor: enabled={gravitySensor.enabled}, value={grav}");
                }
                
                // Legacy sensors (for comparison)
                Debug.Log($"[CompassCoinPlacer]   LEGACY compass: true={Input.compass.trueHeading:F1}°, mag={Input.compass.magneticHeading:F1}°");
                Debug.Log($"[CompassCoinPlacer]   LEGACY accel: {Input.acceleration}, mag={Input.acceleration.magnitude:F3}");
                Debug.Log($"[CompassCoinPlacer]   Camera: rot={arCamera?.transform.eulerAngles}");
            }
            
            return result;
        }
        
        // Smoothed pitch for vertical positioning
        private float smoothedPitch = 0f;
        private float pitchSmoothVelocity = 0f;
        private const float PITCH_SMOOTH_TIME = 0.15f;
        
        /// <summary>
        /// Get device pitch from attitude sensor for vertical coin movement.
        /// </summary>
        private float GetDevicePitch()
        {
            if (attitudeSensor != null && attitudeSensor.enabled)
            {
                Quaternion attitude = attitudeSensor.attitude.ReadValue();
                if (attitude.x != 0 || attitude.y != 0 || attitude.z != 0)
                {
                    Quaternion rotFix = Quaternion.Euler(90f, 0f, 0f);
                    Quaternion deviceRot = rotFix * new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
                    float pitch = deviceRot.eulerAngles.x;
                    if (pitch > 180f) pitch -= 360f;
                    return pitch;
                }
            }
            
            if (gravitySensor != null && gravitySensor.enabled)
            {
                Vector3 gravity = gravitySensor.gravity.ReadValue();
                if (gravity.sqrMagnitude > 0.01f)
                {
                    float pitch = Mathf.Atan2(-gravity.z, -gravity.y) * Mathf.Rad2Deg;
                    return pitch;
                }
            }
            
            return 0f;
        }
        
        private void PositionCoin(float relativeBearing)
        {
            // Convert to radians
            float rad = relativeBearing * Mathf.Deg2Rad;
            
            // Calculate offset from camera center
            // X = how far right/left (sin)
            // Z = how far forward (cos)
            float x = Mathf.Sin(rad) * displayDistance;
            float z = Mathf.Cos(rad) * displayDistance;
            
            // Get pitch for vertical positioning
            float rawPitch = GetDevicePitch();
            smoothedPitch = Mathf.SmoothDamp(smoothedPitch, rawPitch, ref pitchSmoothVelocity, PITCH_SMOOTH_TIME);
            
            // Convert pitch to Y offset — WORLD-ANCHORED behavior:
            // Tilt phone up → coin drops below center (stays in world while view rises)
            // Tilt phone down → coin rises above center (stays in world while view drops)
            float pitchRad = smoothedPitch * Mathf.Deg2Rad;
            float yOffset = Mathf.Clamp(Mathf.Tan(pitchRad) * displayDistance, -3f, 3f);
            
            // Target position relative to camera WITH pitch-based Y offset
            Vector3 camPos = arCamera.transform.position;
            Vector3 targetPos = camPos + new Vector3(x, displayHeight + yOffset, z);
            
            // Smooth movement
            coinTransform.position = Vector3.Lerp(
                coinTransform.position, 
                targetPos, 
                Time.deltaTime * smoothSpeed
            );
            
            // Face camera (billboard)
            Vector3 lookDir = camPos - coinTransform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                coinTransform.rotation = Quaternion.LookRotation(-lookDir, Vector3.up);
                // Debug: log pitch and resulting position (throttled every 0.5s)
                if (logEnabled && Time.realtimeSinceStartup - lastRotationLogTime >= 0.5f)
                {
                    lastRotationLogTime = Time.realtimeSinceStartup;
                    Vector3 euler = coinTransform.eulerAngles;
                    Debug.Log($"[CompassCoinPlacer] POS | pitch={smoothedPitch:F1}° yOff={yOffset:F2}m coinY={coinTransform.position.y:F2} | lookDir=({lookDir.x:F2},{lookDir.y:F2},{lookDir.z:F2}) euler=({euler.x:F1},{euler.y:F1},{euler.z:F1})");
                }
            }
        }
    }
}
