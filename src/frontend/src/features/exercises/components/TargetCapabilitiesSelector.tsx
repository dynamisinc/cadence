/**
 * TargetCapabilitiesSelector - Multi-select for exercise target capabilities
 *
 * Allows Exercise Directors to select which capabilities the exercise will evaluate.
 * Capabilities are grouped by category and displayed as chips.
 * Implements S04 acceptance criteria.
 *
 * @module features/exercises
 * @see docs/features/exercise-capabilities/S04-exercise-target-capabilities.md
 */

import { useMemo, useState } from 'react'
import { Box, Typography, Chip, Stack, Paper, Alert, Collapse } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faShieldAlt, faXmark, faLightbulb, faChevronDown, faChevronRight } from '@fortawesome/free-solid-svg-icons'
import { useCapabilities } from '../../capabilities/hooks/useCapabilities'
import { groupCapabilitiesByCategory } from '../../capabilities/types'
import type { CapabilityDto } from '../../capabilities/types'
import { CobraIconButton } from '@/theme/styledComponents'

interface TargetCapabilitiesSelectorProps {
  /** Organization ID (for multi-tenant support) */
  organizationId: string
  /** Array of selected capability IDs */
  selectedIds: string[]
  /** Called when selection changes */
  onChange: (ids: string[]) => void
  /** Disable all interactions */
  disabled?: boolean
}

/**
 * Target Capabilities Selector Component
 *
 * Features:
 * - Groups capabilities by category
 * - Shows selected capabilities as removable chips
 * - Displays remaining capabilities by category for selection
 * - Shows HSEEP guidance about focusing on 3-5 capabilities
 * - Empty state when no capabilities available
 */
export const TargetCapabilitiesSelector = ({
  organizationId: _organizationId,
  selectedIds,
  onChange,
  disabled = false,
}: TargetCapabilitiesSelectorProps) => {
  const [expanded, setExpanded] = useState(false)
  const { capabilities, loading } = useCapabilities(false)

  // Group capabilities by category
  const groupedCapabilities = useMemo(() => {
    if (!capabilities) return new Map<string, CapabilityDto[]>()
    return groupCapabilitiesByCategory(capabilities)
  }, [capabilities])

  // Get selected capability objects
  const selectedCapabilities = useMemo(() => {
    if (!capabilities) return []
    return capabilities.filter(c => selectedIds.includes(c.id))
  }, [capabilities, selectedIds])

  /**
   * Toggle a capability selection
   */
  const handleToggle = (capabilityId: string) => {
    if (disabled) return

    if (selectedIds.includes(capabilityId)) {
      onChange(selectedIds.filter(id => id !== capabilityId))
    } else {
      onChange([...selectedIds, capabilityId])
    }
  }

  /**
   * Remove a selected capability
   */
  const handleRemove = (capabilityId: string) => {
    if (disabled) return
    onChange(selectedIds.filter(id => id !== capabilityId))
  }

  // Auto-expand when capabilities are already selected (edit mode)
  const isExpanded = expanded || selectedCapabilities.length > 0

  return (
    <Paper elevation={0} sx={{ p: 1.5, border: 1, borderColor: 'divider' }}>
      <Stack
        direction="row"
        spacing={1}
        alignItems="center"
        sx={{ cursor: 'pointer' }}
        onClick={() => setExpanded(prev => !prev)}
      >
        <CobraIconButton size="small" aria-label={isExpanded ? 'Collapse' : 'Expand'}>
          <FontAwesomeIcon icon={isExpanded ? faChevronDown : faChevronRight} size="sm" />
        </CobraIconButton>
        <FontAwesomeIcon icon={faShieldAlt} />
        <Typography variant="subtitle1" fontWeight="bold">
          Target Capabilities
        </Typography>
        <Typography variant="body2" color="text.secondary">
          ({selectedIds.length} selected)
        </Typography>
      </Stack>

      <Collapse in={isExpanded}>
        <Box sx={{ mt: 1.5 }}>
          {/* Selected Capabilities - Show as removable chips at top */}
          {selectedCapabilities.length > 0 && (
            <Box sx={{ mb: 1.5 }}>
              <Typography variant="caption" color="text.secondary" sx={{ mb: 0.5, display: 'block' }}>
                Selected for evaluation:
              </Typography>
              <Stack direction="row" flexWrap="wrap" gap={0.5}>
                {selectedCapabilities.map(cap => (
                  <Chip
                    key={cap.id}
                    label={cap.name}
                    onDelete={disabled ? undefined : () => handleRemove(cap.id)}
                    deleteIcon={<FontAwesomeIcon icon={faXmark} />}
                    color="primary"
                    variant="filled"
                    size="small"
                  />
                ))}
              </Stack>
            </Box>
          )}

          {/* HSEEP Guidance */}
          <Alert severity="info" icon={<FontAwesomeIcon icon={faLightbulb} />} sx={{ mb: 1.5, py: 0.5 }}>
            <Typography variant="caption">
              HSEEP recommends focusing on 3-5 key capabilities per exercise
            </Typography>
          </Alert>

          {/* Available Capabilities - Grouped by category */}
          {Array.from(groupedCapabilities.entries()).map(([category, caps]) => (
            <Box key={category} sx={{ mb: 1.5 }}>
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ mb: 0.5, display: 'block', fontWeight: 600 }}
              >
                {category}
              </Typography>
              <Stack direction="row" flexWrap="wrap" gap={0.5}>
                {caps.map(cap => {
                  const isSelected = selectedIds.includes(cap.id)
                  return (
                    <Chip
                      key={cap.id}
                      label={cap.name}
                      onClick={disabled ? undefined : () => handleToggle(cap.id)}
                      color={isSelected ? 'primary' : 'default'}
                      variant={isSelected ? 'filled' : 'outlined'}
                      size="small"
                      disabled={disabled}
                      sx={{
                        cursor: disabled ? 'default' : 'pointer',
                      }}
                    />
                  )
                })}
              </Stack>
            </Box>
          ))}

          {/* Loading State */}
          {loading && (
            <Typography color="text.secondary" variant="body2">
              Loading capabilities...
            </Typography>
          )}

          {/* Empty State */}
          {!loading && !capabilities?.length && (
            <Typography color="text.secondary" variant="body2">
              No capabilities available. Import a library from Settings → Capability Library.
            </Typography>
          )}
        </Box>
      </Collapse>
    </Paper>
  )
}

export default TargetCapabilitiesSelector
