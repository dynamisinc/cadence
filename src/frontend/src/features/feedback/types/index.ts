/**
 * Feedback Feature Types
 *
 * @module features/feedback
 */

/**
 * Client-supplied context captured automatically at the moment of submission.
 * Contains fields the server cannot know (page URL, screen, app version, exercise context).
 * Roles and org identity are sourced from JWT claims server-side and are not trusted from this object.
 */
export interface FeedbackClientContext {
  currentUrl: string
  screenSize: string
  appVersion: string
  commitSha: string
  exerciseId?: string
  exerciseName?: string
  exerciseRole?: string
}

export interface SubmitBugReportRequest {
  title: string
  description: string
  stepsToReproduce?: string
  severity: string
  context?: FeedbackClientContext
}

export interface SubmitFeatureRequestRequest {
  title: string
  description: string
  useCase?: string
  context?: FeedbackClientContext
}

export interface SubmitGeneralFeedbackRequest {
  category: string
  subject: string
  message: string
  context?: FeedbackClientContext
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
