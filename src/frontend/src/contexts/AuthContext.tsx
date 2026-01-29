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

/** Configuration for refresh retry behavior */
const REFRESH_CONFIG = {
  /** Maximum number of retry attempts for transient failures */
  maxRetries: 2,
  /** Base delay between retries in ms (uses exponential backoff) */
  retryDelayMs: 1000,
  /** Timeout for considering a request as failed due to network */
  networkTimeoutMs: 15000,
}

/**
 * Classify an error to determine if it's a transient/network error
 * that should NOT clear auth state, or an auth error that should.
 */
function classifyError(error: unknown): {
  isNetworkError: boolean;
  isTransientError: boolean;
  isAuthError: boolean;
  reason: string;
} {
  const errorObj = error as {
    message?: string;
    code?: string;
    response?: {
      status?: number;
      data?: { code?: string };
    };
    name?: string;
  }

  const message = errorObj?.message || ''
  const code = errorObj?.code || ''
  const httpStatus = errorObj?.response?.status
  const errorCode = errorObj?.response?.data?.code

  console.log('[AuthContext] classifyError analyzing:', {
    message,
    code,
    httpStatus,
    errorCode,
    errorName: errorObj?.name,
  })

  // Network errors - API is unreachable
  const networkErrorPatterns = [
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
  ]

  const isNetworkError =
    code === 'ERR_NETWORK' ||
    code === 'ECONNABORTED' ||
    code === 'ECONNREFUSED' ||
    code === 'ETIMEDOUT' ||
    networkErrorPatterns.some(pattern =>
      message.includes(pattern) || code.includes(pattern),
    )

  if (isNetworkError) {
    return {
      isNetworkError: true,
      isTransientError: true,
      isAuthError: false,
      reason: `Network error: ${message || code}`,
    }
  }

  // Timeout errors - treat as transient
  if (
    code === 'ECONNABORTED' ||
    message.includes('timeout') ||
    message.includes('Timeout')
  ) {
    return {
      isNetworkError: false,
      isTransientError: true,
      isAuthError: false,
      reason: `Timeout: ${message}`,
    }
  }

  // CORS errors - often look like network errors
  if (
    message.includes('CORS') ||
    message.includes('cross-origin') ||
    message.includes('Access-Control')
  ) {
    return {
      isNetworkError: false,
      isTransientError: true,
      isAuthError: false,
      reason: `CORS error: ${message}`,
    }
  }

  // Server errors (5xx) - transient, server might be recovering
  if (httpStatus && httpStatus >= 500) {
    return {
      isNetworkError: false,
      isTransientError: true,
      isAuthError: false,
      reason: `Server error: ${httpStatus}`,
    }
  }

  // 401/403 with specific auth error codes - these are real auth failures
  if (httpStatus === 401 || httpStatus === 403) {
    // Check for specific error codes that indicate real auth failure
    const authFailureCodes = ['invalid_token', 'token_expired', 'invalid_credentials']
    if (errorCode && authFailureCodes.includes(errorCode)) {
      return {
        isNetworkError: false,
        isTransientError: false,
        isAuthError: true,
        reason: `Auth failure: ${errorCode}`,
      }
    }

    // Generic 401/403 - could be due to cookie not being sent (treat as potentially transient)
    // Only treat as auth error if we've already retried
    return {
      isNetworkError: false,
      isTransientError: true, // First time, treat as transient
      isAuthError: false,
      reason: `HTTP ${httpStatus} - may be transient`,
    }
  }

  // Default: treat as auth error (safe fallback)
  return {
    isNetworkError: false,
    isTransientError: false,
    isAuthError: true,
    reason: `Unknown error: ${message || 'no message'}`,
  }
}

/**
 * Sleep helper for retry delays
 */
function sleep(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms))
}

/**
 * Parse JWT token to extract expiry and user info
 * @returns Parsed token data or null if invalid
 */
