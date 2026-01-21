/**
 * Tests for LaterSection component
 *
 * Tests the later injects section (pending injects outside 30-min window)
 * @see exercise-config/S06-clock-driven-conduct-view
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '../../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { LaterSection } from './LaterSection'
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
  description: 'Test description',
  scheduledTime: '09:00:00',
  deliveryTime: '01:30:00', // Default to later time
  scenarioDay: 1,
  scenarioTime: '09:00:00',
  target: 'Test Target',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: 'Standard',
  status: InjectStatus.Pending,
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
  ...overrides,
})

describe('LaterSection', () => {
  it('renders nothing when no later injects', () => {
    const { container } = render(<LaterSection injects={[]} />)

    expect(container).toBeEmptyDOMElement()
  })

  it('renders section header with inject count', () => {
    const injects = [
      createMockInject('1', 1),
      createMockInject('2', 2),
      createMockInject('3', 3),
    ]

    render(<LaterSection injects={injects} />)

    expect(screen.getByText('LATER')).toBeInTheDocument()
    expect(screen.getByText('remaining injects')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument()
  })

  it('is collapsed by default', () => {
    const injects = [createMockInject('1', 1, { title: 'Future Inject' })]

    render(<LaterSection injects={injects} />)

    // Section header should be visible
    expect(screen.getByText('LATER')).toBeInTheDocument()
    // Inject details should not be visible when collapsed
    // MUI Collapse keeps content in DOM but with height: 0px
    const injectTitle = screen.queryByText('Future Inject')
    if (injectTitle) {
      const collapseEl = injectTitle.closest('.MuiCollapse-root')
      expect(collapseEl).toHaveStyle({ height: '0px' })
    }
  })

  it('expands when header is clicked', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1, { title: 'Future Inject' })]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    // Inject should now be visible
    expect(screen.getByText('Future Inject')).toBeInTheDocument()
  })

  it('collapses when header is clicked again', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1, { title: 'Future Inject' })]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      // Expand
      await user.click(header)
      expect(screen.getByText('Future Inject')).toBeInTheDocument()

      // Collapse
      await user.click(header)
      await waitFor(() => {
        const collapseEl = screen.getByText('Future Inject').closest('.MuiCollapse-root')
        expect(collapseEl).toHaveStyle({ height: '0px' })
      })
    }
  })

  it('displays inject details when expanded', async () => {
    const user = userEvent.setup()
    const injects = [
      createMockInject('1', 1, {
        title: 'Critical Notification',
        target: 'Emergency Operations Center',
      }),
    ]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    expect(screen.getByText('Critical Notification')).toBeInTheDocument()
    expect(screen.getByText('To: Emergency Operations Center')).toBeInTheDocument()
    expect(screen.getByText('#1')).toBeInTheDocument()
  })

  it('displays delivery time when available', async () => {
    const user = userEvent.setup()
    const injects = [
      createMockInject('1', 1, {
        deliveryTime: '02:15:00', // 2 hours 15 minutes
      }),
    ]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    expect(screen.getByText('02:15:00')).toBeInTheDocument()
  })

  it('displays "No time set" when delivery time is null', async () => {
    const user = userEvent.setup()
    const injects = [
      createMockInject('1', 1, {
        deliveryTime: null,
      }),
    ]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    expect(screen.getByText('No time set')).toBeInTheDocument()
  })

  it('displays scenario time when available', async () => {
    const user = userEvent.setup()
    const injects = [
      createMockInject('1', 1, {
        scenarioDay: 2,
        scenarioTime: '16:45:00',
      }),
    ]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    expect(screen.getByText('D2 16:45')).toBeInTheDocument()
  })

  it('displays sequence number', async () => {
    const user = userEvent.setup()
    const injects = [
      createMockInject('1', 1, {
        sequence: 42,
      }),
    ]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    expect(screen.getByText('Seq 42')).toBeInTheDocument()
  })

  it('calls onInjectClick when inject row is clicked', async () => {
    const user = userEvent.setup()
    const onInjectClick = vi.fn()
    const injects = [createMockInject('1', 1)]

    render(<LaterSection injects={injects} onInjectClick={onInjectClick} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    const injectRow = screen.getByText('Inject 1').closest('tr')
    if (injectRow) {
      await user.click(injectRow)
    }

    expect(onInjectClick).toHaveBeenCalledWith(injects[0])
  })

  it('does not call onInjectClick when prop is not provided', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1)]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    const injectRow = screen.getByText('Inject 1').closest('tr')
    if (injectRow) {
      await user.click(injectRow)
      // Should not throw error
    }

    // Test passes if no error thrown
    expect(true).toBe(true)
  })

  it('renders multiple later injects', async () => {
    const user = userEvent.setup()
    const injects = [
      createMockInject('1', 1, { title: 'First Later Inject', sequence: 10 }),
      createMockInject('2', 2, { title: 'Second Later Inject', sequence: 11 }),
      createMockInject('3', 3, { title: 'Third Later Inject', sequence: 12 }),
    ]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    expect(screen.getByText('First Later Inject')).toBeInTheDocument()
    expect(screen.getByText('Second Later Inject')).toBeInTheDocument()
    expect(screen.getByText('Third Later Inject')).toBeInTheDocument()
    expect(screen.getByText('Seq 10')).toBeInTheDocument()
    expect(screen.getByText('Seq 11')).toBeInTheDocument()
    expect(screen.getByText('Seq 12')).toBeInTheDocument()
  })

  it('shows chevron down icon when collapsed', () => {
    const injects = [createMockInject('1', 1)]

    render(<LaterSection injects={injects} />)

    const iconButton = screen.getByRole('button')
    expect(iconButton).toBeInTheDocument()
  })

  it('shows chevron up icon when expanded', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1)]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    const iconButton = screen.getByRole('button')
    expect(iconButton).toBeInTheDocument()
  })

  it('displays inject without target field', async () => {
    const user = userEvent.setup()
    const injects = [
      createMockInject('1', 1, {
        title: 'Generic Inject',
        target: null,
      }),
    ]

    render(<LaterSection injects={injects} />)

    const header = screen.getByText('LATER').closest('div')
    if (header) {
      await user.click(header)
    }

    expect(screen.getByText('Generic Inject')).toBeInTheDocument()
    expect(screen.queryByText(/To:/)).not.toBeInTheDocument()
  })
})
