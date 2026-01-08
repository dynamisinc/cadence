import { apiClient } from '../../../core/services/api'
import type { NoteDto, CreateNoteRequest, UpdateNoteRequest } from '../types'

/**
 * Notes API Service
 *
 * Handles all API calls for notes CRUD operations.
 */
export const notesService = {
  /**
   * Get all notes for the current user
   */
  getNotes: async (): Promise<NoteDto[]> => {
    const response = await apiClient.get<NoteDto[]>('/api/notes')
    return response.data
  },

  /**
   * Get a single note by ID
   */
  getNote: async (id: string): Promise<NoteDto> => {
    const response = await apiClient.get<NoteDto>(`/api/notes/${id}`)
    return response.data
  },

  /**
   * Create a new note
   */
  createNote: async (request: CreateNoteRequest): Promise<NoteDto> => {
    const response = await apiClient.post<NoteDto>('/api/notes', request)
    return response.data
  },

  /**
   * Update an existing note
   */
  updateNote: async (id: string, request: UpdateNoteRequest): Promise<NoteDto> => {
    const response = await apiClient.put<NoteDto>(`/api/notes/${id}`, request)
    return response.data
  },

  /**
   * Delete a note (soft delete)
   */
  deleteNote: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/notes/${id}`)
  },

  /**
   * Restore a deleted note
   */
  restoreNote: async (id: string): Promise<NoteDto> => {
    const response = await apiClient.post<NoteDto>(`/api/notes/${id}/restore`)
    return response.data
  },
}

export default notesService
