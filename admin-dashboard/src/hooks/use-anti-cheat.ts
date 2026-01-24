/**
 * Anti-Cheat Hook
 * Phase M8: Anti-Cheat
 * 
 * Provides cheat detection data and actions for managing flagged players,
 * reviewing flags, and taking enforcement actions.
 */

"use client"

import { useState, useEffect, useCallback, useRef } from "react"
import { createClient } from "@/lib/supabase/client"
import type { 
  CheatFlag, 
  FlaggedPlayer, 
  AntiCheatStats,
  CheatReason,
  CheatSeverity,
  PlayerAction,
  CheatFlagStatus,
  PlayerMovementType
} from "@/types/database"

// ============================================================================
// MOCK DATA GENERATION
// ============================================================================

/**
 * Generate mock cheat flags
 */
function generateMockCheatFlags(): CheatFlag[] {
  const now = new Date()
  return [
    {
      id: 'flag-1',
      user_id: 'user-1',
      reason: 'impossible_speed',
      severity: 'high',
      status: 'pending',
      action_taken: 'none',
      evidence: {
        previous_location: {
          latitude: 37.7749,
          longitude: -122.4194,
          timestamp: new Date(now.getTime() - 10000).toISOString(),
        },
        current_location: {
          latitude: 37.7849,
          longitude: -122.4094,
          timestamp: now.toISOString(),
        },
        distance_meters: 1500,
        time_seconds: 5,
        calculated_speed_kmh: 1080, // Impossible!
        reported_speed_kmh: 0,
        device_id: 'device-abc123',
        device_model: 'OnePlus 9 Pro',
        is_mock_location: false,
      },
      detected_at: now.toISOString(),
      detected_by: 'system',
      created_at: now.toISOString(),
      updated_at: now.toISOString(),
    },
    {
      id: 'flag-2',
      user_id: 'user-2',
      reason: 'teleportation',
      severity: 'critical',
      status: 'investigating',
      action_taken: 'warned',
      evidence: {
        previous_location: {
          latitude: 37.7749,
          longitude: -122.4194,
          timestamp: new Date(now.getTime() - 5000).toISOString(),
        },
        current_location: {
          latitude: 37.8049,
          longitude: -122.2694,
          timestamp: now.toISOString(),
        },
        distance_meters: 15000,
        time_seconds: 3,
        calculated_speed_kmh: 18000, // Teleport!
        device_id: 'device-xyz789',
        device_model: 'iPhone 14 Pro',
        is_mock_location: true,
        is_rooted: false,
      },
      detected_at: new Date(now.getTime() - 3600000).toISOString(),
      detected_by: 'system',
      reviewed_by: 'admin-1',
      reviewed_at: new Date(now.getTime() - 1800000).toISOString(),
      notes: 'Player warned, monitoring closely',
      created_at: new Date(now.getTime() - 3600000).toISOString(),
      updated_at: new Date(now.getTime() - 1800000).toISOString(),
    },
    {
      id: 'flag-3',
      user_id: 'user-3',
      reason: 'mock_location',
      severity: 'medium',
      status: 'confirmed',
      action_taken: 'suspended',
      evidence: {
        current_location: {
          latitude: 37.7749,
          longitude: -122.4194,
          timestamp: now.toISOString(),
        },
        device_id: 'device-def456',
        device_model: 'Samsung Galaxy S21',
        is_mock_location: true,
        is_rooted: true,
        coin_collections_during_incident: 15,
      },
      detected_at: new Date(now.getTime() - 7200000).toISOString(),
      detected_by: 'system',
      reviewed_by: 'admin-1',
      reviewed_at: new Date(now.getTime() - 3600000).toISOString(),
      notes: 'Confirmed mock location, suspended for 7 days',
      created_at: new Date(now.getTime() - 7200000).toISOString(),
      updated_at: new Date(now.getTime() - 3600000).toISOString(),
    },
    {
      id: 'flag-4',
      user_id: 'user-4',
      reason: 'gps_spoofing',
      severity: 'critical',
      status: 'confirmed',
      action_taken: 'banned',
      evidence: {
        previous_location: {
          latitude: 37.7749,
          longitude: -122.4194,
          timestamp: new Date(now.getTime() - 20000).toISOString(),
        },
        current_location: {
          latitude: 40.7128,
          longitude: -74.0060,
          timestamp: now.toISOString(), // NYC!
        },
        distance_meters: 4100000, // 4100 km
        time_seconds: 20,
        calculated_speed_kmh: 738000, // Impossible!
        accuracy_meters: 5000, // Very poor accuracy
        device_id: 'device-ghi789',
        device_model: 'Pixel 6',
        is_mock_location: true,
        is_rooted: true,
        coin_collections_during_incident: 50,
      },
      detected_at: new Date(now.getTime() - 86400000).toISOString(),
      detected_by: 'system',
      reviewed_by: 'admin-1',
      reviewed_at: new Date(now.getTime() - 82800000).toISOString(),
      notes: 'Confirmed GPS spoofing, permanent ban issued',
      created_at: new Date(now.getTime() - 86400000).toISOString(),
      updated_at: new Date(now.getTime() - 82800000).toISOString(),
    },
    {
      id: 'flag-5',
      user_id: 'user-5',
      reason: 'suspicious_pattern',
      severity: 'low',
      status: 'false_positive',
      action_taken: 'cleared',
      evidence: {
        current_location: {
          latitude: 37.7749,
          longitude: -122.4194,
          timestamp: now.toISOString(),
        },
        device_id: 'device-jkl012',
        device_model: 'iPhone 13',
        is_mock_location: false,
        is_rooted: false,
      },
      detected_at: new Date(now.getTime() - 172800000).toISOString(),
      detected_by: 'system',
      reviewed_by: 'admin-1',
      reviewed_at: new Date(now.getTime() - 169200000).toISOString(),
      notes: 'False positive - player was on a train',
      created_at: new Date(now.getTime() - 172800000).toISOString(),
      updated_at: new Date(now.getTime() - 169200000).toISOString(),
    },
  ]
}

