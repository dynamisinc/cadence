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

import { useState, useEffect, useRef } from 'react'
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
import { Box, Typography, Portal } from '@mui/material'
import { notify } from '@/shared/utils/notify'
import type { InjectDto } from '../../types'

/** Minimum time to show the saving indicator (ms) */
const MIN_INDICATOR_TIME = 1000

interface SortableInjectListProps {
  /** List of injects to display (should be ordered by sequence) */
  injects: InjectDto[]
  /** Called when injects are reordered - receives new order of inject IDs */
  onReorder: (injectIds: string[]) => Promise<void>
  /** Disable reordering (e.g., during Active exercise) */
  disabled?: boolean
  /** Child render function - receives ordered inject list */
  children: (injects: InjectDto[]) => React.ReactNode
  /** External control: whether saving indicator is shown (optional) */
  showSavingIndicator?: boolean
  /** External control: callback when saving state changes (optional) */
  onSavingChange?: (isSaving: boolean) => void
}

export const SortableInjectList = ({
  injects,
  onReorder,
  disabled = false,
  children,
  showSavingIndicator,
  onSavingChange,
}: SortableInjectListProps) => {
  const [items, setItems] = useState(injects)
  // Only use internal state if no external control is provided
  const [internalShowSaving, setInternalShowSaving] = useState(false)
  const savingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Use external state if provided, otherwise use internal state
  const showSaving = showSavingIndicator !== undefined ? showSavingIndicator : internalShowSaving
  const setShowSaving = onSavingChange !== undefined ? onSavingChange : setInternalShowSaving

  // Update local state when props change (e.g., from SignalR updates)
  useEffect(() => {
    if (!showSaving) {
      setItems(injects)
    }
  }, [injects, showSaving])

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (savingTimeoutRef.current) {
        clearTimeout(savingTimeoutRef.current)
      }
    }
  }, [])

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

      // Clear any existing timeout
      if (savingTimeoutRef.current) {
        clearTimeout(savingTimeoutRef.current)
        savingTimeoutRef.current = null
      }

      // Show saving indicator with minimum display time
      const startTime = Date.now()
      setShowSaving(true)

      try {
        await onReorder(newItems.map(i => i.id))
      } catch (error) {
        // Rollback on failure
        setItems(injects)
        const message = error instanceof Error ? error.message : 'Failed to reorder injects'
        notify.error(message)
      }

      // Hide indicator after minimum time (don't use finally to avoid race conditions)
      const elapsed = Date.now() - startTime
      const remaining = Math.max(0, MIN_INDICATOR_TIME - elapsed)
      savingTimeoutRef.current = setTimeout(() => {
        setShowSaving(false)
        savingTimeoutRef.current = null
      }, remaining)
    }
  }

  if (disabled) {
    return <>{children(items)}</>
  }

  return (
    <>
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <SortableContext items={items.map(i => i.id)} strategy={verticalListSortingStrategy}>
          {children(items)}
        </SortableContext>
      </DndContext>

      {/* Saving indicator - only render if using internal state (not externally controlled) */}
      {showSaving && showSavingIndicator === undefined && (
        <Portal>
          <Box
            sx={{
              position: 'fixed',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'rgba(255, 255, 255, 0.6)',
              backdropFilter: 'blur(2px)',
              zIndex: 1300,
            }}
          >
            <Box
              sx={{
                bgcolor: 'background.paper',
                border: '1px solid',
                borderColor: 'divider',
                px: 3,
                py: 2,
                borderRadius: 2,
                boxShadow: '0 4px 20px rgba(0, 0, 0, 0.15)',
              }}
            >
              <Typography variant="body1">
                Saving changes...
              </Typography>
            </Box>
          </Box>
        </Portal>
      )}
    </>
  )
}

export default SortableInjectList
