/**
 * Timed Releases Hook (M6)
 *
 * @file admin-dashboard/src/hooks/use-timed-releases.ts
 * @description Manages scheduled coin releases and hunt events
 */

"use client"

import { useState, useEffect, useCallback, useRef } from "react"
import type {
  ReleaseSchedule,
  ReleaseQueueItem,
  TimedReleaseStats,
} from "@/types/database"
import { computeBatchCount } from "@/components/maps/timed-release-config"

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

export function useTimedReleases(options: UseTimedReleasesOptions = {}): UseTimedReleasesResult {
  const { enabled = true, pollIntervalMs = 10000, zoneId } = options
  const [schedules, setSchedules] = useState<ReleaseSchedule[]>([])
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
  const intervalRef = useRef<NodeJS.Timeout | null>(null)

  const fetchData = useCallback(async () => {
    try {
      setError(null)
      const params = new URLSearchParams()
      if (zoneId) params.set("zone_id", zoneId)
      params.set("limit", "100")

      const response = await fetch(`/api/v1/admin/dashboard/timed-releases?${params.toString()}`, {
        cache: "no-store",
      })
      const payload = await response.json()

      if (!response.ok || !payload?.success) {
        throw new Error(payload?.error ?? "Failed to load timed releases")
      }

      const nextQueue = (payload.data?.queue ?? []) as ReleaseQueueItem[]
      const nextSchedules = (payload.data?.schedules ?? []) as ReleaseSchedule[]
      const summary = payload.data?.summary ?? {}

      setSchedules(nextSchedules)
      setQueue(nextQueue)
      setStats({
        active_schedules: summary.active_schedules ?? 0,
        scheduled_today: summary.scheduled_schedules ?? 0,
        completed_today: summary.completed_today ?? 0,
        total_coins_released_today: summary.total_coins_released_today ?? 0,
        total_value_released_today: summary.total_value_released_today ?? 0,
        next_release_in_seconds: nextQueue[0]?.time_until_seconds ?? null,
        next_release_zone: nextQueue[0]?.zone_name ?? null,
      })
    } catch (err) {
      console.error("Error fetching timed releases:", err)
      setError(err instanceof Error ? err.message : "Failed to fetch timed releases")
    }
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
    const response = await fetch("/api/v1/admin/dashboard/timed-releases", {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ scheduleId, action: "pause" }),
    })
    const payload = await response.json()
    if (!response.ok || !payload?.success) {
      throw new Error(payload?.error ?? "Failed to pause schedule")
    }
    await fetchData()
  }, [fetchData])

  const resumeSchedule = useCallback(async (scheduleId: string) => {
    const response = await fetch("/api/v1/admin/dashboard/timed-releases", {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ scheduleId, action: "resume" }),
    })
    const payload = await response.json()
    if (!response.ok || !payload?.success) {
      throw new Error(payload?.error ?? "Failed to resume schedule")
    }
    await fetchData()
  }, [fetchData])

  const cancelSchedule = useCallback(async (scheduleId: string) => {
    const response = await fetch("/api/v1/admin/dashboard/timed-releases", {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ scheduleId, action: "cancel" }),
    })
    const payload = await response.json()
    if (!response.ok || !payload?.success) {
      throw new Error(payload?.error ?? "Failed to cancel schedule")
    }
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
      const response = await fetch("/api/v1/admin/dashboard/timed-releases", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(opts),
      })
      const payload = await response.json()

      if (!response.ok || !payload?.success) {
        throw new Error(payload?.error ?? "Failed to create schedule")
      }

      await fetchData()
      const createdScheduleId = payload.data?.schedule_id as string | undefined
      if (!createdScheduleId) return null

      return (
        schedules.find((schedule) => schedule.id === createdScheduleId) ??
        {
          id: createdScheduleId,
          zone_id: opts.zoneId,
          zone_name: opts.zoneName,
          name: opts.name,
          description: `Dashboard-created timed release for ${opts.zoneName}`,
          total_coins: opts.totalCoins,
          coins_per_release: opts.coinsPerRelease,
          release_interval_seconds: opts.releaseIntervalSeconds,
          start_time: opts.startTime,
          end_time: opts.endTime ?? null,
          status: "scheduled",
          coins_released_so_far: 0,
          batches_completed: 0,
          batches_total: computeBatchCount(opts.totalCoins, opts.coinsPerRelease),
          next_release_at: opts.startTime,
          last_release_at: null,
          created_at: new Date().toISOString(),
          updated_at: new Date().toISOString(),
          cell_id: payload.data?.cell_id ?? null,
        }
      )
    },
    [fetchData, schedules]
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
