/**
 * Breadcrumb Component - Navigation Trail
 *
 * Shows the current location in the app hierarchy:
 * Home / Exercises
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

  // Route mapping
  const routeMap: Record<string, string> = {
    exercises: 'Exercises',
    admin: 'Admin',
  }

  const firstPart = pathParts[0]

  // Handle exercises routes with nested paths
  if (firstPart === 'exercises') {
    // Add Exercises link (always clickable if we're deeper than /exercises)
    if (pathParts.length > 1) {
      items.push({ label: 'Exercises', path: '/exercises' })

      // Handle /exercises/new
      if (pathParts[1] === 'new') {
        items.push({ label: 'New Exercise' })
      } else {
        // Handle /exercises/:id (exercise detail/edit)
        // For now, just show the ID. Pages should pass custom items with exercise name.
        items.push({ label: pathParts[1] })
      }
    } else {
      // Just /exercises
      items.push({ label: 'Exercises' })
    }
    return items
  }

  // Handle other mapped routes
  if (routeMap[firstPart]) {
    items.push({ label: routeMap[firstPart] })
    return items
  }

  // Fallback - capitalize first letter
  const label = firstPart.charAt(0).toUpperCase() + firstPart.slice(1)
  items.push({ label })

  return items
}

export const Breadcrumb: React.FC<BreadcrumbProps> = ({ items: providedItems }) => {
  const theme = useTheme()
  const navigate = useNavigate()
  const autoItems = useAutoBreadcrumbs()

  // Use provided items if available, otherwise auto-generate
  const items = providedItems && providedItems.length > 0 ? providedItems : autoItems

  const handleClick = (e: React.MouseEvent, path: string) => {
    e.preventDefault()
    e.stopPropagation()
    navigate(path)
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
                  href={item.path}
                  onClick={e => handleClick(e, item.path!)}
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
