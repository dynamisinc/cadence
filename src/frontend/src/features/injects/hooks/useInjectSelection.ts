/**
 * useInjectSelection Hook
 *
 * Manages selection state for inject checkboxes in the MSEL view (S12).
 * Provides select/deselect functionality with support for select-all.
 *
 * @module features/injects/hooks
 */

import { useState, useMemo, useCallback } from 'react'
import type { InjectDto } from '../types'

interface UseInjectSelectionOptions {
  /** List of injects to manage selection for */
  injects: InjectDto[]
  /** Callback when selection changes */
  onSelectionChange?: (selectedIds: string[]) => void
}

interface UseInjectSelectionResult {
  /** Array of currently selected inject IDs */
  selectedIds: string[]
  /** Toggle selection for a single inject */
  toggleSelection: (id: string) => void
  /** Select all visible injects */
  selectAll: () => void
  /** Clear all selections */
  clearSelection: () => void
  /** Check if a specific inject is selected */
  isSelected: (id: string) => boolean
  /** Current selection state: 'none' | 'some' | 'all' */
  selectionState: 'none' | 'some' | 'all'
  /** Count of selected injects */
  selectedCount: number
}

/**
 * Hook for managing inject selection state
 *
 * @example
 * const {
 *   selectedIds,
 *   toggleSelection,
 *   selectAll,
 *   clearSelection,
 *   isSelected,
 *   selectionState,
 * } = useInjectSelection({ injects })
 */
export const useInjectSelection = ({
  injects,
  onSelectionChange,
}: UseInjectSelectionOptions): UseInjectSelectionResult => {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  const toggleSelection = useCallback(
    (id: string) => {
      setSelectedIds((prev) => {
        const next = new Set(prev)
        if (next.has(id)) {
          next.delete(id)
        } else {
          next.add(id)
        }
        onSelectionChange?.(Array.from(next))
        return next
      })
    },
    [onSelectionChange],
  )

  const selectAll = useCallback(() => {
    const allIds = new Set(injects.map((i) => i.id))
    setSelectedIds(allIds)
    onSelectionChange?.(Array.from(allIds))
  }, [injects, onSelectionChange])

  const clearSelection = useCallback(() => {
    setSelectedIds(new Set())
    onSelectionChange?.([])
  }, [onSelectionChange])

  const isSelected = useCallback(
    (id: string) => selectedIds.has(id),
    [selectedIds],
  )

  const selectionState = useMemo(() => {
    if (selectedIds.size === 0) return 'none'
    if (injects.length > 0 && selectedIds.size === injects.length) return 'all'
    return 'some'
  }, [selectedIds.size, injects.length])

  return {
    selectedIds: Array.from(selectedIds),
    toggleSelection,
    selectAll,
    clearSelection,
    isSelected,
    selectionState,
    selectedCount: selectedIds.size,
  }
}

export default useInjectSelection
