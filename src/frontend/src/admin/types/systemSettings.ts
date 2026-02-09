export interface SystemSettingsDto {
  id: string | null
  supportAddress: string | null
  defaultSenderAddress: string | null
  defaultSenderName: string | null
  effectiveSupportAddress: string
  effectiveDefaultSenderAddress: string
  effectiveDefaultSenderName: string
  updatedAt: string | null
  updatedBy: string | null
}

export interface UpdateSystemSettingsRequest {
  supportAddress: string | null
  defaultSenderAddress: string | null
  defaultSenderName: string | null
}
