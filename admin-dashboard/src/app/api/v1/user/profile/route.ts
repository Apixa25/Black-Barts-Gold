import { NextRequest, NextResponse } from 'next/server'
import { createPublicClient, createServiceRoleClient } from '@/lib/supabase/server'

interface ProfileResponse {
  success: boolean
  profile?: {
    id: string
    email: string | null
    displayName: string | null
    age: number | null
    phoneNumber: string | null
    avatarUrl: string | null
    avatarPresetId: string | null
    role: string
    updatedAt: string
  }
  error?: string
}

interface UpdateProfileRequest {
  email?: string
  displayName?: string
  age?: number
  phoneNumber?: string | null
  avatarUrl?: string | null
  avatarPresetId?: string | null
}

function normalizePhoneE164(input: string | null | undefined): string | null {
  if (!input) return null
  const trimmed = input.trim()
  if (!trimmed) return null
  const digits = trimmed.replace(/[^\d]/g, '')
  return `+${digits}`
}

function isValidE164(phone: string | null): boolean {
  if (!phone) return true
  return /^\+[1-9]\d{7,14}$/.test(phone)
}

export async function GET(request: NextRequest): Promise<NextResponse<ProfileResponse>> {
  try {
    const authHeader = request.headers.get('Authorization')
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return NextResponse.json({ success: false, error: 'Missing authorization token' }, { status: 401 })
    }

    const token = authHeader.replace('Bearer ', '')
    const publicClient = createPublicClient()
    const serviceClient = createServiceRoleClient()
    const { data: authData, error: authError } = await publicClient.auth.getUser(token)

    if (authError || !authData.user) {
      return NextResponse.json({ success: false, error: 'Invalid or expired token' }, { status: 401 })
    }

    const { data: profile, error: profileError } = await serviceClient
      .from('profiles')
      .select('*')
      .eq('id', authData.user.id)
      .single()

    if (profileError || !profile) {
      return NextResponse.json({ success: false, error: 'Profile not found' }, { status: 404 })
    }

    return NextResponse.json({
      success: true,
      profile: {
        id: profile.id,
        email: profile.email,
        displayName: profile.full_name,
        age: profile.age,
        phoneNumber: profile.phone,
        avatarUrl: profile.avatar_url,
        avatarPresetId: profile.avatar_preset_id,
        role: profile.role,
        updatedAt: profile.updated_at,
      },
    })
  } catch (error) {
    console.error('[User Profile GET] Unexpected error:', error)
    return NextResponse.json({ success: false, error: 'Internal server error' }, { status: 500 })
  }
}

export async function PATCH(request: NextRequest): Promise<NextResponse<ProfileResponse>> {
  try {
    const authHeader = request.headers.get('Authorization')
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return NextResponse.json({ success: false, error: 'Missing authorization token' }, { status: 401 })
    }

    const token = authHeader.replace('Bearer ', '')
    const publicClient = createPublicClient()
    const serviceClient = createServiceRoleClient()

    const { data: authData, error: authError } = await publicClient.auth.getUser(token)
    if (authError || !authData.user) {
      return NextResponse.json({ success: false, error: 'Invalid or expired token' }, { status: 401 })
    }

    const body: UpdateProfileRequest = await request.json()
    const updates: Record<string, string | number | null> = {}

    if (typeof body.displayName === 'string') {
      const name = body.displayName.trim()
      if (name.length < 3 || name.length > 20) {
        return NextResponse.json({ success: false, error: 'Display name must be 3-20 characters' }, { status: 400 })
      }
      updates.full_name = name
    }

    if (typeof body.age === 'number') {
      if (body.age < 13 || body.age > 120) {
        return NextResponse.json({ success: false, error: 'Age must be between 13 and 120' }, { status: 400 })
      }
      updates.age = body.age
    }

    if (typeof body.phoneNumber === 'string' || body.phoneNumber === null) {
      const normalized = normalizePhoneE164(body.phoneNumber)
      if (!isValidE164(normalized)) {
        return NextResponse.json({ success: false, error: 'Phone must be E.164 (example: +14155552671)' }, { status: 400 })
      }
      updates.phone = normalized
    }

    if (typeof body.avatarUrl === 'string' || body.avatarUrl === null) {
      updates.avatar_url = body.avatarUrl && body.avatarUrl.trim() ? body.avatarUrl.trim() : null
    }

    if (typeof body.avatarPresetId === 'string' || body.avatarPresetId === null) {
      updates.avatar_preset_id = body.avatarPresetId && body.avatarPresetId.trim() ? body.avatarPresetId.trim() : null
    }

    if (typeof body.email === 'string') {
      const email = body.email.trim().toLowerCase()
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailRegex.test(email)) {
        return NextResponse.json({ success: false, error: 'Invalid email format' }, { status: 400 })
      }

      const { error: emailError } = await serviceClient.auth.admin.updateUserById(authData.user.id, { email })
      if (emailError) {
        console.error('[User Profile PATCH] Email update failed:', emailError.message)
        return NextResponse.json({ success: false, error: 'Unable to update email' }, { status: 400 })
      }

      updates.email = email
    }

    updates.updated_at = new Date().toISOString()

    const { data: profile, error: updateError } = await serviceClient
      .from('profiles')
      .update(updates)
      .eq('id', authData.user.id)
      .select('*')
      .single()

    if (updateError || !profile) {
      console.error('[User Profile PATCH] Profile update failed:', updateError?.message)
      return NextResponse.json({ success: false, error: 'Failed to update profile' }, { status: 500 })
    }

    return NextResponse.json({
      success: true,
      profile: {
        id: profile.id,
        email: profile.email,
        displayName: profile.full_name,
        age: profile.age,
        phoneNumber: profile.phone,
        avatarUrl: profile.avatar_url,
        avatarPresetId: profile.avatar_preset_id,
        role: profile.role,
        updatedAt: profile.updated_at,
      },
    })
  } catch (error) {
    console.error('[User Profile PATCH] Unexpected error:', error)
    return NextResponse.json({ success: false, error: 'Internal server error' }, { status: 500 })
  }
}
