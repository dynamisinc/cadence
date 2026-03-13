/**
 * useTokenRefresh - Proactive JWT token refresh scheduler
 *
 * Manages the lifecycle of scheduled token refresh timers.
 * Schedules a refresh 2 minutes before token expiry to prevent API
 * calls from failing due to expired tokens.
 *
 * This hook owns only the timer ref, not auth state. The provider
 * (AuthProvider) retains all state ownership and passes callbacks
 * to this hook.
 *
 * @module shared/hooks
 * @see docs/features/authentication/S07-token-refresh.md
 */

import { useRef, useCallback } from 'react'
import { devLog, devWarn } from '../../core/utils/logger'

/** Configuration for refresh retry behavior */
export const REFRESH_CONFIG = {
  /** Maximum number of retry attempts for transient failures */
  maxRetries: 2,
  /** Base delay between retries in ms (uses exponential backoff) */
  retryDelayMs: 1000,
  /** Timeout for considering a request as failed due to network */
  networkTimeoutMs: 15000,
}

/**
 * Sleep helper for retry delays
 */
export function sleep(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms))
}

export interface UseTokenRefreshOptions {
  /**
   * Async function that performs the actual token refresh.
   * Called when the scheduled timer fires.
   */
  refreshTokenFn: () => Promise<void>
}

export interface UseTokenRefreshReturn {
  /**
   * Schedule a proactive refresh 2 minutes before the given expiry time.
   * @param expiresAt - Token expiry time in milliseconds (Date.now()-relative)
   */
  scheduleRefresh: (expiresAt: number) => void
  /**
   * Cancel any pending refresh timer. Call this on unmount or logout.
   */
  cancelRefresh: () => void
}

/**
 * Manages scheduled token refresh timers.
 * The consumer (AuthProvider) remains the state owner; this hook
 * only manages the side-effecting timer.
 *
 * @example
 * const { scheduleRefresh, cancelRefresh } = useTokenRefresh({
 *   refreshTokenFn: refreshAccessToken,
 * })
 */
export const useTokenRefresh = ({
  refreshTokenFn,
}: UseTokenRefreshOptions): UseTokenRefreshReturn => {
  const refreshTimerRef = useRef<number | null>(null)

  const cancelRefresh = useCallback(() => {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current)
      refreshTimerRef.current = null
    }
  }, [])

  const scheduleRefresh = useCallback(
    (expiresAt: number) => {
      // Cancel any existing timer first
      cancelRefresh()

      const refreshIn = expiresAt - Date.now() - 2 * 60 * 1000 // 2 minutes before expiry
      const refreshInSeconds = Math.round(refreshIn / 1000)

      devLog('[useTokenRefresh] scheduleRefresh:', {
        expiresAt: new Date(expiresAt).toISOString(),
        refreshIn: `${refreshInSeconds}s`,
        willRefresh: refreshIn > 0,
      })

      if (refreshIn > 0) {
        refreshTimerRef.current = window.setTimeout(async () => {
          devLog('[useTokenRefresh] Scheduled refresh timer fired')
          try {
            await refreshTokenFn()
            devLog('[useTokenRefresh] Scheduled refresh succeeded')
          } catch (err) {
            // Token refresh failed — will redirect to login on next API call
            devWarn('[useTokenRefresh] Scheduled refresh failed:', err)
          }
        }, refreshIn)
      }
    },
    [refreshTokenFn, cancelRefresh],
  )

  return { scheduleRefresh, cancelRefresh }
}
