import { createClient } from "@/lib/supabase/server"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Coins, Eye, CheckCircle, DollarSign, Sparkles } from "lucide-react"
import { CoinsPageClient } from "./coins-client"

interface CoinsPageProps {
  searchParams: Promise<{ search?: string; status?: string; tier?: string }>
}

export default async function CoinsPage({ searchParams }: CoinsPageProps) {
  const params = await searchParams
  const supabase = await createClient()

  // Get current user
  const { data: { user } } = await supabase.auth.getUser()
  const userId = user?.id || ""

  // Build query with optional filters
  let query = supabase.from("coins").select("*")

  // Apply search filter (location name)
  if (params.search) {
    query = query.ilike("location_name", `%${params.search}%`)
  }

  // Apply status filter
  if (params.status && params.status !== "all") {
    query = query.eq("status", params.status)
  }

  // Apply tier filter
  if (params.tier && params.tier !== "all") {
    query = query.eq("tier", params.tier)
  }

  // Execute query
  const { data: coins, error } = await query.order("created_at", { ascending: false })

  // Get all coins for stats (unfiltered)
  const { data: allCoins } = await supabase.from("coins").select("status, value, tier, is_mythical")

  // Calculate stats
  type CoinRow = { status?: string; value?: number; is_mythical?: boolean }
  const totalCoins = allCoins?.length || 0
  const visibleCoins = allCoins?.filter((c: CoinRow) => c.status === 'visible').length || 0
  const collectedCoins = allCoins?.filter((c: CoinRow) => c.status === 'collected').length || 0
  const totalValue = allCoins?.reduce((sum: number, c: CoinRow) => sum + (c.value || 0), 0) || 0
  const mythicalCoins = allCoins?.filter((c: CoinRow) => c.is_mythical).length || 0

  const hasFilters = params.search || params.status || params.tier

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-saddle-dark">Coin Management</h2>
          <p className="text-leather-light">
            Create, track, and manage treasure coins
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-5">
        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Total Coins
            </CardTitle>
            <Coins className="h-4 w-4 text-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{totalCoins}</div>
            <p className="text-xs text-leather-light">In the system</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Visible
            </CardTitle>
            <Eye className="h-4 w-4 text-brass" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{visibleCoins}</div>
            <p className="text-xs text-leather-light">Available to find</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Collected
            </CardTitle>
            <CheckCircle className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{collectedCoins}</div>
            <p className="text-xs text-leather-light">Found by hunters</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Total Value
            </CardTitle>
            <DollarSign className="h-4 w-4 text-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">
              ${totalValue.toFixed(2)}
            </div>
            <p className="text-xs text-leather-light">In circulation</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Mythical
            </CardTitle>
            <Sparkles className="h-4 w-4 text-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{mythicalCoins}</div>
            <p className="text-xs text-leather-light">Legendary coins</p>
          </CardContent>
        </Card>
      </div>

      {/* Client Component handles search and table */}
      <CoinsPageClient 
        coins={coins || []} 
        userId={userId}
        error={error?.message}
        hasFilters={!!hasFilters}
        totalCoins={totalCoins}
        searchParams={params}
      />
    </div>
  )
}
