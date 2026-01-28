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
import {
  Box,
  Typography,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'

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
  /** Called when Controller resets an inject to pending */
  onReset?: (injectId: string) => Promise<void> | void
  /** Whether the current user can control injects */
  canControl?: boolean
  /** Whether the current user can add observations (Evaluators) */
  canAddObservation?: boolean
  /** Whether actions are currently being submitted */
  isSubmitting?: boolean
  /** ID of inject to show in drawer (controlled externally) */
  openInjectId?: string | null
  /** Called when drawer is closed */
  onDrawerClose?: () => void
  /** Called when user wants to add observation for an inject */
  onAddObservation?: (injectId: string) => void
  /**
   * Pre-confirmation callback for skip.
   * Returns true if confirmation dialog is being shown (reason dialog should wait).
   * Returns false if no confirmation needed (proceed to reason dialog immediately).
   */
  onSkipPreConfirmation?: (injectId: string) => boolean | null
  /** When set, opens the skip reason dialog for this inject (after pre-confirmation) */
  pendingSkipInjectId?: string | null
  /** Called when pending skip is cleared (dialog closed without completing) */
  onPendingSkipClear?: () => void
}

export const ClockDrivenConductView = ({
  exercise: _exercise,
  injects,
  elapsedTimeMs,
  onFire,
  onSkip,
  onReset,
  canControl = true,
  canAddObservation = false,
  isSubmitting = false,
  openInjectId,
  onDrawerClose,
  onAddObservation,
  onSkipPreConfirmation,
  pendingSkipInjectId,
  onPendingSkipClear,
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

  // Skip reason dialog state (for skipping from drawer)
  const [skipDialogOpen, setSkipDialogOpen] = useState(false)
  const [skipInjectId, setSkipInjectId] = useState<string | null>(null)
  const [skipReason, setSkipReason] = useState('')

  // Handle pending skip from parent (after pre-confirmation)
  useEffect(() => {
    if (pendingSkipInjectId) {
      setSkipInjectId(pendingSkipInjectId)
      setSkipReason('')
      setSkipDialogOpen(true)
    }
  }, [pendingSkipInjectId])

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

  // Handle skip button click - checks pre-confirmation first
  const handleSkipClick = (injectId: string) => {
    // Check if pre-confirmation is needed
    if (onSkipPreConfirmation) {
      const needsConfirmation = onSkipPreConfirmation(injectId)
      if (needsConfirmation) {
        // Parent will show pre-confirmation dialog first
        // When confirmed, pendingSkipInjectId will be set to trigger reason dialog
        return
      }
    }
    // No pre-confirmation needed, open reason dialog directly
    setSkipInjectId(injectId)
    setSkipReason('')
    setSkipDialogOpen(true)
  }

  // Handle skip confirmation (after reason is entered)
  const handleSkipConfirm = async () => {
    if (skipInjectId && skipReason.trim()) {
      await onSkip(skipInjectId, { reason: skipReason.trim() })
      setSkipDialogOpen(false)
      setSkipInjectId(null)
      setSkipReason('')
      onPendingSkipClear?.()
    }
  }

  // Handle skip cancel
  const handleSkipCancel = () => {
    setSkipDialogOpen(false)
    setSkipInjectId(null)
    setSkipReason('')
    onPendingSkipClear?.()
  }

  const handleDrawerSkip = (injectId: string) => {
    // Close drawer first, then trigger skip flow
    setDrawerOpen(false)
    onDrawerClose?.()
    handleSkipClick(injectId)
  }

  const handleDrawerReset = async (injectId: string) => {
    if (onReset) {
      await onReset(injectId)
    }
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
          onSkipPreConfirmation={onSkipPreConfirmation}
          pendingSkipInjectId={pendingSkipInjectId}
          onPendingSkipClear={onPendingSkipClear}
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
        canAddObservation={canAddObservation}
        isSubmitting={isSubmitting}
        onFire={handleDrawerFire}
        onSkip={handleDrawerSkip}
        onReset={handleDrawerReset}
        onAddObservation={onAddObservation}
      />

      {/* Skip Reason Dialog */}
      <Dialog open={skipDialogOpen} onClose={handleSkipCancel} maxWidth="sm" fullWidth>
        <DialogTitle>Skip Inject</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Please provide a reason for skipping this inject. This will be recorded for the
            after-action report.
          </Typography>
          <CobraTextField
            label="Skip Reason"
            value={skipReason}
            onChange={e => setSkipReason(e.target.value)}
            multiline
            rows={3}
            fullWidth
            required
            placeholder="e.g., Time constraints, players ahead of schedule, etc."
          />
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={handleSkipCancel} disabled={isSubmitting}>
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={handleSkipConfirm}
            disabled={!skipReason.trim() || isSubmitting}
          >
            Skip Inject
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>
    </>
  )
}

export default ClockDrivenConductView
