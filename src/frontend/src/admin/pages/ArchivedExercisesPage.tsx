/**
 * Archived Exercises Management Page
 *
 * Admin-only page for managing archived exercises:
 * - View all archived exercises with data summary
 * - Restore exercises to their previous status
 * - Permanently delete archived exercises
 * - Bulk selection and actions
 */

import { useState, useMemo, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Typography,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  Paper,
  Checkbox,
  IconButton,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
  Chip,
  Skeleton,
  Alert,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faBoxArchive,
  faRotateLeft,
  faTrash,
  faEllipsisVertical,
} from '@fortawesome/free-solid-svg-icons'
import { formatDate } from '@/shared/utils/dateUtils'
import { notify } from '@/shared/utils/notify'

import {
  CobraPrimaryButton,
  CobraTextField,
} from '../../theme/styledComponents'
import CobraStyles from '../../theme/CobraStyles'
import { PageHeader } from '../../shared/components'
import { exerciseService } from '../../features/exercises/services/exerciseService'
import { RestoreExerciseDialog } from '../../features/exercises/components/RestoreExerciseDialog'
import { DeleteExerciseDialog } from '../../features/exercises/components/DeleteExerciseDialog'
import { ExerciseTypeChip } from '../../features/exercises/components'
import type { ExerciseDto } from '../../features/exercises/types'
import { ExerciseStatus } from '../../types'

type SortField = 'name' | 'previousStatus' | 'archivedAt' | 'scheduledDate'
type SortOrder = 'asc' | 'desc'

/**
 * Get display label for exercise status
 */
const getStatusLabel = (status: ExerciseStatus | null): string => {
  switch (status) {
    case ExerciseStatus.Draft:
    case null:
      return 'Draft'
    case ExerciseStatus.Active:
      return 'Active'
    case ExerciseStatus.Paused:
      return 'Paused'
    case ExerciseStatus.Completed:
      return 'Completed'
    case ExerciseStatus.Archived:
      return 'Archived'
    default:
      return String(status)
  }
}

/**
 * Get status chip color
 */
const getStatusColor = (
  status: ExerciseStatus | null,
): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
  switch (status) {
    case ExerciseStatus.Draft:
    case null:
      return 'default'
    case ExerciseStatus.Active:
      return 'success'
    case ExerciseStatus.Paused:
      return 'warning'
    case ExerciseStatus.Completed:
      return 'info'
    default:
      return 'default'
  }
}

/**
 * Archived Exercises Management Page
 */
