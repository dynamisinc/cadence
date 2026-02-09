/**
 * ImportResults Component
 *
 * Displays the results of a bulk participant import operation.
 * Shows success counts and any errors that occurred.
 */

import { useState } from 'react';
import {
  Box,
  Typography,
  Chip,
  Alert,
  Paper,
  Stack,
  Collapse,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Divider,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faCheck,
  faClock,
  faCircleXmark,
  faChevronDown,
  faChevronRight,
} from '@fortawesome/free-solid-svg-icons';
import { CobraPrimaryButton } from '@/theme/styledComponents';
import type { BulkImportResult } from '../../types/bulkImport';

interface ImportResultsProps {
  /** Import result data */
  result: BulkImportResult;
  /** Called when user closes the results */
  onClose: () => void;
}

export const ImportResults = ({
  result,
  onClose,
}: ImportResultsProps) => {
  const theme = useTheme();
  const [expandedSections, setExpandedSections] = useState({
    assigned: true,
    invited: true,
    errors: true,
  });

  const toggleSection = (section: 'assigned' | 'invited' | 'errors') => {
    setExpandedSections((prev) => ({
      ...prev,
      [section]: !prev[section],
    }));
  };

  // Filter rows by status
  const assignedRows = result.rowOutcomes.filter(
    (row) => row.classification === 'Assign' && row.status === 'Success'
  );
  const updatedRows = result.rowOutcomes.filter(
    (row) => row.classification === 'Update' && row.status === 'Success'
  );
  const invitedRows = result.rowOutcomes.filter(
    (row) => row.classification === 'Invite' && row.status === 'Success'
  );
  const errorRows = result.rowOutcomes.filter(
    (row) => row.status === 'Failed'
  );
  const skippedRows = result.rowOutcomes.filter(
    (row) => row.status === 'Skipped'
  );

  return (
    <Box>
      {/* Success Banner */}
      <Alert severity="success" sx={{ mb: 3 }}>
        <Typography variant="h6">Import completed successfully</Typography>
        <Typography variant="body2">
          Processed {result.assignedCount + result.updatedCount + result.invitedCount + result.errorCount + result.skippedCount} rows
        </Typography>
      </Alert>

      {/* Summary Counters */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack direction="row" spacing={2} flexWrap="wrap">
          {result.assignedCount > 0 && (
            <Chip
              label={`${result.assignedCount} Assigned`}
              sx={{
                bgcolor: theme.palette.success.light,
                color: theme.palette.success.dark,
              }}
            />
          )}
          {result.updatedCount > 0 && (
            <Chip
              label={`${result.updatedCount} Updated`}
              sx={{
                bgcolor: theme.palette.info.light,
                color: theme.palette.info.dark,
              }}
            />
          )}
          {result.invitedCount > 0 && (
            <Chip
              label={`${result.invitedCount} Invited`}
              sx={{
                bgcolor: theme.palette.warning.light,
                color: theme.palette.warning.dark,
              }}
            />
          )}
          {result.errorCount > 0 && (
            <Chip
              label={`${result.errorCount} Errors`}
              sx={{
                bgcolor: theme.palette.error.light,
                color: theme.palette.error.dark,
              }}
            />
          )}
          {result.skippedCount > 0 && (
            <Chip
              label={`${result.skippedCount} Skipped`}
              sx={{
                bgcolor: theme.palette.grey[300],
                color: theme.palette.grey[700],
              }}
            />
          )}
        </Stack>
      </Paper>

      {/* Assigned Participants Section */}
      {(assignedRows.length > 0 || updatedRows.length > 0) && (
        <Paper sx={{ mb: 2 }}>
          <Box
            sx={{
              p: 2,
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: 1,
            }}
            onClick={() => toggleSection('assigned')}
          >
            <FontAwesomeIcon
              icon={expandedSections.assigned ? faChevronDown : faChevronRight}
            />
            <Typography variant="h6">
              Assigned & Updated Participants ({assignedRows.length + updatedRows.length})
            </Typography>
          </Box>
          <Collapse in={expandedSections.assigned}>
            <Divider />
            <List dense sx={{ maxHeight: '300px', overflow: 'auto' }}>
              {[...assignedRows, ...updatedRows].map((row) => (
                <ListItem key={row.rowNumber}>
                  <ListItemIcon>
                    <FontAwesomeIcon
                      icon={faCheck}
                      style={{ color: theme.palette.success.main }}
                    />
                  </ListItemIcon>
                  <ListItemText
                    primary={row.email}
                    secondary={`Row ${row.rowNumber} - ${row.exerciseRole}${row.classification === 'Update' ? ' (Updated)' : ''}`}
                  />
                </ListItem>
              ))}
            </List>
          </Collapse>
        </Paper>
      )}

      {/* Invitations Sent Section */}
      {invitedRows.length > 0 && (
        <Paper sx={{ mb: 2 }}>
          <Box
            sx={{
              p: 2,
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: 1,
            }}
            onClick={() => toggleSection('invited')}
          >
            <FontAwesomeIcon
              icon={expandedSections.invited ? faChevronDown : faChevronRight}
            />
            <Typography variant="h6">
              Invitations Sent ({invitedRows.length})
            </Typography>
          </Box>
          <Collapse in={expandedSections.invited}>
            <Divider />
            <List dense sx={{ maxHeight: '300px', overflow: 'auto' }}>
              {invitedRows.map((row) => (
                <ListItem key={row.rowNumber}>
                  <ListItemIcon>
                    <FontAwesomeIcon
                      icon={faClock}
                      style={{ color: theme.palette.warning.main }}
                    />
                  </ListItemIcon>
                  <ListItemText
                    primary={row.email}
                    secondary={`Row ${row.rowNumber} - ${row.exerciseRole} - ${row.message || 'Invitation sent'}`}
                  />
                </ListItem>
              ))}
            </List>
          </Collapse>
        </Paper>
      )}

      {/* Errors Section */}
      {errorRows.length > 0 && (
        <Paper sx={{ mb: 2 }}>
          <Box
            sx={{
              p: 2,
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: 1,
            }}
            onClick={() => toggleSection('errors')}
          >
            <FontAwesomeIcon
              icon={expandedSections.errors ? faChevronDown : faChevronRight}
            />
            <Typography variant="h6" color="error">
              Errors ({errorRows.length})
            </Typography>
          </Box>
          <Collapse in={expandedSections.errors}>
            <Divider />
            <List dense sx={{ maxHeight: '300px', overflow: 'auto' }}>
              {errorRows.map((row) => (
                <ListItem key={row.rowNumber}>
                  <ListItemIcon>
                    <FontAwesomeIcon
                      icon={faCircleXmark}
                      style={{ color: theme.palette.error.main }}
                    />
                  </ListItemIcon>
                  <ListItemText
                    primary={row.email}
                    secondary={`Row ${row.rowNumber} - ${row.message || 'Import failed'}`}
                  />
                </ListItem>
              ))}
            </List>
          </Collapse>
        </Paper>
      )}

      {/* Skipped Section */}
      {skippedRows.length > 0 && (
        <Paper sx={{ mb: 2 }}>
          <Alert severity="info" sx={{ mb: 0 }}>
            <Typography variant="body2">
              {skippedRows.length} rows were skipped (no action needed)
            </Typography>
          </Alert>
        </Paper>
      )}

      {/* Action Button */}
      <Stack direction="row" justifyContent="flex-end" sx={{ mt: 3 }}>
        <CobraPrimaryButton onClick={onClose}>Done</CobraPrimaryButton>
      </Stack>
    </Box>
  );
};

export default ImportResults;
