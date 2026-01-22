/**
 * ProfileMenu Component Tests
 *
 * Tests for user profile menu functionality including:
 * - Avatar rendering and initials
 * - Menu open/close behavior
 * - HSEEP role display
 * - Logout functionality
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '../../test/testUtils'
import { ProfileMenu } from './ProfileMenu'
import type { UserInfo } from '../../features/auth/types'

// Mock AuthContext
const mockLogout = vi.fn()
const mockUseAuth = vi.fn()

vi.mock('../../contexts/AuthContext', async (importOriginal) => {
  const actual = await importOriginal()
  return {
    ...actual as any,
    useAuth: () => mockUseAuth(),
  }
})

describe('ProfileMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockLogout.mockResolvedValue(undefined)
  })

  describe('with authenticated user', () => {
    const mockUser: UserInfo = {
      id: '123',
      email: 'john.doe@cadence.app',
      displayName: 'John Doe',
      role: 'Administrator',
      status: 'Active',
    }

    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        logout: mockLogout,
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshAccessToken: vi.fn(),
      })
    })

    it('renders profile menu button with avatar', () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      expect(button).toBeInTheDocument()

      const avatar = screen.getByTestId('profile-avatar')
      expect(avatar).toBeInTheDocument()
    })

    it('displays user initials in avatar', () => {
      render(<ProfileMenu />)

      // "John Doe" -> "JD"
      expect(screen.getByText('JD')).toBeInTheDocument()
    })

    it('displays user name in button', () => {
      render(<ProfileMenu />)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })

    it('displays formatted HSEEP role in button', () => {
      render(<ProfileMenu />)

      // "Administrator" stays as "Administrator" (no spaces needed)
      expect(screen.getByText('Administrator')).toBeInTheDocument()
    })

    it('formats ExerciseDirector role with space', () => {
      mockUseAuth.mockReturnValue({
        user: { ...mockUser, role: 'ExerciseDirector' },
        logout: mockLogout,
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      render(<ProfileMenu />)

      // "ExerciseDirector" -> "Exercise Director"
      expect(screen.getByText('Exercise Director')).toBeInTheDocument()
    })

    it('opens dropdown menu when clicking the button', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        expect(screen.getByText('john.doe@cadence.app')).toBeInTheDocument()
      })
    })

    it('displays user info and role in dropdown', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        expect(screen.getByText('john.doe@cadence.app')).toBeInTheDocument()
        // Check for the label "Role:" which is always present
        expect(screen.getByText(/Role:/)).toBeInTheDocument()
      })
    })

    it('shows logout button when user is authenticated', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        const logoutButton = screen.getByTestId('logout-button')
        expect(logoutButton).toBeInTheDocument()
      })
    })

    it('calls logout when clicking logout button', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        const logoutButton = screen.getByTestId('logout-button')
        fireEvent.click(logoutButton)
      })

      expect(mockLogout).toHaveBeenCalledTimes(1)
    })
  })

  describe('without authenticated user', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: null,
        logout: mockLogout,
        isAuthenticated: false,
        isLoading: false,
        accessToken: null,
        login: vi.fn(),
        register: vi.fn(),
        refreshAccessToken: vi.fn(),
      })
    })

    it('displays guest user info', () => {
      render(<ProfileMenu />)

      expect(screen.getByText('Guest User')).toBeInTheDocument()
      expect(screen.getByText('No Role Assigned')).toBeInTheDocument()
    })

    it('shows guest initials', () => {
      render(<ProfileMenu />)

      // "Guest User" -> "GU"
      expect(screen.getByText('GU')).toBeInTheDocument()
    })

    it('does not show logout button when not authenticated', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        expect(screen.queryByTestId('logout-button')).not.toBeInTheDocument()
      })
    })
  })

  describe('initials generation', () => {
    it('generates initials for single name', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '123',
          email: 'madonna@cadence.app',
          displayName: 'Madonna',
          role: 'Controller',
          status: 'Active',
        },
        logout: mockLogout,
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      render(<ProfileMenu />)

      // Single name "Madonna" -> "M"
      expect(screen.getByText('M')).toBeInTheDocument()
    })

    it('generates initials for three names using first and last', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '123',
          email: 'john.smith@cadence.app',
          displayName: 'John Robert Smith',
          role: 'Evaluator',
          status: 'Active',
        },
        logout: mockLogout,
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      render(<ProfileMenu />)

      // "John Robert Smith" -> "JS" (first and last)
      expect(screen.getByText('JS')).toBeInTheDocument()
    })
  })
})
