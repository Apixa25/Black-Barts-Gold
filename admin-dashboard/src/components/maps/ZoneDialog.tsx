/**
 * Zone Dialog Component for Creating/Editing Zones
 * 
 * @file admin-dashboard/src/components/maps/ZoneDialog.tsx
 * @description Comprehensive dialog for zone configuration with tabs
 * 
 * Character count: ~12,500
 */

"use client"

import { useState, useEffect } from "react"
import type { 
  Zone, 
  ZoneType, 
  ZoneGeometry, 
  ZoneAutoSpawnConfig,
  ZoneTimedReleaseConfig,
  ZoneHuntConfig,
  HuntType,
  CoinType
} from "@/types/database"
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
import { Textarea } from "@/components/ui/textarea"
import { Switch } from "@/components/ui/switch"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Slider } from "@/components/ui/slider"
import { 
  ZONE_TYPE_COLORS, 
  HUNT_TYPE_CONFIG,
  DEFAULT_ZONE_RADIUS,
  formatRadius,
  ZONE_RADIUS_LIMITS
} from "./zone-config"
import { 
  MapPin, 
  Circle, 
  Hexagon, 
  Users, 
  Building2, 
  Trophy, 
  Grid3X3,
  Coins,
  Clock,
  Palette
} from "lucide-react"
import { toast } from "sonner"

interface ZoneDialogProps {
  zone?: Zone | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (zoneData: Partial<Zone>) => Promise<void>
  /** Pre-set geometry from map drawing */
  initialGeometry?: ZoneGeometry | null
  userId: string
}

const zoneTypeOptions: { value: ZoneType; label: string; icon: typeof Circle; description: string }[] = [
  { value: "player", label: "Player Zone", icon: Users, description: "Auto-generated around players" },
  { value: "sponsor", label: "Sponsor Zone", icon: Building2, description: "Custom sponsor location" },
  { value: "hunt", label: "Hunt Zone", icon: Trophy, description: "Special treasure hunt" },
  { value: "grid", label: "Grid Zone", icon: Grid3X3, description: "System distribution grid" },
]

const huntTypeOptions = Object.entries(HUNT_TYPE_CONFIG).map(([key, config]) => ({
  value: key as HuntType,
  ...config,
}))

