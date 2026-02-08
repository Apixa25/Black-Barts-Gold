/**
 * Map Controls Component
 * 
 * @file admin-dashboard/src/components/maps/MapControls.tsx
 * @description Zoom controls, layer toggles, and map tools
 * 
 * Character count: ~4,200
 */

"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { 
  ZoomIn, 
  ZoomOut, 
  Maximize2, 
  Layers, 
  MapPin,
  Eye,
  EyeOff,
  Locate,
  Filter
} from "lucide-react"
import { MAP_STYLES, type MapStyleKey } from "./map-config"
import type { CoinStatus } from "@/types/database"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { Switch } from "@/components/ui/switch"
import { Label } from "@/components/ui/label"

interface MapControlsProps {
  onZoomIn: () => void
  onZoomOut: () => void
  onFitBounds: () => void
  onLocateUser?: () => void
  onStyleChange: (style: string) => void
  currentStyle: MapStyleKey
  visibleStatuses: CoinStatus[]
  onStatusFilterChange: (statuses: CoinStatus[]) => void
  showLabels?: boolean
  onToggleLabels?: () => void
  coinCount: number
}

const ALL_STATUSES: CoinStatus[] = ["visible", "hidden", "collected", "expired", "recycled"]

const STATUS_INFO: Record<CoinStatus, { label: string; emoji: string; color: string }> = {
  visible: { label: "Visible", emoji: "👁️", color: "#FFD700" },
  hidden: { label: "Hidden", emoji: "🙈", color: "#8B4513" },
  collected: { label: "Collected", emoji: "✅", color: "#22C55E" },
  expired: { label: "Expired", emoji: "⏰", color: "#6B7280" },
  recycled: { label: "Recycled", emoji: "♻️", color: "#9CA3AF" },
}

export function MapControls({
  onZoomIn,
  onZoomOut,
  onFitBounds,
  onLocateUser,
  onStyleChange,
  currentStyle,
  visibleStatuses,
  onStatusFilterChange,
  showLabels = true,
  onToggleLabels,
  coinCount,
}: MapControlsProps) {
  const [filterOpen, setFilterOpen] = useState(false)

  const toggleStatus = (status: CoinStatus) => {
    if (visibleStatuses.includes(status)) {
      onStatusFilterChange(visibleStatuses.filter(s => s !== status))
    } else {
      onStatusFilterChange([...visibleStatuses, status])
    }
  }

  const selectAll = () => onStatusFilterChange([...ALL_STATUSES])
  const selectNone = () => onStatusFilterChange([])

  return (
    <div className="absolute top-4 right-4 flex flex-col gap-2 z-10">
      {/* Zoom Controls */}
      <div className="bg-white rounded-lg shadow-lg border border-saddle-light/30 overflow-hidden">
        <Button
          variant="ghost"
          size="icon"
          onClick={onZoomIn}
          className="rounded-none border-b border-saddle-light/20 h-9 w-9"
          title="Zoom In"
        >
          <ZoomIn className="h-4 w-4" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          onClick={onZoomOut}
          className="rounded-none h-9 w-9"
          title="Zoom Out"
        >
          <ZoomOut className="h-4 w-4" />
        </Button>
      </div>

      {/* Fit to bounds */}
      <Button
        variant="outline"
        size="icon"
        onClick={onFitBounds}
        className="bg-white shadow-lg h-9 w-9"
        title="Fit all coins"
      >
        <Maximize2 className="h-4 w-4" />
      </Button>

      {/* Locate user (if supported) */}
      {onLocateUser && (
        <Button
          variant="outline"
          size="icon"
          onClick={onLocateUser}
          className="bg-white shadow-lg h-9 w-9"
          title="My location"
        >
          <Locate className="h-4 w-4" />
        </Button>
      )}

      {/* Layer/Style selector */}
      <Select value={currentStyle} onValueChange={(v) => onStyleChange(MAP_STYLES[v as MapStyleKey])}>
        <SelectTrigger className="w-9 h-9 bg-white shadow-lg p-0 justify-center [&>svg:last-child]:hidden" title="Map style">
          <Layers className="h-4 w-4" />
        </SelectTrigger>
        <SelectContent align="end">
          <SelectItem value="custom">🎯 High Contrast</SelectItem>
          <SelectItem value="streets">🗺️ Streets</SelectItem>
          <SelectItem value="satellite">🛰️ Satellite</SelectItem>
          <SelectItem value="outdoors">🏔️ Outdoors</SelectItem>
          <SelectItem value="light">☀️ Light</SelectItem>
          <SelectItem value="dark">🌙 Dark</SelectItem>
        </SelectContent>
      </Select>

      {/* Status Filter */}
      <Popover open={filterOpen} onOpenChange={setFilterOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            size="icon"
            className={`bg-white shadow-lg h-9 w-9 ${
              visibleStatuses.length < ALL_STATUSES.length ? "border-gold text-gold" : ""
            }`}
            title="Filter by status"
          >
            <Filter className="h-4 w-4" />
          </Button>
        </PopoverTrigger>
        <PopoverContent align="end" className="w-56">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h4 className="font-medium text-sm text-saddle-dark">Show Coins</h4>
              <div className="flex gap-1">
                <Button variant="ghost" size="sm" onClick={selectAll} className="h-6 text-xs px-2">
                  All
                </Button>
                <Button variant="ghost" size="sm" onClick={selectNone} className="h-6 text-xs px-2">
                  None
                </Button>
              </div>
            </div>
            <div className="space-y-2">
              {ALL_STATUSES.map((status) => (
                <div key={status} className="flex items-center justify-between">
                  <Label 
                    htmlFor={`status-${status}`} 
                    className="flex items-center gap-2 text-sm cursor-pointer"
                  >
                    <span
                      className="w-3 h-3 rounded-full"
                      style={{ backgroundColor: STATUS_INFO[status].color }}
                    />
                    <span>{STATUS_INFO[status].emoji}</span>
                    <span>{STATUS_INFO[status].label}</span>
                  </Label>
                  <Switch
                    id={`status-${status}`}
                    checked={visibleStatuses.includes(status)}
                    onCheckedChange={() => toggleStatus(status)}
                  />
                </div>
              ))}
            </div>
          </div>
        </PopoverContent>
      </Popover>

      {/* Coin count badge */}
      <div className="bg-gold text-leather text-xs font-bold px-2 py-1 rounded-lg shadow-lg text-center">
        {coinCount} 🪙
      </div>
    </div>
  )
}
