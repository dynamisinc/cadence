/**
 * Authentication Service
 *
 * API client for authentication operations.
 * Uses axios for HTTP requests with credentials support for HttpOnly cookies.
 *
 * @module features/auth
 */

import axios from 'axios'
import type {
  LoginRequest,
  RegistrationRequest,
  AuthResponse,
  AuthMethod,
} from '../types'

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5071'

/**
 * Axios instance for authentication endpoints
 * Configured with credentials support for HttpOnly cookies (refresh tokens)
 */
const authApi = axios.create({
  baseURL: `${API_BASE}/api/auth`,
  withCredentials: true, // CRITICAL: Required for HttpOnly cookie support
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // 10 second timeout for auth requests
})

// Add request/response logging for auth API
authApi.interceptors.request.use(
  config => {
    console.log(`[authService] Request: ${config.method?.toUpperCase()} ${config.url}`, {
      withCredentials: config.withCredentials,
      timeout: config.timeout,
    })
    return config
  },
  error => {
    console.error('[authService] Request error:', error)
    return Promise.reject(error)
  },
)

authApi.interceptors.response.use(
  response => {
    console.log(`[authService] Response: ${response.status} ${response.config.url}`, {
      isSuccess: response.data?.isSuccess,
      hasAccessToken: !!response.data?.accessToken,
    })
    return response
  },
  error => {
    const errorInfo = {
      message: error.message,
      code: error.code,
      status: error.response?.status,
      statusText: error.response?.statusText,
      url: error.config?.url,
      data: error.response?.data,
    }
    console.error('[authService] Response error:', errorInfo)
    return Promise.reject(error)
  },
)

export const authService = {
  /**
   * Authenticate user with email/password
   * Returns access token in response body and refresh token in HttpOnly cookie
   */
  login: async (request: LoginRequest): Promise<AuthResponse> => {
    const response = await authApi.post<AuthResponse>('/login', request)
    return response.data
  },

  /**
   * Register new user account
   * Returns access token in response body and refresh token in HttpOnly cookie
   */
  register: async (request: RegistrationRequest): Promise<AuthResponse> => {
    const response = await authApi.post<AuthResponse>('/register', request)
    return response.data
  },

  /**
   * Log out current user
   * Clears refresh token cookie on the server
   */
  logout: async (): Promise<void> => {
    await authApi.post('/logout')
  },

  /**
   * Refresh access token using refresh token from HttpOnly cookie
   * Returns new access token in response body
   */
  refreshToken: async (): Promise<AuthResponse> => {
    console.log('[authService] refreshToken called - checking cookie presence...')
    // Log cookie info (we can't read HttpOnly cookies, but we can check if any cookies exist)
    console.log('[authService] Document cookies available:', document.cookie ? 'yes (non-HttpOnly)' : 'none visible')

    const response = await authApi.post<AuthResponse>('/refresh')
    console.log('[authService] refreshToken response received:', {
      isSuccess: response.data?.isSuccess,
      hasAccessToken: !!response.data?.accessToken,
      userId: response.data?.userId,
      error: response.data?.error,
    })
    return response.data
  },

  /**
   * Request password reset email
   * Sends reset link to the provided email if account exists
   */
  requestPasswordReset: async (email: string): Promise<void> => {
    await authApi.post('/password-reset/request', { email })
  },

  /**
   * Complete password reset with token
   * Sets new password for the account associated with the token
   */
  completePasswordReset: async (token: string, newPassword: string): Promise<void> => {
    await authApi.post('/password-reset/complete', { token, newPassword })
  },

  /**
   * Get available authentication methods (Identity, Entra, etc.)
   * Used to determine which login buttons to show on the login page
   */
  getAvailableMethods: async (): Promise<AuthMethod[]> => {
    const response = await authApi.get<AuthMethod[]>('/methods')
    return response.data
  },
}
