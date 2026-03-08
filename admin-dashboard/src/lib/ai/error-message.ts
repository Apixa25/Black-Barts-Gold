/**
 * Convert unknown route errors into readable strings.
 *
 * Supabase/PostgREST errors are often plain objects rather than Error instances,
 * so String(error) degrades to "[object Object]". This helper keeps AI route
 * responses actionable when a schema or query mismatch occurs.
 *
 * @file admin-dashboard/src/lib/ai/error-message.ts
 */

export function describeError(error: unknown): string {
  if (error instanceof Error) return error.message

  if (error && typeof error === 'object') {
    const maybeMessage = 'message' in error ? error.message : null
    const maybeDetails = 'details' in error ? error.details : null
    const maybeHint = 'hint' in error ? error.hint : null

    const parts = [maybeMessage, maybeDetails, maybeHint]
      .filter((part): part is string => typeof part === 'string' && part.trim().length > 0)

    if (parts.length > 0) return parts.join(' | ')

    try {
      return JSON.stringify(error)
    } catch {
      return '[unserializable error object]'
    }
  }

  return String(error)
}
