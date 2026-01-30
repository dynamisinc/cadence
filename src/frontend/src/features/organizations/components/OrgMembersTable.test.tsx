/**
 * OrgMembersTable Tests
 *
 * Tests the organization members table component.
 *
 * @module features/organizations/components
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@/test/testUtils'
import userEvent from '@testing-library/user-event'
import { OrgMembersTable } from './OrgMembersTable'
import type { OrgMember, OrgRole } from '../types'

// Mock ConfirmDialog
vi.mock('@/shared/components/ConfirmDialog', () => ({
  ConfirmDialog: ({ open, title, message, onConfirm, onCancel }: any) =>
    open ? (
      <div role="dialog" aria-labelledby="confirm-title">
        <h2 id="confirm-title">{title}</h2>
        <p>{message}</p>
        <button onClick={onConfirm}>Confirm</button>
        <button onClick={onCancel}>Cancel</button>
      </div>
    ) : null,
}))

describe('OrgMembersTable', () => {
  const mockOnAddClick = vi.fn()
  const mockOnRoleChange = vi.fn()
  const mockOnRemove = vi.fn()

  const mockMembers: OrgMember[] = [
    {
      membershipId: 'mem-1',
      userId: 'user-1',
      email: 'admin@example.com',
      displayName: 'Admin User',
      role: 'OrgAdmin',
      joinedAt: '2024-01-01T00:00:00Z',
    },
    {
      membershipId: 'mem-2',
      userId: 'user-2',
      email: 'manager@example.com',
      displayName: 'Manager User',
      role: 'OrgManager',
      joinedAt: '2024-01-02T00:00:00Z',
    },
    {
      membershipId: 'mem-3',
      userId: 'user-3',
      email: 'user@example.com',
      displayName: 'Regular User',
      role: 'OrgUser',
      joinedAt: '2024-01-03T00:00:00Z',
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Rendering', () => {
    it('renders table with members', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      expect(screen.getByText('Admin User')).toBeInTheDocument()
      expect(screen.getByText('Manager User')).toBeInTheDocument()
      expect(screen.getByText('Regular User')).toBeInTheDocument()
    })

    it('displays member count in header', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      expect(screen.getByText('Members (3)')).toBeInTheDocument()
    })

    it('shows Add Member button', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      expect(screen.getByRole('button', { name: /add member/i })).toBeInTheDocument()
    })

    it('displays empty state when no members', () => {
      render(
        <OrgMembersTable
          members={[]}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      expect(screen.getByText(/no members in this organization/i)).toBeInTheDocument()
    })

    it('renders table headers', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      expect(screen.getByRole('columnheader', { name: /name/i })).toBeInTheDocument()
      expect(screen.getByRole('columnheader', { name: /email/i })).toBeInTheDocument()
      expect(screen.getByRole('columnheader', { name: /role/i })).toBeInTheDocument()
      expect(screen.getByRole('columnheader', { name: /joined/i })).toBeInTheDocument()
      expect(screen.getByRole('columnheader', { name: /actions/i })).toBeInTheDocument()
    })

    it('displays member email addresses', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      expect(screen.getByText('admin@example.com')).toBeInTheDocument()
      expect(screen.getByText('manager@example.com')).toBeInTheDocument()
      expect(screen.getByText('user@example.com')).toBeInTheDocument()
    })

    it('displays formatted join dates', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      // Join dates should be formatted as locale date strings
      const date1 = new Date('2024-01-01T00:00:00Z').toLocaleDateString()
      const date2 = new Date('2024-01-02T00:00:00Z').toLocaleDateString()
      const date3 = new Date('2024-01-03T00:00:00Z').toLocaleDateString()

      expect(screen.getByText(date1)).toBeInTheDocument()
      expect(screen.getByText(date2)).toBeInTheDocument()
      expect(screen.getByText(date3)).toBeInTheDocument()
    })

    it('shows hyphen when displayName is missing', () => {
      const memberWithoutName: OrgMember = {
        ...mockMembers[0],
        displayName: '',
      }

      render(
        <OrgMembersTable
          members={[memberWithoutName]}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      // Find the cell in the Name column
      const rows = screen.getAllByRole('row')
      const dataRow = rows[1] // Skip header row
      const cells = within(dataRow).getAllByRole('cell')
      expect(cells[0]).toHaveTextContent('-')
    })
  })

  describe('Add Member Button', () => {
    it('calls onAddClick when Add Member button is clicked', async () => {
      const user = userEvent.setup()
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      expect(mockOnAddClick).toHaveBeenCalledTimes(1)
    })
  })

  describe('Role Management', () => {
    it('displays role select for each member', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      // Verify role selects are present by checking the Role column values
      expect(screen.getByText('Admin')).toBeInTheDocument()
      expect(screen.getByText('Manager')).toBeInTheDocument()
      expect(screen.getByText('User')).toBeInTheDocument()
    })

    it('calls onRoleChange when role is changed', async () => {
      const user = userEvent.setup()
      mockOnRoleChange.mockResolvedValue(undefined)

      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      // Find all comboboxes (role selects)
      const roleSelects = screen.getAllByRole('combobox')
      expect(roleSelects.length).toBeGreaterThan(0)

      // Click the first role select
      await user.click(roleSelects[0])

      // Wait for the dropdown to open and click Manager
      const managerOption = await screen.findByRole('option', { name: /manager/i })
      await user.click(managerOption)

      await waitFor(() => {
        expect(mockOnRoleChange).toHaveBeenCalledWith('mem-1', 'OrgManager')
      })
    })

    it('disables role select when isLoading is true', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
          isLoading={true}
        />
      )

      const roleSelects = screen.getAllByRole('combobox')
      roleSelects.forEach((select) => {
        // MUI Select uses aria-disabled instead of disabled attribute
        expect(select).toHaveAttribute('aria-disabled', 'true')
      })
    })

    it('shows error when role change fails', async () => {
      const user = userEvent.setup()
      const error = {
        response: {
          data: {
            message: 'Cannot change role',
          },
        },
      }
      mockOnRoleChange.mockRejectedValue(error)

      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      const roleSelects = screen.getAllByRole('combobox')
      await user.click(roleSelects[0])

      const managerOption = await screen.findByRole('option', { name: /manager/i })
      await user.click(managerOption)

      expect(await screen.findByText('Cannot change role')).toBeInTheDocument()
    })

    it('shows generic error when role change fails without message', async () => {
      const user = userEvent.setup()
      mockOnRoleChange.mockRejectedValue(new Error('Network error'))

      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      const roleSelects = screen.getAllByRole('combobox')
      await user.click(roleSelects[0])

      const managerOption = await screen.findByRole('option', { name: /manager/i })
      await user.click(managerOption)

      expect(await screen.findByText(/failed to update role/i)).toBeInTheDocument()
    })
  })

  describe('Member Removal', () => {
    it('shows remove button for each member', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      const removeButtons = screen.getAllByTitle(/remove member/i)
      expect(removeButtons).toHaveLength(mockMembers.length)
    })

    it('opens confirmation dialog when remove button is clicked', async () => {
      const user = userEvent.setup()
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      const removeButtons = screen.getAllByTitle(/remove member/i)
      await user.click(removeButtons[0])

      expect(screen.getByRole('dialog')).toBeInTheDocument()
      expect(screen.getByText(/remove member/i)).toBeInTheDocument()
      // Use getAllByText since "Admin User" appears both in table and dialog
      expect(screen.getAllByText(/admin user/i).length).toBeGreaterThan(0)
    })

    it('calls onRemove when removal is confirmed', async () => {
      const user = userEvent.setup()
      mockOnRemove.mockResolvedValue(undefined)

      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      // Click remove button
      const removeButtons = screen.getAllByTitle(/remove member/i)
      await user.click(removeButtons[0])

      // Confirm removal
      const confirmButton = screen.getByRole('button', { name: /confirm/i })
      await user.click(confirmButton)

      await waitFor(() => {
        expect(mockOnRemove).toHaveBeenCalledWith('mem-1', 'Admin User')
      })
    })

    it('uses email as fallback name in confirmation', async () => {
      const user = userEvent.setup()
      const memberWithoutName: OrgMember = {
        ...mockMembers[0],
        displayName: '',
      }

      render(
        <OrgMembersTable
          members={[memberWithoutName]}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      const removeButton = screen.getByTitle(/remove member/i)
      await user.click(removeButton)

      // Check the dialog contains the email
      const dialog = screen.getByRole('dialog')
      expect(within(dialog).getByText(/admin@example\.com/i)).toBeInTheDocument()
    })

    it('closes dialog when removal is cancelled', async () => {
      const user = userEvent.setup()
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      // Open dialog
      const removeButtons = screen.getAllByTitle(/remove member/i)
      await user.click(removeButtons[0])

      expect(screen.getByRole('dialog')).toBeInTheDocument()

      // Cancel
      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      })
    })

    it('shows error when removal fails', async () => {
      const user = userEvent.setup()
      const error = {
        response: {
          data: {
            message: 'Cannot remove member',
          },
        },
      }
      mockOnRemove.mockRejectedValue(error)

      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      // Click remove and confirm
      const removeButtons = screen.getAllByTitle(/remove member/i)
      await user.click(removeButtons[0])

      const confirmButton = screen.getByRole('button', { name: /confirm/i })
      await user.click(confirmButton)

      expect(await screen.findByText('Cannot remove member')).toBeInTheDocument()
    })

    it('disables remove buttons when isLoading is true', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
          isLoading={true}
        />
      )

      const removeButtons = screen.getAllByTitle(/remove member/i)
      removeButtons.forEach((button) => {
        expect(button).toBeDisabled()
      })
    })
  })

  describe('Error Handling', () => {
    it('allows dismissing error alerts', async () => {
      const user = userEvent.setup()
      mockOnRoleChange.mockRejectedValue(new Error('Test error'))

      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      // Trigger error
      const roleSelects = screen.getAllByRole('combobox')
      await user.click(roleSelects[0])

      const managerOption = await screen.findByRole('option', { name: /manager/i })
      await user.click(managerOption)

      const errorAlert = await screen.findByText(/failed to update role/i)
      expect(errorAlert).toBeInTheDocument()

      // Close the error
      const closeButton = screen.getByRole('button', { name: /close/i })
      await user.click(closeButton)

      await waitFor(() => {
        expect(screen.queryByText(/failed to update role/i)).not.toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('has proper table structure', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      expect(screen.getByRole('table')).toBeInTheDocument()
      expect(screen.getAllByRole('row')).toHaveLength(mockMembers.length + 1) // +1 for header
    })

    it('has descriptive title attributes on remove buttons', () => {
      render(
        <OrgMembersTable
          members={mockMembers}
          onAddClick={mockOnAddClick}
          onRoleChange={mockOnRoleChange}
          onRemove={mockOnRemove}
        />
      )

      const removeButtons = screen.getAllByTitle(/remove member/i)
      expect(removeButtons).toHaveLength(mockMembers.length)
    })
  })
})
