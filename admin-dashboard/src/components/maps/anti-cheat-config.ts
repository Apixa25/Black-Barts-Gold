/**
 * Anti-Cheat Configuration
 * Phase M8: Anti-Cheat
 * 
 * Provides detection rules, thresholds, and utilities for identifying
 * GPS spoofing, impossible speeds, teleportation, and other cheating behaviors.
 */

import type { CheatReason, CheatSeverity, PlayerMovementType, CheatFlag } from "@/types/database"
import { calculateDistance as calculateDistanceHaversine } from "./map-config"

// ============================================================================
// DETECTION THRESHOLDS
// ============================================================================

/**
 * Speed thresholds for impossible travel detection (km/h)
 */
export const SPEED_DETECTION_THRESHOLDS = {
  /** Maximum reasonable walking speed */
  maxWalking: 8,        // km/h
  
  /** Maximum reasonable running speed */
  maxRunning: 25,       // km/h
  
  /** Maximum reasonable driving speed in city */
  maxDriving: 130,      // km/h
  
  /** Impossible speed threshold - definitely cheating */
  impossible: 200,      // km/h (>200 km/h = impossible without aircraft)
  
  /** Teleportation threshold - instant location change */
  teleportation: 1000,  // km/h (if calculated speed > 1000 km/h = teleport)
} as const

/**
 * Distance thresholds for detection (meters)
 */
export const DISTANCE_DETECTION_THRESHOLDS = {
  /** Maximum reasonable distance in 5 seconds (walking) */
  max5Seconds: 15,      // meters (walking speed)
  
  /** Maximum reasonable distance in 10 seconds */
  max10Seconds: 50,     // meters (running speed)
  
  /** Maximum reasonable distance in 60 seconds */
  max60Seconds: 500,    // meters (driving speed)
  
  /** Teleportation threshold - instant large distance */
  teleportation: 10000, // meters (10km+ in < 5 seconds = teleport)
} as const

/**
 * Time thresholds for detection (seconds)
 */
export const TIME_DETECTION_THRESHOLDS = {
  /** Minimum time between location updates */
  minUpdateInterval: 1,  // seconds
  
  /** Maximum reasonable time between updates */
  maxUpdateInterval: 30, // seconds
  
  /** Time window for pattern analysis */
  patternWindow: 300,    // 5 minutes
} as const

/**
 * Accuracy thresholds (meters)
 */
export const ACCURACY_THRESHOLDS = {
  /** Minimum GPS accuracy (worse than this is suspicious) */
  minAccuracy: 100,      // meters
  
  /** Good GPS accuracy */
  goodAccuracy: 10,      // meters
} as const

// ============================================================================
// SEVERITY ASSIGNMENT
// ============================================================================

/**
 * Determine cheat severity based on reason and evidence
 */
export function determineSeverity(
  reason: CheatReason,
  evidence: CheatFlag['evidence']
): CheatSeverity {
  switch (reason) {
    case 'gps_spoofing':
    case 'app_tampering':
    case 'device_tampering':
      return 'critical'
    
    case 'teleportation':
      // Check distance
      if (evidence.distance_meters && evidence.distance_meters > 5000) {
        return 'critical'
      }
      return 'high'
    
    case 'impossible_speed':
      // Check speed
      if (evidence.calculated_speed_kmh && evidence.calculated_speed_kmh > 500) {
        return 'critical'
      }
      if (evidence.calculated_speed_kmh && evidence.calculated_speed_kmh > 200) {
        return 'high'
      }
      return 'medium'
    
    case 'mock_location':
      // Mock location alone is medium, but combined with other flags = high
      return 'medium'
    
    case 'suspicious_pattern':
      return 'medium'
    
    case 'location_inconsistency':
      return 'low'
    
    case 'multiple_devices':
      return 'high'
    
    case 'emulator_detected':
      return 'medium'
    
    default:
      return 'medium'
  }
}

/**
 * Get human-readable label for cheat reason
 */
export function getCheatReasonLabel(reason: CheatReason): string {
  const labels: Record<CheatReason, string> = {
    gps_spoofing: 'GPS Spoofing',
    impossible_speed: 'Impossible Speed',
    teleportation: 'Teleportation',
    mock_location: 'Mock Location',
    device_tampering: 'Device Tampering',
    emulator_detected: 'Emulator Detected',
    app_tampering: 'App Tampering',
    suspicious_pattern: 'Suspicious Pattern',
    multiple_devices: 'Multiple Devices',
    location_inconsistency: 'Location Inconsistency',
  }
  return labels[reason]
}

/**
 * Get description for cheat reason
 */
export function getCheatReasonDescription(reason: CheatReason): string {
  const descriptions: Record<CheatReason, string> = {
    gps_spoofing: 'GPS location appears to be spoofed or manipulated',
    impossible_speed: 'Player traveled at an impossible speed',
    teleportation: 'Player instantly moved between distant locations',
    mock_location: 'Device has mock location enabled',
    device_tampering: 'Device is rooted/jailbroken or modified',
    emulator_detected: 'App is running on an emulator',
    app_tampering: 'App has been modified or patched',
    suspicious_pattern: 'Unusual behavior pattern detected',
    multiple_devices: 'Account active on multiple devices simultaneously',
    location_inconsistency: 'Location data is inconsistent or invalid',
  }
  return descriptions[reason]
}

/**
 * Get severity color for UI
 */
export function getSeverityColor(severity: CheatSeverity): string {
  const colors: Record<CheatSeverity, string> = {
    low: 'text-yellow-600',
    medium: 'text-orange-600',
    high: 'text-red-600',
    critical: 'text-red-800',
  }
  return colors[severity]
}

/**
 * Get severity badge color
 */
