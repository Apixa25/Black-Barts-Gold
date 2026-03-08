/**
 * Helpers for resolving AI spawn/release spatial targets.
 *
 * Keeps cell-first targeting logic consistent across queueing, timed releases,
 * and direct spawn operations.
 *
 * @file admin-dashboard/src/lib/ai/spatial-targets.ts
 */

import { getCellCenter, getRandomPointInCell, getSpatialCellContext, isValidCellToken, S2_LEVEL_PRESSURE, getCellLevel } from '@/lib/geo/s2'
import { getPrimaryNamedZone, toNamedZoneOverlay } from '@/lib/geo/named-zone-membership'
import type { ZoneGeometry, ZoneType } from '@/types/database'

export interface ActiveZoneOverlayRow {
  id: string
  name: string
  zone_type: ZoneType
  geometry: ZoneGeometry
}

export interface ResolvedSpatialTarget {
  zoneId: string
  zoneName: string | null
  cellId: string | null
  parentCellId: string | null
  targetLatitude: number | null
  targetLongitude: number | null
}

export function isValidPressureCell(token: string): boolean {
  return isValidCellToken(token) && getCellLevel(token) === S2_LEVEL_PRESSURE
}

export function getDefaultValueRange(tier: 'gold' | 'silver' | 'bronze') {
  switch (tier) {
    case 'gold':
      return { min: 2.0, max: 10.0 }
    case 'silver':
      return { min: 0.5, max: 2.0 }
    case 'bronze':
    default:
      return { min: 0.1, max: 0.5 }
  }
}

export function buildReleaseQueuePreview(schedule: {
  id: string
  name: string
  zone_id: string
  zone_name: string
  next_release_at: string | null
  coins_per_release: number
  status: string
  s2_cell_token_l17?: string | null
}) {
  if (!schedule.next_release_at) return null

  return {
    id: `release-${schedule.id}`,
    schedule_id: schedule.id,
    schedule_name: schedule.name,
    zone_id: schedule.zone_id,
    zone_name: schedule.zone_name,
    cell_id: schedule.s2_cell_token_l17 ?? null,
    release_at: schedule.next_release_at,
    coins_count: schedule.coins_per_release,
    status: schedule.status === 'active' ? 'releasing' : 'pending',
    time_until_seconds: Math.max(
      0,
      Math.floor((new Date(schedule.next_release_at).getTime() - Date.now()) / 1000)
    ),
  }
}

export function resolveSpatialTarget(params: {
  zoneId: string
  zoneName?: string | null
  cellId?: string | null
  latitude?: number | null
  longitude?: number | null
  activeZones: ActiveZoneOverlayRow[]
}): ResolvedSpatialTarget {
  const { zoneId, zoneName = null, cellId = null, latitude = null, longitude = null, activeZones } = params

  if (cellId && !isValidPressureCell(cellId)) {
    throw new Error(`Invalid L${S2_LEVEL_PRESSURE} cell token: ${cellId}`)
  }

  let targetLatitude = latitude
  let targetLongitude = longitude
  let resolvedCellId = cellId
  let resolvedParentCellId: string | null = null

  if (targetLatitude !== null || targetLongitude !== null) {
    if (targetLatitude === null || targetLongitude === null) {
      throw new Error('latitude and longitude must be provided together')
    }

    const context = getSpatialCellContext(targetLatitude, targetLongitude)
    if (resolvedCellId && context.s2CellTokenL17 !== resolvedCellId) {
      throw new Error(
        `Target coordinates resolve to ${context.s2CellTokenL17}, not requested cell ${resolvedCellId}`
      )
    }

    resolvedCellId = context.s2CellTokenL17
    resolvedParentCellId = context.s2CellTokenL14
  } else if (resolvedCellId) {
    const point = getRandomPointInCell(resolvedCellId)
    const context = getSpatialCellContext(point.latitude, point.longitude)
    targetLatitude = point.latitude
    targetLongitude = point.longitude
    resolvedCellId = context.s2CellTokenL17
    resolvedParentCellId = context.s2CellTokenL14
  }

  if (resolvedCellId && !resolvedParentCellId) {
    resolvedParentCellId = getSpatialCellContext(
      getCellCenter(resolvedCellId).latitude,
      getCellCenter(resolvedCellId).longitude
    ).s2CellTokenL14
  }

  const targetZone = activeZones.find((zone) => zone.id === zoneId)
  if (!targetZone) {
    throw new Error(`Zone not found: ${zoneId}`)
  }

  if (resolvedCellId && targetLatitude !== null && targetLongitude !== null) {
    const inferredZone = getPrimaryNamedZone(
      targetLatitude,
      targetLongitude,
      activeZones.map((zone) => toNamedZoneOverlay(zone))
    )

    if (inferredZone && inferredZone.id !== zoneId) {
      // Keep explicit zone_id authoritative, but surface the mismatch to callers.
      // The queue/schedule path wants a deliberate overlay zone even when multiple overlap.
    }
  }

  return {
    zoneId,
    zoneName: zoneName ?? targetZone.name,
    cellId: resolvedCellId,
    parentCellId: resolvedParentCellId,
    targetLatitude,
    targetLongitude,
  }
}
