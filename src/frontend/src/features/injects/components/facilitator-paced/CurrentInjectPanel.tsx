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
  faEye,
  faFire,
  faForwardStep,
  faBook,
  faBullseye,
  faTowerBroadcast,
  faLightbulb,
  faClipboardList,
  faUserTie,
  faLocationDot,
  faFlag,
  faRoad,
  faReply,
} from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import { InjectTypeChip } from '../InjectTypeChip'
import type { InjectDto } from '../../types'
import { formatScenarioTime } from '../../types'

const priorityLabels: Record<number, string> = {
  1: 'Critical',
  2: 'High',
  3: 'Medium',
  4: 'Low',
  5: 'Info',
}

const priorityColors: Record<number, 'error' | 'warning' | 'info' | 'default'> = {
  1: 'error',
  2: 'warning',
  3: 'info',
  4: 'default',
  5: 'default',
}

interface CurrentInjectPanelProps {
  /** The current inject to display */
  inject: InjectDto
  /** Called when Fire & Continue button clicked */
  onFire: () => void
  /** Called when Skip button clicked */
  onSkip: () => void
  /** Called when View button clicked to open the detail drawer */
  onView?: () => void
  /** Whether the current user can control injects */
  canControl?: boolean
  /** Whether actions are currently being submitted */
  isSubmitting?: boolean
}

export const CurrentInjectPanel = ({
  inject,
  onFire,
  onSkip,
  onView,
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
        {/* Header Row: label + type/time chips + action buttons */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mr: 'auto' }}>
            <Box sx={{ color: 'primary.main' }}>
              <FontAwesomeIcon icon={faBook} size="lg" />
            </Box>
            <Typography variant="h6" fontWeight={600}>
              CURRENT INJECT
            </Typography>
            <InjectTypeChip type={inject.injectType} />
            {scenarioTimeDisplay && (
              <Chip
                icon={<FontAwesomeIcon icon={faBook} size="xs" />}
                label={scenarioTimeDisplay}
                color="primary"
                variant="outlined"
              />
            )}
          </Stack>

          {/* Action buttons in the header row */}
          {onView && (
            <CobraSecondaryButton
              size="small"
              startIcon={<FontAwesomeIcon icon={faEye} />}
              onClick={onView}
            >
              View
            </CobraSecondaryButton>
          )}
          {canControl && (
            <>
              <CobraSecondaryButton
                size="small"
                startIcon={<FontAwesomeIcon icon={faForwardStep} />}
                onClick={onSkip}
                disabled={isSubmitting}
              >
                Skip
              </CobraSecondaryButton>
              <CobraPrimaryButton
                size="small"
                startIcon={<FontAwesomeIcon icon={faFire} />}
                onClick={onFire}
                disabled={isSubmitting}
                sx={{ fontWeight: 600 }}
              >
                Fire & Continue
              </CobraPrimaryButton>
            </>
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

        {/* Delivery Context - Two columns */}
        <Stack direction="row" spacing={4} flexWrap="wrap" useFlexGap>
          {/* Left column: Target & Source */}
          <Stack spacing={1} sx={{ minWidth: 200 }}>
            {inject.target && (
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faBullseye} style={{ opacity: 0.7, width: 14 }} />
                <Typography variant="body2" color="text.secondary">
                  To:
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {inject.target}
                </Typography>
              </Stack>
            )}
            {inject.source && (
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faReply} style={{ opacity: 0.7, width: 14 }} />
                <Typography variant="body2" color="text.secondary">
                  From:
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {inject.source}
                </Typography>
              </Stack>
            )}
            {inject.deliveryMethodName && (
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faTowerBroadcast} style={{ opacity: 0.7, width: 14 }} />
                <Typography variant="body2" color="text.secondary">
                  Via:
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {inject.deliveryMethodName}
                </Typography>
              </Stack>
            )}
          </Stack>

          {/* Right column: Organization metadata */}
          <Stack spacing={1} sx={{ minWidth: 200 }}>
            {inject.responsibleController && (
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faUserTie} style={{ opacity: 0.7, width: 14 }} />
                <Typography variant="body2" color="text.secondary">
                  Controller:
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {inject.responsibleController}
                </Typography>
              </Stack>
            )}
            {inject.locationName && (
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faLocationDot} style={{ opacity: 0.7, width: 14 }} />
                <Typography variant="body2" color="text.secondary">
                  Location:
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {inject.locationName}
                </Typography>
              </Stack>
            )}
            {inject.track && (
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faRoad} style={{ opacity: 0.7, width: 14 }} />
                <Typography variant="body2" color="text.secondary">
                  Track:
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {inject.track}
                </Typography>
              </Stack>
            )}
            {inject.priority !== null && inject.priority !== undefined && (
              <Stack direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon icon={faFlag} style={{ opacity: 0.7, width: 14 }} />
                <Typography variant="body2" color="text.secondary">
                  Priority:
                </Typography>
                <Chip
                  label={priorityLabels[inject.priority] || `P${inject.priority}`}
                  size="small"
                  color={priorityColors[inject.priority] || 'default'}
                  variant="outlined"
                />
              </Stack>
            )}
          </Stack>
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

      </Stack>
    </Paper>
  )
}

export default CurrentInjectPanel
