/**
 * Sidebar Component
 *
 * Collapsible left navigation with:
 * - Open (288px) / Closed (64px) states
 * - HSEEP role-based menu filtering
 * - Section headers (CONDUCT, ANALYSIS, SYSTEM)
 * - Active state highlighting
 * - Disabled state for items requiring exercise context
 * - Mobile drawer variant
 * - localStorage persistence for open/closed state
 *
 * @see docs/features/navigation-shell/S01-updated-sidebar-menu.md
 * @see docs/features/navigation-shell/S02-role-based-menu-visibility.md
 */

import React, { useMemo } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import {
  Box,
  Divider,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  IconButton,
  Tooltip,
  Typography,
  useMediaQuery,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChevronLeft, faChevronRight } from '@fortawesome/free-solid-svg-icons'
import {
  useFilteredMenu,
  MENU_SECTION_LABELS,
  type MenuItem,
  type MenuSection,
} from '../../../shared/hooks'

interface SidebarProps {
  open: boolean;
  onToggle: () => void;
  mobileOpen: boolean;
  onMobileClose: () => void;
}

/**
 * Extract exercise ID from the current URL path
 * Matches patterns like /exercises/:id/control, /exercises/:id/queue, etc.
 */
function extractExerciseId(pathname: string): string | null {
  const match = pathname.match(/^\/exercises\/([^/]+)(?:\/|$)/)
  if (match && match[1] && match[1] !== 'new') {
    return match[1]
  }
  return null
}

