/**
 * ObservationCapabilitySelector Component
 *
 * Multi-select capability tagging for observations (S05).
 * Shows target capabilities prominently and other capabilities separately.
 * Used in observation creation and editing forms.
 */

import { useMemo } from 'react'
import { Box, Typography, Stack, Chip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faShieldAlt, faStar } from '@fortawesome/free-solid-svg-icons'
import type { CapabilityDto } from '@/features/capabilities/types'

interface ObservationCapabilitySelectorProps {
  /** All available capabilities for the organization */
  capabilities: CapabilityDto[]
  /** IDs of target capabilities for this exercise (shown prominently) */
  targetCapabilityIds: string[]
  /** Currently selected capability IDs */
  selectedIds: string[]
  /** Called when selection changes */
  onChange: (ids: string[]) => void
  /** Whether the selector is disabled */
  disabled?: boolean
}

export const ObservationCapabilitySelector = ({
  capabilities,
  targetCapabilityIds,
  selectedIds,
  onChange,
  disabled = false,
}: ObservationCapabilitySelectorProps) => {
  // Split into target and other capabilities
  const { targetCapabilities, otherCapabilities } = useMemo(() => {
    const target = capabilities.filter(c => targetCapabilityIds.includes(c.id))
    const other = capabilities.filter(c => !targetCapabilityIds.includes(c.id))
    return { targetCapabilities: target, otherCapabilities: other }
  }, [capabilities, targetCapabilityIds])

  const handleToggle = (capabilityId: string) => {
    if (disabled) return

    if (selectedIds.includes(capabilityId)) {
      onChange(selectedIds.filter(id => id !== capabilityId))
    } else {
      onChange([...selectedIds, capabilityId])
    }
  }

  return (
    <Box>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <FontAwesomeIcon icon={faShieldAlt} />
        <Typography variant="subtitle2" fontWeight="bold">
          Capability Tags
        </Typography>
        <Typography variant="caption" color="text.secondary">
          (optional)
        </Typography>
      </Stack>

      {/* Target Capabilities Section */}
      {targetCapabilities.length > 0 && (
        <Box sx={{ mb: 2 }}>
          <Stack direction="row" spacing={0.5} alignItems="center" sx={{ mb: 1 }}>
            <FontAwesomeIcon icon={faStar} size="sm" color="#FFD700" />
            <Typography variant="caption" fontWeight="medium">
              Target Capabilities
            </Typography>
          </Stack>
          <Stack direction="row" flexWrap="wrap" gap={0.5}>
            {targetCapabilities.map(cap => (
              <Chip
                key={cap.id}
                label={cap.name}
                onClick={disabled ? undefined : () => handleToggle(cap.id)}
                color={selectedIds.includes(cap.id) ? 'primary' : 'default'}
                variant={selectedIds.includes(cap.id) ? 'filled' : 'outlined'}
                size="small"
                disabled={disabled}
              />
            ))}
          </Stack>
        </Box>
      )}

      {/* Other Capabilities Section */}
      {otherCapabilities.length > 0 && (
        <Box>
          <Typography variant="caption" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
            Other Capabilities
          </Typography>
          <Stack direction="row" flexWrap="wrap" gap={0.5}>
            {otherCapabilities.map(cap => (
              <Chip
                key={cap.id}
                label={cap.name}
                onClick={disabled ? undefined : () => handleToggle(cap.id)}
                color={selectedIds.includes(cap.id) ? 'secondary' : 'default'}
                variant={selectedIds.includes(cap.id) ? 'filled' : 'outlined'}
                size="small"
                disabled={disabled}
              />
            ))}
          </Stack>
        </Box>
      )}

      {capabilities.length === 0 && (
        <Typography variant="body2" color="text.secondary">
          No capabilities available for tagging.
        </Typography>
      )}
    </Box>
  )
}

export default ObservationCapabilitySelector
