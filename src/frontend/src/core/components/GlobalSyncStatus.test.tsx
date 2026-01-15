/**
 * Tests for GlobalSyncStatus Component
 *
 * Tests the global sync status display and conflict dialog rendering.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { GlobalSyncStatus } from './GlobalSyncStatus'
import type { ConflictInfo } from '../offline'

// Mock the context
const mockClearConflicts = vi.fn()
const mockConflicts = vi.fn<() => ConflictInfo[]>().mockReturnValue([])

vi.mock('../contexts', () => ({
  useOfflineSyncContext: () => ({
    conflicts: mockConflicts(),
    clearConflicts: mockClearConflicts,
  }),
}))

// Mock the ConflictDialog component
vi.mock('./ConflictDialog', () => ({
  ConflictDialog: ({
    open,
    conflicts,
    onClose,
  }: {
    open: boolean
    conflicts: ConflictInfo[]
    onClose: () => void
  }) =>
    open ? (
      <div data-testid="conflict-dialog">
        <div data-testid="conflict-count">{conflicts.length}</div>
        <button data-testid="close-btn" onClick={onClose}>Close</button>
      </div>
    ) : null,
}))

describe('GlobalSyncStatus', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockConflicts.mockReturnValue([])
  })

  it('renders nothing when no conflicts', () => {
    mockConflicts.mockReturnValue([])

    render(<GlobalSyncStatus />)

    expect(screen.queryByTestId('conflict-dialog')).not.toBeInTheDocument()
  })

  it('renders ConflictDialog when conflicts exist', () => {
    mockConflicts.mockReturnValue([
      {
        actionId: 1,
        type: 'FIRE_INJECT',
        message: 'Conflict occurred',
      },
    ])

    render(<GlobalSyncStatus />)

    expect(screen.getByTestId('conflict-dialog')).toBeInTheDocument()
  })

  it('passes conflicts to ConflictDialog', () => {
    mockConflicts.mockReturnValue([
      { actionId: 1, type: 'FIRE_INJECT', message: 'Conflict 1' },
      { actionId: 2, type: 'SKIP_INJECT', message: 'Conflict 2' },
    ])

    render(<GlobalSyncStatus />)

    expect(screen.getByTestId('conflict-count')).toHaveTextContent('2')
  })

  it('calls clearConflicts when dialog is closed', async () => {
    const user = userEvent.setup()
    mockConflicts.mockReturnValue([
      { actionId: 1, type: 'FIRE_INJECT', message: 'Conflict' },
    ])

    render(<GlobalSyncStatus />)

    await user.click(screen.getByTestId('close-btn'))

    expect(mockClearConflicts).toHaveBeenCalled()
  })

  it('opens dialog when conflicts change from empty to non-empty', () => {
    mockConflicts.mockReturnValue([])
    const { rerender } = render(<GlobalSyncStatus />)

    expect(screen.queryByTestId('conflict-dialog')).not.toBeInTheDocument()

    mockConflicts.mockReturnValue([
      { actionId: 1, type: 'CREATE_OBSERVATION', message: 'Conflict' },
    ])
    rerender(<GlobalSyncStatus />)

    expect(screen.getByTestId('conflict-dialog')).toBeInTheDocument()
  })

  it('closes dialog when conflicts change from non-empty to empty', () => {
    mockConflicts.mockReturnValue([
      { actionId: 1, type: 'FIRE_INJECT', message: 'Conflict' },
    ])
    const { rerender } = render(<GlobalSyncStatus />)

    expect(screen.getByTestId('conflict-dialog')).toBeInTheDocument()

    mockConflicts.mockReturnValue([])
    rerender(<GlobalSyncStatus />)

    expect(screen.queryByTestId('conflict-dialog')).not.toBeInTheDocument()
  })
})
