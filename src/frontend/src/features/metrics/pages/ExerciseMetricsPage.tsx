/**
 * ExerciseMetricsPage
 *
 * Tabbed metrics page for after-action review (AAR).
 * Shows inject delivery summary (S02), observation summary (S03), and timeline (S04).
 */

import { useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Box,
  Tabs,
  Tab,
  Paper,
  Skeleton,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { PageHeader, HelpTooltip } from '@/shared/components'
import { useExerciseRole } from '../../auth'
import {
  faFire,
  faClipboardList,
  faClock,
  faChartBar,
  faUserCog,
  faUserCheck,
  faChartPie,
  faShieldAlt,
  faHome,
} from '@fortawesome/free-solid-svg-icons'

import { useExercise } from '../../exercises/hooks/useExercise'
import { useBreadcrumbs } from '../../../core/contexts'
import { InjectSummaryPanel } from '../components/InjectSummaryPanel'
import { ObservationSummaryPanel } from '../components/ObservationSummaryPanel'
import { TimelineSummaryPanel } from '../components/TimelineSummaryPanel'
import { ControllerActivityPanel } from '../components/ControllerActivityPanel'
import { EvaluatorCoveragePanel } from '../components/EvaluatorCoveragePanel'
import { RatingChartsPanel } from '../components/RatingChartsPanel'
import { CapabilityPerformancePanel } from '../components/CapabilityPerformancePanel'
import CobraStyles from '../../../theme/CobraStyles'

interface TabPanelProps {
  children?: React.ReactNode
  index: number
  value: number
}

const TabPanel = ({ children, value, index }: TabPanelProps) => (
  <div role="tabpanel" hidden={value !== index} id={`metrics-tabpanel-${index}`}>
    {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
  </div>
)

export const ExerciseMetricsPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const [activeTab, setActiveTab] = useState(0)

  const { exercise, loading: isLoading, error } = useExercise(exerciseId ?? '')
  const { effectiveRole } = useExerciseRole(exerciseId ?? null)

  // Set breadcrumbs with exercise name
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'Metrics' },
      ]
      : undefined,
  )

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue)
  }

  if (isLoading) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Skeleton variant="text" width={300} height={40} />
        <Skeleton variant="rectangular" height={48} sx={{ mt: 2 }} />
        <Skeleton variant="rectangular" height={400} sx={{ mt: 2 }} />
      </Box>
    )
  }

  if (error || !exercise) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Alert severity="error">
          Failed to load exercise. Please try again.
        </Alert>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="Exercise Metrics"
        icon={faChartBar}
        subtitle={`After-action review data for ${exercise.name}`}
        chips={<HelpTooltip helpKey="metrics.overview" exerciseRole={effectiveRole ?? undefined} compact />}
      />

      {/* Tabs */}
      <Paper elevation={0} sx={{ borderBottom: 1, borderColor: 'divider', mb: 0 }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          aria-label="Exercise metrics tabs"
        >
          <Tab
            icon={<FontAwesomeIcon icon={faFire} />}
            iconPosition="start"
            label="Inject Summary"
            id="metrics-tab-0"
            aria-controls="metrics-tabpanel-0"
          />
          <Tab
            icon={<FontAwesomeIcon icon={faClipboardList} />}
            iconPosition="start"
            label="Observations"
            id="metrics-tab-1"
            aria-controls="metrics-tabpanel-1"
          />
          <Tab
            icon={<FontAwesomeIcon icon={faClock} />}
            iconPosition="start"
            label="Timeline"
            id="metrics-tab-2"
            aria-controls="metrics-tabpanel-2"
          />
          <Tab
            icon={<FontAwesomeIcon icon={faUserCog} />}
            iconPosition="start"
            label="Controllers"
            id="metrics-tab-3"
            aria-controls="metrics-tabpanel-3"
          />
          <Tab
            icon={<FontAwesomeIcon icon={faUserCheck} />}
            iconPosition="start"
            label="Evaluators"
            id="metrics-tab-4"
            aria-controls="metrics-tabpanel-4"
          />
          <Tab
            icon={<FontAwesomeIcon icon={faChartPie} />}
            iconPosition="start"
            label="Charts"
            id="metrics-tab-5"
            aria-controls="metrics-tabpanel-5"
          />
          <Tab
            icon={<FontAwesomeIcon icon={faShieldAlt} />}
            iconPosition="start"
            label="Capabilities"
            id="metrics-tab-6"
            aria-controls="metrics-tabpanel-6"
          />
        </Tabs>
      </Paper>

      {/* Tab Panels */}
      <TabPanel value={activeTab} index={0}>
        <InjectSummaryPanel exerciseId={exerciseId!} />
      </TabPanel>
      <TabPanel value={activeTab} index={1}>
        <ObservationSummaryPanel exerciseId={exerciseId!} />
      </TabPanel>
      <TabPanel value={activeTab} index={2}>
        <TimelineSummaryPanel exerciseId={exerciseId!} />
      </TabPanel>
      <TabPanel value={activeTab} index={3}>
        <ControllerActivityPanel exerciseId={exerciseId!} />
      </TabPanel>
      <TabPanel value={activeTab} index={4}>
        <EvaluatorCoveragePanel exerciseId={exerciseId!} />
      </TabPanel>
      <TabPanel value={activeTab} index={5}>
        <RatingChartsPanel exerciseId={exerciseId!} />
      </TabPanel>
      <TabPanel value={activeTab} index={6}>
        <CapabilityPerformancePanel exerciseId={exerciseId!} />
      </TabPanel>
    </Box>
  )
}

export default ExerciseMetricsPage
