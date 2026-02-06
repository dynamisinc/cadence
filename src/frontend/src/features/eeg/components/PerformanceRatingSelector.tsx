/**
 * PerformanceRatingSelector Component
 *
 * P/S/M/U rating selection for EEG entries.
 * Displays HSEEP-defined performance ratings with descriptions.
 */

import { Box, ToggleButton, ToggleButtonGroup, Typography, Tooltip } from '@mui/material'
import {
  PerformanceRating,
  PERFORMANCE_RATING_LABELS,
  PERFORMANCE_RATING_DESCRIPTIONS,
  PERFORMANCE_RATING_COLORS,
  PERFORMANCE_RATING_SHORT_LABELS,
} from '../types'

interface PerformanceRatingSelectorProps {
  /** Currently selected rating */
  value: PerformanceRating | null
  /** Called when rating changes */
  onChange: (rating: PerformanceRating | null) => void
  /** Whether the selector is disabled */
  disabled?: boolean
  /** Error state */
  error?: boolean
  /** Helper text (typically error message) */
  helperText?: string
}

/**
 * Displays P/S/M/U rating buttons with HSEEP-compliant descriptions.
 *
 * Features:
 * - Large toggle buttons for touch/click accessibility
 * - Full descriptions on hover/focus
 * - Color-coded selection feedback
 * - Keyboard shortcut hints (1=P, 2=S, 3=M, 4=U)
 */
export const PerformanceRatingSelector = ({
  value,
  onChange,
  disabled = false,
  error = false,
  helperText,
}: PerformanceRatingSelectorProps) => {
  const handleChange = (
    _: React.MouseEvent<HTMLElement>,
    newValue: PerformanceRating | null,
  ) => {
    onChange(newValue)
  }

  // Get the description for the selected rating
  const selectedDescription = value ? PERFORMANCE_RATING_DESCRIPTIONS[value] : null
  const selectedLabel = value ? PERFORMANCE_RATING_LABELS[value] : 'Select a rating'

  return (
    <Box>
      <Typography
        variant="subtitle2"
        color={error ? 'error' : 'text.secondary'}
        gutterBottom
      >
        Performance Rating *
      </Typography>

      <ToggleButtonGroup
        value={value}
        exclusive
        onChange={handleChange}
        disabled={disabled}
        sx={{
          mb: 1,
          width: '100%',
          '& .MuiToggleButton-root': {
            flex: 1,
            py: 1.5,
            fontSize: '1.25rem',
            fontWeight: 700,
            border: error ? '2px solid' : '1px solid',
            borderColor: error ? 'error.main' : 'divider',
            '&.Mui-selected': {
              borderWidth: 2,
            },
          },
        }}
      >
        {Object.values(PerformanceRating).map((rating, index) => (
          <Tooltip
            key={rating}
            title={
              <Box sx={{ maxWidth: 300, p: 1 }}>
                <Typography variant="subtitle2" fontWeight={600}>
                  {PERFORMANCE_RATING_LABELS[rating]}
                </Typography>
                <Typography variant="body2" sx={{ mt: 0.5 }}>
                  {PERFORMANCE_RATING_DESCRIPTIONS[rating]}
                </Typography>
                <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                  Keyboard shortcut: {index + 1}
                </Typography>
              </Box>
            }
            placement="top"
            arrow
          >
            <ToggleButton
              value={rating}
              aria-label={PERFORMANCE_RATING_LABELS[rating]}
              sx={{
                '&.Mui-selected': {
                  backgroundColor: `${PERFORMANCE_RATING_COLORS[rating]}20`,
                  color: PERFORMANCE_RATING_COLORS[rating],
                  borderColor: PERFORMANCE_RATING_COLORS[rating],
                  '&:hover': {
                    backgroundColor: `${PERFORMANCE_RATING_COLORS[rating]}30`,
                  },
                },
              }}
            >
              {PERFORMANCE_RATING_SHORT_LABELS[rating]}
            </ToggleButton>
          </Tooltip>
        ))}
      </ToggleButtonGroup>

      {/* Selected Rating Description */}
      <Box
        sx={{
          p: 1.5,
          bgcolor: value ? `${PERFORMANCE_RATING_COLORS[value]}10` : 'grey.50',
          borderRadius: 1,
          borderLeft: 3,
          borderColor: value ? PERFORMANCE_RATING_COLORS[value] : 'grey.300',
          minHeight: 80,
        }}
      >
        <Typography
          variant="subtitle2"
          sx={{
            color: value ? PERFORMANCE_RATING_COLORS[value] : 'text.secondary',
            fontWeight: 600,
          }}
        >
          {selectedLabel}
        </Typography>
        {selectedDescription && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {selectedDescription}
          </Typography>
        )}
        {!value && (
          <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
            Click a rating button above to see its full description
          </Typography>
        )}
      </Box>

      {helperText && (
        <Typography variant="caption" color={error ? 'error' : 'text.secondary'} sx={{ mt: 0.5 }}>
          {helperText}
        </Typography>
      )}
    </Box>
  )
}

export default PerformanceRatingSelector
