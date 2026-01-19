import { useState } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControlLabel,
  Checkbox,
  FormGroup,
  RadioGroup,
  Radio,
  FormControl,
  FormLabel,
  Alert,
  Box,
  CircularProgress,
  Typography,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faFileExcel, faFileCsv, faDownload, faXmark } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import { useExportMsel } from '../hooks/useExcelExport'
import type { ExportMselRequest } from '../types'

export interface ExportDialogProps {
  open: boolean
  onClose: () => void
  exerciseId: string
  exerciseName: string
}

export function ExportDialog({
  open,
  onClose,
  exerciseId,
  exerciseName,
}: ExportDialogProps): React.ReactElement {
  const [format, setFormat] = useState<'xlsx' | 'csv'>('xlsx')
  const [includeFormatting, setIncludeFormatting] = useState(true)
  const [includePhases, setIncludePhases] = useState(true)
  const [includeObjectives, setIncludeObjectives] = useState(true)
  const [includeConductData, setIncludeConductData] = useState(false)

  const exportMutation = useExportMsel()

  const handleExport = () => {
    const request: ExportMselRequest = {
      exerciseId,
      format,
      includeFormatting,
      includePhases,
      includeObjectives,
      includeConductData,
    }

    exportMutation.mutate(request, {
      onSuccess: info => {
        // Could show a success toast here
        console.log('Export complete:', info)
        onClose()
      },
    })
  }

  const handleClose = () => {
    if (!exportMutation.isPending) {
      onClose()
    }
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon icon={faFileExcel} />
          Export MSEL
        </Box>
      </DialogTitle>

      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Export the MSEL for <strong>{exerciseName}</strong>
        </Typography>

        {exportMutation.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            Export failed. Please try again.
          </Alert>
        )}

        <FormControl component="fieldset" sx={{ mb: 3 }}>
          <FormLabel component="legend">Format</FormLabel>
          <RadioGroup
            row
            value={format}
            onChange={e => setFormat(e.target.value as 'xlsx' | 'csv')}
          >
            <FormControlLabel
              value="xlsx"
              control={<Radio />}
              label={
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <FontAwesomeIcon icon={faFileExcel} />
                  Excel (.xlsx)
                </Box>
              }
            />
            <FormControlLabel
              value="csv"
              control={<Radio />}
              label={
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <FontAwesomeIcon icon={faFileCsv} />
                  CSV (.csv)
                </Box>
              }
            />
          </RadioGroup>
        </FormControl>

        <FormGroup>
          {format === 'xlsx' && (
            <>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={includeFormatting}
                    onChange={e => setIncludeFormatting(e.target.checked)}
                  />
                }
                label="Include formatting (colors, column widths)"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={includePhases}
                    onChange={e => setIncludePhases(e.target.checked)}
                  />
                }
                label="Include Phases worksheet"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={includeObjectives}
                    onChange={e => setIncludeObjectives(e.target.checked)}
                  />
                }
                label="Include Objectives worksheet"
              />
            </>
          )}
          <FormControlLabel
            control={
              <Checkbox
                checked={includeConductData}
                onChange={e => setIncludeConductData(e.target.checked)}
              />
            }
            label="Include conduct data (status, fired time, fired by)"
          />
        </FormGroup>
      </DialogContent>

      <DialogActions>
        <CobraSecondaryButton
          onClick={handleClose}
          disabled={exportMutation.isPending}
          startIcon={<FontAwesomeIcon icon={faXmark} />}
        >
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleExport}
          disabled={exportMutation.isPending}
          startIcon={
            exportMutation.isPending ? (
              <CircularProgress size={16} color="inherit" />
            ) : (
              <FontAwesomeIcon icon={faDownload} />
            )
          }
        >
          {exportMutation.isPending ? 'Exporting...' : 'Export'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}
