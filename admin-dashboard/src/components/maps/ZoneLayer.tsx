/**
 * Zone Layer Component for Map Visualization
 * 
 * @file admin-dashboard/src/components/maps/ZoneLayer.tsx
 * @description Renders zone polygons and circles on the Mapbox map
 * 
 * Character count: ~6,200
 */

"use client"

import { useMemo, useCallback } from "react"
import { Source, Layer, Popup } from "react-map-gl/mapbox"
import type { Zone } from "@/types/database"
import { 
  ZONE_TYPE_COLORS, 
  ZONE_STATUS_COLORS,
  zoneGeometryToGeoJSON,
  formatArea,
  formatRadius,
  calculateZoneArea
} from "./zone-config"
import type { FillLayerSpecification, LineLayerSpecification, SymbolLayerSpecification } from "mapbox-gl"
import { Button } from "@/components/ui/button"
import { Edit, Trash2, Eye, EyeOff, MapPin } from "lucide-react"

interface ZoneLayerProps {
  zones: Zone[]
  selectedZoneId?: string
  onZoneClick?: (zone: Zone) => void
  onZoneEdit?: (zone: Zone) => void
  onZoneDelete?: (zone: Zone) => void
  onZoneToggleStatus?: (zone: Zone) => void
  showLabels?: boolean
  interactive?: boolean
}

