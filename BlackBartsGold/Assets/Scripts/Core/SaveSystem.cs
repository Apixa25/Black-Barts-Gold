// ============================================================================
// SaveSystem.cs
// Black Bart's Gold - Data Persistence System
// Path: Assets/Scripts/Core/SaveSystem.cs
// ============================================================================
// Handles saving and loading player data to device storage using JSON.
// Uses Application.persistentDataPath for cross-platform compatibility.
// ============================================================================

using UnityEngine;
using System;
using System.IO;

namespace BlackBartsGold.Core
{
    /// <summary>
    /// Static utility class for saving and loading player data.
    /// Uses JSON serialization to persistent data path.
    /// </summary>
    public static class SaveSystem
    {
        #region Constants
        
        /// <summary>
        /// Filename for player save data
        /// </summary>
        private const string SAVE_FILE = "player_data.json";
        
        /// <summary>
        /// Filename for settings data
        /// </summary>
        private const string SETTINGS_FILE = "settings.json";
        
        /// <summary>
        /// Backup file suffix
        /// </summary>
        private const string BACKUP_SUFFIX = ".backup";
        
        /// <summary>
        /// PlayerPrefs key for auth token
        /// </summary>
        private const string AUTH_TOKEN_KEY = "auth_token";
        
        /// <summary>
        /// PlayerPrefs key for last user email
        /// </summary>
        private const string LAST_EMAIL_KEY = "last_email";
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Full path to save directory
        /// </summary>
        public static string SavePath => Application.persistentDataPath;
        
        /// <summary>
        /// Full path to player save file
        /// </summary>
        public static string PlayerSaveFilePath => Path.Combine(SavePath, SAVE_FILE);
        
        /// <summary>
        /// Full path to settings file
        /// </summary>
        public static string SettingsFilePath => Path.Combine(SavePath, SETTINGS_FILE);
        
        #endregion
        
        #region Player Data
        
