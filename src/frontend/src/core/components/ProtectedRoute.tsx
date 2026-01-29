/**
 * ProtectedRoute - Wrapper for authenticated routes
 *
 * Redirects to login if user is not authenticated.
 * Does NOT redirect if the API is simply unreachable (offline mode).
 * Optionally checks for required role.
 *
 * @module core/components
 */
import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import { useConnectivity } from '../contexts/ConnectivityContext'
import { Loading } from '../../shared/components/Loading'

interface ProtectedRouteProps {
  /** Required role to access this route (optional) */
  requiredRole?: string;
  /** Content to render if authorized */
  children: React.ReactNode;
}

/**
 * ProtectedRoute component
 * Shows loading state during auth check, redirects to login if unauthenticated
 * When offline, renders children (allows navigation with stale data) rather than
 * redirecting to login - the user may have a valid session we can't verify.
 */
export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  requiredRole,
  children,
}) => {
  const { isAuthenticated, isLoading, user } = useAuth()
  const { isApiReachable } = useConnectivity()
  const location = useLocation()

  // Show loading state during initial auth check
  if (isLoading) {
    return <Loading />
  }

  // If not authenticated but API is unreachable, don't redirect to login
  // The user may have a valid session we simply can't verify right now
  // Allow them to view cached/stale data while offline
  if (!isAuthenticated && !isApiReachable) {
    // Render children - pages will show "offline" states or cached data
    return <>{children}</>
  }

  // Redirect to login if not authenticated (and API IS reachable, so auth failure is real)
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  // Check role if required (Admin can access everything)
  if (requiredRole && user?.role !== requiredRole && user?.role !== 'Admin') {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}
