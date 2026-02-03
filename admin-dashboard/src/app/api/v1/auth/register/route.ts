// ============================================================================
// POST /api/v1/auth/register
// Black Bart's Gold - Mobile App User Registration
// ============================================================================
// Creates a new user account via Supabase Auth.
// Profile is auto-created by database trigger.
// Returns session token and user data.
// ============================================================================

import { NextRequest, NextResponse } from 'next/server'
import { createPublicClient, createServiceRoleClient } from '@/lib/supabase/server'

interface RegisterRequest {
  email: string
  password: string
  displayName?: string
  age?: number
}

interface RegisterResponse {
  success: boolean
  token?: string
  refreshToken?: string
  expiresAt?: number
  user?: {
    id: string
    email: string
    displayName: string | null
    avatarUrl: string | null
    role: string
    createdAt: string
  }
  error?: string
  emailConfirmationRequired?: boolean
}

export async function POST(request: NextRequest): Promise<NextResponse<RegisterResponse>> {
  try {
    // Parse request body
    const body: RegisterRequest = await request.json()
    const { email, password, displayName, age } = body

    // Validate required fields
    if (!email || !password) {
      return NextResponse.json(
        { success: false, error: 'Email and password are required' },
        { status: 400 }
      )
    }

    // Validate email format
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(email)) {
      return NextResponse.json(
        { success: false, error: 'Invalid email format' },
        { status: 400 }
      )
    }

    // Validate password strength
    if (password.length < 6) {
      return NextResponse.json(
        { success: false, error: 'Password must be at least 6 characters' },
        { status: 400 }
      )
    }

    // Validate age if provided
    if (age !== undefined && (age < 13 || age > 120)) {
      return NextResponse.json(
        { success: false, error: 'You must be at least 13 years old to play' },
        { status: 400 }
      )
    }

    // Use Admin API to create user with email_confirm: true — no email verification needed
    const serviceClient = createServiceRoleClient()

    const { data: createData, error: createError } = await serviceClient.auth.admin.createUser({
      email,
      password,
      email_confirm: true,
      user_metadata: {
        full_name: displayName || null,
        age: age || null,
      },
    })

    if (createError) {
      console.error('[Auth Register] createUser error:', createError.message)

      let errorMessage = 'Registration failed'
      if (createError.message.includes('already') || createError.message.includes('exists') || createError.message.includes('registered')) {
        errorMessage = 'An account with this email already exists'
      } else if (createError.message.includes('Password') || createError.message.includes('password')) {
        errorMessage = 'Password does not meet requirements'
      } else if (createError.message.includes('rate limit') || createError.message.includes('limit')) {
        errorMessage = 'Too many registration attempts. Please try again later'
      } else if (createError.message.includes('invalid') || createError.message.includes('format')) {
        errorMessage = 'Invalid email format'
      }

      return NextResponse.json(
        { success: false, error: errorMessage },
        { status: 400 }
      )
    }

    if (!createData.user) {
      return NextResponse.json(
        { success: false, error: 'Registration failed - no user created' },
        { status: 400 }
      )
    }

    // Update profile with display name
    try {
      await serviceClient
        .from('profiles')
        .update({ full_name: displayName || null })
        .eq('id', createData.user.id)
    } catch (updateError) {
      console.error('[Auth Register] Profile update error:', updateError)
      // Don't fail registration if profile update fails
    }

    // Sign in to get session tokens for the mobile app
    const supabase = createPublicClient()
    const { data: signInData, error: signInError } = await supabase.auth.signInWithPassword({
      email,
      password,
    })

    if (signInError || !signInData?.session) {
      console.error('[Auth Register] Sign-in after create failed:', signInError?.message)
      return NextResponse.json({
        success: true,
        emailConfirmationRequired: true,
        user: {
          id: createData.user.id,
          email: createData.user.email || email,
          displayName: displayName || null,
          avatarUrl: null,
          role: 'user',
          createdAt: createData.user.created_at,
        }
      })
    }

    console.log(`[Auth Register] User ${email} registered and logged in`)

    return NextResponse.json({
      success: true,
      token: signInData.session.access_token,
      refreshToken: signInData.session.refresh_token,
      expiresAt: signInData.session.expires_at,
      user: {
        id: signInData.user.id,
        email: signInData.user.email || email,
        displayName: displayName || null,
        avatarUrl: null,
        role: 'user',
        createdAt: signInData.user.created_at,
      }
    })

  } catch (error) {
    console.error('[Auth Register] Unexpected error:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error' },
      { status: 500 }
    )
  }
}
