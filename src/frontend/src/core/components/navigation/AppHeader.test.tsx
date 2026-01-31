/**
 * AppHeader Component Tests
 *
 * Tests for the top navigation bar including:
 * - Logo/branding display
 * - Mobile menu toggle
 * - Profile menu integration
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent } from '../../../test/testUtils'
import { AppHeader } from './AppHeader'

// Mock useAuth to provide a user (ProfileMenu returns null without user)
vi.mock('../../../contexts/AuthContext', async importOriginal => {
  const actual = await importOriginal() as Record<string, unknown>
  return {
    ...actual,
    useAuth: () => ({
      user: {
        id: 'test-user-1',
        email: 'test@example.com',
        displayName: 'Test User',
        role: 'Administrator',
        status: 'Active',
      },
      isAuthenticated: true,
      isLoading: false,
      accessToken: 'mock-token',
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      refreshAccessToken: vi.fn(),
    }),
  }
})

// Mock roleResolutionService used by ProfileMenu
vi.mock('@/features/auth', async importOriginal => {
  const actual = await importOriginal() as Record<string, unknown>
  return {
    ...actual,
    roleResolutionService: {
      getUserExerciseAssignments: vi.fn().mockResolvedValue([]),
    },
    getRoleColor: vi.fn().mockReturnValue('primary'),
    getRoleDisplayName: vi.fn().mockReturnValue('Controller'),
  }
})

describe('AppHeader', () => {
  const mockLocalStorage: Record<string, string> = {}

  beforeEach(() => {
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

  describe('rendering', () => {
    it('renders the app header', () => {
      render(<AppHeader onMobileMenuToggle={vi.fn()} />)

      const header = screen.getByTestId('app-header')
      expect(header).toBeInTheDocument()
    })

    it('displays the app title', () => {
      render(<AppHeader onMobileMenuToggle={vi.fn()} />)

      const title = screen.getByTestId('app-title')
      expect(title).toBeInTheDocument()
      expect(title).toHaveTextContent('Cadence')
    })

    it('renders mobile menu toggle button', () => {
      render(<AppHeader onMobileMenuToggle={vi.fn()} />)

      const mobileToggle = screen.getByTestId('mobile-menu-toggle')
      expect(mobileToggle).toBeInTheDocument()
    })

    it('renders profile menu button', () => {
      render(<AppHeader onMobileMenuToggle={vi.fn()} />)

      const profileButton = screen.getByTestId('profile-menu-button')
      expect(profileButton).toBeInTheDocument()
    })
  })

  describe('mobile menu', () => {
    it('calls onMobileMenuToggle when mobile menu button is clicked', () => {
      const onMobileMenuToggle = vi.fn()
      render(<AppHeader onMobileMenuToggle={onMobileMenuToggle} />)

      const mobileToggle = screen.getByTestId('mobile-menu-toggle')
      fireEvent.click(mobileToggle)

      expect(onMobileMenuToggle).toHaveBeenCalledTimes(1)
    })
  })

  describe('styling', () => {
    it('uses correct header height from theme', () => {
      render(<AppHeader onMobileMenuToggle={vi.fn()} />)

      const header = screen.getByTestId('app-header')
      const styles = window.getComputedStyle(header)

      // Header height should match theme.cssStyling.headerHeight (54px)
      expect(styles.height).toBe('54px')
    })

    it('has fixed positioning', () => {
      render(<AppHeader onMobileMenuToggle={vi.fn()} />)

      const header = screen.getByTestId('app-header')
      const styles = window.getComputedStyle(header)

      expect(styles.position).toBe('fixed')
    })

    it('uses correct z-index to stay above sidebar', () => {
      render(<AppHeader onMobileMenuToggle={vi.fn()} />)

      const header = screen.getByTestId('app-header')
      const styles = window.getComputedStyle(header)

      // Z-index should be higher than drawer (1200)
      expect(parseInt(styles.zIndex)).toBeGreaterThan(1200)
    })
  })
})
