/**
 * Main Map View Component for Coin & Zone Visualization
 * 
 * @file admin-dashboard/src/components/maps/MapView.tsx
 * @description Interactive map showing coins, zones, and controls
 * 
 * Character count: ~9,500
 */

"use client"

import { useState, useCallback, useRef, useMemo } from "react"
import { 
  Map, 
  NavigationControl, 
  ScaleControl,
  type MapRef,
  type ViewStateChangeEvent
} from "react-map-gl/mapbox"
import type { Coin, CoinStatus, Zone, ZoneType, ActivePlayer } from "@/types/database"
import { CoinMarker } from "./CoinMarker"
import { ZoneLayer, ZonePreviewLayer } from "./ZoneLayer"
import { PlayerLayer, PlayerStatusSummary } from "./PlayerLayer"
import { MapControls } from "./MapControls"
import { 
  MAPBOX_TOKEN, 
  isMapboxConfigured,
  DEFAULT_CENTER,
  MAP_STYLE,
  MAP_STYLES,
  type MapStyleKey
} from "./map-config"
import { Card, CardContent } from "@/components/ui/card"
import { AlertTriangle, MapPin } from "lucide-react"

// Import Mapbox CSS
import "mapbox-gl/dist/mapbox-gl.css"

interface MapViewProps {
  coins: Coin[]
  /** Zones to display on the map */
  zones?: Zone[]
  /** Active players to display on the map */
  players?: ActivePlayer[]
  height?: string | number
  onCoinClick?: (coin: Coin) => void
  onCoinEdit?: (coin: Coin) => void
  onCoinDelete?: (coin: Coin) => void
  onCoinDrag?: (coin: Coin, newLat: number, newLng: number) => void
  onMapClick?: (lat: number, lng: number) => void
  /** Zone interaction handlers */
  onZoneClick?: (zone: Zone) => void
  onZoneEdit?: (zone: Zone) => void
  onZoneDelete?: (zone: Zone) => void
  onZoneToggleStatus?: (zone: Zone) => void
  /** Player interaction handlers */
  onPlayerClick?: (player: ActivePlayer) => void
  onViewPlayerProfile?: (player: ActivePlayer) => void
  onTrackPlayer?: (player: ActivePlayer) => void
  selectedCoinId?: string
  /** Selected zone for popup */
  selectedZoneId?: string
  /** Selected player for popup */
  selectedPlayerId?: string
  initialCenter?: { latitude: number; longitude: number; zoom: number }
  className?: string
  /** Enable click-to-place mode */
  placementMode?: boolean
  /** Enable drag-to-reposition coins */
  enableDrag?: boolean
  /** Enable zone drawing mode */
  zoneDrawMode?: 'circle' | 'polygon' | null
  /** Preview geometry while drawing a zone */
  zonePreview?: {
    type: 'circle' | 'polygon'
    center?: { latitude: number; longitude: number }
    radius?: number
    polygon?: { latitude: number; longitude: number }[]
  } | null
  /** Zone type for preview styling */
  previewZoneType?: ZoneType
  /** Show zone labels on map */
  showZoneLabels?: boolean
  /** Show players on map */
  showPlayers?: boolean
  /** Enable player clustering */
  enablePlayerClustering?: boolean
  /** Show player status summary in legend */
  showPlayerStatusSummary?: boolean
}

