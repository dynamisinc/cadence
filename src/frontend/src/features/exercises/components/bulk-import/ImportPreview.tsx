/**
 * ImportPreview Component
 *
 * Displays classification results and allows filtering before confirming import.
 * Shows summary counts and detailed row-by-row breakdown.
 */

import { useState, useMemo } from 'react'
import {
  Box,
  Typography,
  Chip,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Paper,
  Stack,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faUserPlus,
  faUserPen,
  faEnvelope,
  faCircleXmark,
  faArrowRight,
  faTriangleExclamation,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import type {
  ImportPreviewResult,
  ParticipantClassification,
} from '../../types/bulkImport'

interface ImportPreviewProps {
  /** Preview result with classified rows */
  preview: ImportPreviewResult;
  /** Whether the confirmation is in progress */
  isConfirming: boolean;
  /** Called when user confirms import */
  onConfirm: () => void;
  /** Called when user cancels */
  onCancel: () => void;
  /** Error message */
  error: string | null;
}

export const ImportPreview = ({
  preview,
  isConfirming,
  onConfirm,
  onCancel,
  error,
}: ImportPreviewProps) => {
  const theme = useTheme()
  const [activeFilter, setActiveFilter] = useState<
    ParticipantClassification | 'all'
  >('all')

  // Filter rows based on active filter
  const filteredRows = useMemo(() => {
    if (activeFilter === 'all') {
      return preview.rows
    }
    return preview.rows.filter(
      row => row.classification === activeFilter,
    )
  }, [preview.rows, activeFilter])

  // Get classification color
  const getClassificationColor = (
    classification: ParticipantClassification,
  ) => {
    switch (classification) {
      case 'Assign':
        return theme.palette.success.main
      case 'Update':
        return theme.palette.warning.main
      case 'Invite':
        return theme.palette.info.main
      case 'Error':
        return theme.palette.error.main
      default:
        return theme.palette.grey[500]
    }
  }

  // Get classification icon
  const getClassificationIcon = (
    classification: ParticipantClassification,
  ) => {
    switch (classification) {
      case 'Assign':
        return faUserPlus
      case 'Update':
        return faUserPen
      case 'Invite':
        return faEnvelope
      case 'Error':
        return faCircleXmark
      default:
        return faCircleXmark
    }
  }

  return (
    <Box>
      {/* Summary Bar */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Import Preview
        </Typography>
        <Stack direction="row" spacing={2} flexWrap="wrap">
          <Chip
            label={`${preview.assignCount} to assign`}
            sx={{
              bgcolor: theme.palette.success.light,
              color: theme.palette.success.dark,
            }}
            onClick={() => setActiveFilter('Assign')}
          />
          <Chip
            label={`${preview.updateCount} to update`}
            sx={{
              bgcolor: theme.palette.warning.light,
              color: theme.palette.warning.dark,
            }}
            onClick={() => setActiveFilter('Update')}
          />
          <Chip
            label={`${preview.inviteCount} to invite`}
            sx={{
              bgcolor: theme.palette.info.light,
              color: theme.palette.info.dark,
            }}
            onClick={() => setActiveFilter('Invite')}
          />
          {preview.errorCount > 0 && (
            <Chip
              label={`${preview.errorCount} errors`}
              sx={{
                bgcolor: theme.palette.error.light,
                color: theme.palette.error.dark,
              }}
              onClick={() => setActiveFilter('Error')}
            />
          )}
          {activeFilter !== 'all' && (
            <Chip
              label="Show All"
              variant="outlined"
              onClick={() => setActiveFilter('all')}
            />
          )}
        </Stack>
      </Paper>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Classification Table */}
      <Paper sx={{ overflow: 'auto', maxHeight: '400px' }}>
        <Table stickyHeader size="small">
          <TableHead>
            <TableRow>
              <TableCell>Row #</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Display Name</TableCell>
              <TableCell>Exercise Role</TableCell>
              <TableCell>Classification</TableCell>
              <TableCell>Details</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredRows.map(row => (
              <TableRow
                key={row.parsedRow.rowNumber}
                sx={{
                  bgcolor:
                    row.classification === 'Error'
                      ? theme.palette.error.light + '20'
                      : 'inherit',
                }}
              >
                <TableCell>{row.parsedRow.rowNumber}</TableCell>
                <TableCell>{row.parsedRow.email}</TableCell>
                <TableCell>
                  {row.parsedRow.displayName ||
                    row.existingDisplayName ||
                    '-'}
                </TableCell>
                <TableCell>{row.parsedRow.exerciseRole}</TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    icon={
                      <FontAwesomeIcon
                        icon={getClassificationIcon(row.classification)}
                        style={{ fontSize: '0.875rem' }}
                      />
                    }
                    label={row.classificationLabel}
                    sx={{
                      bgcolor: getClassificationColor(row.classification) + '20',
                      color: getClassificationColor(row.classification),
                      borderColor: getClassificationColor(row.classification),
                      border: '1px solid',
                    }}
                  />
                </TableCell>
                <TableCell>
                  <Box>
                    {/* Role change indicator */}
                    {row.isRoleChange && row.currentExerciseRole && (
                      <Typography variant="caption" display="block">
                        Role Change: {row.currentExerciseRole}{' '}
                        <FontAwesomeIcon icon={faArrowRight} size="xs" />{' '}
                        {row.parsedRow.exerciseRole}
                      </Typography>
                    )}

                    {/* New account indicator */}
                    {row.isNewAccount && (
                      <Typography variant="caption" display="block">
                        New Account
                      </Typography>
                    )}

                    {/* Pending invitation indicator */}
                    {row.hasPendingInvitation && (
                      <Typography
                        variant="caption"
                        display="block"
                        color="warning.main"
                      >
                        <FontAwesomeIcon
                          icon={faTriangleExclamation}
                          size="xs"
                        />{' '}
                        Has pending invitation
                      </Typography>
                    )}

                    {/* Notes */}
                    {row.notes.map((note, idx) => (
                      <Typography
                        key={idx}
                        variant="caption"
                        display="block"
                        color="text.secondary"
                      >
                        {note}
                      </Typography>
                    ))}

                    {/* Error message */}
                    {row.errorMessage && (
                      <Typography
                        variant="caption"
                        display="block"
                        color="error.main"
                      >
                        {row.errorMessage}
                      </Typography>
                    )}
                  </Box>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      {/* Action Buttons */}
      <Stack
        direction="row"
        spacing={2}
        justifyContent="flex-end"
        sx={{ mt: 3 }}
      >
        <CobraSecondaryButton onClick={onCancel} disabled={isConfirming}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={onConfirm}
          disabled={!preview.hasProcessableRows || isConfirming}
        >
          {isConfirming ? 'Processing...' : 'Confirm Import'}
        </CobraPrimaryButton>
      </Stack>

      {/* Warning if no processable rows */}
      {!preview.hasProcessableRows && (
        <Alert severity="warning" sx={{ mt: 2 }}>
          No valid rows to import. Please fix errors and try again.
        </Alert>
      )}
    </Box>
  )
}

export default ImportPreview
