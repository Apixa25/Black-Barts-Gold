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
import { createClient } from "@/lib/supabase/client"
import type { 
  Zone,
  Coin,
  DistributionStats,
  DistributionConfig,
  ZoneDistributionStatus,
  SpawnQueueItem,
  SpawnResult,
  DistributionAction,
  DistributionSystemStatus,
} from "@/types/database"
import {
  DEFAULT_DISTRIBUTION_CONFIG,
  generateSpawnPlan,
  calculateCoinsNeeded,
  randomPointInCircle,
  randomPointInPolygon,
  formatTimeUntilSpawn,
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
 * Generate mock zone distribution statuses
 */
function generateMockZoneStatuses(): ZoneDistributionStatus[] {
  return [
    {
      zone_id: 'zone-1',
      zone_name: 'Downtown Player Zone',
      zone_type: 'player',
      auto_spawn_enabled: true,
      min_coins: 3,
      max_coins: 10,
      current_coin_count: 5,
      active_coins: 4,
      collected_today: 12,
      needs_spawn: false,
      coins_to_spawn: 0,
      next_spawn_time: new Date(Date.now() + 180000).toISOString(),
      average_collection_time_hours: 2.5,
      spawn_rate_per_hour: 3.2,
      collection_rate_per_hour: 4.8,
    },
    {
      zone_id: 'zone-2',
      zone_name: 'Golden Gate Hunt Zone',
      zone_type: 'hunt',
      auto_spawn_enabled: false,
      min_coins: 10,
      max_coins: 50,
      current_coin_count: 8,
      active_coins: 8,
      collected_today: 25,
      needs_spawn: true,
      coins_to_spawn: 2,
      average_collection_time_hours: 1.2,
      spawn_rate_per_hour: 8.5,
      collection_rate_per_hour: 12.3,
    },
    {
      zone_id: 'zone-3',
      zone_name: 'Tech Sponsor Zone',
      zone_type: 'sponsor',
      auto_spawn_enabled: true,
      min_coins: 5,
      max_coins: 25,
      current_coin_count: 2,
      active_coins: 2,
      collected_today: 8,
      needs_spawn: true,
      coins_to_spawn: 3,
      next_spawn_time: new Date(Date.now() + 60000).toISOString(),
      average_collection_time_hours: 3.0,
      spawn_rate_per_hour: 2.1,
      collection_rate_per_hour: 2.7,
    },
  ]
}

/**
 * Generate mock distribution stats
 */
function generateMockStats(): DistributionStats {
  return {
    system_status: 'running',
    last_spawn_time: new Date(Date.now() - 300000).toISOString(),
    next_scheduled_spawn: new Date(Date.now() + 60000).toISOString(),
    
    total_zones_with_auto_spawn: 2,
    zones_needing_spawn: 2,
    queue_length: 5,
    
    coins_spawned_today: 47,
    coins_collected_today: 45,
    coins_recycled_today: 3,
    
    total_value_spawned_today: 52.75,
    total_value_collected_today: 48.30,
    average_coin_value: 1.12,
    
    average_spawn_time_ms: 125,
    spawn_success_rate: 0.98,
    errors_today: 1,
  }
}

/**
 * Generate mock spawn queue
 */
function generateMockQueue(): SpawnQueueItem[] {
  return [
    {
      id: 'queue-1',
      zone_id: 'zone-3',
      zone_name: 'Tech Sponsor Zone',
      trigger_type: 'auto',
      scheduled_time: new Date(Date.now() + 60000).toISOString(),
      coin_config: {
        coin_type: 'fixed',
        tier: 'silver',
        min_value: 0.50,
        max_value: 2.00,
        is_mythical: false,
      },
      status: 'pending',
      created_at: new Date().toISOString(),
    },
    {
      id: 'queue-2',
      zone_id: 'zone-3',
      zone_name: 'Tech Sponsor Zone',
      trigger_type: 'auto',
      scheduled_time: new Date(Date.now() + 65000).toISOString(),
      coin_config: {
        coin_type: 'fixed',
        tier: 'bronze',
        min_value: 0.10,
        max_value: 0.50,
        is_mythical: false,
      },
      status: 'pending',
      created_at: new Date().toISOString(),
    },
    {
      id: 'queue-3',
      zone_id: 'zone-2',
      zone_name: 'Golden Gate Hunt Zone',
      trigger_type: 'manual',
      scheduled_time: new Date(Date.now() + 5000).toISOString(),
      coin_config: {
        coin_type: 'fixed',
        tier: 'gold',
        min_value: 2.00,
        max_value: 5.00,
        is_mythical: false,
      },
      status: 'processing',
      created_at: new Date().toISOString(),
    },
  ]
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
  
  const [stats, setStats] = useState<DistributionStats>(generateMockStats())
  const [zoneStatuses, setZoneStatuses] = useState<ZoneDistributionStatus[]>(generateMockZoneStatuses())
  const [spawnQueue, setSpawnQueue] = useState<SpawnQueueItem[]>(generateMockQueue())
  const [config, setConfig] = useState<DistributionConfig>(DEFAULT_DISTRIBUTION_CONFIG)
  const [isSpawning, setIsSpawning] = useState(false)
  const [error, setError] = useState<string | null>(null)
  
  const supabase = createClient()
  const intervalRef = useRef<NodeJS.Timeout | null>(null)
  
  /**
   * Fetch distribution data from database
   */
  const fetchData = useCallback(async () => {
    try {
      // For now, use mock data since tables don't exist yet
      const useMockData = true
      
      if (useMockData) {
        // Simulate data refresh with slight variations
        setStats(prev => ({
          ...prev,
          coins_spawned_today: prev.coins_spawned_today + (Math.random() > 0.7 ? 1 : 0),
          coins_collected_today: prev.coins_collected_today + (Math.random() > 0.8 ? 1 : 0),
          last_spawn_time: Math.random() > 0.9 ? new Date().toISOString() : prev.last_spawn_time,
          next_scheduled_spawn: new Date(Date.now() + 60000).toISOString(),
        }))
        
        // Update zone statuses
        setZoneStatuses(prev => prev.map(zone => ({
          ...zone,
          current_coin_count: Math.max(0, zone.current_coin_count + (Math.random() > 0.5 ? 0 : Math.random() > 0.5 ? 1 : -1)),
          collected_today: zone.collected_today + (Math.random() > 0.8 ? 1 : 0),
        })).map(zone => ({
          ...zone,
          needs_spawn: zone.current_coin_count < zone.min_coins,
          coins_to_spawn: Math.max(0, zone.min_coins - zone.current_coin_count),
        })))
        
        setError(null)
        return
      }
      
      // Real database queries would go here
      // const { data: zones } = await supabase.from('zones').select('*')
      // etc.
      
    } catch (err) {
      console.error('Error fetching distribution data:', err)
      setError(err instanceof Error ? err.message : 'Failed to fetch data')
    }
  }, [supabase, zoneId])
  
  /**
   * Dispatch an action to the distribution system
   */
  const dispatch = useCallback(async (action: DistributionAction) => {
    setError(null)
    
    switch (action.type) {
      case 'start':
        setStats(prev => ({ ...prev, system_status: 'running' as DistributionSystemStatus }))
        break
        
      case 'pause':
        setStats(prev => ({ ...prev, system_status: 'paused' as DistributionSystemStatus }))
        break
        
      case 'stop':
        setStats(prev => ({ ...prev, system_status: 'stopped' as DistributionSystemStatus }))
        break
        
      case 'spawn_now':
        await spawnCoinsForZone(action.zone_id, action.count)
        break
        
      case 'recycle_stale':
        // TODO: Implement stale coin recycling
        console.log('Recycling stale coins for zone:', action.zone_id)
        setStats(prev => ({
          ...prev,
          coins_recycled_today: prev.coins_recycled_today + 1,
        }))
        break
        
      case 'clear_queue':
        setSpawnQueue([])
        break
        
      case 'update_config':
        setConfig(prev => ({ ...prev, ...action.config }))
        break
    }
  }, [])
  
  /**
   * Spawn coins for a specific zone
   */
  const spawnCoinsForZone = useCallback(async (
    targetZoneId: string,
    count: number
  ): Promise<SpawnResult[]> => {
    setIsSpawning(true)
    const results: SpawnResult[] = []
    
    try {
      // Find the zone
      const zone = zoneStatuses.find(z => z.zone_id === targetZoneId)
      if (!zone) {
        throw new Error('Zone not found')
      }
      
      // For now, simulate spawning
      for (let i = 0; i < count; i++) {
        // Generate random location (using SF coordinates as placeholder)
        const location = {
          latitude: 37.7749 + (Math.random() - 0.5) * 0.02,
          longitude: -122.4194 + (Math.random() - 0.5) * 0.02,
        }
        
        results.push({
          success: true,
          coin_id: `coin-${Date.now()}-${i}`,
          spawn_location: location,
          trigger_type: 'manual',
          zone_id: targetZoneId,
          spawned_at: new Date().toISOString(),
        })
      }
      
      // Update stats
      setStats(prev => ({
        ...prev,
        coins_spawned_today: prev.coins_spawned_today + count,
        last_spawn_time: new Date().toISOString(),
      }))
      
      // Update zone status
      setZoneStatuses(prev => prev.map(z => 
        z.zone_id === targetZoneId
          ? { ...z, current_coin_count: z.current_coin_count + count, needs_spawn: false }
          : z
      ))
      
    } catch (err) {
      console.error('Error spawning coins:', err)
      results.push({
        success: false,
        error_message: err instanceof Error ? err.message : 'Spawn failed',
        spawn_location: { latitude: 0, longitude: 0 },
        trigger_type: 'manual',
        zone_id: targetZoneId,
        spawned_at: new Date().toISOString(),
      })
    } finally {
      setIsSpawning(false)
    }
    
    return results
  }, [zoneStatuses])
  
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
    // Update local state
    setZoneStatuses(prev => prev.map(z => {
      if (z.zone_id !== targetZoneId) return z
      
      return {
        ...z,
        auto_spawn_enabled: configUpdate?.enabled ?? z.auto_spawn_enabled,
        min_coins: configUpdate?.min_coins ?? z.min_coins,
        max_coins: configUpdate?.max_coins ?? z.max_coins,
      }
    }))
    
    // TODO: Save to database when ready
    console.log('Updating zone config:', targetZoneId, configUpdate)
  }, [])
  
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
