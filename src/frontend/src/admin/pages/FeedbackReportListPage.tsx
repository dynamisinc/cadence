import { useState, useMemo, useEffect } from 'react'
import {
  Box,
  Typography,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  TablePagination,
  Paper,
  Chip,
  Skeleton,
  Alert,
  Collapse,
  IconButton,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  InputAdornment,
  Link,
  Menu,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faComments,
  faBug,
  faLightbulb,
  faMessage,
  faChevronDown,
  faChevronRight,
  faMagnifyingGlass,
  faXmark,
  faEllipsisVertical,
  faSave,
  faBoxArchive,
  faTrash,
} from '@fortawesome/free-solid-svg-icons'
import { faGithub } from '@fortawesome/free-brands-svg-icons'
import type { IconProp } from '@fortawesome/fontawesome-svg-core'
import { CobraTextField, CobraPrimaryButton } from '@/theme/styledComponents'
import CobraStyles from '@/theme/CobraStyles'
import { PageHeader, ConfirmDialog } from '@/shared/components'
import { useBreadcrumbs } from '@/core/contexts'
import {
  useFeedbackReports,
  useUpdateFeedbackStatus,
  useDeleteFeedbackReport,
} from '../hooks/useFeedbackAdmin'
import {
  FeedbackType,
  FeedbackStatus,
  FeedbackTypeLabels,
  FeedbackStatusLabels,
} from '../types/feedbackReport'
import type { FeedbackReportDto } from '../types/feedbackReport'

type SortField = 'createdAt' | 'title' | 'referenceNumber' | 'reporterEmail' | 'type' | 'status'

const typeChipColor: Record<FeedbackType, 'error' | 'primary' | 'default'> = {
  [FeedbackType.BugReport]: 'error',
  [FeedbackType.FeatureRequest]: 'primary',
  [FeedbackType.General]: 'default',
}

const typeIcons: Record<FeedbackType, typeof faBug> = {
  [FeedbackType.BugReport]: faBug,
  [FeedbackType.FeatureRequest]: faLightbulb,
  [FeedbackType.General]: faMessage,
}

const statusChipColor: Record<FeedbackStatus, 'warning' | 'info' | 'success' | 'default'> = {
  [FeedbackStatus.New]: 'warning',
  [FeedbackStatus.InReview]: 'info',
  [FeedbackStatus.Resolved]: 'success',
  [FeedbackStatus.Closed]: 'default',
}

