/**
 * Tests for ReadyToFireSection component
 *
 * Tests the ready to fire section with fire/skip actions
 * @see exercise-config/S06-clock-driven-conduct-view
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '../../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { ReadyToFireSection } from './ReadyToFireSection'
import { InjectStatus } from '../../../../types'
import type { InjectDto } from '../../../injects/types'

// Mock inject helper
const createMockInject = (
  id: string,
  injectNumber: number,
  overrides?: Partial<InjectDto>,
): InjectDto => ({
  id,
  injectNumber,
  title: `Inject ${injectNumber}`,
  description: 'Test description for the inject',
  scheduledTime: '09:00:00',
  deliveryTime: '00:30:00',
  scenarioDay: 1,
  scenarioTime: '09:00:00',
  target: 'Test Target',
  source: 'Test Source',
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: 'Email',
  deliveryMethodOther: null,
  injectType: 'Standard',
  status: InjectStatus.Ready,
  sequence: injectNumber,
  parentInjectId: null,
  triggerCondition: null,
  expectedAction: null,
  controllerNotes: null,
  readyAt: '2025-01-20T10:00:00Z',
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
  ...overrides,
})

describe('ReadyToFireSection', () => {
  it('renders nothing when no ready injects', () => {
    const { container } = render(
      <ReadyToFireSection
        injects={[]}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(container).toBeEmptyDOMElement()
  })

  it('renders section header with warning styling', () => {
    const injects = [createMockInject('1', 1)]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(screen.getByText('READY TO FIRE')).toBeInTheDocument()
    expect(screen.getByText('1')).toBeInTheDocument() // Count badge
  })

  it('is expanded by default', () => {
    const injects = [createMockInject('1', 1, { title: 'Urgent Inject' })]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(screen.getByText('Urgent Inject')).toBeInTheDocument()
  })

  it('collapses when header is clicked', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1, { title: 'Urgent Inject' })]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    // Content should be visible initially
    expect(screen.getByText('Urgent Inject')).toBeInTheDocument()

    const header = screen.getByText('READY TO FIRE').closest('div')
    if (header) {
      await user.click(header)
    }

    // Content should be hidden after collapse
    await waitFor(() => {
      const collapseEl = screen.getByText('Urgent Inject').closest('.MuiCollapse-root')
      expect(collapseEl).toHaveStyle({ height: '0px' })
    })
  })

  it('displays inject details with all fields', () => {
    const injects = [
      createMockInject('1', 1, {
        title: 'Critical Notification',
        description: 'Immediate response required',
        target: 'Emergency Operations Center',
        source: 'State Emergency Services',
        deliveryMethodName: 'Radio',
      }),
    ]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(screen.getByText('Critical Notification')).toBeInTheDocument()
    expect(screen.getByText('"Immediate response required"')).toBeInTheDocument()
    expect(screen.getByText(/To:/)).toBeInTheDocument()
    expect(screen.getByText('Emergency Operations Center')).toBeInTheDocument()
    expect(screen.getByText(/From:/)).toBeInTheDocument()
    expect(screen.getByText('State Emergency Services')).toBeInTheDocument()
    expect(screen.getByText(/Method:/)).toBeInTheDocument()
    expect(screen.getByText('Radio')).toBeInTheDocument()
  })

  it('shows overdue badge when inject is past delivery time', () => {
    const injects = [
      createMockInject('1', 1, {
        deliveryTime: '00:30:00', // 30 minutes
      }),
    ]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={45 * 60 * 1000} // 45 minutes elapsed (15 min overdue)
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(screen.getByText('OVERDUE')).toBeInTheDocument()
  })

  it('does not show overdue badge when inject is on time', () => {
    const injects = [
      createMockInject('1', 1, {
        deliveryTime: '00:30:00', // 30 minutes
      }),
    ]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={25 * 60 * 1000} // 25 minutes elapsed (5 min early)
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(screen.queryByText('OVERDUE')).not.toBeInTheDocument()
  })

  it('calls onFire when fire button is clicked', async () => {
    const user = userEvent.setup()
    const onFire = vi.fn()
    const injects = [createMockInject('1', 1)]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={onFire}
        onSkip={vi.fn()}
      />,
    )

    const fireButton = screen.getByRole('button', { name: /FIRE INJECT/i })
    await user.click(fireButton)

    expect(onFire).toHaveBeenCalledWith('1')
  })

  it('opens skip dialog when skip button is clicked', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1)]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    const skipButton = screen.getByRole('button', { name: /^Skip$/i })
    await user.click(skipButton)

    // Dialog should appear - check for heading and label
    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /Skip Inject/i })).toBeInTheDocument()
    // Use regex because required TextField adds asterisk to label
    expect(screen.getByLabelText(/Skip Reason/)).toBeInTheDocument()
  })

  it(
    'calls onSkip with reason when skip is confirmed',
    async () => {
      const user = userEvent.setup({ delay: null }) // Faster typing
      const onSkip = vi.fn()
      const injects = [createMockInject('1', 1)]

      render(
        <ReadyToFireSection
          injects={injects}
          elapsedTimeMs={0}
          onFire={vi.fn()}
          onSkip={onSkip}
        />,
      )

      // Open skip dialog
      const skipButton = screen.getByRole('button', { name: /^Skip$/i })
      await user.click(skipButton)

      // Wait for dialog to appear
      await screen.findByRole('dialog')

      // Enter skip reason (regex because required TextField adds asterisk)
      const reasonField = screen.getByLabelText(/Skip Reason/)
      await user.type(reasonField, 'Players ahead of schedule')

      // Confirm skip - look for the button in the dialog
      // There's one "Skip Inject" button in the dialog that becomes enabled when reason is provided
      const confirmButton = screen.getByRole('button', { name: /Skip Inject/i })
      await user.click(confirmButton)

      expect(onSkip).toHaveBeenCalledWith('1', { reason: 'Players ahead of schedule' })
    },
    10000,
  )

  it('disables skip confirm button when reason is empty', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1)]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    // Open skip dialog
    const skipButton = screen.getByRole('button', { name: /^Skip$/i })
    await user.click(skipButton)

    const confirmButtons = screen.getAllByRole('button', { name: /Skip Inject/i })
    const confirmButton = confirmButtons.find(btn => btn.hasAttribute('disabled'))
    expect(confirmButton).toBeDisabled()
  })

  it('closes skip dialog when cancel is clicked', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1)]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    // Open skip dialog
    const skipButton = screen.getByRole('button', { name: /^Skip$/i })
    await user.click(skipButton)

    expect(screen.getByRole('heading', { name: /Skip Inject/i })).toBeInTheDocument()

    // Cancel
    const cancelButton = screen.getByRole('button', { name: /Cancel/i })
    await user.click(cancelButton)

    await waitFor(() => {
      expect(screen.queryByRole('heading', { name: /Skip Inject/i })).not.toBeInTheDocument()
    })
  })

  it('hides action buttons when canControl is false', () => {
    const injects = [createMockInject('1', 1)]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        canControl={false}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(screen.queryByRole('button', { name: /FIRE INJECT/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Skip/i })).not.toBeInTheDocument()
  })

  it('disables buttons when isSubmitting is true', () => {
    const injects = [createMockInject('1', 1)]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        isSubmitting={true}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(screen.getByRole('button', { name: /FIRE INJECT/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Skip/i })).toBeDisabled()
  })

  it('calls onInjectClick when inject title is clicked', async () => {
    const user = userEvent.setup()
    const onInjectClick = vi.fn()
    const injects = [createMockInject('1', 1, { title: 'Clickable Inject' })]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
        onInjectClick={onInjectClick}
      />,
    )

    const title = screen.getByText('Clickable Inject')
    await user.click(title)

    expect(onInjectClick).toHaveBeenCalledWith(injects[0])
  })

  it('displays scenario time when available', () => {
    const injects = [
      createMockInject('1', 1, {
        scenarioDay: 3,
        scenarioTime: '15:30:00',
      }),
    ]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(screen.getByText('D3 15:30')).toBeInTheDocument()
  })

  it('renders multiple ready injects with dividers', () => {
    const injects = [
      createMockInject('1', 1, { title: 'First Ready' }),
      createMockInject('2', 2, { title: 'Second Ready' }),
      createMockInject('3', 3, { title: 'Third Ready' }),
    ]

    render(
      <ReadyToFireSection
        injects={injects}
        elapsedTimeMs={0}
        onFire={vi.fn()}
        onSkip={vi.fn()}
      />,
    )

    expect(screen.getByText('First Ready')).toBeInTheDocument()
    expect(screen.getByText('Second Ready')).toBeInTheDocument()
    expect(screen.getByText('Third Ready')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument() // Count badge
  })

  it(
    'trims whitespace from skip reason',
    async () => {
      const user = userEvent.setup({ delay: null }) // Faster typing
      const onSkip = vi.fn()
      const injects = [createMockInject('1', 1)]

      render(
        <ReadyToFireSection
          injects={injects}
          elapsedTimeMs={0}
          onFire={vi.fn()}
          onSkip={onSkip}
        />,
      )

      // Open skip dialog
      const skipButton = screen.getByRole('button', { name: /^Skip$/i })
      await user.click(skipButton)

      // Wait for dialog to appear
      await screen.findByRole('dialog')

      // Enter skip reason with extra whitespace (regex because required TextField adds asterisk)
      const reasonField = screen.getByLabelText(/Skip Reason/)
      await user.type(reasonField, '  Time constraints  ')

      // Confirm skip - click the "Skip Inject" button in the dialog
      const confirmButton = screen.getByRole('button', { name: /Skip Inject/i })
      await user.click(confirmButton)

      expect(onSkip).toHaveBeenCalledWith('1', { reason: 'Time constraints' })
    },
    10000,
  )
})
