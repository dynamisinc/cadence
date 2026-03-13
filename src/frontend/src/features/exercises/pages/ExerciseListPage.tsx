import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, Typography, Stack, Paper } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faBan, faFolderOpen } from '@fortawesome/free-solid-svg-icons'

import { useExercises } from '../hooks'
import { ExerciseTable } from '../components'
import {
  CobraPrimaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useSystemPermissions } from '../../../shared/hooks'
import { ExerciseStatus } from '../../../types'
import { ImportWizard } from '../../excel-import/components'
import { PageHeader } from '@/shared/components'

/**
 * Exercise List Page (S03)
 *
 * Displays all exercises the user has access to with:
 * - Sortable columns (Name, Type, Status, Date, Injects)
 * - Search/filter by name
 * - Status and type chips with COBRA colors
 * - Practice mode indicator
 * - Inject count column
 * - Create button for Administrators/Exercise Directors
 *
 * Uses the shared ExerciseTable component for consistent rendering
 * across the application (also used by HomePage).
 */
export const ExerciseListPage = () => {
  const navigate = useNavigate()
  const { exercises, loading, isFetching, error } = useExercises()
  const { canCreateExercise } = useSystemPermissions()

  // Use canCreateExercise for system-level permissions (create, delete exercises)
  const canManage = canCreateExercise

  const [searchTerm, setSearchTerm] = useState('')
  const [showArchived, setShowArchived] = useState(false)
  const [importExerciseId, setImportExerciseId] = useState<string | null>(null)

  // Filter exercises by search term (sorting handled by ExerciseTable)
  const filteredExercises = useMemo(() => {
    let filtered = exercises

    // Filter out archived unless showing archived
    if (!showArchived) {
      filtered = filtered.filter(e => e.status !== ExerciseStatus.Archived)
    }

    // Filter by search term
    if (searchTerm) {
      const search = searchTerm.toLowerCase()
      filtered = filtered.filter(e => e.name.toLowerCase().includes(search))
    }

    return filtered
  }, [exercises, searchTerm, showArchived])

  const handleCreateClick = () => {
    navigate('/exercises/new')
  }

  const handleImportClick = (exerciseId: string, e: React.MouseEvent) => {
    e.stopPropagation() // Prevent row click navigation
    setImportExerciseId(exerciseId)
  }

  const handleImportWizardClose = () => {
    setImportExerciseId(null)
  }

  // Check if we have search results but no matches
  const hasSearchButNoMatches =
    searchTerm && filteredExercises.length === 0 && exercises.length > 0

  // Error state
  if (error && exercises.length === 0) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography color="error" gutterBottom>
          {error}
        </Typography>
        <CobraPrimaryButton onClick={() => window.location.reload()}>
          Retry
        </CobraPrimaryButton>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="Exercises"
        icon={faFolderOpen}
        subtitle="View, search, and manage your organization's exercises"
        actions={canManage ? (
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faPlus} />}
            onClick={handleCreateClick}
          >
            Create Exercise
          </CobraPrimaryButton>
        ) : undefined}
      />

      {/* Search and Filters */}
      <Stack
        direction="row"
        spacing={2}
        marginBottom={2}
        alignItems="center"
        flexWrap="wrap"
      >
        <CobraTextField
          placeholder="Search exercises..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          sx={{ width: { xs: '100%', sm: 300 } }}
        />
        <Box
          component="label"
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1,
            cursor: 'pointer',
          }}
        >
          <input
            type="checkbox"
            checked={showArchived}
            onChange={e => setShowArchived(e.target.checked)}
          />
          <Typography variant="body2">Show Archived</Typography>
        </Box>
      </Stack>

      {/* Search "no results" state */}
      {hasSearchButNoMatches ? (
        <Paper
          sx={{
            py: 6,
            px: 4,
            textAlign: 'center',
            backgroundColor: 'grey.50',
            border: '1px dashed',
            borderColor: 'grey.300',
          }}
        >
          <Box
            sx={{
              width: 80,
              height: 80,
              borderRadius: '50%',
              backgroundColor: 'grey.200',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 16px',
              color: 'grey.500',
              fontSize: 40,
            }}
          >
            <FontAwesomeIcon icon={faBan} />
          </Box>
          <Typography variant="h6" gutterBottom>
            No matching exercises
          </Typography>
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{ maxWidth: 300, mx: 'auto' }}
          >
            Try adjusting your search terms or clear filters to see all
            exercises.
          </Typography>
        </Paper>
      ) : (
        <ExerciseTable
          exercises={filteredExercises}
          loading={loading || isFetching}
          error={error}
          canManage={canManage}
          onCreateClick={handleCreateClick}
          sortable
          showImportButton
          onImportClick={handleImportClick}
          size="medium"
          hideArchived={false} // We handle archived filtering above
        />
      )}

      {/* Import Wizard */}
      {importExerciseId && (
        <ImportWizard
          open={!!importExerciseId}
          onClose={handleImportWizardClose}
          exerciseId={importExerciseId}
        />
      )}
    </Box>
  )
}

export default ExerciseListPage
