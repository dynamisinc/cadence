/**
 * ImportExecutionStep Component
 *
 * Fifth step of the import wizard - configure options and execute import.
 */

import { useState } from 'react'
import {
  Box,
  Typography,
  Paper,
  Alert,
  Stack,
  FormControl,
  FormLabel,
  RadioGroup,
  FormControlLabel,
  Radio,
  Checkbox,
  LinearProgress,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faRotate,
  faCodeMerge,
  faCheck,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'

import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import { ImportStrategy, type ImportStrategyType, type ImportResult } from '../types'

interface ImportExecutionStepProps {
  /** Number of rows to import */
  rowCount: number
  /** Whether there are errors to skip */
  hasErrors?: boolean
  /** Is import in progress? */
  isImporting?: boolean
  /** Import result (when complete) */
  importResult?: ImportResult | null
  /** Error message */
  error?: string | null
  /** Called when user wants to go back */
  onBack: () => void
  /** Called when user starts import */
  onImport: (options: ImportOptions) => void
  /** Called when import is complete and user wants to view MSEL */
  onViewMsel?: () => void
  /** Called when user wants to import another file */
  onImportAnother?: () => void
}

export interface ImportOptions {
  strategy: ImportStrategyType
  skipErrorRows: boolean
  createMissingPhases: boolean
  createMissingObjectives: boolean
}

export const ImportExecutionStep = ({
  rowCount,
  hasErrors = false,
  isImporting = false,
  importResult = null,
  error = null,
  onBack,
  onImport,
  onViewMsel,
  onImportAnother,
}: ImportExecutionStepProps) => {
  const [strategy, setStrategy] = useState<ImportStrategyType>(ImportStrategy.Append)
  const [skipErrorRows, setSkipErrorRows] = useState(hasErrors)
  const [createMissingPhases, setCreateMissingPhases] = useState(true)
  const [createMissingObjectives, setCreateMissingObjectives] = useState(false)

  const handleImport = () => {
    onImport({
      strategy,
      skipErrorRows,
      createMissingPhases,
      createMissingObjectives,
    })
  }

  // Import Complete View
  if (importResult) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <FontAwesomeIcon
          icon={faCheck}
          size="4x"
          style={{ color: '#2e7d32', marginBottom: 24 }}
        />
        <Typography variant="h5" gutterBottom>
          Import {importResult.success ? 'Successful' : 'Completed with Issues'}!
        </Typography>

        <Paper sx={{ p: 3, maxWidth: 400, mx: 'auto', mt: 3, textAlign: 'left' }}>
          <Stack spacing={1}>
            <Typography>
              <strong>{importResult.injectsCreated}</strong> inject{importResult.injectsCreated !== 1 ? 's' : ''} created
            </Typography>
            {importResult.injectsUpdated > 0 && (
              <Typography>
                <strong>{importResult.injectsUpdated}</strong> inject{importResult.injectsUpdated !== 1 ? 's' : ''} updated
              </Typography>
            )}
            {importResult.rowsSkipped > 0 && (
              <Typography color="text.secondary">
                {importResult.rowsSkipped} row{importResult.rowsSkipped !== 1 ? 's' : ''} skipped
              </Typography>
            )}
            {importResult.phasesCreated > 0 && (
              <Typography>
                {importResult.phasesCreated} phase{importResult.phasesCreated !== 1 ? 's' : ''} created
              </Typography>
            )}
          </Stack>
        </Paper>

        {importResult.warnings && importResult.warnings.length > 0 && (
          <Alert severity="warning" sx={{ maxWidth: 400, mx: 'auto', mt: 3, textAlign: 'left' }}>
            <Typography variant="subtitle2" gutterBottom>
              Warnings:
            </Typography>
            <ul style={{ margin: 0, paddingLeft: 20 }}>
              {importResult.warnings.slice(0, 5).map((warning, i) => (
                <li key={i}>{warning}</li>
              ))}
              {importResult.warnings.length > 5 && (
                <li>...and {importResult.warnings.length - 5} more</li>
              )}
            </ul>
          </Alert>
        )}

        {importResult.errors && importResult.errors.length > 0 && (
          <Alert severity="error" sx={{ maxWidth: 400, mx: 'auto', mt: 3, textAlign: 'left' }}>
            <Typography variant="subtitle2" gutterBottom>
              Errors:
            </Typography>
            <ul style={{ margin: 0, paddingLeft: 20 }}>
              {importResult.errors.slice(0, 5).map((err, i) => (
                <li key={i}>{err}</li>
              ))}
              {importResult.errors.length > 5 && (
                <li>...and {importResult.errors.length - 5} more</li>
              )}
            </ul>
          </Alert>
        )}

        <Stack direction="row" spacing={2} justifyContent="center" sx={{ mt: 4 }}>
          {onImportAnother && (
            <CobraSecondaryButton onClick={onImportAnother}>
              Import Another
            </CobraSecondaryButton>
          )}
          {onViewMsel && (
            <CobraPrimaryButton onClick={onViewMsel}>
              View MSEL
            </CobraPrimaryButton>
          )}
        </Stack>
      </Box>
    )
  }

  // Import in Progress View
  if (isImporting) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <FontAwesomeIcon
          icon={faSpinner}
          spin
          size="3x"
          style={{ color: '#1976d2', marginBottom: 24 }}
        />
        <Typography variant="h6" gutterBottom>
          Importing {rowCount} rows...
        </Typography>
        <LinearProgress sx={{ maxWidth: 400, mx: 'auto' }} />
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
          Please wait while your data is imported.
        </Typography>
      </Box>
    )
  }

  // Configuration View
  return (
    <Box>
      <Typography variant="subtitle1" fontWeight="bold" gutterBottom>
        Import Options
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Configure how the data should be imported into your MSEL.
      </Typography>

      {/* Import Strategy */}
      <Paper sx={{ p: 2, mb: 2 }}>
        <FormControl component="fieldset">
          <FormLabel component="legend" sx={{ fontWeight: 'bold', mb: 0.5, fontSize: '0.875rem' }}>
            Import Strategy
          </FormLabel>
          <RadioGroup
            value={strategy}
            onChange={e => setStrategy(e.target.value as ImportStrategyType)}
          >
            <FormControlLabel
              value={ImportStrategy.Append}
              control={<Radio size="small" />}
              sx={{ mb: 0.5 }}
              label={
                <Stack direction="row" spacing={1} alignItems="center">
                  <FontAwesomeIcon icon={faPlus} size="sm" />
                  <Box>
                    <Typography variant="body2">Append</Typography>
                    <Typography variant="caption" color="text.secondary">
                      Add new injects to the existing MSEL
                    </Typography>
                  </Box>
                </Stack>
              }
            />
            <FormControlLabel
              value={ImportStrategy.Replace}
              control={<Radio size="small" />}
              sx={{ mb: 0.5 }}
              label={
                <Stack direction="row" spacing={1} alignItems="center">
                  <FontAwesomeIcon icon={faRotate} size="sm" />
                  <Box>
                    <Typography variant="body2">Replace</Typography>
                    <Typography variant="caption" color="text.secondary">
                      Delete all existing injects and replace with imported data
                    </Typography>
                  </Box>
                </Stack>
              }
            />
            <FormControlLabel
              value={ImportStrategy.Merge}
              control={<Radio size="small" disabled />}
              disabled
              label={
                <Stack direction="row" spacing={1} alignItems="center">
                  <FontAwesomeIcon icon={faCodeMerge} size="sm" style={{ opacity: 0.5 }} />
                  <Box>
                    <Typography variant="body2" color="text.disabled">Merge (Coming Soon)</Typography>
                    <Typography variant="caption" color="text.disabled">
                      Update existing injects by inject number, add new ones
                    </Typography>
                  </Box>
                </Stack>
              }
            />
          </RadioGroup>
        </FormControl>
      </Paper>

      {/* Additional Options */}
      <Paper sx={{ p: 2, mb: 2 }}>
        <Typography variant="subtitle2" fontWeight="bold" sx={{ mb: 1 }}>
          Additional Options
        </Typography>

        <Stack spacing={0.5}>
          {hasErrors && (
            <Box>
              <FormControlLabel
                control={
                  <Checkbox
                    size="small"
                    checked={skipErrorRows}
                    onChange={e => setSkipErrorRows(e.target.checked)}
                  />
                }
                label={<Typography variant="body2">Skip rows with errors</Typography>}
              />
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', ml: 3.5, mt: -0.5 }}>
                Import only valid rows and skip any rows that have validation errors.
              </Typography>
            </Box>
          )}
          <Box>
            <FormControlLabel
              control={
                <Checkbox
                  size="small"
                  checked={createMissingPhases}
                  onChange={e => setCreateMissingPhases(e.target.checked)}
                />
              }
              label={<Typography variant="body2">Create missing phases automatically</Typography>}
            />
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', ml: 3.5, mt: -0.5 }}>
              If a phase doesn&apos;t exist, create it in the order it appears in the file.
            </Typography>
          </Box>
          <Box>
            <FormControlLabel
              control={
                <Checkbox
                  size="small"
                  checked={createMissingObjectives}
                  onChange={e => setCreateMissingObjectives(e.target.checked)}
                  disabled
                />
              }
              label={<Typography variant="body2" color="text.disabled">Create missing objectives automatically</Typography>}
            />
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', ml: 3.5, mt: -0.5 }}>
              Coming soon: Automatically create objectives referenced in the import file.
            </Typography>
          </Box>
        </Stack>
      </Paper>

      {/* Warning for Replace */}
      {strategy === ImportStrategy.Replace && (
        <Alert severity="warning" sx={{ mb: 2, py: 0.5 }}>
          This will permanently delete all existing injects in the MSEL before importing.
          This action cannot be undone.
        </Alert>
      )}

      {/* Summary */}
      <Paper sx={{ p: 1.5, mb: 2, backgroundColor: 'grey.50' }}>
        <Typography variant="body2">
          <strong>{rowCount}</strong> row{rowCount !== 1 ? 's' : ''} will be imported using the{' '}
          <strong>{strategy}</strong> strategy.
        </Typography>
      </Paper>

      {/* Error */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Navigation Buttons */}
      <Divider sx={{ my: 1.5 }} />
      <Stack direction="row" spacing={2} justifyContent="flex-end">
        <CobraSecondaryButton onClick={onBack}>
          Back
        </CobraSecondaryButton>
        <CobraPrimaryButton onClick={handleImport}>
          Import {rowCount} Rows
        </CobraPrimaryButton>
      </Stack>
    </Box>
  )
}

export default ImportExecutionStep
