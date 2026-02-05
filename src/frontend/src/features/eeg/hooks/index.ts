/**
 * EEG Hooks
 *
 * React Query hooks for Exercise Evaluation Guide feature.
 */

export {
  useCapabilityTargets,
  useCapabilityTarget,
  capabilityTargetKeys,
} from './useCapabilityTargets'

export {
  useCriticalTasks,
  useCriticalTasksByExercise,
  useCriticalTask,
  useLinkedInjects,
  criticalTaskKeys,
} from './useCriticalTasks'

export {
  useEegEntries,
  useEegEntriesByTask,
  useEegEntry,
  useEegCoverage,
  eegEntryKeys,
} from './useEegEntries'
