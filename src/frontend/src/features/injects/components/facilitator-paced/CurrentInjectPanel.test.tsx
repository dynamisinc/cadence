/**
 * CurrentInjectPanel Tests
 *
 * Tests for current inject panel in facilitator-paced conduct view.
 *
 * @module features/injects
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@/test/testUtils'
import { InjectStatus, InjectType, TriggerType } from '../../../../types'
import type { InjectDto } from '../../types'
import { CurrentInjectPanel } from './CurrentInjectPanel'

// Test helper to create inject with minimal required fields
const createInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: overrides.id || 'test-id',
  injectNumber: overrides.injectNumber || 1,
  title: overrides.title || 'Test Inject',
  description: overrides.description || 'Test description',
  scheduledTime: '08:00:00',
  deliveryTime: null,
  scenarioDay: 1,
  scenarioTime: '10:00:00',
  target: 'Test Target',
  source: 'Test Source',
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: 'Verbal',
  deliveryMethodOther: null,
  injectType: InjectType.Standard,
  status: InjectStatus.Draft,
  sequence: overrides.sequence || 1,
  parentInjectId: null,
  triggerCondition: null,
  expectedAction: 'Expected action details',
  controllerNotes: 'Controller notes here',
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

describe('CurrentInjectPanel', () => {
  it('renders inject number and title', () => {
    const inject = createInject({ injectNumber: 5, title: 'Evacuation Order' })

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    expect(screen.getByText(/#5/)).toBeInTheDocument()
    expect(screen.getByText(/Evacuation Order/)).toBeInTheDocument()
  })

  it('displays full inject description', () => {
    const inject = createInject({
      description: 'This is a full inject description that should be displayed completely.',
    })

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    expect(
      screen.getByText(/This is a full inject description that should be displayed completely/),
    ).toBeInTheDocument()
  })

  it('displays target and delivery method', () => {
    const inject = createInject({
      target: 'EOC Director',
      deliveryMethodName: 'Verbal announcement',
    })

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    expect(screen.getByText(/To:/)).toBeInTheDocument()
    expect(screen.getByText(/EOC Director/)).toBeInTheDocument()
    expect(screen.getByText(/Via:/)).toBeInTheDocument()
    expect(screen.getByText(/Verbal announcement/)).toBeInTheDocument()
  })

  it('displays story time when available', () => {
    const inject = createInject({
      scenarioDay: 2,
      scenarioTime: '14:30:00',
    })

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    // formatScenarioTime returns "D2 14:30" for day 2 at 14:30
    expect(screen.getByText(/D2 14:30/)).toBeInTheDocument()
  })

  it('displays expected action when available', () => {
    const inject = createInject({
      expectedAction: 'Activate EAS, coordinate with transportation',
    })

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    expect(screen.getByText(/Expected Action/i)).toBeInTheDocument()
    expect(screen.getByText(/Activate EAS, coordinate with transportation/)).toBeInTheDocument()
  })

  it('displays controller notes when available', () => {
    const inject = createInject({
      controllerNotes: 'Allow 10-15 minutes discussion. Prompt if they miss shelter ops.',
    })

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    expect(screen.getByText(/Controller Notes/i)).toBeInTheDocument()
    expect(
      screen.getByText(/Allow 10-15 minutes discussion. Prompt if they miss shelter ops./),
    ).toBeInTheDocument()
  })

  it('hides expected action section when not provided', () => {
    const inject = createInject({ expectedAction: null })

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    expect(screen.queryByText(/Expected Action/i)).not.toBeInTheDocument()
  })

  it('hides controller notes section when not provided', () => {
    const inject = createInject({ controllerNotes: null })

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    expect(screen.queryByText(/Controller Notes/i)).not.toBeInTheDocument()
  })

  it('shows Fire & Continue button when canControl is true', () => {
    const inject = createInject()

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    expect(screen.getByText(/FIRE & CONTINUE/)).toBeInTheDocument()
  })

  it('shows Skip button when canControl is true', () => {
    const inject = createInject()

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    expect(screen.getByText('Skip')).toBeInTheDocument()
  })

  it('hides action buttons when canControl is false', () => {
    const inject = createInject()

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={false}
      />,
    )

    expect(screen.queryByText(/FIRE & CONTINUE/)).not.toBeInTheDocument()
    expect(screen.queryByText('Skip')).not.toBeInTheDocument()
  })

  it('calls onFire when Fire & Continue button clicked', async () => {
    const onFire = vi.fn()
    const inject = createInject()

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={onFire}
        onSkip={vi.fn()}
        canControl={true}
      />,
    )

    const fireButton = screen.getByText(/FIRE & CONTINUE/).closest('button')
    if (fireButton) {
      fireEvent.click(fireButton)
    }

    expect(onFire).toHaveBeenCalledTimes(1)
  })

  it('calls onSkip when Skip button clicked', async () => {
    const onSkip = vi.fn()
    const inject = createInject()

    // Mock window.prompt
    vi.spyOn(window, 'prompt').mockReturnValue('Test reason')

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={onSkip}
        canControl={true}
      />,
    )

    const skipButton = screen.getByText('Skip').closest('button')
    if (skipButton) {
      fireEvent.click(skipButton)
    }

    expect(onSkip).toHaveBeenCalledTimes(1)

    vi.restoreAllMocks()
  })

  it('disables buttons when isSubmitting is true', () => {
    const inject = createInject()

    render(
      <CurrentInjectPanel
        inject={inject}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        canControl={true}
        isSubmitting={true}
      />,
    )

    const fireButton = screen.getByText(/FIRE & CONTINUE/).closest('button')
    const skipButton = screen.getByText('Skip').closest('button')

    expect(fireButton).toBeDisabled()
    expect(skipButton).toBeDisabled()
  })
})
