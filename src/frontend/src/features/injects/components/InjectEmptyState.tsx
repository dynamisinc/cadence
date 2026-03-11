/**
 * InjectEmptyState
 *
 * Empty state display for the MSEL inject list. Renders different messages
 * based on the context:
 *
 * 1. Filtered to empty - search/filter returned no results
 * 2. No injects + canCreate - first-time creator CTA
 * 3. No injects + viewer - read-only empty message
 *
 * @module features/injects
 */
import { Box, Paper, Typography } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { alpha } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faListCheck, faMagnifyingGlass, faPlus } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'

export interface InjectEmptyStateProps {
  hasInjects: boolean
  canCreate: boolean
  onCreateClick: () => void
  hasFilters?: boolean
  onClearFilters?: () => void
}

/**
 * Empty state for the inject list, adapts message based on context.
 */
export const InjectEmptyState = ({
  hasInjects,
  canCreate,
  onCreateClick,
  hasFilters = false,
  onClearFilters,
}: InjectEmptyStateProps) => {
  const theme = useTheme()

  if (hasInjects) {
    // Filtered to empty (search/filter result)
    return (
      <Paper
        sx={{
          py: 6,
          px: 4,
          textAlign: 'center',
          backgroundColor: 'grey.50',
          border: '1px dashed',
          borderColor: 'grey.300',
        }}
      >
        <Box
          sx={{
            width: 80,
            height: 80,
            borderRadius: '50%',
            backgroundColor: 'grey.200',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 16px',
          }}
        >
          <FontAwesomeIcon
            icon={faMagnifyingGlass}
            style={{ fontSize: 40, color: theme.palette.neutral[500] }}
          />
        </Box>
        <Typography variant="h6" gutterBottom>
          No matching injects
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ maxWidth: 300, mx: 'auto', mb: hasFilters ? 2 : 0 }}
        >
          Try adjusting your search terms or filters to find the inject you're looking
          for.
        </Typography>
        {hasFilters && onClearFilters && (
          <CobraSecondaryButton onClick={onClearFilters}>
            Clear all filters
          </CobraSecondaryButton>
        )}
      </Paper>
    )
  }

  // No injects at all
  if (canCreate) {
    return (
      <Paper
        sx={{
          py: 8,
          px: 4,
          textAlign: 'center',
          backgroundColor: 'primary.50',
          border: '1px dashed',
          borderColor: 'primary.200',
        }}
      >
        <Box
          sx={{
            width: 100,
            height: 100,
            borderRadius: '50%',
            background: `linear-gradient(135deg, ${alpha(theme.palette.semantic.info, 0.12)} 0%, ${alpha(theme.palette.semantic.info, 0.25)} 100%)`,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 24px',
            boxShadow: '0 4px 20px rgba(33, 150, 243, 0.15)',
          }}
        >
          <FontAwesomeIcon icon={faListCheck} style={{ fontSize: 50, color: theme.palette.semantic.info }} />
        </Box>
        <Typography variant="h5" gutterBottom fontWeight={500}>
          Create Your First Inject
        </Typography>
        <Typography
          variant="body1"
          color="text.secondary"
          sx={{ maxWidth: 400, mx: 'auto', mb: 3 }}
        >
          Build out your MSEL by adding injects. Each inject represents an event,
          message, or action that will be delivered during exercise conduct.
        </Typography>
        <CobraPrimaryButton
          startIcon={<FontAwesomeIcon icon={faPlus} />}
          onClick={onCreateClick}
          size="large"
        >
          New Inject
        </CobraPrimaryButton>
      </Paper>
    )
  }

  // Viewer with no injects
  return (
    <Paper
      sx={{
        py: 6,
        px: 4,
        textAlign: 'center',
        backgroundColor: 'grey.50',
        border: '1px dashed',
        borderColor: 'grey.300',
      }}
    >
      <Box
        sx={{
          width: 80,
          height: 80,
          borderRadius: '50%',
          backgroundColor: 'grey.200',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          margin: '0 auto 16px',
        }}
      >
        <FontAwesomeIcon icon={faListCheck} style={{ fontSize: 40, color: theme.palette.neutral[500] }} />
      </Box>
      <Typography variant="h6" gutterBottom>
        No Injects Yet
      </Typography>
      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ maxWidth: 350, mx: 'auto' }}
      >
        The MSEL for this exercise is empty. Controllers will add injects during
        exercise planning.
      </Typography>
    </Paper>
  )
}
