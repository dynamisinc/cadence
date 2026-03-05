/**
 * RoleOrientationPanel Component
 *
 * Role-based welcome and quick action cards for the HomePage.
 * Shows org-role-specific guidance that can be dismissed via localStorage.
 */

import type { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, Grid, Paper, Stack, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCircleQuestion } from '@fortawesome/free-solid-svg-icons'

import { CobraLinkButton } from '../../../theme/styledComponents'
import { useDismissible } from '../../../shared/hooks/useDismissible'
import {
  ORG_ROLE_ORIENTATIONS,
  type OrientationCard,
} from '../../../shared/constants/roleOrientation'
import type { OrgRole } from '../../organizations/types'

export interface RoleOrientationPanelProps {
  orgRole: OrgRole | string | undefined
  orgName: string | undefined
}

export const RoleOrientationPanel: FC<RoleOrientationPanelProps> = ({
  orgRole,
  orgName,
}) => {
  const navigate = useNavigate()
  const { isDismissed, dismiss, reset } = useDismissible('home-orientation')

  const orientation = orgRole
    ? ORG_ROLE_ORIENTATIONS[orgRole as OrgRole]
    : null

  if (!orientation) return null

  if (isDismissed) {
    return (
      <Box sx={{ mb: 2 }}>
        <CobraLinkButton
          onClick={reset}
          startIcon={<FontAwesomeIcon icon={faCircleQuestion} />}
          size="small"
        >
          Show orientation guide
        </CobraLinkButton>
      </Box>
    )
  }

  return (
    <Paper
      sx={{
        p: 3,
        mb: 3,
        background: 'linear-gradient(135deg, #f5f7fa 0%, #e4e8ec 100%)',
      }}
    >
      <Stack spacing={2}>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="flex-start"
        >
          <Box>
            <Typography variant="h5" component="h1" fontWeight={600}>
              {orientation.headline}
            </Typography>
            {orgName && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                {orgName}
              </Typography>
            )}
          </Box>
          <CobraLinkButton onClick={dismiss} size="small">
            Dismiss
          </CobraLinkButton>
        </Stack>

        <Typography variant="body1" color="text.secondary">
          {orientation.description}
        </Typography>

        <Grid container spacing={2}>
          {orientation.cards.map((card: OrientationCard) => (
            <Grid key={card.path} size={{ xs: 12, sm: 6 }}>
              <Paper
                variant="outlined"
                sx={{
                  p: 2,
                  cursor: 'pointer',
                  transition: 'all 0.15s',
                  '&:hover': {
                    boxShadow: 2,
                    borderColor: 'primary.main',
                  },
                }}
                onClick={() => navigate(card.path)}
                role="link"
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault()
                    navigate(card.path)
                  }
                }}
              >
                <Stack direction="row" spacing={1.5} alignItems="center">
                  <Box
                    sx={{
                      color: 'primary.main',
                      fontSize: 20,
                      width: 32,
                      display: 'flex',
                      justifyContent: 'center',
                    }}
                  >
                    <FontAwesomeIcon icon={card.icon} />
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" fontWeight={600}>
                      {card.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {card.description}
                    </Typography>
                  </Box>
                </Stack>
              </Paper>
            </Grid>
          ))}
        </Grid>
      </Stack>
    </Paper>
  )
}
