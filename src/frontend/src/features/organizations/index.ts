/**
 * Organization Management Feature - Central Export
 *
 * @module features/organizations
 */

// Types
export type {
  Organization,
  OrganizationListItem,
  OrganizationMembership,
  OrgStatus,
  OrgRole,
  CreateOrganizationRequest,
  UpdateOrganizationRequest,
  SlugCheckResponse,
} from './types'

// Services
export { organizationService } from './services/organizationService'

// Hooks
export {
  useOrganizations,
  useOrganization,
  useCurrentOrganization,
  useCreateOrganization,
  useUpdateOrganization,
  useUpdateCurrentOrganization,
  useArchiveOrganization,
  useDeactivateOrganization,
  useRestoreOrganization,
  useCheckSlug,
  organizationKeys,
} from './hooks/useOrganizations'

// Pages
export {
  OrganizationListPage,
  CreateOrganizationPage,
  EditOrganizationPage,
} from './pages'
