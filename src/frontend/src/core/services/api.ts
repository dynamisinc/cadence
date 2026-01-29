/**
 * Axios API Client Configuration
 *
 * Base configuration for all API calls with authentication support.
 * The baseURL is set from environment variables.
 *
 * Environment Configuration:
 * - Development: VITE_API_URL=http://localhost:5071 (local backend)
 * - Production: VITE_API_URL= (empty for same-origin SWA deployment)
 *
 * Authentication:
 * - Access tokens stored in memory (React state)
 * - Refresh tokens in HttpOnly cookies (managed by browser)
 * - Automatic token refresh on 401 errors
 * - Correlation IDs for request tracing
 *
 * @module core/services
 */
import axios, { type AxiosError } from 'axios'

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5071'

/**
 * Main axios instance for API requests
 * Configured with credentials support for HttpOnly cookies
 */
export const apiClient = axios.create({
  baseURL: `${API_BASE}/api`,
  withCredentials: true, // Required for HttpOnly cookie support
  headers: {
    'Content-Type': 'application/json',
  },
})

/**
 * Token getter and refresher functions
 * These are set by the AuthProvider via setAuthInterceptors()
 */
let getAccessToken: (() => string | null) | null = null
let refreshAccessToken: (() => Promise<void>) | null = null

/**
 * Single-flight pattern for token refresh
 * Ensures that only one refresh request is made even if multiple
 * concurrent 401 responses trigger refresh attempts
 */
let refreshPromise: Promise<void> | null = null

/**
 * Configure auth interceptors with token getter and refresher
 * Must be called by AuthProvider on mount
 */
export function setAuthInterceptors(
  tokenGetter: () => string | null,
  tokenRefresher: () => Promise<void>,
): void {
  getAccessToken = tokenGetter
  refreshAccessToken = tokenRefresher
}

/**
 * Request interceptor - add Authorization header and correlation ID
 */
apiClient.interceptors.request.use(
  config => {
    // Add correlation ID for request tracing
    config.headers['X-Correlation-Id'] = crypto.randomUUID()

    // Add auth token if available
    const token = getAccessToken?.()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  },
  error => Promise.reject(error),
)

/**
 * Response interceptor - handle 401 errors with automatic token refresh
 * Uses single-flight pattern to prevent multiple concurrent refresh requests
 */
apiClient.interceptors.response.use(
  response => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean }

    // If 401 and we haven't retried yet, try to refresh token
    if (error.response?.status === 401 && !originalRequest?._retry && refreshAccessToken) {
      originalRequest._retry = true

      try {
        // Single-flight: reuse existing refresh promise if one is in progress
        // This prevents multiple concurrent 401 responses from triggering
        // multiple refresh requests to the server
        if (!refreshPromise) {
          refreshPromise = refreshAccessToken().finally(() => {
            refreshPromise = null
          })
        }

        // All concurrent requests wait for the same refresh promise
        await refreshPromise

        // Retry original request with new token
        const token = getAccessToken?.()
        if (token && originalRequest) {
          originalRequest.headers = originalRequest.headers || {}
          originalRequest.headers.Authorization = `Bearer ${token}`
          return apiClient(originalRequest)
        }
      } catch (refreshError) {
        // Distinguish between network errors and actual auth failures
        // Network errors (API unreachable) should NOT redirect to login - allow offline mode
        const isNetworkError =
          refreshError instanceof Error &&
          (refreshError.message === 'Network Error' ||
            refreshError.message.includes('ECONNREFUSED') ||
            refreshError.message.includes('ERR_NETWORK') ||
            // Axios network errors have no response
            (refreshError as AxiosError).code === 'ERR_NETWORK' ||
            (refreshError as AxiosError).code === 'ECONNABORTED')

        if (isNetworkError) {
          // Network error - don't redirect, let offline handling deal with it
          console.warn('Token refresh failed due to network error - staying in offline mode')
          return Promise.reject(error)
        }

        // Auth failure (invalid credentials, expired refresh token) - redirect to login
        const returnUrl = window.location.pathname
        if (returnUrl !== '/login' && returnUrl !== '/register') {
          sessionStorage.setItem('returnUrl', returnUrl)
        }
        window.location.href = '/login?expired=true'
        return Promise.reject(error)
      }
    }

    // Log errors for debugging
    console.error('API Error:', error.response?.data || error.message)

    return Promise.reject(error)
  },
)

/**
 * Check if the API server is reachable
 * @returns true if API is reachable, false otherwise
 */
export async function checkApiHealth(): Promise<boolean> {
  try {
    const response = await apiClient.get('/health', {
      timeout: 5000, // 5 second timeout for health check
    })
    return response.status === 200
  } catch {
    return false
  }
}

export default apiClient
