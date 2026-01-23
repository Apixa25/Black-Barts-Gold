/**
 * Zones Page Client Component
 * 
 * @file admin-dashboard/src/app/(dashboard)/zones/zones-client.tsx
 * @description Client-side zone management with map visualization and drawing tools
 * 
 * Character count: ~11,500
 */

"use client"

import { useState, useCallback, useMemo } from "react"
import dynamic from "next/dynamic"
import type { Zone, ZoneType, ZoneGeometry, Coin } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { 
  Map, 
  Table2, 
  Loader2, 
  Plus, 
  Circle, 
  Hexagon, 
  X, 
  Eye,
  EyeOff,
  Edit,
  Trash2,
  MapPinned,
  Zap,
  Calendar,
  Building2
} from "lucide-react"
import { toast } from "sonner"
import { ZoneDialog } from "@/components/maps/ZoneDialog"
import { AutoDistributionPanel } from "@/components/dashboard/auto-distribution-panel"
import { TimedReleasesPanel } from "@/components/dashboard/timed-releases-panel"
import { SponsorFeaturesPanel } from "@/components/dashboard/sponsor-features-panel"
import { 
  ZONE_TYPE_COLORS, 
  ZONE_STATUS_COLORS,
  formatRadius,
  formatArea,
  calculateZoneArea
} from "@/components/maps/zone-config"

// Dynamically import MapView to avoid SSR issues with Mapbox
const MapView = dynamic(
  () => import("@/components/maps/MapView").then(mod => mod.MapView),
  { 
    ssr: false,
    loading: () => (
      <div className="h-[500px] bg-parchment rounded-lg flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-gold" />
      </div>
    )
  }
)

interface ZonesPageClientProps {
  zones: Zone[]
  userId: string
}

