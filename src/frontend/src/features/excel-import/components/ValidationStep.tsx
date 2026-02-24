/**
 * ValidationStep Component
 *
 * Fourth step of the import wizard - validate import data and review errors.
 */

import { useState, useMemo } from 'react'
import {
  Box,
  Typography,
  Paper,
  Alert,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Collapse,
  IconButton,
  LinearProgress,
  Card,
  CardContent,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faCheck,
  faExclamationTriangle,
  faXmark,
  faChevronDown,
  faChevronUp,
} from '@fortawesome/free-solid-svg-icons'

import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import { AutoFixPanel } from './AutoFixPanel'
import { InlineEditCell } from './InlineEditCell'
import { computeAutoFixSuggestions } from '../utils/autoFixSuggestions'
import type { ValidationResult, RowValidationResult, ColumnMapping, AutoFixSuggestion } from '../types'

type FilterMode = 'all' | 'valid' | 'warning' | 'error'

interface ValidationStepProps {
  /** Validation results */
  validationResult: ValidationResult
  /** Column mappings for display */
  mappings: ColumnMapping[]
  /** Is validation in progress? */
  isLoading?: boolean
  /** Error message */
  error?: string | null
  /** Called when user wants to go back */
  onBack: () => void
  /** Called when user confirms and wants to proceed (import valid rows) */
  onProceed: (skipErrors: boolean) => void
  /** Called when user applies a bulk auto-fix */
  onApplyFix?: (suggestion: AutoFixSuggestion) => void
  /** Called when user edits a single cell inline */
  onCellEdit?: (rowNumber: number, field: string, newValue: string) => void
  /** Whether an update (auto-fix or inline edit) is in progress */
  isUpdating?: boolean
}

export const ValidationStep = ({
  validationResult,
  mappings,
  isLoading = false,
  error = null,
  onBack,
  onProceed,
  onApplyFix,
  onCellEdit,
  isUpdating = false,
}: ValidationStepProps) => {
  const [filter, setFilter] = useState<FilterMode>('all')
  const [expandedRow, setExpandedRow] = useState<number | null>(null)

  const autoFixSuggestions = useMemo(
    () => computeAutoFixSuggestions(validationResult),
    [validationResult],
  )

  const handleCardFilter = (mode: FilterMode) => {
    setFilter(prev => (prev === mode ? 'all' : mode))
  }

  const toggleRowExpansion = (rowNumber: number) => {
    setExpandedRow(expandedRow === rowNumber ? null : rowNumber)
  }

  const filteredRows = validationResult.rows.filter(row => {
    switch (filter) {
      case 'valid':
        return row.status === 'Valid'
      case 'warning':
        return row.status === 'Warning'
      case 'error':
        return row.status === 'Error'
      default:
        return true
    }
  })

  const mappedFields = mappings.filter(m => m.sourceColumnIndex !== null)

  if (isLoading) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography variant="body1" gutterBottom>
          Validating {validationResult.totalRows} rows...
        </Typography>
        <LinearProgress sx={{ maxWidth: 400, mx: 'auto' }} />
      </Box>
    )
  }

  const hasErrors = validationResult.errorRows > 0
  const hasWarnings = validationResult.warningRows > 0
  const allValid = !hasErrors && !hasWarnings

  return (
    <Box>
      {/* Summary Cards (clickable filters) */}
      <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
        <SummaryCard
          title="Total Rows"
          value={validationResult.totalRows}
          color="default"
          isActive={filter === 'all'}
          onClick={() => handleCardFilter('all')}
        />
        <SummaryCard
          title="Valid"
          value={validationResult.validRows}
          color="success"
          icon={faCheck}
          isActive={filter === 'valid'}
          onClick={() => handleCardFilter('valid')}
        />
        <SummaryCard
          title="Warnings"
          value={validationResult.warningRows}
          color="warning"
          icon={faExclamationTriangle}
          isActive={filter === 'warning'}
          onClick={() => handleCardFilter('warning')}
        />
        <SummaryCard
          title="Errors"
          value={validationResult.errorRows}
          color="error"
          icon={faXmark}
          isActive={filter === 'error'}
          onClick={() => handleCardFilter('error')}
        />
      </Stack>

      {/* Status Alert */}
      {allValid ? (
        <Alert severity="success" sx={{ mb: 3 }}>
          All rows are valid and ready to import.
        </Alert>
      ) : hasErrors ? (
        <Alert severity="error" sx={{ mb: 3 }}>
          {validationResult.errorRows} row{validationResult.errorRows !== 1 ? 's have' : ' has'} errors
          that must be fixed before importing. You can import valid rows only.
        </Alert>
      ) : (
        <Alert severity="warning" sx={{ mb: 3 }}>
          {validationResult.warningRows} row{validationResult.warningRows !== 1 ? 's have' : ' has'} warnings.
          These rows will be imported but may have issues.
        </Alert>
      )}

      {/* Auto-Fix Suggestions */}
      {onApplyFix && autoFixSuggestions.length > 0 && (
        <AutoFixPanel
          suggestions={autoFixSuggestions}
          onApplyFix={onApplyFix}
          isApplying={isUpdating}
        />
      )}

      {/* Validation Table */}
      <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 400, mb: 3 }}>
        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell sx={{ width: 50 }} />
              <TableCell sx={{ fontWeight: 'bold' }}>Row</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Status</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Title</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Time</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Issues</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredRows.map(row => (
              <ValidationRow
                key={row.rowNumber}
                row={row}
                isExpanded={expandedRow === row.rowNumber}
                onToggle={() => toggleRowExpansion(row.rowNumber)}
                mappedFields={mappedFields}
                onCellEdit={onCellEdit}
              />
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Error */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Navigation Buttons */}
      <Stack direction="row" spacing={2} justifyContent="flex-end">
        <CobraSecondaryButton onClick={onBack}>
          Back
        </CobraSecondaryButton>
        {hasErrors ? (
          <CobraPrimaryButton onClick={() => onProceed(true)}>
            Import Valid Rows Only ({validationResult.validRows})
          </CobraPrimaryButton>
        ) : (
          <CobraPrimaryButton onClick={() => onProceed(false)}>
            Import All Rows ({validationResult.totalRows})
          </CobraPrimaryButton>
        )}
      </Stack>
    </Box>
  )
}

