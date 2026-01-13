import { useState } from 'react'
import {
  Box,
  Typography,
  Stack,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import DeleteIcon from '@mui/icons-material/Delete'
import KeyboardArrowUpIcon from '@mui/icons-material/KeyboardArrowUp'
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown'

import {
  CobraSecondaryButton,
  CobraDeleteButton,
} from '../../../theme/styledComponents'
import type { PhaseDto } from '../types'

interface PhaseHeaderProps {
  /** The phase to display */
  phase: PhaseDto
  /** Whether this is the first phase (disable move up) */
  isFirst: boolean
  /** Whether this is the last phase (disable move down) */
  isLast: boolean
  /** Whether the user can edit phases */
  canEdit: boolean
  /** Called when edit is clicked */
  onEdit: (phase: PhaseDto) => void
  /** Called when delete is clicked */
  onDelete: (phase: PhaseDto) => void
  /** Called when move up is clicked */
  onMoveUp: (phaseId: string) => void
  /** Called when move down is clicked */
  onMoveDown: (phaseId: string) => void
  /** Whether any phase operation is in progress */
  isLoading?: boolean
}

/**
 * Phase header with controls for edit, delete, and reorder
 */
export const PhaseHeader = ({
  phase,
  isFirst,
  isLast,
  canEdit,
  onEdit,
  onDelete,
  onMoveUp,
  onMoveDown,
  isLoading = false,
}: PhaseHeaderProps) => {
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)

  const handleEditClick = () => {
    onEdit(phase)
  }

  const handleDeleteClick = () => {
    setDeleteDialogOpen(true)
  }

  const handleDeleteConfirm = () => {
    onDelete(phase)
    setDeleteDialogOpen(false)
  }

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false)
  }

  const handleMoveUp = () => {
    onMoveUp(phase.id)
  }

  const handleMoveDown = () => {
    onMoveDown(phase.id)
  }

  const hasInjects = phase.injectCount > 0

  return (
    <>
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          backgroundColor: 'grey.100',
          px: 2,
          py: 1,
          borderRadius: 1,
        }}
      >
        <Box>
          <Typography variant="subtitle1" fontWeight={600}>
            {phase.name}
          </Typography>
          {phase.description && (
            <Typography variant="caption" color="text.secondary">
              {phase.description}
            </Typography>
          )}
        </Box>

        {canEdit && (
          <Stack direction="row" spacing={0.5} alignItems="center">
            {/* Reorder buttons */}
            <Tooltip title="Move up">
              <span>
                <IconButton
                  size="small"
                  onClick={handleMoveUp}
                  disabled={isFirst || isLoading}
                >
                  <KeyboardArrowUpIcon fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>
            <Tooltip title="Move down">
              <span>
                <IconButton
                  size="small"
                  onClick={handleMoveDown}
                  disabled={isLast || isLoading}
                >
                  <KeyboardArrowDownIcon fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>

            {/* Edit button */}
            <Tooltip title="Edit phase">
              <IconButton
                size="small"
                onClick={handleEditClick}
                disabled={isLoading}
              >
                <EditIcon fontSize="small" />
              </IconButton>
            </Tooltip>

            {/* Delete button */}
            <Tooltip
              title={
                hasInjects
                  ? `Cannot delete - ${phase.injectCount} inject(s) assigned`
                  : 'Delete phase'
              }
            >
              <span>
                <IconButton
                  size="small"
                  onClick={handleDeleteClick}
                  disabled={hasInjects || isLoading}
                  color={hasInjects ? 'default' : 'error'}
                >
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>
          </Stack>
        )}
      </Box>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteDialogOpen} onClose={handleDeleteCancel} maxWidth="sm">
        <DialogTitle>Delete Phase?</DialogTitle>
        <DialogContent>
          <Typography variant="body1">
            Are you sure you want to delete the phase "{phase.name}"?
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={handleDeleteCancel}>
            Cancel
          </CobraSecondaryButton>
          <CobraDeleteButton onClick={handleDeleteConfirm}>
            Delete
          </CobraDeleteButton>
        </DialogActions>
      </Dialog>
    </>
  )
}

export default PhaseHeader
