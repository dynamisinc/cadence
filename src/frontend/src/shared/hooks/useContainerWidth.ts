/**
 * useContainerWidth Hook
 *
 * Measures the actual width of a container element using ResizeObserver.
 * Returns the width so components can make responsive decisions based on
 * real available space rather than viewport width (which doesn't account
 * for sidebars, panels, etc.).
 *
 * Updates are debounced to avoid causing re-renders during CSS transitions
 * (e.g., sidebar expand/collapse animations).
 */

import { useState, useEffect, useRef, type RefObject } from 'react'

const DEBOUNCE_MS = 150

export function useContainerWidth(ref: RefObject<HTMLElement | null>): number {
  const [width, setWidth] = useState(0)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    const el = ref.current
    if (!el) return

    // Set initial width synchronously (no debounce on mount)
    setWidth(el.offsetWidth)

    const observer = new ResizeObserver((entries) => {
      // Debounce: only update state after resizing settles
      if (timerRef.current) {
        clearTimeout(timerRef.current)
      }
      timerRef.current = setTimeout(() => {
        for (const entry of entries) {
          if (entry.borderBoxSize?.length) {
            setWidth(entry.borderBoxSize[0].inlineSize)
          } else {
            setWidth(entry.contentRect.width)
          }
        }
      }, DEBOUNCE_MS)
    })

    observer.observe(el)
    return () => {
      observer.disconnect()
      if (timerRef.current) {
        clearTimeout(timerRef.current)
      }
    }
  }, [ref])

  return width
}
