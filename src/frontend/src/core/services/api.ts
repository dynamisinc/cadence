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
    const correlationId = crypto.randomUUID()
    config.headers['X-Correlation-Id'] = correlationId

    // Add auth token if available
    const token = getAccessToken?.()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    console.log(`[apiClient] Request: ${config.method?.toUpperCase()} ${config.url}`, {
      correlationId: correlationId.substring(0, 8),
      hasAuthToken: !!token,
      tokenLength: token?.length,
    })

    return config
  },
  error => {
    console.error('[apiClient] Request interceptor error:', error)
    return Promise.reject(error)
  },
)

/**
 * Response interceptor - handle 401 errors with automatic token refresh
 * Uses single-flight pattern to prevent multiple concurrent refresh requests
 */
apiClient.interceptors.response.use(
  response => {
    console.log(`[apiClient] Response: ${response.status} ${response.config.url}`)
    return response
  },
  async (error: AxiosError) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean }

    const errorInfo = {
      message: error.message,
      code: error.code,
      status: error.response?.status,
      url: error.config?.url,
      hasResponse: !!error.response,
    }

    console.log('[apiClient] Response error:', errorInfo)

    // If 401 and we haven't retried yet, try to refresh token
    if (error.response?.status === 401 && !originalRequest?._retry && refreshAccessToken) {
      console.log('[apiClient] Got 401, attempting token refresh...')
      originalRequest._retry = true

      try {
        // Single-flight: reuse existing refresh promise if one is in progress
        // This prevents multiple concurrent 401 responses from triggering
        // multiple refresh requests to the server
        if (!refreshPromise) {
          console.log('[apiClient] Starting new refresh request')
          refreshPromise = refreshAccessToken().finally(() => {
            refreshPromise = null
          })
        } else {
          console.log('[apiClient] Joining existing refresh request')
        }

        // All concurrent requests wait for the same refresh promise
        await refreshPromise
        console.log('[apiClient] Token refresh succeeded, retrying original request')

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
        const refreshErrorInfo = {
          message: (refreshError as Error)?.message,
          code: (refreshError as AxiosError)?.code,
          status: (refreshError as AxiosError)?.response?.status,
        }
        console.log('[apiClient] Token refresh failed:', refreshErrorInfo)

        const isNetworkError =
          refreshError instanceof Error &&
          (refreshError.message === 'Network Error' ||
            refreshError.message.includes('ECONNREFUSED') ||
            refreshError.message.includes('ERR_NETWORK') ||
            refreshError.message.includes('ETIMEDOUT') ||
            refreshError.message.includes('ECONNRESET') ||
            refreshError.message.includes('Failed to fetch') ||
            refreshError.message.includes('timeout') ||
            // Axios network errors have no response
            (refreshError as AxiosError).code === 'ERR_NETWORK' ||
            (refreshError as AxiosError).code === 'ECONNABORTED' ||
            (refreshError as AxiosError).code === 'ETIMEDOUT')

        if (isNetworkError) {
          // Network error - don't redirect, let offline handling deal with it
          console.warn('[apiClient] Token refresh failed due to network error - staying in offline mode', {
            errorMessage: (refreshError as Error)?.message,
            errorCode: (refreshError as AxiosError)?.code,
          })
          return Promise.reject(error)
        }

        // Check if this is a transient server error (5xx)
        const serverStatus = (refreshError as AxiosError)?.response?.status
        if (serverStatus && serverStatus >= 500) {
          console.warn('[apiClient] Token refresh failed due to server error - staying in offline mode', {
            status: serverStatus,
          })
          return Promise.reject(error)
        }

        // Auth failure (invalid credentials, expired refresh token)
        // Only redirect if we're sure it's an auth failure
        console.log('[apiClient] Auth failure detected, will redirect to login')
        const returnUrl = window.location.pathname
        if (returnUrl !== '/login' && returnUrl !== '/register') {
          sessionStorage.setItem('returnUrl', returnUrl)
          console.log('[apiClient] Saved return URL:', returnUrl)
        }
        window.location.href = '/login?expired=true'
        return Promise.reject(error)
      }
    }

    // Detect missing organization context errors and show a friendly message
    if (error.response?.status === 400 || error.response?.status === 403) {
      const data = error.response?.data
      const message = typeof data === 'string' ? data
        : (data as Record<string, unknown>)?.message ?? (data as Record<string, unknown>)?.title ?? ''
      if (typeof message === 'string' && /organization.*context|no organization/i.test(message)) {
        const { notify } = await import('@/shared/utils/notify')
        notify.warning('Please select an organization from the header menu to continue.', {
          toastId: 'org-context-missing',
        })
      }
    }

    // Log errors for debugging
    console.error('[apiClient] API Error (not 401 or already retried):', error.response?.data || error.message)

    return Promise.reject(error)
  },
)

/**
 * Check if the API server is reachable
 * @returns true if API is reachable, false otherwise
 */
export async function checkApiHealth(): Promise<boolean> {
  console.log('[apiClient] checkApiHealth called')
  try {
    const response = await apiClient.get('/health', {
      timeout: 5000, // 5 second timeout for health check
    })
    const isHealthy = response.status === 200
    console.log('[apiClient] checkApiHealth result:', {
      status: response.status,
      isHealthy,
      data: response.data,
    })
    return isHealthy
  } catch (error) {
    const errorInfo = {
      message: (error as Error)?.message,
      code: (error as AxiosError)?.code,
      status: (error as AxiosError)?.response?.status,
    }
    console.log('[apiClient] checkApiHealth failed:', errorInfo)
    return false
  }
}

export default apiClient
