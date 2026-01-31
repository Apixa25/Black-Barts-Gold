// ============================================================================
// GET /api/v1/auth/me
// Black Bart's Gold - Token Validation & User Data
// ============================================================================
// Validates the access token and returns current user data.
// Used by Unity app to validate stored sessions on startup.
// ============================================================================

import { NextRequest, NextResponse } from 'next/server'
import { createPublicClient } from '@/lib/supabase/server'

interface MeResponse {
  success: boolean
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

export async function GET(request: NextRequest): Promise<NextResponse<MeResponse>> {
  try {
    // Get token from Authorization header
    const authHeader = request.headers.get('Authorization')
    
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return NextResponse.json(
        { success: false, error: 'No authorization token provided' },
        { status: 401 }
      )
    }

    const token = authHeader.replace('Bearer ', '')

    if (!token) {
      return NextResponse.json(
        { success: false, error: 'Invalid authorization token' },
        { status: 401 }
      )
    }

    // Create Supabase client
    const supabase = createPublicClient()

    // Validate token and get user
    const { data: userData, error: userError } = await supabase.auth.getUser(token)

    if (userError) {
      console.error('[Auth Me] Token validation error:', userError.message)
      
      let errorMessage = 'Invalid or expired token'
      if (userError.message.includes('expired')) {
        errorMessage = 'Session expired. Please login again'
      }
      
      return NextResponse.json(
        { success: false, error: errorMessage },
        { status: 401 }
      )
    }

    if (!userData.user) {
      return NextResponse.json(
        { success: false, error: 'User not found' },
        { status: 401 }
      )
    }

    // Fetch user profile
    const { data: profile, error: profileError } = await supabase
      .from('profiles')
      .select('*')
      .eq('id', userData.user.id)
      .single()

    if (profileError) {
      console.error('[Auth Me] Profile fetch error:', profileError.message)
      // Don't fail if profile doesn't exist
    }

    return NextResponse.json({
      success: true,
      user: {
        id: userData.user.id,
        email: userData.user.email || '',
        displayName: profile?.full_name || userData.user.user_metadata?.full_name || null,
        avatarUrl: profile?.avatar_url || userData.user.user_metadata?.avatar_url || null,
        role: profile?.role || 'user',
        createdAt: userData.user.created_at,
      }
    })

  } catch (error) {
    console.error('[Auth Me] Unexpected error:', error)
    return NextResponse.json(
      { success: false, error: 'Internal server error' },
      { status: 500 }
    )
  }
}
