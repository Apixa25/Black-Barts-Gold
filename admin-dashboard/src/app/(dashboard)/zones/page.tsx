/**
 * Zones Page - Server Component
 * 
 * @file admin-dashboard/src/app/(dashboard)/zones/page.tsx
 * @description Zone management page with map-based visualization and controls
 * 
 * Character count: ~2,800
 */

import { createClient } from "@/lib/supabase/server"
import { redirect } from "next/navigation"
import { ZonesPageClient } from "./zones-client"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { MapPinned, Users, Trophy, Coins } from "lucide-react"
import type { Zone } from "@/types/database"

// Force dynamic rendering - this page needs real data from Supabase
export const dynamic = 'force-dynamic'

// Mock zones for development (until database table is created)
const mockZones: Zone[] = [
  {
    id: "zone-1",
    name: "Downtown SF",
    description: "Main downtown area with high foot traffic",
    zone_type: "player",
    status: "active",
    geometry: {
      type: "circle",
      center: { latitude: 37.7749, longitude: -122.4194 },
      radius_meters: 1609, // 1 mile
    },
    owner_id: null,
    sponsor_id: null,
    auto_spawn_config: {
      enabled: true,
      min_coins: 3,
      max_coins: 10,
      coin_type: "pool",
      min_value: 0.10,
      max_value: 5.00,
      tier_weights: { gold: 10, silver: 30, bronze: 60 },
      respawn_delay_seconds: 60,
    },
    timed_release_config: null,
    hunt_config: {
      hunt_type: "direct_navigation",
      show_distance: true,
      enable_compass: true,
      map_marker_type: "exact",
      vibration_mode: "all",
      multi_find_enabled: false,
    },
    start_time: null,
    end_time: null,
    coins_placed: 5,
    coins_collected: 12,
    total_value_distributed: 45.50,
    active_players: 3,
    fill_color: null,
    border_color: null,
    opacity: 0.3,
    metadata: null,
    created_at: new Date().toISOString(),
    updated_at: new Date().toISOString(),
  },
  {
    id: "zone-2",
    name: "Golden Gate Park",
    description: "Park area with nature trails",
    zone_type: "hunt",
    status: "active",
    geometry: {
      type: "circle",
      center: { latitude: 37.7694, longitude: -122.4862 },
      radius_meters: 2500,
    },
    owner_id: null,
    sponsor_id: null,
    auto_spawn_config: null,
    timed_release_config: {
      enabled: true,
      total_coins: 50,
      release_interval_seconds: 120,
      coins_per_release: 5,
      start_time: new Date().toISOString(),
    },
    hunt_config: {
      hunt_type: "timed_release",
      show_distance: true,
      enable_compass: true,
      map_marker_type: "zone_only",
      vibration_mode: "last_100m",
      multi_find_enabled: true,
      max_finders: 3,
    },
    start_time: new Date().toISOString(),
    end_time: null,
    coins_placed: 25,
    coins_collected: 18,
    total_value_distributed: 125.00,
    active_players: 8,
    fill_color: null,
    border_color: null,
    opacity: 0.3,
    metadata: null,
    created_at: new Date().toISOString(),
    updated_at: new Date().toISOString(),
  },
]

export default async function ZonesPage() {
  const supabase = await createClient()
  
  // Get authenticated user
  const { data: { user }, error: authError } = await supabase.auth.getUser()
  
  if (authError || !user) {
    redirect("/login")
  }

  // Get user profile (use "profiles" table - same as layout.tsx)
  const { data: profile } = await supabase
    .from("profiles")
    .select("*")
    .eq("id", user.id)
    .single()

  // Allow super_admin and sponsor_admin to access zones
  // (redirect only if no profile or if role is just "user")
  if (!profile || profile.role === "user") {
    redirect("/")
  }

  // TODO: Fetch zones from database when table is created
  // const { data: zones, error: zonesError } = await supabase
  //   .from("zones")
  //   .select("*")
  //   .order("created_at", { ascending: false })

  const zones = mockZones

  // Calculate stats
  const stats = {
    total_zones: zones.length,
    active_zones: zones.filter(z => z.status === "active").length,
    player_zones: zones.filter(z => z.zone_type === "player").length,
    sponsor_zones: zones.filter(z => z.zone_type === "sponsor").length,
    hunt_zones: zones.filter(z => z.zone_type === "hunt").length,
    total_coins_in_zones: zones.reduce((sum, z) => sum + z.coins_placed, 0),
    total_active_players: zones.reduce((sum, z) => sum + z.active_players, 0),
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark">Zone Management</h2>
        <p className="text-leather-light">Create, visualize, and manage coin distribution zones</p>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-5">
        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather">Total Zones</CardTitle>
            <MapPinned className="h-4 w-4 text-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{stats.total_zones}</div>
            <p className="text-xs text-leather-light">{stats.active_zones} active</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather">Player Zones</CardTitle>
            <Users className="h-4 w-4 text-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{stats.player_zones}</div>
            <p className="text-xs text-leather-light">Auto-generated</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather">Hunt Zones</CardTitle>
            <Trophy className="h-4 w-4 text-fire" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{stats.hunt_zones}</div>
            <p className="text-xs text-leather-light">Special events</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather">Active Players</CardTitle>
            <Users className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{stats.total_active_players}</div>
            <p className="text-xs text-leather-light">In all zones</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather">Coins in Zones</CardTitle>
            <Coins className="h-4 w-4 text-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{stats.total_coins_in_zones}</div>
            <p className="text-xs text-leather-light">Total placed</p>
          </CardContent>
        </Card>
      </div>

      {/* Main Content */}
      <ZonesPageClient zones={zones} userId={user.id} />
    </div>
  )
}
