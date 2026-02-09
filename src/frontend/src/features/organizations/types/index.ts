/**
 * Organization Management Types
 *
 * Type definitions for organization entities, DTOs, and API requests/responses.
 *
 * @module features/organizations/types
 */

export interface Organization {
  id: string;
  name: string;
  slug: string;
  description?: string;
  contactEmail?: string;
  status: OrgStatus;
  injectApprovalPolicy: string;
  createdAt: string;
  updatedAt: string;
}

export interface OrganizationListItem {
  id: string;
  name: string;
  slug: string;
  status: OrgStatus;
  userCount: number;
  exerciseCount: number;
  createdAt: string;
}

export type OrgStatus = 'Active' | 'Archived' | 'Inactive'

export type OrgRole = 'OrgAdmin' | 'OrgManager' | 'OrgUser'

export interface OrganizationMembership {
  id: string;
  userId: string;
  organizationId: string;
  organizationName: string;
  organizationSlug: string;
  role: OrgRole;
  joinedAt: string;
  isCurrent: boolean;
}

export interface CreateOrganizationRequest {
  name: string;
  slug: string;
  description?: string;
  contactEmail?: string;
  firstAdminEmail: string;
}

export interface UpdateOrganizationRequest {
  name: string;
  description?: string;
  contactEmail?: string;
}

export interface SlugCheckResponse {
  available: boolean;
  suggestion?: string;
}

export interface OrgMember {
  membershipId: string;
  userId: string;
  email: string;
  displayName: string;
  role: string;
  joinedAt: string;
}

export interface AddMemberRequest {
  email: string;
  role: OrgRole;
}

export interface UpdateMemberRoleRequest {
  role: OrgRole;
}

// =========================================================================
// Organization Invitations (EM-02)
// =========================================================================

export type InvitationStatus = 'Pending' | 'Used' | 'Expired' | 'Cancelled'

export interface Invitation {
  id: string;
  email: string;
  code?: string;
  role: OrgRole;
  status: InvitationStatus;
  createdAt: string;
  expiresAt: string;
  usedAt?: string;
  invitedByName: string;
  invitedByEmail: string;
  organizationName?: string;
  emailSent?: boolean | null;
  emailError?: string | null;
  accountExists?: boolean;
}

export interface CreateInvitationRequest {
  email: string;
  role?: OrgRole;
}

export interface InvitationSentResponse {
  invitationId: string;
  email: string;
  message: string;
}

const orgRoleLabels: Record<OrgRole, string> = {
  OrgAdmin: 'Admin',
  OrgManager: 'Manager',
  OrgUser: 'User',
}

export function getOrgRoleLabel(role: OrgRole | string): string {
  return orgRoleLabels[role as OrgRole] ?? role
}
