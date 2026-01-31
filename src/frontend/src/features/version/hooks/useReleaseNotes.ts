import { useState } from 'react'
import type { ReleaseNote } from '../types'
import releaseNotesData from '../data/release-notes.json'

interface UseReleaseNotesResult {
  releaseNotes: ReleaseNote[];
  isLoading: boolean;
  error: Error | null;
}

// Release notes parsed from CHANGELOG.md at build time
const BUNDLED_RELEASE_NOTES: ReleaseNote[] = releaseNotesData

/**
 * Hook to access release notes.
 * Returns bundled release notes for offline support.
 */
export function useReleaseNotes(): UseReleaseNotesResult {
  const [releaseNotes] = useState<ReleaseNote[]>(BUNDLED_RELEASE_NOTES)
  const [isLoading] = useState(false)
  const [error] = useState<Error | null>(null)

  return { releaseNotes, isLoading, error }
}

/**
 * Get release notes for a specific version.
 */
export function getReleaseNotesForVersion(
  releaseNotes: ReleaseNote[],
  version: string,
): ReleaseNote | undefined {
  return releaseNotes.find(note => note.version === version)
}
