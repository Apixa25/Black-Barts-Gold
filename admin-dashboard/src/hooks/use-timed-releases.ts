/**
 * Timed Releases Hook (M6)
 *
 * @file admin-dashboard/src/hooks/use-timed-releases.ts
 * @description Manages scheduled coin releases and hunt events
 */

"use client"

import { useState, useEffect, useCallback, useRef } from "react"
import { createClient } from "@/lib/supabase/client"
import type {
  ReleaseSchedule,
  ReleaseQueueItem,
  TimedReleaseStats,
  ReleaseScheduleStatus,
} from "@/types/database"
import {
  formatTimeUntil,
  formatReleaseTime,
  formatScheduleDateTime,
  computeBatchCount,
  computeTotalDurationSeconds,
} from "@/components/maps/timed-release-config"

interface UseTimedReleasesOptions {
  enabled?: boolean
  pollIntervalMs?: number
  zoneId?: string
}

interface UseTimedReleasesResult {
  schedules: ReleaseSchedule[]
  queue: ReleaseQueueItem[]
  stats: TimedReleaseStats
  isLoading: boolean
  error: string | null
  refresh: () => Promise<void>
  pauseSchedule: (scheduleId: string) => Promise<void>
  resumeSchedule: (scheduleId: string) => Promise<void>
  cancelSchedule: (scheduleId: string) => Promise<void>
  createSchedule: (opts: {
    zoneId: string
    zoneName: string
    name: string
    totalCoins: number
    coinsPerRelease: number
    releaseIntervalSeconds: number
    startTime: string
    endTime?: string
  }) => Promise<ReleaseSchedule | null>
}

function mockSchedules(): ReleaseSchedule[] {
  const now = new Date()
  const in5m = new Date(now.getTime() + 5 * 60 * 1000)
  const in20m = new Date(now.getTime() + 20 * 60 * 1000)

  return [
    {
      id: "sched-1",
      zone_id: "zone-2",
      zone_name: "Golden Gate Hunt Zone",
      name: "Weekend Hunt",
      description: "100 coins over 10 minutes",
      total_coins: 100,
      coins_per_release: 10,
      release_interval_seconds: 60,
      start_time: in5m.toISOString(),
      end_time: null,
      status: "scheduled",
      coins_released_so_far: 0,
      batches_completed: 0,
      batches_total: 10,
      next_release_at: in5m.toISOString(),
      last_release_at: null,
      created_at: now.toISOString(),
      updated_at: now.toISOString(),
    },
    {
      id: "sched-2",
      zone_id: "zone-2",
      zone_name: "Golden Gate Hunt Zone",
      name: "Evening Drop",
      description: "50 coins, 1 per minute",
      total_coins: 50,
      coins_per_release: 1,
      release_interval_seconds: 60,
      start_time: in20m.toISOString(),
      end_time: null,
      status: "scheduled",
      coins_released_so_far: 0,
      batches_completed: 0,
      batches_total: 50,
      next_release_at: in20m.toISOString(),
      last_release_at: null,
      created_at: now.toISOString(),
      updated_at: now.toISOString(),
    },
    {
      id: "sched-3",
      zone_id: "zone-1",
      zone_name: "Downtown Player Zone",
      name: "Lunch Rush",
      description: "30 coins over 5 min",
      total_coins: 30,
      coins_per_release: 6,
      release_interval_seconds: 60,
      start_time: new Date(now.getTime() - 2 * 60 * 1000).toISOString(),
      end_time: null,
      status: "active",
      coins_released_so_far: 12,
      batches_completed: 2,
      batches_total: 5,
      next_release_at: new Date(now.getTime() + 58 * 1000).toISOString(),
      last_release_at: new Date(now.getTime() - 62 * 1000).toISOString(),
      created_at: now.toISOString(),
      updated_at: now.toISOString(),
    },
  ]
}

function mockQueue(schedules: ReleaseSchedule[]): ReleaseQueueItem[] {
  const now = Date.now()
  const items: ReleaseQueueItem[] = []

  for (const s of schedules) {
    if (s.status !== "scheduled" && s.status !== "active") continue
    const next = s.next_release_at ? new Date(s.next_release_at).getTime() : 0
    if (next <= 0) continue
    items.push({
      id: `q-${s.id}`,
      schedule_id: s.id,
      schedule_name: s.name,
      zone_id: s.zone_id,
      zone_name: s.zone_name,
      release_at: s.next_release_at!,
      coins_count: s.coins_per_release,
      status: "pending",
      time_until_seconds: Math.max(0, Math.floor((next - now) / 1000)),
    })
  }

  items.sort((a, b) => a.time_until_seconds - b.time_until_seconds)
  return items.slice(0, 10)
}

