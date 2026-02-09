// User roles for the admin dashboard
export type UserRole = 'super_admin' | 'sponsor_admin' | 'user'

// User profile from Supabase
export interface UserProfile {
  id: string
  email: string
  full_name: string | null
  role: UserRole
  avatar_url: string | null
  created_at: string
  updated_at: string
}

// Coin types
export type CoinType = 'fixed' | 'pool'
export type CoinStatus = 'hidden' | 'visible' | 'collected' | 'expired' | 'recycled'
export type CoinTier = 'gold' | 'silver' | 'bronze'

/** Which 3D coin graphic to show in AR (Unity app) */
export type CoinModel = 'bb_gold' | 'prize_race' | 'color_bb'

export interface Coin {
  id: string
  coin_type: CoinType
  value: number
  tier: CoinTier
  is_mythical: boolean
  latitude: number
  longitude: number
  location_name: string | null
  status: CoinStatus
  hider_id: string | null
  hidden_at: string
  collected_by: string | null
  collected_at: string | null
  sponsor_id: string | null
  logo_url: string | null
  multi_find: boolean
  finds_remaining: number
  description: string | null
  /** Which coin model/graphic to use in AR (bb_gold = Black Bart, prize_race = Prize Race). Optional until migration 011 applied. */
  coin_model?: CoinModel
  created_at: string
  updated_at: string
}

// Coin with relations (joined data)
export interface CoinWithRelations extends Coin {
  hider?: UserProfile | null
  collector?: UserProfile | null
}

// Transaction types
export type TransactionType = 'deposit' | 'found' | 'hidden' | 'gas_consumed' | 'transfer_in' | 'transfer_out' | 'payout'
export type TransactionStatus = 'pending' | 'confirmed' | 'failed' | 'cancelled'

export interface Transaction {
  id: string
  user_id: string
  transaction_type: TransactionType
  amount: number
  balance_after: number
  coin_id: string | null
  related_user_id: string | null
  description: string | null
  status: TransactionStatus
  metadata?: Record<string, unknown>
  created_at: string
  confirmed_at: string | null
}

// Transaction with user info (joined)
export interface TransactionWithUser extends Transaction {
  user?: UserProfile | null
  related_user?: UserProfile | null
}

// Financial summary stats
export interface FinancialStats {
  total_deposits: number
  total_payouts: number
  total_gas_revenue: number
  total_coins_value: number
  net_revenue: number
  transactions_today: number
  transactions_this_week: number
  transactions_this_month: number
}

// Sponsor status
export type SponsorStatus = 'active' | 'inactive' | 'pending'

// Sponsor
export interface Sponsor {
  id: string
  company_name: string
  contact_name: string | null
  contact_email: string
  contact_phone: string | null
  logo_url: string | null
  website_url: string | null
  description: string | null
  // Financial tracking
  total_spent: number
  coins_purchased: number
  coins_collected: number
  // Status
  status: SponsorStatus
  // Associated user (for sponsor_admin role)
  admin_user_id: string | null
  // Timestamps
  created_at: string
  updated_at: string
}

// Sponsor with admin user info
export interface SponsorWithAdmin extends Sponsor {
  admin_user?: UserProfile | null
}

// ============================================================================
// SPONSOR ANALYTICS TYPES - Phase M7: Sponsor Features
// ============================================================================

/**
 * Sponsor zone analytics - performance metrics for a sponsor's zones
 */
export interface SponsorZoneAnalytics {
  zone_id: string
  zone_name: string
  sponsor_id: string
  sponsor_name: string
  
  // Coin metrics
  total_coins_placed: number
  coins_collected: number
  coins_active: number
  coins_expired: number
  
  // Value metrics
  total_value_placed: number
  total_value_collected: number
  average_coin_value: number
  
  // Engagement metrics
  unique_collectors: number
  total_collections: number
  average_collection_time_minutes: number
  
  // Time-based metrics
  first_coin_placed_at: string | null
  last_coin_collected_at: string | null
  peak_collection_hour: number | null // 0-23
  
  // Performance score (0-100)
  performance_score: number
}

/**
 * Sponsor analytics summary - overall performance across all zones
 */
