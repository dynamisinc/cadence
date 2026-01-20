/**
 * CurrentInjectPanel
 *
 * Displays the current inject with full content for facilitator-paced conduct view.
 * Shows title, description, target, delivery method, expected action, and controller notes.
 *
 * @module features/injects
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import {
  Paper,
  Box,
  Typography,
  Stack,
  Divider,
  Chip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faFire,
  faForwardStep,
  faBook,
  faBullseye,
  faTowerBroadcast,
  faLightbulb,
  faClipboardList,
} from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import type { InjectDto } from '../../types'
import { formatScenarioTime } from '../../types'

interface CurrentInjectPanelProps {
  /** The current inject to display */
  inject: InjectDto
  /** Called when Fire & Continue button clicked */
  onFire: () => void
  /** Called when Skip button clicked */
  onSkip: () => void
  /** Whether the current user can control injects */
  canControl?: boolean
  /** Whether actions are currently being submitted */
  isSubmitting?: boolean
}

export const CurrentInjectPanel = ({
  inject,
  onFire,
  onSkip,
  canControl = true,
  isSubmitting = false,
}: CurrentInjectPanelProps) => {
  const scenarioTimeDisplay = formatScenarioTime(inject.scenarioDay, inject.scenarioTime)

  return (
    <Paper
      variant="outlined"
      sx={{
        p: 3,
        backgroundColor: 'primary.50',
        borderColor: 'primary.main',
        borderWidth: 2,
      }}
    >
      <Stack spacing={2}>
        {/* Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Stack direction="row" spacing={2} alignItems="center">
            <Box sx={{ color: 'primary.main' }}>
              <FontAwesomeIcon icon={faBook} size="lg" />
            </Box>
            <Typography variant="h6" fontWeight={600}>
              CURRENT INJECT
            </Typography>
          </Stack>
          {scenarioTimeDisplay && (
            <Chip
              icon={<FontAwesomeIcon icon={faBook} size="xs" />}
              label={scenarioTimeDisplay}
              color="primary"
              variant="outlined"
            />
          )}
        </Box>

        {/* Inject Number and Title */}
        <Box>
          <Typography
            variant="h4"
            sx={{
              display: 'flex',
              alignItems: 'baseline',
              gap: 2,
              mb: 1,
            }}
          >
            <Chip
              label={`#${inject.injectNumber}`}
              size="medium"
              color="primary"
              sx={{ fontSize: '1.1rem', fontWeight: 600 }}
            />
            <span>{inject.title}</span>
          </Typography>
        </Box>

        <Divider />

        {/* Delivery Context */}
        <Stack direction="row" spacing={3} flexWrap="wrap">
          {inject.target && (
            <Stack direction="row" spacing={1} alignItems="center">
              <FontAwesomeIcon icon={faBullseye} style={{ opacity: 0.7 }} />
              <Typography variant="body2" color="text.secondary">
                To:
              </Typography>
              <Typography variant="body2" fontWeight={500}>
                {inject.target}
              </Typography>
            </Stack>
          )}
          {inject.deliveryMethodName && (
            <Stack direction="row" spacing={1} alignItems="center">
              <FontAwesomeIcon icon={faTowerBroadcast} style={{ opacity: 0.7 }} />
              <Typography variant="body2" color="text.secondary">
                Via:
              </Typography>
              <Typography variant="body2" fontWeight={500}>
                {inject.deliveryMethodName}
              </Typography>
            </Stack>
          )}
        </Stack>

        <Divider />

        {/* Description */}
        <Box
          sx={{
            p: 2,
            backgroundColor: 'background.paper',
            borderRadius: 1,
            borderLeft: '4px solid',
            borderLeftColor: 'primary.main',
          }}
        >
          <Typography
            variant="body1"
            sx={{
              fontSize: '1.1rem',
              lineHeight: 1.7,
              whiteSpace: 'pre-wrap',
            }}
          >
            "{inject.description}"
          </Typography>
        </Box>

        {/* Expected Action */}
        {inject.expectedAction && (
          <>
            <Divider />
            <Box>
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
                <FontAwesomeIcon icon={faLightbulb} style={{ color: 'goldenrod' }} />
                <Typography variant="subtitle2" fontWeight={600}>
                  Expected Action:
                </Typography>
              </Stack>
              <Typography variant="body2" color="text.secondary">
                {inject.expectedAction}
              </Typography>
            </Box>
          </>
        )}

        {/* Controller Notes */}
        {inject.controllerNotes && (
          <>
            <Divider />
            <Box>
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
                <FontAwesomeIcon icon={faClipboardList} style={{ color: 'navy' }} />
                <Typography variant="subtitle2" fontWeight={600}>
                  Controller Notes:
                </Typography>
              </Stack>
              <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                {inject.controllerNotes}
              </Typography>
            </Box>
          </>
        )}

        {/* Action Buttons */}
        {canControl && (
          <>
            <Divider />
            <Box sx={{ display: 'flex', justifyContent: 'center', gap: 2, pt: 1 }}>
              <CobraSecondaryButton
                onClick={onSkip}
                disabled={isSubmitting}
                startIcon={<FontAwesomeIcon icon={faForwardStep} />}
                size="large"
              >
                Skip
              </CobraSecondaryButton>
              <CobraPrimaryButton
                onClick={onFire}
                disabled={isSubmitting}
                startIcon={<FontAwesomeIcon icon={faFire} />}
                size="large"
                sx={{ px: 4 }}
              >
                FIRE & CONTINUE
              </CobraPrimaryButton>
            </Box>
          </>
        )}
      </Stack>
    </Paper>
  )
}

export default CurrentInjectPanel
