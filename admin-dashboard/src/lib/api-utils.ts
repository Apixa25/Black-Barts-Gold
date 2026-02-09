/**
 * API Utility Functions
 * 
 * Common utilities for API routes including:
 * - Case conversion (snake_case to camelCase)
 * - Response formatting
 * - Error handling helpers
 * 
 * @file admin-dashboard/src/lib/api-utils.ts
 * Character count: ~2,200
 */

/**
 * Convert snake_case string to camelCase
 */
export function snakeToCamel(str: string): string {
  return str.replace(/_([a-z])/g, (_, letter) => letter.toUpperCase())
}

/**
 * Convert camelCase string to snake_case
 */
export function camelToSnake(str: string): string {
  return str.replace(/[A-Z]/g, letter => `_${letter.toLowerCase()}`)
}

/**
 * Convert object keys from snake_case to camelCase (deep conversion)
 */
export function keysToCamelCase<T>(obj: unknown): T {
  if (obj === null || obj === undefined) {
    return obj as T
  }
  
  if (Array.isArray(obj)) {
    return obj.map(item => keysToCamelCase(item)) as T
  }
  
  if (typeof obj === 'object') {
    const result: Record<string, unknown> = {}
    
    for (const [key, value] of Object.entries(obj as Record<string, unknown>)) {
      const camelKey = snakeToCamel(key)
      result[camelKey] = keysToCamelCase(value)
    }
    
    return result as T
  }
  
  return obj as T
}

/**
 * Convert object keys from camelCase to snake_case (deep conversion)
 */
export function keysToSnakeCase<T>(obj: unknown): T {
  if (obj === null || obj === undefined) {
    return obj as T
  }
  
  if (Array.isArray(obj)) {
    return obj.map(item => keysToSnakeCase(item)) as T
  }
  
  if (typeof obj === 'object') {
    const result: Record<string, unknown> = {}
    
    for (const [key, value] of Object.entries(obj as Record<string, unknown>)) {
      const snakeKey = camelToSnake(key)
      result[snakeKey] = keysToSnakeCase(value)
    }
    
    return result as T
  }
  
  return obj as T
}

/**
 * Standard API error response format
 */
export interface ApiErrorResponse {
  success: false
  error: string
  code: string
  details?: unknown
}

/**
 * Create a standardized error response
 */
export function createErrorResponse(
  error: string,
  code: string,
  status: number = 400,
  details?: unknown
): { body: ApiErrorResponse; status: number } {
  return {
    body: {
      success: false,
      error,
      code,
      details,
    },
    status,
  }
}

/**
 * Coin fields mapping for Unity compatibility
 * Maps database snake_case fields to Unity camelCase fields
 */
export const COIN_FIELD_MAP = {
  coin_type: 'coinType',
  coin_model: 'coinModel',
  is_mythical: 'isMythical',
  location_name: 'locationName',
  hider_id: 'hiderId',
  hidden_at: 'hiddenAt',
  collected_by: 'collectedBy',
  collected_at: 'collectedAt',
  sponsor_id: 'sponsorId',
  logo_url: 'logoUrl',
  multi_find: 'multiFind',
  finds_remaining: 'findsRemaining',
  created_at: 'createdAt',
  updated_at: 'updatedAt',
} as const
