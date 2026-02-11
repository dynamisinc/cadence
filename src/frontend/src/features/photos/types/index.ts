/**
 * Photo Feature Types
 *
 * TypeScript types for photo capture operations.
 * Matches backend DTOs in Cadence.Core.Features.Photos.Models.DTOs
 */

export type PhotoStatus = 'Draft' | 'Complete'
export type ObservationStatus = 'Draft' | 'Complete'

/**
 * Photo DTO - Response from API
 */
export interface PhotoDto {
  id: string
  exerciseId: string
  observationId: string | null
  capturedById: string
  capturedByName: string | null
  fileName: string
  blobUri: string
  thumbnailUri: string
  fileSizeBytes: number
  capturedAt: string // DateTime as ISO string
  scenarioTime: string | null
  latitude: number | null
  longitude: number | null
  locationAccuracy: number | null
  displayOrder: number
  status: PhotoStatus
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
}

/**
 * Request metadata for uploading a photo
 */
export interface UploadPhotoRequest {
  capturedAt: string
  scenarioTime?: string | null
  latitude?: number | null
  longitude?: number | null
  locationAccuracy?: number | null
  observationId?: string | null
}

/**
 * Request body for updating a photo
 */
export interface UpdatePhotoRequest {
  observationId?: string | null
  displayOrder?: number | null
}

/**
 * Query parameters for listing photos
 */
export interface PhotoListQuery {
  capturedById?: string
  from?: string
  to?: string
  linkedOnly?: boolean | null
  page?: number
  pageSize?: number
}

/**
 * Request metadata for quick photo capture
 */
export interface QuickPhotoRequest {
  capturedAt: string
  scenarioTime?: string | null
  latitude?: number | null
  longitude?: number | null
  locationAccuracy?: number | null
}

/**
 * Response from quick photo capture
 */
export interface QuickPhotoResponse {
  photo: PhotoDto
  observationId: string
}

/**
 * Paginated list of photos
 */
export interface PhotoListResponse {
  photos: PhotoDto[]
  totalCount: number
  page: number
  pageSize: number
}

/**
 * Deleted Photo DTO - Response from trash/deleted photos API
 */
export interface DeletedPhotoDto {
  id: string
  exerciseId: string
  observationId: string | null
  capturedById: string
  capturedByName: string | null
  fileName: string
  blobUri: string
  thumbnailUri: string
  fileSizeBytes: number
  capturedAt: string
  scenarioTime: string | null
  status: PhotoStatus
  createdAt: string
  deletedAt: string | null
  deletedBy: string | null
}
