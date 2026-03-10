/**
 * AppHeader Component
 *
 * Top navigation bar with:
 * - Mobile menu toggle (hamburger)
 * - App title/logo
 * - Connection status indicator
 * - Notification bell
 * - Profile menu integration
 *
 * Fixed position, 54px height, stays above sidebar
 */

import React from 'react'
import { AppBar, Toolbar, IconButton, Typography, Box } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { Link } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBars } from '@fortawesome/free-solid-svg-icons'
import { ProfileMenu } from '../ProfileMenu'
import { ConnectionStatusIndicator } from '../ConnectionStatusIndicator'
import { NotificationBell } from '@/features/notifications'
import { OrganizationSwitcher } from '@/shared/components/OrganizationSwitcher'

interface AppHeaderProps {
  onMobileMenuToggle: () => void;
}

export const AppHeader: React.FC<AppHeaderProps> = ({
  onMobileMenuToggle,
}) => {
  const theme = useTheme()

  return (
    <AppBar
      position="fixed"
      data-testid="app-header"
      sx={{
        height: theme.cssStyling.headerHeight,
        zIndex: theme.zIndex.drawer + 1,
        backgroundColor: theme.palette.buttonPrimary.main,
      }}
    >
      <Toolbar
        sx={{
          minHeight: `${theme.cssStyling.headerHeight}px !important`,
          height: theme.cssStyling.headerHeight,
          px: '0 !important',
          pl: '0 !important',
        }}
      >
        {/* Mobile Menu Toggle */}
        {/* COBRA exception: hamburger menu requires color="inherit" for white-on-dark header */}
        <IconButton
          color="inherit"
          onClick={onMobileMenuToggle}
          data-testid="mobile-menu-toggle"
          sx={{
            display: { md: 'none' },
            ml: 1,
          }}
        >
          <FontAwesomeIcon icon={faBars} />
        </IconButton>

        {/* Logo + Title - links to home */}
        <Link
          to="/"
          style={{ display: 'flex', alignItems: 'center', textDecoration: 'none' }}
        >
          <Box
            component="img"
            src="/icon-source-light.svg"
            alt="Cadence Logo"
            data-testid="app-logo"
            sx={{
              height: 40,
              width: 'auto',
              display: { xs: 'none', md: 'block' },
              ml: '11px',
              borderRadius: 0.5,
            }}
          />

          <Typography
            variant="h6"
            component="div"
            data-testid="app-title"
            sx={{
              fontWeight: 'bold',
              color: '#ffffff',
              pl: { xs: 1, md: 2 },
            }}
          >
            Cadence
          </Typography>
        </Link>

        {/* Spacer */}
        <Box sx={{ flexGrow: 1 }} />

        {/* Connection Status Indicator */}
        <Box sx={{ mr: 1 }}>
          <ConnectionStatusIndicator compact />
        </Box>

        {/* Organization Switcher */}
        <Box sx={{ mr: 2 }}>
          <OrganizationSwitcher />
        </Box>

        {/* Notification Bell */}
        <Box sx={{ mr: 1 }}>
          <NotificationBell />
        </Box>

        {/* Profile Menu */}
        <Box sx={{ ml: 'auto' }}>
          <ProfileMenu />
        </Box>
      </Toolbar>
    </AppBar>
  )
}

export default AppHeader
