// ============================================================================
// LocationSimulator.cs
// Black Bart's Gold - Editor Tool for GPS Simulation
// Path: Assets/Editor/LocationSimulator.cs
// ============================================================================
// Provides an Editor window to simulate GPS location for testing.
// Features: Set location, simulate walking, load preset locations.
// ============================================================================

using UnityEngine;
using UnityEditor;
using BlackBartsGold.Location;

public class LocationSimulator : EditorWindow
{
    // Current simulated position
    private static double latitude = 37.7749;   // San Francisco
    private static double longitude = -122.4194;
    private static float heading = 0f;
    
    // Walking simulation
    private static bool isWalking = false;
    private static float walkSpeed = 1.4f; // m/s (average walking speed)
    private static float walkDirection = 0f; // degrees from north
    
    // Preset locations
    private static readonly (string name, double lat, double lng)[] presets = new[]
    {
        ("San Francisco", 37.7749, -122.4194),
        ("New York", 40.7128, -74.0060),
        ("Los Angeles", 34.0522, -118.2437),
        ("Seattle", 47.6062, -122.3321),
        ("Austin", 30.2672, -97.7431),
        ("Miami", 25.7617, -80.1918),
        ("Denver", 39.7392, -104.9903),
        ("Chicago", 41.8781, -87.6298),
    };
    
    private int selectedPreset = 0;
    private double lastUpdateTime = 0;
    
