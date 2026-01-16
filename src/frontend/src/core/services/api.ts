import axios from 'axios'

/**
 * Axios API Client Configuration
 *
 * Base configuration for all API calls.
 * The baseURL is set from environment variables.
 *
 * Environment Configuration:
 * - Development: VITE_API_URL=http://localhost:5071 (Azure Functions local)
 * - Production: VITE_API_URL= (empty for same-origin SWA deployment)
 *
 * IMPORTANT: Service files include '/api/' in their paths.
 */
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
  headers: {
    'Content-Type': 'application/json',
  },
})

/**
 * Check if the API server is reachable
 * @returns true if API is reachable, false otherwise
 */
export async function checkApiHealth(): Promise<boolean> {
  try {
    const response = await apiClient.get('/api/health', {
      timeout: 5000, // 5 second timeout for health check
    })
    return response.status === 200
  } catch {
    return false
  }
}

// Request interceptor for adding auth headers, correlation IDs, etc.
apiClient.interceptors.request.use(
  config => {
    // Add correlation ID for request tracing
    config.headers['X-Correlation-Id'] = crypto.randomUUID()

    // Add auth token if available (future: integrate with auth provider)
    const token = localStorage.getItem('authToken')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  },
  error => Promise.reject(error),
)

// Response interceptor for handling errors globally
apiClient.interceptors.response.use(
  response => response,
  error => {
    // Log errors for debugging
    console.error('API Error:', error.response?.data || error.message)

    // Handle specific error codes
    if (error.response?.status === 401) {
      // Handle unauthorized - redirect to login, etc.
      console.warn('Unauthorized request')
    }

    return Promise.reject(error)
  },
)

export default apiClient
