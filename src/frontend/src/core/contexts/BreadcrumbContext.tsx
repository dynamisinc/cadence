/**
 * BreadcrumbContext - Allows pages to set custom breadcrumb items
 *
 * Pages can use the useBreadcrumbs hook to set custom breadcrumb items
 * that override the auto-generated ones. This is useful for showing
 * dynamic content like exercise names instead of IDs.
 *
 * @module core/contexts
 */

import { createContext, useContext, useState, useCallback, useEffect, useRef } from 'react'
import type { ReactNode } from 'react'
import type { BreadcrumbItem } from '../components/navigation/Breadcrumb'

interface BreadcrumbContextValue {
  breadcrumbs: BreadcrumbItem[] | undefined
  setBreadcrumbs: (items: BreadcrumbItem[] | undefined) => void
}

const BreadcrumbContext = createContext<BreadcrumbContextValue | undefined>(undefined)

interface BreadcrumbProviderProps {
  children: ReactNode
}

/**
 * Provider component for breadcrumb context
 */
export const BreadcrumbProvider = ({ children }: BreadcrumbProviderProps) => {
  const [breadcrumbs, setBreadcrumbs] = useState<BreadcrumbItem[] | undefined>(undefined)

  const updateBreadcrumbs = useCallback((items: BreadcrumbItem[] | undefined) => {
    setBreadcrumbs(items)
  }, [])

  return (
    <BreadcrumbContext.Provider value={{ breadcrumbs, setBreadcrumbs: updateBreadcrumbs }}>
      {children}
    </BreadcrumbContext.Provider>
  )
}

/**
 * Hook to access breadcrumb context
 */
export const useBreadcrumbContext = (): BreadcrumbContextValue => {
  const context = useContext(BreadcrumbContext)
  if (!context) {
    throw new Error('useBreadcrumbContext must be used within BreadcrumbProvider')
  }
  return context
}

/**
 * Hook for pages to set custom breadcrumbs
 *
 * @param items - Breadcrumb items to display, or undefined to use auto-generation
 *
 * @example
 * ```tsx
 * const ExerciseDetailPage = () => {
 *   const { exercise } = useExercise(id)
 *
 *   useBreadcrumbs(exercise ? [
 *     { label: 'Home', path: '/', icon: faHome },
 *     { label: 'Exercises', path: '/exercises' },
 *     { label: exercise.name },
 *   ] : undefined)
 *
 *   return <div>...</div>
 * }
 * ```
 */
export const useBreadcrumbs = (items: BreadcrumbItem[] | undefined) => {
  const { setBreadcrumbs } = useBreadcrumbContext()
  const prevItemsRef = useRef<string | undefined>(undefined)

  // Serialize items for comparison (ignoring icon functions which can't be compared)
  const serializedItems = items
    ? JSON.stringify(items.map(({ label, path }) => ({ label, path })))
    : undefined

  // Update breadcrumbs only when content actually changes
  useEffect(() => {
    if (prevItemsRef.current !== serializedItems) {
      prevItemsRef.current = serializedItems
      setBreadcrumbs(items)
    }
  }, [serializedItems, items, setBreadcrumbs])

  // Clean up when component unmounts
  useEffect(() => {
    return () => {
      prevItemsRef.current = undefined
      setBreadcrumbs(undefined)
    }
  }, [setBreadcrumbs])
}
