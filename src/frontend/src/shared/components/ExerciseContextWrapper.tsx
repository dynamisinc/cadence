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
import { useParams, Outlet } from 'react-router-dom'
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
  const navigate = useNavigate()
  const { currentExercise, enterExercise, exitExercise, updateExercise } = useExerciseNavigation()

  // Fetch exercise data
  const { exercise, loading, error } = useExercise(exerciseId)

  // Get user's role in this exercise
  const { effectiveRole } = useExerciseRole(exerciseId ?? null)

  // Track if we've entered context to avoid re-entering on every render
  const hasEnteredRef = useRef(false)
  const prevExerciseIdRef = useRef<string | null>(null)
  // Track the last values we set to prevent unnecessary updates
  const lastSetValuesRef = useRef<{
    status?: string
    name?: string
    userRole?: string
  }>({})

  // Get stable PRIMITIVE references for comparison - NEVER put objects in dependency arrays
  const exerciseDataId = exercise?.id
  const exerciseStatus = exercise?.status
  const exerciseName = exercise?.name
  const currentExerciseId = currentExercise?.id
  const currentExerciseStatus = currentExercise?.status
  const currentExerciseName = currentExercise?.name
  const currentExerciseRole = currentExercise?.userRole

  // Store exercise data in ref for access inside effects without triggering re-runs
  const exerciseRef = useRef(exercise)

  // Update ref in effect to avoid React Compiler warning about refs during render
  useEffect(() => {
    exerciseRef.current = exercise
  }, [exercise])

  // Enter exercise context when data is loaded
  useEffect(() => {
    // Use primitive checks - exerciseDataId tells us if exercise is loaded
    if (!exerciseId || !exerciseDataId || loading) return

    const exerciseData = exerciseRef.current
    if (!exerciseData) return

    // Check if we need to enter context
    const needsEntry =
      !hasEnteredRef.current ||
      prevExerciseIdRef.current !== exerciseId ||
      currentExerciseId !== exerciseId

    if (needsEntry) {
      const roleToSet = mapToHseepRole(effectiveRole)
      enterExercise({
        id: exerciseData.id,
        name: exerciseData.name,
        status: exerciseData.status,
        userRole: roleToSet,
      })
      hasEnteredRef.current = true
      prevExerciseIdRef.current = exerciseId
      // Track what we set to avoid duplicate updates
      lastSetValuesRef.current = {
        status: exerciseData.status,
        name: exerciseData.name,
        userRole: roleToSet,
      }
    } else if (currentExerciseId === exerciseId) {
      // Only update if values actually changed from what's in context
      // AND different from what we last set (prevents cascading updates)
      const updates: Partial<{ status: string; name: string }> = {}

      if (
        exerciseStatus &&
        currentExerciseStatus !== exerciseStatus &&
        lastSetValuesRef.current.status !== exerciseStatus
      ) {
        updates.status = exerciseStatus
      }

      if (
        exerciseName &&
        currentExerciseName !== exerciseName &&
        lastSetValuesRef.current.name !== exerciseName
      ) {
        updates.name = exerciseName
      }

      if (Object.keys(updates).length > 0) {
        updateExercise(updates)
        lastSetValuesRef.current = { ...lastSetValuesRef.current, ...updates }
      }
    }
  }, [
    // ONLY primitive values - no objects!
    exerciseId,
    exerciseDataId, // primitive string, not exercise object
    loading,
    effectiveRole,
    enterExercise,
    updateExercise,
    currentExerciseId,
    exerciseStatus,
    exerciseName,
    currentExerciseStatus,
    currentExerciseName,
  ])

  // Update user role if it changes - separate effect for clarity
  useEffect(() => {
    // Only update if we're in the right context and role actually changed
    if (!currentExerciseId || currentExerciseId !== exerciseId) return

    const newRole = mapToHseepRole(effectiveRole)

    // Check both current context value AND what we last set
    // This prevents the cascading update loop
    if (
      currentExerciseRole !== newRole &&
      lastSetValuesRef.current.userRole !== newRole
    ) {
      updateExercise({ userRole: newRole })
      lastSetValuesRef.current.userRole = newRole
    }
  }, [effectiveRole, currentExerciseId, currentExerciseRole, exerciseId, updateExercise])

  // Exit exercise context when navigating away from exercise routes
  useEffect(() => {
    return () => {
      // This cleanup runs when the component unmounts
      // Check if we're actually leaving exercise routes
      // Note: We use a short timeout to check the new location after navigation
      setTimeout(() => {
        if (!isExercisePath(window.location.pathname)) {
          exitExercise()
          hasEnteredRef.current = false
          prevExerciseIdRef.current = null
        }
      }, 0)
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
