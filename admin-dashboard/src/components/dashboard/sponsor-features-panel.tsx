/**
 * Sponsor Features Panel (M7)
 *
 * @file admin-dashboard/src/components/dashboard/sponsor-features-panel.tsx
 * @description Sponsor zone management, analytics, and bulk coin placement
 */

"use client"

import { useState, useEffect } from "react"
import { useSponsorAnalytics } from "@/hooks/use-sponsor-analytics"
import type { Zone, Sponsor } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Building2,
  TrendingUp,
  TrendingDown,
  Coins,
  Users,
  DollarSign,
  MapPin,
  Plus,
  RefreshCw,
  Loader2,
  BarChart3,
  Target,
  Sparkles,
} from "lucide-react"
import { toast } from "sonner"
import { formatPerformanceScore, calculateROI } from "@/components/maps/sponsor-config"
import { BulkCoinPlacementDialog } from "./bulk-coin-placement-dialog"
import { SponsorZoneDialog } from "./sponsor-zone-dialog"

interface SponsorFeaturesPanelProps {
  className?: string
  zones?: Zone[]
}

export function SponsorFeaturesPanel({ className = "", zones = [] }: SponsorFeaturesPanelProps) {
  const {
    sponsors,
    analytics,
    loading,
    error,
    fetchAnalytics,
    refresh,
    placeBulkCoins,
  } = useSponsorAnalytics()

  const [selectedSponsorId, setSelectedSponsorId] = useState<string>("")
  const [bulkPlacementOpen, setBulkPlacementOpen] = useState(false)
  const [createZoneOpen, setCreateZoneOpen] = useState(false)

  // Auto-select first sponsor on load
  useEffect(() => {
    if (sponsors.length > 0 && !selectedSponsorId) {
      const firstActive = sponsors.find(s => s.status === 'active') || sponsors[0]
      if (firstActive) {
        setSelectedSponsorId(firstActive.id)
        fetchAnalytics(firstActive.id)
      }
    }
  }, [sponsors, selectedSponsorId, fetchAnalytics])

  // Handle sponsor selection
  const handleSponsorChange = (sponsorId: string) => {
    setSelectedSponsorId(sponsorId)
    if (sponsorId) {
      fetchAnalytics(sponsorId)
    }
  }

  // Get selected sponsor
  const selectedSponsor = sponsors.find(s => s.id === selectedSponsorId)

  // Get sponsor zones
  const sponsorZones = zones.filter(z => z.sponsor_id === selectedSponsorId)

  if (error) {
    return (
      <Card className={className}>
        <CardContent className="pt-6">
          <div className="text-center text-red-600">
            <p>Error loading sponsor data: {error}</p>
            <Button onClick={refresh} variant="outline" className="mt-4">
              <RefreshCw className="mr-2 h-4 w-4" />
              Retry
            </Button>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className={className}>
      {/* Header */}
      <Card className="border-saddle-light/30">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2 text-saddle-dark">
                <Building2 className="h-5 w-5 text-brass" />
                Sponsor Features
              </CardTitle>
              <CardDescription>
                Manage sponsor zones, view analytics, and place bulk coins
              </CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={refresh}
                disabled={loading}
              >
                <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
              </Button>
              <Button
                size="sm"
                onClick={() => setCreateZoneOpen(true)}
                disabled={!selectedSponsorId}
              >
                <Plus className="mr-2 h-4 w-4" />
                Create Zone
              </Button>
              <Button
                size="sm"
                onClick={() => setBulkPlacementOpen(true)}
                disabled={!selectedSponsorId || sponsorZones.length === 0}
              >
                <Coins className="mr-2 h-4 w-4" />
                Bulk Place Coins
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {/* Sponsor Selector */}
          <div className="mb-6">
            <label className="text-sm font-medium text-leather mb-2 block">
              Select Sponsor
            </label>
            <Select
              value={selectedSponsorId}
              onValueChange={handleSponsorChange}
              disabled={loading}
            >
              <SelectTrigger className="w-full max-w-md">
                <SelectValue placeholder="Choose a sponsor..." />
              </SelectTrigger>
              <SelectContent>
                {sponsors.map((sponsor) => (
                  <SelectItem key={sponsor.id} value={sponsor.id}>
                    <div className="flex items-center gap-2">
                      <span>{sponsor.company_name}</span>
                      <Badge
                        variant={
                          sponsor.status === 'active' ? 'default' : 
                          sponsor.status === 'pending' ? 'secondary' : 
                          'outline'
                        }
                        className="text-xs"
                      >
                        {sponsor.status}
                      </Badge>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Loading State */}
          {loading && !analytics && (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-brass" />
            </div>
          )}

          {/* Analytics Dashboard */}
          {analytics && selectedSponsor && (
            <>
              {/* Stats Cards */}
              <div className="grid gap-4 md:grid-cols-4 mb-6">
                <Card className="border-saddle-light/30">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-sm font-medium text-leather-light">
                      Collection Rate
                    </CardTitle>
                    <Target className="h-4 w-4 text-gold" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-saddle-dark">
                      {analytics.collection_rate.toFixed(1)}%
                    </div>
                    <p className="text-xs text-leather-light">
                      {analytics.total_coins_collected} / {analytics.total_coins_placed} coins
                    </p>
                  </CardContent>
                </Card>

                <Card className="border-saddle-light/30">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-sm font-medium text-leather-light">
                      ROI
                    </CardTitle>
                    {analytics.roi_percentage >= 0 ? (
                      <TrendingUp className="h-4 w-4 text-green-600" />
                    ) : (
                      <TrendingDown className="h-4 w-4 text-red-600" />
                    )}
                  </CardHeader>
                  <CardContent>
                    <div className={`text-2xl font-bold ${
                      analytics.roi_percentage >= 0 ? 'text-green-600' : 'text-red-600'
                    }`}>
                      {analytics.roi_percentage >= 0 ? '+' : ''}
                      {analytics.roi_percentage.toFixed(1)}%
                    </div>
                    <p className="text-xs text-leather-light">
                      Return on investment
                    </p>
                  </CardContent>
                </Card>

                <Card className="border-saddle-light/30">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-sm font-medium text-leather-light">
                      Unique Collectors
                    </CardTitle>
                    <Users className="h-4 w-4 text-blue-600" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-saddle-dark">
                      {analytics.total_unique_collectors}
                    </div>
                    <p className="text-xs text-leather-light">
                      ${(analytics.cost_per_unique_collector).toFixed(2)} per collector
                    </p>
                  </CardContent>
                </Card>

                <Card className="border-saddle-light/30">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-sm font-medium text-leather-light">
                      Total Value Collected
                    </CardTitle>
                    <DollarSign className="h-4 w-4 text-green-600" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-green-600">
                      ${analytics.total_value_collected.toFixed(2)}
                    </div>
                    <p className="text-xs text-leather-light">
                      From ${analytics.total_value_placed.toFixed(2)} placed
                    </p>
                  </CardContent>
                </Card>
              </div>

              {/* Zone Analytics Table */}
              <Card className="border-saddle-light/30">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-saddle-dark">
                    <BarChart3 className="h-5 w-5 text-brass" />
                    Zone Performance
                  </CardTitle>
                  <CardDescription>
                    Performance metrics for each sponsor zone
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  {analytics.zone_analytics.length === 0 ? (
                    <div className="text-center py-8 text-leather-light">
                      <MapPin className="mx-auto h-12 w-12 text-saddle-light/50 mb-4" />
                      <p className="text-lg font-medium">No zones yet</p>
                      <p className="text-sm">Create your first sponsor zone to get started!</p>
                    </div>
                  ) : (
                    <Table>
                      <TableHeader>
                        <TableRow className="hover:bg-transparent">
                          <TableHead className="text-leather">Zone</TableHead>
                          <TableHead className="text-leather">Coins</TableHead>
                          <TableHead className="text-leather">Value</TableHead>
                          <TableHead className="text-leather">Collectors</TableHead>
                          <TableHead className="text-leather">Performance</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {analytics.zone_analytics.map((zone) => {
                          const perf = formatPerformanceScore(zone.performance_score)
                          return (
                            <TableRow key={zone.zone_id} className="hover:bg-parchment/50">
                              <TableCell>
                                <div className="font-medium text-saddle-dark">
                                  {zone.zone_name}
                                </div>
                              </TableCell>
                              <TableCell>
                                <div className="flex items-center gap-2">
                                  <span className="text-sm text-leather">
                                    {zone.coins_collected} / {zone.total_coins_placed}
                                  </span>
                                  <Badge
                                    variant={
                                      zone.coins_active > 0 ? 'default' : 'secondary'
                                    }
                                    className="text-xs"
                                  >
                                    {zone.coins_active} active
                                  </Badge>
                                </div>
                              </TableCell>
                              <TableCell>
                                <div className="text-sm">
                                  <div className="font-medium text-green-600">
                                    ${zone.total_value_collected.toFixed(2)}
                                  </div>
                                  <div className="text-xs text-leather-light">
                                    of ${zone.total_value_placed.toFixed(2)}
                                  </div>
                                </div>
                              </TableCell>
                              <TableCell>
                                <div className="flex items-center gap-1">
                                  <Users className="h-3 w-3 text-blue-600" />
                                  <span className="text-sm text-leather">
                                    {zone.unique_collectors}
                                  </span>
                                </div>
                              </TableCell>
                              <TableCell>
                                <Badge
                                  className={`${perf.color} border-0`}
                                  variant="outline"
                                >
                                  <Sparkles className="mr-1 h-3 w-3" />
                                  {perf.label} ({zone.performance_score})
                                </Badge>
                              </TableCell>
                            </TableRow>
                          )
                        })}
                      </TableBody>
                    </Table>
                  )}
                </CardContent>
              </Card>
            </>
          )}

          {/* No Sponsor Selected */}
          {!selectedSponsorId && !loading && (
            <div className="text-center py-12 text-leather-light">
              <Building2 className="mx-auto h-12 w-12 text-saddle-light/50 mb-4" />
              <p className="text-lg font-medium">Select a sponsor</p>
              <p className="text-sm">Choose a sponsor from the dropdown to view analytics</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Dialogs */}
      {selectedSponsor && (
        <>
          <BulkCoinPlacementDialog
            open={bulkPlacementOpen}
            onOpenChange={setBulkPlacementOpen}
            sponsor={selectedSponsor}
            zones={sponsorZones}
            onPlace={async (config) => {
              const result = await placeBulkCoins(config)
              if (result.success) {
                toast.success(result.message)
                setBulkPlacementOpen(false)
                refresh()
              } else {
                toast.error(result.message)
              }
            }}
          />
          <SponsorZoneDialog
            open={createZoneOpen}
            onOpenChange={setCreateZoneOpen}
            sponsor={selectedSponsor}
            onCreated={() => {
              toast.success("Sponsor zone created!")
              setCreateZoneOpen(false)
              refresh()
            }}
          />
        </>
      )}
    </div>
  )
}
