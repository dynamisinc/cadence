import { Chip, Tooltip } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import {
  getExerciseTypeLabel,
  getExerciseTypeFullName,
} from '../../../theme/cobraTheme'
import type { ExerciseType } from '../../../types'

interface ExerciseTypeChipProps {
  type: ExerciseType
}

/**
 * Type chip for exercises showing abbreviation with tooltip for full name
 */
export const ExerciseTypeChip = ({ type }: ExerciseTypeChipProps) => {
  const theme = useTheme()

  return (
    <Tooltip title={getExerciseTypeFullName(type)} arrow>
      <Chip
        label={getExerciseTypeLabel(type)}
        size="small"
        variant="outlined"
        sx={{
          borderColor: theme.palette.divider,
          color: theme.palette.text.primary,
          fontWeight: 500,
          minWidth: 60,
        }}
      />
    </Tooltip>
  )
}

export default ExerciseTypeChip
