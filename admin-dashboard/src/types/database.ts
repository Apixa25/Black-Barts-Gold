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
