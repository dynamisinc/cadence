/**
 * ConflictDialog Tests
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '../../test/testUtils'
import { ConflictDialog } from './ConflictDialog'
import type { ConflictInfo } from '../offline'

describe('ConflictDialog', () => {
  const mockConflicts: ConflictInfo[] = [
    {
      actionId: 1,
      type: 'FIRE_INJECT',
      message: 'Inject was already fired by another user',
      conflictingUser: 'Jane Smith',
    },
    {
      actionId: 2,
      type: 'UPDATE_OBSERVATION',
      message: 'Observation was deleted while offline',
    },
  ]

  it('renders nothing when conflicts array is empty', () => {
    const { container } = render(
      <ConflictDialog open={true} conflicts={[]} onClose={vi.fn()} />,
    )
    expect(container.firstChild).toBeNull()
  })

  it('renders nothing when closed', () => {
    const { container } = render(
      <ConflictDialog open={false} conflicts={mockConflicts} onClose={vi.fn()} />,
    )
    // MUI Dialog should not render content when closed
    expect(screen.queryByTestId('conflict-dialog')).not.toBeInTheDocument()
  })

  it('renders conflicts when open', () => {
    render(
      <ConflictDialog open={true} conflicts={mockConflicts} onClose={vi.fn()} />,
    )

    expect(screen.getByTestId('conflict-dialog')).toBeInTheDocument()
    expect(screen.getByText('Sync Conflicts')).toBeInTheDocument()
    expect(screen.getByText('Fire Inject')).toBeInTheDocument()
    expect(screen.getByText('Update Observation')).toBeInTheDocument()
  })

  it('shows conflict details including user', () => {
    render(
      <ConflictDialog open={true} conflicts={mockConflicts} onClose={vi.fn()} />,
    )

    expect(
      screen.getByText(/Inject was already fired by another user/),
    ).toBeInTheDocument()
    expect(screen.getByText(/\(by Jane Smith\)/)).toBeInTheDocument()
  })

  it('calls onClose when OK button clicked', () => {
    const onClose = vi.fn()
    render(
      <ConflictDialog open={true} conflicts={mockConflicts} onClose={onClose} />,
    )

    fireEvent.click(screen.getByText('OK, I Understand'))
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('renders all action types with correct labels', () => {
    const allTypeConflicts: ConflictInfo[] = [
      { actionId: 1, type: 'FIRE_INJECT', message: 'Test' },
      { actionId: 2, type: 'SKIP_INJECT', message: 'Test' },
      { actionId: 3, type: 'RESET_INJECT', message: 'Test' },
      { actionId: 4, type: 'CREATE_OBSERVATION', message: 'Test' },
      { actionId: 5, type: 'UPDATE_OBSERVATION', message: 'Test' },
      { actionId: 6, type: 'DELETE_OBSERVATION', message: 'Test' },
    ]

    render(
      <ConflictDialog open={true} conflicts={allTypeConflicts} onClose={vi.fn()} />,
    )

    expect(screen.getByText('Fire Inject')).toBeInTheDocument()
    expect(screen.getByText('Skip Inject')).toBeInTheDocument()
    expect(screen.getByText('Reset Inject')).toBeInTheDocument()
    expect(screen.getByText('Create Observation')).toBeInTheDocument()
    expect(screen.getByText('Update Observation')).toBeInTheDocument()
    expect(screen.getByText('Delete Observation')).toBeInTheDocument()
  })
})