export interface SponsorAnalytics {
  sponsor_id: string
  sponsor_name: string
  
  // Zone metrics
  total_zones: number
  active_zones: number
  total_zone_area_km2: number
  
  // Coin metrics (aggregated)
  total_coins_placed: number
  total_coins_collected: number
  total_coins_active: number
  collection_rate: number // percentage
  
  // Value metrics (aggregated)
  total_value_placed: number
  total_value_collected: number
  average_coin_value: number
  roi_percentage: number // return on investment
  
  // Engagement metrics (aggregated)
  total_unique_collectors: number
  total_collections: number
  average_collections_per_zone: number
  
  // Financial metrics
  total_spent: number
  cost_per_collection: number
  cost_per_unique_collector: number
  
  // Time range
  period_start: string
  period_end: string
  
  // Zone analytics
  zone_analytics: SponsorZoneAnalytics[]
}

/**
 * Bulk coin placement configuration
 */
export interface BulkCoinPlacementConfig {
  sponsor_id: string
  zone_id: string | null // If null, place in sponsor's zones
  
  // Placement settings
  coin_count: number
  distribution_strategy: 'random' | 'grid' | 'cluster' | 'perimeter'
  
  // Value settings
  value_range: {
    min: number
    max: number
  }
  tier_distribution: {
    gold: number // percentage
    silver: number
    bronze: number
  }
  
  // Timing
  release_all_at_once: boolean
  scheduled_release_time: string | null
  
  // Advanced
  min_distance_between_coins_meters: number
  avoid_existing_coins: boolean
}

/**
 * Sponsor zone fee configuration
 */
export interface SponsorZoneFeeConfig {
  zone_type: 'sponsor'
  
  // Base fees
  zone_creation_fee: number // One-time fee to create a zone
  monthly_maintenance_fee: number // Monthly fee to keep zone active
  
  // Coin placement fees
  coin_placement_fee_per_coin: number // Fee per coin placed
  bulk_placement_discount_percentage: number // Discount for bulk (e.g., 10% off for 50+ coins)
  
  // Performance-based fees
  collection_fee_percentage: number // Percentage of coin value collected (e.g., 5%)
  
  // Minimums
  minimum_zone_size_km2: number
  minimum_coins_per_zone: number
  minimum_monthly_spend: number
}

// Dashboard stats
export interface DashboardStats {
  total_users: number
  active_users_today: number
  total_coins_in_system: number
  coins_found_today: number
  total_deposits: number
  total_payouts: number
  gas_revenue_today: number
}

// Activity log types
export type ActivityType = 
  | 'login' 
  | 'logout' 
  | 'login_failed'
  | 'password_changed'
  | 'profile_updated'
  | 'role_changed'
  | 'coin_created'
  | 'coin_collected'
  | 'coin_deleted'
  | 'sponsor_created'
  | 'sponsor_updated'
  | 'transaction_created'
  | 'payout_requested'
  | 'suspicious_activity'
  | 'admin_action'

export type ActivitySeverity = 'info' | 'warning' | 'error' | 'critical'

export interface ActivityLog {
  id: string
  user_id: string | null
  activity_type: ActivityType
  severity: ActivitySeverity
  description: string
  ip_address: string | null
  user_agent: string | null
  metadata: Record<string, unknown> | null
  created_at: string
}

// Activity log with user info
export interface ActivityLogWithUser extends ActivityLog {
  user?: UserProfile | null
}

// Security stats
export interface SecurityStats {
  total_logins_today: number
  failed_logins_today: number
  suspicious_activities: number
  active_sessions: number
  new_users_today: number
  admin_actions_today: number
}

// Navigation item type
export interface NavItem {
  name: string
  href: string
  icon: React.ComponentType<{ className?: string }>
}

// ============================================================================
// ZONE TYPES - Phase M3: Zone Management
// ============================================================================

/**
 * Zone types supported by Black Bart's Gold
 * - player: Auto-generated 1-mile radius around active players
 * - sponsor: Custom zones created by sponsors for their locations
 * - hunt: Special zones for timed release treasure hunts
 * - grid: System-generated zones for automated coin distribution
 */
