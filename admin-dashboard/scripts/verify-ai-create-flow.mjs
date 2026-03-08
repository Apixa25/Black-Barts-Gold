import { createClient } from '@supabase/supabase-js'
import crypto from 'node:crypto'

const baseUrl = process.env.VERIFY_BASE_URL ?? 'http://localhost:3000'
const apiKey = process.env.AI_AGENT_API_KEY
const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL
const serviceRoleKey = process.env.SUPABASE_SERVICE_ROLE_KEY

if (!supabaseUrl || !serviceRoleKey) {
  throw new Error('NEXT_PUBLIC_SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY are required')
}

const supabase = createClient(supabaseUrl, serviceRoleKey, {
  auth: { persistSession: false, autoRefreshToken: false },
})

const startedAtIso = new Date().toISOString()
const stamp = Date.now()
const zoneCenter = { latitude: 37.7749, longitude: -122.4194 }
const zoneName = `AI E2E Verify ${stamp}`

function describeError(error) {
  if (error instanceof Error) return error.message
  if (error && typeof error === 'object') {
    if (typeof error.message === 'string') return error.message
    try {
      return JSON.stringify(error)
    } catch {
      return '[unserializable error object]'
    }
  }
  return String(error)
}

async function apiFetch(path, init = {}) {
  const headers = {
    'Content-Type': 'application/json',
    ...(init.headers ?? {}),
  }

  if (apiKey) {
    headers.Authorization = `Bearer ${apiKey}`
  }

  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers,
  })

  const text = await response.text()
  let json
  try {
    json = JSON.parse(text)
  } catch {
    json = text
  }

  return { status: response.status, body: json }
}

async function createZone() {
  const payload = {
    name: zoneName,
    description: 'Temporary zone for AI timed release end-to-end verification',
    zone_type: 'hunt',
    status: 'active',
    geometry: {
      type: 'circle',
      center: zoneCenter,
      radius_meters: 180,
    },
    timed_release_config: {
      enabled: true,
      total_coins: 2,
      coins_per_release: 2,
      release_interval_seconds: 60,
    },
    metadata: {
      verification: 'ai_e2e_create_flow',
      stamp,
    },
  }

  const { data, error } = await supabase
    .from('zones')
    .insert(payload)
    .select('id, name, geometry, created_at')
    .single()

  if (error) throw error
  return data
}

async function fetchVerificationRows(zoneId, scheduleId) {
  const [{ data: scheduleRows, error: scheduleError }, { data: queueRows, error: queueError }] =
    await Promise.all([
      supabase
        .from('release_schedules')
        .select('id, zone_id, name, status, next_release_at, s2_cell_token_l17, s2_cell_token_l14, coin_tier, min_value, max_value, coins_released_so_far, batches_completed, created_at')
        .eq('id', scheduleId),
      supabase
        .from('spawn_queue')
        .select('id, zone_id, trigger_type, scheduled_time, status, spawned_coin_id, target_latitude, target_longitude, s2_cell_token_l17, s2_cell_token_l14, created_at, processed_at')
        .eq('zone_id', zoneId)
        .gte('created_at', startedAtIso)
        .order('created_at', { ascending: true }),
    ])

  if (scheduleError) throw scheduleError
  if (queueError) throw queueError

  const coinIds = (queueRows ?? [])
    .map((row) => row.spawned_coin_id)
    .filter(Boolean)

  const [{ data: coinRows, error: coinError }, { data: historyRows, error: historyError }, { data: actionRows, error: actionError }, { data: batchRows, error: batchError }] =
    await Promise.all([
      coinIds.length > 0
        ? supabase
            .from('coins')
            .select('id, latitude, longitude, status, created_by, s2_cell_token_l17, s2_cell_token_l14, created_at')
            .in('id', coinIds)
            .order('created_at', { ascending: true })
        : Promise.resolve({ data: [], error: null }),
      coinIds.length > 0
        ? supabase
            .from('spawn_history')
            .select('id, coin_id, zone_id, trigger_type, spawn_latitude, spawn_longitude, created_by, s2_cell_token_l17, s2_cell_token_l14, spawned_at, recycled_at')
            .in('coin_id', coinIds)
            .order('spawned_at', { ascending: true })
        : Promise.resolve({ data: [], error: null }),
      supabase
        .from('ai_actions')
        .select('id, tool_called, success, reasoning, result, created_at')
        .in('tool_called', ['schedule_timed_release', 'process_timed_releases', 'process_spawn_queue'])
        .gte('created_at', startedAtIso)
        .order('created_at', { ascending: true }),
      supabase
        .from('release_batches')
        .select('id, schedule_id, zone_id, release_at, coins_count, status, s2_cell_token_l17, s2_cell_token_l14, coin_tier, created_at')
        .eq('schedule_id', scheduleId)
        .order('created_at', { ascending: true }),
    ])

  if (coinError) throw coinError
  if (historyError) throw historyError
  if (actionError) throw actionError
  if (batchError) throw batchError

  return {
    schedules: scheduleRows ?? [],
    queue: queueRows ?? [],
    coins: coinRows ?? [],
    spawn_history: historyRows ?? [],
    ai_actions: actionRows ?? [],
    release_batches: batchRows ?? [],
  }
}

