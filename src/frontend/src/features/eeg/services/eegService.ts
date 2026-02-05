/**
 * EEG (Exercise Evaluation Guide) Service
 *
 * API client for capability targets, critical tasks, and EEG entries.
 */

import { apiClient } from '@/core/services/api'
import type {
  CapabilityTargetDto,
  CapabilityTargetListResponse,
  CreateCapabilityTargetRequest,
  UpdateCapabilityTargetRequest,
  CriticalTaskDto,
  CriticalTaskListResponse,
  CreateCriticalTaskRequest,
  UpdateCriticalTaskRequest,
  SetLinkedInjectsRequest,
  EegEntryDto,
  EegEntryListResponse,
  CreateEegEntryRequest,
  UpdateEegEntryRequest,
  EegCoverageDto,
} from '../types'

// =============================================================================
// Capability Target API
// =============================================================================

export const capabilityTargetService = {
  /**
   * Get all capability targets for an exercise
   */
  async getByExercise(exerciseId: string): Promise<CapabilityTargetListResponse> {
    const response = await apiClient.get<CapabilityTargetListResponse>(
      `/exercises/${exerciseId}/capability-targets`,
    )
    return response.data
  },

  /**
   * Get a single capability target by ID
   */
  async getById(id: string): Promise<CapabilityTargetDto> {
    const response = await apiClient.get<CapabilityTargetDto>(`/capability-targets/${id}`)
    return response.data
  },

  /**
   * Create a new capability target
   */
  async create(
    exerciseId: string,
    request: CreateCapabilityTargetRequest,
  ): Promise<CapabilityTargetDto> {
    const response = await apiClient.post<CapabilityTargetDto>(
      `/exercises/${exerciseId}/capability-targets`,
      request,
    )
    return response.data
  },

  /**
   * Update an existing capability target
   */
  async update(
    exerciseId: string,
    id: string,
    request: UpdateCapabilityTargetRequest,
  ): Promise<CapabilityTargetDto> {
    const response = await apiClient.put<CapabilityTargetDto>(
      `/exercises/${exerciseId}/capability-targets/${id}`,
      request,
    )
    return response.data
  },

  /**
   * Delete a capability target (cascades to critical tasks)
   */
  async delete(exerciseId: string, id: string): Promise<void> {
    await apiClient.delete(`/exercises/${exerciseId}/capability-targets/${id}`)
  },

  /**
   * Reorder capability targets
   */
  async reorder(exerciseId: string, orderedIds: string[]): Promise<void> {
    await apiClient.put(`/exercises/${exerciseId}/capability-targets/reorder`, orderedIds)
  },
}

// =============================================================================
// Critical Task API
// =============================================================================