export type ZoneType = 'player' | 'sponsor' | 'hunt' | 'grid'

/**
 * Zone status
 * - active: Zone is live and coins can be placed/found
 * - inactive: Zone is paused, no new coins spawned
 * - scheduled: Zone will become active at start_time
 * - completed: Hunt zone that has ended
 * - archived: Zone is no longer used
 */
export type ZoneStatus = 'active' | 'inactive' | 'scheduled' | 'completed' | 'archived'

/**
 * Geometry type for zone boundaries
 * - circle: Simple radius around center point
 * - polygon: Custom shape with multiple vertices
 */
export type ZoneGeometryType = 'circle' | 'polygon'

/**
 * Polygon coordinate point
 */
export interface PolygonPoint {
  latitude: number
  longitude: number
}

/**
 * Zone geometry - either a circle or polygon
 */
export interface ZoneGeometry {
  type: ZoneGeometryType
  // For circle type
  center?: {
    latitude: number
    longitude: number
  }
  radius_meters?: number
  // For polygon type
  polygon?: PolygonPoint[]
}

/**
 * Auto-spawn configuration for zones
 * Defines how coins are automatically placed in the zone
 */
export interface ZoneAutoSpawnConfig {
  enabled: boolean
  min_coins: number           // Minimum coins to maintain in zone
  max_coins: number           // Maximum coins allowed in zone
  coin_type: CoinType         // Type of coins to spawn
  min_value: number           // Minimum coin value
  max_value: number           // Maximum coin value
  tier_weights: {             // Probability weights for each tier
    gold: number
    silver: number
    bronze: number
  }
  respawn_delay_seconds: number  // Time before respawning after collection
}

/**
 * Timed release configuration for hunt zones
 * Defines how coins are released over time during a hunt
 */
export interface ZoneTimedReleaseConfig {
  enabled: boolean
  total_coins: number         // Total coins to release
  release_interval_seconds: number  // Time between releases
  coins_per_release: number   // Coins released each interval
  start_time: string          // ISO timestamp when release begins
  end_time?: string           // Optional end time
}

/**
 * Hunt type configuration (matches treasure-hunt-types.md)
 */
export type HuntType = 
  | 'direct_navigation'    // Type 1: Full guidance
  | 'compass_only'         // Type 2: Direction without distance
  | 'pure_ar'             // Type 3: Visual only
  | 'radar_only'          // Type 4: Hot-cold vibration
  | 'timed_release'       // Type 5: Coins appear over time
  | 'multi_find_race'     // Type 6: Gold/Silver/Bronze
  | 'sequential'          // Type 7: One at a time

/**
 * Hunt configuration for zones
 */
export interface ZoneHuntConfig {
  hunt_type: HuntType
  show_distance: boolean
  enable_compass: boolean
  map_marker_type: 'exact' | 'general' | 'zone_only'
  vibration_mode: 'all' | 'last_100m' | 'off'
  multi_find_enabled: boolean
  max_finders?: number
  hunt_duration_hours?: number
}

/**
 * Main Zone interface
 */
export interface Zone {
  id: string
  name: string
  description: string | null
  zone_type: ZoneType
  status: ZoneStatus
  geometry: ZoneGeometry
  
  // Ownership
  owner_id: string | null      // User who created the zone
  sponsor_id: string | null    // If sponsor zone, the sponsor
  
  // Auto-spawn configuration
  auto_spawn_config: ZoneAutoSpawnConfig | null
  
  // Timed release (for hunt zones)
  timed_release_config: ZoneTimedReleaseConfig | null
  
  // Hunt configuration
  hunt_config: ZoneHuntConfig | null
  
  // Scheduling
  start_time: string | null    // When zone becomes active
  end_time: string | null      // When zone deactivates
  
  // Statistics
  coins_placed: number
  coins_collected: number
  total_value_distributed: number
  active_players: number
  
  // Visual customization
  fill_color: string | null    // Zone fill color (hex)
  border_color: string | null  // Zone border color (hex)
  opacity: number              // Fill opacity (0-1)
  
  // Metadata
  metadata: Record<string, unknown> | null
  created_at: string
  updated_at: string
}

