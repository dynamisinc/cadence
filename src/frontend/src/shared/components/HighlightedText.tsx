/**
 * HighlightedText Component
 *
 * Renders text with search matches highlighted.
 */

import { Box } from '@mui/material'
import { findMatchIndices } from '../../features/injects/utils/searchUtils'

export interface HighlightedTextProps {
  /** The text to display */
  text: string
  /** The search term to highlight */
  searchTerm: string
  /** Color for the highlight (default: warning.light) */
  highlightColor?: string
}

export const HighlightedText = ({
  text,
  searchTerm,
  highlightColor = 'warning.light',
}: HighlightedTextProps) => {
  // No search term, render plain text
  if (!searchTerm || !searchTerm.trim()) {
    return <>{text}</>
  }

  // Find match indices
  const matches = findMatchIndices(text, searchTerm)

  // No matches, render plain text
  if (matches.length === 0) {
    return <>{text}</>
  }

  // Build segments with highlights
  const segments: React.ReactNode[] = []
  let lastIndex = 0

  matches.forEach(([start, end], index) => {
    // Add text before match
    if (start > lastIndex) {
      segments.push(
        <span key={`text-${index}`}>{text.slice(lastIndex, start)}</span>,
      )
    }

    // Add highlighted match
    segments.push(
      <Box
        key={`match-${index}`}
        component="mark"
        sx={{
          backgroundColor: highlightColor,
          px: 0.25,
          borderRadius: 0.5,
        }}
      >
        {text.slice(start, end)}
      </Box>,
    )

    lastIndex = end
  })

  // Add remaining text after last match
  if (lastIndex < text.length) {
    segments.push(<span key="text-end">{text.slice(lastIndex)}</span>)
  }

  return <>{segments}</>
}

export default HighlightedText
