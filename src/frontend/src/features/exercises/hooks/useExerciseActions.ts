/**
 * useExerciseActions
 *
 * Encapsulates duplicate, archive, and delete lifecycle actions for the
 * ExerciseDetailPage. Manages the three action dialog open/close states,
 * the "More" menu anchor state, and derived permission flags (canDelete,
 * canArchive). Also owns the navigation callbacks triggered after a
 * successful archive or delete.
 *
 * @module features/exercises
 */

import { useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useDuplicateExercise, useExerciseStatus } from '../hooks'
import { ExerciseStatus } from '../../../types'
import type { ExerciseDto } from '../types'

interface UseExerciseActionsParams {
  /** The exercise ID */
  exerciseId: string
  /** Current exercise data (used for canDelete / canArchive checks) */
  exercise: ExerciseDto | null
  /** Whether the current user has the edit_exercise permission */
  canEditExercise: boolean
  /** Whether the current user has the delete_exercise permission */
  canDeleteExercise: boolean
}

export interface UseExerciseActionsReturn {
  // Dialog open state
  duplicateDialogOpen: boolean
  archiveDialogOpen: boolean
  deleteDialogOpen: boolean
  settingsDialogOpen: boolean

  // Dialog open/close handlers
  openDuplicateDialog: () => void
  closeDuplicateDialog: () => void
  openArchiveDialog: () => void
  closeArchiveDialog: () => void
  openDeleteDialog: () => void
  closeDeleteDialog: () => void
  openSettingsDialog: () => void
  closeSettingsDialog: () => void

  // More menu state
  moreMenuAnchor: HTMLElement | null
  handleMoreMenuOpen: (event: React.MouseEvent<HTMLElement>) => void
  handleMoreMenuClose: () => void

  // Derived permission flags
  canDelete: boolean
  canArchive: boolean

  // Mutation functions (forwarded from hooks)
  duplicate: ReturnType<typeof useDuplicateExercise>['duplicate']
  isDuplicating: boolean
  archive: ReturnType<typeof useExerciseStatus>['archive']
  isArchiving: boolean

  // Navigation callbacks
  handleArchived: () => void
  handleDeleted: () => void
}

/**
 * Manages lifecycle action dialogs and mutations for the exercise detail page.
 *
 * Duplicate flow: open dialog → submit → navigate to new exercise (handled by hook)
 * Archive flow: open dialog → confirm → archive() → handleArchived() → navigate to list
 * Delete flow: open dialog → confirm (two-step) → handleDeleted() → navigate to list
 */
export const useExerciseActions = ({
  exerciseId,
  exercise,
  canEditExercise,
  canDeleteExercise,
}: UseExerciseActionsParams): UseExerciseActionsReturn => {
  const navigate = useNavigate()

  // Dialog open states
  const [duplicateDialogOpen, setDuplicateDialogOpen] = useState(false)
  const [archiveDialogOpen, setArchiveDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [settingsDialogOpen, setSettingsDialogOpen] = useState(false)

  // More menu anchor
  const [moreMenuAnchor, setMoreMenuAnchor] = useState<null | HTMLElement>(null)

  // Duplicate exercise mutation
  const { duplicate, isDuplicating } = useDuplicateExercise()

  // Archive action (from exercise status hook)
  const { archive, isArchiving } = useExerciseStatus(exerciseId)

  // Derived permission flags
  const canDelete = useMemo(() => {
    if (!exercise || !canDeleteExercise) return false
    return !exercise.hasBeenPublished || exercise.status === ExerciseStatus.Archived
  }, [exercise, canDeleteExercise])

  const canArchive = useMemo(() => {
    if (!exercise || !canEditExercise) return false
    return exercise.status !== ExerciseStatus.Archived
  }, [exercise, canEditExercise])

  // Dialog handlers
  const openDuplicateDialog = () => setDuplicateDialogOpen(true)
  const closeDuplicateDialog = () => setDuplicateDialogOpen(false)
  const openArchiveDialog = () => setArchiveDialogOpen(true)
  const closeArchiveDialog = () => setArchiveDialogOpen(false)
  const openDeleteDialog = () => setDeleteDialogOpen(true)
  const closeDeleteDialog = () => setDeleteDialogOpen(false)
  const openSettingsDialog = () => setSettingsDialogOpen(true)
  const closeSettingsDialog = () => setSettingsDialogOpen(false)

  // More menu handlers
  const handleMoreMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setMoreMenuAnchor(event.currentTarget)
  }

  const handleMoreMenuClose = () => {
    setMoreMenuAnchor(null)
  }

  // Post-action navigation
  const handleArchived = () => {
    navigate('/exercises')
  }

  const handleDeleted = () => {
    navigate('/exercises')
  }

  return {
    duplicateDialogOpen,
    archiveDialogOpen,
    deleteDialogOpen,
    settingsDialogOpen,

    openDuplicateDialog,
    closeDuplicateDialog,
    openArchiveDialog,
    closeArchiveDialog,
    openDeleteDialog,
    closeDeleteDialog,
    openSettingsDialog,
    closeSettingsDialog,

    moreMenuAnchor,
    handleMoreMenuOpen,
    handleMoreMenuClose,

    canDelete,
    canArchive,

    duplicate,
    isDuplicating,
    archive,
    isArchiving,

    handleArchived,
    handleDeleted,
  }
}