/**
 * Zone with related data (joined)
 */
export interface ZoneWithRelations extends Zone {
  owner?: UserProfile | null
  sponsor?: Sponsor | null
  coins?: Coin[]
}

/**
 * Zone statistics summary
 */
export interface ZoneStats {
  total_zones: number
  active_zones: number
  player_zones: number
  sponsor_zones: number
  hunt_zones: number
  total_coins_in_zones: number
  total_active_players: number
}

/**
 * Create zone input (for API/forms)
 */
export interface CreateZoneInput {
  name: string
  description?: string
  zone_type: ZoneType
  geometry: ZoneGeometry
  sponsor_id?: string
  auto_spawn_config?: ZoneAutoSpawnConfig
  timed_release_config?: ZoneTimedReleaseConfig
  hunt_config?: ZoneHuntConfig
  start_time?: string
  end_time?: string
  fill_color?: string
  border_color?: string
  opacity?: number
}

// ============================================================================
// PLAYER TRACKING TYPES - Phase M4: Player Tracking
// ============================================================================

/**
 * Player activity status based on last update time
 * - active: Updated within last 30 seconds
 * - idle: Updated within last 5 minutes
 * - stale: Updated within last 30 minutes
 * - offline: No update for 30+ minutes
 */
export type PlayerActivityStatus = 'active' | 'idle' | 'stale' | 'offline'

/**
 * Player movement type for anti-cheat detection
 * - walking: Normal speed (0-6 km/h)
 * - running: Fast movement (6-20 km/h)
 * - driving: Vehicle speed (20-120 km/h)
 * - suspicious: Impossible speed (>120 km/h or teleporting)
 */
export type PlayerMovementType = 'walking' | 'running' | 'driving' | 'suspicious'

/**
 * Player location data from the Unity app
 * This is updated in real-time as players move
 */
export interface PlayerLocation {
  id: string
  user_id: string
  
  // Current position
  latitude: number
  longitude: number
  altitude: number | null
  
  // Accuracy & quality
  accuracy_meters: number        // GPS accuracy radius
  heading: number | null         // Direction in degrees (0-360)
  speed_mps: number | null       // Speed in meters per second
  
  // Device info
  device_id: string | null       // Unique device identifier
  device_model: string | null    // e.g., "OnePlus 9 Pro"
  app_version: string | null     // Unity app version
  
  // Session info
  session_id: string | null      // Current play session ID
  is_ar_active: boolean          // Currently in AR mode
  
  // Anti-cheat metadata
  is_mock_location: boolean      // Device reports mock location enabled
  movement_type: PlayerMovementType
  distance_traveled_session: number  // Total meters this session
  
  // Zone context
  current_zone_id: string | null // Zone player is currently in
  
  // Timestamps
  client_timestamp: string       // When device recorded position
  server_timestamp: string       // When server received position
  created_at: string
  updated_at: string
}

/**
 * Player location with user profile data (joined)
 */
export interface PlayerLocationWithUser extends PlayerLocation {
  user?: UserProfile | null
  current_zone?: Zone | null
}

// ============================================================================
// ANTI-CHEAT TYPES - Phase M8: Anti-Cheat
// ============================================================================

/**
 * Cheat detection reason types
 */
export type CheatReason = 
  | 'gps_spoofing'           // GPS location spoofing detected
  | 'impossible_speed'       // Traveled at impossible speed
  | 'teleportation'          // Instant location change
  | 'mock_location'          // Mock location enabled
  | 'device_tampering'       // Rooted/jailbroken device
  | 'emulator_detected'      // Running on emulator
  | 'app_tampering'          // App modified/patched
  | 'suspicious_pattern'     // Unusual behavior pattern
  | 'multiple_devices'       // Same account on multiple devices simultaneously
  | 'location_inconsistency' // Location doesn't match expected pattern

/**
 * Cheat flag severity levels
 */
export type CheatSeverity = 'low' | 'medium' | 'high' | 'critical'

/**
 * Cheat flag status
 */
