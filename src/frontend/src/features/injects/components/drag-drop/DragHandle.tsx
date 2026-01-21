/**
 * DragHandle Component
 *
 * A visual grip handle for drag-and-drop reordering.
 * Shows grip dots (vertical bars icon) that indicate draggability.
 * Disabled during Active exercise state.
 *
 * @module features/injects/drag-drop
 * @see S10-sequence-drag-drop-reorder
 */

import { Box } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faGripVertical } from '@fortawesome/free-solid-svg-icons'
import type { DraggableAttributes, DraggableSyntheticListeners } from '@dnd-kit/core'

interface DragHandleProps {
  /** Draggable attributes from @dnd-kit */
  attributes?: DraggableAttributes
  /** Draggable listeners from @dnd-kit */
  listeners?: DraggableSyntheticListeners
  /** Disable dragging (e.g., during Active exercise) */
  disabled?: boolean
}

export const DragHandle = ({ attributes, listeners, disabled = false }: DragHandleProps) => {
  if (disabled) {
    return null
  }

  return (
    <Box
      {...attributes}
      {...listeners}
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        px: 1,
        cursor: disabled ? 'default' : 'grab',
        color: 'text.disabled',
        transition: 'color 0.2s',
        '&:hover': {
          color: disabled ? 'text.disabled' : 'text.secondary',
        },
        '&:active': {
          cursor: disabled ? 'default' : 'grabbing',
        },
      }}
      aria-label="Drag to reorder inject"
      role="button"
      tabIndex={0}
    >
      <FontAwesomeIcon icon={faGripVertical} />
    </Box>
  )
}

export default DragHandle
