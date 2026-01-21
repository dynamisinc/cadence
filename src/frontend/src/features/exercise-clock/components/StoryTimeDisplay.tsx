/**
 * StoryTimeDisplay Component
 *
 * Displays the current story time with optional compression indicator.
 * Story time represents the fictional timeline within the scenario narrative.
 *
 * @module features/exercise-clock
 * @see CLK-08 Display Story Time in Clock Area
 */

import { Box, Typography, Chip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBook } from '@fortawesome/free-solid-svg-icons'
import { TimelineMode } from '../../../types'
import type { StoryTime } from '../utils/storyTime'

/**
 * StoryTimeDisplay component props
 */
export interface StoryTimeDisplayProps {
  /** Calculated story time object */
  storyTime: StoryTime | null
  /** Formatted story time string */
  formattedStoryTime: string
  /** Whether exercise is in StoryOnly mode */
  isStoryOnly: boolean
  /** Exercise timeline mode */
  timelineMode: TimelineMode
  /** Time compression scale (for Compressed mode) */
  timeScale: number | null
}

/**
 * Displays story time in the exercise clock area
 *
 * Shows the current story time with appropriate label based on timeline mode.
 * In Compressed mode, displays a compression indicator chip.
 *
 * @param props - Component props
 * @returns Story time display component
 */
export const StoryTimeDisplay = ({
  storyTime: _storyTime,
  formattedStoryTime,
  isStoryOnly,
  timelineMode,
  timeScale,
}: StoryTimeDisplayProps) => {
  const showCompressionIndicator = timelineMode === TimelineMode.Compressed && timeScale !== null

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 1.5,
        py: 1,
      }}
    >
      {/* Book icon */}
      <FontAwesomeIcon
        icon={faBook}
        style={{ opacity: 0.7 }}
        aria-hidden="true"
      />

      {/* Label and time */}
      <Typography
        variant="body1"
        component="div"
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1,
        }}
      >
        <span>{isStoryOnly ? 'Current Scenario Time:' : 'Scenario Time:'}</span>
        <Typography
          component="span"
          sx={{
            fontFamily: 'monospace',
            fontWeight: 600,
            fontSize: '1.1rem',
          }}
        >
          {formattedStoryTime}
        </Typography>
      </Typography>

      {/* Compression indicator */}
      {showCompressionIndicator && (
        <Chip
          label={`${timeScale}x compressed`}
          size="small"
          variant="outlined"
          sx={{
            fontSize: '0.75rem',
            height: '24px',
          }}
        />
      )}
    </Box>
  )
}

export default StoryTimeDisplay
