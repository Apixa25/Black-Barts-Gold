// ============================================================================
// LocationData.cs
// Black Bart's Gold - Location Data Model
// Path: Assets/Scripts/Core/Models/LocationData.cs
// ============================================================================
// Represents GPS location data for positioning in the real world.
// Used for player position, coin positions, and distance calculations.
// ============================================================================

using System;
using UnityEngine;

namespace BlackBartsGold.Core.Models
{
    /// <summary>
    /// Data model representing a GPS location.
    /// Serializable for JSON persistence.
    /// </summary>
    [Serializable]
    public class LocationData
    {
        #region Coordinates
        
        /// <summary>
        /// Latitude in decimal degrees
        /// Range: -90 to 90
        /// </summary>
        public double latitude;
        
        /// <summary>
        /// Longitude in decimal degrees
        /// Range: -180 to 180
        /// </summary>
        public double longitude;
        
        /// <summary>
        /// Altitude in meters above sea level
        /// </summary>
        public float altitude;
        
        #endregion
        
        #region Accuracy
        
        /// <summary>
        /// Horizontal accuracy in meters
        /// Lower = more accurate
        /// </summary>
        public float horizontalAccuracy;
        
        /// <summary>
        /// Vertical accuracy in meters
        /// </summary>
        public float verticalAccuracy;
        
        /// <summary>
        /// Timestamp of this reading (ISO 8601)
        /// </summary>
        public string timestamp;
        
        #endregion
        
        #region Movement
        
        /// <summary>
        /// Speed in meters per second (if available)
        /// </summary>
        public float speed;
        
        /// <summary>
        /// Heading/bearing in degrees (0-360, 0=North)
        /// </summary>
        public float heading;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public LocationData() 
        {
            timestamp = DateTime.UtcNow.ToString("o");
        }
        
        /// <summary>
        /// Create from lat/lng
        /// </summary>
        public LocationData(double lat, double lng)
        {
            latitude = lat;
            longitude = lng;
            timestamp = DateTime.UtcNow.ToString("o");
        }
        
        /// <summary>
        /// Create from Unity LocationInfo
        /// </summary>
        public LocationData(LocationInfo info)
        {
            latitude = info.latitude;
            longitude = info.longitude;
            altitude = info.altitude;
            horizontalAccuracy = info.horizontalAccuracy;
            verticalAccuracy = info.verticalAccuracy;
            timestamp = DateTime.UtcNow.ToString("o");
        }
        
        #endregion
        
        #region Validity
        
        /// <summary>
        /// Is this a valid location (non-zero coordinates)?
        /// </summary>
        public bool IsValid()
        {
            return latitude != 0 || longitude != 0;
        }
        
        /// <summary>
        /// Get accuracy level
        /// </summary>
        public GPSAccuracy GetAccuracyLevel()
        {
            if (horizontalAccuracy <= 0) return GPSAccuracy.None;
            if (horizontalAccuracy <= 10) return GPSAccuracy.High;
            if (horizontalAccuracy <= 50) return GPSAccuracy.Medium;
            return GPSAccuracy.Low;
        }
        
        /// <summary>
        /// Is accuracy good enough for gameplay?
        /// </summary>
        public bool IsAccuracyAcceptable()
        {
            // Accept medium or high accuracy
            return horizontalAccuracy > 0 && horizontalAccuracy <= 50;
        }
        
        /// <summary>
        /// Get age of this location reading
        /// </summary>
        public TimeSpan GetAge()
        {
            if (DateTime.TryParse(timestamp, out DateTime ts))
            {
                return DateTime.UtcNow - ts;
            }
            return TimeSpan.MaxValue;
        }
        
        /// <summary>
        /// Is this location reading fresh (less than X seconds old)?
        /// </summary>
        public bool IsFresh(int maxAgeSeconds = 30)
        {
            return GetAge().TotalSeconds <= maxAgeSeconds;
        }
        
        #endregion
        
        #region Distance Calculations
        
        /// <summary>
        /// Calculate distance to another location using Haversine formula
        /// Returns distance in meters
        /// </summary>
        public float DistanceTo(LocationData other)
        {
            return DistanceTo(other.latitude, other.longitude);
        }
        
        /// <summary>
        /// Calculate distance to coordinates
        /// Returns distance in meters
        /// </summary>
        public float DistanceTo(double targetLat, double targetLng)
        {
            // Haversine formula
            const double R = 6371000; // Earth's radius in meters
            
            double lat1 = latitude * Math.PI / 180;
            double lat2 = targetLat * Math.PI / 180;
            double deltaLat = (targetLat - latitude) * Math.PI / 180;
            double deltaLng = (targetLng - longitude) * Math.PI / 180;
            
            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
            
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return (float)(R * c);
        }
        
        /// <summary>
        /// Calculate bearing/heading to another location
        /// Returns bearing in degrees (0-360, 0=North)
        /// </summary>
        public float BearingTo(LocationData other)
        {
            return BearingTo(other.latitude, other.longitude);
        }
        
