/**
 * Observation Feature Types
 *
 * TypeScript types for observation CRUD operations.
 * Matches backend DTOs in Cadence.Core.Features.Observations.Models.DTOs
 */

import { ObservationRating } from '../../../types'

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
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
  createdBy: string
  createdByName: string | null
  injectTitle: string | null
  injectNumber: number | null
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
}
