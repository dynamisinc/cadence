/**
 * InjectRow Component
 *
 * Single row display for an inject within the conduct view.
 * Shows inject number, offset time, title, status, and action buttons.
 * Highlights injects that are due soon (within 5 minutes).
 */

import {
  TableRow,
  TableCell,
  Typography,
  Stack,
  IconButton,
  Tooltip,
  Chip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlay,
  faForwardStep,
  faRotateLeft,
  faEnvelope,
  faPhone,
  faTowerBroadcast,
  faComment,
  faFileLines,
  faDesktop,
  faEllipsisH,
  faArrowRight,
  faArrowLeft,
} from '@fortawesome/free-solid-svg-icons'
import { keyframes } from '@mui/system'

import { InjectStatusChip, InjectTypeChip } from './'
import { InjectStatus, DeliveryMethod } from '../../../types'
import type { InjectDto } from '../types'
import { formatOffset, formatScheduledTime, formatScenarioTime, formatTimeRemaining, DUE_SOON_THRESHOLD_MS } from '../types'

// Pulse animation for due soon indicator
const pulse = keyframes`
  0%, 100% { opacity: 1; }
  50% { opacity: 0.6; }
`

// Delivery method icon mapping
const DELIVERY_METHOD_ICONS: Record<DeliveryMethod, { icon: typeof faEnvelope; label: string }> = {
  [DeliveryMethod.Email]: { icon: faEnvelope, label: 'Email' },
  [DeliveryMethod.Phone]: { icon: faPhone, label: 'Phone' },
  [DeliveryMethod.Radio]: { icon: faTowerBroadcast, label: 'Radio' },
  [DeliveryMethod.Verbal]: { icon: faComment, label: 'Verbal' },
  [DeliveryMethod.Written]: { icon: faFileLines, label: 'Written' },
  [DeliveryMethod.Simulation]: { icon: faDesktop, label: 'Simulation' },
  [DeliveryMethod.Other]: { icon: faEllipsisH, label: 'Other' },
}

/**
 * Truncate text to specified length with ellipsis
 */
const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text
  return text.substring(0, maxLength).trim() + '...'
}

interface InjectRowProps {
  /** The inject to display */
  inject: InjectDto
  /** Offset in milliseconds from exercise start */
  offsetMs: number
  /** Current elapsed time in milliseconds (for countdown calculation) */
  elapsedTimeMs?: number
  /** Show offset instead of wall clock time */
  showOffset?: boolean
  /** Can the user control this inject (fire/skip/reset)? */
  canControl?: boolean
  /** Show fire button (only for ready-to-fire injects) */
  showFireButton?: boolean
  /** Is an action currently being submitted? */
  isSubmitting?: boolean
  /** Show description preview with delivery context */
  showPreview?: boolean
  /** Called when fire button is clicked */
  onFire?: (injectId: string) => void
  /** Called when skip button is clicked */
  onSkip?: (injectId: string) => void
  /** Called when reset button is clicked */
  onReset?: (injectId: string) => void
  /** Called when row is clicked to view details */
  onClick?: (inject: InjectDto) => void
}

