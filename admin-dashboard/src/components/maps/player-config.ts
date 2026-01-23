/**
 * Player Tracking Configuration
 * 
 * @file admin-dashboard/src/components/maps/player-config.ts
 * @description Colors, styling, and configuration for player tracking on maps
 * 
 * Character count: ~3,800
 */

import type { PlayerActivityStatus, PlayerMovementType } from "@/types/database"

// ============================================================================
// TIMING CONFIGURATION
// ============================================================================

/**
 * Time thresholds for determining player activity status (in seconds)
 */
export const PLAYER_ACTIVITY_THRESHOLDS = {
  /** Player is active if updated within this many seconds */
  active: 30,
  /** Player is idle if updated within this many seconds */
  idle: 5 * 60,      // 5 minutes
  /** Player is stale if updated within this many seconds */
  stale: 30 * 60,    // 30 minutes
  /** Beyond stale threshold = offline */
} as const

/**
 * Update intervals for real-time tracking
 */
export const PLAYER_UPDATE_INTERVALS = {
  /** How often Unity app sends location (ms) */
  clientUpdate: 5000,     // 5 seconds
  /** How often dashboard refreshes player list (ms) */
  dashboardRefresh: 3000, // 3 seconds
  /** Debounce for map re-renders (ms) */
  mapDebounce: 500,
} as const

// ============================================================================
// SPEED THRESHOLDS (Anti-Cheat)
// ============================================================================

/**
 * Speed thresholds in km/h for movement type detection
 */
export const SPEED_THRESHOLDS = {
  walking: 6,      // 0-6 km/h = walking
  running: 20,     // 6-20 km/h = running
  driving: 120,    // 20-120 km/h = driving
  // > 120 km/h = suspicious
} as const

// ============================================================================
// COLORS & STYLING
// ============================================================================

/**
 * Colors for player activity status
 */
export const PLAYER_STATUS_COLORS: Record<PlayerActivityStatus, {
  fill: string
  border: string
  pulse: string
  label: string
  emoji: string
}> = {
  active: {
    fill: "#22C55E",      // Green
    border: "#16A34A",
    pulse: "#22C55E",
    label: "Active",
    emoji: "🟢",
  },
  idle: {
    fill: "#FBBF24",      // Yellow/Gold
    border: "#D97706",
    pulse: "#FBBF24",
    label: "Idle",
    emoji: "🟡",
  },
  stale: {
    fill: "#9CA3AF",      // Gray
    border: "#6B7280",
    pulse: "#9CA3AF",
    label: "Stale",
    emoji: "⚪",
  },
  offline: {
    fill: "#DC2626",      // Red
    border: "#B91C1C",
    pulse: "#DC2626",
    label: "Offline",
    emoji: "🔴",
  },
}

/**
 * Colors for movement type (used in trails and warnings)
 */
export const MOVEMENT_TYPE_COLORS: Record<PlayerMovementType, {
  color: string
  label: string
  emoji: string
}> = {
  walking: {
    color: "#22C55E",     // Green
    label: "Walking",
    emoji: "🚶",
  },
  running: {
    color: "#3B82F6",     // Blue
    label: "Running",
    emoji: "🏃",
  },
  driving: {
    color: "#8B5CF6",     // Purple
    label: "Driving",
    emoji: "🚗",
  },
  suspicious: {
    color: "#DC2626",     // Red
    label: "Suspicious",
    emoji: "⚠️",
  },
}

// ============================================================================
// MAP MARKER CONFIGURATION
// ============================================================================

/**
 * Player marker sizes based on zoom level
 */
export const PLAYER_MARKER_SIZES = {
  default: 32,
  selected: 40,
  clustered: 48,
  minZoom: 20,    // Smallest at low zoom
  maxZoom: 44,    // Largest at high zoom
} as const

/**
 * Player marker styling
 */
