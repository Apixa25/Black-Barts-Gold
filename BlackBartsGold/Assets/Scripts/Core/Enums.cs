// ============================================================================
// Enums.cs
// Black Bart's Gold - Core Enumerations
// Path: Assets/Scripts/Core/Enums.cs
// ============================================================================
// Central location for all game enumerations. Organized by category.
// Reference: Docs/economy-and-currency.md, treasure-hunt-types.md
// ============================================================================

namespace BlackBartsGold.Core
{
    #region Scene Management
    
    /// <summary>
    /// All scenes in the game. Names must match scene file names exactly.
    /// </summary>
    public enum SceneNames
    {
        /// <summary>Main menu / home screen</summary>
        MainMenu,
        
        /// <summary>AR treasure hunting view</summary>
        ARHunt,
        
        /// <summary>2D map view showing coin locations</summary>
        Map,
        
        /// <summary>Wallet and balance management</summary>
        Wallet,
        
        /// <summary>User settings</summary>
        Settings,
        
        /// <summary>Login screen</summary>
        Login,
        
        /// <summary>Registration screen</summary>
        Register,
        
        /// <summary>First-time onboarding</summary>
        Onboarding,
        
        /// <summary>AR test scene (development only)</summary>
        ARTest
    }
    
    #endregion
    
    #region Coins
    
    /// <summary>
    /// Type of coin based on value determination
    /// Reference: Docs/coins-and-collection.md
    /// </summary>
    public enum CoinType
    {
        /// <summary>Fixed value coin - value set when hidden</summary>
        Fixed,
        
        /// <summary>Pool/random coin - value determined when found (slot machine)</summary>
        Pool
    }
    
    /// <summary>
    /// Current status of a coin
    /// </summary>
    public enum CoinStatus
    {
        /// <summary>Coin exists but not yet visible</summary>
        Hidden,
        
        /// <summary>Coin is visible to players</summary>
        Visible,
        
        /// <summary>Coin has been collected, pending confirmation</summary>
        Collected,
        
        /// <summary>Coin collection confirmed (after 24h)</summary>
        Confirmed,
        
        /// <summary>Coin has been removed/expired</summary>
        Removed
    }
    
    /// <summary>
    /// Visual tier based on coin value
    /// Reference: Docs/economy-and-currency.md (Find Limits)
    /// </summary>
    public enum CoinTier
    {
        /// <summary>Bronze: $0.01 - $0.99</summary>
        Bronze,
        
        /// <summary>Silver: $1.00 - $4.99</summary>
        Silver,
        
        /// <summary>Gold: $5.00 - $24.99</summary>
        Gold,
        
        /// <summary>Platinum: $25.00 - $99.99</summary>
        Platinum,
        
        /// <summary>Diamond: $100.00+</summary>
        Diamond,
        
        /// <summary>Unknown (pool coins before reveal)</summary>
        Unknown
    }
    
    /// <summary>
    /// Player find limit tiers (pirate themed!)
    /// Reference: Docs/economy-and-currency.md
    /// </summary>
    public enum FindLimitTier
    {
        /// <summary>$1.00 limit - default for new players</summary>
        CabinBoy,
        
        /// <summary>$5.00 limit</summary>
        DeckHand,
        
        /// <summary>$10.00 limit</summary>
        TreasureHunter,
        
        /// <summary>$25.00 limit</summary>
        Captain,
        
        /// <summary>$50.00 limit</summary>
        PirateLegend,
        
        /// <summary>$100.00+ limit</summary>
        KingOfPirates
    }
    
    #endregion
    
    #region Hunt Types
    
    /// <summary>
    /// Types of treasure hunts available
    /// Reference: Docs/treasure-hunt-types.md
    /// </summary>
    public enum HuntType
    {
        /// <summary>Standard AR hunt - see coins through camera</summary>
        Standard,
        
        /// <summary>Compass only - no AR, just direction indicator</summary>
        CompassOnly,
        
        /// <summary>Radar only - no AR, just distance radar</summary>
        RadarOnly,
        
        /// <summary>Timed release - coins unlock at specific times</summary>
        TimedRelease,
        
        /// <summary>Event hunt - special limited time events</summary>
        Event,
        
        /// <summary>Sponsor hunt - brand-sponsored treasure hunts</summary>
        Sponsor
    }
    
    /// <summary>
    /// Hunt difficulty levels
    /// </summary>
    public enum HuntDifficulty
    {
        /// <summary>Easy - coins are closer together</summary>
        Easy,
        
        /// <summary>Normal - standard coin distribution</summary>
        Normal,
        
        /// <summary>Hard - coins spread further apart</summary>
        Hard,
        
        /// <summary>Expert - maximum spread, minimum hints</summary>
        Expert
    }
    
    #endregion
    
    #region Economy & Transactions
    
    /// <summary>
    /// Types of transactions
    /// Reference: Docs/economy-and-currency.md
    /// </summary>
    public enum TransactionType
    {
        /// <summary>Coin found/collected</summary>
        Found,
        
