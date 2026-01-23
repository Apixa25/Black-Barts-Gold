/**
 * Auto-Distribution Configuration
 * 
 * @file admin-dashboard/src/components/maps/distribution-config.ts
 * @description Configuration and utilities for automatic coin distribution
 * 
 * Character count: ~6,500
 */

import type { 
  CoinTier, 
  CoinType,
  DistributionConfig,
  ValueDistributionStrategy,
  ZoneAutoSpawnConfig,
  ZoneType
} from "@/types/database"

// ============================================================================
// DEFAULT CONFIGURATION
// ============================================================================

/**
 * Default global distribution configuration
 */
export const DEFAULT_DISTRIBUTION_CONFIG: DistributionConfig = {
  // Global settings
  enabled: true,
  check_interval_seconds: 60,        // Check every minute
  max_spawns_per_cycle: 10,          // Max 10 coins per check
  
  // Default zone settings
  default_min_coins: 3,              // Minimum 3 coins per zone
  default_max_coins: 20,             // Maximum 20 coins per zone
  default_value_range: {
    min: 0.10,
    max: 5.00,
  },
  default_tier_weights: {
    gold: 10,                        // 10% gold
    silver: 30,                      // 30% silver
    bronze: 60,                      // 60% bronze
  },
  
  // Value distribution
  value_strategy: 'tiered',
  mythical_spawn_chance: 0.001,      // 0.1% chance
  
  // Recycling
  recycle_enabled: true,
  recycle_after_hours: 48,           // Recycle after 48 hours
  recycle_to_new_location: true,
  
  // Rate limiting
  max_spawns_per_hour: 100,
  cooldown_after_collection_seconds: 300,  // 5 minute cooldown
}

/**
 * Default auto-spawn config for new zones
 */
export const DEFAULT_ZONE_AUTO_SPAWN: ZoneAutoSpawnConfig = {
  enabled: false,
  min_coins: 3,
  max_coins: 10,
  coin_type: 'fixed',
  min_value: 0.10,
  max_value: 2.00,
  tier_weights: {
    gold: 10,
    silver: 30,
    bronze: 60,
  },
  respawn_delay_seconds: 300,        // 5 minutes
}

// ============================================================================
// TIER CONFIGURATION
// ============================================================================

/**
 * Value ranges for each coin tier
 */
export const TIER_VALUE_RANGES: Record<CoinTier, { min: number; max: number }> = {
  bronze: { min: 0.01, max: 0.50 },
  silver: { min: 0.50, max: 2.00 },
  gold: { min: 2.00, max: 10.00 },
}

/**
 * Colors for each tier (matches existing design)
 */
export const TIER_COLORS: Record<CoinTier, { fill: string; border: string; label: string }> = {
  bronze: { fill: '#CD7F32', border: '#8B5A2B', label: 'Bronze' },
  silver: { fill: '#C0C0C0', border: '#808080', label: 'Silver' },
  gold: { fill: '#FFD700', border: '#DAA520', label: 'Gold' },
}

// ============================================================================
// ZONE TYPE PRESETS
// ============================================================================

/**
 * Recommended auto-spawn settings by zone type
 */
export const ZONE_TYPE_SPAWN_PRESETS: Record<ZoneType, Partial<ZoneAutoSpawnConfig>> = {
  player: {
    enabled: true,
    min_coins: 3,
    max_coins: 10,
    min_value: 0.10,
    max_value: 2.00,
    tier_weights: { gold: 10, silver: 30, bronze: 60 },
    respawn_delay_seconds: 300,
  },
  sponsor: {
    enabled: true,
    min_coins: 5,
    max_coins: 25,
    min_value: 0.25,
    max_value: 5.00,
    tier_weights: { gold: 20, silver: 40, bronze: 40 },
    respawn_delay_seconds: 180,
  },
  hunt: {
    enabled: false,  // Hunt zones use timed release instead
    min_coins: 10,
    max_coins: 50,
    min_value: 0.50,
    max_value: 10.00,
    tier_weights: { gold: 30, silver: 40, bronze: 30 },
    respawn_delay_seconds: 0,
  },
  grid: {
    enabled: true,
    min_coins: 5,
    max_coins: 15,
    min_value: 0.10,
    max_value: 1.00,
    tier_weights: { gold: 5, silver: 25, bronze: 70 },
    respawn_delay_seconds: 600,
  },
}