        /// <summary>
        /// Save player data to file
        /// </summary>
        public static bool SavePlayerData(PlayerSaveData data)
        {
            try
            {
                // Create backup of existing file
                CreateBackup(PlayerSaveFilePath);
                
                // Serialize to JSON
                string json = JsonUtility.ToJson(data, true);
                
                // Write to file
                File.WriteAllText(PlayerSaveFilePath, json);
                
                Debug.Log($"[SaveSystem] 💾 Player data saved to: {PlayerSaveFilePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to save player data: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load player data from file
        /// </summary>
        public static PlayerSaveData LoadPlayerData()
        {
            try
            {
                if (!File.Exists(PlayerSaveFilePath))
                {
                    Debug.Log("[SaveSystem] ℹ️ No save file found");
                    return null;
                }
                
                string json = File.ReadAllText(PlayerSaveFilePath);
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                
                Debug.Log($"[SaveSystem] 📂 Player data loaded from: {PlayerSaveFilePath}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to load player data: {e.Message}");
                
                // Try loading backup
                return LoadBackup<PlayerSaveData>(PlayerSaveFilePath);
            }
        }
        
        /// <summary>
        /// Delete player data file
        /// </summary>
        public static void DeletePlayerData()
        {
            try
            {
                if (File.Exists(PlayerSaveFilePath))
                {
                    File.Delete(PlayerSaveFilePath);
                    Debug.Log("[SaveSystem] 🗑️ Player data deleted");
                }
                
                // Also delete backup
                string backupPath = PlayerSaveFilePath + BACKUP_SUFFIX;
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                
                // Clear auth token from PlayerPrefs
                ClearAuthToken();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to delete player data: {e.Message}");
            }
        }
        
        /// <summary>
        /// Check if save file exists
        /// </summary>
        public static bool HasSaveData()
        {
            return File.Exists(PlayerSaveFilePath);
        }
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// Save settings to file
        /// </summary>
        public static bool SaveSettings(GameSettings settings)
        {
            try
            {
                string json = JsonUtility.ToJson(settings, true);
                File.WriteAllText(SettingsFilePath, json);
                
                Debug.Log("[SaveSystem] ⚙️ Settings saved");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to save settings: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load settings from file
        /// </summary>
        public static GameSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return new GameSettings(); // Return defaults
                }
                
                string json = File.ReadAllText(SettingsFilePath);
                return JsonUtility.FromJson<GameSettings>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to load settings: {e.Message}");
                return new GameSettings(); // Return defaults on error
            }
        }
        
        #endregion
        
        #region Authentication Token
        
        /// <summary>
        /// Save auth token to PlayerPrefs (more secure than file)
        /// </summary>
        public static void SaveAuthToken(string token)
        {
            PlayerPrefs.SetString(AUTH_TOKEN_KEY, token);
            PlayerPrefs.Save();
            Debug.Log("[SaveSystem] 🔑 Auth token saved");
        }
        
        /// <summary>
        /// Load auth token from PlayerPrefs
        /// </summary>
        public static string LoadAuthToken()
        {
            return PlayerPrefs.GetString(AUTH_TOKEN_KEY, "");
        }
        
        /// <summary>
        /// Check if auth token exists
        /// </summary>
        public static bool HasAuthToken()
        {
            return !string.IsNullOrEmpty(LoadAuthToken());
        }
        
        /// <summary>
        /// Clear auth token
        /// </summary>
        public static void ClearAuthToken()
        {
            PlayerPrefs.DeleteKey(AUTH_TOKEN_KEY);
            PlayerPrefs.Save();
            Debug.Log("[SaveSystem] 🔑 Auth token cleared");
        }
        
        #endregion
        
        #region Last Email (for login convenience)
        
        /// <summary>
        /// Save last used email for login convenience
        /// </summary>
        public static void SaveLastEmail(string email)
        {
            PlayerPrefs.SetString(LAST_EMAIL_KEY, email);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Get last used email
        /// </summary>
        public static string GetLastEmail()
        {
            return PlayerPrefs.GetString(LAST_EMAIL_KEY, "");
        }
        
        #endregion
        
        #region Backup System
        
        /// <summary>
        /// Create backup of a file
        /// </summary>
        private static void CreateBackup(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string backupPath = filePath + BACKUP_SUFFIX;
                    File.Copy(filePath, backupPath, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] ⚠️ Failed to create backup: {e.Message}");
            }
        }
        
        /// <summary>
        /// Load from backup file
        /// </summary>
        private static T LoadBackup<T>(string originalPath) where T : class
        {
            try
            {
                string backupPath = originalPath + BACKUP_SUFFIX;
                
                if (!File.Exists(backupPath))
                {
                    Debug.LogWarning("[SaveSystem] ⚠️ No backup file found");
                    return null;
                }
                
                string json = File.ReadAllText(backupPath);
                T data = JsonUtility.FromJson<T>(json);
                
                Debug.Log("[SaveSystem] 📂 Loaded from backup");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to load backup: {e.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region Generic Save/Load
        
        /// <summary>
        /// Save any serializable object to a file
        /// </summary>
        public static bool SaveToFile<T>(T data, string filename)
        {
            try
            {
                string path = Path.Combine(SavePath, filename);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to save {filename}: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load any serializable object from a file
        /// </summary>
        public static T LoadFromFile<T>(string filename) where T : class, new()
        {
            try
            {
                string path = Path.Combine(SavePath, filename);
                
                if (!File.Exists(path))
                {
                    return new T();
                }
                
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to load {filename}: {e.Message}");
                return new T();
            }
        }
        
        /// <summary>
        /// Delete a file
        /// </summary>
        public static bool DeleteFile(string filename)
        {
            try
            {
                string path = Path.Combine(SavePath, filename);
                
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to delete {filename}: {e.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Clear all saved data (nuclear option)
        /// </summary>
        public static void ClearAllData()
        {
            try
            {
                // Delete all files in save directory
                string[] files = Directory.GetFiles(SavePath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
                
                // Clear PlayerPrefs
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                
                Debug.Log("[SaveSystem] 🗑️ All data cleared");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] ❌ Failed to clear all data: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get total size of save data in bytes
        /// </summary>
        public static long GetSaveDataSize()
        {
            try
            {
                long totalSize = 0;
                string[] files = Directory.GetFiles(SavePath);
                
                foreach (string file in files)
                {
                    FileInfo info = new FileInfo(file);
                    totalSize += info.Length;
                }
                
                return totalSize;
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Get human readable save data size
        /// </summary>
        public static string GetSaveDataSizeFormatted()
        {
            long bytes = GetSaveDataSize();
            
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F1} KB";
            
            return $"{bytes / (1024f * 1024f):F1} MB";
        }
        
        /// <summary>
        /// Log save system info (for debugging)
        /// </summary>
        public static void DebugLogInfo()
        {
            Debug.Log("=== SaveSystem Info ===");
            Debug.Log($"Save Path: {SavePath}");
            Debug.Log($"Has Save Data: {HasSaveData()}");
            Debug.Log($"Has Auth Token: {HasAuthToken()}");
            Debug.Log($"Data Size: {GetSaveDataSizeFormatted()}");
            Debug.Log("=======================");
        }
        
        #endregion
    }
    
    #region Settings Data Structure
    
    /// <summary>
    /// Game settings that persist across sessions
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        // Audio
        public bool soundEnabled = true;
        public bool musicEnabled = true;
        public float soundVolume = 1f;
        public float musicVolume = 0.7f;
        
        // Haptics
        public bool hapticEnabled = true;
        
        // Notifications
        public bool notificationsEnabled = true;
        public bool nearbyCoinsNotifications = true;
        public bool lowGasNotifications = true;
        
        // Display
        public bool showCoinsOnMap = true;
        public bool showDistanceInMetric = true;
        
        // Performance
        public int maxRenderDistance = 100; // meters
        public int maxVisibleCoins = 20;
        
        // Debug (development only)
        public bool debugMode = false;
        public bool showFPS = false;
    }
    
    #endregion
}
