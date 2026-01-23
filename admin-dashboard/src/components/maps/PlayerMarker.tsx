/**
 * Player Marker Component for Map
 * 
 * @file admin-dashboard/src/components/maps/PlayerMarker.tsx
 * @description Displays a player's location on the map with status indicators
 * 
 * Character count: ~6,800
 */

"use client"

import { useState, useCallback, useMemo } from "react"
import { Marker, Popup } from "react-map-gl/mapbox"
import type { ActivePlayer } from "@/types/database"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { 
  User, 
  Navigation, 
  Eye, 
  MapPin, 
  Clock,
  Coins,
  Activity,
  AlertTriangle
} from "lucide-react"
import {
  PLAYER_STATUS_COLORS,
  MOVEMENT_TYPE_COLORS,
  PLAYER_MARKER_STYLE,
  formatLastSeen,
  getActivityStatus,
} from "./player-config"

interface PlayerMarkerProps {
  player: ActivePlayer
  onClick?: (player: ActivePlayer) => void
  onViewProfile?: (player: ActivePlayer) => void
  onTrackPlayer?: (player: ActivePlayer) => void
  isSelected?: boolean
  showPopupOnHover?: boolean
  size?: number
}

export function PlayerMarker({
  player,
  onClick,
  onViewProfile,
  onTrackPlayer,
  isSelected = false,
  showPopupOnHover = true,
  size = 32,
}: PlayerMarkerProps) {
  const [showPopup, setShowPopup] = useState(false)
  
  // Get current status (may have changed since data was fetched)
  const currentStatus = useMemo(() => {
    return getActivityStatus(player.last_updated)
  }, [player.last_updated])
  
  // Get colors based on status
  const statusColors = PLAYER_STATUS_COLORS[currentStatus]
  const movementColors = MOVEMENT_TYPE_COLORS[player.movement_type]
  
  // Handle marker click
  const handleClick = useCallback((e: React.MouseEvent) => {
    e.stopPropagation()
    if (onClick) {
      onClick(player)
    }
    setShowPopup(true)
  }, [onClick, player])
  
  // Calculate marker styles
  const markerSize = isSelected ? size * 1.25 : size
  const avatarSize = markerSize * PLAYER_MARKER_STYLE.avatarRatio
  
  return (
    <>
      <Marker
        longitude={player.longitude}
        latitude={player.latitude}
        anchor="center"
        onClick={handleClick}
      >
        <div
          className="relative cursor-pointer transition-transform hover:scale-110"
          style={{ width: markerSize, height: markerSize }}
          onMouseEnter={() => showPopupOnHover && setShowPopup(true)}
          onMouseLeave={() => !isSelected && setShowPopup(false)}
        >
          {/* Pulse animation for active players */}
          {currentStatus === 'active' && (
            <div
              className="absolute inset-0 rounded-full animate-ping opacity-50"
              style={{ 
                backgroundColor: statusColors.pulse,
                animationDuration: `${PLAYER_MARKER_STYLE.pulseDuration}ms`,
              }}
            />
          )}
          
          {/* Outer ring (status color) */}
          <div
            className="absolute inset-0 rounded-full shadow-lg"
            style={{
              backgroundColor: statusColors.fill,
              border: `${PLAYER_MARKER_STYLE.borderWidth}px solid ${statusColors.border}`,
              boxShadow: `0 ${PLAYER_MARKER_STYLE.shadowOffset}px ${PLAYER_MARKER_STYLE.shadowBlur}px rgba(0,0,0,0.3)`,
            }}
          />
          
          {/* Avatar */}
          <div 
            className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 rounded-full overflow-hidden bg-white"
            style={{ width: avatarSize, height: avatarSize }}
          >
            <Avatar className="w-full h-full">
              <AvatarImage src={player.avatar_url || undefined} />
              <AvatarFallback className="bg-saddle-100 text-saddle-700 text-xs">
                {player.user_name?.[0]?.toUpperCase() || <User className="w-3 h-3" />}
              </AvatarFallback>
            </Avatar>
          </div>
          
          {/* AR mode indicator */}
          {player.is_ar_active && (
            <div 
              className="absolute -top-1 -right-1 w-4 h-4 bg-gold rounded-full border-2 border-white flex items-center justify-center"
              title="In AR Mode"
            >
              <Eye className="w-2.5 h-2.5 text-leather" />
            </div>
          )}
          
          {/* Suspicious activity warning */}
          {player.movement_type === 'suspicious' && (
            <div 
              className="absolute -bottom-1 -right-1 w-4 h-4 bg-red-500 rounded-full border-2 border-white flex items-center justify-center animate-pulse"
              title="Suspicious Movement"
            >
              <AlertTriangle className="w-2.5 h-2.5 text-white" />
            </div>
          )}
          
          {/* Direction indicator (heading) */}
          {player.heading !== null && currentStatus === 'active' && (
            <div
              className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 pointer-events-none"
              style={{
                transform: `translate(-50%, -50%) rotate(${player.heading}deg)`,
              }}
            >
              <div
                className="absolute left-1/2 -translate-x-1/2"
                style={{
                  width: 0,
                  height: 0,
                  borderLeft: '4px solid transparent',
                  borderRight: '4px solid transparent',
                  borderBottom: `${PLAYER_MARKER_STYLE.headingIndicatorLength}px solid ${statusColors.border}`,
                  bottom: markerSize / 2 + 2,
                }}
              />
            </div>
          )}
        </div>
      </Marker>
      
      {/* Popup with player details */}
      {showPopup && (
        <Popup
          longitude={player.longitude}
          latitude={player.latitude}
          anchor="bottom"
          offset={[0, -markerSize / 2 - 8]}
          closeButton={isSelected}
          closeOnClick={false}
          onClose={() => setShowPopup(false)}
          className="player-popup"
        >
          <div className="p-3 min-w-[220px]">
            {/* Header */}
            <div className="flex items-center gap-3 mb-3">
              <Avatar className="h-10 w-10 border-2" style={{ borderColor: statusColors.fill }}>
                <AvatarImage src={player.avatar_url || undefined} />
                <AvatarFallback className="bg-saddle-100 text-saddle-700">
                  {player.user_name?.[0]?.toUpperCase() || '?'}
                </AvatarFallback>
              </Avatar>
              <div className="flex-1 min-w-0">
                <div className="font-semibold text-saddle-dark truncate">
                  {player.user_name || 'Anonymous'}
                </div>
                <div className="flex items-center gap-1.5 text-xs">
                  <span 
                    className="w-2 h-2 rounded-full" 
                    style={{ backgroundColor: statusColors.fill }}
                  />
                  <span className="text-leather-light">{statusColors.label}</span>
                  <span className="text-leather-light/50">•</span>
                  <Clock className="w-3 h-3 text-leather-light" />
                  <span className="text-leather-light">{formatLastSeen(player.last_updated)}</span>
                </div>
              </div>
            </div>
            
            {/* Status badges */}
            <div className="flex flex-wrap gap-1.5 mb-3">
              {player.is_ar_active && (
                <Badge variant="outline" className="text-xs bg-gold/10 border-gold text-gold-700">
                  <Eye className="w-3 h-3 mr-1" />
                  AR Mode
                </Badge>
              )}
              <Badge 
                variant="outline" 
                className="text-xs"
                style={{ 
                  backgroundColor: `${movementColors.color}10`,
                  borderColor: movementColors.color,
                  color: movementColors.color 
                }}
              >
                {movementColors.emoji} {movementColors.label}
              </Badge>
            </div>
            
            {/* Stats */}
            <div className="grid grid-cols-2 gap-2 text-xs mb-3">
              <div className="flex items-center gap-1.5 text-leather-light">
                <Coins className="w-3.5 h-3.5 text-gold" />
                <span>{player.coins_collected_session} coins</span>
              </div>
              <div className="flex items-center gap-1.5 text-leather-light">
                <Activity className="w-3.5 h-3.5 text-blue-500" />
                <span>{player.time_active_minutes}m active</span>
              </div>
              {player.current_zone_name && (
                <div className="flex items-center gap-1.5 text-leather-light col-span-2">
                  <MapPin className="w-3.5 h-3.5 text-fire" />
                  <span className="truncate">{player.current_zone_name}</span>
                </div>
              )}
            </div>
            
            {/* Accuracy indicator */}
            <div className="text-xs text-leather-light/70 mb-3">
              <Navigation className="w-3 h-3 inline mr-1" />
              GPS accuracy: ±{Math.round(player.accuracy_meters)}m
            </div>
            
            {/* Action buttons */}
            <div className="flex gap-2">
              {onViewProfile && (
                <Button
                  size="sm"
                  variant="outline"
                  className="flex-1 h-7 text-xs"
                  onClick={() => onViewProfile(player)}
                >
                  <User className="w-3 h-3 mr-1" />
                  Profile
                </Button>
              )}
              {onTrackPlayer && (
                <Button
                  size="sm"
                  className="flex-1 h-7 text-xs bg-gold hover:bg-gold-dark text-leather"
                  onClick={() => onTrackPlayer(player)}
                >
                  <Navigation className="w-3 h-3 mr-1" />
                  Track
                </Button>
              )}
            </div>
          </div>
        </Popup>
      )}
    </>
  )
}

