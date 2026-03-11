/**
 * WorksheetSelectionStep Component
 *
 * Second step of the import wizard - select worksheet and preview data.
 */

import { useState, useEffect } from 'react'
import {
  Box,
  Typography,
  Paper,
  Alert,
  Stack,
  RadioGroup,
  FormControlLabel,
  Radio,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Skeleton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faTableCellsLarge,
  faFileExcel,
  faCheck,
} from '@fortawesome/free-solid-svg-icons'

import { useTheme } from '@mui/material/styles'
import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import type { FileAnalysisResult, WorksheetSelectionResult } from '../types'

interface WorksheetSelectionStepProps {
  /** File analysis result from previous step */
  analysisResult: FileAnalysisResult
  /** Selected worksheet result (if any) */
  selectionResult?: WorksheetSelectionResult | null
  /** Is selection in progress? */
  isLoading?: boolean
  /** Error message */
  error?: string | null
  /** Called when worksheet is selected */
  onSelectWorksheet: (worksheetIndex: number) => Promise<void>
  /** Called when user wants to go back */
  onBack: () => void
  /** Called when user confirms selection */
  onConfirm: () => void
}

export const WorksheetSelectionStep = ({
  analysisResult,
  selectionResult,
  isLoading = false,
  error = null,
  onSelectWorksheet,
  onBack,
  onConfirm,
}: WorksheetSelectionStepProps) => {
  const initialIndex = analysisResult.worksheets.findIndex(w => w.looksLikeMsel) >= 0
    ? analysisResult.worksheets.findIndex(w => w.looksLikeMsel)
    : 0

  const [selectedIndex, setSelectedIndex] = useState<number>(initialIndex)
  const theme = useTheme()

  // Auto-select the initial worksheet when the component mounts
  useEffect(() => {
    // Only trigger if we don't have a selection result yet
    if (!selectionResult && analysisResult.worksheets.length > 0) {
      onSelectWorksheet(initialIndex)
    }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const handleWorksheetChange = async (index: number) => {
    setSelectedIndex(index)
    await onSelectWorksheet(index)
  }

  const selectedWorksheet = analysisResult.worksheets[selectedIndex]

  const getConfidenceColor = (confidence: number): 'success' | 'warning' | 'default' => {
    if (confidence >= 70) return 'success'
    if (confidence >= 40) return 'warning'
    return 'default'
  }

  return (
    <Box>
      {/* File Info */}
      <Paper sx={{ p: 2, mb: 3, backgroundColor: 'grey.50' }}>
        <Stack direction="row" spacing={2} alignItems="center">
          <FontAwesomeIcon icon={faFileExcel} size="lg" style={{ color: theme.palette.semantic.excel }} />
          <Box>
            <Typography variant="subtitle1" fontWeight="medium">
              {analysisResult.fileName}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {(analysisResult.fileSize / 1024).toFixed(1)} KB &bull;{' '}
              {analysisResult.worksheets.length} worksheet{analysisResult.worksheets.length !== 1 ? 's' : ''}
            </Typography>
          </Box>
        </Stack>
      </Paper>

      {/* Worksheet Selection */}
      <Typography variant="h6" gutterBottom>
        Select worksheet to import:
      </Typography>

      <RadioGroup
        value={selectedIndex}
        onChange={e => handleWorksheetChange(Number(e.target.value))}
      >
        <Paper variant="outlined" sx={{ mb: 3 }}>
          {analysisResult.worksheets.map((worksheet, index) => (
            <Box
              key={index}
              sx={{
                p: 2,
                borderBottom: index < analysisResult.worksheets.length - 1 ? 1 : 0,
                borderColor: 'divider',
                backgroundColor: selectedIndex === index ? 'action.selected' : 'transparent',
              }}
            >
              <FormControlLabel
                value={index}
                control={<Radio />}
                label={
                  <Stack direction="row" spacing={2} alignItems="center" sx={{ flex: 1 }}>
                    <FontAwesomeIcon icon={faTableCellsLarge} />
                    <Box sx={{ flex: 1 }}>
                      <Typography variant="subtitle2">
                        {worksheet.name}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {worksheet.rowCount} row{worksheet.rowCount !== 1 ? 's' : ''} &bull;{' '}
                        {worksheet.columnCount} column{worksheet.columnCount !== 1 ? 's' : ''}
                      </Typography>
                    </Box>
                    {worksheet.looksLikeMsel && (
                      <Chip
                        size="small"
                        icon={<FontAwesomeIcon icon={faCheck} />}
                        label={`MSEL detected (${worksheet.mselConfidence}%)`}
                        color={getConfidenceColor(worksheet.mselConfidence)}
                      />
                    )}
                  </Stack>
                }
                sx={{ width: '100%', m: 0 }}
              />
            </Box>
          ))}
        </Paper>
      </RadioGroup>

      {/* Preview Table */}
      {isLoading ? (
        <Box>
          <Typography variant="subtitle2" gutterBottom>
            Loading preview...
          </Typography>
          <Skeleton variant="rectangular" height={200} />
        </Box>
      ) : selectionResult && selectionResult.previewRows.length > 0 ? (
        <Box>
          <Typography variant="subtitle2" gutterBottom>
            Preview (first {selectionResult.previewRowCount} rows):
          </Typography>
          <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 300 }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  {selectionResult.columns.map(col => (
                    <TableCell key={col.index} sx={{ fontWeight: 'bold', whiteSpace: 'nowrap' }}>
                      {col.header}
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {selectionResult.previewRows.map((row, rowIndex) => (
                  <TableRow key={rowIndex}>
                    {selectionResult.columns.map(col => (
                      <TableCell key={col.index} sx={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {formatCellValue(row[col.header])}
                      </TableCell>
                    ))}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Box>
      ) : selectedWorksheet && (
        <Alert severity="info">
          Select a worksheet to preview its contents.
        </Alert>
      )}

      {/* Error */}
      {error && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {error}
        </Alert>
      )}

      {/* Navigation Buttons */}
      <Stack direction="row" spacing={2} justifyContent="flex-end" sx={{ mt: 3 }}>
        <CobraSecondaryButton onClick={onBack}>
          Back
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={onConfirm}
          disabled={!selectionResult || isLoading}
        >
          Next
        </CobraPrimaryButton>
      </Stack>
    </Box>
  )
}

function formatCellValue(value: unknown): string {
  if (value === null || value === undefined) return ''
  if (typeof value === 'string') return value
  if (typeof value === 'number') return value.toString()
  if (typeof value === 'boolean') return value ? 'Yes' : 'No'
  if (value instanceof Date) return value.toLocaleString()
  return String(value)
}

export default WorksheetSelectionStep
