/**
 * Check and fix user role for admin dashboard access
 * Run: npx tsx scripts/check-user-role.ts
 */

import { createClient } from '@supabase/supabase-js'

const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL
const supabaseServiceKey = process.env.SUPABASE_SERVICE_ROLE_KEY

if (!supabaseUrl || !supabaseServiceKey) {
  console.error('❌ Missing environment variables!')
  console.error('Need: NEXT_PUBLIC_SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY')
  process.exit(1)
}

// Use service role key to bypass RLS
const supabase = createClient(supabaseUrl, supabaseServiceKey)

async function checkAndFixRole() {
  const email = 'stevensills2@gmail.com'
  
  console.log(`🔍 Checking role for: ${email}...`)
  
  // Check current role
  const { data: profile, error: fetchError } = await supabase
    .from('profiles')
    .select('id, email, role')
    .eq('email', email)
    .single()
  
  if (fetchError) {
    console.error('❌ Error fetching profile:', fetchError.message)
    return
  }
  
  if (!profile) {
    console.error('❌ Profile not found!')
    return
  }
  
  console.log(`📋 Current role: ${profile.role || 'NULL'}`)
  
  if (profile.role !== 'super_admin') {
    console.log('🔧 Updating role to super_admin...')
    
    const { data: updated, error: updateError } = await supabase
      .from('profiles')
      .update({ role: 'super_admin' })
      .eq('email', email)
      .select()
      .single()
    
    if (updateError) {
      console.error('❌ Error updating role:', updateError.message)
      return
    }
    
    console.log('✅ Role updated successfully!')
    console.log(`   New role: ${updated.role}`)
  } else {
    console.log('✅ Role is already set to super_admin!')
  }
}

checkAndFixRole()
  .then(() => {
    console.log('✨ Done!')
    process.exit(0)
  })
  .catch((err) => {
    console.error('❌ Error:', err)
    process.exit(1)
  })
