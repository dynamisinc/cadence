/**
 * TimingConfigurationSection - Exercise timing configuration fields
 *
 * Provides UI for configuring Delivery Mode (Clock-driven / Facilitator-paced)
 * and Timeline Mode (Real-time / Compressed / Story-only) per CLK-03.
 *
 * Smart defaults are applied based on exercise type:
 * - TTX → Facilitator-paced
 * - FSE, FE, CAX → Clock-driven
 *
 * @module features/exercises
 * @see exercise-config/S03-timing-configuration-ui
 */

import { type FC, useMemo } from 'react'
import {
  Box,
  FormControl,
  FormControlLabel,
  FormHelperText,
  FormLabel,
  Radio,
  RadioGroup,
  Stack,
  Typography,
  Paper,
  Tooltip,
  IconButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faLock, faCircleQuestion } from '@fortawesome/free-solid-svg-icons'
import { CobraTextField } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { DeliveryMode, TimelineMode, ExerciseType } from '../../../types'

interface TimingConfigurationSectionProps {
  /** Current delivery mode */
  deliveryMode: DeliveryMode
  /** Current timeline mode */
  timelineMode: TimelineMode
  /** Current time scale (for Compressed mode) */
  timeScale: number | null
  /** Exercise type (kept for API consistency, not directly used in component) */
  exerciseType?: ExerciseType
  /** Whether fields are locked (exercise is Active) */
  isLocked: boolean
  /** Callback for field changes */
  onChange: (field: string, value: DeliveryMode | TimelineMode | number | null) => void
  /** Validation errors */
  errors?: {
    deliveryMode?: string
    timelineMode?: string
    timeScale?: string
  }
}

/**
 * Helper text calculator for time scale
 */
const getTimeScaleHelperText = (timeScale: number | null, error?: string): string => {
  if (error) return error
  if (timeScale && timeScale > 0) {
    return `1 real minute = ${timeScale} story ${timeScale === 1 ? 'minute' : 'minutes'}`
  }
  return '1 real minute = X story minutes'
}

/**
 * TimingConfigurationSection component
 */
