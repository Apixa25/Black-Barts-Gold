/**
 * Check and fix user role for admin dashboard access
 * Run: node scripts/check-user-role.js
 * 
 * Uses service role key to bypass RLS and check/update user role
 */

const { createClient } = require('@supabase/supabase-js')
const fs = require('fs')
const path = require('path')

// Load .env.local - simple parser
const envPath = path.join(__dirname, '..', '.env.local')
const envContent = fs.readFileSync(envPath, 'utf-8')
const envVars = {}

envContent.split('\n').forEach(line => {
  line = line.trim()
  // Skip comments and empty lines
  if (!line || line.startsWith('#')) return
  
  // Match KEY=VALUE (with or without quotes)
  const match = line.match(/^([A-Z_]+)=(.*)$/)
  if (match) {
    const key = match[1].trim()
    let value = match[2].trim()
    // Remove surrounding quotes
    if ((value.startsWith('"') && value.endsWith('"')) || 
        (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1)
    }
    envVars[key] = value
  }
})

const supabaseUrl = envVars.NEXT_PUBLIC_SUPABASE_URL
const supabaseServiceKey = envVars.SUPABASE_SERVICE_ROLE_KEY

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
    console.error('   Details:', fetchError.details)
    console.error('   Hint:', fetchError.hint)
    return
  }
  
  if (!profile) {
    console.error('❌ Profile not found!')
    console.error('   Make sure the user has logged in at least once to create a profile.')
    return
  }
  
  console.log(`📋 Current role: ${profile.role || 'NULL'}`)
  console.log(`   User ID: ${profile.id}`)
  
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
      console.error('   Details:', updateError.details)
      return
    }
    
    console.log('✅ Role updated successfully!')
    console.log(`   New role: ${updated.role}`)
  } else {
    console.log('✅ Role is already set to super_admin!')
  }
  
  // Verify the fix
  console.log('\n🔍 Verifying fix...')
  const { data: verified } = await supabase
    .from('profiles')
    .select('email, role')
    .eq('email', email)
    .single()
  
  console.log(`   Final role: ${verified?.role}`)
}

checkAndFixRole()
  .then(() => {
    console.log('\n✨ Done! Try refreshing the dashboard now.')
    process.exit(0)
  })
  .catch((err) => {
    console.error('❌ Error:', err)
    process.exit(1)
  })
