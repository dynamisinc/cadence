/**
 * TimingConfigurationSection - Exercise timing configuration fields
 *
 * Provides UI for configuring Delivery Mode (Clock-driven / Facilitator-paced)
 * and Clock Speed (1x, 2x, 5x, 10x, 20x) per CLK-03.
 *
 * Smart defaults are applied based on exercise type:
 * - TTX → Facilitator-paced
 * - FSE, FE, CAX → Clock-driven
 *
 * @module features/exercises
 * @see exercise-config/S03-timing-configuration-ui
 */

import { type FC } from 'react'
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
import { DeliveryMode, TimelineMode, ExerciseType } from '../../../types'
import { CLOCK_MULTIPLIER_PRESETS } from '../types'

interface TimingConfigurationSectionProps {
  /** Current delivery mode */
  deliveryMode: DeliveryMode
  /** Current timeline mode */
  timelineMode: TimelineMode
  /** Current clock multiplier (1, 2, 5, 10, or 20) */
  clockMultiplier: number
  /** Exercise type (kept for API consistency, not directly used in component) */
  exerciseType?: ExerciseType
  /** Whether fields are locked (exercise is Active) */
  isLocked: boolean
  /** Callback for field changes */
  onChange: (field: string, value: DeliveryMode | TimelineMode | number) => void
  /** Validation errors */
  errors?: {
    deliveryMode?: string
    timelineMode?: string
    clockMultiplier?: string
  }
}

/**
 * TimingConfigurationSection component
 */
export const TimingConfigurationSection: FC<TimingConfigurationSectionProps> = ({
  deliveryMode,
  timelineMode,
  clockMultiplier,
  exerciseType: _exerciseType,
  isLocked,
  onChange,
  errors = {},
}) => {
  // Timeline options only shown for Clock-driven mode
  const showTimelineOptions = deliveryMode === DeliveryMode.ClockDriven

  // Get label for current clock multiplier
  const getClockMultiplierLabel = (value: number) => {
    const preset = CLOCK_MULTIPLIER_PRESETS.find(p => p.value === value)
    return preset?.label ?? `${value}x`
  }

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
            <>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Timeline Mode:
                </Typography>
                <Typography variant="body1">
                  {timelineMode === TimelineMode.RealTime && 'Real-time'}
                  {timelineMode === TimelineMode.Compressed && 'Compressed'}
                  {timelineMode === TimelineMode.StoryOnly && 'Story-only'}
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Clock Speed:
                </Typography>
                <Typography variant="body1">
                  {getClockMultiplierLabel(clockMultiplier)}
                </Typography>
              </Box>
            </>
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

          {errors.timelineMode && (
            <FormHelperText>{errors.timelineMode}</FormHelperText>
          )}
        </FormControl>
      )}

      {/* Clock Speed Section - Only shown for Clock-driven */}
      {showTimelineOptions && (
        <FormControl component="fieldset" error={!!errors.clockMultiplier} sx={{ minWidth: 280 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <FormLabel component="legend" sx={{ mb: 0 }}>
              Clock Speed
            </FormLabel>
            <Tooltip title="How fast scenario time runs compared to wall clock time">
              <IconButton size="small" aria-label="Clock speed help">
                <FontAwesomeIcon icon={faCircleQuestion} size="sm" />
              </IconButton>
            </Tooltip>
          </Box>

          <RadioGroup
            value={clockMultiplier}
            onChange={e => onChange('clockMultiplier', Number(e.target.value))}
            aria-labelledby="clock-multiplier-label"
          >
            {CLOCK_MULTIPLIER_PRESETS.map(preset => (
              <FormControlLabel
                key={preset.value}
                value={preset.value}
                control={<Radio size="small" />}
                label={
                  <Stack spacing={0}>
                    <span>{preset.label}</span>
                    {preset.value > 1 && (
                      <Typography variant="caption" color="text.secondary">
                        1 minute wall clock = {preset.value} minutes scenario time
                      </Typography>
                    )}
                  </Stack>
                }
              />
            ))}
          </RadioGroup>

          {errors.clockMultiplier && (
            <FormHelperText>{errors.clockMultiplier}</FormHelperText>
          )}
        </FormControl>
      )}
    </Stack>
  )
}

export default TimingConfigurationSection
