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
  useUpdateCurrentApprovalPolicy,
  useArchiveOrganization,
  useDeactivateOrganization,
  useRestoreOrganization,
  useCheckSlug,
  organizationKeys,
} from './hooks/useOrganizations'

export {
  useApprovalPermissions,
  useUpdateApprovalPermissions,
  useCurrentOrgApprovalPermissions,
  useUpdateCurrentOrgApprovalPermissions,
  approvalPermissionKeys,
  currentOrgApprovalPermissionKeys,
} from './hooks/useApprovalPermissions'

export {
  useCurrentOrgMembers,
  useAddCurrentOrgMember,
  useUpdateCurrentOrgMemberRole,
  useRemoveCurrentOrgMember,
  currentOrgMemberKeys,
} from './hooks/useCurrentOrgMembers'

// Pages
export {
  // SysAdmin pages
  OrganizationListPage,
  CreateOrganizationPage,
  EditOrganizationPage,
  // OrgAdmin pages
  OrganizationDetailsPage,
  OrganizationMembersPage,
  OrganizationApprovalPage,
  OrganizationSettingsPage,
} from './pages'
