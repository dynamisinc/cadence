/**
 * ExerciseProgress Component
 *
 * Displays the current phase name and inject completion progress
 * during exercise conduct. Updates in real-time as injects are fired.
 */

import { useMemo } from 'react'
import { Box, Typography, LinearProgress, Stack } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faLayerGroup } from '@fortawesome/free-solid-svg-icons'

import { InjectStatus } from '../../../types'
import type { InjectDto } from '../../injects/types'

interface ExerciseProgressProps {
  /** List of injects to calculate progress from */
  injects: InjectDto[]
}

/**
 * Calculate the current phase name based on inject status.
 * Priority:
 * 1. Phase of most recently released inject
 * 2. Phase of first draft inject (by sequence)
 * 3. "No phase assigned" if no phases
 */
const getCurrentPhaseName = (injects: InjectDto[]): string | null => {
  if (injects.length === 0) return null

  // Find the most recently released inject
  const firedInjects = injects
    .filter(i => i.status === InjectStatus.Released && i.firedAt)
    .sort((a, b) => new Date(b.firedAt!).getTime() - new Date(a.firedAt!).getTime())

  if (firedInjects.length > 0 && firedInjects[0].phaseName) {
    return firedInjects[0].phaseName
  }

  // Fall back to first draft inject's phase
  const pendingInjects = injects
    .filter(i => i.status === InjectStatus.Draft)
    .sort((a, b) => a.sequence - b.sequence)

  if (pendingInjects.length > 0 && pendingInjects[0].phaseName) {
    return pendingInjects[0].phaseName
  }

  // Check if any inject has a phase assigned
  const anyPhase = injects.find(i => i.phaseName)
  if (anyPhase) {
    return anyPhase.phaseName
  }

  return null
}

export const ExerciseProgress = ({ injects }: ExerciseProgressProps) => {
  // Calculate progress: (released + deferred) / total
  const { completed, total, percentage } = useMemo(() => {
    const total = injects.length
    const completed = injects.filter(
      i => i.status === InjectStatus.Released || i.status === InjectStatus.Deferred,
    ).length
    const percentage = total > 0 ? Math.round((completed / total) * 100) : 0

    return { completed, total, percentage }
  }, [injects])

  // Get current phase name
  const currentPhaseName = useMemo(() => getCurrentPhaseName(injects), [injects])

  return (
    <Box sx={{ width: '100%', mt: 2 }}>
      {/* Phase Name */}
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
        <FontAwesomeIcon icon={faLayerGroup} style={{ opacity: 0.7 }} />
        <Typography variant="body2" color="text.secondary">
          {currentPhaseName ? `Phase: ${currentPhaseName}` : 'No phase assigned'}
        </Typography>
      </Stack>

      {/* Progress Bar */}
      <LinearProgress
        variant="determinate"
        value={percentage}
        sx={{
          height: 8,
          borderRadius: 4,
          backgroundColor: 'grey.200',
          '& .MuiLinearProgress-bar': {
            borderRadius: 4,
            backgroundColor: percentage === 100 ? 'success.main' : 'primary.main',
          },
        }}
      />

      {/* Progress Text */}
      <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
        {completed} of {total} injects released
      </Typography>
    </Box>
  )
}

export default ExerciseProgress
