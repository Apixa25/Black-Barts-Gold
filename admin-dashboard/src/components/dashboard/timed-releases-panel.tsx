/**
 * Timed Releases Panel (M6)
 *
 * @file admin-dashboard/src/components/dashboard/timed-releases-panel.tsx
 * @description Schedule coin drops, batch releases, and hunt events
 */

"use client"

import { useState } from "react"
import { useTimedReleases } from "@/hooks/use-timed-releases"
import type { ReleaseSchedule, ReleaseQueueItem, Zone } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Clock,
  Calendar,
  Play,
  Pause,
  Square,
  RefreshCw,
  Plus,
  MoreVertical,
  Loader2,
  MapPin,
  Coins,
  CheckCircle,
  XCircle,
} from "lucide-react"
import { toast } from "sonner"
import {
  formatTimeUntil,
  formatScheduleDateTime,
  formatReleaseTime,
  computeBatchCount,
  computeTotalDurationSeconds,
  RELEASE_INTERVAL_PRESETS,
  DEFAULT_COINS_PER_RELEASE,
  DEFAULT_TOTAL_COINS,
} from "@/components/maps/timed-release-config"

interface TimedReleasesPanelProps {
  className?: string
  zones?: Zone[]
}

export function TimedReleasesPanel({ className = "", zones = [] }: TimedReleasesPanelProps) {
  const {
    schedules,
    queue,
    stats,
    isLoading,
    error,
    refresh,
    pauseSchedule,
    resumeSchedule,
    cancelSchedule,
    createSchedule,
  } = useTimedReleases()

  const [createOpen, setCreateOpen] = useState(false)
  const [form, setForm] = useState({
    zoneId: "",
    zoneName: "",
    name: "",
    totalCoins: DEFAULT_TOTAL_COINS,
    coinsPerRelease: DEFAULT_COINS_PER_RELEASE,
    releaseIntervalSeconds: 60,
    startTime: "",
  })

  const handleCreate = async () => {
    const zoneId = form.zoneId || (form.zoneName.trim() ? "manual" : "")
    const zoneName =
      form.zoneName.trim() ||
      (form.zoneId ? zones.find((z) => z.id === form.zoneId)?.name ?? "" : "")
    if (!form.name.trim()) {
      toast.error("Name is required")
      return
    }
    if (!zoneId || !zoneName) {
      toast.error("Zone is required")
      return
    }
    if (!form.startTime) {
      toast.error("Start time is required")
      return
    }
    try {
      await createSchedule({
        zoneId,
        zoneName,
        name: form.name.trim(),
        totalCoins: form.totalCoins,
        coinsPerRelease: form.coinsPerRelease,
        releaseIntervalSeconds: form.releaseIntervalSeconds,
        startTime: new Date(form.startTime).toISOString(),
      })
      toast.success("Schedule created", { description: form.name })
      setCreateOpen(false)
      setForm({
        zoneId: "",
        zoneName: "",
        name: "",
        totalCoins: DEFAULT_TOTAL_COINS,
        coinsPerRelease: DEFAULT_COINS_PER_RELEASE,
        releaseIntervalSeconds: 60,
        startTime: "",
      })
    } catch (e) {
      toast.error("Failed to create schedule")
    }
  }

  const handleZoneSelect = (zoneId: string) => {
    const z = zones.find((x) => x.id === zoneId)
    setForm((prev) => ({
      ...prev,
      zoneId: zoneId || "",
      zoneName: z?.name ?? "",
    }))
  }

  const batchCount = computeBatchCount(form.totalCoins, form.coinsPerRelease)
  const durationSec = computeTotalDurationSeconds(
    form.totalCoins,
    form.coinsPerRelease,
    form.releaseIntervalSeconds
  )
  const durationMin = Math.ceil(durationSec / 60)

  return (
    <div className={`space-y-4 ${className}`}>
      <Card className="border-saddle-light/30">
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between flex-wrap gap-2">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-gold/10">
                <Calendar className="h-5 w-5 text-gold" />
              </div>
              <div>
                <CardTitle className="text-saddle-dark flex items-center gap-2">
                  Timed Releases
                </CardTitle>
                <CardDescription>Schedule coin drops and hunt events</CardDescription>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={refresh} disabled={isLoading}>
                <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
              </Button>
              <Button
                size="sm"
                className="bg-gold hover:bg-gold-dark text-leather"
                onClick={() => setCreateOpen(true)}
              >
                <Plus className="h-4 w-4 mr-1" />
                New Schedule
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="text-center p-3 rounded-lg bg-parchment">
              <div className="text-2xl font-bold text-gold">{stats.active_schedules}</div>
              <div className="text-xs text-leather-light">Active</div>
            </div>
            <div className="text-center p-3 rounded-lg bg-parchment">
              <div className="text-2xl font-bold text-green-600">{stats.scheduled_today}</div>
              <div className="text-xs text-leather-light">Scheduled Today</div>
            </div>
            <div className="text-center p-3 rounded-lg bg-parchment">
              <div className="text-2xl font-bold text-blue-600">
                {stats.total_coins_released_today}
              </div>
              <div className="text-xs text-leather-light">Coins Released Today</div>
            </div>
            <div className="text-center p-3 rounded-lg bg-parchment">
              <div className="text-2xl font-bold text-fire">
                {stats.next_release_in_seconds != null
                  ? formatTimeUntil(stats.next_release_in_seconds)
                  : "—"}
              </div>
              <div className="text-xs text-leather-light">
                Next: {stats.next_release_zone ?? "—"}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="border-saddle-light/30">
        <CardHeader className="pb-2">
          <CardTitle className="text-saddle-dark text-lg flex items-center gap-2">
            <Clock className="h-5 w-5 text-gold" />
            Release Queue
          </CardTitle>
        </CardHeader>
        <CardContent>
          {queue.length === 0 ? (
            <p className="text-sm text-leather-light py-4">No upcoming releases.</p>
          ) : (
            <div className="space-y-2">
              {queue.slice(0, 8).map((item) => (
                <div
                  key={item.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-parchment"
                >
                  <div>
                    <div className="font-medium text-saddle-dark">{item.schedule_name}</div>
                    <div className="text-xs text-leather-light">
                      {item.zone_name} • {item.coins_count} coins
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="font-medium text-gold">
                      {formatTimeUntil(item.time_until_seconds)}
                    </div>
                    <div className="text-xs text-leather-light">
                      {formatReleaseTime(item.release_at)}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card className="border-saddle-light/30">
        <CardHeader className="pb-2">
          <CardTitle className="text-saddle-dark text-lg flex items-center gap-2">
            <MapPin className="h-5 w-5 text-gold" />
            Schedules
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Schedule</TableHead>
                <TableHead>Zone</TableHead>
                <TableHead className="text-center">Progress</TableHead>
                <TableHead className="text-center">Status</TableHead>
                <TableHead>Next Release</TableHead>
                <TableHead className="w-10" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {schedules.map((s) => (
                <TableRow key={s.id}>
                  <TableCell>
                    <div>
                      <div className="font-medium">{s.name}</div>
                      <div className="text-xs text-leather-light">{s.description}</div>
                    </div>
                  </TableCell>
                  <TableCell className="text-leather-light">{s.zone_name}</TableCell>
                  <TableCell className="text-center">
                    <div className="text-sm">
                      {s.coins_released_so_far} / {s.total_coins}
                    </div>
                    <div className="text-xs text-leather-light">
                      {s.batches_completed} of {s.batches_total} batches
                    </div>
                  </TableCell>
                  <TableCell className="text-center">
                    <StatusBadge status={s.status} />
                  </TableCell>
                  <TableCell className="text-leather-light text-sm">
                    {s.next_release_at
                      ? formatScheduleDateTime(s.next_release_at)
                      : "—"}
                  </TableCell>
                  <TableCell>
                    {s.status !== "completed" && s.status !== "cancelled" && (
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                            <MoreVertical className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          {s.status === "paused" ? (
                            <DropdownMenuItem onClick={() => resumeSchedule(s.id)}>
                              <Play className="h-4 w-4 mr-2" />
                              Resume
                            </DropdownMenuItem>
                          ) : (
                            <DropdownMenuItem onClick={() => pauseSchedule(s.id)}>
                              <Pause className="h-4 w-4 mr-2" />
                              Pause
                            </DropdownMenuItem>
                          )}
                          <DropdownMenuItem
                            className="text-red-600"
                            onClick={() => cancelSchedule(s.id)}
                          >
                            <Square className="h-4 w-4 mr-2" />
                            Cancel
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          {schedules.length === 0 && (
            <p className="text-sm text-leather-light py-6 text-center">
              No schedules yet. Create one to get started.
            </p>
          )}
        </CardContent>
      </Card>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>New Timed Release</DialogTitle>
            <DialogDescription>
              Schedule a batch of coins to release over time in a zone.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label>Name</Label>
              <Input
                value={form.name}
                onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
                placeholder="e.g. Weekend Hunt"
              />
            </div>
            {zones.length > 0 ? (
              <div className="space-y-2">
                <Label>Zone</Label>
                <Select
                  value={form.zoneId}
                  onValueChange={(v) => handleZoneSelect(v)}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select zone" />
                  </SelectTrigger>
                  <SelectContent>
                    {zones.map((z) => (
                      <SelectItem key={z.id} value={z.id}>
                        {z.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            ) : (
              <div className="space-y-2">
                <Label>Zone name</Label>
                <Input
                  value={form.zoneName}
                  onChange={(e) => setForm((p) => ({ ...p, zoneName: e.target.value }))}
                  placeholder="e.g. Golden Gate Hunt Zone"
                />
                <p className="text-xs text-leather-light">
                  Zone dropdown appears when zones exist on this page.
                </p>
              </div>
            )}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Total coins</Label>
                <Input
                  type="number"
                  min={1}
                  value={form.totalCoins}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, totalCoins: parseInt(e.target.value) || 1 }))
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>Coins per release</Label>
                <Input
                  type="number"
                  min={1}
                  value={form.coinsPerRelease}
                  onChange={(e) =>
                    setForm((p) => ({
                      ...p,
                      coinsPerRelease: parseInt(e.target.value) || 1,
                    }))
                  }
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label>Release interval</Label>
              <Select
                value={String(form.releaseIntervalSeconds)}
                onValueChange={(v) =>
                  setForm((p) => ({ ...p, releaseIntervalSeconds: parseInt(v) || 60 }))
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {RELEASE_INTERVAL_PRESETS.map((preset) => (
                    <SelectItem key={preset.value} value={String(preset.value)}>
                      {preset.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Start time</Label>
              <Input
                type="datetime-local"
                value={form.startTime}
                onChange={(e) => setForm((p) => ({ ...p, startTime: e.target.value }))}
              />
            </div>
            <div className="p-3 rounded-lg bg-parchment text-sm">
              <div className="flex justify-between">
                <span className="text-leather-light">Batches:</span>
                <span className="font-medium">{batchCount}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-leather-light">Total duration:</span>
                <span className="font-medium">~{durationMin} min</span>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              Cancel
            </Button>
            <Button
              className="bg-gold hover:bg-gold-dark text-leather"
              onClick={handleCreate}
              disabled={
                !form.name.trim() ||
                (!form.zoneId && !form.zoneName.trim()) ||
                !form.startTime
              }
            >
              Create Schedule
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {error && (
        <Card className="border-red-300 bg-red-50">
          <CardContent className="py-3">
            <div className="flex items-center gap-2 text-red-600">
              <XCircle className="h-4 w-4" />
              <span className="text-sm">{error}</span>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

function StatusBadge({ status }: { status: ReleaseSchedule["status"] }) {
  const map: Record<
    string,
    { className: string; icon: typeof CheckCircle; label: string }
  > = {
    scheduled: {
      className: "text-blue-600 border-blue-600 bg-blue-50",
      icon: Clock,
      label: "Scheduled",
    },
    active: {
      className: "text-green-600 border-green-600 bg-green-50",
      icon: Play,
      label: "Active",
    },
    paused: {
      className: "text-yellow-600 border-yellow-600 bg-yellow-50",
      icon: Pause,
      label: "Paused",
    },
    completed: {
      className: "text-gray-600 border-gray-600 bg-gray-50",
      icon: CheckCircle,
      label: "Completed",
    },
    cancelled: {
      className: "text-red-600 border-red-600 bg-red-50",
      icon: XCircle,
      label: "Cancelled",
    },
  }
  const c = map[status] ?? map.scheduled
  const Icon = c.icon
  return (
    <Badge variant="outline" className={c.className}>
      <Icon className="h-3 w-3 mr-1" />
      {c.label}
    </Badge>
  )
}
