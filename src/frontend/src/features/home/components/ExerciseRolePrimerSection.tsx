/**
 * ExerciseRolePrimerSection Component
 *
 * Brief overview of HSEEP exercise roles. Teaches users what
 * Controllers, Evaluators, etc. do before they enter an exercise.
 * Collapsible and remembers state via localStorage.
 */

import type { FC } from 'react'
import { Box, Collapse, Grid, Paper, Stack, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChevronDown, faChevronRight } from '@fortawesome/free-solid-svg-icons'

import { CobraLinkButton } from '../../../theme/styledComponents'
import { useDismissible } from '../../../shared/hooks/useDismissible'
import { EXERCISE_ROLE_PRIMERS } from '../../../shared/constants/roleOrientation'

export const ExerciseRolePrimerSection: FC = () => {
  const { isDismissed, dismiss, reset } = useDismissible(
    'home-exercise-role-primer',
  )

  return (
    <Box sx={{ mb: 3 }}>
      <CobraLinkButton
        onClick={isDismissed ? reset : dismiss}
        startIcon={
          <FontAwesomeIcon
            icon={isDismissed ? faChevronRight : faChevronDown}
            size="sm"
          />
        }
        size="small"
        sx={{ mb: 1 }}
      >
        HSEEP Exercise Roles
      </CobraLinkButton>

      <Collapse in={!isDismissed}>
        <Grid container spacing={1.5}>
          {EXERCISE_ROLE_PRIMERS.map(primer => (
            <Grid key={primer.role} size={{ xs: 12, sm: 6, md: 3 }}>
              <Paper
                variant="outlined"
                sx={{ p: 1.5, height: '100%' }}
              >
                <Stack spacing={0.5}>
                  <Stack direction="row" spacing={1} alignItems="center">
                    <Box sx={{ color: primer.color, fontSize: 16 }}>
                      <FontAwesomeIcon icon={primer.icon} />
                    </Box>
                    <Typography variant="subtitle2" fontWeight={600}>
                      {primer.role}
                    </Typography>
                  </Stack>
                  <Typography variant="body2" color="text.secondary">
                    {primer.summary}
                  </Typography>
                </Stack>
              </Paper>
            </Grid>
          ))}
        </Grid>
      </Collapse>
    </Box>
  )
}