    [MenuItem("Tools/Black Bart's Gold/Location Simulator")]
    public static void ShowWindow()
    {
        var window = GetWindow<LocationSimulator>("Location Simulator");
        window.minSize = new Vector2(300, 400);
    }
    
    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }
    
    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        isWalking = false;
    }
    
    private void OnEditorUpdate()
    {
        if (!Application.isPlaying) return;
        
        // Simulate walking
        if (isWalking)
        {
            double deltaTime = EditorApplication.timeSinceStartup - lastUpdateTime;
            if (deltaTime >= 0.5) // Update every 0.5 seconds
            {
                SimulateWalkStep(deltaTime);
                lastUpdateTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }
    }
    
    private void SimulateWalkStep(double deltaTime)
    {
        // Calculate distance moved
        double distanceMeters = walkSpeed * deltaTime;
        
        // Convert to degrees (rough approximation)
        // 1 degree latitude ≈ 111,000 meters
        // 1 degree longitude ≈ 111,000 * cos(latitude) meters
        double latChange = (distanceMeters * Mathf.Cos(walkDirection * Mathf.Deg2Rad)) / 111000.0;
        double lngChange = (distanceMeters * Mathf.Sin(walkDirection * Mathf.Deg2Rad)) / (111000.0 * Mathf.Cos((float)latitude * Mathf.Deg2Rad));
        
        latitude += latChange;
        longitude += lngChange;
        
        // Update GPSManager
        UpdateGPSManager();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("GPS Location Simulator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Simulate GPS location for testing without deploying to device.", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Status
        bool isPlaying = Application.isPlaying;
        EditorGUILayout.LabelField("Status", isPlaying ? "Playing - Simulation Active" : "Not Playing");
        
        if (!isPlaying)
        {
            EditorGUILayout.HelpBox("Press Play to enable location simulation.", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        // Preset locations
        EditorGUILayout.LabelField("Preset Locations", EditorStyles.boldLabel);
        int newPreset = EditorGUILayout.Popup("Location", selectedPreset, GetPresetNames());
        if (newPreset != selectedPreset)
        {
            selectedPreset = newPreset;
            latitude = presets[selectedPreset].lat;
            longitude = presets[selectedPreset].lng;
            UpdateGPSManager();
        }
        
        EditorGUILayout.Space(10);
        
        // Manual coordinates
        EditorGUILayout.LabelField("Manual Coordinates", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        latitude = EditorGUILayout.DoubleField("Latitude", latitude);
        longitude = EditorGUILayout.DoubleField("Longitude", longitude);
        if (EditorGUI.EndChangeCheck() && isPlaying)
        {
            UpdateGPSManager();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("Set Location Now"))
        {
            UpdateGPSManager();
        }
        
        EditorGUILayout.Space(10);
        
        // Walking simulation
        EditorGUILayout.LabelField("Walking Simulation", EditorStyles.boldLabel);
        
        walkSpeed = EditorGUILayout.Slider("Walk Speed (m/s)", walkSpeed, 0.5f, 5f);
        walkDirection = EditorGUILayout.Slider("Direction (degrees)", walkDirection, 0f, 360f);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Direction:", GUILayout.Width(60));
        if (GUILayout.Button("N", GUILayout.Width(30))) walkDirection = 0;
        if (GUILayout.Button("E", GUILayout.Width(30))) walkDirection = 90;
        if (GUILayout.Button("S", GUILayout.Width(30))) walkDirection = 180;
        if (GUILayout.Button("W", GUILayout.Width(30))) walkDirection = 270;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        GUI.enabled = isPlaying;
        if (isWalking)
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Stop Walking", GUILayout.Height(30)))
            {
                isWalking = false;
            }
        }
        else
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Start Walking", GUILayout.Height(30)))
            {
                isWalking = true;
                lastUpdateTime = EditorApplication.timeSinceStartup;
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        EditorGUILayout.Space(10);
        
        // Compass heading
        EditorGUILayout.LabelField("Compass Heading", EditorStyles.boldLabel);
        heading = EditorGUILayout.Slider("Heading (degrees)", heading, 0f, 360f);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("N")) heading = 0;
        if (GUILayout.Button("NE")) heading = 45;
        if (GUILayout.Button("E")) heading = 90;
        if (GUILayout.Button("SE")) heading = 135;
        if (GUILayout.Button("S")) heading = 180;
        if (GUILayout.Button("SW")) heading = 225;
        if (GUILayout.Button("W")) heading = 270;
        if (GUILayout.Button("NW")) heading = 315;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Quick actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Move 10m North"))
        {
            latitude += 10.0 / 111000.0;
            UpdateGPSManager();
        }
        
        if (GUILayout.Button("Move 10m Toward Target Coin"))
        {
            MoveTowardTargetCoin(10);
        }
        
        EditorGUILayout.Space(10);
        
        // Current info
        EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Lat: {latitude:F6}");
        EditorGUILayout.LabelField($"Lng: {longitude:F6}");
        EditorGUILayout.LabelField($"Walking: {(isWalking ? "Yes" : "No")}");
        
        if (isPlaying && GPSManager.Exists)
        {
            var loc = GPSManager.Instance.CurrentLocation;
            if (loc != null)
            {
                EditorGUILayout.LabelField($"GPSManager: {loc.latitude:F6}, {loc.longitude:F6}");
            }
        }
    }
    
    private string[] GetPresetNames()
    {
        string[] names = new string[presets.Length];
        for (int i = 0; i < presets.Length; i++)
        {
            names[i] = presets[i].name;
        }
        return names;
    }
    
    private void UpdateGPSManager()
    {
        if (!Application.isPlaying) return;
        
        if (GPSManager.Exists)
        {
            GPSManager.Instance.SetSimulatedLocation(latitude, longitude);
            Debug.Log($"[LocationSimulator] Set location: {latitude:F6}, {longitude:F6}");
        }
        else
        {
            Debug.LogWarning("[LocationSimulator] GPSManager not found!");
        }
    }
    
    private void MoveTowardTargetCoin(float meters)
    {
        if (!Application.isPlaying) return;
        
        // Get target coin from CoinManager
        if (BlackBartsGold.AR.CoinManager.Exists && BlackBartsGold.AR.CoinManager.Instance.HasTarget)
        {
            var target = BlackBartsGold.AR.CoinManager.Instance.TargetCoinData;
            if (target != null)
            {
                // Calculate bearing to target
                float bearing = GeoUtils.CalculateBearing(latitude, longitude, target.latitude, target.longitude);
                
                // Move in that direction
                double latChange = (meters * Mathf.Cos(bearing * Mathf.Deg2Rad)) / 111000.0;
                double lngChange = (meters * Mathf.Sin(bearing * Mathf.Deg2Rad)) / (111000.0 * Mathf.Cos((float)latitude * Mathf.Deg2Rad));
                
                latitude += latChange;
                longitude += lngChange;
                
                UpdateGPSManager();
                
                Debug.Log($"[LocationSimulator] Moved {meters}m toward target coin (bearing: {bearing:F0}°)");
            }
        }
        else
        {
            Debug.LogWarning("[LocationSimulator] No target coin set!");
        }
    }
    
    // Static methods for quick access
    [MenuItem("Tools/Black Bart's Gold/Move 10m North %#n")]
    public static void MoveNorth()
    {
        latitude += 10.0 / 111000.0;
        if (Application.isPlaying && GPSManager.Exists)
        {
            GPSManager.Instance.SetSimulatedLocation(latitude, longitude);
        }
    }
    
    [MenuItem("Tools/Black Bart's Gold/Move 10m South %#s")]
    public static void MoveSouth()
    {
        latitude -= 10.0 / 111000.0;
        if (Application.isPlaying && GPSManager.Exists)
        {
            GPSManager.Instance.SetSimulatedLocation(latitude, longitude);
        }
    }
}
