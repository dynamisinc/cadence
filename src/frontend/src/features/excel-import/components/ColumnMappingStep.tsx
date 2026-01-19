/**
 * ColumnMappingStep Component
 *
 * Third step of the import wizard - map Excel columns to Cadence fields.
 * Includes a toggle to preview how injects will look with current mappings.
 */

import { useState, useEffect, useMemo } from 'react'
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
  FormControl,
  Select,
  MenuItem,
  Chip,
  Collapse,
  IconButton,
  Divider,
  ToggleButtonGroup,
  ToggleButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faCheck,
  faAsterisk,
  faChevronDown,
  faChevronUp,
  faLightbulb,
  faTableColumns,
  faEye,
  faChevronLeft,
  faChevronRight,
} from '@fortawesome/free-solid-svg-icons'

import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import { InjectPreviewCard } from './InjectPreviewCard'
import type { ColumnInfo, ColumnMapping, RowValidationResult } from '../types'

type ViewMode = 'mapping' | 'preview'

interface ColumnMappingStepProps {
  /** Available columns from the worksheet */
  columns: ColumnInfo[]
  /** Initial/suggested mappings */
  initialMappings: ColumnMapping[]
  /** Preview rows from the worksheet (raw data) */
  previewRows?: Record<string, unknown>[]
  /** Is loading suggested mappings? */
  isLoading?: boolean
  /** Error message */
  error?: string | null
  /** Called when user wants to go back */
  onBack: () => void
  /** Called when user confirms mappings */
  onConfirm: (mappings: ColumnMapping[]) => void
}

