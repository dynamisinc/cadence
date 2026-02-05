/**
 * CapabilityTargetCard Component
 *
 * Expandable card displaying a Capability Target with its Critical Tasks.
 * Shows capability name, target description, and task/inject counts.
 */

import { useState } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Stack,
  Paper,
  IconButton,
  Tooltip,
  Collapse,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPen,
  faTrash,
  faChevronDown,
  faChevronUp,
  faClipboardList,
  faPaperclip,
} from '@fortawesome/free-solid-svg-icons'
import { CriticalTaskList } from './CriticalTaskList'
import type { CapabilityTargetDto } from '../types'

interface CapabilityTargetCardProps {
  /** The capability target to display */
  target: CapabilityTargetDto
  /** Whether the user can edit (Director+) */
  canEdit?: boolean
  /** Called when edit is clicked */
  onEdit: (target: CapabilityTargetDto) => void
  /** Called when delete is clicked */
  onDelete: (target: CapabilityTargetDto) => void
  /** Whether to start expanded */
  defaultExpanded?: boolean
}

/**
 * Card component for a Capability Target with expandable Critical Tasks
 */
export const CapabilityTargetCard: FC<CapabilityTargetCardProps> = ({
  target,
  canEdit = true,
  onEdit,
  onDelete,
  defaultExpanded = false,
}) => {
  const [expanded, setExpanded] = useState(defaultExpanded)

  const handleToggleExpand = () => {
    setExpanded(prev => !prev)
  }

  return (
    <Paper
      sx={{
        overflow: 'hidden',
        border: '1px solid',
        borderColor: 'divider',
      }}
    >
      {/* Header */}
      <Box
        sx={{
          px: 2,
          py: 1.5,
          display: 'flex',
          alignItems: 'flex-start',
          gap: 2,
          cursor: 'pointer',
          '&:hover': {
            bgcolor: 'action.hover',
          },
        }}
        onClick={handleToggleExpand}
      >
        {/* Expand/Collapse Icon */}
        <Box
          sx={{
            mt: 0.5,
            color: 'text.secondary',
            transition: 'transform 0.2s',
            transform: expanded ? 'rotate(0deg)' : 'rotate(-90deg)',
          }}
        >
          <FontAwesomeIcon icon={faChevronDown} />
        </Box>

        {/* Content */}
        <Box flex={1} sx={{ minWidth: 0 }}>
          {/* Capability Name */}
          <Stack direction="row" spacing={1} alignItems="center">
            <FontAwesomeIcon
              icon={faClipboardList}
              style={{ color: 'var(--mui-palette-primary-main)' }}
            />
            <Typography variant="subtitle1" fontWeight={600}>
              {target.capabilityName}
            </Typography>
          </Stack>

          {/* Target Description */}
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{ mt: 0.5, fontStyle: 'italic' }}
          >
            &quot;{target.targetDescription}&quot;
          </Typography>

          {/* Stats */}
          <Stack direction="row" spacing={2} sx={{ mt: 1 }}>
            <Typography variant="caption" color="text.secondary">
              <FontAwesomeIcon icon={faClipboardList} style={{ marginRight: 4 }} />
              {target.criticalTaskCount} Critical Task
              {target.criticalTaskCount !== 1 ? 's' : ''}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              <FontAwesomeIcon icon={faPaperclip} style={{ marginRight: 4 }} />
              {target.totalLinkedInjects} linked inject
              {target.totalLinkedInjects !== 1 ? 's' : ''}
            </Typography>
          </Stack>
        </Box>

        {/* Actions */}
        {canEdit && (
          <Stack
            direction="row"
            spacing={0}
            sx={{ flexShrink: 0 }}
            onClick={e => e.stopPropagation()}
          >
            <Tooltip title="Edit target">
              <IconButton size="small" onClick={() => onEdit(target)}>
                <FontAwesomeIcon icon={faPen} size="sm" />
              </IconButton>
            </Tooltip>
            <Tooltip
              title={
                target.criticalTaskCount > 0
                  ? 'Delete target and all tasks'
                  : 'Delete target'
              }
            >
              <IconButton
                size="small"
                onClick={() => onDelete(target)}
                color="error"
              >
                <FontAwesomeIcon icon={faTrash} size="sm" />
              </IconButton>
            </Tooltip>
          </Stack>
        )}

        {/* Expand/Collapse Button */}
        <Tooltip title={expanded ? 'Collapse' : 'Expand'}>
          <IconButton
            size="small"
            onClick={e => {
              e.stopPropagation()
              handleToggleExpand()
            }}
          >
            <FontAwesomeIcon icon={expanded ? faChevronUp : faChevronDown} />
          </IconButton>
        </Tooltip>
      </Box>

      {/* Expanded Content - Critical Tasks */}
      <Collapse in={expanded}>
        <Divider />
        <Box sx={{ px: 2, py: 1.5, bgcolor: 'grey.50' }}>
          <CriticalTaskList
            capabilityTargetId={target.id}
            capabilityTargetName={target.capabilityName}
            canEdit={canEdit}
          />
        </Box>
      </Collapse>
    </Paper>
  )
}

export default CapabilityTargetCard
