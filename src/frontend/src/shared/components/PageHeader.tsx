/**
 * PageHeader Component
 *
 * Standardized header for all pages ensuring consistent visual hierarchy,
 * accessibility (renders as h1), and spacing across the application.
 *
 * Supports two visual layouts:
 * 1. Standard: [Back?] Title [Chips?] ---- [Actions]
 * 2. Settings: [Icon] Title/Subtitle ---- [Actions]
 */

import type { FC, ReactNode } from 'react'
import { Box, IconButton, Stack, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowLeft } from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core'

export interface PageHeaderProps {
  /** Page title text */
  title: string
  /** Optional subtitle/description below the title */
  subtitle?: string | ReactNode
  /** FontAwesome icon displayed to the left of the title */
  icon?: IconDefinition
  /** Action buttons/elements aligned to the right */
  actions?: ReactNode
  /** Show a back navigation button */
  showBackButton?: boolean
  /** Callback when back button is clicked */
  onBackClick?: () => void
  /** Chips/badges rendered next to the title */
  chips?: ReactNode
  /** Bottom margin in theme spacing units (default: 3) */
  mb?: number
}

export const PageHeader: FC<PageHeaderProps> = ({
  title,
  subtitle,
  icon,
  actions,
  showBackButton,
  onBackClick,
  chips,
  mb = 3,
}) => {
  return (
    <Stack
      direction="row"
      justifyContent="space-between"
      alignItems="center"
      sx={{ mb }}
    >
      <Stack direction="row" alignItems="center" spacing={1.5}>
        {showBackButton && (
          <IconButton onClick={onBackClick} size="small" aria-label="Go back">
            <FontAwesomeIcon icon={faArrowLeft} />
          </IconButton>
        )}

        {icon && (
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'primary.main',
              fontSize: 28,
            }}
          >
            <FontAwesomeIcon icon={icon} />
          </Box>
        )}

        <Box>
          <Stack direction="row" alignItems="center" spacing={1.5}>
            <Typography
              variant="h5"
              component="h1"
              sx={icon ? { fontWeight: 600 } : undefined}
            >
              {title}
            </Typography>
            {chips}
          </Stack>
          {subtitle && (
            <Typography variant="body2" color="text.secondary">
              {subtitle}
            </Typography>
          )}
        </Box>
      </Stack>

      {actions && (
        <Stack direction="row" spacing={1} alignItems="center">
          {actions}
        </Stack>
      )}
    </Stack>
  )
}
