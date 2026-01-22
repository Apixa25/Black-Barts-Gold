"use client"

import { useRouter, useSearchParams } from "next/navigation"
import { useState, useTransition } from "react"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Search, X, RefreshCw } from "lucide-react"

export function ActivitySearch() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [isPending, startTransition] = useTransition()
  
  const [activityType, setActivityType] = useState(searchParams.get("type") || "all")
  const [severity, setSeverity] = useState(searchParams.get("severity") || "all")
  const [timeRange, setTimeRange] = useState(searchParams.get("range") || "all")

  const handleSearch = () => {
    startTransition(() => {
      const params = new URLSearchParams()
      if (activityType && activityType !== "all") params.set("type", activityType)
      if (severity && severity !== "all") params.set("severity", severity)
      if (timeRange && timeRange !== "all") params.set("range", timeRange)
      
      router.push(`/security?${params.toString()}`)
    })
  }

  const handleClear = () => {
    setActivityType("all")
    setSeverity("all")
    setTimeRange("all")
    startTransition(() => {
      router.push("/security")
    })
  }

  const handleRefresh = () => {
    startTransition(() => {
      router.refresh()
    })
  }

  const hasFilters = activityType !== "all" || severity !== "all" || timeRange !== "all"

  return (
    <div className="flex flex-col sm:flex-row gap-3">
      <Select value={activityType} onValueChange={setActivityType}>
        <SelectTrigger className="w-full sm:w-[160px] border-saddle-light/30">
          <SelectValue placeholder="Activity Type" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Activities</SelectItem>
          <SelectItem value="login">🔓 Logins</SelectItem>
          <SelectItem value="login_failed">❌ Failed Logins</SelectItem>
          <SelectItem value="profile_updated">👤 Profile Updates</SelectItem>
          <SelectItem value="role_changed">🛡️ Role Changes</SelectItem>
          <SelectItem value="coin_created">🪙 Coins Created</SelectItem>
          <SelectItem value="coin_collected">✨ Coins Collected</SelectItem>
          <SelectItem value="sponsor_created">🏢 Sponsors</SelectItem>
          <SelectItem value="suspicious_activity">⚠️ Suspicious</SelectItem>
          <SelectItem value="admin_action">⚙️ Admin Actions</SelectItem>
        </SelectContent>
      </Select>

      <Select value={severity} onValueChange={setSeverity}>
        <SelectTrigger className="w-full sm:w-[130px] border-saddle-light/30">
          <SelectValue placeholder="Severity" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Levels</SelectItem>
          <SelectItem value="info">ℹ️ Info</SelectItem>
          <SelectItem value="warning">⚠️ Warning</SelectItem>
          <SelectItem value="error">❌ Error</SelectItem>
          <SelectItem value="critical">🚨 Critical</SelectItem>
        </SelectContent>
      </Select>

      <Select value={timeRange} onValueChange={setTimeRange}>
        <SelectTrigger className="w-full sm:w-[130px] border-saddle-light/30">
          <SelectValue placeholder="Time Range" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Time</SelectItem>
          <SelectItem value="hour">Last Hour</SelectItem>
          <SelectItem value="today">Today</SelectItem>
          <SelectItem value="week">This Week</SelectItem>
          <SelectItem value="month">This Month</SelectItem>
        </SelectContent>
      </Select>

      <Button 
        onClick={handleSearch}
        disabled={isPending}
        className="bg-gold hover:bg-gold-dark text-leather"
      >
        <Search className="h-4 w-4 mr-2" />
        {isPending ? "..." : "Filter"}
      </Button>

      {hasFilters && (
        <Button 
          variant="outline" 
          onClick={handleClear}
          className="border-saddle-light/30"
        >
          <X className="h-4 w-4 mr-1" />
          Clear
        </Button>
      )}

      <Button 
        variant="outline" 
        onClick={handleRefresh}
        disabled={isPending}
        className="border-saddle-light/30"
      >
        <RefreshCw className={`h-4 w-4 ${isPending ? 'animate-spin' : ''}`} />
      </Button>
    </div>
  )
}
