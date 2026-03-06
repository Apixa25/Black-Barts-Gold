-- ============================================================================
-- Migration: 014_ai_schema.sql
-- Purpose: Add AI agent audit trail and action logging infrastructure
-- Phase:   AI-1 (AI Integration Step 1)
-- Spec:    Docs/AI-INTEGRATION-SPEC.md — STEP 1
-- ============================================================================
-- This migration is PURELY ADDITIVE. No existing columns, constraints, or
-- policies are removed. All ALTER TABLE statements use IF NOT EXISTS.
-- Safe to run against a live database with existing data.
-- ============================================================================

-- ============================================================================
-- 1a. ADD created_by AND metadata TO coins
-- ============================================================================
-- created_by: lets us distinguish admin-placed vs AI-spawned vs user-hidden coins.
-- metadata:   lets AI agents attach context (hunt pressure, weather signal, etc.)

DO $$
BEGIN
  -- Add created_by if not already present
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name   = 'coins'
      AND column_name  = 'created_by'
  ) THEN
    ALTER TABLE public.coins
      ADD COLUMN created_by TEXT NOT NULL DEFAULT 'system'
        CHECK (created_by IN (
          'system',
          'admin',
          'user',
          'ai_spawn_governor',
          'ai_game_master',
          'ai_economy_balancer'
        ));
    COMMENT ON COLUMN public.coins.created_by IS
      'Who/what created this coin: system (auto-distribution), admin (dashboard), user (player), or an AI agent';
    RAISE NOTICE 'Migration 014: Added created_by to coins';
  ELSE
    RAISE NOTICE 'Migration 014: coins.created_by already exists — skipping';
  END IF;

  -- Add metadata if not already present
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name   = 'coins'
      AND column_name  = 'metadata'
  ) THEN
    ALTER TABLE public.coins ADD COLUMN metadata JSONB;
    COMMENT ON COLUMN public.coins.metadata IS
      'Optional AI agent context attached at spawn time: hunt_pressure, weather_signal, reasoning, etc.';
    RAISE NOTICE 'Migration 014: Added metadata to coins';
  ELSE
    RAISE NOTICE 'Migration 014: coins.metadata already exists — skipping';
  END IF;
END $$;

-- ============================================================================
-- 1b. ADD created_by TO spawn_history (if the table exists)
-- ============================================================================
-- Mirrors coins.created_by so we can audit every spawn in the history log.
-- Wrapped in a DO block — safe even if spawn_history doesn't exist on the target DB.

DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'spawn_history'
  ) THEN
    -- Add created_by column if not already present
    IF NOT EXISTS (
      SELECT 1 FROM information_schema.columns
      WHERE table_schema = 'public'
        AND table_name   = 'spawn_history'
        AND column_name  = 'created_by'
    ) THEN
      ALTER TABLE public.spawn_history
        ADD COLUMN created_by TEXT NOT NULL DEFAULT 'system'
          CHECK (created_by IN (
            'system',
            'admin',
            'user',
            'ai_spawn_governor',
            'ai_game_master',
            'ai_economy_balancer'
          ));

      COMMENT ON COLUMN public.spawn_history.created_by IS
        'Who/what triggered this spawn (mirrors coins.created_by)';

      RAISE NOTICE 'Migration 014: Added created_by to spawn_history';
    ELSE
      RAISE NOTICE 'Migration 014: spawn_history.created_by already exists — skipping';
    END IF;
  ELSE
    RAISE NOTICE 'Migration 014: spawn_history table not found — skipping created_by addition';
  END IF;
END $$;

-- ============================================================================
-- 1c. EXPAND spawn_queue.trigger_type CHECK TO INCLUDE AI VALUES
-- ============================================================================
-- The original inline CHECK was: ('auto', 'scheduled', 'manual', 'recycle')
-- We need to add 'ai_spawn_governor' and 'ai_game_master'.
-- Wrapped in a DO block — safe even if spawn_queue doesn't exist on the target DB.

DO $$
DECLARE
  v_constraint_name TEXT;
