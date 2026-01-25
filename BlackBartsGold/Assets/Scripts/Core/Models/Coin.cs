// ============================================================================
// Coin.cs
// Black Bart's Gold - Coin Data Model
// Path: Assets/Scripts/Core/Models/Coin.cs
// ============================================================================
// Represents a treasure coin that can be found or hidden in the game world.
// Reference: Docs/coins-and-collection.md, Docs/economy-and-currency.md
// ============================================================================

using System;
using UnityEngine;
using Newtonsoft.Json;

namespace BlackBartsGold.Core.Models
{
    /// <summary>
    /// Data model representing a treasure coin.
    /// Serializable for JSON persistence and network transfer.
    /// </summary>
    [Serializable]
    public class Coin
    {
        #region Identity
        
        /// <summary>
        /// Unique identifier for this coin
        /// </summary>
        public string id;
        
        /// <summary>
        /// Type of coin (Fixed value vs Pool/random)
        /// </summary>
        public CoinType coinType;
        
        #endregion
        
        #region Value
        
        /// <summary>
        /// Value in BBG (Black Bart's Gold)
        /// For pool coins, this is determined when found
        /// </summary>
        public float value;
        
        /// <summary>
        /// Contribution amount (what the hider put in)
        /// Value = contribution - platform fee
        /// </summary>
        public float contribution;
        
        /// <summary>
        /// Pool contribution for pool coins
        /// </summary>
        public float poolContribution;
        
        /// <summary>
        /// Visual tier based on value (Gold, Silver, Bronze, etc.)
        /// </summary>
        [JsonProperty("tier")]
        public CoinTier currentTier;
        
        #endregion
        
        #region Location
        
        /// <summary>
        /// GPS latitude coordinate
        /// </summary>
        public double latitude;
        
        /// <summary>
        /// GPS longitude coordinate
        /// </summary>
        public double longitude;
        
        /// <summary>
        /// Altitude in meters (optional)
        /// </summary>
        public float altitude;
        
        /// <summary>
        /// Height offset above ground for AR placement (meters)
        /// Default: 1.5m (eye level)
        /// </summary>
        public float heightOffset = 1.5f;
        
        #endregion
        
        #region Hider Info
        
        /// <summary>
        /// User ID of the player who hid this coin
        /// </summary>
        public string hiderId;
        
        /// <summary>
        /// Alias for hiderId (for API compatibility)
        /// </summary>
        public string hiddenBy 
        { 
            get => hiderId; 
            set => hiderId = value; 
        }
        
        /// <summary>
        /// Display name of hider (for social features)
        /// </summary>
        public string hiderName;
        
        /// <summary>
        /// When this coin was hidden (ISO 8601 string)
        /// </summary>
        public string hiddenAt;
        
        #endregion
        
        #region Hunt Configuration
        
        /// <summary>
        /// Type of hunt this coin belongs to
        /// </summary>
        public HuntType huntType;
        
        /// <summary>
        /// Can this coin be found by multiple players?
        /// </summary>
        public bool multiFind;
        
        /// <summary>
        /// Number of times this coin can still be found
        /// (Only relevant if multiFind is true)
        /// </summary>
        public int findsRemaining;
        
        /// <summary>
        /// Maximum number of finds allowed (for multi-find coins)
        /// </summary>
        public int maxFinds;
        
        #endregion
        
        #region Visual Customization
        
        /// <summary>
        /// URL to custom logo for front of coin (optional)
        /// </summary>
        public string logoFrontUrl;
        
        /// <summary>
        /// URL to custom logo for back of coin (optional)
        /// </summary>
        public string logoBackUrl;
        
        /// <summary>
        /// Sponsor name if this is a sponsored coin
        /// </summary>
        public string sponsorName;
        
        #endregion
        
        #region State
        
        /// <summary>
        /// Current status of this coin
        /// </summary>
        public CoinStatus status;
        
        /// <summary>
        /// When this coin was collected (ISO 8601 string)
        /// </summary>
        public string collectedAt;
        
        /// <summary>
        /// User ID of the player who collected this coin
        /// </summary>
        public string collectedBy;
        
        /// <summary>
        /// Alias for coinType (for compatibility)
        /// </summary>
        public CoinType type 
        { 
            get => coinType; 
            set => coinType = value; 
        }
        
        /// <summary>
        /// Is this coin locked for the current player?
        /// (Above their find limit)
        /// </summary>
        [NonSerialized]
        public bool isLocked;
        
        /// <summary>
        /// Is this coin within collection range?
        /// </summary>
        [JsonProperty("isInRange")]
        public bool isInRange;
        
        /// <summary>
        /// Current distance from player in meters
        /// </summary>
        [JsonProperty("distanceMeters")]
        public float distanceFromPlayer;
        
