/**
 * Named-zone overlay helpers.
 *
 * These helpers intentionally operate on the existing `zones` table geometry so
 * we can preserve backward compatibility while S2 cells become the canonical
 * backend geography.
 *
 * @file admin-dashboard/src/lib/geo/named-zone-membership.ts
 */

import type { Zone, ZoneGeometry, ZoneType, PolygonPoint } from '@/types/database'

export interface NamedZoneOverlay {
  id: string
  name: string
  zone_type: ZoneType
  geometry: ZoneGeometry
}

const EARTH_RADIUS_METERS = 6371000

const ZONE_PRIORITY: Record<ZoneType, number> = {
  sponsor: 1,
  hunt: 2,
  player: 3,
  grid: 4,
}

function toRadians(degrees: number): number {
  return (degrees * Math.PI) / 180
}

function calculateDistanceMeters(
  lat1: number,
  lng1: number,
  lat2: number,
  lng2: number
): number {
  const phi1 = toRadians(lat1)
  const phi2 = toRadians(lat2)
  const deltaPhi = toRadians(lat2 - lat1)
  const deltaLambda = toRadians(lng2 - lng1)

  const a =
    Math.sin(deltaPhi / 2) ** 2 +
    Math.cos(phi1) * Math.cos(phi2) * Math.sin(deltaLambda / 2) ** 2

  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
  return EARTH_RADIUS_METERS * c
}

function isPointInPolygon(lat: number, lng: number, polygon: PolygonPoint[]): boolean {
  let inside = false

  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const yi = polygon[i].latitude
    const xi = polygon[i].longitude
    const yj = polygon[j].latitude
    const xj = polygon[j].longitude

    if (((yi > lat) !== (yj > lat)) &&
      (lng < ((xj - xi) * (lat - yi)) / (yj - yi) + xi)) {
      inside = !inside
    }
  }

  return inside
}

export function isPointInNamedZone(
  latitude: number,
  longitude: number,
  geometry: ZoneGeometry
): boolean {
  if (geometry.type === 'circle' && geometry.center && geometry.radius_meters) {
    return calculateDistanceMeters(
      latitude,
      longitude,
      geometry.center.latitude,
      geometry.center.longitude
    ) <= geometry.radius_meters
  }

  if (geometry.type === 'polygon' && geometry.polygon && geometry.polygon.length >= 3) {
    return isPointInPolygon(latitude, longitude, geometry.polygon)
  }

  return false
}

export function getMatchingNamedZones(
  latitude: number,
  longitude: number,
  zones: NamedZoneOverlay[]
): NamedZoneOverlay[] {
  return zones.filter((zone) => isPointInNamedZone(latitude, longitude, zone.geometry))
}

export function getPrimaryNamedZone(
  latitude: number,
  longitude: number,
  zones: NamedZoneOverlay[]
): NamedZoneOverlay | null {
  const matches = getMatchingNamedZones(latitude, longitude, zones)

  if (matches.length === 0) {
    return null
  }

  return [...matches].sort((a, b) => ZONE_PRIORITY[a.zone_type] - ZONE_PRIORITY[b.zone_type])[0]
}

export function toNamedZoneOverlay(zone: Pick<Zone, 'id' | 'name' | 'zone_type' | 'geometry'>): NamedZoneOverlay {
  return {
    id: zone.id,
    name: zone.name,
    zone_type: zone.zone_type,
    geometry: zone.geometry,
  }
}