BEGIN
  -- Only proceed if spawn_queue exists
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'spawn_queue'
  ) THEN
    RAISE NOTICE 'Migration 014: spawn_queue table not found — skipping trigger_type expansion';
    RETURN;
  END IF;

  -- Find the existing trigger_type check constraint on spawn_queue
  SELECT conname INTO v_constraint_name
  FROM pg_constraint
  WHERE conrelid = 'public.spawn_queue'::regclass
    AND contype = 'c'
    AND pg_get_constraintdef(oid) LIKE '%trigger_type%';

  -- Drop it if found (so we can replace with expanded version)
  IF v_constraint_name IS NOT NULL THEN
    EXECUTE 'ALTER TABLE public.spawn_queue DROP CONSTRAINT ' || quote_ident(v_constraint_name);
    RAISE NOTICE 'Migration 014: Dropped spawn_queue trigger_type constraint: %', v_constraint_name;
  ELSE
    RAISE NOTICE 'Migration 014: No existing trigger_type constraint on spawn_queue — adding fresh';
  END IF;

  -- Add the expanded constraint
  ALTER TABLE public.spawn_queue
    ADD CONSTRAINT spawn_queue_trigger_type_check
      CHECK (trigger_type IN (
        'auto',
        'scheduled',
        'manual',
        'recycle',
        'ai_spawn_governor',
        'ai_game_master'
      ));

  COMMENT ON COLUMN public.spawn_queue.trigger_type IS
    'What caused this spawn: auto (zone minimum), scheduled (timed release), manual (admin), recycle (stale coin), or an AI agent';

  RAISE NOTICE 'Migration 014: spawn_queue trigger_type constraint expanded successfully';
END $$;

-- ============================================================================
-- 1d. CREATE ai_actions AUDIT LOG TABLE
-- ============================================================================
-- Every action taken by an AI agent is logged here.
-- This is the source of truth for the "What did Black Bart do today?" dashboard view.
-- It is also used by get_ai_spend_this_hour() to enforce the hourly spend cap.

