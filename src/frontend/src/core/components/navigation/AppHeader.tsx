/**
 * AppHeader Component
 *
 * Top navigation bar with:
 * - Mobile menu toggle (hamburger)
 * - App title/logo
 * - Profile menu integration
 *
 * Fixed position, 54px height, stays above sidebar
 */

import React from 'react'
import { AppBar, Toolbar, IconButton, Typography, Box } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBars } from '@fortawesome/free-solid-svg-icons'
import { ProfileMenu } from '../ProfileMenu'
import { PermissionRole } from '../../../types'

interface AppHeaderProps {
  onMobileMenuToggle: () => void;
  onProfileChange?: (role: PermissionRole) => void;
}

export const AppHeader: React.FC<AppHeaderProps> = ({
  onMobileMenuToggle,
  onProfileChange,
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

        {/* Logo - visible on desktop, centered with collapsed sidebar icons */}
        <Box
          component="img"
          src="/dynamis-logo.jpg"
          alt="Dynamis Logo"
          data-testid="app-logo"
          sx={{
            height: 40,
            width: 'auto',
            display: { xs: 'none', md: 'block' },
            ml: '11px',
            borderRadius: 0.5,
          }}
        />

        {/* App Title */}
        <Typography
          variant="h6"
          component="div"
          data-testid="app-title"
          sx={{
            fontWeight: 'bold',
            color: '#ffffff',
            flexGrow: 1,
            pl: { xs: 1, md: 2 },
          }}
        >
          Dynamis Reference App
        </Typography>

        {/* Profile Menu */}
        <Box sx={{ ml: 'auto' }}>
          <ProfileMenu onProfileChange={onProfileChange} />
        </Box>
      </Toolbar>
    </AppBar>
  )
}

export default AppHeader
