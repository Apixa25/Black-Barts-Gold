/**
 * Auto-Distribution Hook
 * 
 * @file admin-dashboard/src/hooks/use-auto-distribution.ts
 * @description Manages automatic coin distribution across zones
 * 
 * Character count: ~8,500
 */

"use client"

import { useState, useEffect, useCallback, useRef, useMemo } from "react"
import type { 
  Zone,
  DistributionStats,
  DistributionConfig,
  ZoneDistributionStatus,
  SpawnQueueItem,
  SpawnResult,
  DistributionAction,
} from "@/types/database"
import {
  DEFAULT_DISTRIBUTION_CONFIG,
  randomPointInCircle,
  randomPointInPolygon,
} from "@/components/maps/distribution-config"

interface UseAutoDistributionOptions {
  /** Enable auto-distribution (default: true) */
  enabled?: boolean
  /** Check interval override */
  checkIntervalMs?: number
  /** Zone ID to filter by (optional) */
  zoneId?: string
}

interface UseAutoDistributionResult {
  /** Global distribution statistics */
  stats: DistributionStats
  /** Per-zone distribution status */
  zoneStatuses: ZoneDistributionStatus[]
  /** Current spawn queue */
  spawnQueue: SpawnQueueItem[]
  /** System configuration */
  config: DistributionConfig
  /** Is system currently spawning */
  isSpawning: boolean
  /** Any error message */
  error: string | null
  /** Dispatch an action */
  dispatch: (action: DistributionAction) => Promise<void>
  /** Manually trigger spawn for a zone */
  spawnCoinsForZone: (zoneId: string, count: number) => Promise<SpawnResult[]>
  /** Preview spawn locations for a zone */
  previewSpawnLocations: (zone: Zone, count: number) => Array<{ latitude: number; longitude: number }>
  /** Update zone auto-spawn config */
  updateZoneConfig: (zoneId: string, config: Partial<Zone['auto_spawn_config']>) => Promise<void>
  /** Refresh all data */
  refresh: () => Promise<void>
}

/**
 * Hook for managing auto-distribution
 */
