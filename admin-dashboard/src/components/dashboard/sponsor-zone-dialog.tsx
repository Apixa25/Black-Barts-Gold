/**
 * Sponsor Zone Dialog (M7)
 *
 * @file admin-dashboard/src/components/dashboard/sponsor-zone-dialog.tsx
 * @description Dialog for creating sponsor zones
 */

"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { createClient } from "@/lib/supabase/client"
import type { Sponsor, ZoneGeometry } from "@/types/database"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Building2, MapPin, Loader2, Info } from "lucide-react"
import { toast } from "sonner"
import { DEFAULT_SPONSOR_ZONE_FEES } from "@/components/maps/sponsor-config"

interface SponsorZoneDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  sponsor: Sponsor
  onCreated?: () => void
}

export function SponsorZoneDialog({
  open,
  onOpenChange,
  sponsor,
  onCreated,
}: SponsorZoneDialogProps) {
  const router = useRouter()
  const supabase = createClient()
  const [loading, setLoading] = useState(false)
  const [form, setForm] = useState({
    name: "",
    description: "",
    geometry_type: "circle" as "circle" | "polygon",
    center_lat: "",
    center_lng: "",
    radius_meters: "500", // Default 500m radius
  })

  const handleSubmit = async () => {
    if (!form.name.trim()) {
      toast.error("Zone name is required")
      return
    }

    if (form.geometry_type === "circle") {
      if (!form.center_lat || !form.center_lng || !form.radius_meters) {
        toast.error("Circle requires center coordinates and radius")
        return
      }
    }

    setLoading(true)
    try {
      // Build geometry based on type
      let geometry: ZoneGeometry
      if (form.geometry_type === "circle") {
        geometry = {
          type: "circle",
          center: {
            latitude: parseFloat(form.center_lat),
            longitude: parseFloat(form.center_lng),
          },
          radius_meters: parseFloat(form.radius_meters),
        }
      } else {
        // For polygon, we'd need to get coordinates from map drawing
        // For now, create a simple square around the center
        const lat = parseFloat(form.center_lat)
        const lng = parseFloat(form.center_lng)
        const radius = parseFloat(form.radius_meters) / 111000 // Convert to degrees (rough approximation)
        
        geometry = {
          type: "polygon",
          polygon: [
            { latitude: lat - radius, longitude: lng - radius },
            { latitude: lat - radius, longitude: lng + radius },
            { latitude: lat + radius, longitude: lng + radius },
            { latitude: lat + radius, longitude: lng - radius },
            { latitude: lat - radius, longitude: lng - radius }, // Close polygon
          ],
        }
      }

      // Create zone
      const { data, error } = await supabase
        .from("zones")
        .insert({
          name: form.name,
          description: form.description || null,
          zone_type: "sponsor",
          status: "active",
          sponsor_id: sponsor.id,
          geometry: geometry,
          auto_spawn_config: null, // Sponsor zones use manual/bulk placement
          coins_placed: 0,
          coins_collected: 0,
          total_value_distributed: 0,
          active_players: 0,
          fill_color: "#B87333", // Brass color for sponsor zones
          border_color: "#B87333",
          opacity: 0.2,
        })
        .select()
        .single()

      if (error) throw error

      toast.success(`Sponsor zone "${form.name}" created! 🎉`)
      onOpenChange(false)
      
      // Reset form
      setForm({
        name: "",
        description: "",
        geometry_type: "circle",
        center_lat: "",
        center_lng: "",
        radius_meters: "500",
      })

      router.refresh()
      onCreated?.()
    } catch (error) {
      console.error("Failed to create sponsor zone:", error)
      toast.error(
        error instanceof Error
          ? error.message
          : "Failed to create sponsor zone"
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Building2 className="h-5 w-5 text-brass" />
            Create Sponsor Zone
          </DialogTitle>
          <DialogDescription>
            Create a new zone for {sponsor.company_name}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {/* Zone Name */}
          <div className="space-y-2">
            <Label>Zone Name *</Label>
            <Input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder="e.g., Downtown Storefront"
            />
          </div>

          {/* Description */}
          <div className="space-y-2">
            <Label>Description</Label>
            <Textarea
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              placeholder="Optional description of the zone location..."
              rows={3}
            />
          </div>

          {/* Geometry Type */}
          <div className="space-y-2">
            <Label>Zone Shape</Label>
            <Select
              value={form.geometry_type}
              onValueChange={(value: "circle" | "polygon") =>
                setForm({ ...form, geometry_type: value })
              }
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="circle">
                  <div className="flex items-center gap-2">
                    <MapPin className="h-4 w-4" />
                    Circle (Center + Radius)
                  </div>
                </SelectItem>
                <SelectItem value="polygon">
                  <div className="flex items-center gap-2">
                    <MapPin className="h-4 w-4" />
                    Polygon (Custom Shape)
                  </div>
                </SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Circle Geometry */}
          {form.geometry_type === "circle" && (
            <div className="space-y-4 border-t pt-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Center Latitude *</Label>
                  <Input
                    type="number"
                    step="any"
                    value={form.center_lat}
                    onChange={(e) => setForm({ ...form, center_lat: e.target.value })}
                    placeholder="37.7749"
                  />
                </div>
                <div className="space-y-2">
                  <Label>Center Longitude *</Label>
                  <Input
                    type="number"
                    step="any"
                    value={form.center_lng}
                    onChange={(e) => setForm({ ...form, center_lng: e.target.value })}
                    placeholder="-122.4194"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label>Radius (meters) *</Label>
                <Input
                  type="number"
                  min="100"
                  value={form.radius_meters}
                  onChange={(e) => setForm({ ...form, radius_meters: e.target.value })}
                  placeholder="500"
                />
                <p className="text-xs text-leather-light">
                  Minimum: 100m (0.1 km² area)
                </p>
              </div>
            </div>
          )}

          {/* Polygon Geometry */}
          {form.geometry_type === "polygon" && (
            <div className="space-y-4 border-t pt-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Center Latitude</Label>
                  <Input
                    type="number"
                    step="any"
                    value={form.center_lat}
                    onChange={(e) => setForm({ ...form, center_lat: e.target.value })}
                    placeholder="37.7749"
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
                  />
                </div>
              </div>
              <div className="p-4 bg-parchment/50 rounded-lg">
                <div className="flex items-start gap-2">
                  <Info className="h-4 w-4 text-blue-600 mt-0.5" />
                  <div className="text-sm text-leather-light">
                    <p className="font-medium mb-1">Polygon zones require map drawing</p>
                    <p>
                      For now, a square zone will be created around the center point.
                      Full polygon drawing will be available in the map view.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Fee Information */}
          <div className="border-t pt-4">
            <div className="p-4 bg-parchment/50 rounded-lg">
              <div className="flex items-start gap-2 mb-2">
                <Info className="h-4 w-4 text-brass mt-0.5" />
                <div className="flex-1">
                  <p className="text-sm font-medium text-leather mb-1">
                    Zone Creation Fee
                  </p>
                  <p className="text-xs text-leather-light">
                    One-time fee: ${DEFAULT_SPONSOR_ZONE_FEES.zone_creation_fee.toFixed(2)}
                  </p>
                  <p className="text-xs text-leather-light mt-1">
                    Monthly maintenance: ${DEFAULT_SPONSOR_ZONE_FEES.monthly_maintenance_fee.toFixed(2)}/month
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={loading}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={loading || !form.name.trim()}>
            {loading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Creating...
              </>
            ) : (
              <>
                <MapPin className="mr-2 h-4 w-4" />
                Create Zone
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
