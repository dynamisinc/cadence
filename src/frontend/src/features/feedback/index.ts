/**
 * Feedback Feature Module
 *
 * Bug reports, feature requests, and general feedback.
 *
 * @module features/feedback
 */

// Components
export { FeedbackDialog } from './components/FeedbackDialog'

// Services
export { feedbackService } from './services/feedbackService'

// Types
export type {
  SubmitBugReportRequest,
  SubmitFeatureRequestRequest,
  SubmitGeneralFeedbackRequest,
  SubmitErrorReportRequest,
  FeedbackResponse,
  FeedbackTab,
} from './types'
