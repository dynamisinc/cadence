import { useState, useEffect, useCallback } from 'react'
import { toast } from 'react-toastify'
import { notesService } from '../services/notesService'
import { useSignalR } from '../../../shared/hooks'
import type { NoteDto, CreateNoteRequest, UpdateNoteRequest } from '../types'

/** SignalR event data types */
interface NoteEventData {
  noteId: string;
  userId: string;
  timestamp: string;
}

/**
 * Hook for managing notes state and operations
 * Includes real-time updates via SignalR
 */
export const useNotes = () => {
  const [notes, setNotes] = useState<NoteDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // SignalR connection for real-time updates (optional - gracefully degrades if not configured)
  const signalRUrl = import.meta.env.VITE_SIGNALR_URL
  const { connectionState, on, off } = useSignalR({
    autoConnect: !!signalRUrl, // Only auto-connect if SignalR URL is configured
  })

  const fetchNotes = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await notesService.getNotes()
      setNotes(data)
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Failed to load notes'
      setError(message)
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }, [])

  const createNote = useCallback(
    async (request: CreateNoteRequest) => {
      try {
        setLoading(true)
        const newNote = await notesService.createNote(request)
        setNotes(prev => [newNote, ...prev])
        toast.success('Note created')
        return newNote
      } catch (err) {
        const message =
          err instanceof Error ? err.message : 'Failed to create note'
        toast.error(message)
        throw err
      } finally {
        setLoading(false)
      }
    },
    [],
  )

  const updateNote = useCallback(
    async (id: string, request: UpdateNoteRequest) => {
      try {
        setLoading(true)
        const updated = await notesService.updateNote(id, request)
        setNotes(prev =>
          prev.map(note => (note.id === id ? updated : note)),
        )
        toast.success('Note updated')
        return updated
      } catch (err) {
        const message =
          err instanceof Error ? err.message : 'Failed to update note'
        toast.error(message)
        throw err
      } finally {
        setLoading(false)
      }
    },
    [],
  )

  const deleteNote = useCallback(async (id: string) => {
    try {
      setLoading(true)
      await notesService.deleteNote(id)
      setNotes(prev => prev.filter(note => note.id !== id))
      toast.success('Note deleted')
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Failed to delete note'
      toast.error(message)
      throw err
    } finally {
      setLoading(false)
    }
  }, [])

  // Initial fetch
  useEffect(() => {
    fetchNotes()
  }, [fetchNotes])

  // SignalR real-time event handlers (only active when connected)
  useEffect(() => {
    // Skip if SignalR is not configured or not connected
    if (!signalRUrl || connectionState !== 'connected') return

    // Handler for when another user creates a note
    const handleNoteCreated = (_data: NoteEventData) => {
      // Refresh notes to get the new note
      fetchNotes()
    }

    // Handler for when another user updates a note
    const handleNoteUpdated = (_data: NoteEventData) => {
      // Refresh notes to get the updated content
      fetchNotes()
    }

    // Handler for when another user deletes a note
    const handleNoteDeleted = (data: NoteEventData) => {
      // Remove the deleted note from local state
      setNotes(prev => prev.filter(note => note.id !== data.noteId))
    }

    // Subscribe to SignalR events (using camelCase to match backend)
    on('noteCreated', handleNoteCreated)
    on('noteUpdated', handleNoteUpdated)
    on('noteDeleted', handleNoteDeleted)

    // Cleanup subscriptions on unmount
    return () => {
      off('noteCreated', handleNoteCreated as (...args: unknown[]) => void)
      off('noteUpdated', handleNoteUpdated as (...args: unknown[]) => void)
      off('noteDeleted', handleNoteDeleted as (...args: unknown[]) => void)
    }
  }, [signalRUrl, connectionState, on, off, fetchNotes])

  return {
    notes,
    loading,
    error,
    fetchNotes,
    createNote,
    updateNote,
    deleteNote,
  }
}

export default useNotes
