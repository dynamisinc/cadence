/**
 * MyAssignmentsPage Component
 *
 * Displays all exercise assignments for the current user,
 * grouped into Active, Upcoming, and Completed sections.
 */
import { useMemo } from 'react'
import { Box, Typography, Alert } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faClipboardList, faHome } from '@fortawesome/free-solid-svg-icons'
import { useBreadcrumbs } from '@/core/contexts'
import { useMyAssignments } from '../hooks/useMyAssignments'
import { AssignmentSection } from '../components/AssignmentSection'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { HelpTooltip, PageHeader } from '@/shared/components'
import CobraStyles from '@/theme/CobraStyles'

export function MyAssignmentsPage() {
  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'My Assignments' },
  ])

  const theme = useTheme()
  const { data, isLoading, isError, error, refetch } = useMyAssignments()

  // Show organization name on cards when assignments span multiple organizations
  const hasMultipleOrgs = useMemo(() => {
    if (!data) return false
    const allAssignments = [...data.active, ...data.upcoming, ...data.completed]
    const orgNames = new Set(allAssignments.map(a => a.organizationName))
    return orgNames.size > 1
  }, [data])

  // Loading state
  if (isLoading) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <PageHeader title="My Assignments" icon={faClipboardList} subtitle="Your exercise role assignments, grouped by status" chips={<HelpTooltip helpKey="assignments.overview" compact />} />
        <AssignmentSection
          title="Active Now"
          type="active"
          assignments={[]}
          isLoading={true}
        />
        <AssignmentSection
          title="Upcoming"
          type="upcoming"
          assignments={[]}
          isLoading={true}
        />
      </Box>
    )
  }

  // Error state
  if (isError) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <PageHeader title="My Assignments" icon={faClipboardList} subtitle="Your exercise role assignments, grouped by status" chips={<HelpTooltip helpKey="assignments.overview" compact />} />
        <Alert
          severity="error"
          action={
            <CobraPrimaryButton size="small" onClick={() => refetch()}>
              Retry
            </CobraPrimaryButton>
          }
        >
          Failed to load assignments: {error?.message || 'Unknown error'}
        </Alert>
      </Box>
    )
  }

  // Empty state - no assignments at all
  const hasNoAssignments =
    !data ||
    (data.active.length === 0 &&
      data.upcoming.length === 0 &&
      data.completed.length === 0)

  if (hasNoAssignments) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <PageHeader title="My Assignments" icon={faClipboardList} subtitle="Your exercise role assignments, grouped by status" chips={<HelpTooltip helpKey="assignments.overview" compact />} />
        <Box
          display="flex"
          flexDirection="column"
          alignItems="center"
          justifyContent="center"
          py={8}
          textAlign="center"
        >
          <FontAwesomeIcon
            icon={faClipboardList}
            style={{ fontSize: '4rem', color: theme.palette.neutral[300], marginBottom: '1rem' }}
          />
          <Typography variant="h5" color="text.secondary" gutterBottom>
            No Assignments Yet
          </Typography>
          <Typography variant="body1" color="text.secondary" maxWidth={400}>
            You have not been assigned to any exercises. When you are added as a
            Controller, Evaluator, or other role, your assignments will appear
            here.
          </Typography>
        </Box>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader title="My Assignments" icon={faClipboardList} subtitle="Your exercise role assignments, grouped by status" chips={<HelpTooltip helpKey="assignments.overview" compact />} />

      {/* Active Section - always show */}
      <AssignmentSection
        title="Active Now"
        type="active"
        assignments={data.active}
        emptyMessage="No exercises are currently in conduct."
        showOrganization={hasMultipleOrgs}
      />

      {/* Upcoming Section - always show */}
      <AssignmentSection
        title="Upcoming"
        type="upcoming"
        assignments={data.upcoming}
        emptyMessage="No upcoming exercises."
        showOrganization={hasMultipleOrgs}
      />

      {/* Completed Section - always show (collapsed by default) */}
      <AssignmentSection
        title="Recently Completed"
        type="completed"
        assignments={data.completed}
        emptyMessage="No completed exercises."
        showOrganization={hasMultipleOrgs}
      />
    </Box>
  )
}
