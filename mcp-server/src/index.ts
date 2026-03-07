/**
 * Black Bart's Gold MCP Server — Entry Point
 *
 * Runs as a long-lived stdio process. Cursor (or any MCP-compatible host)
 * spawns this process and communicates via stdin/stdout using the
 * Model Context Protocol JSON-RPC messages.
 *
 * Usage:
 *   Development:  npx tsx src/index.ts
 *   Production:   npm run build && node dist/index.js
 *
 * Required environment variables:
 *   ADMIN_API_BASE_URL   — the admin dashboard base URL (no trailing slash)
 *   AI_AGENT_API_KEY     — bearer token sent in every admin API request
 *
 * @file mcp-server/src/index.ts
 */

import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js'
import { createGameServer } from './game-mcp-server.js'

// Load .env if present (dev convenience — production uses real env vars)
try {
  const { config } = await import('dotenv')
  config()
} catch {
  // dotenv not available or .env file missing — carry on with real env vars
}

async function main() {
  // Validate required environment variables before connecting
  const adminBase = process.env.ADMIN_API_BASE_URL
  if (!adminBase) {
    process.stderr.write(
      '[BBG MCP] ERROR: ADMIN_API_BASE_URL is not set.\n' +
      '  Copy mcp-server/.env.example to mcp-server/.env and fill in the values.\n'
    )
    process.exit(1)
  }

  const apiKey = process.env.AI_AGENT_API_KEY
  if (!apiKey || apiKey === 'change-me-to-a-strong-secret') {
    process.stderr.write(
      '[BBG MCP] WARNING: AI_AGENT_API_KEY is not set or is still the example placeholder.\n' +
      '  Requests to the admin API will proceed without authentication.\n'
    )
  }

  process.stderr.write(`[BBG MCP] Starting server → admin API at ${adminBase}\n`)

  const server = createGameServer()
  const transport = new StdioServerTransport()

  await server.connect(transport)

  process.stderr.write('[BBG MCP] Server connected and ready ✓\n')
}

main().catch((err) => {
  process.stderr.write(`[BBG MCP] Fatal error: ${err instanceof Error ? err.stack : String(err)}\n`)
  process.exit(1)
})
