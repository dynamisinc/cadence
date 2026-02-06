/**
 * Feedback Feature Types
 *
 * @module features/feedback
 */

export interface SubmitBugReportRequest {
  title: string
  description: string
  stepsToReproduce?: string
  severity: string
}

export interface SubmitFeatureRequestRequest {
  title: string
  description: string
  useCase?: string
}

export interface SubmitGeneralFeedbackRequest {
  category: string
  subject: string
  message: string
}

export interface SubmitErrorReportRequest {
  errorMessage: string
  stackTrace?: string
  componentStack?: string
  url: string
  browser: string
}

export interface FeedbackResponse {
  referenceNumber: string
  message: string
}

export type FeedbackTab = 'bug' | 'feature' | 'feedback'
