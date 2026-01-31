/**
 * OrganizationStatusActions - Status display and action buttons for organizations
 *
 * Handles archive, deactivate, and restore actions with confirmation dialogs.
 *
 * @module features/organizations/components
 */
import type { FC } from 'react'
import { Box, Typography, Paper, Alert, Divider } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArchive, faBan, faRotateLeft } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraDeleteButton,
} from '@/theme/styledComponents'
import { StatusChip } from '@/shared/components'
import type { OrgStatus } from '../types'

interface OrganizationStatusActionsProps {
  status: OrgStatus
  isPending: boolean
  onArchive: () => void
  onDeactivate: () => void
  onRestore: () => void
}

export const OrganizationStatusActions: FC<OrganizationStatusActionsProps> = ({
  status,
  isPending,
  onArchive,
  onDeactivate,
  onRestore,
}) => {
  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        Organization Status
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Current status: <StatusChip status={status} />
      </Typography>

      <Divider sx={{ my: 2 }} />

      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
        {status === 'Active' && (
          <>
            <CobraSecondaryButton
              startIcon={<FontAwesomeIcon icon={faArchive} />}
              onClick={onArchive}
              disabled={isPending}
            >
              Archive Organization
            </CobraSecondaryButton>
            <CobraDeleteButton
              startIcon={<FontAwesomeIcon icon={faBan} />}
              onClick={onDeactivate}
              disabled={isPending}
            >
              Deactivate Organization
            </CobraDeleteButton>
          </>
        )}

        {status === 'Archived' && (
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
            onClick={onRestore}
            disabled={isPending}
          >
            Restore to Active
          </CobraPrimaryButton>
        )}

        {status === 'Inactive' && (
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
            onClick={onRestore}
            disabled={isPending}
          >
            Restore to Active
          </CobraPrimaryButton>
        )}
      </Box>

      {status !== 'Active' && (
        <Alert severity="warning" sx={{ mt: 2 }}>
          This organization is {status.toLowerCase()}. Users cannot access it until it is restored.
        </Alert>
      )}
    </Paper>
  )
}

export default OrganizationStatusActions
