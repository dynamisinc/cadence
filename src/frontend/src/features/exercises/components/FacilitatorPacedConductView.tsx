/**
 * FacilitatorPacedConductView
 *
 * Conduct view for facilitator-paced delivery mode exercises.
 * Focuses on current inject with manual progression through the MSEL.
 * No elapsed time clock - facilitator controls the pace.
 *
 * @module features/exercises
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import { useMemo, useState } from 'react'
import { Box, Typography, Paper, Stack, Skeleton } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBook, faCheck } from '@fortawesome/free-solid-svg-icons'

import type { ExerciseDto } from '../types'
import type { InjectDto, SkipInjectRequest } from '../../injects/types'
import {
  getCurrentInject,
  getInjectsToSkip,
} from '../../injects/utils/facilitatorGrouping'
import {
  CurrentInjectPanel,
  UpNextList,
  JumpConfirmationDialog,
} from '../../injects/components/facilitator-paced'
import { InjectDetailDrawer } from '../../injects/components'
import { CompletedSection } from './clock-driven-sections'
import { InjectStatus } from '../../../types'

interface FacilitatorPacedConductViewProps {
  /** The exercise being conducted */
  exercise: ExerciseDto
  /** All injects in the exercise */
  injects: InjectDto[]
  /** Called when Controller fires an inject */
  onFire: (injectId: string) => Promise<void> | void
  /** Called when Controller skips an inject */
  onSkip: (injectId: string, request: SkipInjectRequest) => Promise<void> | void
  /** Called when Controller jumps to a later inject */
  onJumpTo: (targetInjectId: string, skipInjectIds: string[]) => Promise<void> | void
  /** Whether the current user can control injects */
  canControl?: boolean
  /** Whether actions are currently being submitted */
  isSubmitting?: boolean
  /** Whether injects are loading */
  isLoading?: boolean
}

