/**
 * RatingBadge Component
 *
 * Displays a P/S/M/U observation rating as a colored badge.
 * Uses HSEEP standard colors for each rating level.
 */

import { Chip, Tooltip } from '@mui/material'
import {
  ObservationRating,
  ObservationRatingLabels,
  ObservationRatingShortLabels,
} from '../../../types'

interface RatingBadgeProps {
  rating: ObservationRating | null | undefined
  showLabel?: boolean
  size?: 'small' | 'medium'
}

/**
 * Get rating-specific colors
 */
const getRatingColors = (rating: ObservationRating | null | undefined) => {
  switch (rating) {
    case ObservationRating.Performed:
      return {
        bgcolor: '#e8f5e9', // Light green
        color: '#2e7d32', // Dark green
        borderColor: '#4caf50',
      }
    case ObservationRating.Satisfactory:
      return {
        bgcolor: '#e3f2fd', // Light blue
        color: '#1565c0', // Dark blue
        borderColor: '#2196f3',
      }
    case ObservationRating.Marginal:
      return {
        bgcolor: '#fff3e0', // Light orange
        color: '#e65100', // Dark orange
        borderColor: '#ff9800',
      }
    case ObservationRating.Unsatisfactory:
      return {
        bgcolor: '#ffebee', // Light red
        color: '#c62828', // Dark red
        borderColor: '#f44336',
      }
    default:
      return {
        bgcolor: '#f5f5f5', // Light gray
        color: '#757575', // Dark gray
        borderColor: '#9e9e9e',
      }
  }
}

export const RatingBadge = ({
  rating,
  showLabel = false,
  size = 'small',
}: RatingBadgeProps) => {
  if (!rating) {
    return null
  }

  const colors = getRatingColors(rating)
  const label = showLabel
    ? ObservationRatingLabels[rating]
    : ObservationRatingShortLabels[rating]
  const tooltipText = ObservationRatingLabels[rating]

  return (
    <Tooltip title={tooltipText} placement="top">
      <Chip
        label={label}
        size={size}
        sx={{
          bgcolor: colors.bgcolor,
          color: colors.color,
          border: `1px solid ${colors.borderColor}`,
          fontWeight: 600,
          minWidth: showLabel ? 'auto' : 32,
          '& .MuiChip-label': {
            px: showLabel ? 1.5 : 1,
          },
        }}
      />
    </Tooltip>
  )
}

export default RatingBadge