// ============================================================================
// SPAWN LOCATION UTILITIES
// ============================================================================

/**
 * Generate a random point within a circle
 */
export function randomPointInCircle(
  centerLat: number,
  centerLng: number,
  radiusMeters: number
): { latitude: number; longitude: number } {
  // Random angle and distance
  const angle = Math.random() * 2 * Math.PI
  const distance = Math.sqrt(Math.random()) * radiusMeters
  
  // Convert to lat/lng offset
  const latOffset = (distance / 111320) * Math.cos(angle)
  const lngOffset = (distance / (111320 * Math.cos(centerLat * Math.PI / 180))) * Math.sin(angle)
  
  return {
    latitude: centerLat + latOffset,
    longitude: centerLng + lngOffset,
  }
}

/**
 * Generate a random point within a polygon
 * Uses rejection sampling
 */
export function randomPointInPolygon(
  polygon: Array<{ latitude: number; longitude: number }>
): { latitude: number; longitude: number } {
  // Get bounding box
  const lats = polygon.map(p => p.latitude)
  const lngs = polygon.map(p => p.longitude)
  const minLat = Math.min(...lats)
  const maxLat = Math.max(...lats)
  const minLng = Math.min(...lngs)
  const maxLng = Math.max(...lngs)
  
  // Rejection sampling - try up to 100 times
  for (let i = 0; i < 100; i++) {
    const lat = minLat + Math.random() * (maxLat - minLat)
    const lng = minLng + Math.random() * (maxLng - minLng)
    
    if (isPointInPolygon({ latitude: lat, longitude: lng }, polygon)) {
      return { latitude: lat, longitude: lng }
    }
  }
  
  // Fallback to centroid
  return {
    latitude: (minLat + maxLat) / 2,
    longitude: (minLng + maxLng) / 2,
  }
}

/**
 * Check if point is inside polygon (ray casting)
 */
export function isPointInPolygon(
  point: { latitude: number; longitude: number },
  polygon: Array<{ latitude: number; longitude: number }>
): boolean {
  let inside = false
  const x = point.longitude
  const y = point.latitude
  
  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const xi = polygon[i].longitude
    const yi = polygon[i].latitude
    const xj = polygon[j].longitude
    const yj = polygon[j].latitude
    
    if (((yi > y) !== (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi)) {
      inside = !inside
    }
  }
  
  return inside
}

// ============================================================================
// VALUE CALCULATION UTILITIES
// ============================================================================

/**
 * Select a tier based on weights
 */
export function selectTierByWeight(weights: { gold: number; silver: number; bronze: number }): CoinTier {
  const total = weights.gold + weights.silver + weights.bronze
  const random = Math.random() * total
  
  if (random < weights.bronze) return 'bronze'
  if (random < weights.bronze + weights.silver) return 'silver'
  return 'gold'
}

/**
 * Calculate coin value based on strategy and tier
 */
export function calculateCoinValue(
  tier: CoinTier,
  strategy: ValueDistributionStrategy,
  config: { min: number; max: number }
): number {
  const tierRange = TIER_VALUE_RANGES[tier]
  const min = Math.max(config.min, tierRange.min)
  const max = Math.min(config.max, tierRange.max)
  
  let value: number
  
  switch (strategy) {
    case 'uniform':
      value = min + Math.random() * (max - min)
      break
    case 'weighted_low':
      // Bias towards lower values
      value = min + Math.pow(Math.random(), 2) * (max - min)
      break
    case 'weighted_high':
      // Bias towards higher values
      value = min + (1 - Math.pow(1 - Math.random(), 2)) * (max - min)
      break
    case 'tiered':
      // Use tier midpoint with small variance
      const midpoint = (tierRange.min + tierRange.max) / 2
      const variance = (tierRange.max - tierRange.min) * 0.2
      value = midpoint + (Math.random() - 0.5) * variance
      break
    case 'random':
    default:
      value = min + Math.random() * (max - min)
  }
  
  // Round to 2 decimal places
  return Math.round(value * 100) / 100
}

