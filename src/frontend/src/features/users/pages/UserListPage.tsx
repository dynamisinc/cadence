/**
 * UserListPage Component
 *
 * Administrative page for managing all users in the system.
 * Displays paginated user list with search, filters, and actions.
 *
 * Features:
 * - View all users with pagination
 * - Search by name or email
 * - Filter by role
 * - Edit user details
 * - Change user roles
 * - Deactivate/reactivate accounts
 * - View and manage organization memberships
 *
 * @module features/users/pages
 * @see authentication/S10 View User List
 * @see authentication/S11 Edit User Details
 * @see authentication/S12 Deactivate User Account
 * @see authentication/S13 Global Role Assignment
 */
import { useState, Fragment } from 'react'
import type { FC } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TablePagination,
  TableContainer,
  Chip,
  MenuItem,
  Select,
  InputAdornment,
  Alert,
  Stack,
  Collapse,
  Paper,
  Tooltip,
  Skeleton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPen,
  faUserSlash,
  faUserCheck,
  faMagnifyingGlass,
  faChevronDown,
  faChevronRight,
  faBuilding,
  faPlus,
  faTrash,
  faUsers,
  faHome,
  faShieldHalved,
} from '@fortawesome/free-solid-svg-icons'
import { CobraTextField, CobraIconButton, CobraPrimaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { PageHeader } from '@/shared/components'
import { useBreadcrumbs } from '@/core/contexts'
import { formatDate } from '../../../shared/utils/dateUtils'
import type { UserMembershipDto } from '../types'
import { EditUserDialog } from '../components/EditUserDialog'
import { RoleSelect } from '../components/RoleSelect'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import { USER_ROLES } from '../types'
import { organizationService } from '../../organizations/services/organizationService'
import type { OrganizationListItem, OrgRole } from '../../organizations/types'
import { AddUserToOrgDialog } from '../components/AddUserToOrgDialog'
import {
  useUserList,
  useUserMemberships,
  useUpdateUser,
  useChangeUserRole,
  useDeactivateUser,
  useReactivateUser,
  userKeys,
} from '../hooks/useUsers'
import { useOrganizations } from '../../organizations/hooks/useOrganizations'
import { notify } from '@/shared/utils/notify'

// Compact icon button style for table actions
const compactIconButtonSx = {
  padding: '4px',
  fontSize: '0.875rem',
  '& svg': {
    width: '14px',
    height: '14px',
  },
}

// Role badge colors
const getRoleColor = (role: string) => {
  switch (role) {
    case 'OrgAdmin':
      return 'primary'
    case 'OrgManager':
      return 'secondary'
    default:
      return 'default'
  }
}

/**
 * ExpandedUserRow - Loads and displays membership data for an expanded user row.
 *
 * Uses React Query so each user's memberships are independently cached,
 * fetched on demand, and automatically kept up to date.
 */
interface ExpandedUserRowProps {
  userId: string;
  userName: string;
  availableOrgs: OrganizationListItem[];
  onAddToOrg: (userId: string) => void;
}

const ExpandedUserRow: FC<ExpandedUserRowProps> = ({ userId, userName: _userName, availableOrgs, onAddToOrg }) => {
  const queryClient = useQueryClient()
  const { data: memberships = [], isLoading } = useUserMemberships(userId)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const handleRemoveFromOrg = async (membership: UserMembershipDto) => {
    try {
      await organizationService.removeMember(membership.organizationId, membership.id)
      queryClient.invalidateQueries({ queryKey: userKeys.memberships(userId) })
    } catch {
      setErrorMessage('Failed to remove user from organization')
    }
  }

  const handleOrgRoleChange = async (membership: UserMembershipDto, newRole: string) => {
    try {
      await organizationService.updateMemberRole(
        membership.organizationId,
        membership.id,
        { role: newRole as OrgRole },
      )
      queryClient.invalidateQueries({ queryKey: userKeys.memberships(userId) })
    } catch {
      setErrorMessage('Failed to update role')
    }
  }

  return (
    <Box sx={{ py: 2, px: 4, bgcolor: 'grey.50' }}>
      {errorMessage && (
        <Alert severity="error" onClose={() => setErrorMessage(null)} sx={{ mb: 1 }}>
          {errorMessage}
        </Alert>
      )}
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 1.5 }}>
        <Typography variant="subtitle2" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon icon={faBuilding} />
          Organization Memberships
        </Typography>
        <CobraPrimaryButton
          size="small"
          startIcon={<FontAwesomeIcon icon={faPlus} />}
          onClick={() => onAddToOrg(userId)}
          disabled={availableOrgs.length === 0}
          sx={{ fontSize: '0.75rem', py: 0.5 }}
        >
          Add to Org
        </CobraPrimaryButton>
      </Stack>

      {isLoading ? (
        <Stack spacing={1}>
          <Skeleton height={32} />
          <Skeleton height={32} />
        </Stack>
      ) : memberships.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>
          Not a member of any organization
        </Typography>
      ) : (
        <Table size="small">
          <TableHead>
            <TableRow sx={{ '& th': { py: 0.75, fontWeight: 500, bgcolor: 'transparent' } }}>
              <TableCell>Organization</TableCell>
              <TableCell width={150}>Role</TableCell>
              <TableCell width={120}>Joined</TableCell>
              <TableCell width={60} align="right"></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {memberships.map(membership => (
              <TableRow key={membership.id} sx={{ '& td': { py: 0.5 } }}>
                <TableCell>
                  <Box>
                    <Typography variant="body2">{membership.organizationName}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {membership.organizationSlug}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell>
                  <Select
                    value={membership.role}
                    onChange={e => handleOrgRoleChange(membership, e.target.value)}
                    size="small"
                    sx={{ minWidth: 120, fontSize: '0.875rem' }}
                  >
                    <MenuItem value="OrgAdmin">OrgAdmin</MenuItem>
                    <MenuItem value="OrgManager">OrgManager</MenuItem>
                    <MenuItem value="OrgUser">OrgUser</MenuItem>
                  </Select>
                </TableCell>
                <TableCell>
                  <Typography variant="caption" color="text.secondary">
                    {formatDate(membership.joinedAt)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Tooltip title="Remove from organization">
                    <CobraIconButton
                      onClick={() => handleRemoveFromOrg(membership)}
                      sx={{ ...compactIconButtonSx, color: 'error.main' }}
                    >
                      <FontAwesomeIcon icon={faTrash} />
                    </CobraIconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Box>
  )
}

/**
 * User management page
 * Administrators only
 */
export const UserListPage: FC = () => {
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'System Settings', path: '/admin', icon: faShieldHalved },
    { label: 'Users' },
  ])

  // Pagination and filter state
  const [page, setPage] = useState(0) // 0-indexed for TablePagination
  const [pageSize, setPageSize] = useState(20)
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [orgFilter, setOrgFilter] = useState('')

  // Dialog state
  const [editingUserId, setEditingUserId] = useState<string | null>(null)
  const [expandedUserId, setExpandedUserId] = useState<string | null>(null)
  const [addToOrgDialogUserId, setAddToOrgDialogUserId] = useState<string | null>(null)
  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean;
    title: string;
    message: string;
    onConfirm: () => void;
  }>({ open: false, title: '', message: '', onConfirm: () => {} })
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  // React Query data fetching
  const userListParams = {
    page: page + 1, // API is 1-indexed
    pageSize,
    search: search || undefined,
    role: roleFilter || undefined,
    status: statusFilter || undefined,
    organizationId: orgFilter || undefined,
  }

  const {
    data: userListData,
    isLoading,
    isFetching,
  } = useUserList(userListParams)

  const users = userListData?.users ?? []
  const totalCount = userListData?.pagination.totalCount ?? 0

  // Organizations for filter dropdown and add-to-org dialog
  const { data: orgsData } = useOrganizations({ status: 'Active' })
  const organizations = orgsData?.items ?? []

  // Mutations
  const updateUserMutation = useUpdateUser()
  const changeRoleMutation = useChangeUserRole()
  const deactivateUserMutation = useDeactivateUser()
  const reactivateUserMutation = useReactivateUser()
  const queryClient = useQueryClient()

  // Find the user being edited
  const editingUser = editingUserId ? users.find(u => u.id === editingUserId) ?? null : null

  /**
   * Get organizations the user is NOT already a member of
   */
  const getAvailableOrgs = (userId: string) => {
    const membershipsData = queryClient.getQueryData<UserMembershipDto[]>(
      userKeys.memberships(userId),
    )
    const memberOrgIds = new Set((membershipsData ?? []).map(m => m.organizationId))
    return organizations.filter(org => !memberOrgIds.has(org.id))
  }

  /**
   * Handle role change for a user
   */
  const handleRoleChange = (userId: string, newRole: string) => {
    changeRoleMutation.mutate(
      { id: userId, request: { systemRole: newRole } },
      {
        onError: (err: unknown) => {
          const axiosError = err as { response?: { data?: { error?: string } } }
          if (axiosError.response?.data?.error === 'last_administrator') {
            setErrorMessage('Cannot remove the last Administrator. Assign another Administrator first.')
          } else {
            setErrorMessage('Failed to change user role')
          }
        },
      },
    )
  }

  /**
   * Show confirmation dialog for deactivation
   */
  const handleDeactivate = (userId: string, displayName: string) => {
    setConfirmDialog({
      open: true,
      title: 'Deactivate User',
      message: `Are you sure you want to deactivate ${displayName}? They will no longer be able to log in.`,
      onConfirm: () => {
        deactivateUserMutation.mutate(userId, {
          onSettled: () => setConfirmDialog(prev => ({ ...prev, open: false })),
          onError: () => setErrorMessage('Failed to deactivate user'),
        })
      },
    })
  }

  /**
   * Reactivate a deactivated user
   */
  const handleReactivate = (userId: string) => {
    reactivateUserMutation.mutate(userId, {
      onError: () => setErrorMessage('Failed to reactivate user'),
    })
  }

  /**
   * Handle user edit save
   */
  const handleEditSave = async (updates: { displayName?: string }) => {
    if (editingUserId) {
      await updateUserMutation.mutateAsync(
        { id: editingUserId, request: updates },
        {
          onError: () => setErrorMessage('Failed to update user'),
        },
      )
      setEditingUserId(null)
    }
  }

  /**
   * Handle adding user to organization
   */
  const handleAddToOrg = async (userId: string, orgId: string, role: OrgRole) => {
    try {
      const user = users.find(u => u.id === userId)
      if (!user) return

      await organizationService.addMember(orgId, { email: user.email, role })
      // Invalidate this user's memberships cache to trigger a fresh fetch
      queryClient.invalidateQueries({ queryKey: userKeys.memberships(userId) })
      setAddToOrgDialogUserId(null)
      notify.success('User added to organization')
    } catch (error: unknown) {
      const axiosError = error as { response?: { data?: { message?: string } } }
      throw new Error(axiosError.response?.data?.message || 'Failed to add user to organization')
    }
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader title="User Management" icon={faUsers} subtitle="View and manage platform users, roles, and organization memberships" />

      {/* Error display */}
      {errorMessage && (
        <Alert severity="error" onClose={() => setErrorMessage(null)} sx={{ mb: 2 }}>
          {errorMessage}
        </Alert>
      )}

      {/* Compact Filters */}
      <Stack direction="row" spacing={1.5} sx={{ mb: 2 }} flexWrap="wrap" useFlexGap>
        <CobraTextField
          placeholder="Search name or email..."
          value={search}
          onChange={e => {
            setSearch(e.target.value)
            setPage(0) // Reset to first page on search
          }}
          size="small"
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <FontAwesomeIcon icon={faMagnifyingGlass} style={{ fontSize: '0.875rem' }} />
              </InputAdornment>
            ),
          }}
          sx={{ width: 280 }}
        />
        <Select
          value={roleFilter}
          onChange={e => {
            setRoleFilter(e.target.value)
            setPage(0)
          }}
          displayEmpty
          size="small"
          sx={{ minWidth: 120 }}
        >
          <MenuItem value="">All Roles</MenuItem>
          {USER_ROLES.map(role => (
            <MenuItem key={role} value={role}>
              {role}
            </MenuItem>
          ))}
        </Select>
        <Select
          value={statusFilter}
          onChange={e => {
            setStatusFilter(e.target.value)
            setPage(0)
          }}
          displayEmpty
          size="small"
          sx={{ minWidth: 110 }}
        >
          <MenuItem value="">All Status</MenuItem>
          <MenuItem value="Active">Active</MenuItem>
          <MenuItem value="Disabled">Inactive</MenuItem>
          <MenuItem value="Pending">Pending</MenuItem>
        </Select>
        <Select
          value={orgFilter}
          onChange={e => {
            setOrgFilter(e.target.value)
            setPage(0)
          }}
          displayEmpty
          size="small"
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">All Organizations</MenuItem>
          {organizations.map(org => (
            <MenuItem key={org.id} value={org.id}>
              {org.name}
            </MenuItem>
          ))}
        </Select>
      </Stack>

      {/* Users Table */}
      <TableContainer component={Paper} variant="outlined" sx={{ opacity: isFetching ? 0.7 : 1 }}>
        <Table size="small">
          <TableHead>
            <TableRow sx={{ '& th': { fontWeight: 600, py: 1.5 } }}>
              <TableCell width={32}></TableCell>
              <TableCell>User</TableCell>
              <TableCell width={120}>System Role</TableCell>
              <TableCell width={200}>Organizations</TableCell>
              <TableCell width={80}>Status</TableCell>
              <TableCell width={100}>Last Login</TableCell>
              <TableCell width={80} align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading ? (
              // Loading skeleton
              Array.from({ length: 5 }).map((_, idx) => (
                <TableRow key={idx}>
                  <TableCell><Skeleton width={20} /></TableCell>
                  <TableCell><Skeleton /></TableCell>
                  <TableCell><Skeleton width={80} /></TableCell>
                  <TableCell><Skeleton width={150} /></TableCell>
                  <TableCell><Skeleton width={60} /></TableCell>
                  <TableCell><Skeleton width={80} /></TableCell>
                  <TableCell><Skeleton width={60} /></TableCell>
                </TableRow>
              ))
            ) : (
              users.map(user => {
                const memberships = queryClient.getQueryData<UserMembershipDto[]>(
                  userKeys.memberships(user.id),
                )
                return (
                  <Fragment key={user.id}>
                    <TableRow
                      hover
                      sx={{
                        '& td': { py: 1 },
                        cursor: 'pointer',
                        bgcolor: expandedUserId === user.id ? 'action.selected' : 'inherit',
                      }}
                      onClick={() =>
                        setExpandedUserId(prev => prev === user.id ? null : user.id)
                      }
                    >
                      {/* Expand indicator */}
                      <TableCell sx={{ pr: 0 }}>
                        <FontAwesomeIcon
                          icon={expandedUserId === user.id ? faChevronDown : faChevronRight}
                          style={{ fontSize: '0.75rem', color: '#666' }}
                        />
                      </TableCell>

                      {/* User info - compact */}
                      <TableCell>
                        <Box>
                          <Typography variant="body2" sx={{ fontWeight: 500 }}>
                            {user.displayName}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {user.email}
                          </Typography>
                        </Box>
                      </TableCell>

                      {/* System Role */}
                      <TableCell onClick={e => e.stopPropagation()}>
                        <RoleSelect
                          value={user.systemRole}
                          onChange={role => handleRoleChange(user.id, role)}
                        />
                      </TableCell>

                      {/* Organizations - inline chips */}
                      <TableCell>
                        <Stack direction="row" spacing={0.5} flexWrap="wrap" gap={0.5}>
                          {memberships === undefined ? (
                            // Memberships not yet loaded - show placeholder
                            <Typography variant="caption" color="text.disabled">
                              Click to view
                            </Typography>
                          ) : memberships.length === 0 ? (
                            // No memberships
                            <Typography variant="caption" color="text.disabled">
                              None
                            </Typography>
                          ) : (
                            // Show membership chips
                            <>
                              {memberships.slice(0, 2).map(m => (
                                <Tooltip key={m.id} title={`${m.organizationName} (${m.role})`}>
                                  <Chip
                                    size="small"
                                    label={m.organizationSlug || m.organizationName.substring(0, 12)}
                                    color={getRoleColor(m.role)}
                                    sx={{ height: 20, fontSize: '0.7rem' }}
                                  />
                                </Tooltip>
                              ))}
                              {memberships.length > 2 && (
                                <Chip
                                  size="small"
                                  label={`+${memberships.length - 2}`}
                                  variant="outlined"
                                  sx={{ height: 20, fontSize: '0.7rem' }}
                                />
                              )}
                            </>
                          )}
                        </Stack>
                      </TableCell>

                      {/* Status */}
                      <TableCell>
                        <Chip
                          label={user.status}
                          color={user.status === 'Active' ? 'success' : 'default'}
                          size="small"
                          sx={{ height: 22, fontSize: '0.75rem' }}
                        />
                      </TableCell>

                      {/* Last Login */}
                      <TableCell>
                        <Typography variant="caption" color="text.secondary">
                          {user.lastLoginAt
                            ? formatDate(user.lastLoginAt)
                            : 'Never'}
                        </Typography>
                      </TableCell>

                      {/* Actions - compact */}
                      <TableCell align="right" onClick={e => e.stopPropagation()}>
                        <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                          <Tooltip title="Edit user">
                            <CobraIconButton
                              onClick={() => setEditingUserId(user.id)}
                              aria-label="Edit user"
                              sx={compactIconButtonSx}
                            >
                              <FontAwesomeIcon icon={faPen} />
                            </CobraIconButton>
                          </Tooltip>
                          {user.status === 'Active' ? (
                            <Tooltip title="Deactivate user">
                              <CobraIconButton
                                onClick={() => handleDeactivate(user.id, user.displayName)}
                                aria-label="Deactivate user"
                                sx={{ ...compactIconButtonSx, color: 'warning.main' }}
                              >
                                <FontAwesomeIcon icon={faUserSlash} />
                              </CobraIconButton>
                            </Tooltip>
                          ) : (
                            <Tooltip title="Reactivate user">
                              <CobraIconButton
                                onClick={() => handleReactivate(user.id)}
                                aria-label="Reactivate user"
                                sx={{ ...compactIconButtonSx, color: 'success.main' }}
                              >
                                <FontAwesomeIcon icon={faUserCheck} />
                              </CobraIconButton>
                            </Tooltip>
                          )}
                        </Stack>
                      </TableCell>
                    </TableRow>

                    {/* Expanded row for org management */}
                    <TableRow key={`${user.id}-expanded`}>
                      <TableCell
                        colSpan={7}
                        sx={{ py: 0, borderBottom: expandedUserId === user.id ? 1 : 0 }}
                      >
                        <Collapse in={expandedUserId === user.id} timeout="auto" unmountOnExit>
                          <ExpandedUserRow
                            userId={user.id}
                            userName={user.displayName}
                            availableOrgs={getAvailableOrgs(user.id)}
                            onAddToOrg={setAddToOrgDialogUserId}
                          />
                        </Collapse>
                      </TableCell>
                    </TableRow>
                  </Fragment>
                )
              })
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Pagination */}
      <TablePagination
        component="div"
        count={totalCount}
        page={page}
        onPageChange={(_, newPage) => setPage(newPage)}
        rowsPerPage={pageSize}
        onRowsPerPageChange={e => {
          setPageSize(parseInt(e.target.value))
          setPage(0)
        }}
        rowsPerPageOptions={[10, 20, 50]}
        sx={{ borderTop: 1, borderColor: 'divider' }}
      />

      {/* Edit Dialog */}
      {editingUser && (
        <EditUserDialog
          user={editingUser}
          onClose={() => setEditingUserId(null)}
          onSave={handleEditSave}
        />
      )}

      {/* Add to Org Dialog */}
      {addToOrgDialogUserId && (
        <AddUserToOrgDialog
          userId={addToOrgDialogUserId}
          userName={users.find(u => u.id === addToOrgDialogUserId)?.displayName || ''}
          availableOrgs={getAvailableOrgs(addToOrgDialogUserId)}
          onClose={() => setAddToOrgDialogUserId(null)}
          onAdd={handleAddToOrg}
        />
      )}

      {/* Confirm Dialog */}
      <ConfirmDialog
        open={confirmDialog.open}
        title={confirmDialog.title}
        message={confirmDialog.message}
        severity="danger"
        confirmLabel="Confirm"
        onConfirm={confirmDialog.onConfirm}
        onCancel={() => setConfirmDialog(prev => ({ ...prev, open: false }))}
      />
    </Box>
  )
}
