# 🗄️ Supabase Setup - Black Bart's Gold

This document tracks Supabase project configuration. 

> ⚠️ **SECURITY**: Never store passwords or secret keys in this file! This file is committed to git.

---

## 📋 Project Information

| Setting | Value |
|---------|-------|
| **Project Name** | Black Barts Gold |
| **Project ID** | `gvkfiommpbugvxwuloea` |
| **Region** | (check your dashboard) |
| **Project URL** | `https://gvkfiommpbugvxwuloea.supabase.co` |

---

## 🔐 Where to Find Credentials

1. Go to: https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/settings/api
2. Copy the **anon public** key → paste into `.env.local` as `NEXT_PUBLIC_SUPABASE_ANON_KEY`
3. Copy the **service_role** key → paste into `.env.local` as `SUPABASE_SERVICE_ROLE_KEY`

---

## 📁 Environment Files

### `admin-dashboard/.env.local` (NOT in git)

This file contains your actual secret keys. It's in `.gitignore` and should never be committed.

```env
NEXT_PUBLIC_SUPABASE_URL=https://gvkfiommpbugvxwuloea.supabase.co
NEXT_PUBLIC_SUPABASE_ANON_KEY=your-anon-key-here
SUPABASE_SERVICE_ROLE_KEY=your-service-role-key-here
```

---

## 🗃️ Database Schema

After setting up credentials, run this SQL in Supabase SQL Editor to create the database tables.

### Go to: SQL Editor
https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/sql/new

### Run this SQL:

```sql
-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================
-- User Profiles Table
-- =============================================
CREATE TABLE public.profiles (
  id UUID REFERENCES auth.users(id) ON DELETE CASCADE PRIMARY KEY,
  email TEXT UNIQUE NOT NULL,
  full_name TEXT,
  role TEXT DEFAULT 'user' CHECK (role IN ('super_admin', 'sponsor_admin', 'user')),
  avatar_url TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable Row Level Security
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;

-- =============================================
-- RLS Policies for profiles
-- =============================================

-- Users can view their own profile
CREATE POLICY "Users can view own profile" ON public.profiles
  FOR SELECT USING (auth.uid() = id);

-- Users can update their own profile
CREATE POLICY "Users can update own profile" ON public.profiles
  FOR UPDATE USING (auth.uid() = id);

-- Admins can view all profiles
CREATE POLICY "Admins can view all profiles" ON public.profiles
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Admins can update all profiles
CREATE POLICY "Admins can update all profiles" ON public.profiles
  FOR UPDATE USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- =============================================
-- Function to create profile on signup
-- =============================================
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name)
  VALUES (NEW.id, NEW.email, NEW.raw_user_meta_data->>'full_name');
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Trigger for new user signup
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- =============================================
-- Function to update updated_at timestamp
-- =============================================
CREATE OR REPLACE FUNCTION public.update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger for profiles updated_at
CREATE TRIGGER profiles_updated_at
  BEFORE UPDATE ON public.profiles
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();
```

---

## 🪙 Phase 3: Coins Schema

After setting up profiles, run this SQL to add the coins table:

### Go to: SQL Editor
https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/sql/new

### Run this SQL:

```sql
-- =============================================
-- Coins Table - Phase 3
-- =============================================
CREATE TABLE public.coins (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  
  -- Coin Type & Value
  coin_type TEXT DEFAULT 'fixed' CHECK (coin_type IN ('fixed', 'pool')),
  value DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
  tier TEXT DEFAULT 'gold' CHECK (tier IN ('gold', 'silver', 'bronze')),
  is_mythical BOOLEAN DEFAULT false,
  
  -- Location
  latitude DECIMAL(10, 7) NOT NULL,
  longitude DECIMAL(10, 7) NOT NULL,
  location_name TEXT, -- Optional friendly name like "Central Park"
  
  -- Status
  status TEXT DEFAULT 'hidden' CHECK (status IN ('hidden', 'visible', 'collected', 'expired', 'recycled')),
  
  -- Ownership & Collection
  hider_id UUID REFERENCES public.profiles(id),
  hidden_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  collected_by UUID REFERENCES public.profiles(id),
  collected_at TIMESTAMP WITH TIME ZONE,
  
  -- Sponsor Integration
  sponsor_id UUID, -- Will reference sponsors table later
  logo_url TEXT,
  
  -- Multi-find tracking
  multi_find BOOLEAN DEFAULT false,
  finds_remaining INTEGER DEFAULT 1,
  
  -- Metadata
  description TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable Row Level Security
ALTER TABLE public.coins ENABLE ROW LEVEL SECURITY;

-- =============================================
-- RLS Policies for coins
-- =============================================

-- Anyone authenticated can view coins
CREATE POLICY "Authenticated users can view coins" ON public.coins
  FOR SELECT TO authenticated USING (true);

-- Super admins can do everything with coins
CREATE POLICY "Admins can manage all coins" ON public.coins
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Sponsor admins can manage their own coins
CREATE POLICY "Sponsor admins can manage own coins" ON public.coins
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'sponsor_admin'
    )
    AND hider_id = auth.uid()
  );

-- Users can hide coins (insert)
CREATE POLICY "Users can hide coins" ON public.coins
  FOR INSERT TO authenticated
  WITH CHECK (hider_id = auth.uid());

-- Users can update their own uncollected coins
CREATE POLICY "Users can update own uncollected coins" ON public.coins
  FOR UPDATE USING (
    hider_id = auth.uid() AND status IN ('hidden', 'visible')
  );

-- Trigger for coins updated_at
CREATE TRIGGER coins_updated_at
  BEFORE UPDATE ON public.coins
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();

-- =============================================
-- Create indexes for performance
-- =============================================
CREATE INDEX idx_coins_status ON public.coins(status);
CREATE INDEX idx_coins_hider ON public.coins(hider_id);
CREATE INDEX idx_coins_collected_by ON public.coins(collected_by);
CREATE INDEX idx_coins_location ON public.coins(latitude, longitude);
CREATE INDEX idx_coins_tier ON public.coins(tier);
```

