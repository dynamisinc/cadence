import { useMemo } from 'react'
import { useNavigate, useParams, useLocation } from 'react-router-dom'
import {
  Box,
  Paper,
  Skeleton,
} from '@mui/material'
import { faHome } from '@fortawesome/free-solid-svg-icons'

import { InjectForm } from '../components/InjectForm'
import { useInjects } from '../hooks'
import { useExercise } from '../../exercises/hooks/useExercise'
import { usePhases } from '../../phases/hooks'
import { useBreadcrumbs } from '../../../core/contexts'
import CobraStyles from '../../../theme/CobraStyles'
import { useUnsavedChangesWarning } from '@/shared/hooks/useUnsavedChangesWarning'
import { PageHeader } from '@/shared/components'
import { TriggerType } from '../../../types'
import type { CreateInjectRequest, InjectDto, InjectFormValues } from '../types'

function buildDuplicateValues(source: InjectDto): Partial<InjectFormValues> {
  return {
    title: `${source.title} (Copy)`,
    description: source.description,
    target: source.target,
    source: source.source ?? '',
    deliveryMethodId: source.deliveryMethodId ?? '',
    deliveryMethodOther: source.deliveryMethodOther ?? '',
    injectType: source.injectType,
    expectedAction: source.expectedAction ?? '',
    controllerNotes: source.controllerNotes ?? '',
    phaseId: source.phaseId ?? '',
    objectiveIds: source.objectiveIds ?? [],
    sourceReference: source.sourceReference ?? '',
    priority: source.priority?.toString() ?? '',
    triggerType: source.triggerType ?? TriggerType.Manual,
    triggerCondition: source.triggerCondition ?? '',
    responsibleController: source.responsibleController ?? '',
    locationName: source.locationName ?? '',
    locationType: source.locationType ?? '',
    track: source.track ?? '',
  }
}

/**
 * Create Inject Page
 *
 * Form for creating a new inject in an exercise's MSEL.
 * Supports duplicate mode when navigated with `duplicateFrom` in router state.
 */
export const CreateInjectPage = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const { id: exerciseId } = useParams<{ id: string }>()
  const { exercise, loading: exerciseLoading } = useExercise(exerciseId || '')
  const { createInject, isCreating } = useInjects(exerciseId || '')
  const { phases } = usePhases(exerciseId || '')

  const duplicateFrom = (location.state as { duplicateFrom?: InjectDto } | null)?.duplicateFrom
  const isDuplicate = !!duplicateFrom

  const duplicateValues = useMemo(
    () => (duplicateFrom ? buildDuplicateValues(duplicateFrom) : undefined),
    [duplicateFrom],
  )

  // Set custom breadcrumbs with exercise name, MSEL, and New Inject
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'MSEL', path: `/exercises/${exerciseId}/msel` },
        { label: isDuplicate ? 'Duplicate Inject' : 'New Inject' },
      ]
      : undefined,
  )

  // TODO: Replace `false` with actual form dirty state (e.g. from InjectForm's isDirty)
  const { UnsavedChangesDialog } = useUnsavedChangesWarning(false)

  const handleBackClick = () => {
    navigate(`/exercises/${exerciseId}/msel`)
  }

  const handleSubmit = async (request: CreateInjectRequest) => {
    await createInject(request)
    navigate(`/exercises/${exerciseId}/msel`)
  }

  const handleSubmitAndContinue = async (request: CreateInjectRequest) => {
    await createInject(request)
    // Hook handles success toast; form component handles reset internally
  }

  const handleCancel = () => {
    navigate(`/exercises/${exerciseId}/msel`)
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title={isDuplicate ? 'New Inject (from duplicate)' : 'New Inject'}
        showBackButton
        onBackClick={handleBackClick}
        subtitle={exerciseLoading ? <Skeleton width={200} /> : exercise?.name || 'Exercise'}
      />

      {/* Form */}
      <Paper sx={{ p: 3 }}>
        <InjectForm
          exerciseId={exerciseId || ''}
          phases={phases}
          onSubmit={handleSubmit}
          onSubmitAndContinue={handleSubmitAndContinue}
          onCancel={handleCancel}
          isSubmitting={isCreating}
          initialValues={duplicateValues}
        />
      </Paper>

      <UnsavedChangesDialog />
    </Box>
  )
}

export default CreateInjectPage
