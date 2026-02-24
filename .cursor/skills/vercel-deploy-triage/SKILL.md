---
name: vercel-deploy-triage
description: Diagnose and fix Vercel deployment failures for the Black Bart's Gold admin-dashboard using the Vercel CLI. Use when deployment fails, production is stale, build errors appear in Vercel, or when the user asks to inspect Vercel status/logs.
---

# Vercel Deploy Triage

## Scope

- **Project**: Black Bart's Gold admin dashboard (`admin-dashboard/`)
- **Framework**: Next.js (`next build`)
- **Primary goal**: explain why deployment failed and provide a safe fix path
- **Change style**: additive-first, low risk, preserve existing behavior unless requested

Run commands from `admin-dashboard/` with `npx vercel ...` (global install optional).

## Quick Commands

| Task | Command |
| ------ | --------- |
| Check CLI | `npx vercel --version` |
| Login | `npx vercel login` |
| Verify auth | `npx vercel whoami` |
| List projects | `npx vercel projects ls` |
| Link local dir | `npx vercel link` |
| List recent deploys | `npx vercel ls` |
| Inspect deployment | `npx vercel inspect <deployment-url-or-id>` |
| View deployment logs | `npx vercel logs <deployment-url-or-id>` |
| Pull env vars | `npx vercel env pull .env.local` |
| Test prod build locally | `npm run build` |

## Standard Workflow

1. **Authenticate**
   - Run `npx vercel whoami`.
   - If not authenticated, run `npx vercel login` and complete browser device auth.

2. **Confirm project linkage**
   - Run `npx vercel link` if `.vercel/project.json` is missing.
   - Verify local folder points to the expected Vercel project.

3. **Find the failed deployment**
   - Run `npx vercel ls`.
   - Identify the latest failed deployment (state/error summary).

4. **Inspect failure details**
   - Run `npx vercel inspect <id-or-url>`.
   - Run `npx vercel logs <id-or-url>`.
   - Capture root cause category:
     - missing environment variable
     - build-time TypeScript/ESLint error
     - runtime/server function crash
     - framework/config mismatch

5. **Reproduce locally before editing**
   - Run `npm run build`.
   - If local build passes but Vercel fails, prioritize env/config differences.

6. **Apply minimal safe fix**
   - Prefer additive updates (guards, defaults, config alignment, explicit env validation).
   - Avoid broad refactors while triaging production incidents.

7. **Verify**
   - Re-run `npm run build`.
   - Re-check lint only for touched files if needed.
   - Trigger a redeploy after confidence is high.

## Common Failure Playbook

### Missing environment variables

- Signals:
  - `undefined` in build logs
  - auth/DB client initialization failures
- Actions:
  1. `npx vercel env pull .env.local`
  2. compare required env usage in code vs available env keys
  3. add missing vars in Vercel project settings

### Next.js build failures

- Signals:
  - type errors
  - static generation crashes
- Actions:
  1. run `npm run build` locally
  2. patch smallest failing surface
  3. avoid changing unrelated modules

### Wrong project linked

- Signals:
  - deploy logs/project name do not match expected dashboard
- Actions:
  1. re-run `npx vercel link`
  2. select correct team/project
  3. retry deployment inspection

## Agent Notes

- If CLI auth is required, prompt user to complete device login in browser.
- Always report:
  1. failed deployment ID/URL
  2. root cause
  3. exact files changed (if any)
  4. verification commands run
- Do not delete unrelated code during incident triage.
