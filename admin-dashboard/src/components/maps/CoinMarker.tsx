/**
 * Coin Marker Component for Map Display
 * 
 * @file admin-dashboard/src/components/maps/CoinMarker.tsx
 * @description Individual coin marker with popup for map visualization
 * 
 * Character count: ~4,800
 */

"use client"

import { useState, useCallback } from "react"
import { Marker, Popup } from "react-map-gl/mapbox"
import type { Coin } from "@/types/database"
import { 
  COIN_STATUS_COLORS, 
  COIN_TIER_COLORS,
  COIN_STATUS_CONFIG,
  COIN_TIER_CONFIG,
  getMarkerSize,
  formatCoordinates 
} from "./map-config"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Edit, Trash2, Eye, MapPin, Move } from "lucide-react"

interface CoinMarkerProps {
  coin: Coin
  onClick?: (coin: Coin) => void
  onEdit?: (coin: Coin) => void
  onDelete?: (coin: Coin) => void
  onDragEnd?: (coin: Coin, newLat: number, newLng: number) => void
  isSelected?: boolean
  draggable?: boolean
}

export function CoinMarker({ 
  coin, 
  onClick, 
  onEdit, 
  onDelete,
  onDragEnd,
  isSelected = false,
  draggable = false,
}: CoinMarkerProps) {
  const [showPopup, setShowPopup] = useState(false)
  const [isDragging, setIsDragging] = useState(false)

  const handleMarkerClick = useCallback((e: React.MouseEvent) => {
    e.stopPropagation()
    setShowPopup(!showPopup)
    onClick?.(coin)
  }, [coin, onClick, showPopup])

  const handleEdit = useCallback((e: React.MouseEvent) => {
    e.stopPropagation()
    onEdit?.(coin)
    setShowPopup(false)
  }, [coin, onEdit])

  const handleDelete = useCallback((e: React.MouseEvent) => {
    e.stopPropagation()
    onDelete?.(coin)
    setShowPopup(false)
  }, [coin, onDelete])

  // Handle drag events
  const handleDragStart = useCallback(() => {
    setIsDragging(true)
    setShowPopup(false)
  }, [])

  const handleDragEnd = useCallback((event: { lngLat: { lng: number; lat: number } }) => {
    setIsDragging(false)
    if (onDragEnd) {
      onDragEnd(coin, event.lngLat.lat, event.lngLat.lng)
    }
  }, [coin, onDragEnd])

  // Get colors based on status and tier
  const statusColor = COIN_STATUS_COLORS[coin.status]
  const tierColor = COIN_TIER_COLORS[coin.tier]
  const markerSize = getMarkerSize(coin.value)
  
  // Use tier color for visible coins, status color for others
  const markerColor = coin.status === "visible" ? tierColor : statusColor

  return (
    <>
      <Marker
        longitude={coin.longitude}
        latitude={coin.latitude}
        anchor="center"
        onClick={handleMarkerClick}
        draggable={draggable}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
      >
        <div
          className={`
            transition-all duration-200
            hover:scale-110 hover:z-10
            ${isSelected ? "scale-125 z-20" : ""}
            ${isDragging ? "scale-150 z-30 opacity-75" : ""}
            ${draggable ? "cursor-move" : "cursor-pointer"}
          `}
          title={`${coin.coin_type === "pool" ? "?" : `$${coin.value}`} - ${coin.status}${draggable ? " (drag to move)" : ""}`}
        >
          {/* Coin marker SVG */}
          <svg
            width={markerSize}
            height={markerSize}
            viewBox="0 0 24 24"
            style={{
              filter: isSelected ? "drop-shadow(0 0 8px rgba(255, 215, 0, 0.8))" : "drop-shadow(0 2px 4px rgba(0,0,0,0.3))",
            }}
          >
            {/* Outer ring */}
            <circle
              cx="12"
              cy="12"
              r="11"
              fill={markerColor}
              stroke="#3D2914"
              strokeWidth="1.5"
            />
            {/* Inner detail */}
            <circle
              cx="12"
              cy="12"
              r="7"
              fill="none"
              stroke="#3D2914"
              strokeWidth="0.5"
              opacity="0.5"
            />
            {/* Value or mystery indicator */}
            <text
              x="12"
              y="16"
              textAnchor="middle"
              fontSize={coin.coin_type === "pool" ? "12" : "8"}
              fontWeight="bold"
              fill="#3D2914"
            >
              {coin.coin_type === "pool" ? "?" : "$"}
            </text>
            {/* Mythical sparkle indicator */}
            {coin.is_mythical && (
              <circle
                cx="18"
                cy="6"
                r="3"
                fill="#E25822"
                stroke="#FFD700"
                strokeWidth="0.5"
              />
            )}
          </svg>
        </div>
      </Marker>

      {/* Popup with coin details */}
      {showPopup && (
        <Popup
          longitude={coin.longitude}
          latitude={coin.latitude}
          anchor="bottom"
          onClose={() => setShowPopup(false)}
          closeOnClick={false}
          className="coin-popup"
          maxWidth="280px"
        >
          <div className="p-2 space-y-3 min-w-[240px]">
            {/* Header */}
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-lg">
                  {coin.coin_type === "pool" ? "🎰" : "🪙"}
                </span>
                <span className="font-bold text-saddle-dark">
                  {coin.coin_type === "pool" ? "Mystery Coin" : `$${coin.value.toFixed(2)}`}
                </span>
                {coin.is_mythical && (
                  <span title="Mythical">✨</span>
                )}
              </div>
              <Badge 
                variant="outline"
                className="text-xs"
                style={{ 
                  borderColor: statusColor,
                  color: statusColor 
                }}
              >
                {COIN_STATUS_CONFIG[coin.status].emoji} {COIN_STATUS_CONFIG[coin.status].label}
              </Badge>
            </div>

            {/* Details */}
            <div className="space-y-1 text-sm text-leather">
              <div className="flex items-center gap-2">
                <span className="text-leather-light">Tier:</span>
                <span>
                  {COIN_TIER_CONFIG[coin.tier].emoji} {COIN_TIER_CONFIG[coin.tier].label}
                </span>
              </div>
              
              {coin.location_name && (
                <div className="flex items-center gap-2">
                  <MapPin className="h-3 w-3 text-leather-light" />
                  <span className="truncate">{coin.location_name}</span>
                </div>
              )}
              
              <div className="text-xs text-leather-light font-mono">
                {formatCoordinates(coin.latitude, coin.longitude)}
              </div>

              {coin.multi_find && (
                <div className="flex items-center gap-2 text-xs">
                  <Eye className="h-3 w-3" />
                  <span>Multi-find: {coin.finds_remaining} remaining</span>
                </div>
              )}

              {coin.description && (
                <p className="text-xs text-leather-light italic mt-2">
                  {coin.description}
                </p>
              )}
            </div>

            {/* Drag hint */}
            {draggable && (
              <div className="flex items-center gap-1 text-xs text-leather-light bg-parchment rounded px-2 py-1">
                <Move className="h-3 w-3" />
                <span>Drag marker to reposition</span>
              </div>
            )}

            {/* Actions */}
            <div className="flex gap-2 pt-2 border-t border-saddle-light/20">
              {onEdit && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={handleEdit}
                  className="flex-1 h-8 text-xs"
                >
                  <Edit className="h-3 w-3 mr-1" />
                  Edit
                </Button>
              )}
              {onDelete && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={handleDelete}
                  className="flex-1 h-8 text-xs text-fire hover:text-fire hover:bg-fire/10"
                >
                  <Trash2 className="h-3 w-3 mr-1" />
                  Delete
                </Button>
              )}
            </div>
          </div>
        </Popup>
      )}
    </>
  )
}