function mockStats(schedules: ReleaseSchedule[], queue: ReleaseQueueItem[]): TimedReleaseStats {
  const active = schedules.filter((s) => s.status === "active").length
  const scheduled = schedules.filter((s) => s.status === "scheduled").length
  const next = queue[0]

  return {
    active_schedules: active,
    scheduled_today: scheduled + active,
    completed_today: 3,
    total_coins_released_today: 127,
    total_value_released_today: 89.5,
    next_release_in_seconds: next?.time_until_seconds ?? null,
    next_release_zone: next?.zone_name ?? null,
  }
}

export function useTimedReleases(options: UseTimedReleasesOptions = {}): UseTimedReleasesResult {
  const { enabled = true, pollIntervalMs = 10000, zoneId } = options
  const [schedules, setSchedules] = useState<ReleaseSchedule[]>(mockSchedules())
  const [queue, setQueue] = useState<ReleaseQueueItem[]>([])
  const [stats, setStats] = useState<TimedReleaseStats>({
    active_schedules: 0,
    scheduled_today: 0,
    completed_today: 0,
    total_coins_released_today: 0,
    total_value_released_today: 0,
    next_release_in_seconds: null,
    next_release_zone: null,
  })
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const supabase = createClient()
  const intervalRef = useRef<NodeJS.Timeout | null>(null)

  const fetchData = useCallback(async () => {
    setError(null)
    const useMock = true

    if (useMock) {
      let s = mockSchedules()
      if (zoneId) s = s.filter((x) => x.zone_id === zoneId)
      setSchedules(s)
      const q = mockQueue(s)
      setQueue(q)
      setStats(mockStats(s, q))
      return
    }

    // TODO: Supabase queries when release_schedules table exists
  }, [zoneId])

  const refresh = useCallback(async () => {
    setIsLoading(true)
    try {
      await fetchData()
    } finally {
      setIsLoading(false)
    }
  }, [fetchData])

  const pauseSchedule = useCallback(async (scheduleId: string) => {
    setSchedules((prev) =>
      prev.map((s) =>
        s.id === scheduleId ? { ...s, status: "paused" as ReleaseScheduleStatus } : s
      )
    )
    await fetchData()
  }, [fetchData])

  const resumeSchedule = useCallback(async (scheduleId: string) => {
    setSchedules((prev) =>
      prev.map((s) =>
        s.id === scheduleId ? { ...s, status: "active" as ReleaseScheduleStatus } : s
      )
    )
    await fetchData()
  }, [fetchData])

  const cancelSchedule = useCallback(async (scheduleId: string) => {
    setSchedules((prev) =>
      prev.map((s) =>
        s.id === scheduleId ? { ...s, status: "cancelled" as ReleaseScheduleStatus } : s
      )
    )
    await fetchData()
  }, [fetchData])

  const createSchedule = useCallback(
    async (opts: {
      zoneId: string
      zoneName: string
      name: string
      totalCoins: number
      coinsPerRelease: number
      releaseIntervalSeconds: number
      startTime: string
      endTime?: string
    }): Promise<ReleaseSchedule | null> => {
      const batches = computeBatchCount(opts.totalCoins, opts.coinsPerRelease)
      const start = new Date(opts.startTime)
      const schedule: ReleaseSchedule = {
        id: `sched-${Date.now()}`,
        zone_id: opts.zoneId,
        zone_name: opts.zoneName,
        name: opts.name,
        description: `${opts.totalCoins} coins, ${opts.coinsPerRelease} per release, every ${opts.releaseIntervalSeconds}s`,
        total_coins: opts.totalCoins,
        coins_per_release: opts.coinsPerRelease,
        release_interval_seconds: opts.releaseIntervalSeconds,
        start_time: opts.startTime,
        end_time: opts.endTime ?? null,
        status: "scheduled",
        coins_released_so_far: 0,
        batches_completed: 0,
        batches_total: batches,
        next_release_at: opts.startTime,
        last_release_at: null,
        created_at: new Date().toISOString(),
        updated_at: new Date().toISOString(),
      }
      setSchedules((prev) => [...prev, schedule])
      await fetchData()
      return schedule
    },
    [fetchData]
  )

  useEffect(() => {
    if (!enabled) return
    fetchData()
    intervalRef.current = setInterval(fetchData, pollIntervalMs)
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [enabled, fetchData, pollIntervalMs])

  // Update queue timers every second when there are pending items
  useEffect(() => {
    if (queue.length === 0) return
    const t = setInterval(() => {
      setQueue((prev) =>
        prev
          .map((item) => {
            const next = new Date(item.release_at).getTime()
            const sec = Math.max(0, Math.floor((next - Date.now()) / 1000))
            return { ...item, time_until_seconds: sec }
          })
          .filter((item) => item.time_until_seconds >= 0 || item.status === "releasing")
      )
    }, 1000)
    return () => clearInterval(t)
  }, [queue.length])

  return {
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
  }
}
