/**
 * Shared admin-session authorization for dashboard-backed API routes.
 *
 * Keeps human-admin routes aligned with the project vision's "hard guardrails
 * in the API layer" by enforcing role checks server-side.
 *
 * @file admin-dashboard/src/lib/admin-session.ts
 */

import { createClient } from '@/lib/supabase/server'

export type AdminRole = 'super_admin' | 'sponsor_admin'

export interface AdminSessionResult {
  userId: string
  role: AdminRole
}

export async function requireAdminSession(): Promise<AdminSessionResult> {
  const supabase = await createClient()
  const {
    data: { user },
  } = await supabase.auth.getUser()

  if (!user) {
    throw new Error('UNAUTHORIZED')
  }

  const { data: profile, error } = await supabase
    .from('profiles')
    .select('role')
    .eq('id', user.id)
    .single()

  if (error) {
    throw new Error(error.message)
  }

  if (profile?.role !== 'super_admin' && profile?.role !== 'sponsor_admin') {
    throw new Error('FORBIDDEN')
  }

  return {
    userId: user.id,
    role: profile.role as AdminRole,
  }
}