---

## 💰 Phase 4: Transactions Schema

After setting up coins, run this SQL to add the transactions table:

### Go to: SQL Editor
https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/sql/new

### Run this SQL:

```sql
-- =============================================
-- Transactions Table - Phase 4
-- =============================================
CREATE TABLE public.transactions (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  
  -- User who made the transaction
  user_id UUID REFERENCES public.profiles(id) NOT NULL,
  
  -- Transaction details
  transaction_type TEXT NOT NULL CHECK (transaction_type IN (
    'deposit',      -- User purchased BBG
    'found',        -- Coin collected
    'hidden',       -- Coin placed (deducted from balance)
    'gas_consumed', -- Daily gas fee
    'transfer_in',  -- Received from another user
    'transfer_out', -- Sent to another user
    'payout'        -- Withdrawal/cashout
  )),
  
  -- Amount (positive for income, stored as absolute value)
  amount DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
  balance_after DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
  
  -- Related entities
  coin_id UUID REFERENCES public.coins(id),
  related_user_id UUID REFERENCES public.profiles(id), -- For transfers
  
  -- Description and status
  description TEXT,
  status TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'failed', 'cancelled')),
  
  -- Flexible metadata (JSON)
  metadata JSONB DEFAULT '{}',
  
  -- Timestamps
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  confirmed_at TIMESTAMP WITH TIME ZONE
);

-- Enable Row Level Security
ALTER TABLE public.transactions ENABLE ROW LEVEL SECURITY;

-- =============================================
-- RLS Policies for transactions
-- =============================================

-- Users can view their own transactions
CREATE POLICY "Users can view own transactions" ON public.transactions
  FOR SELECT USING (auth.uid() = user_id);

-- Super admins can view all transactions
CREATE POLICY "Admins can view all transactions" ON public.transactions
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Super admins can manage all transactions
CREATE POLICY "Admins can manage all transactions" ON public.transactions
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Users can create their own transactions (for hiding coins, etc.)
CREATE POLICY "Users can create own transactions" ON public.transactions
  FOR INSERT TO authenticated
  WITH CHECK (user_id = auth.uid());

-- =============================================
-- Indexes for performance
-- =============================================
CREATE INDEX idx_transactions_user ON public.transactions(user_id);
CREATE INDEX idx_transactions_type ON public.transactions(transaction_type);
CREATE INDEX idx_transactions_status ON public.transactions(status);
CREATE INDEX idx_transactions_created ON public.transactions(created_at DESC);
CREATE INDEX idx_transactions_coin ON public.transactions(coin_id);
```

---

## 🏢 Phase 5: Sponsors Schema

After setting up transactions, run this SQL to add the sponsors table:

### Go to: SQL Editor
https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/sql/new

### Run this SQL:

