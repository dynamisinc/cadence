/**
 * AuthContext - Authentication state management
 *
 * Provides authentication state and operations across the app.
 * Implements JWT token management with:
 * - Access tokens in memory (React state) for security
 * - Refresh tokens in HttpOnly cookies (managed by backend)
 * - Proactive token refresh (2 minutes before expiry)
 * - Automatic retry on 401 errors
 * - Cross-tab logout synchronization
 * - Resilient offline recovery (preserves auth state during network issues)
 *
 * Token refresh scheduling is delegated to useTokenRefresh.
 * Mount-time initialization is delegated to useAuthInit.
 *
 * @module contexts
 * @see docs/features/authentication/S05-jwt-issuance.md
 * @see docs/features/authentication/S07-token-refresh.md
 * @see docs/features/authentication/S08-expiration-handling.md
 */
import { createContext, useContext, useState, useEffect, useLayoutEffect, useCallback, useRef } from 'react'
import type { FC, ReactNode } from 'react'
import type {
  UserInfo,
  LoginRequest,
  RegistrationRequest,
  AuthResponse,
} from '../features/auth/types'
import { authService } from '../features/auth/services/authService'
import { setAuthInterceptors } from '../core/services/api'
import { setAuthenticatedUser, clearAuthenticatedUser, trackEvent } from '../core/services/telemetry'
import { devLog, devWarn } from '../core/utils/logger'
import { REFRESH_CONFIG, sleep, useTokenRefresh } from '../shared/hooks/useTokenRefresh'
import { useAuthInit } from '../shared/hooks/useAuthInit'
import {
  cacheUserInfo,
  classifyAuthError,
  parseToken,
} from './authHelpers'

