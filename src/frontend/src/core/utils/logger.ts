/**
 * Dev-only logger utilities
 *
 * Wraps console.log and console.warn so they are only emitted in development
 * builds. Production builds compile out to no-ops, ensuring no sensitive
 * auth or diagnostic data leaks to end users.
 *
 * Usage:
 * ```typescript
 * import { devLog, devWarn } from '@/core/utils/logger';
 *
 * devLog('[AuthContext] Token refreshed');     // DEV only
 * devWarn('[SignalR] Reconnecting:', err);      // DEV only
 * console.error('[API] Unhandled error', err); // Always (production errors)
 * ```
 *
 * Rules:
 * - Use `devLog`  for informational trace messages
 * - Use `devWarn` for expected-but-noteworthy situations
 * - Keep `console.error` for production-visible error reporting
 *
 * @module core/utils
 */

/**
 * Log to the console only in development mode.
 *
 * @param args - Arguments forwarded to console.log
 */
export function devLog(...args: unknown[]): void {
  if (import.meta.env.DEV) {
    console.log(...args)
  }
}

/**
 * Warn to the console only in development mode.
 *
 * @param args - Arguments forwarded to console.warn
 */
export function devWarn(...args: unknown[]): void {
  if (import.meta.env.DEV) {
    console.warn(...args)
  }
}
