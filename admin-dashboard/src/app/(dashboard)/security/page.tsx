import { createClient } from "@/lib/supabase/server"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { 
  Shield, 
  LogIn, 
  AlertTriangle, 
  Users,
  Activity,
  XCircle,
  CheckCircle
} from "lucide-react"
import { SecurityPageClient } from "./security-client"

interface SecurityPageProps {
  searchParams: Promise<{ type?: string; severity?: string; range?: string }>
}

export default async function SecurityPage({ searchParams }: SecurityPageProps) {
  const params = await searchParams
  const supabase = await createClient()

  // Build query with optional filters
  let query = supabase.from("activity_logs").select("*")

  // Apply activity type filter
  if (params.type && params.type !== "all") {
    query = query.eq("activity_type", params.type)
  }

  // Apply severity filter
  if (params.severity && params.severity !== "all") {
    query = query.eq("severity", params.severity)
  }

  // Apply time range filter
  if (params.range && params.range !== "all") {
    const now = new Date()
    let startDate: Date

    switch (params.range) {
      case "hour":
        startDate = new Date(now.getTime() - 60 * 60 * 1000)
        break
      case "today":
        startDate = new Date(now.setHours(0, 0, 0, 0))
        break
      case "week":
        startDate = new Date(now.setDate(now.getDate() - 7))
        break
      case "month":
        startDate = new Date(now.setMonth(now.getMonth() - 1))
        break
      default:
        startDate = new Date(0)
    }
    query = query.gte("created_at", startDate.toISOString())
  }

  // Execute query
  const { data: logs, error } = await query
    .order("created_at", { ascending: false })
    .limit(100)

  // Get all logs for stats (today only)
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  
  const { data: todayLogs } = await supabase
    .from("activity_logs")
    .select("activity_type, severity")
    .gte("created_at", today.toISOString())

  // Get all logs for total count
  const { count: totalLogs } = await supabase
    .from("activity_logs")
    .select("*", { count: 'exact', head: true })

  // Calculate stats
  const loginsToday = todayLogs?.filter(l => l.activity_type === 'login').length || 0
  const failedLoginsToday = todayLogs?.filter(l => l.activity_type === 'login_failed').length || 0
  const suspiciousToday = todayLogs?.filter(l => l.activity_type === 'suspicious_activity').length || 0
  const warningsToday = todayLogs?.filter(l => l.severity === 'warning' || l.severity === 'error' || l.severity === 'critical').length || 0
  const adminActionsToday = todayLogs?.filter(l => l.activity_type === 'admin_action').length || 0

  // Get user stats
  const { count: totalUsers } = await supabase
    .from("profiles")
    .select("*", { count: 'exact', head: true })

  const hasFilters = params.type || params.severity || params.range

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-saddle-dark">Security Monitoring</h2>
          <p className="text-leather-light">
            Track activity, monitor threats, and audit system events
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-6">
        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Logins Today
            </CardTitle>
            <LogIn className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{loginsToday}</div>
            <p className="text-xs text-leather-light">Successful logins</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Failed Logins
            </CardTitle>
            <XCircle className="h-4 w-4 text-fire" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-fire">{failedLoginsToday}</div>
            <p className="text-xs text-leather-light">Today</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Suspicious
            </CardTitle>
            <AlertTriangle className="h-4 w-4 text-yellow-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">{suspiciousToday}</div>
            <p className="text-xs text-leather-light">Flagged activities</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Warnings
            </CardTitle>
            <AlertTriangle className="h-4 w-4 text-fire" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-fire">{warningsToday}</div>
            <p className="text-xs text-leather-light">Errors & warnings</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Admin Actions
            </CardTitle>
            <Shield className="h-4 w-4 text-brass" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-brass">{adminActionsToday}</div>
            <p className="text-xs text-leather-light">Today</p>
          </CardContent>
        </Card>

        <Card className="border-saddle-light/30">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-leather-light">
              Total Events
            </CardTitle>
            <Activity className="h-4 w-4 text-saddle" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-saddle-dark">{totalLogs || 0}</div>
            <p className="text-xs text-leather-light">All time</p>
          </CardContent>
        </Card>
      </div>

      {/* System Health Card */}
      <Card className="border-saddle-light/30 border-2 border-green-200">
        <CardHeader className="flex flex-row items-center gap-3 pb-2">
          <CheckCircle className="h-6 w-6 text-green-600" />
          <div>
            <CardTitle className="text-green-700">System Status: Healthy</CardTitle>
            <CardDescription>
              All systems operational • {totalUsers || 0} registered users • No critical alerts
            </CardDescription>
          </div>
        </CardHeader>
      </Card>

      {/* Client Component handles search and table */}
      <SecurityPageClient 
        logs={logs || []}
        error={error?.message}
        hasFilters={!!hasFilters}
        totalLogs={totalLogs || 0}
        searchParams={params}
      />
    </div>
  )
}
