/**
 * POST /api/v1/admin/ai/trigger-governor
 *
 * Manually triggers a Spawn Governor cycle. The "Summon Black Bart" button
 * in the AI Governor dashboard calls this route.
 *
 * Calls the deployed Supabase Edge Function directly (server-to-server),
 * waits for the result, and returns it to the admin UI.
 *
 * Requires: super_admin session.
 * If the Edge Function is not yet deployed, returns a helpful 503 with setup instructions.
 *
 * @file admin-dashboard/src/app/api/v1/admin/ai/trigger-governor/route.ts
 */

import { NextResponse } from 'next/server'
import { createClient } from '@/lib/supabase/server'

export const dynamic = 'force-dynamic'

export async function POST() {
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
      { success: false, error: 'super_admin role required' },
      { status: 403 }
    )
  }

  // ── Validate Edge Function config ──────────────────────────────────────────
  const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL
  const serviceRoleKey = process.env.SUPABASE_SERVICE_ROLE_KEY

  if (!supabaseUrl || !serviceRoleKey) {
    return NextResponse.json(
      {
        success: false,
        error: 'Spawn Governor Edge Function is not configured',
        code: 'EDGE_FUNCTION_NOT_CONFIGURED',
        setup_instructions: [
          '1. Deploy: supabase functions deploy spawn-governor --no-verify-jwt',
          '2. Set secrets: supabase secrets set ADMIN_API_BASE_URL=... AI_AGENT_API_KEY=...',
          '3. Ensure SUPABASE_SERVICE_ROLE_KEY is set in admin-dashboard .env.local',
        ],
      },
      { status: 503 }
    )
  }

  // ── Trigger the Edge Function ──────────────────────────────────────────────
  const edgeFunctionUrl = `${supabaseUrl}/functions/v1/spawn-governor?trigger=manual`

  try {
    const res = await fetch(edgeFunctionUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${serviceRoleKey}`,
      },
      body: JSON.stringify({ trigger: 'manual', triggered_by: user.id }),
      signal: AbortSignal.timeout(45_000), // 45s timeout for full governor cycle
    })

    const data = await res.json()

    return NextResponse.json({
      success: res.ok,
      data,
      triggered_by: user.id,
      timestamp: new Date().toISOString(),
    }, { status: res.ok ? 200 : res.status })
  } catch (err) {
    const isTimeout = err instanceof Error && err.name === 'TimeoutError'
    return NextResponse.json(
      {
        success: false,
        error: isTimeout
          ? 'Governor cycle timed out after 45 seconds (this can happen if many pressure cells need spawning)'
          : `Edge Function call failed: ${err instanceof Error ? err.message : String(err)}`,
        code: isTimeout ? 'TIMEOUT' : 'EDGE_FUNCTION_ERROR',
      },
      { status: 500 }
    )
  }
}
