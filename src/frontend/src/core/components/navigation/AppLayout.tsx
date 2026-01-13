/**
 * AppLayout Component
 *
 * Main application layout combining:
 * - AppHeader (fixed top navigation)
 * - Sidebar (collapsible left navigation)
 * - Breadcrumb (navigation trail)
 * - Main content area
 *
 * Manages sidebar state with localStorage persistence
 */

import React, { useState, useEffect } from 'react'
import { Box, useMediaQuery } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { AppHeader } from './AppHeader'
import { Sidebar } from './Sidebar'
import { Breadcrumb } from './Breadcrumb'
import type { BreadcrumbItem } from './Breadcrumb'
import { PermissionRole } from '../../../types'

const SIDEBAR_STATE_KEY = 'cadence-sidebar-open'

interface AppLayoutProps {
  children: React.ReactNode;
  breadcrumbItems?: BreadcrumbItem[];
  hideBreadcrumb?: boolean;
  onProfileChange?: (role: PermissionRole) => void;
}

/**
 * Get stored sidebar state from localStorage
 */
const getStoredSidebarState = (): boolean => {
  try {
    const stored = localStorage.getItem(SIDEBAR_STATE_KEY)
    if (stored !== null) {
      return stored === 'true'
    }
  } catch (error) {
    console.error('Failed to load sidebar state:', error)
  }
  return true // Default to open
}

/**
 * Save sidebar state to localStorage
 */
const saveSidebarState = (isOpen: boolean) => {
  try {
    localStorage.setItem(SIDEBAR_STATE_KEY, String(isOpen))
  } catch (error) {
    console.error('Failed to save sidebar state:', error)
  }
}

export const AppLayout: React.FC<AppLayoutProps> = ({
  children,
  breadcrumbItems,
  hideBreadcrumb = false,
  onProfileChange,
}) => {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))

  // Sidebar state
  const [sidebarOpen, setSidebarOpen] = useState(getStoredSidebarState)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  // Persist sidebar state to localStorage
  useEffect(() => {
    saveSidebarState(sidebarOpen)
  }, [sidebarOpen])

  const handleSidebarToggle = () => {
    setSidebarOpen(prev => !prev)
  }

  const handleMobileMenuToggle = () => {
    setMobileMenuOpen(prev => !prev)
  }

  const handleMobileMenuClose = () => {
    setMobileMenuOpen(false)
  }

  // Calculate content margin based on sidebar state
  const contentMarginLeft = isMobile
    ? 0
    : sidebarOpen
      ? theme.cssStyling.drawerOpenWidth
      : theme.cssStyling.drawerClosedWidth

  return (
    <Box
      data-testid="app-layout"
      sx={{
        display: 'flex',
        minHeight: '100vh',
        backgroundColor: theme.palette.background.default,
      }}
    >
      {/* Header */}
      <AppHeader
        onMobileMenuToggle={handleMobileMenuToggle}
        onProfileChange={onProfileChange}
      />

      {/* Breadcrumb (pinned under header, spans from sidebar edge to right edge) */}
      {!hideBreadcrumb && (
        <Box
          sx={{
            position: 'fixed',
            top: `${theme.cssStyling.headerHeight}px`,
            left: isMobile ? 0 : `${contentMarginLeft}px`,
            right: 0,
            zIndex: theme.zIndex.appBar - 1,
            transition: theme.transitions.create('left', {
              easing: theme.transitions.easing.sharp,
              duration: theme.transitions.duration.enteringScreen,
            }),
          }}
        >
          <Breadcrumb items={breadcrumbItems} />
        </Box>
      )}

      {/* Sidebar */}
      <Sidebar
        open={sidebarOpen}
        onToggle={handleSidebarToggle}
        mobileOpen={mobileMenuOpen}
        onMobileClose={handleMobileMenuClose}
      />

      {/* Main Content Area */}
      <Box
        component="main"
        data-testid="main-content"
        sx={{
          flexGrow: 1,
          mt: `calc(${theme.cssStyling.headerHeight}px + 40px)`, // 40px = breadcrumb height
          minHeight: `calc(100vh - ${theme.cssStyling.headerHeight}px - 40px)`,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {/* Page Content */}
        <Box
          data-testid="workspace-content"
          sx={{
            flex: 1,
            p: 2,
            overflow: 'auto',
          }}
        >
          {children}
        </Box>
      </Box>
    </Box>
  )
}

export default AppLayout
