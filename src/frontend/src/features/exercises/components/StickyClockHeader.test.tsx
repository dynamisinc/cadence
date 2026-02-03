/**
 * StickyClockHeader Component Tests
 *
 * Tests for the sticky header bar showing clock, controls, phase, and progress.
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../../../theme/cobraTheme'
import { StickyClockHeader } from './StickyClockHeader'
import {
  ExerciseClockState,
  InjectStatus,
  InjectType,
  TriggerType,
  ExerciseStatus,
  ExerciseType,
  DeliveryMode,
  TimelineMode,
} from '../../../types'
import type { ExerciseClockDto } from '../../exercise-clock/types'
import type { InjectDto } from '../../injects/types'
import type { ExerciseDto } from '../types'

// Helper to render with theme
const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

// Helper to create mock exercise
const createMockExercise = (overrides: Partial<ExerciseDto> = {}): ExerciseDto => ({
  id: 'exercise-1',
  name: 'Test Exercise',
  description: null,
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Active,
  isPracticeMode: false,
  scheduledDate: '2025-01-15',
  startTime: null,
  endTime: null,
  timeZoneId: 'America/New_York',
  location: null,
  organizationId: 'org-1',
  activeMselId: 'msel-1',
  deliveryMode: DeliveryMode.ClockDriven,
  timelineMode: TimelineMode.RealTime,
  timeScale: null,
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  createdBy: 'user-1',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: false,
  previousStatus: null,
  ...overrides,
})

// Helper to create mock clock state
const createMockClockState = (
  overrides: Partial<ExerciseClockDto> = {},
): ExerciseClockDto => ({
  exerciseId: 'exercise-1',
  state: ExerciseClockState.Stopped,
  startedAt: null,
  elapsedTime: '00:00:00',
  startedBy: null,
  startedByName: null,
  capturedAt: '2025-01-14T12:00:00Z',
  exerciseStartTime: '09:00:00',
  ...overrides,
})

// Helper to create mock inject
const createMockInject = (
  overrides: Partial<InjectDto> = {},
): InjectDto => ({
  id: 'inject-1',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description',
  scheduledTime: '09:00:00',
  deliveryTime: null,
  scenarioDay: null,
  scenarioTime: null,
  target: 'EOC Director',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: InjectType.Standard,
  status: InjectStatus.Draft,
  sequence: 1,
  parentInjectId: null,
  triggerCondition: null,
  expectedAction: null,
  controllerNotes: null,
  readyAt: null,
  firedAt: null,
  firedBy: null,
  firedByName: null,
  skippedAt: null,
  skippedBy: null,
  skippedByName: null,
  skipReason: null,
  mselId: 'msel-1',
  phaseId: null,
  phaseName: null,
  objectiveIds: [],
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  sourceReference: null,
  priority: null,
  triggerType: TriggerType.Manual,
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

describe('StickyClockHeader', () => {
  const defaultProps = {
    exercise: createMockExercise(),
    clockState: createMockClockState(),
    displayTime: '00:00:00',
    elapsedTimeMs: 0,
    injects: [],
    readyToFireCount: 0,
  }

  describe('Clock Display', () => {
    it('renders the display time', () => {
      renderWithTheme(<StickyClockHeader {...defaultProps} displayTime="01:23:45" />)

      expect(screen.getByText('01:23:45')).toBeInTheDocument()
    })

    it('shows placeholder when loading', () => {
      renderWithTheme(<StickyClockHeader {...defaultProps} loading={true} />)

      expect(screen.getByText('--:--:--')).toBeInTheDocument()
    })

    it('applies success color when clock is running', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Running })
      renderWithTheme(
        <StickyClockHeader {...defaultProps} clockState={clockState} displayTime="00:15:30" />,
      )

      const timeDisplay = screen.getByText('00:15:30')
      expect(timeDisplay).toBeInTheDocument()
      // Component uses sx prop with color: 'success.main'
    })

    it('applies warning color when clock is paused', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Paused })
      renderWithTheme(
        <StickyClockHeader {...defaultProps} clockState={clockState} displayTime="00:15:30" />,
      )

      const timeDisplay = screen.getByText('00:15:30')
      expect(timeDisplay).toBeInTheDocument()
      // Component uses sx prop with color: 'warning.main'
    })

    it('applies default color when clock is stopped', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
      renderWithTheme(
        <StickyClockHeader {...defaultProps} clockState={clockState} displayTime="00:00:00" />,
      )

      const timeDisplay = screen.getByText('00:00:00')
      expect(timeDisplay).toBeInTheDocument()
      // Component uses sx prop with color: 'text.primary'
    })
  })

  describe('Clock Controls', () => {
    it('hides controls when canControl is false', () => {
      renderWithTheme(<StickyClockHeader {...defaultProps} canControl={false} />)

      expect(screen.queryByRole('button')).not.toBeInTheDocument()
    })

    it('shows play button when clock is stopped and canControl is true', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onStart={vi.fn()}
        />,
      )

      // Play button should be visible (using FontAwesome play icon)
      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)
    })

    it('shows pause and stop buttons when clock is running', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Running })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onPause={vi.fn()}
          onStop={vi.fn()}
        />,
      )

      // Should have pause and stop buttons
      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBe(2) // Pause and Stop
    })

    it('shows play and stop buttons when clock is paused', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Paused })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onStart={vi.fn()}
          onStop={vi.fn()}
        />,
      )

      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBe(2) // Play and Stop
    })

    it('shows reset button when clock is stopped', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onStart={vi.fn()}
          onReset={vi.fn()}
        />,
      )

      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBe(2) // Play and Reset
    })

    it('calls onStart when play button is clicked', async () => {
      const onStart = vi.fn()
      const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onStart={onStart}
        />,
      )

      const buttons = screen.getAllByRole('button')
      await userEvent.click(buttons[0]) // First button should be play

      expect(onStart).toHaveBeenCalledOnce()
    })

    it('calls onPause when pause button is clicked', async () => {
      const onPause = vi.fn()
      const clockState = createMockClockState({ state: ExerciseClockState.Running })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onPause={onPause}
          onStop={vi.fn()}
        />,
      )

      const buttons = screen.getAllByRole('button')
      await userEvent.click(buttons[0]) // First button should be pause

      expect(onPause).toHaveBeenCalledOnce()
    })

    it('calls onStop when stop button is clicked', async () => {
      const onStop = vi.fn()
      const clockState = createMockClockState({ state: ExerciseClockState.Running })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onPause={vi.fn()}
          onStop={onStop}
        />,
      )

      const buttons = screen.getAllByRole('button')
      await userEvent.click(buttons[1]) // Second button should be stop

      expect(onStop).toHaveBeenCalledOnce()
    })

    it('calls onReset when reset button is clicked', async () => {
      const onReset = vi.fn()
      const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onStart={vi.fn()}
          onReset={onReset}
        />,
      )

      const buttons = screen.getAllByRole('button')
      await userEvent.click(buttons[1]) // Second button should be reset

      expect(onReset).toHaveBeenCalledOnce()
    })

    it('disables start button when isStarting is true', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onStart={vi.fn()}
          isStarting={true}
        />,
      )

      const buttons = screen.getAllByRole('button')
      expect(buttons[0]).toBeDisabled()
    })

    it('disables pause button when isPausing is true', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Running })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onPause={vi.fn()}
          onStop={vi.fn()}
          isPausing={true}
        />,
      )

      const buttons = screen.getAllByRole('button')
      expect(buttons[0]).toBeDisabled()
    })

    it('disables stop button when isStopping is true', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Running })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onPause={vi.fn()}
          onStop={vi.fn()}
          isStopping={true}
        />,
      )

      const buttons = screen.getAllByRole('button')
      expect(buttons[1]).toBeDisabled()
    })

    it('disables reset button when isResetting is true', () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={clockState}
          canControl={true}
          onStart={vi.fn()}
          onReset={vi.fn()}
          isResetting={true}
        />,
      )

      const buttons = screen.getAllByRole('button')
      expect(buttons[1]).toBeDisabled()
    })
  })

  describe('Phase Display', () => {
    it('shows current phase name from most recently fired inject', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Released,
          firedAt: '2025-01-14T10:00:00Z',
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Released,
          firedAt: '2025-01-14T10:30:00Z',
          phaseId: 'phase-2',
          phaseName: 'Evacuation',
        }),
        createMockInject({
          id: '3',
          status: InjectStatus.Draft,
          phaseId: 'phase-2',
          phaseName: 'Evacuation',
        }),
      ]

      renderWithTheme(<StickyClockHeader {...defaultProps} injects={injects} />)

      expect(screen.getByText('Evacuation')).toBeInTheDocument()
    })

    it('shows phase from first pending inject when no injects fired', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Draft,
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Draft,
          phaseId: 'phase-2',
          phaseName: 'Evacuation',
        }),
      ]

      renderWithTheme(<StickyClockHeader {...defaultProps} injects={injects} />)

      expect(screen.getByText('Initial Response')).toBeInTheDocument()
    })

    it('hides phase when no injects have phase assigned', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Draft,
          phaseId: null,
          phaseName: null,
        }),
      ]

      renderWithTheme(<StickyClockHeader {...defaultProps} injects={injects} />)

      // Phase should not be rendered when null
      expect(screen.queryByText(/Initial Response/i)).not.toBeInTheDocument()
      expect(screen.queryByText(/Evacuation/i)).not.toBeInTheDocument()
    })

    it('hides phase when inject list is empty', () => {
      renderWithTheme(<StickyClockHeader {...defaultProps} injects={[]} />)

      // No phase text should appear
      expect(screen.queryByText(/Response/i)).not.toBeInTheDocument()
    })
  })

  describe('Progress Display', () => {
    it('shows 0/0 progress when no injects', () => {
      renderWithTheme(<StickyClockHeader {...defaultProps} injects={[]} />)

      expect(screen.getByText('0/0 injects')).toBeInTheDocument()
    })

    it('shows correct progress count with pending injects', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Draft }),
        createMockInject({ id: '2', status: InjectStatus.Draft }),
        createMockInject({ id: '3', status: InjectStatus.Draft }),
      ]

      renderWithTheme(<StickyClockHeader {...defaultProps} injects={injects} />)

      expect(screen.getByText('0/3 injects')).toBeInTheDocument()
    })

    it('counts fired injects in progress', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Released, firedAt: '2025-01-14T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Released, firedAt: '2025-01-14T10:05:00Z' }),
        createMockInject({ id: '3', status: InjectStatus.Draft }),
      ]

      renderWithTheme(<StickyClockHeader {...defaultProps} injects={injects} />)

      expect(screen.getByText('2/3 injects')).toBeInTheDocument()
    })

    it('counts skipped injects in progress', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Released, firedAt: '2025-01-14T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Deferred, skippedAt: '2025-01-14T10:05:00Z' }),
        createMockInject({ id: '3', status: InjectStatus.Draft }),
      ]

      renderWithTheme(<StickyClockHeader {...defaultProps} injects={injects} />)

      expect(screen.getByText('2/3 injects')).toBeInTheDocument()
    })

    it('shows 100% progress when all injects completed', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Released, firedAt: '2025-01-14T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Deferred, skippedAt: '2025-01-14T10:05:00Z' }),
        createMockInject({ id: '3', status: InjectStatus.Released, firedAt: '2025-01-14T10:10:00Z' }),
      ]

      renderWithTheme(<StickyClockHeader {...defaultProps} injects={injects} />)

      expect(screen.getByText('3/3 injects')).toBeInTheDocument()
    })

    it('renders progress bar with correct percentage', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Released, firedAt: '2025-01-14T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Draft }),
        createMockInject({ id: '3', status: InjectStatus.Draft }),
        createMockInject({ id: '4', status: InjectStatus.Draft }),
      ]

      const { container } = render(<StickyClockHeader {...defaultProps} injects={injects} />)

      // MUI LinearProgress uses aria-valuenow
      const progressBar = container.querySelector('[role="progressbar"]')
      expect(progressBar).toBeInTheDocument()
      expect(progressBar).toHaveAttribute('aria-valuenow', '25')
    })

    it('renders 0% progress bar when no injects completed', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Draft }),
        createMockInject({ id: '2', status: InjectStatus.Draft }),
      ]

      const { container } = render(<StickyClockHeader {...defaultProps} injects={injects} />)

      const progressBar = container.querySelector('[role="progressbar"]')
      expect(progressBar).toHaveAttribute('aria-valuenow', '0')
    })

    it('renders 100% progress bar when all injects completed', () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Released, firedAt: '2025-01-14T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Deferred, skippedAt: '2025-01-14T10:05:00Z' }),
      ]

      const { container } = render(<StickyClockHeader {...defaultProps} injects={injects} />)

      const progressBar = container.querySelector('[role="progressbar"]')
      expect(progressBar).toHaveAttribute('aria-valuenow', '100')
    })
  })

  describe('Ready to Fire Badge', () => {
    it('shows ready to fire badge when count > 0', () => {
      renderWithTheme(<StickyClockHeader {...defaultProps} readyToFireCount={3} />)

      // Badge should be rendered with count
      expect(screen.getByText('3')).toBeInTheDocument()
    })

    it('hides ready to fire badge when count is 0', () => {
      const { container } = render(<StickyClockHeader {...defaultProps} readyToFireCount={0} />)

      // Badge should not be rendered
      const badge = container.querySelector('.MuiBadge-badge')
      expect(badge).not.toBeInTheDocument()
    })
  })

  describe('Null Clock State Handling', () => {
    it('handles null clock state gracefully', () => {
      renderWithTheme(<StickyClockHeader {...defaultProps} clockState={null} displayTime="00:00:00" />)

      expect(screen.getByText('00:00:00')).toBeInTheDocument()
    })

    it('does not show controls when clock state is null', () => {
      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={null}
          canControl={true}
          onStart={vi.fn()}
        />,
      )

      // No buttons should be rendered when clock state is null
      expect(screen.queryByRole('button')).not.toBeInTheDocument()
    })
  })

  describe('Component Structure', () => {
    it('renders all major sections', () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Draft,
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
      ]

      renderWithTheme(
        <StickyClockHeader
          {...defaultProps}
          clockState={createMockClockState({ state: ExerciseClockState.Running })}
          displayTime="00:15:30"
          injects={injects}
          readyToFireCount={2}
          canControl={true}
          onPause={vi.fn()}
          onStop={vi.fn()}
        />,
      )

      // Clock display
      expect(screen.getByText('00:15:30')).toBeInTheDocument()

      // Controls
      expect(screen.getAllByRole('button').length).toBeGreaterThan(0)

      // Phase
      expect(screen.getByText('Initial Response')).toBeInTheDocument()

      // Progress
      expect(screen.getByText('0/1 injects')).toBeInTheDocument()

      // Ready badge
      expect(screen.getByText('2')).toBeInTheDocument()
    })
  })
})
