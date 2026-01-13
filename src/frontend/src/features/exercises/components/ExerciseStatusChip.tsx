import { Chip } from '@mui/material'
import { getExerciseStatusChipColor } from '../../../theme/cobraTheme'
import type { ExerciseStatus } from '../../../types'

interface ExerciseStatusChipProps {
  status: ExerciseStatus
}

/**
 * Status chip for exercises with COBRA-compliant colors
 *
 * Per S03-view-exercise-list.md:
 * - Draft: Neutral styling
 * - Active: Success/green styling
 * - Completed: Muted styling
 * - Archived: Gray if shown
 */
export const ExerciseStatusChip = ({ status }: ExerciseStatusChipProps) => {
  const colors = getExerciseStatusChipColor(status)

  return (
    <Chip
      label={status}
      size="small"
      sx={{
        backgroundColor: colors.bg,
        color: colors.text,
        fontWeight: 500,
        minWidth: 80,
      }}
    />
  )
}

export default ExerciseStatusChip
