/**
 * MselInjectRowCells
 *
 * Table cells for an inject row in the MSEL view (without the TableRow wrapper).
 * Used by SortableInjectRow which provides its own TableRow element.
 *
 * Includes support for:
 * - Batch selection checkboxes (approval workflow S12)
 * - Fire/Skip actions for Controllers
 * - Edit/Duplicate/Delete actions for managers
 * - Text highlighting for search terms
 *
 * @module features/injects
 */
import {
  TableCell,
  Typography,
  Stack,
  IconButton,
  Tooltip,
  Checkbox,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlay,
  faForwardStep,
  faPen,
  faTrash,
  faCopy,
} from '@fortawesome/free-solid-svg-icons'
import { InjectStatusChip } from './InjectStatusChip'
import { SubmitForApprovalButton } from './SubmitForApprovalButton'
import { HighlightedText } from '@/shared/components/HighlightedText'
import { InjectStatus } from '@/types'
import type { InjectDto } from '../types'
import { formatScenarioTime, formatScheduledTime } from '../types'

export interface MselInjectRowCellsProps {
  inject: InjectDto
  onClick: () => void
  canFireInjects: boolean
  canManage: boolean
  onFire: (e: React.MouseEvent) => void
  onSkip: (e: React.MouseEvent) => void
  onEdit: (e: React.MouseEvent) => void
  onDuplicate: (e: React.MouseEvent) => void
  onDelete: (e: React.MouseEvent) => void
  isFiring: boolean
  isSkipping: boolean
  searchTerm?: string
  showPhase?: boolean
  /** Approval workflow props (S12-S13) */
  approvalEnabled?: boolean
  exerciseId?: string
  isSelected?: boolean
  onToggleSelection?: (id: string) => void
}

/**
 * Table cells for an inject row (without the TableRow wrapper).
 * Used by SortableInjectRow which provides its own TableRow.
 */
export const MselInjectRowCells = ({
  inject,
  onClick,
  canFireInjects,
  canManage,
  onFire,
  onSkip,
  onEdit,
  onDuplicate,
  onDelete,
  isFiring,
  isSkipping,
  searchTerm = '',
  showPhase = false,
  approvalEnabled = false,
  exerciseId = '',
  isSelected = false,
  onToggleSelection,
}: MselInjectRowCellsProps) => {
  const scenarioTimeDisplay = formatScenarioTime(inject.scenarioDay, inject.scenarioTime)
  const scheduledTimeDisplay = formatScheduledTime(inject.scheduledTime)
  const isPending = inject.status === InjectStatus.Draft

  const handleCheckboxClick = (e: React.MouseEvent) => {
    e.stopPropagation()
    onToggleSelection?.(inject.id)
  }

  return (
    <>
      {/* Checkbox cell for batch selection (S12) */}
      {approvalEnabled && onToggleSelection && (
        <TableCell padding="checkbox" onClick={handleCheckboxClick}>
          <Checkbox
            checked={isSelected}
            inputProps={{ 'aria-label': `Select inject ${inject.injectNumber}` }}
          />
        </TableCell>
      )}
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography variant="body2" fontWeight={500}>
          {inject.injectNumber}
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography variant="body2">{scheduledTimeDisplay}</Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography
          variant="body2"
          color={scenarioTimeDisplay ? 'text.primary' : 'text.secondary'}
        >
          {scenarioTimeDisplay ?? '—'}
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography
          variant="body2"
          sx={{
            maxWidth: 300,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          <HighlightedText text={inject.title} searchTerm={searchTerm} />
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography
          variant="body2"
          color={inject.target ? 'text.primary' : 'text.secondary'}
          sx={{
            maxWidth: 140,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {inject.target ?? '—'}
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography
          variant="body2"
          color={inject.deliveryMethod ? 'text.primary' : 'text.secondary'}
        >
          {inject.deliveryMethod ?? '—'}
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <InjectStatusChip status={inject.status} />
      </TableCell>
      {showPhase && (
        <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
          <Typography
            variant="body2"
            color={inject.phaseName ? 'text.primary' : 'text.secondary'}
            sx={{
              maxWidth: 120,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {inject.phaseName ?? 'Unassigned'}
          </Typography>
        </TableCell>
      )}
      {(canFireInjects || canManage || approvalEnabled) && (
        <TableCell>
          <Stack direction="row" spacing={0.5}>
            {/* Quick Submit button (S13) - shown first for Draft injects when approval enabled */}
            {approvalEnabled && exerciseId && canManage && (
              <SubmitForApprovalButton
                inject={inject}
                exerciseId={exerciseId}
                approvalEnabled={approvalEnabled}
                canSubmit={canManage}
                size="small"
              />
            )}
            {/* Fire/Skip - only for Draft (when no approval) or Approved injects */}
            {canFireInjects && (
              // When approval enabled: only show for Approved injects
              // When approval disabled: show for Draft injects (isPending)
              (approvalEnabled ? inject.status === InjectStatus.Approved : isPending) && (
                <>
                  <Tooltip title="Fire inject">
                    <IconButton
                      size="small"
                      color="success"
                      onClick={onFire}
                      disabled={isFiring || isSkipping}
                    >
                      <FontAwesomeIcon icon={faPlay} size="sm" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Skip inject">
                    <IconButton
                      size="small"
                      color="warning"
                      onClick={onSkip}
                      disabled={isFiring || isSkipping}
                    >
                      <FontAwesomeIcon icon={faForwardStep} size="sm" />
                    </IconButton>
                  </Tooltip>
                </>
              )
            )}
            {canManage && (
              <>
                <Tooltip title="Edit inject">
                  <IconButton
                    size="small"
                    color="primary"
                    onClick={onEdit}
                  >
                    <FontAwesomeIcon icon={faPen} size="sm" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Duplicate inject">
                  <IconButton
                    size="small"
                    color="primary"
                    onClick={onDuplicate}
                  >
                    <FontAwesomeIcon icon={faCopy} size="sm" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Delete inject">
                  <IconButton
                    size="small"
                    color="error"
                    onClick={onDelete}
                  >
                    <FontAwesomeIcon icon={faTrash} size="sm" />
                  </IconButton>
                </Tooltip>
              </>
            )}
          </Stack>
        </TableCell>
      )}
    </>
  )
}
