/**
 * ExerciseDetailTabs
 *
 * Tabbed interface for the ExerciseDetailPage (view-only mode). Renders:
 * - Tab 0 – Details: Exercise metadata + setup progress + MSEL progress
 * - Tab 1 – Objectives: ObjectiveList component
 * - Tab 2 – Participants: ExerciseParticipantsPage component
 * - Tab 3 – EEG Setup: CapabilityTargetList component
 *
 * @module features/exercises
 */

import type { FC } from 'react'
import {
  Box,
  Tabs,
  Tab,
  Paper,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faUsers, faClipboardCheck } from '@fortawesome/free-solid-svg-icons'

import { ObjectiveList } from '../../objectives'
import { CapabilityTargetList } from '../../eeg'
import { ExerciseParticipantsPage } from '../pages/ExerciseParticipantsPage'
import { ExerciseSetupProgressSection } from './ExerciseSetupProgressSection'
import type { ExerciseDto, SetupProgressDto, MselSummaryDto, ExerciseParticipantDto } from '../types'

// =========================================================================
// TabPanel helper
// =========================================================================

interface TabPanelProps {
  children?: React.ReactNode
  index: number
  value: number
}

function TabPanel({ children, value, index }: TabPanelProps) {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`exercise-tabpanel-${index}`}
      aria-labelledby={`exercise-tab-${index}`}
    >
      {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
    </div>
  )
}

// =========================================================================
// ExerciseDetailTabs
// =========================================================================

interface ExerciseDetailTabsProps {
  /** The exercise being displayed */
  exercise: ExerciseDto
  /** Exercise ID */
  exerciseId: string
  /** Currently active tab index */
  activeTab: number
  /** Called when the user changes tabs */
  onTabChange: (newTab: number) => void
  /** Whether the current user can edit the exercise */
  canEdit: boolean
  /** Setup progress data */
  setupProgress: SetupProgressDto | undefined
  /** Whether setup progress is loading */
  setupProgressLoading: boolean
  /** Setup progress error */
  setupProgressError: Error | null
  /** MSEL summary for the progress bar */
  mselSummary: MselSummaryDto | undefined
  /** The assigned Exercise Director participant */
  director: ExerciseParticipantDto | undefined
}

/**
 * Renders the tabbed interface for the exercise detail view.
 *
 * The tab bar and all four tab panels are contained here so ExerciseDetailPage
 * only needs to manage the `activeTab` state and pass down pre-fetched data.
 */
export const ExerciseDetailTabs: FC<ExerciseDetailTabsProps> = ({
  exercise,
  exerciseId,
  activeTab,
  onTabChange,
  canEdit,
  setupProgress,
  setupProgressLoading,
  setupProgressError,
  mselSummary,
  director,
}) => {
  return (
    <>
      {/* Tab Bar */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs
          value={activeTab}
          onChange={(_, newValue: number) => onTabChange(newValue)}
          aria-label="exercise detail tabs"
        >
          <Tab label="Details" id="exercise-tab-0" aria-controls="exercise-tabpanel-0" />
          <Tab label="Objectives" id="exercise-tab-1" aria-controls="exercise-tabpanel-1" />
          <Tab
            label="Participants"
            icon={<FontAwesomeIcon icon={faUsers} />}
            iconPosition="start"
            id="exercise-tab-2"
            aria-controls="exercise-tabpanel-2"
          />
          <Tab
            label="EEG Setup"
            icon={<FontAwesomeIcon icon={faClipboardCheck} />}
            iconPosition="start"
            id="exercise-tab-3"
            aria-controls="exercise-tabpanel-3"
          />
        </Tabs>
      </Box>

      {/* Details Tab */}
      <TabPanel value={activeTab} index={0}>
        <ExerciseSetupProgressSection
          exercise={exercise}
          exerciseId={exerciseId}
          setupProgress={setupProgress}
          setupProgressLoading={setupProgressLoading}
          setupProgressError={setupProgressError}
          mselSummary={mselSummary}
          director={director}
        />
      </TabPanel>

      {/* Objectives Tab */}
      <TabPanel value={activeTab} index={1}>
        <Paper sx={{ p: 3 }}>
          <ObjectiveList exerciseId={exercise.id} canEdit={canEdit} />
        </Paper>
      </TabPanel>

      {/* Participants Tab */}
      <TabPanel value={activeTab} index={2}>
        <ExerciseParticipantsPage exerciseId={exerciseId} />
      </TabPanel>

      {/* EEG Setup Tab */}
      <TabPanel value={activeTab} index={3}>
        <Paper sx={{ p: 3 }}>
          <CapabilityTargetList exerciseId={exercise.id} canEdit={canEdit} />
        </Paper>
      </TabPanel>
    </>
  )
}

export default ExerciseDetailTabs
