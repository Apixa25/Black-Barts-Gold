// ============================================================================
// User.cs
// Black Bart's Gold - User Data Model
// Path: Assets/Scripts/Core/Models/User.cs
// ============================================================================
// Represents a player account with profile info, settings, and game stats.
// Reference: Docs/user-accounts-security.md
// ============================================================================

using System;

namespace BlackBartsGold.Core.Models
{
    /// <summary>
    /// Data model representing a player account.
    /// Serializable for JSON persistence.
    /// </summary>
    [Serializable]
    public class User
    {
        #region Identity
        
        /// <summary>
        /// Unique user identifier
        /// </summary>
        public string id;
        
        /// <summary>
        /// User's email address
        /// </summary>
        public string email;
        
        /// <summary>
        /// Display name / username
        /// </summary>
        public string displayName;
        
        /// <summary>
        /// URL to profile avatar image
        /// </summary>
        public string avatarUrl;

        /// <summary>
        /// Selected preset avatar identifier (preferred over raw URL for MVP safety).
        /// </summary>
        public string avatarPresetId;
        
        #endregion
        
        #region Economy
        
        /// <summary>
        /// Total BBG balance
        /// </summary>
        public float bbgBalance;
        
        /// <summary>
        /// Gas remaining in days (decimal for partial days)
        /// </summary>
        public float gasRemaining;
        
        /// <summary>
        /// Current find limit based on highest hidden coin
        /// Default: $1.00
        /// Reference: Docs/economy-and-currency.md
        /// </summary>
        public float findLimit = 1.00f;
        
        /// <summary>
        /// Highest value coin ever hidden by this user
        /// This determines findLimit
        /// </summary>
        public float highestHiddenValue;
        
        #endregion
        
        #region Game Progress
        
        /// <summary>
        /// Player statistics
        /// </summary>
        public UserStats stats;
        
        /// <summary>
        /// Current tier based on findLimit
        /// </summary>
        public FindLimitTier currentTier;
        
        /// <summary>
        /// Total experience points
        /// </summary>
        public int experience;
        
        /// <summary>
        /// Player level
        /// </summary>
        public int level = 1;
        
        #endregion
        
        #region Account Info
        
        /// <summary>
        /// When account was created (ISO 8601)
        /// </summary>
        public string createdAt;
        
        /// <summary>
        /// Last login timestamp (ISO 8601)
        /// </summary>
        public string lastLoginAt;
        
        /// <summary>
        /// Last gas charge timestamp (ISO 8601)
        /// </summary>
        public string lastGasChargeAt;
        
        /// <summary>
        /// Account status
        /// </summary>
        public AccountStatus accountStatus = AccountStatus.Active;
        
        /// <summary>
        /// Authentication method used
        /// </summary>
        public AuthMethod authMethod;
        
        /// <summary>
        /// Is email verified?
        /// </summary>
        public bool emailVerified;
        
        /// <summary>
        /// User's age (for legal compliance)
        /// </summary>
        public int age;

        /// <summary>
        /// Whether user dismissed first-time profile completion prompt.
        /// </summary>
        public bool profileOnboardingDismissed;
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// Haptic feedback enabled
        /// </summary>
        public bool hapticEnabled = true;
        
        /// <summary>
        /// Sound effects enabled
        /// </summary>
        public bool soundEnabled = true;
        
        /// <summary>
        /// Music enabled
        /// </summary>
        public bool musicEnabled = true;
        
        /// <summary>
        /// Push notifications enabled
        /// </summary>
        public bool notificationsEnabled = true;
        
        /// <summary>
        /// Show coins on map
        /// </summary>
        public bool showCoinsOnMap = true;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public User()
        {
            stats = new UserStats();
        }
        