export const ArchivedExercisesPage = () => {
  const queryClient = useQueryClient()

  // State
  const [searchTerm, setSearchTerm] = useState('')
  const [sortField, setSortField] = useState<SortField>('archivedAt')
  const [sortOrder, setSortOrder] = useState<SortOrder>('desc')
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [menuAnchor, setMenuAnchor] = useState<{ element: HTMLElement; exerciseId: string } | null>(
    null,
  )
  const [restoreDialogExercise, setRestoreDialogExercise] = useState<ExerciseDto | null>(null)
  const [deleteDialogExercise, setDeleteDialogExercise] = useState<ExerciseDto | null>(null)

  // Fetch archived exercises only
  const {
    data: exercises = [],
    isLoading,
    error,
  } = useQuery({
    queryKey: ['exercises', 'archived'],
    queryFn: () => exerciseService.getExercises({ archivedOnly: true }),
  })

  // Restore mutation
  const restoreMutation = useMutation({
    mutationFn: (id: string) => exerciseService.unarchiveExercise(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['exercises'] })
      notify.success('Exercise restored successfully')
    },
    onError: () => {
      notify.error('Failed to restore exercise')
    },
  })

  // Filter and sort exercises
  const filteredExercises = useMemo(() => {
    let filtered = exercises

    // Filter by search term
    if (searchTerm) {
      const search = searchTerm.toLowerCase()
      filtered = filtered.filter(e => e.name.toLowerCase().includes(search))
    }

    // Sort
    filtered = [...filtered].sort((a, b) => {
      let comparison = 0

      switch (sortField) {
        case 'name':
          comparison = a.name.localeCompare(b.name)
          break
        case 'previousStatus':
          comparison = getStatusLabel(a.previousStatus).localeCompare(
            getStatusLabel(b.previousStatus),
          )
          break
        case 'archivedAt':
          comparison = (a.archivedAt || '').localeCompare(b.archivedAt || '')
          break
        case 'scheduledDate':
          comparison = a.scheduledDate.localeCompare(b.scheduledDate)
          break
      }

      return sortOrder === 'asc' ? comparison : -comparison
    })

    return filtered
  }, [exercises, searchTerm, sortField, sortOrder])

  // Handlers
  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortOrder('asc')
    }
  }

  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      setSelectedIds(new Set(filteredExercises.map(e => e.id)))
    } else {
      setSelectedIds(new Set())
    }
  }

  const handleSelectOne = (id: string, checked: boolean) => {
    const newSelected = new Set(selectedIds)
    if (checked) {
      newSelected.add(id)
    } else {
      newSelected.delete(id)
    }
    setSelectedIds(newSelected)
  }

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, exerciseId: string) => {
    event.stopPropagation()
    setMenuAnchor({ element: event.currentTarget, exerciseId })
  }

  const handleMenuClose = () => {
    setMenuAnchor(null)
  }

  const handleRestore = useCallback(
    (exercise: ExerciseDto) => {
      setRestoreDialogExercise(exercise)
      handleMenuClose()
    },
    [],
  )

  const handleDelete = useCallback(
    (exercise: ExerciseDto) => {
      setDeleteDialogExercise(exercise)
      handleMenuClose()
    },
    [],
  )

  const handleRestoreConfirm = async () => {
    if (restoreDialogExercise) {
      await restoreMutation.mutateAsync(restoreDialogExercise.id)
      setRestoreDialogExercise(null)
    }
  }

  const handleDeleteComplete = () => {
    queryClient.invalidateQueries({ queryKey: ['exercises'] })
    setDeleteDialogExercise(null)
    notify.success('Exercise permanently deleted')
  }

  const handleBulkRestore = async () => {
    const ids = Array.from(selectedIds)
    for (const id of ids) {
      await restoreMutation.mutateAsync(id)
    }
    setSelectedIds(new Set())
  }

  const formatDateSafe = (dateStr: string | null) => {
    if (!dateStr) return '-'
    try {
      return formatDate(dateStr)
    } catch {
      return dateStr
    }
  }

  // Get the currently selected menu exercise
  const menuExercise = menuAnchor
    ? exercises.find(e => e.id === menuAnchor.exerciseId)
    : null

  // Error state
  if (error) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load archived exercises. Please try again.
        </Alert>
        <CobraPrimaryButton onClick={() => queryClient.invalidateQueries({ queryKey: ['exercises'] })}>
          Retry
        </CobraPrimaryButton>
      </Box>
    )
  }

  const allSelected = filteredExercises.length > 0 && selectedIds.size === filteredExercises.length
  const someSelected = selectedIds.size > 0 && selectedIds.size < filteredExercises.length

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="Archived Exercises"
        icon={faBoxArchive}
        subtitle="Manage archived exercises. Restore them to make them active again, or permanently delete them."
      />

      {/* Summary Stats */}
      <Paper sx={{ p: 2, mb: 3, bgcolor: 'grey.50' }}>
        <Stack direction="row" spacing={4}>
          <Box>
            <Typography variant="h4" component="span" fontWeight={600}>
              {exercises.length}
            </Typography>
            <Typography variant="body2" color="text.secondary" component="span" sx={{ ml: 1 }}>
              archived exercises
            </Typography>
          </Box>
        </Stack>
      </Paper>

      {/* Search and Bulk Actions */}
      <Stack direction="row" spacing={2} marginBottom={2} alignItems="center">
        <CobraTextField
          placeholder="Search by name..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          sx={{ width: 300 }}
        />

        {selectedIds.size > 0 && (
          <>
            <Divider orientation="vertical" flexItem />
            <Typography variant="body2" color="text.secondary">
              {selectedIds.size} selected
            </Typography>
            <CobraPrimaryButton
              size="small"
              startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
              onClick={handleBulkRestore}
              disabled={restoreMutation.isPending}
            >
              Restore Selected
            </CobraPrimaryButton>
          </>
        )}
      </Stack>

      {/* Table */}
      {isLoading ? (
        <ArchivedExerciseTableSkeleton />
      ) : filteredExercises.length === 0 ? (
        <EmptyState hasExercises={exercises.length > 0} />
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell padding="checkbox">
                  <Checkbox
                    checked={allSelected}
                    indeterminate={someSelected}
                    onChange={e => handleSelectAll(e.target.checked)}
                  />
                </TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'name'}
                    direction={sortField === 'name' ? sortOrder : 'asc'}
                    onClick={() => handleSort('name')}
                  >
                    Exercise Name
                  </TableSortLabel>
                </TableCell>
                <TableCell>Type</TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'previousStatus'}
                    direction={sortField === 'previousStatus' ? sortOrder : 'asc'}
                    onClick={() => handleSort('previousStatus')}
                  >
                    Previous Status
                  </TableSortLabel>
                </TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'archivedAt'}
                    direction={sortField === 'archivedAt' ? sortOrder : 'asc'}
                    onClick={() => handleSort('archivedAt')}
                  >
                    Archived
                  </TableSortLabel>
                </TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'scheduledDate'}
                    direction={sortField === 'scheduledDate' ? sortOrder : 'asc'}
                    onClick={() => handleSort('scheduledDate')}
                  >
                    Original Date
                  </TableSortLabel>
                </TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredExercises.map(exercise => (
                <TableRow
                  key={exercise.id}
                  hover
                  selected={selectedIds.has(exercise.id)}
                >
                  <TableCell padding="checkbox">
                    <Checkbox
                      checked={selectedIds.has(exercise.id)}
                      onChange={e => handleSelectOne(exercise.id, e.target.checked)}
                      onClick={e => e.stopPropagation()}
                    />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body1">{exercise.name}</Typography>
                  </TableCell>
                  <TableCell>
                    <ExerciseTypeChip type={exercise.exerciseType} />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={getStatusLabel(exercise.previousStatus)}
                      size="small"
                      color={getStatusColor(exercise.previousStatus)}
                    />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">{formatDateSafe(exercise.archivedAt)}</Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">{formatDateSafe(exercise.scheduledDate)}</Typography>
                  </TableCell>
                  <TableCell align="right">
                    <IconButton
                      size="small"
                      onClick={e => handleMenuOpen(e, exercise.id)}
                      aria-label="Actions"
                    >
                      <FontAwesomeIcon icon={faEllipsisVertical} />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Row Actions Menu */}
      <Menu
        anchorEl={menuAnchor?.element}
        open={Boolean(menuAnchor)}
        onClose={handleMenuClose}
      >
        {menuExercise && (
          <>
            <MenuItem onClick={() => handleRestore(menuExercise)}>
              <ListItemIcon>
                <FontAwesomeIcon icon={faRotateLeft} />
              </ListItemIcon>
              <ListItemText>Restore</ListItemText>
            </MenuItem>
            <Divider />
            <MenuItem onClick={() => handleDelete(menuExercise)} sx={{ color: 'error.main' }}>
              <ListItemIcon sx={{ color: 'error.main' }}>
                <FontAwesomeIcon icon={faTrash} />
              </ListItemIcon>
              <ListItemText>Permanently Delete</ListItemText>
            </MenuItem>
          </>
        )}
      </Menu>

      {/* Restore Dialog */}
      <RestoreExerciseDialog
        open={restoreDialogExercise !== null}
        exercise={restoreDialogExercise}
        onClose={() => setRestoreDialogExercise(null)}
        onConfirm={handleRestoreConfirm}
        isRestoring={restoreMutation.isPending}
      />

      {/* Delete Dialog */}
      <DeleteExerciseDialog
        open={deleteDialogExercise !== null}
        exercise={deleteDialogExercise}
        onClose={() => setDeleteDialogExercise(null)}
        onDeleted={handleDeleteComplete}
      />
    </Box>
  )
}

