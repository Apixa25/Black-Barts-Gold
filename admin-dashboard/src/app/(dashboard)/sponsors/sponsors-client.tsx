"use client"

import { useState, Suspense } from "react"
import type { Sponsor } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { SponsorsTable } from "@/components/dashboard/sponsors-table"
import { SponsorsSearch } from "@/components/dashboard/sponsors-search"
import { SponsorDialog } from "@/components/dashboard/sponsor-dialog"

interface SponsorsPageClientProps {
  sponsors: Sponsor[]
  error?: string
  hasFilters: boolean
  totalSponsors: number
  searchParams: { search?: string; status?: string }
}

export function SponsorsPageClient({ 
  sponsors, 
  error, 
  hasFilters, 
  totalSponsors,
  searchParams 
}: SponsorsPageClientProps) {
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingSponsor, setEditingSponsor] = useState<Sponsor | null>(null)

  const handleCreateClick = () => {
    setEditingSponsor(null)
    setDialogOpen(true)
  }

  const handleEditSponsor = (sponsor: Sponsor) => {
    setEditingSponsor(sponsor)
    setDialogOpen(true)
  }

  return (
    <>
      {/* Search and Filter */}
      <Suspense fallback={<div className="h-10 bg-parchment animate-pulse rounded" />}>
        <SponsorsSearch onCreateClick={handleCreateClick} />
      </Suspense>

      {/* Results info */}
      {hasFilters && (
        <p className="text-sm text-leather-light">
          Showing {sponsors.length} of {totalSponsors} sponsors
          {searchParams.search && <span> matching &quot;{searchParams.search}&quot;</span>}
          {searchParams.status && searchParams.status !== "all" && (
            <span> with status &quot;{searchParams.status}&quot;</span>
          )}
        </p>
      )}

      {/* Sponsors Table */}
      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">
            {hasFilters ? "Search Results" : "All Sponsors"}
          </CardTitle>
          <CardDescription>
            {sponsors.length} {hasFilters ? "matching" : "total"} sponsors
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error ? (
            <div className="text-fire text-sm p-4 bg-fire/10 rounded-lg">
              Error loading sponsors: {error}
            </div>
          ) : (
            <SponsorsTable sponsors={sponsors} onEdit={handleEditSponsor} />
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Dialog */}
      <SponsorDialog
        sponsor={editingSponsor}
        open={dialogOpen}
        onOpenChange={setDialogOpen}
      />
    </>
  )
}
