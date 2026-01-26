/**
 * ExerciseContextWrapper
 *
 * Route wrapper that manages exercise navigation context.
 * Automatically enters exercise context when navigating to exercise routes
 * and exits when leaving.
 *
 * Features:
 * - Reads exerciseId from URL params
 * - Fetches exercise data and user role
 * - Enters exercise context on mount
 * - Exits context when navigating away from exercise routes
 *
 * @module shared/components
 * @see docs/features/navigation-shell/S03-in-exercise-context-navigation.md
 */

import { useEffect, useRef } from 'react'
import { useParams, useLocation, Outlet } from 'react-router-dom'
import { CircularProgress, Box, Alert } from '@mui/material'
import { useExercise } from '@/features/exercises/hooks'
import { useExerciseRole } from '@/features/auth'
import { useExerciseNavigation } from '@/shared/contexts'
import { HseepRole } from '@/types'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { useNavigate } from 'react-router-dom'

/**
 * Map ExerciseRole string to HseepRole
 */
function mapToHseepRole(role: string): HseepRole {
  return role as HseepRole
}

/**
 * Check if a path is within an exercise route
 */
function isExercisePath(pathname: string): boolean {
  // Match /exercises/:id and /exercises/:id/*
  // But not /exercises, /exercises/new, etc.
  const match = pathname.match(/^\/exercises\/([^/]+)/)
  if (match && match[1] && match[1] !== 'new') {
    return true
  }
  return false
}

/**
 * Wrapper component for exercise routes
 *
 * Place this component as a parent route for all /exercises/:id/* routes.
 * It will automatically manage the exercise navigation context.
 *
 * @example
 * ```tsx
 * // In route configuration
 * {
 *   path: 'exercises/:id',
 *   element: <ExerciseContextWrapper />,
 *   children: [
 *     { index: true, element: <ExerciseDetailPage /> },
 *     { path: 'conduct', element: <ExerciseConductPage /> },
 *     { path: 'msel', element: <InjectListPage /> },
 *   ]
 * }
 * ```
 */
export const ExerciseContextWrapper = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const _location = useLocation()
  const navigate = useNavigate()
  const { currentExercise, enterExercise, exitExercise, updateExercise } = useExerciseNavigation()

  // Fetch exercise data
  const { exercise, loading, error } = useExercise(exerciseId)

  // Get user's role in this exercise
  const { effectiveRole } = useExerciseRole(exerciseId ?? null)

  // Track if we've entered context to avoid re-entering on every render
  const hasEnteredRef = useRef(false)
  const prevExerciseIdRef = useRef<string | null>(null)

  // Enter exercise context when data is loaded
  useEffect(() => {
    if (!exerciseId || !exercise || loading) return

    // Check if we need to enter context
    const needsEntry =
      !hasEnteredRef.current ||
      prevExerciseIdRef.current !== exerciseId ||
      currentExercise?.id !== exerciseId

    if (needsEntry) {
      enterExercise({
        id: exercise.id,
        name: exercise.name,
        status: exercise.status,
        userRole: mapToHseepRole(effectiveRole),
      })
      hasEnteredRef.current = true
      prevExerciseIdRef.current = exerciseId
    } else if (
      currentExercise &&
      (currentExercise.status !== exercise.status ||
        currentExercise.name !== exercise.name)
    ) {
      // Update context if exercise data changed (e.g., status changed)
      updateExercise({
        status: exercise.status,
        name: exercise.name,
      })
    }
  }, [
    exerciseId,
    exercise,
    loading,
    effectiveRole,
    enterExercise,
    updateExercise,
    currentExercise,
  ])

  // Update user role if it changes
  useEffect(() => {
    if (currentExercise && effectiveRole) {
      const newRole = mapToHseepRole(effectiveRole)
      if (currentExercise.userRole !== newRole) {
        updateExercise({ userRole: newRole })
      }
    }
  }, [effectiveRole, currentExercise, updateExercise])

  // Exit exercise context when navigating away from exercise routes
  useEffect(() => {
    return () => {
      // This cleanup runs when the component unmounts
      // Check if we're actually leaving exercise routes
      // Note: We use a short timeout to check the new location after navigation
      const timeoutId = setTimeout(() => {
        if (!isExercisePath(window.location.pathname)) {
          exitExercise()
          hasEnteredRef.current = false
          prevExerciseIdRef.current = null
        }
      }, 0)

      return () => clearTimeout(timeoutId)
    }
  }, [exitExercise])

  // Loading state
  if (loading && !exercise) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="200px"
      >
        <CircularProgress />
      </Box>
    )
  }

  // Error state
  if (error && !exercise) {
    return (
      <Box p={3}>
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
        <CobraPrimaryButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraPrimaryButton>
      </Box>
    )
  }

  // Not found
  if (!exerciseId || (!loading && !exercise)) {
    return (
      <Box p={3}>
        <Alert severity="warning" sx={{ mb: 2 }}>
          Exercise not found
        </Alert>
        <CobraPrimaryButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraPrimaryButton>
      </Box>
    )
  }

  // Render children
  return <Outlet />
}

export default ExerciseContextWrapper
