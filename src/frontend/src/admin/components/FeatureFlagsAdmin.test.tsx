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

      // Notes starts as "Active", click "Hidden"
      const notesToggle = screen.getByTestId('feature-flag-toggle-notes')
      const hiddenButton = notesToggle.querySelector(
        'button[value="Hidden"]',
      ) as HTMLButtonElement

      fireEvent.click(hiddenButton)

      await waitFor(() => {
        // Check localStorage was updated
        expect(mockLocalStorage['dynamis-feature-flags']).toContain('Hidden')
      })
    })

    it('persists state changes to localStorage', async () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      const notesToggle = screen.getByTestId('feature-flag-toggle-notes')
      const comingSoonButton = notesToggle.querySelector(
        'button[value="ComingSoon"]',
      ) as HTMLButtonElement

      fireEvent.click(comingSoonButton)

      await waitFor(() => {
        const stored = JSON.parse(mockLocalStorage['dynamis-feature-flags'])
        expect(stored.notes).toBe('ComingSoon')
      })
    })
  })

  describe('reset functionality', () => {
    it('resets all flags to defaults when reset button is clicked', async () => {
      // Pre-set some non-default values
      mockLocalStorage['dynamis-feature-flags'] = JSON.stringify({
        notes: 'Hidden',
        exampleTool1: 'Active',
        exampleTool2: 'Active',
      })

      renderWithProvider(<FeatureFlagsAdmin />)

      const resetButton = screen.getByTestId('reset-flags-button')
      fireEvent.click(resetButton)

      await waitFor(() => {
        const stored = JSON.parse(mockLocalStorage['dynamis-feature-flags'])
        expect(stored).toEqual(defaultFeatureFlags)
      })
    })
  })

  describe('sections', () => {
    it('renders tools section', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      expect(screen.getByTestId('tools-flags-section')).toBeInTheDocument()
      expect(screen.getByText('Tools')).toBeInTheDocument()
    })

    it('renders experimental section', () => {
      renderWithProvider(<FeatureFlagsAdmin />)

      expect(
        screen.getByTestId('experimental-flags-section'),
      ).toBeInTheDocument()
      expect(screen.getByText('Experimental')).toBeInTheDocument()
    })
  })
})
