/**
 * InviteMembersDialog - Invite organization members to an exercise
 *
 * Shows organization members who are not yet exercise participants.
 * Allows Exercise Directors to invite multiple members at once,
 * assigning each an exercise role (Controller, Evaluator, Observer, ExerciseDirector).
 *
 * When members are invited, they:
 * - Are added to the exercise with the assigned role
 * - Receive an email notification with exercise details (backend)
 *
 * @module features/exercises/components
 * @see email-communications/EM-03-S01-invite-existing-members.md
 */

import { useState, useEffect, useMemo } from 'react'
import type { FC } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Stack,
  Typography,
  Box,
  CircularProgress,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Checkbox,
  Select,
  MenuItem,
  FormControl,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faUserPlus, faEnvelope } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraLinkButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { organizationService } from '../../organizations/services/organizationService'
import type { OrgMember } from '../../organizations/types'
import type { ExerciseParticipantDto } from '../types'

interface InviteMembersDialogProps {
  open: boolean
  exerciseId: string
  currentParticipants: ExerciseParticipantDto[]
  onInvite: (invitations: Array<{ userId: string; role: string }>) => Promise<void>
  onClose: () => void
}

// Exercise roles available for assignment
const EXERCISE_ROLES = [
  { value: 'Observer', label: 'Observer' },
  { value: 'Evaluator', label: 'Evaluator' },
  { value: 'Controller', label: 'Controller' },
  { value: 'ExerciseDirector', label: 'Exercise Director' },
]

interface MemberSelection {
  member: OrgMember
  selected: boolean
  role: string
}