function parseToken(token: string): { exp: number; user: UserInfo } | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))

    // ClaimTypes.Role uses full URI: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    const roleClaim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      || payload.role
      || 'User'

    const parsed = {
      exp: payload.exp * 1000, // Convert to milliseconds
      user: {
        id: payload.sub,
        email: payload.email,
        displayName: payload.name,
        role: roleClaim,
        status: 'Active' as const,
      },
    }

    console.log('[AuthContext] parseToken success:', {
      userId: parsed.user.id,
      email: parsed.user.email,
      role: parsed.user.role,
      expiresAt: new Date(parsed.exp).toISOString(),
    })

    return parsed
  } catch (err) {
    console.error('[AuthContext] parseToken failed:', err)
    return null
  }
}

/**
 * Authentication context provider
 * Manages JWT tokens and user session state
 */
export const AuthProvider: FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [accessToken, setAccessToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [_tokenExpiry, setTokenExpiry] = useState<number | null>(null)
  const refreshTimerRef = useRef<number | null>(null)
  const refreshInProgressRef = useRef<Promise<void> | null>(null)
  const consecutiveFailuresRef = useRef<number>(0)

  // Store user in ref for logging without causing dependency changes
  const userRef = useRef(user)
  userRef.current = user
  const accessTokenRef = useRef(accessToken)
  accessTokenRef.current = accessToken

  /**
   * Log current auth state for debugging
   * Uses refs to avoid recreating this callback when user/token changes
   */
  const logAuthState = useCallback((context: string) => {
    const currentUser = userRef.current
    const currentToken = accessTokenRef.current
    console.log(`[AuthContext] ${context} - Current state:`, {
      hasUser: !!currentUser,
      userId: currentUser?.id,
      userEmail: currentUser?.email,
      hasAccessToken: !!currentToken,
      tokenLength: currentToken?.length,
      consecutiveFailures: consecutiveFailuresRef.current,
    })
  }, []) // Empty deps - uses refs

  /**
   * Schedule token refresh 2 minutes before expiry (S07)
   * This proactive refresh prevents API calls from failing
   */
  const scheduleRefresh = useCallback((expiresAt: number) => {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current)
    }

    const refreshIn = expiresAt - Date.now() - 2 * 60 * 1000 // 2 minutes before expiry
    const refreshInSeconds = Math.round(refreshIn / 1000)

    console.log('[AuthContext] scheduleRefresh:', {
      expiresAt: new Date(expiresAt).toISOString(),
      refreshIn: `${refreshInSeconds}s`,
      willRefresh: refreshIn > 0,
    })

    if (refreshIn > 0) {
      refreshTimerRef.current = setTimeout(async () => {
        console.log('[AuthContext] Scheduled refresh timer fired')
        try {
          await refreshAccessToken()
          console.log('[AuthContext] Scheduled refresh succeeded')
        } catch (err) {
          // Token refresh failed - will redirect to login on next API call
          console.warn('[AuthContext] Scheduled refresh failed:', err)
        }
      }, refreshIn)
    }
  }, [])

  /**
   * Refresh access token using refresh token cookie (S07)
   * Called proactively before expiry and reactively on 401 errors
   *
   * Features:
   * - Single-flight pattern (prevents duplicate concurrent refreshes)
   * - Retry with exponential backoff for transient failures
   * - Preserves auth state on network errors (offline mode support)
   */
  const refreshAccessToken = useCallback(async () => {
    // Single-flight: if refresh is already in progress, wait for it
    if (refreshInProgressRef.current) {
      console.log('[AuthContext] refreshAccessToken - already in progress, waiting...')
      return refreshInProgressRef.current
    }

    const doRefresh = async () => {
      console.log('[AuthContext] refreshAccessToken starting...')
      logAuthState('Before refresh')

      let lastError: unknown = null

      for (let attempt = 0; attempt <= REFRESH_CONFIG.maxRetries; attempt++) {
        try {
          if (attempt > 0) {
            const delay = REFRESH_CONFIG.retryDelayMs * Math.pow(2, attempt - 1)
            console.log(`[AuthContext] Retry attempt ${attempt}/${REFRESH_CONFIG.maxRetries} after ${delay}ms delay`)
            await sleep(delay)
          }

          console.log(`[AuthContext] Calling authService.refreshToken() (attempt ${attempt + 1})`)
          const response = await authService.refreshToken()

          console.log('[AuthContext] authService.refreshToken() response:', {
            isSuccess: response.isSuccess,
            hasAccessToken: !!response.accessToken,
            userId: response.userId,
            error: response.error,
          })

          if (response.isSuccess && response.accessToken) {
            const parsed = parseToken(response.accessToken)
            if (parsed) {
              console.log('[AuthContext] Token refresh successful, updating state')
              setAccessToken(response.accessToken)
              setUser(parsed.user)
              setTokenExpiry(parsed.exp)
              scheduleRefresh(parsed.exp)
              consecutiveFailuresRef.current = 0 // Reset failure counter
              logAuthState('After successful refresh')
              return
            } else {
              console.error('[AuthContext] Failed to parse access token')
              throw new Error('Failed to parse access token')
            }
          } else {
            // Server returned success=false - this is a definite auth failure
            const errorMsg = response.error?.message || 'Token refresh failed'
            console.log('[AuthContext] Server returned isSuccess=false:', errorMsg)
            throw new Error(errorMsg)
          }
        } catch (error) {
          lastError = error
          const classification = classifyError(error)

          console.log(`[AuthContext] Refresh attempt ${attempt + 1} failed:`, {
            ...classification,
            attempt: attempt + 1,
            maxRetries: REFRESH_CONFIG.maxRetries,
          })

          // If it's a definite auth error, don't retry
          if (classification.isAuthError && !classification.isTransientError) {
            console.log('[AuthContext] Auth error detected, not retrying')
            break
          }

          // If it's not transient and we've exhausted retries, break
          if (!classification.isTransientError && attempt >= REFRESH_CONFIG.maxRetries) {
            console.log('[AuthContext] Non-transient error and max retries reached')
            break
          }

          // If it's transient, continue to next retry (unless max reached)
          if (classification.isTransientError && attempt < REFRESH_CONFIG.maxRetries) {
            console.log('[AuthContext] Transient error, will retry...')
            continue
          }
        }
      }

      // All retries exhausted or non-retryable error
      const finalClassification = classifyError(lastError)
      consecutiveFailuresRef.current++

      console.log('[AuthContext] All refresh attempts failed:', {
        ...finalClassification,
        consecutiveFailures: consecutiveFailuresRef.current,
        currentUser: userRef.current?.email,
      })

      // Network/transient errors: preserve auth state for offline mode
      if (finalClassification.isNetworkError || finalClassification.isTransientError) {
        console.warn(
          '[AuthContext] Refresh failed due to network/transient error - PRESERVING auth state for offline mode',
          { user: userRef.current?.email, reason: finalClassification.reason },
        )
        // Re-throw so callers know refresh failed, but don't clear auth state
        throw lastError
      }

      // Auth errors: only clear state if we have consecutive failures
      // This prevents a single bad response from logging out the user
      if (consecutiveFailuresRef.current >= 2) {
        console.log(
          '[AuthContext] Multiple consecutive auth failures - clearing auth state',
          { consecutiveFailures: consecutiveFailuresRef.current },
        )
        setAccessToken(null)
        setUser(null)
        setTokenExpiry(null)
      } else {
        console.warn(
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
  }, [scheduleRefresh, logAuthState]) // No user dependency - uses userRef

  /**
   * Initialize: Try to refresh token on mount (S08)
   * This checks if user has a valid refresh token cookie
   */
  useEffect(() => {
    const initAuth = async () => {
      console.log('[AuthContext] initAuth starting...')
      console.log('[AuthContext] Browser online status:', navigator.onLine)

      try {
        await refreshAccessToken()
        console.log('[AuthContext] initAuth: refreshAccessToken succeeded')
      } catch (error) {
        // No valid session - user needs to log in
        // This is normal for first visit or expired session
        const classification = classifyError(error)
        console.log('[AuthContext] initAuth: refreshAccessToken failed:', {
          ...classification,
          browserOnline: navigator.onLine,
        })
      } finally {
        setIsLoading(false)
        console.log('[AuthContext] initAuth complete, isLoading=false')
      }
    }
    initAuth()

    return () => {
      if (refreshTimerRef.current) {
        clearTimeout(refreshTimerRef.current)
      }
    }
  }, [refreshAccessToken])

  /**
   * Configure API interceptors with token getter and refresher
   * This allows axios to access current token and trigger refresh on 401
   *
   * IMPORTANT: Using useLayoutEffect (not useEffect) ensures interceptors are
   * configured synchronously before child components can make API calls.
   * Regular useEffect runs after render, which creates a race condition where
   * API calls can happen before interceptors are set up.
   */
  useLayoutEffect(() => {
    console.log('[AuthContext] Setting up auth interceptors', {
      hasToken: !!accessToken,
    })
    setAuthInterceptors(
      () => accessToken,
      refreshAccessToken,
    )
  }, [accessToken, refreshAccessToken])

  /**
   * Cross-tab logout synchronization (S09)
   * When user logs out in one tab, all other tabs are also logged out
   */
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'logout') {
        console.log('[AuthContext] Cross-tab logout detected')
        setUser(null)
        setAccessToken(null)
        setTokenExpiry(null)
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
    console.log('[AuthContext] Auth state changed:', {
      hasUser: !!user,
      userId: user?.id,
      userEmail: user?.email,
      hasAccessToken: !!accessToken,
      isLoading,
    })
  }, [user, accessToken, isLoading])

  /**
   * Login with email/password (S04, S05)
   * Sets access token in memory and refresh token in HttpOnly cookie
   */
  const login = async (request: LoginRequest): Promise<AuthResponse> => {
    console.log('[AuthContext] login called for:', request.email)
    const response = await authService.login(request)

    if (response.isSuccess && response.accessToken) {
      const parsed = parseToken(response.accessToken)
      if (parsed) {
        console.log('[AuthContext] login successful, setting user state')
        setAccessToken(response.accessToken)
        setUser(parsed.user)
        setTokenExpiry(parsed.exp)
        scheduleRefresh(parsed.exp)
        consecutiveFailuresRef.current = 0
      }
    } else {
      console.log('[AuthContext] login failed:', response.error)
    }

    return response
  }

  /**
   * Register new user account (S03)
   * Sets access token in memory and refresh token in HttpOnly cookie
   */
  const register = async (request: RegistrationRequest): Promise<AuthResponse> => {
    console.log('[AuthContext] register called for:', request.email)
    const response = await authService.register(request)

    if (response.isSuccess && response.accessToken) {
      const parsed = parseToken(response.accessToken)
      if (parsed) {
        console.log('[AuthContext] registration successful, setting user state')
        setAccessToken(response.accessToken)
        setUser(parsed.user)
        setTokenExpiry(parsed.exp)
        scheduleRefresh(parsed.exp)
        consecutiveFailuresRef.current = 0
      }
    } else {
      console.log('[AuthContext] registration failed:', response.error)
    }

    return response
  }

  /**
   * Logout current user (S09)
   * Clears local state and notifies other tabs
   */
  const logout = async () => {
    console.log('[AuthContext] logout called')
    try {
      // Call backend to clear refresh token cookie
      await authService.logout()
      console.log('[AuthContext] Backend logout succeeded')
    } catch (err) {
      // Continue with local logout even if server call fails
      console.warn('[AuthContext] Backend logout failed, continuing with local logout:', err)
    }

    // Clear local state
    console.log('[AuthContext] Clearing local auth state')
    setUser(null)
    setAccessToken(null)
    setTokenExpiry(null)
    consecutiveFailuresRef.current = 0

    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current)
    }

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
 * Hook to access authentication context
 * Must be used within AuthProvider
 */
export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
