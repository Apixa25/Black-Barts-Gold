// ============================================================================
// POST /api/v1/auth/login
// Black Bart's Gold - Mobile App Authentication
// ============================================================================
// Authenticates users via email/password for the Unity mobile app.
// Returns Supabase session token and user profile data.
// ============================================================================

import { NextRequest, NextResponse } from 'next/server'
import { createPublicClient } from '@/lib/supabase/server'

interface LoginRequest {
  email: string
  password: string
}

interface UserProfile {
  id: string
  email: string
  full_name: string | null
  role: string
  avatar_url: string | null
  created_at: string
  updated_at: string
}

interface LoginResponse {
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
}

export async function POST(request: NextRequest): Promise<NextResponse<LoginResponse>> {
  try {
    // Parse request body
    const body: LoginRequest = await request.json()
    const { email, password } = body

    // Validate input
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

    // Create Supabase client (public client for mobile auth)
    const supabase = createPublicClient()

    // Attempt to sign in
    const { data: authData, error: authError } = await supabase.auth.signInWithPassword({
      email,
      password,
    })

    if (authError) {
      console.error('[Auth Login] Supabase auth error:', authError.message)
      
      // Map common errors to user-friendly messages
      let errorMessage = 'Login failed'
      if (authError.message.includes('Invalid login credentials')) {
        errorMessage = 'Invalid email or password'
      } else if (authError.message.includes('Email not confirmed')) {
        errorMessage = 'Please verify your email before logging in'
      } else if (authError.message.includes('Too many requests')) {
        errorMessage = 'Too many login attempts. Please try again later'
      }
      
      return NextResponse.json(
        { success: false, error: errorMessage },
        { status: 401 }
      )
    }

    if (!authData.session || !authData.user) {
      return NextResponse.json(
        { success: false, error: 'Login failed - no session created' },
        { status: 401 }
      )
    }

    // Fetch user profile
    const { data: profile, error: profileError } = await supabase
      .from('profiles')
      .select('*')
      .eq('id', authData.user.id)
      .single()

    if (profileError) {
      console.error('[Auth Login] Profile fetch error:', profileError.message)
      // Don't fail login if profile fetch fails - profile might not exist yet
    }

    const userProfile = profile as UserProfile | null

    console.log(`[Auth Login] User ${email} logged in successfully`)

    return NextResponse.json({
      success: true,
      token: authData.session.access_token,
      refreshToken: authData.session.refresh_token,
      expiresAt: authData.session.expires_at,
      user: {
        id: authData.user.id,
        email: authData.user.email || email,
        displayName: userProfile?.full_name || authData.user.user_metadata?.full_name || null,
        avatarUrl: userProfile?.avatar_url || authData.user.user_metadata?.avatar_url || null,
        role: userProfile?.role || 'user',
        createdAt: authData.user.created_at,
      }
    })

  } catch (error) {
    console.error('[Auth Login] Unexpected error:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error' },
      { status: 500 }
    )
  }
}
