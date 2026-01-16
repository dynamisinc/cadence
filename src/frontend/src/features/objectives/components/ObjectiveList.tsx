import { useState } from 'react'
import {
  Box,
  Typography,
  Stack,
  Paper,
  IconButton,
  Tooltip,
  Skeleton,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faPen, faTrash } from '@fortawesome/free-solid-svg-icons'

import {
  CobraPrimaryButton,
} from '../../../theme/styledComponents'
import { useObjectives } from '../hooks/useObjectives'
import { ObjectiveFormDialog } from './ObjectiveFormDialog'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import type { ObjectiveDto, ObjectiveFormValues, CreateObjectiveRequest, UpdateObjectiveRequest } from '../types'

interface ObjectiveListProps {
  exerciseId: string
  /** Whether the exercise can be edited (not archived) */
  canEdit?: boolean
}

/**
 * Component for displaying and managing exercise objectives
 */
export const ObjectiveList = ({ exerciseId, canEdit = true }: ObjectiveListProps) => {
  const {
    objectives,
    loading,
    error,
    createObjective,
    updateObjective,
    deleteObjective,
    isCreating,
    isUpdating,
    isDeleting,
  } = useObjectives(exerciseId)

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingObjective, setEditingObjective] = useState<ObjectiveDto | null>(null)
  const [deletingObjective, setDeletingObjective] = useState<ObjectiveDto | null>(null)

  const handleOpenCreate = () => {
    setEditingObjective(null)
    setIsFormOpen(true)
  }

  const handleOpenEdit = (objective: ObjectiveDto) => {
    setEditingObjective(objective)
    setIsFormOpen(true)
  }

  const handleCloseForm = () => {
    setIsFormOpen(false)
    setEditingObjective(null)
  }

  const handleSubmit = async (values: ObjectiveFormValues) => {
    if (editingObjective) {
      // Update
      const request: UpdateObjectiveRequest = {
        objectiveNumber: values.objectiveNumber,
        name: values.name,
        description: values.description || null,
      }
      await updateObjective(editingObjective.id, request)
    } else {
      // Create
      const request: CreateObjectiveRequest = {
        objectiveNumber: values.objectiveNumber || null,
        name: values.name,
        description: values.description || null,
      }
      await createObjective(request)
    }
    handleCloseForm()
  }

  const handleConfirmDelete = async () => {
    if (deletingObjective) {
      await deleteObjective(deletingObjective.id)
      setDeletingObjective(null)
    }
  }

  if (loading) {
    return (
      <Stack spacing={2}>
        <Skeleton variant="rectangular" height={60} />
        <Skeleton variant="rectangular" height={60} />
      </Stack>
    )
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>
  }

  return (
    <Box>
      {/* Header */}
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
        <Typography variant="h6">
          Exercise Objectives ({objectives.length})
        </Typography>
        {canEdit && (
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faPlus} />}
            onClick={handleOpenCreate}
            size="small"
          >
            Add Objective
          </CobraPrimaryButton>
        )}
      </Stack>

      {/* Objective List */}
      {objectives.length === 0 ? (
        <Paper
          sx={{
            p: 3,
            textAlign: 'center',
            bgcolor: 'background.default',
          }}
        >
          <Typography color="text.secondary">
            No objectives defined yet.
          </Typography>
          <Typography variant="body2" color="text.secondary" mt={1}>
            Objectives define what capabilities will be tested during this exercise.
          </Typography>
        </Paper>
      ) : (
        <Stack spacing={1}>
          {objectives.map(objective => (
            <Paper
              key={objective.id}
              sx={{
                p: 2,
                display: 'flex',
                alignItems: 'flex-start',
                gap: 2,
              }}
            >
              {/* Objective Number */}
              <Box
                sx={{
                  minWidth: 40,
                  height: 40,
                  borderRadius: 1,
                  bgcolor: 'primary.main',
                  color: 'primary.contrastText',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontWeight: 'bold',
                  fontSize: '0.875rem',
                }}
              >
                {objective.objectiveNumber}
              </Box>

              {/* Content */}
              <Box flex={1}>
                <Typography variant="subtitle1" fontWeight={500}>
                  {objective.name}
                </Typography>
                {objective.description && (
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{
                      mt: 0.5,
                      display: '-webkit-box',
                      WebkitLineClamp: 2,
                      WebkitBoxOrient: 'vertical',
                      overflow: 'hidden',
                    }}
                  >
                    {objective.description}
                  </Typography>
                )}
                <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                  {objective.linkedInjectCount} inject{objective.linkedInjectCount !== 1 ? 's' : ''} linked
                </Typography>
              </Box>

              {/* Actions */}
              {canEdit && (
                <Stack direction="row" spacing={0.5}>
                  <Tooltip title="Edit objective">
                    <IconButton
                      size="small"
                      onClick={() => handleOpenEdit(objective)}
                    >
                      <FontAwesomeIcon icon={faPen} size="sm" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title={objective.linkedInjectCount > 0 ? 'Remove linked injects first' : 'Delete objective'}>
                    <span>
                      <IconButton
                        size="small"
                        onClick={() => setDeletingObjective(objective)}
                        disabled={objective.linkedInjectCount > 0}
                        color="error"
                      >
                        <FontAwesomeIcon icon={faTrash} size="sm" />
                      </IconButton>
                    </span>
                  </Tooltip>
                </Stack>
              )}
            </Paper>
          ))}
        </Stack>
      )}

      {/* Form Dialog */}
      <ObjectiveFormDialog
        open={isFormOpen}
        objective={editingObjective}
        onClose={handleCloseForm}
        onSubmit={handleSubmit}
        isSubmitting={isCreating || isUpdating}
      />

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        open={!!deletingObjective}
        title="Delete Objective"
        message={`Are you sure you want to delete objective "${deletingObjective?.objectiveNumber}. ${deletingObjective?.name}"?`}
        confirmLabel="Delete"
        severity="danger"
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeletingObjective(null)}
        isConfirming={isDeleting}
      />
    </Box>
  )
}

export default ObjectiveList
