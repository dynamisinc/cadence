/**
 * EEG Components
 *
 * React components for Exercise Evaluation Guide feature.
 */

// Capability Target components
export { CapabilityTargetList } from './CapabilityTargetList'
export { CapabilityTargetCard } from './CapabilityTargetCard'
export { CapabilityTargetFormDialog } from './CapabilityTargetFormDialog'

// Critical Task components
export { CriticalTaskList } from './CriticalTaskList'
export { CriticalTaskFormDialog } from './CriticalTaskFormDialog'

// Re-export constants
export {
  CAPABILITY_TARGET_FIELD_LIMITS,
  CRITICAL_TASK_FIELD_LIMITS,
} from '../constants'

// EEG Entry components
export { EegEntryForm } from './EegEntryForm'
export { PerformanceRatingSelector } from './PerformanceRatingSelector'
export { EvaluatorContactPrompt } from './EvaluatorContactPrompt'
export { shouldShowPhonePrompt, isPromptDismissed } from '../utils/phonePromptUtils'

// Dashboard components
export { EegCoverageDashboard } from './EegCoverageDashboard'

// List components
export { EegEntriesList } from './EegEntriesList'
export { EegEntriesGroupedByCapability } from './EegEntriesGroupedByCapability'
export { EegEntriesGroupedByEvaluator } from './EegEntriesGroupedByEvaluator'
export { EntryDetailDialog, DeleteConfirmDialog } from './EegEntryDialogs'

// Export components
export { EegExportDialog } from './EegExportDialog'
export { EegDocumentDialog } from './EegDocumentDialog'
