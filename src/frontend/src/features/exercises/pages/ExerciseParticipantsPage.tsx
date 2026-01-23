/**
 * ExerciseParticipantsPage - Manage exercise participants (S14)
 *
 * Full page for managing exercise-specific role assignments.
 * Directors and Admins can add, edit, and remove participants.
 * Other users have read-only access.
 *
 * @module features/exercises/pages
 */

import { FC, useState, useCallback } from 'react'
import { useParams } from 'react-router-dom'
import { Box, Alert } from '@mui/material'
import { ParticipantList } from '../components/ParticipantList'
import { AddParticipantDialog } from '../components/AddParticipantDialog'
import { useExerciseParticipants } from '../hooks/useExerciseParticipants'
import { usePermissions } from '../../../shared/hooks'
import CobraStyles from '../../../theme/CobraStyles'
import type { AddParticipantRequest } from '../types'

/**
 * Exercise Participants Page
 *
 * Acceptance Criteria:
 * - AC1: Directors see a "Participants" section
 * - AC2: Can click "Add Participant" to search for users
 * - AC3: Can select a user and choose their role for this exercise
 * - AC4: Added participants have assigned role permissions
 * - AC6: Participants can view their effective role clearly
 * - AC7: Admins can manage participants for any exercise
 * - AC8: Non-Directors/Admins cannot manage participants
 */
export const ExerciseParticipantsPage: FC = () => {
  const { exerciseId } = useParams<{ exerciseId: string }>()
  const { canManage } = usePermissions()
  const [dialogOpen, setDialogOpen] = useState(false)

  // Use empty string fallback for hooks - they will handle missing exerciseId gracefully
  const safeExerciseId = exerciseId ?? ''

  const {
    participants,
    isLoading,
    isError,
    error,
    addParticipant,
    updateParticipantRole,
    removeParticipant,
  } = useExerciseParticipants(safeExerciseId)

  const handleAddClick = useCallback(() => {
    setDialogOpen(true)
  }, [])

  const handleAddParticipant = useCallback(
    async (request: AddParticipantRequest) => {
      try {
        await addParticipant(request)
        setDialogOpen(false)
      } catch (error) {
        // Error toast handled by hook
        console.error('Failed to add participant:', error)
      }
    },
    [addParticipant],
  )

  const handleRoleChange = useCallback(
    async (userId: string, newRole: string) => {
      try {
        await updateParticipantRole(userId, { role: newRole })
      } catch (error) {
        // Error toast handled by hook
        console.error('Failed to update role:', error)
      }
    },
    [updateParticipantRole],
  )

  const handleRemove = useCallback(
    async (userId: string, displayName: string) => {
      const confirmed = window.confirm(
        `Are you sure you want to remove ${displayName} from this exercise?`,
      )
      if (!confirmed) return

      try {
        await removeParticipant(userId)
      } catch (error) {
        // Error toast handled by hook
        console.error('Failed to remove participant:', error)
      }
    },
    [removeParticipant],
  )

  // Early return for missing exerciseId (after all hooks)
  if (!exerciseId) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Alert severity="error">Exercise ID is required</Alert>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Error State */}
      {isError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load participants: {error instanceof Error ? error.message : 'Unknown error'}
        </Alert>
      )}

      {/* Participant List */}
      <ParticipantList
        participants={participants}
        canEdit={canManage}
        loading={isLoading}
        onAdd={handleAddClick}
        onRoleChange={handleRoleChange}
        onRemove={handleRemove}
      />

      {/* Add Participant Dialog */}
      {canManage && (
        <AddParticipantDialog
          open={dialogOpen}
          onAdd={handleAddParticipant}
          onClose={() => setDialogOpen(false)}
        />
      )}
    </Box>
  )
}
