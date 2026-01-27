import { useState, useEffect, useMemo } from 'react'
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
  Paper,
  Tooltip,
  Skeleton,
  Chip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faScrewdriverWrench, faClipboardList, faListCheck } from '@fortawesome/free-solid-svg-icons'
import { ExerciseStatusChip, ExerciseTypeChip } from '../../exercises'
import { formatDate } from '../../../shared/utils/dateUtils'
import { CobraPrimaryButton } from '../../../theme/styledComponents'
import { ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../../exercises'
import { useAuth } from '../../../contexts/AuthContext'
import { roleResolutionService, getRoleDisplayName, getRoleColor } from '@/features/auth'
import type { ExerciseAssignmentDto, ExerciseRole } from '@/features/auth'

interface ExerciseListProps {
  exercises: ExerciseDto[]
  loading: boolean
  error: string | null
  canManage: boolean
  onCreateClick: () => void
  /** Maximum number of items to display. If undefined, shows all. */
  maxItems?: number
}

/**
 * Exercise List Component
 *
 * Displays a list of exercises in a table format with:
 * - Name, Type, Status, Date columns
 * - Practice mode indicator
 * - Click to navigate to exercise detail
 * - Empty states for no exercises or no access
 */
export const ExerciseList = ({
  exercises,
  loading,
  error,
  canManage,
  onCreateClick,
  maxItems,
}: ExerciseListProps) => {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [exerciseAssignments, setExerciseAssignments] = useState<ExerciseAssignmentDto[]>([])

  // Fetch user's exercise role assignments
  useEffect(() => {
    if (!user?.id) return

    const fetchAssignments = async () => {
      try {
        const assignments = await roleResolutionService.getUserExerciseAssignments(user.id)
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

  // Filter out archived and apply maxItems limit
  const displayedExercises = exercises
    .filter(e => e.status !== ExerciseStatus.Archived)
    .slice(0, maxItems)

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
    return <ExerciseListSkeleton />
  }

  // Empty state
  if (exercises.length === 0) {
    return (
      <EmptyState canManage={canManage} onCreateClick={onCreateClick} />
    )
  }

  // No non-archived exercises
  if (displayedExercises.length === 0) {
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
          All exercises are archived. Check the full exercise list to view archived exercises.
        </Typography>
      </Paper>
    )
  }

  return (
    <TableContainer component={Paper}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Date</TableCell>
            <TableCell>Your Role</TableCell>
            <TableCell width={60}>Practice</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {displayedExercises.map(exercise => {
            const userRole = roleByExerciseId.get(exercise.id)
            return (
              <TableRow
                key={exercise.id}
                hover
                onClick={() => handleRowClick(exercise.id)}
                sx={{
                  cursor: 'pointer',
                  '& td': { py: 1.5 },
                }}
              >
                <TableCell>
                  <Typography variant="body2" fontWeight={500}>
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
                  {userRole ? (
                    <Chip
                      label={getRoleDisplayName(userRole)}
                      size="small"
                      color={getRoleColor(userRole)}
                      sx={{
                        fontWeight: 600,
                        fontSize: '0.7rem',
                      }}
                    />
                  ) : (
                    <Typography variant="body2" color="text.secondary" fontStyle="italic">
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
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

/**
 * Loading skeleton for the exercise list
 */
const ExerciseListSkeleton = () => {
  const skeletonRows = Array.from({ length: 3 }, (_, i) => i)

  return (
    <TableContainer component={Paper}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Date</TableCell>
            <TableCell>Your Role</TableCell>
            <TableCell>Practice</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {skeletonRows.map(index => (
            <TableRow key={index}>
              <TableCell>
                <Skeleton variant="text" width={160} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={45} height={22} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={60} height={22} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={90} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={70} height={22} />
              </TableCell>
              <TableCell>
                <Skeleton variant="circular" width={18} height={18} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

interface EmptyStateProps {
  canManage: boolean
  onCreateClick: () => void
}

const EmptyState = ({ canManage, onCreateClick }: EmptyStateProps) => {
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
        <CobraPrimaryButton
          startIcon={<FontAwesomeIcon icon={faPlus} />}
          onClick={onCreateClick}
        >
          Create Exercise
        </CobraPrimaryButton>
      </Paper>
    )
  }

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

export default ExerciseList
