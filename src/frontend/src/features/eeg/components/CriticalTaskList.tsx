/**
 * CriticalTaskList Component
 *
 * Displays a list of Critical Tasks within a Capability Target.
 * Supports CRUD operations and shows linked inject/entry counts.
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
  Skeleton,
  Alert,
  Chip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faPen,
  faTrash,
  faPaperclip,
  faFileLines,
  faTriangleExclamation,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
} from '@/theme/styledComponents'
import { useCriticalTasks } from '../hooks/useCriticalTasks'
import { CriticalTaskFormDialog } from './CriticalTaskFormDialog'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import type {
  CriticalTaskDto,
  CreateCriticalTaskRequest,
  UpdateCriticalTaskRequest,
} from '../types'

interface CriticalTaskListProps {
  /** Exercise ID (required for authorization) */
  exerciseId: string
  /** Parent capability target ID */
  capabilityTargetId: string
  /** Parent capability target name for display */
  capabilityTargetName: string
  /** Whether the user can edit (Director+) */
  canEdit?: boolean
}

/**
 * List of Critical Tasks within a Capability Target
 */
export const CriticalTaskList: FC<CriticalTaskListProps> = ({
  exerciseId,
  capabilityTargetId,
  capabilityTargetName,
  canEdit = true,
}) => {
  const {
    criticalTasks,
    loading,
    error,
    createCriticalTask,
    updateCriticalTask,
    deleteCriticalTask,
    isDeleting,
  } = useCriticalTasks(exerciseId, capabilityTargetId)

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingTask, setEditingTask] = useState<CriticalTaskDto | null>(null)
  const [deletingTask, setDeletingTask] = useState<CriticalTaskDto | null>(null)

  const handleOpenCreate = () => {
    setEditingTask(null)
    setIsFormOpen(true)
  }

  const handleOpenEdit = (task: CriticalTaskDto) => {
    setEditingTask(task)
    setIsFormOpen(true)
  }

  const handleCloseForm = () => {
    setIsFormOpen(false)
    setEditingTask(null)
  }

  const handleCreate = async (request: CreateCriticalTaskRequest) => {
    await createCriticalTask(request)
  }

  const handleUpdate = async (id: string, request: UpdateCriticalTaskRequest) => {
    await updateCriticalTask(id, request)
  }

  const handleConfirmDelete = async () => {
    if (deletingTask) {
      await deleteCriticalTask(deletingTask.id)
      setDeletingTask(null)
    }
  }

  // Build delete warning message
  const getDeleteWarning = (task: CriticalTaskDto) => {
    const warnings: string[] = []
    if (task.linkedInjectCount > 0) {
      warnings.push(`${task.linkedInjectCount} linked inject association${task.linkedInjectCount !== 1 ? 's' : ''}`)
    }
    if (task.eegEntryCount > 0) {
      warnings.push(`${task.eegEntryCount} EEG entr${task.eegEntryCount !== 1 ? 'ies' : 'y'}`)
    }
    return warnings.length > 0
      ? `This will also delete: ${warnings.join(' and ')}.`
      : null
  }

  if (loading) {
    return (
      <Stack spacing={1} sx={{ pl: 2 }}>
        <Skeleton variant="rectangular" height={48} />
        <Skeleton variant="rectangular" height={48} />
      </Stack>
    )
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ ml: 2 }}>
        {error}
      </Alert>
    )
  }

  return (
    <Box sx={{ pl: 2, pt: 1 }}>
      {/* Header */}
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
        <Typography variant="subtitle2" color="text.secondary">
          Critical Tasks
        </Typography>
        {canEdit && (
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faPlus} />}
            onClick={handleOpenCreate}
            size="small"
          >
            Add Task
          </CobraPrimaryButton>
        )}
      </Stack>

      {/* Task List */}
      {criticalTasks.length === 0 ? (
        <Paper
          sx={{
            p: 2,
            textAlign: 'center',
            bgcolor: 'background.default',
          }}
        >
          <Typography color="text.secondary" variant="body2">
            No critical tasks defined yet.
          </Typography>
          <Typography variant="caption" color="text.secondary" mt={0.5} display="block">
            Critical tasks specify the observable actions evaluators should assess.
          </Typography>
        </Paper>
      ) : (
        <Stack spacing={0.5}>
          {criticalTasks.map((task, index) => (
            <Paper
              key={task.id}
              sx={{
                px: 1.5,
                py: 1,
                display: 'flex',
                alignItems: 'flex-start',
                gap: 1.5,
                bgcolor: 'background.paper',
              }}
              variant="outlined"
            >
              {/* Task Number */}
              <Box
                sx={{
                  minWidth: 24,
                  height: 24,
                  borderRadius: 0.5,
                  bgcolor: 'grey.200',
                  color: 'text.secondary',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontWeight: 'medium',
                  fontSize: '0.75rem',
                  flexShrink: 0,
                }}
              >
                {index + 1}
              </Box>

              {/* Content */}
              <Box flex={1} sx={{ minWidth: 0 }}>
                <Typography variant="body2" fontWeight={500} sx={{ lineHeight: 1.3 }}>
                  {task.taskDescription}
                </Typography>
                {task.standard && (
                  <Typography
                    variant="caption"
                    color="text.secondary"
                    sx={{ mt: 0.25, display: 'block', fontStyle: 'italic' }}
                  >
                    Standard: {task.standard}
                  </Typography>
                )}
                <Stack direction="row" spacing={1} sx={{ mt: 0.5 }}>
                  {task.linkedInjectCount > 0 ? (
                    <Chip
                      icon={<FontAwesomeIcon icon={faPaperclip} />}
                      label={`${task.linkedInjectCount} inject${task.linkedInjectCount !== 1 ? 's' : ''}`}
                      size="small"
                      variant="outlined"
                      sx={{ height: 20, fontSize: '0.7rem' }}
                    />
                  ) : (
                    <Chip
                      icon={<FontAwesomeIcon icon={faTriangleExclamation} />}
                      label="No injects"
                      size="small"
                      color="warning"
                      variant="outlined"
                      sx={{ height: 20, fontSize: '0.7rem' }}
                    />
                  )}
                  {task.eegEntryCount > 0 && (
                    <Chip
                      icon={<FontAwesomeIcon icon={faFileLines} />}
                      label={`${task.eegEntryCount} entr${task.eegEntryCount !== 1 ? 'ies' : 'y'}`}
                      size="small"
                      variant="outlined"
                      sx={{ height: 20, fontSize: '0.7rem' }}
                    />
                  )}
                </Stack>
              </Box>

              {/* Actions */}
              {canEdit && (
                <Stack direction="row" spacing={0} sx={{ flexShrink: 0 }}>
                  <Tooltip title="Edit task">
                    <IconButton size="small" onClick={() => handleOpenEdit(task)}>
                      <FontAwesomeIcon icon={faPen} size="xs" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Delete task">
                    <IconButton
                      size="small"
                      onClick={() => setDeletingTask(task)}
                      color="error"
                    >
                      <FontAwesomeIcon icon={faTrash} size="xs" />
                    </IconButton>
                  </Tooltip>
                </Stack>
              )}
            </Paper>
          ))}
        </Stack>
      )}

      {/* Tip */}
      {criticalTasks.length > 0 && criticalTasks.some(t => t.linkedInjectCount === 0) && (
        <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
          Link injects to tasks in the MSEL view to enable traceability.
        </Typography>
      )}

      {/* Form Dialog */}
      <CriticalTaskFormDialog
        open={isFormOpen}
        task={editingTask}
        capabilityTargetName={capabilityTargetName}
        onClose={handleCloseForm}
        onCreate={handleCreate}
        onUpdate={handleUpdate}
      />

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        open={!!deletingTask}
        title="Delete Critical Task"
        message={
          <>
            Are you sure you want to delete this Critical Task?
            <br />
            <br />
            <strong>&quot;{deletingTask?.taskDescription}&quot;</strong>
            {deletingTask && getDeleteWarning(deletingTask) && (
              <>
                <br />
                <br />
                <Typography component="span" color="warning.main">
                  {getDeleteWarning(deletingTask)}
                </Typography>
              </>
            )}
          </>
        }
        confirmLabel="Delete"
        severity="danger"
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeletingTask(null)}
        isConfirming={isDeleting}
      />
    </Box>
  )
}

export default CriticalTaskList
