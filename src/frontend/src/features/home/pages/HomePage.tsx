import { useNavigate } from 'react-router-dom'
import { Box, Typography, Stack } from '@mui/material'

import { useExercises, ExerciseTable } from '../../exercises'
import { CobraSecondaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useSystemPermissions } from '../../../shared/hooks'
import { useOrganization } from '../../../contexts/OrganizationContext'
import { RoleOrientationPanel } from '../components/RoleOrientationPanel'
import { ExerciseRolePrimerSection } from '../components/ExerciseRolePrimerSection'

/**
 * Home Page Component
 *
 * Landing page that provides:
 * - Role-based orientation panel with org-specific quick actions
 * - HSEEP exercise role primer for new users
 * - Exercise list as primary content (using shared ExerciseTable)
 */
export const HomePage = () => {
  const navigate = useNavigate()
  const { exercises, loading, error } = useExercises()
  const { canCreateExercise } = useSystemPermissions()
  const { currentOrg } = useOrganization()

  const handleCreateExercise = () => {
    navigate('/exercises/new')
  }

  const handleViewAllExercises = () => {
    navigate('/exercises')
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Role-Based Orientation */}
      <RoleOrientationPanel
        orgRole={currentOrg?.role}
        orgName={currentOrg?.name}
      />

      {/* HSEEP Exercise Role Primer */}
      <ExerciseRolePrimerSection />

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

        <ExerciseTable
          exercises={exercises}
          loading={loading}
          error={error}
          canManage={canCreateExercise}
          onCreateClick={handleCreateExercise}
          maxItems={5}
          size="small"
          hideArchived
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
