/**
 * ClockDrivenConductView
 *
 * Conduct view for clock-driven delivery mode exercises.
 * Displays injects grouped into Ready to Fire, Upcoming, and Completed sections.
 *
 * @module features/exercises
 * @see exercise-config/S06-clock-driven-conduct-view
 */

import { useMemo, useState } from 'react'
import { Box, Typography } from '@mui/material'

import type { ExerciseDto } from '../types'
import type { InjectDto, SkipInjectRequest } from '../../injects/types'
import { groupInjectsForClockDriven } from '../../injects/utils/clockDrivenGrouping'
import {
  ReadyToFireSection,
  UpcomingSection,
  CompletedSection,
} from './clock-driven-sections'

interface ClockDrivenConductViewProps {
  /** The exercise being conducted */
  exercise: ExerciseDto
  /** All injects in the exercise */
  injects: InjectDto[]
  /** Current elapsed time in milliseconds */
  elapsedTimeMs: number
  /** Called when Controller fires an inject */
  onFire: (injectId: string) => Promise<void> | void
  /** Called when Controller skips an inject */
  onSkip: (injectId: string, request: SkipInjectRequest) => Promise<void> | void
  /** Whether the current user can control injects */
  canControl?: boolean
  /** Whether actions are currently being submitted */
  isSubmitting?: boolean
}

export const ClockDrivenConductView = ({
  exercise: _exercise,
  injects,
  elapsedTimeMs,
  onFire,
  onSkip,
  canControl = true,
  isSubmitting = false,
}: ClockDrivenConductViewProps) => {
  // Group injects by section
  const grouped = useMemo(
    () => groupInjectsForClockDriven(injects, elapsedTimeMs),
    [injects, elapsedTimeMs],
  )

  // Completed section collapse state
  const [completedExpanded, setCompletedExpanded] = useState(false)

  // Show alert if no injects at all
  if (injects.length === 0) {
    return (
      <Box sx={{ py: 4, textAlign: 'center' }}>
        <Typography variant="body2" color="text.secondary">
          No injects in this exercise's MSEL.
        </Typography>
      </Box>
    )
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      {/* Ready to Fire Section */}
      <ReadyToFireSection
        injects={grouped.ready}
        elapsedTimeMs={elapsedTimeMs}
        canControl={canControl}
        isSubmitting={isSubmitting}
        onFire={onFire}
        onSkip={onSkip}
      />

      {/* Upcoming Section */}
      <UpcomingSection
        injects={grouped.upcoming}
        elapsedTimeMs={elapsedTimeMs}
      />

      {/* Completed Section */}
      <CompletedSection
        injects={grouped.completed}
        expanded={completedExpanded}
        onToggle={() => setCompletedExpanded(!completedExpanded)}
      />
    </Box>
  )
}

export default ClockDrivenConductView
