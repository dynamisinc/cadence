export interface OrganizationSuggestionDto {
  id: string
  fieldName: SuggestionFieldName
  value: string
  sortOrder: number
  isActive: boolean
  isBlocked: boolean
  createdAt: string
  updatedAt: string
}

export type SuggestionFieldName =
  | 'Source'
  | 'Target'
  | 'Track'
  | 'LocationName'
  | 'LocationType'
  | 'ResponsibleController'

export interface CreateSuggestionRequest {
  fieldName: SuggestionFieldName
  value: string
  sortOrder?: number
}

export interface UpdateSuggestionRequest {
  value: string
  sortOrder: number
  isActive: boolean
}

export interface BulkCreateSuggestionsRequest {
  fieldName: SuggestionFieldName
  values: string[]
}

export interface BulkCreateSuggestionsResult {
  totalProvided: number
  created: number
  skippedDuplicates: number
}

export interface BlockSuggestionRequest {
  fieldName: SuggestionFieldName
  value: string
}

export const SUGGESTION_FIELDS: {
  name: SuggestionFieldName
  label: string
}[] = [
  { name: 'Source', label: 'Source (From)' },
  { name: 'Target', label: 'Target (To)' },
  { name: 'Track', label: 'Track' },
  { name: 'LocationName', label: 'Location Name' },
  { name: 'LocationType', label: 'Location Type' },
  { name: 'ResponsibleController', label: 'Responsible Controller' },
]
