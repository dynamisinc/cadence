/**
 * CapabilityList Component
 *
 * Displays capabilities grouped by category with collapsible sections.
 * Provides edit and deactivate actions for each capability.
 */

import { useState } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Stack,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faChevronDown,
  faPen,
  faTrash,
  faCircleInfo,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraSecondaryButton,
  CobraDeleteButton,
} from '@/theme/styledComponents'
import type { CapabilityDto } from '../types'
import { groupCapabilitiesByCategory } from '../types'

interface CapabilityListProps {
  /** List of capabilities to display */
  capabilities: CapabilityDto[]
  /** Called when edit button is clicked */
  onEdit: (capability: CapabilityDto) => void
  /** Called when deactivate is confirmed */
  onDeactivate: (id: string) => Promise<void>
  /** Whether deactivate action is in progress */
  isDeleting?: boolean
}

/**
 * Single capability card within a category
 */
const CapabilityCard: FC<{
  capability: CapabilityDto
  onEdit: () => void
  onDeactivate: () => void
  isDeleting?: boolean
}> = ({ capability, onEdit, onDeactivate, isDeleting }) => (
  <Box
    sx={{
      p: 2,
      mb: 1,
      borderRadius: 1,
      bgcolor: capability.isActive ? 'background.paper' : 'action.disabledBackground',
      border: '1px solid',
      borderColor: capability.isActive ? 'divider' : 'action.disabled',
      opacity: capability.isActive ? 1 : 0.7,
      '&:last-child': { mb: 0 },
    }}
  >
    <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
      <Box sx={{ flex: 1 }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
          <Typography variant="subtitle1" fontWeight={500}>
            {capability.name}
          </Typography>
          {!capability.isActive && (
            <Chip label="Inactive" size="small" color="default" variant="outlined" />
          )}
          {capability.sourceLibrary && (
            <Chip
              label={capability.sourceLibrary}
              size="small"
              variant="outlined"
              color="primary"
            />
          )}
        </Stack>
        {capability.description && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {capability.description}
          </Typography>
        )}
      </Box>
      <Stack direction="row" spacing={0.5}>
        <Tooltip title="Edit capability">
          <IconButton size="small" onClick={onEdit}>
            <FontAwesomeIcon icon={faPen} size="sm" />
          </IconButton>
        </Tooltip>
        {capability.isActive && (
          <Tooltip title="Deactivate capability">
            <IconButton
              size="small"
              onClick={onDeactivate}
              disabled={isDeleting}
              color="error"
            >
              <FontAwesomeIcon icon={faTrash} size="sm" />
            </IconButton>
          </Tooltip>
        )}
      </Stack>
    </Stack>
  </Box>
)

/**
 * Category accordion with capabilities
 */
const CategoryAccordion: FC<{
  category: string
  capabilities: CapabilityDto[]
  onEdit: (capability: CapabilityDto) => void
  onDeactivate: (id: string) => void
  isDeleting?: boolean
  defaultExpanded?: boolean
}> = ({ category, capabilities, onEdit, onDeactivate, isDeleting, defaultExpanded = true }) => {
  const activeCount = capabilities.filter(c => c.isActive).length
  const totalCount = capabilities.length

  return (
    <Accordion defaultExpanded={defaultExpanded}>
      <AccordionSummary expandIcon={<FontAwesomeIcon icon={faChevronDown} />}>
        <Stack direction="row" spacing={2} alignItems="center" sx={{ flex: 1, mr: 2 }}>
          <Typography fontWeight="medium">{category}</Typography>
          <Chip
            label={`${activeCount}${totalCount !== activeCount ? ` / ${totalCount}` : ''} capabilities`}
            size="small"
            variant="outlined"
          />
        </Stack>
      </AccordionSummary>
      <AccordionDetails sx={{ bgcolor: 'grey.50' }}>
        {capabilities
          .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name))
          .map(capability => (
            <CapabilityCard
              key={capability.id}
              capability={capability}
              onEdit={() => onEdit(capability)}
              onDeactivate={() => onDeactivate(capability.id)}
              isDeleting={isDeleting}
            />
          ))}
      </AccordionDetails>
    </Accordion>
  )
}

/**
 * CapabilityList Component
 *
 * Displays capabilities grouped by category with edit/deactivate actions.
 */
export const CapabilityList: FC<CapabilityListProps> = ({
  capabilities,
  onEdit,
  onDeactivate,
  isDeleting,
}) => {
  const [deactivateTarget, setDeactivateTarget] = useState<CapabilityDto | null>(null)

  const grouped = groupCapabilitiesByCategory(capabilities)
  const sortedCategories = Array.from(grouped.keys()).sort((a, b) => {
    // Put "Uncategorized" last
    if (a === 'Uncategorized') return 1
    if (b === 'Uncategorized') return -1
    return a.localeCompare(b)
  })

  const handleDeactivateClick = (id: string) => {
    const cap = capabilities.find(c => c.id === id)
    if (cap) {
      setDeactivateTarget(cap)
    }
  }

  const handleDeactivateConfirm = async () => {
    if (deactivateTarget) {
      await onDeactivate(deactivateTarget.id)
      setDeactivateTarget(null)
    }
  }

  if (capabilities.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 6 }}>
        <FontAwesomeIcon icon={faCircleInfo} size="3x" style={{ opacity: 0.3 }} />
        <Typography variant="h6" color="text.secondary" sx={{ mt: 2 }}>
          No capabilities defined
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          Add capabilities manually or import from a predefined library.
        </Typography>
      </Box>
    )
  }

  return (
    <>
      <Box>
        {sortedCategories.map((category, index) => (
          <CategoryAccordion
            key={category}
            category={category}
            capabilities={grouped.get(category) || []}
            onEdit={onEdit}
            onDeactivate={handleDeactivateClick}
            isDeleting={isDeleting}
            defaultExpanded={index < 3} // Expand first 3 categories by default
          />
        ))}
      </Box>

      {/* Deactivate Confirmation Dialog */}
      <Dialog
        open={!!deactivateTarget}
        onClose={() => setDeactivateTarget(null)}
        maxWidth="sm"
      >
        <DialogTitle>Deactivate Capability</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to deactivate{' '}
            <strong>{deactivateTarget?.name}</strong>?
          </DialogContentText>
          <DialogContentText sx={{ mt: 1, color: 'text.secondary' }}>
            This capability will be hidden from selection lists but preserved for
            historical data. You can view inactive capabilities using the filter.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={() => setDeactivateTarget(null)}>
            Cancel
          </CobraSecondaryButton>
          <CobraDeleteButton onClick={handleDeactivateConfirm} disabled={isDeleting}>
            Deactivate
          </CobraDeleteButton>
        </DialogActions>
      </Dialog>
    </>
  )
}

export default CapabilityList
