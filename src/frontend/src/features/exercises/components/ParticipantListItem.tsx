/**
 * ParticipantListItem - Displays a single exercise participant
 *
 * Shows participant details with inline role editing for Directors/Admins.
 * Displays both exercise role and system role for context.
 *
 * @module features/exercises/components
 */

import { FC } from 'react'
import {
  TableRow,
  TableCell,
  Typography,
  Select,
  MenuItem,
  IconButton,
  Box,
  FormControl,
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTrash } from '@fortawesome/free-solid-svg-icons'
import type { ExerciseParticipantDto } from '../types'

interface ParticipantListItemProps {
  participant: ExerciseParticipantDto
  canEdit: boolean
  onRoleChange: (userId: string, newRole: string) => void
  onRemove: (userId: string, displayName: string) => void
}

// Exercise roles available for assignment
const EXERCISE_ROLES = [
  { value: 'Observer', label: 'Observer' },
  { value: 'Evaluator', label: 'Evaluator' },
  { value: 'Controller', label: 'Controller' },
  { value: 'ExerciseDirector', label: 'Exercise Director' },
]

export const ParticipantListItem: FC<ParticipantListItemProps> = ({
  participant,
  canEdit,
  onRoleChange,
  onRemove,
}) => {
  const handleRoleChange = (event: SelectChangeEvent) => {
    onRoleChange(participant.userId, event.target.value)
  }

  const handleRemove = () => {
    onRemove(participant.userId, participant.displayName)
  }

  return (
    <TableRow>
      {/* Name */}
      <TableCell>
        <Typography variant="body1">{participant.displayName}</Typography>
      </TableCell>

      {/* Email */}
      <TableCell>
        <Typography variant="body2" color="text.secondary">
          {participant.email}
        </Typography>
      </TableCell>

      {/* System Role */}
      <TableCell>
        <Typography variant="body2" color="text.secondary">
          System: {participant.systemRole}
        </Typography>
      </TableCell>

      {/* Exercise Role */}
      <TableCell>
        {canEdit ? (
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <Select
              value={participant.exerciseRole}
              onChange={handleRoleChange}
              inputProps={{
                'aria-label': 'Exercise role',
              }}
            >
              {EXERCISE_ROLES.map(role => (
                <MenuItem key={role.value} value={role.value}>
                  {role.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        ) : (
          <Typography variant="body1">{participant.exerciseRole}</Typography>
        )}
      </TableCell>

      {/* Actions */}
      <TableCell align="right">
        {canEdit && (
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
            <IconButton
              onClick={handleRemove}
              aria-label={`Remove ${participant.displayName}`}
              color="error"
              size="small"
            >
              <FontAwesomeIcon icon={faTrash} />
            </IconButton>
          </Box>
        )}
      </TableCell>
    </TableRow>
  )
}
