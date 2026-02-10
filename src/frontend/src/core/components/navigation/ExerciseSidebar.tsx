/**
 * ExerciseSidebar Component
 *
 * Sidebar variant shown when user is in exercise context.
 * Features:
 * - Back button to exit exercise context
 * - Exercise name display (truncated if long)
 * - Compact clock display with real-time updates
 * - Status badge (Active/Paused/Not Started/Completed)
 * - Role-filtered exercise menu items
 *
 * @see docs/features/navigation-shell/S03-in-exercise-context-navigation.md
 * @see docs/features/navigation-shell/S04-exercise-header-with-clock.md
 */

import React, { useMemo } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import {
  Box,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  IconButton,
  Tooltip,
  Typography,
  Chip,
  CircularProgress,
  useMediaQuery,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faArrowLeft,
  faChevronLeft,
  faChevronRight,
  faPlay,
  faPause,
  faStop,
  faCheck,
} from '@fortawesome/free-solid-svg-icons'
import { useExerciseClock } from '@/features/exercise-clock'
import { useExerciseNavigation } from '@/shared/contexts'
import {
  getExerciseMenuItems,
  buildExerciseMenuPath,
  type ExerciseMenuItem,
} from '@/shared/components/navigation'
import { ExerciseStatus, ExerciseClockState } from '@/types'

interface ExerciseSidebarProps {
  open: boolean
  onToggle: () => void
  mobileOpen: boolean
  onMobileClose: () => void
}

/**
 * Get status badge configuration based on clock/exercise state
 */
function getStatusBadge(
  clockState: ExerciseClockState | undefined,
  exerciseStatus: ExerciseStatus,
): { label: string; color: 'success' | 'warning' | 'default' | 'info'; icon: typeof faPlay } {
  // If exercise is completed, show completed badge
  if (exerciseStatus === ExerciseStatus.Completed) {
    return { label: 'Completed', color: 'info', icon: faCheck }
  }

  // Otherwise, show clock state
  switch (clockState) {
    case ExerciseClockState.Running:
      return { label: 'Active', color: 'success', icon: faPlay }
    case ExerciseClockState.Paused:
      return { label: 'Paused', color: 'warning', icon: faPause }
    case ExerciseClockState.Stopped:
    default:
      return { label: 'Not Started', color: 'default', icon: faStop }
  }
}

