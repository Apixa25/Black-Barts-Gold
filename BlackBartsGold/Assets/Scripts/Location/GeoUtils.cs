// ============================================================================
// GeoUtils.cs
// Black Bart's Gold - Geospatial Utility Functions
// Path: Assets/Scripts/Location/GeoUtils.cs
// ============================================================================
// Static utility class for GPS calculations: distance, bearing, GPS↔AR
// position conversion, and batch operations on coin collections.
// Reference: BUILD-GUIDE.md Prompt 4.2
// ============================================================================

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using BlackBartsGold.Core;
using BlackBartsGold.Core.Models;

namespace BlackBartsGold.Location
{
    /// <summary>
    /// Static utility class for geospatial calculations.
    /// Includes Haversine distance, bearing, and GPS↔AR conversions.
    /// </summary>
    public static class GeoUtils
    {
        #region Constants
        
        /// <summary>
        /// Earth's radius in meters
        /// </summary>
        public const double EARTH_RADIUS_METERS = 6371000.0;
        
        /// <summary>
        /// Meters per degree of latitude (approximate)
        /// </summary>
        public const double METERS_PER_DEGREE_LAT = 111320.0;
        
        /// <summary>
        /// Degrees to radians multiplier
        /// </summary>
        public const double DEG_TO_RAD = Math.PI / 180.0;
        
        /// <summary>
        /// Radians to degrees multiplier
        /// </summary>
        public const double RAD_TO_DEG = 180.0 / Math.PI;
        
        #endregion
        
        #region Distance Calculations
        
        /// <summary>
        /// Calculate distance between two GPS coordinates using Haversine formula.
        /// Returns distance in meters.
        /// </summary>
        public static float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * DEG_TO_RAD;
            double dLon = (lon2 - lon1) * DEG_TO_RAD;
            
            double lat1Rad = lat1 * DEG_TO_RAD;
            double lat2Rad = lat2 * DEG_TO_RAD;
            
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return (float)(EARTH_RADIUS_METERS * c);
        }
        
        /// <summary>
        /// Calculate distance between two LocationData objects
        /// </summary>
        public static float CalculateDistance(LocationData from, LocationData to)
        {
            if (from == null || to == null) return float.MaxValue;
            return CalculateDistance(from.latitude, from.longitude, to.latitude, to.longitude);
        }
        
        /// <summary>
        /// Check if two points are within a certain radius
        /// </summary>
        public static bool IsWithinRadius(LocationData point1, LocationData point2, float radiusMeters)
        {
            return CalculateDistance(point1, point2) <= radiusMeters;
        }
        
        /// <summary>
        /// Check if a coin is within range of a location
        /// </summary>
        public static bool IsCoinInRange(Coin coin, LocationData playerLocation, float rangeMeters)
        {
            if (coin == null || playerLocation == null) return false;
            float distance = CalculateDistance(
                playerLocation.latitude, playerLocation.longitude,
                coin.latitude, coin.longitude
            );
            return distance <= rangeMeters;
        }
        
        #endregion
        
        #region Bearing Calculations
        
        /// <summary>
        /// Calculate bearing from one point to another.
        /// Returns bearing in degrees (0-360, 0=North, 90=East).
        /// </summary>
        public static float CalculateBearing(double fromLat, double fromLon, double toLat, double toLon)
        {
            double lat1 = fromLat * DEG_TO_RAD;
            double lat2 = toLat * DEG_TO_RAD;
            double dLon = (toLon - fromLon) * DEG_TO_RAD;
            
            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) -
                       Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
            
            double bearing = Math.Atan2(y, x) * RAD_TO_DEG;
            
