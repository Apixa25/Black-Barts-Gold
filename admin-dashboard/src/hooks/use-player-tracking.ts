/**
 * Player Tracking Hook with Supabase Realtime
 * 
 * @file admin-dashboard/src/hooks/use-player-tracking.ts
 * @description Real-time player location tracking using Supabase subscriptions
 * 
 * Character count: ~7,500
 */

"use client"

import { useState, useEffect, useCallback, useRef } from "react"
import { createClient } from "@/lib/supabase/client"
import type { 
  ActivePlayer, 
  PlayerLocation, 
  PlayerTrackingStats,
  PlayerActivityStatus 
} from "@/types/database"
import { 
  getActivityStatus, 
  PLAYER_UPDATE_INTERVALS,
  PLAYER_ACTIVITY_THRESHOLDS 
} from "@/components/maps/player-config"

type ConnectionStatus = 'connecting' | 'connected' | 'disconnected' | 'error'

interface UsePlayerTrackingOptions {
  /** Enable real-time updates (default: true) */
  enabled?: boolean
  /** Only fetch players in this zone */
  zoneId?: string
  /** Filter by activity status */
  statusFilter?: PlayerActivityStatus[]
  /** Refresh interval in ms (default: 3000) */
  refreshInterval?: number
  /** Include offline players (default: false) */
  includeOffline?: boolean
}

interface UsePlayerTrackingResult {
  /** List of active players */
  players: ActivePlayer[]
  /** Tracking statistics */
  stats: PlayerTrackingStats | null
  /** Real-time connection status */
  connectionStatus: ConnectionStatus
  /** Loading state */
  isLoading: boolean
  /** Error message if any */
  error: string | null
  /** Manually refresh player list */
  refresh: () => Promise<void>
  /** Force reconnect to real-time */
  reconnect: () => void
}

/**
 * Transform raw player location data to ActivePlayer format
 */
function transformToActivePlayer(
  location: PlayerLocation & { 
    profiles?: { full_name: string | null; avatar_url: string | null } | null 
    zones?: { name: string } | null
  }
): ActivePlayer {
  return {
    id: location.id,
    user_id: location.user_id,
    user_name: location.profiles?.full_name || null,
    avatar_url: location.profiles?.avatar_url || null,
    latitude: location.latitude,
    longitude: location.longitude,
    accuracy_meters: location.accuracy_meters,
    heading: location.heading,
    activity_status: getActivityStatus(location.updated_at),
    is_ar_active: location.is_ar_active,
    movement_type: location.movement_type,
    current_zone_id: location.current_zone_id,
    current_zone_name: location.zones?.name || null,
    coins_collected_session: 0, // TODO: Join with session stats
    time_active_minutes: 0,     // TODO: Calculate from session start
    last_updated: location.updated_at,
  }
}

/**
 * Generate mock player data for development
 */
function generateMockPlayers(count: number = 5): ActivePlayer[] {
  const names = ['Alice', 'Bob', 'Charlie', 'Diana', 'Eve', 'Frank', 'Grace', 'Henry']
  const statuses: PlayerActivityStatus[] = ['active', 'active', 'active', 'idle', 'stale']
  
  // San Francisco area coordinates
  const baseLat = 37.7749
  const baseLng = -122.4194
  
  return Array.from({ length: count }, (_, i) => {
    const status = statuses[i % statuses.length]
    const lastUpdated = new Date()
    
    // Adjust last updated based on status for realistic mock data
    if (status === 'idle') {
      lastUpdated.setMinutes(lastUpdated.getMinutes() - 3)
    } else if (status === 'stale') {
      lastUpdated.setMinutes(lastUpdated.getMinutes() - 15)
    }
    
    return {
      id: `mock-player-${i}`,
      user_id: `user-${i}`,
      user_name: names[i % names.length],
      avatar_url: null,
      latitude: baseLat + (Math.random() - 0.5) * 0.05,
      longitude: baseLng + (Math.random() - 0.5) * 0.05,
      accuracy_meters: 5 + Math.random() * 20,
      heading: Math.random() * 360,
      activity_status: status,
      is_ar_active: status === 'active' && Math.random() > 0.5,
      movement_type: Math.random() > 0.9 ? 'suspicious' : 'walking',
      current_zone_id: null,
      current_zone_name: null,
      coins_collected_session: Math.floor(Math.random() * 10),
      time_active_minutes: Math.floor(Math.random() * 60),
      last_updated: lastUpdated.toISOString(),
    }
  })
}

/**
 * Hook for real-time player tracking
 */
