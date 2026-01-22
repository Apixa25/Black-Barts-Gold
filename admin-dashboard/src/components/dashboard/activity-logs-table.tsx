"use client"

import type { ActivityLog, ActivityType, ActivitySeverity } from "@/types/database"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { format } from "date-fns"
import { 
  LogIn,
  LogOut,
  KeyRound,
  UserCog,
  Shield,
  Coins,
  Building2,
  CreditCard,
  AlertTriangle,
  Settings,
  XCircle,
  Activity
} from "lucide-react"

interface ActivityLogsTableProps {
  logs: ActivityLog[]
}

const activityConfig: Record<ActivityType, { 
  label: string
  icon: typeof LogIn
  color: string
}> = {
  login: { label: "Login", icon: LogIn, color: "text-green-600" },
  logout: { label: "Logout", icon: LogOut, color: "text-gray-500" },
  login_failed: { label: "Failed Login", icon: XCircle, color: "text-fire" },
  password_changed: { label: "Password Changed", icon: KeyRound, color: "text-yellow-600" },
  profile_updated: { label: "Profile Updated", icon: UserCog, color: "text-blue-600" },
  role_changed: { label: "Role Changed", icon: Shield, color: "text-purple-600" },
  coin_created: { label: "Coin Created", icon: Coins, color: "text-gold" },
  coin_collected: { label: "Coin Collected", icon: Coins, color: "text-green-600" },
  coin_deleted: { label: "Coin Deleted", icon: Coins, color: "text-fire" },
  sponsor_created: { label: "Sponsor Added", icon: Building2, color: "text-brass" },
  sponsor_updated: { label: "Sponsor Updated", icon: Building2, color: "text-blue-600" },
  transaction_created: { label: "Transaction", icon: CreditCard, color: "text-green-600" },
  payout_requested: { label: "Payout Request", icon: CreditCard, color: "text-yellow-600" },
  suspicious_activity: { label: "Suspicious", icon: AlertTriangle, color: "text-fire" },
  admin_action: { label: "Admin Action", icon: Settings, color: "text-saddle" },
}

const severityConfig: Record<ActivitySeverity, { label: string; color: string }> = {
  info: { label: "Info", color: "bg-blue-100 text-blue-700" },
  warning: { label: "Warning", color: "bg-yellow-100 text-yellow-700" },
  error: { label: "Error", color: "bg-fire/20 text-fire" },
  critical: { label: "Critical", color: "bg-red-600 text-white" },
}

export function ActivityLogsTable({ logs }: ActivityLogsTableProps) {
  if (logs.length === 0) {
    return (
      <div className="text-center py-12 text-leather-light">
        <Activity className="mx-auto h-12 w-12 text-saddle-light/50 mb-4" />
        <p className="text-lg font-medium">No activity yet</p>
        <p className="text-sm">Activity logs will appear here as events occur.</p>
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow className="hover:bg-transparent">
          <TableHead className="text-leather">Time</TableHead>
          <TableHead className="text-leather">Activity</TableHead>
          <TableHead className="text-leather">Severity</TableHead>
          <TableHead className="text-leather">Description</TableHead>
          <TableHead className="text-leather">IP Address</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {logs.map((log) => {
          const activity = activityConfig[log.activity_type] || activityConfig.admin_action
          const severity = severityConfig[log.severity] || severityConfig.info
          const ActivityIcon = activity.icon

          return (
            <TableRow key={log.id} className="hover:bg-parchment/50">
              <TableCell className="text-leather-light text-sm whitespace-nowrap">
                {format(new Date(log.created_at), "MMM d, HH:mm:ss")}
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-2">
                  <ActivityIcon className={`h-4 w-4 ${activity.color}`} />
                  <span className="text-sm font-medium text-saddle-dark">
                    {activity.label}
                  </span>
                </div>
              </TableCell>
              <TableCell>
                <Badge className={`${severity.color} text-xs`}>
                  {severity.label}
                </Badge>
              </TableCell>
              <TableCell className="text-leather text-sm max-w-[300px] truncate">
                {log.description}
              </TableCell>
              <TableCell className="text-leather-light text-sm font-mono">
                {log.ip_address || '—'}
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
