/**
 * ExerciseNavigationContext
 *
 * Manages the navigation context when a user is working within a specific exercise.
 * When in exercise context, the sidebar transforms to show exercise-specific navigation
 * with clock display and role-filtered menu items.
 *
 * Features:
 * - Tracks current exercise (id, name, status, user role)
 * - Persists to sessionStorage for refresh survival
 * - Clears on explicit exit or browser close
 *
 * @module shared/contexts
 * @see docs/features/navigation-shell/S03-in-exercise-context-navigation.md
 */

import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  useMemo,
} from 'react'
import type { ReactNode } from 'react'
import type { ExerciseStatus, HseepRole } from '@/types'
import { devWarn } from '@/core/utils/logger'

/**
 * Exercise data stored in navigation context
 */
export interface ExerciseNavigationData {
  id: string
  name: string
  status: ExerciseStatus
  userRole: HseepRole
}

/**
 * Context value shape
 */
interface ExerciseNavigationContextValue {
  /** Current exercise data (null if not in exercise context) */
  currentExercise: ExerciseNavigationData | null
  /** Enter exercise navigation context */
  enterExercise: (exercise: ExerciseNavigationData) => void
  /** Exit exercise navigation context */
  exitExercise: () => void
  /** Whether user is currently in exercise context */
  isInExerciseContext: boolean
  /** Update exercise data (e.g., when status changes) */
  updateExercise: (updates: Partial<ExerciseNavigationData>) => void
}

const STORAGE_KEY = 'cadence-exercise-navigation-context'

/**
 * Load exercise context from sessionStorage
 */
function loadFromStorage(): ExerciseNavigationData | null {
  try {
    const stored = sessionStorage.getItem(STORAGE_KEY)
    if (stored) {
      return JSON.parse(stored) as ExerciseNavigationData
    }
  } catch (error) {
    devWarn('Failed to load exercise navigation context from storage:', error)
  }
  return null
}

/**
 * Save exercise context to sessionStorage
 */
function saveToStorage(data: ExerciseNavigationData | null): void {
  try {
    if (data) {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(data))
    } else {
      sessionStorage.removeItem(STORAGE_KEY)
    }
  } catch (error) {
    devWarn('Failed to save exercise navigation context to storage:', error)
  }
}

const ExerciseNavigationContext = createContext<ExerciseNavigationContextValue | undefined>(
  undefined,
)

interface ExerciseNavigationProviderProps {
  children: ReactNode
}

/**
 * Provider component for exercise navigation context
 *
 * Wrap your app with this provider to enable exercise-specific navigation.
 *
 * @example
 * ```tsx
 * <ExerciseNavigationProvider>
 *   <App />
 * </ExerciseNavigationProvider>
 * ```
 */
export const ExerciseNavigationProvider = ({ children }: ExerciseNavigationProviderProps) => {
  // Initialize from sessionStorage for refresh survival
  const [currentExercise, setCurrentExercise] = useState<ExerciseNavigationData | null>(() =>
    loadFromStorage(),
  )

  // Persist to sessionStorage when context changes
  useEffect(() => {
    saveToStorage(currentExercise)
  }, [currentExercise])

  /**
   * Enter exercise navigation context
   */
  const enterExercise = useCallback((exercise: ExerciseNavigationData) => {
    setCurrentExercise(exercise)
  }, [])

  /**
   * Exit exercise navigation context
   */
  const exitExercise = useCallback(() => {
    setCurrentExercise(null)
  }, [])

  /**
   * Update current exercise data (e.g., when status changes via SignalR)
   */
  const updateExercise = useCallback((updates: Partial<ExerciseNavigationData>) => {
    setCurrentExercise(prev => {
      if (!prev) return null
      return { ...prev, ...updates }
    })
  }, [])

  /**
   * Whether user is currently in exercise context
   */
  const isInExerciseContext = useMemo(() => currentExercise !== null, [currentExercise])

  const value = useMemo(
    () => ({
      currentExercise,
      enterExercise,
      exitExercise,
      isInExerciseContext,
      updateExercise,
    }),
    [currentExercise, enterExercise, exitExercise, isInExerciseContext, updateExercise],
  )

  return (
    <ExerciseNavigationContext.Provider value={value}>
      {children}
    </ExerciseNavigationContext.Provider>
  )
}

/**
 * Hook to access exercise navigation context
 *
 * @throws Error if used outside of ExerciseNavigationProvider
 *
 * @example
 * ```tsx
 * const { currentExercise, isInExerciseContext, exitExercise } = useExerciseNavigation()
 *
 * if (isInExerciseContext) {
 *   return <ExerciseSidebar exercise={currentExercise} onBack={exitExercise} />
 * }
 * return <GlobalSidebar />
 * ```
 */
export const useExerciseNavigation = (): ExerciseNavigationContextValue => {
  const context = useContext(ExerciseNavigationContext)
  if (!context) {
    throw new Error('useExerciseNavigation must be used within ExerciseNavigationProvider')
  }
  return context
}

export default ExerciseNavigationProvider
