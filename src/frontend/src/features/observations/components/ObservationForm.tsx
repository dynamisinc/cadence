/**
 * ObservationForm Component
 *
 * Form for creating and editing observations during exercise conduct.
 * Evaluators use this to record their observations with HSEEP P/S/M/U ratings.
 */

import { useState, useMemo } from 'react'
import {
  Box,
  Stack,
  Typography,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faXmark, faSpinner } from '@fortawesome/free-solid-svg-icons'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import { ObservationRating, InjectStatus, getObservationRatingLabel } from '../../../types'
import type { InjectDto } from '../../injects/types'
import type { CreateObservationRequest, UpdateObservationRequest } from '../types'

interface ObservationFormProps {
  /** Inject to link observation to (optional) */
  inject?: InjectDto | null
  /** Available injects for selection */
  injects?: InjectDto[]
  /** Initial values for editing */
  initialValues?: {
    rating: ObservationRating
    content: string
    recommendation?: string
    injectId?: string
  }
  /** Called on submit */
  onSubmit: (data: CreateObservationRequest | UpdateObservationRequest) => Promise<void>
  /** Called on cancel */
  onCancel: () => void
  /** Is form submitting? */
  isSubmitting?: boolean
}

export const ObservationForm = ({
  inject,
  injects = [],
  initialValues,
  onSubmit,
  onCancel,
  isSubmitting = false,
}: ObservationFormProps) => {
  const [rating, setRating] = useState<ObservationRating>(
    initialValues?.rating ?? ObservationRating.Satisfactory,
  )
  const [content, setContent] = useState(initialValues?.content ?? '')
  const [recommendation, setRecommendation] = useState(initialValues?.recommendation ?? '')
  const [selectedInjectId, setSelectedInjectId] = useState<string>(
    inject?.id ?? initialValues?.injectId ?? '',
  )

  // Sort injects: recently fired first, then by sequence
  const sortedInjects = useMemo(() => {
    return [...injects].sort((a, b) => {
      // Fired injects first, sorted by firedAt descending (most recent first)
      const aIsFired = a.status === InjectStatus.Fired
      const bIsFired = b.status === InjectStatus.Fired
      const aIsSkipped = a.status === InjectStatus.Skipped

      if (aIsFired && !bIsFired) return -1
      if (!aIsFired && bIsFired) return 1
      if (aIsFired && bIsFired) {
        // Both fired, sort by firedAt descending
        const aTime = a.firedAt ? new Date(a.firedAt).getTime() : 0
        const bTime = b.firedAt ? new Date(b.firedAt).getTime() : 0
        return bTime - aTime
      }

      // Skipped injects after fired
      const bIsSkipped = b.status === InjectStatus.Skipped
      if (aIsSkipped && !bIsSkipped && !bIsFired) return -1
      if (!aIsSkipped && !aIsFired && bIsSkipped) return 1

      // Otherwise sort by sequence
      return a.sequence - b.sequence
    })
  }, [injects])

  // Get status label for inject
  const getStatusLabel = (inject: InjectDto): string => {
    if (inject.status === InjectStatus.Fired) return ' (Fired)'
    if (inject.status === InjectStatus.Skipped) return ' (Skipped)'
    return ''
  }

  const handleRatingChange = (event: SelectChangeEvent<ObservationRating>) => {
    setRating(event.target.value as ObservationRating)
  }

  const handleInjectChange = (event: SelectChangeEvent<string>) => {
    setSelectedInjectId(event.target.value)
  }

  const handleSubmit = async () => {
    const data: CreateObservationRequest = {
      rating,
      content: content.trim(),
      recommendation: recommendation.trim() || undefined,
      injectId: selectedInjectId || undefined,
    }

    await onSubmit(data)
  }

  const isValid = content.trim().length > 0

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        {initialValues ? 'Edit Observation' : 'Add Observation'}
      </Typography>

      <Stack spacing={2}>
        {/* Rating Selection */}
        <FormControl fullWidth size="small">
          <InputLabel id="rating-label">Rating</InputLabel>
          <Select
            labelId="rating-label"
            value={rating}
            label="Rating"
            onChange={handleRatingChange}
          >
            <MenuItem value={ObservationRating.Performed}>
              {getObservationRatingLabel(ObservationRating.Performed)}
            </MenuItem>
            <MenuItem value={ObservationRating.Satisfactory}>
              {getObservationRatingLabel(ObservationRating.Satisfactory)}
            </MenuItem>
            <MenuItem value={ObservationRating.Marginal}>
              {getObservationRatingLabel(ObservationRating.Marginal)}
            </MenuItem>
            <MenuItem value={ObservationRating.Unsatisfactory}>
              {getObservationRatingLabel(ObservationRating.Unsatisfactory)}
            </MenuItem>
          </Select>
        </FormControl>

        {/* Inject Selection (if not pre-selected) */}
        {!inject && injects.length > 0 && (
          <FormControl fullWidth size="small">
            <InputLabel id="inject-label">Related Inject (Optional)</InputLabel>
            <Select
              labelId="inject-label"
              value={selectedInjectId}
              label="Related Inject (Optional)"
              onChange={handleInjectChange}
            >
              <MenuItem value="">
                <em>None</em>
              </MenuItem>
              {sortedInjects.map(inj => (
                <MenuItem key={inj.id} value={inj.id}>
                  #{inj.injectNumber} - {inj.title || inj.description.slice(0, 50)}
                  {!inj.title && inj.description.length > 50 ? '...' : ''}
                  {getStatusLabel(inj)}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        )}

        {/* Observation Content */}
        <CobraTextField
          label="Observation"
          placeholder="Describe what you observed..."
          multiline
          rows={3}
          value={content}
          onChange={e => setContent(e.target.value)}
          required
          fullWidth
        />

        {/* Recommendation (Optional) */}
        <CobraTextField
          label="Recommendation (Optional)"
          placeholder="Suggest improvements or corrective actions..."
          multiline
          rows={2}
          value={recommendation}
          onChange={e => setRecommendation(e.target.value)}
          fullWidth
        />

        {/* Actions */}
        <Stack direction="row" spacing={1} justifyContent="flex-end">
          <CobraSecondaryButton
            onClick={onCancel}
            disabled={isSubmitting}
            startIcon={<FontAwesomeIcon icon={faXmark} />}
          >
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={handleSubmit}
            disabled={!isValid || isSubmitting}
            startIcon={
              isSubmitting ? (
                <FontAwesomeIcon icon={faSpinner} spin />
              ) : (
                <FontAwesomeIcon icon={faCheck} />
              )
            }
          >
            {initialValues ? 'Update' : 'Save'}
          </CobraPrimaryButton>
        </Stack>
      </Stack>
    </Box>
  )
}

export default ObservationForm
