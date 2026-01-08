/**
 * ProfileMenu Component Tests
 *
 * Tests for user profile menu functionality including:
 * - Avatar rendering and initials
 * - Menu open/close behavior
 * - Role selection
 * - Account switching dialog
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '../../test/testUtils'
import { ProfileMenu } from './ProfileMenu'
import { PermissionRole } from '../../types'

describe('ProfileMenu', () => {
  const mockLocalStorage: Record<string, string> = {}

  beforeEach(() => {
    // Clear and mock localStorage
    Object.keys(mockLocalStorage).forEach(key => delete mockLocalStorage[key])

    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(key => {
      return mockLocalStorage[key] || null
    })

    vi.spyOn(Storage.prototype, 'setItem').mockImplementation((key, value) => {
      mockLocalStorage[key] = value
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('initial rendering', () => {
    it('renders profile menu button with avatar', () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      expect(button).toBeInTheDocument()

      const avatar = screen.getByTestId('profile-avatar')
      expect(avatar).toBeInTheDocument()
    })

    it('shows default user initials when no profile saved', () => {
      render(<ProfileMenu />)

      // Default user is "Demo User" -> "DU"
      expect(screen.getByText('DU')).toBeInTheDocument()
    })

    it('displays default user name', () => {
      render(<ProfileMenu />)

      expect(screen.getByText('Demo User')).toBeInTheDocument()
    })
  })

  describe('menu interactions', () => {
    it('opens dropdown menu when clicking the button', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        expect(screen.getByText('Profile Settings (Demo) - For testing purposes')).toBeInTheDocument()
      })
    })

    it('shows current user info in dropdown', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        expect(screen.getByText('user@dynamis.com')).toBeInTheDocument()
      })
    })
  })

  describe('role selection', () => {
    it('shows permission role section', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        expect(screen.getByText('Permission Role')).toBeInTheDocument()
      })
    })

    it('displays role options', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        const radios = screen.getAllByRole('radio')
        expect(radios.length).toBe(3)
      })
    })

    it('calls onProfileChange when role is changed', async () => {
      const onProfileChange = vi.fn()
      render(<ProfileMenu onProfileChange={onProfileChange} />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        const manageRadio = screen.getByRole('radio', { name: /Manage/i })
        fireEvent.click(manageRadio)
      })

      expect(onProfileChange).toHaveBeenCalledWith(PermissionRole.MANAGE)
    })
  })

  describe('switch account dialog', () => {
    it('shows switch account button when menu is open', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        const switchButton = screen.getByTestId('switch-account-button')
        expect(switchButton).toBeInTheDocument()
      })
    })

    it('opens account switch dialog when clicking switch button', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        const switchButton = screen.getByTestId('switch-account-button')
        fireEvent.click(switchButton)
      })

      await waitFor(() => {
        expect(screen.getByTestId('account-switch-dialog')).toBeInTheDocument()
      })
    })
  })

  describe('localStorage persistence', () => {
    it('loads saved profile from localStorage', () => {
      mockLocalStorage['dynamisUserProfile'] = JSON.stringify({
        role: PermissionRole.MANAGE,
        email: 'custom@example.com',
        fullName: 'Custom User',
      })

      render(<ProfileMenu />)

      // Should show initials from stored profile "Custom User" -> "CU"
      expect(screen.getByText('CU')).toBeInTheDocument()
      expect(screen.getByText('Custom User')).toBeInTheDocument()
    })

    it('saves role change to localStorage', async () => {
      render(<ProfileMenu />)

      const button = screen.getByTestId('profile-menu-button')
      fireEvent.click(button)

      await waitFor(() => {
        const manageRadio = screen.getByRole('radio', { name: /Manage/i })
        fireEvent.click(manageRadio)
      })

      expect(mockLocalStorage['dynamisUserProfile']).toContain('Manage')
    })
  })
})