/**
 * Generate mock flagged players
 */
function generateMockFlaggedPlayers(): FlaggedPlayer[] {
  const flags = generateMockCheatFlags()
  
  return [
    {
      user_id: 'user-1',
      user_name: 'John Doe',
      user_email: 'john@example.com',
      total_flags: 1,
      active_flags: 1,
      highest_severity: 'high',
      current_action: 'none',
      last_flag_at: flags[0].detected_at,
      last_location_update: new Date().toISOString(),
      current_movement_type: 'suspicious',
      device_id: 'device-abc123',
      is_mock_location: false,
      is_rooted: null,
      flags: [flags[0]],
    },
    {
      user_id: 'user-2',
      user_name: 'Jane Smith',
      user_email: 'jane@example.com',
      total_flags: 1,
      active_flags: 1,
      highest_severity: 'critical',
      current_action: 'warned',
      last_flag_at: flags[1].detected_at,
      last_location_update: new Date().toISOString(),
      current_movement_type: 'suspicious',
      device_id: 'device-xyz789',
      is_mock_location: true,
      is_rooted: false,
      flags: [flags[1]],
    },
    {
      user_id: 'user-3',
      user_name: 'Bob Johnson',
      user_email: 'bob@example.com',
      total_flags: 1,
      active_flags: 0,
      highest_severity: 'medium',
      current_action: 'suspended',
      last_flag_at: flags[2].detected_at,
      last_location_update: new Date(Date.now() - 3600000).toISOString(),
      current_movement_type: 'walking',
      device_id: 'device-def456',
      is_mock_location: true,
      is_rooted: true,
      flags: [flags[2]],
    },
    {
      user_id: 'user-4',
      user_name: 'Alice Williams',
      user_email: 'alice@example.com',
      total_flags: 1,
      active_flags: 0,
      highest_severity: 'critical',
      current_action: 'banned',
      last_flag_at: flags[3].detected_at,
      last_location_update: new Date(Date.now() - 86400000).toISOString(),
      current_movement_type: 'suspicious',
      device_id: 'device-ghi789',
      is_mock_location: true,
      is_rooted: true,
      flags: [flags[3]],
    },
  ]
}

/**
 * Generate mock anti-cheat stats
 */
