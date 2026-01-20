/**
 * SortableInjectRow Component
 *
 * Wrapper for InjectRow that adds drag-and-drop functionality.
 * Uses @dnd-kit/sortable for reordering with visual feedback.
 *
 * @module features/injects/drag-drop
 * @see S10-sequence-drag-drop-reorder
 */

import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { TableRow, TableCell } from '@mui/material'
import type { InjectDto } from '../../types'
import { DragHandle } from './DragHandle'

interface SortableInjectRowProps {
  /** The inject to display */
  inject: InjectDto
  /** Disable dragging */
  disabled?: boolean
  /** Child content (inject row cells) */
  children: React.ReactNode
}

export const SortableInjectRow = ({
  inject,
  disabled = false,
  children,
}: SortableInjectRowProps) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: inject.id,
    disabled,
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
    zIndex: isDragging ? 1000 : 'auto',
  }

  return (
    <TableRow
      ref={setNodeRef}
      style={style}
      sx={{
        backgroundColor: isDragging ? 'action.selected' : 'inherit',
        '&:hover': {
          backgroundColor: isDragging ? 'action.selected' : 'action.hover',
        },
      }}
    >
      {/* Drag Handle Cell */}
      <TableCell sx={{ width: 40, p: 0 }}>
        <DragHandle attributes={attributes} listeners={listeners} disabled={disabled} />
      </TableCell>

      {/* Inject Row Content */}
      {children}
    </TableRow>
  )
}

export default SortableInjectRow
