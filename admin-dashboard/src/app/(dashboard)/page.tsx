import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Users, Coins, DollarSign, TrendingUp } from "lucide-react"

export default async function DashboardPage() {
  // Stats will be fetched from Supabase once tables are created
  const stats = [
    {
      name: "Total Users",
      value: "—",
      icon: Users,
      change: "Connect Supabase",
      changeType: "neutral" as const,
    },
    {
      name: "Active Coins",
      value: "—",
      icon: Coins,
      change: "Coming in Phase 3",
      changeType: "neutral" as const,
    },
    {
      name: "Total Deposits",
      value: "—",
      icon: DollarSign,
      change: "Coming in Phase 4",
      changeType: "neutral" as const,
    },
    {
      name: "Daily Revenue",
      value: "—",
      icon: TrendingUp,
      change: "Coming in Phase 4",
      changeType: "neutral" as const,
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
          <Card key={stat.name} className="border-saddle-light/30">
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
        ))}
      </div>

      {/* Info Cards */}
      <div className="grid gap-4 md:grid-cols-2">
        <Card className="border-saddle-light/30">
          <CardHeader>
            <CardTitle className="text-saddle-dark">🎉 Phase 0 Complete!</CardTitle>
            <CardDescription>Project foundation is ready</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            <p className="text-leather text-sm">
              The admin dashboard project has been initialized with:
            </p>
            <ul className="text-sm text-leather-light list-disc list-inside space-y-1">
              <li>Next.js 14+ with App Router</li>
              <li>TypeScript for type safety</li>
              <li>Tailwind CSS with Western Gold &amp; Brown theme</li>
              <li>shadcn/ui components</li>
              <li>Supabase client setup</li>
              <li>Authentication pages ready</li>
            </ul>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader>
            <CardTitle className="text-saddle-dark">📋 Next Steps</CardTitle>
            <CardDescription>Phase 1 &amp; 2 tasks</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            <ol className="text-sm text-leather-light list-decimal list-inside space-y-1">
              <li>Create Supabase project at supabase.com</li>
              <li>Add credentials to .env.local</li>
              <li>Run database schema SQL</li>
              <li>Create first admin user</li>
              <li>Test login flow</li>
              <li>Build user management features</li>
            </ol>
            <p className="text-xs text-leather-light mt-4">
              See ADMIN-DASHBOARD-BUILD-GUIDE.md for detailed instructions.
            </p>
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
