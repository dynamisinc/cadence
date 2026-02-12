import { useState } from 'react'
import {
  Box,
  Typography,
  Paper,
  Chip,
  Button,
  Card,
  CardContent,
  List,
  ListItem,
  ListItemText,
  Skeleton,
  Alert,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faCircle,
  faStar,
  faBug,
  faTriangleExclamation,
  faChevronDown,
  faChevronUp,
  faHome,
  faCircleInfo,
} from '@fortawesome/free-solid-svg-icons'
import { appVersion } from '@/config/version'
import { useReleaseNotes } from '../hooks/useReleaseNotes'
import { useApiVersion } from '../hooks/useApiVersion'
import { useBreadcrumbs } from '@/core/contexts'
import CobraStyles from '@/theme/CobraStyles'
import type { ReleaseNote } from '../types'

const INITIAL_RELEASES_SHOWN = 5

/**
 * About page displaying version information and release history.
 * Accessible from profile menu and Settings page.
 */
export function AboutPage() {
  const { releaseNotes, isLoading: notesLoading } = useReleaseNotes()
  const { apiVersion, isConnected, isLoading: apiLoading } = useApiVersion()
  const [showAllReleases, setShowAllReleases] = useState(false)

  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'About' },
  ])

  const displayedReleases = showAllReleases
    ? releaseNotes
    : releaseNotes.slice(0, INITIAL_RELEASES_SHOWN)

  const hasMoreReleases = releaseNotes.length > INITIAL_RELEASES_SHOWN

  return (
    <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
      {/* Header */}
      <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 3 }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            width: 40,
            height: 40,
            borderRadius: 2,
            backgroundColor: 'primary.main',
            color: 'primary.contrastText',
          }}
        >
          <FontAwesomeIcon icon={faCircleInfo} />
        </Box>
        <Typography variant="h5" component="h1">
          About Cadence
        </Typography>
      </Stack>

      {/* Version info card */}
      <Paper sx={{ p: 3, mb: 4 }} variant="outlined">
        <Box sx={{ display: 'grid', gap: 2 }}>
          <VersionRow
            label="App Version"
            value={appVersion.version}
          />
          <VersionRow
            label="API Version"
            value={apiLoading ? <Skeleton width={80} /> : apiVersion?.version ?? 'Unknown'}
            status={
              apiLoading ? undefined : (
                <Chip
                  size="small"
                  icon={<FontAwesomeIcon icon={faCircle} />}
                  label={isConnected ? 'Connected' : 'Unavailable'}
                  color={isConnected ? 'success' : 'default'}
                  variant="outlined"
                  sx={{
                    '& .MuiChip-icon': {
                      fontSize: '0.5rem',
                      color: isConnected ? 'success.main' : 'text.disabled',
                    },
                  }}
                />
              )
            }
          />
          <VersionRow
            label="Build Date"
            value={formatBuildDate(appVersion.buildDate)}
          />
          {appVersion.commitSha && appVersion.commitSha !== 'local' && (
            <VersionRow
              label="Build"
              value={appVersion.commitSha}
              mono
            />
          )}
        </Box>
      </Paper>

      {/* Release history */}
      <Box>
        <Typography variant="h6" component="h2" gutterBottom>
          Release History
        </Typography>

        {notesLoading ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {[1, 2, 3].map(i => (
              <Skeleton key={i} variant="rounded" height={120} />
            ))}
          </Box>
        ) : releaseNotes.length === 0 ? (
          <Alert severity="info">No release notes available.</Alert>
        ) : (
          <>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              {displayedReleases.map(release => (
                <ReleaseCard key={release.version} release={release} />
              ))}
            </Box>

            {hasMoreReleases && (
              <Button
                onClick={() => setShowAllReleases(!showAllReleases)}
                endIcon={
                  <FontAwesomeIcon
                    icon={showAllReleases ? faChevronUp : faChevronDown}
                  />
                }
                sx={{ mt: 2 }}
              >
                {showAllReleases
                  ? 'Show fewer'
                  : `Show ${releaseNotes.length - INITIAL_RELEASES_SHOWN} older releases`}
              </Button>
            )}
          </>
        )}
      </Box>

      {/* Footer */}
      <Box sx={{ mt: 6, pt: 3, borderTop: 1, borderColor: 'divider' }}>
        <Typography variant="body2" color="text.secondary">
          Cadence is a HSEEP-compliant MSEL management platform for emergency
          management exercises.
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          &copy; {new Date().getFullYear()}
        </Typography>
      </Box>
    </Box>
  )
}

