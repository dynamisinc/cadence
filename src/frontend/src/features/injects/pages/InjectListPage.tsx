import { useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
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
  Paper,
  Skeleton,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faArrowLeft,
  faMagnifyingGlass,
  faListCheck,
  faPlay,
  faForwardStep,
  faHome,
} from '@fortawesome/free-solid-svg-icons'

import { useInjects } from '../hooks'
import { useBreadcrumbs } from '../../../core/contexts'
import { useExercise } from '../../exercises/hooks/useExercise'
import { usePhases } from '../../phases/hooks'
import { PhaseHeader, PhaseFormDialog } from '../../phases/components'
import { InjectStatusChip, InjectTypeChip } from '../components'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { usePermissions } from '../../../shared/hooks'
import { InjectStatus } from '../../../types'
import type { InjectDto } from '../types'
import type { PhaseDto } from '../../phases/types'
import { formatScenarioTime, formatScheduledTime } from '../types'

/**
 * MSEL (Inject List) Page
 *
 * Displays all injects for an exercise with:
 * - Grouping by phase
 * - Status chips (Pending, Fired, Skipped)
 * - Fire/Skip actions for Controllers
 * - Search/filter capability
 * - Create button for Controllers/Directors
 */
export const InjectListPage = () => {
  const navigate = useNavigate()
  const { exerciseId } = useParams<{ exerciseId: string }>()
  const { exercise, loading: exerciseLoading } = useExercise(exerciseId || '')
  const { injects, groupedByPhase, loading, error, fireInject, skipInject, isFiring, isSkipping } =
    useInjects(exerciseId || '')
  const {
    phases,
    createPhase,
    updatePhase,
    deletePhase,
    movePhaseUp,
    movePhaseDown,
    isCreating: isCreatingPhase,
    isUpdating: isUpdatingPhase,
    isDeleting: isDeletingPhase,
    isReordering: isReorderingPhase,
  } = usePhases(exerciseId || '')
  const { canFireInjects, canManage } = usePermissions()

  // Set custom breadcrumbs with exercise name and MSEL
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'MSEL' },
      ]
      : undefined,
  )

  const [searchTerm, setSearchTerm] = useState('')
  const [skipDialogOpen, setSkipDialogOpen] = useState(false)
  const [skipInjectId, setSkipInjectId] = useState<string | null>(null)
  const [skipReason, setSkipReason] = useState('')

  // Phase form dialog state
  const [phaseFormOpen, setPhaseFormOpen] = useState(false)
  const [editingPhase, setEditingPhase] = useState<PhaseDto | null>(null)

  const isPhaseLoading = isCreatingPhase || isUpdatingPhase || isDeletingPhase || isReorderingPhase

  // Filter injects by search term
  const filteredGroups = useMemo(() => {
    if (!searchTerm) return groupedByPhase

    const search = searchTerm.toLowerCase()
    return groupedByPhase
      .map((group) => ({
        ...group,
        injects: group.injects.filter(
          (inject) =>
            inject.title.toLowerCase().includes(search) ||
            inject.description.toLowerCase().includes(search) ||
            inject.injectNumber.toString().includes(search),
        ),
      }))
      .filter((group) => group.injects.length > 0)
  }, [groupedByPhase, searchTerm])

  const handleRowClick = (injectId: string) => {
    navigate(`/exercises/${exerciseId}/injects/${injectId}`)
  }

  const handleCreateClick = () => {
    navigate(`/exercises/${exerciseId}/injects/new`)
  }

  const handleBackClick = () => {
    navigate(`/exercises/${exerciseId}`)
  }

  const handleFireClick = async (
    e: React.MouseEvent,
    injectId: string,
  ) => {
    e.stopPropagation()
    await fireInject(injectId)
  }

  const handleSkipClick = (e: React.MouseEvent, injectId: string) => {
    e.stopPropagation()
    setSkipInjectId(injectId)
    setSkipReason('')
    setSkipDialogOpen(true)
  }

  const handleSkipConfirm = async () => {
    if (skipInjectId && skipReason.trim()) {
      await skipInject(skipInjectId, { reason: skipReason.trim() })
      setSkipDialogOpen(false)
      setSkipInjectId(null)
      setSkipReason('')
    }
  }

  const handleSkipCancel = () => {
    setSkipDialogOpen(false)
    setSkipInjectId(null)
    setSkipReason('')
  }

  // Phase management handlers
  const handleAddPhaseClick = () => {
    setEditingPhase(null)
    setPhaseFormOpen(true)
  }

  const handleEditPhase = (phase: PhaseDto) => {
    setEditingPhase(phase)
    setPhaseFormOpen(true)
  }

  const handleDeletePhase = async (phase: PhaseDto) => {
    await deletePhase(phase.id)
  }

  const handlePhaseFormClose = () => {
    setPhaseFormOpen(false)
    setEditingPhase(null)
  }

  const handlePhaseFormSave = async (values: { name: string; description: string }) => {
    if (editingPhase) {
      await updatePhase(editingPhase.id, {
        name: values.name,
        description: values.description || null,
      })
    } else {
      await createPhase({
        name: values.name,
        description: values.description || null,
      })
    }
    handlePhaseFormClose()
  }

  // Get phase data for rendering with inject counts
  const getPhaseForGroup = (phaseId: string | null): PhaseDto | null => {
    if (!phaseId) return null
    return phases.find((p) => p.id === phaseId) || null
  }

  // Error state
  if (error && injects.length === 0) {
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

  const isLoading = loading || exerciseLoading

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        marginBottom={1}
      >
        <Stack direction="row" alignItems="center" spacing={1}>
          <IconButton onClick={handleBackClick} size="small">
            <FontAwesomeIcon icon={faArrowLeft} />
          </IconButton>
          <Typography variant="h5" component="h1">
            MSEL
          </Typography>
        </Stack>

        {(canFireInjects || canManage) && (
          <Stack direction="row" spacing={1}>
            <CobraSecondaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={handleAddPhaseClick}
            >
              Add Phase
            </CobraSecondaryButton>
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={handleCreateClick}
            >
              New Inject
            </CobraPrimaryButton>
          </Stack>
        )}
      </Stack>

      {/* Exercise name subtitle */}
      <Typography variant="body2" color="text.secondary" marginBottom={3}>
        {exerciseLoading ? (
          <Skeleton width={200} />
        ) : (
          exercise?.name || 'Exercise'
        )}
      </Typography>

      {/* Search */}
      <Stack direction="row" spacing={2} marginBottom={2} alignItems="center">
        <CobraTextField
          placeholder="Search injects..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          sx={{ width: 300 }}
        />
        <Typography variant="body2" color="text.secondary">
          {injects.length} inject{injects.length !== 1 ? 's' : ''}
        </Typography>
      </Stack>

      {/* Loading skeleton state */}
      {isLoading && injects.length === 0 ? (
        <InjectTableSkeleton />
      ) : filteredGroups.length === 0 ? (
        <EmptyState
          hasInjects={injects.length > 0}
          canCreate={canFireInjects || canManage}
          onCreateClick={handleCreateClick}
        />
      ) : (
        <Stack spacing={3}>
          {filteredGroups.map((group, index) => {
            const phase = getPhaseForGroup(group.phaseId)
            const isFirst = index === 0
            const isLast = index === filteredGroups.length - 1

            return (
              <Box key={group.phaseId ?? 'unassigned'}>
                {/* Phase header */}
                {phase ? (
                  <Box marginBottom={1}>
                    <PhaseHeader
                      phase={phase}
                      isFirst={isFirst}
                      isLast={isLast}
                      canEdit={canFireInjects || canManage}
                      onEdit={handleEditPhase}
                      onDelete={handleDeletePhase}
                      onMoveUp={movePhaseUp}
                      onMoveDown={movePhaseDown}
                      isLoading={isPhaseLoading}
                    />
                  </Box>
                ) : (
                  <Typography
                    variant="subtitle1"
                    fontWeight={600}
                    sx={{
                      backgroundColor: 'grey.100',
                      px: 2,
                      py: 1,
                      borderRadius: 1,
                      mb: 1,
                    }}
                  >
                    Unassigned Phase
                  </Typography>
                )}

                <TableContainer component={Paper}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell width={60}>#</TableCell>
                      <TableCell width={100}>Scheduled</TableCell>
                      <TableCell width={100}>Scenario</TableCell>
                      <TableCell>Title</TableCell>
                      <TableCell width={80}>Type</TableCell>
                      <TableCell width={90}>Status</TableCell>
                      {canFireInjects && <TableCell width={100}>Actions</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {group.injects.map((inject) => (
                      <InjectRow
                        key={inject.id}
                        inject={inject}
                        onClick={() => handleRowClick(inject.id)}
                        canFireInjects={canFireInjects}
                        onFire={(e) => handleFireClick(e, inject.id)}
                        onSkip={(e) => handleSkipClick(e, inject.id)}
                        isFiring={isFiring}
                        isSkipping={isSkipping}
                      />
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
              </Box>
            )
          })}
        </Stack>
      )}

      {/* Skip Reason Dialog */}
      <Dialog open={skipDialogOpen} onClose={handleSkipCancel} maxWidth="sm" fullWidth>
        <DialogTitle>Skip Inject</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" marginBottom={2}>
            Please provide a reason for skipping this inject. This will be recorded
            for the after-action report.
          </Typography>
          <CobraTextField
            label="Skip Reason"
            value={skipReason}
            onChange={(e) => setSkipReason(e.target.value)}
            multiline
            rows={3}
            fullWidth
            required
            placeholder="e.g., Time constraints, players ahead of schedule, etc."
          />
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={handleSkipCancel}>
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={handleSkipConfirm}
            disabled={!skipReason.trim() || isSkipping}
          >
            Skip Inject
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>

      {/* Phase Form Dialog */}
      <PhaseFormDialog
        open={phaseFormOpen}
        phase={editingPhase}
        onClose={handlePhaseFormClose}
        onSubmit={handlePhaseFormSave}
        isSubmitting={isCreatingPhase || isUpdatingPhase}
      />
    </Box>
  )
}

/**
 * Loading skeleton for the inject table
 */
const InjectTableSkeleton = () => {
  const skeletonRows = Array.from({ length: 5 }, (_, i) => i)

  return (
    <TableContainer component={Paper}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>#</TableCell>
            <TableCell>Scheduled</TableCell>
            <TableCell>Scenario</TableCell>
            <TableCell>Title</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {skeletonRows.map((index) => (
            <TableRow key={index}>
              <TableCell>
                <Skeleton variant="text" width={30} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={70} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={70} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={200} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={60} height={24} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={70} height={24} />
              </TableCell>
              <TableCell>
                <Skeleton variant="circular" width={32} height={32} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

interface InjectRowProps {
  inject: InjectDto
  onClick: () => void
  canFireInjects: boolean
  onFire: (e: React.MouseEvent) => void
  onSkip: (e: React.MouseEvent) => void
  isFiring: boolean
  isSkipping: boolean
}

const InjectRow = ({
  inject,
  onClick,
  canFireInjects,
  onFire,
  onSkip,
  isFiring,
  isSkipping,
}: InjectRowProps) => {
  const scenarioTimeDisplay = formatScenarioTime(
    inject.scenarioDay,
    inject.scenarioTime,
  )
  const scheduledTimeDisplay = formatScheduledTime(inject.scheduledTime)
  const isPending = inject.status === InjectStatus.Pending

  return (
    <TableRow
      hover
      onClick={onClick}
      sx={{
        cursor: 'pointer',
        '& td': { minHeight: 44 },
        // Dim fired/skipped rows slightly
        opacity: isPending ? 1 : 0.85,
      }}
    >
      <TableCell>
        <Typography variant="body2" fontWeight={500}>
          {inject.injectNumber}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography variant="body2">{scheduledTimeDisplay}</Typography>
      </TableCell>
      <TableCell>
        <Typography variant="body2" color={scenarioTimeDisplay ? 'text.primary' : 'text.secondary'}>
          {scenarioTimeDisplay ?? '—'}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography
          variant="body2"
          sx={{
            maxWidth: 300,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {inject.title}
        </Typography>
      </TableCell>
      <TableCell>
        <InjectTypeChip type={inject.injectType} />
      </TableCell>
      <TableCell>
        <InjectStatusChip status={inject.status} />
      </TableCell>
      {canFireInjects && (
        <TableCell>
          {isPending && (
            <Stack direction="row" spacing={0.5}>
              <Tooltip title="Fire inject">
                <IconButton
                  size="small"
                  color="success"
                  onClick={onFire}
                  disabled={isFiring || isSkipping}
                >
                  <FontAwesomeIcon icon={faPlay} size="sm" />
                </IconButton>
              </Tooltip>
              <Tooltip title="Skip inject">
                <IconButton
                  size="small"
                  color="warning"
                  onClick={onSkip}
                  disabled={isFiring || isSkipping}
                >
                  <FontAwesomeIcon icon={faForwardStep} size="sm" />
                </IconButton>
              </Tooltip>
            </Stack>
          )}
        </TableCell>
      )}
    </TableRow>
  )
}

interface EmptyStateProps {
  hasInjects: boolean
  canCreate: boolean
  onCreateClick: () => void
}

const EmptyState = ({ hasInjects, canCreate, onCreateClick }: EmptyStateProps) => {
  if (hasInjects) {
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
          }}
        >
          <FontAwesomeIcon icon={faMagnifyingGlass} style={{ fontSize: 40, color: '#9e9e9e' }} />
        </Box>
        <Typography variant="h6" gutterBottom>
          No matching injects
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ maxWidth: 300, mx: 'auto' }}
        >
          Try adjusting your search terms to find the inject you're looking for.
        </Typography>
      </Paper>
    )
  }

  // No injects at all
  if (canCreate) {
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
          }}
        >
          <FontAwesomeIcon icon={faListCheck} style={{ fontSize: 50, color: '#1976d2' }} />
        </Box>
        <Typography variant="h5" gutterBottom fontWeight={500}>
          Create Your First Inject
        </Typography>
        <Typography
          variant="body1"
          color="text.secondary"
          sx={{ maxWidth: 400, mx: 'auto', mb: 3 }}
        >
          Build out your MSEL by adding injects. Each inject represents an event,
          message, or action that will be delivered during exercise conduct.
        </Typography>
        <CobraPrimaryButton
          startIcon={<FontAwesomeIcon icon={faPlus} />}
          onClick={onCreateClick}
          size="large"
        >
          New Inject
        </CobraPrimaryButton>
      </Paper>
    )
  }

  // Viewer with no injects
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
        }}
      >
        <FontAwesomeIcon icon={faListCheck} style={{ fontSize: 40, color: '#9e9e9e' }} />
      </Box>
      <Typography variant="h6" gutterBottom>
        No Injects Yet
      </Typography>
      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ maxWidth: 350, mx: 'auto' }}
      >
        The MSEL for this exercise is empty. Controllers will add injects during
        exercise planning.
      </Typography>
    </Paper>
  )
}

export default InjectListPage