export type CheatFlagStatus = 
  | 'pending'      // Flagged, awaiting review
  | 'investigating' // Under investigation
  | 'confirmed'    // Confirmed as cheating
  | 'false_positive' // False alarm, cleared
  | 'resolved'     // Issue resolved

/**
 * Player action taken after cheat detection
 */
export type PlayerAction = 
  | 'none'         // No action taken
  | 'warned'       // Warning issued
  | 'suspended'     // Temporary suspension
  | 'banned'       // Permanent ban
  | 'cleared'      // Cleared of suspicion

/**
 * Cheat detection flag
 */
export interface CheatFlag {
  id: string
  user_id: string
  
  // Detection details
  reason: CheatReason
  severity: CheatSeverity
  status: CheatFlagStatus
  action_taken: PlayerAction
  
  // Evidence
  evidence: {
    // Location data
    previous_location?: {
      latitude: number
      longitude: number
      timestamp: string
    }
    current_location?: {
      latitude: number
      longitude: number
      timestamp: string
    }
    
    // Speed/distance
    distance_meters?: number
    time_seconds?: number
    calculated_speed_kmh?: number
    reported_speed_kmh?: number
    
    // Device info
    device_id?: string
    device_model?: string
    is_mock_location?: boolean
    is_rooted?: boolean
    is_emulator?: boolean
    
    // Additional context
    session_id?: string
    coin_collections_during_incident?: number
    [key: string]: unknown
  }
  
  // Metadata
  detected_at: string
  detected_by?: string  // System or admin user ID
  reviewed_by?: string  // Admin who reviewed
  reviewed_at?: string
  notes?: string
  
  // Timestamps
  created_at: string
  updated_at: string
}

/**
 * Cheat detection statistics
 */
export interface AntiCheatStats {
  // Flag counts
  total_flags: number
  pending_flags: number
  confirmed_cheaters: number
  false_positives: number
  
  // By reason
  flags_by_reason: Record<CheatReason, number>
  
  // By severity
  flags_by_severity: Record<CheatSeverity, number>
  
  // Actions taken
  players_warned: number
  players_suspended: number
  players_banned: number
  
  // Time-based
  flags_today: number
  flags_this_week: number
  flags_this_month: number
  
  // Detection rate
  detection_rate: number  // Percentage of suspicious activity detected
}

/**
 * Player with cheat flags
 */
export interface FlaggedPlayer {
  user_id: string
  user_name: string
  user_email: string
  
  // Flag summary
  total_flags: number
  active_flags: number
  highest_severity: CheatSeverity
  current_action: PlayerAction
  
  // Recent activity
  last_flag_at: string | null
  last_location_update: string | null
  current_movement_type: PlayerMovementType
  
  // Device info
  device_id: string | null
  is_mock_location: boolean
  is_rooted: boolean | null
  
  // Flags
  flags: CheatFlag[]
}

/**
 * Real-time player data for map display
 * Simplified version with computed activity status
 */
export interface ActivePlayer {
  id: string
  user_id: string
  user_name: string | null
  avatar_url: string | null
  
  // Position
  latitude: number
  longitude: number
  accuracy_meters: number
  heading: number | null
  
  // Status
  activity_status: PlayerActivityStatus
  is_ar_active: boolean
  movement_type: PlayerMovementType
  
  // Context
  current_zone_id: string | null
  current_zone_name: string | null
  
  // Session stats
  coins_collected_session: number
  time_active_minutes: number
  
  // Timestamps
  last_updated: string
}

/**
 * Player tracking statistics for dashboard
 */
export interface PlayerTrackingStats {
  total_active_players: number       // Players updated in last 30 seconds
  total_idle_players: number         // Players updated in last 5 minutes
  total_players_today: number        // Unique players today
  players_in_ar_mode: number         // Currently hunting in AR
  players_by_zone: Record<string, number>  // Count per zone
  suspicious_players: number         // Flagged for review
  average_session_minutes: number    // Average play session length
  total_distance_traveled_km: number // Combined distance today
}

/**
 * Player location history point (for trails)
 */
export interface PlayerLocationHistory {
  id: string
  user_id: string
  latitude: number
  longitude: number
  accuracy_meters: number
  speed_mps: number | null
  movement_type: PlayerMovementType
  recorded_at: string
}