/**
 * Cluster marker for grouped players
 */
interface PlayerClusterMarkerProps {
  longitude: number
  latitude: number
  count: number
  onClick?: () => void
}

export function PlayerClusterMarker({
  longitude,
  latitude,
  count,
  onClick,
}: PlayerClusterMarkerProps) {
  // Size based on count
  const size = Math.min(48 + Math.log2(count) * 8, 72)
  
  return (
    <Marker
      longitude={longitude}
      latitude={latitude}
      anchor="center"
      onClick={onClick}
    >
      <div
        className="relative cursor-pointer transition-transform hover:scale-110"
        style={{ width: size, height: size }}
      >
        {/* Outer ring */}
        <div
          className="absolute inset-0 rounded-full bg-blue-500/20 animate-pulse"
          style={{ animationDuration: '2s' }}
        />
        
        {/* Main circle */}
        <div className="absolute inset-1 rounded-full bg-blue-500 shadow-lg flex items-center justify-center">
          <span className="text-white font-bold text-sm">{count}</span>
        </div>
        
        {/* Player icons indicator */}
        <div className="absolute -bottom-1 left-1/2 -translate-x-1/2 flex">
          <User className="w-3 h-3 text-blue-700" />
          <User className="w-3 h-3 text-blue-700 -ml-1" />
          {count > 2 && <User className="w-3 h-3 text-blue-700 -ml-1" />}
        </div>
      </div>
    </Marker>
  )
}
