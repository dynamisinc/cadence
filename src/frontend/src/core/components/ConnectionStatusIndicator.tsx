/**
 * ConnectionStatusIndicator Component
 *
 * Visual indicator showing current connectivity state:
 * - Green "Live": In exercise, SignalR connected and joined
 * - Green: Connected/Online (hidden in compact mode)
 * - Yellow/Orange: Connecting/Reconnecting
 * - Red: Offline/Disconnected
 *
 * Shows pending count when there are queued offline actions.
 * Clicking on the indicator opens a popover with pending action details
 * and a "Sync Now" button.
 */

import React, { useState } from 'react'
import { Box, Tooltip, Typography, Chip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faWifi,
  faSpinner,
  faTriangleExclamation,
  faTowerBroadcast,
} from '@fortawesome/free-solid-svg-icons'
import { useConnectivity, type ConnectivityState } from '../contexts/ConnectivityContext'
import { PendingActionsPopover } from './PendingActionsPopover'

interface StatusConfig {
  color: string
  bgColor: string
  icon: typeof faWifi
  spin?: boolean
  label: string
  tooltip: string
}

const statusConfigs: Record<ConnectivityState | 'live', StatusConfig> = {
  live: {
    color: '#22c55e', // green-500
    bgColor: 'rgba(34, 197, 94, 0.1)',
    icon: faTowerBroadcast,
    label: 'Live',
    tooltip: 'Connected to exercise — receiving real-time updates',
  },
  online: {
    color: '#22c55e', // green-500
    bgColor: 'rgba(34, 197, 94, 0.1)',
    icon: faWifi,
    label: '',
    tooltip: 'Connected',
  },
  connecting: {
    color: '#f59e0b', // amber-500
    bgColor: 'rgba(245, 158, 11, 0.1)',
    icon: faSpinner,
    spin: true,
    label: 'Connecting',
    tooltip: 'Establishing connection...',
  },
  reconnecting: {
    color: '#f59e0b', // amber-500
    bgColor: 'rgba(245, 158, 11, 0.1)',
    icon: faSpinner,
    spin: true,
    label: 'Reconnecting',
    tooltip: 'Connection lost. Attempting to reconnect...',
  },
  offline: {
    color: '#ef4444', // red-500
    bgColor: 'rgba(239, 68, 68, 0.1)',
    icon: faTriangleExclamation,
    label: 'Offline',
    tooltip: 'You are offline. Changes will sync when connection restores.',
  },
}

interface ConnectionStatusIndicatorProps {
  /** Show compact version (icon only when online) */
  compact?: boolean
  /** Custom className */
  className?: string
}

export const ConnectionStatusIndicator: React.FC<ConnectionStatusIndicatorProps> = ({
  compact = true,
  className,
}) => {
  const { connectivityState, isInExercise, isSignalRJoined, pendingCount } = useConnectivity()
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null)

  // Show "Live" when in exercise, connected, and joined the exercise group
  const isLive = isInExercise && connectivityState === 'online' && isSignalRJoined
  const displayState = isLive ? 'live' : connectivityState
  const config = statusConfigs[displayState]

  // In compact mode, hide when online (not in exercise) with no pending
  if (compact && displayState === 'online' && pendingCount === 0) {
    return null
  }

  const showLabel = !compact || displayState !== 'online'
  const showPending = pendingCount > 0
  const isClickable = pendingCount > 0
  const popoverOpen = Boolean(anchorEl)

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    if (isClickable) {
      setAnchorEl(event.currentTarget)
    }
  }

  const handleClose = () => {
    setAnchorEl(null)
  }

  const tooltipText = isClickable
    ? `${config.tooltip} Click to view pending changes.`
    : config.tooltip

  return (
    <>
      <Tooltip title={tooltipText} arrow>
        <Chip
          data-testid="connection-status-indicator"
          data-status={displayState}
          onClick={handleClick}
          icon={
            <FontAwesomeIcon
              icon={config.icon}
              spin={config.spin}
              style={{ color: config.color }}
            />
          }
          label={
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              {showLabel && config.label && (
                <Typography
                  variant="caption"
                  sx={{ color: config.color, fontWeight: 500 }}
                >
                  {config.label}
                </Typography>
              )}
              {showPending && (
                <Typography
                  variant="caption"
                  sx={{
                    color: config.color,
                    fontWeight: 600,
                    ml: showLabel && config.label ? 0.5 : 0,
                  }}
                >
                  ({pendingCount} pending)
                </Typography>
              )}
            </Box>
          }
          size="small"
          className={className}
          sx={{
            backgroundColor: config.bgColor,
            border: `1px solid ${config.color}`,
            '& .MuiChip-icon': {
              color: config.color,
              ml: 1,
            },
            '& .MuiChip-label': {
              px: showLabel || showPending ? 1 : 0.5,
            },
            height: 28,
            cursor: isClickable ? 'pointer' : 'default',
            '&:hover': isClickable
              ? {
                backgroundColor: config.bgColor,
                filter: 'brightness(0.95)',
              }
              : {},
          }}
        />
      </Tooltip>
      <PendingActionsPopover anchorEl={anchorEl} open={popoverOpen} onClose={handleClose} />
    </>
  )
}

export default ConnectionStatusIndicator