```sql
-- =============================================
-- Sponsors Table - Phase 5
-- =============================================
CREATE TABLE public.sponsors (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  
  -- Company info
  company_name TEXT NOT NULL,
  contact_name TEXT,
  contact_email TEXT NOT NULL,
  contact_phone TEXT,
  logo_url TEXT,
  website_url TEXT,
  description TEXT,
  
  -- Financial tracking
  total_spent DECIMAL(10, 2) DEFAULT 0.00,
  coins_purchased INTEGER DEFAULT 0,
  coins_collected INTEGER DEFAULT 0,
  
  -- Status
  status TEXT DEFAULT 'pending' CHECK (status IN ('active', 'inactive', 'pending')),
  
  -- Associated admin user (sponsor_admin role)
  admin_user_id UUID REFERENCES public.profiles(id),
  
  -- Timestamps
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable Row Level Security
ALTER TABLE public.sponsors ENABLE ROW LEVEL SECURITY;

-- =============================================
-- RLS Policies for sponsors
-- =============================================

-- Super admins can do everything
CREATE POLICY "Admins can manage all sponsors" ON public.sponsors
  FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- Sponsor admins can view their own sponsor
CREATE POLICY "Sponsor admins can view own sponsor" ON public.sponsors
  FOR SELECT USING (admin_user_id = auth.uid());

-- Sponsor admins can update their own sponsor (limited fields)
CREATE POLICY "Sponsor admins can update own sponsor" ON public.sponsors
  FOR UPDATE USING (admin_user_id = auth.uid());

-- Trigger for updated_at
CREATE TRIGGER sponsors_updated_at
  BEFORE UPDATE ON public.sponsors
  FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();

-- =============================================
-- Indexes for performance
-- =============================================
CREATE INDEX idx_sponsors_status ON public.sponsors(status);
CREATE INDEX idx_sponsors_admin ON public.sponsors(admin_user_id);
CREATE INDEX idx_sponsors_company ON public.sponsors(company_name);

-- =============================================
-- Update coins table to reference sponsors
-- =============================================
-- Add foreign key constraint to coins.sponsor_id
ALTER TABLE public.coins
  ADD CONSTRAINT fk_coins_sponsor
  FOREIGN KEY (sponsor_id) REFERENCES public.sponsors(id)
  ON DELETE SET NULL;
```

---

## 🔒 Phase 6: Activity Logs Schema

After setting up sponsors, run this SQL to add security monitoring:

### Go to: SQL Editor
https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/sql/new

### Run this SQL:

```sql
-- =============================================
-- Activity Logs Table - Phase 6
-- =============================================
CREATE TABLE public.activity_logs (
  id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
  
  -- Who performed the action (null for system events)
  user_id UUID REFERENCES public.profiles(id),
  
  -- What happened
  activity_type TEXT NOT NULL CHECK (activity_type IN (
    'login',
    'logout', 
    'login_failed',
    'password_changed',
    'profile_updated',
    'role_changed',
    'coin_created',
    'coin_collected',
    'coin_deleted',
    'sponsor_created',
    'sponsor_updated',
    'transaction_created',
    'payout_requested',
    'suspicious_activity',
    'admin_action'
  )),
  
  -- Severity level
  severity TEXT DEFAULT 'info' CHECK (severity IN ('info', 'warning', 'error', 'critical')),
  
  -- Human-readable description
  description TEXT NOT NULL,
  
  -- Request info
  ip_address INET,
  user_agent TEXT,
  
  -- Additional data (JSON)
  metadata JSONB DEFAULT '{}',
  
  -- Timestamp
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable Row Level Security
ALTER TABLE public.activity_logs ENABLE ROW LEVEL SECURITY;

-- =============================================
-- RLS Policies for activity_logs
-- =============================================

-- Only super admins can view activity logs
CREATE POLICY "Admins can view all activity logs" ON public.activity_logs
  FOR SELECT USING (
    EXISTS (
      SELECT 1 FROM public.profiles
      WHERE id = auth.uid() AND role = 'super_admin'
    )
  );

-- System can insert logs (via service role)
CREATE POLICY "System can insert activity logs" ON public.activity_logs
  FOR INSERT WITH CHECK (true);

-- =============================================
-- Indexes for performance
-- =============================================
CREATE INDEX idx_activity_user ON public.activity_logs(user_id);
CREATE INDEX idx_activity_type ON public.activity_logs(activity_type);
CREATE INDEX idx_activity_severity ON public.activity_logs(severity);
CREATE INDEX idx_activity_created ON public.activity_logs(created_at DESC);

-- =============================================
-- Insert some sample activity for testing
-- =============================================
INSERT INTO public.activity_logs (activity_type, severity, description, metadata) VALUES
  ('admin_action', 'info', 'Admin dashboard deployed', '{"version": "1.0.0"}'),
  ('login', 'info', 'System initialized', '{"event": "startup"}');
```

---

## 👤 Create Your First Admin User

After running the schema:

1. Go to **Authentication** in Supabase dashboard
2. Click **Add user** → **Create new user**
3. Enter your email and a password
4. After creating, go to **Table Editor** → **profiles**
5. Find your user and change `role` from `user` to `super_admin`

---

## ✅ Setup Checklist

- [ ] Supabase project created
- [ ] Project URL copied to `.env.local`
- [ ] Anon key copied to `.env.local`
- [ ] Service role key copied to `.env.local`
- [ ] Database schema SQL executed
- [ ] First admin user created
- [ ] Admin role updated in profiles table
- [ ] Login tested in dashboard

---

## 🔗 Useful Links

- **Dashboard**: https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea
- **SQL Editor**: https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/sql/new
- **Auth Users**: https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/auth/users
- **Table Editor**: https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/editor
- **API Settings**: https://supabase.com/dashboard/project/gvkfiommpbugvxwuloea/settings/api

---

*Document created: January 22, 2026*
