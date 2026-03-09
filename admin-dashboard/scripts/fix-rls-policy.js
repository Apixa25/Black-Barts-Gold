/**
 * Fix RLS policy for player_locations using service role
 * Run: node scripts/fix-rls-policy.js
 */

async function loadEnv() {
  const fs = await import('node:fs/promises')
  const path = await import('node:path')
  const envPath = path.join(process.cwd(), '.env.local')
  const envContent = await fs.readFile(envPath, 'utf-8')
  const envVars = {}

  envContent.split('\n').forEach((rawLine) => {
    const line = rawLine.trim()
    if (!line || line.startsWith('#')) return
    const match = line.match(/^([A-Z_]+)=(.*)$/)
    if (!match) return

    const key = match[1].trim()
    let value = match[2].trim()
    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1)
    }
    envVars[key] = value
  })

  return envVars
}

async function fixRLSPolicy() {
  const envVars = await loadEnv()
  const supabaseUrl = envVars.NEXT_PUBLIC_SUPABASE_URL
  const supabaseServiceKey = envVars.SUPABASE_SERVICE_ROLE_KEY

  if (!supabaseUrl || !supabaseServiceKey) {
    throw new Error('Missing NEXT_PUBLIC_SUPABASE_URL or SUPABASE_SERVICE_ROLE_KEY')
  }

  console.log('🔧 Fixing RLS policy for player_locations...\n')
  
  const sql = `
-- Create helper function
CREATE OR REPLACE FUNCTION public.is_super_admin(check_user_id UUID DEFAULT auth.uid())
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  RETURN EXISTS (
    SELECT 1 FROM public.profiles
    WHERE id = check_user_id
    AND role = 'super_admin'
  );
END;
$$;

-- Drop old policy
DROP POLICY IF EXISTS "Admins can view all player locations" ON public.player_locations;

-- Create new policy
CREATE POLICY "Admins can view all player locations" ON public.player_locations
  FOR SELECT USING (
    public.is_super_admin() OR
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'sponsor_admin'
    )
  );

-- Ensure users can see own location
DROP POLICY IF EXISTS "Users can view own location" ON public.player_locations;
CREATE POLICY "Users can view own location" ON public.player_locations
  FOR SELECT USING (auth.uid() = user_id);
`
  
  // Execute SQL via RPC (if available) or direct query
  // Note: Supabase JS client doesn't support raw SQL, so we'll use the REST API
  const response = await fetch(`${supabaseUrl}/rest/v1/rpc/exec_sql`, {
    method: 'POST',
    headers: {
      'apikey': supabaseServiceKey,
      'Authorization': `Bearer ${supabaseServiceKey}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ sql }),
  })
  
  if (!response.ok) {
    // Try alternative: use Supabase's SQL execution endpoint
    console.log('⚠️  Direct SQL execution not available via client')
    console.log('📋 Please run this SQL in Supabase Dashboard → SQL Editor:\n')
    console.log(sql)
    return
  }
  
  const result = await response.json()
  console.log('✅ RLS policy fixed!')
  console.log('Result:', result)
}

fixRLSPolicy()
  .then(() => {
    console.log('\n✨ Done!')
    process.exit(0)
  })
  .catch((err) => {
    console.error('❌ Error:', err)
    console.log('\n📋 Alternative: Run the SQL in Supabase Dashboard → SQL Editor')
    console.log('   File: admin-dashboard/fix-rls-policy.sql')
    process.exit(1)
  })
