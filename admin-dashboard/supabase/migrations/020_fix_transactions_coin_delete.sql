-- ============================================================================
-- Fix transactions -> coins delete behavior
-- ============================================================================
-- Transaction history should remain intact even when an admin deletes an
-- uncollected coin from the dashboard. The transaction row can keep its
-- financial metadata while dropping the direct coin reference.
-- ============================================================================

DO $$
DECLARE
  existing_constraint_name TEXT;
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = 'public'
      AND table_name = 'transactions'
  ) THEN
    RETURN;
  END IF;

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
    AND tc.table_name = 'transactions'
    AND kcu.column_name = 'coin_id'
    AND ccu.table_schema = 'public'
    AND ccu.table_name = 'coins'
    AND ccu.column_name = 'id'
  LIMIT 1;

  IF existing_constraint_name IS NOT NULL THEN
    EXECUTE format(
      'ALTER TABLE public.transactions DROP CONSTRAINT %I',
      existing_constraint_name
    );
  END IF;

  ALTER TABLE public.transactions
    ADD CONSTRAINT transactions_coin_id_fkey
    FOREIGN KEY (coin_id)
    REFERENCES public.coins(id)
    ON DELETE SET NULL;
END $$;