export const criticalTaskService = {
  /**
   * Get all critical tasks for a capability target
   */
  async getByCapabilityTarget(targetId: string): Promise<CriticalTaskListResponse> {
    const response = await apiClient.get<CriticalTaskListResponse>(
      `/capability-targets/${targetId}/critical-tasks`,
    )
    return response.data
  },

  /**
   * Get all critical tasks for an exercise
   */
  async getByExercise(
    exerciseId: string,
    filters?: { hasInjects?: boolean; hasEegEntries?: boolean },
  ): Promise<CriticalTaskListResponse> {
    const params = new URLSearchParams()
    if (filters?.hasInjects !== undefined) {
      params.append('hasInjects', String(filters.hasInjects))
    }
    if (filters?.hasEegEntries !== undefined) {
      params.append('hasEegEntries', String(filters.hasEegEntries))
    }
    const response = await apiClient.get<CriticalTaskListResponse>(
      `/exercises/${exerciseId}/critical-tasks`,
      { params },
    )
    return response.data
  },

  /**
   * Get a single critical task by ID
   */
  async getById(id: string): Promise<CriticalTaskDto> {
    const response = await apiClient.get<CriticalTaskDto>(`/critical-tasks/${id}`)
    return response.data
  },

  /**
   * Create a new critical task
   */
  async create(
    exerciseId: string,
    targetId: string,
    request: CreateCriticalTaskRequest,
  ): Promise<CriticalTaskDto> {
    const response = await apiClient.post<CriticalTaskDto>(
      `/exercises/${exerciseId}/capability-targets/${targetId}/critical-tasks`,
      request,
    )
    return response.data
  },

  /**
   * Update an existing critical task
   */
  async update(
    exerciseId: string,
    id: string,
    request: UpdateCriticalTaskRequest,
  ): Promise<CriticalTaskDto> {
    const response = await apiClient.put<CriticalTaskDto>(
      `/exercises/${exerciseId}/critical-tasks/${id}`,
      request,
    )
    return response.data
  },

  /**
   * Delete a critical task
   */
  async delete(exerciseId: string, id: string): Promise<void> {
    await apiClient.delete(`/exercises/${exerciseId}/critical-tasks/${id}`)
  },

  /**
   * Reorder critical tasks within a capability target
   */
  async reorder(exerciseId: string, targetId: string, orderedIds: string[]): Promise<void> {
    await apiClient.put(
      `/exercises/${exerciseId}/capability-targets/${targetId}/critical-tasks/reorder`,
      orderedIds,
    )
  },

  /**
   * Set linked injects for a critical task
   */
  async setLinkedInjects(
    exerciseId: string,
    id: string,
    request: SetLinkedInjectsRequest,
  ): Promise<void> {
    await apiClient.put(`/exercises/${exerciseId}/critical-tasks/${id}/injects`, request)
  },

  /**
   * Get linked inject IDs for a critical task
   */
  async getLinkedInjectIds(id: string): Promise<string[]> {
    const response = await apiClient.get<string[]>(`/critical-tasks/${id}/injects`)
    return response.data
  },
}

// =============================================================================
// EEG Entry API
// =============================================================================

export const eegEntryService = {
  /**
   * Get all EEG entries for an exercise
   */
  async getByExercise(exerciseId: string): Promise<EegEntryListResponse> {
    const response = await apiClient.get<EegEntryListResponse>(
      `/exercises/${exerciseId}/eeg-entries`,
    )
    return response.data
  },

  /**
   * Get all EEG entries for a critical task
   */
  async getByCriticalTask(taskId: string): Promise<EegEntryListResponse> {
    const response = await apiClient.get<EegEntryListResponse>(
      `/critical-tasks/${taskId}/eeg-entries`,
    )
    return response.data
  },

  /**
   * Get a single EEG entry by ID
   */
  async getById(id: string): Promise<EegEntryDto> {
    const response = await apiClient.get<EegEntryDto>(`/eeg-entries/${id}`)
    return response.data
  },

  /**
   * Create a new EEG entry
   */
  async create(
    exerciseId: string,
    taskId: string,
    request: CreateEegEntryRequest,
  ): Promise<EegEntryDto> {
    const response = await apiClient.post<EegEntryDto>(
      `/exercises/${exerciseId}/critical-tasks/${taskId}/eeg-entries`,
      request,
    )
    return response.data
  },

  /**
   * Update an existing EEG entry
   */
  async update(
    exerciseId: string,
    id: string,
    request: UpdateEegEntryRequest,
  ): Promise<EegEntryDto> {
    const response = await apiClient.put<EegEntryDto>(
      `/exercises/${exerciseId}/eeg-entries/${id}`,
      request,
    )
    return response.data
  },

  /**
   * Delete an EEG entry
   */
  async delete(exerciseId: string, id: string): Promise<void> {
    await apiClient.delete(`/exercises/${exerciseId}/eeg-entries/${id}`)
  },

  /**
   * Get EEG coverage statistics for an exercise
   */
  async getCoverage(exerciseId: string): Promise<EegCoverageDto> {
    const response = await apiClient.get<EegCoverageDto>(`/exercises/${exerciseId}/eeg-coverage`)
    return response.data
  },
}