interface AuthContextType {
  /** Currently authenticated user (null if not logged in) */
  user: UserInfo | null;
  /** Whether user is authenticated */
  isAuthenticated: boolean;
  /** Loading state for initial auth check */
  isLoading: boolean;
  /** Current access token (for use by API interceptors) */
  accessToken: string | null;
  /** Login with email/password */
  login: (request: LoginRequest) => Promise<AuthResponse>;
  /** Register new user account */
  register: (request: RegistrationRequest) => Promise<AuthResponse>;
  /** Logout current user */
  logout: () => Promise<void>;
  /** Refresh access token (called automatically by API interceptor) */
  refreshAccessToken: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

interface AuthProviderProps {
  children: ReactNode;
}

/**
 * Authentication context provider
 * Manages JWT tokens and user session state.
 *
 * State ownership stays here. useTokenRefresh and useAuthInit are
 * side-effect delegates that receive callbacks rather than owning state.
 */
export const AuthProvider: FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [accessToken, setAccessToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const refreshInProgressRef = useRef<Promise<void> | null>(null)
  const consecutiveFailuresRef = useRef<number>(0)

  // Store user in ref for logging without causing dependency changes
  const userRef = useRef(user)
  userRef.current = user
  const accessTokenRef = useRef(accessToken)
  accessTokenRef.current = accessToken

  // Ref for scheduleRefresh to break the circular dependency between
  // refreshAccessToken (defined first) and useTokenRefresh (defined after).
  // This ensures refreshAccessToken always calls the latest scheduleRefresh
  // without needing it in the useCallback dependency array.
  const scheduleRefreshRef = useRef<(expiresAt: number) => void>(() => {})

  /**
   * Log current auth state for debugging.
   * Uses refs to avoid recreating this callback when user/token changes.
   */
  const logAuthState = useCallback((context: string) => {
    const currentUser = userRef.current
    const currentToken = accessTokenRef.current
    devLog(`[AuthContext] ${context} - Current state:`, {
      hasUser: !!currentUser,
      userId: currentUser?.id,
      userEmail: currentUser?.email,
      hasAccessToken: !!currentToken,
      tokenLength: currentToken?.length,
      consecutiveFailures: consecutiveFailuresRef.current,
    })
  }, []) // Empty deps - uses refs

  /**
   * Refresh access token using refresh token cookie (S07)
   * Called proactively before expiry and reactively on 401 errors.
   *
   * Features:
   * - Single-flight pattern (prevents duplicate concurrent refreshes)
   * - Retry with exponential backoff for transient failures
   * - Preserves auth state on network errors (offline mode support)
   *
   * Defined before useTokenRefresh so scheduleRefresh can reference it.
   */
  const refreshAccessToken = useCallback(async () => {
    // Single-flight: if refresh is already in progress, wait for it
    if (refreshInProgressRef.current) {
      devLog('[AuthContext] refreshAccessToken - already in progress, waiting...')
      return refreshInProgressRef.current
    }

    const doRefresh = async () => {
      devLog('[AuthContext] refreshAccessToken starting...')
      logAuthState('Before refresh')

      let lastError: unknown = null

      for (let attempt = 0; attempt <= REFRESH_CONFIG.maxRetries; attempt++) {
        try {
          if (attempt > 0) {
            const delay = REFRESH_CONFIG.retryDelayMs * Math.pow(2, attempt - 1)
            devLog(`[AuthContext] Retry attempt ${attempt}/${REFRESH_CONFIG.maxRetries} after ${delay}ms delay`)
            await sleep(delay)
          }

          devLog(`[AuthContext] Calling authService.refreshToken() (attempt ${attempt + 1})`)
          const response = await authService.refreshToken()

          devLog('[AuthContext] authService.refreshToken() response:', {
            isSuccess: response.isSuccess,
            hasAccessToken: !!response.accessToken,
            userId: response.userId,
            error: response.error,
          })

          if (response.isSuccess && response.accessToken) {
            const parsed = parseToken(response.accessToken)
            if (parsed) {
              devLog('[AuthContext] Token refresh successful, updating state')
              setAccessToken(response.accessToken)
              setUser(parsed.user)
              scheduleRefreshRef.current(parsed.exp)
              consecutiveFailuresRef.current = 0 // Reset failure counter
              cacheUserInfo(parsed.user) // Cache for offline support
              logAuthState('After successful refresh')
              return
            } else {
              console.error('[AuthContext] Failed to parse access token')
              throw new Error('Failed to parse access token')
            }
          } else {
            // Server returned success=false - this is a definite auth failure
            const errorMsg = response.error?.message || 'Token refresh failed'
            devLog('[AuthContext] Server returned isSuccess=false:', errorMsg)
            throw new Error(errorMsg)
          }
        } catch (error) {
          lastError = error
          const classification = classifyAuthError(error)

          devLog(`[AuthContext] Refresh attempt ${attempt + 1} failed:`, {
            ...classification,
            attempt: attempt + 1,
            maxRetries: REFRESH_CONFIG.maxRetries,
          })

          // If it's a definite auth error, don't retry
          if (classification.isAuthError && !classification.isTransientError) {
            devLog('[AuthContext] Auth error detected, not retrying')
            break
          }

          // If it's not transient and we've exhausted retries, break
          if (!classification.isTransientError && attempt >= REFRESH_CONFIG.maxRetries) {
            devLog('[AuthContext] Non-transient error and max retries reached')
            break
          }

          // If it's transient, continue to next retry (unless max reached)
          if (classification.isTransientError && attempt < REFRESH_CONFIG.maxRetries) {
            devLog('[AuthContext] Transient error, will retry...')
            continue
          }
        }
      }

      // All retries exhausted or non-retryable error
      const finalClassification = classifyAuthError(lastError)
      consecutiveFailuresRef.current++

      devLog('[AuthContext] All refresh attempts failed:', {
        ...finalClassification,
        consecutiveFailures: consecutiveFailuresRef.current,
        currentUser: userRef.current?.email,
      })

      // Network/transient errors: preserve auth state for offline mode
      if (finalClassification.isNetworkError || finalClassification.isTransientError) {
        devWarn(
          '[AuthContext] Refresh failed due to network/transient error - PRESERVING auth state for offline mode',
          { user: userRef.current?.email, reason: finalClassification.reason },
        )
        // Re-throw so callers know refresh failed, but don't clear auth state
        throw lastError
      }

      // Auth errors: only clear state if we have consecutive failures
      // This prevents a single bad response from logging out the user
      if (consecutiveFailuresRef.current >= 2) {
        devLog(
          '[AuthContext] Multiple consecutive auth failures - clearing auth state',
          { consecutiveFailures: consecutiveFailuresRef.current },
        )
        setAccessToken(null)
        setUser(null)
        cacheUserInfo(null) // Clear cached user on confirmed auth failure
      } else {
        devWarn(
          '[AuthContext] First auth failure - preserving state, will clear on next failure',
          { consecutiveFailures: consecutiveFailuresRef.current },
        )
      }

      throw lastError
    }

    // Set up single-flight
    refreshInProgressRef.current = doRefresh().finally(() => {
      refreshInProgressRef.current = null
    })

    return refreshInProgressRef.current
  }, [logAuthState])

  // Delegate timer management to useTokenRefresh.
  // refreshAccessToken must be defined first (above) since scheduleRefresh calls it.
  const { scheduleRefresh, cancelRefresh } = useTokenRefresh({
    refreshTokenFn: refreshAccessToken,
  })

  // Keep the ref in sync so refreshAccessToken always uses the latest scheduleRefresh
  scheduleRefreshRef.current = scheduleRefresh

  // Delegate mount-time initialization to useAuthInit
  useAuthInit({
    refreshAccessToken,
    setUser,
    setIsLoading,
  })

  // Clean up the refresh timer on unmount (logout also cancels via logout handler)
  useEffect(() => {
    return () => {
      cancelRefresh()
    }
  }, [cancelRefresh])

  /**
   * Configure API interceptors with token getter and refresher.
   *
   * IMPORTANT: Using useLayoutEffect (not useEffect) ensures interceptors are
   * configured synchronously before child components can make API calls.
   * Regular useEffect runs after render, which creates a race condition where
   * API calls can happen before interceptors are set up.
   */
  useLayoutEffect(() => {
    devLog('[AuthContext] Setting up auth interceptors', {
      hasToken: !!accessToken,
    })
    setAuthInterceptors(
      () => accessToken,
      refreshAccessToken,
    )
  }, [accessToken, refreshAccessToken])

  /**
   * Cross-tab logout synchronization (S09)
   * When user logs out in one tab, all other tabs are also logged out.
   */
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'logout') {
        devLog('[AuthContext] Cross-tab logout detected')
        setUser(null)
        setAccessToken(null)
        window.location.href = '/login'
      }
    }
    window.addEventListener('storage', handleStorageChange)
    return () => window.removeEventListener('storage', handleStorageChange)
  }, [])

  /**
   * Log auth state changes for debugging
   */
  useEffect(() => {
    devLog('[AuthContext] Auth state changed:', {
      hasUser: !!user,
      userId: user?.id,
      userEmail: user?.email,
      hasAccessToken: !!accessToken,
      isLoading,
    })
  }, [user, accessToken, isLoading])

  /**
   * Login with email/password (S04, S05)
   * Sets access token in memory and refresh token in HttpOnly cookie.
   */
  const login = async (request: LoginRequest): Promise<AuthResponse> => {
    devLog('[AuthContext] login called for:', request.email)
    const response = await authService.login(request)

    if (response.isSuccess && response.accessToken) {
      const parsed = parseToken(response.accessToken)
      if (parsed) {
        devLog('[AuthContext] login successful, setting user state')
        setAccessToken(response.accessToken)
        setUser(parsed.user)
        scheduleRefresh(parsed.exp)
        consecutiveFailuresRef.current = 0
        cacheUserInfo(parsed.user) // Cache for offline support

        // Set telemetry user context (hashed for privacy)
        setAuthenticatedUser(parsed.user.id)
        trackEvent('Login', { method: 'password' })
      }
    } else {
      devLog('[AuthContext] login failed:', response.error)
      trackEvent('LoginFailed', { error: response.error?.code || 'unknown' })
    }

    return response
  }

  /**
   * Register new user account (S03)
   * Sets access token in memory and refresh token in HttpOnly cookie.
   */
  const register = async (request: RegistrationRequest): Promise<AuthResponse> => {
    devLog('[AuthContext] register called for:', request.email)
    const response = await authService.register(request)

    if (response.isSuccess && response.accessToken) {
      const parsed = parseToken(response.accessToken)
      if (parsed) {
        devLog('[AuthContext] registration successful, setting user state')
        setAccessToken(response.accessToken)
        setUser(parsed.user)
        scheduleRefresh(parsed.exp)
        consecutiveFailuresRef.current = 0
        cacheUserInfo(parsed.user) // Cache for offline support

        // Set telemetry user context (hashed for privacy)
        setAuthenticatedUser(parsed.user.id)
        trackEvent('Registration')
      }
    } else {
      devLog('[AuthContext] registration failed:', response.error)
      trackEvent('RegistrationFailed', { error: response.error?.code || 'unknown' })
    }

    return response
  }

  /**
   * Logout current user (S09)
   * Clears local state and notifies other tabs.
   */
  const logout = async () => {
    devLog('[AuthContext] logout called')
    try {
      // Call backend to clear refresh token cookie
      await authService.logout()
      devLog('[AuthContext] Backend logout succeeded')
    } catch (err) {
      // Continue with local logout even if server call fails
      devWarn('[AuthContext] Backend logout failed, continuing with local logout:', err)
    }

    // Clear local state
    devLog('[AuthContext] Clearing local auth state')
    setUser(null)
    setAccessToken(null)
    consecutiveFailuresRef.current = 0
    cacheUserInfo(null) // Clear cached user

    // Clear telemetry user context
    trackEvent('Logout')
    clearAuthenticatedUser()

    cancelRefresh()

    // Notify other tabs to logout (S09)
    localStorage.setItem('logout', Date.now().toString())
    localStorage.removeItem('logout')
  }

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    accessToken,
    login,
    register,
    logout,
    refreshAccessToken,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

/**
 * Hook to access authentication context.
 * Must be used within AuthProvider.
 */
export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
