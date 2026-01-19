import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
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
  Tooltip,
  Skeleton,
  Chip,
  IconButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faScrewdriverWrench, faBan, faClipboardList, faListCheck, faFileImport } from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import { useExercises } from '../hooks'
import { ExerciseStatusChip, ExerciseTypeChip } from '../components'
import {
  CobraPrimaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { usePermissions } from '../../../shared/hooks'
import { ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../types'
import { ImportWizard } from '../../excel-import/components'

type SortField = 'name' | 'exerciseType' | 'status' | 'scheduledDate'
type SortOrder = 'asc' | 'desc'

/**
 * Exercise List Page (S03)
 *
 * Displays all exercises the user has access to with:
 * - Sortable columns (Name, Type, Status, Date)
 * - Search/filter by name
 * - Status and type chips with COBRA colors
 * - Practice mode indicator
 * - Create button for Administrators/Exercise Directors
 */
export const ExerciseListPage = () => {
  const navigate = useNavigate()
  const { exercises, loading, isFetching, error } = useExercises()
  const { canManage } = usePermissions()

  const [searchTerm, setSearchTerm] = useState('')
  const [sortField, setSortField] = useState<SortField>('scheduledDate')
  const [sortOrder, setSortOrder] = useState<SortOrder>('desc')
  const [showArchived, setShowArchived] = useState(false)
  const [importExerciseId, setImportExerciseId] = useState<string | null>(null)

  // Filter and sort exercises
  const filteredExercises = useMemo(() => {
    let filtered = exercises

    // Filter out archived unless showing archived
    if (!showArchived) {
      filtered = filtered.filter(e => e.status !== ExerciseStatus.Archived)
    }

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
        case 'exerciseType':
          comparison = a.exerciseType.localeCompare(b.exerciseType)
          break
        case 'status':
          comparison = a.status.localeCompare(b.status)
          break
        case 'scheduledDate':
          comparison = a.scheduledDate.localeCompare(b.scheduledDate)
          break
      }

      return sortOrder === 'asc' ? comparison : -comparison
    })

    return filtered
  }, [exercises, searchTerm, sortField, sortOrder, showArchived])

  const handleSort = (field: SortField) => {
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

  const handleCreateClick = () => {
    navigate('/exercises/new')
  }

  const handleImportClick = (exerciseId: string, e: React.MouseEvent) => {
    e.stopPropagation() // Prevent row click navigation
    setImportExerciseId(exerciseId)
  }

  const handleImportWizardClose = () => {
    setImportExerciseId(null)
  }

  const formatDate = (dateStr: string) => {
    try {
      return format(parseISO(dateStr), 'MMM d, yyyy')
    } catch {
      return dateStr
    }
  }

  // Error state
  if (error && exercises.length === 0) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography color="error" gutterBottom>
          {error}
        </Typography>
        <CobraPrimaryButton onClick={() => window.location.reload()}>
          Retry
        </CobraPrimaryButton>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        marginBottom={3}
      >
        <Typography variant="h5" component="h1">
          Exercises
        </Typography>

        {canManage && (
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faPlus} />}
            onClick={handleCreateClick}
          >
            Create Exercise
          </CobraPrimaryButton>
        )}
      </Stack>

      {/* Search and Filters */}
      <Stack
        direction="row"
        spacing={2}
        marginBottom={2}
        alignItems="center"
      >
        <CobraTextField
          placeholder="Search exercises..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          sx={{ width: 300 }}
        />
        <Box
          component="label"
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1,
            cursor: 'pointer',
          }}
        >
          <input
            type="checkbox"
            checked={showArchived}
            onChange={e => setShowArchived(e.target.checked)}
          />
          <Typography variant="body2">Show Archived</Typography>
        </Box>
      </Stack>

      {/* Loading skeleton state - show during initial load OR background refetch with no data */}
      {(loading || isFetching) && exercises.length === 0 ? (
        <ExerciseTableSkeleton />
      ) : filteredExercises.length === 0 ? (
        <EmptyState
          hasExercises={exercises.length > 0}
          canManage={canManage}
          onCreateClick={handleCreateClick}
        />
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'name'}
                    direction={sortField === 'name' ? sortOrder : 'asc'}
                    onClick={() => handleSort('name')}
                  >
                    Name
                  </TableSortLabel>
                </TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'exerciseType'}
                    direction={sortField === 'exerciseType' ? sortOrder : 'asc'}
                    onClick={() => handleSort('exerciseType')}
                  >
                    Type
                  </TableSortLabel>
                </TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'status'}
                    direction={sortField === 'status' ? sortOrder : 'asc'}
                    onClick={() => handleSort('status')}
                  >
                    Status
                  </TableSortLabel>
                </TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'scheduledDate'}
                    direction={
                      sortField === 'scheduledDate' ? sortOrder : 'asc'
                    }
                    onClick={() => handleSort('scheduledDate')}
                  >
                    Date
                  </TableSortLabel>
                </TableCell>
                <TableCell>Practice</TableCell>
                {canManage && <TableCell align="right">Actions</TableCell>}
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredExercises.map(exercise => (
                <ExerciseRow
                  key={exercise.id}
                  exercise={exercise}
                  onClick={() => handleRowClick(exercise.id)}
                  formatDate={formatDate}
                  canManage={canManage}
                  onImportClick={(e) => handleImportClick(exercise.id, e)}
                />
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Import Wizard */}
      {importExerciseId && (
        <ImportWizard
          open={!!importExerciseId}
          onClose={handleImportWizardClose}
          exerciseId={importExerciseId}
        />
      )}
    </Box>
  )
}

