/**
 * HelpTooltip Component
 *
 * Contextual help for features and HSEEP concepts.
 * Renders a FontAwesome info icon that shows help content.
 *
 * Two modes:
 * - Compact: MUI Tooltip on hover (short one-liners)
 * - Full (default): MUI Popover on click (detailed explanations with glossary)
 */

import { useState, type FC, type MouseEvent } from 'react'
import { Box, Popover, Stack, Tooltip, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCircleInfo } from '@fortawesome/free-solid-svg-icons'

import {
  CONTEXTUAL_HELP,
  HSEEP_GLOSSARY,
  type ContextualHelp,
} from '../constants/hseepGlossary'
import CobraStyles from '../../theme/CobraStyles'

export interface HelpTooltipProps {
  /** Key into CONTEXTUAL_HELP registry */
  helpKey?: string
  /** Inline summary (overrides helpKey) */
  summary?: string
  /** Inline details (overrides helpKey) */
  details?: string
  /** Related HSEEP glossary term keys */
  relatedTerms?: string[]
  /** Role-specific tips keyed by role name */
  roleTips?: Record<string, string>
  /** Current user's exercise role (for role-specific tips) */
  exerciseRole?: string
  /** Icon size */
  size?: 'sm' | 'lg' | '1x'
  /** Use simple MUI Tooltip on hover instead of Popover */
  compact?: boolean
}

function resolveContent(props: HelpTooltipProps): ContextualHelp | null {
  if (props.summary) {
    return {
      summary: props.summary,
      details: props.details,
      relatedTerms: props.relatedTerms,
      roleTips: props.roleTips,
    }
  }
  if (props.helpKey && CONTEXTUAL_HELP[props.helpKey]) {
    return CONTEXTUAL_HELP[props.helpKey]
  }
  return null
}

export const HelpTooltip: FC<HelpTooltipProps> = props => {
  const { exerciseRole, size = 'sm', compact = false } = props
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null)

  const content = resolveContent(props)
  if (!content) return null

  const handleClick = (e: MouseEvent<HTMLElement>) => {
    e.stopPropagation()
    setAnchorEl(e.currentTarget)
  }

  const handleClose = () => {
    setAnchorEl(null)
  }

  const roleTip =
    exerciseRole && content.roleTips?.[exerciseRole]
      ? content.roleTips[exerciseRole]
      : null

  const icon = (
    <Box
      component="span"
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        cursor: 'help',
        color: 'text.secondary',
        opacity: 0.6,
        transition: 'opacity 0.15s',
        '&:hover': { opacity: 1 },
      }}
      onClick={compact ? undefined : handleClick}
      data-testid="help-tooltip-icon"
      aria-label="Help"
    >
      <FontAwesomeIcon icon={faCircleInfo} size={size} />
    </Box>
  )

  if (compact) {
    return <Tooltip title={content.summary} arrow>{icon}</Tooltip>
  }

  const open = Boolean(anchorEl)

  return (
    <>
      {icon}
      <Popover
        open={open}
        anchorEl={anchorEl}
        onClose={handleClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
        slotProps={{
          paper: {
            sx: { maxWidth: 360, p: CobraStyles.Padding.PopoverContent },
          },
        }}
      >
        <Stack spacing={1.5}>
          <Typography variant="subtitle2" fontWeight={600}>
            {content.summary}
          </Typography>

          {content.details && (
            <Typography variant="body2" color="text.secondary">
              {content.details}
            </Typography>
          )}

          {content.relatedTerms && content.relatedTerms.length > 0 && (
            <Box>
              <Typography
                variant="caption"
                fontWeight={600}
                color="text.secondary"
                sx={{ mb: 0.5, display: 'block' }}
              >
                HSEEP Terms
              </Typography>
              {content.relatedTerms.map(termKey => {
                const entry = HSEEP_GLOSSARY[termKey]
                if (!entry) return null
                return (
                  <Typography
                    key={termKey}
                    variant="body2"
                    sx={{ mb: 0.5 }}
                  >
                    <Box component="span" fontWeight={600}>
                      {entry.term}
                    </Box>
                    {' \u2014 '}
                    {entry.definition}
                  </Typography>
                )
              })}
            </Box>
          )}

          {roleTip && (
            <Box
              sx={{
                bgcolor: 'primary.50',
                borderRadius: 1,
                p: 1,
                border: '1px solid',
                borderColor: 'primary.100',
              }}
            >
              <Typography variant="caption" fontWeight={600} color="primary.main">
                What you can do
              </Typography>
              <Typography variant="body2">{roleTip}</Typography>
            </Box>
          )}
        </Stack>
      </Popover>
    </>
  )
}
