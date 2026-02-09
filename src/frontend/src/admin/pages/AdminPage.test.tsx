/**
 * AdminPage Component Tests
 *
 * Tests for the admin page:
 * - Page rendering
 * - Contains feature flags section
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen } from '../../test/testUtils'
import { AdminPage } from './AdminPage'
import { FeatureFlagsProvider } from '../contexts/FeatureFlagsContext'

// Wrapper with provider
const renderWithProvider = (ui: React.ReactElement) => {
  return render(<FeatureFlagsProvider>{ui}</FeatureFlagsProvider>)
}

describe('AdminPage', () => {
  const mockLocalStorage: Record<string, string> = {}

  beforeEach(() => {
    Object.keys(mockLocalStorage).forEach(
      key => delete mockLocalStorage[key],
    )

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
    it('renders the admin page', () => {
      renderWithProvider(<AdminPage />)

      expect(screen.getByTestId('admin-page')).toBeInTheDocument()
    })

    it('displays the page title', () => {
      renderWithProvider(<AdminPage />)

      expect(screen.getByText('System Settings')).toBeInTheDocument()
    })

    it('displays the page description', () => {
      renderWithProvider(<AdminPage />)

      expect(
        screen.getByText('Manage platform-wide configuration and feature availability'),
      ).toBeInTheDocument()
    })

    it('renders the feature flags section', () => {
      renderWithProvider(<AdminPage />)

      expect(screen.getByTestId('feature-flags-section')).toBeInTheDocument()
    })

    it('renders the system settings section', () => {
      renderWithProvider(<AdminPage />)

      expect(
        screen.getByTestId('system-settings-section'),
      ).toBeInTheDocument()
    })
  })

  describe('content', () => {
    it('contains the FeatureFlagsAdmin component', () => {
      renderWithProvider(<AdminPage />)

      // The FeatureFlagsAdmin component should render
      expect(screen.getByTestId('feature-flags-admin')).toBeInTheDocument()
    })
  })
})
