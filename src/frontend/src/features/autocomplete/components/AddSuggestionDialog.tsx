import { useState } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import { CobraPrimaryButton, CobraSecondaryButton, CobraTextField } from '@/theme/styledComponents'
import type { SuggestionFieldName } from '../types'
import { useCreateSuggestion } from '../hooks/useSuggestionManagement'

interface AddSuggestionDialogProps {
  open: boolean
  onClose: () => void
  fieldName: SuggestionFieldName
}

export function AddSuggestionDialog({ open, onClose, fieldName }: AddSuggestionDialogProps) {
  const [value, setValue] = useState('')
  const createMutation = useCreateSuggestion()

  const handleSubmit = () => {
    const trimmed = value.trim()
    if (!trimmed) return

    createMutation.mutate(
      { fieldName, value: trimmed },
      {
        onSuccess: () => {
          setValue('')
          onClose()
        },
      },
    )
  }

  const handleClose = () => {
    setValue('')
    onClose()
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Add Suggestion</DialogTitle>
      <DialogContent>
        <CobraTextField
          label="Value"
          value={value}
          onChange={e => setValue(e.target.value)}
          onKeyDown={e => {
            if (e.key === 'Enter') {
              e.preventDefault()
              handleSubmit()
            }
          }}
          fullWidth
          autoFocus
          placeholder="Enter a suggestion value"
          sx={{ mt: 1 }}
        />
      </DialogContent>
      <DialogActions>
        <CobraSecondaryButton onClick={handleClose}>Cancel</CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleSubmit}
          disabled={!value.trim() || createMutation.isPending}
        >
          {createMutation.isPending ? 'Adding...' : 'Add'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}