function generateMockStats(): AntiCheatStats {
  const flags = generateMockCheatFlags()
  
  return {
    total_flags: flags.length,
    pending_flags: flags.filter(f => f.status === 'pending').length,
    confirmed_cheaters: flags.filter(f => f.status === 'confirmed').length,
    false_positives: flags.filter(f => f.status === 'false_positive').length,
    flags_by_reason: {
      gps_spoofing: flags.filter(f => f.reason === 'gps_spoofing').length,
      impossible_speed: flags.filter(f => f.reason === 'impossible_speed').length,
      teleportation: flags.filter(f => f.reason === 'teleportation').length,
      mock_location: flags.filter(f => f.reason === 'mock_location').length,
      device_tampering: flags.filter(f => f.reason === 'device_tampering').length,
      emulator_detected: flags.filter(f => f.reason === 'emulator_detected').length,
      app_tampering: flags.filter(f => f.reason === 'app_tampering').length,
      suspicious_pattern: flags.filter(f => f.reason === 'suspicious_pattern').length,
      multiple_devices: flags.filter(f => f.reason === 'multiple_devices').length,
      location_inconsistency: flags.filter(f => f.reason === 'location_inconsistency').length,
    },
    flags_by_severity: {
      low: flags.filter(f => f.severity === 'low').length,
      medium: flags.filter(f => f.severity === 'medium').length,
      high: flags.filter(f => f.severity === 'high').length,
      critical: flags.filter(f => f.severity === 'critical').length,
    },
    players_warned: flags.filter(f => f.action_taken === 'warned').length,
    players_suspended: flags.filter(f => f.action_taken === 'suspended').length,
    players_banned: flags.filter(f => f.action_taken === 'banned').length,
    flags_today: 2,
    flags_this_week: 4,
    flags_this_month: 12,
    detection_rate: 95.5, // Percentage
  }
}

// ============================================================================
// HOOK
// ============================================================================

interface UseAntiCheatReturn {
  // Data
  flaggedPlayers: FlaggedPlayer[]
  stats: AntiCheatStats | null
  flags: CheatFlag[]
  loading: boolean
  error: string | null
  
  // Actions
  refresh: () => Promise<void>
  reviewFlag: (flagId: string, status: CheatFlagStatus, action: PlayerAction, notes?: string) => Promise<void>
  takeAction: (userId: string, action: PlayerAction, reason: string) => Promise<void>
  clearFlag: (flagId: string, notes: string) => Promise<void>
  getPlayerFlags: (userId: string) => CheatFlag[]
}

