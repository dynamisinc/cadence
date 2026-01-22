/**
 * ProfileMenu Component
 *
 * User profile dropdown showing:
 * - Avatar with user initials
 * - User's HSEEP role
 * - Logout option
 */

import React, { useState } from 'react'
import {
  Box,
  Button,
  Menu,
  Typography,
  Divider,
  Avatar,
  MenuItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChevronDown, faRightFromBracket } from '@fortawesome/free-solid-svg-icons'
import { cobraTheme } from '../../theme/cobraTheme'
import { useAuth } from '../../contexts/AuthContext'

/**
 * Get user initials from full name
 */
const getInitials = (fullName: string): string => {
  const names = fullName.trim().split(/\s+/)
  if (names.length === 0 || !names[0]) return '?'
  if (names.length === 1) return names[0].charAt(0).toUpperCase()
  return (names[0].charAt(0) + names[names.length - 1].charAt(0)).toUpperCase()
}

/**
 * Format HSEEP role for display (convert from API format to readable)
 */
const formatRole = (role: string): string => {
  // Convert from API format (e.g., "ExerciseDirector") to display format (e.g., "Exercise Director")
  const formatted = role
    .replace(/([A-Z])/g, ' $1')
    .trim()
  return formatted
}

export const ProfileMenu: React.FC = () => {
  const { user, logout } = useAuth()
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)

  const open = Boolean(anchorEl)

  // Default to Guest if no user
  const accountEmail = user?.email || 'guest@cadence.app'
  const accountFullName = user?.displayName || 'Guest User'
  const accountRole = user?.role ? formatRole(user.role) : 'No Role Assigned'

  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget)
  }

  const handleClose = () => {
    setAnchorEl(null)
  }

  const handleLogout = async () => {
    handleClose()
    await logout()
  }

  const userInitials = getInitials(accountFullName)

  return (
    <>
      <Button
        onClick={handleClick}
        data-testid="profile-menu-button"
        sx={{
          color: 'white',
          textTransform: 'none',
          display: 'flex',
          alignItems: 'center',
          gap: 1,
          '&:hover': {
            backgroundColor: 'rgba(255, 255, 255, 0.1)',
          },
        }}
      >
        <Avatar
          data-testid="profile-avatar"
          sx={{
            width: 32,
            height: 32,
            fontSize: '0.875rem',
            fontWeight: 'bold',
            backgroundColor: cobraTheme.palette.secondary.main,
            color: '#ffffff',
          }}
        >
          {userInitials}
        </Avatar>
        <Box
          sx={{
            display: { xs: 'none', sm: 'flex' },
            flexDirection: 'column',
            alignItems: 'flex-start',
          }}
        >
          <Typography
            variant="body2"
            sx={{ fontWeight: 'bold', lineHeight: 1.2 }}
          >
            {accountFullName}
          </Typography>
          <Typography variant="caption" sx={{ opacity: 0.8, lineHeight: 1.2 }}>
            {accountRole}
          </Typography>
        </Box>
        <FontAwesomeIcon icon={faChevronDown} size="sm" />
      </Button>

      <Menu
        anchorEl={anchorEl}
        open={open}
        onClose={handleClose}
        data-testid="profile-menu-dropdown"
        PaperProps={{
          sx: {
            minWidth: 300,
            maxWidth: 360,
          },
        }}
      >
        {/* Header with Avatar and Account Info */}
        <Box sx={{ px: 2, py: 1.5 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <Avatar
              sx={{
                width: 48,
                height: 48,
                fontSize: '1.25rem',
                fontWeight: 'bold',
                backgroundColor: cobraTheme.palette.buttonPrimary.main,
                color: '#ffffff',
              }}
            >
              {userInitials}
            </Avatar>
            <Box sx={{ flex: 1 }}>
              <Typography
                variant="body1"
                sx={{ fontWeight: 'bold', lineHeight: 1.2 }}
              >
                {accountFullName}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {accountEmail}
              </Typography>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
                <strong>Role:</strong> {accountRole}
              </Typography>
            </Box>
          </Box>
        </Box>

        <Divider />

        {/* Logout Button (only show when authenticated) */}
        {user && (
          <MenuItem onClick={handleLogout} data-testid="logout-button">
            <ListItemIcon>
              <FontAwesomeIcon icon={faRightFromBracket} />
            </ListItemIcon>
            <ListItemText>Logout</ListItemText>
          </MenuItem>
        )}
      </Menu>
    </>
  )
}

export default ProfileMenu
