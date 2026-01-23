import { createClient } from "@/lib/supabase/server"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { 
  DollarSign, 
  TrendingUp, 
  TrendingDown, 
  Fuel, 
  ArrowDownCircle,
  ArrowUpCircle,
  Activity
} from "lucide-react"
import { FinancesPageClient } from "./finances-client"

interface FinancesPageProps {
  searchParams: Promise<{ type?: string; status?: string; range?: string }>
}

export default async function FinancesPage({ searchParams }: FinancesPageProps) {
  const params = await searchParams
  const supabase = await createClient()

  // Build query with optional filters
  let query = supabase.from("transactions").select("*")

  // Apply type filter
  if (params.type && params.type !== "all") {
    query = query.eq("transaction_type", params.type)
  }

  // Apply status filter
  if (params.status && params.status !== "all") {
    query = query.eq("status", params.status)
  }

  // Apply date range filter
  if (params.range && params.range !== "all") {
    const now = new Date()
    let startDate: Date

    switch (params.range) {
      case "today":
        startDate = new Date(now.setHours(0, 0, 0, 0))
        break
      case "week":
        startDate = new Date(now.setDate(now.getDate() - 7))
        break
      case "month":
        startDate = new Date(now.setMonth(now.getMonth() - 1))
        break
      case "year":
        startDate = new Date(now.setFullYear(now.getFullYear() - 1))
        break
      default:
        startDate = new Date(0)
    }
    query = query.gte("created_at", startDate.toISOString())
  }

  // Execute query
  const { data: transactions, error } = await query.order("created_at", { ascending: false }).limit(100)

  // Get all transactions for stats (unfiltered)
  const { data: allTransactions } = await supabase
    .from("transactions")
    .select("transaction_type, amount, status, created_at")

  // Calculate financial stats
  type TxRow = { status?: string; transaction_type?: string; amount?: number; created_at?: string }
  const confirmedTx = allTransactions?.filter((t: TxRow) => t.status === 'confirmed') || []

  const totalDeposits = confirmedTx
    .filter((t: TxRow) => t.transaction_type === 'deposit')
    .reduce((sum: number, t: TxRow) => sum + (t.amount || 0), 0)

  const totalPayouts = confirmedTx
    .filter((t: TxRow) => t.transaction_type === 'payout')
    .reduce((sum: number, t: TxRow) => sum + (t.amount || 0), 0)

  const totalGasRevenue = confirmedTx
    .filter((t: TxRow) => t.transaction_type === 'gas_consumed')
    .reduce((sum: number, t: TxRow) => sum + (t.amount || 0), 0)

  const netRevenue = totalDeposits + totalGasRevenue - totalPayouts

  // Calculate today's stats
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const todayTx = allTransactions?.filter((t: TxRow) => new Date(t.created_at!) >= today) || []
  const depositsToday = todayTx
    .filter((t: TxRow) => t.transaction_type === 'deposit' && t.status === 'confirmed')
    .reduce((sum: number, t: TxRow) => sum + (t.amount || 0), 0)

  // Calculate this week's stats
  const weekAgo = new Date()
  weekAgo.setDate(weekAgo.getDate() - 7)
  const weekTx = allTransactions?.filter((t: TxRow) => new Date(t.created_at!) >= weekAgo) || []

  const hasFilters = params.type || params.status || params.range
  const totalTransactions = allTransactions?.length || 0

  // Prepare chart data (last 7 days)
  const chartData = []
  for (let i = 6; i >= 0; i--) {
    const date = new Date()
    date.setDate(date.getDate() - i)
    date.setHours(0, 0, 0, 0)
    const nextDate = new Date(date)
    nextDate.setDate(nextDate.getDate() + 1)

    const dayTx = allTransactions?.filter((t: TxRow) => {
      const txDate = new Date(t.created_at!)
      return txDate >= date && txDate < nextDate && t.status === 'confirmed'
    }) || []

    chartData.push({
      date: date.toLocaleDateString('en-US', { weekday: 'short' }),
      deposits: dayTx.filter((t: TxRow) => t.transaction_type === 'deposit').reduce((s: number, t: TxRow) => s + (t.amount || 0), 0),
      gasRevenue: dayTx.filter((t: TxRow) => t.transaction_type === 'gas_consumed').reduce((s: number, t: TxRow) => s + (t.amount || 0), 0),
      payouts: dayTx.filter((t: TxRow) => t.transaction_type === 'payout').reduce((s: number, t: TxRow) => s + (t.amount || 0), 0),
    })
  }

  // Prepare breakdown data
  const breakdownData = [
    { type: 'Deposits', count: confirmedTx.filter((t: TxRow) => t.transaction_type === 'deposit').length, amount: totalDeposits },
    { type: 'Found', count: confirmedTx.filter((t: TxRow) => t.transaction_type === 'found').length, amount: confirmedTx.filter((t: TxRow) => t.transaction_type === 'found').reduce((s: number, t: TxRow) => s + (t.amount || 0), 0) },
    { type: 'Gas', count: confirmedTx.filter((t: TxRow) => t.transaction_type === 'gas_consumed').length, amount: totalGasRevenue },
    { type: 'Payouts', count: confirmedTx.filter((t: TxRow) => t.transaction_type === 'payout').length, amount: totalPayouts },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-saddle-dark">Financial Dashboard</h2>
          <p className="text-leather-light">
            Track revenue, transactions, and financial health
          </p>
        </div>
      </div>

      {/* Main Stats Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Total Deposits
            </CardTitle>
            <ArrowDownCircle className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              ${totalDeposits.toFixed(2)}
            </div>
            <p className="text-xs text-leather-light">
              +${depositsToday.toFixed(2)} today
            </p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Gas Revenue
            </CardTitle>
            <Fuel className="h-4 w-4 text-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-gold-dark">
              ${totalGasRevenue.toFixed(2)}
            </div>
            <p className="text-xs text-leather-light">
              From daily fees
            </p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Total Payouts
            </CardTitle>
            <ArrowUpCircle className="h-4 w-4 text-fire" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-fire">
              ${totalPayouts.toFixed(2)}
            </div>
            <p className="text-xs text-leather-light">
              User withdrawals
            </p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30 border-2 border-gold/50">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Net Revenue
            </CardTitle>
            {netRevenue >= 0 ? (
              <TrendingUp className="h-4 w-4 text-green-600" />
            ) : (
              <TrendingDown className="h-4 w-4 text-fire" />
            )}
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${netRevenue >= 0 ? 'text-green-600' : 'text-fire'}`}>
              ${netRevenue.toFixed(2)}
            </div>
            <p className="text-xs text-leather-light">
              Deposits + Gas - Payouts
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Secondary Stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Total Transactions
            </CardTitle>
            <Activity className="h-4 w-4 text-brass" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{totalTransactions}</div>
            <p className="text-xs text-leather-light">
              {todayTx.length} today, {weekTx.length} this week
            </p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Pending
            </CardTitle>
            <DollarSign className="h-4 w-4 text-yellow-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">
              {allTransactions?.filter((t: TxRow) => t.status === 'pending').length || 0}
            </div>
            <p className="text-xs text-leather-light">
              Awaiting confirmation
            </p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Avg Transaction
            </CardTitle>
            <DollarSign className="h-4 w-4 text-saddle" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">
              ${totalTransactions > 0 
                ? (confirmedTx.reduce((s: number, t: TxRow) => s + (t.amount || 0), 0) / confirmedTx.length).toFixed(2)
                : '0.00'
              }
            </div>
            <p className="text-xs text-leather-light">
              Per confirmed transaction
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Charts and Transactions */}
      <FinancesPageClient 
        transactions={transactions || []}
        chartData={chartData}
        breakdownData={breakdownData}
        error={error?.message}
        hasFilters={!!hasFilters}
        totalTransactions={totalTransactions}
        searchParams={params}
      />
    </div>
  )
}
