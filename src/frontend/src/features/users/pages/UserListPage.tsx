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
 *
 * @module features/users/pages
 * @see authentication/S10 View User List
 * @see authentication/S11 Edit User Details
 * @see authentication/S12 Deactivate User Account
 * @see authentication/S13 Global Role Assignment
 */
import { FC, useState, useEffect } from 'react'
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TablePagination,
  IconButton,
  Chip,
  MenuItem,
  Select,
  InputAdornment,
  Alert,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPen,
  faUserSlash,
  faUserCheck,
  faMagnifyingGlass,
} from '@fortawesome/free-solid-svg-icons'
import { CobraTextField } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { userService } from '../services/userService'
import type { UserDto } from '../types'
import { EditUserDialog } from '../components/EditUserDialog'
import { RoleSelect } from '../components/RoleSelect'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import { USER_ROLES } from '../types'

/**
 * User management page
 * Administrators only
 */
export const UserListPage: FC = () => {
  const [users, setUsers] = useState<UserDto[]>([])
  const [pagination, setPagination] = useState({
    page: 0, // 0-indexed for TablePagination
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
  })
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState('')
  const [_isLoading, setIsLoading] = useState(true)
  const [editingUser, setEditingUser] = useState<UserDto | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean;
    title: string;
    message: string;
    onConfirm: () => void;
  }>({ open: false, title: '', message: '', onConfirm: () => {} })

  /**
   * Load users from API with current filters and pagination
   */
  const loadUsers = async () => {
    setIsLoading(true)
    setErrorMessage(null)
    try {
      const response = await userService.getUsers({
        page: pagination.page + 1, // API is 1-indexed
        pageSize: pagination.pageSize,
        search: search || undefined,
        role: roleFilter || undefined,
      })
      setUsers(response.users)
      setPagination(prev => ({
        ...prev,
        totalCount: response.pagination.totalCount,
        totalPages: response.pagination.totalPages,
      }))
    } catch {
      setErrorMessage('Failed to load users')
    } finally {
      setIsLoading(false)
    }
  }

  // Load users when filters or pagination change
  useEffect(() => {
    loadUsers()
  }, [pagination.page, pagination.pageSize, search, roleFilter])

  /**
   * Handle role change for a user
   */
  const handleRoleChange = async (user: UserDto, newRole: string) => {
    try {
      await userService.changeRole(user.id, { systemRole: newRole })
      loadUsers()
    } catch (error: unknown) {
      const axiosError = error as { response?: { data?: { error?: string } } }
      if (axiosError.response?.data?.error === 'last_administrator') {
        setErrorMessage('Cannot remove the last Administrator. Assign another Administrator first.')
      } else {
        setErrorMessage('Failed to change user role')
      }
    }
  }

  /**
   * Show confirmation dialog for deactivation
   */
  const handleDeactivate = (user: UserDto) => {
    setConfirmDialog({
      open: true,
      title: 'Deactivate User',
      message: `Are you sure you want to deactivate ${user.displayName}? They will no longer be able to log in.`,
      onConfirm: async () => {
        try {
          await userService.deactivateUser(user.id)
          loadUsers()
          setConfirmDialog(prev => ({ ...prev, open: false }))
        } catch {
          setErrorMessage('Failed to deactivate user')
          setConfirmDialog(prev => ({ ...prev, open: false }))
        }
      },
    })
  }

  /**
   * Reactivate a deactivated user
   */
  const handleReactivate = async (user: UserDto) => {
    try {
      await userService.reactivateUser(user.id)
      loadUsers()
    } catch {
      setErrorMessage('Failed to reactivate user')
    }
  }

  /**
   * Handle user edit save
   */
  const handleEditSave = async (updates: { displayName?: string }) => {
    if (editingUser) {
      await userService.updateUser(editingUser.id, updates)
      loadUsers()
      setEditingUser(null)
    }
  }

  return (
    <Box sx={{ p: CobraStyles.Padding.MainWindow }}>
      <Typography variant="h4" gutterBottom>
        User Management
      </Typography>

      {/* Error display */}
      {errorMessage && (
        <Alert severity="error" onClose={() => setErrorMessage(null)} sx={{ mb: 2 }}>
          {errorMessage}
        </Alert>
      )}

      {/* Filters */}
      <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
        <CobraTextField
          placeholder="Search by name or email"
          value={search}
          onChange={e => setSearch(e.target.value)}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <FontAwesomeIcon icon={faMagnifyingGlass} />
              </InputAdornment>
            ),
          }}
          sx={{ minWidth: 300 }}
        />
        <Select
          value={roleFilter}
          onChange={e => setRoleFilter(e.target.value)}
          displayEmpty
          size="small"
          sx={{ minWidth: 150 }}
        >
          <MenuItem value="">All Roles</MenuItem>
          {USER_ROLES.map(role => (
            <MenuItem key={role} value={role}>
              {role}
            </MenuItem>
          ))}
        </Select>
      </Stack>

      {/* Users Table */}
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Email</TableCell>
            <TableCell>Role</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Last Login</TableCell>
            <TableCell align="right">Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {users.map(user => (
            <TableRow key={user.id}>
              <TableCell>{user.displayName}</TableCell>
              <TableCell>{user.email}</TableCell>
              <TableCell>
                <RoleSelect
                  value={user.systemRole}
                  onChange={role => handleRoleChange(user, role)}
                />
              </TableCell>
              <TableCell>
                <Chip
                  label={user.status}
                  color={user.status === 'Active' ? 'success' : 'default'}
                  size="small"
                />
              </TableCell>
              <TableCell>
                {user.lastLoginAt
                  ? new Date(user.lastLoginAt).toLocaleDateString()
                  : 'Never'}
              </TableCell>
              <TableCell align="right">
                <IconButton
                  onClick={() => setEditingUser(user)}
                  aria-label="Edit user"
                  title="Edit user"
                >
                  <FontAwesomeIcon icon={faPen} />
                </IconButton>
                {user.status === 'Active' ? (
                  <IconButton
                    onClick={() => handleDeactivate(user)}
                    aria-label="Deactivate user"
                    title="Deactivate user"
                    color="warning"
                  >
                    <FontAwesomeIcon icon={faUserSlash} />
                  </IconButton>
                ) : (
                  <IconButton
                    onClick={() => handleReactivate(user)}
                    aria-label="Reactivate user"
                    title="Reactivate user"
                    color="success"
                  >
                    <FontAwesomeIcon icon={faUserCheck} />
                  </IconButton>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      {/* Pagination */}
      <TablePagination
        component="div"
        count={pagination.totalCount}
        page={pagination.page}
        onPageChange={(_, page) => setPagination(prev => ({ ...prev, page }))}
        rowsPerPage={pagination.pageSize}
        onRowsPerPageChange={e =>
          setPagination(prev => ({ ...prev, pageSize: parseInt(e.target.value), page: 0 }))
        }
        rowsPerPageOptions={[10, 20, 50]}
      />

      {/* Edit Dialog */}
      {editingUser && (
        <EditUserDialog
          user={editingUser}
          onClose={() => setEditingUser(null)}
          onSave={handleEditSave}
        />
      )}

      {/* Confirm Dialog */}
      <ConfirmDialog
        open={confirmDialog.open}
        title={confirmDialog.title}
        message={confirmDialog.message}
        severity="danger"
        confirmLabel="Deactivate"
        onConfirm={confirmDialog.onConfirm}
        onCancel={() => setConfirmDialog(prev => ({ ...prev, open: false }))}
      />
    </Box>
  )
}
