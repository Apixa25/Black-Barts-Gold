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
  PlayerTrackingStats,
  PlayerActivityStatus 
} from "@/types/database"
import { 
  PLAYER_UPDATE_INTERVALS
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
 * Compute tracking stats from active players list
 */
function computeStatsFromPlayers(players: ActivePlayer[]): PlayerTrackingStats {
  const zoneCounts: Record<string, number> = {}
  players.forEach((p) => {
    const zoneId = p.current_zone_id ?? 'none'
    zoneCounts[zoneId] = (zoneCounts[zoneId] ?? 0) + 1
  })
  return {
    total_active_players: players.filter((p) => p.activity_status === 'active').length,
    total_idle_players: players.filter((p) => p.activity_status === 'idle').length,
    total_players_today: players.length,
    players_in_ar_mode: players.filter((p) => p.is_ar_active).length,
    players_by_zone: zoneCounts,
    suspicious_players: players.filter((p) => p.movement_type === 'suspicious').length,
    average_session_minutes: 0,
    total_distance_traveled_km: 0,
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
      user_email: `${names[i % names.length].toLowerCase()}@example.com`,
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
      // Use real API data by default; mock path remains for emergency fallback.
      const useMockData = false
      
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
      
      const params = new URLSearchParams()
      if (zoneId) params.set('zoneId', zoneId)
      if (includeOffline) params.set('includeOffline', 'true')
      const queryString = params.toString()
      const url = `/api/v1/player/location${queryString ? `?${queryString}` : ''}`

      const response = await fetch(url, {
        method: 'GET',
        cache: 'no-store',
      })

      if (!response.ok) {
        const payload = await response.json().catch(() => null)
        const message = payload?.error || `Failed to fetch players (${response.status})`
        throw new Error(message)
      }

      const payload = await response.json()
      let transformed: ActivePlayer[] = payload?.players || []
      
      if (statusFilter) {
        transformed = transformed.filter((p: ActivePlayer) =>
          statusFilter.includes(p.activity_status)
        )
      }
      
      setPlayers(transformed)
      setStats(computeStatsFromPlayers(transformed))
      setError(null)
      
    } catch (err) {
      console.error('Error fetching players:', err)
      
      // Provide more helpful error messages
      let errorMessage = 'Failed to fetch players'
      if (err instanceof Error) {
        errorMessage = err.message
        // Check for common RLS errors
        if (err.message.includes('permission denied') || err.message.includes('RLS')) {
          errorMessage = 'Permission denied. Please ensure your user has super_admin role in the profiles table.'
        } else if (err.message.includes('relation') && err.message.includes('does not exist')) {
          errorMessage = 'Table not found. Please run the M4 migration (003_player_locations.sql) in Supabase.'
        } else if (err.message.includes('JWT')) {
          errorMessage = 'Authentication error. Please log out and log back in.'
        }
      }
      
      setError(errorMessage)
    }
  }, [zoneId, statusFilter, includeOffline])
  
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
        'postgres_changes',
        {
          event: '*',
          schema: 'public',
          table: 'player_locations',
          ...(zoneId ? { filter: `current_zone_id=eq.${zoneId}` } : {}),
        },
        () => {
          // Always re-fetch enriched rows so popups have real names/avatars.
          fetchPlayers()
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
  }, [supabase, enabled, zoneId, fetchPlayers])
  
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
    
    // Setup real-time subscription (Realtime enabled in Supabase)
    setupRealtime()
    
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
