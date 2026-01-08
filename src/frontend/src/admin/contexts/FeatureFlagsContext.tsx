/**
 * Feature Flags Context
 *
 * Provides global state management for feature flags with:
 * - localStorage persistence
 * - Cross-tab synchronization
 * - Helper hooks for checking feature states
 *
 * Usage:
 * 1. Wrap app with FeatureFlagsProvider
 * 2. Use useFeatureFlags() for full control
 * 3. Use useFeatureFlagState(key) for individual flag state
 */

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react'
import type {
  FeatureFlags,
  FeatureFlagState,
} from '../types/featureFlags'
import { defaultFeatureFlags } from '../types/featureFlags'

const STORAGE_KEY = 'dynamis-feature-flags'

/**
 * Context value interface
 */
interface FeatureFlagsContextValue {
  flags: FeatureFlags;
  updateFlag: (key: keyof FeatureFlags, value: FeatureFlagState) => void;
  updateFlags: (updates: Partial<FeatureFlags>) => void;
  resetFlags: () => void;
  getState: (key: keyof FeatureFlags) => FeatureFlagState;
  isActive: (key: keyof FeatureFlags) => boolean;
  isVisible: (key: keyof FeatureFlags) => boolean;
  isComingSoon: (key: keyof FeatureFlags) => boolean;
}

const FeatureFlagsContext = createContext<FeatureFlagsContextValue | null>(null)

/**
 * Load flags from localStorage
 */
const loadFlags = (): FeatureFlags => {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      const parsed = JSON.parse(stored)
      // Merge with defaults to handle new flags
      return { ...defaultFeatureFlags, ...parsed }
    }
  } catch (error) {
    console.error('Failed to load feature flags:', error)
  }
  return defaultFeatureFlags
}

/**
 * Save flags to localStorage
 */
const saveFlags = (flags: FeatureFlags): void => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(flags))
  } catch (error) {
    console.error('Failed to save feature flags:', error)
  }
}

/**
 * Feature Flags Provider Component
 */
export const FeatureFlagsProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [flags, setFlags] = useState<FeatureFlags>(loadFlags)

  // Persist to localStorage whenever flags change
  useEffect(() => {
    saveFlags(flags)
  }, [flags])

  // Listen for storage events (cross-tab sync)
  useEffect(() => {
    const handleStorage = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY && e.newValue) {
        try {
          const newFlags = JSON.parse(e.newValue)
          setFlags({ ...defaultFeatureFlags, ...newFlags })
        } catch (error) {
          console.error('Failed to parse storage event:', error)
        }
      }
    }

    window.addEventListener('storage', handleStorage)
    return () => window.removeEventListener('storage', handleStorage)
  }, [])

  const updateFlag = useCallback(
    (key: keyof FeatureFlags, value: FeatureFlagState) => {
      setFlags(prev => ({ ...prev, [key]: value }))
    },
    [],
  )

  const updateFlags = useCallback((updates: Partial<FeatureFlags>) => {
    setFlags(prev => ({ ...prev, ...updates }))
  }, [])

  const resetFlags = useCallback(() => {
    setFlags(defaultFeatureFlags)
  }, [])

  const getState = useCallback(
    (key: keyof FeatureFlags): FeatureFlagState => {
      return flags[key]
    },
    [flags],
  )

  const isActive = useCallback(
    (key: keyof FeatureFlags): boolean => {
      return flags[key] === 'Active'
    },
    [flags],
  )

  const isVisible = useCallback(
    (key: keyof FeatureFlags): boolean => {
      return flags[key] !== 'Hidden'
    },
    [flags],
  )

  const isComingSoon = useCallback(
    (key: keyof FeatureFlags): boolean => {
      return flags[key] === 'ComingSoon'
    },
    [flags],
  )

  const value: FeatureFlagsContextValue = {
    flags,
    updateFlag,
    updateFlags,
    resetFlags,
    getState,
    isActive,
    isVisible,
    isComingSoon,
  }

  return (
    <FeatureFlagsContext.Provider value={value}>
      {children}
    </FeatureFlagsContext.Provider>
  )
}

/**
 * Hook to access full feature flags context
 */
export const useFeatureFlags = (): FeatureFlagsContextValue => {
  const context = useContext(FeatureFlagsContext)
  if (!context) {
    throw new Error('useFeatureFlags must be used within FeatureFlagsProvider')
  }
  return context
}

/**
 * Hook to get a single feature flag's state
 */
export const useFeatureFlagState = (
  key: keyof FeatureFlags,
): {
  state: FeatureFlagState;
  isActive: boolean;
  isVisible: boolean;
  isComingSoon: boolean;
} => {
  const { getState, isActive, isVisible, isComingSoon } = useFeatureFlags()

  return {
    state: getState(key),
    isActive: isActive(key),
    isVisible: isVisible(key),
    isComingSoon: isComingSoon(key),
  }
}