export function MapView({
  coins,
  zones = [],
  players = [],
  height = 500,
  onCoinClick,
  onCoinEdit,
  onCoinDelete,
  onCoinDrag,
  onMapClick,
  onZoneClick,
  onZoneEdit,
  onZoneDelete,
  onZoneToggleStatus,
  onPlayerClick,
  onViewPlayerProfile,
  onTrackPlayer,
  selectedCoinId,
  selectedZoneId,
  selectedPlayerId,
  initialCenter,
  className = "",
  placementMode = false,
  enableDrag = false,
  zoneDrawMode = null,
  zonePreview = null,
  previewZoneType = "player",
  showZoneLabels = true,
  showPlayers = true,
  enablePlayerClustering = true,
  showPlayerStatusSummary = true,
}: MapViewProps) {
  const mapRef = useRef<MapRef>(null)
  
  // View state for controlled map
  const [viewState, setViewState] = useState({
    longitude: initialCenter?.longitude ?? DEFAULT_CENTER.longitude,
    latitude: initialCenter?.latitude ?? DEFAULT_CENTER.latitude,
    zoom: initialCenter?.zoom ?? DEFAULT_CENTER.zoom,
  })

  // Map style (derives initial key from MAP_STYLE so dropdown matches displayed style)
  const [mapStyle, setMapStyle] = useState<string>(MAP_STYLE)
  const [currentStyleKey, setCurrentStyleKey] = useState<MapStyleKey>(() => {
    const key = Object.entries(MAP_STYLES).find(([, v]) => v === MAP_STYLE)?.[0] as MapStyleKey
    return key ?? "custom"
  })

  // Filter state - show all statuses by default
  const [visibleStatuses, setVisibleStatuses] = useState<CoinStatus[]>([
    "visible", "hidden", "collected", "expired", "recycled"
  ])

  // Filter coins based on status
  const filteredCoins = useMemo(() => {
    return coins.filter(coin => visibleStatuses.includes(coin.status))
  }, [coins, visibleStatuses])

  // Calculate bounds to fit all coins (user-triggered via Fit Bounds control)
  const fitBounds = useCallback(() => {
    if (!mapRef.current || filteredCoins.length === 0) return

    const lngs = filteredCoins.map(c => c.longitude)
    const lats = filteredCoins.map(c => c.latitude)

    const minLng = Math.min(...lngs)
    const maxLng = Math.max(...lngs)
    const minLat = Math.min(...lats)
    const maxLat = Math.max(...lats)

    // Add padding
    const padding = 50

    mapRef.current.fitBounds(
      [[minLng, minLat], [maxLng, maxLat]],
      { padding, duration: 1000 }
    )
  }, [filteredCoins])

  // Map always starts at default center (Crescent City, CA); use Fit Bounds to zoom to coins

  // Handle zoom controls
  const handleZoomIn = useCallback(() => {
    setViewState(prev => ({ ...prev, zoom: Math.min(prev.zoom + 1, 20) }))
  }, [])

  const handleZoomOut = useCallback(() => {
    setViewState(prev => ({ ...prev, zoom: Math.max(prev.zoom - 1, 1) }))
  }, [])

  // Handle style change
  const handleStyleChange = useCallback((style: string) => {
    setMapStyle(style)
    // Find the key for this style
    const key = Object.entries(MAP_STYLES).find(([, v]) => v === style)?.[0] as MapStyleKey
    if (key) setCurrentStyleKey(key)
  }, [])

  // Handle map click (for placing coins)
  const handleMapClick = useCallback((event: mapboxgl.MapLayerMouseEvent) => {
    if (onMapClick) {
      onMapClick(event.lngLat.lat, event.lngLat.lng)
    }
  }, [onMapClick])

  // Locate user (browser geolocation)
  const handleLocateUser = useCallback(() => {
    if (!navigator.geolocation) {
      alert("Geolocation is not supported by your browser")
      return
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setViewState({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          zoom: 15,
        })
      },
      (error) => {
        console.error("Geolocation error:", error)
        alert("Unable to get your location")
      }
    )
  }, [])

  // Check if Mapbox is configured
  if (!isMapboxConfigured()) {
    return (
      <Card className={`border-fire/50 bg-fire/5 ${className}`} style={{ height }}>
        <CardContent className="flex flex-col items-center justify-center h-full gap-4">
          <AlertTriangle className="h-12 w-12 text-fire" />
          <div className="text-center">
            <h3 className="font-bold text-saddle-dark">Mapbox Not Configured</h3>
            <p className="text-sm text-leather-light mt-1">
              Add your Mapbox token to <code className="bg-parchment-dark px-1 rounded">.env.local</code>
            </p>
            <pre className="mt-2 text-xs bg-parchment p-2 rounded text-left">
              NEXT_PUBLIC_MAPBOX_TOKEN=pk.your-token-here
            </pre>
          </div>
        </CardContent>
      </Card>
    )
  }

  // Show message if no coins, no zones, AND no players (all empty)
  if (coins.length === 0 && zones.length === 0 && players.length === 0) {
    return (
      <Card className={`border-saddle-light/30 ${className}`} style={{ height }}>
        <CardContent className="flex flex-col items-center justify-center h-full gap-4">
          <MapPin className="h-12 w-12 text-leather-light" />
          <div className="text-center">
            <h3 className="font-bold text-saddle-dark">No Items Yet</h3>
            <p className="text-sm text-leather-light mt-1">
              Create coins or zones to see them on the map
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <div 
      className={`relative rounded-lg overflow-hidden border border-saddle-light/30 ${className} ${
        placementMode ? "ring-2 ring-gold" : ""
      } ${
        zoneDrawMode ? "ring-2 ring-fire" : ""
      }`} 
      style={{ height }}
    >
      <Map
        ref={mapRef}
        {...viewState}
        onMove={(evt: ViewStateChangeEvent) => setViewState(evt.viewState)}
        onClick={handleMapClick}
        mapStyle={mapStyle}
        mapboxAccessToken={MAPBOX_TOKEN}
        style={{ width: "100%", height: "100%" }}
        attributionControl={false}
        reuseMaps
        cursor={placementMode || zoneDrawMode ? "crosshair" : "grab"}
      >
        {/* Navigation control (compass) */}
        <NavigationControl position="bottom-left" showCompass showZoom={false} />
        
        {/* Scale bar */}
        <ScaleControl position="bottom-right" />

        {/* Zone layers (render before coins so coins appear on top) */}
        {zones.length > 0 && (
          <ZoneLayer
            zones={zones}
            selectedZoneId={selectedZoneId}
            onZoneClick={onZoneClick}
            onZoneEdit={onZoneEdit}
            onZoneDelete={onZoneDelete}
            onZoneToggleStatus={onZoneToggleStatus}
            showLabels={showZoneLabels}
            interactive={!placementMode && !zoneDrawMode}
          />
        )}

        {/* Zone preview while drawing */}
        {zonePreview && (
          <ZonePreviewLayer 
            geometry={zonePreview}
            zoneType={previewZoneType}
          />
        )}

        {/* Coin markers */}
        {filteredCoins.map((coin) => (
          <CoinMarker
            key={coin.id}
            coin={coin}
            onClick={onCoinClick}
            onEdit={onCoinEdit}
            onDelete={onCoinDelete}
            onDragEnd={onCoinDrag}
            isSelected={coin.id === selectedCoinId}
            draggable={enableDrag && !placementMode && !zoneDrawMode}
          />
        ))}

        {/* Player markers */}
        {showPlayers && players.length > 0 && (
          <PlayerLayer
            players={players}
            selectedPlayerId={selectedPlayerId}
            onPlayerClick={onPlayerClick}
            onViewProfile={onViewPlayerProfile}
            onTrackPlayer={onTrackPlayer}
            enableClustering={enablePlayerClustering}
            showPopupsOnHover={!placementMode && !zoneDrawMode}
          />
        )}
      </Map>

      {/* Placement mode crosshair overlay */}
      {placementMode && (
        <div className="absolute inset-0 pointer-events-none flex items-center justify-center">
          <div className="relative">
            {/* Crosshair */}
            <div className="absolute -translate-x-1/2 -translate-y-1/2 left-1/2 top-1/2">
              <div className="w-8 h-0.5 bg-gold shadow-sm" />
              <div className="w-0.5 h-8 bg-gold shadow-sm absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2" />
            </div>
            {/* Pulsing coin preview */}
            <div className="w-6 h-6 bg-gold/50 rounded-full animate-ping absolute -translate-x-1/2 -translate-y-1/2 left-1/2 top-1/2" />
            <div className="w-4 h-4 bg-gold rounded-full border-2 border-saddle absolute -translate-x-1/2 -translate-y-1/2 left-1/2 top-1/2" />
          </div>
        </div>
      )}

      {/* Placement mode instruction banner */}
      {placementMode && (
        <div className="absolute top-4 left-1/2 -translate-x-1/2 bg-gold text-leather px-4 py-2 rounded-full shadow-lg text-sm font-medium flex items-center gap-2 animate-bounce">
          <span>🎯</span>
          Click to place coin here
        </div>
      )}

      {/* Zone draw mode instruction banner */}
      {zoneDrawMode === 'circle' && (
        <div className="absolute top-4 left-1/2 -translate-x-1/2 bg-fire text-white px-4 py-2 rounded-full shadow-lg text-sm font-medium flex items-center gap-2 animate-bounce">
          <span>⭕</span>
          Click to set zone center
        </div>
      )}
      {zoneDrawMode === 'polygon' && (
        <div className="absolute top-4 left-1/2 -translate-x-1/2 bg-fire text-white px-4 py-2 rounded-full shadow-lg text-sm font-medium flex items-center gap-2 animate-bounce">
          <span>📐</span>
          Click to add points, double-click to finish
        </div>
      )}

      {/* Map controls overlay */}
      <MapControls
        onZoomIn={handleZoomIn}
        onZoomOut={handleZoomOut}
        onFitBounds={fitBounds}
        onLocateUser={handleLocateUser}
        onStyleChange={handleStyleChange}
        currentStyle={currentStyleKey}
        visibleStatuses={visibleStatuses}
        onStatusFilterChange={setVisibleStatuses}
        coinCount={filteredCoins.length}
      />

      {/* Legend */}
      <div className="absolute bottom-4 left-4 bg-white/90 backdrop-blur-sm rounded-lg p-2 shadow-lg text-xs">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-1">
            <span className="w-3 h-3 rounded-full bg-[#FFD700]" />
            <span>Visible</span>
          </div>
          <div className="flex items-center gap-1">
            <span className="w-3 h-3 rounded-full bg-[#8B4513]" />
            <span>Hidden</span>
          </div>
          <div className="flex items-center gap-1">
            <span className="w-3 h-3 rounded-full bg-[#22C55E]" />
            <span>Collected</span>
          </div>
        </div>
        {/* Player status summary */}
        {showPlayers && showPlayerStatusSummary && players.length > 0 && (
          <div className="mt-2 pt-2 border-t border-gray-200">
            <PlayerStatusSummary players={players} />
          </div>
        )}
      </div>
    </div>
  )
}
