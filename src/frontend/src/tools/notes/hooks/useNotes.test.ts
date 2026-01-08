import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { useNotes } from './useNotes'
import { notesService } from '../services/notesService'
import type { NoteDto } from '../types'

// Mock the notes service
vi.mock('../services/notesService', () => ({
  notesService: {
    getNotes: vi.fn(),
    getNote: vi.fn(),
    createNote: vi.fn(),
    updateNote: vi.fn(),
    deleteNote: vi.fn(),
    restoreNote: vi.fn(),
  },
}))

// Mock react-toastify
vi.mock('react-toastify', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

describe('useNotes', () => {
  const mockNotes: NoteDto[] = [
    {
      id: '1',
      title: 'Note 1',
      content: 'Content 1',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
    {
      id: '2',
      title: 'Note 2',
      content: 'Content 2',
      createdAt: '2024-01-02T00:00:00Z',
      updatedAt: '2024-01-02T00:00:00Z',
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(notesService.getNotes).mockResolvedValue(mockNotes)
  })

  describe('initial state', () => {
    it('starts with empty notes array', () => {
      vi.mocked(notesService.getNotes).mockImplementation(
        () => new Promise(() => {}), // Never resolves
      )

      const { result } = renderHook(() => useNotes())

      expect(result.current.notes).toEqual([])
    })

    it('starts with loading state', () => {
      vi.mocked(notesService.getNotes).mockImplementation(
        () => new Promise(() => {}),
      )

      const { result } = renderHook(() => useNotes())

      expect(result.current.loading).toBe(true)
    })

    it('starts with no error', () => {
      vi.mocked(notesService.getNotes).mockImplementation(
        () => new Promise(() => {}),
      )

      const { result } = renderHook(() => useNotes())

      expect(result.current.error).toBeNull()
    })
  })

  describe('fetchNotes', () => {
    it('fetches notes on mount', async () => {
      const { result } = renderHook(() => useNotes())

      await waitFor(() => {
        expect(result.current.notes).toEqual(mockNotes)
      })

      expect(notesService.getNotes).toHaveBeenCalledTimes(1)
    })

    it('sets loading to false after fetch', async () => {
      const { result } = renderHook(() => useNotes())

      await waitFor(() => {
        expect(result.current.loading).toBe(false)
      })
    })

    it('sets error on fetch failure', async () => {
      vi.mocked(notesService.getNotes).mockRejectedValue(
        new Error('Failed to fetch'),
      )

      const { result } = renderHook(() => useNotes())

      await waitFor(() => {
        expect(result.current.error).toBe('Failed to fetch')
      })
    })
  })

  describe('createNote', () => {
    it('creates a note and adds it to the list', async () => {
      const newNote: NoteDto = {
        id: '3',
        title: 'New Note',
        content: 'New content',
        createdAt: '2024-01-03T00:00:00Z',
        updatedAt: '2024-01-03T00:00:00Z',
      }

      vi.mocked(notesService.createNote).mockResolvedValue(newNote)

      const { result } = renderHook(() => useNotes())

      await waitFor(() => {
        expect(result.current.notes).toEqual(mockNotes)
      })

      await act(async () => {
        await result.current.createNote({
          title: 'New Note',
          content: 'New content',
        })
      })

      expect(result.current.notes).toContainEqual(newNote)
      expect(result.current.notes[0]).toEqual(newNote) // Added at start
    })

    it('returns the created note', async () => {
      const newNote: NoteDto = {
        id: '3',
        title: 'New Note',
        content: null,
        createdAt: '2024-01-03T00:00:00Z',
        updatedAt: '2024-01-03T00:00:00Z',
      }

      vi.mocked(notesService.createNote).mockResolvedValue(newNote)

      const { result } = renderHook(() => useNotes())

      await waitFor(() => {
        expect(result.current.loading).toBe(false)
      })

      let createdNote: NoteDto | undefined
      await act(async () => {
        createdNote = await result.current.createNote({
          title: 'New Note',
          content: null,
        })
      })

      expect(createdNote).toEqual(newNote)
    })
  })

  describe('updateNote', () => {
    it('updates a note in the list', async () => {
      const updatedNote: NoteDto = {
        ...mockNotes[0],
        title: 'Updated Title',
      }

      vi.mocked(notesService.updateNote).mockResolvedValue(updatedNote)

      const { result } = renderHook(() => useNotes())

      await waitFor(() => {
        expect(result.current.notes).toEqual(mockNotes)
      })

      await act(async () => {
        await result.current.updateNote('1', {
          title: 'Updated Title',
          content: mockNotes[0].content,
        })
      })

      expect(result.current.notes.find(n => n.id === '1')?.title).toBe(
        'Updated Title',
      )
    })
  })

  describe('deleteNote', () => {
    it('removes a note from the list', async () => {
      vi.mocked(notesService.deleteNote).mockResolvedValue()

      const { result } = renderHook(() => useNotes())

      await waitFor(() => {
        expect(result.current.notes).toEqual(mockNotes)
      })

      await act(async () => {
        await result.current.deleteNote('1')
      })

      expect(result.current.notes.find(n => n.id === '1')).toBeUndefined()
      expect(result.current.notes).toHaveLength(1)
    })
  })
})
