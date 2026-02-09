/**
 * ParticipantList - Displays list of exercise participants with management
 *
 * Shows all participants assigned to an exercise with their roles.
 * Directors and Admins can add, edit, and remove participants.
 *
 * @module features/exercises/components
 */

import type { FC } from 'react'
import {
  Box,
  Typography,
  Paper,
  Table,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  Skeleton,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faUserPlus, faUsers, faEnvelope, faFileImport } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import { ParticipantListItem } from './ParticipantListItem'
import type { ExerciseParticipantDto } from '../types'

interface ParticipantListProps {
  participants: ExerciseParticipantDto[]
  canEdit: boolean
  loading: boolean
  onAdd: () => void
  onInviteMembers?: () => void
  onBulkImport?: () => void
  onRoleChange: (userId: string, newRole: string) => void
  onRemove: (userId: string, displayName: string) => void
}

/**
 * Loading skeleton for participant rows
 */
const ParticipantSkeleton: FC = () => (
  <TableBody>
    {Array.from({ length: 3 }, (_, i) => (
      <TableRow key={i}>
        <TableCell>
          <Skeleton variant="text" width={180} data-testid={`skeleton-name-${i}`} />
        </TableCell>
        <TableCell>
          <Skeleton variant="text" width={200} data-testid={`skeleton-email-${i}`} />
        </TableCell>
        <TableCell>
          <Skeleton variant="text" width={100} data-testid={`skeleton-system-${i}`} />
        </TableCell>
        <TableCell>
          <Skeleton variant="rounded" width={140} height={40} data-testid={`skeleton-role-${i}`} />
        </TableCell>
        <TableCell>
          <Skeleton variant="circular" width={32} height={32} data-testid={`skeleton-actions-${i}`} />
        </TableCell>
      </TableRow>
    ))}
  </TableBody>
)

/**
 * Empty state when no participants exist
 */
const EmptyState: FC<{ canEdit: boolean; onAdd: () => void }> = ({ canEdit, onAdd }) => (
  <Box
    sx={{
      py: 8,
      textAlign: 'center',
      backgroundColor: canEdit ? 'primary.50' : 'background.paper',
      border: '1px dashed',
      borderColor: canEdit ? 'primary.main' : 'grey.300',
      borderRadius: 1,
    }}
  >
    <Box
      sx={{
        width: 80,
        height: 80,
        borderRadius: '50%',
        backgroundColor: canEdit ? 'primary.100' : 'grey.100',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        margin: '0 auto 16px',
      }}
    >
      <FontAwesomeIcon
        icon={faUsers}
        size="2x"
        color={canEdit ? 'var(--mui-palette-primary-main)' : 'var(--mui-palette-grey-500)'}
      />
    </Box>

    {canEdit ? (
      <>
        <Typography variant="h5" gutterBottom>
          Add Your First Participant
        </Typography>
        <Typography variant="body1" color="text.secondary" paragraph>
          Start building your exercise team by assigning roles to users.
        </Typography>
        <CobraPrimaryButton
          startIcon={<FontAwesomeIcon icon={faUserPlus} />}
          onClick={onAdd}
          size="large"
        >
          Add Participant
        </CobraPrimaryButton>
      </>
    ) : (
      <>
        <Typography variant="h6" gutterBottom>
          No Participants
        </Typography>
        <Typography variant="body2" color="text.secondary">
          No participants have been assigned to this exercise yet.
        </Typography>
      </>
    )}
  </Box>
)

export const ParticipantList: FC<ParticipantListProps> = ({
  participants,
  canEdit,
  loading,
  onAdd,
  onInviteMembers,
  onBulkImport,
  onRoleChange,
  onRemove,
}) => {
  // Show empty state if no participants and not loading
  if (!loading && participants.length === 0) {
    return (
      <Box>
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
          <Typography variant="h5">Exercise Participants</Typography>
        </Stack>
        <EmptyState canEdit={canEdit} onAdd={onAdd} />
      </Box>
    )
  }

  return (
    <Box>
      {/* Header */}
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5">Exercise Participants</Typography>
        {canEdit && (
          <Stack direction="row" spacing={1}>
            {onInviteMembers && (
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faEnvelope} />}
                onClick={onInviteMembers}
              >
                Invite Members
              </CobraSecondaryButton>
            )}
            {onBulkImport && (
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faFileImport} />}
                onClick={onBulkImport}
              >
                Bulk Import
              </CobraSecondaryButton>
            )}
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faUserPlus} />}
              onClick={onAdd}
            >
              Add Participant
            </CobraPrimaryButton>
          </Stack>
        )}
      </Stack>

      {/* Table */}
      <Paper>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>System Role</TableCell>
              <TableCell>Exercise Role</TableCell>
              {canEdit && <TableCell align="right">Actions</TableCell>}
            </TableRow>
          </TableHead>

          {loading ? (
            <ParticipantSkeleton />
          ) : (
            <TableBody>
              {participants.map(participant => (
                <ParticipantListItem
                  key={participant.participantId}
                  participant={participant}
                  canEdit={canEdit}
                  onRoleChange={onRoleChange}
                  onRemove={onRemove}
                />
              ))}
            </TableBody>
          )}
        </Table>
      </Paper>
    </Box>
  )
}