            // Normalize to 0-360
            return (float)((bearing + 360) % 360);
        }
        
        /// <summary>
        /// Calculate bearing between two LocationData objects
        /// </summary>
        public static float CalculateBearing(LocationData from, LocationData to)
        {
            if (from == null || to == null) return 0f;
            return CalculateBearing(from.latitude, from.longitude, to.latitude, to.longitude);
        }
        
        /// <summary>
        /// Get cardinal direction from bearing
        /// </summary>
        public static string GetCardinalDirection(float bearing)
        {
            // Normalize bearing
            bearing = ((bearing % 360) + 360) % 360;
            
            if (bearing >= 337.5f || bearing < 22.5f) return "N";
            if (bearing >= 22.5f && bearing < 67.5f) return "NE";
            if (bearing >= 67.5f && bearing < 112.5f) return "E";
            if (bearing >= 112.5f && bearing < 157.5f) return "SE";
            if (bearing >= 157.5f && bearing < 202.5f) return "S";
            if (bearing >= 202.5f && bearing < 247.5f) return "SW";
            if (bearing >= 247.5f && bearing < 292.5f) return "W";
            return "NW";
        }
        
        /// <summary>
        /// Get full cardinal direction name
        /// </summary>
        public static string GetCardinalDirectionFull(float bearing)
        {
            string abbrev = GetCardinalDirection(bearing);
            return abbrev switch
            {
                "N" => "North",
                "NE" => "Northeast",
                "E" => "East",
                "SE" => "Southeast",
                "S" => "South",
                "SW" => "Southwest",
                "W" => "West",
                "NW" => "Northwest",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Calculate relative bearing (accounting for device heading)
        /// </summary>
        public static float CalculateRelativeBearing(float targetBearing, float deviceHeading)
        {
            float relative = targetBearing - deviceHeading;
            
            // Normalize to -180 to 180
            while (relative > 180) relative -= 360;
            while (relative < -180) relative += 360;
            
            return relative;
        }
        
        #endregion
        
        #region GPS to AR Conversion
        
        /// <summary>
        /// Convert GPS coordinates to AR world position relative to player.
        /// </summary>
        /// <param name="playerPos">Player's current GPS position (origin)</param>
        /// <param name="targetPos">Target GPS position to convert</param>
        /// <param name="heightAboveGround">Y position in AR space</param>
        /// <returns>Vector3 position in AR world space</returns>
        public static Vector3 GpsToArPosition(LocationData playerPos, LocationData targetPos, float heightAboveGround = 1.5f)
        {
            if (playerPos == null || targetPos == null)
            {
                return Vector3.zero;
            }
            
            // Calculate distance and bearing
            float distance = CalculateDistance(playerPos, targetPos);
            float bearing = CalculateBearing(playerPos, targetPos);
            
            // Convert bearing to radians
            float bearingRad = bearing * Mathf.Deg2Rad;
            
            // Calculate X (east-west) and Z (north-south) offsets
            // Unity coordinate system: +X is right (east), +Z is forward (north)
            float x = distance * Mathf.Sin(bearingRad);
            float z = distance * Mathf.Cos(bearingRad);
            
            return new Vector3(x, heightAboveGround, z);
        }
        
        /// <summary>
        /// Convert GPS coordinates to AR position with coin data
        /// </summary>
        public static Vector3 CoinToArPosition(Coin coin, LocationData playerPos)
        {
            if (coin == null || playerPos == null) return Vector3.zero;
            
            LocationData coinLocation = new LocationData(coin.latitude, coin.longitude);
            return GpsToArPosition(playerPos, coinLocation, coin.heightOffset);
        }
        
        /// <summary>
        /// Convert AR world position back to GPS coordinates.
        /// </summary>
        /// <param name="playerPos">Player's current GPS position (origin)</param>
        /// <param name="arPosition">Position in AR world space</param>
        /// <returns>GPS coordinates as LocationData</returns>
        public static LocationData ArPositionToGps(LocationData playerPos, Vector3 arPosition)
        {
            if (playerPos == null) return null;
            
            // Calculate distance from AR position
            float distance = new Vector2(arPosition.x, arPosition.z).magnitude;
            
            // Calculate bearing from AR position
            // atan2(x, z) gives angle from north (z-axis)
            float bearing = Mathf.Atan2(arPosition.x, arPosition.z) * Mathf.Rad2Deg;
            
            // Normalize bearing to 0-360
            if (bearing < 0) bearing += 360f;
            
            // Calculate GPS offset
            // Latitude changes with north-south movement
            // Longitude changes with east-west movement (adjusted for latitude)
            double latOffset = distance * Math.Cos(bearing * DEG_TO_RAD) / METERS_PER_DEGREE_LAT;
            double lonOffset = distance * Math.Sin(bearing * DEG_TO_RAD) / 
                              (METERS_PER_DEGREE_LAT * Math.Cos(playerPos.latitude * DEG_TO_RAD));
            
            return new LocationData(
                playerPos.latitude + latOffset,
                playerPos.longitude + lonOffset
            );
        }
        
        /// <summary>
        /// Create a GPS location at a specific offset from origin
        /// </summary>
        public static LocationData CreateOffsetLocation(LocationData origin, float metersNorth, float metersEast)
        {
            if (origin == null) return null;
            
            double latOffset = metersNorth / METERS_PER_DEGREE_LAT;
            double lonOffset = metersEast / (METERS_PER_DEGREE_LAT * Math.Cos(origin.latitude * DEG_TO_RAD));
            
            return new LocationData(
                origin.latitude + latOffset,
                origin.longitude + lonOffset
            );
        }
        
        #endregion
        
        #region Proximity Zones
        
        /// <summary>
        /// Get proximity zone for a distance
        /// </summary>
        public static ProximityZone GetProximityZone(float distanceMeters)
        {
            if (distanceMeters <= 5f) return ProximityZone.Collectible;
            if (distanceMeters <= 15f) return ProximityZone.Near;
            if (distanceMeters <= 30f) return ProximityZone.Medium;
            if (distanceMeters <= 50f) return ProximityZone.Far;
            return ProximityZone.OutOfRange;
        }
        
        /// <summary>
        /// Get proximity zone for a coin relative to player
        /// </summary>
        public static ProximityZone GetCoinProximityZone(Coin coin, LocationData playerPos)
        {
            if (coin == null || playerPos == null) return ProximityZone.OutOfRange;
            
            float distance = CalculateDistance(
                playerPos.latitude, playerPos.longitude,
                coin.latitude, coin.longitude
            );
            return GetProximityZone(distance);
        }
        
        /// <summary>
        /// Get description for proximity zone
        /// </summary>
        public static string GetProximityDescription(ProximityZone zone)
        {
            return zone switch
            {
                ProximityZone.Collectible => "In range! Tap to collect!",
                ProximityZone.Near => "Almost there!",
                ProximityZone.Medium => "Getting warmer...",
                ProximityZone.Far => "Keep searching...",
                ProximityZone.OutOfRange => "Too far away",
                _ => "Unknown"
            };
        }
        
        #endregion
        
        #region Batch Operations
        
        /// <summary>
        /// Filter coins by maximum distance from player
        /// </summary>
        public static List<Coin> FilterCoinsByDistance(List<Coin> coins, LocationData playerPos, float maxDistance)
        {
            if (coins == null || playerPos == null) return new List<Coin>();
            
            return coins.Where(coin => 
                CalculateDistance(playerPos.latitude, playerPos.longitude, 
                                  coin.latitude, coin.longitude) <= maxDistance
            ).ToList();
        }
        
        /// <summary>
        /// Sort coins by distance from player (nearest first)
        /// </summary>
        public static List<Coin> SortCoinsByDistance(List<Coin> coins, LocationData playerPos)
        {
            if (coins == null || playerPos == null) return new List<Coin>();
            
            return coins.OrderBy(coin => 
                CalculateDistance(playerPos.latitude, playerPos.longitude,
                                  coin.latitude, coin.longitude)
            ).ToList();
        }
        
        /// <summary>
        /// Get the nearest coin to player
        /// </summary>
        public static Coin GetNearestCoin(List<Coin> coins, LocationData playerPos)
        {
            if (coins == null || coins.Count == 0 || playerPos == null) return null;
            
            return coins.OrderBy(coin => 
                CalculateDistance(playerPos.latitude, playerPos.longitude,
                                  coin.latitude, coin.longitude)
            ).FirstOrDefault();
        }
        
        /// <summary>
        /// Get coins within collectible range
        /// </summary>
        public static List<Coin> GetCollectibleCoins(List<Coin> coins, LocationData playerPos)
        {
            return FilterCoinsByDistance(coins, playerPos, 5f); // 5m collection range
        }
        
        /// <summary>
        /// Update distance and bearing for all coins
        /// </summary>
        public static void UpdateCoinDistances(List<Coin> coins, LocationData playerPos)
        {
            if (coins == null || playerPos == null) return;
            
            foreach (var coin in coins)
            {
                coin.distanceFromPlayer = CalculateDistance(
                    playerPos.latitude, playerPos.longitude,
                    coin.latitude, coin.longitude
                );
                coin.bearingFromPlayer = CalculateBearing(
                    playerPos.latitude, playerPos.longitude,
                    coin.latitude, coin.longitude
                );
                coin.isInRange = coin.distanceFromPlayer <= 5f;
            }
        }
        
        #endregion
        
        #region Formatting
        
        /// <summary>
        /// Format distance for display
        /// </summary>
        public static string FormatDistance(float meters)
        {
            if (meters < 1f)
            {
                return $"{(meters * 100):F0}cm";
            }
            if (meters < 1000f)
            {
                return $"{meters:F0}m";
            }
            return $"{(meters / 1000f):F1}km";
        }
        
        /// <summary>
        /// Format bearing for display
        /// </summary>
        public static string FormatBearing(float bearing)
        {
            return $"{bearing:F0}° {GetCardinalDirection(bearing)}";
        }
        
        /// <summary>
        /// Format coordinates for display
        /// </summary>
        public static string FormatCoordinates(double latitude, double longitude, int decimals = 4)
        {
            string latDir = latitude >= 0 ? "N" : "S";
            string lonDir = longitude >= 0 ? "E" : "W";
            
            return $"{Math.Abs(latitude).ToString($"F{decimals}")}{latDir}, " +
                   $"{Math.Abs(longitude).ToString($"F{decimals}")}{lonDir}";
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Check if coordinates are valid
        /// </summary>
        public static bool AreCoordinatesValid(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 &&
                   longitude >= -180 && longitude <= 180 &&
                   (latitude != 0 || longitude != 0); // Not null island
        }
        
        /// <summary>
        /// Check if a location is valid
        /// </summary>
        public static bool IsLocationValid(LocationData location)
        {
            if (location == null) return false;
            return AreCoordinatesValid(location.latitude, location.longitude);
        }
        
        #endregion
    }
}
