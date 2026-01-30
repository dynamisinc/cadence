/**
 * RoleChip - Display organization role
 *
 * Shows user's role within an organization with friendly labels.
 *
 * @module shared/components
 */
import type { FC } from 'react'
import { Chip } from '@mui/material'
import type { OrgRole } from '@/features/organizations/types'

interface RoleChipProps {
  /** Organization role */
  role: OrgRole;
  /** Chip size */
  size?: 'small' | 'medium';
}

/**
 * Map role to display label
 */
function getRoleLabel(role: OrgRole): string {
  switch (role) {
    case 'OrgAdmin':
      return 'Admin'
    case 'OrgManager':
      return 'Manager'
    case 'OrgUser':
      return 'User'
    default:
      return role
  }
}

/**
 * RoleChip component
 */
export const RoleChip: FC<RoleChipProps> = ({ role, size = 'small' }) => {
  return (
    <Chip
      label={getRoleLabel(role)}
      color="primary"
      variant="outlined"
      size={size}
      sx={{ fontWeight: 500 }}
    />
  )
}
