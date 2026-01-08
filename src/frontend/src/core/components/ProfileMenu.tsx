/**
 * ProfileMenu Component
 *
 * User profile dropdown with:
 * - Avatar with user initials
 * - Account switching (for testing/demo)
 * - Role selection (Readonly, Contributor, Manage)
 *
 * For POC/demo purposes - in production, roles come from authentication.
 */

import React, { useState, useEffect } from 'react'
import {
  Box,
  Button,
  Menu,
  Typography,
  Divider,
  Radio,
  RadioGroup,
  FormControlLabel,
  FormControl,
  FormLabel,
  Avatar,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChevronDown, faUserPen } from '@fortawesome/free-solid-svg-icons'
import { PermissionRole } from '../../types'
import { cobraTheme } from '../../theme/cobraTheme'

interface ProfileMenuProps {
  onProfileChange?: (role: PermissionRole) => void;
}

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
 * Get stored profile from localStorage
 */
const getStoredProfile = () => {
  try {
    const stored = localStorage.getItem('dynamisUserProfile')
    if (stored) {
      return JSON.parse(stored)
    }
  } catch (error) {
    console.error('Failed to load stored profile:', error)
  }
  return {
    role: PermissionRole.CONTRIBUTOR,
    email: 'user@dynamis.com',
    fullName: 'Demo User',
  }
}

/**
 * Save profile to localStorage
 */
const saveProfile = (role: PermissionRole, email: string, fullName: string) => {
  try {
    localStorage.setItem(
      'dynamisUserProfile',
      JSON.stringify({ role, email, fullName }),
    )
    window.dispatchEvent(new Event('profile-changed'))
  } catch (error) {
    console.error('Failed to save profile:', error)
  }
}

export const ProfileMenu: React.FC<ProfileMenuProps> = ({
  onProfileChange,
}) => {
  const storedProfile = getStoredProfile()

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [selectedRole, setSelectedRole] = useState<PermissionRole>(
    storedProfile.role,
  )
  const [accountEmail, setAccountEmail] = useState<string>(storedProfile.email)
  const [accountFullName, setAccountFullName] = useState<string>(
    storedProfile.fullName,
  )

  // Account switch dialog state
  const [accountDialogOpen, setAccountDialogOpen] = useState(false)
  const [tempEmail, setTempEmail] = useState('')
  const [tempFullName, setTempFullName] = useState('')

  const open = Boolean(anchorEl)

  // Sync with storage on mount
  useEffect(() => {
    const handleProfileChange = () => {
      const profile = getStoredProfile()
      setSelectedRole(profile.role)
      setAccountEmail(profile.email)
      setAccountFullName(profile.fullName)
    }
    window.addEventListener('profileChanged', handleProfileChange)
    return () =>
      window.removeEventListener('profileChanged', handleProfileChange)
  }, [])

  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget)
  }

  const handleClose = () => {
    setAnchorEl(null)
  }

  const handleRoleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const newRole = event.target.value as PermissionRole
    setSelectedRole(newRole)
    saveProfile(newRole, accountEmail, accountFullName)
    onProfileChange?.(newRole)
  }

  const handleOpenAccountDialog = () => {
    setTempEmail(accountEmail)
    setTempFullName(accountFullName)
    setAccountDialogOpen(true)
  }

  const handleCloseAccountDialog = () => {
    setAccountDialogOpen(false)
    setTempEmail('')
    setTempFullName('')
  }

  const handleSaveAccount = () => {
    const newEmail = tempEmail.trim() || 'user@dynamis.com'
    const newFullName = tempFullName.trim() || newEmail.split('@')[0]

    setAccountEmail(newEmail)
    setAccountFullName(newFullName)
    saveProfile(selectedRole, newEmail, newFullName)

    handleCloseAccountDialog()
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
            {selectedRole}
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
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1 }}>
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
            </Box>
            <Button
              size="small"
              onClick={handleOpenAccountDialog}
              sx={{ minWidth: 'auto', p: 1 }}
              title="Switch Account"
              data-testid="switch-account-button"
            >
              <FontAwesomeIcon icon={faUserPen} />
            </Button>
          </Box>
          <Typography variant="caption" color="text.secondary">
            Profile Settings (Demo) - For testing purposes
          </Typography>
        </Box>

        <Divider />

        {/* Permission Role Selection */}
        <Box sx={{ px: 2, py: 1.5 }}>
          <FormControl component="fieldset">
            <FormLabel
              component="legend"
              sx={{ fontWeight: 'bold', fontSize: '0.875rem', mb: 0.5 }}
            >
              Permission Role
            </FormLabel>
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ mb: 1, display: 'block' }}
            >
              Controls access to features
            </Typography>
            <RadioGroup value={selectedRole} onChange={handleRoleChange}>
              <FormControlLabel
                value={PermissionRole.READONLY}
                control={<Radio size="small" />}
                label={
                  <Box component="span" sx={{ display: 'block' }}>
                    <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                      Readonly
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      View only access
                    </Typography>
                  </Box>
                }
              />
              <FormControlLabel
                value={PermissionRole.CONTRIBUTOR}
                control={<Radio size="small" />}
                label={
                  <Box component="span" sx={{ display: 'block' }}>
                    <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                      Contributor
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Create and edit content
                    </Typography>
                  </Box>
                }
              />
              <FormControlLabel
                value={PermissionRole.MANAGE}
                control={<Radio size="small" />}
                label={
                  <Box component="span" sx={{ display: 'block' }}>
                    <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                      Manage
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Full admin access
                    </Typography>
                  </Box>
                }
              />
            </RadioGroup>
          </FormControl>
        </Box>

        <Divider />

        {/* Current Selection Summary */}
        <Box
          sx={{
            px: 2,
            py: 1.5,
            backgroundColor: cobraTheme.palette.grid.light,
          }}
        >
          <Typography
            variant="caption"
            sx={{ fontWeight: 'bold', display: 'block', mb: 0.5 }}
          >
            Current Profile:
          </Typography>
          <Typography variant="caption">
            <strong>Account:</strong> {accountFullName} ({accountEmail})
          </Typography>
          <br />
          <Typography variant="caption">
            <strong>Role:</strong> {selectedRole}
          </Typography>
        </Box>
      </Menu>

      {/* Account Switch Dialog */}
      <Dialog
        open={accountDialogOpen}
        onClose={handleCloseAccountDialog}
        maxWidth="xs"
        fullWidth
        data-testid="account-switch-dialog"
      >
        <DialogTitle>Switch Account</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Enter an email address and name to simulate a different user.
          </Typography>
          <TextField
            autoFocus
            margin="dense"
            label="Email Address"
            type="email"
            fullWidth
            variant="outlined"
            value={tempEmail}
            onChange={e => setTempEmail(e.target.value)}
            placeholder="user@dynamis.com"
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Full Name"
            type="text"
            fullWidth
            variant="outlined"
            value={tempFullName}
            onChange={e => setTempFullName(e.target.value)}
            placeholder="John Doe"
            helperText="If left blank, will use email prefix as name"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseAccountDialog}>Cancel</Button>
          <Button onClick={handleSaveAccount} variant="contained">
            Switch Account
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}

export default ProfileMenu