async function cleanup(zoneId, scheduleId, verificationRows) {
  const queueIds = verificationRows.queue.map((row) => row.id)
  const coinIds = verificationRows.coins.map((row) => row.id)
  const actionIds = verificationRows.ai_actions.map((row) => row.id)

  if (queueIds.length > 0) {
    const { error } = await supabase.from('spawn_queue').delete().in('id', queueIds)
    if (error) throw error
  }

  if (verificationRows.release_batches.length > 0) {
    const { error } = await supabase
      .from('release_batches')
      .delete()
      .in('id', verificationRows.release_batches.map((row) => row.id))
    if (error) throw error
  }

  if (scheduleId) {
    const { error } = await supabase.from('release_schedules').delete().eq('id', scheduleId)
    if (error) throw error
  }

  if (verificationRows.spawn_history.length > 0) {
    const { error } = await supabase
      .from('spawn_history')
      .delete()
      .in('id', verificationRows.spawn_history.map((row) => row.id))
    if (error) throw error
  }

  if (coinIds.length > 0) {
    const { error } = await supabase.from('coins').delete().in('id', coinIds)
    if (error) throw error
  }

  if (zoneId) {
    const { error } = await supabase.from('zones').delete().eq('id', zoneId)
    if (error) throw error
  }

  if (actionIds.length > 0) {
    const { error } = await supabase.from('ai_actions').delete().in('id', actionIds)
    if (error) throw error
  }
}

function assertVerification(result) {
  if (result.createSchedule.status !== 200) {
    throw new Error(`Create timed release failed: ${JSON.stringify(result.createSchedule.body)}`)
  }
  if (result.processTimedReleases.status !== 200) {
    throw new Error(`Process timed releases failed: ${JSON.stringify(result.processTimedReleases.body)}`)
  }
  if (result.processSpawnQueue.status !== 200) {
    throw new Error(`Process spawn queue failed: ${JSON.stringify(result.processSpawnQueue.body)}`)
  }

  const scheduleCell = result.rows.schedules[0]?.s2_cell_token_l17
  if (!scheduleCell) throw new Error('Schedule was created without s2_cell_token_l17')

  if (result.rows.queue.length === 0) throw new Error('No queue rows were created')
  if (result.rows.coins.length === 0) throw new Error('No coins were spawned from the queue')
  if (result.rows.spawn_history.length === 0) throw new Error('No spawn_history rows were recorded')

  for (const row of result.rows.queue) {
    if (row.s2_cell_token_l17 !== scheduleCell) {
      throw new Error(`Queue row ${row.id} cell mismatch: expected ${scheduleCell}, got ${row.s2_cell_token_l17}`)
    }
    if (!row.target_latitude || !row.target_longitude) {
      throw new Error(`Queue row ${row.id} is missing target coordinates`)
    }
  }

  for (const row of result.rows.coins) {
    if (row.s2_cell_token_l17 !== scheduleCell) {
      throw new Error(`Coin ${row.id} cell mismatch: expected ${scheduleCell}, got ${row.s2_cell_token_l17}`)
    }
  }

  for (const row of result.rows.spawn_history) {
    if (row.s2_cell_token_l17 !== scheduleCell) {
      throw new Error(`Spawn history ${row.id} cell mismatch: expected ${scheduleCell}, got ${row.s2_cell_token_l17}`)
    }
  }
}

