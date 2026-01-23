/**
 * EditUserDialog Component Tests
 */
import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { EditUserDialog } from './EditUserDialog'
import type { UserDto } from '../types'

const mockUser: UserDto = {
  id: '123',
  email: 'jane@example.com',
  displayName: 'Jane Smith',
  systemRole: 'User',
  status: 'Active',
  lastLoginAt: '2025-01-20T14:30:00Z',
  createdAt: '2025-01-01T09:00:00Z',
}

describe('EditUserDialog', () => {
  it('renders with user data pre-filled', () => {
    render(<EditUserDialog user={mockUser} onClose={vi.fn()} onSave={vi.fn()} />)

    expect(screen.getByLabelText(/display name/i)).toHaveValue('Jane Smith')
    expect(screen.getByLabelText(/email/i)).toHaveValue('jane@example.com')
  })

  it('calls onClose when cancel button clicked', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()

    render(<EditUserDialog user={mockUser} onClose={onClose} onSave={vi.fn()} />)

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    expect(onClose).toHaveBeenCalled()
  })

  it('calls onSave with updated fields when save clicked', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn().mockResolvedValue(undefined)

    render(<EditUserDialog user={mockUser} onClose={vi.fn()} onSave={onSave} />)

    const displayNameInput = screen.getByLabelText(/display name/i)
    await user.clear(displayNameInput)
    await user.type(displayNameInput, 'Jane Doe')

    await user.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(onSave).toHaveBeenCalledWith({
        displayName: 'Jane Doe',
      })
    })
  })

  it('only includes changed fields in save request', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn().mockResolvedValue(undefined)

    render(<EditUserDialog user={mockUser} onClose={vi.fn()} onSave={onSave} />)

    // Only change display name
    const displayNameInput = screen.getByLabelText(/display name/i)
    await user.clear(displayNameInput)
    await user.type(displayNameInput, 'Jane Updated')

    await user.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(onSave).toHaveBeenCalledWith({
        displayName: 'Jane Updated',
      })
    })
  })

  it('shows loading state during save', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn().mockImplementation(() => new Promise(() => {})) // Never resolves

    render(<EditUserDialog user={mockUser} onClose={vi.fn()} onSave={onSave} />)

    await user.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /save/i })).toBeDisabled()
    })
  })

  it('displays error message on save failure', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn().mockRejectedValue({
      response: { data: { message: 'Email already in use' } },
    })

    render(<EditUserDialog user={mockUser} onClose={vi.fn()} onSave={onSave} />)

    await user.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(screen.getByText(/email already in use/i)).toBeInTheDocument()
    })
  })
})
