/**
 * FacilitatorPacedConductView Tests
 *
 * Tests for facilitator-paced conduct view.
 *
 * @module features/exercises
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@/test/testUtils'
import {
  InjectStatus,
  InjectType,
  TriggerType,
  ExerciseStatus,
  ExerciseType,
  DeliveryMode,
  TimelineMode,
} from '../../../types'
import type { ExerciseDto } from '../types'
import type { InjectDto } from '../../injects/types'
import { FacilitatorPacedConductView } from './FacilitatorPacedConductView'

// Test helper to create exercise
const createExercise = (overrides: Partial<ExerciseDto> = {}): ExerciseDto => ({
  id: 'test-exercise-id',
  name: 'Test Exercise',
  description: null,
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Active,
  isPracticeMode: false,
  scheduledDate: '2024-01-15',
  startTime: null,
  endTime: null,
  timeZoneId: 'America/New_York',
  location: null,
  organizationId: 'test-org-id',
  activeMselId: 'test-msel-id',
  deliveryMode: DeliveryMode.FacilitatorPaced,
  timelineMode: TimelineMode.StoryOnly,
  timeScale: null,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  createdBy: 'test-user-id',
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

// Test helper to create inject
const createInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: overrides.id || 'test-id',
  injectNumber: overrides.injectNumber || 1,
  title: overrides.title || 'Test Inject',
  description: overrides.description || 'Test description',
  scheduledTime: '08:00:00',
  deliveryTime: null,
  scenarioDay: null,
  scenarioTime: null,
  target: 'Test Target',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: InjectType.Standard,
  status: InjectStatus.Pending,
  sequence: overrides.sequence || 1,
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
  mselId: 'test-msel-id',
  phaseId: null,
  phaseName: null,
  objectiveIds: [],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  sourceReference: null,
  priority: null,
  triggerType: TriggerType.Manual,
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

describe('FacilitatorPacedConductView', () => {
  it('does not show elapsed time clock', () => {
    const exercise = createExercise()
    const injects = [createInject({ id: 'inject-1', sequence: 1 })]

    render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
      />,
    )

    // Should not show elapsed time - check that typical clock elements don't exist
    expect(screen.queryByText(/elapsed/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/00:00:00/)).not.toBeInTheDocument()
  })

  it('shows progress indicator', () => {
    const exercise = createExercise()
    const injects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Fired }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
      createInject({ id: 'inject-4', sequence: 4, status: InjectStatus.Pending }),
    ]

    render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
      />,
    )

    expect(screen.getByText(/3 of 4 injects/i)).toBeInTheDocument()
  })

  it('shows current inject panel', () => {
    const exercise = createExercise()
    const injects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending, title: 'First Pending Inject' }),
    ]

    render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
      />,
    )

    // Check for the header and the inject title
    expect(screen.getByRole('heading', { name: /CURRENT INJECT/i })).toBeInTheDocument()
    expect(screen.getByText(/First Pending Inject/)).toBeInTheDocument()
  })

  it('shows up next section with upcoming injects', () => {
    const exercise = createExercise()
    const injects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending, title: 'Next Inject' }),
    ]

    render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
      />,
    )

    expect(screen.getByText(/UP NEXT/i)).toBeInTheDocument()
    expect(screen.getByText(/Next Inject/)).toBeInTheDocument()
  })

  it('shows completed section with fired and skipped injects', () => {
    const exercise = createExercise()
    const injects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
    ]

    render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
      />,
    )

    expect(screen.getByText(/COMPLETED/i)).toBeInTheDocument()
  })

  it('advances to next inject after firing', async () => {
    const exercise = createExercise()
    const injects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending, title: 'First Inject' }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending, title: 'Second Inject' }),
    ]
    const onFire = vi.fn()

    const { rerender } = render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={onFire}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
      />,
    )

    // Current inject should be First Inject
    expect(screen.getByText(/First Inject/)).toBeInTheDocument()

    // Fire button should call onFire
    fireEvent.click(screen.getByRole('button', { name: /fire & continue/i }))
    expect(onFire).toHaveBeenCalledWith('inject-1')

    // Simulate inject being fired (status change)
    const updatedInjects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired, title: 'First Inject' }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending, title: 'Second Inject' }),
    ]

    rerender(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={updatedInjects}
        onFire={onFire}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
      />,
    )

    // Current inject should now be Second Inject
    expect(screen.getByText(/Second Inject/)).toBeInTheDocument()
  })

  it('opens jump confirmation dialog when Jump clicked', () => {
    const exercise = createExercise()
    const injects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
    ]

    render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
      />,
    )

    // Click Jump on an inject in Up Next
    const jumpButtons = screen.getAllByRole('button', { name: /jump/i })
    fireEvent.click(jumpButtons[0])

    // Dialog should open
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText(/jump to inject/i)).toBeInTheDocument()
  })

  it('calls onJumpTo when jump confirmed', async () => {
    const exercise = createExercise()
    const injects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
    ]
    const onJumpTo = vi.fn()

    render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onJumpTo={onJumpTo}
      />,
    )

    // Click Jump
    const jumpButtons = screen.getAllByRole('button', { name: /jump/i })
    fireEvent.click(jumpButtons[0])

    // Confirm
    fireEvent.click(screen.getByRole('button', { name: /skip & jump/i }))

    await waitFor(() => {
      expect(onJumpTo).toHaveBeenCalledWith('inject-2', ['inject-1'])
    })
  })

  it('shows Exercise Complete when all injects done', () => {
    const exercise = createExercise()
    const injects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Fired }),
    ]

    render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
      />,
    )

    expect(screen.getByText(/exercise complete/i)).toBeInTheDocument()
  })

  it('respects canControl prop', () => {
    const exercise = createExercise()
    const injects = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
    ]

    render(
      <FacilitatorPacedConductView
        exercise={exercise}
        injects={injects}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onJumpTo={vi.fn()}
        canControl={false}
      />,
    )

    expect(screen.queryByRole('button', { name: /fire & continue/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /jump/i })).not.toBeInTheDocument()
  })
})