export function useAntiCheat(): UseAntiCheatReturn {
  const [flaggedPlayers, setFlaggedPlayers] = useState<FlaggedPlayer[]>([])
  const [stats, setStats] = useState<AntiCheatStats | null>(null)
  const [flags, setFlags] = useState<CheatFlag[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  
  const supabase = createClient()
  const channelRef = useRef<ReturnType<typeof supabase.channel> | null>(null)

  // Fetch all data
  const fetchData = useCallback(async () => {
    setLoading(true)
    setError(null)
    
    try {
      // Use real Supabase data (Realtime enabled)
      const useMockData = false
      
      if (useMockData) {
        // Mock data fallback
        await new Promise(resolve => setTimeout(resolve, 500))
        const mockFlags = generateMockCheatFlags()
        const mockPlayers = generateMockFlaggedPlayers()
        const mockStats = generateMockStats()
        setFlags(mockFlags)
        setFlaggedPlayers(mockPlayers)
        setStats(mockStats)
        return
      }
      
      // Fetch cheat flags
      const { data: flagsData, error: flagsError } = await supabase
        .from('cheat_flags')
        .select('*')
        .order('detected_at', { ascending: false })
      
      if (flagsError) throw flagsError
      
      const fetchedFlags = (flagsData || []) as CheatFlag[]
      setFlags(fetchedFlags)
      
      // Group flags by user and create flagged players
      const playersMap = new Map<string, FlaggedPlayer>()
      
      for (const flag of fetchedFlags) {
        if (!playersMap.has(flag.user_id)) {
          // Fetch user profile
          const { data: profile } = await supabase
            .from('profiles')
            .select('full_name, email')
            .eq('id', flag.user_id)
            .single()
          
          // Extract device/location info from flag evidence
          const evidence = flag.evidence || {}
          const currentLocationTimestamp = evidence.current_location?.timestamp || null
          
          // Infer movement type from flag reason
          let movementType: PlayerMovementType = 'suspicious'
          if (flag.reason === 'impossible_speed') {
            movementType = 'driving'
          } else if (flag.reason === 'teleportation' || flag.reason === 'gps_spoofing' || flag.reason === 'mock_location') {
            movementType = 'suspicious'
          }
          
          playersMap.set(flag.user_id, {
            user_id: flag.user_id,
            user_name: profile?.full_name || 'Unknown',
            user_email: profile?.email || '',
            active_flags: 0,
            total_flags: 0,
            highest_severity: flag.severity,
            current_action: flag.action_taken,
            last_flag_at: flag.detected_at,
            last_location_update: currentLocationTimestamp,
            current_movement_type: movementType,
            device_id: evidence.device_id || null,
            is_mock_location: evidence.is_mock_location || false,
            is_rooted: evidence.is_rooted || null,
            flags: [],
          })
        }
        
        const player = playersMap.get(flag.user_id)!
        player.flags.push(flag)
        player.total_flags++
        if (flag.status === 'pending' || flag.status === 'investigating') {
          player.active_flags++
        }
        // Update highest severity
        const severityOrder = ['low', 'medium', 'high', 'critical']
        if (severityOrder.indexOf(flag.severity) > severityOrder.indexOf(player.highest_severity)) {
          player.highest_severity = flag.severity
        }
        // Update current action
        if (flag.action_taken !== 'none' && flag.action_taken !== 'cleared') {
          player.current_action = flag.action_taken
        }
        // Update last flag time
        if (new Date(flag.detected_at) > new Date(player.last_flag_at || '')) {
          player.last_flag_at = flag.detected_at
        }
        // Update last location update if this flag has a more recent location
        const evidence = flag.evidence || {}
        const currentLocationTimestamp = evidence.current_location?.timestamp
        if (currentLocationTimestamp) {
          if (!player.last_location_update || 
              new Date(currentLocationTimestamp) > new Date(player.last_location_update)) {
            player.last_location_update = currentLocationTimestamp
          }
        }
        // Update device info if available
        if (evidence.device_id) {
          player.device_id = evidence.device_id
        }
        if (evidence.is_mock_location !== undefined) {
          player.is_mock_location = evidence.is_mock_location
        }
        if (evidence.is_rooted !== undefined) {
          player.is_rooted = evidence.is_rooted
        }
        // Update movement type based on flag reason
        if (flag.reason === 'impossible_speed') {
          player.current_movement_type = 'driving'
        } else if (flag.reason === 'teleportation' || flag.reason === 'gps_spoofing' || flag.reason === 'mock_location') {
          player.current_movement_type = 'suspicious'
        }
      }
      
      setFlaggedPlayers(Array.from(playersMap.values()))
      
      // Calculate stats
      const today = new Date()
      today.setHours(0, 0, 0, 0)
      
      const flagsToday = fetchedFlags.filter(f => new Date(f.detected_at) >= today)
      const confirmedCheaters = new Set(
        fetchedFlags.filter(f => f.status === 'confirmed').map(f => f.user_id)
      )
      const bannedPlayers = new Set(
        fetchedFlags.filter(f => f.action_taken === 'banned').map(f => f.user_id)
      )
      
      setStats({
        total_flags: fetchedFlags.length,
        pending_flags: fetchedFlags.filter(f => f.status === 'pending').length,
        confirmed_cheaters: confirmedCheaters.size,
        false_positives: fetchedFlags.filter(f => f.status === 'false_positive').length,
        flags_by_reason: fetchedFlags.reduce((acc, f) => {
          acc[f.reason] = (acc[f.reason] || 0) + 1
          return acc
        }, {} as Record<string, number>),
        flags_by_severity: fetchedFlags.reduce((acc, f) => {
          acc[f.severity] = (acc[f.severity] || 0) + 1
          return acc
        }, {} as Record<string, number>),
        players_warned: new Set(fetchedFlags.filter(f => f.action_taken === 'warned').map(f => f.user_id)).size,
        players_suspended: new Set(fetchedFlags.filter(f => f.action_taken === 'suspended').map(f => f.user_id)).size,
        players_banned: bannedPlayers.size,
        flags_today: flagsToday.length,
        flags_this_week: fetchedFlags.filter(f => {
          const weekAgo = new Date()
          weekAgo.setDate(weekAgo.getDate() - 7)
          return new Date(f.detected_at) >= weekAgo
        }).length,
        flags_this_month: fetchedFlags.filter(f => {
          const monthAgo = new Date()
          monthAgo.setMonth(monthAgo.getMonth() - 1)
          return new Date(f.detected_at) >= monthAgo
        }).length,
        detection_rate: fetchedFlags.length > 0 
          ? ((confirmedCheaters.size / fetchedFlags.length) * 100) 
          : 0,
      })
      
    } catch (err) {
      console.error('Error fetching anti-cheat data:', err)
      setError(err instanceof Error ? err.message : 'Failed to fetch anti-cheat data')
    } finally {
      setLoading(false)
    }
  }, [supabase])

  // Review a flag
  const reviewFlag = useCallback(async (
    flagId: string,
    status: CheatFlagStatus,
    action: PlayerAction,
    notes?: string
  ) => {
    setLoading(true)
    setError(null)
    
    try {
      // Get current user
      const { data: { user } } = await supabase.auth.getUser()
      
      const { error } = await supabase
        .from('cheat_flags')
        .update({
          status,
          action_taken: action,
          reviewed_by: user?.id || null,
          reviewed_at: new Date().toISOString(),
          notes: notes || null,
          updated_at: new Date().toISOString(),
        })
        .eq('id', flagId)
      
      if (error) throw error
      
      // Update local state immediately (Realtime will also update)
      setFlags(prev => prev.map(flag => 
        flag.id === flagId 
          ? {
              ...flag,
              status,
              action_taken: action,
              reviewed_at: new Date().toISOString(),
              reviewed_by: user?.id || undefined,
              notes: notes || flag.notes,
              updated_at: new Date().toISOString(),
            }
          : flag
      ))
      
      // Refresh data to get updated flagged players
      await fetchData()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to review flag')
    } finally {
      setLoading(false)
    }
  }, [supabase, fetchData])

  // Take action on a player
  const takeAction = useCallback(async (
    userId: string,
    action: PlayerAction,
    reason: string
  ) => {
    setLoading(true)
    setError(null)
    
    try {
      // Get current user
      const { data: { user } } = await supabase.auth.getUser()
      if (!user) throw new Error('Not authenticated')
      
      // Insert player action
      const { error: actionError } = await supabase
        .from('player_actions')
        .insert({
          user_id: userId,
          action,
          reason,
          performed_by: user.id,
          performed_at: new Date().toISOString(),
        })
      
      if (actionError) throw actionError
      
      // Update all flags for this user with the action
      const { error: flagsError } = await supabase
        .from('cheat_flags')
        .update({
          action_taken: action,
          updated_at: new Date().toISOString(),
        })
        .eq('user_id', userId)
        .eq('action_taken', 'none') // Only update flags with no action yet
      
      if (flagsError) throw flagsError
      
      // Refresh data (Realtime will also update)
      await fetchData()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to take action')
    } finally {
      setLoading(false)
    }
  }, [supabase, fetchData])

  // Clear a flag (mark as false positive)
  const clearFlag = async (flagId: string, notes: string) => {
    await reviewFlag(flagId, 'false_positive', 'cleared', notes)
  }

  // Get flags for a specific player
  const getPlayerFlags = (userId: string): CheatFlag[] => {
    return flags.filter(flag => flag.user_id === userId)
  }

  // Set up Realtime subscription for cheat_flags
  const setupRealtime = useCallback(() => {
    // Create channel for cheat_flags changes
    const channel = supabase
      .channel('anti-cheat-flags')
      .on(
        'postgres_changes' as any,
        {
          event: '*',
          schema: 'public',
          table: 'cheat_flags',
        } as any,
        (payload: { eventType: string; new?: CheatFlag; old?: CheatFlag }) => {
          console.log('Cheat flag change:', payload)

          if (payload.eventType === 'INSERT' || payload.eventType === 'UPDATE') {
            const newFlag = payload.new as CheatFlag
            setFlags(prev => {
              const index = prev.findIndex(f => f.id === newFlag.id)
              if (index >= 0) {
                // Update existing
                const updated = [...prev]
                updated[index] = newFlag
                return updated
              } else {
                // Add new
                return [...prev, newFlag].sort((a, b) => 
                  new Date(b.detected_at).getTime() - new Date(a.detected_at).getTime()
                )
              }
            })
            // Refresh to update flagged players and stats
            fetchData()
          } else if (payload.eventType === 'DELETE') {
            const oldFlag = payload.old as CheatFlag
            setFlags(prev => prev.filter(f => f.id !== oldFlag.id))
            // Refresh to update flagged players and stats
            fetchData()
          }
        }
      )
      .subscribe((status: string) => {
        console.log('Anti-cheat Realtime subscription status:', status)
      })
    
    channelRef.current = channel
    
    return () => {
      channel.unsubscribe()
    }
  }, [supabase, fetchData])

  // Initial fetch and Realtime setup
  useEffect(() => {
    fetchData()
    const cleanup = setupRealtime()
    
    return () => {
      if (cleanup) cleanup()
      if (channelRef.current) {
        channelRef.current.unsubscribe()
      }
    }
  }, [fetchData, setupRealtime])

  return {
    flaggedPlayers,
    stats,
    flags,
    loading,
    error,
    refresh: fetchData,
    reviewFlag,
    takeAction,
    clearFlag,
    getPlayerFlags,
  }
}
