import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { StatusHistoryTimeline } from './StatusHistoryTimeline'
import type { InjectStatusHistoryDto } from '../types'

const mockHistory: InjectStatusHistoryDto[] = [
  {
    id: 'hist-1',
    injectId: 'inject-1',
    fromStatus: 'Draft',
    toStatus: 'Submitted',
    changedByUserId: 'user-1',
    changedByName: 'Jane Controller',
    changedAt: '2024-01-15T10:00:00Z',
    notes: null,
  },
  {
    id: 'hist-2',
    injectId: 'inject-1',
    fromStatus: 'Submitted',
    toStatus: 'Rejected',
    changedByUserId: 'user-2',
    changedByName: 'John Director',
    changedAt: '2024-01-15T11:30:00Z',
    notes: 'Missing target details',
  },
  {
    id: 'hist-3',
    injectId: 'inject-1',
    fromStatus: 'Draft',
    toStatus: 'Submitted',
    changedByUserId: 'user-1',
    changedByName: 'Jane Controller',
    changedAt: '2024-01-15T14:00:00Z',
    notes: null,
  },
]

describe('StatusHistoryTimeline', () => {
  it('renders nothing when history is empty', () => {
    const { container } = render(
      <StatusHistoryTimeline history={[]} />,
    )
    expect(container.firstChild).toBeNull()
  })

  it('renders nothing when loading', () => {
    const { container } = render(
      <StatusHistoryTimeline history={[]} loading={true} />,
    )
    expect(container.firstChild).toBeNull()
  })

  it('renders the count in the toggle header', () => {
    render(<StatusHistoryTimeline history={mockHistory} />)

    expect(screen.getByText('Status History (3)')).toBeInTheDocument()
  })

  it('starts collapsed by default', () => {
    render(<StatusHistoryTimeline history={mockHistory} />)

    // Notes should not be visible when collapsed
    expect(screen.queryByText('Missing target details')).not.toBeVisible()
  })

  it('expands to show entries when clicked', async () => {
    const user = userEvent.setup()
    render(<StatusHistoryTimeline history={mockHistory} />)

    // Click the header to expand
    await user.click(screen.getByText('Status History (3)'))

    // Status transitions should now be visible
    expect(screen.getAllByText(/Draft → Submitted/)).toHaveLength(2)
    expect(screen.getByText(/Submitted → Rejected/)).toBeVisible()
  })

  it('shows actor names and notes when expanded', async () => {
    const user = userEvent.setup()
    render(<StatusHistoryTimeline history={mockHistory} />)

    await user.click(screen.getByText('Status History (3)'))

    // Actor names should be visible
    expect(screen.getAllByText(/Jane Controller/)).toHaveLength(2)
    expect(screen.getByText(/John Director/)).toBeInTheDocument()

    // Notes should be visible
    expect(screen.getByText('Missing target details')).toBeVisible()
  })

  it('shows "System" when changedByName is null', async () => {
    const user = userEvent.setup()
    const historyWithNull: InjectStatusHistoryDto[] = [
      {
        id: 'hist-1',
        injectId: 'inject-1',
        fromStatus: 'Synchronized',
        toStatus: 'Released',
        changedByUserId: 'system',
        changedByName: null,
        changedAt: '2024-01-15T10:00:00Z',
        notes: null,
      },
    ]

    render(<StatusHistoryTimeline history={historyWithNull} />)
    await user.click(screen.getByText('Status History (1)'))

    expect(screen.getByText(/System/)).toBeInTheDocument()
  })

  it('collapses again when clicked a second time', async () => {
    const user = userEvent.setup()
    render(<StatusHistoryTimeline history={mockHistory} />)

    // Expand
    await user.click(screen.getByText('Status History (3)'))
    expect(screen.getByText('Missing target details')).toBeVisible()

    // Collapse
    await user.click(screen.getByText('Status History (3)'))
    // After collapsing, the expand button aria label should change back
    expect(screen.getByLabelText('Expand history')).toBeInTheDocument()
  })
})
