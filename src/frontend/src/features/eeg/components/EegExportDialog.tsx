/**
 * EEG Export Dialog
 *
 * Dialog for exporting EEG data to Excel or JSON format for AAR preparation.
 * Allows users to select which sections to include in the export.
 */

import { useState } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControlLabel,
  Checkbox,
  FormGroup,
  Box,
  Typography,
  Alert,
  CircularProgress,
  Divider,
  RadioGroup,
  Radio,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faFileExcel,
  faDownload,
  faXmark,
  faFileCode,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import { eegExportService, type ExportEegOptions } from '../services/eegService'
import type { EegCoverageDto } from '../types'

interface EegExportDialogProps {
  open: boolean
  onClose: () => void
  exerciseId: string
  exerciseName?: string
  coverage?: EegCoverageDto | null
}

export const EegExportDialog = ({
  open,
  onClose,
  exerciseId,
  exerciseName,
  coverage,
}: EegExportDialogProps) => {
  const [format, setFormat] = useState<'xlsx' | 'json'>('xlsx')
  const [includeSummary, setIncludeSummary] = useState(true)
  const [includeByCapability, setIncludeByCapability] = useState(true)
  const [includeAllEntries, setIncludeAllEntries] = useState(true)
  const [includeCoverageGaps, setIncludeCoverageGaps] = useState(true)
  const [includeEvaluatorNames, setIncludeEvaluatorNames] = useState(true)
  const [includeFormatting, setIncludeFormatting] = useState(true)
  const [isExporting, setIsExporting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleExport = async () => {
    setIsExporting(true)
    setError(null)

    try {
      const options: ExportEegOptions = {
        format,
        includeSummary,
        includeByCapability,
        includeAllEntries,
        includeCoverageGaps,
        includeEvaluatorNames,
        includeFormatting,
        filename: exerciseName ? `EEG_Export_${exerciseName.replace(/\s+/g, '_')}` : undefined,
      }

      if (format === 'json') {
        const jsonData = await eegExportService.exportToJson(exerciseId, includeEvaluatorNames)
        // Download JSON as file
        const blob = new Blob([JSON.stringify(jsonData, null, 2)], { type: 'application/json' })
        const url = window.URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        a.download = `${options.filename || 'EEG_Export'}.json`
        document.body.appendChild(a)
        a.click()
        window.URL.revokeObjectURL(url)
        document.body.removeChild(a)
      } else {
        await eegExportService.downloadExport(exerciseId, options)
      }

      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to export EEG data')
    } finally {
      setIsExporting(false)
    }
  }

  const entryCount = coverage?.ratingDistribution
    ? Object.values(coverage.ratingDistribution).reduce((sum, count) => sum + count, 0)
    : 0
  const hasEntries = entryCount > 0

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon icon={faFileExcel} />
          Export EEG Data
        </Box>
      </DialogTitle>

      <DialogContent dividers>
        {/* Exercise Info */}
        <Box sx={{ mb: 3 }}>
          {exerciseName && (
            <Typography variant="subtitle2" color="text.secondary">
              Exercise: {exerciseName}
            </Typography>
          )}
          <Typography variant="body2" color="text.secondary">
            EEG Entries: {entryCount}
          </Typography>
          {coverage && (
            <Typography variant="body2" color="text.secondary">
              Task Coverage: {coverage.coveragePercentage}% ({coverage.evaluatedTasks}/{coverage.totalTasks})
            </Typography>
          )}
        </Box>

        {!hasEntries && (
          <Alert severity="warning" sx={{ mb: 3 }}>
            No EEG entries to export. Record some entries first.
          </Alert>
        )}

        <Divider sx={{ mb: 2 }} />

        {/* Export Format */}
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Export Format
        </Typography>
        <RadioGroup
          value={format}
          onChange={e => setFormat(e.target.value as 'xlsx' | 'json')}
          sx={{ mb: 2 }}
        >
          <FormControlLabel
            value="xlsx"
            control={<Radio />}
            label={
              <Box>
                <Typography variant="body2" fontWeight={500}>
                  <FontAwesomeIcon icon={faFileExcel} style={{ marginRight: 8 }} />
                  Excel Workbook (.xlsx)
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Multi-sheet workbook organized for AAR preparation
                </Typography>
              </Box>
            }
          />
          <FormControlLabel
            value="json"
            control={<Radio />}
            label={
              <Box>
                <Typography variant="body2" fontWeight={500}>
                  <FontAwesomeIcon icon={faFileCode} style={{ marginRight: 8 }} />
                  JSON Data
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Raw data format for integration with other tools
                </Typography>
              </Box>
            }
          />
        </RadioGroup>

        <Divider sx={{ mb: 2 }} />

        {/* Include Options (Excel only) */}
        {format === 'xlsx' && (
          <>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Include Sections
            </Typography>
            <FormGroup sx={{ mb: 2 }}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={includeSummary}
                    onChange={e => setIncludeSummary(e.target.checked)}
                  />
                }
                label="Summary statistics"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={includeByCapability}
                    onChange={e => setIncludeByCapability(e.target.checked)}
                  />
                }
                label="Entries by capability (for AAR)"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={includeAllEntries}
                    onChange={e => setIncludeAllEntries(e.target.checked)}
                  />
                }
                label="All entries (flat list)"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={includeCoverageGaps}
                    onChange={e => setIncludeCoverageGaps(e.target.checked)}
                  />
                }
                label="Coverage gaps"
              />
            </FormGroup>

            <Divider sx={{ mb: 2 }} />

            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Additional Options
            </Typography>
            <FormGroup>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={includeEvaluatorNames}
                    onChange={e => setIncludeEvaluatorNames(e.target.checked)}
                  />
                }
                label="Include evaluator names"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={includeFormatting}
                    onChange={e => setIncludeFormatting(e.target.checked)}
                  />
                }
                label="Include formatting and colors"
              />
            </FormGroup>
          </>
        )}

        {format === 'json' && (
          <FormGroup>
            <FormControlLabel
              control={
                <Checkbox
                  checked={includeEvaluatorNames}
                  onChange={e => setIncludeEvaluatorNames(e.target.checked)}
                />
              }
              label="Include evaluator names"
            />
          </FormGroup>
        )}

        {error && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {error}
          </Alert>
        )}
      </DialogContent>

      <DialogActions>
        <CobraSecondaryButton
          onClick={onClose}
          disabled={isExporting}
          startIcon={<FontAwesomeIcon icon={faXmark} />}
        >
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleExport}
          disabled={isExporting || !hasEntries}
          startIcon={
            isExporting ? (
              <CircularProgress size={16} color="inherit" />
            ) : (
              <FontAwesomeIcon icon={faDownload} />
            )
          }
        >
          {isExporting ? 'Exporting...' : 'Export'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default EegExportDialog
