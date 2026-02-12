/**
 * PendingInvitationsList Component
 *
 * Displays pending exercise invitations for users who haven't yet
 * accepted their organization membership.
 *
 * Shows invitation status, expiration, and allows resending invitations.
 */

import { useState } from 'react'
import {
  Box,
  Typography,
  Paper,
  Stack,
  Chip,
  Collapse,
  IconButton,
  Alert,
  CircularProgress,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronDown,
  faChevronUp,
  faEnvelope,
  faClock,
  faCheckCircle,
  faTimesCircle,
} from '@fortawesome/free-solid-svg-icons'
import { CobraSecondaryButton } from '@/theme/styledComponents'
import type { PendingExerciseAssignmentDto } from '../types/bulkImport'
import { formatDistanceToNow } from 'date-fns'

interface PendingInvitationsListProps {
  /** Pending assignments to display */
  pendingAssignments: PendingExerciseAssignmentDto[]
  /** Whether data is loading */
  loading?: boolean
  /** Callback to resend an invitation */
  onResend?: (invitationId: string, email: string) => Promise<void>
  /** Whether a resend operation is in progress */
  isResending?: boolean
}

export const PendingInvitationsList = ({
  pendingAssignments,
  loading = false,
  onResend,
  isResending = false,
}: PendingInvitationsListProps) => {
  const [expanded, setExpanded] = useState(true)
  const [resendingId, setResendingId] = useState<string | null>(null)

  const toggleExpanded = () => setExpanded(!expanded)

  const handleResend = async (invitationId: string, email: string) => {
    if (!onResend) return

    setResendingId(invitationId)
    try {
      await onResend(invitationId, email)
    } finally {
      setResendingId(null)
    }
  }

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Accepted':
        return <FontAwesomeIcon icon={faCheckCircle} style={{ color: '#2e7d32' }} />
      case 'Expired':
        return <FontAwesomeIcon icon={faTimesCircle} style={{ color: '#d32f2f' }} />
      case 'Pending':
      default:
        return <FontAwesomeIcon icon={faClock} style={{ color: '#ed6c02' }} />
    }
  }

  const getStatusColor = (status: string): 'success' | 'error' | 'warning' | 'default' => {
    switch (status) {
      case 'Accepted':
        return 'success'
      case 'Expired':
        return 'error'
      case 'Pending':
      default:
        return 'warning'
    }
  }

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  if (pendingAssignments.length === 0) {
    return null // Don't show section if no pending invitations
  }

  return (
    <Paper
      elevation={1}
      sx={{
        mt: 3,
        border: '1px solid',
        borderColor: 'divider',
      }}
    >
      {/* Header */}
      <Box
        onClick={toggleExpanded}
        sx={{
          p: 2,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          cursor: 'pointer',
          '&:hover': {
            backgroundColor: 'action.hover',
          },
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <FontAwesomeIcon
            icon={faEnvelope}
            size="lg"
            style={{ color: '#ed6c02' }}
          />
          <Typography variant="h6">
            Pending Invitations ({pendingAssignments.length})
          </Typography>
        </Box>
        <IconButton size="small">
          <FontAwesomeIcon
            icon={expanded ? faChevronUp : faChevronDown}
          />
        </IconButton>
      </Box>

      {/* Content */}
      <Collapse in={expanded}>
        <Box sx={{ p: 2, pt: 0 }}>
          <Alert severity="info" sx={{ mb: 2 }}>
            These participants have been invited to join the organization and will be
            automatically added to this exercise once they accept.
          </Alert>

          <Stack spacing={2}>
            {pendingAssignments.map(assignment => (
              <Paper
                key={assignment.id}
                variant="outlined"
                sx={{ p: 2 }}
              >
                <Stack spacing={1}>
                  {/* Email and Status */}
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      {getStatusIcon(assignment.invitationStatus)}
                      <Typography variant="body1" fontWeight="medium">
                        {assignment.email}
                      </Typography>
                    </Box>
                    <Chip
                      label={assignment.invitationStatus}
                      color={getStatusColor(assignment.invitationStatus)}
                      size="small"
                    />
                  </Box>

                  {/* Role and Expiration */}
                  <Box sx={{ display: 'flex', gap: 2, ml: 4 }}>
                    <Typography variant="body2" color="text.secondary">
                      Role: <strong>{assignment.exerciseRole}</strong>
                    </Typography>
                    {assignment.invitationExpiresAt && (
                      <Typography variant="body2" color="text.secondary">
                        •{' '}
                        {assignment.invitationStatus === 'Expired'
                          ? 'Expired'
                          : `Expires ${formatDistanceToNow(new Date(assignment.invitationExpiresAt), { addSuffix: true })}`}
                      </Typography>
                    )}
                  </Box>

                  {/* Resend Button */}
                  {onResend && (assignment.invitationStatus === 'Pending' || assignment.invitationStatus === 'Expired') && (
                    <Box sx={{ ml: 4, mt: 1 }}>
                      <CobraSecondaryButton
                        size="small"
                        onClick={() => handleResend(
                          assignment.organizationInviteId,
                          assignment.email,
                        )}
                        disabled={
                          isResending
                          || resendingId === assignment.organizationInviteId
                        }
                      >
                        {resendingId === assignment.organizationInviteId ? (
                          <>
                            <CircularProgress size={16} sx={{ mr: 1 }} />
                            Sending...
                          </>
                        ) : (
                          'Resend Invitation'
                        )}
                      </CobraSecondaryButton>
                    </Box>
                  )}
                </Stack>
              </Paper>
            ))}
          </Stack>
        </Box>
      </Collapse>
    </Paper>
  )
}

export default PendingInvitationsList