CREATE TABLE IF NOT EXISTS public.ai_actions (
  id            UUID DEFAULT gen_random_uuid() PRIMARY KEY,

  -- Agent identity
  agent_id      TEXT NOT NULL
    CHECK (agent_id IN (
      'ai_spawn_governor',
      'ai_game_master',
      'ai_economy_balancer',
      'ai_churn_agent'
    )),

  -- What the agent did
  tool_called   TEXT NOT NULL,        -- e.g. 'spawn_coin', 'recycle_stale_coins'
  parameters    JSONB NOT NULL,       -- exact parameters passed to the tool
  reasoning     TEXT,                 -- AI's stated reason (stored from prompt response)

  -- What happened
  result        JSONB,                -- tool return value or error details
  success       BOOLEAN NOT NULL DEFAULT FALSE,
  error_code    TEXT,                 -- matches API error code strings (e.g. 'SPEND_LIMIT_EXCEEDED')

  -- Financial impact — critical for hourly spend cap enforcement
  cost_usd      DECIMAL(10, 4) NOT NULL DEFAULT 0,

  -- Timestamps
  created_at    TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Index for "What did the AI do today?" dashboard query
CREATE INDEX IF NOT EXISTS ai_actions_agent_time_idx
  ON public.ai_actions (agent_id, created_at DESC);

-- Index for hourly spend cap queries (get_ai_spend_this_hour uses this)
CREATE INDEX IF NOT EXISTS ai_actions_spend_idx
  ON public.ai_actions (tool_called, success, created_at DESC)
  WHERE tool_called = 'spawn_coin' AND success = TRUE;

-- General time-series index
CREATE INDEX IF NOT EXISTS ai_actions_time_idx
  ON public.ai_actions (created_at DESC);

-- Enable Row Level Security
ALTER TABLE public.ai_actions ENABLE ROW LEVEL SECURITY;

-- Only super admins can read the AI action log
DROP POLICY IF EXISTS "Admins can view AI actions" ON public.ai_actions;
CREATE POLICY "Admins can view AI actions" ON public.ai_actions
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Service role (used by API routes and Edge Functions) can insert and read everything
-- No explicit policy needed — service role bypasses RLS by default in Supabase

COMMENT ON TABLE public.ai_actions IS
  'Audit log of every action taken by AI agents. Source for the AI activity dashboard and hourly spend cap enforcement.';
COMMENT ON COLUMN public.ai_actions.agent_id    IS 'Which AI agent took this action';
COMMENT ON COLUMN public.ai_actions.tool_called IS 'Which MCP tool / API route was called';
COMMENT ON COLUMN public.ai_actions.reasoning   IS 'The AI agent''s stated reason for this action (from prompt response)';
COMMENT ON COLUMN public.ai_actions.cost_usd    IS 'USD value of coins spawned. Used to enforce autonomous spend limits.';

-- ============================================================================
-- 1e. ENABLE REALTIME ON coins AND spawn_history
-- ============================================================================
-- player_locations Realtime was already enabled in Migration 003.
-- coins Realtime lets the Spawn Governor react immediately when coins are collected.
-- spawn_history Realtime lets the admin dashboard update live when AI spawns coins.

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime'
      AND schemaname = 'public'
      AND tablename = 'coins'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.coins;
    RAISE NOTICE 'Migration 014: Added coins to supabase_realtime publication';
  ELSE
    RAISE NOTICE 'Migration 014: coins already in supabase_realtime — skipping';
  END IF;
END $$;

DO $$
BEGIN
  -- Only add spawn_history to Realtime if the table actually exists
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'spawn_history'
  ) THEN
    RAISE NOTICE 'Migration 014: spawn_history table not found — skipping Realtime setup';
  ELSIF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime'
      AND schemaname = 'public'
      AND tablename = 'spawn_history'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.spawn_history;
    RAISE NOTICE 'Migration 014: Added spawn_history to supabase_realtime publication';
  ELSE
    RAISE NOTICE 'Migration 014: spawn_history already in supabase_realtime — skipping';
  END IF;
END $$;

-- ============================================================================
-- 1f. get_ai_spend_this_hour() FUNCTION
-- ============================================================================
-- Returns the total USD value of successful AI coin spawns in the current clock hour.
-- Called by every spawn API route before executing to enforce the spend cap.
-- Cap value is defined in: admin-dashboard/src/lib/ai-guardrails.ts

CREATE OR REPLACE FUNCTION public.get_ai_spend_this_hour(
  p_agent_id TEXT DEFAULT NULL
)
RETURNS DECIMAL AS $$
DECLARE
  v_spend DECIMAL;
BEGIN
  SELECT COALESCE(SUM(cost_usd), 0)
  INTO v_spend
  FROM public.ai_actions
  WHERE success      = TRUE
    AND tool_called  = 'spawn_coin'
    AND created_at  >= DATE_TRUNC('hour', NOW())
    AND (p_agent_id IS NULL OR agent_id = p_agent_id);

  RETURN v_spend;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

COMMENT ON FUNCTION public.get_ai_spend_this_hour IS
  'Returns total USD spent by AI agents spawning coins in the current clock hour. Used by spawn routes to enforce the autonomous spend cap.';

-- ============================================================================
-- COMPLETION NOTICE
-- ============================================================================

DO $$
BEGIN
  RAISE NOTICE '=======================================================';
  RAISE NOTICE 'Migration 014: AI schema additions applied successfully.';
  RAISE NOTICE '  coins.created_by       — added';
  RAISE NOTICE '  coins.metadata         — added';
  RAISE NOTICE '  spawn_history.created_by — added';
  RAISE NOTICE '  spawn_queue trigger_type — expanded to include AI values';
  RAISE NOTICE '  ai_actions table       — created';
  RAISE NOTICE '  Realtime: coins        — enabled';
  RAISE NOTICE '  Realtime: spawn_history — enabled';
  RAISE NOTICE '  get_ai_spend_this_hour() — created';
  RAISE NOTICE '=======================================================';
END $$;
