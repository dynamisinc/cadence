/**
 * CriticalTaskList Component
 *
 * Displays a list of Critical Tasks within a Capability Target.
 * Supports CRUD operations, drag-and-drop reordering, and inject linking (S04-S05).
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
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faPen,
  faTrash,
  faPaperclip,
  faFileLines,
  faTriangleExclamation,
  faGripVertical,
  faChevronUp,
  faChevronDown,
} from '@fortawesome/free-solid-svg-icons'
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { useCriticalTasks } from '../hooks/useCriticalTasks'
import { CriticalTaskFormDialog } from './CriticalTaskFormDialog'
import { LinkedInjectsDialog } from './LinkedInjectsDialog'
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

interface SortableTaskItemProps {
  task: CriticalTaskDto
  index: number
  canEdit: boolean
  isMobile: boolean
  isFirst: boolean
  isLast: boolean
  isReordering: boolean
  onEdit: (task: CriticalTaskDto) => void
  onDelete: (task: CriticalTaskDto) => void
  onLinkInjects: (task: CriticalTaskDto) => void
  onMoveUp: () => void
  onMoveDown: () => void
}

/**
 * Sortable task item with drag handle (S04)
 * Uses compact horizontal layout on md+ screens, vertical on mobile
 */
const SortableTaskItem: FC<SortableTaskItemProps> = ({
  task,
  index,
  canEdit,
  isMobile,
  isFirst,
  isLast,
  isReordering,
  onEdit,
  onDelete,
  onLinkInjects,
  onMoveUp,
  onMoveDown,
}) => {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: task.id,
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  }

  return (
    <Paper
      ref={setNodeRef}
      style={style}
      sx={{
        px: 1.5,
        py: isMobile ? 1 : 0.75,
        display: 'flex',
        alignItems: isMobile ? 'flex-start' : 'center',
        gap: isMobile ? 1.5 : 1,
        bgcolor: 'background.paper',
      }}
      variant="outlined"
    >
      {/* Drag Handle (desktop) or Move Buttons (mobile) */}
      {canEdit && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.25, pt: isMobile ? 0.25 : 0 }}>
          {isMobile ? (
            <>
              <IconButton
                size="small"
                onClick={onMoveUp}
                disabled={isFirst || isReordering}
                sx={{ p: 0.25 }}
              >
                <FontAwesomeIcon icon={faChevronUp} size="xs" />
              </IconButton>
              <IconButton
                size="small"
                onClick={onMoveDown}
                disabled={isLast || isReordering}
                sx={{ p: 0.25 }}
              >
                <FontAwesomeIcon icon={faChevronDown} size="xs" />
              </IconButton>
            </>
          ) : (
            <Box
              {...attributes}
              {...listeners}
              sx={{
                cursor: isDragging ? 'grabbing' : 'grab',
                color: 'text.secondary',
                display: 'flex',
                alignItems: 'center',
                p: 0.25,
                '&:hover': {
                  color: 'primary.main',
                },
              }}
            >
              <FontAwesomeIcon icon={faGripVertical} size="sm" />
            </Box>
          )}
        </Box>
      )}

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

      {/* Content - Horizontal layout on md+, vertical on mobile */}
      <Box
        flex={1}
        sx={{
          minWidth: 0,
          display: 'flex',
          flexDirection: isMobile ? 'column' : 'row',
          alignItems: isMobile ? 'flex-start' : 'center',
          gap: isMobile ? 0 : 1.5,
        }}
      >
        {/* Task Description */}
        <Box
          flex={1}
          sx={{
            minWidth: 0,
            display: 'flex',
            flexDirection: 'column',
            gap: isMobile ? 0.25 : 0,
          }}
        >
          <Typography variant="body2" fontWeight={500} sx={{ lineHeight: 1.3 }}>
            {task.taskDescription}
          </Typography>
          {task.standard && (
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{
                mt: isMobile ? 0.25 : 0,
                display: isMobile ? 'block' : 'inline',
                fontStyle: 'italic',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: isMobile ? 'normal' : 'nowrap',
              }}
            >
              Standard: {task.standard}
            </Typography>
          )}
        </Box>

        {/* Chips - Inline on md+, below on mobile */}
        <Stack
          direction="row"
          spacing={0.5}
          sx={{
            mt: isMobile ? 0.5 : 0,
            flexShrink: 0,
            alignItems: 'center',
          }}
        >
          {/* Inject Count Chip - Clickable to link injects (S05) */}
          {task.linkedInjectCount > 0 ? (
            <Chip
              icon={<FontAwesomeIcon icon={faPaperclip} />}
              label={`${task.linkedInjectCount} inject${task.linkedInjectCount !== 1 ? 's' : ''}`}
              size="small"
              variant="outlined"
              onClick={canEdit ? () => onLinkInjects(task) : undefined}
              sx={{
                height: 20,
                fontSize: '0.7rem',
                cursor: canEdit ? 'pointer' : 'default',
                '&:hover': canEdit ? { bgcolor: 'action.hover' } : {},
              }}
            />
          ) : (
            <Chip
              icon={<FontAwesomeIcon icon={faTriangleExclamation} />}
              label="No injects"
              size="small"
              color="warning"
              variant="outlined"
              onClick={canEdit ? () => onLinkInjects(task) : undefined}
              sx={{
                height: 20,
                fontSize: '0.7rem',
                cursor: canEdit ? 'pointer' : 'default',
                '&:hover': canEdit ? { bgcolor: 'action.hover' } : {},
              }}
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
            <IconButton size="small" onClick={() => onEdit(task)}>
              <FontAwesomeIcon icon={faPen} size="xs" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Delete task">
            <IconButton size="small" onClick={() => onDelete(task)} color="error">
              <FontAwesomeIcon icon={faTrash} size="xs" />
            </IconButton>
          </Tooltip>
        </Stack>
      )}
    </Paper>
  )
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
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))

  const {
    criticalTasks,
    loading,
    error,
    createCriticalTask,
    updateCriticalTask,
    deleteCriticalTask,
    reorderCriticalTasks,
    isDeleting,
    isReordering,
  } = useCriticalTasks(exerciseId, capabilityTargetId)

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingTask, setEditingTask] = useState<CriticalTaskDto | null>(null)
  const [deletingTask, setDeletingTask] = useState<CriticalTaskDto | null>(null)
  const [linkingTask, setLinkingTask] = useState<CriticalTaskDto | null>(null)

  // Drag and drop sensors
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  )

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

  const handleOpenInjectLinking = (task: CriticalTaskDto) => {
    setLinkingTask(task)
  }

  const handleCloseInjectLinking = () => {
    setLinkingTask(null)
  }

  // Drag and drop handler
  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event

    if (over && active.id !== over.id) {
      const oldIndex = criticalTasks.findIndex(t => t.id === active.id)
      const newIndex = criticalTasks.findIndex(t => t.id === over.id)

      const reorderedTasks = arrayMove(criticalTasks, oldIndex, newIndex)
      const orderedIds = reorderedTasks.map(t => t.id)

      await reorderCriticalTasks(orderedIds)
    }
  }

  // Mobile up/down arrow handlers
  const handleMoveUp = async (index: number) => {
    if (index === 0) return
    const reorderedTasks = arrayMove(criticalTasks, index, index - 1)
    const orderedIds = reorderedTasks.map(t => t.id)
    await reorderCriticalTasks(orderedIds)
  }

  const handleMoveDown = async (index: number) => {
    if (index === criticalTasks.length - 1) return
    const reorderedTasks = arrayMove(criticalTasks, index, index + 1)
    const orderedIds = reorderedTasks.map(t => t.id)
    await reorderCriticalTasks(orderedIds)
  }

  // Build delete warning message
  const getDeleteWarning = (task: CriticalTaskDto) => {
    const warnings: string[] = []
    if (task.linkedInjectCount > 0) {
      warnings.push(
        `${task.linkedInjectCount} linked inject ` +
          `association${task.linkedInjectCount !== 1 ? 's' : ''}`,
      )
    }
    if (task.eegEntryCount > 0) {
      warnings.push(`${task.eegEntryCount} EEG entr${task.eegEntryCount !== 1 ? 'ies' : 'y'}`)
    }
    return warnings.length > 0 ? `This will also delete: ${warnings.join(' and ')}.` : null
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
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext
            items={criticalTasks.map(t => t.id)}
            strategy={verticalListSortingStrategy}
          >
            <Stack spacing={0.5}>
              {criticalTasks.map((task, index) => (
                <SortableTaskItem
                  key={task.id}
                  task={task}
                  index={index}
                  canEdit={canEdit}
                  isMobile={isMobile}
                  isFirst={index === 0}
                  isLast={index === criticalTasks.length - 1}
                  isReordering={isReordering}
                  onEdit={handleOpenEdit}
                  onDelete={setDeletingTask}
                  onLinkInjects={handleOpenInjectLinking}
                  onMoveUp={() => handleMoveUp(index)}
                  onMoveDown={() => handleMoveDown(index)}
                />
              ))}
            </Stack>
          </SortableContext>
        </DndContext>
      )}

      {/* Tip */}
      {criticalTasks.length > 0 && criticalTasks.some(t => t.linkedInjectCount === 0) && (
        <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
          Click on the warning icon or &quot;No injects&quot; chip to link injects to tasks.
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

      {/* Inject Linking Dialog (S05) */}
      <LinkedInjectsDialog
        open={!!linkingTask}
        exerciseId={exerciseId}
        task={linkingTask}
        onClose={handleCloseInjectLinking}
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
