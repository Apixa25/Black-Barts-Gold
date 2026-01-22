import { redirect } from "next/navigation"
import { createClient, isSupabaseConfigured } from "@/lib/supabase/server"
import { DashboardSidebar } from "@/components/layout/dashboard-sidebar"
import { DashboardHeader } from "@/components/layout/dashboard-header"
import type { UserProfile } from "@/types/database"

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const supabase = await createClient()
  const { data: { user } } = await supabase.auth.getUser()

  // In demo mode (no Supabase configured), show dashboard without auth
  const isDemoMode = !isSupabaseConfigured()
  
  if (!isDemoMode && !user) {
    redirect("/login")
  }

  // Get user profile with role (or use demo profile)
  let userProfile: UserProfile

  if (isDemoMode) {
    userProfile = {
      id: "demo-user",
      email: "demo@blackbartsgold.com",
      full_name: "Demo Partner",
      role: "super_admin",
      avatar_url: null,
      created_at: new Date().toISOString(),
      updated_at: new Date().toISOString(),
    }
  } else if (user) {
    const { data: profile } = await supabase
      .from("profiles")
      .select("*")
      .eq("id", user.id)
      .single()

    userProfile = profile || {
      id: user.id,
      email: user.email || "",
      full_name: user.user_metadata?.full_name || null,
      role: "user",
      avatar_url: null,
      created_at: new Date().toISOString(),
      updated_at: new Date().toISOString(),
    }
  } else {
    // This shouldn't happen, but fallback
    redirect("/login")
  }

  return (
    <div className="min-h-screen bg-parchment-light">
      {isDemoMode && (
        <div className="bg-gold text-leather text-center text-sm py-2 px-4">
          🤠 <strong>Demo Mode</strong> - Supabase not configured. 
          Add credentials to <code className="bg-gold-dark/20 px-1 rounded">.env.local</code> to enable authentication.
        </div>
      )}
      <DashboardSidebar user={userProfile} />
      <div className="lg:pl-64">
        <DashboardHeader user={userProfile} />
        <main className="p-6 pb-24">
          {children}
        </main>
      </div>
    </div>
  )
}
