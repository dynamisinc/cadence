/**
 * ExerciseParticipantsPage - Manage exercise participants (S14)
 *
 * Full page for managing exercise-specific role assignments.
 * Directors and Admins can add, edit, and remove participants.
 * Other users have read-only access.
 *
 * Can be used standalone (with route param) or embedded (with prop).
 *
 * @module features/exercises/pages
 */

import { useState, useCallback } from 'react'
import type { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Box, Alert } from '@mui/material'
import { faHome, faUsers } from '@fortawesome/free-solid-svg-icons'
import { HelpTooltip, PageHeader } from '@/shared/components'
import { ParticipantList } from '../components/ParticipantList'
import { AddParticipantDialog } from '../components/AddParticipantDialog'
import { InviteMembersDialog } from '../components/InviteMembersDialog'
import { BulkImportDialog } from '../components/bulk-import/BulkImportDialog'
import { PendingInvitationsList } from '../components/PendingInvitationsList'
import { useExerciseParticipants } from '../hooks/useExerciseParticipants'
import { usePendingAssignments } from '../hooks/usePendingAssignments'
import { useExercise } from '../hooks'
import { useExerciseRole } from '../../auth/hooks/useExerciseRole'
import { useBreadcrumbs } from '../../../core/contexts'
import CobraStyles from '../../../theme/CobraStyles'
import type { AddParticipantRequest } from '../types'

interface ExerciseParticipantsPageProps {
  /** Exercise ID - if not provided, will attempt to get from route params */
  exerciseId?: string
}

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
export const ExerciseParticipantsPage: FC<ExerciseParticipantsPageProps> = ({
  exerciseId: propExerciseId,
}) => {
  // Support both prop-based (embedded in tabs) and route-based (standalone page) usage
  const params = useParams<{ exerciseId?: string; id?: string }>()
  const exerciseId = propExerciseId ?? params.exerciseId ?? params.id
  const isStandalone = !propExerciseId // Only set breadcrumbs when used as standalone page
  const { effectiveRole, can } = useExerciseRole(exerciseId ?? null)
  const canManageParticipants = can('manage_participants')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false)
  const [bulkImportDialogOpen, setBulkImportDialogOpen] = useState(false)

  // Use empty string fallback for hooks - they will handle missing exerciseId gracefully
  const safeExerciseId = exerciseId ?? ''

  // Fetch exercise data for breadcrumbs (only needed for standalone)
  const { exercise } = useExercise(isStandalone ? safeExerciseId : undefined)

  // Set breadcrumbs with exercise name (only for standalone page)
  useBreadcrumbs(
    isStandalone && exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'Participants' },
      ]
      : undefined,
  )

  const {
    participants,
    isLoading,
    isError,
    error,
    refetch: refetchParticipants,
    addParticipant,
    updateParticipantRole,
    removeParticipant,
  } = useExerciseParticipants(safeExerciseId)

  // Fetch pending assignments (invited users who haven't registered yet)
  const {
    pendingAssignments,
    isLoading: isPendingLoading,
    refetch: refetchPending,
    resendInvitation,
    isResending,
  } = usePendingAssignments(exerciseId)

  const handleAddClick = useCallback(() => {
    setDialogOpen(true)
  }, [])

  const handleInviteMembersClick = useCallback(() => {
    setInviteDialogOpen(true)
  }, [])

  const handleBulkImportClick = useCallback(() => {
    setBulkImportDialogOpen(true)
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

  const handleInviteMembers = useCallback(
    async (invitations: Array<{ userId: string; role: string }>) => {
      try {
        // Add each member to the exercise
        for (const invitation of invitations) {
          await addParticipant(invitation)
        }
        setInviteDialogOpen(false)
      } catch (error) {
        // Error toast handled by hook
        console.error('Failed to invite members:', error)
        throw error // Re-throw so dialog can handle the error
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

  const handleBulkImportComplete = useCallback(() => {
    // Refresh both participants and pending assignments after import
    refetchParticipants()
    refetchPending()
  }, [refetchParticipants, refetchPending])

  const handleResendInvitation = useCallback(
    async (invitationId: string, email: string) => {
      try {
        await resendInvitation(invitationId)
        // Success toast could be added here
        console.log(`Invitation resent to ${email}`)
      } catch (error) {
        console.error('Failed to resend invitation:', error)
        // Error toast could be added here
      }
    },
    [resendInvitation],
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
      {/* Page Header - only show in standalone mode */}
      {isStandalone && (
        <PageHeader
          title="Participants"
          icon={faUsers}
          subtitle={exercise ? `Manage participants for ${exercise.name}` : undefined}
          chips={<HelpTooltip helpKey="participants.roles" exerciseRole={effectiveRole ?? undefined} compact />}
        />
      )}

      {/* Error State */}
      {isError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load participants: {error instanceof Error ? error.message : 'Unknown error'}
        </Alert>
      )}

      {/* Participant List */}
      <ParticipantList
        participants={participants}
        canEdit={canManageParticipants}
        loading={isLoading}
        onAdd={handleAddClick}
        onInviteMembers={canManageParticipants ? handleInviteMembersClick : undefined}
        onBulkImport={canManageParticipants ? handleBulkImportClick : undefined}
        onRoleChange={handleRoleChange}
        onRemove={handleRemove}
      />

      {/* Pending Invitations */}
      {canManageParticipants && (
        <PendingInvitationsList
          pendingAssignments={pendingAssignments}
          loading={isPendingLoading}
          onResend={handleResendInvitation}
          isResending={isResending}
        />
      )}

      {/* Add Participant Dialog */}
      {canManageParticipants && (
        <AddParticipantDialog
          open={dialogOpen}
          onAdd={handleAddParticipant}
          onClose={() => setDialogOpen(false)}
        />
      )}

      {/* Invite Members Dialog */}
      {canManageParticipants && (
        <InviteMembersDialog
          open={inviteDialogOpen}
          exerciseId={exerciseId}
          currentParticipants={participants}
          onInvite={handleInviteMembers}
          onClose={() => setInviteDialogOpen(false)}
        />
      )}

      {/* Bulk Import Dialog */}
      {canManageParticipants && (
        <BulkImportDialog
          open={bulkImportDialogOpen}
          exerciseId={exerciseId}
          onClose={() => setBulkImportDialogOpen(false)}
          onImportComplete={handleBulkImportComplete}
        />
      )}
    </Box>
  )
}