export function ZonesPageClient({ zones, userId }: ZonesPageClientProps) {
  const [activeTab, setActiveTab] = useState<string>("map")
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingZone, setEditingZone] = useState<Zone | null>(null)
  const [selectedZoneId, setSelectedZoneId] = useState<string | undefined>()
  
  // Zone draw mode: null = off, 'circle' = drawing circle, 'polygon' = drawing polygon
  const [zoneDrawMode, setZoneDrawMode] = useState<'circle' | 'polygon' | null>(null)
  const [newZoneType, setNewZoneType] = useState<ZoneType>("player")
  
  // Preview geometry while drawing
  const [zonePreview, setZonePreview] = useState<{
    type: 'circle' | 'polygon'
    center?: { latitude: number; longitude: number }
    radius?: number
    polygon?: { latitude: number; longitude: number }[]
  } | null>(null)
  
  // Initial geometry for the dialog (from map drawing)
  const [initialGeometry, setInitialGeometry] = useState<ZoneGeometry | null>(null)

  // Empty coins array for map (zones page doesn't show coins)
  const emptyCoins: Coin[] = []

  // Handle zone click
  const handleZoneClick = useCallback((zone: Zone) => {
    setSelectedZoneId(zone.id)
  }, [])

  // Handle zone edit
  const handleZoneEdit = useCallback((zone: Zone) => {
    setEditingZone(zone)
    setInitialGeometry(zone.geometry)
    setDialogOpen(true)
  }, [])

  // Handle zone delete
  const handleZoneDelete = useCallback((zone: Zone) => {
    // TODO: Implement delete with confirmation dialog
    toast.info("Delete zone", {
      description: `Would delete zone: ${zone.name}`,
    })
  }, [])

  // Handle zone status toggle
  const handleZoneToggleStatus = useCallback((zone: Zone) => {
    const newStatus = zone.status === 'active' ? 'inactive' : 'active'
    toast.success(`Zone ${newStatus}`, {
      description: `${zone.name} is now ${newStatus}`,
    })
    // TODO: Update in database
  }, [])

  // Handle map click for zone drawing
  const handleMapClick = useCallback((lat: number, lng: number) => {
    if (zoneDrawMode === 'circle') {
      // Set center and show preview
      setZonePreview({
        type: 'circle',
        center: { latitude: lat, longitude: lng },
        radius: 1609, // Default 1 mile
      })
      
      // Set initial geometry and open dialog
      setInitialGeometry({
        type: 'circle',
        center: { latitude: lat, longitude: lng },
        radius_meters: 1609,
      })
      setEditingZone(null)
      setDialogOpen(true)
      setZoneDrawMode(null)
      
      toast.success("📍 Zone center set!", {
        description: `${lat.toFixed(6)}, ${lng.toFixed(6)}`,
      })
    } else if (zoneDrawMode === 'polygon') {
      // Add point to polygon
      setZonePreview(prev => {
        const newPolygon = prev?.polygon 
          ? [...prev.polygon, { latitude: lat, longitude: lng }]
          : [{ latitude: lat, longitude: lng }]
        
        return {
          type: 'polygon',
          polygon: newPolygon,
        }
      })
    }
  }, [zoneDrawMode])

  // Start circle draw mode
  const startCircleDraw = (type: ZoneType) => {
    setZoneDrawMode('circle')
    setNewZoneType(type)
    setZonePreview(null)
    toast.info("⭕ Click on the map to set zone center", {
      duration: 5000,
    })
  }

  // Start polygon draw mode
  const startPolygonDraw = (type: ZoneType) => {
    setZoneDrawMode('polygon')
    setNewZoneType(type)
    setZonePreview({ type: 'polygon', polygon: [] })
    toast.info("📐 Click to add points, click first point to close", {
      duration: 5000,
    })
  }

  // Cancel draw mode
  const cancelDrawMode = () => {
    setZoneDrawMode(null)
    setZonePreview(null)
    toast.dismiss()
  }

  // Create zone handler
  const handleCreateZone = () => {
    setEditingZone(null)
    setInitialGeometry(null)
    setDialogOpen(true)
  }

  // Save zone handler
  const handleSaveZone = async (zoneData: Partial<Zone>) => {
    // TODO: Save to database
    console.log("Saving zone:", zoneData)
    toast.success("Zone saved! 🗺️", {
      description: zoneData.name,
    })
    setZonePreview(null)
  }

  // Zone type filter
  const [zoneTypeFilter, setZoneTypeFilter] = useState<ZoneType | 'all'>('all')
  
  const filteredZones = useMemo(() => {
    if (zoneTypeFilter === 'all') return zones
    return zones.filter(z => z.zone_type === zoneTypeFilter)
  }, [zones, zoneTypeFilter])

  return (
    <>
      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
        <TabsList className="bg-parchment border border-saddle-light/30">
          <TabsTrigger 
            value="map" 
            className="data-[state=active]:bg-gold data-[state=active]:text-leather"
          >
            <Map className="h-4 w-4 mr-2" />
            Map View
          </TabsTrigger>
          <TabsTrigger 
            value="list"
            className="data-[state=active]:bg-gold data-[state=active]:text-leather"
          >
            <Table2 className="h-4 w-4 mr-2" />
            List View
          </TabsTrigger>
          <TabsTrigger 
            value="distribution"
            className="data-[state=active]:bg-gold data-[state=active]:text-leather"
          >
            <Zap className="h-4 w-4 mr-2" />
            Auto-Distribution
          </TabsTrigger>
          <TabsTrigger 
            value="timed-releases"
            className="data-[state=active]:bg-gold data-[state=active]:text-leather"
          >
            <Calendar className="h-4 w-4 mr-2" />
            Timed Releases
          </TabsTrigger>
          <TabsTrigger 
            value="sponsor-features"
            className="data-[state=active]:bg-gold data-[state=active]:text-leather"
          >
            <Building2 className="h-4 w-4 mr-2" />
            Sponsor Features
          </TabsTrigger>
        </TabsList>

        {/* Map View Tab */}
        <TabsContent value="map" className="mt-4">
          <Card className={`border-saddle-light/30 ${
            zoneDrawMode ? "ring-2 ring-fire ring-offset-2" : ""
          }`}>
            <CardHeader className="pb-2">
              <div className="flex items-center justify-between flex-wrap gap-2">
                <div>
                  <CardTitle className="text-saddle-dark flex items-center gap-2">
                    <MapPinned className="h-5 w-5 text-gold" />
                    Zone Map
                    {zoneDrawMode && (
                      <span className="ml-2 px-2 py-0.5 bg-fire/20 text-fire text-xs rounded-full animate-pulse">
                        {zoneDrawMode === 'circle' ? '⭕ Drawing Circle' : '📐 Drawing Polygon'}
                      </span>
                    )}
                  </CardTitle>
                  <CardDescription>
                    {zoneDrawMode 
                      ? zoneDrawMode === 'circle' 
                        ? "Click on the map to set the zone center"
                        : "Click to add points, double-click to finish"
                      : `${filteredZones.length} zones displayed • Click zones for details`
                    }
                  </CardDescription>
                </div>
                
                {/* Action Buttons */}
                <div className="flex gap-2 flex-wrap">
                  {zoneDrawMode ? (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={cancelDrawMode}
                      className="border-fire text-fire hover:bg-fire/10"
                    >
                      <X className="h-4 w-4 mr-1" />
                      Cancel
                    </Button>
                  ) : (
                    <>
                      {/* Quick draw buttons */}
                      <div className="flex gap-1 border rounded-lg p-1 bg-parchment">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => startCircleDraw('player')}
                          className="h-8 px-2"
                          title="Draw circle zone"
                        >
                          <Circle className="h-4 w-4 text-gold" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => startPolygonDraw('player')}
                          className="h-8 px-2"
                          title="Draw polygon zone"
                        >
                          <Hexagon className="h-4 w-4 text-gold" />
                        </Button>
                      </div>
                      
                      <Button
                        size="sm"
                        onClick={handleCreateZone}
                        className="bg-gold hover:bg-gold-dark text-leather"
                      >
                        <Plus className="h-4 w-4 mr-1" />
                        Create Zone
                      </Button>
                    </>
                  )}
                </div>
              </div>
              
              {/* Zone type filter chips */}
              <div className="flex gap-2 pt-2 flex-wrap">
                <Badge 
                  variant={zoneTypeFilter === 'all' ? 'default' : 'outline'}
                  className="cursor-pointer"
                  onClick={() => setZoneTypeFilter('all')}
                >
                  All ({zones.length})
                </Badge>
                {(['player', 'sponsor', 'hunt', 'grid'] as ZoneType[]).map(type => {
                  const count = zones.filter(z => z.zone_type === type).length
                  const colors = ZONE_TYPE_COLORS[type]
                  return (
                    <Badge 
                      key={type}
                      variant={zoneTypeFilter === type ? 'default' : 'outline'}
                      className="cursor-pointer"
                      style={{ 
                        borderColor: colors.border,
                        backgroundColor: zoneTypeFilter === type ? colors.border : 'transparent',
                        color: zoneTypeFilter === type ? 'white' : colors.border
                      }}
                      onClick={() => setZoneTypeFilter(type)}
                    >
                      {colors.label} ({count})
                    </Badge>
                  )
                })}
              </div>
            </CardHeader>
            
            <CardContent className="p-0">
              <MapView
                coins={emptyCoins}
                zones={filteredZones}
                height={500}
                onMapClick={handleMapClick}
                onZoneClick={handleZoneClick}
                onZoneEdit={handleZoneEdit}
                onZoneDelete={handleZoneDelete}
                onZoneToggleStatus={handleZoneToggleStatus}
                selectedZoneId={selectedZoneId}
                zoneDrawMode={zoneDrawMode}
                zonePreview={zonePreview}
                previewZoneType={newZoneType}
                showZoneLabels={true}
                className="rounded-b-lg"
              />
            </CardContent>
          </Card>
        </TabsContent>

        {/* List View Tab */}
        <TabsContent value="list" className="mt-4">
          <Card className="border-saddle-light/30">
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle className="text-saddle-dark flex items-center gap-2">
                  <Table2 className="h-5 w-5 text-gold" />
                  Zone List
                </CardTitle>
                <CardDescription>
                  {filteredZones.length} zones
                </CardDescription>
              </div>
              <Button
                size="sm"
                onClick={handleCreateZone}
                className="bg-gold hover:bg-gold-dark text-leather"
              >
                <Plus className="h-4 w-4 mr-1" />
                Create Zone
              </Button>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {filteredZones.map(zone => {
                  const typeColors = ZONE_TYPE_COLORS[zone.zone_type]
                  const statusColors = ZONE_STATUS_COLORS[zone.status]
                  
                  return (
                    <div 
                      key={zone.id}
                      className="flex items-center justify-between p-4 rounded-lg border border-saddle-light/30 hover:bg-parchment/50 transition-colors"
                    >
                      <div className="flex items-center gap-4">
                        {/* Zone type icon */}
                        <div 
                          className="w-10 h-10 rounded-full flex items-center justify-center"
                          style={{ backgroundColor: typeColors.fill, border: `2px solid ${typeColors.border}` }}
                        >
                          <MapPinned className="h-5 w-5" style={{ color: typeColors.border }} />
                        </div>
                        
                        {/* Zone info */}
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-semibold text-saddle-dark">{zone.name}</span>
                            <Badge 
                              variant="outline" 
                              className="text-xs"
                              style={{ 
                                borderColor: statusColors.color,
                                color: statusColors.color 
                              }}
                            >
                              {statusColors.emoji} {zone.status}
                            </Badge>
                          </div>
                          <div className="text-sm text-leather-light">
                            {typeColors.label} • 
                            {zone.geometry.type === 'circle' && zone.geometry.radius_meters && (
                              <> {formatRadius(zone.geometry.radius_meters)} radius • </>
                            )}
                            {formatArea(calculateZoneArea(zone.geometry))}
                          </div>
                        </div>
                      </div>
                      
                      {/* Stats and Actions */}
                      <div className="flex items-center gap-6">
                        {/* Stats */}
                        <div className="flex gap-4 text-center text-sm">
                          <div>
                            <div className="font-bold text-gold">{zone.coins_placed}</div>
                            <div className="text-xs text-leather-light">Coins</div>
                          </div>
                          <div>
                            <div className="font-bold text-blue-600">{zone.active_players}</div>
                            <div className="text-xs text-leather-light">Players</div>
                          </div>
                        </div>
                        
                        {/* Actions */}
                        <div className="flex gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleZoneToggleStatus(zone)}
                            className="h-8 w-8 p-0"
                          >
                            {zone.status === 'active' ? (
                              <EyeOff className="h-4 w-4" />
                            ) : (
                              <Eye className="h-4 w-4" />
                            )}
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleZoneEdit(zone)}
                            className="h-8 w-8 p-0"
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleZoneDelete(zone)}
                            className="h-8 w-8 p-0 text-red-600 hover:text-red-700 hover:bg-red-50"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    </div>
                  )
                })}
                
                {filteredZones.length === 0 && (
                  <div className="text-center py-12 text-leather-light">
                    <MapPinned className="h-12 w-12 mx-auto mb-4 opacity-50" />
                    <p>No zones yet. Create your first zone to get started!</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Auto-Distribution Tab */}
        <TabsContent value="distribution" className="mt-4">
          <AutoDistributionPanel />
        </TabsContent>

        {/* Timed Releases Tab */}
        <TabsContent value="timed-releases" className="mt-4">
          <TimedReleasesPanel zones={filteredZones} />
        </TabsContent>

        {/* Sponsor Features Tab */}
        <TabsContent value="sponsor-features" className="mt-4">
          <SponsorFeaturesPanel zones={filteredZones} />
        </TabsContent>
      </Tabs>

      {/* Zone Dialog */}
      <ZoneDialog
        zone={editingZone}
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        onSave={handleSaveZone}
        initialGeometry={initialGeometry}
        userId={userId}
      />
    </>
  )
}
