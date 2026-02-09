"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { createClient } from "@/lib/supabase/client"
import type { Coin, CoinType, CoinTier, CoinStatus, CoinModel } from "@/types/database"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Coins, MapPin, Sparkles, Target, Dices, Trash2 } from "lucide-react"
import { toast } from "sonner"

interface CoinDialogProps {
  coin?: Coin | null
  open: boolean
  onOpenChange: (open: boolean) => void
  userId: string
  /** Pre-filled coordinates from map click */
  initialCoordinates?: { lat: number; lng: number } | null
  /** Called when user deletes the coin (only when editing) */
  onDelete?: (coin: Coin) => void
}

const tierOptions: { value: CoinTier; label: string; emoji: string }[] = [
  { value: "gold", label: "Gold", emoji: "🥇" },
  { value: "silver", label: "Silver", emoji: "🥈" },
  { value: "bronze", label: "Bronze", emoji: "🥉" },
]

const typeOptions: { value: CoinType; label: string; icon: typeof Target; description: string }[] = [
  { value: "fixed", label: "Fixed Value", icon: Target, description: "Exact amount shown to finders" },
  { value: "pool", label: "Pool/Mystery", icon: Dices, description: "Value determined at collection" },
]

const findTypeOptions: { value: "one_time" | "multi"; label: string; description: string }[] = [
  { value: "one_time", label: "One-time find", description: "One user finds it → coin is removed from the map" },
  { value: "multi", label: "Multi-find", description: "Up to N users can find it, then it’s removed" },
]

const coinModelOptions: { value: CoinModel; label: string }[] = [
  { value: "color_bb", label: "Color BB (default)" },
  { value: "bb_gold", label: "Black Bart's Gold" },
  { value: "prize_race", label: "Prize Race" },
]

