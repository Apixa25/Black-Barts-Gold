import { createClient } from "@/lib/supabase/server"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Users, Coins, DollarSign, TrendingUp } from "lucide-react"
import Link from "next/link"
import { Button } from "@/components/ui/button"

export default async function DashboardPage() {
  const supabase = await createClient()

  // Fetch real user count from Supabase
  const { count: userCount } = await supabase
    .from("profiles")
    .select("*", { count: "exact", head: true })

  const stats = [
    {
      name: "Total Users",
      value: userCount ?? 0,
      icon: Users,
      change: userCount === 1 ? "You're the first!" : `${userCount} registered`,
      changeType: "positive" as const,
      href: "/users",
    },
    {
      name: "Active Coins",
      value: "—",
      icon: Coins,
      change: "Coming in Phase 3",
      changeType: "neutral" as const,
      href: "/coins",
    },
    {
      name: "Total Deposits",
      value: "—",
      icon: DollarSign,
      change: "Coming in Phase 4",
      changeType: "neutral" as const,
      href: "/finances",
    },
    {
      name: "Daily Revenue",
      value: "—",
      icon: TrendingUp,
      change: "Coming in Phase 4",
      changeType: "neutral" as const,
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
                  stat.changeType === "positive" ? "text-green-600" :
                  stat.changeType === "negative" ? "text-fire" :
                  "text-leather-light"
                }`}>
                  {stat.change}
                </p>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      {/* Quick Actions */}
      <div className="grid gap-4 md:grid-cols-2">
        <Card className="border-saddle-light/30">
          <CardHeader>
            <CardTitle className="text-saddle-dark">🎉 Phase 1 & 2 In Progress!</CardTitle>
            <CardDescription>Building user management features</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-leather text-sm">
              Current progress:
            </p>
            <ul className="text-sm text-leather-light list-disc list-inside space-y-1">
              <li className="text-green-600">✓ Supabase connected</li>
              <li className="text-green-600">✓ Authentication working</li>
              <li className="text-green-600">✓ Database schema created</li>
              <li className="text-green-600">✓ Admin user logged in</li>
              <li>Building user management...</li>
            </ul>
            <Button asChild className="w-full bg-gold hover:bg-gold-dark text-leather">
              <Link href="/users">Manage Users →</Link>
            </Button>
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
                View All Users
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start border-saddle-light/50">
              <Link href="/coins">
                <Coins className="mr-2 h-4 w-4" />
                Manage Coins (Coming Soon)
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start border-saddle-light/50">
              <Link href="/settings">
                <TrendingUp className="mr-2 h-4 w-4" />
                Dashboard Settings
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
