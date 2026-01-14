/**
 * ExerciseHeader Component
 *
 * Reusable header for exercise pages displaying:
 * - Exercise name
 * - Exercise type chip
 * - Exercise status chip
 * - Practice mode indicator (optional)
 * - Custom action buttons (provided by parent)
 */

import { Box, Stack, Typography, Tooltip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faScrewdriverWrench } from '@fortawesome/free-solid-svg-icons'

import { ExerciseStatusChip } from './ExerciseStatusChip'
import { ExerciseTypeChip } from './ExerciseTypeChip'
import type { ExerciseDto } from '../types'

interface ExerciseHeaderProps {
  /** The exercise to display */
  exercise: ExerciseDto
  /** Action buttons to display on the right side */
  actions?: React.ReactNode
  /** Bottom margin (default: 2) */
  marginBottom?: number
}

export const ExerciseHeader = ({
  exercise,
  actions,
  marginBottom = 2,
}: ExerciseHeaderProps) => {
  return (
    <Stack
      direction="row"
      justifyContent="space-between"
      alignItems="center"
      sx={{ flexShrink: 0 }}
      marginBottom={marginBottom}
    >
      <Stack direction="row" spacing={2} alignItems="center">
        <Typography variant="h5" component="h1">
          {exercise.name}
        </Typography>
        {exercise.isPracticeMode && (
          <Tooltip title="Practice Mode - excluded from production reports">
            <Box component="span" sx={{ color: 'action.active' }}>
              <FontAwesomeIcon icon={faScrewdriverWrench} />
            </Box>
          </Tooltip>
        )}
        <ExerciseTypeChip type={exercise.exerciseType} />
        <ExerciseStatusChip status={exercise.status} />
      </Stack>

      {actions && (
        <Stack direction="row" spacing={1} alignItems="center">
          {actions}
        </Stack>
      )}
    </Stack>
  )
}

export default ExerciseHeader
