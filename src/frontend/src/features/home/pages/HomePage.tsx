import { useNavigate } from 'react-router-dom'
import { Box, Typography, Stack, Paper, Divider } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faList } from '@fortawesome/free-solid-svg-icons'

import { useExercises } from '../../exercises'
import { ExerciseList } from '../components'
import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { usePermissions } from '../../../shared/hooks'
import { PermissionRole } from '../../../types'

/**
 * Get a user-friendly role display name
 */
const getRoleDisplayName = (role: PermissionRole): string => {
  switch (role) {
    case PermissionRole.MANAGE:
      return 'Exercise Director'
    case PermissionRole.CONTRIBUTOR:
      return 'Controller'
    case PermissionRole.READONLY:
      return 'Observer'
    default:
      return 'User'
  }
}

/**
 * Get role-specific welcome message
 */
const getRoleWelcomeMessage = (role: PermissionRole): string => {
  switch (role) {
    case PermissionRole.MANAGE:
      return 'You have full access to create and manage exercises.'
    case PermissionRole.CONTRIBUTOR:
      return 'You can view exercises and fire injects during conduct.'
    case PermissionRole.READONLY:
      return 'You have read-only access to view exercises.'
    default:
      return 'Welcome to the MSEL management platform.'
  }
}

/**
 * Home Page Component
 *
 * Landing page that provides:
 * - Role-aware welcome message
 * - Quick actions based on permissions
 * - Exercise list as primary content
 *
 * This page serves as the dashboard entry point and will evolve
 * to include metrics, recent activity, and role-specific widgets.
 */
export const HomePage = () => {
  const navigate = useNavigate()
  const { exercises, loading, error } = useExercises()
  const { role, canManage } = usePermissions()

  const roleDisplayName = getRoleDisplayName(role)
  const welcomeMessage = getRoleWelcomeMessage(role)

  const handleCreateExercise = () => {
    navigate('/exercises/new')
  }

  const handleViewAllExercises = () => {
    navigate('/exercises')
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Welcome Section */}
      <Paper
        sx={{
          p: 3,
          mb: 3,
          background: 'linear-gradient(135deg, #f5f7fa 0%, #e4e8ec 100%)',
        }}
      >
        <Stack spacing={1}>
          <Typography variant="h4" component="h1">
            Welcome to Cadence
          </Typography>
          <Typography variant="body1" color="text.secondary">
            HSEEP-Compliant MSEL Management Platform
          </Typography>
          <Divider sx={{ my: 1 }} />
          <Stack direction="row" spacing={1} alignItems="center">
            <Typography variant="body2" fontWeight={500}>
              Your Role:
            </Typography>
            <Typography variant="body2" color="primary.main" fontWeight={600}>
              {roleDisplayName}
            </Typography>
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {welcomeMessage}
          </Typography>
        </Stack>
      </Paper>

      {/* Quick Actions */}
      <Stack direction="row" spacing={2} marginBottom={3}>
        {canManage && (
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faPlus} />}
            onClick={handleCreateExercise}
          >
            Create Exercise
          </CobraPrimaryButton>
        )}
        <CobraSecondaryButton
          startIcon={<FontAwesomeIcon icon={faList} />}
          onClick={handleViewAllExercises}
        >
          View All Exercises
        </CobraSecondaryButton>
      </Stack>

      {/* Exercise List Section */}
      <Box>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          marginBottom={2}
        >
          <Typography variant="h5" component="h2">
            Your Exercises
          </Typography>
        </Stack>

        <ExerciseList
          exercises={exercises}
          loading={loading}
          error={error}
          canManage={canManage}
          onCreateClick={handleCreateExercise}
          maxItems={5}
        />

        {exercises.length > 5 && (
          <Box sx={{ mt: 2, textAlign: 'center' }}>
            <CobraSecondaryButton onClick={handleViewAllExercises}>
              View All {exercises.length} Exercises
            </CobraSecondaryButton>
          </Box>
        )}
      </Box>
    </Box>
  )
}

export default HomePage
