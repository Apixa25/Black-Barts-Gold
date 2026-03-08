import { createClient } from '@supabase/supabase-js'

const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL
const serviceRoleKey = process.env.SUPABASE_SERVICE_ROLE_KEY

if (!supabaseUrl || !serviceRoleKey) {
  throw new Error('NEXT_PUBLIC_SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY are required')
}

const supabase = createClient(supabaseUrl, serviceRoleKey, {
  auth: { persistSession: false, autoRefreshToken: false },
})

const { data: zones, error: zonesError } = await supabase
  .from('zones')
  .select('id, name, metadata')
  .contains('metadata', { verification: 'ai_e2e_create_flow' })
  .order('created_at', { ascending: true })

if (zonesError) throw zonesError

const summary = []

for (const zone of zones ?? []) {
  const { data: queueRows, error: queueError } = await supabase
    .from('spawn_queue')
    .select('id, spawned_coin_id')
    .eq('zone_id', zone.id)

  if (queueError) throw queueError

  const { data: batchRows, error: batchError } = await supabase
    .from('release_batches')
    .select('id')
    .eq('zone_id', zone.id)

  if (batchError) throw batchError

  const { data: scheduleRows, error: scheduleError } = await supabase
    .from('release_schedules')
    .select('id')
    .eq('zone_id', zone.id)

  if (scheduleError) throw scheduleError

  const { data: historyRows, error: historyError } = await supabase
    .from('spawn_history')
    .select('id, coin_id')
    .eq('zone_id', zone.id)

  if (historyError) throw historyError

  const coinIds = [
    ...new Set(
      [...(queueRows ?? []).map((row) => row.spawned_coin_id), ...(historyRows ?? []).map((row) => row.coin_id)]
        .filter(Boolean)
    ),
  ]

  if ((queueRows ?? []).length > 0) {
    const { error } = await supabase.from('spawn_queue').delete().in('id', queueRows.map((row) => row.id))
    if (error) throw error
  }

  if ((batchRows ?? []).length > 0) {
    const { error } = await supabase.from('release_batches').delete().in('id', batchRows.map((row) => row.id))
    if (error) throw error
  }

  if ((scheduleRows ?? []).length > 0) {
    const { error } = await supabase.from('release_schedules').delete().in('id', scheduleRows.map((row) => row.id))
    if (error) throw error
  }

  if ((historyRows ?? []).length > 0) {
    const { error } = await supabase.from('spawn_history').delete().in('id', historyRows.map((row) => row.id))
    if (error) throw error
  }

  if (coinIds.length > 0) {
    const { error } = await supabase.from('coins').delete().in('id', coinIds)
    if (error) throw error
  }

  const { error: zoneDeleteError } = await supabase.from('zones').delete().eq('id', zone.id)
  if (zoneDeleteError) throw zoneDeleteError

  summary.push({
    zone_id: zone.id,
    zone_name: zone.name,
    queue_rows_deleted: queueRows?.length ?? 0,
    schedule_rows_deleted: scheduleRows?.length ?? 0,
    batch_rows_deleted: batchRows?.length ?? 0,
    spawn_history_deleted: historyRows?.length ?? 0,
    coins_deleted: coinIds.length,
  })
}

const { data: actionRows, error: actionRowsError } = await supabase
  .from('ai_actions')
  .select('id')
  .ilike('reasoning', '%end-to-end verification%')

if (actionRowsError) throw actionRowsError

if ((actionRows ?? []).length > 0) {
  const { error: actionDeleteError } = await supabase
    .from('ai_actions')
    .delete()
    .in('id', actionRows.map((row) => row.id))

  if (actionDeleteError) throw actionDeleteError
}

console.log(
  JSON.stringify(
    {
      success: true,
      cleaned_zones: summary,
      ai_actions_deleted: actionRows?.length ?? 0,
    },
    null,
    2
  )
)