export function ZoneLayer({
  zones,
  selectedZoneId,
  onZoneClick: _onZoneClick, // Reserved for future Mapbox layer click events
  onZoneEdit,
  onZoneDelete,
  onZoneToggleStatus,
  showLabels = true,
  interactive = true,
}: ZoneLayerProps) {
  // Note: _onZoneClick will be used when we implement Mapbox layer interactivity
  void _onZoneClick
  // Selected zone for popup
  const selectedZone = useMemo(() => {
    return zones.find(z => z.id === selectedZoneId)
  }, [zones, selectedZoneId])

  // Convert zones to GeoJSON FeatureCollection
  const geoJsonData = useMemo(() => {
    const features = zones
      .filter(zone => {
        const coordinates = zoneGeometryToGeoJSON(zone.geometry)
        if (coordinates.length === 0) {
          console.warn(`Zone ${zone.name} has no valid geometry`)
          return false
        }
        return true
      })
      .map(zone => {
        const coordinates = zoneGeometryToGeoJSON(zone.geometry)
      
        // Get colors based on zone type (or use custom if set)
        const typeColors = ZONE_TYPE_COLORS[zone.zone_type]
        const fillColor = zone.fill_color || typeColors.fill
        const borderColor = zone.border_color || typeColors.border
        // Use zone's opacity if set, otherwise use type's default opacity
        const opacity = zone.opacity ?? typeColors.opacity ?? 0.3
      
        // Determine if zone should be highlighted
        const isSelected = zone.id === selectedZoneId
      
        return {
          type: "Feature" as const,
          id: zone.id,
          properties: {
            id: zone.id,
            name: zone.name,
            zone_type: zone.zone_type,
            status: zone.status,
            fillColor,
            borderColor,
            opacity: isSelected ? Math.min(opacity + 0.2, 0.6) : opacity,
            borderWidth: isSelected ? 4 : 2,
            coins_placed: zone.coins_placed,
            coins_collected: zone.coins_collected,
            active_players: zone.active_players,
          },
          geometry: {
            type: "Polygon" as const,
            coordinates: [coordinates],
          },
        }
      })

    return {
      type: "FeatureCollection" as const,
      features,
    }
  }, [zones, selectedZoneId])

  // Label points (center of each zone)
  const labelData = useMemo(() => {
    if (!showLabels) return { type: "FeatureCollection" as const, features: [] }

    const features = zones.map(zone => {
      let center = { latitude: 0, longitude: 0 }
      
      if (zone.geometry.type === 'circle' && zone.geometry.center) {
        center = zone.geometry.center
      } else if (zone.geometry.type === 'polygon' && zone.geometry.polygon) {
        // Calculate centroid
        const sum = zone.geometry.polygon.reduce(
          (acc, p) => ({ latitude: acc.latitude + p.latitude, longitude: acc.longitude + p.longitude }),
          { latitude: 0, longitude: 0 }
        )
        center = {
          latitude: sum.latitude / zone.geometry.polygon.length,
          longitude: sum.longitude / zone.geometry.polygon.length,
        }
      }

      const statusConfig = ZONE_STATUS_COLORS[zone.status]
      
      return {
        type: "Feature" as const,
        properties: {
          id: zone.id,
          name: zone.name,
          emoji: statusConfig.emoji,
          coins: zone.coins_placed,
        },
        geometry: {
          type: "Point" as const,
          coordinates: [center.longitude, center.latitude],
        },
      }
    })

    return {
      type: "FeatureCollection" as const,
      features,
    }
  }, [zones, showLabels])

  // Fill layer style - Omit 'source' as it's inherited from parent Source component
  const fillLayerStyle: Omit<FillLayerSpecification, 'source'> = {
    id: "zones-fill",
    type: "fill",
    paint: {
      "fill-color": ["get", "fillColor"],
      "fill-opacity": ["get", "opacity"],
    },
  }

  // Border layer style
  const borderLayerStyle: Omit<LineLayerSpecification, 'source'> = {
    id: "zones-border",
    type: "line",
    paint: {
      "line-color": ["get", "borderColor"],
      "line-width": ["get", "borderWidth"],
      "line-dasharray": [2, 2], // Dashed for Western map feel
    },
  }

  // Label layer style
  const labelLayerStyle: Omit<SymbolLayerSpecification, 'source'> = {
    id: "zones-labels",
    type: "symbol",
    layout: {
      "text-field": ["concat", ["get", "emoji"], " ", ["get", "name"]],
      "text-size": 12,
      "text-anchor": "center",
      "text-allow-overlap": false,
    },
    paint: {
      "text-color": "#3D2914",
      "text-halo-color": "#FFFFFF",
      "text-halo-width": 2,
    },
  }

  // Get popup position for selected zone
  const getPopupPosition = useCallback(() => {
    if (!selectedZone) return null
    
    if (selectedZone.geometry.type === 'circle' && selectedZone.geometry.center) {
      return {
        latitude: selectedZone.geometry.center.latitude,
        longitude: selectedZone.geometry.center.longitude,
      }
    }
    
    if (selectedZone.geometry.type === 'polygon' && selectedZone.geometry.polygon) {
      const sum = selectedZone.geometry.polygon.reduce(
        (acc, p) => ({ latitude: acc.latitude + p.latitude, longitude: acc.longitude + p.longitude }),
        { latitude: 0, longitude: 0 }
      )
      return {
        latitude: sum.latitude / selectedZone.geometry.polygon.length,
        longitude: sum.longitude / selectedZone.geometry.polygon.length,
      }
    }
    
    return null
  }, [selectedZone])

  const popupPosition = getPopupPosition()

  return (
    <>
      {/* Zone polygons */}
      <Source id="zones-source" type="geojson" data={geoJsonData}>
        <Layer {...fillLayerStyle} />
        <Layer {...borderLayerStyle} />
      </Source>

      {/* Zone labels */}
      {showLabels && (
        <Source id="zones-labels-source" type="geojson" data={labelData}>
          <Layer {...labelLayerStyle} />
        </Source>
      )}

      {/* Selected zone popup */}
      {selectedZone && popupPosition && interactive && (
        <Popup
          latitude={popupPosition.latitude}
          longitude={popupPosition.longitude}
          closeButton={false}
          closeOnClick={false}
          anchor="bottom"
          offset={20}
          className="zone-popup"
        >
          <div className="bg-white rounded-lg shadow-lg p-3 min-w-[220px]">
            {/* Header */}
            <div className="flex items-center gap-2 mb-2">
              <span className="text-lg">
                {ZONE_STATUS_COLORS[selectedZone.status].emoji}
              </span>
              <div className="flex-1">
                <h4 className="font-bold text-saddle-dark text-sm">
                  {selectedZone.name}
                </h4>
                <p className="text-xs text-leather-light capitalize">
                  {ZONE_TYPE_COLORS[selectedZone.zone_type].label}
                </p>
              </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-3 gap-2 text-center text-xs mb-3 py-2 bg-parchment rounded">
              <div>
                <div className="font-bold text-gold">{selectedZone.coins_placed}</div>
                <div className="text-leather-light">Coins</div>
              </div>
              <div>
                <div className="font-bold text-green-600">{selectedZone.coins_collected}</div>
                <div className="text-leather-light">Found</div>
              </div>
              <div>
                <div className="font-bold text-blue-600">{selectedZone.active_players}</div>
                <div className="text-leather-light">Players</div>
              </div>
            </div>

            {/* Zone info */}
            <div className="text-xs text-leather-light mb-3 space-y-1">
              {selectedZone.geometry.type === 'circle' && selectedZone.geometry.radius_meters && (
                <div className="flex items-center gap-1">
                  <MapPin className="h-3 w-3" />
                  <span>Radius: {formatRadius(selectedZone.geometry.radius_meters)}</span>
                </div>
              )}
              <div className="flex items-center gap-1">
                <span>Area: {formatArea(calculateZoneArea(selectedZone.geometry))}</span>
              </div>
            </div>

            {/* Actions */}
            <div className="flex gap-1 justify-end border-t border-saddle-light/20 pt-2">
              {onZoneToggleStatus && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onZoneToggleStatus(selectedZone)}
                  className="h-7 px-2"
                  title={selectedZone.status === 'active' ? 'Deactivate' : 'Activate'}
                >
                  {selectedZone.status === 'active' ? (
                    <EyeOff className="h-3.5 w-3.5" />
                  ) : (
                    <Eye className="h-3.5 w-3.5" />
                  )}
                </Button>
              )}
              {onZoneEdit && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onZoneEdit(selectedZone)}
                  className="h-7 px-2"
                >
                  <Edit className="h-3.5 w-3.5" />
                </Button>
              )}
              {onZoneDelete && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onZoneDelete(selectedZone)}
                  className="h-7 px-2 text-red-600 hover:text-red-700 hover:bg-red-50"
                >
                  <Trash2 className="h-3.5 w-3.5" />
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
 * Preview layer for zone being drawn/created
 */
