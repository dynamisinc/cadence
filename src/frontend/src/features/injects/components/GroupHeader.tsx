/**
 * GroupHeader Component
 *
 * A collapsible section header for grouped injects.
 * Shows group name, count badge, and expand/collapse toggle.
 * When grouping by phase, can optionally show phase management controls.
 */

import { Box, Typography, IconButton, Badge, Tooltip, Stack } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronDown,
  faChevronRight,
  faChevronUp,
  faPen,
  faTrash,
} from '@fortawesome/free-solid-svg-icons'
import type { GroupBy } from '../types/organization'
import { InjectStatus } from '../../../types'

export interface PhaseManagementProps {
  /** The phase ID (null for unassigned) */
  phaseId: string | null
  /** Whether this is the first phase */
  isFirst: boolean
  /** Whether this is the last phase */
  isLast: boolean
  /** Called when edit is clicked */
  onEdit: () => void
  /** Called when delete is clicked */
  onDelete: () => void
  /** Called when move up is clicked */
  onMoveUp: () => void
  /** Called when move down is clicked */
  onMoveDown: () => void
  /** Whether any operation is in progress */
  isLoading?: boolean
}

export interface GroupHeaderProps {
  /** Group display name */
  name: string
  /** Number of items in the group */
  count: number
  /** Whether the group is expanded */
  expanded: boolean
  /** Callback when expand/collapse is toggled */
  onToggle: () => void
  /** The grouping mode (for styling) */
  groupBy: GroupBy
  /** Optional: The status value (for status-based coloring) */
  statusValue?: string
  /** Optional: Phase management controls (only for phase grouping) */
  phaseManagement?: PhaseManagementProps
  /** Whether the user can manage phases */
  canManagePhases?: boolean
}

/**
 * Get background color based on status or default
 */
function getHeaderBackground(groupBy: GroupBy, statusValue?: string): string {
  if (groupBy === 'status' && statusValue) {
    switch (statusValue) {
      case InjectStatus.Pending:
        return 'grey.100'
      case InjectStatus.Fired:
        return 'success.50'
      case InjectStatus.Skipped:
        return 'warning.50'
      default:
        return 'grey.100'
    }
  }
  return 'grey.100'
}

/**
 * Get badge color based on status or default
 */
function getBadgeColor(
  groupBy: GroupBy,
  statusValue?: string,
): 'default' | 'success' | 'warning' | 'error' | 'primary' {
  if (groupBy === 'status' && statusValue) {
    switch (statusValue) {
      case InjectStatus.Pending:
        return 'default'
      case InjectStatus.Fired:
        return 'success'
      case InjectStatus.Skipped:
        return 'warning'
      default:
        return 'default'
    }
  }
  return 'primary'
}

export const GroupHeader = ({
  name,
  count,
  expanded,
  onToggle,
  groupBy,
  statusValue,
  phaseManagement,
  canManagePhases = false,
}: GroupHeaderProps) => {
  const backgroundColor = getHeaderBackground(groupBy, statusValue)
  const badgeColor = getBadgeColor(groupBy, statusValue)

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      onToggle()
    }
  }

  // Stop propagation for management button clicks so they don't toggle the group
  const handleManagementClick = (e: React.MouseEvent, action: () => void) => {
    e.stopPropagation()
    action()
  }

  // Check if this is a real phase (not "Unassigned")
  const isRealPhase = phaseManagement && phaseManagement.phaseId !== null
  const showPhaseControls = canManagePhases && groupBy === 'phase' && phaseManagement && isRealPhase

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 1,
        p: 1.5,
        backgroundColor,
        borderRadius: expanded ? '8px 8px 0 0' : '8px',
        cursor: 'pointer',
        transition: 'background-color 0.2s',
        '&:hover': {
          filter: 'brightness(0.97)',
        },
        '&:focus-visible': {
          outline: '2px solid',
          outlineColor: 'primary.main',
          outlineOffset: '-2px',
        },
      }}
      onClick={onToggle}
      onKeyDown={handleKeyDown}
      tabIndex={0}
      role="button"
      aria-expanded={expanded}
    >
      <IconButton size="small" sx={{ p: 0.5 }} tabIndex={-1}>
        <FontAwesomeIcon
          icon={expanded ? faChevronDown : faChevronRight}
          size="sm"
        />
      </IconButton>

      <Typography variant="subtitle2" fontWeight={600} sx={{ flexGrow: 1 }}>
        {name}
      </Typography>

      {/* Phase management controls */}
      {showPhaseControls && (
        <Stack direction="row" spacing={0.5} alignItems="center" sx={{ mr: 1 }}>
          {/* Move up */}
          <Tooltip title="Move phase up">
            <span>
              <IconButton
                size="small"
                onClick={(e) => handleManagementClick(e, phaseManagement.onMoveUp)}
                disabled={phaseManagement.isFirst || phaseManagement.isLoading}
                sx={{ p: 0.5 }}
              >
                <FontAwesomeIcon icon={faChevronUp} size="xs" />
              </IconButton>
            </span>
          </Tooltip>

          {/* Move down */}
          <Tooltip title="Move phase down">
            <span>
              <IconButton
                size="small"
                onClick={(e) => handleManagementClick(e, phaseManagement.onMoveDown)}
                disabled={phaseManagement.isLast || phaseManagement.isLoading}
                sx={{ p: 0.5 }}
              >
                <FontAwesomeIcon icon={faChevronDown} size="xs" />
              </IconButton>
            </span>
          </Tooltip>

          {/* Edit */}
          <Tooltip title="Edit phase">
            <IconButton
              size="small"
              onClick={(e) => handleManagementClick(e, phaseManagement.onEdit)}
              disabled={phaseManagement.isLoading}
              sx={{ p: 0.5 }}
            >
              <FontAwesomeIcon icon={faPen} size="xs" />
            </IconButton>
          </Tooltip>

          {/* Delete - disabled if has injects */}
          <Tooltip
            title={
              count > 0
                ? `Cannot delete - ${count} inject(s) assigned`
                : 'Delete phase'
            }
          >
            <span>
              <IconButton
                size="small"
                onClick={(e) => handleManagementClick(e, phaseManagement.onDelete)}
                disabled={count > 0 || phaseManagement.isLoading}
                color={count > 0 ? 'default' : 'error'}
                sx={{ p: 0.5 }}
              >
                <FontAwesomeIcon icon={faTrash} size="xs" />
              </IconButton>
            </span>
          </Tooltip>
        </Stack>
      )}

      <Badge
        badgeContent={count}
        color={badgeColor}
        sx={{
          '& .MuiBadge-badge': {
            position: 'static',
            transform: 'none',
          },
        }}
      />
    </Box>
  )
}

export default GroupHeader
