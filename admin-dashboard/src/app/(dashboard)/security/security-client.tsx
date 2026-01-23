"use client"

import { Suspense, useState } from "react"
import type { ActivityLog } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ActivityLogsTable } from "@/components/dashboard/activity-logs-table"
import { ActivitySearch } from "@/components/dashboard/activity-search"
import { AntiCheatPanel } from "@/components/dashboard/anti-cheat-panel"
import { Activity, Shield } from "lucide-react"

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
  const [activeTab, setActiveTab] = useState<string>("activity")

  return (
    <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
      <TabsList className="bg-parchment border border-saddle-light/30">
        <TabsTrigger 
          value="activity"
          className="data-[state=active]:bg-gold data-[state=active]:text-leather"
        >
          <Activity className="h-4 w-4 mr-2" />
          Activity Logs
        </TabsTrigger>
        <TabsTrigger 
          value="anti-cheat"
          className="data-[state=active]:bg-gold data-[state=active]:text-leather"
        >
          <Shield className="h-4 w-4 mr-2" />
          Anti-Cheat
        </TabsTrigger>
      </TabsList>

      {/* Activity Logs Tab */}
      <TabsContent value="activity" className="mt-4">
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
      </TabsContent>

      {/* Anti-Cheat Tab */}
      <TabsContent value="anti-cheat" className="mt-4">
        <AntiCheatPanel />
      </TabsContent>
    </Tabs>
  )
}
