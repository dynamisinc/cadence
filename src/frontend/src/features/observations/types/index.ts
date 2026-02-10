/**
 * Observation Feature Types
 *
 * TypeScript types for observation CRUD operations.
 * Matches backend DTOs in Cadence.Core.Features.Observations.Models.DTOs
 */

import { ObservationRating } from '../../../types'

export type ObservationStatus = 'Draft' | 'Complete'

/**
 * Capability tag DTO - Lightweight capability info for observation display
 */
export interface CapabilityTagDto {
  id: string
  name: string
  category: string | null
}

/**
 * Photo tag DTO - Lightweight photo info for observation display
 */
export interface PhotoTagDto {
  id: string
  thumbnailUri: string
  capturedAt: string
  displayOrder: number
}

/**
 * Observation DTO - Response from API
 */
export interface ObservationDto {
  id: string
  exerciseId: string
  injectId: string | null
  objectiveId: string | null
  content: string
  rating: ObservationRating | null
  recommendation: string | null
  observedAt: string // DateTime as ISO string
  location: string | null
  status: ObservationStatus
  photos: PhotoTagDto[]
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
  createdBy: string
  createdByName: string | null
  injectTitle: string | null
  injectNumber: number | null
  capabilities: CapabilityTagDto[] // S05: Capability tags
}

/**
 * Request body for creating a new observation
 */
export interface CreateObservationRequest {
  content: string
  rating?: ObservationRating | null
  recommendation?: string | null
  observedAt?: string | null // ISO datetime string
  location?: string | null
  injectId?: string | null
  objectiveId?: string | null
  capabilityIds?: string[] // S05: Capability tags
}

/**
 * Request body for updating an observation
 */
export interface UpdateObservationRequest {
  content: string
  rating?: ObservationRating | null
  recommendation?: string | null
  observedAt?: string | null
  location?: string | null
  injectId?: string | null
  objectiveId?: string | null
  capabilityIds?: string[] // S05: Capability tags
}