export const PLAYER_MARKER_STYLE = {
  /** Border width in pixels */
  borderWidth: 2,
  /** Shadow blur radius */
  shadowBlur: 4,
  /** Shadow offset */
  shadowOffset: 2,
  /** Inner avatar size ratio (0-1) */
  avatarRatio: 0.7,
  /** Pulse animation duration (ms) */
  pulseDuration: 2000,
  /** Direction indicator length */
  headingIndicatorLength: 16,
} as const

// ============================================================================
// CLUSTERING CONFIGURATION
// ============================================================================

/**
 * Player clustering settings for performance at scale
 */
export const PLAYER_CLUSTERING = {
  /** Enable clustering */
  enabled: true,
  /** Minimum players to form a cluster */
  minPoints: 3,
  /** Cluster radius in pixels */
  radius: 50,
  /** Max zoom level for clustering (beyond this, show individuals) */
  maxZoom: 16,
  /** Show count badge on clusters */
  showCount: true,
} as const

// ============================================================================
// TRAIL CONFIGURATION
// ============================================================================

/**
 * Default player trail configuration
 */
export const DEFAULT_TRAIL_CONFIG = {
  enabled: false,
  maxPoints: 50,
  maxAgeMinutes: 10,
  showSpeedColors: true,
  lineWidth: 2,
  opacity: 0.6,
} as const

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

/**
 * Get activity status based on last update time
 */
export function getActivityStatus(lastUpdated: string | Date): PlayerActivityStatus {
  const lastUpdate = typeof lastUpdated === 'string' ? new Date(lastUpdated) : lastUpdated
  const secondsAgo = (Date.now() - lastUpdate.getTime()) / 1000
  
  if (secondsAgo <= PLAYER_ACTIVITY_THRESHOLDS.active) return 'active'
  if (secondsAgo <= PLAYER_ACTIVITY_THRESHOLDS.idle) return 'idle'
  if (secondsAgo <= PLAYER_ACTIVITY_THRESHOLDS.stale) return 'stale'
  return 'offline'
}

/**
 * Get movement type based on speed (km/h)
 */
export function getMovementType(speedKmh: number): PlayerMovementType {
  if (speedKmh <= SPEED_THRESHOLDS.walking) return 'walking'
  if (speedKmh <= SPEED_THRESHOLDS.running) return 'running'
  if (speedKmh <= SPEED_THRESHOLDS.driving) return 'driving'
  return 'suspicious'
}

/**
 * Convert meters per second to km/h
 */
export function mpsToKmh(mps: number): number {
  return mps * 3.6
}

/**
 * Format time since last update
 */
export function formatLastSeen(lastUpdated: string | Date): string {
  const lastUpdate = typeof lastUpdated === 'string' ? new Date(lastUpdated) : lastUpdated
  const secondsAgo = Math.floor((Date.now() - lastUpdate.getTime()) / 1000)
  
  if (secondsAgo < 60) return `${secondsAgo}s ago`
  if (secondsAgo < 3600) return `${Math.floor(secondsAgo / 60)}m ago`
  if (secondsAgo < 86400) return `${Math.floor(secondsAgo / 3600)}h ago`
  return `${Math.floor(secondsAgo / 86400)}d ago`
}

/**
 * Get status color for a player
 */
export function getPlayerStatusColor(status: PlayerActivityStatus): string {
  return PLAYER_STATUS_COLORS[status].fill
}

/**
 * Get marker size based on zoom level
 */
export function getMarkerSizeForZoom(zoom: number): number {
  const minZoom = 10
  const maxZoom = 18
  const clampedZoom = Math.min(Math.max(zoom, minZoom), maxZoom)
  const ratio = (clampedZoom - minZoom) / (maxZoom - minZoom)
  return PLAYER_MARKER_SIZES.minZoom + ratio * (PLAYER_MARKER_SIZES.maxZoom - PLAYER_MARKER_SIZES.minZoom)
}
