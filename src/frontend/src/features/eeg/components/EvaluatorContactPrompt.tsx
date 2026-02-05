/**
 * EvaluatorContactPrompt Component
 *
 * Prompts evaluators for their phone number before entering EEG observations.
 * Displays as a collapsible banner at the top of the EEG Entry form.
 * Per S12: Shows only when phone is null and can be dismissed per exercise.
 */

import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Stack,
  Alert,
  Collapse,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPhone, faXmark, faCheck } from '@fortawesome/free-solid-svg-icons'
import {
  CobraTextField,
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import { userService } from '@/features/users/services/userService'
import type { CurrentUserProfileDto } from '@/features/users/types'

interface EvaluatorContactPromptProps {
  /** Exercise ID for dismissal tracking */
  exerciseId: string
  /** Current user's profile */
  userProfile: CurrentUserProfileDto | null
  /** Called when user saves phone or dismisses prompt */
  onComplete: () => void
  /** Called when phone is saved successfully */
  onPhoneSaved?: (phone: string) => void
}

/** Max phone number length per S12 spec */
const MAX_PHONE_LENGTH = 25

/** LocalStorage key for dismissed prompts */
const getDismissalKey = (exerciseId: string) => `eeg_phone_prompt_dismissed_${exerciseId}`

/**
 * Check if the prompt has been dismissed for this exercise
 */
export const isPromptDismissed = (exerciseId: string): boolean => {
  try {
    return localStorage.getItem(getDismissalKey(exerciseId)) === 'true'
  } catch {
    return false
  }
}

/**
 * Check if the user needs to see the phone prompt
 */
export const shouldShowPhonePrompt = (
  userProfile: CurrentUserProfileDto | null,
  exerciseId: string
): boolean => {
  if (!userProfile) return false
  if (userProfile.phoneNumber) return false
  if (isPromptDismissed(exerciseId)) return false
  return true
}

/**
 * Evaluator contact information prompt component
 */
export const EvaluatorContactPrompt: FC<EvaluatorContactPromptProps> = ({
  exerciseId,
  userProfile,
  onComplete,
  onPhoneSaved,
}) => {
  const [phone, setPhone] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showValidationHint, setShowValidationHint] = useState(false)

  // Reset state when exercise changes
  useEffect(() => {
    setPhone('')
    setError(null)
    setShowValidationHint(false)
  }, [exerciseId])

  const handlePhoneChange = (value: string) => {
    setPhone(value)
    setError(null)
    setShowValidationHint(false)
  }

  const validatePhone = (value: string): boolean => {
    if (value.length > MAX_PHONE_LENGTH) {
      setError(`Phone number cannot exceed ${MAX_PHONE_LENGTH} characters`)
      return false
    }
    // Warn but allow if < 7 chars (per S12 spec)
    if (value.length > 0 && value.length < 7) {
      setShowValidationHint(true)
    }
    return true
  }

  const handleSave = async () => {
    const trimmedPhone = phone.trim()

    if (!trimmedPhone) {
      setError('Please enter a phone number or click Skip')
      return
    }

    if (!validatePhone(trimmedPhone)) {
      return
    }

    setIsSubmitting(true)
    setError(null)

    try {
      await userService.updateMyContact({ phoneNumber: trimmedPhone })
      onPhoneSaved?.(trimmedPhone)
      onComplete()
    } catch (err: unknown) {
      const axiosError = err as { response?: { data?: { errors?: { phoneNumber?: string[] } } } }
      const phoneError = axiosError.response?.data?.errors?.phoneNumber?.[0]
      setError(phoneError || 'Failed to save phone number. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleSkip = () => {
    // Save dismissal to localStorage (client-specific per S12 spec)
    try {
      localStorage.setItem(getDismissalKey(exerciseId), 'true')
    } catch {
      // Ignore localStorage errors
    }
    onComplete()
  }

  if (!userProfile) {
    return null
  }

  return (
    <Alert
      severity="info"
      icon={<FontAwesomeIcon icon={faPhone} />}
      sx={{
        mb: 2,
        '& .MuiAlert-message': { width: '100%' },
      }}
    >
      <Typography variant="subtitle1" fontWeight={600} gutterBottom>
        Complete Your Evaluator Contact Info
      </Typography>

      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Your contact information appears on EEG documents. Phone number is optional.
      </Typography>

      <Stack spacing={1.5}>
        {/* Read-only fields from auth */}
        <Box>
          <Typography variant="caption" color="text.secondary">
            Name
          </Typography>
          <Typography variant="body2">
            {userProfile.displayName}
            <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1 }}>
              (from account)
            </Typography>
          </Typography>
        </Box>

        <Box>
          <Typography variant="caption" color="text.secondary">
            Email
          </Typography>
          <Typography variant="body2">
            {userProfile.email}
            <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1 }}>
              (from account)
            </Typography>
          </Typography>
        </Box>

        {/* Phone input */}
        <Box>
          <CobraTextField
            label="Phone"
            value={phone}
            onChange={(e) => handlePhoneChange(e.target.value)}
            placeholder="e.g., (555) 123-4567 or +1-555-123-4567"
            fullWidth
            size="small"
            error={!!error}
            helperText={
              error ||
              (showValidationHint
                ? 'Phone number seems short - are you sure?'
                : `${phone.length}/${MAX_PHONE_LENGTH}`)
            }
            slotProps={{
              htmlInput: {
                maxLength: MAX_PHONE_LENGTH,
              },
            }}
          />
        </Box>

        {/* Actions */}
        <Stack direction="row" spacing={1} justifyContent="flex-end" sx={{ mt: 1 }}>
          <CobraSecondaryButton
            size="small"
            onClick={handleSkip}
            disabled={isSubmitting}
            startIcon={<FontAwesomeIcon icon={faXmark} />}
          >
            Skip for Now
          </CobraSecondaryButton>
          <CobraPrimaryButton
            size="small"
            onClick={handleSave}
            disabled={isSubmitting || !phone.trim()}
            startIcon={<FontAwesomeIcon icon={faCheck} />}
          >
            {isSubmitting ? 'Saving...' : 'Save & Continue'}
          </CobraPrimaryButton>
        </Stack>
      </Stack>
    </Alert>
  )
}

export default EvaluatorContactPrompt
