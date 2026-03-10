-- ============================================================================
-- Fix spawn_queue -> coins delete behavior
-- ============================================================================
-- Auto-spawned coins can be referenced by spawn_queue.spawned_coin_id after the
-- queue item is marked completed. Deleting those coins from the admin dashboard
-- should not fail with a foreign-key conflict, because spawn_history already
-- preserves the analytics trail and uses ON DELETE SET NULL.
--
-- This migration updates the spawn_queue foreign key so deleting a coin simply
-- nulls the historical queue reference instead of blocking the delete.
-- ============================================================================

DO $$
DECLARE
  existing_constraint_name TEXT;
BEGIN
  SELECT tc.constraint_name
  INTO existing_constraint_name
  FROM information_schema.table_constraints AS tc
  JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
   AND tc.table_schema = kcu.table_schema
  JOIN information_schema.constraint_column_usage AS ccu
    ON tc.constraint_name = ccu.constraint_name
   AND tc.table_schema = ccu.table_schema
  WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_schema = 'public'
    AND tc.table_name = 'spawn_queue'
    AND kcu.column_name = 'spawned_coin_id'
    AND ccu.table_schema = 'public'
    AND ccu.table_name = 'coins'
    AND ccu.column_name = 'id'
  LIMIT 1;

  IF existing_constraint_name IS NOT NULL THEN
    EXECUTE format(
      'ALTER TABLE public.spawn_queue DROP CONSTRAINT %I',
      existing_constraint_name
    );
  END IF;

  ALTER TABLE public.spawn_queue
    ADD CONSTRAINT spawn_queue_spawned_coin_id_fkey
    FOREIGN KEY (spawned_coin_id)
    REFERENCES public.coins(id)
    ON DELETE SET NULL;
END $$;
