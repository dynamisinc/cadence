/**
 * OrgAdminRoute - Wrapper for organization admin routes
 *
 * Restricts access to users with OrgAdmin role in their current organization
 * OR users with SystemRole.Admin (platform admins).
 *
 * @module core/components
 */
import React from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import { useOrganization } from '../../contexts/OrganizationContext'
import { Loading } from '../../shared/components/Loading'

interface OrgAdminRouteProps {
  /** Content to render if authorized */
  children: React.ReactNode;
}

/**
 * OrgAdminRoute component
 *
 * Shows loading state during org context initialization.
 * Redirects to home if user is not an OrgAdmin or SystemAdmin.
 */
export const OrgAdminRoute: React.FC<OrgAdminRouteProps> = ({ children }) => {
  const { user, isLoading: isAuthLoading } = useAuth()
  const { currentOrg, isLoading: isOrgLoading } = useOrganization()

  // Show loading state during initial context loading
  if (isAuthLoading || isOrgLoading) {
    return <Loading />
  }

  // System Admins can always access organization routes
  if (user?.role === 'Admin') {
    return <>{children}</>
  }

  // OrgAdmins can access their organization's routes
  if (currentOrg?.role === 'OrgAdmin') {
    return <>{children}</>
  }

  // Not authorized - redirect to home
  return <Navigate to="/" replace />
}