export function ZoneDialog({
  zone,
  open,
  onOpenChange,
  onSave,
  initialGeometry,
  userId,
}: ZoneDialogProps) {
  const isEditing = !!zone
  const [isSaving, setIsSaving] = useState(false)
  const [activeTab, setActiveTab] = useState("basic")

  // Form state
  const [form, setForm] = useState({
    name: "",
    description: "",
    zone_type: "player" as ZoneType,
    geometry_type: "circle" as "circle" | "polygon",
    center_lat: "",
    center_lng: "",
    radius: DEFAULT_ZONE_RADIUS,
    // Auto-spawn settings
    auto_spawn_enabled: true,
    min_coins: 3,
    max_coins: 10,
    coin_type: "pool" as CoinType,
    min_value: 0.10,
    max_value: 5.00,
    respawn_delay: 60,
    // Timed release settings
    timed_release_enabled: false,
    total_coins: 100,
    release_interval: 60,
    coins_per_release: 1,
    start_time: "",
    end_time: "",
    // Hunt config
    hunt_type: "direct_navigation" as HuntType,
    show_distance: true,
    enable_compass: true,
    vibration_mode: "all" as "all" | "last_100m" | "off",
    // Visual
    fill_color: ZONE_TYPE_COLORS.player.fill,
    border_color: ZONE_TYPE_COLORS.player.border,
    opacity: 0.3,
  })

  // Reset form when dialog opens or zone changes
  useEffect(() => {
    if (open && zone) {
      // Editing existing zone
      setForm({
        name: zone.name,
        description: zone.description || "",
        zone_type: zone.zone_type,
        geometry_type: zone.geometry.type,
        center_lat: zone.geometry.center?.latitude.toString() || "",
        center_lng: zone.geometry.center?.longitude.toString() || "",
        radius: zone.geometry.radius_meters || DEFAULT_ZONE_RADIUS,
        auto_spawn_enabled: zone.auto_spawn_config?.enabled ?? true,
        min_coins: zone.auto_spawn_config?.min_coins ?? 3,
        max_coins: zone.auto_spawn_config?.max_coins ?? 10,
        coin_type: zone.auto_spawn_config?.coin_type ?? "pool",
        min_value: zone.auto_spawn_config?.min_value ?? 0.10,
        max_value: zone.auto_spawn_config?.max_value ?? 5.00,
        respawn_delay: zone.auto_spawn_config?.respawn_delay_seconds ?? 60,
        timed_release_enabled: zone.timed_release_config?.enabled ?? false,
        total_coins: zone.timed_release_config?.total_coins ?? 100,
        release_interval: zone.timed_release_config?.release_interval_seconds ?? 60,
        coins_per_release: zone.timed_release_config?.coins_per_release ?? 1,
        start_time: zone.timed_release_config?.start_time || "",
        end_time: zone.timed_release_config?.end_time || "",
        hunt_type: zone.hunt_config?.hunt_type ?? "direct_navigation",
        show_distance: zone.hunt_config?.show_distance ?? true,
        enable_compass: zone.hunt_config?.enable_compass ?? true,
        vibration_mode: zone.hunt_config?.vibration_mode ?? "all",
        fill_color: zone.fill_color || ZONE_TYPE_COLORS[zone.zone_type].fill,
        border_color: zone.border_color || ZONE_TYPE_COLORS[zone.zone_type].border,
        opacity: zone.opacity ?? 0.3,
      })
    } else if (open && !zone) {
      // Creating new zone - use initial geometry if provided
      const defaultColors = ZONE_TYPE_COLORS.player
      setForm({
        name: "",
        description: "",
        zone_type: "player",
        geometry_type: initialGeometry?.type || "circle",
        center_lat: initialGeometry?.center?.latitude.toString() || "",
        center_lng: initialGeometry?.center?.longitude.toString() || "",
        radius: initialGeometry?.radius_meters || DEFAULT_ZONE_RADIUS,
        auto_spawn_enabled: true,
        min_coins: 3,
        max_coins: 10,
        coin_type: "pool",
        min_value: 0.10,
        max_value: 5.00,
        respawn_delay: 60,
        timed_release_enabled: false,
        total_coins: 100,
        release_interval: 60,
        coins_per_release: 1,
        start_time: "",
        end_time: "",
        hunt_type: "direct_navigation",
        show_distance: true,
        enable_compass: true,
        vibration_mode: "all",
        fill_color: defaultColors.fill,
        border_color: defaultColors.border,
        opacity: 0.3,
      })
    }
  }, [open, zone, initialGeometry])

  // Update colors when zone type changes
  useEffect(() => {
    const colors = ZONE_TYPE_COLORS[form.zone_type]
    setForm(prev => ({
      ...prev,
      fill_color: colors.fill,
      border_color: colors.border,
    }))
  }, [form.zone_type])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSaving(true)

    // Validate
    if (!form.name.trim()) {
      toast.error("Please enter a zone name")
      setIsSaving(false)
      return
    }

    if (form.geometry_type === 'circle' && (!form.center_lat || !form.center_lng)) {
      toast.error("Please enter zone center coordinates")
      setIsSaving(false)
      return
    }

    // Build geometry
    const geometry: ZoneGeometry = {
      type: form.geometry_type,
      center: form.geometry_type === 'circle' ? {
        latitude: parseFloat(form.center_lat),
        longitude: parseFloat(form.center_lng),
      } : undefined,
      radius_meters: form.geometry_type === 'circle' ? form.radius : undefined,
      polygon: initialGeometry?.polygon,
    }

    // Build auto-spawn config
    const autoSpawnConfig: ZoneAutoSpawnConfig | null = form.auto_spawn_enabled ? {
      enabled: true,
      min_coins: form.min_coins,
      max_coins: form.max_coins,
      coin_type: form.coin_type,
      min_value: form.min_value,
      max_value: form.max_value,
      tier_weights: { gold: 10, silver: 30, bronze: 60 },
      respawn_delay_seconds: form.respawn_delay,
    } : null

    // Build timed release config
    const timedReleaseConfig: ZoneTimedReleaseConfig | null = form.timed_release_enabled ? {
      enabled: true,
      total_coins: form.total_coins,
      release_interval_seconds: form.release_interval,
      coins_per_release: form.coins_per_release,
      start_time: form.start_time || new Date().toISOString(),
      end_time: form.end_time || undefined,
    } : null

    // Build hunt config
    const huntConfig: ZoneHuntConfig = {
      hunt_type: form.hunt_type,
      show_distance: form.show_distance,
      enable_compass: form.enable_compass,
      map_marker_type: 'exact',
      vibration_mode: form.vibration_mode,
      multi_find_enabled: false,
    }

    const zoneData: Partial<Zone> = {
      name: form.name.trim(),
      description: form.description.trim() || null,
      zone_type: form.zone_type,
      geometry,
      owner_id: userId,
      auto_spawn_config: autoSpawnConfig,
      timed_release_config: timedReleaseConfig,
      hunt_config: huntConfig,
      fill_color: form.fill_color,
      border_color: form.border_color,
      opacity: form.opacity,
      status: 'active',
    }

    try {
      await onSave(zoneData)
      toast.success(`Zone ${isEditing ? 'updated' : 'created'}! 🗺️`, {
        description: form.name,
      })
      onOpenChange(false)
    } catch (error) {
      toast.error(`Failed to ${isEditing ? 'update' : 'create'} zone`, {
        description: error instanceof Error ? error.message : 'Unknown error',
      })
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-saddle-dark flex items-center gap-2">
            <MapPin className="h-5 w-5 text-gold" />
            {isEditing ? "Edit Zone" : "Create New Zone"}
          </DialogTitle>
          <DialogDescription>
            {isEditing ? "Update zone configuration" : "Define a new zone for coin distribution"}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
            <TabsList className="grid w-full grid-cols-4 mb-4">
              <TabsTrigger value="basic" className="text-xs sm:text-sm">
                <MapPin className="h-4 w-4 mr-1 hidden sm:inline" />
                Basic
              </TabsTrigger>
              <TabsTrigger value="coins" className="text-xs sm:text-sm">
                <Coins className="h-4 w-4 mr-1 hidden sm:inline" />
                Coins
              </TabsTrigger>
              <TabsTrigger value="timing" className="text-xs sm:text-sm">
                <Clock className="h-4 w-4 mr-1 hidden sm:inline" />
                Timing
              </TabsTrigger>
              <TabsTrigger value="style" className="text-xs sm:text-sm">
                <Palette className="h-4 w-4 mr-1 hidden sm:inline" />
                Style
              </TabsTrigger>
            </TabsList>

            {/* Basic Tab */}
            <TabsContent value="basic" className="space-y-4">
              {/* Zone Name */}
              <div className="space-y-2">
                <Label htmlFor="name">Zone Name *</Label>
                <Input
                  id="name"
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                  placeholder="e.g., Downtown SF, Mall Zone"
                  className="border-saddle-light/30"
                  required
                />
              </div>

              {/* Zone Type */}
              <div className="space-y-2">
                <Label>Zone Type</Label>
                <div className="grid grid-cols-2 gap-2">
                  {zoneTypeOptions.map((option) => {
                    const Icon = option.icon
                    const isSelected = form.zone_type === option.value
                    const colors = ZONE_TYPE_COLORS[option.value]
                    return (
                      <button
                        key={option.value}
                        type="button"
                        onClick={() => setForm({ ...form, zone_type: option.value })}
                        className={`flex items-center gap-2 p-3 rounded-lg border-2 transition-colors text-left ${
                          isSelected
                            ? "border-gold bg-gold/10"
                            : "border-saddle-light/30 hover:border-saddle-light"
                        }`}
                      >
                        <div 
                          className="w-8 h-8 rounded-full flex items-center justify-center"
                          style={{ backgroundColor: colors.fill, borderColor: colors.border, borderWidth: 2 }}
                        >
                          <Icon className="h-4 w-4" style={{ color: colors.border }} />
                        </div>
                        <div>
                          <p className="font-medium text-sm text-saddle-dark">{option.label}</p>
                          <p className="text-xs text-leather-light">{option.description}</p>
                        </div>
                      </button>
                    )
                  })}
                </div>
              </div>

              {/* Geometry Type */}
              <div className="space-y-2">
                <Label>Shape</Label>
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => setForm({ ...form, geometry_type: 'circle' })}
                    className={`flex-1 flex items-center justify-center gap-2 p-3 rounded-lg border-2 transition-colors ${
                      form.geometry_type === 'circle'
                        ? "border-gold bg-gold/10"
                        : "border-saddle-light/30 hover:border-saddle-light"
                    }`}
                  >
                    <Circle className="h-5 w-5" />
                    <span>Circle</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => setForm({ ...form, geometry_type: 'polygon' })}
                    className={`flex-1 flex items-center justify-center gap-2 p-3 rounded-lg border-2 transition-colors ${
                      form.geometry_type === 'polygon'
                        ? "border-gold bg-gold/10"
                        : "border-saddle-light/30 hover:border-saddle-light"
                    }`}
                  >
                    <Hexagon className="h-5 w-5" />
                    <span>Polygon</span>
                  </button>
                </div>
              </div>

              {/* Circle coordinates */}
              {form.geometry_type === 'circle' && (
                <>
                  <div className="grid grid-cols-2 gap-2">
                    <div className="space-y-2">
                      <Label>Center Latitude</Label>
                      <Input
                        type="number"
                        step="any"
                        value={form.center_lat}
                        onChange={(e) => setForm({ ...form, center_lat: e.target.value })}
                        placeholder="37.7749"
                        className={`border-saddle-light/30 ${
                          initialGeometry?.center ? "bg-green-50 border-green-300" : ""
                        }`}
                        required
                      />
                    </div>
                    <div className="space-y-2">
                      <Label>Center Longitude</Label>
                      <Input
                        type="number"
                        step="any"
                        value={form.center_lng}
                        onChange={(e) => setForm({ ...form, center_lng: e.target.value })}
                        placeholder="-122.4194"
                        className={`border-saddle-light/30 ${
                          initialGeometry?.center ? "bg-green-50 border-green-300" : ""
                        }`}
                        required
                      />
                    </div>
                  </div>

                  {/* Radius slider */}
                  <div className="space-y-2">
                    <div className="flex justify-between">
                      <Label>Radius</Label>
                      <span className="text-sm text-leather-light">
                        {formatRadius(form.radius)}
                      </span>
                    </div>
                    <Slider
                      value={[form.radius]}
                      onValueChange={([value]) => setForm({ ...form, radius: value })}
                      min={ZONE_RADIUS_LIMITS.min}
                      max={ZONE_RADIUS_LIMITS.max}
                      step={50}
                      className="py-4"
                    />
                    <div className="flex justify-between text-xs text-leather-light">
                      <span>{formatRadius(ZONE_RADIUS_LIMITS.min)}</span>
                      <span>1 mile = 1609m</span>
                      <span>{formatRadius(ZONE_RADIUS_LIMITS.max)}</span>
                    </div>
                  </div>
                </>
              )}

              {/* Description */}
              <div className="space-y-2">
                <Label htmlFor="description">Description (optional)</Label>
                <Textarea
                  id="description"
                  value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })}
                  placeholder="Add notes about this zone..."
                  className="border-saddle-light/30 resize-none"
                  rows={2}
                />
              </div>
            </TabsContent>

            {/* Coins Tab */}
            <TabsContent value="coins" className="space-y-4">
              {/* Auto-spawn toggle */}
              <div className="flex items-center justify-between p-3 bg-parchment rounded-lg">
                <div>
                  <Label htmlFor="autospawn">Auto-Spawn Coins</Label>
                  <p className="text-xs text-leather-light">
                    Automatically maintain coins in this zone
                  </p>
                </div>
                <Switch
                  id="autospawn"
                  checked={form.auto_spawn_enabled}
                  onCheckedChange={(checked) => setForm({ ...form, auto_spawn_enabled: checked })}
                />
              </div>

              {form.auto_spawn_enabled && (
                <>
                  {/* Min/Max coins */}
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label>Min Coins</Label>
                      <Input
                        type="number"
                        min={1}
                        max={100}
                        value={form.min_coins}
                        onChange={(e) => setForm({ ...form, min_coins: parseInt(e.target.value) || 1 })}
                        className="border-saddle-light/30"
                      />
                      <p className="text-xs text-leather-light">Always maintain at least this many</p>
                    </div>
                    <div className="space-y-2">
                      <Label>Max Coins</Label>
                      <Input
                        type="number"
                        min={1}
                        max={500}
                        value={form.max_coins}
                        onChange={(e) => setForm({ ...form, max_coins: parseInt(e.target.value) || 10 })}
                        className="border-saddle-light/30"
                      />
                      <p className="text-xs text-leather-light">Never exceed this amount</p>
                    </div>
                  </div>

                  {/* Value range */}
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label>Min Value ($)</Label>
                      <Input
                        type="number"
                        step="0.01"
                        min={0.01}
                        value={form.min_value}
                        onChange={(e) => setForm({ ...form, min_value: parseFloat(e.target.value) || 0.10 })}
                        className="border-saddle-light/30"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label>Max Value ($)</Label>
                      <Input
                        type="number"
                        step="0.01"
                        min={0.01}
                        value={form.max_value}
                        onChange={(e) => setForm({ ...form, max_value: parseFloat(e.target.value) || 5.00 })}
                        className="border-saddle-light/30"
                      />
                    </div>
                  </div>

                  {/* Respawn delay */}
                  <div className="space-y-2">
                    <Label>Respawn Delay (seconds)</Label>
                    <Input
                      type="number"
                      min={10}
                      max={3600}
                      value={form.respawn_delay}
                      onChange={(e) => setForm({ ...form, respawn_delay: parseInt(e.target.value) || 60 })}
                      className="border-saddle-light/30"
                    />
                    <p className="text-xs text-leather-light">
                      Time before respawning after a coin is collected
                    </p>
                  </div>
                </>
              )}
            </TabsContent>

            {/* Timing Tab */}
            <TabsContent value="timing" className="space-y-4">
              {/* Timed release toggle */}
              <div className="flex items-center justify-between p-3 bg-parchment rounded-lg">
                <div>
                  <Label htmlFor="timedrelease">Timed Release Hunt</Label>
                  <p className="text-xs text-leather-light">
                    Release coins gradually over time
                  </p>
                </div>
                <Switch
                  id="timedrelease"
                  checked={form.timed_release_enabled}
                  onCheckedChange={(checked) => setForm({ ...form, timed_release_enabled: checked })}
                />
              </div>

              {form.timed_release_enabled && (
                <>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label>Total Coins</Label>
                      <Input
                        type="number"
                        min={1}
                        value={form.total_coins}
                        onChange={(e) => setForm({ ...form, total_coins: parseInt(e.target.value) || 100 })}
                        className="border-saddle-light/30"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label>Coins Per Release</Label>
                      <Input
                        type="number"
                        min={1}
                        value={form.coins_per_release}
                        onChange={(e) => setForm({ ...form, coins_per_release: parseInt(e.target.value) || 1 })}
                        className="border-saddle-light/30"
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label>Release Interval (seconds)</Label>
                    <Input
                      type="number"
                      min={10}
                      value={form.release_interval}
                      onChange={(e) => setForm({ ...form, release_interval: parseInt(e.target.value) || 60 })}
                      className="border-saddle-light/30"
                    />
                    <p className="text-xs text-leather-light">
                      Example: 100 coins, 1 per release, 60 sec interval = ~100 minutes total
                    </p>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label>Start Time</Label>
                      <Input
                        type="datetime-local"
                        value={form.start_time}
                        onChange={(e) => setForm({ ...form, start_time: e.target.value })}
                        className="border-saddle-light/30"
                      />
                    </div>
                    <div className="space-y-2">
                      <Label>End Time (optional)</Label>
                      <Input
                        type="datetime-local"
                        value={form.end_time}
                        onChange={(e) => setForm({ ...form, end_time: e.target.value })}
                        className="border-saddle-light/30"
                      />
                    </div>
                  </div>
                </>
              )}

              {/* Hunt Type */}
              <div className="space-y-2">
                <Label>Hunt Type</Label>
                <div className="grid grid-cols-2 gap-2 max-h-48 overflow-y-auto">
                  {huntTypeOptions.map((option) => {
                    const isSelected = form.hunt_type === option.value
                    return (
                      <button
                        key={option.value}
                        type="button"
                        onClick={() => setForm({ ...form, hunt_type: option.value })}
                        className={`flex items-start gap-2 p-2 rounded-lg border-2 transition-colors text-left ${
                          isSelected
                            ? "border-gold bg-gold/10"
                            : "border-saddle-light/30 hover:border-saddle-light"
                        }`}
                      >
                        <span className="text-lg">{option.emoji}</span>
                        <div>
                          <p className="font-medium text-xs text-saddle-dark">{option.label}</p>
                          <p className="text-xs text-leather-light leading-tight">{option.description}</p>
                        </div>
                      </button>
                    )
                  })}
                </div>
              </div>
            </TabsContent>

            {/* Style Tab */}
            <TabsContent value="style" className="space-y-4">
              <div className="space-y-2">
                <Label>Zone Opacity</Label>
                <Slider
                  value={[form.opacity * 100]}
                  onValueChange={([value]) => setForm({ ...form, opacity: value / 100 })}
                  min={10}
                  max={80}
                  step={5}
                  className="py-4"
                />
                <div className="flex justify-between text-xs text-leather-light">
                  <span>Transparent</span>
                  <span>{Math.round(form.opacity * 100)}%</span>
                  <span>Opaque</span>
                </div>
              </div>

              {/* Preview */}
              <div className="p-4 bg-parchment rounded-lg">
                <Label className="mb-2 block">Preview</Label>
                <div 
                  className="w-full h-24 rounded-lg border-2 border-dashed"
                  style={{
                    backgroundColor: form.fill_color,
                    borderColor: form.border_color,
                    opacity: form.opacity + 0.3,
                  }}
                />
              </div>
            </TabsContent>
          </Tabs>

          <DialogFooter className="mt-6">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              className="border-saddle-light/30"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSaving}
              className="bg-gold hover:bg-gold-dark text-leather"
            >
              {isSaving ? "Saving..." : isEditing ? "Save Changes" : "Create Zone"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
