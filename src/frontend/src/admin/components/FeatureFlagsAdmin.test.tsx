/**
 * FeatureFlagsAdmin Component Tests
 *
 * Tests for the feature flags admin panel:
 * - Renders feature flag cards
 * - State toggle functionality
 * - Reset to defaults
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '../../test/testUtils'
import { FeatureFlagsAdmin } from './FeatureFlagsAdmin'
import { FeatureFlagsProvider } from '../contexts/FeatureFlagsContext'
import { defaultFeatureFlags, featureFlagInfo } from '../types/featureFlags'

// Wrapper with provider
const renderWithProvider = (ui: React.ReactElement) => {
  return render(<FeatureFlagsProvider>{ui}</FeatureFlagsProvider>)
}

describe('FeatureFlagsAdmin', () => {
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
    it('renders the feature flags admin panel', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      expect(screen.getByTestId('feature-flags-admin')).toBeInTheDocument()
    })

    it('displays the header and description', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      expect(screen.getByText('Feature Flags')).toBeInTheDocument()
      expect(
        screen.getByText(
          'Control feature visibility and availability across the application',
        ),
      ).toBeInTheDocument()
    })

    it('renders reset button', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      expect(screen.getByTestId('reset-flags-button')).toBeInTheDocument()
      expect(screen.getByText('Reset to Defaults')).toBeInTheDocument()
    })

    it('renders a card for each feature flag', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      featureFlagInfo.forEach(flag => {
        expect(
          screen.getByTestId(`feature-flag-card-${flag.key}`),
        ).toBeInTheDocument()
      })
    })

    it('displays feature flag labels and descriptions', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      featureFlagInfo.forEach(flag => {
        expect(screen.getByText(flag.label)).toBeInTheDocument()
        expect(screen.getByText(flag.description)).toBeInTheDocument()
      })
    })
  })

  describe('state toggling', () => {
    it('renders toggle button groups for each flag', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      featureFlagInfo.forEach(flag => {
        expect(
          screen.getByTestId(`feature-flag-toggle-${flag.key}`),
        ).toBeInTheDocument()
      })
    })

    it('changes flag state when toggle button is clicked', async () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      // templates starts as "Hidden", click "Active"
      const templatesToggle = screen.getByTestId('feature-flag-toggle-templates')
      const activeButton = templatesToggle.querySelector(
        'button[value="Active"]',
      ) as HTMLButtonElement

      fireEvent.click(activeButton)

      await waitFor(() => {
        // Check localStorage was updated
        const stored = JSON.parse(mockLocalStorage['cadence-feature-flags'])
        expect(stored.templates).toBe('Active')
      })
    })

    it('persists state changes to localStorage', async () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      const controlRoomToggle = screen.getByTestId('feature-flag-toggle-controlRoom')
      const comingSoonButton = controlRoomToggle.querySelector(
        'button[value="ComingSoon"]',
      ) as HTMLButtonElement

      fireEvent.click(comingSoonButton)

      await waitFor(() => {
        const stored = JSON.parse(mockLocalStorage['cadence-feature-flags'])
        expect(stored.controlRoom).toBe('ComingSoon')
      })
    })
  })

  describe('reset functionality', () => {
    it('resets all flags to defaults when reset button is clicked', async () => {
      // Pre-set some non-default values
      mockLocalStorage['cadence-feature-flags'] = JSON.stringify({
        templates: 'Active',
        reports: 'ComingSoon',
        controlRoom: 'Active',
      })

      renderWithProvider(<FeatureFlagsAdmin />)

      const resetButton = screen.getByTestId('reset-flags-button')
      fireEvent.click(resetButton)

      await waitFor(() => {
        const stored = JSON.parse(mockLocalStorage['cadence-feature-flags'])
        expect(stored).toEqual(defaultFeatureFlags)
      })
    })
  })

  describe('sections', () => {
    it('renders conduct section with Control Room', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      expect(screen.getByTestId('conduct-flags-section')).toBeInTheDocument()
      expect(screen.getByText('Conduct')).toBeInTheDocument()
      expect(screen.getByText('Control Room')).toBeInTheDocument()
    })

    it('renders analysis section with Reports', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      expect(screen.getByTestId('analysis-flags-section')).toBeInTheDocument()
      expect(screen.getByText('Analysis')).toBeInTheDocument()
      expect(screen.getByText('Organization Reports')).toBeInTheDocument()
    })

    it('renders system section with Templates', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      expect(screen.getByTestId('system-flags-section')).toBeInTheDocument()
      expect(screen.getByText('System')).toBeInTheDocument()
      expect(screen.getByText('Templates')).toBeInTheDocument()
    })
  })
})
