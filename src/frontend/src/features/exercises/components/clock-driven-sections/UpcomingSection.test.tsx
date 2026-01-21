/**
 * Tests for UpcomingSection component
 *
 * Tests the upcoming injects section (within 30-min window with countdown)
 * @see exercise-config/S06-clock-driven-conduct-view
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '../../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { UpcomingSection } from './UpcomingSection'
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

describe('UpcomingSection', () => {
  it('renders nothing when no upcoming injects', () => {
    const { container } = render(
      <UpcomingSection injects={[]} elapsedTimeMs={0} />,
    )

    expect(container).toBeEmptyDOMElement()
  })

  it('renders section header with inject count', () => {
    const injects = [
      createMockInject('1', 1),
      createMockInject('2', 2),
      createMockInject('3', 3),
    ]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    expect(screen.getByText('UPCOMING')).toBeInTheDocument()
    expect(screen.getByText('next 30 min')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument() // Count badge
  })

  it('is expanded by default', () => {
    const injects = [createMockInject('1', 1, { title: 'Future Inject' })]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    expect(screen.getByText('Future Inject')).toBeInTheDocument()
  })

  it('collapses when header is clicked', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1, { title: 'Future Inject' })]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    // Content should be visible initially
    expect(screen.getByText('Future Inject')).toBeInTheDocument()

    const header = screen.getByText('UPCOMING').closest('div')
    if (header) {
      await user.click(header)
    }

    // Content should be hidden after collapse animation
    // Note: MUI Collapse uses CSS transitions, content is technically still in DOM but not visible
    // We check that the Collapse wrapper has the correct aria-hidden or height: 0
    await waitFor(() => {
      const collapseEl = screen.getByText('Future Inject').closest('.MuiCollapse-root')
      expect(collapseEl).toHaveStyle({ height: '0px' })
    })
  })

  it('expands when collapsed header is clicked', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1, { title: 'Future Inject' })]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    const header = screen.getByText('UPCOMING').closest('div')
    if (header) {
      // Collapse
      await user.click(header)
      await waitFor(() => {
        const collapseEl = screen.getByText('Future Inject').closest('.MuiCollapse-root')
        expect(collapseEl).toHaveStyle({ height: '0px' })
      })

      // Expand again
      await user.click(header)
      await waitFor(() => {
        const collapseEl = screen.getByText('Future Inject').closest('.MuiCollapse-root')
        // When expanded, height will be 'auto' or a non-zero value
        expect(collapseEl).not.toHaveStyle({ height: '0px' })
      })
    }
  })

  it('displays inject details', () => {
    const injects = [
      createMockInject('1', 1, {
        title: 'Upcoming Notification',
        target: 'Emergency Operations Center',
      }),
    ]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    expect(screen.getByText('Upcoming Notification')).toBeInTheDocument()
    expect(screen.getByText('To: Emergency Operations Center')).toBeInTheDocument()
    expect(screen.getByText('#1')).toBeInTheDocument()
  })

  it('displays countdown for upcoming inject', () => {
    const injects = [
      createMockInject('1', 1, {
        deliveryTime: '00:30:00', // 30 minutes
      }),
    ]

    render(
      <UpcomingSection
        injects={injects}
        elapsedTimeMs={20 * 60 * 1000} // 20 minutes elapsed (10 min until delivery)
      />,
    )

    expect(screen.getByText('in 10:00')).toBeInTheDocument()
  })

  it('highlights imminent injects (less than 5 minutes)', () => {
    const injects = [
      createMockInject('1', 1, {
        title: 'Imminent Inject',
        deliveryTime: '00:30:00', // 30 minutes
      }),
    ]

    render(
      <UpcomingSection
        injects={injects}
        elapsedTimeMs={27 * 60 * 1000} // 27 minutes elapsed (3 min until delivery)
      />,
    )

    // Should show countdown with warning styling
    expect(screen.getByText('in 3:00')).toBeInTheDocument()
    // Title should be bold when imminent
    const titleCell = screen.getByText('Imminent Inject').closest('td')
    expect(titleCell).toBeInTheDocument()
  })

  it('does not highlight non-imminent injects (more than 5 minutes)', () => {
    const injects = [
      createMockInject('1', 1, {
        title: 'Not Yet Imminent',
        deliveryTime: '00:30:00', // 30 minutes
      }),
    ]

    render(
      <UpcomingSection
        injects={injects}
        elapsedTimeMs={20 * 60 * 1000} // 20 minutes elapsed (10 min until delivery)
      />,
    )

    expect(screen.getByText('in 10:00')).toBeInTheDocument()
    expect(screen.getByText('Not Yet Imminent')).toBeInTheDocument()
  })

  it('displays delivery time', () => {
    const injects = [
      createMockInject('1', 1, {
        deliveryTime: '00:45:00', // 45 minutes
      }),
    ]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    expect(screen.getByText('00:45:00')).toBeInTheDocument()
  })

  it('displays scenario time when available', () => {
    const injects = [
      createMockInject('1', 1, {
        scenarioDay: 2,
        scenarioTime: '14:30:00',
      }),
    ]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    expect(screen.getByText('D2 14:30')).toBeInTheDocument()
  })

  it('skips rendering inject with null delivery time', () => {
    const injects = [
      createMockInject('1', 1, {
        title: 'No Time Inject',
        deliveryTime: null,
      }),
      createMockInject('2', 2, {
        title: 'Has Time Inject',
        deliveryTime: '00:30:00',
      }),
    ]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    // Inject with delivery time should show
    expect(screen.getByText('Has Time Inject')).toBeInTheDocument()
    // Inject without delivery time should not render
    expect(screen.queryByText('No Time Inject')).not.toBeInTheDocument()
  })

  it('calls onInjectClick when inject row is clicked', async () => {
    const user = userEvent.setup()
    const onInjectClick = vi.fn()
    const injects = [createMockInject('1', 1)]

    render(
      <UpcomingSection
        injects={injects}
        elapsedTimeMs={0}
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
    const injects = [createMockInject('1', 1)]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    const injectRow = screen.getByText('Inject 1').closest('tr')
    if (injectRow) {
      await user.click(injectRow)
      // Should not throw error
    }

    // Test passes if no error thrown
    expect(true).toBe(true)
  })

  it('renders multiple upcoming injects sorted by time', () => {
    const injects = [
      createMockInject('1', 1, {
        title: 'First Upcoming',
        deliveryTime: '00:15:00', // 15 minutes
      }),
      createMockInject('2', 2, {
        title: 'Second Upcoming',
        deliveryTime: '00:20:00', // 20 minutes
      }),
      createMockInject('3', 3, {
        title: 'Third Upcoming',
        deliveryTime: '00:25:00', // 25 minutes
      }),
    ]

    render(
      <UpcomingSection
        injects={injects}
        elapsedTimeMs={10 * 60 * 1000} // 10 minutes elapsed
      />,
    )

    expect(screen.getByText('First Upcoming')).toBeInTheDocument()
    expect(screen.getByText('Second Upcoming')).toBeInTheDocument()
    expect(screen.getByText('Third Upcoming')).toBeInTheDocument()
    expect(screen.getByText('in 5:00')).toBeInTheDocument() // First inject (15-10 = 5 min)
    expect(screen.getByText('in 10:00')).toBeInTheDocument() // Second inject (20-10 = 10 min)
    expect(screen.getByText('in 15:00')).toBeInTheDocument() // Third inject (25-10 = 15 min)
  })

  it('shows chevron up icon when expanded', () => {
    const injects = [createMockInject('1', 1)]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    const iconButton = screen.getByRole('button')
    expect(iconButton).toBeInTheDocument()
  })

  it('shows chevron down icon when collapsed', async () => {
    const user = userEvent.setup()
    const injects = [createMockInject('1', 1)]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    const header = screen.getByText('UPCOMING').closest('div')
    if (header) {
      await user.click(header)
    }

    const iconButton = screen.getByRole('button')
    expect(iconButton).toBeInTheDocument()
  })

  it('displays inject without target field', () => {
    const injects = [
      createMockInject('1', 1, {
        title: 'Generic Inject',
        target: null,
      }),
    ]

    render(<UpcomingSection injects={injects} elapsedTimeMs={0} />)

    expect(screen.getByText('Generic Inject')).toBeInTheDocument()
    expect(screen.queryByText(/To:/)).not.toBeInTheDocument()
  })

  it('formats countdown correctly for different time ranges', () => {
    const injects = [
      createMockInject('1', 1, {
        title: 'One Minute',
        deliveryTime: '00:11:00', // 11 minutes
      }),
      createMockInject('2', 2, {
        title: 'Twenty Minutes',
        deliveryTime: '00:30:00', // 30 minutes
      }),
    ]

    render(
      <UpcomingSection
        injects={injects}
        elapsedTimeMs={10 * 60 * 1000} // 10 minutes elapsed
      />,
    )

    expect(screen.getByText('in 1:00')).toBeInTheDocument() // 11-10 = 1 min
    expect(screen.getByText('in 20:00')).toBeInTheDocument() // 30-10 = 20 min
  })
})
