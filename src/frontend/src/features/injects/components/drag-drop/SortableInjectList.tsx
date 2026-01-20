/**
 * SortableInjectList Component
 *
 * Provides drag-and-drop context for reordering injects in the MSEL view.
 * Implements optimistic updates with rollback on error.
 * Disables reordering when exercise is Active.
 *
 * @module features/injects/drag-drop
 * @see S10-sequence-drag-drop-reorder
 */

import { useState, useEffect } from 'react'
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable'
import { toast } from 'react-toastify'
import type { InjectDto } from '../../types'

interface SortableInjectListProps {
  /** List of injects to display (should be ordered by sequence) */
  injects: InjectDto[]
  /** Called when injects are reordered - receives new order of inject IDs */
  onReorder: (injectIds: string[]) => Promise<void>
  /** Disable reordering (e.g., during Active exercise) */
  disabled?: boolean
  /** Child render function - receives ordered inject list */
  children: (injects: InjectDto[]) => React.ReactNode
}

export const SortableInjectList = ({
  injects,
  onReorder,
  disabled = false,
  children,
}: SortableInjectListProps) => {
  const [items, setItems] = useState(injects)
  const [isReordering, setIsReordering] = useState(false)

  // Update local state when props change (e.g., from SignalR updates)
  useEffect(() => {
    if (!isReordering) {
      setItems(injects)
    }
  }, [injects, isReordering])

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8, // Prevent accidental drags (8px threshold)
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  )

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event

    if (over && active.id !== over.id) {
      const oldIndex = items.findIndex(i => i.id === active.id)
      const newIndex = items.findIndex(i => i.id === over.id)

      // Optimistic update - reorder immediately for responsive UI
      const newItems = arrayMove(items, oldIndex, newIndex)
      setItems(newItems)

      try {
        setIsReordering(true)
        await onReorder(newItems.map(i => i.id))
      } catch (error) {
        // Rollback on failure
        setItems(injects)
        const message = error instanceof Error ? error.message : 'Failed to reorder injects'
        toast.error(message)
      } finally {
        setIsReordering(false)
      }
    }
  }

  if (disabled) {
    return <>{children(items)}</>
  }

  return (
    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
      <SortableContext items={items.map(i => i.id)} strategy={verticalListSortingStrategy}>
        {children(items)}
      </SortableContext>
    </DndContext>
  )
}

export default SortableInjectList
