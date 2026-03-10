/**
 * EegCoverageTab - Coverage tab content for the EEG Entries page
 *
 * Displays the full EEG coverage dashboard showing capability target
 * coverage, task ratings, and unevaluated tasks.
 * Evaluators with the add_observation permission can trigger the
 * "Assess Task" flow directly from this view.
 *
 * @module features/eeg
 * @see EegEntriesPage
 */

import { Box } from '@mui/material'
import { EegCoverageDashboard } from './EegCoverageDashboard'

interface EegCoverageTabProps {
  /** The exercise ID scoping all coverage data */
  exerciseId: string
  /**
   * If provided the coverage dashboard shows "Assess" buttons on tasks.
   * Pass undefined when the current user cannot add observations.
   */
  onAssessTask?: (taskId: string, capabilityTargetId: string) => void
}

/**
 * Coverage tab panel rendered inside EegEntriesPage when the user is
 * on the "Coverage" tab.
 */
export const EegCoverageTab = ({ exerciseId, onAssessTask }: EegCoverageTabProps) => {
  return (
    <Box role="tabpanel" aria-labelledby="coverage-tab">
      <EegCoverageDashboard
        exerciseId={exerciseId}
        onAssessTask={onAssessTask}
      />
    </Box>
  )
}
