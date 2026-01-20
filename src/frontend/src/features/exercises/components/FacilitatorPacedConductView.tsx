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
import { Box, Typography, Paper, Stack } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBook, faCheck } from '@fortawesome/free-solid-svg-icons'

import type { ExerciseDto } from '../types'
import type { InjectDto, SkipInjectRequest } from '../../injects/types'
import {
  getCurrentInject,
  getUpNextInjects,
  getInjectsToSkip,
} from '../../injects/utils/facilitatorGrouping'
import {
  CurrentInjectPanel,
  UpNextList,
  JumpConfirmationDialog,
} from '../../injects/components/facilitator-paced'
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
}

export const FacilitatorPacedConductView = ({
  exercise: _exercise,
  injects,
  onFire,
  onSkip,
  onJumpTo,
  canControl = true,
  isSubmitting = false,
}: FacilitatorPacedConductViewProps) => {
  // Get current and upcoming injects
  const currentInject = useMemo(() => getCurrentInject(injects), [injects])
  const upNextInjects = useMemo(
    () => getUpNextInjects(injects, currentInject?.sequence || 0),
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

  // Show completion message if no current inject
  if (!currentInject) {
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
        canControl={canControl}
        isSubmitting={isSubmitting}
      />

      {/* Completed Section */}
      <CompletedSection
        injects={completedInjects}
        expanded={completedExpanded}
        onToggle={() => setCompletedExpanded(!completedExpanded)}
      />

      {/* Jump Confirmation Dialog */}
      <JumpConfirmationDialog
        open={!!jumpTarget}
        targetInject={jumpTarget}
        skippedInjects={injectsToSkip}
        onConfirm={handleJumpConfirm}
        onCancel={() => setJumpTarget(null)}
      />
    </Box>
  )
}

export default FacilitatorPacedConductView
