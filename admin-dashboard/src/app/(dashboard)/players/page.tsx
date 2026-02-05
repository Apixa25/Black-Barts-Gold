/**
 * Players Page - Full-Screen Live Player Tracking
 *
 * @file admin-dashboard/src/app/(dashboard)/players/page.tsx
 * @description Full-view page for real-time player location tracking on the map
 */

import { createClient } from "@/lib/supabase/server"
import { redirect } from "next/navigation"
import { LivePlayersMap } from "@/components/dashboard/live-players-map"

// Force dynamic rendering - real-time data
export const dynamic = "force-dynamic"

export default async function PlayersPage() {
  const supabase = await createClient()

  const { data: { user }, error: authError } = await supabase.auth.getUser()

  if (authError || !user) {
    redirect("/login")
  }

  const { data: profile } = await supabase
    .from("profiles")
    .select("*")
    .eq("id", user.id)
    .single()

  // Require admin role (super_admin or sponsor_admin can view players)
  if (!profile || (profile.role !== "super_admin" && profile.role !== "sponsor_admin")) {
    redirect("/")
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark">Live Players</h2>
        <p className="text-leather-light">
          Real-time player locations on the map — full view
        </p>
      </div>

      {/* Full-screen map (taller than dashboard widget) */}
      <LivePlayersMap height={600} showFullScreenLink={false} />
    </div>
  )
}
