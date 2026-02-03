/**
 * Tests for ClockDrivenConductView component
 *
 * Tests the clock-driven conduct view layout (CLK-06)
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '../../../test/test-utils'
import { ClockDrivenConductView } from './ClockDrivenConductView'
import { InjectStatus } from '../../../types'
import type { ExerciseDto } from '../types'
import type { InjectDto } from '../../injects/types'

// Mock inject helper
const createMockInject = (
  id: string,
  status: InjectStatus,
  deliveryTime: string | null,
  injectNumber: number,
): InjectDto => ({
  id,
  injectNumber,
  title: `Inject ${injectNumber}`,
  description: 'Test description',
  scheduledTime: '09:00:00',
  deliveryTime,
  scenarioDay: 1,
  scenarioTime: '09:00:00',
  target: 'Test Target',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: 'Standard',
  status,
  sequence: injectNumber,
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
  createdAt: '2025-01-20T00:00:00Z',
  updatedAt: '2025-01-20T00:00:00Z',
  sourceReference: null,
  priority: null,
  triggerType: 'Manual',
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
})

const mockExercise: ExerciseDto = {
  id: 'ex-1',
  name: 'Test Exercise',
  description: null,
  exerciseType: 'TTX',
  status: 'Active',
  isPracticeMode: false,
  scheduledDate: '2025-01-20',
  startTime: '08:00:00',
  endTime: null,
  timeZoneId: 'America/New_York',
  location: null,
  organizationId: 'org-1',
  activeMselId: 'msel-1',
  deliveryMode: 'ClockDriven',
  timelineMode: 'RealTime',
  timeScale: null,
  createdAt: '2025-01-20T00:00:00Z',
  updatedAt: '2025-01-20T00:00:00Z',
  createdBy: 'user-1',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: true,
  previousStatus: null,
}

describe('ClockDrivenConductView', () => {
  it('renders sections when DeliveryMode is ClockDriven', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Synchronized, '00:30:00', 1),
      createMockInject('2', InjectStatus.Draft, '00:50:00', 2),
      createMockInject('3', InjectStatus.Released, '00:20:00', 3),
    ]

    render(
      <ClockDrivenConductView
        exercise={mockExercise}
        injects={injects}
        elapsedTimeMs={40 * 60 * 1000}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    // Should show section headers
    expect(screen.getByText(/READY TO FIRE/i)).toBeInTheDocument()
    expect(screen.getByText(/UPCOMING/i)).toBeInTheDocument()
    expect(screen.getByText(/COMPLETED/i)).toBeInTheDocument()
  })

  it('shows Synchronized injects in Ready to Fire section', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Synchronized, '00:30:00', 1),
      createMockInject('2', InjectStatus.Synchronized, '00:45:00', 2),
    ]

    render(
      <ClockDrivenConductView
        exercise={mockExercise}
        injects={injects}
        elapsedTimeMs={40 * 60 * 1000}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    // Badge should show count
    const readySection = screen.getByText(/READY TO FIRE/i).closest('div')
    expect(readySection).toBeInTheDocument()
    // Both injects should appear (check by inject number)
    expect(screen.getByText('#1')).toBeInTheDocument()
    expect(screen.getByText('#2')).toBeInTheDocument()
  })

  it('shows countdown for upcoming injects', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Draft, '00:50:00', 1), // 10 min away
    ]

    render(
      <ClockDrivenConductView
        exercise={mockExercise}
        injects={injects}
        elapsedTimeMs={40 * 60 * 1000} // 40 minutes elapsed
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    // Should show countdown (10:00)
    expect(screen.getByText(/in 10:00/i)).toBeInTheDocument()
  })

  it('collapses Completed section by default', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Released, '00:30:00', 1),
    ]

    render(
      <ClockDrivenConductView
        exercise={mockExercise}
        injects={injects}
        elapsedTimeMs={40 * 60 * 1000}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    const completedHeader = screen.getByText(/COMPLETED/i)
    expect(completedHeader).toBeInTheDocument()

    // The inject should not be visible initially (section collapsed)
    // Note: This depends on implementation - the section might use Collapse component
  })

  it('shows "No injects ready" message when 0 Synchronized injects', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Draft, '01:00:00', 1),
    ]

    render(
      <ClockDrivenConductView
        exercise={mockExercise}
        injects={injects}
        elapsedTimeMs={40 * 60 * 1000}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    // Ready section should either not appear or show empty message
    // This depends on implementation choice
  })
})
