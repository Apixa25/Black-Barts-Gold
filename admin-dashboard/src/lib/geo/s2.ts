/**
 * S2 spatial helpers for Black Bart's Gold.
 *
 * Canonical backend geography:
 * - L17 = neighborhood-scale pressure/spawn cell
 * - L14 = parent macro summary cell
 *
 * @file admin-dashboard/src/lib/geo/s2.ts
 */

import { s1, s2 } from 's2js'

export const S2_LEVEL_SUMMARY = 14
export const S2_LEVEL_PRESSURE = 17

export interface CellCenter {
  latitude: number
  longitude: number
}

export interface SpatialCellContext {
  s2CellTokenL17: string
  s2CellTokenL14: string
}

function toLeafCellId(latitude: number, longitude: number) {
  return s2.cellid.fromLatLng(s2.LatLng.fromDegrees(latitude, longitude))
}

function toCellTokenAtLevel(latitude: number, longitude: number, level: number): string {
  const leafCellId = toLeafCellId(latitude, longitude)
  const cellAtLevel = s2.cellid.parent(leafCellId, level)
  return s2.cellid.toToken(cellAtLevel)
}

function toCellIdFromToken(token: string) {
  return s2.cellid.fromToken(token)
}

export function getSpatialCellContext(latitude: number, longitude: number): SpatialCellContext {
  return {
    s2CellTokenL17: toCellTokenAtLevel(latitude, longitude, S2_LEVEL_PRESSURE),
    s2CellTokenL14: toCellTokenAtLevel(latitude, longitude, S2_LEVEL_SUMMARY),
  }
}

export function getParentCellToken(token: string, parentLevel: number): string {
  const cellId = toCellIdFromToken(token)
  return s2.cellid.toToken(s2.cellid.parent(cellId, parentLevel))
}

export function getCellLevel(token: string): number {
  return s2.cellid.level(toCellIdFromToken(token))
}

export function getCellCenter(token: string): CellCenter {
  const cellId = toCellIdFromToken(token)
  const cell = s2.Cell.fromCellID(cellId)
  const centerPoint = cell.center()
  const centerLatLng = s2.LatLng.fromPoint(centerPoint)

  return {
    latitude: s1.angle.degrees(centerLatLng.lat),
    longitude: s1.angle.degrees(centerLatLng.lng),
  }
}

function randomBetween(min: number, max: number): number {
  return min + Math.random() * (max - min)
}

function randomLongitudeBetween(min: number, max: number): number {
  if (max >= min) {
    return randomBetween(min, max)
  }

  const wrappedSpan = (max + 360) - min
  const wrappedValue = min + Math.random() * wrappedSpan
  return ((wrappedValue + 540) % 360) - 180
}

export function getRandomPointInCell(token: string, maxAttempts: number = 24): CellCenter {
  const cellId = toCellIdFromToken(token)
  const cell = s2.Cell.fromCellID(cellId)
  const rect = cell.rectBound()
  const latMin = s1.angle.degrees(rect.lat.lo)
  const latMax = s1.angle.degrees(rect.lat.hi)
  const lngMin = s1.angle.degrees(rect.lng.lo)
  const lngMax = s1.angle.degrees(rect.lng.hi)

  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    const latitude = randomBetween(latMin, latMax)
    const longitude = randomLongitudeBetween(lngMin, lngMax)
    const point = s2.Point.fromLatLng(s2.LatLng.fromDegrees(latitude, longitude))

    if (cell.containsPoint(point)) {
      return { latitude, longitude }
    }
  }

  return getCellCenter(token)
}

export function getNeighborCellTokens(token: string, level: number = getCellLevel(token)): string[] {
  const cellId = toCellIdFromToken(token)
  return s2.cellid.allNeighbors(cellId, level).map((neighborId) => s2.cellid.toToken(neighborId))
}

export function isValidCellToken(token: string): boolean {
  try {
    return s2.cellid.valid(toCellIdFromToken(token))
  } catch {
    return false
  }
}