export const ExerciseSidebar: React.FC<ExerciseSidebarProps> = ({
  open,
  onToggle,
  mobileOpen,
  onMobileClose,
}) => {
  const theme = useTheme()
  const navigate = useNavigate()
  const location = useLocation()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))

  // Get exercise context
  const { currentExercise, exitExercise } = useExerciseNavigation()

  // Get clock state for the current exercise
  const { clockState, displayTime, loading: clockLoading } = useExerciseClock(
    currentExercise?.id ?? '',
  )

  // Get menu items filtered by user's role
  const userRole = currentExercise?.userRole
  const menuItems = useMemo(() => {
    if (!userRole) return []
    return getExerciseMenuItems(userRole)
  }, [userRole])

  const drawerWidth = open
    ? theme.cssStyling.drawerOpenWidth
    : theme.cssStyling.drawerClosedWidth

  /**
   * Handle back button click - exit exercise context
   */
  const handleBack = () => {
    exitExercise()
    // Navigate to exercises list or use browser history
    navigate('/exercises')
    if (isMobile) {
      onMobileClose()
    }
  }

  /**
   * Handle menu item click
   */
  const handleNavigation = (item: ExerciseMenuItem) => {
    if (!currentExercise) return
    const path = buildExerciseMenuPath(currentExercise.id, item.path)
    navigate(path)
    if (isMobile) {
      onMobileClose()
    }
  }

  /**
   * Check if menu item is active
   */
  const isActive = (item: ExerciseMenuItem): boolean => {
    if (!currentExercise) return false
    const itemPath = buildExerciseMenuPath(currentExercise.id, item.path)

    // Hub (empty path) should match exact
    if (item.path === '') {
      return location.pathname === itemPath || location.pathname === `${itemPath}/`
    }

    // Exact match always wins
    if (location.pathname === itemPath) return true

    // Prefix match, but only if no sibling item has a more specific match
    if (location.pathname.startsWith(itemPath + '/')) {
      const hasMoreSpecificMatch = menuItems.some(
        other => other.id !== item.id
          && other.path.startsWith(item.path + '/')
          && location.pathname.startsWith(buildExerciseMenuPath(currentExercise.id, other.path)),
      )
      return !hasMoreSpecificMatch
    }

    return false
  }

  // Don't render if no exercise context
  if (!currentExercise) {
    return null
  }

  const statusBadge = getStatusBadge(clockState?.state, currentExercise.status)

  // Sidebar content
  const drawerContent = (
    <Box
      data-testid="exercise-sidebar-content"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        pt: `${theme.cssStyling.headerHeight}px`,
      }}
    >
      {/* Toggle Button (Desktop only) */}
      {!isMobile && (
        <Box
          sx={{
            display: 'flex',
            justifyContent: open ? 'flex-end' : 'center',
            alignItems: 'center',
            minHeight: 40,
            px: 1,
            borderBottom: `1px solid ${theme.palette.divider}`,
          }}
        >
          <IconButton
            onClick={onToggle}
            data-testid="sidebar-toggle"
            size="small"
            sx={{ color: theme.palette.text.secondary }}
          >
            <FontAwesomeIcon icon={open ? faChevronLeft : faChevronRight} />
          </IconButton>
        </Box>
      )}

      {/* Exercise Header Section */}
      <Box sx={{ p: 2, borderBottom: `1px solid ${theme.palette.divider}` }}>
        {/* Back Button */}
        <Box
          onClick={handleBack}
          data-testid="exercise-sidebar-back"
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1,
            cursor: 'pointer',
            color: theme.palette.text.secondary,
            mb: 2,
            '&:hover': {
              color: theme.palette.buttonPrimary.main,
            },
          }}
        >
          <FontAwesomeIcon icon={faArrowLeft} size="sm" />
          {open && (
            <Typography variant="body2" sx={{ fontWeight: 500 }}>
              Back
            </Typography>
          )}
        </Box>

        {/* Exercise Name (only when open) */}
        {open && (
          <Tooltip title={currentExercise.name} placement="right">
            <Typography
              variant="subtitle1"
              data-testid="exercise-sidebar-name"
              sx={{
                fontWeight: 600,
                mb: 2,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {currentExercise.name}
            </Typography>
          </Tooltip>
        )}

        {/* Clock Display */}
        <Box
          sx={{
            display: 'flex',
            flexDirection: open ? 'row' : 'column',
            alignItems: 'center',
            justifyContent: open ? 'space-between' : 'center',
            gap: 1,
          }}
        >
          {/* Time Display */}
          {clockLoading ? (
            <CircularProgress size={16} />
          ) : (
            <Typography
              data-testid="exercise-sidebar-clock"
              sx={{
                fontFamily: 'monospace',
                fontSize: open ? '1.25rem' : '0.875rem',
                fontWeight: 600,
                color:
                  clockState?.state === ExerciseClockState.Running
                    ? theme.palette.success.main
                    : clockState?.state === ExerciseClockState.Paused
                      ? theme.palette.warning.main
                      : theme.palette.text.secondary,
              }}
            >
              {displayTime}
            </Typography>
          )}

          {/* Status Badge */}
          <Chip
            icon={<FontAwesomeIcon icon={statusBadge.icon} size="xs" />}
            label={open ? statusBadge.label : undefined}
            size="small"
            color={statusBadge.color}
            data-testid="exercise-sidebar-status"
            sx={{
              height: open ? 24 : 20,
              '& .MuiChip-label': {
                px: open ? 1 : 0,
              },
              '& .MuiChip-icon': {
                ml: open ? undefined : '4px',
                mr: open ? undefined : '4px',
              },
            }}
          />
        </Box>
      </Box>

      {/* Navigation Menu */}
      <List sx={{ flex: 1, py: 1 }}>
        {menuItems.map(item => (
          <ExerciseNavItem
            key={item.id}
            item={item}
            isOpen={open}
            isActive={isActive(item)}
            onClick={() => handleNavigation(item)}
          />
        ))}
      </List>
    </Box>
  )

  return (
    <>
      {/* Desktop Drawer */}
      {!isMobile && (
        <Drawer
          variant="permanent"
          data-testid="exercise-sidebar-desktop"
          sx={{
            width: drawerWidth,
            flexShrink: 0,
            '& .MuiDrawer-paper': {
              width: drawerWidth,
              boxSizing: 'border-box',
              borderRight: `1px solid ${theme.palette.divider}`,
              transition: theme.transitions.create('width', {
                easing: theme.transitions.easing.sharp,
                duration: theme.transitions.duration.enteringScreen,
              }),
              overflowX: 'hidden',
            },
          }}
        >
          {drawerContent}
        </Drawer>
      )}

      {/* Mobile Drawer */}
      {isMobile && (
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={onMobileClose}
          data-testid="exercise-sidebar-mobile"
          ModalProps={{ keepMounted: true }}
          sx={{
            '& .MuiDrawer-paper': {
              width: theme.cssStyling.drawerOpenWidth,
              boxSizing: 'border-box',
            },
          }}
        >
          {drawerContent}
        </Drawer>
      )}
    </>
  )
}

/**
 * Exercise Navigation Item Button
 */
interface ExerciseNavItemProps {
  item: ExerciseMenuItem
  isOpen: boolean
  isActive: boolean
  onClick: () => void
}

const ExerciseNavItem: React.FC<ExerciseNavItemProps> = ({
  item,
  isOpen,
  isActive,
  onClick,
}) => {
  const theme = useTheme()

  const button = (
    <ListItem disablePadding sx={{ display: 'block' }}>
      <ListItemButton
        onClick={onClick}
        data-testid={`exercise-nav-item-${item.id}`}
        sx={{
          minHeight: 48,
          justifyContent: isOpen ? 'initial' : 'center',
          px: 2.5,
          backgroundColor: isActive ? theme.palette.grid.light : 'transparent',
          borderLeft: isActive
            ? `3px solid ${theme.palette.buttonPrimary.main}`
            : '3px solid transparent',
          '&:hover': {
            backgroundColor: isActive
              ? theme.palette.grid.main
              : theme.palette.action.hover,
          },
        }}
      >
        <ListItemIcon
          sx={{
            minWidth: 0,
            mr: isOpen ? 2 : 'auto',
            justifyContent: 'center',
            color: isActive
              ? theme.palette.buttonPrimary.main
              : theme.palette.text.secondary,
          }}
        >
          <FontAwesomeIcon icon={item.icon} />
        </ListItemIcon>
        {isOpen && (
          <ListItemText
            primary={item.label}
            primaryTypographyProps={{
              fontWeight: isActive ? 'bold' : 'normal',
              color: isActive
                ? theme.palette.buttonPrimary.main
                : theme.palette.text.primary,
            }}
          />
        )}
      </ListItemButton>
    </ListItem>
  )

  // Show tooltip when sidebar is collapsed
  if (!isOpen) {
    return (
      <Tooltip title={item.label} placement="right">
        {button}
      </Tooltip>
    )
  }

  return button
}

export default ExerciseSidebar
