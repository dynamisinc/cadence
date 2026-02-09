/**
 * BulkImportDialog Component
 *
 * Main dialog wrapper that orchestrates the bulk participant import flow.
 * Manages the wizard-style flow through upload, preview, and results steps.
 */

import {
  Dialog,
  DialogTitle,
  DialogContent,
  Box,
  Typography,
  IconButton,
  Stack,
  CircularProgress,
} from '@mui/material';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faXmark, faFileImport } from '@fortawesome/free-solid-svg-icons';
import { useParticipantImport } from '../../hooks/useParticipantImport';
import { ImportUploadStep } from './ImportUploadStep';
import { ImportPreview } from './ImportPreview';
import { ImportResults } from './ImportResults';

interface BulkImportDialogProps {
  /** Whether the dialog is open */
  open: boolean;
  /** Called when the dialog should close */
  onClose: () => void;
  /** Exercise ID to import participants into */
  exerciseId: string;
  /** Called when import is complete */
  onImportComplete?: () => void;
}

export const BulkImportDialog = ({
  open,
  onClose,
  exerciseId,
  onImportComplete,
}: BulkImportDialogProps) => {
  const {
    step,
    parseResult,
    previewResult,
    importResult,
    error,
    isLoading,
    uploadFile,
    confirmImport,
    goBackToUpload,
    reset,
  } = useParticipantImport(exerciseId);

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleImportComplete = () => {
    if (onImportComplete) {
      onImportComplete();
    }
    handleClose();
  };

  // Render content based on current step
  const renderContent = () => {
    switch (step) {
      case 'idle':
      case 'uploading':
        return (
          <ImportUploadStep
            onFileSelected={uploadFile}
            isUploading={step === 'uploading'}
            error={error}
            exerciseId={exerciseId}
          />
        );

      case 'preview':
        if (!previewResult) return null;
        return (
          <ImportPreview
            preview={previewResult}
            isConfirming={false}
            onConfirm={confirmImport}
            onCancel={goBackToUpload}
            error={error}
          />
        );

      case 'confirming':
        return (
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              minHeight: '300px',
            }}
          >
            <CircularProgress size={60} sx={{ mb: 2 }} />
            <Typography variant="h6">Processing import...</Typography>
            <Typography variant="body2" color="text.secondary">
              This may take a moment
            </Typography>
          </Box>
        );

      case 'results':
        if (!importResult) return null;
        return (
          <ImportResults
            result={importResult}
            onClose={handleImportComplete}
          />
        );

      default:
        return null;
    }
  };

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="md"
      fullWidth
      PaperProps={{
        sx: {
          minHeight: '60vh',
        },
      }}
    >
      <DialogTitle>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <FontAwesomeIcon icon={faFileImport} />
            <Typography variant="h6">Bulk Import Participants</Typography>
          </Box>
          <IconButton onClick={handleClose} size="small" disabled={step === 'confirming'}>
            <FontAwesomeIcon icon={faXmark} />
          </IconButton>
        </Stack>
      </DialogTitle>

      <DialogContent dividers>
        <Box sx={{ minHeight: '400px' }}>{renderContent()}</Box>
      </DialogContent>
    </Dialog>
  );
};

export default BulkImportDialog;
