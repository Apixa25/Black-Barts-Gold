/**
 * Live Players Map Component
 * 
 * @file admin-dashboard/src/components/dashboard/live-players-map.tsx
 * @description Real-time player tracking map for the dashboard
 * 
 * Character count: ~5,500
 */

"use client"

import { useState, useCallback } from "react"
import dynamic from "next/dynamic"
import { usePlayerTracking } from "@/hooks/use-player-tracking"
import type { ActivePlayer } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { 
  Users, 
  RefreshCw, 
  Wifi, 
  WifiOff, 
  Eye,
  MapPin,
  Activity,
  AlertTriangle,
  Loader2,
  Maximize2
} from "lucide-react"
import Link from "next/link"

// Dynamically import MapView to avoid SSR issues
const MapView = dynamic(
  () => import("@/components/maps/MapView").then(mod => mod.MapView),
  { 
    ssr: false,
    loading: () => (
      <div className="h-[300px] bg-parchment rounded-lg flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-gold" />
      </div>
    )
  }
)

interface LivePlayersMapProps {
  height?: number
  showFullScreenLink?: boolean
}

export function LivePlayersMap({ 
  height = 300,
  showFullScreenLink = true 
}: LivePlayersMapProps) {
  const [selectedPlayerId, setSelectedPlayerId] = useState<string | undefined>()
  
  const {
    players,
    stats,
    connectionStatus,
    isLoading,
    error,
    refresh,
  } = usePlayerTracking({
    enabled: true,
    includeOffline: false,
  })
  
  // Handle player click
  const handlePlayerClick = useCallback((player: ActivePlayer) => {
    setSelectedPlayerId(player.id)
  }, [])
  
  // Handle view profile
  const handleViewProfile = useCallback((player: ActivePlayer) => {
    // TODO: Navigate to user profile
    console.log("View profile:", player.user_id)
  }, [])
  
  // Handle track player
  const handleTrackPlayer = useCallback((player: ActivePlayer) => {
    // TODO: Center map on player and follow
    console.log("Track player:", player.user_id)
  }, [])
  
  // Connection status indicator
  const ConnectionIndicator = () => {
    switch (connectionStatus) {
      case 'connected':
        return (
          <Badge variant="outline" className="text-green-600 border-green-600 bg-green-50">
            <Wifi className="w-3 h-3 mr-1" />
            Live
          </Badge>
        )
      case 'connecting':
        return (
          <Badge variant="outline" className="text-yellow-600 border-yellow-600 bg-yellow-50">
            <Loader2 className="w-3 h-3 mr-1 animate-spin" />
            Connecting
          </Badge>
        )
      default:
        return (
          <Badge variant="outline" className="text-red-600 border-red-600 bg-red-50">
            <WifiOff className="w-3 h-3 mr-1" />
            Offline
          </Badge>
        )
    }
  }
  
  return (
    <Card className="border-saddle-light/30">
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="text-saddle-dark flex items-center gap-2">
              <Users className="h-5 w-5 text-gold" />
              Live Players
              <ConnectionIndicator />
            </CardTitle>
            <CardDescription>
              Real-time player locations on the map
            </CardDescription>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={refresh}
              disabled={isLoading}
              className="h-8 w-8 p-0"
            >
              <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
            </Button>
            {showFullScreenLink && (
              <Button asChild variant="outline" size="sm" className="h-8">
                <Link href="/players">
                  <Maximize2 className="h-4 w-4 mr-1" />
                  Full View
                </Link>
              </Button>
            )}
          </div>
        </div>
        
        {/* Stats row */}
        {stats && (
          <div className="flex flex-wrap gap-4 mt-3 pt-3 border-t border-saddle-light/20">
            <div className="flex items-center gap-1.5 text-sm">
              <div className="w-2 h-2 rounded-full bg-green-500" />
              <span className="text-leather">{stats.total_active_players} active</span>
            </div>
            <div className="flex items-center gap-1.5 text-sm">
              <div className="w-2 h-2 rounded-full bg-yellow-500" />
              <span className="text-leather">{stats.total_idle_players} idle</span>
            </div>
            <div className="flex items-center gap-1.5 text-sm">
              <Eye className="w-3.5 h-3.5 text-gold" />
              <span className="text-leather">{stats.players_in_ar_mode} in AR</span>
            </div>
            {stats.suspicious_players > 0 && (
              <div className="flex items-center gap-1.5 text-sm text-red-600">
                <AlertTriangle className="w-3.5 h-3.5" />
                <span>{stats.suspicious_players} flagged</span>
              </div>
            )}
          </div>
        )}
      </CardHeader>
      
      <CardContent className="p-0">
        {error ? (
          <div className="h-[300px] flex flex-col items-center justify-center gap-3 bg-red-50 rounded-b-lg">
            <AlertTriangle className="h-8 w-8 text-red-500" />
            <p className="text-sm text-red-600">{error}</p>
            <Button variant="outline" size="sm" onClick={refresh}>
              Try Again
            </Button>
          </div>
        ) : (
          <MapView
            coins={[]}
            zones={[]}
            players={players}
            height={height}
            showPlayers={true}
            enablePlayerClustering={true}
            showPlayerStatusSummary={true}
            onPlayerClick={handlePlayerClick}
            onViewPlayerProfile={handleViewProfile}
            onTrackPlayer={handleTrackPlayer}
            selectedPlayerId={selectedPlayerId}
            className="rounded-b-lg"
          />
        )}
      </CardContent>
    </Card>
  )
}

/**
 * Compact player stats widget for sidebar or header
 */
export function PlayerStatsWidget() {
  const { stats, connectionStatus } = usePlayerTracking({
    enabled: true,
    includeOffline: false,
  })
  
  if (!stats) return null
  
  return (
    <div className="flex items-center gap-3 text-sm">
      <div className="flex items-center gap-1">
        <div className={`w-2 h-2 rounded-full ${
          connectionStatus === 'connected' ? 'bg-green-500 animate-pulse' : 'bg-gray-400'
        }`} />
        <span className="font-medium">{stats.total_active_players}</span>
        <span className="text-leather-light">online</span>
      </div>
      {stats.players_in_ar_mode > 0 && (
        <div className="flex items-center gap-1 text-gold-700">
          <Eye className="w-3.5 h-3.5" />
          <span>{stats.players_in_ar_mode}</span>
        </div>
      )}
    </div>
  )
}
