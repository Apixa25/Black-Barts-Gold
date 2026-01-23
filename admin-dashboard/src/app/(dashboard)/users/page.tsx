import { createClient } from "@/lib/supabase/server"
import { UsersTable } from "@/components/dashboard/users-table"
import { UsersSearch } from "@/components/dashboard/users-search"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Users, UserPlus, Shield } from "lucide-react"
import { Suspense } from "react"

interface UsersPageProps {
  searchParams: Promise<{ search?: string; role?: string }>
}

export default async function UsersPage({ searchParams }: UsersPageProps) {
  const params = await searchParams
  const supabase = await createClient()

  // Build query with optional filters
  let query = supabase.from("profiles").select("*")

  // Apply search filter
  if (params.search) {
    query = query.or(`email.ilike.%${params.search}%,full_name.ilike.%${params.search}%`)
  }

  // Apply role filter
  if (params.role && params.role !== "all") {
    query = query.eq("role", params.role)
  }

  // Execute query
  const { data: users, error } = await query.order("created_at", { ascending: false })

  // Get all users for stats (unfiltered)
  const { data: allUsers } = await supabase.from("profiles").select("role")

  // Count by role
  type UserRow = { role?: string }
  const superAdmins = allUsers?.filter((u: UserRow) => u.role === 'super_admin').length || 0
  const sponsorAdmins = allUsers?.filter((u: UserRow) => u.role === 'sponsor_admin').length || 0
  const regularUsers = allUsers?.filter((u: UserRow) => u.role === 'user').length || 0
  const totalUsers = allUsers?.length || 0

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-saddle-dark">User Management</h2>
          <p className="text-leather-light">
            Manage users, roles, and permissions
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Total Users
            </CardTitle>
            <Users className="h-4 w-4 text-saddle" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{totalUsers}</div>
            <p className="text-xs text-leather-light">
              Registered accounts
            </p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Admins
            </CardTitle>
            <Shield className="h-4 w-4 text-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{superAdmins + sponsorAdmins}</div>
            <p className="text-xs text-leather-light">
              {superAdmins} super, {sponsorAdmins} sponsor
            </p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Regular Users
            </CardTitle>
            <UserPlus className="h-4 w-4 text-brass" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{regularUsers}</div>
            <p className="text-xs text-leather-light">
              Standard accounts
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Search and Filter */}
      <Suspense fallback={<div className="h-10 bg-parchment animate-pulse rounded" />}>
        <UsersSearch />
      </Suspense>

      {/* Results info */}
      {(params.search || params.role) && (
        <p className="text-sm text-leather-light">
          Showing {users?.length || 0} of {totalUsers} users
          {params.search && <span> matching &quot;{params.search}&quot;</span>}
          {params.role && params.role !== "all" && <span> with role &quot;{params.role}&quot;</span>}
        </p>
      )}

      {/* Users Table */}
      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">
            {params.search || params.role ? "Search Results" : "All Users"}
          </CardTitle>
          <CardDescription>
            {users?.length || 0} {(params.search || params.role) ? "matching" : "total"} users
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error ? (
            <div className="text-fire text-sm p-4 bg-fire/10 rounded-lg">
              Error loading users: {error.message}
            </div>
          ) : (
            <UsersTable users={users || []} />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
