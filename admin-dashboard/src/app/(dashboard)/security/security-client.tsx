"use client"

import { Suspense } from "react"
import type { ActivityLog } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { ActivityLogsTable } from "@/components/dashboard/activity-logs-table"
import { ActivitySearch } from "@/components/dashboard/activity-search"

interface SecurityPageClientProps {
  logs: ActivityLog[]
  error?: string
  hasFilters: boolean
  totalLogs: number
  searchParams: { type?: string; severity?: string; range?: string }
}

export function SecurityPageClient({ 
  logs, 
  error, 
  hasFilters, 
  totalLogs,
  searchParams 
}: SecurityPageClientProps) {
  return (
    <>
      {/* Search and Filter */}
      <Suspense fallback={<div className="h-10 bg-parchment animate-pulse rounded" />}>
        <ActivitySearch />
      </Suspense>

      {/* Results info */}
      {hasFilters && (
        <p className="text-sm text-leather-light">
          Showing {logs.length} of {totalLogs} events
          {searchParams.type && searchParams.type !== "all" && (
            <span> of type &quot;{searchParams.type}&quot;</span>
          )}
          {searchParams.severity && searchParams.severity !== "all" && (
            <span> with severity &quot;{searchParams.severity}&quot;</span>
          )}
          {searchParams.range && searchParams.range !== "all" && (
            <span> from {searchParams.range}</span>
          )}
        </p>
      )}

      {/* Activity Logs Table */}
      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">
            {hasFilters ? "Filtered Activity" : "Recent Activity"}
          </CardTitle>
          <CardDescription>
            {logs.length} {hasFilters ? "matching" : "most recent"} events
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error ? (
            <div className="text-fire text-sm p-4 bg-fire/10 rounded-lg">
              Error loading activity logs: {error}
            </div>
          ) : (
            <ActivityLogsTable logs={logs} />
          )}
        </CardContent>
      </Card>
    </>
  )
}
