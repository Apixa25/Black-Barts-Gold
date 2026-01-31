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

    // Create Supabase client
    const supabase = createPublicClient()

    // Attempt to sign up
    const { data: authData, error: authError } = await supabase.auth.signUp({
      email,
      password,
      options: {
        data: {
          full_name: displayName || null,
          age: age || null,
        },
        // For mobile apps, we might want to skip email confirmation
        // This can be configured in Supabase dashboard
      }
    })

    if (authError) {
      console.error('[Auth Register] Supabase auth error:', authError.message)
      
      // Map common errors to user-friendly messages
      let errorMessage = 'Registration failed'
      if (authError.message.includes('already registered') || 
          authError.message.includes('already exists')) {
        errorMessage = 'An account with this email already exists'
      } else if (authError.message.includes('Password')) {
        errorMessage = 'Password does not meet requirements'
      } else if (authError.message.includes('rate limit')) {
        errorMessage = 'Too many registration attempts. Please try again later'
      }
      
      return NextResponse.json(
        { success: false, error: errorMessage },
        { status: 400 }
      )
    }

    if (!authData.user) {
      return NextResponse.json(
        { success: false, error: 'Registration failed - no user created' },
        { status: 400 }
      )
    }

    // Check if email confirmation is required
    const emailConfirmationRequired = !authData.session

    // If we have a session, user is auto-confirmed (configured in Supabase)
    if (authData.session) {
      // Update profile with additional data using service role
      try {
        const serviceClient = createServiceRoleClient()
        await serviceClient
          .from('profiles')
          .update({
            full_name: displayName || null,
          })
          .eq('id', authData.user.id)
      } catch (updateError) {
        console.error('[Auth Register] Profile update error:', updateError)
        // Don't fail registration if profile update fails
      }

      console.log(`[Auth Register] User ${email} registered and logged in`)

      return NextResponse.json({
        success: true,
        token: authData.session.access_token,
        refreshToken: authData.session.refresh_token,
        expiresAt: authData.session.expires_at,
        user: {
          id: authData.user.id,
          email: authData.user.email || email,
          displayName: displayName || null,
          avatarUrl: null,
          role: 'user',
          createdAt: authData.user.created_at,
        }
      })
    }

    // Email confirmation required
    console.log(`[Auth Register] User ${email} registered - email confirmation required`)

    return NextResponse.json({
      success: true,
      emailConfirmationRequired: true,
      user: {
        id: authData.user.id,
        email: authData.user.email || email,
        displayName: displayName || null,
        avatarUrl: null,
        role: 'user',
        createdAt: authData.user.created_at,
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