// --- Helper Components ---

interface VersionRowProps {
  label: string;
  value: React.ReactNode;
  status?: React.ReactNode;
  mono?: boolean;
}

function VersionRow({ label, value, status, mono }: VersionRowProps) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ minWidth: 100 }}
      >
        {label}
      </Typography>
      <Typography
        variant="body1"
        fontWeight="medium"
        sx={{ fontFamily: mono ? 'monospace' : undefined }}
      >
        {value}
      </Typography>
      {status}
    </Box>
  )
}

interface ReleaseCardProps {
  release: ReleaseNote;
}

function ReleaseCard({ release }: ReleaseCardProps) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1, mb: 1 }}>
          <Typography variant="subtitle1" fontWeight="bold">
            v{release.version}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            &middot; {formatReleaseDate(release.date)}
          </Typography>
        </Box>

        {release.features.length > 0 && (
          <Box sx={{ mb: 1.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, mb: 0.5 }}>
              <FontAwesomeIcon icon={faStar} size="sm" color="#4caf50" />
              <Typography variant="subtitle2" fontWeight="bold">
                Features
              </Typography>
            </Box>
            <List dense disablePadding sx={{ pl: 0.5 }}>
              {release.features.map((feature, i) => (
                <ListItem key={i} disableGutters sx={{ py: 0.25 }}>
                  <ListItemText
                    primary={feature}
                    primaryTypographyProps={{ variant: 'body2' }}
                  />
                </ListItem>
              ))}
            </List>
          </Box>
        )}

        {release.fixes.length > 0 && (
          <Box sx={{ mb: 1.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, mb: 0.5 }}>
              <FontAwesomeIcon icon={faBug} size="sm" color="#ff9800" />
              <Typography variant="subtitle2" fontWeight="bold">
                Fixes
              </Typography>
            </Box>
            <List dense disablePadding sx={{ pl: 0.5 }}>
              {release.fixes.map((fix, i) => (
                <ListItem key={i} disableGutters sx={{ py: 0.25 }}>
                  <ListItemText
                    primary={fix}
                    primaryTypographyProps={{ variant: 'body2' }}
                  />
                </ListItem>
              ))}
            </List>
          </Box>
        )}

        {release.breaking && release.breaking.length > 0 && (
          <Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, mb: 0.5 }}>
              <FontAwesomeIcon icon={faTriangleExclamation} size="sm" color="#f44336" />
              <Typography variant="subtitle2" fontWeight="bold">
                Breaking Changes
              </Typography>
            </Box>
            <List dense disablePadding sx={{ pl: 0.5 }}>
              {release.breaking.map((change, i) => (
                <ListItem key={i} disableGutters sx={{ py: 0.25 }}>
                  <ListItemText
                    primary={change}
                    primaryTypographyProps={{ variant: 'body2' }}
                  />
                </ListItem>
              ))}
            </List>
          </Box>
        )}
      </CardContent>
    </Card>
  )
}

// --- Utilities ---

function formatBuildDate(isoDate: string): string {
  try {
    return new Date(isoDate).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    })
  } catch {
    return isoDate
  }
}

function formatReleaseDate(date: string): string {
  try {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    })
  } catch {
    return date
  }
}
