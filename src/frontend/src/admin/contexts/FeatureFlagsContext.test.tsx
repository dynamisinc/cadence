/**
 * FeatureFlagsContext Tests
 *
 * Tests for the feature flags context and hooks:
 * - Provider functionality
 * - localStorage persistence
 * - Hook return values
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '../../test/testUtils'
import { render as rawRender } from '@testing-library/react'
import {
  FeatureFlagsProvider,
  useFeatureFlags,
  useFeatureFlagState,
} from './FeatureFlagsContext'

// Test component that uses the hooks
const TestComponent = () => {
  const { flags, updateFlag, resetFlags, isActive, isVisible, isComingSoon } =
    useFeatureFlags()

  return (
    <div>
      <span data-testid="templates-state">{flags.templates}</span>
      <span data-testid="reports-state">{flags.reports}</span>
      <span data-testid="controlRoom-state">{flags.controlRoom}</span>
      <span data-testid="templates-active">{isActive('templates').toString()}</span>
      <span data-testid="reports-visible">
        {isVisible('reports').toString()}
      </span>
      <span data-testid="controlRoom-coming">
        {isComingSoon('controlRoom').toString()}
      </span>
      <button
        data-testid="set-templates-active"
        onClick={() => updateFlag('templates', 'Active')}
      >
        Set Active
      </button>
      <button
        data-testid="set-templates-coming"
        onClick={() => updateFlag('templates', 'ComingSoon')}
      >
        Set Coming Soon
      </button>
      <button data-testid="reset" onClick={resetFlags}>
        Reset
      </button>
    </div>
  )
}

// Test component for useFeatureFlagState
const SingleFlagTestComponent = () => {
  const { state, isActive, isVisible, isComingSoon } =
    useFeatureFlagState('templates')

  return (
    <div>
      <span data-testid="state">{state}</span>
      <span data-testid="is-active">{isActive.toString()}</span>
      <span data-testid="is-visible">{isVisible.toString()}</span>
      <span data-testid="is-coming">{isComingSoon.toString()}</span>
    </div>
  )
}

describe('FeatureFlagsContext', () => {
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

  describe('useFeatureFlags', () => {
    it('provides default flag values (all Hidden for unimplemented features)', () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      // All features default to Hidden since they are not yet implemented
      expect(screen.getByTestId('templates-state')).toHaveTextContent('Hidden')
      expect(screen.getByTestId('reports-state')).toHaveTextContent('Hidden')
      expect(screen.getByTestId('controlRoom-state')).toHaveTextContent('Hidden')
    })

    it('loads saved flags from localStorage', () => {
      mockLocalStorage['cadence-feature-flags'] = JSON.stringify({
        templates: 'Active',
        reports: 'ComingSoon',
        controlRoom: 'Hidden',
      })

      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      expect(screen.getByTestId('templates-state')).toHaveTextContent('Active')
      expect(screen.getByTestId('reports-state')).toHaveTextContent('ComingSoon')
    })

    it('updates flags when updateFlag is called', async () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      fireEvent.click(screen.getByTestId('set-templates-active'))

      await waitFor(() => {
        expect(screen.getByTestId('templates-state')).toHaveTextContent('Active')
      })
    })

    it('persists flag changes to localStorage', async () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      fireEvent.click(screen.getByTestId('set-templates-active'))

      await waitFor(() => {
        const stored = JSON.parse(mockLocalStorage['cadence-feature-flags'])
        expect(stored.templates).toBe('Active')
      })
    })

    it('resets flags to defaults when resetFlags is called', async () => {
      mockLocalStorage['cadence-feature-flags'] = JSON.stringify({
        templates: 'Active',
        reports: 'ComingSoon',
        controlRoom: 'ComingSoon',
      })

      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      fireEvent.click(screen.getByTestId('reset'))

      await waitFor(() => {
        // All features reset to Hidden (default for unimplemented features)
        expect(screen.getByTestId('templates-state')).toHaveTextContent('Hidden')
        expect(screen.getByTestId('reports-state')).toHaveTextContent('Hidden')
      })
    })

    it('returns correct value for isActive', () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      // templates is "Hidden" by default, not Active
      expect(screen.getByTestId('templates-active')).toHaveTextContent('false')
    })

    it('returns correct value for isVisible', () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      // reports is "Hidden" by default
      expect(screen.getByTestId('reports-visible')).toHaveTextContent('false')
    })

    it('returns correct value for isComingSoon', () => {
      mockLocalStorage['cadence-feature-flags'] = JSON.stringify({
        templates: 'Hidden',
        reports: 'Hidden',
        controlRoom: 'ComingSoon',
      })

      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      // controlRoom is set to "ComingSoon" in localStorage
      expect(screen.getByTestId('controlRoom-coming')).toHaveTextContent('true')
    })
  })

  describe('useFeatureFlagState', () => {
    it('returns state and helper values for a single flag', () => {
      render(
        <FeatureFlagsProvider>
          <SingleFlagTestComponent />
        </FeatureFlagsProvider>,
      )

      // templates defaults to 'Hidden' (unimplemented feature)
      expect(screen.getByTestId('state')).toHaveTextContent('Hidden')
      expect(screen.getByTestId('is-active')).toHaveTextContent('false')
      expect(screen.getByTestId('is-visible')).toHaveTextContent('false')
      expect(screen.getByTestId('is-coming')).toHaveTextContent('false')
    })

    it('returns correct values for a ComingSoon flag', () => {
      // Pre-set templates to ComingSoon
      const mockLocalStorage: Record<string, string> = {
        'cadence-feature-flags': JSON.stringify({
          templates: 'ComingSoon',
          reports: 'Hidden',
          controlRoom: 'Hidden',
        }),
      }

      vi.spyOn(Storage.prototype, 'getItem').mockImplementation(key => {
        return mockLocalStorage[key] || null
      })

      render(
        <FeatureFlagsProvider>
          <SingleFlagTestComponent />
        </FeatureFlagsProvider>,
      )

      expect(screen.getByTestId('state')).toHaveTextContent('ComingSoon')
      expect(screen.getByTestId('is-active')).toHaveTextContent('false')
      expect(screen.getByTestId('is-visible')).toHaveTextContent('true')
      expect(screen.getByTestId('is-coming')).toHaveTextContent('true')
    })
  })

  describe('error handling', () => {
    it('throws error when useFeatureFlags is used outside provider', () => {
      // Suppress console.error for this test
      const consoleSpy = vi
        .spyOn(console, 'error')
        .mockImplementation(() => {})

      // Use rawRender (without providers) to test error case
      expect(() => {
        rawRender(<TestComponent />)
      }).toThrow('useFeatureFlags must be used within FeatureFlagsProvider')

      consoleSpy.mockRestore()
    })
  })
})