export const InviteMembersDialog: FC<InviteMembersDialogProps> = ({
  open,
  exerciseId,
  currentParticipants,
  onInvite,
  onClose,
}) => {
  const [members, setMembers] = useState<MemberSelection[]>([])
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Load organization members when dialog opens
  useEffect(() => {
    if (open) {
      loadOrgMembers()
    }
  }, [open, exerciseId, currentParticipants])

  // Reset state when dialog closes
  useEffect(() => {
    if (!open) {
      setMembers([])
      setError(null)
    }
  }, [open])

  const loadOrgMembers = async () => {
    try {
      setLoading(true)
      setError(null)

      // Get all organization members
      const orgMembers = await organizationService.getCurrentOrgMembers()

      // Filter out members who are already exercise participants
      const participantUserIds = new Set(currentParticipants.map(p => p.userId))
      const availableMembers = orgMembers.filter(
        member => !participantUserIds.has(member.userId)
      )

      // Initialize selection state
      setMembers(
        availableMembers.map(member => ({
          member,
          selected: false,
          role: 'Observer', // Default role
        }))
      )
    } catch (err) {
      console.error('Failed to load organization members:', err)
      setError('Failed to load organization members. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  const handleToggleSelect = (userId: string) => {
    setMembers(prev =>
      prev.map(m =>
        m.member.userId === userId ? { ...m, selected: !m.selected } : m
      )
    )
  }

  const handleRoleChange = (userId: string, role: string) => {
    setMembers(prev =>
      prev.map(m => (m.member.userId === userId ? { ...m, role } : m))
    )
  }

  const handleSelectAll = () => {
    const allSelected = members.every(m => m.selected)
    setMembers(prev => prev.map(m => ({ ...m, selected: !allSelected })))
  }

  const handleInvite = async () => {
    const selectedMembers = members.filter(m => m.selected)

    if (selectedMembers.length === 0) {
      setError('Please select at least one member to invite.')
      return
    }

    try {
      setSubmitting(true)
      setError(null)

      // Prepare invitations
      const invitations = selectedMembers.map(m => ({
        userId: m.member.userId,
        role: m.role,
      }))

      await onInvite(invitations)
      onClose()
    } catch (err) {
      console.error('Failed to invite members:', err)
      setError(
        err instanceof Error ? err.message : 'Failed to invite members. Please try again.'
      )
    } finally {
      setSubmitting(false)
    }
  }

  const selectedCount = useMemo(
    () => members.filter(m => m.selected).length,
    [members]
  )

  const allSelected = useMemo(
    () => members.length > 0 && members.every(m => m.selected),
    [members]
  )

  const someSelected = useMemo(
    () => members.some(m => m.selected) && !allSelected,
    [members, allSelected]
  )

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        <Box display="flex" alignItems="center" gap={1}>
          <FontAwesomeIcon icon={faUserPlus} />
          <Typography variant="h6">Invite Members to Exercise</Typography>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Stack spacing={CobraStyles.Spacing.FormFields}>
          {/* Instructions */}
          <Alert severity="info" icon={<FontAwesomeIcon icon={faEnvelope} />}>
            Select organization members to invite to this exercise. Each member will receive
            an email notification with exercise details and their assigned role.
          </Alert>

          {/* Error Alert */}
          {error && <Alert severity="error">{error}</Alert>}

          {/* Loading State */}
          {loading && (
            <Box display="flex" justifyContent="center" py={4}>
              <CircularProgress />
            </Box>
          )}

          {/* No Members Available */}
          {!loading && members.length === 0 && (
            <Alert severity="warning">
              All organization members are already participants in this exercise.
            </Alert>
          )}

          {/* Members Table */}
          {!loading && members.length > 0 && (
            <>
              {/* Selection Summary */}
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Typography variant="body2" color="text.secondary">
                  {selectedCount} of {members.length} members selected
                </Typography>
              </Box>

              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell padding="checkbox">
                        <Checkbox
                          indeterminate={someSelected}
                          checked={allSelected}
                          onChange={handleSelectAll}
                          inputProps={{ 'aria-label': 'Select all members' }}
                        />
                      </TableCell>
                      <TableCell>Name</TableCell>
                      <TableCell>Email</TableCell>
                      <TableCell>Org Role</TableCell>
                      <TableCell>Exercise Role</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {members.map(({ member, selected, role }) => (
                      <TableRow
                        key={member.userId}
                        hover
                        selected={selected}
                        onClick={() => handleToggleSelect(member.userId)}
                        sx={{ cursor: 'pointer' }}
                      >
                        <TableCell padding="checkbox">
                          <Checkbox
                            checked={selected}
                            inputProps={{
                              'aria-label': `Select ${member.displayName}`,
                            }}
                          />
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{member.displayName}</Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" color="text.secondary">
                            {member.email}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" color="text.secondary">
                            {member.role}
                          </Typography>
                        </TableCell>
                        <TableCell onClick={(e) => e.stopPropagation()}>
                          <FormControl size="small" fullWidth disabled={!selected}>
                            <Select
                              value={role}
                              onChange={e =>
                                handleRoleChange(member.userId, e.target.value)
                              }
                              variant="outlined"
                            >
                              {EXERCISE_ROLES.map(exerciseRole => (
                                <MenuItem
                                  key={exerciseRole.value}
                                  value={exerciseRole.value}
                                >
                                  {exerciseRole.label}
                                </MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>

              {/* Helper Text */}
              <Typography variant="caption" color="text.secondary">
                Selected members will receive an email invitation with exercise details and their
                assigned role.
              </Typography>
            </>
          )}
        </Stack>
      </DialogContent>

      <DialogActions>
        <CobraLinkButton onClick={onClose} disabled={submitting}>
          Cancel
        </CobraLinkButton>
        <CobraPrimaryButton
          onClick={handleInvite}
          disabled={loading || submitting || selectedCount === 0}
          startIcon={
            submitting ? (
              <CircularProgress size={16} />
            ) : (
              <FontAwesomeIcon icon={faEnvelope} />
            )
          }
        >
          {submitting
            ? 'Sending Invitations...'
            : `Invite ${selectedCount} Member${selectedCount !== 1 ? 's' : ''}`}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}
