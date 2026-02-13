import { useState } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Switch,
  IconButton,
  Typography,
  Box,
  Skeleton,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTrash, faPen, faCheck, faXmark } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton, CobraTextField } from '@/theme/styledComponents'
import type { OrganizationSuggestionDto, SuggestionFieldName, UpdateSuggestionRequest } from '../types'
import { useUpdateSuggestion, useDeleteSuggestion } from '../hooks/useSuggestionManagement'

interface SuggestionTableProps {
  suggestions: OrganizationSuggestionDto[]
  fieldName: SuggestionFieldName
  isLoading: boolean
}

export function SuggestionTable({ suggestions, fieldName, isLoading }: SuggestionTableProps) {
  const updateMutation = useUpdateSuggestion(fieldName)
  const deleteMutation = useDeleteSuggestion(fieldName)

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editValue, setEditValue] = useState('')
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const handleStartEdit = (suggestion: OrganizationSuggestionDto) => {
    setEditingId(suggestion.id)
    setEditValue(suggestion.value)
  }

  const handleCancelEdit = () => {
    setEditingId(null)
    setEditValue('')
  }

  const handleSaveEdit = (suggestion: OrganizationSuggestionDto) => {
    const trimmed = editValue.trim()
    if (!trimmed || trimmed === suggestion.value) {
      handleCancelEdit()
      return
    }

    const request: UpdateSuggestionRequest = {
      value: trimmed,
      sortOrder: suggestion.sortOrder,
      isActive: suggestion.isActive,
    }

    updateMutation.mutate(
      { id: suggestion.id, request },
      { onSuccess: () => handleCancelEdit() },
    )
  }

  const handleToggleActive = (suggestion: OrganizationSuggestionDto) => {
    const request: UpdateSuggestionRequest = {
      value: suggestion.value,
      sortOrder: suggestion.sortOrder,
      isActive: !suggestion.isActive,
    }
    updateMutation.mutate({ id: suggestion.id, request })
  }

  const handleDelete = (id: string) => {
    deleteMutation.mutate(id, { onSuccess: () => setDeleteConfirmId(null) })
  }

  if (isLoading) {
    return (
      <Box>
        {[1, 2, 3].map(n => (
          <Skeleton key={n} variant="rectangular" height={48} sx={{ mb: 1, borderRadius: 1 }} />
        ))}
      </Box>
    )
  }

  if (suggestions.length === 0) {
    return (
      <Alert severity="info">
        No managed suggestions for this field yet. Add suggestions to pre-populate autocomplete dropdowns.
      </Alert>
    )
  }

  const deleteTarget = suggestions.find(s => s.id === deleteConfirmId)

  return (
    <>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Value</TableCell>
              <TableCell align="center" sx={{ width: 80 }}>Active</TableCell>
              <TableCell align="right" sx={{ width: 100 }}>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {suggestions.map(suggestion => (
              <TableRow key={suggestion.id} sx={{ opacity: suggestion.isActive ? 1 : 0.5 }}>
                <TableCell>
                  {editingId === suggestion.id ? (
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <CobraTextField
                        value={editValue}
                        onChange={e => setEditValue(e.target.value)}
                        onKeyDown={e => {
                          if (e.key === 'Enter') handleSaveEdit(suggestion)
                          if (e.key === 'Escape') handleCancelEdit()
                        }}
                        size="small"
                        fullWidth
                        autoFocus
                      />
                      <IconButton
                        size="small"
                        color="primary"
                        onClick={() => handleSaveEdit(suggestion)}
                        disabled={updateMutation.isPending}
                      >
                        <FontAwesomeIcon icon={faCheck} />
                      </IconButton>
                      <IconButton size="small" onClick={handleCancelEdit}>
                        <FontAwesomeIcon icon={faXmark} />
                      </IconButton>
                    </Box>
                  ) : (
                    <Typography variant="body2">{suggestion.value}</Typography>
                  )}
                </TableCell>
                <TableCell align="center">
                  <Switch
                    size="small"
                    checked={suggestion.isActive}
                    onChange={() => handleToggleActive(suggestion)}
                    disabled={updateMutation.isPending}
                  />
                </TableCell>
                <TableCell align="right">
                  {editingId !== suggestion.id && (
                    <>
                      <IconButton
                        size="small"
                        onClick={() => handleStartEdit(suggestion)}
                        title="Edit"
                      >
                        <FontAwesomeIcon icon={faPen} style={{ fontSize: '0.875rem' }} />
                      </IconButton>
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => setDeleteConfirmId(suggestion.id)}
                        title="Delete"
                      >
                        <FontAwesomeIcon icon={faTrash} style={{ fontSize: '0.875rem' }} />
                      </IconButton>
                    </>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Delete confirmation dialog */}
      <Dialog open={!!deleteConfirmId} onClose={() => setDeleteConfirmId(null)}>
        <DialogTitle>Delete Suggestion</DialogTitle>
        <DialogContent>
          <Typography>
            Remove &quot;{deleteTarget?.value}&quot; from the suggestion list?
          </Typography>
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={() => setDeleteConfirmId(null)}>
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton
            color="error"
            onClick={() => deleteConfirmId && handleDelete(deleteConfirmId)}
            disabled={deleteMutation.isPending}
          >
            Delete
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>
    </>
  )
}