/**
 * Player trail configuration
 */
export interface PlayerTrailConfig {
  enabled: boolean
  max_points: number           // Max points to show in trail
  max_age_minutes: number      // Remove points older than this
  show_speed_colors: boolean   // Color trail by speed
  line_width: number
  opacity: number
}

/**
 * Cluster of players (for high-density areas)
 */
export interface PlayerCluster {
  id: string
  center: {
    latitude: number
    longitude: number
  }
  player_count: number
  players: ActivePlayer[]
  bounds: {
    north: number
    south: number
    east: number
    west: number
  }
}

// ============================================================================
// AUTO-DISTRIBUTION TYPES - Phase M5: Auto-Distribution
// ============================================================================

/**
 * Distribution system status
 * - running: System is actively spawning coins
 * - paused: System is paused by admin
 * - stopped: System is stopped (no auto-spawn)
 * - error: System encountered an error
 */
export type DistributionSystemStatus = 'running' | 'paused' | 'stopped' | 'error'

/**
 * Spawn trigger type - what caused the spawn
 * - auto: Automatic spawn to maintain minimum
 * - scheduled: Scheduled release
 * - manual: Admin triggered spawn
 * - recycle: Recycled stale coin
 */
export type SpawnTriggerType = 'auto' | 'scheduled' | 'manual' | 'recycle'

/**
 * Value distribution strategy
 * - uniform: Equal probability across range
 * - weighted_low: More low-value coins
 * - weighted_high: More high-value coins
 * - tiered: Follows tier_weights configuration
 * - random: Completely random within range
 */
export type ValueDistributionStrategy = 'uniform' | 'weighted_low' | 'weighted_high' | 'tiered' | 'random'

/**
 * Grid cell for distribution planning
 * Used to visualize coin density across zones
 */
export interface DistributionGridCell {
  id: string
  bounds: {
    north: number
    south: number
    east: number
    west: number
  }
  center: {
    latitude: number
    longitude: number
  }
  coin_count: number
  target_count: number
  needs_spawn: boolean
  zone_id: string | null
}

/**
 * Spawn queue item - coin waiting to be spawned
 */
export interface SpawnQueueItem {
  id: string
  zone_id: string
  zone_name: string
  trigger_type: SpawnTriggerType
  scheduled_time: string
  coin_config: {
    coin_type: CoinType
    tier: CoinTier
    min_value: number
    max_value: number
    is_mythical: boolean
  }
  target_location?: {
    latitude: number
    longitude: number
  }
  status: 'pending' | 'processing' | 'completed' | 'failed'
  error_message?: string
  created_at: string
}

/**
 * Spawn result after attempting to create a coin
 */
export interface SpawnResult {
  success: boolean
  coin_id?: string
  coin?: Coin
  error_message?: string
  spawn_location: {
    latitude: number
    longitude: number
  }
  trigger_type: SpawnTriggerType
  zone_id: string
  spawned_at: string
}

/**
 * Zone distribution status - real-time status for a zone
 */
export interface ZoneDistributionStatus {
  zone_id: string
  zone_name: string
  zone_type: ZoneType
  
  // Configuration
  auto_spawn_enabled: boolean
  min_coins: number
  max_coins: number
  
  // Current state
  current_coin_count: number
  active_coins: number        // Visible/hidden status
  collected_today: number
  
  // Status
  needs_spawn: boolean
  coins_to_spawn: number      // How many coins needed to reach minimum
  next_spawn_time?: string    // When next automatic spawn will occur
  
  // Performance
  average_collection_time_hours: number
  spawn_rate_per_hour: number
  collection_rate_per_hour: number
}

/**
 * Global distribution statistics
 */
export interface DistributionStats {
  // System status
  system_status: DistributionSystemStatus
  last_spawn_time: string | null
  next_scheduled_spawn: string | null
  
  // Counts
  total_zones_with_auto_spawn: number
  zones_needing_spawn: number
  queue_length: number
  
  // Today's activity
  coins_spawned_today: number
  coins_collected_today: number
  coins_recycled_today: number
  
