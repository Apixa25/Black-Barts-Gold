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
export type CoinStatus = 'hidden' | 'visible' | 'collected' | 'expired'
export type CoinTier = 'gold' | 'silver' | 'bronze'

export interface Coin {
  id: string
  coin_type: CoinType
  value: number
  latitude: number
  longitude: number
  hider_id: string
  hidden_at: string
  collected_by: string | null
  collected_at: string | null
  status: CoinStatus
  tier: CoinTier
  is_mythical: boolean
  sponsor_id: string | null
  logo_url: string | null
  created_at: string
  updated_at: string
}

// Transaction types
export type TransactionType = 'found' | 'hidden' | 'gas_consumed' | 'purchased' | 'transfer' | 'payout'
export type TransactionStatus = 'pending' | 'confirmed' | 'failed'

export interface Transaction {
  id: string
  user_id: string
  transaction_type: TransactionType
  amount: number
  coin_id: string | null
  status: TransactionStatus
  metadata?: Record<string, unknown>
  created_at: string
  confirmed_at: string | null
}

// Sponsor
export interface Sponsor {
  id: string
  company_name: string
  contact_email: string
  logo_url: string | null
  coins_purchased: number
  coins_found: number
  is_active: boolean
  created_at: string
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

// Navigation item type
export interface NavItem {
  name: string
  href: string
  icon: React.ComponentType<{ className?: string }>
}
