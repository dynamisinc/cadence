/**
 * InvitationsTable - Display and manage organization invitations
 *
 * Shows pending invitations with the ability to resend or cancel them.
 *
 * @module features/organizations/components
 * @see EM-02-S01 Send invitation
 * @see EM-02-S02 Resend invitation
 */
import type { FC } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Box,
  Typography,
  IconButton,
  Tooltip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEnvelope, faXmark } from '@fortawesome/free-solid-svg-icons'
import type { Invitation } from '../types'
import { getOrgRoleLabel } from '../types'
import { format } from 'date-fns'

interface InvitationsTableProps {
  invitations: Invitation[];
  isLoading?: boolean;
  onResend: (invitationId: string) => void;
  onCancel: (invitationId: string) => void;
}

export const InvitationsTable: FC<InvitationsTableProps> = ({
  invitations,
  isLoading = false,
  onResend,
  onCancel,
}) => {
  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Pending':
        return 'warning'
      case 'Used':
        return 'success'
      case 'Expired':
        return 'error'
      case 'Cancelled':
        return 'default'
      default:
        return 'default'
    }
  }

  const formatDate = (dateString: string) => {
    try {
      return format(new Date(dateString), 'MMM d, yyyy h:mm a')
    } catch {
      return dateString
    }
  }

  if (invitations.length === 0) {
    return (
      <Box
        sx={{
          textAlign: 'center',
          py: 4,
          color: 'text.secondary',
        }}
      >
        <FontAwesomeIcon icon={faEnvelope} size="2x" style={{ opacity: 0.3, marginBottom: 16 }} />
        <Typography variant="body1">No pending invitations</Typography>
        <Typography variant="body2" sx={{ mt: 1 }}>
          Invite members to join your organization
        </Typography>
      </Box>
    )
  }

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Email</TableCell>
            <TableCell>Role</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Created</TableCell>
            <TableCell>Expires</TableCell>
            <TableCell>Created By</TableCell>
            <TableCell align="right">Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {invitations.map(invitation => (
            <TableRow key={invitation.id}>
              <TableCell>
                <Typography variant="body2" fontWeight={500}>
                  {invitation.email}
                </Typography>
              </TableCell>
              <TableCell>
                <Typography variant="body2">{getOrgRoleLabel(invitation.role)}</Typography>
              </TableCell>
              <TableCell>
                <Chip
                  label={invitation.status}
                  size="small"
                  color={getStatusColor(invitation.status)}
                />
              </TableCell>
              <TableCell>
                <Typography variant="body2" color="text.secondary">
                  {formatDate(invitation.createdAt)}
                </Typography>
              </TableCell>
              <TableCell>
                <Typography variant="body2" color="text.secondary">
                  {formatDate(invitation.expiresAt)}
                </Typography>
              </TableCell>
              <TableCell>
                <Typography variant="body2" color="text.secondary">
                  {invitation.invitedByName}
                </Typography>
              </TableCell>
              <TableCell align="right">
                <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'flex-end' }}>
                  {invitation.status === 'Pending' && (
                    <>
                      <Tooltip title="Resend invitation email">
                        <IconButton
                          size="small"
                          onClick={() => onResend(invitation.id)}
                          disabled={isLoading}
                          aria-label={`Resend invitation to ${invitation.email}`}
                        >
                          <FontAwesomeIcon icon={faEnvelope} />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Cancel invitation">
                        <IconButton
                          size="small"
                          onClick={() => onCancel(invitation.id)}
                          disabled={isLoading}
                          color="error"
                          aria-label={`Cancel invitation to ${invitation.email}`}
                        >
                          <FontAwesomeIcon icon={faXmark} />
                        </IconButton>
                      </Tooltip>
                    </>
                  )}
                </Box>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

export default InvitationsTable
