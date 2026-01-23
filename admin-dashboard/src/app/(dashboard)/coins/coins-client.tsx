"use client"

import { useState, Suspense } from "react"
import dynamic from "next/dynamic"
import type { Coin } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { CoinsTable } from "@/components/dashboard/coins-table"
import { CoinsSearch } from "@/components/dashboard/coins-search"
import { CoinDialog } from "@/components/dashboard/coin-dialog"
import { Map, Table2, Loader2 } from "lucide-react"

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

  const handleCreateClick = () => {
    setEditingCoin(null)
    setDialogOpen(true)
  }

  const handleEditCoin = (coin: Coin) => {
    setEditingCoin(coin)
    setDialogOpen(true)
  }

  const handleCoinClick = (coin: Coin) => {
    setSelectedCoinId(coin.id)
  }

  const handleMapClick = (lat: number, lng: number) => {
    // Pre-fill coordinates when creating a new coin from map click
    console.log("Map clicked at:", lat, lng)
    // Future: Open dialog with pre-filled coordinates
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
          <Card className="border-saddle-light/30">
            <CardHeader className="pb-2">
              <CardTitle className="text-saddle-dark flex items-center gap-2">
                <Map className="h-5 w-5 text-gold" />
                Coin Map
              </CardTitle>
              <CardDescription>
                {coins.length} coins displayed • Click coins for details
              </CardDescription>
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
                  onMapClick={handleMapClick}
                  selectedCoinId={selectedCoinId}
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
        onOpenChange={setDialogOpen}
        userId={userId}
      />
    </>
  )
}