        /// <summary>
        /// Compass bearing to this coin (degrees, 0=North)
        /// </summary>
        [JsonProperty("bearingDegrees")]
        public float bearingFromPlayer;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Default constructor (required for serialization)
        /// </summary>
        public Coin() { }
        
        /// <summary>
        /// Create a new coin with basic properties
        /// </summary>
        public Coin(string id, CoinType type, float value, double lat, double lng)
        {
            this.id = id;
            this.coinType = type;
            this.value = value;
            this.latitude = lat;
            this.longitude = lng;
            this.status = CoinStatus.Visible;
            this.currentTier = CalculateTier(value);
            this.hiddenAt = DateTime.UtcNow.ToString("o");
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Calculate coin tier based on value
        /// </summary>
        public static CoinTier CalculateTier(float value)
        {
            if (value <= 0) return CoinTier.Unknown;
            if (value < 1.00f) return CoinTier.Bronze;
            if (value < 5.00f) return CoinTier.Silver;
            if (value < 25.00f) return CoinTier.Gold;
            if (value < 100.00f) return CoinTier.Platinum;
            return CoinTier.Diamond;
        }
        
        /// <summary>
        /// Get display string for value
        /// Returns "?" for unrevealed pool coins
        /// </summary>
        public string GetDisplayValue()
        {
            if (coinType == CoinType.Pool && status == CoinStatus.Visible)
            {
                return "?";
            }
            return $"${value:F2}";
        }
        
        /// <summary>
        /// Get effective value of coin (used for limit checks)
        /// For pool coins before collection, returns max possible value
        /// </summary>
        public float GetEffectiveValue()
        {
            return value;
        }
        
        /// <summary>
        /// Get the tier of this coin
        /// </summary>
        public CoinTier GetTier()
        {
            return currentTier;
        }
        
        /// <summary>
        /// Get the Unity Vector3 position for AR placement
        /// Note: This is relative to AR origin, not world position
        /// </summary>
        public Vector3 GetARPosition(Vector3 playerARPosition)
        {
            // This will be calculated by GeoUtils when we implement it
            // Placeholder returns a position relative to player
            return new Vector3(0, heightOffset, 5);
        }
        
        /// <summary>
        /// Check if player can collect this coin
        /// </summary>
        public bool CanCollect(float playerFindLimit)
        {
            if (isLocked) return false;
            if (!isInRange) return false;
            if (status != CoinStatus.Visible) return false;
            if (value > playerFindLimit) return false;
            return true;
        }
        
        /// <summary>
        /// Get reason why coin can't be collected
        /// </summary>
        public string GetCollectionBlockReason(float playerFindLimit)
        {
            if (status != CoinStatus.Visible)
                return "This coin is no longer available";
            
            if (value > playerFindLimit)
                return $"Above your find limit! Hide ${value:F2} to unlock.";
            
            if (isLocked)
                return "This treasure be above yer limit, matey!";
            
            if (!isInRange)
                return $"Get closer! {distanceFromPlayer:F0}m away";
            
            return null; // Can collect
        }
        
        /// <summary>
        /// Convert hidden timestamp to DateTime
        /// </summary>
        public DateTime GetHiddenDateTime()
        {
            if (DateTime.TryParse(hiddenAt, out DateTime dt))
            {
                return dt;
            }
            return DateTime.MinValue;
        }
        
        /// <summary>
        /// Get time since coin was hidden
        /// </summary>
        public TimeSpan GetAge()
        {
            return DateTime.UtcNow - GetHiddenDateTime();
        }
        
        /// <summary>
        /// Debug string representation
        /// </summary>
        public override string ToString()
        {
            return $"Coin[{id}]: {GetDisplayValue()} at ({latitude:F4}, {longitude:F4}) - {status}";
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create a test coin for development
        /// </summary>
        public static Coin CreateTestCoin(float value, float distance = 5f)
        {
            return CreateTestCoin(CoinType.Fixed, value, distance);
        }
        
        /// <summary>
        /// Create a test coin with specific type for development
        /// </summary>
        public static Coin CreateTestCoin(CoinType type, float value, float distance = 5f)
        {
            return new Coin
            {
                id = Guid.NewGuid().ToString(),
                coinType = type,
                value = value,
                contribution = value * 1.1f,
                currentTier = CalculateTier(value),
                latitude = 0,
                longitude = 0,
                status = CoinStatus.Visible,
                huntType = HuntType.Standard,
                multiFind = false,
                findsRemaining = 1,
                maxFinds = 1,
                hiderId = "test-hider",
                hiderName = "Test Pirate",
                hiddenAt = DateTime.UtcNow.ToString("o"),
                distanceFromPlayer = distance,
                isInRange = distance <= 5f,
                isLocked = false
            };
        }
        
        #endregion
    }
}