// =============================================================================
// EEG Export API
// =============================================================================

export interface ExportEegOptions {
  format?: 'xlsx' | 'json'
  includeSummary?: boolean
  includeByCapability?: boolean
  includeAllEntries?: boolean
  includeCoverageGaps?: boolean
  includeEvaluatorNames?: boolean
  includeFormatting?: boolean
  filename?: string
}

export const eegExportService = {
  /**
   * Export EEG data to Excel format
   * Returns the file as a blob for download
   */
  async exportToExcel(exerciseId: string, options: ExportEegOptions = {}): Promise<Blob> {
    const params = new URLSearchParams()
    params.append('format', 'xlsx')
    if (options.includeSummary !== undefined)
      params.append('includeSummary', String(options.includeSummary))
    if (options.includeByCapability !== undefined)
      params.append('includeByCapability', String(options.includeByCapability))
    if (options.includeAllEntries !== undefined)
      params.append('includeAllEntries', String(options.includeAllEntries))
    if (options.includeCoverageGaps !== undefined)
      params.append('includeCoverageGaps', String(options.includeCoverageGaps))
    if (options.includeEvaluatorNames !== undefined)
      params.append('includeEvaluatorNames', String(options.includeEvaluatorNames))
    if (options.includeFormatting !== undefined)
      params.append('includeFormatting', String(options.includeFormatting))
    if (options.filename) params.append('filename', options.filename)

    const response = await apiClient.get(`/exercises/${exerciseId}/eeg-export`, {
      params,
      responseType: 'blob',
    })
    return response.data
  },

  /**
   * Export EEG data to JSON format
   */
  async exportToJson(
    exerciseId: string,
    includeEvaluatorNames: boolean = true,
  ): Promise<EegExportJsonDto> {
    const params = new URLSearchParams()
    params.append('format', 'json')
    params.append('includeEvaluatorNames', String(includeEvaluatorNames))

    const response = await apiClient.get<EegExportJsonDto>(
      `/exercises/${exerciseId}/eeg-export`,
      { params },
    )
    return response.data
  },

  /**
   * Download EEG export as a file
   */
  async downloadExport(exerciseId: string, options: ExportEegOptions = {}): Promise<void> {
    const blob = await this.exportToExcel(exerciseId, options)
    const filename = options.filename || `EEG_Export_${new Date().toISOString().split('T')[0]}`
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${filename}.xlsx`
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    document.body.removeChild(a)
  },
}

// JSON export types
export interface EegExportJsonDto {
  exercise: ExerciseInfoJsonDto
  summary: EegSummaryJsonDto
  byCapability: CapabilityExportJsonDto[]
  coverageGaps: CoverageGapJsonDto[]
  generatedAt: string
}

export interface ExerciseInfoJsonDto {
  name: string
  date: string
  status: string
}

export interface EegSummaryJsonDto {
  totalEntries: number
  tasksCoverage: TaskCoverageJsonDto
  ratingDistribution: RatingDistributionJsonDto
}

export interface TaskCoverageJsonDto {
  evaluated: number
  total: number
  percentage: number
}

export interface RatingDistributionJsonDto {
  p: number
  s: number
  m: number
  u: number
}

export interface CapabilityExportJsonDto {
  capabilityName: string
  targetDescription: string
  tasks: TaskExportJsonDto[]
}

export interface TaskExportJsonDto {
  taskDescription: string
  entries: EntryExportJsonDto[]
}

export interface EntryExportJsonDto {
  rating: string
  observation: string
  evaluator: string | null
  observedAt: string
}

export interface CoverageGapJsonDto {
  capabilityName: string
  targetDescription: string
  taskDescription: string
}

// Default export with all services
export const eegService = {
  capabilityTargets: capabilityTargetService,
  criticalTasks: criticalTaskService,
  eegEntries: eegEntryService,
  export: eegExportService,
}

export default eegService
