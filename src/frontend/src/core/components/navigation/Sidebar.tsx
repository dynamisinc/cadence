/**
 * Sidebar Component
 *
 * Collapsible left navigation with:
 * - Open (288px) / Closed (64px) states
 * - Navigation items with icons
 * - Active state highlighting
 * - Mobile drawer variant
 * - localStorage persistence for open/closed state
 * - Feature flag integration for tool visibility
 *
 * Navigation Items:
 * - Home (/)
 * - Exercises (/exercises)
 */

import React from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import {
  Box,
  Chip,
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
import {
  faHome,
  faClipboardList,
  faChevronLeft,
  faChevronRight,
  faGear,
  faWrench,
  faFlask,
} from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/free-solid-svg-icons'
import { useFeatureFlags } from '../../../admin'
import type { FeatureFlags } from '../../../admin'
import usePermissions from '../../../shared/hooks/usePermissions'

interface NavItem {
  id: string;
  label: string;
  icon: IconDefinition;
  path: string;
  section?: 'main' | 'tools' | 'admin';
  /** Optional feature flag key - if set, item visibility is controlled by the flag */
  featureFlagKey?: keyof FeatureFlags;
}

const navigationItems: NavItem[] = [
  { id: 'home', label: 'Home', icon: faHome, path: '/', section: 'main' },
  {
    id: 'exercises',
    label: 'Exercises',
    icon: faClipboardList,
    path: '/exercises',
    section: 'main',
  },
  {
    id: 'example-tool-1',
    label: 'Example Tool 1',
    icon: faWrench,
    path: '/example-tool-1',
    section: 'tools',
    featureFlagKey: 'exampleTool1',
  },
  {
    id: 'example-tool-2',
    label: 'Example Tool 2',
    icon: faFlask,
    path: '/example-tool-2',
    section: 'tools',
    featureFlagKey: 'exampleTool2',
  },
  {
    id: 'admin',
    label: 'Admin',
    icon: faGear,
    path: '/admin',
    section: 'admin',
  },
]

interface SidebarProps {
  open: boolean;
  onToggle: () => void;
  mobileOpen: boolean;
  onMobileClose: () => void;
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
  const { isVisible, isComingSoon } = useFeatureFlags()
  const { canManage } = usePermissions()

  const drawerWidth = open
    ? theme.cssStyling.drawerOpenWidth
    : theme.cssStyling.drawerClosedWidth

  /**
   * Filter nav items based on feature flags
   * - Items without featureFlagKey are always shown
   * - Items with featureFlagKey are shown only if flag is not "Hidden"
   */
  const getVisibleItems = (items: NavItem[]) => {
    return items.filter(item => {
      if (!item.featureFlagKey) return true
      return isVisible(item.featureFlagKey)
    })
  }

  /**
   * Check if a nav item is in "Coming Soon" state
   */
  const isItemComingSoon = (item: NavItem): boolean => {
    if (!item.featureFlagKey) return false
    return isComingSoon(item.featureFlagKey)
  }

  const handleNavigation = (path: string, item: NavItem) => {
    // Don't navigate if item is coming soon
    if (isItemComingSoon(item)) return

    navigate(path)
    if (isMobile) {
      onMobileClose()
    }
  }

  const isActive = (path: string): boolean => {
    if (path === '/') {
      return location.pathname === '/'
    }
    return location.pathname.startsWith(path)
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
        {/* Main Section */}
        {getVisibleItems(
          navigationItems.filter(item => item.section === 'main'),
        ).map(item => (
          <NavItemButton
            key={item.id}
            item={item}
            isOpen={open}
            isActive={isActive(item.path)}
            isComingSoon={isItemComingSoon(item)}
            onClick={() => handleNavigation(item.path, item)}
          />
        ))}

        {/* Tools Section */}
        {open &&
          getVisibleItems(
            navigationItems.filter(item => item.section === 'tools'),
          ).length > 0 && (
          <Typography
            variant="caption"
            data-testid="tools-section-label"
            sx={{
              px: 2,
              py: 1,
              display: 'block',
              color: theme.palette.text.secondary,
              fontWeight: 'bold',
              textTransform: 'uppercase',
              letterSpacing: 1,
            }}
          >
            Tools
          </Typography>
        )}
        {getVisibleItems(
          navigationItems.filter(item => item.section === 'tools'),
        ).map(item => (
          <NavItemButton
            key={item.id}
            item={item}
            isOpen={open}
            isActive={isActive(item.path)}
            isComingSoon={isItemComingSoon(item)}
            onClick={() => handleNavigation(item.path, item)}
          />
        ))}

        {/* Spacer to push admin to bottom */}
        <Box sx={{ flex: 1 }} />

        {/* Admin Section - at bottom, only for manage role */}
        {canManage && (
          <>
            <Divider sx={{ my: 1 }} />
            {getVisibleItems(
              navigationItems.filter(item => item.section === 'admin'),
            ).map(item => (
              <NavItemButton
                key={item.id}
                item={item}
                isOpen={open}
                isActive={isActive(item.path)}
                isComingSoon={isItemComingSoon(item)}
                onClick={() => handleNavigation(item.path, item)}
              />
            ))}
          </>
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
  item: NavItem;
  isOpen: boolean;
  isActive: boolean;
  isComingSoon?: boolean;
  onClick: () => void;
}

const NavItemButton: React.FC<NavItemButtonProps> = ({
  item,
  isOpen,
  isActive,
  isComingSoon = false,
  onClick,
}) => {
  const theme = useTheme()

  // Determine colors based on state
  const getIconColor = () => {
    if (isComingSoon) return theme.palette.text.disabled
    if (isActive) return theme.palette.buttonPrimary.main
    return theme.palette.text.secondary
  }

  const getTextColor = () => {
    if (isComingSoon) return theme.palette.text.disabled
    if (isActive) return theme.palette.buttonPrimary.main
    return theme.palette.text.primary
  }

  const tooltipTitle = isComingSoon
    ? `${item.label} (Coming Soon)`
    : item.label

  const button = (
    <ListItem disablePadding sx={{ display: 'block' }}>
      <ListItemButton
        onClick={onClick}
        disabled={isComingSoon}
        data-testid={`nav-item-${item.id}`}
        data-coming-soon={isComingSoon}
        role="button"
        sx={{
          minHeight: 48,
          justifyContent: isOpen ? 'initial' : 'center',
          px: 2.5,
          backgroundColor:
            isActive && !isComingSoon
              ? theme.palette.grid.light
              : 'transparent',
          borderLeft:
            isActive && !isComingSoon
              ? `3px solid ${theme.palette.buttonPrimary.main}`
              : '3px solid transparent',
          opacity: isComingSoon ? 0.7 : 1,
          cursor: isComingSoon ? 'not-allowed' : 'pointer',
          '&:hover': {
            backgroundColor:
              isActive && !isComingSoon
                ? theme.palette.grid.main
                : isComingSoon
                  ? 'transparent'
                  : theme.palette.action.hover,
          },
          '&.Mui-disabled': {
            opacity: 0.7,
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
          <Box sx={{ display: 'flex', alignItems: 'center', flex: 1, gap: 1 }}>
            <ListItemText
              primary={item.label}
              primaryTypographyProps={{
                fontWeight: isActive && !isComingSoon ? 'bold' : 'normal',
                color: getTextColor(),
              }}
            />
            {isComingSoon && (
              <Chip
                label="Soon"
                size="small"
                data-testid={`nav-item-${item.id}-coming-soon`}
                sx={{
                  height: 20,
                  fontSize: '0.65rem',
                  backgroundColor: theme.palette.warning.light,
                  color: theme.palette.warning.contrastText,
                }}
              />
            )}
          </Box>
        )}
      </ListItemButton>
    </ListItem>
  )

  // Show tooltip when sidebar is collapsed
  if (!isOpen) {
    return (
      <Tooltip title={tooltipTitle} placement="right">
        {button}
      </Tooltip>
    )
  }

  return button
}

export default Sidebar