export const FeedbackReportListPage = () => {
  useBreadcrumbs([
    { label: 'System Settings', path: '/admin' },
    { label: 'Feedback Reports' },
  ])

  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState<FeedbackType | ''>('')
  const [statusFilter, setStatusFilter] = useState<FeedbackStatus | ''>('')
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [sortField, setSortField] = useState<SortField>('createdAt')
  const [sortDesc, setSortDesc] = useState(true)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [menuAnchor, setMenuAnchor] = useState<{ element: HTMLElement; reportId: string } | null>(
    null,
  )
  const [deleteConfirm, setDeleteConfirm] = useState<{ id: string; refNumber: string } | null>(null)
  const [closeConfirm, setCloseConfirm] = useState<{ id: string; refNumber: string; adminNotes: string | null } | null>(null)

  const updateStatus = useUpdateFeedbackStatus()
  const deleteReport = useDeleteFeedbackReport()

  const queryParams = useMemo(
    () => ({
      page: page + 1,
      pageSize,
      search: search || undefined,
      type: typeFilter !== '' ? typeFilter : undefined,
      status: statusFilter !== '' ? statusFilter : undefined,
      sortBy: sortField,
      sortDesc,
    }),
    [page, pageSize, search, typeFilter, statusFilter, sortField, sortDesc],
  )

  const { data, isLoading, error } = useFeedbackReports(queryParams)

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDesc(!sortDesc)
    } else {
      setSortField(field)
      setSortDesc(true)
    }
  }

  const handleClearFilters = () => {
    setSearch('')
    setTypeFilter('')
    setStatusFilter('')
    setPage(0)
  }

  const hasActiveFilters = !!(search || typeFilter !== '' || statusFilter !== '')

  if (error) return <Alert severity="error">Failed to load feedback reports.</Alert>

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="Feedback Reports"
        icon={faComments}
        subtitle="View and manage feedback submissions from users"
      />

      {/* Filters */}
      <Stack direction="row" spacing={2} sx={{ mb: 3 }} alignItems="center">
        <CobraTextField
          size="small"
          placeholder="Search by title, reference, email..."
          value={search}
          onChange={e => {
            setSearch(e.target.value)
            setPage(0)
          }}
          sx={{ minWidth: 280 }}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <FontAwesomeIcon icon={faMagnifyingGlass} />
                </InputAdornment>
              ),
              endAdornment: search ? (
                <InputAdornment position="end">
                  <IconButton size="small" onClick={() => setSearch('')}>
                    <FontAwesomeIcon icon={faXmark} size="sm" />
                  </IconButton>
                </InputAdornment>
              ) : undefined,
            },
          }}
        />

        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Type</InputLabel>
          <Select
            value={typeFilter}
            label="Type"
            onChange={e => {
              setTypeFilter(e.target.value as FeedbackType | '')
              setPage(0)
            }}
          >
            <MenuItem value="">All Types</MenuItem>
            {Object.values(FeedbackType).map(t => (
              <MenuItem key={t} value={t}>
                {FeedbackTypeLabels[t]}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Status</InputLabel>
          <Select
            value={statusFilter}
            label="Status"
            onChange={e => {
              setStatusFilter(e.target.value as FeedbackStatus | '')
              setPage(0)
            }}
          >
            <MenuItem value="">All Statuses</MenuItem>
            {Object.values(FeedbackStatus).map(s => (
              <MenuItem key={s} value={s}>
                {FeedbackStatusLabels[s]}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        {hasActiveFilters && (
          <Chip label="Clear filters" size="small" onDelete={handleClearFilters} />
        )}
      </Stack>

      {/* Table */}
      {isLoading ? (
        <TableSkeleton />
      ) : !data || data.reports.length === 0 ? (
        <EmptyState hasFilters={hasActiveFilters} />
      ) : (
        <>
          <TableContainer component={Paper}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell width={40} />
                  <SortableHeader
                    field="referenceNumber"
                    label="Ref #"
                    sortField={sortField}
                    sortDesc={sortDesc}
                    onSort={handleSort}
                  />
                  <SortableHeader
                    field="type"
                    label="Type"
                    sortField={sortField}
                    sortDesc={sortDesc}
                    onSort={handleSort}
                  />
                  <SortableHeader
                    field="status"
                    label="Status"
                    sortField={sortField}
                    sortDesc={sortDesc}
                    onSort={handleSort}
                  />
                  <SortableHeader
                    field="title"
                    label="Title"
                    sortField={sortField}
                    sortDesc={sortDesc}
                    onSort={handleSort}
                  />
                  <SortableHeader
                    field="reporterEmail"
                    label="Reporter"
                    sortField={sortField}
                    sortDesc={sortDesc}
                    onSort={handleSort}
                  />
                  <TableCell>Org</TableCell>
                  <TableCell>GitHub</TableCell>
                  <SortableHeader
                    field="createdAt"
                    label="Submitted"
                    sortField={sortField}
                    sortDesc={sortDesc}
                    onSort={handleSort}
                  />
                  <TableCell width={48} />
                </TableRow>
              </TableHead>
              <TableBody>
                {data.reports.map(report => (
                  <ReportRow
                    key={report.id}
                    report={report}
                    expanded={expandedId === report.id}
                    onToggle={() => setExpandedId(expandedId === report.id ? null : report.id)}
                    onStatusChange={status => {
                      if (status === FeedbackStatus.Closed) {
                        setCloseConfirm({ id: report.id, refNumber: report.referenceNumber, adminNotes: report.adminNotes })
                      } else {
                        updateStatus.mutate({
                          id: report.id,
                          request: { status, adminNotes: report.adminNotes },
                        })
                      }
                    }}
                    onSaveNotes={notes =>
                      updateStatus.mutate({
                        id: report.id,
                        request: { status: report.status, adminNotes: notes },
                      })
                    }
                    onMenuOpen={e => setMenuAnchor({ element: e.currentTarget, reportId: report.id })}
                    isUpdating={updateStatus.isPending}
                  />
                ))}
              </TableBody>
            </Table>
          </TableContainer>

          <TablePagination
            component="div"
            count={data.pagination.totalCount}
            page={page}
            onPageChange={(_, newPage) => setPage(newPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={e => {
              setPageSize(parseInt(e.target.value, 10))
              setPage(0)
            }}
            rowsPerPageOptions={[10, 25, 50]}
          />

          {/* Shared actions menu */}
          <Menu
            anchorEl={menuAnchor?.element}
            open={Boolean(menuAnchor)}
            onClose={() => setMenuAnchor(null)}
          >
            <MenuItem
              onClick={() => {
                if (menuAnchor) {
                  const report = data.reports.find(r => r.id === menuAnchor.reportId)
                  if (report) {
                    setCloseConfirm({ id: report.id, refNumber: report.referenceNumber, adminNotes: report.adminNotes })
                  }
                }
                setMenuAnchor(null)
              }}
            >
              <FontAwesomeIcon icon={faBoxArchive} style={{ marginRight: 8 }} />
              Archive (Close)
            </MenuItem>
            <Divider />
            <MenuItem
              sx={{ color: 'error.main' }}
              onClick={() => {
                if (menuAnchor) {
                  const report = data.reports.find(r => r.id === menuAnchor.reportId)
                  if (report) {
                    setDeleteConfirm({ id: report.id, refNumber: report.referenceNumber })
                  }
                }
                setMenuAnchor(null)
              }}
            >
              <FontAwesomeIcon icon={faTrash} style={{ marginRight: 8 }} />
              Delete
            </MenuItem>
          </Menu>

          {/* Close confirmation */}
          <ConfirmDialog
            open={Boolean(closeConfirm)}
            title="Close Feedback Report"
            message={`Close ${closeConfirm?.refNumber ?? 'this report'}? This will also close the linked GitHub issue if one exists.`}
            confirmLabel="Close Report"
            severity="warning"
            onConfirm={() => {
              if (closeConfirm) {
                updateStatus.mutate({
                  id: closeConfirm.id,
                  request: { status: FeedbackStatus.Closed, adminNotes: closeConfirm.adminNotes },
                })
              }
              setCloseConfirm(null)
            }}
            onCancel={() => setCloseConfirm(null)}
          />

          {/* Delete confirmation */}
          <ConfirmDialog
            open={Boolean(deleteConfirm)}
            title="Delete Feedback Report"
            message={`Are you sure you want to delete ${deleteConfirm?.refNumber ?? 'this report'}? This action cannot be undone.`}
            confirmLabel="Delete"
            severity="danger"
            onConfirm={() => {
              if (deleteConfirm) {
                deleteReport.mutate(deleteConfirm.id)
              }
              setDeleteConfirm(null)
            }}
            onCancel={() => setDeleteConfirm(null)}
          />
        </>
      )}
    </Box>
  )
}

// ── Sub-components ──

interface SortableHeaderProps {
  field: SortField
  label: string
  sortField: SortField
  sortDesc: boolean
  onSort: (field: SortField) => void
}

const SortableHeader = ({ field, label, sortField, sortDesc, onSort }: SortableHeaderProps) => (
  <TableCell>
    <TableSortLabel
      active={sortField === field}
      direction={sortField === field ? (sortDesc ? 'desc' : 'asc') : 'asc'}
      onClick={() => onSort(field)}
    >
      {label}
    </TableSortLabel>
  </TableCell>
)

interface ReportRowProps {
  report: FeedbackReportDto
  expanded: boolean
  onToggle: () => void
  onStatusChange: (status: FeedbackStatus) => void
  onSaveNotes: (notes: string) => void
  onMenuOpen: (e: React.MouseEvent<HTMLButtonElement>) => void
  isUpdating: boolean
}

const ReportRow = ({ report, expanded, onToggle, onStatusChange, onSaveNotes, onMenuOpen, isUpdating }: ReportRowProps) => {
  const [editNotes, setEditNotes] = useState(report.adminNotes ?? '')
  const notesChanged = editNotes !== (report.adminNotes ?? '')

  // Sync local state when report data refreshes (e.g. after save)
  useEffect(() => {
    setEditNotes(report.adminNotes ?? '')
  }, [report.adminNotes])

  const parsedContent = useMemo(() => {
    if (!report.contentJson) return null
    try {
      return JSON.parse(report.contentJson) as Record<string, string>
    } catch {
      return null
    }
  }, [report.contentJson])

  return (
    <>
      <TableRow
        hover
        onClick={onToggle}
        sx={{ cursor: 'pointer', '& > *': { borderBottom: expanded ? 'unset' : undefined } }}
      >
        <TableCell>
          <IconButton size="small">
            <FontAwesomeIcon icon={expanded ? faChevronDown : faChevronRight} size="sm" />
          </IconButton>
        </TableCell>
        <TableCell>
          <Typography variant="body2" fontFamily="monospace" fontSize={12}>
            {report.referenceNumber}
          </Typography>
        </TableCell>
        <TableCell>
          <Chip
            icon={<FontAwesomeIcon icon={typeIcons[report.type]} />}
            label={FeedbackTypeLabels[report.type]}
            color={typeChipColor[report.type]}
            size="small"
            variant="outlined"
          />
        </TableCell>
        <TableCell onClick={e => e.stopPropagation()}>
          <Select
            size="small"
            value={report.status}
            onChange={e => onStatusChange(e.target.value as FeedbackStatus)}
            disabled={isUpdating}
            sx={{ minWidth: 120, fontSize: 13 }}
          >
            {Object.values(FeedbackStatus).map(s => (
              <MenuItem key={s} value={s}>
                <Chip
                  label={FeedbackStatusLabels[s]}
                  color={statusChipColor[s]}
                  size="small"
                  sx={{ cursor: 'pointer' }}
                />
              </MenuItem>
            ))}
          </Select>
        </TableCell>
        <TableCell>
          <Typography variant="body2" noWrap sx={{ maxWidth: 300 }}>
            {report.title}
          </Typography>
        </TableCell>
        <TableCell>
          <Typography variant="body2" noWrap>
            {report.reporterName ?? report.reporterEmail}
          </Typography>
        </TableCell>
        <TableCell>
          <Typography variant="body2" color="text.secondary" noWrap>
            {report.orgName ?? '-'}
          </Typography>
        </TableCell>
        <TableCell>
          {report.gitHubIssueUrl ? (
            <Link
              href={report.gitHubIssueUrl}
              target="_blank"
              rel="noreferrer"
              onClick={e => e.stopPropagation()}
              sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}
            >
              <FontAwesomeIcon icon={faGithub as IconProp} />
              <Typography variant="body2">#{report.gitHubIssueNumber}</Typography>
            </Link>
          ) : (
            <Typography variant="body2" color="text.secondary">
              -
            </Typography>
          )}
        </TableCell>
        <TableCell>
          <Typography variant="body2" color="text.secondary">
            {new Date(report.createdAt).toLocaleDateString()}
          </Typography>
        </TableCell>
        <TableCell onClick={e => e.stopPropagation()}>
          <IconButton size="small" onClick={onMenuOpen}>
            <FontAwesomeIcon icon={faEllipsisVertical} />
          </IconButton>
        </TableCell>
      </TableRow>

      <TableRow>
        <TableCell sx={{ py: 0 }} colSpan={10}>
          <Collapse in={expanded} timeout="auto" unmountOnExit>
            <Box sx={{ py: 2, px: 2 }}>
              <Stack spacing={2}>
                {/* Content */}
                {parsedContent && (
                  <Box>
                    <Typography variant="subtitle2" gutterBottom>
                      Content
                    </Typography>
                    {Object.entries(parsedContent).map(([key, value]) =>
                      value ? (
                        <Box key={key} sx={{ mb: 1 }}>
                          <Typography variant="caption" color="text.secondary">
                            {formatPropertyName(key)}
                          </Typography>
                          <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                            {value}
                          </Typography>
                        </Box>
                      ) : null,
                    )}
                  </Box>
                )}

                {/* Context */}
                <Box>
                  <Typography variant="subtitle2" gutterBottom>
                    Context
                  </Typography>
                  <Stack direction="row" spacing={3} flexWrap="wrap" useFlexGap>
                    {report.severity && <ContextItem label="Severity" value={report.severity} />}
                    {report.userRole && <ContextItem label="Role" value={report.userRole} />}
                    {report.orgRole && <ContextItem label="Org Role" value={report.orgRole} />}
                    {report.appVersion && (
                      <ContextItem label="Version" value={report.appVersion} />
                    )}
                    {report.commitSha && (
                      <ContextItem label="Commit" value={report.commitSha} />
                    )}
                    {report.screenSize && (
                      <ContextItem label="Screen" value={report.screenSize} />
                    )}
                    {report.currentUrl && <ContextItem label="URL" value={report.currentUrl} />}
                    {report.exerciseName && (
                      <ContextItem label="Exercise" value={report.exerciseName} />
                    )}
                    {report.exerciseRole && (
                      <ContextItem label="Exercise Role" value={report.exerciseRole} />
                    )}
                  </Stack>
                </Box>

                {/* Admin Notes */}
                <Box>
                  <Typography variant="subtitle2" gutterBottom>
                    Admin Notes
                  </Typography>
                  <Stack direction="row" spacing={1} alignItems="flex-start">
                    <CobraTextField
                      size="small"
                      multiline
                      minRows={2}
                      maxRows={6}
                      fullWidth
                      placeholder="Add notes about this report..."
                      value={editNotes}
                      onChange={e => setEditNotes(e.target.value)}
                    />
                    <CobraPrimaryButton
                      size="small"
                      disabled={!notesChanged || isUpdating}
                      onClick={() => onSaveNotes(editNotes)}
                      startIcon={<FontAwesomeIcon icon={faSave} />}
                      sx={{ whiteSpace: 'nowrap' }}
                    >
                      Save
                    </CobraPrimaryButton>
                  </Stack>
                </Box>
              </Stack>
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  )
}

const ContextItem = ({ label, value }: { label: string; value: string }) => (
  <Box>
    <Typography variant="caption" color="text.secondary">
      {label}
    </Typography>
    <Typography variant="body2" fontSize={12}>
      {value}
    </Typography>
  </Box>
)

const TableSkeleton = () => (
  <TableContainer component={Paper}>
    <Table size="small">
      <TableHead>
        <TableRow>
          <TableCell width={40} />
          {['Ref #', 'Type', 'Status', 'Title', 'Reporter', 'Org', 'GitHub', 'Submitted', ''].map(
            h => (
              <TableCell key={h}>{h}</TableCell>
            ),
          )}
        </TableRow>
      </TableHead>
      <TableBody>
        {Array.from({ length: 5 }, (_, i) => (
          <TableRow key={i}>
            <TableCell>
              <Skeleton variant="circular" width={24} height={24} />
            </TableCell>
            <TableCell>
              <Skeleton variant="text" width={140} />
            </TableCell>
            <TableCell>
              <Skeleton variant="rounded" width={100} height={24} />
            </TableCell>
            <TableCell>
              <Skeleton variant="rounded" width={60} height={24} />
            </TableCell>
            <TableCell>
              <Skeleton variant="text" width={200} />
            </TableCell>
            <TableCell>
              <Skeleton variant="text" width={120} />
            </TableCell>
            <TableCell>
              <Skeleton variant="text" width={80} />
            </TableCell>
            <TableCell>
              <Skeleton variant="text" width={30} />
            </TableCell>
            <TableCell>
              <Skeleton variant="text" width={80} />
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  </TableContainer>
)

const EmptyState = ({ hasFilters }: { hasFilters: boolean }) => (
  <Paper
    sx={{
      py: 8,
      px: 4,
      textAlign: 'center',
      bgcolor: 'grey.50',
      border: '1px dashed',
      borderColor: 'grey.300',
    }}
  >
    <Box
      sx={{
        width: 80,
        height: 80,
        borderRadius: '50%',
        bgcolor: 'grey.200',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        mx: 'auto',
        mb: 2,
        fontSize: 32,
        color: 'grey.500',
      }}
    >
      <FontAwesomeIcon icon={faComments} />
    </Box>
    <Typography variant="h6" gutterBottom>
      {hasFilters ? 'No matching reports' : 'No feedback reports'}
    </Typography>
    <Typography variant="body2" color="text.secondary">
      {hasFilters
        ? 'Try adjusting your search or filters.'
        : 'Feedback reports from users will appear here.'}
    </Typography>
  </Paper>
)

const formatPropertyName = (name: string): string => {
  return name.replace(/([A-Z])/g, ' $1').replace(/^./, s => s.toUpperCase())
}
