import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { notesService } from './notesService'
import { apiClient } from '../../../core/services/api'

// Mock the apiClient module
vi.mock('../../../core/services/api', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('notesService', () => {
  const mockNote = {
    id: '1',
    title: 'Test Note',
    content: 'Test content',
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.resetAllMocks()
  })

  describe('getNotes', () => {
    it('fetches notes from the API', async () => {
      const notes = [mockNote, { ...mockNote, id: '2' }]
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: notes })

      const result = await notesService.getNotes()

      expect(apiClient.get).toHaveBeenCalledWith('/api/notes')
      expect(result).toEqual(notes)
    })

    it('throws error when API fails', async () => {
      vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'))

      await expect(notesService.getNotes()).rejects.toThrow('Network error')
    })
  })

  describe('getNote', () => {
    it('fetches a single note by ID', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockNote })

      const result = await notesService.getNote('1')

      expect(apiClient.get).toHaveBeenCalledWith('/api/notes/1')
      expect(result).toEqual(mockNote)
    })
  })

  describe('createNote', () => {
    it('creates a new note', async () => {
      const newNote = { title: 'New Note', content: 'New content' }
      vi.mocked(apiClient.post).mockResolvedValueOnce({ data: { ...mockNote, ...newNote } })

      const result = await notesService.createNote(newNote)

      expect(apiClient.post).toHaveBeenCalledWith('/api/notes', newNote)
      expect(result.title).toBe('New Note')
    })

    it('creates note with null content', async () => {
      const newNote = { title: 'New Note', content: null }
      vi.mocked(apiClient.post).mockResolvedValueOnce({ data: { ...mockNote, ...newNote } })

      const result = await notesService.createNote(newNote)

      expect(apiClient.post).toHaveBeenCalledWith('/api/notes', newNote)
      expect(result.content).toBeNull()
    })
  })

  describe('updateNote', () => {
    it('updates an existing note', async () => {
      const updates = { title: 'Updated Title', content: 'Updated content' }
      vi.mocked(apiClient.put).mockResolvedValueOnce({ data: { ...mockNote, ...updates } })

      const result = await notesService.updateNote('1', updates)

      expect(apiClient.put).toHaveBeenCalledWith('/api/notes/1', updates)
      expect(result.title).toBe('Updated Title')
    })
  })

  describe('deleteNote', () => {
    it('deletes a note', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce({})

      await notesService.deleteNote('1')

      expect(apiClient.delete).toHaveBeenCalledWith('/api/notes/1')
    })
  })

  describe('restoreNote', () => {
    it('restores a deleted note', async () => {
      vi.mocked(apiClient.post).mockResolvedValueOnce({ data: mockNote })

      const result = await notesService.restoreNote('1')

      expect(apiClient.post).toHaveBeenCalledWith('/api/notes/1/restore')
      expect(result).toEqual(mockNote)
    })
  })
})
