/**
 * ExerciseNavigationContext Tests
 *
 * Tests for the exercise navigation context that manages
 * sidebar transformation when entering/exiting exercises.
 */

import { renderHook, act } from '@testing-library/react'
import { describe, it, expect, beforeEach, vi } from 'vitest'
import {
  ExerciseNavigationProvider,
  useExerciseNavigation,
  type ExerciseNavigationData,
} from './ExerciseNavigationContext'
import { ExerciseStatus, HseepRole } from '@/types'

// Mock sessionStorage
const mockSessionStorage = (() => {
  let store: Record<string, string> = {}
  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key]
    }),
    clear: () => {
      store = {}
    },
  }
})()

Object.defineProperty(window, 'sessionStorage', {
  value: mockSessionStorage,
})

// Test wrapper
const wrapper = ({ children }: { children: React.ReactNode }) => (
  <ExerciseNavigationProvider>{children}</ExerciseNavigationProvider>
)

// Sample exercise data
const sampleExercise: ExerciseNavigationData = {
  id: 'exercise-123',
  name: 'Hurricane Response 2025',
  status: ExerciseStatus.Active,
  userRole: HseepRole.Controller,
}

describe('ExerciseNavigationContext', () => {
  beforeEach(() => {
    mockSessionStorage.clear()
    vi.clearAllMocks()
  })

  describe('initial state', () => {
    it('starts with null exercise context', () => {
      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      expect(result.current.currentExercise).toBeNull()
      expect(result.current.isInExerciseContext).toBe(false)
    })

    it('restores context from sessionStorage on mount', () => {
      // Pre-populate sessionStorage
      mockSessionStorage.setItem(
        'cadence-exercise-navigation-context',
        JSON.stringify(sampleExercise),
      )

      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      expect(result.current.currentExercise).toEqual(sampleExercise)
      expect(result.current.isInExerciseContext).toBe(true)
    })
  })

  describe('enterExercise', () => {
    it('sets current exercise context', () => {
      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      act(() => {
        result.current.enterExercise(sampleExercise)
      })

      expect(result.current.currentExercise).toEqual(sampleExercise)
      expect(result.current.isInExerciseContext).toBe(true)
    })

    it('persists exercise to sessionStorage', () => {
      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      act(() => {
        result.current.enterExercise(sampleExercise)
      })

      expect(mockSessionStorage.setItem).toHaveBeenCalledWith(
        'cadence-exercise-navigation-context',
        JSON.stringify(sampleExercise),
      )
    })

    it('replaces existing exercise context', () => {
      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      const anotherExercise: ExerciseNavigationData = {
        id: 'exercise-456',
        name: 'Earthquake Drill 2025',
        status: ExerciseStatus.Draft,
        userRole: HseepRole.Evaluator,
      }

      act(() => {
        result.current.enterExercise(sampleExercise)
      })

      act(() => {
        result.current.enterExercise(anotherExercise)
      })

      expect(result.current.currentExercise).toEqual(anotherExercise)
    })
  })

  describe('exitExercise', () => {
    it('clears exercise context', () => {
      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      act(() => {
        result.current.enterExercise(sampleExercise)
      })

      act(() => {
        result.current.exitExercise()
      })

      expect(result.current.currentExercise).toBeNull()
      expect(result.current.isInExerciseContext).toBe(false)
    })

    it('removes exercise from sessionStorage', () => {
      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      act(() => {
        result.current.enterExercise(sampleExercise)
      })

      act(() => {
        result.current.exitExercise()
      })

      expect(mockSessionStorage.removeItem).toHaveBeenCalledWith(
        'cadence-exercise-navigation-context',
      )
    })
  })

  describe('updateExercise', () => {
    it('updates exercise status', () => {
      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      act(() => {
        result.current.enterExercise(sampleExercise)
      })

      act(() => {
        result.current.updateExercise({ status: ExerciseStatus.Completed })
      })

      expect(result.current.currentExercise?.status).toBe(ExerciseStatus.Completed)
      expect(result.current.currentExercise?.name).toBe(sampleExercise.name)
    })

    it('updates exercise name', () => {
      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      act(() => {
        result.current.enterExercise(sampleExercise)
      })

      act(() => {
        result.current.updateExercise({ name: 'Updated Exercise Name' })
      })

      expect(result.current.currentExercise?.name).toBe('Updated Exercise Name')
    })

    it('does nothing when no exercise context', () => {
      const { result } = renderHook(() => useExerciseNavigation(), { wrapper })

      act(() => {
        result.current.updateExercise({ status: ExerciseStatus.Completed })
      })

      expect(result.current.currentExercise).toBeNull()
    })
  })

  describe('context persistence', () => {
    it('survives provider re-mount (simulating page refresh)', () => {
      // First mount - enter exercise
      const { result: result1, unmount } = renderHook(
        () => useExerciseNavigation(),
        { wrapper },
      )

      act(() => {
        result1.current.enterExercise(sampleExercise)
      })

      // Unmount
      unmount()

      // Re-mount (simulates refresh)
      const { result: result2 } = renderHook(() => useExerciseNavigation(), {
        wrapper,
      })

      expect(result2.current.currentExercise).toEqual(sampleExercise)
      expect(result2.current.isInExerciseContext).toBe(true)
    })
  })

  describe('error handling', () => {
    it('throws when used outside provider', () => {
      // Suppress console.error for this test
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      expect(() => {
        renderHook(() => useExerciseNavigation())
      }).toThrow('useExerciseNavigation must be used within ExerciseNavigationProvider')

      consoleSpy.mockRestore()
    })
  })
})
