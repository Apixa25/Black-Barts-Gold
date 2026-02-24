# AGENTS.md

## Cursor Cloud specific instructions

### Repository overview

This is a monorepo for **Black Bart's Gold**, an AR treasure hunting mobile game. It contains:

| Component | Path | Stack | Runnable on Cloud VM? |
|-----------|------|-------|-----------------------|
| Admin Dashboard (web) | `admin-dashboard/` | Next.js 16, React 19, TypeScript, Tailwind CSS, Supabase | Yes |
| Unity Mobile App | `BlackBartsGold/` | Unity 6 (6000.x LTS), C#, AR Foundation 6.x | No (requires Unity Editor GUI) |
| Browser Tools Extension | `BrowserTools-MCP-Extension/` | Chrome Extension (JS) | Optional |

Only the **admin dashboard** can be developed and run in this VM environment.

### Running the admin dashboard

```bash
cd admin-dashboard
npm run dev        # starts dev server on port 3000
npm run build      # production build
npm run lint       # eslint (pre-existing warnings/errors exist)
```

### Environment variables

The following secrets must be injected as environment variables (configured in Cursor Cloud Secrets):

- `NEXT_PUBLIC_SUPABASE_URL` — Supabase project URL
- `NEXT_PUBLIC_SUPABASE_ANON_KEY` — Supabase anonymous key
- `SUPABASE_SERVICE_ROLE_KEY` — Supabase service role key
- `NEXT_PUBLIC_MAPBOX_TOKEN` — Mapbox API token (for map features)
- `NEXT_PUBLIC_APP_URL` — Application URL

The update script writes these into `admin-dashboard/.env.local` at startup. If you add new env vars, update the script accordingly.

### Key caveats

- **Lint has pre-existing issues**: `npm run lint` exits with code 1 due to ~27 errors and ~51 warnings already in the codebase. These are not regressions.
- **Auth required for dashboard pages**: All routes except `/login` are protected by Supabase Auth middleware. Without valid credentials, you'll be redirected to `/login`. Test account credentials are needed to access the full dashboard.
- **Next.js 16 middleware deprecation warning**: The build/dev server shows a warning about the `middleware` file convention being deprecated in favor of `proxy`. This is informational only and doesn't affect functionality.
- **API routes work without auth for some endpoints**: `GET /api/v1/coins/nearby` works unauthenticated and can be used to verify Supabase connectivity.
- **Unity project**: Cannot be built or tested in this environment. Only the `admin-dashboard/` is in scope for cloud agent work.

### Useful API test commands

```bash
# Verify Supabase connectivity (no auth needed)
curl "localhost:3000/api/v1/coins/nearby?lat=37.7749&lng=-122.4194&radius=1000"

# Verify auth rejection
curl localhost:3000/api/v1/auth/me
```

### Documentation references

- Brand guide: `Docs/brand-guide.md`
- Project vision: `Docs/project-vision.md`
- Admin dashboard build guide: `Docs/ADMIN-DASHBOARD-BUILD-GUIDE.md`
- Unity build guide: `Docs/BUILD-GUIDE.md`
