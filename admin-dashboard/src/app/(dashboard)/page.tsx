import { createClient } from "@/lib/supabase/server"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Users, Coins, DollarSign, TrendingUp, Building2, Shield, Settings, MapPin } from "lucide-react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { LivePlayersMap } from "@/components/dashboard/live-players-map"

export default async function DashboardPage() {
  const supabase = await createClient()

  // Fetch real counts from Supabase
  const { count: userCount } = await supabase
    .from("profiles")
    .select("*", { count: "exact", head: true })

  const { count: coinCount } = await supabase
    .from("coins")
    .select("*", { count: "exact", head: true })

  const { count: visibleCoinCount } = await supabase
    .from("coins")
    .select("*", { count: "exact", head: true })
    .eq("status", "visible")

  const { count: sponsorCount } = await supabase
    .from("sponsors")
    .select("*", { count: "exact", head: true })

  const { count: transactionCount } = await supabase
    .from("transactions")
    .select("*", { count: "exact", head: true })

  // Get total deposits
  const { data: depositData } = await supabase
    .from("transactions")
    .select("amount")
    .eq("transaction_type", "deposit")
    .eq("status", "confirmed")

  const totalDeposits = depositData?.reduce((sum: number, tx: { amount: number }) => sum + tx.amount, 0) ?? 0

  // Get gas revenue
  const { data: gasData } = await supabase
    .from("transactions")
    .select("amount")
    .eq("transaction_type", "gas_consumed")
    .eq("status", "confirmed")

  const totalGasRevenue = gasData?.reduce((sum: number, tx: { amount: number }) => sum + tx.amount, 0) ?? 0

  const stats = [
    {
      name: "Total Users",
      value: userCount ?? 0,
      icon: Users,
      change: userCount === 1 ? "1 admin registered" : `${userCount} registered`,
      changeType: "positive" as const,
      href: "/users",
    },
    {
      name: "Active Coins",
      value: coinCount ?? 0,
      icon: Coins,
      change: visibleCoinCount ? `${visibleCoinCount} visible` : "None visible yet",
      changeType: (coinCount ?? 0) > 0 ? "positive" as const : "neutral" as const,
      href: "/coins",
    },
    {
      name: "Total Deposits",
      value: totalDeposits > 0 ? `$${totalDeposits.toFixed(2)}` : "$0.00",
      icon: DollarSign,
      change: transactionCount ? `${transactionCount} transactions` : "No transactions yet",
      changeType: totalDeposits > 0 ? "positive" as const : "neutral" as const,
      href: "/finances",
    },
    {
      name: "Gas Revenue",
      value: totalGasRevenue > 0 ? `$${totalGasRevenue.toFixed(2)}` : "$0.00",
      icon: TrendingUp,
      change: totalGasRevenue > 0 ? "Platform earnings" : "Awaiting activity",
      changeType: totalGasRevenue > 0 ? "positive" as const : "neutral" as const,
      href: "/finances",
    },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark">Dashboard</h2>
        <p className="text-leather-light">
          Overview of Black Bart&apos;s Gold treasure hunting operations
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <Link key={stat.name} href={stat.href}>
            <Card className="border-saddle-light/30 hover:border-gold transition-colors cursor-pointer">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium text-leather-light">
                  {stat.name}
                </CardTitle>
                <stat.icon className="h-4 w-4 text-saddle" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-saddle-dark">{stat.value}</div>
                <p className={`text-xs ${
                  stat.changeType === "positive" ? "text-green-600" : "text-leather-light"
                }`}>
                  {stat.change}
                </p>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      {/* Live Players Map */}
      <LivePlayersMap height={350} showFullScreenLink={true} />

      {/* Quick Actions */}
      <div className="grid gap-4 md:grid-cols-2">
        <Card className="border-saddle-light/30">
          <CardHeader>
            <CardTitle className="text-saddle-dark">🏆 System Status</CardTitle>
            <CardDescription>All systems operational</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="flex items-center gap-2 text-sm">
                <div className="h-2 w-2 rounded-full bg-green-500" />
                <span className="text-leather">Database</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <div className="h-2 w-2 rounded-full bg-green-500" />
                <span className="text-leather">Authentication</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <div className="h-2 w-2 rounded-full bg-green-500" />
                <span className="text-leather">User Management</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <div className="h-2 w-2 rounded-full bg-green-500" />
                <span className="text-leather">Coin System</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <div className="h-2 w-2 rounded-full bg-green-500" />
                <span className="text-leather">Finances</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <div className="h-2 w-2 rounded-full bg-green-500" />
                <span className="text-leather">Sponsors</span>
              </div>
            </div>
            <div className="pt-2 border-t border-saddle-light/20">
              <p className="text-xs text-leather-light">
                Admin dashboard fully operational with {userCount ?? 0} user(s), {coinCount ?? 0} coin(s), and {sponsorCount ?? 0} sponsor(s).
              </p>
            </div>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader>
            <CardTitle className="text-saddle-dark">📋 Quick Actions</CardTitle>
            <CardDescription>Common administrative tasks</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            <Button asChild variant="outline" className="w-full justify-start border-saddle-light/50">
              <Link href="/users">
                <Users className="mr-2 h-4 w-4" />
                Manage Users
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start border-saddle-light/50">
              <Link href="/coins">
                <Coins className="mr-2 h-4 w-4" />
                Manage Coins
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start border-saddle-light/50">
              <Link href="/zones">
                <MapPin className="mr-2 h-4 w-4" />
                Manage Zones
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start border-saddle-light/50">
              <Link href="/sponsors">
                <Building2 className="mr-2 h-4 w-4" />
                Manage Sponsors
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start border-saddle-light/50">
              <Link href="/security">
                <Shield className="mr-2 h-4 w-4" />
                Security Logs
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start border-saddle-light/50">
              <Link href="/settings">
                <Settings className="mr-2 h-4 w-4" />
                Settings
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Quick Reference */}
      <Card className="border-gold/50 bg-gold/5">
        <CardHeader>
          <CardTitle className="text-saddle-dark flex items-center gap-2">
            🤠 Remember: Black Bart was NOT a pirate!
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-leather">
            Charles E. Boles (Black Bart) was a <strong>Wild West stagecoach robber</strong> from 
            the California Gold Rush era. Use Western terminology, not nautical/pirate themes. 
            See <code className="bg-parchment-dark px-1 rounded">Docs/brand-guide.md</code> for details.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
