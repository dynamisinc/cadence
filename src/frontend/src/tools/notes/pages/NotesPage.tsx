import { useState } from 'react'
import {
  Box,
  Typography,
  Stack,
  Card,
  CardContent,
  IconButton,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import DeleteIcon from '@mui/icons-material/Delete'
import AddIcon from '@mui/icons-material/Add'
import { useNotes } from '../hooks/useNotes'
import { formatSmartDateTime } from '../../../shared/utils/dateUtils'
import {
  CobraPrimaryButton,
  CobraTextField,
  CobraDeleteButton,
  CobraLinkButton,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import type { NoteDto, CreateNoteRequest, UpdateNoteRequest } from '../types'

/**
 * Notes Page - Main page for viewing and managing notes
 *
 * Demonstrates:
 * - COBRA styling with MUI components
 * - CRUD operations via custom hook
 * - Dialog patterns for create/edit
 * - Loading and error states
 */
export const NotesPage = () => {
  const { notes, loading, error, createNote, updateNote, deleteNote } =
    useNotes()

  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [editingNote, setEditingNote] = useState<NoteDto | null>(null)
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')

  const handleOpenCreate = () => {
    setEditingNote(null)
    setTitle('')
    setContent('')
    setIsDialogOpen(true)
  }

  const handleOpenEdit = (note: NoteDto) => {
    setEditingNote(note)
    setTitle(note.title)
    setContent(note.content || '')
    setIsDialogOpen(true)
  }

  const handleClose = () => {
    setIsDialogOpen(false)
    setEditingNote(null)
    setTitle('')
    setContent('')
  }

  const handleSave = async () => {
    if (!title.trim()) return

    try {
      if (editingNote) {
        const request: UpdateNoteRequest = { title, content: content || null }
        await updateNote(editingNote.id, request)
      } else {
        const request: CreateNoteRequest = { title, content: content || null }
        await createNote(request)
      }
      handleClose()
    } catch {
      // Error handled by hook
    }
  }

  const handleDelete = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this note?')) {
      await deleteNote(id)
    }
  }

  if (loading && notes.length === 0) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="200px"
      >
        <CircularProgress />
      </Box>
    )
  }

  if (error && notes.length === 0) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography color="error">{error}</Typography>
        <CobraPrimaryButton onClick={() => window.location.reload()}>
          Retry
        </CobraPrimaryButton>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        marginBottom={2}
      >
        <Typography variant="h5">Notes</Typography>
        <CobraPrimaryButton startIcon={<AddIcon />} onClick={handleOpenCreate}>
          New Note
        </CobraPrimaryButton>
      </Stack>

      {notes.length === 0 ? (
        <Typography color="text.secondary">
          No notes yet. Create your first note!
        </Typography>
      ) : (
        <Stack spacing={CobraStyles.Spacing.FormFields}>
          {notes.map(note => (
            <Card key={note.id}>
              <CardContent>
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  alignItems="flex-start"
                >
                  <Box>
                    <Typography variant="h6">{note.title}</Typography>
                    {note.content && (
                      <Typography
                        variant="body2"
                        color="text.secondary"
                        sx={{ whiteSpace: 'pre-wrap' }}
                      >
                        {note.content}
                      </Typography>
                    )}
                    <Typography variant="caption" color="text.secondary">
                      Updated: {formatSmartDateTime(note.updatedAt)}
                    </Typography>
                  </Box>
                  <Stack direction="row">
                    <IconButton
                      onClick={() => handleOpenEdit(note)}
                      size="small"
                    >
                      <EditIcon />
                    </IconButton>
                    <IconButton
                      onClick={() => handleDelete(note.id)}
                      size="small"
                      color="error"
                    >
                      <DeleteIcon />
                    </IconButton>
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          ))}
        </Stack>
      )}

      {/* Create/Edit Dialog */}
      <Dialog open={isDialogOpen} onClose={handleClose} maxWidth="sm" fullWidth>
        <DialogTitle>{editingNote ? 'Edit Note' : 'New Note'}</DialogTitle>
        <DialogContent>
          <Stack spacing={CobraStyles.Spacing.FormFields} sx={{ mt: 1 }}>
            <CobraTextField
              label="Title"
              value={title}
              onChange={e => setTitle(e.target.value)}
              fullWidth
              required
              autoFocus
            />
            <CobraTextField
              label="Content"
              value={content}
              onChange={e => setContent(e.target.value)}
              fullWidth
              multiline
              rows={4}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <CobraLinkButton onClick={handleClose}>Cancel</CobraLinkButton>
          {editingNote && (
            <CobraDeleteButton
              onClick={() => {
                handleDelete(editingNote.id)
                handleClose()
              }}
            >
              Delete
            </CobraDeleteButton>
          )}
          <CobraPrimaryButton onClick={handleSave} disabled={!title.trim()}>
            {editingNote ? 'Save Changes' : 'Create Note'}
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

export default NotesPage
