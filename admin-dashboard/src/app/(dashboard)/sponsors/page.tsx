import { createClient } from "@/lib/supabase/server"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Building2, CheckCircle, Clock, DollarSign, Coins } from "lucide-react"
import { SponsorsPageClient } from "./sponsors-client"

interface SponsorsPageProps {
  searchParams: Promise<{ search?: string; status?: string }>
}

export default async function SponsorsPage({ searchParams }: SponsorsPageProps) {
  const params = await searchParams
  const supabase = await createClient()

  // Build query with optional filters
  let query = supabase.from("sponsors").select("*")

  // Apply search filter
  if (params.search) {
    query = query.ilike("company_name", `%${params.search}%`)
  }

  // Apply status filter
  if (params.status && params.status !== "all") {
    query = query.eq("status", params.status)
  }

  // Execute query
  const { data: sponsors, error } = await query.order("created_at", { ascending: false })

  // Get all sponsors for stats (unfiltered)
  const { data: allSponsors } = await supabase
    .from("sponsors")
    .select("status, total_spent, coins_purchased")

  // Calculate stats
  type SponsorRow = { status?: string; total_spent?: number; coins_purchased?: number }
  const totalSponsors = allSponsors?.length || 0
  const activeSponsors = allSponsors?.filter((s: SponsorRow) => s.status === 'active').length || 0
  const pendingSponsors = allSponsors?.filter((s: SponsorRow) => s.status === 'pending').length || 0
  const totalRevenue = allSponsors?.reduce((sum: number, s: SponsorRow) => sum + (s.total_spent || 0), 0) || 0
  const totalCoinsPurchased = allSponsors?.reduce((sum: number, s: SponsorRow) => sum + (s.coins_purchased || 0), 0) || 0

  const hasFilters = params.search || params.status

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-saddle-dark">Sponsor Management</h2>
          <p className="text-leather-light">
            Manage advertising sponsors and partnerships
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-5">
        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Total Sponsors
            </CardTitle>
            <Building2 className="h-4 w-4 text-brass" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{totalSponsors}</div>
            <p className="text-xs text-leather-light">All partnerships</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Active
            </CardTitle>
            <CheckCircle className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{activeSponsors}</div>
            <p className="text-xs text-leather-light">Currently active</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Pending
            </CardTitle>
            <Clock className="h-4 w-4 text-yellow-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">{pendingSponsors}</div>
            <p className="text-xs text-leather-light">Awaiting approval</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Total Revenue
            </CardTitle>
            <DollarSign className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              ${totalRevenue.toFixed(2)}
            </div>
            <p className="text-xs text-leather-light">From sponsors</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Coins Purchased
            </CardTitle>
            <Coins className="h-4 w-4 text-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-gold-dark">{totalCoinsPurchased}</div>
            <p className="text-xs text-leather-light">Branded coins</p>
          </CardContent>
        </Card>
      </div>

      {/* Client Component handles search and table */}
      <SponsorsPageClient 
        sponsors={sponsors || []}
        error={error?.message}
        hasFilters={!!hasFilters}
        totalSponsors={totalSponsors}
        searchParams={params}
      />
    </div>
  )
}
