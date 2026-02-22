/**
 * MyAssignmentsPage Component
 *
 * Displays all exercise assignments for the current user,
 * grouped into Active, Upcoming, and Completed sections.
 */
import { Box, Typography, Alert } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faClipboardList, faHome } from '@fortawesome/free-solid-svg-icons'
import { useBreadcrumbs } from '@/core/contexts'
import { useMyAssignments } from '../hooks/useMyAssignments'
import { AssignmentSection } from '../components/AssignmentSection'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { PageHeader } from '@/shared/components'
import CobraStyles from '@/theme/CobraStyles'

export function MyAssignmentsPage() {
  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'My Assignments' },
  ])

  const { data, isLoading, isError, error, refetch } = useMyAssignments()

  // Loading state
  if (isLoading) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <PageHeader title="My Assignments" icon={faClipboardList} />
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
        <PageHeader title="My Assignments" icon={faClipboardList} />
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
        <PageHeader title="My Assignments" icon={faClipboardList} />
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
            style={{ fontSize: '4rem', color: '#ccc', marginBottom: '1rem' }}
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
      {/* Page Header */}
      <Box display="flex" alignItems="center" gap={2} marginBottom={3}>
        <FontAwesomeIcon icon={faClipboardList} size="lg" />
        <Typography variant="h5" component="h1">My Assignments</Typography>
      </Box>

      {/* Active Section - always show */}
      <AssignmentSection
        title="Active Now"
        type="active"
        assignments={data.active}
        emptyMessage="No exercises are currently in conduct."
      />

      {/* Upcoming Section - always show */}
      <AssignmentSection
        title="Upcoming"
        type="upcoming"
        assignments={data.upcoming}
        emptyMessage="No upcoming exercises."
      />

      {/* Completed Section - always show (collapsed by default) */}
      <AssignmentSection
        title="Recently Completed"
        type="completed"
        assignments={data.completed}
        emptyMessage="No completed exercises."
      />
    </Box>
  )
}
