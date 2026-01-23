/**
 * Zone Configuration for Black Bart's Gold Admin Dashboard
 * 
 * @file admin-dashboard/src/components/maps/zone-config.ts
 * @description Centralized zone settings, styles, colors, and utility functions
 * 
 * Character count: ~5,800
 */

import type { 
  ZoneType, 
  ZoneStatus, 
  HuntType, 
  ZoneGeometry,
  PolygonPoint 
} from "@/types/database"

// ============================================================================
// ZONE COLORS & STYLING
// ============================================================================

/**
 * Zone colors by type - Western/Gold theme
 * Using hex colors for Mapbox compatibility (opacity handled separately in layer)
 */
export const ZONE_TYPE_COLORS: Record<ZoneType, { fill: string; border: string; label: string; opacity: number }> = {
  player: { 
    fill: "#FFD700",                       // Treasure Gold
    border: "#FFD700",                     // Treasure Gold
    label: "Player Zone",
    opacity: 0.25,
  },
  sponsor: { 
    fill: "#B87333",                       // Brass
    border: "#B87333",                     // Brass
    label: "Sponsor Zone",
    opacity: 0.3,
  },
  hunt: { 
    fill: "#E25822",                       // Fire Orange
    border: "#E25822",                     // Fire Orange
    label: "Hunt Zone",
    opacity: 0.3,
  },
  grid: { 
    fill: "#8B4513",                       // Saddle Brown
    border: "#8B4513",                     // Saddle Brown
    label: "Grid Zone",
    opacity: 0.2,
  },
}

/**
 * Zone status colors and styling
 */
export const ZONE_STATUS_COLORS: Record<ZoneStatus, { color: string; bgColor: string; emoji: string }> = {
  active: { 
    color: "#22C55E",      // Green
    bgColor: "bg-green-500/10",
    emoji: "✅"
  },
  inactive: { 
    color: "#6B7280",      // Gray
    bgColor: "bg-gray-500/10",
    emoji: "⏸️"
  },
  scheduled: { 
    color: "#3B82F6",      // Blue
    bgColor: "bg-blue-500/10",
    emoji: "📅"
  },
  completed: { 
    color: "#8B5CF6",      // Purple
    bgColor: "bg-purple-500/10",
    emoji: "🏁"
  },
  archived: { 
    color: "#9CA3AF",      // Light gray
    bgColor: "bg-gray-400/10",
    emoji: "📦"
  },
}

/**
 * Hunt type configuration display
 */
export const HUNT_TYPE_CONFIG: Record<HuntType, { label: string; emoji: string; description: string }> = {
  direct_navigation: {
    label: "Direct Navigation",
    emoji: "🧭",
    description: "Full guidance with map, compass, and distance"
  },
  compass_only: {
    label: "Compass Only",
    emoji: "🔄",
    description: "Direction without exact distance"
  },
  pure_ar: {
    label: "Pure AR",
    emoji: "📱",
    description: "See it to find it - no map markers"
  },
  radar_only: {
    label: "Radar/Hot-Cold",
    emoji: "📡",
    description: "Vibration intensity guides you"
  },
  timed_release: {
    label: "Timed Release",
    emoji: "⏱️",
    description: "Coins appear over time"
  },
  multi_find_race: {
    label: "Multi-Find Race",
    emoji: "🏃",
    description: "Gold/Silver/Bronze for multiple finders"
  },
  sequential: {
    label: "Sequential",
    emoji: "1️⃣",
    description: "One finder per coin, multiple coins"
  },
}

// ============================================================================
// ZONE GEOMETRY UTILITIES
// ============================================================================

/**
 * Default zone radius in meters (1 mile)
 */
export const DEFAULT_ZONE_RADIUS = 1609 // 1 mile in meters

/**
 * Minimum and maximum zone radius
 */
export const ZONE_RADIUS_LIMITS = {
  min: 50,      // 50 meters
  max: 50000,   // 50 km
}

/**
 * Default zone settings
 */
export const DEFAULT_ZONE_SETTINGS = {
  opacity: 0.3,
  borderWidth: 2,
  minCoins: 3,
  maxCoins: 10,
}

/**
 * Convert degrees to radians
 */
const toRadians = (degrees: number): number => (degrees * Math.PI) / 180

/**
 * Convert radians to degrees
 */
const toDegrees = (radians: number): number => (radians * 180) / Math.PI

/**
 * Earth's radius in meters
 */
const EARTH_RADIUS = 6371000

/**
 * Generate a circle polygon from center and radius
 * Mapbox uses polygons, so we create a polygon approximating a circle
 * 
 * @param centerLat - Center latitude
 * @param centerLng - Center longitude
 * @param radiusMeters - Radius in meters
 * @param numPoints - Number of points to generate (default 64 for smooth circle)
 * @returns Array of [longitude, latitude] pairs for GeoJSON polygon
 */
export function generateCirclePolygon(
  centerLat: number,
  centerLng: number,
  radiusMeters: number,
  numPoints: number = 64
): [number, number][] {
  const coordinates: [number, number][] = []
  
  for (let i = 0; i <= numPoints; i++) {
    const angle = (i / numPoints) * 2 * Math.PI
    
    // Calculate the latitude offset
    const latOffset = (radiusMeters / EARTH_RADIUS) * Math.cos(angle)
    const lat = centerLat + toDegrees(latOffset)
    
    // Calculate the longitude offset (accounts for latitude)
    const lngOffset = (radiusMeters / EARTH_RADIUS) * Math.sin(angle) / Math.cos(toRadians(centerLat))
    const lng = centerLng + toDegrees(lngOffset)
    
    coordinates.push([lng, lat])
  }
  
  return coordinates
}

/**
 * Convert PolygonPoint[] to GeoJSON coordinates
 */
