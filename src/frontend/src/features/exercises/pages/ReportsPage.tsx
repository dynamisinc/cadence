/**
 * ReportsPage
 *
 * Exercise reports and data export page.
 * Provides options to export MSEL, Observations, and full exercise packages.
 */

import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Stack,
  CircularProgress,
  Alert,
  Card,
  CardContent,
  CardActions,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faHome,
  faFileExcel,
  faFileArchive,
  faClipboardList,
  faArrowLeft,
} from '@fortawesome/free-solid-svg-icons'
import CobraStyles from '@/theme/CobraStyles'
import { CobraPrimaryButton, CobraLinkButton } from '@/theme/styledComponents'
import { useBreadcrumbs } from '@/core/contexts'
import { PageHeader } from '@/shared/components'
import { useExercise, useMselSummary } from '../hooks'
import {
  useExportMsel,
  useExportObservations,
  useExportFullPackage,
} from '@/features/excel-export'
import { useObservations } from '@/features/observations'

export const ReportsPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { exercise, loading: exerciseLoading, error: exerciseError } = useExercise(exerciseId)
  const { data: mselSummary } = useMselSummary(exerciseId ?? '')
  const { observations } = useObservations(exerciseId ?? '')
  const [error, setError] = useState<string | null>(null)

  const exportMsel = useExportMsel()
  const exportObservations = useExportObservations()
  const exportFullPackage = useExportFullPackage()

  // Set breadcrumbs with exercise name
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'Reports' },
      ]
      : undefined,
  )

  const handleExportMsel = async () => {
    if (!exerciseId) return
    setError(null)
    try {
      await exportMsel.mutateAsync({
        exerciseId,
        format: 'xlsx',
        includeFormatting: true,
        includeConductData: true,
        includePhases: true,
        includeObjectives: true,
      })
    } catch {
      setError('Failed to export MSEL. Please try again.')
    }
  }

  const handleExportObservations = async () => {
    if (!exerciseId) return
    setError(null)
    try {
      await exportObservations.mutateAsync({
        exerciseId,
        includeFormatting: true,
      })
    } catch {
      setError('Failed to export observations. Please try again.')
    }
  }

  const handleExportFullPackage = async () => {
    if (!exerciseId) return
    setError(null)
    try {
      await exportFullPackage.mutateAsync({
        exerciseId,
        includeFormatting: true,
      })
    } catch {
      setError('Failed to export full package. Please try again.')
    }
  }

  // Loading state
  if (exerciseLoading && !exercise) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="200px"
      >
        <CircularProgress />
      </Box>
    )
  }

  // Error state
  if (exerciseError && !exercise) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography color="error" gutterBottom>
          {exerciseError}
        </Typography>
        <CobraPrimaryButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraPrimaryButton>
      </Box>
    )
  }

  // Not found
  if (!exercise) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography variant="h6" gutterBottom>
          Exercise not found
        </Typography>
        <CobraPrimaryButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraPrimaryButton>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="Reports & Export"
        subtitle="Export exercise data for analysis and documentation"
        actions={
          <CobraLinkButton
            startIcon={<FontAwesomeIcon icon={faArrowLeft} />}
            onClick={() => navigate(`/exercises/${exerciseId}`)}
          >
            Back to Exercise
          </CobraLinkButton>
        }
      />

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Export Cards */}
      <Stack spacing={3}>
        {/* MSEL Export */}
        <Card>
          <CardContent>
            <Stack direction="row" spacing={2} alignItems="flex-start">
              <Box
                sx={{
                  p: 1.5,
                  borderRadius: 2,
                  backgroundColor: 'success.light',
                  color: 'success.contrastText',
                }}
              >
                <FontAwesomeIcon icon={faFileExcel} size="lg" />
              </Box>
              <Box sx={{ flex: 1 }}>
                <Typography variant="h6" gutterBottom>
                  Export MSEL
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  Download the Master Scenario Events List as an Excel file. Includes all injects,
                  phases, objectives, and conduct data (status, fire times).
                </Typography>
                {mselSummary && (
                  <Typography variant="caption" color="text.secondary">
                    {mselSummary.totalInjects} injects ({mselSummary.releasedCount} released,{' '}
                    {mselSummary.draftCount} draft, {mselSummary.deferredCount} deferred)
                  </Typography>
                )}
              </Box>
            </Stack>
          </CardContent>
          <CardActions sx={{ px: 2, pb: 2 }}>
            <CobraPrimaryButton
              onClick={handleExportMsel}
              disabled={exportMsel.isPending}
              startIcon={
                exportMsel.isPending ? (
                  <CircularProgress size={16} color="inherit" />
                ) : (
                  <FontAwesomeIcon icon={faFileExcel} />
                )
              }
            >
              {exportMsel.isPending ? 'Exporting...' : 'Export MSEL'}
            </CobraPrimaryButton>
          </CardActions>
        </Card>

        {/* Observations Export */}
        <Card>
          <CardContent>
            <Stack direction="row" spacing={2} alignItems="flex-start">
              <Box
                sx={{
                  p: 1.5,
                  borderRadius: 2,
                  backgroundColor: 'info.light',
                  color: 'info.contrastText',
                }}
              >
                <FontAwesomeIcon icon={faClipboardList} size="lg" />
              </Box>
              <Box sx={{ flex: 1 }}>
                <Typography variant="h6" gutterBottom>
                  Export Observations
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  Download all evaluator observations as an Excel file. Includes timestamps,
                  observer names, ratings, recommendations, and related injects.
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {observations.length === 0
                    ? 'No observations recorded yet. Add observations from the Exercise Hub to export them.'
                    : `${observations.length} observation${observations.length === 1 ? '' : 's'} available for export`}
                </Typography>
              </Box>
            </Stack>
          </CardContent>
          <CardActions sx={{ px: 2, pb: 2 }}>
            <CobraPrimaryButton
              onClick={handleExportObservations}
              disabled={exportObservations.isPending || observations.length === 0}
              startIcon={
                exportObservations.isPending ? (
                  <CircularProgress size={16} color="inherit" />
                ) : (
                  <FontAwesomeIcon icon={faClipboardList} />
                )
              }
            >
              {exportObservations.isPending ? 'Exporting...' : 'Export Observations'}
            </CobraPrimaryButton>
          </CardActions>
        </Card>

        {/* Full Package Export */}
        <Card>
          <CardContent>
            <Stack direction="row" spacing={2} alignItems="flex-start">
              <Box
                sx={{
                  p: 1.5,
                  borderRadius: 2,
                  backgroundColor: 'secondary.light',
                  color: 'secondary.contrastText',
                }}
              >
                <FontAwesomeIcon icon={faFileArchive} size="lg" />
              </Box>
              <Box sx={{ flex: 1 }}>
                <Typography variant="h6" gutterBottom>
                  Export Full Package
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                  Download a complete exercise package as a ZIP file containing:
                </Typography>
                <Typography variant="body2" color="text.secondary" component="ul" sx={{ m: 0, pl: 2 }}>
                  <li>MSEL.xlsx - Full inject list with conduct data</li>
                  <li>Observations.xlsx - All evaluator observations</li>
                  <li>Summary.json - Exercise metadata and statistics</li>
                </Typography>
              </Box>
            </Stack>
          </CardContent>
          <CardActions sx={{ px: 2, pb: 2 }}>
            <CobraPrimaryButton
              onClick={handleExportFullPackage}
              disabled={exportFullPackage.isPending}
              startIcon={
                exportFullPackage.isPending ? (
                  <CircularProgress size={16} color="inherit" />
                ) : (
                  <FontAwesomeIcon icon={faFileArchive} />
                )
              }
            >
              {exportFullPackage.isPending ? 'Exporting...' : 'Export Full Package'}
            </CobraPrimaryButton>
          </CardActions>
        </Card>
      </Stack>
    </Box>
  )
}

export default ReportsPage
