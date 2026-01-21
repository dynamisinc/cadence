/**
 * ClockDrivenConductView
 *
 * Conduct view for clock-driven delivery mode exercises.
 * Displays injects grouped into Ready to Fire, Upcoming, Later, and Completed sections.
 *
 * @module features/exercises
 * @see exercise-config/S06-clock-driven-conduct-view
 */

import { useMemo, useState, useEffect } from 'react'
import { Box, Typography } from '@mui/material'

import type { ExerciseDto } from '../types'
import type { InjectDto, SkipInjectRequest } from '../../injects/types'
import { parseDeliveryTime } from '../../injects/types'
import { groupInjectsForClockDriven } from '../../injects/utils/clockDrivenGrouping'
import { InjectDetailDrawer } from '../../injects/components/InjectDetailDrawer'
import {
  ReadyToFireSection,
  UpcomingSection,
  LaterSection,
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
  /** ID of inject to show in drawer (controlled externally) */
  openInjectId?: string | null
  /** Called when drawer is closed */
  onDrawerClose?: () => void
}

export const ClockDrivenConductView = ({
  exercise: _exercise,
  injects,
  elapsedTimeMs,
  onFire,
  onSkip,
  canControl = true,
  isSubmitting = false,
  openInjectId,
  onDrawerClose,
}: ClockDrivenConductViewProps) => {
  // Group injects by section
  const grouped = useMemo(
    () => groupInjectsForClockDriven(injects, elapsedTimeMs),
    [injects, elapsedTimeMs],
  )

  // Completed section collapse state
  const [completedExpanded, setCompletedExpanded] = useState(false)

  // Drawer state
  const [selectedInject, setSelectedInject] = useState<InjectDto | null>(null)
  const [selectedInjectOffset, setSelectedInjectOffset] = useState<number | null>(null)
  const [drawerOpen, setDrawerOpen] = useState(false)

  // Open drawer when openInjectId is set externally
  useEffect(() => {
    if (openInjectId) {
      const inject = injects.find(i => i.id === openInjectId)
      if (inject) {
        const offsetMs = parseDeliveryTime(inject.deliveryTime)
        setSelectedInject(inject)
        setSelectedInjectOffset(offsetMs)
        setDrawerOpen(true)
      }
    }
  }, [openInjectId, injects])

  // Drawer handlers
  const handleInjectClick = (inject: InjectDto) => {
    const offsetMs = parseDeliveryTime(inject.deliveryTime)
    setSelectedInject(inject)
    setSelectedInjectOffset(offsetMs)
    setDrawerOpen(true)
  }

  const handleDrawerClose = () => {
    setDrawerOpen(false)
    onDrawerClose?.()
  }

  const handleDrawerFire = async (injectId: string) => {
    await onFire(injectId)
  }

  const handleDrawerSkip = (_injectId: string) => {
    // Close drawer first, then the section's skip dialog will handle the reason
    setDrawerOpen(false)
    onDrawerClose?.()
  }

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
    <>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        {/* Ready to Fire Section */}
        <ReadyToFireSection
          injects={grouped.ready}
          elapsedTimeMs={elapsedTimeMs}
          canControl={canControl}
          isSubmitting={isSubmitting}
          onFire={onFire}
          onSkip={onSkip}
          onInjectClick={handleInjectClick}
        />

        {/* Upcoming Section */}
        <UpcomingSection
          injects={grouped.upcoming}
          elapsedTimeMs={elapsedTimeMs}
          onInjectClick={handleInjectClick}
        />

        {/* Later Section */}
        <LaterSection injects={grouped.later} onInjectClick={handleInjectClick} />

        {/* Completed Section */}
        <CompletedSection
          injects={grouped.completed}
          expanded={completedExpanded}
          onToggle={() => setCompletedExpanded(!completedExpanded)}
          onInjectClick={handleInjectClick}
        />
      </Box>

      {/* Inject Detail Drawer */}
      <InjectDetailDrawer
        inject={selectedInject}
        offsetMs={selectedInjectOffset ?? undefined}
        open={drawerOpen}
        onClose={handleDrawerClose}
        canControl={canControl}
        isSubmitting={isSubmitting}
        onFire={handleDrawerFire}
        onSkip={handleDrawerSkip}
      />
    </>
  )
}

export default ClockDrivenConductView