export function polygonPointsToGeoJSON(points: PolygonPoint[]): [number, number][] {
  const coords: [number, number][] = points.map(p => [p.longitude, p.latitude])
  // Close the polygon if not already closed
  if (coords.length > 0) {
    const first = coords[0]
    const last = coords[coords.length - 1]
    if (first[0] !== last[0] || first[1] !== last[1]) {
      coords.push([...first])
    }
  }
  return coords
}

/**
 * Convert ZoneGeometry to GeoJSON polygon coordinates
 */
export function zoneGeometryToGeoJSON(geometry: ZoneGeometry): [number, number][] {
  if (geometry.type === 'circle' && geometry.center && geometry.radius_meters) {
    return generateCirclePolygon(
      geometry.center.latitude,
      geometry.center.longitude,
      geometry.radius_meters
    )
  }
  
  if (geometry.type === 'polygon' && geometry.polygon) {
    return polygonPointsToGeoJSON(geometry.polygon)
  }
  
  return []
}

/**
 * Calculate the center point of a polygon
 */
export function getPolygonCenter(points: PolygonPoint[]): PolygonPoint {
  if (points.length === 0) {
    return { latitude: 0, longitude: 0 }
  }
  
  const sum = points.reduce(
    (acc, point) => ({
      latitude: acc.latitude + point.latitude,
      longitude: acc.longitude + point.longitude,
    }),
    { latitude: 0, longitude: 0 }
  )
  
  return {
    latitude: sum.latitude / points.length,
    longitude: sum.longitude / points.length,
  }
}

/**
 * Calculate approximate area of a zone in square meters
 */
export function calculateZoneArea(geometry: ZoneGeometry): number {
  if (geometry.type === 'circle' && geometry.radius_meters) {
    return Math.PI * geometry.radius_meters * geometry.radius_meters
  }
  
  if (geometry.type === 'polygon' && geometry.polygon && geometry.polygon.length >= 3) {
    // Shoelace formula for polygon area
    let area = 0
    const n = geometry.polygon.length
    
    for (let i = 0; i < n; i++) {
      const j = (i + 1) % n
      const lat1 = toRadians(geometry.polygon[i].latitude)
      const lat2 = toRadians(geometry.polygon[j].latitude)
      const lng1 = toRadians(geometry.polygon[i].longitude)
      const lng2 = toRadians(geometry.polygon[j].longitude)
      
      area += lng1 * Math.sin(lat2) - lng2 * Math.sin(lat1)
    }
    
    return Math.abs(area * EARTH_RADIUS * EARTH_RADIUS / 2)
  }
  
  return 0
}

/**
 * Format area for display
 */
export function formatArea(squareMeters: number): string {
  if (squareMeters >= 1000000) {
    return `${(squareMeters / 1000000).toFixed(2)} km²`
  }
  if (squareMeters >= 10000) {
    return `${(squareMeters / 10000).toFixed(2)} hectares`
  }
  return `${Math.round(squareMeters)} m²`
}

/**
 * Format radius for display
 */
export function formatRadius(meters: number): string {
  if (meters >= 1609) {
    return `${(meters / 1609).toFixed(2)} mi`
  }
  if (meters >= 1000) {
    return `${(meters / 1000).toFixed(1)} km`
  }
  return `${Math.round(meters)} m`
}

/**
 * Check if a point is inside a zone
 */
export function isPointInZone(
  lat: number, 
  lng: number, 
  geometry: ZoneGeometry
): boolean {
  if (geometry.type === 'circle' && geometry.center && geometry.radius_meters) {
    const distance = calculateDistanceBetweenPoints(
      lat, lng,
      geometry.center.latitude, geometry.center.longitude
    )
    return distance <= geometry.radius_meters
  }
  
  if (geometry.type === 'polygon' && geometry.polygon && geometry.polygon.length >= 3) {
    return isPointInPolygon(lat, lng, geometry.polygon)
  }
  
  return false
}

/**
 * Calculate distance between two points (Haversine formula)
 */
function calculateDistanceBetweenPoints(
  lat1: number, lng1: number,
  lat2: number, lng2: number
): number {
  const φ1 = toRadians(lat1)
  const φ2 = toRadians(lat2)
  const Δφ = toRadians(lat2 - lat1)
  const Δλ = toRadians(lng2 - lng1)

  const a = Math.sin(Δφ / 2) ** 2 + 
            Math.cos(φ1) * Math.cos(φ2) * Math.sin(Δλ / 2) ** 2
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))

  return EARTH_RADIUS * c
}

/**
 * Ray casting algorithm to check if point is in polygon
 */
function isPointInPolygon(lat: number, lng: number, polygon: PolygonPoint[]): boolean {
  let inside = false
  const n = polygon.length
  
  for (let i = 0, j = n - 1; i < n; j = i++) {
    const yi = polygon[i].latitude
    const xi = polygon[i].longitude
    const yj = polygon[j].latitude
    const xj = polygon[j].longitude
    
    if (((yi > lat) !== (yj > lat)) && 
        (lng < (xj - xi) * (lat - yi) / (yj - yi) + xi)) {
      inside = !inside
    }
  }
  
  return inside
}

/**
 * Get zone bounds (for fitting map view)
 */
export function getZoneBounds(geometry: ZoneGeometry): {
  minLat: number
  maxLat: number
  minLng: number
  maxLng: number
} | null {
  const coords = zoneGeometryToGeoJSON(geometry)
  
  if (coords.length === 0) return null
  
  const lngs = coords.map(c => c[0])
  const lats = coords.map(c => c[1])
  
  return {
    minLng: Math.min(...lngs),
    maxLng: Math.max(...lngs),
    minLat: Math.min(...lats),
    maxLat: Math.max(...lats),
  }
}
