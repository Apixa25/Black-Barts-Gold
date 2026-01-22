import { createClient } from "@/lib/supabase/server"
import { redirect } from "next/navigation"
import { SettingsPageClient } from "./settings-client"

export default async function SettingsPage() {
  const supabase = await createClient()
  
  // Get current user
  const { data: { user } } = await supabase.auth.getUser()
  
  if (!user) {
    redirect("/login")
  }

  // Get user profile
  const { data: profile } = await supabase
    .from("profiles")
    .select("*")
    .eq("id", user.id)
    .single()

  // Get system stats for admin tools
  const { count: totalUsers } = await supabase
    .from("profiles")
    .select("*", { count: 'exact', head: true })

  const { count: totalCoins } = await supabase
    .from("coins")
    .select("*", { count: 'exact', head: true })

  const { count: totalTransactions } = await supabase
    .from("transactions")
    .select("*", { count: 'exact', head: true })

  const { count: totalSponsors } = await supabase
    .from("sponsors")
    .select("*", { count: 'exact', head: true })

  const { count: totalLogs } = await supabase
    .from("activity_logs")
    .select("*", { count: 'exact', head: true })

  const systemStats = {
    users: totalUsers || 0,
    coins: totalCoins || 0,
    transactions: totalTransactions || 0,
    sponsors: totalSponsors || 0,
    logs: totalLogs || 0,
  }

  return (
    <SettingsPageClient 
      user={user}
      profile={profile}
      systemStats={systemStats}
    />
  )
}
