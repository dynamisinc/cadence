import type { Annotation } from '../types'

/**
 * Parse annotations from JSON string
 * Returns empty array if JSON is invalid or empty
 */
export function parseAnnotationsJson(json: string | null | undefined): Annotation[] {
  if (!json) return []
  try {
    const parsed = JSON.parse(json)
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}
