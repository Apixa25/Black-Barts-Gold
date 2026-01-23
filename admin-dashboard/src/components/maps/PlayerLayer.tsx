/**
 * Player Layer Component for Map
 * 
 * @file admin-dashboard/src/components/maps/PlayerLayer.tsx
 * @description Renders multiple players on the map with optional clustering
 * 
 * Character count: ~5,200
 */

"use client"

import { useMemo, useCallback } from "react"
import { useMap } from "react-map-gl/mapbox"
import type { ActivePlayer, PlayerCluster } from "@/types/database"
import { PlayerMarker, PlayerClusterMarker } from "./PlayerMarker"
import { PLAYER_CLUSTERING } from "./player-config"

interface PlayerLayerProps {
  players: ActivePlayer[]
  selectedPlayerId?: string
  onPlayerClick?: (player: ActivePlayer) => void
  onViewProfile?: (player: ActivePlayer) => void
  onTrackPlayer?: (player: ActivePlayer) => void
  enableClustering?: boolean
  showPopupsOnHover?: boolean
}

/**
 * Simple grid-based clustering algorithm
 * Groups players that are close together at current zoom level
 */
function clusterPlayers(
  players: ActivePlayer[],
  zoom: number,
  clusterRadius: number = PLAYER_CLUSTERING.radius,
  minPoints: number = PLAYER_CLUSTERING.minPoints
): { clusters: PlayerCluster[]; unclustered: ActivePlayer[] } {
  // Don't cluster at high zoom levels
  if (zoom > PLAYER_CLUSTERING.maxZoom || !PLAYER_CLUSTERING.enabled) {
    return { clusters: [], unclustered: players }
  }
  
  // Grid cell size based on zoom (smaller cells at higher zoom)
  const cellSize = clusterRadius / Math.pow(2, zoom - 10)
  
  // Group players into grid cells
  const grid: Map<string, ActivePlayer[]> = new Map()
  
  players.forEach(player => {
    const cellX = Math.floor(player.longitude / cellSize)
    const cellY = Math.floor(player.latitude / cellSize)
    const key = `${cellX},${cellY}`
    
    if (!grid.has(key)) {
      grid.set(key, [])
    }
    grid.get(key)!.push(player)
  })
  
  // Convert grid cells to clusters or individual markers
  const clusters: PlayerCluster[] = []
  const unclustered: ActivePlayer[] = []
  
  grid.forEach((cellPlayers, key) => {
    if (cellPlayers.length >= minPoints) {
      // Create cluster
      const lats = cellPlayers.map(p => p.latitude)
      const lngs = cellPlayers.map(p => p.longitude)
      
      clusters.push({
        id: `cluster-${key}`,
        center: {
          latitude: lats.reduce((a, b) => a + b, 0) / lats.length,
          longitude: lngs.reduce((a, b) => a + b, 0) / lngs.length,
        },
        player_count: cellPlayers.length,
        players: cellPlayers,
        bounds: {
          north: Math.max(...lats),
          south: Math.min(...lats),
          east: Math.max(...lngs),
          west: Math.min(...lngs),
        },
      })
    } else {
      // Keep as individual markers
      unclustered.push(...cellPlayers)
    }
  })
  
  return { clusters, unclustered }
}

export function PlayerLayer({
  players,
  selectedPlayerId,
  onPlayerClick,
  onViewProfile,
  onTrackPlayer,
  enableClustering = true,
  showPopupsOnHover = true,
}: PlayerLayerProps) {
  const { current: map } = useMap()
  
  // Get current zoom level
  const zoom = map?.getZoom() ?? 12
  
  // Cluster players based on current zoom
  const { clusters, unclustered } = useMemo(() => {
    if (!enableClustering) {
      return { clusters: [], unclustered: players }
    }
    return clusterPlayers(players, zoom)
  }, [players, zoom, enableClustering])
  
  // Handle cluster click - zoom to fit all players in cluster
  const handleClusterClick = useCallback((cluster: PlayerCluster) => {
    if (!map) return
    
    // Fit bounds to cluster
    map.fitBounds(
      [
        [cluster.bounds.west, cluster.bounds.south],
        [cluster.bounds.east, cluster.bounds.north],
      ],
      {
        padding: 50,
        duration: 500,
        maxZoom: PLAYER_CLUSTERING.maxZoom + 1,
      }
    )
  }, [map])
  
  return (
    <>
      {/* Render cluster markers */}
      {clusters.map(cluster => (
        <PlayerClusterMarker
          key={cluster.id}
          longitude={cluster.center.longitude}
          latitude={cluster.center.latitude}
          count={cluster.player_count}
          onClick={() => handleClusterClick(cluster)}
        />
      ))}
      
      {/* Render individual player markers */}
      {unclustered.map(player => (
        <PlayerMarker
          key={player.id}
          player={player}
          onClick={onPlayerClick}
          onViewProfile={onViewProfile}
          onTrackPlayer={onTrackPlayer}
          isSelected={player.id === selectedPlayerId}
          showPopupOnHover={showPopupsOnHover}
        />
      ))}
    </>
  )
}

/**
 * Player count by status summary component
 */
interface PlayerStatusSummaryProps {
  players: ActivePlayer[]
  className?: string
}

export function PlayerStatusSummary({ players, className = "" }: PlayerStatusSummaryProps) {
  const counts = useMemo(() => {
    const result = {
      active: 0,
      idle: 0,
      stale: 0,
      offline: 0,
      inAR: 0,
      suspicious: 0,
    }
    
    players.forEach(player => {
      result[player.activity_status]++
      if (player.is_ar_active) result.inAR++
      if (player.movement_type === 'suspicious') result.suspicious++
    })
    
    return result
  }, [players])
  
  return (
    <div className={`flex flex-wrap gap-3 text-xs ${className}`}>
      <div className="flex items-center gap-1">
        <span className="w-2 h-2 rounded-full bg-green-500" />
        <span>{counts.active} active</span>
      </div>
      <div className="flex items-center gap-1">
        <span className="w-2 h-2 rounded-full bg-yellow-500" />
        <span>{counts.idle} idle</span>
      </div>
      <div className="flex items-center gap-1">
        <span className="w-2 h-2 rounded-full bg-gray-400" />
        <span>{counts.stale} stale</span>
      </div>
      {counts.inAR > 0 && (
        <div className="flex items-center gap-1 text-gold-700">
          <span>👁️</span>
          <span>{counts.inAR} in AR</span>
        </div>
      )}
      {counts.suspicious > 0 && (
        <div className="flex items-center gap-1 text-red-600">
          <span>⚠️</span>
          <span>{counts.suspicious} flagged</span>
        </div>
      )}
    </div>
  )
}
