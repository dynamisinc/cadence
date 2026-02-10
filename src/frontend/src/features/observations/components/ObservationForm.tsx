/**
 * ObservationForm Component
 *
 * Form for creating and editing observations during exercise conduct.
 * Evaluators use this to record their observations with HSEEP P/S/M/U ratings.
 */

import { useState, useMemo, useRef, useEffect } from 'react'
import {
  Box,
  Stack,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Divider,
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
import type { CreateObservationRequest, UpdateObservationRequest, ObservationDto } from '../types'
import type { CapabilityDto } from '../../capabilities/types'
import { ObservationCapabilitySelector } from './ObservationCapabilitySelector'
import { PhotoAttachmentSection } from '../../photos/components/PhotoAttachmentSection'

interface ObservationFormProps {
  /** Exercise ID for photo uploads */
  exerciseId: string
  /** Inject to link observation to (optional) */
  inject?: InjectDto | null
  /** Available injects for selection */
  injects?: InjectDto[]
  /** Available capabilities for tagging */
  capabilities?: CapabilityDto[]
  /** Target capability IDs for this exercise */
  targetCapabilityIds?: string[]
  /** Initial values for editing */
  initialValues?: {
    rating: ObservationRating
    content: string
    recommendation?: string
    location?: string
    injectId?: string
    capabilityIds?: string[]
  }
  /** Current observation (for editing with photos) */
  observation?: ObservationDto
  /** Scenario time to stamp photos with */
  scenarioTime?: string | null
  /** Called on submit (pendingPhotos provided in create mode for post-creation upload) */
  onSubmit: (data: CreateObservationRequest | UpdateObservationRequest, pendingPhotos?: File[]) => Promise<void>
  /** Called on cancel */
  onCancel: () => void
  /** Is form submitting? */
  isSubmitting?: boolean
  /** Called when a photo is added (to refresh observation) */
  onPhotoAdded?: () => void
}

export const ObservationForm = ({
  exerciseId: _exerciseId,
  inject,
  injects = [],
  capabilities = [],
  targetCapabilityIds = [],
  initialValues,
  observation,
  scenarioTime: _scenarioTime,
  onSubmit,
  onCancel,
  isSubmitting = false,
  onPhotoAdded: _onPhotoAdded,
}: ObservationFormProps) => {
  // Rating starts as null for new observations (requires active selection)
  const [rating, setRating] = useState<ObservationRating | null>(
    initialValues?.rating ?? null,
  )
  const [content, setContent] = useState(initialValues?.content ?? '')
  const [recommendation, setRecommendation] = useState(initialValues?.recommendation ?? '')
  const [location, setLocation] = useState(initialValues?.location ?? '')
  const [selectedInjectId, setSelectedInjectId] = useState<string>(
    inject?.id ?? initialValues?.injectId ?? '',
  )
  const [selectedCapabilityIds, setSelectedCapabilityIds] = useState<string[]>(
    initialValues?.capabilityIds ?? [],
  )
  const [pendingPhotos, setPendingPhotos] = useState<File[]>([])

  // Ref for auto-focusing the content input
  const contentInputRef = useRef<HTMLTextAreaElement>(null)

  // Auto-focus content input when form opens
  useEffect(() => {
    // Small delay to ensure the dialog animation completes
    const timer = setTimeout(() => {
      contentInputRef.current?.focus()
    }, 100)
    return () => clearTimeout(timer)
  }, [])

  // Sort injects: recently released first, then by sequence
  const sortedInjects = useMemo(() => {
    return [...injects].sort((a, b) => {
      // Released injects first, sorted by firedAt descending (most recent first)
      const aIsFired = a.status === InjectStatus.Released
      const bIsFired = b.status === InjectStatus.Released
      const aIsDeferred = a.status === InjectStatus.Deferred

      if (aIsFired && !bIsFired) return -1
      if (!aIsFired && bIsFired) return 1
      if (aIsFired && bIsFired) {
        // Both fired, sort by firedAt descending
        const aTime = a.firedAt ? new Date(a.firedAt).getTime() : 0
        const bTime = b.firedAt ? new Date(b.firedAt).getTime() : 0
        return bTime - aTime
      }

      // Deferred injects after fired
      const bIsDeferred = b.status === InjectStatus.Deferred
      if (aIsDeferred && !bIsDeferred && !bIsFired) return -1
      if (!aIsDeferred && !aIsFired && bIsDeferred) return 1

      // Otherwise sort by sequence
      return a.sequence - b.sequence
    })
  }, [injects])

  // Get status label for inject
  const getStatusLabel = (inject: InjectDto): string => {
    if (inject.status === InjectStatus.Released) return ' (Released)'
    if (inject.status === InjectStatus.Deferred) return ' (Deferred)'
    return ''
  }

  const handleRatingChange = (event: SelectChangeEvent<ObservationRating | ''>) => {
    const value = event.target.value
    setRating(value === '' ? null : (value as ObservationRating))
  }

  const handleInjectChange = (event: SelectChangeEvent<string>) => {
    setSelectedInjectId(event.target.value)
  }

  const handleSubmit = async () => {
    if (!rating) return // Guard against submitting without rating

    const data: CreateObservationRequest = {
      rating,
      content: content.trim(),
      recommendation: recommendation.trim() || undefined,
      location: location.trim() || undefined,
      injectId: selectedInjectId || undefined,
      capabilityIds: selectedCapabilityIds.length > 0 ? selectedCapabilityIds : undefined,
    }

    const photosToUpload = pendingPhotos.length > 0 ? pendingPhotos : undefined
    await onSubmit(data, photosToUpload)
  }

  // Require both content and rating selection
  const isValid = content.trim().length > 0 && rating !== null

  return (
    <Box>
      <Stack spacing={2}>
        {/* Rating Selection */}
        <FormControl fullWidth size="small" required>
          <InputLabel id="rating-label" shrink>Rating *</InputLabel>
          <Select
            labelId="rating-label"
            value={rating ?? ''}
            label="Rating *"
            onChange={handleRatingChange}
            displayEmpty
            notched
          >
            <MenuItem value="" disabled>
              <em>Select a rating...</em>
            </MenuItem>
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
          inputRef={contentInputRef}
        />

        {/* Photo Attachments */}
        <PhotoAttachmentSection
          photos={observation?.photos ?? []}
          pendingFiles={pendingPhotos}
          onPendingFilesChange={setPendingPhotos}
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

        {/* Location (Optional) */}
        <CobraTextField
          label="Location (Optional)"
          placeholder="Where did this occur? (e.g., EOC, Field Site A)"
          value={location}
          onChange={e => setLocation(e.target.value)}
          fullWidth
          slotProps={{
            htmlInput: { maxLength: 200 },
          }}
          helperText={location.length > 0 ? `${location.length}/200` : undefined}
        />

        {/* Capability Tags (S05) */}
        {capabilities.length > 0 && (
          <>
            <Divider sx={{ my: 1 }} />
            <ObservationCapabilitySelector
              capabilities={capabilities}
              targetCapabilityIds={targetCapabilityIds}
              selectedIds={selectedCapabilityIds}
              onChange={setSelectedCapabilityIds}
              disabled={isSubmitting}
            />
          </>
        )}

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
