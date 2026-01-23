/**
 * Tests for AddParticipantDialog component
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { AddParticipantDialog } from './AddParticipantDialog'
import { userService } from '../../users/services/userService'
import type { UserDto } from '../../users/types'

vi.mock('../../users/services/userService')

describe('AddParticipantDialog', () => {
  const mockUsers: UserDto[] = [
    {
      id: 'u1',
      email: 'john@example.com',
      displayName: 'John Doe',
      systemRole: 'User',
      status: 'Active',
      lastLoginAt: null,
      createdAt: '2025-01-20T12:00:00Z',
    },
    {
      id: 'u2',
      email: 'jane@example.com',
      displayName: 'Jane Smith',
      systemRole: 'Manager',
      status: 'Active',
      lastLoginAt: null,
      createdAt: '2025-01-20T12:00:00Z',
    },
  ]

  const mockHandlers = {
    onAdd: vi.fn(),
    onClose: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(userService.getUsers).mockResolvedValue({
      users: mockUsers,
      pagination: {
        page: 1,
        pageSize: 100,
        totalCount: 2,
        totalPages: 1,
      },
    })
  })

  it('renders dialog title', () => {
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    expect(screen.getByText('Add Participant')).toBeInTheDocument()
  })

  it('does not render when closed', () => {
    render(<AddParticipantDialog open={false} {...mockHandlers} />)

    expect(screen.queryByText('Add Participant')).not.toBeInTheDocument()
  })

  it('loads and displays users in autocomplete', async () => {
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    const autocomplete = screen.getByLabelText(/select user/i)
    await userEvent.click(autocomplete)

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    })
  })

  it('shows user system role in autocomplete options', async () => {
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    const autocomplete = screen.getByLabelText(/select user/i)
    await userEvent.click(autocomplete)

    await waitFor(() => {
      expect(screen.getByText(/User/)).toBeInTheDocument()
      expect(screen.getByText(/Manager/)).toBeInTheDocument()
    })
  })

  it('shows exercise role selector', () => {
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    expect(screen.getByLabelText(/exercise role/i)).toBeInTheDocument()
  })

  it('defaults to Observer role', () => {
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    const roleSelect = screen.getByLabelText(/exercise role/i)
    expect(roleSelect).toHaveValue('Observer')
  })

  it('allows selecting different exercise roles', async () => {
    const user = userEvent.setup()
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    const roleSelect = screen.getByLabelText(/exercise role/i)
    await user.selectOptions(roleSelect, 'Controller')

    expect(roleSelect).toHaveValue('Controller')
  })

  it('disables add button when no user selected', () => {
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    const addButton = screen.getByRole('button', { name: /add participant/i })
    expect(addButton).toBeDisabled()
  })

  it('enables add button when user is selected', async () => {
    const user = userEvent.setup()
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    const autocomplete = screen.getByLabelText(/select user/i)
    await user.click(autocomplete)

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })

    await user.click(screen.getByText('John Doe'))

    const addButton = screen.getByRole('button', { name: /add participant/i })
    expect(addButton).toBeEnabled()
  })

  it('calls onAdd with selected user and role', async () => {
    const user = userEvent.setup()
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    // Select user
    const autocomplete = screen.getByLabelText(/select user/i)
    await user.click(autocomplete)
    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })
    await user.click(screen.getByText('John Doe'))

    // Select role
    const roleSelect = screen.getByLabelText(/exercise role/i)
    await user.selectOptions(roleSelect, 'Evaluator')

    // Click add
    const addButton = screen.getByRole('button', { name: /add participant/i })
    await user.click(addButton)

    expect(mockHandlers.onAdd).toHaveBeenCalledWith({
      userId: 'u1',
      role: 'Evaluator',
    })
  })

  it('calls onClose when cancel is clicked', async () => {
    const user = userEvent.setup()
    render(<AddParticipantDialog open={true} {...mockHandlers} />)

    const cancelButton = screen.getByRole('button', { name: /cancel/i })
    await user.click(cancelButton)

    expect(mockHandlers.onClose).toHaveBeenCalled()
  })

  it('resets form when dialog is closed and reopened', async () => {
    const user = userEvent.setup()
    const { rerender } = render(<AddParticipantDialog open={true} {...mockHandlers} />)

    // Select a user
    const autocomplete = screen.getByLabelText(/select user/i)
    await user.click(autocomplete)
    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })
    await user.click(screen.getByText('John Doe'))

    // Close dialog
    rerender(<AddParticipantDialog open={false} {...mockHandlers} />)

    // Reopen dialog
    rerender(<AddParticipantDialog open={true} {...mockHandlers} />)

    // Form should be reset
    const addButton = screen.getByRole('button', { name: /add participant/i })
    expect(addButton).toBeDisabled()
  })
})
