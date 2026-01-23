/**
 * UserListPage Component Tests
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { UserListPage } from './UserListPage'
import { userService } from '../services/userService'
import type { UserListResponse } from '../types'

// Mock the user service
vi.mock('../services/userService')

const mockUserList: UserListResponse = {
  users: [
    {
      id: '1',
      email: 'admin@example.com',
      displayName: 'Admin User',
      systemRole: 'Admin',
      status: 'Active',
      lastLoginAt: '2025-01-20T14:30:00Z',
      createdAt: '2025-01-01T09:00:00Z',
    },
    {
      id: '2',
      email: 'jane@example.com',
      displayName: 'Jane Smith',
      systemRole: 'Manager',
      status: 'Active',
      lastLoginAt: '2025-01-15T10:00:00Z',
      createdAt: '2025-01-05T12:00:00Z',
    },
    {
      id: '3',
      email: 'deactivated@example.com',
      displayName: 'Old User',
      systemRole: 'User',
      status: 'Deactivated',
      lastLoginAt: null,
      createdAt: '2024-12-01T08:00:00Z',
    },
  ],
  pagination: {
    page: 1,
    pageSize: 20,
    totalCount: 3,
    totalPages: 1,
  },
}

describe('UserListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(userService.getUsers).mockResolvedValue(mockUserList)
  })

  it('loads and displays user list', async () => {
    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
      expect(screen.getByText('Old User')).toBeInTheDocument()
    })
  })

  it('shows user details in table columns', async () => {
    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('admin@example.com')).toBeInTheDocument()
      expect(screen.getByText('Admin')).toBeInTheDocument()
      // Multiple "Active" statuses will exist, just check one is present
      expect(screen.getAllByText('Active').length).toBeGreaterThan(0)
    })
  })

  it('filters users by search term', async () => {
    const user = userEvent.setup()
    render(<UserListPage />)

    // Wait for initial load
    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
    })

    // Search for "jane"
    const searchInput = screen.getByPlaceholderText(/search by name or email/i)
    await user.type(searchInput, 'jane')

    await waitFor(() => {
      expect(userService.getUsers).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'jane' }),
      )
    })
  })

  it('filters users by role', async () => {
    const user = userEvent.setup()
    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
    })

    // Find the role filter dropdown (it shows "All Roles" initially)
    const roleFilter = screen.getByText('All Roles').closest('[role="combobox"]') as HTMLElement
    await user.click(roleFilter)

    // Click Manager option in the listbox (system roles are Admin, Manager, User)
    const managerOption = await screen.findByRole('option', { name: 'Manager' })
    await user.click(managerOption)

    await waitFor(() => {
      expect(userService.getUsers).toHaveBeenCalledWith(
        expect.objectContaining({ role: 'Manager' }),
      )
    })
  })

  it('opens edit dialog when edit button clicked', async () => {
    const user = userEvent.setup()
    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    })

    const editButtons = screen.getAllByLabelText(/edit user/i)
    await user.click(editButtons[1]) // Click Jane's edit button

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Jane Smith')).toBeInTheDocument()
    })
  })

  it('shows deactivate confirmation dialog', async () => {
    const user = userEvent.setup()
    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    })

    const deactivateButtons = screen.getAllByLabelText(/deactivate user/i)
    await user.click(deactivateButtons[0]) // Click active user's deactivate

    await waitFor(() => {
      expect(screen.getByText(/are you sure/i)).toBeInTheDocument()
      expect(screen.getByText(/no longer be able to log in/i)).toBeInTheDocument()
    })
  })

  it('calls deactivateUser when confirmed', async () => {
    const user = userEvent.setup()
    vi.mocked(userService.deactivateUser).mockResolvedValue({
      ...mockUserList.users[1],
      status: 'Deactivated',
    })

    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    })

    const deactivateButtons = screen.getAllByLabelText(/deactivate user/i)
    // Click Jane's deactivate button (second active user - index 1)
    await user.click(deactivateButtons[1])

    // Confirm in dialog - wait for the confirm button to appear and be enabled
    const confirmButton = await screen.findByRole('button', { name: /deactivate/i })
    expect(confirmButton).toBeInTheDocument()

    await user.click(confirmButton)

    await waitFor(() => {
      expect(userService.deactivateUser).toHaveBeenCalledWith('2')
    }, { timeout: 3000 })
  })

  it('shows reactivate button for deactivated users', async () => {
    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Old User')).toBeInTheDocument()
    })

    expect(screen.getByLabelText(/reactivate user/i)).toBeInTheDocument()
  })

  it('calls reactivateUser when reactivate clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(userService.reactivateUser).mockResolvedValue({
      ...mockUserList.users[2],
      status: 'Active',
    })

    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Old User')).toBeInTheDocument()
    })

    const reactivateButton = screen.getByLabelText(/reactivate user/i)
    await user.click(reactivateButton)

    await waitFor(() => {
      expect(userService.reactivateUser).toHaveBeenCalledWith('3')
    })
  })

  it('handles role change', async () => {
    const user = userEvent.setup()
    vi.mocked(userService.changeRole).mockResolvedValue({
      ...mockUserList.users[1],
      systemRole: 'User',
    })

    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    })

    // Find Jane's role dropdown - filter for user role dropdowns (not the filter dropdown)
    // System roles are: Admin, Manager, User
    const roleSelects = screen.getAllByRole('combobox').filter(el =>
      el.textContent?.includes('Manager') || el.textContent?.includes('Admin') || el.textContent?.includes('User'),
    )
    const janeRoleSelect = roleSelects.find(el => el.textContent === 'Manager')
    expect(janeRoleSelect).toBeDefined()

    await user.click(janeRoleSelect!)

    const userOption = await screen.findByRole('option', { name: 'User' })
    await user.click(userOption)

    await waitFor(() => {
      expect(userService.changeRole).toHaveBeenCalledWith('2', { systemRole: 'User' })
    }, { timeout: 3000 })
  })

  it('shows warning when trying to remove last administrator', async () => {
    const user = userEvent.setup()
    vi.mocked(userService.changeRole).mockRejectedValue({
      response: { data: { error: 'last_administrator' } },
    })

    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
    })

    // Find admin's role dropdown
    // System roles are: Admin, Manager, User
    const roleSelects = screen.getAllByRole('combobox').filter(el =>
      el.textContent?.includes('Manager') || el.textContent?.includes('Admin') || el.textContent?.includes('User'),
    )
    const adminRoleSelect = roleSelects.find(el => el.textContent === 'Admin')
    expect(adminRoleSelect).toBeDefined()

    await user.click(adminRoleSelect!)

    const managerOption = await screen.findByRole('option', { name: 'Manager' })
    await user.click(managerOption)

    await waitFor(() => {
      expect(screen.getByText(/assign another administrator first/i)).toBeInTheDocument()
    }, { timeout: 3000 })
  })

  it('supports pagination', async () => {
    const _user = userEvent.setup()
    render(<UserListPage />)

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
    })

    // Page size selector should exist
    expect(screen.getByText(/rows per page/i)).toBeInTheDocument()
  })
})
