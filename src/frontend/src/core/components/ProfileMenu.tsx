/**
 * ProfileMenu Component
 *
 * User profile dropdown showing:
 * - Avatar with user initials
 * - User's system role
 * - Exercise role assignments (if any)
 * - Logout option
 */

import React, { useState, useEffect } from 'react'
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
  Chip,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChevronDown, faRightFromBracket, faDumbbell, faPlay, faGear, faCircleInfo, faCommentDots } from '@fortawesome/free-solid-svg-icons'
import { cobraTheme } from '../../theme/cobraTheme'
import { useAuth } from '../../contexts/AuthContext'
import { roleResolutionService, getRoleColor, getRoleDisplayName } from '@/features/auth'
import type { ExerciseRole, ExerciseAssignmentDto } from '@/features/auth'
import { useExerciseNavigation } from '@/shared/contexts'
import { UserSettingsDialog } from '@/features/settings'
import { FeedbackDialog } from '@/features/feedback'
import { useNavigate } from 'react-router-dom'

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
  // Convert "ExerciseDirector" to "Exercise Director"
  const formatted = role
    .replace(/([A-Z])/g, ' $1')
    .trim()
  return formatted
}

export const ProfileMenu: React.FC = () => {
  const { user, logout } = useAuth()
  const { currentExercise, isInExerciseContext } = useExerciseNavigation()
  const navigate = useNavigate()
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const [exerciseAssignments, setExerciseAssignments] = useState<ExerciseAssignmentDto[]>([])
  const [isLoadingAssignments, setIsLoadingAssignments] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [feedbackOpen, setFeedbackOpen] = useState(false)

  const open = Boolean(anchorEl)

  // Fetch exercise assignments when menu opens
  useEffect(() => {
    if (!open || !user) {
      return
    }

    const fetchAssignments = async () => {
      try {
        setIsLoadingAssignments(true)
        const assignments = await roleResolutionService.getUserExerciseAssignments(user.id)
        setExerciseAssignments(assignments)
      } catch (error) {
        console.error('Failed to fetch exercise assignments:', error)
        setExerciseAssignments([])
      } finally {
        setIsLoadingAssignments(false)
      }
    }

    fetchAssignments()
  }, [open, user])

  // Don't render if no user - prevents showing "Guest User" when API is offline
  if (!user) {
    return null
  }

  const accountEmail = user.email
  const accountFullName = user.displayName
  const accountRole = formatRole(user.role)

  // Current exercise role for display
  const currentExerciseRole = isInExerciseContext && currentExercise?.userRole
    ? getRoleDisplayName(currentExercise.userRole)
    : null

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

  const handleOpenSettings = () => {
    handleClose()
    setSettingsOpen(true)
  }

  const handleCloseSettings = () => {
    setSettingsOpen(false)
  }

  const handleOpenFeedback = () => {
    handleClose()
    setFeedbackOpen(true)
  }

  const handleCloseFeedback = () => {
    setFeedbackOpen(false)
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
            {currentExerciseRole || accountRole}
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
                <strong>System Role:</strong> {accountRole}
              </Typography>
            </Box>
          </Box>
        </Box>

        <Divider />

        {/* Current Exercise Context - Show prominently when in exercise */}
        {isInExerciseContext && currentExercise && (
          <>
            <Box
              sx={{
                px: 2,
                py: 1.5,
                bgcolor: `${getRoleColor(currentExercise.userRole)}.50`,
                borderLeft: '4px solid',
                borderLeftColor: `${getRoleColor(currentExercise.userRole)}.main`,
              }}
            >
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                <FontAwesomeIcon icon={faPlay} size="xs" />
                <Typography variant="caption" fontWeight={600} color="text.secondary">
                  Current Exercise
                </Typography>
              </Stack>
              <Typography variant="body2" fontWeight={600} sx={{ mb: 0.5 }}>
                {currentExercise.name}
              </Typography>
              <Chip
                label={getRoleDisplayName(currentExercise.userRole)}
                size="small"
                color={getRoleColor(currentExercise.userRole)}
                sx={{
                  height: 22,
                  fontSize: '0.75rem',
                  fontWeight: 600,
                }}
              />
            </Box>
            <Divider />
          </>
        )}

        {/* Exercise Assignments Section */}
        {user && (
          <Box sx={{ px: 2, py: 1.5 }}>
            <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
              <FontAwesomeIcon icon={faDumbbell} size="sm" />
              <Typography variant="caption" fontWeight={600} color="text.secondary">
                Exercise Assignments
              </Typography>
            </Stack>
            {isLoadingAssignments ? (
              <Typography variant="caption" color="text.secondary">
                Loading...
              </Typography>
            ) : exerciseAssignments.length === 0 ? (
              <Typography variant="caption" color="text.secondary" fontStyle="italic">
                No active exercise assignments
              </Typography>
            ) : (
              <Stack spacing={1}>
                {exerciseAssignments.map(assignment => (
                  <Box
                    key={assignment.exerciseId}
                    sx={{
                      p: 1,
                      bgcolor: 'grey.50',
                      borderRadius: 1,
                      borderLeft: '3px solid',
                      borderLeftColor: `${getRoleColor(assignment.exerciseRole as ExerciseRole)}.main`,
                    }}
                  >
                    <Typography variant="body2" fontWeight={500} sx={{ mb: 0.5 }}>
                      {assignment.exerciseName}
                    </Typography>
                    <Chip
                      label={getRoleDisplayName(assignment.exerciseRole as ExerciseRole)}
                      size="small"
                      color={getRoleColor(assignment.exerciseRole as ExerciseRole)}
                      sx={{
                        height: 20,
                        fontSize: '0.7rem',
                        fontWeight: 600,
                      }}
                    />
                  </Box>
                ))}
              </Stack>
            )}
          </Box>
        )}

        <Divider />

        {/* Settings */}
        {user && (
          <MenuItem onClick={handleOpenSettings} data-testid="settings-button">
            <ListItemIcon>
              <FontAwesomeIcon icon={faGear} />
            </ListItemIcon>
            <ListItemText>Settings</ListItemText>
          </MenuItem>
        )}

        {/* Send Feedback */}
        {user && (
          <MenuItem onClick={handleOpenFeedback} data-testid="feedback-button">
            <ListItemIcon>
              <FontAwesomeIcon icon={faCommentDots} />
            </ListItemIcon>
            <ListItemText>Send Feedback</ListItemText>
          </MenuItem>
        )}

        {/* About */}
        {user && (
          <MenuItem
            onClick={() => {
              handleClose()
              navigate('/about')
            }}
            data-testid="about-button"
          >
            <ListItemIcon>
              <FontAwesomeIcon icon={faCircleInfo} />
            </ListItemIcon>
            <ListItemText>About</ListItemText>
          </MenuItem>
        )}

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

      {/* Settings Dialog */}
      <UserSettingsDialog open={settingsOpen} onClose={handleCloseSettings} />

      {/* Feedback Dialog */}
      <FeedbackDialog open={feedbackOpen} onClose={handleCloseFeedback} />
    </>
  )
}

export default ProfileMenu
