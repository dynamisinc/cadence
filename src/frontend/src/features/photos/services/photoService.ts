/**
 * Photo API Service
 *
 * Handles all API calls for photo capture operations.
 * Photo uploads use multipart/form-data with automatic image compression.
 */

import { apiClient } from '../../../core/services/api'
import type {
  PhotoDto,
  PhotoListQuery,
  PhotoListResponse,
  UpdatePhotoRequest,
  QuickPhotoResponse,
  DeletedPhotoDto,
} from '../types'

export const photoService = {
  /**
   * Upload a photo with metadata
   * The FormData should contain:
   * - file: The image file
   * - capturedAt: ISO datetime string
   * - scenarioTime?: ISO datetime string (optional)
   * - latitude?: number (optional)
   * - longitude?: number (optional)
   * - locationAccuracy?: number (optional)
   * - observationId?: string (optional)
   */
  uploadPhoto: async (
    exerciseId: string, formData: FormData, idempotencyKey?: string,
  ): Promise<PhotoDto> => {
    const headers: Record<string, string> = { 'Content-Type': 'multipart/form-data' }
    if (idempotencyKey) {
      headers['X-Idempotency-Key'] = idempotencyKey
    }
    const response = await apiClient.post<PhotoDto>(
      `/exercises/${exerciseId}/photos`,
      formData,
      { headers },
    )
    return response.data
  },

  /**
   * Get all photos for an exercise with optional filtering
   */
  getPhotos: async (
    exerciseId: string,
    query?: PhotoListQuery,
  ): Promise<PhotoListResponse> => {
    const response = await apiClient.get<PhotoListResponse>(
      `/exercises/${exerciseId}/photos`,
      { params: query },
    )
    return response.data
  },

  /**
   * Get a single photo by ID
   */
  getPhoto: async (exerciseId: string, photoId: string): Promise<PhotoDto> => {
    const response = await apiClient.get<PhotoDto>(
      `/exercises/${exerciseId}/photos/${photoId}`,
    )
    return response.data
  },

  /**
   * Update photo metadata (link to observation, change display order)
   */
  updatePhoto: async (
    exerciseId: string,
    photoId: string,
    request: UpdatePhotoRequest,
  ): Promise<PhotoDto> => {
    const response = await apiClient.put<PhotoDto>(
      `/exercises/${exerciseId}/photos/${photoId}`,
      request,
    )
    return response.data
  },

  /**
   * Delete a photo (soft delete)
   */
  deletePhoto: async (exerciseId: string, photoId: string): Promise<void> => {
    await apiClient.delete(`/exercises/${exerciseId}/photos/${photoId}`)
  },

  /**
   * Quick photo capture - upload photo and auto-create observation
   * The FormData should contain:
   * - file: The image file
   * - capturedAt: ISO datetime string
   * - scenarioTime?: ISO datetime string (optional)
   * - latitude?: number (optional)
   * - longitude?: number (optional)
   * - locationAccuracy?: number (optional)
   */
  quickPhoto: async (
    exerciseId: string,
    formData: FormData,
    idempotencyKey?: string,
  ): Promise<QuickPhotoResponse> => {
    const headers: Record<string, string> = { 'Content-Type': 'multipart/form-data' }
    if (idempotencyKey) {
      headers['X-Idempotency-Key'] = idempotencyKey
    }
    const response = await apiClient.post<QuickPhotoResponse>(
      `/exercises/${exerciseId}/photos/quick`,
      formData,
      { headers },
    )
    return response.data
  },

  /**
   * Get all soft-deleted photos for an exercise
   */
  getDeletedPhotos: async (exerciseId: string): Promise<DeletedPhotoDto[]> => {
    const response = await apiClient.get<DeletedPhotoDto[]>(
      `/exercises/${exerciseId}/photos/deleted`,
    )
    return response.data
  },

  /**
   * Restore a soft-deleted photo
   */
  restorePhoto: async (exerciseId: string, photoId: string): Promise<PhotoDto> => {
    const response = await apiClient.post<PhotoDto>(
      `/exercises/${exerciseId}/photos/${photoId}/restore`,
    )
    return response.data
  },

  /**
   * Permanently delete a photo (cannot be undone)
   */
  permanentDeletePhoto: async (exerciseId: string, photoId: string): Promise<void> => {
    await apiClient.delete(
      `/exercises/${exerciseId}/photos/${photoId}/permanent`,
    )
  },
}

export default photoService
