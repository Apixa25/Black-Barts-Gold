/**
 * Timed Release Configuration
 *
 * @file admin-dashboard/src/components/maps/timed-release-config.ts
 * @description Utilities for scheduled coin releases (M6)
 */

import type { ZoneTimedReleaseConfig, ReleaseScheduleStatus } from "@/types/database"

// ============================================================================
// DEFAULTS
// ============================================================================

export const DEFAULT_RELEASE_INTERVAL_SECONDS = 60
export const DEFAULT_COINS_PER_RELEASE = 1
export const DEFAULT_TOTAL_COINS = 100

export const RELEASE_INTERVAL_PRESETS = [
  { value: 30, label: "30 seconds" },
  { value: 60, label: "1 minute" },
  { value: 120, label: "2 minutes" },
  { value: 300, label: "5 minutes" },
  { value: 600, label: "10 minutes" },
  { value: 900, label: "15 minutes" },
] as const

// ============================================================================
// BATCH CALCULATION
// ============================================================================

/**
 * Compute number of batches for a timed release
 */
export function computeBatchCount(
  totalCoins: number,
  coinsPerRelease: number
): number {
  if (coinsPerRelease <= 0) return 0
  return Math.ceil(totalCoins / coinsPerRelease)
}

/**
 * Total duration in seconds for a full release
 */
export function computeTotalDurationSeconds(
  totalCoins: number,
  coinsPerRelease: number,
  releaseIntervalSeconds: number
): number {
  const batches = computeBatchCount(totalCoins, coinsPerRelease)
  return (batches - 1) * releaseIntervalSeconds
}

/**
 * Generate batch times from start_time
 */
export function generateBatchTimes(
  config: ZoneTimedReleaseConfig
): Date[] {
  const { total_coins, coins_per_release, release_interval_seconds, start_time } = config
  const batches = computeBatchCount(total_coins, coins_per_release)
  const start = new Date(start_time)
  const times: Date[] = []

  for (let i = 0; i < batches; i++) {
    const t = new Date(start.getTime() + i * release_interval_seconds * 1000)
    times.push(t)
  }

  return times
}

// ============================================================================
// TIME FORMATTING
// ============================================================================

/**
 * Format seconds as "Xm Ys" or "Xh Ym"
 */
export function formatTimeUntil(seconds: number): string {
  if (seconds <= 0) return "Now"
  const m = Math.floor(seconds / 60)
  const s = Math.floor(seconds % 60)
  const h = Math.floor(m / 60)
  const mm = m % 60

  if (h > 0) return `${h}h ${mm}m`
  if (m > 0) return `${m}m ${s}s`
  return `${s}s`
}

/**
 * Format a date for display (short)
 */
export function formatReleaseTime(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  })
}

/**
 * Format date + time for schedule display
 */
export function formatScheduleDateTime(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })
}

// ============================================================================
// VALIDATION
// ============================================================================

export function validateTimedReleaseConfig(config: {
  total_coins: number
  coins_per_release: number
  release_interval_seconds: number
  start_time: string
}): { valid: boolean; errors: string[] } {
  const errors: string[] = []
  if (config.total_coins < 1) errors.push("Total coins must be at least 1")
  if (config.coins_per_release < 1) errors.push("Coins per release must be at least 1")
  if (config.coins_per_release > config.total_coins)
    errors.push("Coins per release cannot exceed total coins")
  if (config.release_interval_seconds < 10)
    errors.push("Release interval must be at least 10 seconds")
  if (!config.start_time?.trim()) errors.push("Start time is required")
  const start = new Date(config.start_time)
  if (isNaN(start.getTime())) errors.push("Invalid start time")
  return { valid: errors.length === 0, errors }
}

// ============================================================================
// STATUS HELPERS
// ============================================================================

export function getScheduleStatusLabel(status: ReleaseScheduleStatus): string {
  switch (status) {
    case "scheduled":
      return "Scheduled"
    case "active":
      return "Active"
    case "paused":
      return "Paused"
    case "completed":
      return "Completed"
    case "cancelled":
      return "Cancelled"
    default:
      return status
  }
}
