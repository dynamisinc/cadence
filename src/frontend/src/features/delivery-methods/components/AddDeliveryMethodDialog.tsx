import { useState } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControlLabel,
  Checkbox,
} from '@mui/material'
import { CobraPrimaryButton, CobraSecondaryButton, CobraTextField } from '@/theme/styledComponents'
import { useCreateDeliveryMethod } from '../hooks/useDeliveryMethodManagement'

interface AddDeliveryMethodDialogProps {
  open: boolean
  onClose: () => void
  otherExists: boolean
}

export function AddDeliveryMethodDialog({ open, onClose, otherExists }: AddDeliveryMethodDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [isOther, setIsOther] = useState(false)
  const createMutation = useCreateDeliveryMethod()

  const handleSubmit = () => {
    const trimmedName = name.trim()
    if (!trimmedName) return

    createMutation.mutate(
      {
        name: trimmedName,
        description: description.trim() || null,
        isOther,
      },
      {
        onSuccess: () => {
          setName('')
          setDescription('')
          setIsOther(false)
          onClose()
        },
      },
    )
  }

  const handleClose = () => {
    setName('')
    setDescription('')
    setIsOther(false)
    onClose()
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Add Delivery Method</DialogTitle>
      <DialogContent>
        <CobraTextField
          label="Name"
          value={name}
          onChange={e => setName(e.target.value)}
          onKeyDown={e => {
            if (e.key === 'Enter') {
              e.preventDefault()
              handleSubmit()
            }
          }}
          fullWidth
          autoFocus
          required
          placeholder="e.g., Radio, Email, Verbal"
          sx={{ mt: 1 }}
        />
        <CobraTextField
          label="Description"
          value={description}
          onChange={e => setDescription(e.target.value)}
          fullWidth
          multiline
          rows={2}
          placeholder="Optional: when/how to use this method"
          sx={{ mt: 2 }}
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={isOther}
              onChange={e => setIsOther(e.target.checked)}
              disabled={otherExists}
            />
          }
          label={otherExists
            ? "An 'Other' delivery method already exists"
            : "This is the 'Other' option (allows free-text input)"}
          sx={{ mt: 1 }}
        />
      </DialogContent>
      <DialogActions>
        <CobraSecondaryButton onClick={handleClose}>Cancel</CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleSubmit}
          disabled={!name.trim() || createMutation.isPending}
        >
          {createMutation.isPending ? 'Adding...' : 'Add'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}
