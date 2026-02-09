// ============================================================================
// MapboxService.cs
// Black Bart's Gold - Mapbox Map Tile Service
// Path: Assets/Scripts/Core/MapboxService.cs
// Created: 2026-01-27 - Real map integration!
// ============================================================================
// Fetches real map tiles from Mapbox Static Images API.
// This gives us Pokemon GO-style real maps!
// ============================================================================

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Service for fetching Mapbox static map tiles.
    /// Provides real-world map imagery for mini-map and full map.
    /// </summary>
    public class MapboxService : MonoBehaviour
    {
        #region Singleton
        
        private static MapboxService _instance;
        public static MapboxService Instance => _instance;
        public static bool Exists => _instance != null;
        
        #endregion
        
        #region Configuration
        
        [Header("Mapbox Configuration")]
        [Tooltip("Set in Inspector or leave empty to use MAPBOX_ACCESS_TOKEN from environment. Never commit real tokens.")]
        [SerializeField] private string accessToken = "";
        
        [Header("Map Style")]
        [Tooltip("Mapbox style ID - see mapbox.com/studio for options")]
        [SerializeField] private MapStyle mapStyle = MapStyle.Custom; // Custom high-contrast style
        [Tooltip("Custom style ID when MapStyle.Custom is selected - format: username/style-id")]
        [SerializeField] private string customStyleId = "stevensills2/cmld26kz2000301st3vnmfft9";
        
        [Header("Cache Settings")]
        [SerializeField] private bool enableCache = true;
        [SerializeField] private int maxCacheSize = 50;
        [SerializeField] private float cacheExpirySeconds = 300f; // 5 minutes
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = true;
        
        /// <summary>Token from Inspector, or MAPBOX_ACCESS_TOKEN env var. Keeps secrets out of source control.</summary>
        private string EffectiveAccessToken =>
            !string.IsNullOrEmpty(accessToken) ? accessToken : (Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN") ?? "");
        
        #endregion
        
        #region Map Styles
        
        public enum MapStyle
        {
            Custom,         // stevensills2/cmld26kz2000301st3vnmfft9 - high contrast, better building/road visibility
            Streets,        // mapbox/streets-v12
            Outdoors,       // mapbox/outdoors-v12
            Light,          // mapbox/light-v11
            Dark,           // mapbox/dark-v11
            Satellite,      // mapbox/satellite-v9
            SatelliteStreets, // mapbox/satellite-streets-v12
            NavigationDay,  // mapbox/navigation-day-v1
            NavigationNight // mapbox/navigation-night-v1
        }
        
        private string GetStyleId(MapStyle style)
        {
            if (style == MapStyle.Custom && !string.IsNullOrEmpty(customStyleId))
                return customStyleId;
            return style switch
            {
                MapStyle.Streets => "mapbox/streets-v12",
                MapStyle.Outdoors => "mapbox/outdoors-v12",
                MapStyle.Light => "mapbox/light-v11",
                MapStyle.Dark => "mapbox/dark-v11",
                MapStyle.Satellite => "mapbox/satellite-v9",
                MapStyle.SatelliteStreets => "mapbox/satellite-streets-v12",
                MapStyle.NavigationDay => "mapbox/navigation-day-v1",
                MapStyle.NavigationNight => "mapbox/navigation-night-v1",
                _ => "mapbox/streets-v12"
            };
        }
        
        #endregion
        
        #region Cache
        
        private class CachedTile
        {
            public Texture2D texture;
            public float timestamp;
            public string key;
        }
        
        private Dictionary<string, CachedTile> _tileCache = new Dictionary<string, CachedTile>();
        private Queue<string> _cacheOrder = new Queue<string>();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Log("MapboxService initialized!");
            // Diagnostic: always log style at startup (visible in ADB logcat)
            string styleId = GetStyleId(mapStyle);
            Debug.Log($"[Mapbox] STARTUP mapStyle={mapStyle} styleId={styleId} customStyleId={customStyleId}");
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            // Clean up cached textures
            foreach (var tile in _tileCache.Values)
            {
                if (tile.texture != null)
                {
                    Destroy(tile.texture);
                }
            }
            _tileCache.Clear();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get a static map tile centered on the given coordinates.
        /// </summary>
        /// <param name="latitude">Center latitude</param>
        /// <param name="longitude">Center longitude</param>
        /// <param name="zoom">Zoom level (0-22, default 15)</param>
        /// <param name="width">Image width in pixels</param>
        /// <param name="height">Image height in pixels</param>
        /// <param name="callback">Callback with the texture (null on error)</param>
        /// <param name="bearing">Map rotation (0 = north up)</param>
        public void GetMapTile(
            double latitude, 
            double longitude, 
            int zoom, 
            int width, 
            int height, 
            Action<Texture2D> callback,
            float bearing = 0f)
        {
            StartCoroutine(FetchMapTile(latitude, longitude, zoom, width, height, bearing, callback));
        }
        
        /// <summary>
        /// Get a map tile for the mini-map (square, smaller size)
        /// </summary>
        public void GetMiniMapTile(double latitude, double longitude, float bearing, Action<Texture2D> callback)
        {
            GetMapTile(latitude, longitude, 16, 400, 400, callback, bearing);
        }
        
        /// <summary>
        /// Get a map tile for the full map (larger, higher detail)
        /// </summary>
        public void GetFullMapTile(double latitude, double longitude, int zoom, Action<Texture2D> callback)
        {
            GetMapTile(latitude, longitude, zoom, 1024, 1024, callback, 0f);
        }
        
        /// <summary>
        /// Build a URL for a static map with markers
        /// </summary>
        public string BuildMarkerUrl(
            double centerLat, 
            double centerLng, 
            int zoom,
            int width,
            int height,
            List<MarkerInfo> markers)
        {
            string styleId = GetStyleId(mapStyle);
            
            // Build marker overlay string
            string markerOverlay = "";
            if (markers != null && markers.Count > 0)
            {
                var markerStrings = new List<string>();
                foreach (var marker in markers)
                {
                    // pin-s = small pin, pin-l = large pin
                    // Color is hex without #
                    string pin = $"pin-s-{marker.label}+{marker.color}({marker.longitude},{marker.latitude})";
                    markerStrings.Add(pin);
                }
                markerOverlay = string.Join(",", markerStrings) + "/";
            }
            
            // Retina (@2x) for high DPI displays
            string url = $"https://api.mapbox.com/styles/v1/{styleId}/static/{markerOverlay}{centerLng},{centerLat},{zoom},0/{width}x{height}@2x?access_token={EffectiveAccessToken}";
            
            return url;
        }
        
        /// <summary>
        /// Change the map style
        /// </summary>
        public void SetMapStyle(MapStyle style)
        {
            mapStyle = style;
            ClearCache(); // Clear cache when style changes
            Log($"Map style changed to: {style}");
        }
        
        /// <summary>
        /// Clear the tile cache
        /// </summary>
        public void ClearCache()
        {
            foreach (var tile in _tileCache.Values)
            {
                if (tile.texture != null)
                {
                    Destroy(tile.texture);
                }
            }
            _tileCache.Clear();
            _cacheOrder.Clear();
            Log("Tile cache cleared");
        }
        
        #endregion
        
        #region Marker Info
        
        public struct MarkerInfo
        {
            public double latitude;
            public double longitude;
            public string label; // Single character or empty
            public string color; // Hex color without #
            
            public MarkerInfo(double lat, double lng, string lbl = "", string clr = "f4d03f")
            {
                latitude = lat;
                longitude = lng;
                label = lbl;
                color = clr;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private IEnumerator FetchMapTile(
            double latitude, 
            double longitude, 
            int zoom, 
            int width, 
            int height, 
            float bearing,
            Action<Texture2D> callback)
        {
            // Round coordinates for cache key (reduces API calls for small movements)
            double roundedLat = Math.Round(latitude, 4);
            double roundedLng = Math.Round(longitude, 4);
            int roundedBearing = Mathf.RoundToInt(bearing / 15) * 15; // Round to 15 degree increments
            
            string cacheKey = $"{roundedLat}_{roundedLng}_{zoom}_{width}_{height}_{roundedBearing}_{mapStyle}";
            
            // Check cache
            if (enableCache && _tileCache.TryGetValue(cacheKey, out CachedTile cached))
            {
                if (Time.time - cached.timestamp < cacheExpirySeconds)
                {
                    Log($"Cache hit: {cacheKey}");
                    callback?.Invoke(cached.texture);
                    yield break;
                }
                else
                {
                    // Expired - remove from cache
                    if (cached.texture != null)
                    {
                        Destroy(cached.texture);
                    }
                    _tileCache.Remove(cacheKey);
                }
            }
            
            // Build URL
            string styleId = GetStyleId(mapStyle);
            string url = $"https://api.mapbox.com/styles/v1/{styleId}/static/{longitude},{latitude},{zoom},{bearing}/{width}x{height}@2x?access_token={EffectiveAccessToken}";
            
            Log($"Fetching map tile: zoom={zoom}, size={width}x{height}, styleId={styleId}");
            
            // Fetch from Mapbox
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    
                    // Add to cache
                    if (enableCache)
                    {
                        AddToCache(cacheKey, texture);
                    }
                    
                    Log($"Map tile loaded successfully: {width}x{height}");
                    callback?.Invoke(texture);
                }
                else
                {
                    LogError($"Failed to load map tile: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }
        
        private void AddToCache(string key, Texture2D texture)
        {
            // Enforce max cache size
            while (_tileCache.Count >= maxCacheSize && _cacheOrder.Count > 0)
            {
                string oldestKey = _cacheOrder.Dequeue();
                if (_tileCache.TryGetValue(oldestKey, out CachedTile oldest))
                {
                    if (oldest.texture != null)
                    {
                        Destroy(oldest.texture);
                    }
                    _tileCache.Remove(oldestKey);
                }
            }
            
            // Add new tile
            _tileCache[key] = new CachedTile
            {
                texture = texture,
                timestamp = Time.time,
                key = key
            };
            _cacheOrder.Enqueue(key);
        }
        
        #endregion
        
        #region Logging
        
        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[Mapbox] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[Mapbox] {message}");
        }
        
        #endregion
    }
}
