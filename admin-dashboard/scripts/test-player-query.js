/**
 * Test player_locations query to see actual error
 * Run: node scripts/test-player-query.js
 */

const { createClient } = require('@supabase/supabase-js')
const fs = require('fs')
const path = require('path')

// Load .env.local
const envPath = path.join(__dirname, '..', '.env.local')
const envContent = fs.readFileSync(envPath, 'utf-8')
const envVars = {}

envContent.split('\n').forEach(line => {
  line = line.trim()
  if (!line || line.startsWith('#')) return
  const match = line.match(/^([A-Z_]+)=(.*)$/)
  if (match) {
    const key = match[1].trim()
    let value = match[2].trim()
    if ((value.startsWith('"') && value.endsWith('"')) || 
        (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1)
    }
    envVars[key] = value
  }
})

const supabaseUrl = envVars.NEXT_PUBLIC_SUPABASE_URL
const supabaseAnonKey = envVars.NEXT_PUBLIC_SUPABASE_ANON_KEY

if (!supabaseUrl || !supabaseAnonKey) {
  console.error('❌ Missing environment variables!')
  process.exit(1)
}

// Use anon key (like the frontend does)
const supabase = createClient(supabaseUrl, supabaseAnonKey)

async function testQuery() {
  console.log('🔍 Testing player_locations query...\n')
  
  // Test 1: Simple select
  console.log('Test 1: Simple SELECT *')
  const { data: data1, error: error1 } = await supabase
    .from('player_locations')
    .select('*')
    .limit(1)
  
  if (error1) {
    console.error('❌ Error:', error1.message)
    console.error('   Code:', error1.code)
    console.error('   Details:', error1.details)
    console.error('   Hint:', error1.hint)
  } else {
    console.log('✅ Success! Found', data1?.length || 0, 'rows')
  }
  
  // Test 2: Check if table exists
  console.log('\nTest 2: Check table exists')
  const { data: data2, error: error2 } = await supabase
    .rpc('check_table_exists', { table_name: 'player_locations' })
    .single()
  
  if (error2 && !error2.message.includes('function')) {
    console.log('   (RPC function not available, skipping)')
  }
  
  // Test 3: Check RLS
  console.log('\nTest 3: Check current user')
  const { data: { user } } = await supabase.auth.getUser()
  console.log('   User:', user ? user.email : 'Not authenticated')
  
  if (user) {
    // Check profile
    const { data: profile } = await supabase
      .from('profiles')
      .select('id, email, role')
      .eq('id', user.id)
      .single()
    
    console.log('   Profile role:', profile?.role || 'NULL')
  }
}

testQuery()
  .then(() => {
    console.log('\n✨ Done!')
    process.exit(0)
  })
  .catch((err) => {
    console.error('❌ Error:', err)
    process.exit(1)
  })
