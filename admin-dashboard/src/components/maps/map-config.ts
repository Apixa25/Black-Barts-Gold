/**
 * Map Configuration for Black Bart's Gold Admin Dashboard
 * 
 * @file admin-dashboard/src/components/maps/map-config.ts
 * @description Centralized map settings, styles, and utility functions
 * 
 * Character count: ~3,200
 */

import type { CoinStatus, CoinTier } from "@/types/database"

// Mapbox access token from environment
export const MAPBOX_TOKEN = process.env.NEXT_PUBLIC_MAPBOX_TOKEN || ""

// Check if Mapbox is configured
export const isMapboxConfigured = () => {
  return MAPBOX_TOKEN && MAPBOX_TOKEN.startsWith("pk.")
}

// Default map center (Crescent City, California - home base)
export const DEFAULT_CENTER = {
  longitude: -124.2017,
  latitude: 41.7561,
  zoom: 12,
}

// Map style - using Mapbox Streets with custom Western color overlay
// We'll use a sepia-toned style for the Western theme
export const MAP_STYLE = "mapbox://styles/mapbox/streets-v12"

// Alternative styles for different views
export const MAP_STYLES = {
  streets: "mapbox://styles/mapbox/streets-v12",
  satellite: "mapbox://styles/mapbox/satellite-streets-v12",
  dark: "mapbox://styles/mapbox/dark-v11",
  light: "mapbox://styles/mapbox/light-v11",
  outdoors: "mapbox://styles/mapbox/outdoors-v12",
} as const

export type MapStyleKey = keyof typeof MAP_STYLES

// Coin marker colors based on status
export const COIN_STATUS_COLORS: Record<CoinStatus, string> = {
  visible: "#FFD700",    // Gold - available to find
  hidden: "#8B4513",     // Saddle brown - hidden/not yet visible
  collected: "#22C55E",  // Green - found by hunter
  expired: "#6B7280",    // Gray - expired
  recycled: "#9CA3AF",   // Light gray - recycled back to pool
}

// Coin marker colors based on tier
export const COIN_TIER_COLORS: Record<CoinTier, string> = {
  gold: "#FFD700",
  silver: "#C0C0C0",
  bronze: "#CD7F32",
}

// Marker sizes based on coin value
export const getMarkerSize = (value: number): number => {
  if (value >= 100) return 40    // Mythical coins
  if (value >= 25) return 32     // High value
  if (value >= 5) return 24      // Medium value
  return 18                       // Low value
}

// Status display configuration
export const COIN_STATUS_CONFIG: Record<CoinStatus, { label: string; emoji: string }> = {
  visible: { label: "Visible", emoji: "👁️" },
  hidden: { label: "Hidden", emoji: "🙈" },
  collected: { label: "Collected", emoji: "✅" },
  expired: { label: "Expired", emoji: "⏰" },
  recycled: { label: "Recycled", emoji: "♻️" },
}

// Tier display configuration
export const COIN_TIER_CONFIG: Record<CoinTier, { label: string; emoji: string }> = {
  gold: { label: "Gold", emoji: "🥇" },
  silver: { label: "Silver", emoji: "🥈" },
  bronze: { label: "Bronze", emoji: "🥉" },
}

// Map bounds for different zoom levels
export const ZOOM_LEVELS = {
  world: 2,
  country: 4,
  state: 6,
  city: 10,
  neighborhood: 14,
  street: 16,
  building: 18,
} as const

// Convert meters to appropriate zoom level
export const metersToZoom = (meters: number): number => {
  // Rough conversion: 1 mile ≈ 1609 meters
  if (meters >= 100000) return 6    // 100km+
  if (meters >= 50000) return 8     // 50km
  if (meters >= 10000) return 10    // 10km
  if (meters >= 5000) return 12     // 5km
  if (meters >= 1609) return 13     // 1 mile
  if (meters >= 500) return 15      // 500m
  if (meters >= 100) return 17      // 100m
  return 18                          // < 100m
}

// Format coordinates for display
export const formatCoordinates = (lat: number, lng: number): string => {
  const latDir = lat >= 0 ? "N" : "S"
  const lngDir = lng >= 0 ? "E" : "W"
  return `${Math.abs(lat).toFixed(6)}°${latDir}, ${Math.abs(lng).toFixed(6)}°${lngDir}`
}

// Calculate distance between two points (Haversine formula)
export const calculateDistance = (
  lat1: number,
  lon1: number,
  lat2: number,
  lon2: number
): number => {
  const R = 6371e3 // Earth's radius in meters
  const φ1 = (lat1 * Math.PI) / 180
  const φ2 = (lat2 * Math.PI) / 180
  const Δφ = ((lat2 - lat1) * Math.PI) / 180
  const Δλ = ((lon2 - lon1) * Math.PI) / 180

  const a =
    Math.sin(Δφ / 2) * Math.sin(Δφ / 2) +
    Math.cos(φ1) * Math.cos(φ2) * Math.sin(Δλ / 2) * Math.sin(Δλ / 2)
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))

  return R * c // Distance in meters
}

// Format distance for display
export const formatDistance = (meters: number): string => {
  if (meters >= 1609) {
    const miles = meters / 1609
    return `${miles.toFixed(1)} mi`
  }
  if (meters >= 1000) {
    return `${(meters / 1000).toFixed(1)} km`
  }
  return `${Math.round(meters)} m`
}
