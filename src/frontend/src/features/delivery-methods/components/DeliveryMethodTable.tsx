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
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTrash, faPen, faCheck, faXmark } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton, CobraTextField } from '@/theme/styledComponents'
import type { DeliveryMethodDto, UpdateDeliveryMethodRequest } from '../types'
import { useUpdateDeliveryMethod, useDeleteDeliveryMethod } from '../hooks/useDeliveryMethodManagement'

interface DeliveryMethodTableProps {
  methods: DeliveryMethodDto[]
  isLoading: boolean
}

export function DeliveryMethodTable({ methods, isLoading }: DeliveryMethodTableProps) {
  const updateMutation = useUpdateDeliveryMethod()
  const deleteMutation = useDeleteDeliveryMethod()

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editName, setEditName] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const handleStartEdit = (method: DeliveryMethodDto) => {
    setEditingId(method.id)
    setEditName(method.name)
    setEditDescription(method.description || '')
  }

  const handleCancelEdit = () => {
    setEditingId(null)
    setEditName('')
    setEditDescription('')
  }

  const handleSaveEdit = (method: DeliveryMethodDto) => {
    const trimmedName = editName.trim()
    if (!trimmedName) {
      handleCancelEdit()
      return
    }

    const trimmedDesc = editDescription.trim()
    if (trimmedName === method.name && trimmedDesc === (method.description || '')) {
      handleCancelEdit()
      return
    }

    const request: UpdateDeliveryMethodRequest = {
      name: trimmedName,
      description: trimmedDesc || null,
      sortOrder: method.sortOrder,
      isActive: method.isActive,
      isOther: method.isOther,
    }

    updateMutation.mutate(
      { id: method.id, request },
      { onSuccess: () => handleCancelEdit() },
    )
  }

  const handleToggleActive = (method: DeliveryMethodDto) => {
    const request: UpdateDeliveryMethodRequest = {
      name: method.name,
      description: method.description,
      sortOrder: method.sortOrder,
      isActive: !method.isActive,
      isOther: method.isOther,
    }
    updateMutation.mutate({ id: method.id, request })
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

  if (methods.length === 0) {
    return (
      <Alert severity="info">
        No delivery methods configured. Add delivery methods to populate the inject form dropdown.
      </Alert>
    )
  }

  const deleteTarget = methods.find(m => m.id === deleteConfirmId)

  return (
    <>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Description</TableCell>
              <TableCell align="center" sx={{ width: 80 }}>Active</TableCell>
              <TableCell align="right" sx={{ width: 100 }}>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {methods.map(method => (
              <TableRow key={method.id} sx={{ opacity: method.isActive ? 1 : 0.5 }}>
                <TableCell>
                  {editingId === method.id ? (
                    <CobraTextField
                      value={editName}
                      onChange={e => setEditName(e.target.value)}
                      onKeyDown={e => {
                        if (e.key === 'Enter') handleSaveEdit(method)
                        if (e.key === 'Escape') handleCancelEdit()
                      }}
                      size="small"
                      fullWidth
                      autoFocus
                    />
                  ) : (
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="body2">{method.name}</Typography>
                      {method.isOther && (
                        <Chip label="Other" size="small" variant="outlined" color="info" />
                      )}
                    </Box>
                  )}
                </TableCell>
                <TableCell>
                  {editingId === method.id ? (
                    <CobraTextField
                      value={editDescription}
                      onChange={e => setEditDescription(e.target.value)}
                      onKeyDown={e => {
                        if (e.key === 'Enter') handleSaveEdit(method)
                        if (e.key === 'Escape') handleCancelEdit()
                      }}
                      size="small"
                      fullWidth
                      placeholder="Optional description"
                    />
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      {method.description || '\u2014'}
                    </Typography>
                  )}
                </TableCell>
                <TableCell align="center">
                  <Switch
                    size="small"
                    checked={method.isActive}
                    onChange={() => handleToggleActive(method)}
                    disabled={updateMutation.isPending}
                  />
                </TableCell>
                <TableCell align="right">
                  {editingId === method.id ? (
                    <>
                      <IconButton
                        size="small"
                        color="primary"
                        onClick={() => handleSaveEdit(method)}
                        disabled={updateMutation.isPending}
                      >
                        <FontAwesomeIcon icon={faCheck} />
                      </IconButton>
                      <IconButton size="small" onClick={handleCancelEdit}>
                        <FontAwesomeIcon icon={faXmark} />
                      </IconButton>
                    </>
                  ) : (
                    <>
                      <IconButton
                        size="small"
                        onClick={() => handleStartEdit(method)}
                        title="Edit"
                      >
                        <FontAwesomeIcon icon={faPen} style={{ fontSize: '0.875rem' }} />
                      </IconButton>
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => setDeleteConfirmId(method.id)}
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
        <DialogTitle>Delete Delivery Method</DialogTitle>
        <DialogContent>
          <Typography>
            Remove &quot;{deleteTarget?.name}&quot; from the delivery methods list?
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Existing injects using this method will not be affected.
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
