import { describe, it, expect, vi } from 'vitest'
import { screen, fireEvent, waitFor } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { PhaseHeader } from './PhaseHeader'
import type { PhaseDto } from '../types'

const mockPhase: PhaseDto = {
  id: 'phase-1',
  name: 'Initial Response',
  description: 'First phase of the exercise',
  sequence: 1,
  startTime: null,
  endTime: null,
  exerciseId: 'exercise-1',
  injectCount: 0,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
}

const mockPhaseWithInjects: PhaseDto = {
  ...mockPhase,
  injectCount: 3,
}

describe('PhaseHeader', () => {
  const defaultProps = {
    phase: mockPhase,
    isFirst: false,
    isLast: false,
    canEdit: true,
    onEdit: vi.fn(),
    onDelete: vi.fn(),
    onMoveUp: vi.fn(),
    onMoveDown: vi.fn(),
    isLoading: false,
  }

  it('renders phase name', () => {
    render(<PhaseHeader {...defaultProps} />)
    expect(screen.getByText('Initial Response')).toBeInTheDocument()
  })

  it('renders phase description when provided', () => {
    render(<PhaseHeader {...defaultProps} />)
    expect(screen.getByText('First phase of the exercise')).toBeInTheDocument()
  })

  it('does not render description when not provided', () => {
    const phaseWithoutDescription = { ...mockPhase, description: null }
    render(<PhaseHeader {...defaultProps} phase={phaseWithoutDescription} />)
    expect(screen.queryByText('First phase of the exercise')).not.toBeInTheDocument()
  })

  it('hides controls when canEdit is false', () => {
    render(<PhaseHeader {...defaultProps} canEdit={false} />)
    // Edit button should not be present
    expect(screen.queryByRole('button', { name: /edit phase/i })).not.toBeInTheDocument()
  })

  it('shows controls when canEdit is true', () => {
    render(<PhaseHeader {...defaultProps} canEdit={true} />)
    // Edit button should be present (within tooltip)
    expect(screen.getByRole('button', { name: /edit phase/i })).toBeInTheDocument()
  })

  it('disables move up button when isFirst is true', () => {
    render(<PhaseHeader {...defaultProps} isFirst={true} />)
    const buttons = screen.getAllByRole('button')
    // First button (move up) should be disabled
    expect(buttons[0]).toBeDisabled()
  })

  it('disables move down button when isLast is true', () => {
    render(<PhaseHeader {...defaultProps} isLast={true} />)
    const buttons = screen.getAllByRole('button')
    // Second button (move down) should be disabled
    expect(buttons[1]).toBeDisabled()
  })

  it('disables delete button when phase has injects', () => {
    render(<PhaseHeader {...defaultProps} phase={mockPhaseWithInjects} />)
    // Last button is the delete button
    const allButtons = screen.getAllByRole('button')
    const deleteButton = allButtons[allButtons.length - 1]
    expect(deleteButton).toBeDisabled()
  })

  it('calls onEdit when edit button is clicked', () => {
    const onEdit = vi.fn()
    render(<PhaseHeader {...defaultProps} onEdit={onEdit} />)

    const editButton = screen.getByRole('button', { name: /edit phase/i })
    fireEvent.click(editButton)

    expect(onEdit).toHaveBeenCalledWith(mockPhase)
  })

  it('calls onMoveUp when move up button is clicked', () => {
    const onMoveUp = vi.fn()
    render(<PhaseHeader {...defaultProps} onMoveUp={onMoveUp} />)

    // First button in the controls is move up
    const buttons = screen.getAllByRole('button')
    fireEvent.click(buttons[0])

    expect(onMoveUp).toHaveBeenCalledWith('phase-1')
  })

  it('calls onMoveDown when move down button is clicked', () => {
    const onMoveDown = vi.fn()
    render(<PhaseHeader {...defaultProps} onMoveDown={onMoveDown} />)

    // Second button in the controls is move down
    const buttons = screen.getAllByRole('button')
    fireEvent.click(buttons[1])

    expect(onMoveDown).toHaveBeenCalledWith('phase-1')
  })

  it('shows delete confirmation dialog when delete is clicked', () => {
    render(<PhaseHeader {...defaultProps} />)

    // Find and click delete button (last button with non-disabled state)
    const buttons = screen.getAllByRole('button')
    const deleteButton = buttons[buttons.length - 1]
    fireEvent.click(deleteButton)

    // Dialog should appear
    expect(screen.getByText('Delete Phase?')).toBeInTheDocument()
    expect(screen.getByText(/Are you sure you want to delete/)).toBeInTheDocument()
  })

  it('calls onDelete when delete is confirmed', () => {
    const onDelete = vi.fn()
    render(<PhaseHeader {...defaultProps} onDelete={onDelete} />)

    // Open dialog
    const buttons = screen.getAllByRole('button')
    const deleteButton = buttons[buttons.length - 1]
    fireEvent.click(deleteButton)

    // Click confirm
    const confirmButton = screen.getByRole('button', { name: /delete$/i })
    fireEvent.click(confirmButton)

    expect(onDelete).toHaveBeenCalledWith(mockPhase)
  })

  it('closes dialog when cancel is clicked', async () => {
    render(<PhaseHeader {...defaultProps} />)

    // Open dialog
    const buttons = screen.getAllByRole('button')
    const deleteButton = buttons[buttons.length - 1]
    fireEvent.click(deleteButton)

    // Wait for dialog to appear
    await waitFor(() => {
      expect(screen.getByText('Delete Phase?')).toBeInTheDocument()
    })

    // Click cancel
    const cancelButton = screen.getByRole('button', { name: /cancel/i })
    fireEvent.click(cancelButton)

    // Dialog should be gone (wait for animation)
    await waitFor(() => {
      expect(screen.queryByText('Delete Phase?')).not.toBeInTheDocument()
    })
  })

  it('disables all buttons when isLoading is true', () => {
    render(<PhaseHeader {...defaultProps} isLoading={true} />)

    const buttons = screen.getAllByRole('button')
    // All 4 buttons should be disabled when loading
    expect(buttons.length).toBe(4)
    expect(buttons[0]).toBeDisabled() // Move up
    expect(buttons[1]).toBeDisabled() // Move down
    expect(buttons[2]).toBeDisabled() // Edit
    expect(buttons[3]).toBeDisabled() // Delete
  })
})