        /// <summary>
        /// Calculate bearing to coordinates
        /// </summary>
        public float BearingTo(double targetLat, double targetLng)
        {
            double lat1 = latitude * Math.PI / 180;
            double lat2 = targetLat * Math.PI / 180;
            double lng1 = longitude * Math.PI / 180;
            double lng2 = targetLng * Math.PI / 180;
            
            double y = Math.Sin(lng2 - lng1) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) -
                       Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(lng2 - lng1);
            
            double bearing = Math.Atan2(y, x) * 180 / Math.PI;
            
            // Normalize to 0-360
            return (float)((bearing + 360) % 360);
        }
        
        /// <summary>
        /// Check if another location is within radius
        /// </summary>
        public bool IsWithinRadius(LocationData other, float radiusMeters)
        {
            return DistanceTo(other) <= radiusMeters;
        }
        
        /// <summary>
        /// Get proximity zone based on distance
        /// Reference: Docs/prize-finder-details.md
        /// </summary>
        public ProximityZone GetProximityZone(float distanceMeters)
        {
            if (distanceMeters <= 5) return ProximityZone.Collectible;
            if (distanceMeters <= 15) return ProximityZone.Near;
            if (distanceMeters <= 30) return ProximityZone.Medium;
            if (distanceMeters <= 50) return ProximityZone.Far;
            return ProximityZone.OutOfRange;
        }
        
        #endregion
        
        #region Direction Helpers
        
        /// <summary>
        /// Get cardinal direction from bearing
        /// </summary>
        public static string BearingToCardinal(float bearing)
        {
            string[] cardinals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };
            int index = (int)Math.Round(bearing / 45);
            return cardinals[index];
        }
        
        /// <summary>
        /// Get cardinal direction to another location
        /// </summary>
        public string GetCardinalDirectionTo(LocationData other)
        {
            float bearing = BearingTo(other);
            return BearingToCardinal(bearing);
        }
        
        #endregion
        
        #region Unity Conversion
        
        /// <summary>
        /// Convert to AR position relative to a reference point
        /// Reference point is typically the player's position
        /// </summary>
        public Vector3 ToARPosition(LocationData reference, float heightAboveGround = 1.5f)
        {
            float distance = reference.DistanceTo(this);
            float bearing = reference.BearingTo(this);
            
            // Convert bearing to radians
            float bearingRad = bearing * Mathf.Deg2Rad;
            
            // Calculate X (east-west) and Z (north-south) offsets
            // Unity: +X is right, +Z is forward (north)
            float x = distance * Mathf.Sin(bearingRad);
            float z = distance * Mathf.Cos(bearingRad);
            
            return new Vector3(x, heightAboveGround, z);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Create a copy of this location
        /// </summary>
        public LocationData Clone()
        {
            return new LocationData
            {
                latitude = this.latitude,
                longitude = this.longitude,
                altitude = this.altitude,
                horizontalAccuracy = this.horizontalAccuracy,
                verticalAccuracy = this.verticalAccuracy,
                timestamp = this.timestamp,
                speed = this.speed,
                heading = this.heading
            };
        }
        
        /// <summary>
        /// Format as coordinates string
        /// </summary>
        public string ToCoordinateString(int decimals = 6)
        {
            return $"{latitude.ToString($"F{decimals}")}, {longitude.ToString($"F{decimals}")}";
        }
        
        /// <summary>
        /// Debug string representation
        /// </summary>
        public override string ToString()
        {
            return $"Location({latitude:F4}, {longitude:F4}) ±{horizontalAccuracy}m";
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create from Unity's Input.location
        /// </summary>
        public static LocationData FromUnityLocation()
        {
            if (Input.location.status != LocationServiceStatus.Running)
            {
                return null;
            }
            
            return new LocationData(Input.location.lastData);
        }
        
        /// <summary>
        /// Create a test location (San Francisco)
        /// </summary>
        public static LocationData CreateTestLocation()
        {
            return new LocationData
            {
                latitude = 37.7749,
                longitude = -122.4194,
                altitude = 10f,
                horizontalAccuracy = 5f,
                verticalAccuracy = 3f,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }
        
        /// <summary>
        /// Create a location at offset from another
        /// Useful for creating test coins near player
        /// </summary>
        public static LocationData CreateAtOffset(LocationData origin, float metersNorth, float metersEast)
        {
            // Approximate conversion (works well for short distances)
            // 1 degree latitude ≈ 111,320 meters
            // 1 degree longitude ≈ 111,320 * cos(latitude) meters
            
            double latOffset = metersNorth / 111320.0;
            double lngOffset = metersEast / (111320.0 * Math.Cos(origin.latitude * Math.PI / 180));
            
            return new LocationData
            {
                latitude = origin.latitude + latOffset,
                longitude = origin.longitude + lngOffset,
                altitude = origin.altitude,
                horizontalAccuracy = origin.horizontalAccuracy,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }
        
        #endregion
    }
}