export function CoinDialog({ coin, open, onOpenChange, userId, initialCoordinates, onDelete }: CoinDialogProps) {
  const router = useRouter()
  const supabase = createClient()
  const [isSaving, setIsSaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  
  const isEditing = !!coin
  
  const [form, setForm] = useState({
    coin_type: "fixed" as CoinType,
    coin_model: "color_bb" as CoinModel,
    value: "",
    tier: "gold" as CoinTier,
    latitude: "",
    longitude: "",
    location_name: "",
    description: "",
    is_mythical: false,
    multi_find: false,
    finds_remaining: "1",
  })

  // Reset form when dialog opens/closes or coin changes
  useEffect(() => {
    if (open && coin) {
      // Editing existing coin
      setForm({
        coin_type: coin.coin_type,
        coin_model: coin.coin_model ?? "color_bb",
        value: coin.value.toString(),
        tier: coin.tier,
        latitude: coin.latitude.toString(),
        longitude: coin.longitude.toString(),
        location_name: coin.location_name || "",
        description: coin.description || "",
        is_mythical: coin.is_mythical,
        multi_find: coin.multi_find,
        finds_remaining: coin.finds_remaining.toString(),
      })
    } else if (open && !coin) {
      // Creating new coin - use initialCoordinates if provided
      setForm({
        coin_type: "fixed",
        coin_model: "color_bb",
        value: "",
        tier: "gold",
        latitude: initialCoordinates?.lat?.toString() || "",
        longitude: initialCoordinates?.lng?.toString() || "",
        location_name: "",
        description: "",
        is_mythical: false,
        multi_find: false,
        finds_remaining: "1",
      })
    }
  }, [open, coin, initialCoordinates])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSaving(true)

    const coinData = {
      coin_type: form.coin_type,
      coin_model: form.coin_model,
      value: parseFloat(form.value) || 0,
      tier: form.tier,
      latitude: parseFloat(form.latitude),
      longitude: parseFloat(form.longitude),
      location_name: form.location_name || null,
      description: form.description || null,
      is_mythical: form.is_mythical,
      multi_find: form.multi_find,
      finds_remaining: form.multi_find ? parseInt(form.finds_remaining) || 3 : 1,
      hider_id: userId,
      status: "hidden" as CoinStatus,
    }

    // Validate required fields
    if (!form.value || isNaN(parseFloat(form.value))) {
      toast.error("Please enter a valid value")
      setIsSaving(false)
      return
    }
    if (!form.latitude || !form.longitude) {
      toast.error("Please enter valid coordinates")
      setIsSaving(false)
      return
    }

    let error
    if (isEditing && coin) {
      const { error: updateError } = await supabase
        .from("coins")
        .update(coinData)
        .eq("id", coin.id)
      error = updateError
    } else {
      const { error: insertError } = await supabase
        .from("coins")
        .insert(coinData)
      error = insertError
    }

    setIsSaving(false)

    if (error) {
      toast.error(`Failed to ${isEditing ? "update" : "create"} coin`, {
        description: error.message,
      })
      return
    }

    toast.success(`Coin ${isEditing ? "updated" : "created"}! 🪙`, {
      description: `$${coinData.value.toFixed(2)} ${coinData.tier} coin`,
    })
    onOpenChange(false)
    router.refresh()
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-saddle-dark flex items-center gap-2">
            <Coins className="h-5 w-5 text-gold" />
            {isEditing ? "Edit Coin" : "Create New Coin"}
          </DialogTitle>
          <DialogDescription>
            {isEditing ? "Update coin details" : "Hide a new coin for treasure hunters to find"}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-4">
          {/* Coin Type */}
          <div className="space-y-2">
            <Label>Coin Type</Label>
            <div className="grid grid-cols-2 gap-2">
              {typeOptions.map((option) => {
                const Icon = option.icon
                const isSelected = form.coin_type === option.value
                return (
                  <button
                    key={option.value}
                    type="button"
                    onClick={() => setForm({ ...form, coin_type: option.value })}
                    className={`flex items-center gap-2 p-3 rounded-lg border-2 transition-colors text-left ${
                      isSelected
                        ? "border-gold bg-gold/10"
                        : "border-saddle-light/30 hover:border-saddle-light"
                    }`}
                  >
                    <Icon className={`h-5 w-5 ${isSelected ? "text-gold" : "text-saddle"}`} />
                    <div>
                      <p className="font-medium text-sm text-saddle-dark">{option.label}</p>
                      <p className="text-xs text-leather-light">{option.description}</p>
                    </div>
                  </button>
                )
              })}
            </div>
          </div>

          {/* Coin Model (AR appearance) */}
          <div className="space-y-2">
            <Label>Coin model (AR appearance)</Label>
            <Select
              value={form.coin_model}
              onValueChange={(value) => setForm({ ...form, coin_model: value as CoinModel })}
            >
              <SelectTrigger className="w-full border-saddle-light/30">
                <SelectValue placeholder="Choose coin graphic" />
              </SelectTrigger>
              <SelectContent>
                {coinModelOptions.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-leather-light">
              Which 3D coin graphic players see when they find this coin in AR
            </p>
          </div>

          {/* Value, Tier & Find Type */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="value">Value ($)</Label>
              <Input
                id="value"
                type="number"
                step="0.01"
                min="0"
                value={form.value}
                onChange={(e) => setForm({ ...form, value: e.target.value })}
                placeholder="10.00"
                className="border-saddle-light/30"
                required
              />
            </div>
            <div className="space-y-2">
              <Label>Tier</Label>
              <div className="flex gap-1">
                {tierOptions.map((option) => {
                  const isSelected = form.tier === option.value
                  return (
                    <button
                      key={option.value}
                      type="button"
                      onClick={() => setForm({ ...form, tier: option.value })}
                      className={`flex-1 p-2 rounded-lg border-2 transition-colors text-center ${
                        isSelected
                          ? "border-gold bg-gold/10"
                          : "border-saddle-light/30 hover:border-saddle-light"
                      }`}
                    >
                      <span className="text-lg">{option.emoji}</span>
                    </button>
                  )
                })}
              </div>
            </div>
          </div>

          {/* How is this coin found? (one-time vs multi-find, then removal) */}
          <div className="space-y-2">
            <Label>How is this coin found?</Label>
            <p className="text-xs text-leather-light mb-2">
              One-time: one finder, then removed. Multi-find: up to N finders, then removed from the database.
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
              {findTypeOptions.map((option) => {
                const isSelected =
                  (option.value === "one_time" && !form.multi_find) ||
                  (option.value === "multi" && form.multi_find)
                return (
                  <button
                    key={option.value}
                    type="button"
                    onClick={() =>
                      setForm({
                        ...form,
                        multi_find: option.value === "multi",
                        finds_remaining:
                          option.value === "multi"
                            ? (form.multi_find ? form.finds_remaining : "3")
                            : "1",
                      })
                    }
                    className={`flex flex-col items-start p-3 rounded-lg border-2 transition-colors text-left ${
                      isSelected
                        ? "border-gold bg-gold/10"
                        : "border-saddle-light/30 hover:border-saddle-light"
                    }`}
                  >
                    <span className="font-medium text-sm text-saddle-dark">{option.label}</span>
                    <span className="text-xs text-leather-light mt-0.5">{option.description}</span>
                  </button>
                )
              })}
            </div>
            {form.multi_find && (
              <div className="space-y-2 pl-2 pt-2 border-l-2 border-gold/30">
                <Label htmlFor="finds">Max number of finders</Label>
                <Input
                  id="finds"
                  type="number"
                  min="2"
                  max="20"
                  value={form.finds_remaining}
                  onChange={(e) => setForm({ ...form, finds_remaining: e.target.value })}
                  className="w-24 border-saddle-light/30"
                />
                <p className="text-xs text-leather-light">
                  After this many users find the coin, it is removed from the database.
                </p>
              </div>
            )}
          </div>

          {/* Location */}
          <div className="space-y-2">
            <Label className="flex items-center gap-1">
              <MapPin className="h-4 w-4" />
              Location
              {initialCoordinates && !coin && (
                <span className="ml-2 text-xs text-green-600 font-normal">
                  ✓ Set from map
                </span>
              )}
            </Label>
            <div className="grid grid-cols-2 gap-2">
              <Input
                type="number"
                step="any"
                value={form.latitude}
                onChange={(e) => setForm({ ...form, latitude: e.target.value })}
                placeholder="Latitude (e.g., 40.7128)"
                className={`border-saddle-light/30 ${
                  initialCoordinates && !coin ? "bg-green-50 border-green-300" : ""
                }`}
                required
              />
              <Input
                type="number"
                step="any"
                value={form.longitude}
                onChange={(e) => setForm({ ...form, longitude: e.target.value })}
                placeholder="Longitude (e.g., -74.0060)"
                className={`border-saddle-light/30 ${
                  initialCoordinates && !coin ? "bg-green-50 border-green-300" : ""
                }`}
                required
              />
            </div>
            <Input
              value={form.location_name}
              onChange={(e) => setForm({ ...form, location_name: e.target.value })}
              placeholder="Location name (optional, e.g., Central Park)"
              className="border-saddle-light/30"
            />
          </div>

          {/* Description */}
          <div className="space-y-2">
            <Label htmlFor="description">Description (optional)</Label>
            <Textarea
              id="description"
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              placeholder="Add notes about this coin..."
              className="border-saddle-light/30 resize-none"
              rows={2}
            />
          </div>

          {/* Special Options */}
          <div className="space-y-3 pt-2 border-t border-saddle-light/20">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Sparkles className="h-4 w-4 text-gold" />
                <div>
                  <Label htmlFor="mythical" className="cursor-pointer">Mythical Coin</Label>
                  <p className="text-xs text-leather-light">High-value legendary coin</p>
                </div>
              </div>
              <Switch
                id="mythical"
                checked={form.is_mythical}
                onCheckedChange={(checked) => setForm({ ...form, is_mythical: checked })}
              />
            </div>

          </div>
        </form>

        <DialogFooter className="flex-wrap gap-2 sm:justify-between">
          <div className="flex gap-2 order-2 sm:order-1">
            {isEditing && coin && onDelete && (
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  if (!confirm("Delete this coin? It will be removed from the database. This cannot be undone.")) return
                  setIsDeleting(true)
                  onDelete(coin)
                  onOpenChange(false)
                  setIsDeleting(false)
                }}
                disabled={isSaving || isDeleting}
                className="border-fire/50 text-fire hover:bg-fire/10"
              >
                <Trash2 className="h-4 w-4 mr-1" />
                Delete coin
              </Button>
            )}
          </div>
          <div className="flex gap-2 order-1 sm:order-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              className="border-saddle-light/30"
            >
              Cancel
            </Button>
            <Button
              onClick={handleSubmit}
              disabled={isSaving || isDeleting}
              className="bg-gold hover:bg-gold-dark text-leather"
            >
              {isSaving ? "Saving..." : isEditing ? "Save Changes" : "Create Coin"}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
