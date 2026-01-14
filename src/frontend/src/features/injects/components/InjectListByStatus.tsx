/**
 * InjectListByStatus Component
 *
 * Displays injects organized into time-based sections for exercise conduct.
 * Sections: Ready to Fire, Upcoming (30 min window), Later, Fired, Skipped.
 *
 * Injects automatically move between sections as the exercise clock progresses.
 */

import { useState, useMemo, useEffect } from 'react'
import {
  Box,
  Typography,
  Alert,
  Skeleton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'

import { InjectSection } from './InjectSection'
import { InjectRow } from './InjectRow'
import { InjectDetailDrawer } from './InjectDetailDrawer'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import { InjectStatus } from '../../../types'
import type { InjectDto, SkipInjectRequest } from '../types'
import { calculateScheduledOffset, UPCOMING_WINDOW_MS } from '../types'

interface InjectListByStatusProps {
  /** List of injects to display */
  injects: InjectDto[]
  /** Exercise's planned start time (HH:MM:SS), null defaults to 00:00:00 */
  exerciseStartTime: string | null
  /** Current elapsed time in milliseconds */
  elapsedTimeMs: number
  /** Can the user control injects (fire/skip/reset)? */
  canControl?: boolean
  /** Is data loading? */
  loading?: boolean
  /** Error message if failed to load */
  error?: string | null
  /** Called when fire button is clicked */
  onFire?: (injectId: string) => Promise<void> | Promise<InjectDto>
  /** Called when skip button is clicked */
  onSkip?: (injectId: string, request: SkipInjectRequest) => Promise<void> | Promise<InjectDto>
  /** Called when reset button is clicked */
  onReset?: (injectId: string) => Promise<void> | Promise<InjectDto>
  /** ID of inject to open in drawer (controlled externally) */
  openInjectId?: string | null
  /** Called when drawer is closed (to clear openInjectId) */
  onDrawerClose?: () => void
}

interface InjectWithOffset {
  inject: InjectDto
  offsetMs: number
}

export const InjectListByStatus = ({
  injects,
  exerciseStartTime,
  elapsedTimeMs,
  canControl = false,
  loading = false,
  error = null,
  onFire,
  onSkip,
  onReset,
  openInjectId,
  onDrawerClose,
}: InjectListByStatusProps) => {
  const [skipDialogOpen, setSkipDialogOpen] = useState(false)
  const [skipInjectId, setSkipInjectId] = useState<string | null>(null)
  const [skipReason, setSkipReason] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Drawer state for inject details
  const [selectedInject, setSelectedInject] = useState<InjectDto | null>(null)
  const [selectedInjectOffset, setSelectedInjectOffset] = useState<number>(0)
  const [drawerOpen, setDrawerOpen] = useState(false)

  // Calculate offsets for all injects and group by section
  const sections = useMemo(() => {
    const withOffsets: InjectWithOffset[] = injects.map(inject => ({
      inject,
      offsetMs: calculateScheduledOffset(inject.scheduledTime, exerciseStartTime),
    }))

    const readyToFire: InjectWithOffset[] = []
    const upcoming: InjectWithOffset[] = []
    const later: InjectWithOffset[] = []
    const fired: InjectWithOffset[] = []
    const skipped: InjectWithOffset[] = []

    const upcomingThreshold = elapsedTimeMs + UPCOMING_WINDOW_MS

    for (const item of withOffsets) {
      const { inject, offsetMs } = item

      switch (inject.status) {
        case InjectStatus.Fired:
          fired.push(item)
          break
        case InjectStatus.Skipped:
          skipped.push(item)
          break
        case InjectStatus.Pending:
        default:
          if (offsetMs <= elapsedTimeMs) {
            // Ready to fire (offset has passed)
            readyToFire.push(item)
          } else if (offsetMs <= upcomingThreshold) {
            // Upcoming (within 30 min window)
            upcoming.push(item)
          } else {
            // Later (beyond 30 min window)
            later.push(item)
          }
          break
      }
    }

    // Sort each section appropriately
    // Ready to Fire: oldest offset first (most overdue at top)
    readyToFire.sort((a, b) => a.offsetMs - b.offsetMs)
    // Upcoming: soonest first
    upcoming.sort((a, b) => a.offsetMs - b.offsetMs)
    // Later: offset ascending
    later.sort((a, b) => a.offsetMs - b.offsetMs)
    // Fired: most recently fired first
    fired.sort((a, b) => {
      const aTime = a.inject.firedAt ? new Date(a.inject.firedAt).getTime() : 0
      const bTime = b.inject.firedAt ? new Date(b.inject.firedAt).getTime() : 0
      return bTime - aTime
    })
    // Skipped: most recently skipped first
    skipped.sort((a, b) => {
      const aTime = a.inject.skippedAt ? new Date(a.inject.skippedAt).getTime() : 0
      const bTime = b.inject.skippedAt ? new Date(b.inject.skippedAt).getTime() : 0
      return bTime - aTime
    })

    return { readyToFire, upcoming, later, fired, skipped }
  }, [injects, exerciseStartTime, elapsedTimeMs])

  // Open drawer when openInjectId is set externally
  useEffect(() => {
    if (openInjectId) {
      const inject = injects.find(i => i.id === openInjectId)
      if (inject) {
        const offsetMs = calculateScheduledOffset(inject.scheduledTime, exerciseStartTime)
        setSelectedInject(inject)
        setSelectedInjectOffset(offsetMs)
        setDrawerOpen(true)
      }
    }
  }, [openInjectId, injects, exerciseStartTime])

  // Handlers
  const handleFireClick = async (injectId: string) => {
    if (onFire) {
      setIsSubmitting(true)
      try {
        await onFire(injectId)
      } finally {
        setIsSubmitting(false)
      }
    }
  }

  const handleSkipClick = (injectId: string) => {
    setSkipInjectId(injectId)
    setSkipReason('')
    setSkipDialogOpen(true)
  }

  const handleSkipConfirm = async () => {
    if (skipInjectId && skipReason.trim() && onSkip) {
      setIsSubmitting(true)
      try {
        await onSkip(skipInjectId, { reason: skipReason.trim() })
        setSkipDialogOpen(false)
        setSkipInjectId(null)
        setSkipReason('')
      } finally {
        setIsSubmitting(false)
      }
    }
  }

  const handleSkipCancel = () => {
    setSkipDialogOpen(false)
    setSkipInjectId(null)
    setSkipReason('')
  }

  const handleResetClick = async (injectId: string) => {
    if (onReset) {
      setIsSubmitting(true)
      try {
        await onReset(injectId)
      } finally {
        setIsSubmitting(false)
      }
    }
  }

  // Drawer handlers
  const handleInjectClick = (inject: InjectDto) => {
    const offsetMs = calculateScheduledOffset(inject.scheduledTime, exerciseStartTime)
    setSelectedInject(inject)
    setSelectedInjectOffset(offsetMs)
    setDrawerOpen(true)
  }

  const handleDrawerClose = () => {
    setDrawerOpen(false)
    onDrawerClose?.()
  }

  const handleDrawerFire = async (injectId: string) => {
    await handleFireClick(injectId)
    // Don't close drawer - let user see updated state
  }

  const handleDrawerSkip = (injectId: string) => {
    handleSkipClick(injectId)
    // Skip dialog will open, drawer stays open
  }

  const handleDrawerReset = async (injectId: string) => {
    await handleResetClick(injectId)
    // Don't close drawer - let user see updated state
  }

  // Error state
  if (error) {
    return <Alert severity="error">{error}</Alert>
  }

  // Loading state
  if (loading && injects.length === 0) {
    return (
      <Box>
        <Skeleton variant="rectangular" height={48} sx={{ mb: 1, borderRadius: 1 }} />
        <Skeleton variant="rectangular" height={120} sx={{ mb: 2, borderRadius: 1 }} />
        <Skeleton variant="rectangular" height={48} sx={{ mb: 1, borderRadius: 1 }} />
        <Skeleton variant="rectangular" height={80} sx={{ borderRadius: 1 }} />
      </Box>
    )
  }

  // Empty state
  if (injects.length === 0) {
    return (
      <Box sx={{ py: 4, textAlign: 'center' }}>
        <Typography variant="body2" color="text.secondary">
          No injects in this exercise's MSEL.
        </Typography>
      </Box>
    )
  }

  const { readyToFire, upcoming, later, fired, skipped } = sections

  // Keep selectedInject synced with latest inject data (for real-time updates)
  const currentSelectedInject = selectedInject
    ? injects.find(i => i.id === selectedInject.id) ?? selectedInject
    : null

  return (
    <>
      <Box>
        {/* Ready to Fire Section - Always show preview */}
        <InjectSection
          variant="ready"
          title="READY TO FIRE"
          count={readyToFire.length}
          defaultExpanded
        >
          {readyToFire.map(({ inject, offsetMs }) => (
            <InjectRow
              key={inject.id}
              inject={inject}
              offsetMs={offsetMs}
              elapsedTimeMs={elapsedTimeMs}
              canControl={canControl}
              showFireButton
              showPreview
              isSubmitting={isSubmitting}
              onFire={handleFireClick}
              onSkip={handleSkipClick}
              onReset={handleResetClick}
              onClick={handleInjectClick}
            />
          ))}
        </InjectSection>

        {/* Upcoming Section - Show preview by default */}
        <InjectSection
          variant="upcoming"
          title="UPCOMING"
          count={upcoming.length}
          subtitle="next 30 min"
          defaultExpanded
        >
          {upcoming.map(({ inject, offsetMs }) => (
            <InjectRow
              key={inject.id}
              inject={inject}
              offsetMs={offsetMs}
              elapsedTimeMs={elapsedTimeMs}
              canControl={canControl}
              showFireButton={false}
              showPreview
              isSubmitting={isSubmitting}
              onFire={handleFireClick}
              onSkip={handleSkipClick}
              onReset={handleResetClick}
              onClick={handleInjectClick}
            />
          ))}
        </InjectSection>

        {/* Later Section */}
        <InjectSection
          variant="later"
          title="LATER"
          count={later.length}
          defaultExpanded={later.length <= 5}
        >
          {later.map(({ inject, offsetMs }) => (
            <InjectRow
              key={inject.id}
              inject={inject}
              offsetMs={offsetMs}
              elapsedTimeMs={elapsedTimeMs}
              canControl={canControl}
              showFireButton={false}
              isSubmitting={isSubmitting}
              onFire={handleFireClick}
              onSkip={handleSkipClick}
              onReset={handleResetClick}
              onClick={handleInjectClick}
            />
          ))}
        </InjectSection>

        {/* Fired Section */}
        <InjectSection
          variant="fired"
          title="FIRED"
          count={fired.length}
          defaultExpanded={fired.length <= 3}
        >
          {fired.map(({ inject, offsetMs }) => (
            <InjectRow
              key={inject.id}
              inject={inject}
              offsetMs={offsetMs}
              elapsedTimeMs={elapsedTimeMs}
              showOffset={false}
              canControl={canControl}
              showFireButton={false}
              isSubmitting={isSubmitting}
              onFire={handleFireClick}
              onSkip={handleSkipClick}
              onReset={handleResetClick}
              onClick={handleInjectClick}
            />
          ))}
        </InjectSection>

        {/* Skipped Section */}
        <InjectSection
          variant="skipped"
          title="SKIPPED"
          count={skipped.length}
          defaultExpanded={skipped.length <= 3}
        >
          {skipped.map(({ inject, offsetMs }) => (
            <InjectRow
              key={inject.id}
              inject={inject}
              offsetMs={offsetMs}
              elapsedTimeMs={elapsedTimeMs}
              showOffset={false}
              canControl={canControl}
              showFireButton={false}
              isSubmitting={isSubmitting}
              onFire={handleFireClick}
              onSkip={handleSkipClick}
              onReset={handleResetClick}
              onClick={handleInjectClick}
            />
          ))}
        </InjectSection>
      </Box>

      {/* Skip Reason Dialog */}
      <Dialog open={skipDialogOpen} onClose={handleSkipCancel} maxWidth="sm" fullWidth>
        <DialogTitle>Skip Inject</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" marginBottom={2}>
            Please provide a reason for skipping this inject. This will be recorded for
            the after-action report.
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

      {/* Inject Detail Drawer */}
      <InjectDetailDrawer
        inject={currentSelectedInject}
        offsetMs={selectedInjectOffset}
        open={drawerOpen}
        onClose={handleDrawerClose}
        canControl={canControl}
        isSubmitting={isSubmitting}
        onFire={handleDrawerFire}
        onSkip={handleDrawerSkip}
        onReset={handleDrawerReset}
      />
    </>
  )
}

export default InjectListByStatus
