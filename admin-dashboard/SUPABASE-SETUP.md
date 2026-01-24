# Supabase CLI Setup Guide

> **Skill**: This workflow is available as a Cursor skill. Reference `@.cursor/skills/supabase-setup` when working with Supabase so the agent can apply it automatically.

## Quick Setup Steps

### 1. Login to Supabase
Open PowerShell/Command Prompt in `admin-dashboard` folder and run:

```powershell
cd c:\Users\Admin\Black-Barts-Gold\admin-dashboard
npm run supabase:login
```

This will open your browser to authenticate. After logging in, you'll see a success message.

### 2. Link to Your Project
After logging in, run:

```powershell
npm run supabase:link
```

You'll be prompted to:
- Select your organization
- Select your project (e.g., "Black Bart's Gold" or similar)

### 3. Verify Connection
Check that you're linked:

```powershell
npm run supabase:status
```

## Using the CLI

### Push Migrations to Remote
After linking, you can push your local migrations:

```powershell
npm run supabase:db:push
```

This will apply all migrations in `supabase/migrations/` to your remote database.

### Enable Realtime via CLI
You can enable Realtime for tables:

```powershell
npx supabase db remote commit
```

Or use SQL directly in Supabase Dashboard (easier).

### Other Useful Commands

```powershell
# Pull remote schema as migration
npm run supabase:db:pull

# Generate TypeScript types from your schema
npx supabase gen types typescript --linked > src/types/database-generated.ts

# Check migration status
npx supabase migration list
```

## Troubleshooting

### "Not logged in" error
Run `npm run supabase:login` again.

### "Project not linked" error
Run `npm run supabase:link` and select your project.

### "Cannot use automatic login flow"
This means you're in a non-interactive environment. Either:
1. Run the command in your own terminal (recommended)
2. Get an access token from https://supabase.com/dashboard/account/tokens and use:
   ```powershell
   $env:SUPABASE_ACCESS_TOKEN="your-token-here"
   npm run supabase:login
   ```