interface SummaryCardProps {
  title: string
  value: number
  color: 'default' | 'success' | 'warning' | 'error'
  icon?: typeof faCheck
  isActive?: boolean
  onClick?: () => void
}

const SummaryCard = ({ title, value, color, icon, isActive = false, onClick }: SummaryCardProps) => {
  const colorMap = {
    default: 'grey.100',
    success: 'success.light',
    warning: 'warning.light',
    error: 'error.light',
  }

  const borderColorMap = {
    default: 'grey.500',
    success: 'success.main',
    warning: 'warning.main',
    error: 'error.main',
  }

  const textColorMap = {
    default: 'text.primary',
    success: 'success.dark',
    warning: 'warning.dark',
    error: 'error.dark',
  }

  return (
    <Card
      onClick={onClick}
      sx={{
        flex: 1,
        backgroundColor: colorMap[color],
        cursor: 'pointer',
        border: 2,
        borderColor: isActive ? borderColorMap[color] : 'transparent',
        transition: 'border-color 0.15s, box-shadow 0.15s',
        boxShadow: isActive ? 3 : 1,
        '&:hover': {
          borderColor: borderColorMap[color],
          boxShadow: 2,
        },
      }}
    >
      <CardContent sx={{ textAlign: 'center', py: 1.5, '&:last-child': { pb: 1.5 } }}>
        <Typography variant="h5" sx={{ color: textColorMap[color] }}>
          {icon && <FontAwesomeIcon icon={icon} style={{ marginRight: 8 }} />}
          {value}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {title}
        </Typography>
      </CardContent>
    </Card>
  )
}

interface ValidationRowProps {
  row: RowValidationResult
  isExpanded: boolean
  onToggle: () => void
  mappedFields: ColumnMapping[]
  onCellEdit?: (rowNumber: number, field: string, newValue: string) => void
}