export const Sidebar: React.FC<SidebarProps> = ({
  open,
  onToggle,
  mobileOpen,
  onMobileClose,
}) => {
  const theme = useTheme()
  const navigate = useNavigate()
  const location = useLocation()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))

  // Extract exercise ID from current URL for context-aware navigation
  const currentExerciseId = useMemo(
    () => extractExerciseId(location.pathname),
    [location.pathname],
  )

  // Get filtered menu items based on user role and exercise context
  const {
    groupedBySection,
    visibleSections,
    isItemDisabled,
    getDisabledTooltip,
  } = useFilteredMenu({ exerciseId: currentExerciseId })

  const drawerWidth = open
    ? theme.cssStyling.drawerOpenWidth
    : theme.cssStyling.drawerClosedWidth

  /**
   * Build the actual path for an item, replacing :id with current exercise ID
   */
  const buildPath = (item: MenuItem): string => {
    if (item.path.includes(':id') && currentExerciseId) {
      return item.path.replace(':id', currentExerciseId)
    }
    return item.path
  }

  const handleNavigation = (item: MenuItem) => {
    // Don't navigate if item is disabled
    if (isItemDisabled(item.id)) return

    const path = buildPath(item)
    navigate(path)
    if (isMobile) {
      onMobileClose()
    }
  }

  /**
   * Check if a menu item is currently active
   */
  const isActive = (item: MenuItem): boolean => {
    const path = buildPath(item)
    if (path === '/') {
      return location.pathname === '/'
    }
    // For exercise-scoped routes, check exact match or prefix
    if (item.requiresExerciseContext && currentExerciseId) {
      return location.pathname.startsWith(path)
    }
    return location.pathname.startsWith(path)
  }

  /**
   * Render a section with its items
   */
  const renderSection = (section: MenuSection, items: MenuItem[], isLast: boolean) => {
    if (items.length === 0) return null

    return (
      <React.Fragment key={section}>
        {/* Section Header (only when sidebar is open) */}
        {open && (
          <Typography
            variant="caption"
            data-testid={`section-${section}`}
            sx={{
              px: 2,
              pt: section === 'conduct' ? 1 : 2,
              pb: 0.5,
              display: 'block',
              color: theme.palette.text.secondary,
              fontWeight: 'bold',
              textTransform: 'uppercase',
              letterSpacing: 1,
              fontSize: '0.7rem',
            }}
          >
            {MENU_SECTION_LABELS[section]}
          </Typography>
        )}

        {/* Section Items */}
        {items.map(item => (
          <NavItemButton
            key={item.id}
            item={item}
            isOpen={open}
            isActive={isActive(item)}
            isDisabled={isItemDisabled(item.id)}
            disabledTooltip={getDisabledTooltip(item.id)}
            onClick={() => handleNavigation(item)}
          />
        ))}

        {/* Divider between sections (except after last) */}
        {!isLast && open && (
          <Divider sx={{ mt: 1 }} />
        )}
      </React.Fragment>
    )
  }

  // Sidebar content shared between desktop and mobile
  const drawerContent = (
    <Box
      data-testid="sidebar-content"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        pt: `${theme.cssStyling.headerHeight}px`,
      }}
    >
      {/* Toggle Button (Desktop only) - matches breadcrumb height */}
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
            sx={{
              color: theme.palette.text.secondary,
            }}
          >
            <FontAwesomeIcon icon={open ? faChevronLeft : faChevronRight} />
          </IconButton>
        </Box>
      )}

      {/* Navigation List */}
      <List sx={{ flex: 1, py: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Render visible sections in order */}
        {visibleSections.map((section, index) =>
          renderSection(
            section,
            groupedBySection[section],
            index === visibleSections.length - 1,
          ),
        )}

        {/* Spacer to push system section to bottom if it exists */}
        {visibleSections.includes('system') && visibleSections.length > 1 && (
          <Box sx={{ flex: 1 }} />
        )}
      </List>
    </Box>
  )

  return (
    <>
      {/* Desktop Drawer */}
      {!isMobile && (
        <Drawer
          variant="permanent"
          data-testid="sidebar-desktop"
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
          data-testid="sidebar-mobile"
          ModalProps={{
            keepMounted: true, // Better mobile performance
          }}
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
 * Navigation Item Button
 */
interface NavItemButtonProps {
  item: MenuItem;
  isOpen: boolean;
  isActive: boolean;
  isDisabled?: boolean;
  disabledTooltip?: string;
  onClick: () => void;
}

const NavItemButton: React.FC<NavItemButtonProps> = ({
  item,
  isOpen,
  isActive,
  isDisabled = false,
  disabledTooltip,
  onClick,
}) => {
  const theme = useTheme()

  // Determine colors based on state
  const getIconColor = () => {
    if (isDisabled) return theme.palette.text.disabled
    if (isActive) return theme.palette.buttonPrimary.main
    return theme.palette.text.secondary
  }

  const getTextColor = () => {
    if (isDisabled) return theme.palette.text.disabled
    if (isActive) return theme.palette.buttonPrimary.main
    return theme.palette.text.primary
  }

  // Tooltip shows label when collapsed, or disabled reason when disabled
  const tooltipTitle = isDisabled && disabledTooltip
    ? disabledTooltip
    : isOpen
      ? ''
      : item.label

  const button = (
    <ListItem disablePadding sx={{ display: 'block' }}>
      <ListItemButton
        onClick={onClick}
        disabled={isDisabled}
        data-testid={`nav-item-${item.id}`}
        data-disabled={isDisabled}
        role="button"
        sx={{
          minHeight: 48,
          justifyContent: isOpen ? 'initial' : 'center',
          px: 2.5,
          backgroundColor:
            isActive && !isDisabled
              ? theme.palette.grid.light
              : 'transparent',
          borderLeft:
            isActive && !isDisabled
              ? `3px solid ${theme.palette.buttonPrimary.main}`
              : '3px solid transparent',
          opacity: isDisabled ? 0.6 : 1,
          cursor: isDisabled ? 'not-allowed' : 'pointer',
          '&:hover': {
            backgroundColor:
              isActive && !isDisabled
                ? theme.palette.grid.main
                : isDisabled
                  ? 'transparent'
                  : theme.palette.action.hover,
          },
          '&.Mui-disabled': {
            opacity: 0.6,
            pointerEvents: 'auto', // Allow hover for tooltip
          },
        }}
      >
        <ListItemIcon
          sx={{
            minWidth: 0,
            mr: isOpen ? 2 : 'auto',
            justifyContent: 'center',
            color: getIconColor(),
          }}
        >
          <FontAwesomeIcon icon={item.icon} />
        </ListItemIcon>
        {isOpen && (
          <ListItemText
            primary={item.label}
            primaryTypographyProps={{
              fontWeight: isActive && !isDisabled ? 'bold' : 'normal',
              color: getTextColor(),
            }}
          />
        )}
      </ListItemButton>
    </ListItem>
  )

  // Show tooltip when sidebar is collapsed OR when item is disabled
  if (!isOpen || (isDisabled && disabledTooltip)) {
    return (
      <Tooltip title={tooltipTitle} placement="right">
        {button}
      </Tooltip>
    )
  }

  return button
}

export default Sidebar
