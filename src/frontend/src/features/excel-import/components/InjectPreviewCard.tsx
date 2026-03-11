/**
 * InjectPreviewCard Component
 *
 * Displays a preview of how an imported inject will appear in Cadence.
 * Uses similar styling to InjectDetailDrawer for consistency.
 */

import {
  Box,
  Typography,
  Stack,
  Chip,
  Divider,
  Paper,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faClock,
  faCalendarDay,
  faBullseye,
  faUser,
  faEnvelope,
  faClipboardList,
  faNoteSticky,
  faLocationDot,
  faRoad,
  faUserTie,
  faFlag,
  faSitemap,
  faExclamationTriangle,
} from '@fortawesome/free-solid-svg-icons'

import type { ColumnMapping, RowValidationResult } from '../types'

interface InjectPreviewCardProps {
  /** The row data to preview */
  rowData: RowValidationResult
  /** Column mappings to understand field names */
  mappings: ColumnMapping[]
  /** Row index (0-based) for display */
  rowIndex: number
}

// Helper to get mapped value
const getValue = (values: Record<string, unknown>, field: string): string | null => {
  const value = values[field]
  if (value === null || value === undefined || value === '') return null
  return String(value)
}

// Priority display config
const priorityLabels: Record<number, string> = {
  1: 'Critical',
  2: 'High',
  3: 'Medium',
  4: 'Low',
  5: 'Info',
}

const priorityColors: Record<number, string> = {
  1: 'error.main',
  2: 'warning.main',
  3: 'info.main',
  4: 'text.secondary',
  5: 'text.disabled',
}

