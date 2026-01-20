/**
 * UpNextList
 *
 * Displays the next 2-3 upcoming injects in facilitator-paced conduct view.
 * Each inject has a "Jump" button to skip ahead if needed.
 *
 * @module features/injects
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import {
  Paper,
  Box,
  Typography,
  Stack,
  Card,
  CardContent,
  CardActions,
  Chip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faForward, faArrowRight } from '@fortawesome/free-solid-svg-icons'
import { CobraSecondaryButton } from '@/theme/styledComponents'
import type { InjectDto } from '../../types'
import { formatScenarioTime } from '../../types'

/**
 * Truncate text to specified length with ellipsis
 */
const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text
  return text.substring(0, maxLength).trim() + '...'
}

interface UpNextListProps {
  /** Upcoming injects to display */
  injects: InjectDto[]
  /** Called when Jump button clicked */
  onJumpTo: (inject: InjectDto) => void
  /** Whether the current user can control injects */
  canControl?: boolean
  /** Whether actions are currently being submitted */
  isSubmitting?: boolean
}

export const UpNextList = ({
  injects,
  onJumpTo,
  canControl = true,
  isSubmitting = false,
}: UpNextListProps) => {
  return (
    <Paper variant="outlined">
      {/* Section Header */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
          p: 2,
          backgroundColor: 'grey.50',
        }}
      >
        <Box sx={{ color: 'info.main' }}>
          <FontAwesomeIcon icon={faForward} />
        </Box>

        <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 600 }}>
          UP NEXT
        </Typography>
      </Box>

      {/* Section Content */}
      <Box sx={{ p: 2 }}>
        {injects.length === 0 ? (
          <Typography variant="body2" color="text.secondary" align="center" sx={{ py: 2 }}>
            No upcoming injects.
          </Typography>
        ) : (
          <Stack spacing={2}>
            {injects.map(inject => {
              const scenarioTimeDisplay = formatScenarioTime(
                inject.scenarioDay, inject.scenarioTime,
              )

              return (
                <Card key={inject.id} variant="outlined">
                  <CardContent sx={{ pb: 1 }}>
                    <Stack spacing={1}>
                      {/* Inject Number and Title */}
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Chip
                          label={`#${inject.injectNumber}`}
                          size="small"
                          variant="outlined"
                          color="primary"
                        />
                        <Typography variant="body1" fontWeight={500}>
                          {inject.title}
                        </Typography>
                        {scenarioTimeDisplay && (
                          <Chip
                            label={scenarioTimeDisplay}
                            size="small"
                            variant="outlined"
                          />
                        )}
                      </Box>

                      {/* Description Preview */}
                      <Typography
                        variant="body2"
                        color="text.secondary"
                        sx={{ fontStyle: 'italic' }}
                      >
                        "{truncateText(inject.description, 100)}"
                      </Typography>
                    </Stack>
                  </CardContent>

                  {canControl && (
                    <CardActions sx={{ justifyContent: 'flex-end', pt: 0 }}>
                      <CobraSecondaryButton
                        size="small"
                        onClick={() => onJumpTo(inject)}
                        disabled={isSubmitting}
                        endIcon={<FontAwesomeIcon icon={faArrowRight} />}
                      >
                        Jump
                      </CobraSecondaryButton>
                    </CardActions>
                  )}
                </Card>
              )
            })}
          </Stack>
        )}
      </Box>
    </Paper>
  )
}

export default UpNextList
