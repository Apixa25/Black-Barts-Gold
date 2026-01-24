---
name: supabase-setup
description: Setup and use Supabase CLI for the Black Bart's Gold admin-dashboard. Handles login, linking to remote project, migrations, Realtime, and troubleshooting. Use when the user wants to use Supabase, run migrations, enable Realtime, link the project, or work with the Supabase CLI.
---

# Supabase Setup

## Scope

- **Project**: Black Bart's Gold admin-dashboard
- **CLI**: Installed as dev dependency in `admin-dashboard/`
- **Config**: `admin-dashboard/supabase/config.toml`
- **Migrations**: `admin-dashboard/supabase/migrations/`

All commands run from `admin-dashboard/`. Use PowerShell; avoid `&&`—use `;` or separate commands.

## Quick Reference

| Task | Command |
|------|---------|
| Login | `npm run supabase:login` |
| Link project | `npm run supabase:link` |
| Status | `npm run supabase:status` |
| Push migrations | `npm run supabase:db:push` |
| Pull schema | `npm run supabase:db:pull` |
| Reset local DB | `npm run supabase:db:reset` |
| Start local (Docker) | `npm run supabase:start` |
| Stop local | `npm run supabase:stop` |

## One-Time Setup (User Terminal)

Login and link require an interactive terminal (browser auth). The agent cannot run these in non-TTY environments.

**User must run:**
```powershell
cd admin-dashboard
npm run supabase:login
npm run supabase:link
```
Select org and project (e.g. Black Bart's Gold). After linking, the agent can use `db:push`, `db:pull`, etc.

## Enable Realtime (SQL)

Realtime is enabled via Supabase Dashboard → SQL Editor, not the CLI. Run:

```sql
ALTER PUBLICATION supabase_realtime ADD TABLE public.player_locations;
ALTER PUBLICATION supabase_realtime ADD TABLE public.cheat_flags;
```

"Success, no rows returned" is expected.

## Migration Order (If Running Manually)

If applying migrations manually in SQL Editor, run in order:
1. M4: `003_player_locations.sql` (creates `player_locations`)
2. M8: `007_anti_cheat.sql` (uses `player_locations`)

M4 has a zones dependency; use the modified M4 (no `zones` FK) if `zones` does not exist yet.

## Troubleshooting

| Error | Action |
|-------|--------|
| "Not logged in" | User runs `npm run supabase:login` in their terminal |
| "Project not linked" | User runs `npm run supabase:link` in their terminal |
| "Cannot use automatic login flow" | Non-TTY: user runs login/link locally, or sets `SUPABASE_ACCESS_TOKEN` |
| "relation player_locations does not exist" | Run M4 (`003_player_locations.sql`) before M8 |
| "relation zones does not exist" | Use M4 variant without `zones` FK |

## Additional Resources

- Full setup guide: `admin-dashboard/SUPABASE-SETUP.md`
- Map integration (Realtime, M4/M8): `Docs/MAP-INTEGRATION-PROGRESS.md`
