-- ============================================================================
-- Migration 016: Spawn Governor — pg_cron Schedule
--
-- Schedules the Spawn Governor Edge Function to run every 5 minutes using
-- pg_cron + pg_net. Requires both extensions to be enabled on the project.
--
-- IMPORTANT — READ BEFORE APPLYING:
--
-- 1. Deploy the Edge Function first:
--      supabase functions deploy spawn-governor --no-verify-jwt
--
-- 2. Set the required secrets:
--      supabase secrets set ADMIN_API_BASE_URL=https://your-bbg-admin.vercel.app
--      supabase secrets set AI_AGENT_API_KEY=your-secret-key
--
-- 3. Update the two placeholder values in this migration:
--      'YOUR_PROJECT_REF'    → your Supabase project ref (e.g. abcdefghijklmnop)
--      'YOUR_SERVICE_ROLE_KEY' → your project service role key (from Settings > API)
--
-- 4. Apply this migration:
--      supabase db push
--
-- To check the cron job is running:
--      SELECT * FROM cron.job;
--      SELECT * FROM cron.job_run_details ORDER BY start_time DESC LIMIT 10;
--
-- To remove the cron job:
--      SELECT cron.unschedule('spawn-governor-5min');
-- ============================================================================

-- Enable pg_cron (schedule jobs) and pg_net (make HTTP requests from SQL)
CREATE EXTENSION IF NOT EXISTS pg_cron;
CREATE EXTENSION IF NOT EXISTS pg_net;

-- ── Store the Edge Function URL as a database-level configuration setting ────
-- Update 'YOUR_PROJECT_REF' before applying this migration.
-- Format: https://<project_ref>.supabase.co
DO $$
BEGIN
  -- Only set if not already configured
  IF current_setting('app.spawn_governor_base_url', true) IS NULL
    OR current_setting('app.spawn_governor_base_url', true) = '' THEN
    PERFORM set_config(
      'app.spawn_governor_base_url',
      'https://YOUR_PROJECT_REF.supabase.co',
      false  -- not transaction-local (persists)
    );
  END IF;
END $$;

-- ── Schedule the governor to run every 5 minutes ─────────────────────────────
-- Remove the existing job first if it exists (idempotent re-run)
SELECT cron.unschedule('spawn-governor-5min')
WHERE EXISTS (SELECT 1 FROM cron.job WHERE jobname = 'spawn-governor-5min');

SELECT cron.schedule(
  'spawn-governor-5min',  -- job name
  '*/5 * * * *',          -- every 5 minutes (cron syntax)
  $$
    SELECT net.http_post(
      url := current_setting('app.spawn_governor_base_url', true)
             || '/functions/v1/spawn-governor',
      headers := jsonb_build_object(
        'Content-Type',  'application/json',
        'Authorization', 'Bearer YOUR_SERVICE_ROLE_KEY'
      ),
      body    := '{"trigger": "cron"}'::jsonb,
      timeout_milliseconds := 30000
    ) AS request_id;
  $$
);

-- ── Schedule a daily midnight recycle sweep ───────────────────────────────────
-- A full recycle pass every night at midnight to clean up any stragglers.
-- Uses a 48-hour age limit as the overnight catch-all.
SELECT cron.unschedule('spawn-governor-midnight-recycle')
WHERE EXISTS (SELECT 1 FROM cron.job WHERE jobname = 'spawn-governor-midnight-recycle');

SELECT cron.schedule(
  'spawn-governor-midnight-recycle',
  '0 0 * * *',  -- midnight every day
  $$
    SELECT net.http_post(
      url := current_setting('app.spawn_governor_base_url', true)
             || '/functions/v1/spawn-governor?trigger=midnight_recycle',
      headers := jsonb_build_object(
        'Content-Type',  'application/json',
        'Authorization', 'Bearer YOUR_SERVICE_ROLE_KEY'
      ),
      body    := '{"trigger": "midnight_recycle"}'::jsonb,
      timeout_milliseconds := 60000
    ) AS request_id;
  $$
);

-- ── Verify the jobs were created ─────────────────────────────────────────────
-- (SELECT only — no side effects, safe to run multiple times)
DO $$
DECLARE
  v_job_count INTEGER;
BEGIN
  SELECT COUNT(*) INTO v_job_count
  FROM cron.job
  WHERE jobname IN ('spawn-governor-5min', 'spawn-governor-midnight-recycle');

  IF v_job_count = 2 THEN
    RAISE NOTICE 'Migration 016: Both cron jobs created successfully ✓';
    RAISE NOTICE '  spawn-governor-5min: runs every 5 minutes';
    RAISE NOTICE '  spawn-governor-midnight-recycle: runs at midnight';
  ELSE
    RAISE WARNING 'Migration 016: Expected 2 cron jobs but found %. Check cron.job table.', v_job_count;
  END IF;
END $$;
