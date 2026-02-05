/**
 * EEG Document Generation Dialog
 *
 * Dialog for generating HSEEP-compliant EEG documents (Word format).
 * Supports blank EEG for evaluators to use during conduct,
 * or completed EEG with recorded observations.
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
  faFileWord,
  faDownload,
  faXmark,
  faFileZipper,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import {
  eegDocumentService,
  type EegDocumentMode,
  type EegDocumentOutputFormat,
  type GenerateEegDocumentRequest,
} from '../services/eegService'
import type { EegCoverageDto } from '../types'

interface EegDocumentDialogProps {
  open: boolean
  onClose: () => void
  exerciseId: string
  exerciseName?: string
  coverage?: EegCoverageDto | null
  /** Default mode to select (blank or completed) */
  defaultMode?: EegDocumentMode
}

export const EegDocumentDialog = ({
  open,
  onClose,
  exerciseId,
  exerciseName,
  coverage,
  defaultMode = 'blank',
}: EegDocumentDialogProps) => {
  const [mode, setMode] = useState<EegDocumentMode>(defaultMode)
  const [outputFormat, setOutputFormat] = useState<EegDocumentOutputFormat>('single')
  const [includeEvaluatorNames, setIncludeEvaluatorNames] = useState(true)
  const [isGenerating, setIsGenerating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleGenerate = async () => {
    setIsGenerating(true)
    setError(null)

    try {
      const request: GenerateEegDocumentRequest = {
        mode,
        outputFormat,
        includeEvaluatorNames,
      }

      await eegDocumentService.download(exerciseId, exerciseName || 'Exercise', request)
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate EEG document')
    } finally {
      setIsGenerating(false)
    }
  }

  const entryCount = coverage?.ratingDistribution
    ? Object.values(coverage.ratingDistribution).reduce((sum, count) => sum + count, 0)
    : 0
  const hasEntries = entryCount > 0
  const hasTargets = (coverage?.totalTasks ?? 0) > 0

  // For blank mode, we need capability targets defined
  // For completed mode, we also need entries recorded
  const canGenerate = hasTargets && (mode === 'blank' || hasEntries)

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon icon={faFileWord} />
          Generate EEG Document
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
          {coverage && (
            <>
              <Typography variant="body2" color="text.secondary">
                Capability Targets: {coverage.capabilityTargetCount}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Critical Tasks: {coverage.totalTasks}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                EEG Entries: {entryCount}
              </Typography>
            </>
          )}
        </Box>

        {!hasTargets && (
          <Alert severity="warning" sx={{ mb: 3 }}>
            No capability targets defined. Define targets and tasks to generate an EEG document.
          </Alert>
        )}

        <Divider sx={{ mb: 2 }} />

        {/* Document Mode */}
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Document Type
        </Typography>
        <RadioGroup
          value={mode}
          onChange={e => setMode(e.target.value as EegDocumentMode)}
          sx={{ mb: 2 }}
        >
          <FormControlLabel
            value="blank"
            control={<Radio />}
            label={
              <Box>
                <Typography variant="body2" fontWeight={500}>
                  Blank EEG
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Template for evaluators to record observations during conduct
                </Typography>
              </Box>
            }
          />
          <FormControlLabel
            value="completed"
            control={<Radio />}
            disabled={!hasEntries}
            label={
              <Box>
                <Typography
                  variant="body2"
                  fontWeight={500}
                  color={!hasEntries ? 'text.disabled' : 'text.primary'}
                >
                  Completed EEG
                </Typography>
                <Typography
                  variant="caption"
                  color={!hasEntries ? 'text.disabled' : 'text.secondary'}
                >
                  Document with recorded observations and ratings
                  {!hasEntries && ' (requires entries)'}
                </Typography>
              </Box>
            }
          />
        </RadioGroup>

        <Divider sx={{ mb: 2 }} />

        {/* Output Format */}
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Output Format
        </Typography>
        <RadioGroup
          value={outputFormat}
          onChange={e => setOutputFormat(e.target.value as EegDocumentOutputFormat)}
          sx={{ mb: 2 }}
        >
          <FormControlLabel
            value="single"
            control={<Radio />}
            label={
              <Box>
                <Typography variant="body2" fontWeight={500}>
                  <FontAwesomeIcon icon={faFileWord} style={{ marginRight: 8 }} />
                  Single Document (.docx)
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  All capabilities in one document
                </Typography>
              </Box>
            }
          />
          <FormControlLabel
            value="perCapability"
            control={<Radio />}
            label={
              <Box>
                <Typography variant="body2" fontWeight={500}>
                  <FontAwesomeIcon icon={faFileZipper} style={{ marginRight: 8 }} />
                  Per Capability (.zip)
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Separate document for each capability (for distributed evaluation)
                </Typography>
              </Box>
            }
          />
        </RadioGroup>

        {/* Additional Options - Only for Completed mode */}
        {mode === 'completed' && (
          <>
            <Divider sx={{ mb: 2 }} />
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Options
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
            </FormGroup>
          </>
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
          disabled={isGenerating}
          startIcon={<FontAwesomeIcon icon={faXmark} />}
        >
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleGenerate}
          disabled={isGenerating || !canGenerate}
          startIcon={
            isGenerating ? (
              <CircularProgress size={16} color="inherit" />
            ) : (
              <FontAwesomeIcon icon={faDownload} />
            )
          }
        >
          {isGenerating ? 'Generating...' : 'Generate'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default EegDocumentDialog
