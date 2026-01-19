/**
 * FloatingClockChip Component Tests
 *
 * Tests for the floating clock display with expandable controls panel.
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { FloatingClockChip } from './FloatingClockChip'
import { InjectStatus, ExerciseClockState } from '../../../types'
import type { ExerciseClockDto } from '../../exercise-clock/types'
import type { InjectDto } from '../../injects/types'

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {}

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value.toString()
    },
    removeItem: (key: string) => {
      delete store[key]
    },
    clear: () => {
      store = {}
    },
  }
})()

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
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
  scenarioDay: null,
  scenarioTime: null,
  target: 'EOC Director',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: 'Standard',
  status: InjectStatus.Pending,
  sequence: 1,
  parentInjectId: null,
  triggerCondition: null,
  expectedAction: null,
  controllerNotes: null,
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
  triggerType: 'Manual',
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

// Helper to create mock clock state
const createMockClockState = (
  overrides: Partial<ExerciseClockDto> = {},
): ExerciseClockDto => ({
  exerciseId: 'exercise-1',
  state: ExerciseClockState.Stopped,
  elapsedTime: '00:00:00',
  startedAt: null,
  startedBy: null,
  startedByName: null,
  capturedAt: '2025-01-01T00:00:00Z',
  exerciseStartTime: null,
  ...overrides,
})

describe('FloatingClockChip', () => {
  beforeEach(() => {
    localStorageMock.clear()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('Clock Display', () => {
    it('shows "00:00:00" when clock is null', () => {
      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      expect(screen.getByText('00:00:00')).toBeInTheDocument()
    })

    it('shows "00:00:00" when clock is stopped', () => {
      const clockState = createMockClockState({
        state: ExerciseClockState.Stopped,
      })

      render(
        <FloatingClockChip
          clockState={clockState}
          displayTime="00:00:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      expect(screen.getByText('00:00:00')).toBeInTheDocument()
    })

    it('shows elapsed time when clock is running', () => {
      const clockState = createMockClockState({
        state: ExerciseClockState.Running,
      })

      render(
        <FloatingClockChip
          clockState={clockState}
          displayTime="01:01:05"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      expect(screen.getByText('01:01:05')).toBeInTheDocument()
    })

    it('shows elapsed time when clock is paused', () => {
      const clockState = createMockClockState({
        state: ExerciseClockState.Paused,
      })

      render(
        <FloatingClockChip
          clockState={clockState}
          displayTime="02:00:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      expect(screen.getByText('02:00:00')).toBeInTheDocument()
    })

    it('shows "--:--:--" when loading', () => {
      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          loading={true}
          injects={[]}
          readyToFireCount={0}
        />,
      )

      expect(screen.getByText('--:--:--')).toBeInTheDocument()
    })
  })

  describe('Clock Icon Color', () => {
    it('shows green clock icon when running', () => {
      const clockState = createMockClockState({
        state: ExerciseClockState.Running,
      })

      const { container } = render(
        <FloatingClockChip
          clockState={clockState}
          displayTime="00:01:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      // Clock icon should have green color
      const clockIcon = container.querySelector('svg[data-icon="clock"]')
      expect(clockIcon).toHaveStyle({ color: '#2e7d32' })
    })

    it('shows orange clock icon when paused', () => {
      const clockState = createMockClockState({
        state: ExerciseClockState.Paused,
      })

      const { container } = render(
        <FloatingClockChip
          clockState={clockState}
          displayTime="00:01:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      // Clock icon should have orange color
      const clockIcon = container.querySelector('svg[data-icon="clock"]')
      expect(clockIcon).toHaveStyle({ color: '#ed6c02' })
    })

    it('shows default clock icon color when stopped', () => {
      const clockState = createMockClockState({
        state: ExerciseClockState.Stopped,
      })

      const { container } = render(
        <FloatingClockChip
          clockState={clockState}
          displayTime="00:00:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      // Clock icon should not have explicit color
      const clockIcon = container.querySelector('svg[data-icon="clock"]')
      expect(clockIcon).not.toHaveStyle({ color: '#2e7d32' })
      expect(clockIcon).not.toHaveStyle({ color: '#ed6c02' })
    })
  })

  describe('Ready to Fire Badge', () => {
    it('shows badge when readyToFireCount > 0', () => {
      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={[]}
          readyToFireCount={3}
        />,
      )

      // Badge should be visible with count
      expect(screen.getByText('3')).toBeInTheDocument()
    })

    it('hides badge when readyToFireCount is 0', () => {
      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      // No badge should be shown (no number badge visible)
      const badges = screen.queryAllByText(/^\d+$/)
      expect(badges.length).toBe(0)
    })
  })

  describe('Expand/Collapse Behavior', () => {
    it('starts collapsed by default', () => {
      const injects = [createMockInject()]

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={injects}
          readyToFireCount={0}
        />,
      )

      // Progress text should be in DOM but hidden (height 0)
      const progressText = screen.getByText(/Progress:/)
      const collapseContainer = progressText.closest('.MuiCollapse-root')
      expect(collapseContainer).toHaveStyle({ height: '0px' })
    })

    it('expands when clock display is clicked', async () => {
      const injects = [createMockInject()]
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={injects}
          readyToFireCount={0}
        />,
      )

      // Click on the time display
      await user.click(screen.getByText('00:00:00'))

      // Progress text should now be visible
      expect(screen.getByText(/Progress: 0 of 1 injects/)).toBeInTheDocument()
    })

    it('collapses when clicked again', async () => {
      const injects = [createMockInject()]
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={injects}
          readyToFireCount={0}
        />,
      )

      // Click to expand
      await user.click(screen.getByText('00:00:00'))
      const progressText = screen.getByText(/Progress:/)
      const collapseContainer = progressText.closest('.MuiCollapse-root')

      // Wait for expansion animation to complete
      await waitFor(() => {
        expect(collapseContainer).not.toHaveStyle({ height: '0px' })
      })

      // Click to collapse
      await user.click(screen.getByText('00:00:00'))

      // Wait for collapse animation to complete
      await waitFor(() => {
        expect(collapseContainer).toHaveStyle({ height: '0px' })
      })
    })

    it('shows chevron-down icon when collapsed', () => {
      const { container } = render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      expect(container.querySelector('svg[data-icon="chevron-down"]')).toBeInTheDocument()
    })

    it('shows chevron-up icon when expanded', async () => {
      const user = userEvent.setup()

      const { container } = render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      await user.click(screen.getByText('00:00:00'))

      expect(container.querySelector('svg[data-icon="chevron-up"]')).toBeInTheDocument()
    })

    it('collapses when clicking away', async () => {
      const injects = [createMockInject()]
      const user = userEvent.setup()

      render(
        <div>
          <div data-testid="outside">Outside element</div>
          <FloatingClockChip
            clockState={null}
            displayTime="00:00:00"
            injects={injects}
            readyToFireCount={0}
          />
        </div>,
      )

      // Click to expand
      await user.click(screen.getByText('00:00:00'))
      const progressText = screen.getByText(/Progress:/)
      const collapseContainer = progressText.closest('.MuiCollapse-root')

      // Wait for expansion animation to complete
      await waitFor(() => {
        expect(collapseContainer).not.toHaveStyle({ height: '0px' })
      })

      // Click outside
      const outside = screen.getByTestId('outside')
      await user.click(outside)

      // Wait for collapse animation to complete
      await waitFor(() => {
        expect(collapseContainer).toHaveStyle({ height: '0px' })
      })
    })
  })

  describe('Progress Display', () => {
    it('shows "0 of X injects" when no injects fired', async () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Pending }),
        createMockInject({ id: '2', status: InjectStatus.Pending }),
        createMockInject({ id: '3', status: InjectStatus.Pending }),
      ]
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={injects}
          readyToFireCount={0}
        />,
      )

      await user.click(screen.getByText('00:00:00'))

      expect(screen.getByText('Progress: 0 of 3 injects')).toBeInTheDocument()
    })

    it('counts fired injects in progress', async () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Fired, firedAt: '2025-01-01T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Fired, firedAt: '2025-01-01T10:05:00Z' }),
        createMockInject({ id: '3', status: InjectStatus.Pending }),
      ]
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={injects}
          readyToFireCount={0}
        />,
      )

      await user.click(screen.getByText('00:00:00'))

      expect(screen.getByText('Progress: 2 of 3 injects')).toBeInTheDocument()
    })

    it('counts skipped injects in progress', async () => {
      const injects = [
        createMockInject({ id: '1', status: InjectStatus.Fired, firedAt: '2025-01-01T10:00:00Z' }),
        createMockInject({ id: '2', status: InjectStatus.Skipped, skippedAt: '2025-01-01T10:05:00Z' }),
        createMockInject({ id: '3', status: InjectStatus.Pending }),
      ]
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={injects}
          readyToFireCount={0}
        />,
      )

      await user.click(screen.getByText('00:00:00'))

      expect(screen.getByText('Progress: 2 of 3 injects')).toBeInTheDocument()
    })

    it('handles empty inject list', async () => {
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={[]}
          readyToFireCount={0}
        />,
      )

      await user.click(screen.getByText('00:00:00'))

      expect(screen.getByText('Progress: 0 of 0 injects')).toBeInTheDocument()
    })
  })

  describe('Phase Display', () => {
    it('shows current phase from most recently fired inject', async () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:00:00Z',
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Fired,
          firedAt: '2025-01-01T10:30:00Z',
          phaseId: 'phase-2',
          phaseName: 'Evacuation',
        }),
        createMockInject({
          id: '3',
          status: InjectStatus.Pending,
          phaseId: 'phase-2',
          phaseName: 'Evacuation',
        }),
      ]
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={injects}
          readyToFireCount={0}
        />,
      )

      await user.click(screen.getByText('00:00:00'))

      expect(screen.getByText('Phase: Evacuation')).toBeInTheDocument()
    })

    it('shows phase from first pending inject when no injects fired', async () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Pending,
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
        createMockInject({
          id: '2',
          status: InjectStatus.Pending,
          phaseId: 'phase-1',
          phaseName: 'Initial Response',
        }),
      ]
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={injects}
          readyToFireCount={0}
        />,
      )

      await user.click(screen.getByText('00:00:00'))

      expect(screen.getByText('Phase: Initial Response')).toBeInTheDocument()
    })

    it('does not show phase when injects have no phase assigned', async () => {
      const injects = [
        createMockInject({
          id: '1',
          status: InjectStatus.Pending,
          phaseId: null,
          phaseName: null,
        }),
      ]
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={null}
          displayTime="00:00:00"
          injects={injects}
          readyToFireCount={0}
        />,
      )

      await user.click(screen.getByText('00:00:00'))

      expect(screen.queryByText(/Phase:/)).not.toBeInTheDocument()
    })
  })

  describe('Clock Controls', () => {
    describe('Start/Resume Button', () => {
      it('shows Start button when clock is stopped and canControl is true', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:00:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
          />,
        )

        await user.click(screen.getByText('00:00:00'))

        expect(screen.getByRole('button', { name: /Start/i })).toBeInTheDocument()
      })

      it('shows Resume button when clock is paused and canControl is true', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Paused })
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:00:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
          />,
        )

        await user.click(screen.getByText('00:00:00'))

        expect(screen.getByRole('button', { name: /Resume/i })).toBeInTheDocument()
      })

      it('calls onStart when Start button is clicked', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
        const onStart = vi.fn()
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:00:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
            onStart={onStart}
          />,
        )

        await user.click(screen.getByText('00:00:00'))
        await user.click(screen.getByRole('button', { name: /Start/i }))

        expect(onStart).toHaveBeenCalledTimes(1)
      })

      it('shows spinner when isStarting is true', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
        const user = userEvent.setup()

        const { container } = render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:00:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
            isStarting={true}
          />,
        )

        await user.click(screen.getByText('00:00:00'))

        expect(container.querySelector('svg[data-icon="spinner"]')).toBeInTheDocument()
      })
    })

    describe('Pause Button', () => {
      it('shows Pause button when clock is running and canControl is true', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Running })
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:01:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
          />,
        )

        await user.click(screen.getByText('00:01:00'))

        expect(screen.getByRole('button', { name: /Pause/i })).toBeInTheDocument()
      })

      it('calls onPause when Pause button is clicked', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Running })
        const onPause = vi.fn()
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:01:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
            onPause={onPause}
          />,
        )

        await user.click(screen.getByText('00:01:00'))
        await user.click(screen.getByRole('button', { name: /Pause/i }))

        expect(onPause).toHaveBeenCalledTimes(1)
      })
    })

    describe('Stop Button', () => {
      it('shows Stop button when clock is running and canControl is true', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Running })
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:01:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
          />,
        )

        await user.click(screen.getByText('00:01:00'))

        expect(screen.getByRole('button', { name: /Stop/i })).toBeInTheDocument()
      })

      it('shows Stop button when clock is paused and canControl is true', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Paused })
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:01:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
          />,
        )

        await user.click(screen.getByText('00:01:00'))

        expect(screen.getByRole('button', { name: /Stop/i })).toBeInTheDocument()
      })

      it('calls onStop when Stop button is clicked', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Running })
        const onStop = vi.fn()
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:01:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
            onStop={onStop}
          />,
        )

        await user.click(screen.getByText('00:01:00'))
        await user.click(screen.getByRole('button', { name: /Stop/i }))

        expect(onStop).toHaveBeenCalledTimes(1)
      })
    })

    describe('Reset Button', () => {
      it('shows Reset button when clock is stopped and canControl is true', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:00:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
          />,
        )

        await user.click(screen.getByText('00:00:00'))

        expect(screen.getByRole('button', { name: /Reset/i })).toBeInTheDocument()
      })

      it('calls onReset when Reset button is clicked', async () => {
        const clockState = createMockClockState({ state: ExerciseClockState.Stopped })
        const onReset = vi.fn()
        const user = userEvent.setup()

        render(
          <FloatingClockChip
            clockState={clockState}
            displayTime="00:00:00"
            injects={[]}
            readyToFireCount={0}
            canControl={true}
            onReset={onReset}
          />,
        )

        await user.click(screen.getByText('00:00:00'))
        await user.click(screen.getByRole('button', { name: /Reset/i }))

        expect(onReset).toHaveBeenCalledTimes(1)
      })
    })

    it('hides all control buttons when canControl is false', async () => {
      const clockState = createMockClockState({ state: ExerciseClockState.Running })
      const user = userEvent.setup()

      render(
        <FloatingClockChip
          clockState={clockState}
          displayTime="00:01:00"
          injects={[]}
          readyToFireCount={0}
          canControl={false}
        />,
      )

      await user.click(screen.getByText('00:01:00'))

      expect(screen.queryByRole('button', { name: /Start/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /Pause/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /Stop/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /Reset/i })).not.toBeInTheDocument()
    })
  })
})
