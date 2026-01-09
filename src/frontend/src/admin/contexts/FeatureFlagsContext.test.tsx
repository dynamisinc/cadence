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
      <span data-testid="notes-state">{flags.notes}</span>
      <span data-testid="example1-state">{flags.exampleTool1}</span>
      <span data-testid="notes-active">{isActive('notes').toString()}</span>
      <span data-testid="example2-visible">
        {isVisible('exampleTool2').toString()}
      </span>
      <span data-testid="example1-coming">
        {isComingSoon('exampleTool1').toString()}
      </span>
      <button
        data-testid="set-notes-hidden"
        onClick={() => updateFlag('notes', 'Hidden')}
      >
        Set Hidden
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
    useFeatureFlagState('notes')

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
    it('provides default flag values', () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      expect(screen.getByTestId('notes-state')).toHaveTextContent('Active')
      expect(screen.getByTestId('example1-state')).toHaveTextContent(
        'ComingSoon',
      )
    })

    it('loads saved flags from localStorage', () => {
      mockLocalStorage['cadence-feature-flags'] = JSON.stringify({
        notes: 'Hidden',
        exampleTool1: 'Active',
        exampleTool2: 'ComingSoon',
      })

      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      expect(screen.getByTestId('notes-state')).toHaveTextContent('Hidden')
      expect(screen.getByTestId('example1-state')).toHaveTextContent('Active')
    })

    it('updates flags when updateFlag is called', async () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      fireEvent.click(screen.getByTestId('set-notes-hidden'))

      await waitFor(() => {
        expect(screen.getByTestId('notes-state')).toHaveTextContent('Hidden')
      })
    })

    it('persists flag changes to localStorage', async () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      fireEvent.click(screen.getByTestId('set-notes-hidden'))

      await waitFor(() => {
        const stored = JSON.parse(mockLocalStorage['cadence-feature-flags'])
        expect(stored.notes).toBe('Hidden')
      })
    })

    it('resets flags to defaults when resetFlags is called', async () => {
      mockLocalStorage['cadence-feature-flags'] = JSON.stringify({
        notes: 'Hidden',
        exampleTool1: 'Active',
        exampleTool2: 'ComingSoon',
      })

      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      fireEvent.click(screen.getByTestId('reset'))

      await waitFor(() => {
        expect(screen.getByTestId('notes-state')).toHaveTextContent('Active')
        expect(screen.getByTestId('example1-state')).toHaveTextContent(
          'ComingSoon',
        )
      })
    })

    it('returns correct value for isActive', () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      expect(screen.getByTestId('notes-active')).toHaveTextContent('true')
    })

    it('returns correct value for isVisible', () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      // exampleTool2 is "Hidden" by default
      expect(screen.getByTestId('example2-visible')).toHaveTextContent('false')
    })

    it('returns correct value for isComingSoon', () => {
      render(
        <FeatureFlagsProvider>
          <TestComponent />
        </FeatureFlagsProvider>,
      )

      // exampleTool1 is "ComingSoon" by default
      expect(screen.getByTestId('example1-coming')).toHaveTextContent('true')
    })
  })

  describe('useFeatureFlagState', () => {
    it('returns state and helper values for a single flag', () => {
      render(
        <FeatureFlagsProvider>
          <SingleFlagTestComponent />
        </FeatureFlagsProvider>,
      )

      expect(screen.getByTestId('state')).toHaveTextContent('Active')
      expect(screen.getByTestId('is-active')).toHaveTextContent('true')
      expect(screen.getByTestId('is-visible')).toHaveTextContent('true')
      expect(screen.getByTestId('is-coming')).toHaveTextContent('false')
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
