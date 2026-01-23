/**
 * UserAutocomplete Component Tests
 *
 * Tests for the reusable user autocomplete component
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { render } from '../../test/testUtils'
import userEvent from '@testing-library/user-event'
import { UserAutocomplete } from './UserAutocomplete'
import { userService } from '../../features/users/services/userService'
import type { UserDto } from '../../features/users/types'

// Mock the user service
vi.mock('../../features/users/services/userService', () => ({
  userService: {
    getUsers: vi.fn(),
  },
}))

const mockUsers: UserDto[] = [
  {
    id: '1',
    email: 'admin@test.com',
    displayName: 'Admin User',
    systemRole: 'Admin',
    status: 'Active',
    lastLoginAt: '2026-01-20T10:00:00Z',
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: '2',
    email: 'manager@test.com',
    displayName: 'Manager User',
    systemRole: 'Manager',
    status: 'Active',
    lastLoginAt: '2026-01-20T09:00:00Z',
    createdAt: '2026-01-02T00:00:00Z',
  },
  {
    id: '3',
    email: 'user@test.com',
    displayName: 'Regular User',
    systemRole: 'User',
    status: 'Active',
    lastLoginAt: '2026-01-20T08:00:00Z',
    createdAt: '2026-01-03T00:00:00Z',
  },
  {
    id: '4',
    email: 'deactivated@test.com',
    displayName: 'Deactivated User',
    systemRole: 'User',
    status: 'Deactivated',
    lastLoginAt: null,
    createdAt: '2026-01-04T00:00:00Z',
  },
]

describe('UserAutocomplete', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders with label', () => {
    render(
      <UserAutocomplete
        value={null}
        onChange={vi.fn()}
        label="Select User"
      />
    )

    expect(screen.getByLabelText(/select user/i)).toBeInTheDocument()
  })

  it('loads and displays all users when no role filter is specified', async () => {
    vi.mocked(userService.getUsers).mockResolvedValue({
      users: mockUsers,
      pagination: { page: 1, pageSize: 100, totalCount: 4, totalPages: 1 },
    })

    render(
      <UserAutocomplete
        value={null}
        onChange={vi.fn()}
        label="Select User"
      />
    )

    const input = screen.getByLabelText(/select user/i)
    await userEvent.click(input)

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
      expect(screen.getByText('Manager User')).toBeInTheDocument()
      expect(screen.getByText('Regular User')).toBeInTheDocument()
    })
  })

  it('filters to Admin and Manager roles when filterToDirectorEligible is true', async () => {
    const adminsAndManagers = mockUsers.filter(u =>
      u.systemRole === 'Admin' || u.systemRole === 'Manager'
    )

    vi.mocked(userService.getUsers).mockResolvedValue({
      users: adminsAndManagers,
      pagination: { page: 1, pageSize: 100, totalCount: 2, totalPages: 1 },
    })

    render(
      <UserAutocomplete
        value={null}
        onChange={vi.fn()}
        label="Select Director"
        filterToDirectorEligible
      />
    )

    const input = screen.getByLabelText(/select director/i)
    await userEvent.click(input)

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
      expect(screen.getByText('Manager User')).toBeInTheDocument()
      expect(screen.queryByText('Regular User')).not.toBeInTheDocument()
    })
  })

  it('excludes deactivated users', async () => {
    const activeUsers = mockUsers.filter(u => u.status === 'Active')

    vi.mocked(userService.getUsers).mockResolvedValue({
      users: activeUsers,
      pagination: { page: 1, pageSize: 100, totalCount: 3, totalPages: 1 },
    })

    render(
      <UserAutocomplete
        value={null}
        onChange={vi.fn()}
        label="Select User"
      />
    )

    const input = screen.getByLabelText(/select user/i)
    await userEvent.click(input)

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
      expect(screen.queryByText('Deactivated User')).not.toBeInTheDocument()
    })
  })

  it('displays user email and role in options', async () => {
    vi.mocked(userService.getUsers).mockResolvedValue({
      users: [mockUsers[0]],
      pagination: { page: 1, pageSize: 100, totalCount: 1, totalPages: 1 },
    })

    render(
      <UserAutocomplete
        value={null}
        onChange={vi.fn()}
        label="Select User"
      />
    )

    const input = screen.getByLabelText(/select user/i)
    await userEvent.click(input)

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
      expect(screen.getByText(/admin@test\.com/i)).toBeInTheDocument()
      // Check for role in caption text that includes email
      const captionElement = screen.getByText((content, element) => {
        return element?.tagName.toLowerCase() === 'span' &&
               element?.className.includes('MuiTypography-caption') &&
               content.includes('admin@test.com') &&
               content.includes('Admin')
      })
      expect(captionElement).toBeInTheDocument()
    })
  })

  it('calls onChange when user is selected', async () => {
    const handleChange = vi.fn()

    vi.mocked(userService.getUsers).mockResolvedValue({
      users: mockUsers,
      pagination: { page: 1, pageSize: 100, totalCount: 4, totalPages: 1 },
    })

    render(
      <UserAutocomplete
        value={null}
        onChange={handleChange}
        label="Select User"
      />
    )

    const input = screen.getByLabelText(/select user/i)
    await userEvent.click(input)

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument()
    })

    await userEvent.click(screen.getByText('Admin User'))

    expect(handleChange).toHaveBeenCalledWith(mockUsers[0])
  })

  it('displays loading state while fetching users', () => {
    vi.mocked(userService.getUsers).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    )

    render(
      <UserAutocomplete
        value={null}
        onChange={vi.fn()}
        label="Select User"
      />
    )

    const input = screen.getByLabelText(/select user/i)
    userEvent.click(input)

    // Loading indicator should appear
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('shows helper text when provided', () => {
    render(
      <UserAutocomplete
        value={null}
        onChange={vi.fn()}
        label="Select User"
        helperText="Choose an Exercise Director"
      />
    )

    expect(screen.getByText('Choose an Exercise Director')).toBeInTheDocument()
  })

  it('marks field as required when required prop is true', () => {
    render(
      <UserAutocomplete
        value={null}
        onChange={vi.fn()}
        label="Select User"
        required
      />
    )

    const input = screen.getByLabelText(/select user/i)
    expect(input).toBeRequired()
  })

  it('displays selected user value', async () => {
    const selectedUser = mockUsers[0]

    vi.mocked(userService.getUsers).mockResolvedValue({
      users: mockUsers,
      pagination: { page: 1, pageSize: 100, totalCount: 4, totalPages: 1 },
    })

    render(
      <UserAutocomplete
        value={selectedUser}
        onChange={vi.fn()}
        label="Select User"
      />
    )

    await waitFor(() => {
      const input = screen.getByLabelText(/select user/i) as HTMLInputElement
      expect(input.value).toBe('Admin User')
    })
  })

  it('can clear selection when clearable', async () => {
    const handleChange = vi.fn()
    const selectedUser = mockUsers[0]

    vi.mocked(userService.getUsers).mockResolvedValue({
      users: mockUsers,
      pagination: { page: 1, pageSize: 100, totalCount: 4, totalPages: 1 },
    })

    render(
      <UserAutocomplete
        value={selectedUser}
        onChange={handleChange}
        label="Select User"
      />
    )

    // Find and click the clear button
    const clearButton = screen.getByTitle('Clear')
    await userEvent.click(clearButton)

    expect(handleChange).toHaveBeenCalledWith(null)
  })
})
