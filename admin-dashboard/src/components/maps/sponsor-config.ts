/**
 * Sponsor Features Configuration
 * Phase M7: Sponsor Features
 * 
 * Provides configuration, utilities, and helpers for sponsor zone management,
 * bulk coin placement, analytics, and fee calculation.
 */

import type { SponsorZoneFeeConfig, BulkCoinPlacementConfig } from "@/types/database"

// ============================================================================
// SPONSOR ZONE FEES
// ============================================================================

/**
 * Default sponsor zone fee configuration
 */
export const DEFAULT_SPONSOR_ZONE_FEES: SponsorZoneFeeConfig = {
  zone_type: 'sponsor',
  
  // Base fees
  zone_creation_fee: 99.00, // $99 one-time to create a sponsor zone
  monthly_maintenance_fee: 49.00, // $49/month to keep zone active
  
  // Coin placement fees
  coin_placement_fee_per_coin: 0.25, // $0.25 per coin placed
  bulk_placement_discount_percentage: 10, // 10% off for 50+ coins
  
  // Performance-based fees
  collection_fee_percentage: 5, // 5% of coin value when collected
  
  // Minimums
  minimum_zone_size_km2: 0.1, // Minimum 0.1 km² (about 316m radius)
  minimum_coins_per_zone: 10, // Must place at least 10 coins
  minimum_monthly_spend: 100.00, // $100/month minimum
}

/**
 * Calculate total cost for bulk coin placement
 */
export function calculateBulkPlacementCost(
  config: BulkCoinPlacementConfig,
  feeConfig: SponsorZoneFeeConfig = DEFAULT_SPONSOR_ZONE_FEES
): number {
  const baseCost = config.coin_count * feeConfig.coin_placement_fee_per_coin
  
  // Apply bulk discount
  let discount = 0
  if (config.coin_count >= 50) {
    discount = baseCost * (feeConfig.bulk_placement_discount_percentage / 100)
  }
  
  return baseCost - discount
}

/**
 * Calculate monthly cost for a sponsor zone
 */
export function calculateMonthlyZoneCost(
  coinsInZone: number,
  feeConfig: SponsorZoneFeeConfig = DEFAULT_SPONSOR_ZONE_FEES
): number {
  const maintenanceFee = feeConfig.monthly_maintenance_fee
  const coinFees = coinsInZone * feeConfig.coin_placement_fee_per_coin
  
  return maintenanceFee + coinFees
}

// ============================================================================
// BULK PLACEMENT STRATEGIES
// ============================================================================

/**
 * Distribution strategy presets
 */
export const DISTRIBUTION_STRATEGY_PRESETS = {
  random: {
    label: 'Random Distribution',
    description: 'Coins placed randomly throughout the zone',
    icon: '🎲',
  },
  grid: {
    label: 'Grid Pattern',
    description: 'Coins placed in a regular grid pattern',
    icon: '📐',
  },
  cluster: {
    label: 'Clustered',
    description: 'Coins grouped in clusters for high-traffic areas',
    icon: '📍',
  },
  perimeter: {
    label: 'Perimeter',
    description: 'Coins placed along zone boundaries',
    icon: '🔲',
  },
} as const

/**
 * Default bulk placement configuration
 */
export const DEFAULT_BULK_PLACEMENT_CONFIG: Omit<BulkCoinPlacementConfig, 'sponsor_id'> = {
  zone_id: null,
  coin_count: 25,
  distribution_strategy: 'random',
  value_range: {
    min: 0.25,
    max: 5.00,
  },
  tier_distribution: {
    gold: 20, // 20% gold
    silver: 40, // 40% silver
    bronze: 40, // 40% bronze
  },
  release_all_at_once: true,
  scheduled_release_time: null,
  min_distance_between_coins_meters: 10, // 10 meters minimum spacing
  avoid_existing_coins: true,
}

// ============================================================================
// ANALYTICS UTILITIES
// ============================================================================

/**
 * Calculate performance score (0-100) for a sponsor zone
 */
export function calculatePerformanceScore(
  coinsPlaced: number,
  coinsCollected: number,
  totalValuePlaced: number,
  totalValueCollected: number,
  uniqueCollectors: number,
  daysActive: number
): number {
  if (coinsPlaced === 0) return 0
  
  // Collection rate (0-40 points)
  const collectionRate = coinsCollected / coinsPlaced
  const collectionScore = Math.min(collectionRate * 40, 40)
  
  // Value efficiency (0-30 points)
  const valueEfficiency = totalValuePlaced > 0 
    ? (totalValueCollected / totalValuePlaced) * 30
    : 0
  const valueScore = Math.min(valueEfficiency, 30)
  
  // Engagement (0-20 points)
  const engagementScore = Math.min((uniqueCollectors / Math.max(coinsPlaced, 1)) * 20, 20)
  
  // Activity (0-10 points) - bonus for recent activity
  const activityScore = daysActive > 0 
    ? Math.min(10 / (1 + daysActive / 30), 10) // Bonus for zones active < 30 days
    : 0
  
  return Math.round(collectionScore + valueScore + engagementScore + activityScore)
}

/**
 * Calculate ROI percentage
 */
export function calculateROI(
  totalSpent: number,
  totalValueCollected: number
): number {
  if (totalSpent === 0) return 0
  return ((totalValueCollected - totalSpent) / totalSpent) * 100
}

/**
 * Calculate cost per collection
 */
export function calculateCostPerCollection(
  totalSpent: number,
  totalCollections: number
): number {
  if (totalCollections === 0) return totalSpent
  return totalSpent / totalCollections
}

/**
 * Calculate cost per unique collector
 */
export function calculateCostPerCollector(
  totalSpent: number,
  uniqueCollectors: number
): number {
  if (uniqueCollectors === 0) return totalSpent
  return totalSpent / uniqueCollectors
}

/**
 * Format performance score with color coding
 */
export function formatPerformanceScore(score: number): {
  label: string
  color: string
  description: string
} {
  if (score >= 80) {
    return {
      label: 'Excellent',
      color: 'text-green-600',
      description: 'Outstanding performance',
    }
  } else if (score >= 60) {
    return {
      label: 'Good',
      color: 'text-blue-600',
      description: 'Above average performance',
    }
  } else if (score >= 40) {
    return {
      label: 'Fair',
      color: 'text-yellow-600',
      description: 'Average performance',
    }
  } else {
    return {
      label: 'Needs Improvement',
      color: 'text-red-600',
      description: 'Below average performance',
    }
  }
}

// ============================================================================
// VALIDATION
// ============================================================================

/**
 * Validate bulk placement configuration
 */
export function validateBulkPlacementConfig(
  config: BulkCoinPlacementConfig
): { valid: boolean; errors: string[] } {
  const errors: string[] = []
  
  if (config.coin_count < 1) {
    errors.push('Must place at least 1 coin')
  }
  
  if (config.coin_count > 1000) {
    errors.push('Cannot place more than 1000 coins at once')
  }
  
  if (config.value_range.min < 0) {
    errors.push('Minimum value cannot be negative')
  }
  
  if (config.value_range.max < config.value_range.min) {
    errors.push('Maximum value must be greater than minimum value')
  }
  
  const tierSum = config.tier_distribution.gold + 
                  config.tier_distribution.silver + 
                  config.tier_distribution.bronze
  
  if (Math.abs(tierSum - 100) > 0.01) {
    errors.push('Tier distribution must sum to 100%')
  }
  
  if (config.min_distance_between_coins_meters < 1) {
    errors.push('Minimum distance must be at least 1 meter')
  }
  
  return {
    valid: errors.length === 0,
    errors,
  }
}
