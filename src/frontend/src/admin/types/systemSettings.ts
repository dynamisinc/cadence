export interface SystemSettingsDto {
  id: string | null
  supportAddress: string | null
  defaultSenderAddress: string | null
  defaultSenderName: string | null
  effectiveSupportAddress: string
  effectiveDefaultSenderAddress: string
  effectiveDefaultSenderName: string
  // GitHub integration
  gitHubOwner: string | null
  gitHubRepo: string | null
  gitHubLabelsEnabled: boolean
  gitHubTokenConfigured: boolean
  gitHubTokenMasked: string | null
  updatedAt: string | null
  updatedBy: string | null
}

export interface UpdateSystemSettingsRequest {
  supportAddress: string | null
  defaultSenderAddress: string | null
  defaultSenderName: string | null
  // GitHub integration (null = no change, "__clear__" = remove)
  gitHubToken: string | null
  gitHubOwner: string | null
  gitHubRepo: string | null
  gitHubLabelsEnabled: boolean | null
}

export interface GitHubConnectionTestResult {
  success: boolean
  message: string
}