export const TimingConfigurationSection: FC<TimingConfigurationSectionProps> = ({
  deliveryMode,
  timelineMode,
  timeScale,
  exerciseType: _exerciseType,
  isLocked,
  onChange,
  errors = {},
}) => {
  // Timeline options only shown for Clock-driven mode
  const showTimelineOptions = deliveryMode === DeliveryMode.ClockDriven

  // Calculate if time scale input should be shown
  const showTimeScale = useMemo(
    () => showTimelineOptions && timelineMode === TimelineMode.Compressed,
    [showTimelineOptions, timelineMode],
  )

  // Calculate helper text for time scale
  const timeScaleHelperText = useMemo(
    () => getTimeScaleHelperText(timeScale, errors.timeScale),
    [timeScale, errors.timeScale],
  )

  // Locked state UI
  if (isLocked) {
    return (
      <Paper
        variant="outlined"
        sx={{
          p: 2,
          backgroundColor: 'grey.50',
          borderColor: 'grey.300',
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
          <FontAwesomeIcon icon={faLock} />
          <Typography variant="body1" fontWeight={500}>
            Timing Configuration (locked during active exercise)
          </Typography>
        </Box>

        <Stack direction="row" spacing={4}>
          <Box>
            <Typography variant="body2" color="text.secondary">
              Delivery Mode:
            </Typography>
            <Typography variant="body1">
              {deliveryMode === DeliveryMode.ClockDriven ? 'Clock-driven' : 'Facilitator-paced'}
            </Typography>
          </Box>

          {showTimelineOptions && (
            <Box>
              <Typography variant="body2" color="text.secondary">
                Timeline Mode:
              </Typography>
              <Typography variant="body1">
                {timelineMode === TimelineMode.RealTime && 'Real-time (1:1)'}
                {timelineMode === TimelineMode.Compressed && `Compressed (${timeScale}x)`}
                {timelineMode === TimelineMode.StoryOnly && 'Story-only'}
              </Typography>
            </Box>
          )}
        </Stack>

        <Typography variant="caption" color="text.secondary" sx={{ mt: 1.5, display: 'block' }}>
          To change these settings, stop the exercise first.
        </Typography>
      </Paper>
    )
  }

  // Editable state UI
  return (
    <Stack direction="row" spacing={4} sx={{ flexWrap: 'wrap' }}>
      {/* Delivery Mode Section */}
      <FormControl component="fieldset" error={!!errors.deliveryMode} sx={{ minWidth: 280 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
          <FormLabel component="legend" sx={{ mb: 0 }}>
            How will injects be delivered?
          </FormLabel>
          <Tooltip title="Controls when injects become ready to fire during exercise conduct">
            <IconButton size="small" aria-label="Delivery mode help">
              <FontAwesomeIcon icon={faCircleQuestion} size="sm" />
            </IconButton>
          </Tooltip>
        </Box>

        <RadioGroup
          value={deliveryMode}
          onChange={e => onChange('deliveryMode', e.target.value as DeliveryMode)}
          aria-labelledby="delivery-mode-label"
        >
          <FormControlLabel
            value={DeliveryMode.ClockDriven}
            control={<Radio />}
            label={
              <Box>
                <Typography variant="body1" fontWeight={500}>
                  Clock-driven
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Injects become Ready at their Delivery Time
                </Typography>
              </Box>
            }
          />
          <FormControlLabel
            value={DeliveryMode.FacilitatorPaced}
            control={<Radio />}
            label={
              <Box>
                <Typography variant="body1" fontWeight={500}>
                  Facilitator-paced
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  You control when each inject is delivered
                </Typography>
              </Box>
            }
          />
        </RadioGroup>
        {errors.deliveryMode && (
          <FormHelperText>{errors.deliveryMode}</FormHelperText>
        )}
      </FormControl>

      {/* Timeline Mode Section - Only shown for Clock-driven */}
      {showTimelineOptions && (
        <FormControl component="fieldset" error={!!errors.timelineMode} sx={{ minWidth: 280 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <FormLabel component="legend" sx={{ mb: 0 }}>
              What timeline will the exercise use?
            </FormLabel>
            <Tooltip title="Determines how exercise time relates to real-world time">
              <IconButton size="small" aria-label="Timeline mode help">
                <FontAwesomeIcon icon={faCircleQuestion} size="sm" />
              </IconButton>
            </Tooltip>
          </Box>

          <RadioGroup
            value={timelineMode}
            onChange={e => onChange('timelineMode', e.target.value as TimelineMode)}
            aria-labelledby="timeline-mode-label"
          >
            <FormControlLabel
              value={TimelineMode.RealTime}
              control={<Radio />}
              label={
                <Box>
                  <Typography variant="body1" fontWeight={500}>
                    Real-time
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Exercise clock matches wall clock (1:1)
                  </Typography>
                </Box>
              }
            />
            <FormControlLabel
              value={TimelineMode.Compressed}
              control={<Radio />}
              label={
                <Box>
                  <Typography variant="body1" fontWeight={500}>
                    Compressed
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Simulate longer scenarios in less time
                  </Typography>
                </Box>
              }
            />
            <FormControlLabel
              value={TimelineMode.StoryOnly}
              control={<Radio />}
              label={
                <Box>
                  <Typography variant="body1" fontWeight={500}>
                    Story-only
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    No real-time clock, just narrative timestamps
                  </Typography>
                </Box>
              }
            />
          </RadioGroup>

          {/* Time Scale Input (shown only for Compressed mode) */}
          {showTimeScale && (
            <Box sx={{ mt: 2, ml: 4 }}>
              <CobraTextField
                label="Time Scale"
                type="number"
                value={timeScale ?? ''}
                onChange={e => {
                  const value = e.target.value ? parseFloat(e.target.value) : null
                  onChange('timeScale', value)
                }}
                error={!!errors.timeScale}
                helperText={timeScaleHelperText}
                placeholder="e.g., 4"
                slotProps={{
                  htmlInput: {
                    min: 0.1,
                    max: 60,
                    step: 0.1,
                  },
                }}
                sx={{ maxWidth: 200 }}
              />
            </Box>
          )}

          {errors.timelineMode && (
            <FormHelperText>{errors.timelineMode}</FormHelperText>
          )}
        </FormControl>
      )}
    </Stack>
  )
}

export default TimingConfigurationSection
