/**
 * useAuthInit - Mount-time authentication initialization
 *
 * Runs the silent token refresh on component mount to restore a
 * previous session from the HttpOnly refresh-token cookie.
 * Falls back to cached user info (localStorage) when the backend
 * is unreachable (offline mode).
 *
 * This hook performs the side-effecting initialization; the provider
 * (AuthProvider) owns all state and passes setters in.
 *
 * @module shared/hooks
 * @see docs/features/authentication/S08-expiration-handling.md
 */

import { useEffect } from 'react'
import type { UserInfo } from '../../features/auth/types'
import { devLog } from '../../core/utils/logger'
import { classifyAuthError, getCachedUserInfo } from '../../contexts/authHelpers'

export interface UseAuthInitOptions {
  /**
   * Async function that performs the token refresh. Provided by AuthProvider.
   * On success it sets user + token state internally; on network error it throws.
   */
  refreshAccessToken: () => Promise<void>
  /**
   * Setter for the user state. Called when offline cache is restored.
   */
  setUser: (user: UserInfo | null) => void
  /**
   * Setter for the isLoading flag. Called when initialization completes.
   */
  setIsLoading: (loading: boolean) => void
}

/**
 * Runs the authentication initialization sequence on mount.
 *
 * Sequence:
 * 1. Attempt a silent token refresh (uses HttpOnly cookie).
 * 2. On network/transient error: restore cached user for offline mode.
 * 3. On hard auth failure: leave user null (unauthenticated).
 * 4. Always set isLoading=false when done.
 *
 * @example
 * useAuthInit({
 *   refreshAccessToken,
 *   setUser,
 *   setIsLoading,
 * })
 */
export const useAuthInit = ({
  refreshAccessToken,
  setUser,
  setIsLoading,
}: UseAuthInitOptions): void => {
  useEffect(() => {
    const initAuth = async () => {
      devLog('[useAuthInit] initAuth starting...')
      devLog('[useAuthInit] Browser online status:', navigator.onLine)

      try {
        await refreshAccessToken()
        devLog('[useAuthInit] initAuth: refreshAccessToken succeeded')
      } catch (error) {
        const classification = classifyAuthError(error)
        devLog('[useAuthInit] initAuth: refreshAccessToken failed:', {
          ...classification,
          browserOnline: navigator.onLine,
        })

        // If network error, try to restore from cache for offline support
        if (classification.isNetworkError || classification.isTransientError) {
          const cachedUser = getCachedUserInfo()
          if (cachedUser) {
            devLog('[useAuthInit] initAuth: Restoring user from cache for offline mode')
            setUser(cachedUser)
            // No access token available, so API calls will fail.
            // When back online, the next API call will trigger a refresh via interceptor.
          }
        }
      } finally {
        setIsLoading(false)
        devLog('[useAuthInit] initAuth complete, isLoading=false')
      }
    }

    initAuth()
    // refreshAccessToken is stable (wrapped in useCallback in provider)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [refreshAccessToken])
}