export function usePlayerTracking(
  options: UsePlayerTrackingOptions = {}
): UsePlayerTrackingResult {
  const {
    enabled = true,
    zoneId,
    statusFilter,
    refreshInterval = PLAYER_UPDATE_INTERVALS.dashboardRefresh,
    includeOffline = false,
  } = options
  
  const [players, setPlayers] = useState<ActivePlayer[]>([])
  const [stats, setStats] = useState<PlayerTrackingStats | null>(null)
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('disconnected')
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  const supabase = createClient()
  const channelRef = useRef<ReturnType<typeof supabase.channel> | null>(null)
  const refreshIntervalRef = useRef<NodeJS.Timeout | null>(null)
  
  /**
   * Fetch players from database
   */
  const fetchPlayers = useCallback(async () => {
    try {
      // For now, use mock data since table doesn't exist yet
      // TODO: Replace with real Supabase query when table is created
      const useMockData = true
      
      if (useMockData) {
        const mockPlayers = generateMockPlayers(8)
        
        // Apply status filter
        const filtered = statusFilter
          ? mockPlayers.filter(p => statusFilter.includes(p.activity_status))
          : mockPlayers
        
        // Remove offline if not included
        const finalPlayers = includeOffline
          ? filtered
          : filtered.filter(p => p.activity_status !== 'offline')
        
        setPlayers(finalPlayers)
        
        // Generate mock stats
        setStats({
          total_active_players: finalPlayers.filter(p => p.activity_status === 'active').length,
          total_idle_players: finalPlayers.filter(p => p.activity_status === 'idle').length,
          total_players_today: finalPlayers.length + 5,
          players_in_ar_mode: finalPlayers.filter(p => p.is_ar_active).length,
          players_by_zone: {},
          suspicious_players: finalPlayers.filter(p => p.movement_type === 'suspicious').length,
          average_session_minutes: 25,
          total_distance_traveled_km: 42.5,
        })
        
        setError(null)
        return
      }
      
      // Real Supabase query (for when table exists)
      let query = supabase
        .from('player_locations')
        .select(`
          *,
          profiles:user_id (full_name, avatar_url),
          zones:current_zone_id (name)
        `)
        .order('updated_at', { ascending: false })
      
      // Filter by zone if specified
      if (zoneId) {
        query = query.eq('current_zone_id', zoneId)
      }
      
      // Filter out old entries unless including offline
      if (!includeOffline) {
        const cutoffTime = new Date()
        cutoffTime.setSeconds(cutoffTime.getSeconds() - PLAYER_ACTIVITY_THRESHOLDS.stale)
        query = query.gte('updated_at', cutoffTime.toISOString())
      }
      
      const { data, error: fetchError } = await query
      
      if (fetchError) throw fetchError
      
      // Transform and filter
      let transformed = (data || []).map(transformToActivePlayer)
      
      if (statusFilter) {
        transformed = transformed.filter((p: ActivePlayer) =>
          statusFilter.includes(p.activity_status)
        )
      }
      
      setPlayers(transformed)
      setError(null)
      
    } catch (err) {
      console.error('Error fetching players:', err)
      setError(err instanceof Error ? err.message : 'Failed to fetch players')
    }
  }, [supabase, zoneId, statusFilter, includeOffline])
  
  /**
   * Set up real-time subscription
   */
  const setupRealtime = useCallback(() => {
    if (!enabled) return
    
    setConnectionStatus('connecting')
    
    // Create channel for player_locations changes
    const channel = supabase
      .channel('player-tracking')
      .on(
        'postgres_changes' as any,
        {
          event: '*',
          schema: 'public',
          table: 'player_locations',
          ...(zoneId ? { filter: `current_zone_id=eq.${zoneId}` } : {}),
        } as any,
        (payload: { eventType: string; new?: PlayerLocation; old?: PlayerLocation }) => {
          console.log('Player location change:', payload)

          if (payload.eventType === 'INSERT' || payload.eventType === 'UPDATE') {
            const newLocation = payload.new as PlayerLocation
            const activePlayer = transformToActivePlayer(newLocation)

            setPlayers(prev => {
              const index = prev.findIndex((p: ActivePlayer) => p.user_id === activePlayer.user_id)
              if (index >= 0) {
                // Update existing
                const updated = [...prev]
                updated[index] = activePlayer
                return updated
              } else {
                // Add new
                return [...prev, activePlayer]
              }
            })
          } else if (payload.eventType === 'DELETE') {
            const oldLocation = payload.old as PlayerLocation
            setPlayers(prev => prev.filter((p: ActivePlayer) => p.user_id !== oldLocation.user_id))
          }
        }
      )
      .subscribe((status: string) => {
        console.log('Realtime subscription status:', status)
        if (status === 'SUBSCRIBED') {
          setConnectionStatus('connected')
        } else if (status === 'CLOSED' || status === 'CHANNEL_ERROR') {
          setConnectionStatus('disconnected')
        }
      })
    
    channelRef.current = channel
  }, [supabase, enabled, zoneId])
  
  /**
   * Cleanup subscription
   */
  const cleanup = useCallback(() => {
    if (channelRef.current) {
      supabase.removeChannel(channelRef.current)
      channelRef.current = null
    }
    if (refreshIntervalRef.current) {
      clearInterval(refreshIntervalRef.current)
      refreshIntervalRef.current = null
    }
  }, [supabase])
  
  /**
   * Manual refresh
   */
  const refresh = useCallback(async () => {
    setIsLoading(true)
    await fetchPlayers()
    setIsLoading(false)
  }, [fetchPlayers])
  
  /**
   * Force reconnect
   */
  const reconnect = useCallback(() => {
    cleanup()
    setupRealtime()
    refresh()
  }, [cleanup, setupRealtime, refresh])
  
  // Initial setup
  useEffect(() => {
    if (!enabled) return
    
    // Initial fetch
    setIsLoading(true)
    fetchPlayers().finally(() => setIsLoading(false))
    
    // Setup real-time (will use mock for now)
    // setupRealtime()
    setConnectionStatus('connected') // Mock connected status
    
    // Refresh interval for updating activity statuses
    refreshIntervalRef.current = setInterval(() => {
      fetchPlayers()
    }, refreshInterval)
    
    return cleanup
  }, [enabled, fetchPlayers, setupRealtime, cleanup, refreshInterval])
  
  return {
    players,
    stats,
    connectionStatus,
    isLoading,
    error,
    refresh,
    reconnect,
  }
}