interface ZonePreviewLayerProps {
  geometry: {
    type: 'circle' | 'polygon'
    center?: { latitude: number; longitude: number }
    radius?: number
    polygon?: { latitude: number; longitude: number }[]
  } | null
  zoneType: "player" | "sponsor" | "hunt" | "grid"
}

export function ZonePreviewLayer({ geometry, zoneType }: ZonePreviewLayerProps) {
  const previewData = useMemo(() => {
    if (!geometry) {
      return { type: "FeatureCollection" as const, features: [] }
    }

    let coordinates: [number, number][] = []

    if (geometry.type === 'circle' && geometry.center && geometry.radius) {
      // Generate circle polygon
      const numPoints = 64
      for (let i = 0; i <= numPoints; i++) {
        const angle = (i / numPoints) * 2 * Math.PI
        const earthRadius = 6371000
        const latOffset = (geometry.radius / earthRadius) * Math.cos(angle) * (180 / Math.PI)
        const lngOffset = (geometry.radius / earthRadius) * Math.sin(angle) / 
                          Math.cos(geometry.center.latitude * Math.PI / 180) * (180 / Math.PI)
        coordinates.push([
          geometry.center.longitude + lngOffset,
          geometry.center.latitude + latOffset
        ])
      }
    } else if (geometry.type === 'polygon' && geometry.polygon && geometry.polygon.length >= 2) {
      coordinates = geometry.polygon.map(p => [p.longitude, p.latitude])
      // Close polygon for preview
      if (coordinates.length >= 3) {
        coordinates.push([...coordinates[0]])
      }
    }

    if (coordinates.length < 3) {
      return { type: "FeatureCollection" as const, features: [] }
    }

    const typeColors = ZONE_TYPE_COLORS[zoneType]

    return {
      type: "FeatureCollection" as const,
      features: [{
        type: "Feature" as const,
        properties: {
          fillColor: typeColors.fill,
          borderColor: typeColors.border,
        },
        geometry: {
          type: "Polygon" as const,
          coordinates: [coordinates],
        },
      }],
    }
  }, [geometry, zoneType])

  if (!geometry) return null

  return (
    <Source id="zone-preview-source" type="geojson" data={previewData}>
      <Layer
        id="zone-preview-fill"
        type="fill"
        paint={{
          "fill-color": ["get", "fillColor"],
          "fill-opacity": 0.4,
        }}
      />
      <Layer
        id="zone-preview-border"
        type="line"
        paint={{
          "line-color": ["get", "borderColor"],
          "line-width": 3,
          "line-dasharray": [4, 2],
        }}
      />
    </Source>
  )
}