        /// <summary>Coin hidden by player</summary>
        Hidden,
        
        /// <summary>Daily gas consumption</summary>
        GasConsumed,
        
        /// <summary>BBG purchased with real money</summary>
        Purchased,
        
        /// <summary>Transfer to/from another player</summary>
        Transfer,
        
        /// <summary>Coins parked (moved to protected storage)</summary>
        Parked,
        
        /// <summary>Coins unparked (moved back to gas tank)</summary>
        Unparked,
        
        /// <summary>Withdrawal to external wallet</summary>
        Withdrawal,
        
        /// <summary>Bonus/reward from system</summary>
        Bonus,
        
        /// <summary>Refund</summary>
        Refund
    }
    
    /// <summary>
    /// Status of a transaction
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>Transaction pending confirmation (0-24 hours)</summary>
        Pending,
        
        /// <summary>Transaction confirmed and final</summary>
        Confirmed,
        
        /// <summary>Transaction failed</summary>
        Failed,
        
        /// <summary>Transaction cancelled</summary>
        Cancelled
    }
    
    /// <summary>
    /// Source of coins in wallet
    /// Reference: Docs/economy-and-currency.md
    /// </summary>
    public enum CoinSource
    {
        /// <summary>Coins purchased with real money - must use as gas</summary>
        Purchased,
        
        /// <summary>Coins found during hunts - can be parked</summary>
        Found,
        
        /// <summary>Bonus coins from system</summary>
        Bonus
    }
    
    /// <summary>
    /// Gas tank status
    /// </summary>
    public enum GasStatus
    {
        /// <summary>Full tank (>50%)</summary>
        Full,
        
        /// <summary>Normal level (15-50%)</summary>
        Normal,
        
        /// <summary>Low fuel (<15%) - show warning</summary>
        Low,
        
        /// <summary>Empty (0) - can't play</summary>
        Empty
    }
    
    #endregion
    
    #region User & Authentication
    
    /// <summary>
    /// Authentication methods
    /// </summary>
    public enum AuthMethod
    {
        /// <summary>Email and password</summary>
        Email,
        
        /// <summary>Google OAuth</summary>
        Google,
        
        /// <summary>Apple Sign-In</summary>
        Apple,
        
        /// <summary>Guest/anonymous (limited features)</summary>
        Guest
    }
    
    /// <summary>
    /// User account status
    /// </summary>
    public enum AccountStatus
    {
        /// <summary>Active account in good standing</summary>
        Active,
        
        /// <summary>Account temporarily suspended</summary>
        Suspended,
        
        /// <summary>Account banned</summary>
        Banned,
        
        /// <summary>Account pending email verification</summary>
        PendingVerification
    }
    
    #endregion
    
    #region AR & Location
    
    /// <summary>
    /// AR tracking quality
    /// </summary>
    public enum ARTrackingQuality
    {
        /// <summary>No tracking</summary>
        None,
        
        /// <summary>Limited tracking - may be inaccurate</summary>
        Limited,
        
        /// <summary>Normal tracking - acceptable</summary>
        Normal,
        
        /// <summary>Excellent tracking</summary>
        Excellent
    }
    
    /// <summary>
    /// GPS accuracy level
    /// </summary>
    public enum GPSAccuracy
    {
        /// <summary>No GPS signal</summary>
        None,
        
        /// <summary>Low accuracy (>50m)</summary>
        Low,
        
        /// <summary>Medium accuracy (10-50m)</summary>
        Medium,
        
        /// <summary>High accuracy (<10m)</summary>
        High
    }
    
    /// <summary>
    /// Distance from coin (for haptic feedback zones)
    /// Reference: Docs/prize-finder-details.md
    /// </summary>
    public enum ProximityZone
    {
        /// <summary>Out of range (>50m) - no feedback</summary>
        OutOfRange,
        
        /// <summary>Far (30-50m) - light pulse every 2s</summary>
        Far,
        
        /// <summary>Medium (15-30m) - medium pulse every 1s</summary>
        Medium,
        
        /// <summary>Near (5-15m) - heavy pulse every 0.5s</summary>
        Near,
        
        /// <summary>Collectible (<5m) - continuous buzz</summary>
        Collectible
    }
    
    #endregion
    
    #region UI & Notifications
    
    /// <summary>
    /// Types of UI popups/modals
    /// </summary>
    public enum PopupType
    {
        /// <summary>Information message</summary>
        Info,
        
        /// <summary>Success message</summary>
        Success,
        
        /// <summary>Warning message</summary>
        Warning,
        
        /// <summary>Error message</summary>
        Error,
        
        /// <summary>Confirmation dialog</summary>
        Confirm,
        
        /// <summary>Coin collection celebration</summary>
        CoinCollected,
        
        /// <summary>Find limit reached</summary>
        OverLimit,
        
        /// <summary>Low gas warning</summary>
        LowGas,
        
        /// <summary>No gas - can't play</summary>
        NoGas
    }
    
    #endregion
}
