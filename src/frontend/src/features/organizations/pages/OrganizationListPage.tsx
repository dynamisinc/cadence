/**
 * OrganizationListPage - Admin page to view and manage all organizations
 *
 * Features:
 * - List all organizations with search/filter/sort
 * - Status indicators (Active, Archived, Inactive)
 * - User and exercise counts
 * - Navigate to create/edit
 *
 * @module features/organizations/pages
 * @see docs/features/organization-management/OM-01-organization-list.md
 */
import { useState } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  Paper,
  InputAdornment,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Skeleton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faSearch, faBuilding, faHome, faShieldHalved } from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from 'react-router-dom'
import { CobraPrimaryButton, CobraTextField } from '@/theme/styledComponents'
import { PageHeader } from '@/shared/components'
import { useBreadcrumbs } from '@/core/contexts'
import CobraStyles from '@/theme/CobraStyles'
import { useOrganizations } from '../hooks/useOrganizations'
import { formatDate } from '@/shared/utils/dateUtils'
import { StatusChip } from '@/shared/components/StatusChip'
import type { OrgStatus } from '../types'

type SortField = 'name' | 'slug' | 'status' | 'userCount' | 'exerciseCount' | 'createdAt'
type SortDirection = 'asc' | 'desc'

export const OrganizationListPage: FC = () => {
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'System Settings', path: '/admin', icon: faShieldHalved },
    { label: 'Organizations' },
  ])

  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<OrgStatus | ''>('')
  const [sortField, setSortField] = useState<SortField>('name')
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc')

  const { data, isLoading, error } = useOrganizations({
    search: search || undefined,
    status: statusFilter || undefined,
    sortBy: sortField,
    sortDir: sortDirection,
  })

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortDirection('asc')
    }
  }

  const handleRowClick = (id: string) => {
    navigate(`/admin/organizations/${id}`)
  }

  const formatDateSafe = (dateString: string) => {
    try {
      return formatDate(dateString)
    } catch {
      return dateString
    }
  }

  if (error) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography color="error">
          Failed to load organizations. Please try again.
        </Typography>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="Organization Management"
        icon={faBuilding}
        subtitle="View, create, and manage all organizations on the platform"
        actions={
          <CobraPrimaryButton
            startIcon={<FontAwesomeIcon icon={faPlus} />}
            onClick={() => navigate('/admin/organizations/new')}
          >
            Create Organization
          </CobraPrimaryButton>
        }
      />

      {/* Filters */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
        <CobraTextField
          placeholder="Search organizations..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          size="small"
          sx={{ minWidth: 300 }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <FontAwesomeIcon icon={faSearch} />
              </InputAdornment>
            ),
          }}
        />
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Status</InputLabel>
          <Select
            value={statusFilter}
            label="Status"
            onChange={e => setStatusFilter(e.target.value as OrgStatus | '')}
          >
            <MenuItem value="">All</MenuItem>
            <MenuItem value="Active">Active</MenuItem>
            <MenuItem value="Archived">Archived</MenuItem>
            <MenuItem value="Inactive">Inactive</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {/* Table */}
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>
                <TableSortLabel
                  active={sortField === 'name'}
                  direction={sortField === 'name' ? sortDirection : 'asc'}
                  onClick={() => handleSort('name')}
                >
                  Name
                </TableSortLabel>
              </TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortField === 'slug'}
                  direction={sortField === 'slug' ? sortDirection : 'asc'}
                  onClick={() => handleSort('slug')}
                >
                  Slug
                </TableSortLabel>
              </TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortField === 'status'}
                  direction={sortField === 'status' ? sortDirection : 'asc'}
                  onClick={() => handleSort('status')}
                >
                  Status
                </TableSortLabel>
              </TableCell>
              <TableCell align="right">
                <TableSortLabel
                  active={sortField === 'userCount'}
                  direction={sortField === 'userCount' ? sortDirection : 'asc'}
                  onClick={() => handleSort('userCount')}
                >
                  Users
                </TableSortLabel>
              </TableCell>
              <TableCell align="right">
                <TableSortLabel
                  active={sortField === 'exerciseCount'}
                  direction={sortField === 'exerciseCount' ? sortDirection : 'asc'}
                  onClick={() => handleSort('exerciseCount')}
                >
                  Exercises
                </TableSortLabel>
              </TableCell>
              <TableCell>
                <TableSortLabel
                  active={sortField === 'createdAt'}
                  direction={sortField === 'createdAt' ? sortDirection : 'asc'}
                  onClick={() => handleSort('createdAt')}
                >
                  Created
                </TableSortLabel>
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading ? (
              // Skeleton loading
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell><Skeleton /></TableCell>
                  <TableCell><Skeleton /></TableCell>
                  <TableCell><Skeleton width={80} /></TableCell>
                  <TableCell><Skeleton width={40} /></TableCell>
                  <TableCell><Skeleton width={40} /></TableCell>
                  <TableCell><Skeleton width={100} /></TableCell>
                </TableRow>
              ))
            ) : data?.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                  <Typography color="text.secondary">
                    {search || statusFilter
                      ? 'No organizations match your filters.'
                      : 'No organizations yet. Create your first organization to get started.'}
                  </Typography>
                  {!search && !statusFilter && (
                    <CobraPrimaryButton
                      startIcon={<FontAwesomeIcon icon={faPlus} />}
                      onClick={() => navigate('/admin/organizations/new')}
                      sx={{ mt: 2 }}
                    >
                      Create Organization
                    </CobraPrimaryButton>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              data?.items.map(org => (
                <TableRow
                  key={org.id}
                  hover
                  onClick={() => handleRowClick(org.id)}
                  sx={{
                    cursor: 'pointer',
                    opacity: org.status === 'Inactive' ? 0.5 : 1,
                  }}
                >
                  <TableCell>
                    <Typography fontWeight={500}>{org.name}</Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {org.slug}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <StatusChip status={org.status} />
                  </TableCell>
                  <TableCell align="right">{org.userCount}</TableCell>
                  <TableCell align="right">{org.exerciseCount}</TableCell>
                  <TableCell>{formatDateSafe(org.createdAt)}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Total count */}
      {data && data.totalCount > 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
          {data.totalCount} organization{data.totalCount !== 1 ? 's' : ''}
        </Typography>
      )}
    </Box>
  )
}

export default OrganizationListPage