export const InjectRow = ({
  inject,
  offsetMs,
  elapsedTimeMs = 0,
  showOffset = true,
  canControl = false,
  showFireButton = true,
  isSubmitting = false,
  showPreview = false,
  onFire,
  onSkip,
  onReset,
  onClick,
}: InjectRowProps) => {
  const isPending = inject.status === InjectStatus.Pending
  const isFired = inject.status === InjectStatus.Fired
  const isSkipped = inject.status === InjectStatus.Skipped

  // Calculate time remaining until this inject is due
  const timeRemainingMs = offsetMs - elapsedTimeMs
  const isDueSoon = isPending && timeRemainingMs > 0 && timeRemainingMs <= DUE_SOON_THRESHOLD_MS
  const isUpcoming = isPending && timeRemainingMs > 0

  const handleFire = () => {
    if (onFire) onFire(inject.id)
  }

  const handleSkip = () => {
    if (onSkip) onSkip(inject.id)
  }

  const handleReset = () => {
    if (onReset) onReset(inject.id)
  }

  const handleRowClick = (e: React.MouseEvent) => {
    // Don't trigger row click if clicking on action buttons
    if ((e.target as HTMLElement).closest('button')) return
    if (onClick) onClick(inject)
  }

  // Format fired/skipped time for display
  const formatActionTime = (isoString: string | null): string => {
    if (!isoString) return ''
    const date = new Date(isoString)
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  }

  return (
    <TableRow
      onClick={handleRowClick}
      sx={{
        opacity: isPending ? 1 : 0.8,
        backgroundColor: isDueSoon ? 'warning.50' : 'inherit',
        cursor: onClick ? 'pointer' : 'default',
        '&:hover': {
          backgroundColor: isDueSoon ? 'warning.100' : 'action.hover',
        },
      }}
    >
      {/* Inject Number */}
      <TableCell sx={{ width: 50 }}>
        <Typography variant="body2" fontWeight={600}>
          #{inject.injectNumber}
        </Typography>
      </TableCell>

      {/* Time Display */}
      <TableCell sx={{ width: 100 }}>
        <Stack spacing={0}>
          <Typography variant="body2" fontFamily="monospace">
            {showOffset ? formatOffset(offsetMs) : formatScheduledTime(inject.scheduledTime)}
          </Typography>
          {/* Show countdown for upcoming injects */}
          {isUpcoming && (
            <Chip
              label={formatTimeRemaining(timeRemainingMs)}
              size="small"
              color={isDueSoon ? 'warning' : 'default'}
              sx={{
                height: 18,
                fontSize: '0.7rem',
                fontFamily: 'monospace',
                animation: isDueSoon ? `${pulse} 1.5s ease-in-out infinite` : 'none',
                '& .MuiChip-label': {
                  px: 0.75,
                },
              }}
            />
          )}
          {inject.scenarioDay && (
            <Typography variant="caption" color="text.secondary" display="block">
              {formatScenarioTime(inject.scenarioDay, inject.scenarioTime)}
            </Typography>
          )}
        </Stack>
      </TableCell>

      {/* Title and Description Preview */}
      <TableCell>
        <Stack spacing={0.5}>
          <Typography
            variant="body2"
            sx={{
              maxWidth: 300,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {inject.title}
          </Typography>

          {/* Description Preview */}
          {showPreview && (
            <Stack
              spacing={0.5}
              sx={{
                p: 1,
                bgcolor: 'grey.50',
                borderRadius: 1,
                borderLeft: '3px solid',
                borderLeftColor: 'primary.light',
              }}
            >
              {/* Description text */}
              <Typography
                variant="body2"
                color="text.secondary"
                data-testid="inject-description-preview"
                sx={{
                  fontStyle: 'italic',
                  fontSize: '0.85rem',
                  lineHeight: 1.4,
                }}
              >
                "{truncateText(inject.description, 150)}"
              </Typography>

              {/* Delivery context */}
              <Stack direction="row" spacing={2} flexWrap="wrap" sx={{ fontSize: '0.75rem' }}>
                {/* To (Target) */}
                {inject.target && (
                  <Stack direction="row" spacing={0.5} alignItems="center">
                    <FontAwesomeIcon icon={faArrowRight} size="xs" style={{ opacity: 0.6 }} />
                    <Typography variant="caption" color="text.secondary">
                      To:
                    </Typography>
                    <Typography variant="caption">{inject.target}</Typography>
                  </Stack>
                )}

                {/* Delivery Method */}
                {inject.deliveryMethod && DELIVERY_METHOD_ICONS[inject.deliveryMethod] && (
                  <Stack direction="row" spacing={0.5} alignItems="center">
                    <FontAwesomeIcon
                      icon={DELIVERY_METHOD_ICONS[inject.deliveryMethod].icon}
                      size="xs"
                      style={{ opacity: 0.6 }}
                    />
                    <Typography variant="caption">
                      {DELIVERY_METHOD_ICONS[inject.deliveryMethod].label}
                    </Typography>
                  </Stack>
                )}

                {/* From (Source) */}
                {inject.source && (
                  <Stack direction="row" spacing={0.5} alignItems="center">
                    <FontAwesomeIcon icon={faArrowLeft} size="xs" style={{ opacity: 0.6 }} />
                    <Typography variant="caption" color="text.secondary">
                      From:
                    </Typography>
                    <Typography variant="caption">{inject.source}</Typography>
                  </Stack>
                )}
              </Stack>
            </Stack>
          )}
        </Stack>
      </TableCell>

      {/* Type */}
      <TableCell sx={{ width: 90 }}>
        <InjectTypeChip type={inject.injectType} />
      </TableCell>

      {/* Status or Action Info */}
      <TableCell sx={{ width: 140 }}>
        {isPending ? (
          <InjectStatusChip status={inject.status} />
        ) : (
          <Stack spacing={0}>
            <Stack direction="row" spacing={0.5} alignItems="center">
              <InjectStatusChip status={inject.status} />
              {isFired && inject.firedAt && (
                <Typography variant="caption" color="text.secondary">
                  {formatActionTime(inject.firedAt)}
                </Typography>
              )}
              {isSkipped && inject.skippedAt && (
                <Typography variant="caption" color="text.secondary">
                  {formatActionTime(inject.skippedAt)}
                </Typography>
              )}
            </Stack>
            {/* Show who performed the action */}
            {isFired && inject.firedByName && (
              <Typography variant="caption" color="text.secondary" sx={{ fontSize: '0.7rem' }}>
                by {inject.firedByName}
              </Typography>
            )}
            {isSkipped && inject.skippedByName && (
              <Typography variant="caption" color="text.secondary" sx={{ fontSize: '0.7rem' }}>
                by {inject.skippedByName}
              </Typography>
            )}
          </Stack>
        )}
      </TableCell>

      {/* Actions */}
      {canControl && (
        <TableCell sx={{ width: 100 }}>
          <Stack direction="row" spacing={0.5}>
            {isPending && showFireButton && (
              <Tooltip title="Fire inject">
                <IconButton
                  size="small"
                  color="success"
                  onClick={handleFire}
                  disabled={isSubmitting}
                >
                  <FontAwesomeIcon icon={faPlay} size="sm" />
                </IconButton>
              </Tooltip>
            )}
            {isPending && (
              <Tooltip title="Skip inject">
                <IconButton
                  size="small"
                  color="warning"
                  onClick={handleSkip}
                  disabled={isSubmitting}
                >
                  <FontAwesomeIcon icon={faForwardStep} size="sm" />
                </IconButton>
              </Tooltip>
            )}
            {(isFired || isSkipped) && (
              <Tooltip title="Reset to pending">
                <IconButton
                  size="small"
                  onClick={handleReset}
                  disabled={isSubmitting}
                >
                  <FontAwesomeIcon icon={faRotateLeft} size="sm" />
                </IconButton>
              </Tooltip>
            )}
          </Stack>
        </TableCell>
      )}
    </TableRow>
  )
}

export default InjectRow