        /// <summary>
        /// Create user with basic info
        /// </summary>
        public User(string id, string email, string displayName)
        {
            this.id = id;
            this.email = email;
            this.displayName = displayName;
            this.stats = new UserStats();
            this.createdAt = DateTime.UtcNow.ToString("o");
            this.lastLoginAt = DateTime.UtcNow.ToString("o");
            this.findLimit = 1.00f;
            this.currentTier = FindLimitTier.CabinBoy;
            this.accountStatus = AccountStatus.Active;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get current tier based on find limit
        /// Reference: Docs/economy-and-currency.md
        /// </summary>
        public FindLimitTier GetTier()
        {
            if (findLimit >= 100.00f) return FindLimitTier.KingOfPirates;
            if (findLimit >= 50.00f) return FindLimitTier.PirateLegend;
            if (findLimit >= 25.00f) return FindLimitTier.Captain;
            if (findLimit >= 10.00f) return FindLimitTier.TreasureHunter;
            if (findLimit >= 5.00f) return FindLimitTier.DeckHand;
            return FindLimitTier.CabinBoy;
        }
        
        /// <summary>
        /// Get tier display name
        /// </summary>
        public string GetTierName()
        {
            return GetTier() switch
            {
                FindLimitTier.CabinBoy => "Cabin Boy",
                FindLimitTier.DeckHand => "Deck Hand",
                FindLimitTier.TreasureHunter => "Treasure Hunter",
                FindLimitTier.Captain => "Captain",
                FindLimitTier.PirateLegend => "Pirate Legend",
                FindLimitTier.KingOfPirates => "King of Pirates",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Update find limit after hiding a coin
        /// </summary>
        public void UpdateFindLimit(float hiddenValue)
        {
            if (hiddenValue > highestHiddenValue)
            {
                highestHiddenValue = hiddenValue;
                findLimit = hiddenValue;
                currentTier = GetTier();
            }
        }
        
        /// <summary>
        /// Check if user can find a coin of given value
        /// </summary>
        public bool CanFind(float coinValue)
        {
            return coinValue <= findLimit;
        }

        /// <summary>
        /// Has user selected some form of profile image?
        /// </summary>
        public bool HasAvatar()
        {
            return !string.IsNullOrWhiteSpace(avatarPresetId) || !string.IsNullOrWhiteSpace(avatarUrl);
        }

        /// <summary>
        /// Is profile complete enough for social identity features?
        /// </summary>
        public bool IsProfileComplete()
        {
            return !string.IsNullOrWhiteSpace(displayName) && age >= 13 && HasAvatar();
        }

        /// <summary>
        /// Assign a safe preset avatar and mirror into avatarUrl namespace.
        /// </summary>
        public void SetAvatarPreset(string presetId)
        {
            avatarPresetId = presetId;
            avatarUrl = string.IsNullOrWhiteSpace(presetId) ? avatarUrl : $"preset://{presetId}";
        }
        
        /// <summary>
        /// Get how much gas in days remains
        /// </summary>
        public int GetGasDaysRemaining()
        {
            return (int)Math.Floor(gasRemaining);
        }
        
        /// <summary>
        /// Get gas status
        /// </summary>
        public GasStatus GetGasStatus()
        {
            if (gasRemaining <= 0) return GasStatus.Empty;
            
            // Assuming 30 days is full tank
            float percentage = gasRemaining / 30f;
            
            if (percentage < 0.15f) return GasStatus.Low;
            if (percentage < 0.50f) return GasStatus.Normal;
            return GasStatus.Full;
        }
        
        /// <summary>
        /// Can the user play (has gas)?
        /// </summary>
        public bool CanPlay()
        {
            return gasRemaining > 0;
        }
        
        /// <summary>
        /// Consume daily gas
        /// </summary>
        public void ConsumeGas(float days = 1f)
        {
            gasRemaining = Math.Max(0, gasRemaining - days);
            lastGasChargeAt = DateTime.UtcNow.ToString("o");
        }
        
        /// <summary>
        /// Add gas (from purchase or unpark)
        /// </summary>
        public void AddGas(float days)
        {
            gasRemaining += days;
        }
        
        /// <summary>
        /// Debug string representation
        /// </summary>
        public override string ToString()
        {
            return $"User[{id}]: {displayName} - ${bbgBalance:F2} BBG, {GetGasDaysRemaining()} days gas, {GetTierName()}";
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create a test user for development
        /// </summary>
        public static User CreateTestUser()
        {
            return new User
            {
                id = "test-user-001",
                email = "pirate@blackbartsgold.com",
                displayName = "Test Pirate",
                bbgBalance = 25.00f,
                gasRemaining = 15f, // 15 days
                findLimit = 5.00f,
                highestHiddenValue = 5.00f,
                currentTier = FindLimitTier.DeckHand,
                stats = new UserStats
                {
                    totalFound = 10,
                    totalHidden = 2,
                    totalValueFound = 12.50f,
                    totalValueHidden = 7.00f,
                    highestValueFound = 5.00f
                },
                createdAt = DateTime.UtcNow.AddDays(-30).ToString("o"),
                lastLoginAt = DateTime.UtcNow.ToString("o"),
                accountStatus = AccountStatus.Active,
                authMethod = AuthMethod.Email,
                emailVerified = true,
                age = 25,
                avatarPresetId = "outlaw-hat-01",
                avatarUrl = "preset://outlaw-hat-01",
                profileOnboardingDismissed = true
            };
        }
        
        /// <summary>
        /// Create a new user with starter values
        /// </summary>
        public static User CreateNewUser(string email, string displayName, int age)
        {
            return new User
            {
                id = Guid.NewGuid().ToString(),
                email = email,
                displayName = displayName,
                age = age,
                bbgBalance = 0f,
                gasRemaining = 0f, // Must purchase gas to play
                findLimit = 1.00f, // Default limit
                highestHiddenValue = 0f,
                currentTier = FindLimitTier.CabinBoy,
                stats = new UserStats(),
                createdAt = DateTime.UtcNow.ToString("o"),
                lastLoginAt = DateTime.UtcNow.ToString("o"),
                accountStatus = AccountStatus.Active,
                emailVerified = false,
                profileOnboardingDismissed = false
            };
        }
        
        #endregion
    }
}
