/**
 * CapabilityDialog Component
 *
 * Modal dialog for creating or editing a capability.
 * Provides form fields for name, description, and category.
 */

import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  Stack,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSave, faXmark, faPlus } from '@fortawesome/free-solid-svg-icons'
import {
  CobraTextField,
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import CobraStyles from '@/theme/CobraStyles'
import type {
  CapabilityDto,
  CreateCapabilityRequest,
  UpdateCapabilityRequest,
} from '../types'

interface CapabilityDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Capability to edit (null for create mode) */
  capability?: CapabilityDto | null
  /** Available categories for selection */
  existingCategories?: string[]
  /** Called when dialog should close */
  onClose: () => void
  /** Called when save is clicked for creating */
  onCreate?: (request: CreateCapabilityRequest) => Promise<void>
  /** Called when save is clicked for updating */
  onUpdate?: (id: string, request: UpdateCapabilityRequest) => Promise<void>
}

/**
 * Dialog for creating or editing capabilities
 */
export const CapabilityDialog: FC<CapabilityDialogProps> = ({
  open,
  capability,
  existingCategories = [],
  onClose,
  onCreate,
  onUpdate,
}) => {
  const isEditMode = !!capability

  // Form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [category, setCategory] = useState('')
  const [newCategory, setNewCategory] = useState('')
  const [showNewCategory, setShowNewCategory] = useState(false)
  const [sortOrder, setSortOrder] = useState(0)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Initialize form when capability changes or dialog opens
  useEffect(() => {
    if (capability) {
      setName(capability.name)
      setDescription(capability.description || '')
      setCategory(capability.category || '')
      setSortOrder(capability.sortOrder)
      setNewCategory('')
      setShowNewCategory(false)
    } else {
      setName('')
      setDescription('')
      setCategory('')
      setNewCategory('')
      setShowNewCategory(false)
      setSortOrder(0)
    }
    setError(null)
    setIsLoading(false)
  }, [capability, open])

  const handleSave = async () => {
    // Validation
    if (!name.trim()) {
      setError('Name is required')
      return
    }

    if (name.length < 2) {
      setError('Name must be at least 2 characters')
      return
    }

    if (name.length > 200) {
      setError('Name must be 200 characters or less')
      return
    }

    if (description.length > 1000) {
      setError('Description must be 1000 characters or less')
      return
    }

    const finalCategory = showNewCategory ? newCategory.trim() : category

    if (finalCategory && finalCategory.length > 100) {
      setError('Category must be 100 characters or less')
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      if (isEditMode && capability && onUpdate) {
        const request: UpdateCapabilityRequest = {
          name: name.trim(),
          description: description.trim() || null,
          category: finalCategory || null,
          sortOrder,
          isActive: capability.isActive,
        }
        await onUpdate(capability.id, request)
      } else if (onCreate) {
        const request: CreateCapabilityRequest = {
          name: name.trim(),
          description: description.trim() || null,
          category: finalCategory || null,
          sortOrder,
        }
        await onCreate(request)
      }
      onClose()
    } catch (err: unknown) {
      const axiosError = err as { response?: { data?: { message?: string } } }
      setError(axiosError.response?.data?.message || 'Failed to save capability')
      setIsLoading(false)
    }
  }

  // Validation state
  const isNameValid = name.trim().length >= 2 && name.trim().length <= 200
  const canSave = isNameValid && !isLoading

  const handleCategoryChange = (value: string) => {
    if (value === '__new__') {
      setShowNewCategory(true)
      setCategory('')
    } else {
      setShowNewCategory(false)
      setCategory(value)
    }
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        {isEditMode ? 'Edit Capability' : 'Add Capability'}
      </DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <Stack spacing={CobraStyles.Spacing.FormFields} sx={{ mt: 1 }}>
          <CobraTextField
            label="Name"
            value={name}
            onChange={e => setName(e.target.value)}
            fullWidth
            required
            placeholder="e.g., Mass Care Services"
            helperText="2-200 characters"
          />

          <CobraTextField
            label="Description"
            value={description}
            onChange={e => setDescription(e.target.value)}
            fullWidth
            multiline
            rows={3}
            placeholder="Describe what this capability encompasses..."
            helperText={`${description.length}/1000 characters`}
          />

          <FormControl fullWidth>
            <InputLabel id="category-label">Category</InputLabel>
            <Select
              labelId="category-label"
              value={showNewCategory ? '__new__' : category}
              onChange={e => handleCategoryChange(e.target.value as string)}
              label="Category"
            >
              <MenuItem value="">
                <em>None</em>
              </MenuItem>
              {existingCategories.map(cat => (
                <MenuItem key={cat} value={cat}>
                  {cat}
                </MenuItem>
              ))}
              <MenuItem value="__new__">
                <Stack direction="row" spacing={1} alignItems="center">
                  <FontAwesomeIcon icon={faPlus} />
                  <Typography>Add new category...</Typography>
                </Stack>
              </MenuItem>
            </Select>
          </FormControl>

          {showNewCategory && (
            <CobraTextField
              label="New Category"
              value={newCategory}
              onChange={e => setNewCategory(e.target.value)}
              fullWidth
              placeholder="e.g., Response"
              helperText="Up to 100 characters"
              autoFocus
            />
          )}

          <CobraTextField
            label="Sort Order"
            type="number"
            value={sortOrder}
            onChange={e => setSortOrder(parseInt(e.target.value, 10) || 0)}
            fullWidth
            helperText="Lower numbers appear first within a category"
            inputProps={{ min: 0, max: 9999 }}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <CobraSecondaryButton onClick={onClose} disabled={isLoading}>
          <FontAwesomeIcon icon={faXmark} style={{ marginRight: 8 }} />
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton onClick={handleSave} disabled={!canSave}>
          <FontAwesomeIcon icon={faSave} style={{ marginRight: 8 }} />
          {isLoading ? 'Saving...' : 'Save'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default CapabilityDialog
