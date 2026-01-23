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
 *
 * @module contexts
 * @see docs/features/authentication/S05-jwt-issuance.md
 * @see docs/features/authentication/S07-token-refresh.md
 * @see docs/features/authentication/S08-expiration-handling.md
 */
import { createContext, useContext, FC, ReactNode, useState, useEffect, useCallback, useRef } from 'react'
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

    return {
      exp: payload.exp * 1000, // Convert to milliseconds
      user: {
        id: payload.sub,
        email: payload.email,
        displayName: payload.name,
        role: roleClaim,
        status: 'Active',
      },
    }
  } catch {
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
  const [tokenExpiry, setTokenExpiry] = useState<number | null>(null)
  const refreshTimerRef = useRef<NodeJS.Timeout | null>(null)

  /**
   * Schedule token refresh 2 minutes before expiry (S07)
   * This proactive refresh prevents API calls from failing
   */
  const scheduleRefresh = useCallback((expiresAt: number) => {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current)
    }

    const refreshIn = expiresAt - Date.now() - 2 * 60 * 1000 // 2 minutes before expiry
    if (refreshIn > 0) {
      refreshTimerRef.current = setTimeout(async () => {
        try {
          await refreshAccessToken()
        } catch {
          // Token refresh failed - will redirect to login on next API call
          console.warn('Proactive token refresh failed')
        }
      }, refreshIn)
    }
  }, [])

  /**
   * Refresh access token using refresh token cookie (S07)
   * Called proactively before expiry and reactively on 401 errors
   */
  const refreshAccessToken = useCallback(async () => {
    try {
      const response = await authService.refreshToken()

      if (response.isSuccess && response.accessToken) {
        const parsed = parseToken(response.accessToken)
        if (parsed) {
          setAccessToken(response.accessToken)
          setUser(parsed.user)
          setTokenExpiry(parsed.exp)
          scheduleRefresh(parsed.exp)
        }
      } else {
        // Refresh failed - clear auth state
        throw new Error(response.error?.message || 'Token refresh failed')
      }
    } catch (error) {
      // Refresh failed - clear auth state
      setAccessToken(null)
      setUser(null)
      setTokenExpiry(null)
      throw error
    }
  }, [scheduleRefresh])

  /**
   * Initialize: Try to refresh token on mount (S08)
   * This checks if user has a valid refresh token cookie
   */
  useEffect(() => {
    const initAuth = async () => {
      try {
        await refreshAccessToken()
      } catch {
        // No valid session - user needs to log in
        // This is normal for first visit or expired session
      } finally {
        setIsLoading(false)
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
   */
  useEffect(() => {
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
   * Login with email/password (S04, S05)
   * Sets access token in memory and refresh token in HttpOnly cookie
   */
  const login = async (request: LoginRequest): Promise<AuthResponse> => {
    const response = await authService.login(request)

    if (response.isSuccess && response.accessToken) {
      const parsed = parseToken(response.accessToken)
      if (parsed) {
        setAccessToken(response.accessToken)
        setUser(parsed.user)
        setTokenExpiry(parsed.exp)
        scheduleRefresh(parsed.exp)
      }
    }

    return response
  }

  /**
   * Register new user account (S03)
   * Sets access token in memory and refresh token in HttpOnly cookie
   */
  const register = async (request: RegistrationRequest): Promise<AuthResponse> => {
    const response = await authService.register(request)

    if (response.isSuccess && response.accessToken) {
      const parsed = parseToken(response.accessToken)
      if (parsed) {
        setAccessToken(response.accessToken)
        setUser(parsed.user)
        setTokenExpiry(parsed.exp)
        scheduleRefresh(parsed.exp)
      }
    }

    return response
  }

  /**
   * Logout current user (S09)
   * Clears local state and notifies other tabs
   */
  const logout = async () => {
    try {
      // Call backend to clear refresh token cookie
      await authService.logout()
    } catch {
      // Continue with local logout even if server call fails
    }

    // Clear local state
    setUser(null)
    setAccessToken(null)
    setTokenExpiry(null)

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