export const InjectPreviewCard = ({
  rowData,
  mappings: _mappings,
  rowIndex,
}: InjectPreviewCardProps) => {
  const theme = useTheme()
  const { values, status, issues } = rowData

  // Extract values from the mapped data
  const injectNumber = getValue(values, 'InjectNumber')
  const title = getValue(values, 'Title')
  const description = getValue(values, 'Description')
  const scheduledTime = getValue(values, 'ScheduledTime')
  const scenarioDay = getValue(values, 'ScenarioDay')
  const scenarioTime = getValue(values, 'ScenarioTime')
  const target = getValue(values, 'Target')
  const source = getValue(values, 'Source')
  const deliveryMethod = getValue(values, 'DeliveryMethod')
  const track = getValue(values, 'Track')
  const phase = getValue(values, 'Phase')
  const priority = getValue(values, 'Priority')
  const locationName = getValue(values, 'LocationName')
  const locationType = getValue(values, 'LocationType')
  const responsibleController = getValue(values, 'ResponsibleController')
  const expectedAction = getValue(values, 'ExpectedAction')
  const notes = getValue(values, 'Notes')
  const injectType = getValue(values, 'InjectType')
  const triggerType = getValue(values, 'TriggerType')

  const priorityNum = priority ? parseInt(priority, 10) : null

  // Get status color
  const getStatusColor = () => {
    switch (status) {
      case 'Valid':
        return 'success'
      case 'Warning':
        return 'warning'
      case 'Error':
        return 'error'
      default:
        return 'default'
    }
  }

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      {/* Header */}
      <Stack direction="row" justifyContent="space-between" alignItems="flex-start" sx={{ mb: 2 }}>
        <Box sx={{ flex: 1 }}>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
            <Typography variant="subtitle2" color="text.secondary">
              Preview Row {rowIndex + 1}
            </Typography>
            <Chip
              size="small"
              label={status}
              color={getStatusColor()}
              variant="outlined"
            />
            {injectType && (
              <Chip size="small" label={injectType} variant="outlined" />
            )}
          </Stack>
          <Typography variant="h6" fontWeight={500}>
            {injectNumber && `#${injectNumber} - `}
            {title || <em style={{ color: theme.palette.neutral[400] }}>No title mapped</em>}
          </Typography>
        </Box>
      </Stack>

      {/* Validation Issues */}
      {issues && issues.length > 0 && (
        <Box sx={{ mb: 2, p: 1.5, bgcolor: status === 'Error' ? 'error.light' : 'warning.light', borderRadius: 1 }}>
          <Stack spacing={0.5}>
            {issues.map((issue, idx) => (
              <Stack key={idx} direction="row" spacing={1} alignItems="center">
                <FontAwesomeIcon
                  icon={faExclamationTriangle}
                  style={{ color: issue.severity === 'Error' ? theme.palette.roleColor.exerciseDirector : theme.palette.semantic.warning, fontSize: '0.875rem' }}
                />
                <Typography variant="body2">
                  <strong>{issue.field}:</strong> {issue.message}
                </Typography>
              </Stack>
            ))}
          </Stack>
        </Box>
      )}

      <Stack spacing={2}>
        {/* Time Information */}
        {(scheduledTime || scenarioDay || phase) && (
          <Box>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              <FontAwesomeIcon icon={faClock} style={{ marginRight: 8 }} />
              Timing
            </Typography>
            <Stack spacing={0.5} sx={{ pl: 3 }}>
              {scheduledTime && (
                <Stack direction="row" spacing={2}>
                  <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                    Scheduled:
                  </Typography>
                  <Typography variant="body2" fontFamily="monospace">
                    {scheduledTime}
                  </Typography>
                </Stack>
              )}
              {scenarioDay && (
                <Stack direction="row" spacing={2}>
                  <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                    Scenario:
                  </Typography>
                  <Typography variant="body2">
                    Day {scenarioDay}
                    {scenarioTime && ` at ${scenarioTime}`}
                  </Typography>
                </Stack>
              )}
              {phase && (
                <Stack direction="row" spacing={2}>
                  <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                    Phase:
                  </Typography>
                  <Chip label={phase} size="small" />
                </Stack>
              )}
            </Stack>
          </Box>
        )}

        {/* Target & Source */}
        {(target || source || deliveryMethod || track) && (
          <>
            <Divider />
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                <FontAwesomeIcon icon={faBullseye} style={{ marginRight: 8 }} />
                Delivery
              </Typography>
              <Stack spacing={0.5} sx={{ pl: 3 }}>
                {target && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                      Target:
                    </Typography>
                    <Typography variant="body2" fontWeight={500}>
                      {target}
                    </Typography>
                  </Stack>
                )}
                {source && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                      Source:
                    </Typography>
                    <Typography variant="body2">
                      {source}
                    </Typography>
                  </Stack>
                )}
                {deliveryMethod && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                      Method:
                    </Typography>
                    <Chip
                      icon={<FontAwesomeIcon icon={faEnvelope} />}
                      label={deliveryMethod}
                      size="small"
                      variant="outlined"
                    />
                  </Stack>
                )}
                {track && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                      Track:
                    </Typography>
                    <Chip
                      icon={<FontAwesomeIcon icon={faRoad} />}
                      label={track}
                      size="small"
                      variant="outlined"
                    />
                  </Stack>
                )}
              </Stack>
            </Box>
          </>
        )}

        {/* Organization */}
        {(responsibleController || locationName || priorityNum) && (
          <>
            <Divider />
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                <FontAwesomeIcon icon={faSitemap} style={{ marginRight: 8 }} />
                Organization
              </Typography>
              <Stack spacing={0.5} sx={{ pl: 3 }}>
                {responsibleController && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                      Controller:
                    </Typography>
                    <Stack direction="row" spacing={1} alignItems="center">
                      <FontAwesomeIcon icon={faUserTie} style={{ fontSize: '0.75rem', color: 'rgba(0,0,0,0.54)' }} />
                      <Typography variant="body2">
                        {responsibleController}
                      </Typography>
                    </Stack>
                  </Stack>
                )}
                {locationName && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                      Location:
                    </Typography>
                    <Stack direction="row" spacing={1} alignItems="center">
                      <FontAwesomeIcon icon={faLocationDot} style={{ fontSize: '0.75rem', color: 'rgba(0,0,0,0.54)' }} />
                      <Typography variant="body2">
                        {locationName}
                        {locationType && ` (${locationType})`}
                      </Typography>
                    </Stack>
                  </Stack>
                )}
                {priorityNum && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 90 }}>
                      Priority:
                    </Typography>
                    <Stack direction="row" spacing={1} alignItems="center">
                      <FontAwesomeIcon icon={faFlag} style={{ fontSize: '0.75rem', color: 'rgba(0,0,0,0.54)' }} />
                      <Typography variant="body2" sx={{ color: priorityColors[priorityNum] || 'text.secondary' }}>
                        {priorityNum} - {priorityLabels[priorityNum] || 'Unknown'}
                      </Typography>
                    </Stack>
                  </Stack>
                )}
              </Stack>
            </Box>
          </>
        )}

        {/* Description */}
        <Divider />
        <Box>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            <FontAwesomeIcon icon={faClipboardList} style={{ marginRight: 8 }} />
            Description
          </Typography>
          <Typography
            variant="body2"
            sx={{
              pl: 3,
              whiteSpace: 'pre-wrap',
              backgroundColor: 'action.hover',
              p: 1.5,
              borderRadius: 1,
            }}
          >
            {description || <em style={{ color: theme.palette.neutral[400] }}>No description mapped</em>}
          </Typography>
        </Box>

        {/* Expected Action */}
        {expectedAction && (
          <>
            <Divider />
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                <FontAwesomeIcon icon={faUser} style={{ marginRight: 8 }} />
                Expected Action
              </Typography>
              <Typography
                variant="body2"
                sx={{ pl: 3, whiteSpace: 'pre-wrap' }}
              >
                {expectedAction}
              </Typography>
            </Box>
          </>
        )}

        {/* Controller Notes */}
        {notes && (
          <>
            <Divider />
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                <FontAwesomeIcon icon={faNoteSticky} style={{ marginRight: 8 }} />
                Controller Notes
              </Typography>
              <Typography
                variant="body2"
                sx={{
                  pl: 3,
                  whiteSpace: 'pre-wrap',
                  fontStyle: 'italic',
                  color: 'text.secondary',
                }}
              >
                {notes}
              </Typography>
            </Box>
          </>
        )}

        {/* Additional metadata */}
        {triggerType && (
          <>
            <Divider />
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                <FontAwesomeIcon icon={faCalendarDay} style={{ marginRight: 8 }} />
                Trigger
              </Typography>
              <Typography variant="body2" sx={{ pl: 3 }}>
                {triggerType}
              </Typography>
            </Box>
          </>
        )}
      </Stack>
    </Paper>
  )
}

export default InjectPreviewCard
