/**
 * authHelpers - Pure helper functions for authentication logic
 *
 * Extracted from AuthContext to be shared between AuthContext.tsx,
 * useTokenRefresh, and useAuthInit without creating circular dependencies.
 *
 * @module contexts
 */

import type { UserInfo } from '../features/auth/types'
import { devLog, devWarn } from '../core/utils/logger'
import { isNetworkError as checkIsNetworkError } from '../core/utils/networkErrors'

/** localStorage key for cached user info (offline support) */
export const CACHED_USER_KEY = 'cadence-cached-user'

/**
 * Cache user info to localStorage for offline support
 */
export function cacheUserInfo(user: UserInfo | null): void {
  try {
    if (user) {
      localStorage.setItem(CACHED_USER_KEY, JSON.stringify(user))
      devLog('[authHelpers] Cached user info for offline support:', user.email)
    } else {
      localStorage.removeItem(CACHED_USER_KEY)
      devLog('[authHelpers] Cleared cached user info')
    }
  } catch (err) {
    devWarn('[authHelpers] Failed to cache user info:', err)
  }
}

/**
 * Restore cached user info from localStorage (for offline mode)
 */
export function getCachedUserInfo(): UserInfo | null {
  try {
    const cached = localStorage.getItem(CACHED_USER_KEY)
    if (cached) {
      const user = JSON.parse(cached) as UserInfo
      devLog('[authHelpers] Restored cached user info:', user.email)
      return user
    }
  } catch (err) {
    devWarn('[authHelpers] Failed to restore cached user info:', err)
  }
  return null
}

/**
 * Classify an error to determine if it's a transient/network error
 * that should NOT clear auth state, or an auth error that should.
 */
export function classifyAuthError(error: unknown): {
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

  devLog('[authHelpers] classifyAuthError analyzing:', {
    message,
    code,
    httpStatus,
    errorCode,
    errorName: errorObj?.name,
  })

  // Network errors - API is unreachable (uses shared utility)
  if (checkIsNetworkError(errorObj)) {
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
 * Parse JWT token to extract expiry and user info
 * @returns Parsed token data or null if invalid
 */
export function parseToken(token: string): { exp: number; user: UserInfo } | null {
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

    devLog('[authHelpers] parseToken success:', {
      userId: parsed.user.id,
      email: parsed.user.email,
      role: parsed.user.role,
      expiresAt: new Date(parsed.exp).toISOString(),
    })

    return parsed
  } catch (err) {
    console.error('[authHelpers] parseToken failed:', err)
    return null
  }
}
