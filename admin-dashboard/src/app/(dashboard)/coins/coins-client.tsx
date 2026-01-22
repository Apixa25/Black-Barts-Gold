"use client"

import { useState, Suspense } from "react"
import type { Coin } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { CoinsTable } from "@/components/dashboard/coins-table"
import { CoinsSearch } from "@/components/dashboard/coins-search"
import { CoinDialog } from "@/components/dashboard/coin-dialog"

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

  const handleCreateClick = () => {
    setEditingCoin(null)
    setDialogOpen(true)
  }

  const handleEditCoin = (coin: Coin) => {
    setEditingCoin(coin)
    setDialogOpen(true)
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

      {/* Coins Table */}
      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">
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