const result = {
  baseUrl,
  started_at: startedAtIso,
  zone: null,
  createSchedule: null,
  getTimedReleases: null,
  processTimedReleases: null,
  getSpawnQueue: null,
  processSpawnQueue: null,
  rows: null,
  cleanup: null,
}

let zoneId = null
let scheduleId = null

try {
  const zone = await createZone()
  zoneId = zone.id
  result.zone = zone

  const createPayload = {
    zone_id: zone.id,
    name: `AI Create Flow ${stamp}`,
    description: 'Temporary timed release created by live end-to-end verification',
    total_coins: 2,
    coins_per_release: 2,
    release_interval_seconds: 60,
    start_time: new Date(Date.now() - 1000).toISOString(),
    tier: 'bronze',
    target_latitude: zoneCenter.latitude,
    target_longitude: zoneCenter.longitude,
    agent_id: 'ai_game_master',
    reasoning: 'Live end-to-end verification of timed release create flow',
    idempotency_key: crypto.randomUUID(),
  }

  result.createSchedule = await apiFetch('/api/v1/admin/ai/timed-releases', {
    method: 'POST',
    body: JSON.stringify(createPayload),
  })

  scheduleId = result.createSchedule.body?.data?.schedule_id ?? null
  if (!scheduleId) {
    throw new Error(`Schedule creation did not return schedule_id: ${JSON.stringify(result.createSchedule.body)}`)
  }

  result.getTimedReleases = await apiFetch(`/api/v1/admin/ai/timed-releases?zone_id=${zone.id}&limit=10`)
  result.processTimedReleases = await apiFetch('/api/v1/admin/ai/process-timed-releases', {
    method: 'POST',
    body: JSON.stringify({
      agent_id: 'ai_game_master',
      reasoning: 'Expand due timed releases into queue items for end-to-end verification',
      limit: 10,
    }),
  })

  result.getSpawnQueue = await apiFetch(`/api/v1/admin/ai/spawn-queue?limit=20`)
  result.processSpawnQueue = await apiFetch('/api/v1/admin/ai/process-spawn-queue', {
    method: 'POST',
    body: JSON.stringify({
      agent_id: 'ai_game_master',
      reasoning: 'Process queued spawns for end-to-end verification',
    }),
  })

  result.rows = await fetchVerificationRows(zone.id, scheduleId)
  assertVerification(result)

  await cleanup(zone.id, scheduleId, result.rows)
  result.cleanup = { success: true }

  console.log(JSON.stringify({ success: true, result }, null, 2))
} catch (error) {
  try {
    if (zoneId) {
      if (!result.rows) {
        result.rows = await fetchVerificationRows(zoneId, scheduleId)
      }
      await cleanup(zoneId, scheduleId, result.rows)
      result.cleanup = { success: true }
    }
  } catch (cleanupError) {
    result.cleanup = {
      success: false,
      error: describeError(cleanupError),
    }
  }

  console.error(
    JSON.stringify(
      {
        success: false,
        error: describeError(error),
        result,
      },
      null,
      2
    )
  )
  process.exit(1)
}
