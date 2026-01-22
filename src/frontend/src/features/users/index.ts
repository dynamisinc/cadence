/**
 * User Management Feature
 *
 * Public exports for the user management module.
 *
 * @module features/users
 */

// Pages
export { UserListPage } from './pages/UserListPage';

// Components
export { EditUserDialog } from './components/EditUserDialog';
export { RoleSelect } from './components/RoleSelect';

// Services
export { userService } from './services/userService';
export type { UserListParams } from './services/userService';

// Types
export type {
  UserDto,
  UserListResponse,
  UpdateUserRequest,
  ChangeRoleRequest,
  UserRole,
} from './types';
export { USER_ROLES } from './types';
