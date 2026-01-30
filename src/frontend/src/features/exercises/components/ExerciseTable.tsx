import { useMemo, useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  Paper,
  Tooltip,
  Skeleton,
  Chip,
  IconButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faScrewdriverWrench,
  faBan,
  faClipboardList,
  faListCheck,
  faFileImport,
} from '@fortawesome/free-solid-svg-icons'

import { ExerciseStatusChip } from './ExerciseStatusChip'
import { ExerciseTypeChip } from './ExerciseTypeChip'
import { formatDate } from '../../../shared/utils/dateUtils'
import { CobraPrimaryButton } from '../../../theme/styledComponents'
import { ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../types'
import { useAuth } from '../../../contexts/AuthContext'
import {
  roleResolutionService,
  getRoleDisplayName,
  getRoleColor,
} from '@/features/auth'
import type { ExerciseAssignmentDto, ExerciseRole } from '@/features/auth'

// =========================================================================
// Types
// =========================================================================

type SortField = 'name' | 'exerciseType' | 'status' | 'scheduledDate' | 'injectCount'
type SortOrder = 'asc' | 'desc'

export interface ExerciseTableProps {
  /** List of exercises to display */
  exercises: ExerciseDto[]
  /** Loading state - shows skeleton */
  loading?: boolean
  /** Error message to display */
  error?: string | null
  /** Whether user can manage exercises (create, import) */
  canManage?: boolean
  /** Callback for create button click */
  onCreateClick?: () => void
  /** Maximum number of items to display. If undefined, shows all. */
  maxItems?: number
  /** Enable sorting by clicking column headers */
  sortable?: boolean
  /** Enable import button per row (only for Draft exercises when canManage=true) */
  showImportButton?: boolean
  /** Callback when import button is clicked */
  onImportClick?: (exerciseId: string, event: React.MouseEvent) => void
  /** Table size: 'small' for compact, 'medium' for normal spacing */
  size?: 'small' | 'medium'
  /** Filter out archived exercises (default: true) */
  hideArchived?: boolean
}

// =========================================================================
// Main Component
// =========================================================================

/**
 * ExerciseTable Component
 *
 * Shared table component for displaying exercise lists with:
 * - Name, Type, Status, Date, Inject Count, User Role columns
 * - Practice mode indicator
 * - Optional sortable columns
 * - Optional import button per row
 * - Click to navigate to exercise detail
 * - Loading skeleton state
 * - Empty states for no exercises or no access
 *
 * Used by both HomePage (compact, limited items) and ExerciseListPage (full features).
 */
export const ExerciseTable = ({
  exercises,
  loading = false,
  error = null,
  canManage = false,
  onCreateClick,
  maxItems,
  sortable = false,
  showImportButton = false,
  onImportClick,
  size = 'medium',
  hideArchived = true,
}: ExerciseTableProps) => {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [exerciseAssignments, setExerciseAssignments] = useState<
    ExerciseAssignmentDto[]
  >([])

  // Sorting state (only used when sortable=true)
  const [sortField, setSortField] = useState<SortField>('scheduledDate')
  const [sortOrder, setSortOrder] = useState<SortOrder>('desc')

  // Fetch user's exercise role assignments
  useEffect(() => {
    if (!user?.id) return

    const fetchAssignments = async () => {
      try {
        const assignments =
          await roleResolutionService.getUserExerciseAssignments(user.id)
        setExerciseAssignments(assignments)
      } catch (err) {
        console.error('Failed to fetch exercise assignments:', err)
      }
    }

    fetchAssignments()
  }, [user?.id])

  // Create a map for quick role lookup by exercise ID
  const roleByExerciseId = useMemo(() => {
    const map = new Map<string, ExerciseRole>()
    exerciseAssignments.forEach(a => {
      map.set(a.exerciseId, a.exerciseRole as ExerciseRole)
    })
    return map
  }, [exerciseAssignments])

  // Status priority for default sorting (lower = higher priority)
  const getStatusPriority = (status: ExerciseStatus): number => {
    switch (status) {
      case ExerciseStatus.Active:
        return 1
      case ExerciseStatus.Draft:
        return 2
      case ExerciseStatus.Completed:
        return 3
      case ExerciseStatus.Archived:
        return 4
      default:
        return 5
    }
  }

  // Filter and sort exercises
  const displayedExercises = useMemo(() => {
    let filtered = exercises

    // Filter out archived if requested
    if (hideArchived) {
      filtered = filtered.filter(e => e.status !== ExerciseStatus.Archived)
    }

    // Sort exercises
    if (sortable) {
      // User-controlled sorting via column headers
      filtered = [...filtered].sort((a, b) => {
        let comparison = 0

        switch (sortField) {
          case 'name':
            comparison = a.name.localeCompare(b.name)
            break
          case 'exerciseType':
            comparison = a.exerciseType.localeCompare(b.exerciseType)
            break
          case 'status':
            comparison = a.status.localeCompare(b.status)
            break
          case 'scheduledDate':
            comparison = a.scheduledDate.localeCompare(b.scheduledDate)
            break
          case 'injectCount':
            comparison = a.injectCount - b.injectCount
            break
        }

        return sortOrder === 'asc' ? comparison : -comparison
      })
    } else {
      // Default sort: Status priority (Active first), then scheduled date asc (soonest first)
      filtered = [...filtered].sort((a, b) => {
        const statusDiff = getStatusPriority(a.status) - getStatusPriority(b.status)
        if (statusDiff !== 0) return statusDiff
        // Within same status, sort by scheduled date ascending (soonest first)
        return a.scheduledDate.localeCompare(b.scheduledDate)
      })
    }

    // Apply maxItems limit
    if (maxItems !== undefined) {
      filtered = filtered.slice(0, maxItems)
    }

    return filtered
  }, [exercises, hideArchived, sortable, sortField, sortOrder, maxItems])

  const handleSort = (field: SortField) => {
    if (!sortable) return

    if (sortField === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortOrder('asc')
    }
  }

  const handleRowClick = (id: string) => {
    navigate(`/exercises/${id}`)
  }

  // Error state
  if (error && exercises.length === 0) {
    return (
      <Paper sx={{ p: 3, textAlign: 'center' }}>
        <Typography color="error" gutterBottom>
          {error}
        </Typography>
        <CobraPrimaryButton onClick={() => window.location.reload()}>
          Retry
        </CobraPrimaryButton>
      </Paper>
    )
  }

  // Loading state
  if (loading && exercises.length === 0) {
    return <ExerciseTableSkeleton size={size} showImportButton={showImportButton && canManage} />
  }

  // Empty state - no exercises at all
  if (exercises.length === 0) {
    return (
      <EmptyState
        canManage={canManage}
        onCreateClick={onCreateClick}
        variant="no-exercises"
      />
    )
  }

  // Empty state - all filtered out (e.g., all archived)
  if (displayedExercises.length === 0) {
    return (
      <EmptyState
        canManage={canManage}
        onCreateClick={onCreateClick}
        variant="all-filtered"
      />
    )
  }

  const showActionsColumn = showImportButton && canManage

  return (
    <TableContainer component={Paper}>
      <Table size={size}>
        <TableHead>
          <TableRow>
            <TableCell>
              {sortable ? (
                <TableSortLabel
                  active={sortField === 'name'}
                  direction={sortField === 'name' ? sortOrder : 'asc'}
                  onClick={() => handleSort('name')}
                >
                  Name
                </TableSortLabel>
              ) : (
                'Name'
              )}
            </TableCell>
            <TableCell>
              {sortable ? (
                <TableSortLabel
                  active={sortField === 'exerciseType'}
                  direction={sortField === 'exerciseType' ? sortOrder : 'asc'}
                  onClick={() => handleSort('exerciseType')}
                >
                  Type
                </TableSortLabel>
              ) : (
                'Type'
              )}
            </TableCell>
            <TableCell>
              {sortable ? (
                <TableSortLabel
                  active={sortField === 'status'}
                  direction={sortField === 'status' ? sortOrder : 'asc'}
                  onClick={() => handleSort('status')}
                >
                  Status
                </TableSortLabel>
              ) : (
                'Status'
              )}
            </TableCell>
            <TableCell>
              {sortable ? (
                <TableSortLabel
                  active={sortField === 'scheduledDate'}
                  direction={sortField === 'scheduledDate' ? sortOrder : 'asc'}
                  onClick={() => handleSort('scheduledDate')}
                >
                  Date
                </TableSortLabel>
              ) : (
                'Date'
              )}
            </TableCell>
            <TableCell>
              {sortable ? (
                <TableSortLabel
                  active={sortField === 'injectCount'}
                  direction={sortField === 'injectCount' ? sortOrder : 'asc'}
                  onClick={() => handleSort('injectCount')}
                >
                  Progress
                </TableSortLabel>
              ) : (
                'Progress'
              )}
            </TableCell>
            <TableCell>Your Role</TableCell>
            <TableCell width={60}>Practice</TableCell>
            {showActionsColumn && <TableCell align="right">Actions</TableCell>}
          </TableRow>
        </TableHead>
        <TableBody>
          {displayedExercises.map(exercise => {
            const userRole = roleByExerciseId.get(exercise.id)
            const canImport =
              showImportButton &&
              canManage &&
              exercise.status === ExerciseStatus.Draft

            return (
              <TableRow
                key={exercise.id}
                hover
                onClick={() => handleRowClick(exercise.id)}
                sx={{
                  cursor: 'pointer',
                  '& td': { py: size === 'small' ? 1.5 : 2 },
                }}
              >
                <TableCell>
                  <Typography
                    variant={size === 'small' ? 'body2' : 'body1'}
                    fontWeight={500}
                  >
                    {exercise.name}
                  </Typography>
                </TableCell>
                <TableCell>
                  <ExerciseTypeChip type={exercise.exerciseType} />
                </TableCell>
                <TableCell>
                  <ExerciseStatusChip status={exercise.status} />
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {formatDate(exercise.scheduledDate)}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2" color="text.secondary">
                    {exercise.firedInjectCount}/{exercise.injectCount}
                  </Typography>
                </TableCell>
                <TableCell>
                  {userRole ? (
                    <Chip
                      label={getRoleDisplayName(userRole)}
                      size="small"
                      color={getRoleColor(userRole)}
                      sx={{
                        fontWeight: 600,
                        fontSize: size === 'small' ? '0.7rem' : '0.75rem',
                      }}
                    />
                  ) : (
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      fontStyle="italic"
                    >
                      Not assigned
                    </Typography>
                  )}
                </TableCell>
                <TableCell>
                  {exercise.isPracticeMode && (
                    <Tooltip title="Practice Mode - excluded from production reports">
                      <Box component="span" sx={{ color: 'text.secondary' }}>
                        <FontAwesomeIcon icon={faScrewdriverWrench} size="sm" />
                      </Box>
                    </Tooltip>
                  )}
                </TableCell>
                {showActionsColumn && (
                  <TableCell align="right">
                    {canImport && onImportClick && (
                      <Tooltip title="Import MSEL from Excel">
                        <IconButton
                          size="small"
                          onClick={e => onImportClick(exercise.id, e)}
                          sx={{
                            color: 'primary.main',
                            '&:hover': {
                              backgroundColor: 'primary.light',
                              color: 'primary.dark',
                            },
                          }}
                        >
                          <FontAwesomeIcon icon={faFileImport} />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                )}
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

// =========================================================================
// Skeleton Component
// =========================================================================

interface ExerciseTableSkeletonProps {
  size?: 'small' | 'medium'
  showImportButton?: boolean
}

const ExerciseTableSkeleton = ({
  size = 'medium',
  showImportButton = false,
}: ExerciseTableSkeletonProps) => {
  const skeletonRows = Array.from({ length: size === 'small' ? 3 : 5 }, (_, i) => i)

  return (
    <TableContainer component={Paper}>
      <Table size={size}>
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Date</TableCell>
            <TableCell>Progress</TableCell>
            <TableCell>Your Role</TableCell>
            <TableCell>Practice</TableCell>
            {showImportButton && <TableCell align="right">Actions</TableCell>}
          </TableRow>
        </TableHead>
        <TableBody>
          {skeletonRows.map(index => (
            <TableRow key={index}>
              <TableCell>
                <Skeleton variant="text" width={size === 'small' ? 160 : 180} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={size === 'small' ? 45 : 50} height={size === 'small' ? 22 : 24} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={size === 'small' ? 60 : 70} height={size === 'small' ? 22 : 24} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={size === 'small' ? 90 : 100} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={45} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={size === 'small' ? 70 : 80} height={size === 'small' ? 22 : 24} />
              </TableCell>
              <TableCell>
                <Skeleton variant="circular" width={size === 'small' ? 18 : 20} height={size === 'small' ? 18 : 20} />
              </TableCell>
              {showImportButton && (
                <TableCell>
                  <Skeleton variant="circular" width={24} height={24} />
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

// =========================================================================
// Empty State Component
// =========================================================================

interface EmptyStateProps {
  canManage: boolean
  onCreateClick?: () => void
  variant: 'no-exercises' | 'all-filtered' | 'no-matches'
}

const EmptyState = ({ canManage, onCreateClick, variant }: EmptyStateProps) => {
  // Filtered to empty (all archived)
  if (variant === 'all-filtered') {
    return (
      <Paper
        sx={{
          py: 4,
          px: 3,
          textAlign: 'center',
          backgroundColor: 'grey.50',
          border: '1px dashed',
          borderColor: 'grey.300',
        }}
      >
        <Typography variant="body1" color="text.secondary">
          All exercises are archived. Check the full exercise list to view
          archived exercises.
        </Typography>
      </Paper>
    )
  }

  // No matching exercises (search/filter)
  if (variant === 'no-matches') {
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
          <FontAwesomeIcon icon={faBan} />
        </Box>
        <Typography variant="h6" gutterBottom>
          No matching exercises
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ maxWidth: 300, mx: 'auto' }}
        >
          Try adjusting your search terms or clear filters to see all exercises.
        </Typography>
      </Paper>
    )
  }

  // No exercises at all - manager can create
  if (canManage) {
    return (
      <Paper
        sx={{
          py: 6,
          px: 4,
          textAlign: 'center',
          backgroundColor: 'primary.50',
          border: '1px dashed',
          borderColor: 'primary.200',
        }}
      >
        <Box
          sx={{
            width: 80,
            height: 80,
            borderRadius: '50%',
            background: 'linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 20px',
            boxShadow: '0 4px 16px rgba(33, 150, 243, 0.15)',
            color: 'primary.main',
            fontSize: 40,
          }}
        >
          <FontAwesomeIcon icon={faListCheck} />
        </Box>
        <Typography variant="h6" gutterBottom fontWeight={500}>
          Create Your First Exercise
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ maxWidth: 360, mx: 'auto', mb: 2 }}
        >
          Get started by creating an exercise to manage your MSEL and conduct
          operations-based training.
        </Typography>
        {onCreateClick && (
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faPlus} />}
            onClick={onCreateClick}
          >
            Create Exercise
          </CobraPrimaryButton>
        )}
      </Paper>
    )
  }

  // Non-manager with no exercises assigned
  return (
    <Paper
      sx={{
        py: 5,
        px: 4,
        textAlign: 'center',
        backgroundColor: 'grey.50',
        border: '1px dashed',
        borderColor: 'grey.300',
      }}
    >
      <Box
        sx={{
          width: 64,
          height: 64,
          borderRadius: '50%',
          backgroundColor: 'grey.200',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          margin: '0 auto 16px',
          color: 'grey.500',
          fontSize: 32,
        }}
      >
        <FontAwesomeIcon icon={faClipboardList} />
      </Box>
      <Typography variant="h6" gutterBottom>
        No Exercises Assigned
      </Typography>
      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ maxWidth: 320, mx: 'auto' }}
      >
        You haven't been assigned to any exercises yet. Contact your Exercise
        Director to get added to an upcoming exercise.
      </Typography>
    </Paper>
  )
}

export default ExerciseTable
