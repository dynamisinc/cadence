/**
 * Breadcrumb Component - Navigation Trail
 *
 * Shows the current location in the app hierarchy:
 * Home / Notes
 *
 * Features:
 * - Auto-generates breadcrumbs based on current route
 * - Clickable links for navigation
 * - Current item (last) is not a link
 * - Light gray background per COBRA theme
 */

import React from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Box, Typography, Link, Stack } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHome } from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/free-solid-svg-icons'

export interface BreadcrumbItem {
  label: string;
  path?: string;
  icon?: IconDefinition;
}

interface BreadcrumbProps {
  items?: BreadcrumbItem[];
}

/**
 * Generate breadcrumbs based on current route
 */
const useAutoBreadcrumbs = (): BreadcrumbItem[] => {
  const location = useLocation()
  const pathParts = location.pathname.split('/').filter(Boolean)

  const items: BreadcrumbItem[] = [
    { label: 'Home', path: '/', icon: faHome },
  ]

  // If we're at root, just show Home
  if (pathParts.length === 0) {
    return [{ label: 'Home', icon: faHome }]
  }

  // Tool routes mapping
  const toolMap: Record<string, string> = {
    notes: 'Notes',
    // Add more tools here as they are implemented
  }

  if (toolMap[pathParts[0]]) {
    items.push({ label: toolMap[pathParts[0]] })
    return items
  }

  // Fallback - capitalize first letter
  const label = pathParts[0].charAt(0).toUpperCase() + pathParts[0].slice(1)
  items.push({ label })

  return items
}

export const Breadcrumb: React.FC<BreadcrumbProps> = ({ items: providedItems }) => {
  const theme = useTheme()
  const navigate = useNavigate()
  const autoItems = useAutoBreadcrumbs()

  // Use provided items if available, otherwise auto-generate
  const items = providedItems && providedItems.length > 0 ? providedItems : autoItems

  const handleClick = (path?: string) => {
    if (path) {
      navigate(path)
    }
  }

  return (
    <Box
      data-testid="breadcrumb-container"
      sx={{
        backgroundColor: theme.palette.breadcrumb.background,
        px: 2,
        py: 1,
        borderBottom: `1px solid ${theme.palette.divider}`,
        minHeight: 40,
        display: 'flex',
        alignItems: 'center',
      }}
    >
      <Stack
        direction="row"
        spacing={1}
        alignItems="center"
        sx={{ flexWrap: 'wrap' }}
      >
        {items.map((item, index) => {
          const isLast = index === items.length - 1
          const isClickable = !!item.path && !isLast

          return (
            <React.Fragment key={index}>
              {/* Separator (except for first item) */}
              {index > 0 && (
                <Typography
                  component="span"
                  sx={{
                    color: theme.palette.text.secondary,
                    fontSize: 12,
                    mx: 0.5,
                  }}
                >
                  /
                </Typography>
              )}

              {/* Breadcrumb Item */}
              {isClickable ? (
                <Link
                  component="button"
                  onClick={() => handleClick(item.path)}
                  data-testid={`breadcrumb-link-${index}`}
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 0.5,
                    color: theme.palette.buttonPrimary.main,
                    textDecoration: 'none',
                    fontSize: 14,
                    fontWeight: 400,
                    cursor: 'pointer',
                    '&:hover': {
                      textDecoration: 'underline',
                    },
                  }}
                >
                  {item.icon && <FontAwesomeIcon icon={item.icon} size="sm" />}
                  {item.label}
                </Link>
              ) : (
                <Typography
                  component="span"
                  data-testid={`breadcrumb-item-${index}`}
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 0.5,
                    color: isLast
                      ? theme.palette.text.primary
                      : theme.palette.text.secondary,
                    fontSize: 14,
                    fontWeight: isLast ? 500 : 400,
                  }}
                >
                  {item.icon && <FontAwesomeIcon icon={item.icon} size="sm" />}
                  {item.label}
                </Typography>
              )}
            </React.Fragment>
          )
        })}
      </Stack>
    </Box>
  )
}

export default Breadcrumb