/**
 * Check if a mythical coin should spawn
 */
export function shouldSpawnMythical(chance: number = DEFAULT_DISTRIBUTION_CONFIG.mythical_spawn_chance): boolean {
  return Math.random() < chance
}

// ============================================================================
// SPAWN PLANNING UTILITIES
// ============================================================================

/**
 * Calculate how many coins a zone needs
 */
export function calculateCoinsNeeded(
  currentCount: number,
  minCoins: number,
  maxCoins: number
): number {
  if (currentCount >= minCoins) return 0
  return Math.min(minCoins - currentCount, maxCoins - currentCount)
}

/**
 * Generate spawn plan for a zone
 */
export function generateSpawnPlan(
  zoneConfig: ZoneAutoSpawnConfig,
  currentCoinCount: number,
  valueStrategy: ValueDistributionStrategy = 'tiered'
): Array<{ tier: CoinTier; value: number }> {
  const needed = calculateCoinsNeeded(currentCoinCount, zoneConfig.min_coins, zoneConfig.max_coins)
  if (needed <= 0) return []
  
  const plan: Array<{ tier: CoinTier; value: number }> = []
  
  for (let i = 0; i < needed; i++) {
    const tier = selectTierByWeight(zoneConfig.tier_weights)
    const value = calculateCoinValue(tier, valueStrategy, {
      min: zoneConfig.min_value,
      max: zoneConfig.max_value,
    })
    plan.push({ tier, value })
  }
  
  return plan
}

// ============================================================================
// TIME UTILITIES
// ============================================================================

/**
 * Calculate next spawn time based on respawn delay
 */
export function calculateNextSpawnTime(lastSpawnTime: Date | string, delaySeconds: number): Date {
  const last = typeof lastSpawnTime === 'string' ? new Date(lastSpawnTime) : lastSpawnTime
  return new Date(last.getTime() + delaySeconds * 1000)
}

/**
 * Check if enough time has passed since last spawn
 */
export function canSpawnNow(lastSpawnTime: Date | string | null, delaySeconds: number): boolean {
  if (!lastSpawnTime) return true
  const nextSpawn = calculateNextSpawnTime(lastSpawnTime, delaySeconds)
  return new Date() >= nextSpawn
}

/**
 * Format time until next spawn
 */
export function formatTimeUntilSpawn(nextSpawnTime: Date | string): string {
  const next = typeof nextSpawnTime === 'string' ? new Date(nextSpawnTime) : nextSpawnTime
  const now = new Date()
  const diffMs = next.getTime() - now.getTime()
  
  if (diffMs <= 0) return 'Ready'
  
  const seconds = Math.floor(diffMs / 1000)
  const minutes = Math.floor(seconds / 60)
  const hours = Math.floor(minutes / 60)
  
  if (hours > 0) return `${hours}h ${minutes % 60}m`
  if (minutes > 0) return `${minutes}m ${seconds % 60}s`
  return `${seconds}s`
}

// ============================================================================
// VALIDATION
// ============================================================================

/**
 * Validate auto-spawn configuration
 */
export function validateAutoSpawnConfig(config: ZoneAutoSpawnConfig): { 
  valid: boolean; 
  errors: string[] 
} {
  const errors: string[] = []
  
  if (config.min_coins < 0) errors.push('Minimum coins cannot be negative')
  if (config.max_coins < config.min_coins) errors.push('Maximum coins must be >= minimum coins')
  if (config.min_value < 0) errors.push('Minimum value cannot be negative')
  if (config.max_value < config.min_value) errors.push('Maximum value must be >= minimum value')
  if (config.respawn_delay_seconds < 0) errors.push('Respawn delay cannot be negative')
  
  const totalWeight = config.tier_weights.gold + config.tier_weights.silver + config.tier_weights.bronze
  if (totalWeight <= 0) errors.push('Tier weights must sum to > 0')
  
  return { valid: errors.length === 0, errors }
}
