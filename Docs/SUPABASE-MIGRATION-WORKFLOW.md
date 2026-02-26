# Supabase Migration Workflow

This is the standard migration process for Black Bart's Gold.

Use this guide any time we add/modify database schema (tables, columns, constraints, policies, storage setup, etc.).

## Why this guide exists

- Keeps migrations consistent and low-risk.
- Makes it easy to ask the agent: "follow `Docs/SUPABASE-MIGRATION-WORKFLOW.md`."
- Matches this project's Supabase CLI setup and folder layout.

## Project paths

- Supabase project config: `admin-dashboard/supabase/config.toml`
- Migration files: `admin-dashboard/supabase/migrations/`
- API routes that usually need updates:
  - `admin-dashboard/src/app/api/v1/**`
  - `admin-dashboard/src/types/database.ts`

## Prerequisites (one-time per machine/session)

Run in PowerShell from `admin-dashboard/`:

```powershell
cd admin-dashboard
npm run supabase:login
npm run supabase:link
```

Notes:
- `login` and `link` are interactive and may need to be run by you in a normal terminal.
- If already linked, you can skip re-linking.

## Standard migration flow

### 1) Create migration file

Add a new SQL file in `admin-dashboard/supabase/migrations/` with an incremental prefix:

```text
012_some_descriptive_name.sql
```

Guidelines:
- Prefer `IF NOT EXISTS` for additive safety.
- Canonical operational policy: `.cursor/rules/proactive-support-defaults.mdc`. For migrations specifically, prioritize reliability, simplicity, and rollback safety.
- Add clear comments at the top.

### 2) Update app/API types and routes

Typical follow-up changes:
- `admin-dashboard/src/types/database.ts`
- API routes that read/write the changed fields
- Unity-side request/response models if mobile app depends on those fields

### 3) Validate migration locally (recommended)

```powershell
cd admin-dashboard
npm run supabase:status
```

Optional local DB workflow:

```powershell
cd admin-dashboard
npm run supabase:start
npm run supabase:db:reset
```

### 4) Push migration to linked remote project

```powershell
cd admin-dashboard
npm run supabase:db:push
```

### 5) Verify migration applied

Option A: CLI/schema pull

```powershell
cd admin-dashboard
npm run supabase:db:pull
```

Option B: Supabase Dashboard
- Open Table Editor and confirm new columns/constraints exist.
- Test read/write from API route(s).

### 6) Smoke test app behavior

- Hit affected API endpoint(s) manually or via app flow.
- Verify both success and validation failure paths.
- Confirm no regressions in auth/session behavior.

## Storage bucket changes (when needed)

Storage buckets are usually created in server code with service role or in dashboard:

- Preferred for this project: create/check bucket in server route with service role.
- Keep uploads size-limited and validate MIME types.
- Store only URL/path in DB, not raw image data.

## Safety checklist before merge

- Migration is additive and reversible when possible.
- API + type updates included.
- Validation rules added for new fields.
- Existing users/data paths still work.
- Lints pass on changed files.

## Common commands quick reference

```powershell
cd admin-dashboard
npm run supabase:status
npm run supabase:db:push
npm run supabase:db:pull
```

## Current profile migration reference

For the profile upgrade (age/phone/avatar preset):

- SQL migration: `admin-dashboard/supabase/migrations/013_profiles_contact_and_age.sql`
- Profile API route: `admin-dashboard/src/app/api/v1/user/profile/route.ts`

