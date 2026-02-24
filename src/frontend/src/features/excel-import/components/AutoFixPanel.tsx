/**
 * AutoFixPanel Component
 *
 * Displays bulk auto-fix suggestions above the validation table.
 * Each suggestion shows the issue pattern and a one-click fix button.
 */

import { Typography, Stack, Paper } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faWandMagicSparkles } from '@fortawesome/free-solid-svg-icons'

import { CobraPrimaryButton } from '../../../theme/styledComponents'
import type { AutoFixSuggestion } from '../types'

interface AutoFixPanelProps {
  /** Available fix suggestions */
  suggestions: AutoFixSuggestion[]
  /** Called when user clicks a fix button */
  onApplyFix: (suggestion: AutoFixSuggestion) => void
  /** Whether a fix is currently being applied */
  isApplying: boolean
}

export const AutoFixPanel = ({ suggestions, onApplyFix, isApplying }: AutoFixPanelProps) => {
  if (suggestions.length === 0) return null

  return (
    <Paper
      variant="outlined"
      sx={{
        p: 2,
        mb: 2,
        backgroundColor: 'info.light',
        borderColor: 'info.main',
      }}
    >
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={1} alignItems="center">
          <FontAwesomeIcon icon={faWandMagicSparkles} />
          <Typography variant="subtitle2">Quick Fixes Available</Typography>
        </Stack>

        {suggestions.map(suggestion => (
          <Stack
            key={suggestion.id}
            direction="row"
            spacing={2}
            alignItems="center"
            justifyContent="space-between"
          >
            <Typography variant="body2">
              <strong>{suggestion.description}</strong>
              {' \u2192 '}
              {suggestion.action}
            </Typography>
            <CobraPrimaryButton
              size="small"
              onClick={() => onApplyFix(suggestion)}
              disabled={isApplying}
            >
              Fix ({suggestion.affectedRows})
            </CobraPrimaryButton>
          </Stack>
        ))}
      </Stack>
    </Paper>
  )
}

export default AutoFixPanel
