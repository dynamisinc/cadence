import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Box,
  Skeleton,
  Alert,
  Chip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBan, faRotateLeft } from '@fortawesome/free-solid-svg-icons'
import { CobraDeleteButton, CobraSecondaryButton } from '@/theme/styledComponents'
import type { OrganizationSuggestionDto, SuggestionFieldName } from '../types'
import {
  useHistoricalValues,
  useBlockValue,
  useUnblockValue,
} from '../hooks/useSuggestionManagement'

interface HistoricalValuesSectionProps {
  fieldName: SuggestionFieldName
  blockedSuggestions: OrganizationSuggestionDto[]
}

export function HistoricalValuesSection({
  fieldName,
  blockedSuggestions,
}: HistoricalValuesSectionProps) {
  const { data: historicalValues = [], isLoading } = useHistoricalValues(fieldName)
  const blockMutation = useBlockValue(fieldName)
  const unblockMutation = useUnblockValue(fieldName)

  const handleBlock = (value: string) => {
    blockMutation.mutate({ fieldName, value })
  }

  const handleUnblock = (id: string) => {
    unblockMutation.mutate(id)
  }

  const hasContent = historicalValues.length > 0 || blockedSuggestions.length > 0

  if (isLoading) {
    return (
      <Box>
        {[1, 2].map(n => (
          <Skeleton key={n} variant="rectangular" height={40} sx={{ mb: 1, borderRadius: 1 }} />
        ))}
      </Box>
    )
  }

  if (!hasContent) {
    return (
      <Alert severity="info">
        No historical values found for this field. Values will appear here as they are used in
        injects.
      </Alert>
    )
  }

  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Value</TableCell>
            <TableCell align="center" sx={{ width: 100 }}>
              Status
            </TableCell>
            <TableCell align="right" sx={{ width: 120 }}>
              Actions
            </TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {/* Blocked values first */}
          {blockedSuggestions.map(blocked => (
            <TableRow key={blocked.id} sx={{ opacity: 0.6 }}>
              <TableCell>
                <Typography
                  variant="body2"
                  sx={{ textDecoration: 'line-through', color: 'text.disabled' }}
                >
                  {blocked.value}
                </Typography>
              </TableCell>
              <TableCell align="center">
                <Chip label="Blocked" size="small" color="error" variant="outlined" />
              </TableCell>
              <TableCell align="right">
                <CobraSecondaryButton
                  size="small"
                  startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
                  onClick={() => handleUnblock(blocked.id)}
                  disabled={unblockMutation.isPending}
                >
                  Unblock
                </CobraSecondaryButton>
              </TableCell>
            </TableRow>
          ))}
          {/* Available historical values */}
          {historicalValues.map(value => (
            <TableRow key={value}>
              <TableCell>
                <Typography variant="body2">{value}</Typography>
              </TableCell>
              <TableCell align="center">
                <Chip label="Historical" size="small" variant="outlined" />
              </TableCell>
              <TableCell align="right">
                <CobraDeleteButton
                  size="small"
                  startIcon={<FontAwesomeIcon icon={faBan} />}
                  onClick={() => handleBlock(value)}
                  disabled={blockMutation.isPending}
                >
                  Block
                </CobraDeleteButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