export function getSeverityBadgeColor(severity: CheatSeverity): string {
  const colors: Record<CheatSeverity, string> = {
    low: 'bg-yellow-100 text-yellow-800 border-yellow-300',
    medium: 'bg-orange-100 text-orange-800 border-orange-300',
    high: 'bg-red-100 text-red-800 border-red-300',
    critical: 'bg-red-200 text-red-900 border-red-500',
  }
  return colors[severity]
}

// ============================================================================
// DETECTION FUNCTIONS
// ============================================================================

/**
 * Calculate distance between two coordinates (uses map-config function)
 * Returns distance in meters
 * Note: Re-exported from map-config to avoid circular dependency
 */
function calculateDistance(
  lat1: number,
  lon1: number,
  lat2: number,
  lon2: number
): number {
  return calculateDistanceHaversine(lat1, lon1, lat2, lon2)
}

/**
 * Calculate speed from distance and time
 * Returns speed in km/h
 */
export function calculateSpeed(
  distanceMeters: number,
  timeSeconds: number
): number {
  if (timeSeconds <= 0) return 0
  const speedMps = distanceMeters / timeSeconds
  return speedMps * 3.6 // Convert to km/h
}

/**
 * Detect if movement is impossible based on speed
 */
export function detectImpossibleSpeed(
  speedKmh: number,
  previousMovementType?: PlayerMovementType
): boolean {
  // If speed exceeds impossible threshold
  if (speedKmh > SPEED_DETECTION_THRESHOLDS.impossible) {
    return true
  }
  
  // If speed is way beyond expected for movement type
  if (previousMovementType === 'walking' && speedKmh > SPEED_DETECTION_THRESHOLDS.maxRunning) {
    return true
  }
  
  if (previousMovementType === 'running' && speedKmh > SPEED_DETECTION_THRESHOLDS.maxDriving) {
    return true
  }
  
  return false
}

/**
 * Detect teleportation (instant large distance change)
 */
export function detectTeleportation(
  distanceMeters: number,
  timeSeconds: number
): boolean {
  // If distance is huge and time is very short
  if (distanceMeters > DISTANCE_DETECTION_THRESHOLDS.teleportation && timeSeconds < 5) {
    return true
  }
  
  // Calculate speed
  const speedKmh = calculateSpeed(distanceMeters, timeSeconds)
  if (speedKmh > SPEED_DETECTION_THRESHOLDS.teleportation) {
    return true
  }
  
  return false
}

/**
 * Detect GPS spoofing based on multiple factors
 */
export function detectGPSSpoofing(evidence: {
  is_mock_location?: boolean
  accuracy_meters?: number
  speed_kmh?: number
  distance_meters?: number
  time_seconds?: number
  device_model?: string
}): boolean {
  // Mock location is a strong indicator
  if (evidence.is_mock_location) {
    return true
  }
  
  // Very poor accuracy combined with high speed
  if (
    evidence.accuracy_meters &&
    evidence.accuracy_meters > ACCURACY_THRESHOLDS.minAccuracy &&
    evidence.speed_kmh &&
    evidence.speed_kmh > SPEED_DETECTION_THRESHOLDS.maxDriving
  ) {
    return true
  }
  
  // Impossible movement pattern
  if (
    evidence.distance_meters &&
    evidence.time_seconds &&
    detectTeleportation(evidence.distance_meters, evidence.time_seconds)
  ) {
    return true
  }
  
  return false
}

/**
 * Detect suspicious pattern (multiple minor flags)
 */
export function detectSuspiciousPattern(flags: Array<{ reason: CheatReason; severity: CheatSeverity }>): boolean {
  // If player has multiple low/medium severity flags
  const recentFlags = flags.filter(f => f.severity === 'low' || f.severity === 'medium')
  return recentFlags.length >= 3
}

// ============================================================================
// VALIDATION
// ============================================================================

/**
 * Validate location data for consistency
 */
export function validateLocationData(data: {
  latitude: number
  longitude: number
  accuracy_meters?: number
  speed_mps?: number | null
  timestamp: string
  previousLocation?: {
    latitude: number
    longitude: number
    timestamp: string
  }
}): {
  valid: boolean
  issues: string[]
} {
  const issues: string[] = []
  
  // Validate coordinates
  if (data.latitude < -90 || data.latitude > 90) {
    issues.push('Invalid latitude')
  }
  if (data.longitude < -180 || data.longitude > 180) {
    issues.push('Invalid longitude')
  }
  
  // Validate accuracy
  if (data.accuracy_meters && data.accuracy_meters > ACCURACY_THRESHOLDS.minAccuracy) {
    issues.push('GPS accuracy is very poor')
  }
  
  // Validate speed if previous location exists
  if (data.previousLocation && data.speed_mps !== null && data.speed_mps !== undefined) {
    const distance = calculateDistance(
      data.previousLocation.latitude,
      data.previousLocation.longitude,
      data.latitude,
      data.longitude
    )
    const timeSeconds = (new Date(data.timestamp).getTime() - new Date(data.previousLocation.timestamp).getTime()) / 1000
    
    if (timeSeconds > 0) {
      const calculatedSpeed = calculateSpeed(distance, timeSeconds)
      const reportedSpeed = (data.speed_mps || 0) * 3.6
      
      // Speed mismatch (calculated vs reported)
      if (Math.abs(calculatedSpeed - reportedSpeed) > 50) {
        issues.push('Speed mismatch between calculated and reported')
      }
      
      // Impossible speed
      if (detectImpossibleSpeed(calculatedSpeed)) {
        issues.push('Impossible speed detected')
      }
      
      // Teleportation
      if (detectTeleportation(distance, timeSeconds)) {
        issues.push('Teleportation detected')
      }
    }
  }
  
  return {
    valid: issues.length === 0,
    issues,
  }
}
