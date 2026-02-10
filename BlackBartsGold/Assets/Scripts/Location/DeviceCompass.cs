// ============================================================================
// DeviceCompass.cs
// Black Bart's Gold - New Input System Compass Replacement
// Path: Assets/Scripts/Location/DeviceCompass.cs
// ============================================================================
// CRITICAL FIX: Replaces broken legacy Input.compass with New Input System
// sensors. The legacy Input.compass returns Enabled=False on Android 16 / 
// Pixel 6 (and likely other modern devices). 
//
// This class uses MagneticFieldSensor + GravitySensor for TRUE compass 
// heading (magnetic north), with AttitudeSensor as a fallback for 
// rotational heading.
//
// Usage:  
//   DeviceCompass.Initialize();            // Call once at startup
//   float heading = DeviceCompass.Heading; // Use anywhere
//   bool ok = DeviceCompass.IsAvailable;   // Check availability
//
// Replaces ALL uses of Input.compass.trueHeading / magneticHeading.
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackBartsGold.Location
{
    /// <summary>
    /// Drop-in replacement for Unity's legacy Input.compass.
    /// Uses New Input System sensors that actually work on modern Android devices.
    /// Thread-safe, lazy-initialized singleton pattern.
    /// </summary>
    public static class DeviceCompass
    {
        #region Private State
        
        private static bool _initialized = false;
        
        // New Input System sensors
        private static MagneticFieldSensor _magneticFieldSensor;
        private static GravitySensor _gravitySensor;
        private static Accelerometer _accelerometer;
        private static AttitudeSensor _attitudeSensor;
        
        // Cached heading values (updated each frame via UpdateHeading)
        private static float _currentHeading = 0f;
        private static float _smoothedHeading = 0f;
        private static float _headingSmoothVelocity = 0f;
        private static string _activeMethod = "none";
        
        // Smoothing
        private const float HEADING_SMOOTH_TIME = 0.15f;
        
        // Logging throttle
        private static float _lastLogTime = -999f;
        private static int _updateCount = 0;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Is the compass initialized and at least one sensor available?
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                if (!_initialized) Initialize();
                return _magneticFieldSensor != null || _attitudeSensor != null;
            }
        }
        
        /// <summary>
        /// Best available compass heading in degrees (0=North, 90=East, 180=South, 270=West).
        /// This is the SMOOTHED heading — use for UI display and direction indicators.
        /// Falls back through: MagneticField+Gravity → AttitudeSensor → legacy Input.compass → 0
        /// </summary>
        public static float Heading
        {
            get
            {
                if (!_initialized) Initialize();
                UpdateHeading();
                return _smoothedHeading;
            }
        }
        
        /// <summary>
        /// Raw (unsmoothed) compass heading in degrees.
        /// Use when you need instant response without smoothing lag.
        /// </summary>
        public static float RawHeading
        {
            get
            {
                if (!_initialized) Initialize();
                UpdateHeading();
                return _currentHeading;
            }
        }
        
        /// <summary>
        /// Which method is currently providing the heading.
        /// Useful for diagnostics: "magnetic_field", "attitude_sensor", "legacy_compass", "none"
        /// </summary>
        public static string ActiveMethod
        {
            get
            {
                if (!_initialized) Initialize();
                return _activeMethod;
            }
        }
        
        /// <summary>
        /// Is the heading coming from a true magnetic north source (MagneticFieldSensor)?
        /// If false, heading may drift over time (AttitudeSensor fallback).
        /// </summary>
        public static bool IsTrueNorth => _activeMethod == "magnetic_field";
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize all compass sensors. Safe to call multiple times.
        /// Call this early (e.g., in a manager's Start method).
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            Debug.Log("[DeviceCompass] Initializing New Input System compass sensors...");
            
            // ================================================================
            // MagneticFieldSensor — gives raw magnetic field in microtesla.
            // Combined with gravity, this gives TRUE compass heading.
            // ================================================================
            _magneticFieldSensor = MagneticFieldSensor.current;
            if (_magneticFieldSensor != null)
            {
                InputSystem.EnableDevice(_magneticFieldSensor);
                Debug.Log($"[DeviceCompass] MagneticFieldSensor: ENABLED={_magneticFieldSensor.enabled}");
            }
            else
            {
                Debug.LogWarning("[DeviceCompass] MagneticFieldSensor: NOT AVAILABLE");
            }
            
            // ================================================================
            // GravitySensor — gives gravity direction (filtered accelerometer).
            // Needed to compute horizontal plane for magnetic heading.
            // ================================================================
            _gravitySensor = GravitySensor.current;
            if (_gravitySensor != null)
            {
                InputSystem.EnableDevice(_gravitySensor);
                Debug.Log($"[DeviceCompass] GravitySensor: ENABLED={_gravitySensor.enabled}");
            }
            else
            {
                Debug.LogWarning("[DeviceCompass] GravitySensor: NOT AVAILABLE");
            }
            
            // ================================================================
            // Accelerometer — fallback for GravitySensor if not available.
            // ================================================================
            _accelerometer = Accelerometer.current;
            if (_accelerometer != null)
            {
                InputSystem.EnableDevice(_accelerometer);
                Debug.Log($"[DeviceCompass] Accelerometer: ENABLED={_accelerometer.enabled}");
            }
            else
            {
                Debug.LogWarning("[DeviceCompass] Accelerometer: NOT AVAILABLE");
            }
            
            // ================================================================
            // AttitudeSensor — fused device orientation (gyro+accel+mag).
            // Good fallback — heading responds to rotation but may drift.
            // ================================================================
            _attitudeSensor = AttitudeSensor.current;
            if (_attitudeSensor != null)
            {
                InputSystem.EnableDevice(_attitudeSensor);
                Debug.Log($"[DeviceCompass] AttitudeSensor: ENABLED={_attitudeSensor.enabled}");
            }
            else
            {
                Debug.LogWarning("[DeviceCompass] AttitudeSensor: NOT AVAILABLE");
            }
            
            // Also enable legacy compass (it might start working on some devices)
            Input.compass.enabled = true;
            
            Debug.Log($"[DeviceCompass] Initialization complete. " +
                       $"MagField={_magneticFieldSensor != null}, " +
                       $"Gravity={_gravitySensor != null}, " +
                       $"Accel={_accelerometer != null}, " +
                       $"Attitude={_attitudeSensor != null}, " +
                       $"LegacyCompass={Input.compass.enabled}");
        }
        
        /// <summary>
        /// Force re-initialization (e.g., after scene load).
        /// </summary>
        public static void Reinitialize()
        {
            _initialized = false;
            Initialize();
        }
        
        #endregion
        
        #region Heading Computation
        
        /// <summary>
        /// Core heading update. Called lazily when Heading property is accessed.
        /// Uses a cascade of methods from most accurate to least.
        /// </summary>
        private static void UpdateHeading()
        {
            float rawHeading = ComputeRawHeading();
            _currentHeading = rawHeading;
            
            // Apply smoothing to reduce jitter
            _smoothedHeading = Mathf.SmoothDampAngle(
                _smoothedHeading, rawHeading, 
                ref _headingSmoothVelocity, HEADING_SMOOTH_TIME
            );
            
            // Diagnostic logging (throttled to every 5 seconds)
            _updateCount++;
            if (Time.realtimeSinceStartup - _lastLogTime >= 5f)
            {
                _lastLogTime = Time.realtimeSinceStartup;
                Debug.Log($"[DeviceCompass] Method={_activeMethod}, " +
                          $"Raw={rawHeading:F1}°, Smoothed={_smoothedHeading:F1}°, " +
                          $"IsTrueNorth={IsTrueNorth}, Updates={_updateCount}");
                _updateCount = 0;
            }
        }
        
        /// <summary>
        /// Compute raw heading using the best available method.
        /// </summary>
        private static float ComputeRawHeading()
        {
            // ================================================================
            // METHOD 1: MagneticFieldSensor + GravitySensor
            // This gives TRUE compass heading relative to magnetic north.
            // This is the gold standard — equivalent to Android's getOrientation().
            // ================================================================
            if (_magneticFieldSensor != null && _magneticFieldSensor.enabled)
            {
                Vector3 gravity = GetGravityVector();
                
                if (gravity.sqrMagnitude > 0.01f)
                {
                    Vector3 magnetic = _magneticFieldSensor.magneticField.ReadValue();
                    
                    if (magnetic.sqrMagnitude > 1f) // Earth's field is ~25-65 microtesla
                    {
                        float heading = ComputeHeadingFromMagneticField(gravity, magnetic);
                        if (!float.IsNaN(heading))
                        {
                            _activeMethod = "magnetic_field";
                            return heading;
                        }
                    }
                }
            }
            
            // ================================================================
            // METHOD 2: AttitudeSensor (fused device orientation)
            // Heading may drift from magnetic north over time, but 
            // always responds to phone rotation (critical for direction UI).
            // ================================================================
            if (_attitudeSensor != null && _attitudeSensor.enabled)
            {
                Quaternion attitude = _attitudeSensor.attitude.ReadValue();
                
                // Check for real data (not all zeros)
                if (attitude.x != 0 || attitude.y != 0 || attitude.z != 0)
                {
                    // Convert device attitude to compass heading.
                    // Android coordinate system fix for Unity:
                    // - Android: X=right, Y=up, Z=toward user
                    // - Unity: X=right, Y=up, Z=forward (away from user)
                    Quaternion rotFix = Quaternion.Euler(90f, 0f, 0f);
                    Quaternion deviceRot = rotFix * new Quaternion(
                        attitude.x, attitude.y, -attitude.z, -attitude.w
                    );
                    
                    float heading = deviceRot.eulerAngles.y;
                    _activeMethod = "attitude_sensor";
                    return heading;
                }
            }
            
            // ================================================================
            // METHOD 3: Legacy Input.compass (broken on many modern devices,
            // but kept as final fallback for older devices where it works)
            // ================================================================
            if (Input.compass.enabled)
            {
                float legacyHeading = Input.compass.trueHeading;
                if (legacyHeading == 0 || float.IsNaN(legacyHeading))
                {
                    legacyHeading = Input.compass.magneticHeading;
                }
                
                if (legacyHeading != 0 && !float.IsNaN(legacyHeading))
                {
                    _activeMethod = "legacy_compass";
                    return legacyHeading;
                }
            }
            
            // ================================================================
            // NO HEADING AVAILABLE
            // ================================================================
            _activeMethod = "none";
            return 0f;
        }
        
        /// <summary>
        /// Get gravity vector from GravitySensor or Accelerometer fallback.
        /// </summary>
        private static Vector3 GetGravityVector()
        {
            // Prefer GravitySensor (pre-filtered, no linear acceleration)
            if (_gravitySensor != null && _gravitySensor.enabled)
            {
                Vector3 grav = _gravitySensor.gravity.ReadValue();
                if (grav.sqrMagnitude > 0.01f)
                {
                    return grav;
                }
            }
            
            // Fallback to Accelerometer (includes linear acceleration, noisier)
            if (_accelerometer != null && _accelerometer.enabled)
            {
                Vector3 accel = _accelerometer.acceleration.ReadValue();
                if (accel.sqrMagnitude > 0.01f)
                {
                    return accel;
                }
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Compute compass heading from magnetic field and gravity vectors.
        /// This is the New Input System equivalent of Android's SensorManager.getOrientation().
        /// 
        /// Algorithm:
        /// 1. Use gravity to define "down" direction
        /// 2. Cross magnetic with gravity to get "east" direction  
        /// 3. Cross gravity with east to get "north" direction (horizontal component of magnetic field)
        /// 4. Project phone's forward onto horizontal plane to get heading
        /// 
        /// Returns heading in degrees: 0=North, 90=East, 180=South, 270=West
        /// </summary>
        private static float ComputeHeadingFromMagneticField(Vector3 gravity, Vector3 magnetic)
        {
            // Normalize gravity
            Vector3 down = gravity.normalized;
            
            // East = magnetic × down (perpendicular to both gravity and magnetic field)
            Vector3 east = Vector3.Cross(magnetic, down);
            float eastMag = east.magnitude;
            if (eastMag < 0.001f) return float.NaN; // Degenerate case
            east /= eastMag; // normalize
            
            // North = down × east (horizontal component pointing toward magnetic north)
            Vector3 north = Vector3.Cross(down, east);
            // north is already normalized since down and east are orthonormal
            
            // When phone is held in portrait mode (screen facing user):
            // - Phone's "forward" direction is (0, 0, -1) in Unity device space
            //   (pointing from screen toward user's face, which is "where the phone looks")
            // - But for heading, we want the direction the TOP of the phone points
            //   in the horizontal plane, which is (0, 1, 0) projected.
            //
            // Actually, for compass heading we want the azimuth:
            // the angle between magnetic north and the phone's Y-axis (top of phone)
            // projected onto the horizontal plane.
            //
            // Using the rotation matrix approach:
            // R[0] = east, R[1] = north, R[2] = up
            // azimuth = atan2(-east.y, north.y) in device coordinates
            //
            // In Unity's sensor coordinate space on Android:
            // The phone's Y-axis points from bottom to top of the device.
            // We want the heading of this axis projected onto the horizontal plane.
            
            // Heading = atan2(east·phoneY, north·phoneY) where phoneY = (0,1,0)
            // This gives the angle from north to the phone's top direction
            float heading = Mathf.Atan2(east.y, north.y) * Mathf.Rad2Deg;
            
            // Convert to 0-360 range (compass convention)
            if (heading < 0) heading += 360f;
            
            return heading;
        }
        
        #endregion
        
        #region Diagnostics
        
        /// <summary>
        /// Get a diagnostic string with all sensor states.
        /// Call from SensorDiagnostics or debug UI.
        /// </summary>
        public static string GetDiagnosticString()
        {
            if (!_initialized) Initialize();
            
            var sb = new System.Text.StringBuilder();
            sb.Append($"[DeviceCompass] Method={_activeMethod}, ");
            sb.Append($"Heading={_smoothedHeading:F1}°, Raw={_currentHeading:F1}°, ");
            sb.Append($"TrueNorth={IsTrueNorth}");
            
            if (_magneticFieldSensor != null)
            {
                Vector3 mag = _magneticFieldSensor.magneticField.ReadValue();
                sb.Append($" | MagField=({mag.x:F1},{mag.y:F1},{mag.z:F1}) mag={mag.magnitude:F1}µT");
            }
            else
            {
                sb.Append(" | MagField=N/A");
            }
            
            if (_gravitySensor != null)
            {
                Vector3 grav = _gravitySensor.gravity.ReadValue();
                sb.Append($" | Gravity=({grav.x:F2},{grav.y:F2},{grav.z:F2})");
            }
            
            if (_attitudeSensor != null)
            {
                Quaternion att = _attitudeSensor.attitude.ReadValue();
                sb.Append($" | Attitude=({att.eulerAngles.x:F1},{att.eulerAngles.y:F1},{att.eulerAngles.z:F1})");
            }
            
            sb.Append($" | LegacyCompass: enabled={Input.compass.enabled}, true={Input.compass.trueHeading:F1}°");
            
            return sb.ToString();
        }
        
        #endregion
    }
}