  // Value tracking
  total_value_spawned_today: number
  total_value_collected_today: number
  average_coin_value: number
  
  // Performance
  average_spawn_time_ms: number
  spawn_success_rate: number
  errors_today: number
}

/**
 * Distribution configuration for the entire system
 */
export interface DistributionConfig {
  // Global settings
  enabled: boolean
  check_interval_seconds: number      // How often to check for spawn needs
  max_spawns_per_cycle: number        // Limit spawns per check cycle
  
  // Default zone settings
  default_min_coins: number
  default_max_coins: number
  default_value_range: {
    min: number
    max: number
  }
  default_tier_weights: {
    gold: number
    silver: number
    bronze: number
  }
  
  // Value distribution
  value_strategy: ValueDistributionStrategy
  mythical_spawn_chance: number       // 0-1 probability
  
  // Recycling
  recycle_enabled: boolean
  recycle_after_hours: number         // Recycle unfound coins after X hours
  recycle_to_new_location: boolean    // Move recycled coins to new spot
  
  // Rate limiting
  max_spawns_per_hour: number
  cooldown_after_collection_seconds: number
}

/**
 * Spawn history entry for auditing
 */
export interface SpawnHistoryEntry {
  id: string
  coin_id: string
  zone_id: string
  zone_name: string
  trigger_type: SpawnTriggerType
  coin_value: number
  coin_tier: CoinTier
  spawn_location: {
    latitude: number
    longitude: number
  }
  spawned_at: string
  collected_at?: string
  collected_by_user_id?: string
  recycled_at?: string
  time_to_collection_hours?: number
}

/**
 * Distribution action for admin commands
 */
export type DistributionAction = 
  | { type: 'start' }
  | { type: 'pause' }
  | { type: 'stop' }
  | { type: 'spawn_now'; zone_id: string; count: number }
  | { type: 'recycle_stale'; zone_id?: string }
  | { type: 'clear_queue' }
  | { type: 'update_config'; config: Partial<DistributionConfig> }

/**
 * Zone spawn preview - for visualizing where coins would spawn
 */
export interface ZoneSpawnPreview {
  zone_id: string
  spawn_points: Array<{
    latitude: number
    longitude: number
    suggested_value: number
    suggested_tier: CoinTier
  }>
  total_value: number
  estimated_time_to_collection_hours: number
}

// ============================================================================
// TIMED RELEASES - Phase M6: Timed Releases
// ============================================================================

/**
 * Release schedule status
 */
export type ReleaseScheduleStatus = 'scheduled' | 'active' | 'paused' | 'completed' | 'cancelled'

/**
 * Single release batch (e.g., "5 coins at 2:00 PM")
 */
export interface ReleaseBatch {
  id: string
  schedule_id: string
  zone_id: string
  zone_name: string
  release_at: string           // ISO timestamp
  coins_count: number
  coins_released: number       // How many actually spawned
  status: 'pending' | 'released' | 'partial' | 'failed'
  error_message?: string
}

/**
 * Timed release schedule (hunt event or zone batch)
 */
export interface ReleaseSchedule {
  id: string
  zone_id: string
  zone_name: string
  name: string
  description: string | null
  
  // Configuration
  total_coins: number
  coins_per_release: number
  release_interval_seconds: number
  start_time: string
  end_time: string | null
  
  // Status
  status: ReleaseScheduleStatus
  coins_released_so_far: number
  batches_completed: number
  batches_total: number
  
  // Timestamps
  next_release_at: string | null
  last_release_at: string | null
  created_at: string
  updated_at: string
}

/**
 * Release queue item - upcoming batch
 */
export interface ReleaseQueueItem {
  id: string
  schedule_id: string
  schedule_name: string
  zone_id: string
  zone_name: string
  release_at: string
  coins_count: number
  status: 'pending' | 'releasing'
  time_until_seconds: number
}

/**
 * Timed release statistics
 */
export interface TimedReleaseStats {
  active_schedules: number
  scheduled_today: number
  completed_today: number
  total_coins_released_today: number
  total_value_released_today: number
  next_release_in_seconds: number | null
  next_release_zone: string | null
}
