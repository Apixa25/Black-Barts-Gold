/**
 * Anti-Cheat Hook
 * Phase M8: Anti-Cheat
 * 
 * Provides cheat detection data and actions for managing flagged players,
 * reviewing flags, and taking enforcement actions.
 */

"use client"

import { useState, useEffect } from "react"
import type { 
  CheatFlag, 
  FlaggedPlayer, 
  AntiCheatStats,
  CheatReason,
  CheatSeverity,
  PlayerAction,
  CheatFlagStatus
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

  // Fetch all data
  const fetchData = async () => {
    setLoading(true)
    setError(null)
    
    try {
      // TODO: Replace with actual Supabase calls
      // const { data: flagsData, error: flagsError } = await supabase
      //   .from('cheat_flags')
      //   .select('*')
      //   .order('detected_at', { ascending: false })
      // if (flagsError) throw flagsError
      
      // Mock data for now
      await new Promise(resolve => setTimeout(resolve, 500)) // Simulate API call
      
      const mockFlags = generateMockCheatFlags()
      const mockPlayers = generateMockFlaggedPlayers()
      const mockStats = generateMockStats()
      
      setFlags(mockFlags)
      setFlaggedPlayers(mockPlayers)
      setStats(mockStats)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch anti-cheat data')
    } finally {
      setLoading(false)
    }
  }

  // Review a flag
  const reviewFlag = async (
    flagId: string,
    status: CheatFlagStatus,
    action: PlayerAction,
    notes?: string
  ) => {
    setLoading(true)
    setError(null)
    
    try {
      // TODO: Replace with actual Supabase call
      // const { error } = await supabase
      //   .from('cheat_flags')
      //   .update({
      //     status,
      //     action_taken: action,
      //     reviewed_by: currentUser.id,
      //     reviewed_at: new Date().toISOString(),
      //     notes,
      //   })
      //   .eq('id', flagId)
      // if (error) throw error
      
      // Mock implementation
      await new Promise(resolve => setTimeout(resolve, 300))
      
      setFlags(prev => prev.map(flag => 
        flag.id === flagId 
          ? {
              ...flag,
              status,
              action_taken: action,
              reviewed_at: new Date().toISOString(),
              notes: notes || flag.notes,
              updated_at: new Date().toISOString(),
            }
          : flag
      ))
      
      // Refresh data
      await fetchData()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to review flag')
    } finally {
      setLoading(false)
    }
  }

  // Take action on a player
  const takeAction = async (
    userId: string,
    action: PlayerAction,
    reason: string
  ) => {
    setLoading(true)
    setError(null)
    
    try {
      // TODO: Replace with actual Supabase call
      // const { error } = await supabase
      //   .from('user_actions')
      //   .insert({
      //     user_id: userId,
      //     action,
      //     reason,
      //     performed_by: currentUser.id,
      //   })
      // if (error) throw error
      
      // Mock implementation
      await new Promise(resolve => setTimeout(resolve, 300))
      
      // Update flagged players
      setFlaggedPlayers(prev => prev.map(player =>
        player.user_id === userId
          ? { ...player, current_action: action }
          : player
      ))
      
      // Refresh data
      await fetchData()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to take action')
    } finally {
      setLoading(false)
    }
  }

  // Clear a flag (mark as false positive)
  const clearFlag = async (flagId: string, notes: string) => {
    await reviewFlag(flagId, 'false_positive', 'cleared', notes)
  }

  // Get flags for a specific player
  const getPlayerFlags = (userId: string): CheatFlag[] => {
    return flags.filter(flag => flag.user_id === userId)
  }

  // Initial fetch
  useEffect(() => {
    fetchData()
  }, [])

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
