"use client"

import { useState, Suspense, useCallback } from "react"
import dynamic from "next/dynamic"
import type { Coin } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { CoinsTable } from "@/components/dashboard/coins-table"
import { CoinsSearch } from "@/components/dashboard/coins-search"
import { CoinDialog } from "@/components/dashboard/coin-dialog"
import { Button } from "@/components/ui/button"
import { Map, Table2, Loader2, MapPin, MousePointerClick, X, Move } from "lucide-react"
import { toast } from "sonner"
import { createClient } from "@/lib/supabase/client"
import { useRouter } from "next/navigation"

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

interface CoinsPageClientProps {
  coins: Coin[]
  userId: string
  error?: string
  hasFilters: boolean
  totalCoins: number
  searchParams: { search?: string; status?: string; tier?: string }
}

export function CoinsPageClient({ 
  coins, 
  userId, 
  error, 
  hasFilters, 
  totalCoins,
  searchParams 
}: CoinsPageClientProps) {
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingCoin, setEditingCoin] = useState<Coin | null>(null)
  const [selectedCoinId, setSelectedCoinId] = useState<string | undefined>()
  const [activeTab, setActiveTab] = useState<string>("map")
  
  // Placement mode state
  const [placementMode, setPlacementMode] = useState(false)
  const [clickedCoordinates, setClickedCoordinates] = useState<{ lat: number; lng: number } | null>(null)
  
  // Drag mode state
  const [dragMode, setDragMode] = useState(false)
  
  const router = useRouter()
  const supabase = createClient()

  const handleCreateClick = () => {
    setEditingCoin(null)
    setClickedCoordinates(null) // Clear any map-clicked coordinates
    setDialogOpen(true)
  }

  const handleEditCoin = (coin: Coin) => {
    setEditingCoin(coin)
    setClickedCoordinates(null)
    setDialogOpen(true)
  }

  const handleCoinClick = (coin: Coin) => {
    setSelectedCoinId(coin.id)
  }

  // Handle map click - only active in placement mode
  const handleMapClick = useCallback((lat: number, lng: number) => {
    if (placementMode) {
      // Store clicked coordinates and open dialog
      setClickedCoordinates({ lat, lng })
      setEditingCoin(null)
      setDialogOpen(true)
      setPlacementMode(false) // Exit placement mode after click
      toast.success("📍 Location selected!", {
        description: `${lat.toFixed(6)}, ${lng.toFixed(6)}`,
      })
    }
  }, [placementMode])

  // Toggle placement mode
  const togglePlacementMode = () => {
    if (!placementMode) {
      setPlacementMode(true)
      setDragMode(false) // Can't be in both modes
      toast.info("🎯 Click on the map to place a coin", {
        description: "Click anywhere on the map to set the coin location",
        duration: 5000,
      })
    } else {
      setPlacementMode(false)
      toast.dismiss()
    }
  }

  // Clear coordinates when dialog closes
  const handleDialogChange = (open: boolean) => {
    setDialogOpen(open)
    if (!open) {
      // Don't clear coordinates immediately - let the dialog use them first
      setTimeout(() => setClickedCoordinates(null), 100)
    }
  }

  // Handle coin drag to new position
  const handleCoinDrag = useCallback(async (coin: Coin, newLat: number, newLng: number) => {
    const { error } = await supabase
      .from("coins")
      .update({ 
        latitude: newLat, 
        longitude: newLng,
        updated_at: new Date().toISOString()
      })
      .eq("id", coin.id)

    if (error) {
      toast.error("Failed to move coin", {
        description: error.message,
      })
    } else {
      toast.success("📍 Coin moved!", {
        description: `New position: ${newLat.toFixed(6)}, ${newLng.toFixed(6)}`,
      })
      router.refresh()
    }
  }, [supabase, router])

  // Toggle drag mode
  const toggleDragMode = () => {
    if (!dragMode) {
      setDragMode(true)
      setPlacementMode(false) // Can't be in both modes
      toast.info("🖐️ Drag mode enabled", {
        description: "Drag any coin marker to reposition it",
        duration: 5000,
      })
    } else {
      setDragMode(false)
      toast.dismiss()
    }
  }

  return (
    <>
      {/* Search and Filter */}
      <Suspense fallback={<div className="h-10 bg-parchment animate-pulse rounded" />}>
        <CoinsSearch onCreateClick={handleCreateClick} />
      </Suspense>

      {/* Results info */}
      {hasFilters && (
        <p className="text-sm text-leather-light">
          Showing {coins.length} of {totalCoins} coins
          {searchParams.search && <span> matching &quot;{searchParams.search}&quot;</span>}
          {searchParams.status && searchParams.status !== "all" && (
            <span> with status &quot;{searchParams.status}&quot;</span>
          )}
          {searchParams.tier && searchParams.tier !== "all" && (
            <span> tier &quot;{searchParams.tier}&quot;</span>
          )}
        </p>
      )}

      {/* Map and Table Tabs */}
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
            value="table"
            className="data-[state=active]:bg-gold data-[state=active]:text-leather"
          >
            <Table2 className="h-4 w-4 mr-2" />
            Table View
          </TabsTrigger>
        </TabsList>

        {/* Map View Tab */}
        <TabsContent value="map" className="mt-4">
          <Card className={`border-saddle-light/30 ${
            placementMode ? "ring-2 ring-gold ring-offset-2" : 
            dragMode ? "ring-2 ring-brass ring-offset-2" : ""
          }`}>
            <CardHeader className="pb-2">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle className="text-saddle-dark flex items-center gap-2">
                    <Map className="h-5 w-5 text-gold" />
                    Coin Map
                    {placementMode && (
                      <span className="ml-2 px-2 py-0.5 bg-gold/20 text-gold-dark text-xs rounded-full animate-pulse">
                        📍 Placement Mode
                      </span>
                    )}
                    {dragMode && (
                      <span className="ml-2 px-2 py-0.5 bg-brass/20 text-brass text-xs rounded-full animate-pulse">
                        🖐️ Drag Mode
                      </span>
                    )}
                  </CardTitle>
                  <CardDescription>
                    {placementMode 
                      ? "Click anywhere on the map to place a new coin"
                      : dragMode
                        ? "Drag coin markers to reposition them"
                        : `${coins.length} coins displayed • Click coins for details`
                    }
                  </CardDescription>
                </div>
                <div className="flex gap-2">
                  <Button
                    variant={dragMode ? "default" : "outline"}
                    size="sm"
                    onClick={toggleDragMode}
                    className={dragMode 
                      ? "bg-brass hover:bg-brass/80 text-white" 
                      : "border-saddle-light/50"
                    }
                    disabled={placementMode}
                  >
                    {dragMode ? (
                      <>
                        <X className="h-4 w-4 mr-1" />
                        Done
                      </>
                    ) : (
                      <>
                        <Move className="h-4 w-4 mr-1" />
                        Move
                      </>
                    )}
                  </Button>
                  <Button
                    variant={placementMode ? "default" : "outline"}
                    size="sm"
                    onClick={togglePlacementMode}
                    className={placementMode 
                      ? "bg-gold hover:bg-gold-dark text-leather" 
                      : "border-saddle-light/50"
                    }
                    disabled={dragMode}
                  >
                    {placementMode ? (
                      <>
                        <X className="h-4 w-4 mr-1" />
                        Cancel
                      </>
                    ) : (
                      <>
                        <MousePointerClick className="h-4 w-4 mr-1" />
                        Place
                      </>
                    )}
                  </Button>
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-0">
              {error ? (
                <div className="text-fire text-sm p-4 bg-fire/10 rounded-lg m-4">
                  Error loading coins: {error}
                </div>
              ) : (
                <MapView
                  coins={coins}
                  height={500}
                  onCoinClick={handleCoinClick}
                  onCoinEdit={handleEditCoin}
                  onCoinDrag={handleCoinDrag}
                  onMapClick={handleMapClick}
                  selectedCoinId={selectedCoinId}
                  placementMode={placementMode}
                  enableDrag={dragMode}
                  className="rounded-b-lg"
                />
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Table View Tab */}
        <TabsContent value="table" className="mt-4">
          <Card className="border-saddle-light/30">
            <CardHeader>
              <CardTitle className="text-saddle-dark flex items-center gap-2">
                <Table2 className="h-5 w-5 text-gold" />
                {hasFilters ? "Search Results" : "All Coins"}
              </CardTitle>
              <CardDescription>
                {coins.length} {hasFilters ? "matching" : "total"} coins
              </CardDescription>
            </CardHeader>
            <CardContent>
              {error ? (
                <div className="text-fire text-sm p-4 bg-fire/10 rounded-lg">
                  Error loading coins: {error}
                </div>
              ) : (
                <CoinsTable coins={coins} onEdit={handleEditCoin} />
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Create/Edit Dialog */}
      <CoinDialog
        coin={editingCoin}
        open={dialogOpen}
        onOpenChange={handleDialogChange}
        userId={userId}
        initialCoordinates={clickedCoordinates}
      />
    </>
  )
}