export const ColumnMappingStep = ({
  columns,
  initialMappings,
  previewRows = [],
  isLoading = false,
  error = null,
  onBack,
  onConfirm,
}: ColumnMappingStepProps) => {
  const [mappings, setMappings] = useState<ColumnMapping[]>(initialMappings)
  const [showOptional, setShowOptional] = useState(true)
  const [viewMode, setViewMode] = useState<ViewMode>('mapping')
  const [previewIndex, setPreviewIndex] = useState(0)

  // Update mappings when initial mappings change
  useEffect(() => {
    setMappings(initialMappings)
  }, [initialMappings])

  const handleMappingChange = (fieldName: string, columnIndex: number | null) => {
    setMappings((prev) =>
      prev.map((m) =>
        m.cadenceField === fieldName
          ? { ...m, sourceColumnIndex: columnIndex }
          : m
      )
    )
  }

  const handleConfirm = () => {
    onConfirm(mappings)
  }

  const handleViewModeChange = (_: React.MouseEvent<HTMLElement>, newMode: ViewMode | null) => {
    if (newMode !== null) {
      setViewMode(newMode)
    }
  }

  const handlePrevPreview = () => {
    setPreviewIndex((prev) => Math.max(0, prev - 1))
  }

  const handleNextPreview = () => {
    setPreviewIndex((prev) => Math.min(previewRows.length - 1, prev + 1))
  }

  // Apply current mappings to preview rows to create preview data
  const previewData = useMemo((): RowValidationResult[] => {
    if (!previewRows || previewRows.length === 0) return []

    return previewRows.map((row, rowIndex) => {
      const values: Record<string, unknown> = {}

      // Apply each mapping to extract values
      mappings.forEach((mapping) => {
        if (mapping.sourceColumnIndex !== null && mapping.sourceColumnIndex !== undefined) {
          // Find the column header for this index
          const column = columns.find((c) => c.index === mapping.sourceColumnIndex)
          if (column) {
            // The preview row data uses column headers as keys
            values[mapping.cadenceField] = row[column.header] ?? null
          }
        }
      })

      return {
        rowNumber: rowIndex + 2, // +2 because row 1 is headers, data starts at row 2
        status: 'Valid' as const, // Preview doesn't do validation
        values,
        issues: [],
      }
    })
  }, [previewRows, mappings, columns])

  const requiredMappings = mappings.filter((m) => m.isRequired)
  const optionalMappings = mappings.filter((m) => !m.isRequired)

  const missingRequired = requiredMappings.filter(
    (m) => m.sourceColumnIndex === null || m.sourceColumnIndex === undefined
  )

  const autoMappedCount = mappings.filter(
    (m) => m.suggestedColumnIndex !== null && m.suggestedColumnIndex !== undefined
  ).length

  const getSampleValue = (columnIndex: number | null | undefined): string => {
    if (columnIndex === null || columnIndex === undefined) return '-'
    const col = columns.find((c) => c.index === columnIndex)
    if (!col || !col.sampleValues || col.sampleValues.length === 0) return '-'
    const sample = col.sampleValues.find((s) => s !== null && s !== '')
    return sample ? (sample.length > 30 ? sample.substring(0, 30) + '...' : sample) : '-'
  }

  if (isLoading) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography variant="body1">Analyzing columns...</Typography>
      </Box>
    )
  }

  const currentPreviewRow = previewData[previewIndex]
  const hasPreviewData = previewData.length > 0

  return (
    <Box>
      {/* View Mode Toggle */}
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h6">
          {viewMode === 'mapping' ? 'Column Mapping' : 'Preview Inject'}
        </Typography>
        {hasPreviewData && (
          <ToggleButtonGroup
            value={viewMode}
            exclusive
            onChange={handleViewModeChange}
            size="small"
          >
            <ToggleButton value="mapping">
              <FontAwesomeIcon icon={faTableColumns} style={{ marginRight: 6 }} />
              Mapping
            </ToggleButton>
            <ToggleButton value="preview">
              <FontAwesomeIcon icon={faEye} style={{ marginRight: 6 }} />
              Preview
            </ToggleButton>
          </ToggleButtonGroup>
        )}
      </Stack>

      {viewMode === 'mapping' ? (
        <>
          {/* Auto-mapping summary */}
          {autoMappedCount > 0 && (
            <Alert severity="info" icon={<FontAwesomeIcon icon={faLightbulb} />} sx={{ mb: 3 }}>
              {autoMappedCount} column{autoMappedCount !== 1 ? 's were' : ' was'} automatically mapped
              based on column headers. Review and adjust as needed.
            </Alert>
          )}

          {/* Required Fields */}
          <Typography variant="subtitle1" fontWeight="bold" gutterBottom>
            Required Fields
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            These fields must be mapped to import injects.
          </Typography>

          <TableContainer component={Paper} variant="outlined" sx={{ mb: 3 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 'bold' }}>Cadence Field</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Excel Column</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Sample Value</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {requiredMappings.map((mapping) => (
                  <MappingRow
                    key={mapping.cadenceField}
                    mapping={mapping}
                    columns={columns}
                    allMappings={mappings}
                    onMappingChange={handleMappingChange}
                    getSampleValue={getSampleValue}
                  />
                ))}
              </TableBody>
            </Table>
          </TableContainer>

          {/* Optional Fields */}
          <Box sx={{ mb: 3 }}>
            <Stack
              direction="row"
              alignItems="center"
              spacing={1}
              sx={{ cursor: 'pointer', mb: 1 }}
              onClick={() => setShowOptional(!showOptional)}
            >
              <Typography variant="subtitle1" fontWeight="bold">
                Optional Fields ({optionalMappings.length})
              </Typography>
              <IconButton size="small">
                <FontAwesomeIcon icon={showOptional ? faChevronUp : faChevronDown} />
              </IconButton>
            </Stack>

            <Collapse in={showOptional}>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                These fields are optional. Map them if your spreadsheet contains this data.
              </Typography>

              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={{ fontWeight: 'bold' }}>Cadence Field</TableCell>
                      <TableCell sx={{ fontWeight: 'bold' }}>Excel Column</TableCell>
                      <TableCell sx={{ fontWeight: 'bold' }}>Sample Value</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {optionalMappings.map((mapping) => (
                      <MappingRow
                        key={mapping.cadenceField}
                        mapping={mapping}
                        columns={columns}
                        allMappings={mappings}
                        onMappingChange={handleMappingChange}
                        getSampleValue={getSampleValue}
                      />
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Collapse>
          </Box>
        </>
      ) : (
        /* Preview Mode */
        <Box>
          {/* Preview Navigation */}
          <Stack direction="row" alignItems="center" justifyContent="center" spacing={2} sx={{ mb: 2 }}>
            <IconButton
              onClick={handlePrevPreview}
              disabled={previewIndex === 0}
              size="small"
            >
              <FontAwesomeIcon icon={faChevronLeft} />
            </IconButton>
            <Typography variant="body2" color="text.secondary">
              Row {previewIndex + 1} of {previewData.length}
            </Typography>
            <IconButton
              onClick={handleNextPreview}
              disabled={previewIndex >= previewData.length - 1}
              size="small"
            >
              <FontAwesomeIcon icon={faChevronRight} />
            </IconButton>
          </Stack>

          {/* Hint */}
          <Alert severity="info" sx={{ mb: 2, py: 0.5 }}>
            This preview shows how your data will appear in Cadence. Switch to Mapping view to adjust column assignments.
          </Alert>

          {/* Preview Card */}
          {currentPreviewRow && (
            <InjectPreviewCard
              rowData={currentPreviewRow}
              mappings={mappings}
              rowIndex={previewIndex}
            />
          )}
        </Box>
      )}

      {/* Validation Error */}
      {missingRequired.length > 0 && (
        <Alert severity="warning" sx={{ mb: 3, mt: viewMode === 'preview' ? 2 : 0 }}>
          Please map the following required fields: {missingRequired.map((m) => m.displayName).join(', ')}
        </Alert>
      )}

      {/* Error */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Navigation Buttons */}
      <Divider sx={{ my: 2 }} />
      <Stack direction="row" spacing={2} justifyContent="flex-end">
        <CobraSecondaryButton onClick={onBack}>
          Back
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleConfirm}
          disabled={missingRequired.length > 0}
        >
          Next
        </CobraPrimaryButton>
      </Stack>
    </Box>
  )
}

interface MappingRowProps {
  mapping: ColumnMapping
  columns: ColumnInfo[]
  allMappings: ColumnMapping[]
  onMappingChange: (fieldName: string, columnIndex: number | null) => void
  getSampleValue: (columnIndex: number | null | undefined) => string
}

const MappingRow = ({ mapping, columns, allMappings, onMappingChange, getSampleValue }: MappingRowProps) => {
  const hasSuggestion = mapping.suggestedColumnIndex !== null && mapping.suggestedColumnIndex !== undefined
  const isAutoMapped = hasSuggestion && mapping.sourceColumnIndex === mapping.suggestedColumnIndex

  // Build a map of which columns are already mapped (excluding current mapping)
  const usedColumns = useMemo(() => {
    const used = new Map<number, string>()
    allMappings.forEach((m) => {
      if (
        m.cadenceField !== mapping.cadenceField &&
        m.sourceColumnIndex !== null &&
        m.sourceColumnIndex !== undefined
      ) {
        used.set(m.sourceColumnIndex, m.displayName)
      }
    })
    return used
  }, [allMappings, mapping.cadenceField])

  return (
    <TableRow>
      <TableCell>
        <Stack direction="row" spacing={1} alignItems="center">
          {mapping.isRequired && (
            <FontAwesomeIcon icon={faAsterisk} size="xs" style={{ color: '#d32f2f' }} />
          )}
          <Box>
            <Typography variant="body2" fontWeight="medium">
              {mapping.displayName}
            </Typography>
            {mapping.description && (
              <Typography variant="caption" color="text.secondary">
                {mapping.description}
              </Typography>
            )}
          </Box>
        </Stack>
      </TableCell>
      <TableCell>
        <Stack direction="row" spacing={1} alignItems="center">
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <Select
              value={mapping.sourceColumnIndex ?? ''}
              onChange={(e) =>
                onMappingChange(
                  mapping.cadenceField,
                  e.target.value === '' ? null : Number(e.target.value)
                )
              }
              displayEmpty
            >
              <MenuItem value="">
                <em>Don&apos;t import</em>
              </MenuItem>
              {columns.map((col) => {
                const mappedTo = usedColumns.get(col.index)
                return (
                  <MenuItem key={col.index} value={col.index}>
                    <Stack direction="row" spacing={1} alignItems="center" sx={{ width: '100%' }}>
                      <span>{col.letter}: {col.header}</span>
                      {mappedTo && (
                        <Chip
                          size="small"
                          label={`→ ${mappedTo}`}
                          sx={{
                            ml: 'auto',
                            height: 20,
                            fontSize: '0.7rem',
                            backgroundColor: 'action.selected',
                          }}
                        />
                      )}
                    </Stack>
                  </MenuItem>
                )
              })}
            </Select>
          </FormControl>
          {isAutoMapped && (
            <Chip
              size="small"
              icon={<FontAwesomeIcon icon={faCheck} />}
              label="Auto"
              color="success"
              variant="outlined"
            />
          )}
        </Stack>
      </TableCell>
      <TableCell>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{
            maxWidth: 200,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {getSampleValue(mapping.sourceColumnIndex)}
        </Typography>
      </TableCell>
    </TableRow>
  )
}

export default ColumnMappingStep
