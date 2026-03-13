/**
 * ImportUploadStep Component
 *
 * File upload area for bulk participant import.
 * Allows users to select CSV or XLSX files containing participant data.
 */

import { useState, useRef, useCallback } from 'react'
import { Box, Typography, Paper, Alert, Stack, Link } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faUpload,
  faSpinner,
  faFileArrowDown,
} from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { bulkImportService } from '../../services/bulkImportService'

const SUPPORTED_EXTENSIONS = ['.csv', '.xlsx']
const MAX_FILE_SIZE_MB = 10
const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024

interface ImportUploadStepProps {
  /** Called when a file is selected and passes validation */
  onFileSelected: (file: File) => void;
  /** Whether the upload is in progress */
  isUploading: boolean;
  /** Error message from upload */
  error: string | null;
  /** Exercise ID for template download */
  exerciseId: string;
}

export const ImportUploadStep = ({
  onFileSelected,
  isUploading,
  error,
  exerciseId,
}: ImportUploadStepProps) => {
  const theme = useTheme()
  const [validationError, setValidationError] = useState<string | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const validateFile = (file: File): string | null => {
    // Check file extension
    const extension = '.' + file.name.split('.').pop()?.toLowerCase()
    if (!SUPPORTED_EXTENSIONS.includes(extension)) {
      return `Unsupported file format. Please use: ${SUPPORTED_EXTENSIONS.join(', ')}`
    }

    // Check file size
    if (file.size > MAX_FILE_SIZE_BYTES) {
      return `File size exceeds the maximum of ${MAX_FILE_SIZE_MB} MB`
    }

    return null
  }

  const handleFileSelect = useCallback(
    (file: File) => {
      const error = validateFile(file)
      if (error) {
        setValidationError(error)
        return
      }

      setValidationError(null)
      onFileSelected(file)
    },
    [onFileSelected],
  )

  const handleBrowseClick = () => {
    fileInputRef.current?.click()
  }

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (files && files.length > 0) {
      handleFileSelect(files[0])
    }
    // Reset input so the same file can be selected again
    e.target.value = ''
  }

  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (!isUploading) {
      setIsDragging(true)
    }
  }, [isUploading])

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
  }, [])

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(false)
  }, [])

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault()
      e.stopPropagation()
      setIsDragging(false)

      if (isUploading) return

      const files = e.dataTransfer.files
      if (files && files.length > 0) {
        handleFileSelect(files[0])
      }
    },
    [isUploading, handleFileSelect],
  )

  const csvTemplateUrl = bulkImportService.getTemplateUrl(exerciseId, 'csv')
  const xlsxTemplateUrl = bulkImportService.getTemplateUrl(exerciseId, 'xlsx')

  return (
    <Box>
      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        accept={SUPPORTED_EXTENSIONS.join(',')}
        onChange={handleFileInputChange}
        style={{ display: 'none' }}
      />

      {/* Upload Area */}
      <Paper
        onClick={!isUploading ? handleBrowseClick : undefined}
        onDragEnter={handleDragEnter}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        sx={{
          p: 4,
          textAlign: 'center',
          border: '2px dashed',
          borderColor:
            validationError || error
              ? 'error.main'
              : isDragging
                ? 'primary.main'
                : 'grey.400',
          borderRadius: 2,
          backgroundColor: isDragging
            ? 'action.selected'
            : 'background.paper',
          cursor: !isUploading ? 'pointer' : 'default',
          transition: 'all 0.2s ease-in-out',
          '&:hover': !isUploading
            ? {
              borderColor: 'primary.main',
              backgroundColor: 'action.hover',
            }
            : {},
        }}
      >
        {isUploading ? (
          <Box>
            <FontAwesomeIcon
              icon={faSpinner}
              spin
              size="3x"
              style={{ color: theme.palette.semantic.info, marginBottom: 16 }}
            />
            <Typography variant="h6" gutterBottom>
              Processing file...
            </Typography>
          </Box>
        ) : (
          <Box>
            <FontAwesomeIcon
              icon={faUpload}
              size="3x"
              style={{ color: theme.palette.neutral[500], marginBottom: 16 }}
            />
            <Typography variant="h6" gutterBottom>
              {isDragging ? 'Drop file here' : 'Drag & drop or click to select'}
            </Typography>
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{ mb: 2 }}
            >
              CSV or XLSX files • Max 500 rows, 10 MB
            </Typography>
            <CobraPrimaryButton
              onClick={e => {
                e.stopPropagation()
                handleBrowseClick()
              }}
            >
              Choose File
            </CobraPrimaryButton>
          </Box>
        )}
      </Paper>

      {/* Validation Error */}
      {validationError && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {validationError}
        </Alert>
      )}

      {/* Upload Error */}
      {error && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {error}
        </Alert>
      )}

      {/* Template Download Links */}
      <Stack
        direction="row"
        spacing={1}
        alignItems="center"
        sx={{ mt: 3 }}
      >
        <FontAwesomeIcon
          icon={faFileArrowDown}
          style={{ color: theme.palette.semantic.info }}
        />
        <Typography variant="body2">
          Download template:{' '}
          <Link href={csvTemplateUrl} target="_blank" rel="noopener">
            CSV
          </Link>
          {' | '}
          <Link href={xlsxTemplateUrl} target="_blank" rel="noopener">
            XLSX
          </Link>
        </Typography>
      </Stack>
    </Box>
  )
}

export default ImportUploadStep