export function useAutoDistribution(
  options: UseAutoDistributionOptions = {}
): UseAutoDistributionResult {
  const {
    enabled = true,
    checkIntervalMs = DEFAULT_DISTRIBUTION_CONFIG.check_interval_seconds * 1000,
    zoneId,
  } = options
  
  const [stats, setStats] = useState<DistributionStats>({
    system_status: 'paused',
    last_spawn_time: null,
    next_scheduled_spawn: null,
    total_zones_with_auto_spawn: 0,
    zones_needing_spawn: 0,
    queue_length: 0,
    coins_spawned_today: 0,
    coins_collected_today: 0,
    coins_recycled_today: 0,
    total_value_spawned_today: 0,
    total_value_collected_today: 0,
    average_coin_value: 0,
    average_spawn_time_ms: 0,
    spawn_success_rate: 1,
    errors_today: 0,
  })
  const [zoneStatuses, setZoneStatuses] = useState<ZoneDistributionStatus[]>([])
  const [spawnQueue, setSpawnQueue] = useState<SpawnQueueItem[]>([])
  const [config, setConfig] = useState<DistributionConfig>(DEFAULT_DISTRIBUTION_CONFIG)
  const [isSpawning, setIsSpawning] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const intervalRef = useRef<NodeJS.Timeout | null>(null)
  
  /**
   * Fetch distribution data from database
   */
  const fetchData = useCallback(async () => {
    try {
      setError(null)
      const response = await fetch('/api/v1/admin/dashboard/auto-distribution', {
        cache: 'no-store',
      })
      const payload = await response.json()

      if (!response.ok || !payload?.success) {
        throw new Error(payload?.error ?? 'Failed to fetch auto-distribution data')
      }

      let nextZoneStatuses = (payload.data?.zoneStatuses ?? []) as ZoneDistributionStatus[]
      let nextSpawnQueue = (payload.data?.spawnQueue ?? []) as SpawnQueueItem[]
      if (zoneId) {
        nextZoneStatuses = nextZoneStatuses.filter(zone => zone.zone_id === zoneId)
        nextSpawnQueue = nextSpawnQueue.filter(item => item.zone_id === zoneId)
      }

      setStats((payload.data?.stats ?? {
        system_status: 'paused',
        last_spawn_time: null,
        next_scheduled_spawn: null,
        total_zones_with_auto_spawn: 0,
        zones_needing_spawn: 0,
        queue_length: 0,
        coins_spawned_today: 0,
        coins_collected_today: 0,
        coins_recycled_today: 0,
        total_value_spawned_today: 0,
        total_value_collected_today: 0,
        average_coin_value: 0,
        average_spawn_time_ms: 0,
        spawn_success_rate: 1,
        errors_today: 0,
      }) as DistributionStats)
      setZoneStatuses(nextZoneStatuses)
      setSpawnQueue(nextSpawnQueue)
      if (payload.data?.config) {
        setConfig(payload.data.config as DistributionConfig)
      }
    } catch (err) {
      console.error('Error fetching distribution data:', err)
      setError(err instanceof Error ? err.message : 'Failed to fetch data')
    }
  }, [zoneId])
  
  /**
   * Dispatch an action to the distribution system
   */
  const dispatch = useCallback(async (action: DistributionAction) => {
    setError(null)
    setIsSpawning(action.type === 'spawn_now')

    try {
      const response = await fetch('/api/v1/admin/dashboard/auto-distribution', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(action),
      })
      const payload = await response.json()
      if (!response.ok || !payload?.success) {
        throw new Error(payload?.error ?? `Failed to execute ${action.type}`)
      }
      await fetchData()
    } finally {
      setIsSpawning(false)
    }
  }, [fetchData])
  
  /**
   * Spawn coins for a specific zone
   */
  const spawnCoinsForZone = useCallback(async (
    targetZoneId: string,
    count: number
  ): Promise<SpawnResult[]> => {
    setIsSpawning(true)
    
    try {
      const zone = zoneStatuses.find(z => z.zone_id === targetZoneId)
      if (!zone) {
        throw new Error('Zone not found')
      }

      const response = await fetch('/api/v1/admin/dashboard/auto-distribution', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ type: 'spawn_now', zone_id: targetZoneId, count }),
      })
      const payload = await response.json()
      if (!response.ok || !payload?.success) {
        throw new Error(payload?.error ?? 'Spawn failed')
      }

      const results = ((payload.data?.results ?? []) as Array<{ data?: Record<string, unknown>; success?: boolean; error?: string }>).map((item) => {
        const data = item.data ?? {}
        return {
          success: Boolean(item.success),
          coin_id: (data.coin_id as string | undefined) ?? undefined,
          error_message: item.error,
          spawn_location: {
            latitude: Number(data.latitude ?? 0),
            longitude: Number(data.longitude ?? 0),
          },
          trigger_type: 'manual',
          zone_id: (data.zone_id as string | null) ?? targetZoneId,
          cell_id: (data.cell_id as string | null) ?? null,
          spawned_at: new Date().toISOString(),
        } as SpawnResult
      })

      await fetchData()
      return results
    } catch (err) {
      console.error('Error spawning coins:', err)
      return [{
        success: false,
        error_message: err instanceof Error ? err.message : 'Spawn failed',
        spawn_location: { latitude: 0, longitude: 0 },
        trigger_type: 'manual',
        zone_id: targetZoneId,
        spawned_at: new Date().toISOString(),
      }]
    } finally {
      setIsSpawning(false)
    }
  }, [fetchData, zoneStatuses])
  
  /**
   * Preview spawn locations for a zone
   */
  const previewSpawnLocations = useCallback((
    zone: Zone,
    count: number
  ): Array<{ latitude: number; longitude: number }> => {
    const locations: Array<{ latitude: number; longitude: number }> = []
    
    for (let i = 0; i < count; i++) {
      if (zone.geometry.type === 'circle' && zone.geometry.center && zone.geometry.radius_meters) {
        locations.push(randomPointInCircle(
          zone.geometry.center.latitude,
          zone.geometry.center.longitude,
          zone.geometry.radius_meters
        ))
      } else if (zone.geometry.type === 'polygon' && zone.geometry.polygon) {
        locations.push(randomPointInPolygon(zone.geometry.polygon))
      }
    }
    
    return locations
  }, [])
  
  /**
   * Update zone auto-spawn configuration
   */
  const updateZoneConfig = useCallback(async (
    targetZoneId: string,
    configUpdate: Partial<Zone['auto_spawn_config']>
  ) => {
    const response = await fetch(`/api/v1/admin/dashboard/zones/${targetZoneId}/auto-spawn`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(configUpdate),
    })
    const payload = await response.json()
    if (!response.ok || !payload?.success) {
      throw new Error(payload?.error ?? 'Failed to update zone config')
    }

    await fetchData()
  }, [fetchData])
  
  /**
   * Refresh all data
   */
  const refresh = useCallback(async () => {
    await fetchData()
  }, [fetchData])
  
  // Initial fetch and interval setup
  useEffect(() => {
    if (!enabled) return
    
    fetchData()
    
    // Set up periodic refresh
    intervalRef.current = setInterval(fetchData, checkIntervalMs)
    
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
      }
    }
  }, [enabled, fetchData, checkIntervalMs])
  
  // Filter zone statuses if zoneId provided
  const filteredZoneStatuses = useMemo(() => {
    if (!zoneId) return zoneStatuses
    return zoneStatuses.filter(z => z.zone_id === zoneId)
  }, [zoneStatuses, zoneId])
  
  return {
    stats,
    zoneStatuses: filteredZoneStatuses,
    spawnQueue,
    config,
    isSpawning,
    error,
    dispatch,
    spawnCoinsForZone,
    previewSpawnLocations,
    updateZoneConfig,
    refresh,
  }
}
