/**
 * POST /api/v1/admin/ai/kill-switch
 *
 * Toggles the distribution_config.enabled flag — the master kill switch for
 * all AI autonomous spawning. When disabled, every spawn route returns HTTP 503.
 *
 * This route is for HUMAN ADMINS only (super_admin role required).
 * The AI agent reads the kill switch state via GET /api/v1/admin/ai/hunt-pressure
 * and respects it — it never calls this route itself.
 *
 * Request Body: { enabled: boolean }
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/kill-switch/route.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createClient, createServiceRoleClient } from '@/lib/supabase/server'

export const dynamic = 'force-dynamic'

export async function POST(request: NextRequest) {
  // ── Admin session auth ─────────────────────────────────────────────────────
  const supabase = await createClient()
  const { data: { user } } = await supabase.auth.getUser()

  if (!user) {
    return NextResponse.json({ success: false, error: 'Unauthorized' }, { status: 401 })
  }

  const { data: profile } = await supabase
    .from('profiles')
    .select('role')
    .eq('id', user.id)
    .single()

  if (profile?.role !== 'super_admin') {
    return NextResponse.json(
      { success: false, error: 'super_admin role required to toggle the kill switch' },
      { status: 403 }
    )
  }

  // ── Parse body ─────────────────────────────────────────────────────────────
  let body: { enabled?: unknown }
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ success: false, error: 'Invalid JSON body' }, { status: 400 })
  }

  if (typeof body.enabled !== 'boolean') {
    return NextResponse.json(
      { success: false, error: '`enabled` must be a boolean' },
      { status: 400 }
    )
  }

  // ── Update distribution_config ─────────────────────────────────────────────
  const adminSupabase = createServiceRoleClient()
  const { error } = await adminSupabase
    .from('distribution_config')
    .update({ enabled: body.enabled })
    .eq('id', '00000000-0000-0000-0000-000000000001')

  if (error) {
    return NextResponse.json(
      { success: false, error: `Database update failed: ${error.message}` },
      { status: 500 }
    )
  }

  return NextResponse.json({
    success: true,
    data: {
      enabled: body.enabled,
      changed_by: user.id,
      message: body.enabled
        ? 'Auto-distribution ENABLED — Black Bart is back on duty 🤠'
        : 'Auto-distribution DISABLED — kill switch active 🛑',
    },
    timestamp: new Date().toISOString(),
  })
}
