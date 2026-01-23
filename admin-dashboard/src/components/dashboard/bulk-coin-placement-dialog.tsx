/**
 * Bulk Coin Placement Dialog (M7)
 *
 * @file admin-dashboard/src/components/dashboard/bulk-coin-placement-dialog.tsx
 * @description Dialog for placing multiple coins at once in sponsor zones
 */

"use client"

import { useState } from "react"
import type { Sponsor, Zone, BulkCoinPlacementConfig } from "@/types/database"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { Badge } from "@/components/ui/badge"
import { Coins, MapPin, DollarSign, Loader2 } from "lucide-react"
import {
  DEFAULT_BULK_PLACEMENT_CONFIG,
  DISTRIBUTION_STRATEGY_PRESETS,
  calculateBulkPlacementCost,
  DEFAULT_SPONSOR_ZONE_FEES,
  validateBulkPlacementConfig,
} from "@/components/maps/sponsor-config"

interface BulkCoinPlacementDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  sponsor: Sponsor
  zones: Zone[]
  onPlace: (config: BulkCoinPlacementConfig) => Promise<void>
}

export function BulkCoinPlacementDialog({
  open,
  onOpenChange,
  sponsor,
  zones,
  onPlace,
}: BulkCoinPlacementDialogProps) {
  const [loading, setLoading] = useState(false)
  const [form, setForm] = useState<BulkCoinPlacementConfig>({
    sponsor_id: sponsor.id,
    zone_id: zones.length > 0 ? zones[0].id : null,
    coin_count: DEFAULT_BULK_PLACEMENT_CONFIG.coin_count,
    distribution_strategy: DEFAULT_BULK_PLACEMENT_CONFIG.distribution_strategy,
    value_range: { ...DEFAULT_BULK_PLACEMENT_CONFIG.value_range },
    tier_distribution: { ...DEFAULT_BULK_PLACEMENT_CONFIG.tier_distribution },
    release_all_at_once: DEFAULT_BULK_PLACEMENT_CONFIG.release_all_at_once,
    scheduled_release_time: null,
    min_distance_between_coins_meters: DEFAULT_BULK_PLACEMENT_CONFIG.min_distance_between_coins_meters,
    avoid_existing_coins: DEFAULT_BULK_PLACEMENT_CONFIG.avoid_existing_coins,
  })

  const validation = validateBulkPlacementConfig(form)
  const totalCost = calculateBulkPlacementCost(form, DEFAULT_SPONSOR_ZONE_FEES)

  const handleSubmit = async () => {
    if (!validation.valid) {
      return
    }

    setLoading(true)
    try {
      await onPlace(form)
      onOpenChange(false)
      // Reset form
      setForm({
        sponsor_id: sponsor.id,
        zone_id: zones.length > 0 ? zones[0].id : null,
        coin_count: DEFAULT_BULK_PLACEMENT_CONFIG.coin_count,
        distribution_strategy: DEFAULT_BULK_PLACEMENT_CONFIG.distribution_strategy,
        value_range: { ...DEFAULT_BULK_PLACEMENT_CONFIG.value_range },
        tier_distribution: { ...DEFAULT_BULK_PLACEMENT_CONFIG.tier_distribution },
        release_all_at_once: DEFAULT_BULK_PLACEMENT_CONFIG.release_all_at_once,
        scheduled_release_time: null,
        min_distance_between_coins_meters: DEFAULT_BULK_PLACEMENT_CONFIG.min_distance_between_coins_meters,
        avoid_existing_coins: DEFAULT_BULK_PLACEMENT_CONFIG.avoid_existing_coins,
      })
    } catch (error) {
      console.error('Failed to place coins:', error)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Coins className="h-5 w-5 text-gold" />
            Bulk Coin Placement
          </DialogTitle>
          <DialogDescription>
            Place multiple coins at once for {sponsor.company_name}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {/* Zone Selection */}
          <div className="space-y-2">
            <Label>Target Zone</Label>
            <Select
              value={form.zone_id || ""}
              onValueChange={(value) => setForm({ ...form, zone_id: value || null })}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select a zone..." />
              </SelectTrigger>
              <SelectContent>
                {zones.length === 0 ? (
                  <SelectItem value="" disabled>No zones available</SelectItem>
                ) : (
                  zones.map((zone) => (
                    <SelectItem key={zone.id} value={zone.id}>
                      <div className="flex items-center gap-2">
                        <MapPin className="h-3 w-3" />
                        {zone.name}
                      </div>
                    </SelectItem>
                  ))
                )}
              </SelectContent>
            </Select>
            {zones.length === 0 && (
              <p className="text-xs text-leather-light">
                Create a sponsor zone first before placing coins
              </p>
            )}
          </div>

          {/* Coin Count */}
          <div className="space-y-2">
            <Label>Number of Coins</Label>
            <Input
              type="number"
              min={1}
              max={1000}
              value={form.coin_count}
              onChange={(e) => setForm({ ...form, coin_count: parseInt(e.target.value) || 1 })}
            />
            <p className="text-xs text-leather-light">
              Place between 1 and 1000 coins at once
            </p>
          </div>

          {/* Distribution Strategy */}
          <div className="space-y-2">
            <Label>Distribution Strategy</Label>
            <Select
              value={form.distribution_strategy}
              onValueChange={(value: BulkCoinPlacementConfig['distribution_strategy']) =>
                setForm({ ...form, distribution_strategy: value })
              }
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(DISTRIBUTION_STRATEGY_PRESETS).map(([key, preset]) => (
                  <SelectItem key={key} value={key}>
                    <div className="flex items-center gap-2">
                      <span>{preset.icon}</span>
                      <div>
                        <div className="font-medium">{preset.label}</div>
                        <div className="text-xs text-leather-light">{preset.description}</div>
                      </div>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Value Range */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Min Value ($)</Label>
              <Input
                type="number"
                step="0.01"
                min="0"
                value={form.value_range.min}
                onChange={(e) =>
                  setForm({
                    ...form,
                    value_range: { ...form.value_range, min: parseFloat(e.target.value) || 0 },
                  })
                }
              />
            </div>
            <div className="space-y-2">
              <Label>Max Value ($)</Label>
              <Input
                type="number"
                step="0.01"
                min="0"
                value={form.value_range.max}
                onChange={(e) =>
                  setForm({
                    ...form,
                    value_range: { ...form.value_range, max: parseFloat(e.target.value) || 0 },
                  })
                }
              />
            </div>
          </div>

          {/* Tier Distribution */}
          <div className="space-y-2">
            <Label>Tier Distribution (%)</Label>
            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label className="text-xs">Gold</Label>
                <Input
                  type="number"
                  min="0"
                  max="100"
                  value={form.tier_distribution.gold}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      tier_distribution: {
                        ...form.tier_distribution,
                        gold: parseInt(e.target.value) || 0,
                      },
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label className="text-xs">Silver</Label>
                <Input
                  type="number"
                  min="0"
                  max="100"
                  value={form.tier_distribution.silver}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      tier_distribution: {
                        ...form.tier_distribution,
                        silver: parseInt(e.target.value) || 0,
                      },
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label className="text-xs">Bronze</Label>
                <Input
                  type="number"
                  min="0"
                  max="100"
                  value={form.tier_distribution.bronze}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      tier_distribution: {
                        ...form.tier_distribution,
                        bronze: parseInt(e.target.value) || 0,
                      },
                    })
                  }
                />
              </div>
            </div>
            <p className="text-xs text-leather-light">
              Total: {form.tier_distribution.gold + form.tier_distribution.silver + form.tier_distribution.bronze}%
            </p>
          </div>

          {/* Advanced Options */}
          <div className="space-y-4 border-t pt-4">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Release All at Once</Label>
                <p className="text-xs text-leather-light">
                  Place all coins immediately
                </p>
              </div>
              <Switch
                checked={form.release_all_at_once}
                onCheckedChange={(checked) =>
                  setForm({ ...form, release_all_at_once: checked })
                }
              />
            </div>

            <div className="space-y-2">
              <Label>Minimum Distance Between Coins (meters)</Label>
              <Input
                type="number"
                min="1"
                value={form.min_distance_between_coins_meters}
                onChange={(e) =>
                  setForm({
                    ...form,
                    min_distance_between_coins_meters: parseInt(e.target.value) || 1,
                  })
                }
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Avoid Existing Coins</Label>
                <p className="text-xs text-leather-light">
                  Don't place coins too close to existing ones
                </p>
              </div>
              <Switch
                checked={form.avoid_existing_coins}
                onCheckedChange={(checked) =>
                  setForm({ ...form, avoid_existing_coins: checked })
                }
              />
            </div>
          </div>

          {/* Cost Summary */}
          <div className="border-t pt-4">
            <div className="flex items-center justify-between p-4 bg-parchment/50 rounded-lg">
              <div className="flex items-center gap-2">
                <DollarSign className="h-5 w-5 text-green-600" />
                <span className="font-medium text-leather">Total Cost</span>
              </div>
              <div className="text-2xl font-bold text-green-600">
                ${totalCost.toFixed(2)}
              </div>
            </div>
            {form.coin_count >= 50 && (
              <p className="text-xs text-leather-light mt-2 text-center">
                🎉 Bulk discount applied! ({DEFAULT_SPONSOR_ZONE_FEES.bulk_placement_discount_percentage}% off)
              </p>
            )}
          </div>

          {/* Validation Errors */}
          {!validation.valid && (
            <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm font-medium text-red-800 mb-2">Please fix the following errors:</p>
              <ul className="list-disc list-inside text-sm text-red-700 space-y-1">
                {validation.errors.map((error, i) => (
                  <li key={i}>{error}</li>
                ))}
              </ul>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={loading}>
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!validation.valid || loading || zones.length === 0}
          >
            {loading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Placing Coins...
              </>
            ) : (
              <>
                <Coins className="mr-2 h-4 w-4" />
                Place {form.coin_count} Coins
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
