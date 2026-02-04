/**
 * PublishButton Component
 *
 * Button to publish (activate) an exercise with approval validation (S07).
 * Shows blocked dialog if there are unapproved injects.
 *
 * @module features/exercises/components
 */

import { useState } from 'react'
import { Tooltip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faRocket,
  faExclamationTriangle,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { usePublishValidation } from '../hooks'
import { PublishBlockedDialog } from './PublishBlockedDialog'

interface PublishButtonProps {
  /** The exercise ID */
  exerciseId: string
  /** Called when user clicks to publish (if allowed) */
  onPublish: () => void
  /** Whether a publish operation is in progress */
  isPublishing?: boolean
  /** Button size */
  size?: 'small' | 'medium' | 'large'
}

/**
 * Publish Button
 *
 * Shows a publish/activate button that validates approval status first.
 * If there are unapproved injects, shows a warning and blocks publishing.
 *
 * @example
 * <PublishButton
 *   exerciseId={exerciseId}
 *   onPublish={handleActivate}
 *   isPublishing={isActivating}
 * />
 */
export const PublishButton = ({
  exerciseId,
  onPublish,
  isPublishing = false,
  size = 'medium',
}: PublishButtonProps) => {
  const [blockedDialogOpen, setBlockedDialogOpen] = useState(false)
  const { validation, isLoading, canPublish, unapprovedCount, refetch } =
    usePublishValidation(exerciseId)

  const handleClick = async () => {
    // Refresh validation before checking
    await refetch()

    if (canPublish) {
      onPublish()
    } else {
      setBlockedDialogOpen(true)
    }
  }

  const hasUnapproved = unapprovedCount > 0
  const tooltipText = hasUnapproved
    ? `${unapprovedCount} inject${unapprovedCount !== 1 ? 's' : ''} need approval`
    : 'Activate exercise for conduct'

  return (
    <>
      <Tooltip title={tooltipText}>
        <span>
          <CobraPrimaryButton
            size={size}
            onClick={handleClick}
            disabled={isLoading || isPublishing}
            color={hasUnapproved ? 'warning' : 'primary'}
            startIcon={
              <FontAwesomeIcon
                icon={
                  isLoading || isPublishing
                    ? faSpinner
                    : hasUnapproved
                      ? faExclamationTriangle
                      : faRocket
                }
                spin={isLoading || isPublishing}
              />
            }
          >
            Activate Exercise
          </CobraPrimaryButton>
        </span>
      </Tooltip>

      <PublishBlockedDialog
        open={blockedDialogOpen}
        onClose={() => setBlockedDialogOpen(false)}
        exerciseId={exerciseId}
        validation={validation}
      />
    </>
  )
}

export default PublishButton
