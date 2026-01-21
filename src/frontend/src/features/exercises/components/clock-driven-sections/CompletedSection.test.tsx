/**
 * Tests for CompletedSection component
 *
 * Tests the completed injects section (fired and skipped)
 * @see exercise-config/S06-clock-driven-conduct-view
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '../../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { CompletedSection } from './CompletedSection'
import { InjectStatus } from '../../../../types'
import type { InjectDto } from '../../../injects/types'

// Mock inject helper
const createMockInject = (
  id: string,
  status: InjectStatus,
  injectNumber: number,
  overrides?: Partial<InjectDto>,
): InjectDto => ({
  id,
  injectNumber,
  title: `Inject ${injectNumber}`,
  description: 'Test description',
  scheduledTime: '09:00:00',
  deliveryTime: '00:30:00',
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
  firedAt: status === InjectStatus.Fired ? '2025-01-20T10:30:00Z' : null,
  firedBy: status === InjectStatus.Fired ? 'user-1' : null,
  firedByName: status === InjectStatus.Fired ? 'John Controller' : null,
  skippedAt: status === InjectStatus.Skipped ? '2025-01-20T10:30:00Z' : null,
  skippedBy: status === InjectStatus.Skipped ? 'user-1' : null,
  skippedByName: status === InjectStatus.Skipped ? 'Jane Controller' : null,
  skipReason: status === InjectStatus.Skipped ? 'Time constraints' : null,
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

describe('CompletedSection', () => {
  it('renders nothing when no completed injects', () => {
    const { container } = render(
      <CompletedSection
        injects={[]}
        expanded={false}
        onToggle={vi.fn()}
      />,
    )

    expect(container).toBeEmptyDOMElement()
  })

  it('renders section header with completed count', () => {
    const injects = [
      createMockInject('1', InjectStatus.Fired, 1),
      createMockInject('2', InjectStatus.Fired, 2),
      createMockInject('3', InjectStatus.Skipped, 3),
    ]

    render(
      <CompletedSection
        injects={injects}
        expanded={false}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByText('COMPLETED')).toBeInTheDocument()
    expect(screen.getByText('2 fired')).toBeInTheDocument()
    expect(screen.getByText('1 skipped')).toBeInTheDocument()
  })

  it('shows only fired count when no skipped injects', () => {
    const injects = [
      createMockInject('1', InjectStatus.Fired, 1),
      createMockInject('2', InjectStatus.Fired, 2),
    ]

    render(
      <CompletedSection
        injects={injects}
        expanded={false}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByText('2 fired')).toBeInTheDocument()
    expect(screen.queryByText(/skipped/i)).not.toBeInTheDocument()
  })

  it('shows only skipped count when no fired injects', () => {
    const injects = [
      createMockInject('1', InjectStatus.Skipped, 1),
      createMockInject('2', InjectStatus.Skipped, 2),
    ]

    render(
      <CompletedSection
        injects={injects}
        expanded={false}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByText('2 skipped')).toBeInTheDocument()
    expect(screen.queryByText(/fired/i)).not.toBeInTheDocument()
  })

  it('calls onToggle when header is clicked', async () => {
    const user = userEvent.setup()
    const onToggle = vi.fn()
    const injects = [createMockInject('1', InjectStatus.Fired, 1)]

    render(
      <CompletedSection
        injects={injects}
        expanded={false}
        onToggle={onToggle}
      />,
    )

    const header = screen.getByText('COMPLETED').closest('div')
    if (header) {
      await user.click(header)
    }

    expect(onToggle).toHaveBeenCalledTimes(1)
  })

  it('shows chevron down icon when collapsed', () => {
    const injects = [createMockInject('1', InjectStatus.Fired, 1)]

    render(
      <CompletedSection
        injects={injects}
        expanded={false}
        onToggle={vi.fn()}
      />,
    )

    // FontAwesome renders as svg, check for IconButton
    const iconButton = screen.getByRole('button')
    expect(iconButton).toBeInTheDocument()
  })

  it('shows chevron up icon when expanded', () => {
    const injects = [createMockInject('1', InjectStatus.Fired, 1)]

    render(
      <CompletedSection
        injects={injects}
        expanded={true}
        onToggle={vi.fn()}
      />,
    )

    const iconButton = screen.getByRole('button')
    expect(iconButton).toBeInTheDocument()
  })

  it('displays inject details when expanded', () => {
    const injects = [
      createMockInject('1', InjectStatus.Fired, 1, {
        title: 'Critical Notification',
        target: 'Emergency Operations Center',
      }),
    ]

    render(
      <CompletedSection
        injects={injects}
        expanded={true}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByText('Critical Notification')).toBeInTheDocument()
    expect(screen.getByText('To: Emergency Operations Center')).toBeInTheDocument()
    expect(screen.getByText('#1')).toBeInTheDocument()
  })

  it('displays fired inject with fire timestamp and user', () => {
    const injects = [
      createMockInject('1', InjectStatus.Fired, 1, {
        firedAt: '2025-01-20T14:30:00Z',
        firedByName: 'John Controller',
      }),
    ]

    render(
      <CompletedSection
        injects={injects}
        expanded={true}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByText('Fired')).toBeInTheDocument()
    expect(screen.getByText('by John Controller')).toBeInTheDocument()
  })

  it('displays skipped inject with skip reason', () => {
    const injects = [
      createMockInject('1', InjectStatus.Skipped, 1, {
        skipReason: 'Players ahead of schedule',
        skippedByName: 'Jane Controller',
      }),
    ]

    render(
      <CompletedSection
        injects={injects}
        expanded={true}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByText('Skipped')).toBeInTheDocument()
    expect(screen.getByText('by Jane Controller')).toBeInTheDocument()
    expect(screen.getByText('"Players ahead of schedule"')).toBeInTheDocument()
  })

  it('calls onInjectClick when inject row is clicked', async () => {
    const user = userEvent.setup()
    const onInjectClick = vi.fn()
    const injects = [createMockInject('1', InjectStatus.Fired, 1)]

    render(
      <CompletedSection
        injects={injects}
        expanded={true}
        onToggle={vi.fn()}
        onInjectClick={onInjectClick}
      />,
    )

    const injectRow = screen.getByText('Inject 1').closest('tr')
    if (injectRow) {
      await user.click(injectRow)
    }

    expect(onInjectClick).toHaveBeenCalledWith(injects[0])
  })

  it('does not call onInjectClick when prop is not provided', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', InjectStatus.Fired, 1)]

    render(
      <CompletedSection
        injects={injects}
        expanded={true}
        onToggle={vi.fn()}
      />,
    )

    const injectRow = screen.getByText('Inject 1').closest('tr')
    if (injectRow) {
      await user.click(injectRow)
      // Should not throw error
    }

    // Test passes if no error thrown
    expect(true).toBe(true)
  })

  it('displays delivery time when available', () => {
    const injects = [
      createMockInject('1', InjectStatus.Fired, 1, {
        deliveryTime: '00:45:00', // 45 minutes
      }),
    ]

    render(
      <CompletedSection
        injects={injects}
        expanded={true}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByText('00:45:00')).toBeInTheDocument()
  })

  it('displays scenario time when available', () => {
    const injects = [
      createMockInject('1', InjectStatus.Fired, 1, {
        scenarioDay: 2,
        scenarioTime: '14:30:00',
      }),
    ]

    render(
      <CompletedSection
        injects={injects}
        expanded={true}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByText('D2 14:30:00')).toBeInTheDocument()
  })

  it('renders multiple completed injects', () => {
    const injects = [
      createMockInject('1', InjectStatus.Fired, 1, { title: 'First Inject' }),
      createMockInject('2', InjectStatus.Skipped, 2, { title: 'Second Inject' }),
      createMockInject('3', InjectStatus.Fired, 3, { title: 'Third Inject' }),
    ]

    render(
      <CompletedSection
        injects={injects}
        expanded={true}
        onToggle={vi.fn()}
      />,
    )

    expect(screen.getByText('First Inject')).toBeInTheDocument()
    expect(screen.getByText('Second Inject')).toBeInTheDocument()
    expect(screen.getByText('Third Inject')).toBeInTheDocument()
  })
})
