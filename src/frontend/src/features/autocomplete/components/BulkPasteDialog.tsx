import { useState, useMemo } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
} from '@mui/material'
import { CobraPrimaryButton, CobraSecondaryButton, CobraTextField } from '@/theme/styledComponents'
import type { SuggestionFieldName } from '../types'
import { useBulkCreateSuggestions } from '../hooks/useSuggestionManagement'

interface BulkPasteDialogProps {
  open: boolean
  onClose: () => void
  fieldName: SuggestionFieldName
}

export function BulkPasteDialog({ open, onClose, fieldName }: BulkPasteDialogProps) {
  const [text, setText] = useState('')
  const bulkMutation = useBulkCreateSuggestions()

  const parsedValues = useMemo(() => {
    return text
      .split('\n')
      .map(line => line.trim())
      .filter(line => line.length > 0)
  }, [text])

  const uniqueCount = useMemo(() => {
    return new Set(parsedValues.map(v => v.toLowerCase())).size
  }, [parsedValues])

  const handleSubmit = () => {
    if (parsedValues.length === 0) return

    bulkMutation.mutate(
      { fieldName, values: parsedValues },
      {
        onSuccess: () => {
          setText('')
          onClose()
        },
      },
    )
  }

  const handleClose = () => {
    setText('')
    onClose()
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Bulk Add Suggestions</DialogTitle>
      <DialogContent>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          Paste one value per line. Duplicates will be skipped automatically.
        </Typography>
        <CobraTextField
          label="Values"
          value={text}
          onChange={e => setText(e.target.value)}
          fullWidth
          multiline
          rows={8}
          placeholder={'Fire Department\nPolice Department\nEMS\nPublic Health\nRed Cross'}
          sx={{ mt: 1 }}
        />
        {parsedValues.length > 0 && (
          <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
            {uniqueCount} unique value{uniqueCount !== 1 ? 's' : ''} to import
          </Typography>
        )}
      </DialogContent>
      <DialogActions>
        <CobraSecondaryButton onClick={handleClose}>Cancel</CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleSubmit}
          disabled={parsedValues.length === 0 || bulkMutation.isPending}
        >
          {bulkMutation.isPending ? 'Importing...' : `Import ${uniqueCount} Value${uniqueCount !== 1 ? 's' : ''}`}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}