export const FacilitatorPacedConductView = ({
  exercise: _exercise,
  injects,
  onFire,
  onSkip,
  onJumpTo,
  canControl = true,
  isSubmitting = false,
  isLoading = false,
}: FacilitatorPacedConductViewProps) => {
  // Get current and upcoming injects
  const currentInject = useMemo(() => getCurrentInject(injects), [injects])

  // Get all remaining pending injects (excluding current)
  const upNextInjects = useMemo(
    () =>
      injects
        .filter(i => i.status === InjectStatus.Pending)
        .filter(i => i.sequence > (currentInject?.sequence || 0))
        .sort((a, b) => a.sequence - b.sequence),
    [injects, currentInject],
  )

  // Get completed injects (fired or skipped)
  const completedInjects = useMemo(
    () =>
      injects.filter(
        i => i.status === InjectStatus.Fired || i.status === InjectStatus.Skipped,
      ),
    [injects],
  )

  // Jump confirmation state
  const [jumpTarget, setJumpTarget] = useState<InjectDto | null>(null)
  const [completedExpanded, setCompletedExpanded] = useState(false)

  // Drawer state for inject details
  const [selectedInject, setSelectedInject] = useState<InjectDto | null>(null)

  // Handle fire current inject
  const handleFireAndContinue = async () => {
    if (currentInject) {
      await onFire(currentInject.id)
    }
  }

  // Handle skip current inject
  const handleSkip = async () => {
    if (currentInject) {
      const reason = prompt('Reason for skipping (optional):')
      if (reason !== null) {
        // User didn't cancel
        await onSkip(currentInject.id, { reason: reason || 'Skipped by facilitator' })
      }
    }
  }

  // Handle jump confirmation
  const handleJumpConfirm = async () => {
    if (!jumpTarget || !currentInject) return

    // Find all injects between current and target
    const skipIds = getInjectsToSkip(injects, currentInject.sequence, jumpTarget.sequence).map(
      i => i.id,
    )

    await onJumpTo(jumpTarget.id, skipIds)
    setJumpTarget(null)
  }

  // Calculate injects to skip for jump confirmation
  const injectsToSkip = useMemo(() => {
    if (!jumpTarget || !currentInject) return []
    return getInjectsToSkip(injects, currentInject.sequence, jumpTarget.sequence)
  }, [jumpTarget, currentInject, injects])

  // Show loading skeleton while injects are being fetched
  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
        {/* Header skeleton */}
        <Paper variant="outlined" sx={{ p: 2, backgroundColor: 'grey.50' }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Skeleton variant="text" width={200} height={32} />
            <Skeleton variant="text" width={120} height={24} />
          </Stack>
        </Paper>

        {/* Current inject panel skeleton */}
        <Paper variant="outlined" sx={{ p: 3 }}>
          <Stack spacing={2}>
            <Skeleton variant="text" width={150} height={28} />
            <Skeleton variant="text" width="60%" height={40} />
            <Skeleton variant="rectangular" height={100} />
            <Stack direction="row" spacing={2} justifyContent="center">
              <Skeleton variant="rectangular" width={100} height={40} />
              <Skeleton variant="rectangular" width={160} height={40} />
            </Stack>
          </Stack>
        </Paper>

        {/* Up next section skeleton */}
        <Paper variant="outlined">
          <Box sx={{ p: 2, backgroundColor: 'grey.50' }}>
            <Skeleton variant="text" width={100} height={28} />
          </Box>
          <Box sx={{ p: 2 }}>
            <Stack spacing={2}>
              <Skeleton variant="rectangular" height={80} />
              <Skeleton variant="rectangular" height={80} />
            </Stack>
          </Box>
        </Paper>
      </Box>
    )
  }

  // Show completion message only when all injects are actually complete
  // (not when injects array is empty due to loading)
  if (!currentInject && injects.length > 0) {
    return (
      <Paper
        sx={{
          p: 6,
          textAlign: 'center',
          backgroundColor: 'success.50',
          border: '2px solid',
          borderColor: 'success.main',
        }}
      >
        <Box sx={{ mb: 2, color: 'success.main' }}>
          <FontAwesomeIcon icon={faCheck} size="3x" />
        </Box>
        <Typography variant="h4" gutterBottom fontWeight={600}>
          Exercise Complete
        </Typography>
        <Typography variant="body1" color="text.secondary">
          All injects have been delivered. The exercise can now be concluded.
        </Typography>
      </Paper>
    )
  }

  // Show empty state if no injects at all
  if (!currentInject && injects.length === 0) {
    return (
      <Paper
        sx={{
          p: 6,
          textAlign: 'center',
          backgroundColor: 'grey.50',
        }}
      >
        <Box sx={{ mb: 2, color: 'text.secondary' }}>
          <FontAwesomeIcon icon={faBook} size="3x" />
        </Box>
        <Typography variant="h5" gutterBottom fontWeight={600}>
          No Injects
        </Typography>
        <Typography variant="body1" color="text.secondary">
          This exercise has no injects. Add injects to the MSEL to begin.
        </Typography>
      </Paper>
    )
  }

  // At this point, currentInject is guaranteed non-null (all null cases handled above)
  if (!currentInject) {
    return null
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      {/* Header - No Clock, Just Progress */}
      <Paper
        variant="outlined"
        sx={{
          p: 2,
          backgroundColor: 'grey.50',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}
      >
        <Stack direction="row" spacing={2} alignItems="center">
          <Box sx={{ color: 'primary.main' }}>
            <FontAwesomeIcon icon={faBook} size="lg" />
          </Box>
          <Typography variant="h6" fontWeight={600}>
            Facilitator-Paced Mode
          </Typography>
        </Stack>
        <Typography variant="body1" color="text.secondary">
          Progress: {completedInjects.length + 1} of {injects.length} injects
        </Typography>
      </Paper>

      {/* Current Inject Panel */}
      <CurrentInjectPanel
        inject={currentInject}
        onFire={handleFireAndContinue}
        onSkip={handleSkip}
        canControl={canControl}
        isSubmitting={isSubmitting}
      />

      {/* Up Next Section */}
      <UpNextList
        injects={upNextInjects}
        onJumpTo={setJumpTarget}
        onInjectClick={setSelectedInject}
        canControl={canControl}
        isSubmitting={isSubmitting}
      />

      {/* Completed Section */}
      <CompletedSection
        injects={completedInjects}
        expanded={completedExpanded}
        onToggle={() => setCompletedExpanded(!completedExpanded)}
        onInjectClick={setSelectedInject}
      />

      {/* Jump Confirmation Dialog */}
      <JumpConfirmationDialog
        open={!!jumpTarget}
        targetInject={jumpTarget}
        skippedInjects={injectsToSkip}
        onConfirm={handleJumpConfirm}
        onCancel={() => setJumpTarget(null)}
      />

      {/* Inject Detail Drawer */}
      <InjectDetailDrawer
        inject={selectedInject}
        open={!!selectedInject}
        onClose={() => setSelectedInject(null)}
        canControl={canControl}
        isSubmitting={isSubmitting}
        onFire={id => {
          onFire(id)
          setSelectedInject(null)
        }}
        onSkip={id => {
          const reason = prompt('Reason for skipping (optional):')
          if (reason !== null) {
            onSkip(id, { reason: reason || 'Skipped by facilitator' })
            setSelectedInject(null)
          }
        }}
      />
    </Box>
  )
}

export default FacilitatorPacedConductView
