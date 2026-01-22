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
