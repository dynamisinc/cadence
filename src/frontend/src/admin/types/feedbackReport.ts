export const FeedbackType = {
  BugReport: 'BugReport',
  FeatureRequest: 'FeatureRequest',
  General: 'General',
} as const
export type FeedbackType = (typeof FeedbackType)[keyof typeof FeedbackType]

export const FeedbackStatus = {
  New: 'New',
  InReview: 'InReview',
  Resolved: 'Resolved',
  Closed: 'Closed',
} as const
export type FeedbackStatus = (typeof FeedbackStatus)[keyof typeof FeedbackStatus]

export const FeedbackTypeLabels: Record<FeedbackType, string> = {
  [FeedbackType.BugReport]: 'Bug Report',
  [FeedbackType.FeatureRequest]: 'Feature Request',
  [FeedbackType.General]: 'General',
}

export const FeedbackStatusLabels: Record<FeedbackStatus, string> = {
  [FeedbackStatus.New]: 'New',
  [FeedbackStatus.InReview]: 'In Review',
  [FeedbackStatus.Resolved]: 'Resolved',
  [FeedbackStatus.Closed]: 'Closed',
}

export interface FeedbackReportDto {
  id: string
  referenceNumber: string
  type: FeedbackType
  status: FeedbackStatus
  title: string
  severity: string | null
  contentJson: string | null
  reporterEmail: string
  reporterName: string | null
  userRole: string | null
  orgName: string | null
  orgRole: string | null
  currentUrl: string | null
  screenSize: string | null
  appVersion: string | null
  commitSha: string | null
  exerciseId: string | null
  exerciseName: string | null
  exerciseRole: string | null
  adminNotes: string | null
  gitHubIssueNumber: number | null
  gitHubIssueUrl: string | null
  createdAt: string
  updatedAt: string
}

export interface UpdateFeedbackStatusRequest {
  status: FeedbackStatus
  adminNotes?: string | null
}

export interface PaginationInfo {
  totalCount: number
  totalPages: number
  page: number
  pageSize: number
}

export interface FeedbackListResponse {
  reports: FeedbackReportDto[]
  pagination: PaginationInfo
}

export interface FeedbackQueryParams {
  page?: number
  pageSize?: number
  search?: string
  type?: FeedbackType | null
  status?: FeedbackStatus | null
  sortBy?: string
  sortDesc?: boolean
}