const ValidationRow = ({
  row,
  isExpanded,
  onToggle,
  mappedFields: _mappedFields,
  onCellEdit,
}: ValidationRowProps) => {
  const getStatusChip = () => {
    switch (row.status) {
      case 'Valid':
        return <Chip size="small" icon={<FontAwesomeIcon icon={faCheck} />} label="Valid" color="success" />
      case 'Warning':
        return <Chip size="small" icon={<FontAwesomeIcon icon={faExclamationTriangle} />} label="Warning" color="warning" />
      case 'Error':
        return <Chip size="small" icon={<FontAwesomeIcon icon={faXmark} />} label="Error" color="error" />
    }
  }

  const titleValue = (row.values['Title'] as string) || ''
  const timeValue = row.values['ScheduledTime']
  const timeDisplay = timeValue ? String(timeValue) : ''
  const issueCount = row.issues?.length || 0

  const titleIssue = row.issues?.find(i => i.field === 'Title')
  const timeIssue = row.issues?.find(i => i.field === 'ScheduledTime')

  return (
    <>
      <TableRow
        sx={{
          backgroundColor:
            row.status === 'Error'
              ? 'error.light'
              : row.status === 'Warning'
                ? 'warning.light'
                : 'transparent',
          '&:hover': { backgroundColor: 'action.hover' },
          cursor: issueCount > 0 ? 'pointer' : 'default',
        }}
        onClick={issueCount > 0 ? onToggle : undefined}
      >
        <TableCell>
          {issueCount > 0 && (
            <IconButton size="small">
              <FontAwesomeIcon icon={isExpanded ? faChevronUp : faChevronDown} />
            </IconButton>
          )}
        </TableCell>
        <TableCell>{row.rowNumber}</TableCell>
        <TableCell>{getStatusChip()}</TableCell>
        <TableCell
          sx={{ maxWidth: 200 }}
          onClick={titleIssue ? e => e.stopPropagation() : undefined}
        >
          {onCellEdit ? (
            <InlineEditCell
              value={titleValue}
              field="Title"
              rowNumber={row.rowNumber}
              hasIssue={!!titleIssue}
              issueSeverity={titleIssue?.severity}
              onSave={onCellEdit}
            />
          ) : (
            titleValue || '-'
          )}
        </TableCell>
        <TableCell onClick={timeIssue ? e => e.stopPropagation() : undefined}>
          {onCellEdit ? (
            <InlineEditCell
              value={timeDisplay}
              field="ScheduledTime"
              rowNumber={row.rowNumber}
              hasIssue={!!timeIssue}
              issueSeverity={timeIssue?.severity}
              onSave={onCellEdit}
            />
          ) : (
            timeDisplay || '-'
          )}
        </TableCell>
        <TableCell>
          {issueCount > 0 ? `${issueCount} issue${issueCount !== 1 ? 's' : ''}` : '-'}
        </TableCell>
      </TableRow>

      {/* Expanded row details */}
      {issueCount > 0 && (
        <TableRow>
          <TableCell colSpan={6} sx={{ py: 0 }}>
            <Collapse in={isExpanded}>
              <Box sx={{ p: 2, backgroundColor: 'grey.50' }}>
                <Typography variant="subtitle2" gutterBottom>
                  Issues:
                </Typography>
                <Stack spacing={1}>
                  {row.issues?.map((issue, index) => (
                    <Alert
                      key={index}
                      severity={issue.severity === 'Error' ? 'error' : 'warning'}
                      sx={{ py: 0 }}
                    >
                      <strong>{issue.field}:</strong> {issue.message}
                      {issue.originalValue && (
                        <Typography variant="caption" display="block" color="text.secondary">
                          Original value: &quot;{issue.originalValue}&quot;
                        </Typography>
                      )}
                    </Alert>
                  ))}
                </Stack>
              </Box>
            </Collapse>
          </TableCell>
        </TableRow>
      )}
    </>
  )
}

export default ValidationStep
