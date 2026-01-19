/**
 * FileUploadStep Component
 *
 * First step of the import wizard - file upload with drag and drop support.
 */

import { useState, useCallback, useRef } from 'react'
import {
  Box,
  Typography,
  Paper,
  Alert,
  LinearProgress,
  Stack,
  Link,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faFileExcel,
  faCloudArrowUp,
  faCheck,
  faXmark,
} from '@fortawesome/free-solid-svg-icons'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '../../../theme/styledComponents'
import type { FileAnalysisResult } from '../types'

const SUPPORTED_EXTENSIONS = ['.xlsx', '.xls', '.csv']
const MAX_FILE_SIZE_MB = 10
const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024

interface FileUploadStepProps {
  /** Called when file is uploaded and analyzed */
  onFileAnalyzed: (result: FileAnalysisResult) => void
  /** Function to upload and analyze file */
  uploadFile: (file: File) => Promise<FileAnalysisResult>
  /** Is upload in progress? */
  isUploading?: boolean
  /** Error message from upload */
  uploadError?: string | null
  /** URL for template download (optional) */
  templateUrl?: string
}

export const FileUploadStep = ({
  onFileAnalyzed,
  uploadFile,
  isUploading = false,
  uploadError = null,
  templateUrl,
}: FileUploadStepProps) => {
  const [isDragOver, setIsDragOver] = useState(false)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  const [isProcessing, setIsProcessing] = useState(false)
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

  const handleFileSelect = useCallback(async (file: File) => {
    const error = validateFile(file)
    if (error) {
      setValidationError(error)
      setSelectedFile(null)
      return
    }

    setValidationError(null)
    setSelectedFile(file)
    setIsProcessing(true)

    try {
      const result = await uploadFile(file)
      onFileAnalyzed(result)
    } catch {
      setValidationError('Failed to process file. Please try again.')
    } finally {
      setIsProcessing(false)
    }
  }, [uploadFile, onFileAnalyzed])

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragOver(true)
  }, [])

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragOver(false)
  }, [])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragOver(false)

    const files = e.dataTransfer.files
    if (files.length > 0) {
      handleFileSelect(files[0])
    }
  }, [handleFileSelect])

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

  const handleRemoveFile = () => {
    setSelectedFile(null)
    setValidationError(null)
  }

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  const isLoading = isUploading || isProcessing

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

      {/* Drop Zone */}
      <Paper
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={!selectedFile && !isLoading ? handleBrowseClick : undefined}
        sx={{
          p: 4,
          textAlign: 'center',
          border: '2px dashed',
          borderColor: isDragOver
            ? 'primary.main'
            : validationError || uploadError
              ? 'error.main'
              : 'grey.400',
          borderRadius: 2,
          backgroundColor: isDragOver
            ? 'action.hover'
            : selectedFile
              ? 'success.light'
              : 'background.paper',
          cursor: !selectedFile && !isLoading ? 'pointer' : 'default',
          transition: 'all 0.2s ease-in-out',
          '&:hover': !selectedFile && !isLoading ? {
            borderColor: 'primary.main',
            backgroundColor: 'action.hover',
          } : {},
        }}
      >
        {isLoading ? (
          <Box>
            <FontAwesomeIcon
              icon={faCloudArrowUp}
              size="3x"
              style={{ color: '#1976d2', marginBottom: 16 }}
            />
            <Typography variant="h6" gutterBottom>
              Processing file...
            </Typography>
            <LinearProgress sx={{ mt: 2, maxWidth: 300, mx: 'auto' }} />
          </Box>
        ) : selectedFile ? (
          <Box>
            <FontAwesomeIcon
              icon={faCheck}
              size="3x"
              style={{ color: '#2e7d32', marginBottom: 16 }}
            />
            <Typography variant="h6" gutterBottom>
              {selectedFile.name}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Size: {formatFileSize(selectedFile.size)}
            </Typography>
            <CobraSecondaryButton
              onClick={e => {
                e.stopPropagation()
                handleRemoveFile()
              }}
              startIcon={<FontAwesomeIcon icon={faXmark} />}
              sx={{ mt: 2 }}
            >
              Remove
            </CobraSecondaryButton>
          </Box>
        ) : (
          <Box>
            <FontAwesomeIcon
              icon={faFileExcel}
              size="3x"
              style={{ color: isDragOver ? '#1976d2' : '#757575', marginBottom: 16 }}
            />
            <Typography variant="h6" gutterBottom>
              Drag and drop your Excel file here
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              or
            </Typography>
            <CobraPrimaryButton
              onClick={e => {
                e.stopPropagation()
                handleBrowseClick()
              }}
            >
              Browse Files
            </CobraPrimaryButton>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
              Supported formats: {SUPPORTED_EXTENSIONS.join(', ')}
            </Typography>
            <Typography variant="caption" color="text.secondary" display="block">
              Maximum size: {MAX_FILE_SIZE_MB} MB
            </Typography>
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
      {uploadError && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {uploadError}
        </Alert>
      )}

      {/* Template Download Link */}
      {templateUrl && (
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mt: 3 }}>
          <FontAwesomeIcon icon={faFileExcel} style={{ color: '#1976d2' }} />
          <Typography variant="body2">
            Don&apos;t have a file ready?{' '}
            <Link href={templateUrl} target="_blank" rel="noopener">
              Download MSEL Template
            </Link>
          </Typography>
        </Stack>
      )}
    </Box>
  )
}

export default FileUploadStep
