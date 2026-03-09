import { Box, Paper, Typography, Chip, Skeleton } from '@mui/material'
import { CobraLinkButton } from '@/theme/styledComponents'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCircle, faArrowRight } from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from 'react-router-dom'
import { appVersion } from '@/config/version'
import { useApiVersion } from '../hooks/useApiVersion'

/**
 * Compact version info card for embedding in Settings page.
 * Shows current versions with link to full About page.
 */
export function VersionInfoCard() {
  const navigate = useNavigate()
  const { apiVersion, isConnected, isLoading } = useApiVersion()

  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Typography variant="subtitle1" fontWeight="medium" gutterBottom>
        Version Information
      </Typography>

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, mb: 2 }}>
        {/* App Version */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Typography variant="body2" color="text.secondary" sx={{ minWidth: 100 }}>
            App Version
          </Typography>
          <Typography variant="body2" fontWeight="medium">
            {appVersion.version}
          </Typography>
        </Box>

        {/* API Version */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Typography variant="body2" color="text.secondary" sx={{ minWidth: 100 }}>
            API Version
          </Typography>
          {isLoading ? (
            <Skeleton width={80} />
          ) : (
            <>
              <Typography variant="body2" fontWeight="medium">
                {apiVersion?.version ?? 'Unknown'}
              </Typography>
              <Chip
                size="small"
                icon={
                  <FontAwesomeIcon
                    icon={faCircle}
                    style={{ fontSize: '0.5rem' }}
                  />
                }
                label={isConnected ? 'Connected' : 'Unavailable'}
                color={isConnected ? 'success' : 'default'}
                variant="outlined"
                sx={{
                  height: 20,
                  '& .MuiChip-label': { px: 1, fontSize: '0.75rem' },
                  '& .MuiChip-icon': {
                    color: isConnected ? 'success.main' : 'text.disabled',
                  },
                }}
              />
            </>
          )}
        </Box>

        {/* Build Info */}
        {appVersion.commitSha && appVersion.commitSha !== 'local' && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Typography variant="body2" color="text.secondary" sx={{ minWidth: 100 }}>
              Build
            </Typography>
            <Typography variant="body2" fontFamily="monospace" color="text.secondary">
              {appVersion.commitSha}
            </Typography>
          </Box>
        )}
      </Box>

      <CobraLinkButton
        size="small"
        endIcon={<FontAwesomeIcon icon={faArrowRight} />}
        onClick={() => navigate('/about')}
      >
        View release notes
      </CobraLinkButton>
    </Paper>
  )
}
