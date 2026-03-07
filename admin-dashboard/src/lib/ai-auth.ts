/**
 * AI Route Authentication
 *
 * Validates the bearer token sent by the MCP server (and any other AI agent)
 * on every request to the /api/v1/admin/ai/* routes.
 *
 * The expected key is set via the AI_AGENT_API_KEY environment variable on
 * the admin dashboard deployment. The same value must be set as AI_AGENT_API_KEY
 * in the mcp-server/.env file.
 *
 * If AI_AGENT_API_KEY is not configured, all requests are allowed through
 * (useful for local development). Set it in production.
 *
 * @file admin-dashboard/src/lib/ai-auth.ts
 */

import { NextRequest, NextResponse } from 'next/server'

/**
 * Returns true if the request carries a valid AI_AGENT_API_KEY bearer token.
 * Returns true unconditionally when the key is not configured (dev mode).
 */
export function isValidAiApiKey(request: NextRequest): boolean {
  const expectedKey = process.env.AI_AGENT_API_KEY
  if (!expectedKey) return true // dev mode — no key configured

  const auth = request.headers.get('Authorization')
  if (!auth?.startsWith('Bearer ')) return false
  return auth.slice(7) === expectedKey
}

/** Returns a 401 Unauthorized response for invalid/missing AI API keys */
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