/**
 * Loading skeleton for the archived exercises table
 */
const ArchivedExerciseTableSkeleton = () => {
  const skeletonRows = Array.from({ length: 5 }, (_, i) => i)

  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell padding="checkbox">
              <Skeleton variant="rectangular" width={20} height={20} />
            </TableCell>
            <TableCell>Exercise Name</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Previous Status</TableCell>
            <TableCell>Archived</TableCell>
            <TableCell>Original Date</TableCell>
            <TableCell align="right">Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {skeletonRows.map(index => (
            <TableRow key={index}>
              <TableCell padding="checkbox">
                <Skeleton variant="rectangular" width={20} height={20} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={180} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={50} height={24} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={70} height={24} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={100} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={100} />
              </TableCell>
              <TableCell align="right">
                <Skeleton variant="circular" width={30} height={30} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

interface EmptyStateProps {
  hasExercises: boolean
}

const EmptyState = ({ hasExercises }: EmptyStateProps) => {
  if (hasExercises) {
    // Filtered to empty (search result)
    return (
      <Paper
        sx={{
          py: 6,
          px: 4,
          textAlign: 'center',
          backgroundColor: 'grey.50',
          border: '1px dashed',
          borderColor: 'grey.300',
        }}
      >
        <Typography variant="h6" gutterBottom>
          No matching archived exercises
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Try adjusting your search terms.
        </Typography>
      </Paper>
    )
  }

  // No archived exercises at all
  return (
    <Paper
      sx={{
        py: 8,
        px: 4,
        textAlign: 'center',
        backgroundColor: 'grey.50',
        border: '1px dashed',
        borderColor: 'grey.300',
      }}
    >
      <Box
        sx={{
          width: 80,
          height: 80,
          borderRadius: '50%',
          backgroundColor: 'grey.200',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          margin: '0 auto 16px',
          color: 'grey.500',
          fontSize: 40,
        }}
      >
        <FontAwesomeIcon icon={faBoxArchive} />
      </Box>
      <Typography variant="h6" gutterBottom>
        No Archived Exercises
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 400, mx: 'auto' }}>
        There are no archived exercises. When exercises are archived, they will appear here for
        management.
      </Typography>
    </Paper>
  )
}

export default ArchivedExercisesPage
