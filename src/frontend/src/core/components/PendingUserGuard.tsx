/**
 * PendingUserGuard - Redirects users without organization membership to /pending
 *
 * Users in "pending" status (no organization assignment) are redirected to the
 * pending user page, except for certain allowed routes like settings and logout.
 *
 * @module core/components
 */
import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useOrganization } from '../../contexts/OrganizationContext'
import { useAuth } from '../../contexts/AuthContext'
import { Loading } from '../../shared/components/Loading'

interface PendingUserGuardProps {
  children: React.ReactNode;
}

// Routes that pending users can access
const ALLOWED_ROUTES = [
  '/pending',
  '/settings',
  '/login',
  '/logout',
  '/register',
  '/forgot-password',
  '/reset-password',
  '/invite',
]

/**
 * PendingUserGuard component
 * Redirects pending users to /pending page, allows access to whitelisted routes
 */
export const PendingUserGuard: React.FC<PendingUserGuardProps> = ({ children }) => {
  const { isPending, isLoading: isOrgLoading } = useOrganization()
  const { user, isLoading: isAuthLoading, isAuthenticated } = useAuth()
  const location = useLocation()

  // Show loading while checking organization status
  if (isAuthLoading || (isAuthenticated && isOrgLoading)) {
    return <Loading />
  }

  // Not authenticated - let ProtectedRoute handle it
  if (!isAuthenticated) {
    return <>{children}</>
  }

  // SysAdmins bypass pending check (they can access all orgs)
  if (user?.role === 'Admin') {
    return <>{children}</>
  }

  // Check if current route is allowed for pending users
  const isAllowedRoute = ALLOWED_ROUTES.some(route =>
    location.pathname.startsWith(route),
  )

  // Pending user trying to access restricted route - redirect to /pending
  if (isPending && !isAllowedRoute) {
    return <Navigate to="/pending" replace />
  }

  return <>{children}</>
}
