/**
 * OrganizationSwitcher - Switch between user's organizations
 *
 * Shows current organization and allows multi-org users to switch context.
 * Single-org users see just the org name. Multi-org users see a dropdown.
 * SysAdmins can switch to ANY organization in the system.
 *
 * Features:
 * - Current org highlighted with checkmark
 * - Shows user's role in each org
 * - Loading state during switch
 * - SysAdmin mode: shows all organizations with ability to switch to any
 * - Uses COBRA styling and FontAwesome icons
 *
 * @module shared/components
 * @see docs/features/organization-management/OM-06-organization-switcher.md
 */
import { useState } from 'react'
import type { FC } from 'react'
import { Box, Typography, Menu, MenuItem, ListItemIcon, ListItemText, Divider, Chip, CircularProgress } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBuilding, faChevronDown, faCheck, faShieldHalved } from '@fortawesome/free-solid-svg-icons'
import { useQuery } from '@tanstack/react-query'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { useOrganization } from '@/contexts/OrganizationContext'
import { useAuth } from '@/contexts/AuthContext'
import { organizationService } from '@/features/organizations/services/organizationService'
import { toast } from 'react-toastify'

/**
 * Display role label in user-friendly format
 */
function formatRole(role: string): string {
  switch (role) {
    case 'OrgAdmin':
      return 'Admin'
    case 'OrgManager':
      return 'Manager'
    case 'OrgUser':
      return 'User'
    default:
      return role
  }
}

/**
 * OrganizationSwitcher component
 */
export const OrganizationSwitcher: FC = () => {
  const { currentOrg, memberships, isLoading, isPending, switchOrganization } = useOrganization()
  const { user } = useAuth()
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [isSwitching, setIsSwitching] = useState(false)

  // Check if user is SysAdmin
  const isSysAdmin = user?.role === 'Admin'

  // Fetch all organizations for SysAdmins
  const { data: allOrgsData, isLoading: allOrgsLoading } = useQuery({
    queryKey: ['all-organizations'],
    queryFn: () => organizationService.getAll({ status: 'Active', sortBy: 'name' }),
    enabled: isSysAdmin && Boolean(anchorEl), // Only fetch when dropdown is open for SysAdmins
    staleTime: 30000, // Cache for 30 seconds
  })

  // Don't render while loading or if user is pending (and not a SysAdmin)
  if (isLoading || (isPending && !isSysAdmin) || (!currentOrg && !isSysAdmin)) {
    return null
  }

  // Get orgs that user doesn't have membership in (for SysAdmin view)
  const memberOrgIds = new Set(memberships.map(m => m.organizationId))
  const otherOrgs = allOrgsData?.items.filter(org => !memberOrgIds.has(org.id)) || []

  // SysAdmins always get the dropdown (they can switch to any org)
  // Regular users need multiple memberships
  const hasMultipleOptions = memberships.length > 1 || isSysAdmin
  const menuOpen = Boolean(anchorEl)

  const handleOpenMenu = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget)
  }

  const handleCloseMenu = () => {
    setAnchorEl(null)
  }

  const handleSwitchOrg = async (orgId: string) => {
    if (currentOrg && orgId === currentOrg.id) {
      handleCloseMenu()
      return
    }

    setIsSwitching(true)
    handleCloseMenu()

    try {
      await switchOrganization(orgId)
      // Page will reload after successful switch
    } catch (error) {
      console.error('[OrganizationSwitcher] Failed to switch organization:', error)
      toast.error('Failed to switch organization. Please try again.')
      setIsSwitching(false)
    }
  }

  // Single org and not SysAdmin - just display the name
  if (!hasMultipleOptions) {
    return (
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1,
          px: 1,
        }}
      >
        <FontAwesomeIcon icon={faBuilding} size="sm" />
        <Typography variant="body2" fontWeight={500} color="inherit">
          {currentOrg?.name || 'No Organization'}
        </Typography>
      </Box>
    )
  }

  // Multi-org or SysAdmin - show dropdown button
  return (
    <>
      <CobraPrimaryButton
        onClick={handleOpenMenu}
        endIcon={<FontAwesomeIcon icon={faChevronDown} size="sm" />}
        startIcon={<FontAwesomeIcon icon={isSysAdmin ? faShieldHalved : faBuilding} size="sm" />}
        aria-controls={menuOpen ? 'org-menu' : undefined}
        aria-haspopup="true"
        aria-expanded={menuOpen ? 'true' : undefined}
        disabled={isSwitching}
        size="small"
      >
        {currentOrg?.name || 'Select Organization'}
      </CobraPrimaryButton>

      <Menu
        id="org-menu"
        anchorEl={anchorEl}
        open={menuOpen}
        onClose={handleCloseMenu}
        MenuListProps={{
          'aria-labelledby': 'org-button',
        }}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'right',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'right',
        }}
        slotProps={{
          paper: {
            sx: { maxHeight: 400, minWidth: 280 },
          },
        }}
      >
        {/* User's own memberships */}
        {memberships.length > 0 && [
          <Box key="your-orgs-header" sx={{ px: 2, py: 1 }}>
            <Typography variant="caption" color="text.secondary">
              Your Organizations
            </Typography>
          </Box>,

          ...memberships.map(membership => (
            <MenuItem
              key={membership.id}
              onClick={() => handleSwitchOrg(membership.organizationId)}
              selected={membership.isCurrent}
            >
              <ListItemIcon>
                {membership.isCurrent ? (
                  <FontAwesomeIcon icon={faCheck} />
                ) : (
                  <Box sx={{ width: 16 }} />
                )}
              </ListItemIcon>
              <ListItemText
                primary={membership.organizationName}
                secondary={formatRole(membership.role)}
              />
            </MenuItem>
          )),
        ]}

        {/* SysAdmin: All other organizations */}
        {isSysAdmin && [
          memberships.length > 0 && otherOrgs.length > 0 && <Divider key="sysadmin-divider" sx={{ my: 1 }} />,

          <Box key="all-orgs-header" sx={{ px: 2, py: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="caption" color="text.secondary">
              All Organizations
            </Typography>
            <Chip
              label="SysAdmin"
              size="small"
              color="error"
              sx={{ height: 18, fontSize: '0.65rem' }}
            />
          </Box>,

          allOrgsLoading ? (
            <Box key="loading" sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
              <CircularProgress size={20} />
            </Box>
          ) : otherOrgs.length === 0 ? (
            <MenuItem key="no-orgs" disabled>
              <ListItemText secondary="No other organizations" />
            </MenuItem>
          ) : (
            otherOrgs.map(org => (
              <MenuItem
                key={org.id}
                onClick={() => handleSwitchOrg(org.id)}
                selected={currentOrg?.id === org.id}
              >
                <ListItemIcon>
                  {currentOrg?.id === org.id ? (
                    <FontAwesomeIcon icon={faCheck} />
                  ) : (
                    <Box sx={{ width: 16 }} />
                  )}
                </ListItemIcon>
                <ListItemText
                  primary={org.name}
                  secondary={`${org.userCount} members`}
                />
              </MenuItem>
            ))
          ),
        ]}
      </Menu>

      {/* Loading overlay during switch */}
      {isSwitching && (
        <Box
          sx={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            bgcolor: 'rgba(0, 0, 0, 0.5)',
            zIndex: 9999,
          }}
        >
          <Box
            sx={{
              bgcolor: 'background.paper',
              p: 3,
              borderRadius: 1,
              textAlign: 'center',
            }}
          >
            <Typography variant="h6">Switching organization...</Typography>
          </Box>
        </Box>
      )}
    </>
  )
}
