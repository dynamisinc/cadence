/**
 * Phone Prompt Utilities
 *
 * Utility functions for managing the evaluator phone prompt state.
 * Separated from component to avoid React fast refresh issues.
 */

import type { CurrentUserProfileDto } from '@/features/users/types'

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
 * Set the prompt as dismissed for this exercise
 */
export const setPromptDismissed = (exerciseId: string): void => {
  try {
    localStorage.setItem(getDismissalKey(exerciseId), 'true')
  } catch {
    // Ignore localStorage errors
  }
}

/**
 * Check if the user needs to see the phone prompt
 */
export const shouldShowPhonePrompt = (
  userProfile: CurrentUserProfileDto | null,
  exerciseId: string,
): boolean => {
  if (!userProfile) return false
  if (userProfile.phoneNumber) return false
  if (isPromptDismissed(exerciseId)) return false
  return true
}
