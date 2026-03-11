/**
 * RatingBadge Component
 *
 * Displays a P/S/M/U observation rating as a colored badge.
 * Uses HSEEP standard colors for each rating level via COBRA theme tokens.
 */

import { Chip, Tooltip } from '@mui/material'
import { useTheme } from '@mui/material/styles'
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
 * Maps ObservationRating to the corresponding COBRA rating palette key.
 */
const ratingToPaletteKey = (
  rating: ObservationRating | null | undefined,
): 'performed' | 'satisfactory' | 'marginal' | 'unsatisfactory' | 'unrated' => {
  switch (rating) {
    case ObservationRating.Performed:
      return 'performed'
    case ObservationRating.Satisfactory:
      return 'satisfactory'
    case ObservationRating.Marginal:
      return 'marginal'
    case ObservationRating.Unsatisfactory:
      return 'unsatisfactory'
    default:
      return 'unrated'
  }
}

export const RatingBadge = ({
  rating,
  showLabel = false,
  size = 'small',
}: RatingBadgeProps) => {
  const theme = useTheme()

  if (!rating) {
    return null
  }

  const paletteKey = ratingToPaletteKey(rating)
  const colors = theme.palette.rating[paletteKey]
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
          bgcolor: colors.bg,
          color: colors.text,
          border: `1px solid ${colors.border}`,
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