/**
 * Loading skeleton for the exercise table
 */
const ExerciseTableSkeleton = () => {
  const skeletonRows = Array.from({ length: 5 }, (_, i) => i)

  return (
    <TableContainer component={Paper}>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Date</TableCell>
            <TableCell>Practice</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {skeletonRows.map(index => (
            <TableRow key={index}>
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
                <Skeleton variant="circular" width={20} height={20} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

interface ExerciseRowProps {
  exercise: ExerciseDto
  onClick: () => void
  formatDate: (date: string) => string
  canManage: boolean
  onImportClick: (e: React.MouseEvent) => void
}

const ExerciseRow = ({ exercise, onClick, formatDate, canManage, onImportClick }: ExerciseRowProps) => {
  // Only show import button for Draft exercises
  const canImport = canManage && exercise.status === ExerciseStatus.Draft

  return (
    <TableRow
      hover
      onClick={onClick}
      sx={{
        cursor: 'pointer',
        '& td': { minHeight: 44 }, // Touch-friendly target size
      }}
    >
      <TableCell>
        <Typography variant="body1">{exercise.name}</Typography>
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
        {exercise.isPracticeMode && (
          <Tooltip title="Practice Mode - excluded from production reports">
            <Chip
              icon={<FontAwesomeIcon icon={faScrewdriverWrench} size="xs" />}
              label="Practice"
              size="small"
              sx={{
                backgroundColor: 'warning.main',
                color: 'white',
                fontWeight: 500,
                '& .MuiChip-icon': {
                  color: 'white',
                  fontSize: '0.75rem',
                },
              }}
            />
          </Tooltip>
        )}
      </TableCell>
      {canManage && (
        <TableCell align="right">
          {canImport && (
            <Tooltip title="Import MSEL from Excel">
              <IconButton
                size="small"
                onClick={onImportClick}
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
}

interface EmptyStateProps {
  hasExercises: boolean
  canManage: boolean
  onCreateClick: () => void
}

const EmptyState = ({
  hasExercises,
  canManage,
  onCreateClick,
}: EmptyStateProps) => {
  if (hasExercises) {
    // Filtered to empty (search/filter result)
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
        <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 300, mx: 'auto' }}>
          Try adjusting your search terms or clear filters to see all exercises.
        </Typography>
      </Paper>
    )
  }

  // No exercises at all - different states for managers vs viewers
  if (canManage) {
    return (
      <Paper
        sx={{
          py: 8,
          px: 4,
          textAlign: 'center',
          backgroundColor: 'primary.50',
          border: '1px dashed',
          borderColor: 'primary.200',
        }}
      >
        <Box
          sx={{
            width: 100,
            height: 100,
            borderRadius: '50%',
            background: 'linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 24px',
            boxShadow: '0 4px 20px rgba(33, 150, 243, 0.15)',
            color: 'primary.main',
            fontSize: 50,
          }}
        >
          <FontAwesomeIcon icon={faListCheck} />
        </Box>
        <Typography variant="h5" gutterBottom fontWeight={500}>
          Create Your First Exercise
        </Typography>
        <Typography
          variant="body1"
          color="text.secondary"
          sx={{ maxWidth: 400, mx: 'auto', mb: 3 }}
        >
          Get started by creating an exercise. You can set up tabletop exercises,
          functional exercises, or full-scale drills to test your emergency response plans.
        </Typography>
        <CobraPrimaryButton
          startIcon={<FontAwesomeIcon icon={faPlus} />}
          onClick={onCreateClick}
          size="large"
        >
          Create Exercise
        </CobraPrimaryButton>
      </Paper>
    )
  }

  // Viewer with no exercises assigned
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
        <FontAwesomeIcon icon={faClipboardList} />
      </Box>
      <Typography variant="h6" gutterBottom>
        No Exercises Assigned
      </Typography>
      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ maxWidth: 350, mx: 'auto' }}
      >
        You haven't been assigned to any exercises yet. Contact your Exercise Director
        to get added to an upcoming exercise.
      </Typography>
    </Paper>
  )
}

export default ExerciseListPage
