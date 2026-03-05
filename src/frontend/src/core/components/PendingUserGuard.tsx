/**
 * PendingUserGuard - Redirects users without organization membership to /pending
 *
 * Also handles the "needs org selection" state: user has memberships but no org
 * is currently selected. Shows an org picker dialog instead of letting API calls fail.
 *
 * @module core/components
 */
import React, { useState } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import {
  Box,
  Dialog,
  DialogContent,
  DialogTitle,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Typography,
  CircularProgress,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBuilding, faArrowRight } from '@fortawesome/free-solid-svg-icons'
import { useOrganization } from '../../contexts/OrganizationContext'
import { useAuth } from '../../contexts/AuthContext'
import { Loading } from '../../shared/components/Loading'
import { notify } from '@/shared/utils/notify'

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
 * Redirects pending users to /pending page, allows access to whitelisted routes.
 * Shows an org picker dialog when user has memberships but none selected.
 */
export const PendingUserGuard: React.FC<PendingUserGuardProps> = ({ children }) => {
  const { isPending, needsOrgSelection, memberships, isLoading: isOrgLoading, switchOrganization } = useOrganization()
  const { user, isLoading: isAuthLoading, isAuthenticated } = useAuth()
  const location = useLocation()
  const [isSwitching, setIsSwitching] = useState(false)

  const handleSelectOrg = async (orgId: string) => {
    setIsSwitching(true)
    try {
      await switchOrganization(orgId)
      // Page will reload after switch
    } catch {
      notify.error('Failed to select organization. Please try again.')
      setIsSwitching(false)
    }
  }

  // Show loading while checking organization status
  if (isAuthLoading || (isAuthenticated && isOrgLoading)) {
    return <Loading />
  }

  // Not authenticated - let ProtectedRoute handle it
  if (!isAuthenticated) {
    return <>{children}</>
  }

  // No org selected — show org picker (applies to ALL users including SysAdmins)
  // SysAdmins still need an org context for org-scoped pages like Members, Exercises, etc.
  if (needsOrgSelection) {
    return (
      <>
        {children}
        <Dialog
          open
          maxWidth="xs"
          fullWidth
          disableEscapeKeyDown
          slotProps={{ backdrop: { sx: { backgroundColor: 'rgba(0, 0, 0, 0.7)' } } }}
        >
          <DialogTitle sx={{ textAlign: 'center', pb: 1 }}>
            <FontAwesomeIcon icon={faBuilding} style={{ marginRight: 8 }} />
            Choose an Organization
          </DialogTitle>
          <DialogContent>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2, textAlign: 'center' }}>
              {memberships.length === 1
                ? 'Select your organization to continue.'
                : 'You belong to multiple organizations. Select one to continue.'}
            </Typography>
            <List disablePadding>
              {memberships.map(m => (
                <ListItemButton
                  key={m.id}
                  onClick={() => handleSelectOrg(m.organizationId)}
                  disabled={isSwitching}
                  sx={{
                    borderRadius: 1,
                    mb: 0.5,
                    border: 1,
                    borderColor: 'divider',
                  }}
                >
                  <ListItemIcon sx={{ minWidth: 36 }}>
                    <FontAwesomeIcon icon={faBuilding} />
                  </ListItemIcon>
                  <ListItemText
                    primary={m.organizationName}
                    secondary={m.role.replace('Org', '')}
                  />
                  {isSwitching ? (
                    <CircularProgress size={18} />
                  ) : (
                    <FontAwesomeIcon icon={faArrowRight} style={{ opacity: 0.4 }} />
                  )}
                </ListItemButton>
              ))}
            </List>
          </DialogContent>
        </Dialog>
      </>
    )
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
