/**
 * Network error detection utility
 *
 * Centralises the logic for deciding whether a caught error represents a
 * transient network/connectivity failure (as opposed to an application-level
 * authentication or server error).
 *
 * Previously this check was duplicated in:
 *   - `src/contexts/AuthContext.tsx`  (classifyError → isNetworkError)
 *   - `src/core/services/api.ts`      (inline isNetworkError block)
 *
 * Both callers now import from here.
 *
 * @module core/utils
 */

/**
 * Shape accepted from both Axios errors and generic Error objects.
 * We use `unknown` as the parameter type and cast inside so callers don't
 * need to import Axios types.
 */
interface NetworkCheckable {
  message?: string;
  code?: string;
}

/** Network-error message patterns that indicate the API is unreachable. */
const NETWORK_ERROR_MESSAGES = [
  'Network Error',
  'ECONNREFUSED',
  'ERR_NETWORK',
  'ENOTFOUND',
  'ETIMEDOUT',
  'ECONNRESET',
  'Failed to fetch',
  'Load failed',
  'net::',
  'NetworkError',
  'timeout',
] as const

/** Axios error codes that indicate a network / connectivity problem. */
const NETWORK_ERROR_CODES = [
  'ERR_NETWORK',
  'ECONNABORTED',
  'ECONNREFUSED',
  'ETIMEDOUT',
] as const

/**
 * Returns `true` when the error looks like a transient network/connectivity
 * failure that should NOT be treated as an authentication error.
 *
 * Handles:
 * - Axios network errors (`error.code === 'ERR_NETWORK'`, etc.)
 * - Browser fetch failures (`'Network Error'`, `'Failed to fetch'`, etc.)
 * - Timeout errors (Axios `ECONNABORTED`, message containing `'timeout'`)
 *
 * @param error - The caught error (any type is accepted)
 * @returns `true` if the error is a transient network error
 */
export function isNetworkError(error: unknown): boolean {
  const err = error as NetworkCheckable

  const message = err?.message ?? ''
  const code = err?.code ?? ''

  // Check Axios-style error codes
  if (NETWORK_ERROR_CODES.some(c => code === c)) {
    return true
  }

  // Check message and code strings against known patterns
  if (
    NETWORK_ERROR_MESSAGES.some(
      pattern => message.includes(pattern) || code.includes(pattern),
    )
  ) {
    return true
  }

  return false
}
