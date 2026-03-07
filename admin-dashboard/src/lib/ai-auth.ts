/**
 * AI Route Authentication
 *
 * Two auth helpers for the /api/v1/admin/ai/* routes:
 *
 * isValidAiApiKey(request)   — synchronous, for write routes (spawn, recycle)
 *                              that should only be called by AI agents.
 *
 * isAuthorizedRequest(request) — async dual-auth, for read routes (hunt-pressure,
 *                               economy-health, actions) that can also be called
 *                               by human admin users browsing the dashboard.
 *                               Accepts EITHER:
 *                                 1. Valid AI_AGENT_API_KEY bearer token
 *                                 2. Valid Supabase super_admin / sponsor_admin session
 *
 * @file admin-dashboard/src/lib/ai-auth.ts
 */

import { NextRequest, NextResponse } from 'next/server'
import { createClient } from '@/lib/supabase/server'

/**
 * Synchronous API-key-only check.
 * Use this on write routes (spawn, recycle) that must only be called by AI agents.
 * Returns true unconditionally when AI_AGENT_API_KEY is not configured (dev mode).
 */
export function isValidAiApiKey(request: NextRequest): boolean {
  const expectedKey = process.env.AI_AGENT_API_KEY
  if (!expectedKey) return true // dev mode — no key configured

  const auth = request.headers.get('Authorization')
  if (!auth?.startsWith('Bearer ')) return false
  return auth.slice(7) === expectedKey
}

/**
 * Async dual-auth check.
 * Use this on read routes (hunt-pressure, economy-health, actions) that the
 * admin dashboard UI also needs to call directly from the browser.
 *
 * Returns true if ANY of these conditions are met:
 *   1. AI_AGENT_API_KEY is not configured (dev mode)
 *   2. Valid AI_AGENT_API_KEY bearer token in the Authorization header
 *   3. Valid Supabase session cookie with super_admin or sponsor_admin role
 */
export async function isAuthorizedRequest(request: NextRequest): Promise<boolean> {
  const expectedKey = process.env.AI_AGENT_API_KEY

  // Dev mode: no key configured — allow all requests
  if (!expectedKey) return true

  // Fast path: API key check (synchronous, no DB needed)
  const auth = request.headers.get('Authorization')
  if (auth?.startsWith('Bearer ') && auth.slice(7) === expectedKey) return true

  // Slow path: valid admin session cookie
  try {
    const supabase = await createClient()
    const { data: { user } } = await supabase.auth.getUser()
    if (!user) return false

    const { data: profile } = await supabase
      .from('profiles')
      .select('role')
      .eq('id', user.id)
      .single()

    return profile?.role === 'super_admin' || profile?.role === 'sponsor_admin'
  } catch {
    return false
  }
}

/**
 * Returns a standard 401 Unauthorized response.
 */
export function unauthorizedResponse(): NextResponse {
  return NextResponse.json(
    {
      success: false,
      error: 'Unauthorized — valid AI_AGENT_API_KEY bearer token required',
      code: 'UNAUTHORIZED',
    },
    { status: 401 }
  )
}

/**
 * Returns a standard 403 Forbidden response (authenticated but insufficient role).
 */
export function forbiddenResponse(): NextResponse {
  return NextResponse.json(
    {
      success: false,
      error: 'Forbidden — super_admin role required',
      code: 'FORBIDDEN',
    },
    { status: 403 }
  )
}
